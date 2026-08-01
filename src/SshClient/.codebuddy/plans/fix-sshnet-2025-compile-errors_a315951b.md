---
name: fix-sshnet-2025-compile-errors
overview: 升级到 SSH.NET 2025.1.0 后，项目因 8 处破坏性 API 变更无法编译。本方案逐文件修复这些 API 用法以恢复编译与运行。
todos:
  - id: fix-sshconnection
    content: 修正 SshConnection.vb：HostKeyReceived 处理加 sender 参数，KeyboardInteractiveConnectionInfo 改 6 参数构造
    status: completed
  - id: fix-commandrunner
    content: 修正 SshCommandRunner.vb：用 StreamReader 包装输出流，Wait 改为 BeginExecute/EndExecute
    status: completed
    dependencies:
      - fix-sshconnection
  - id: fix-program-shell
    content: 修正 Program.vb 的 ServerVersion 与 SshShellSession.vb 的 ChangeWindowSize
    status: completed
    dependencies:
      - fix-sshconnection
  - id: verify-build
    content: 运行 dotnet build 验证 0 错误 0 警告
    status: completed
    dependencies:
      - fix-commandrunner
      - fix-program-shell
---

## 用户需求

修复一个 VB.NET 编写的 SSH 客户端命令行程序（c:\Users\Administrator\Downloads\SshClient）。该项目将 SSH.NET NuGet 包升级到最新版 2025.1.0 后，因部分公开 API 被移除或签名变更，导致无法编译（共 8 个编译错误）。

## 产品概述

该程序是一个命令行 SSH 客户端，支持密码/私钥认证、代理连接、交互式 Shell、单命令执行、SCP/SFTP 文件传输。修复目标是让项目在 SSH.NET 2025.1.0 下能够成功编译并保留原有功能行为。

## 核心特性（需修复的 API 用法）

- 主机密钥验证事件 `HostKeyReceived` 的处理程序需符合 `EventHandler(Of HostKeyEventArgs)` 签名（增加 `sender As Object` 参数）。
- `KeyboardInteractiveConnectionInfo` 构造函数改用 6 参数代理重载，认证方法通过 `AuthenticationMethods.Add` 添加。
- `SshCommand.OutputStream` / `ExtendedOutputStream` 现在返回 `Stream`，需使用 `StreamReader` 包装后再读取。
- `SshCommand.Wait()` 已移除，改用 `BeginExecute` / `EndExecute` 保留实时输出线程模型（或 `Execute()`）。
- `SshClient.ServerVersion` 已移至 `ConnectionInfo.ServerVersion`，通过 `Client.ConnectionInfo.ServerVersion` 访问。
- `ShellStream.SendWindowChangeRequest` 已移除，改用 `ShellStream.ChangeWindowSize(columns, rows, width, height)`。

## 技术栈

- 语言：VB.NET（项目文件 SshClient.vbproj，目标框架 net10.0）
- 依赖：SSH.NET 2025.1.0（Renci.SshNet）
- 构建：.NET SDK（dotnet build）

## 实现方案

通过逐文件修正 API 用法来消除 8 个编译错误，严格对照 SSH.NET 2025.1.0 官方 API 文档，不改动程序功能行为与用户交互逻辑。所有修改均为最小化、定点替换，避免引入新依赖或重构。

### 关键修复点与理由

1. **SshConnection.vb — HostKeyReceived 事件（行 43-53）**
`ConnectionInfo.HostKeyReceived` 委托为 `EventHandler(Of HostKeyEventArgs)`。两处 `AddHandler` 的内联 `Sub(hostKey As HostKeyEventArgs)` 与 `OnHostKeyReceived(e As HostKeyEventArgs)` 均缺少 `sender As Object` 参数，导致 BC36670 / BC31143。
修复：将处理程序改为 `Sub(sender As Object, e As HostKeyEventArgs)`，并把 `OnHostKeyReceived` 方法签名改为 `(sender As Object, e As HostKeyEventArgs)`（`sender` 参数可忽略不使用）。

