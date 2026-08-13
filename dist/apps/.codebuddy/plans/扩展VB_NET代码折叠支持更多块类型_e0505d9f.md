---
name: 扩展VB.NET代码折叠支持更多块类型
overview: 修复 CodeFolder.computeVbNet 的核心 bug：openers 检测在 endMatch 之前，导致 End Function/End Class 等行被 openers 误匹配为新 opener，外层真正的声明块永远不被关闭，只有用 Next 关闭的 For 能折叠。重排检测顺序并新增 Else/ElseIf 子块折叠，让 if/else/elseif/sub/function/loop/#region/class/module/structure/enum/namespace 全部支持折叠。
todos:
  - id: rewrite-computevbnet
    content: 重写 computeVbNet 方法：修复检测顺序、扩展栈结构、新增 Else/ElseIf 子块折叠、For Each 正则修复、Select Case 归一化、单行 If 注释修复
    status: completed
  - id: build-and-verify
    content: 运行 build.cmd 编译并使用 [subagent:code-explorer] 验证所有块类型折叠完整性
    status: completed
    dependencies:
      - rewrite-computevbnet
---

## 用户需求

用户报告代码编辑器的代码块折叠功能不完善，目前仅支持 `For` 循环代码块的折叠。需要修改 TypeScript 代码，使 `if`、`else`、`elseif`、`sub`、`function`、`loop`、`#region`、`class`、`module`、`structure`、`enum`、`namespace` 这些代码块也都支持折叠。

## 产品概述

这是一个基于 TypeScript + DOM 的轻量级代码编辑器，代码折叠功能由 `src/features/CodeFolder.ts` 的 `computeVbNet` 方法实现，使用基于栈的状态机分析 VB.NET 代码结构并生成 FoldRange 数组，由 `Editor.ts` 的 `renderGutter` 方法在行号区渲染折叠/展开标记。

## 核心功能

- 修复 VB.NET 代码块折叠的核心排序 bug，使所有声明的块类型（if/else/elseif/sub/function/loop/region/class/module/structure/enum/namespace）都能正确识别并生成折叠范围
- 支持 Else/ElseIf 作为 If 块的子块单独折叠
- 保持 For、#Region 等已有功能的正常工作

## 技术栈

- 语言: TypeScript (target: ES2019, module: none, strict mode)
- 构建: tsc 编译器 (build.cmd)，输出到 dist/editor.bundle.js
- 无第三方依赖，纯正则 + 栈状态机实现

## 根因分析

### 核心 Bug：opener 检测在 End X 检测之前执行且无 continue 保护

当前 `computeVbNet` 主循环（第 82-119 行）的执行顺序：

1. 先执行 `openMatch = openers.exec(trimmed)`，匹配则 push 到 stack（**无 continue，继续往下执行**）
2. 后执行 `endMatch = /\bEnd\s+(...)\b/i.exec(trimmed)`

对于 `End Function` 行：

- openers 正则 `/\b(...|Function|...)\b/i` 会匹配 "End Function" 中的 "Function"，push `{line:i, text:"function"}`（把 End Function 行本身当作新 opener）
- 然后 endMatch 匹配 "End Function"，kind="function"，从栈顶弹出刚 push 的项
- `i > top.line` → `i == top.line` → false → **不生成 range**
- **外层真正的 Function opener 永远不会被弹出** → Function 块无 fold range

For 循环用 `Next` 关闭（不经过 openers 误匹配路径），所以只有 For 能正常折叠——完全符合用户描述的现象。

### Bug 2：For Each 永远匹配 "For"

openers 正则 alternation 顺序为 `For|For Each`，"For" 先匹配，"For Each" 永远匹配不到。

### Bug 3：Select Case 归一化缺失

"Select Case" push 为 "select case"，但 `End Select` 的 endMatch kind 为 "select"，不匹配，永不关闭。

### Bug 4：单行 If 注释误判

