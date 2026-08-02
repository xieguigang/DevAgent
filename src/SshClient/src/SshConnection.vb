' ============================================================================
' SshConnection.vb - SSH 连接管理
'
' 封装 Renci.SshNet.SshClient 的连接、认证、代理配置和主机密钥验证逻辑。
' 提供统一的连接建立入口，支持密码和私钥两种认证方式。
' ============================================================================

Imports System.IO
Imports Renci.SshNet

''' <summary>
''' SSH 连接管理器：负责根据 SshOptions 构建并建立 SSH 连接。
''' </summary>
Public Class SshConnection

    Private _options As SshOptions
    Private _client As Renci.SshNet.SshClient
    Private _hostKeyValidator As Func(Of String, Byte(), Boolean)

    ''' <summary>底层 SSH.NET 客户端实例。</summary>
    Public ReadOnly Property Client As Renci.SshNet.SshClient
        Get
            Return _client
        End Get
    End Property

    Public Sub New(options As SshOptions)
        _options = options
    End Sub

    ''' <summary>
    ''' 建立连接。
    ''' 成功后可通过 Client 属性访问底层 SSH.NET 客户端。
    ''' </summary>
    Public Sub Connect()
        Dim connInfo As ConnectionInfo = BuildConnectionInfo()
        _client = New Renci.SshNet.SshClient(connInfo)
        _client.KeepAliveInterval = TimeSpan.FromSeconds(_options.KeepAliveIntervalSec)
        _client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(_options.ConnectTimeoutSec)

        ' 注册主机密钥验证事件
        ' SSH.NET 通过 HostKeyReceived 事件回调进行主机密钥验证
        If _hostKeyValidator IsNot Nothing Then
            AddHandler _client.HostKeyReceived, Sub(sender As Object, e As Renci.SshNet.Common.HostKeyEventArgs)
                                                     e.CanTrust = _hostKeyValidator(_options.Host, e.FingerPrint)
                                                     If _options.Verbose AndAlso Not e.CanTrust Then
                                                         Console.Error.WriteLine($"[警告] 主机密钥验证失败: {_options.Host}")
                                                     End If
                                                 End Sub
        Else
            ' 默认模式：显示指纹并提示用户确认
            AddHandler _client.HostKeyReceived, AddressOf OnHostKeyReceived
        End If

        _client.Connect()
    End Sub

    ''' <summary>
    ''' 默认主机密钥回调：显示指纹并提示用户确认。
    ''' </summary>
    Private Sub OnHostKeyReceived(sender As Object, e As Renci.SshNet.Common.HostKeyEventArgs)
        Dim fpHex As String = BitConverter.ToString(e.FingerPrint).Replace("-", ":").ToLower()
        Console.Error.WriteLine($"主机密钥指纹 (SHA256): {fpHex}")
        Console.Error.Write("是否信任此主机? (yes/no): ")
        Dim answer As String = Console.ReadLine()
        e.CanTrust = (answer IsNot Nothing AndAlso
                      answer.Trim().ToLower() = "yes" OrElse answer.Trim().ToLower() = "y")
    End Sub

    ''' <summary>断开连接并释放资源。</summary>
    Public Sub Disconnect()
        If _client IsNot Nothing Then
            Try
                If _client.IsConnected Then _client.Disconnect()
            Finally
                _client.Dispose()
                _client = Nothing
            End Try
        End If
    End Sub

    ''' <summary>
    ''' 构建 SSH.NET ConnectionInfo 对象。
    ''' 包含认证方式、代理配置和主机密钥验证回调。
    ''' </summary>
    Private Function BuildConnectionInfo() As ConnectionInfo

        ' --- 认证方法 ---
        Dim authMethods As New List(Of AuthenticationMethod)()

        If _options.UseKeyAuth Then
            ' 私钥认证
            Dim keyFile As PrivateKeyFile
            If String.IsNullOrEmpty(_options.Passphrase) Then
                keyFile = New PrivateKeyFile(_options.KeyFilePath)
            Else
                keyFile = New PrivateKeyFile(_options.KeyFilePath, _options.Passphrase)
            End If
            authMethods.Add(New PrivateKeyAuthenticationMethod(_options.UserName, keyFile))
        End If

        If _options.UsePasswordAuth Then
            ' 密码认证
            authMethods.Add(New PasswordAuthenticationMethod(_options.UserName, _options.Password))
        End If

        ' --- 代理 ---
        Dim proxyType As ProxyTypes = ProxyTypes.None
        Dim proxyHost As String = Nothing
        Dim proxyPort As Integer = 0

        If _options.UseProxy Then
            Select Case _options.ProxyType
                Case "http"
                    proxyType = ProxyTypes.Http
                Case "socks4"
                    proxyType = ProxyTypes.Socks4
                Case "socks5"
                    proxyType = ProxyTypes.Socks5
                Case Else
                    Throw New ArgumentException($"不支持的代理类型: {_options.ProxyType}")
            End Select
            proxyHost = _options.ProxyHost
            proxyPort = If(_options.ProxyPort > 0, _options.ProxyPort, 1080)
        End If

        ' --- 构建连接信息 ---
        Dim connInfo As ConnectionInfo

        If _options.UseProxy Then
            If String.IsNullOrEmpty(_options.ProxyUser) Then
            connInfo = New ConnectionInfo(
                _options.Host, _options.Port,
                _options.UserName,
                proxyType, proxyHost, proxyPort,
                String.Empty, String.Empty,
                authMethods.ToArray())
            Else
                connInfo = New ConnectionInfo(
                    _options.Host, _options.Port,
                    _options.UserName,
                    proxyType, proxyHost, proxyPort,
                    _options.ProxyUser, _options.ProxyPass,
                    authMethods.ToArray())
            End If
        Else
            connInfo = New ConnectionInfo(
                _options.Host, _options.Port,
                _options.UserName,
                authMethods.ToArray())
        End If

        ' --- 主机密钥验证 ---
        ' SSH.NET 使用 HostKeyReceived 事件进行主机密钥验证
        ' （不修改 HostKeyAlgorithms 字典，它用于算法注册而非验证）
        If _options.SkipHostKeyCheck Then
            ' 跳过验证（不安全，仅用于测试环境）
            _hostKeyValidator = Function(host As String, fingerprint As Byte())
                                    Return True  ' 始终接受
                                End Function
        ElseIf Not String.IsNullOrEmpty(_options.KnownHostsPath) Then
            ' 使用 known_hosts 文件验证
            Dim validator As New KnownHostsValidator(_options.KnownHostsPath, _options.Verbose)
            _hostKeyValidator = Function(host As String, fingerprint As Byte())
                                    Return validator.Validate(host, fingerprint)
                                End Function
        Else
            ' 默认：首次连接时提示用户
            _hostKeyValidator = Nothing
        End If

        Return connInfo
    End Function