2. **SshConnection.vb — KeyboardInteractiveConnectionInfo 构造函数（行 133）**
2025.1.0 中 `KeyboardInteractiveConnectionInfo` 不存在接受 `params AuthenticationMethod[]` 的 7 参数重载。合法代理重载为 6 参数 `(host, port, username, proxyType, proxyHost, proxyPort)`。代码第 137 行已通过 `_connInfo.AuthenticationMethods.Add(Kauth)` 添加键盘交互认证方法。
修复：移除构造函数调用末尾多余的 `authMethods` 参数（第 7 个实参），保留 6 参数形式。

3. **SshCommandRunner.vb — 输出流（行 39、54）**
`SshCommand.OutputStream` 与 `ExtendedOutputStream` 类型由原先的 `StreamReader` 变为 `Stream`。当前代码直接 `Using reader As StreamReader = cmd.OutputStream` 触发 BC30311。
修复：改为 `Using reader As New StreamReader(cmd.OutputStream)`，扩展流同理 `New StreamReader(cmd.ExtendedOutputStream)`，其余读取逻辑（缓冲区循环）保持不变。

4. **SshCommandRunner.vb — 命令执行等待（行 68）**
`SshCommand.Wait()` 在 2025.1.0 已移除。为保留原有“后台线程实时读取 stdout/stderr 流”的设计，改用异步执行模型：
`Dim ar As IAsyncResult = cmd.BeginExecute()`，后台线程持续泵送 `OutputStream`/`ExtendedOutputStream`，主线程以 `ar.AsyncWaitHandle.WaitOne()` 阻塞等待完成，最后调用 `cmd.EndExecute(ar)` 获取结果并取 `ExitStatus`。该方案对现有线程结构改动最小、行为等价。

5. **Program.vb — ServerVersion（行 135）**
`SshClient.ServerVersion` 已不存在；`ServerVersion` 是 `ConnectionInfo` 的属性。
修复：`client.ServerVersion` 改为 `client.ConnectionInfo.ServerVersion`。

6. **SshShellSession.vb — 窗口尺寸变更（行 199）**
`ShellStream.SendWindowChangeRequest` 已移除，替代为 `ChangeWindowSize(columns, rows, width, height)`。
修复：`_stream.SendWindowChangeRequest(CUInt(cols), CUInt(rows), 0UI, 0UI)` 改为 `_stream.ChangeWindowSize(CUInt(cols), CUInt(rows), 0UI, 0UI)`。

## 实现注意事项

- 仅做定点 API 替换，不重构流程、不改动命令行参数解析与业务逻辑，控制改动范围与回归风险。
- 后台输出线程在 `BeginExecute` 模型下与之前 `Wait` 模型行为一致；需注意 `EndExecute` 后再等待 `stdoutTask`/`stderrTask` 读取完毕（原代码已 `Wait()` 两个任务）。
- 修改后需再次运行 `dotnet build` 验证 0 错误、0 警告（当前 0 warning）。

## 架构设计

项目为单程序集控制台应用，无新增模块；本次为兼容性修复，不引入架构变更。数据流保持不变：
Program 解析参数 → SshConnection 建立连接 → 交互式 Shell（SshShellSession）或单命令（SshCommandRunner）或文件传输（SshFileTransfer）。

## 目录结构（受影响文件）

```
c:\Users\Administrator\Downloads\SshClient\
├── SshConnection.vb      # [MODIFY] 修正 HostKeyReceived 事件处理签名；KeyboardInteractiveConnectionInfo 改用 6 参数构造函数
├── SshCommandRunner.vb   # [MODIFY] OutputStream/ExtendedOutputStream 用 StreamReader 包装；Wait() 改为 BeginExecute/EndExecute
├── Program.vb            # [MODIFY] client.ServerVersion 改为 client.ConnectionInfo.ServerVersion
└── SshShellSession.vb    # [MODIFY] SendWindowChangeRequest 改为 ChangeWindowSize
```

（SshOptions.vb、SshFileTransfer.vb 无需改动，当前编译无误。）