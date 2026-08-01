' ============================================================================
' Program.vb - SSH 客户端命令行入口
'
' 模式:
'   1. 交互式 Shell (默认): 建立连接后进入交互式伪终端
'   2. 单命令执行 (-c): 在远程执行一条命令并返回退出码
'
' 额外命令 (交互模式下输入):
'   :upload <local> <remote>     上传文件 (SCP)
'   :download <remote> <local>   下载文件 (SCP)
'   :exit / :quit                退出会话
'
' 用法示例:
'   SshClient -h 192.168.1.100 -u root -P mypassword
'   SshClient -h server.com -u admin -k ~/.ssh/id_rsa -c "ls -la /var/log"
'   SshClient -h host.com -p 2222 -u deploy -k ./deploy_key --no-host-check
' ============================================================================

Imports System
Imports System.IO
Imports Renci.SshNet
Imports Renci.SshNet.Common

Module Program

    Function Main(args As String()) As Integer

        ' 显示 Banner
        Console.WriteLine("SSH.NET 客户端 v1.0  (VB.NET / .NET 10)")
        Console.WriteLine()

        ' 解析命令行参数
        Dim options As SshOptions = Nothing
        If Not SshOptionParser.Parse(args, options) Then
            Return 1
        End If

        ' 验证配置
        Dim errMsg As String = ""
        If Not options.Validate(errMsg) Then
            Console.Error.WriteLine($"配置错误: {errMsg}")
            Console.Error.WriteLine("使用 --help 查看帮助。")
            Return 1
        End If

        ' 显示连接信息
        If options.Verbose Then
            Console.Error.WriteLine($"[调试] 主机: {options.Host}:{options.Port}")
            Console.Error.WriteLine($"[调试] 用户: {options.UserName}")
            Console.Error.WriteLine($"[调试] 认证: {If(options.UseKeyAuth, $"私钥({options.KeyFilePath})", "密码")}")
            If options.UseProxy Then
                Console.Error.WriteLine($"[调试] 代理: {options.ProxyType}://{options.ProxyHost}:{options.ProxyPort}")
            End If
            Console.Error.WriteLine($"[调试] 超时: {options.ConnectTimeoutSec}s, KeepAlive: {options.KeepAliveIntervalSec}s")
            Console.Error.WriteLine()
        End If

        ' 建立连接
        Dim connection As SshConnection = Nothing
        Try
            connection = New SshConnection(options)
            Console.WriteLine($"正在连接 {options.Host}:{options.Port} ...")

            connection.Connect()
            Console.WriteLine($"已连接! (服务器: {GetServerVersion(connection.Client)})")
            Console.WriteLine()

        Catch ex As SshConnectionException
            Console.Error.WriteLine($"SSH 连接失败: {ex.Message}")
            If options.Verbose Then Console.Error.WriteLine($"[详细] {ex.ToString()}")
            Return 2

        Catch ex As Renci.SshNet.Common.SshAuthenticationException
            Console.Error.WriteLine($"认证失败: {ex.Message}")
            If options.Verbose Then Console.Error.WriteLine($"[详细] {ex.ToString()}")
            Return 3

        Catch ex As Exception
            Console.Error.WriteLine($"连接错误: {ex.Message}")
            If options.Verbose Then Console.Error.WriteLine($"[详细] {ex.ToString()}")
            Return 4
        End Try

        ' 根据模式分发
        Dim exitCode As Integer

        Try
            If options.Interactive Then
                ' --- 交互式 Shell 模式 ---
                exitCode = RunInteractiveMode(connection, options)
            Else
                ' --- 单命令执行模式 ---
                Dim runner As New SshCommandRunner(connection.Client, options.Verbose)
                exitCode = runner.Run(options.Command)
            End If

        Catch ex As Exception
            Console.Error.WriteLine($"执行错误: {ex.Message}")
            If options.Verbose Then Console.Error.WriteLine($"[详细] {ex.ToString()}")
            exitCode = 5

        Finally
            connection.Disconnect()
            Console.Error.WriteLine("连接已断开。")
        End Try

        Return exitCode
    End Function

    ''' <summary>
    ''' 交互式 Shell 模式。
    ''' </summary>
    Private Function RunInteractiveMode(connection As SshConnection, options As SshOptions) As Integer
        Console.WriteLine("进入交互式 Shell (输入 :exit 退出)")
        Console.WriteLine(New String("-"c, 50))

        ' 处理特殊内置命令
        Dim line As String = options.Command  ' 通常为空

        ' 启动 Shell 会话
        Using session As New SshShellSession(connection.Client, options.Verbose)
            session.Start()
        End Using

        Console.WriteLine()
        Console.WriteLine("Shell 会话已结束。")
        Return 0
    End Function

    ''' <summary>
    ''' 获取远程服务器 SSH 版本信息。
    ''' </summary>
    Private Function GetServerVersion(client As Renci.SshNet.SshClient) As String
        Try
            Return client.ConnectionInfo.ServerVersion
        Catch
            Return "unknown"
        End Try
    End Function

End Module