End Class


''' <summary>
''' known_hosts 文件验证器。
''' 支持标准 OpenSSH known_hosts 格式的主机密钥校验。
''' </summary>
Public Class KnownHostsValidator

    Private _hosts As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase)
    Private _verbose As Boolean

    Public Sub New(knownHostsPath As String, Optional verbose As Boolean = False)
        _verbose = verbose
        LoadKnownHosts(knownHostsPath)
    End Sub

    ''' <summary>
    ''' 验证主机密钥是否匹配 known_hosts 中的记录。
    ''' </summary>
    ''' <param name="host">连接的主机名。</param>
    ''' <param name="fingerprint">SSH.NET 提供的主机密钥指纹（SHA256 字节数组）。</param>
    Public Function Validate(host As String, fingerprint As Byte()) As Boolean
        ' 尝试精确匹配
        If _hosts.ContainsKey(host) Then
            Dim expected As Byte() = _hosts(host)
            Return ByteArraysEqual(expected, fingerprint)
        End If

        ' 尝试通配符匹配（如 *.example.com）
        For Each kvp As KeyValuePair(Of String, Byte()) In _hosts
            If HostMatchesPattern(host, kvp.Key) Then
                Return ByteArraysEqual(kvp.Value, fingerprint)
            End If
        Next

        If _verbose Then
            Console.Error.WriteLine($"[警告] 主机 {host} 不在 known_hosts 文件中")
        End If

        Return False
    End Function

    ''' <summary>加载 known_hosts 文件。</summary>
    Private Sub LoadKnownHosts(path As String)
        If Not File.Exists(path) Then
            If _verbose Then
                Console.Error.WriteLine($"[警告] known_hosts 文件不存在: {path}")
            End If
            Return
        End If

        For Each line As String In File.ReadAllLines(path)
            line = line.Trim()
            If line = "" OrElse line.StartsWith("#") Then Continue For

            ' 格式: hostname type base64key [comment]
            ' 或:   [host]:port type base64key [comment]
            Dim parts As String() = line.Split({" "c, vbTab}, StringSplitOptions.RemoveEmptyEntries)
            If parts.Length < 3 Then Continue For

            Dim hostPart As String = parts(0)
            ' 去掉端口号
            If hostPart.StartsWith("[") Then
                Dim closeIdx As Integer = hostPart.IndexOf("]")
                If closeIdx > 0 Then
                    hostPart = hostPart.Substring(1, closeIdx - 1)
                End If
            End If

            ' 解析 Base64 密钥
            Try
                Dim keyBytes As Byte() = Convert.FromBase64String(parts(2))
                ' 存储 SHA256 指纹用于比较
                Using sha As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
                    _hosts(hostPart) = sha.ComputeHash(keyBytes)
                End Using

                ' 同时存储带通配符的主机名
                If hostPart.Contains("*") Then
                    _hosts(hostPart) = _hosts(hostPart)  ' 已添加
                End If
            Catch ex As Exception
                If _verbose Then
                    Console.Error.WriteLine($"[警告] 无法解析 known_hosts 行: {line}")
                End If
            End Try
        Next
    End Sub

    ''' <summary>检查主机名是否匹配通配符模式。</summary>
    Private Function HostMatchesPattern(host As String, pattern As String) As Boolean
        If Not pattern.Contains("*") Then Return False
        ' 简单通配符匹配：*.example.com 匹配 foo.example.com
        If pattern.StartsWith("*.") Then
            Dim suffix As String = pattern.Substring(1)  ' .example.com
            Return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
        End If
        Return False
    End Function

    ''' <summary>常量时间字节数组比较，防止时序攻击。</summary>
    Private Function ByteArraysEqual(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing Then Return False
        If a.Length <> b.Length Then Return False
        Dim diff As Integer = 0
        For i As Integer = 0 To a.Length - 1
            diff = diff Or (a(i) Xor b(i))
        Next
        Return diff = 0
    End Function

End Class