`If x Then ' comment` 被 `\bThen\s* 检测误判为单行 If（Then 后是注释不是代码），实际是多行 If。

### Bug 5：Else/ElseIf 完全未识别

当前代码没有处理 Else/ElseIf 作为折叠块边界。

## 实现方案

### 核心策略：重排检测顺序 + 扩展栈结构 + 新增 Else 子块

1. **重排检测顺序**：把所有关闭符检测（End X、Next、Loop、EndIf、Else/ElseIf）移到 opener 检测之前，每个分支带 continue。这样 `End Function` 行不会走到 opener 正则。
2. **扩展 stack 元素**：新增 `elseStarts: number[]` 字段记录 If 块内的 Else/ElseIf 行号。
3. **Else/ElseIf 子块折叠**：遇到 Else/ElseIf 时记录到栈顶 if 的 elseStarts；关闭 If 时为每个 elseStart 生成子 range。
4. **For Each / Select Case 归一化**：调整正则顺序和 push 的 text 值。
5. **单行 If 修复**：去除行尾注释后再判断。

## 实现注意事项

- **性能**：单次 O(n) 遍历，n 为行数。每个分支仅做一次正则匹配 + 可选的栈回溯（最坏 O(d)，d 为嵌套深度）。总体 O(n*d)，d 通常很小，无性能问题。
- **向后兼容**：不修改 FoldRange 接口、computeFoldRanges 入口、其他语言的 compute 方法。仅修改 computeVbNet 内部实现。
- **边界处理**：ElseIf 也是一种 Else 子块的开始；For Each 的 kind 标签用 "for each" 区分但栈 text 统一为 "for"（Next 关闭）；空行和注释行跳过。

## 目录结构

```
g:\DevAgent\code-editor\
├── src\features\
│   └── CodeFolder.ts    # [MODIFY] 重写 computeVbNet 方法，修复检测顺序+新增Else子块+For Each修复
└── build.cmd             # [RUN] 编译 TypeScript 到 dist/editor.bundle.js
```

### CodeFolder.ts 修改详情

**修改点 1 — stack 元素结构（第 47 行）**:

```ts
// 之前: const stack: { line: number; text: string }[] = [];
// 之后: const stack: { line: number; text: string; elseStarts: number[] }[] = [];
```

**修改点 2 — 重排主循环检测顺序（第 55-197 行）**:
重写整个 for 循环体，检测顺序改为：

1. 跳过注释/REM
2. 去除行尾注释得到 codePart
3. Region open → push，continue
4. Region close → pop，continue
5. **End X 语句**（End Function/Class/Sub/...）→ pop 匹配 opener + 生成 else 子 ranges，continue
6. 独立 `End`（If/Select 等）→ pop，continue
7. `Next` → pop for，continue
8. `Loop` → pop do/while，continue
9. `EndIf` / `End If`（单独处理，因为 endMatch 的 `End\s+If` 可能与 `End\s+(...)` 的通用 End 检测重叠）→ pop if + else 子 ranges，continue
10. **Else / ElseIf**（新增）→ 找栈顶 if，记录 elseStart，continue
11. **opener 检测**（放最后）→ 用 codePart 匹配，单行 If 用 codePart 判断，push 带 elseStarts:[]

**修改点 3 — openers 正则修复（第 50 行）**:

```ts
// For Each 放在 For 之前
const openers = /\b(Class|Module|Structure|Interface|Enum|Namespace|Sub|Function|Property|Operator|Event|Get|Set|AddHandler|RemoveHandler|RaiseEvent|Using|While|For Each|For|If|Select Case|Select|Try|SyncLock|With|Do)\b/i;
```

**修改点 4 — push 时归一化 text 值**:

```ts
// For Each → text:"for", kind 标签保留区分
// Select Case / Select → text:"select"
let text = openMatch[1].toLowerCase();
if (text === "for each") text = "for";
if (text === "select case") text = "select";
stack.push({ line: i, text, elseStarts: [] });
```

**修改点 5 — Else/ElseIf 处理（新增分支）**:

```ts
// Else / ElseIf: 记录到栈顶 if 的 elseStarts
if (/^(Else|ElseIf)\b/i.test(codePart)) {
    for (let k = stack.length - 1; k >= 0; k--) {
        if (stack[k].text === "if") {
            stack[k].elseStarts.push(i);
            break;
        }
    }
    continue;
}
```

**修改点 6 — 关闭 If 时生成 else 子 ranges**:
在 `End If` / `EndIf` 关闭 if 块时，除了生成主 range [ifLine, endIfLine]，还为 elseStarts 生成子 ranges：

```ts
// 为每个 else 分支生成子 range
const allPoints = [top.line, ...top.elseStarts, i];
for (let e = 0; e < allPoints.length - 1; e++) {
    const subStart = allPoints[e];
    const subEnd = allPoints[e + 1] - 1;
    if (subEnd > subStart) {
        ranges.push({ startLine: subStart, endLine: subEnd, collapsedText: "...", kind: "if-block" });
    }
}
```

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 对修复后的 CodeFolder.ts 进行交叉验证，确认所有 VB.NET 块类型的折叠范围计算逻辑正确无遗漏
- Expected outcome: 生成完整的块类型覆盖报告，确认 if/else/elseif/sub/function/loop/region/class/module/structure/enum/namespace 全部正确生成 fold ranges