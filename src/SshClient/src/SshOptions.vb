' ============================================================================
' SshOptions.vb - SSH 客户端配置与命令行解析
'
' 支持的参数:
'   -h, --host <host>           主机地址（必填）
'   -p, --port <port>           端口号，默认 22
'   -u, --user <user>           用户名（必填）
'   -P, --password <pwd>        密码认证
'   -k, --keyfile <path>        私钥文件路径
'   -K, --passphrase <pwd>      私钥密码（如有）
'   -c, --command <cmd>         执行单条命令后退出
'   -t, --timeout <sec>         连接超时秒数，默认 15
'   --keepalive <sec>           KeepAlive 间隔秒数，默认 10
'   --no-host-check             跳过主机密钥验证（危险！仅用于测试）
'   --known-hosts <path>        known_hosts 文件路径
'   --proxy <type>              代理类型: none|http|socks4|socks5
'   --proxy-host <host>         代理主机
'   --proxy-port <port>         代理端口
'   --proxy-user <user>         代理用户名（可选）
'   --proxy-pass <pwd>          代理密码（可选）
'   -v, --verbose               详细输出
'   --help                      显示帮助
' ============================================================================

Imports System.Collections.Generic
Imports System.IO
Imports System.Net

''' <summary>
''' SSH 客户端所有配置选项。
''' </summary>
Public Class SshOptions

    ' --- 连接参数 ---
    Public Property Host As String = ""
    Public Property Port As Integer = 22
    Public Property UserName As String = ""
    Public Property Password As String = ""
    Public Property KeyFilePath As String = ""
    Public Property Passphrase As String = ""

    ' --- 运行模式 ---
    Public Property Command As String = ""
    Public Property Interactive As Boolean = True

    ' --- 超时与 KeepAlive ---
    Public Property ConnectTimeoutSec As Integer = 15
    Public Property KeepAliveIntervalSec As Integer = 10

    ' --- 主机密钥验证 ---
    Public Property SkipHostKeyCheck As Boolean = False
    Public Property KnownHostsPath As String = ""

    ' --- 代理 ---
    Public Property ProxyType As String = "none"
    Public Property ProxyHost As String = ""
    Public Property ProxyPort As Integer = 0
    Public Property ProxyUser As String = ""
    Public Property ProxyPass As String = ""

    ' --- 其他 ---
    Public Property Verbose As Boolean = False

    ''' <summary>是否使用密码认证。</summary>
    Public ReadOnly Property UsePasswordAuth As Boolean
        Get
            Return String.IsNullOrEmpty(KeyFilePath) AndAlso Not String.IsNullOrEmpty(Password)
        End Get
    End Property

    ''' <summary>是否使用密钥认证。</summary>
    Public ReadOnly Property UseKeyAuth As Boolean
        Get
            Return Not String.IsNullOrEmpty(KeyFilePath)
        End Get
    End Property

    ''' <summary>是否使用代理。</summary>
    Public ReadOnly Property UseProxy As Boolean
        Get
            Return Not String.IsNullOrEmpty(ProxyType) AndAlso
                   ProxyType <> "none" AndAlso Not String.IsNullOrEmpty(ProxyHost)
        End Get
    End Property

    ''' <summary>验证配置完整性。</summary>
    Public Function Validate(ByRef errMsg As String) As Boolean
        If String.IsNullOrEmpty(Host) Then
            errMsg = "缺少主机地址 (--host)"
            Return False
        End If
        If String.IsNullOrEmpty(UserName) Then
            errMsg = "缺少用户名 (--user)"
            Return False
        End If
        If Not UsePasswordAuth AndAlso Not UseKeyAuth Then
            errMsg = "需要指定认证方式: 密码 (--password) 或私钥 (--keyfile)"
            Return False
        End If
        If UseKeyAuth AndAlso Not File.Exists(KeyFilePath) Then
            errMsg = $"私钥文件不存在: {KeyFilePath}"
            Return False
        End If
        If Port < 1 OrElse Port > 65535 Then
            errMsg = $"端口号无效: {Port}"
            Return False
        End If
        Return True
    End Function

End Class


''' <summary>
''' 命令行参数解析器。
''' </summary>
Public Module SshOptionParser

    ''' <summary>
    ''' 解析命令行参数。
    ''' 用法: SshClient -h <host> -u <user> [options] [-c <command>]
    ''' </summary>
    Public Function Parse(args As String(), ByRef options As SshOptions) As Boolean
        options = New SshOptions()

        If args Is Nothing OrElse args.Length = 0 Then
            ShowHelp()
            Return False
        End If

        Dim i As Integer = 0
        Do While i < args.Length

            Dim arg As String = args(i)
            Select Case arg

                ' --- 帮助 ---
                Case "--help", "-?", "/?"
                    ShowHelp()
                    Return False

                ' --- 主机 ---
                Case "-h", "--host"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --host 参数值")
                    options.Host = args(i + 1) : i += 1

                ' --- 端口 ---
                Case "-p", "--port"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --port 参数值")
                    If Not Integer.TryParse(args(i + 1), options.Port) Then
                        Return ParseError($"无效的端口号: {args(i + 1)}")
                    End If
                    i += 1

                ' --- 用户名 ---
                Case "-u", "--user"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --user 参数值")
                    options.UserName = args(i + 1) : i += 1

                ' --- 密码 ---
                Case "-P", "--password"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --password 参数值")
                    options.Password = args(i + 1) : i += 1

                ' --- 私钥文件 ---
                Case "-k", "--keyfile"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --keyfile 参数值")
                    options.KeyFilePath = args(i + 1) : i += 1

                ' --- 私钥密码 ---
                Case "-K", "--passphrase"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --passphrase 参数值")
                    options.Passphrase = args(i + 1) : i += 1

                ' --- 单命令执行 ---
                Case "-c", "--command"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --command 参数值")
                    ' 支持引号包裹的多词命令，也支持后续所有参数拼作为命令
                    Dim cmdParts As New List(Of String)
                    i += 1
                    Do While i < args.Length
                        cmdParts.Add(args(i))
                        i += 1
                    Loop
                    options.Command = String.Join(" ", cmdParts)
                    options.Interactive = False
                    Exit Do

                ' --- 连接超时 ---
                Case "-t", "--timeout"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --timeout 参数值")
                    If Not Integer.TryParse(args(i + 1), options.ConnectTimeoutSec) Then
                        Return ParseError($"无效的超时秒数: {args(i + 1)}")
                    End If
                    i += 1

                ' --- KeepAlive ---
                Case "--keepalive"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --keepalive 参数值")
                    If Not Integer.TryParse(args(i + 1), options.KeepAliveIntervalSec) Then
                        Return ParseError($"无效的 KeepAlive 间隔: {args(i + 1)}")
                    End If
                    i += 1

                ' --- 主机密钥验证 ---
                Case "--no-host-check"
                    options.SkipHostKeyCheck = True

                Case "--known-hosts"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --known-hosts 参数值")
                    options.KnownHostsPath = args(i + 1) : i += 1

                ' --- 代理 ---
                Case "--proxy"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --proxy 参数值")
                    options.ProxyType = args(i + 1).ToLowerInvariant() : i += 1

                Case "--proxy-host"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --proxy-host 参数值")
                    options.ProxyHost = args(i + 1) : i += 1

                Case "--proxy-port"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --proxy-port 参数值")
                    If Not Integer.TryParse(args(i + 1), options.ProxyPort) Then
                        Return ParseError($"无效的代理端口: {args(i + 1)}")
                    End If
                    i += 1

                Case "--proxy-user"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --proxy-user 参数值")
                    options.ProxyUser = args(i + 1) : i += 1

                Case "--proxy-pass"
                    If i + 1 >= args.Length Then Return ParseError("缺少 --proxy-pass 参数值")
                    options.ProxyPass = args(i + 1) : i += 1

                ' --- 详细输出 ---
                Case "-v", "--verbose"
                    options.Verbose = True

                Case Else
                    Console.Error.WriteLine($"未知参数: {arg}")
                    ShowHelp()
                    Return False

            End Select

            i += 1
        Loop

        Return True
    End Function

    Private Function ParseError(msg As String) As Boolean
        Console.Error.WriteLine($"参数错误: {msg}")
        Console.Error.WriteLine("使用 --help 查看帮助。")
        Return False
    End Function

    ''' <summary>显示帮助信息。</summary>
    Public Sub ShowHelp()
        Console.WriteLine(
"SSH.NET 客户端 - VB.NET 命令行 SSH 工具

用法:
  SshClient -h <host> -u <user> [认证] [选项] [-c <command>]

必填参数:
  -h, --host <host>           目标主机地址
  -u, --user <user>           登录用户名

认证方式 (二选一):
  -P, --password <pwd>        密码认证
  -k, --keyfile <path>        私钥文件认证
  -K, --passphrase <pwd>      私钥密码 (如需)

运行模式:
  (无 -c)                     交互式 Shell 模式
  -c, --command <cmd>         执行单条命令后退出

连接选项:
  -p, --port <port>           SSH 端口，默认 22
  -t, --timeout <sec>         连接超时秒数，默认 15
  --keepalive <sec>           KeepAlive 间隔秒数，默认 10

主机密钥验证:
  --no-host-check             跳过主机密钥验证 (危险!)
  --known-hosts <path>        known_hosts 文件路径

代理:
  --proxy <type>              代理类型: none|http|socks4|socks5
  --proxy-host <host>         代理主机地址
  --proxy-port <port>         代理端口
  --proxy-user <user>         代理用户名 (可选)
  --proxy-pass <pwd>          代理密码 (可选)

其他:
  -v, --verbose               详细输出
  --help                      显示本帮助

示例:
  # 密码登录，交互式 Shell
  SshClient -h 192.168.1.100 -u root -P mypassword

  # 私钥登录，执行命令后退出
  SshClient -h server.com -u admin -k ~/.ssh/id_rsa -c ""ls -la /var/log""

  # 通过 SOCKS5 代理连接
  SshClient -h 10.0.0.5 -u user -P pwd --proxy socks5 --proxy-host 127.0.0.1 --proxy-port 1080

  # 使用非标准端口，跳过主机密钥检查
  SshClient -h host.com -p 2222 -u deploy -k ./deploy_key --no-host-check -c ""systemctl status nginx""")
    End Sub

End Module
