' ============================================================================
' SshShellSession.vb - 交互式 Shell 会话
'
' 基于 Renci.SshNet.ShellStream 实现交互式伪终端会话。
' 将本地终端的输入转发到远程 Shell，将远程输出打印到本地控制台。
'
' 支持:
'   - 双向数据流（键盘输入 → SSH → 远程，远程 → SSH → 控制台）
'   - Ctrl+C 中断信号
'   - 终端窗口大小自适应
'   - 异步读写避免阻塞
' ============================================================================

Imports System.IO
Imports System.Text
Imports System.Threading
Imports Renci.SshNet

''' <summary>
''' 交互式 SSH Shell 会话管理器。
''' </summary>
Public Class SshShellSession

    Implements IDisposable

    Private _client As Renci.SshNet.SshClient
    Private _stream As ShellStream
    Private _verbose As Boolean
    Private _running As Boolean

    ' 终端参数
    Private Const TerminalName As String = "xterm-256color"
    Private Const DefaultCols As Integer = 120
    Private Const DefaultRows As Integer = 40

    ' 窗口尺寸监视
    Private _lastCols As Integer = DefaultCols
    Private _lastRows As Integer = DefaultRows

    ''' <summary>
    ''' 创建交互式 Shell 会话。
    ''' </summary>
    ''' <param name="client">已连接的 SSH.NET 客户端。</param>
    ''' <param name="verbose">是否输出调试信息。</param>
    Public Sub New(client As Renci.SshNet.SshClient, Optional verbose As Boolean = False)
        _client = client
        _verbose = verbose
    End Sub

    ''' <summary>
    ''' 启动交互式 Shell 会话。
    ''' 阻塞直到用户退出（输入 exit 或 Ctrl+D）。
    ''' </summary>
    Public Sub Start()
        ' 获取终端窗口大小
        Dim cols As Integer = DefaultCols
        Dim rows As Integer = DefaultRows
        Try
            Dim w As Integer = Console.WindowWidth
            Dim h As Integer = Console.WindowHeight
            If w > 0 Then cols = w
            If h > 0 Then rows = h
        Catch ex As Exception
            ' 非交互式终端（如管道重定向），使用默认值
        End Try

        ' 记录初始窗口尺寸，避免启动后立即误触发一次 resize
        _lastCols = cols
        _lastRows = rows

        ' 创建 Shell 流
        _stream = _client.CreateShellStream(
            TerminalName,
            CUInt(cols), CUInt(rows),
            0UI, 0UI,      ' 像素宽高（终端字符模式通常为 0）
            4096)           ' 缓冲区大小

        _running = True

        If _verbose Then
            Console.Error.WriteLine($"[调试] Shell 会话已启动 ({cols}x{rows})")
        End If

        ' 启动输出读取线程
        Dim outputThread As New Thread(AddressOf ReadRemoteOutput) With {
            .IsBackground = True,
            .Name = "ssh-output-reader"
        }
        outputThread.Start()

        ' 启动窗口尺寸监视线程（实时感知本地 console 窗口大小变化）
        Dim resizeThread As New Thread(AddressOf WatchResize) With {
            .IsBackground = True,
            .Name = "ssh-resize-watcher"
        }
        resizeThread.Start()

        ' 主线程负责读取本地输入并转发到远程
        ForwardLocalInput()

        _running = False

        ' 等待输出线程结束
        outputThread.Join(TimeSpan.FromMilliseconds(500))
        ' 等待窗口监视线程结束
        resizeThread.Join(TimeSpan.FromMilliseconds(500))
    End Sub

    ''' <summary>
    ''' 读取远程输出并打印到控制台。
    ''' 运行在后台线程上。
    ''' </summary>
    Private Sub ReadRemoteOutput()
        Dim buffer(4096) As Byte
        Try
            While _running AndAlso _stream IsNot Nothing
                Dim bytesRead As Integer = _stream.Read(buffer, 0, buffer.Length)
                If bytesRead > 0 Then
                    Dim text As String = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                    Console.Write(text)
                    Console.Out.Flush()
                ElseIf bytesRead = 0 Then
                    ' 流已关闭
                    Exit While
                End If
            End While
        Catch ex As Exception
            If _running Then
                Console.Error.WriteLine($"[远程输出错误] {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' 读取本地控制台输入并转发到远程 Shell。
    ''' 运行在主线程上。
    ''' </summary>
    Private Sub ForwardLocalInput()
        Try
            ' 禁用控制台回显（远程会回显），让原始按键直接传过去
            Dim oldEcho As Boolean = True
            Dim oldMode As Boolean = True
            Try
                oldEcho = Console.TreatControlCAsInput
                Console.TreatControlCAsInput = True
            Catch ex As Exception
                ' 某些终端不支持
            End Try

            While _running
                Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(intercept:=True)

                ' 处理特殊按键
                Select Case keyInfo.Key
                    Case ConsoleKey.C
                        If (keyInfo.Modifiers And ConsoleModifiers.Control) <> 0 Then
                            ' Ctrl+C → 发送 ETX (0x03)
                            _stream.WriteByte(&H3)
                            _stream.Flush()
                            Continue While
                        End If

                    Case ConsoleKey.D
                        If (keyInfo.Modifiers And ConsoleModifiers.Control) <> 0 Then
                            ' Ctrl+D → 发送 EOT (0x04)，表示 EOF
                            _stream.WriteByte(&H4)
                            _stream.Flush()
                            ' 检测远程是否已退出
                            If _stream.DataAvailable = False Then
                                Thread.Sleep(100)
                                If _stream.DataAvailable = False Then
                                    _running = False
                                    Exit While
                                End If
                            End If
                            Continue While
                        End If

                    Case ConsoleKey.Z
                        If (keyInfo.Modifiers And ConsoleModifiers.Control) <> 0 Then
                            ' Ctrl+Z → 发送 SUB (0x1A)，挂起
                            _stream.WriteByte(&H1A)
                            _stream.Flush()
                            Continue While
                        End If
                End Select

                ' 普通字符直接转发
                Dim ch As Char = keyInfo.KeyChar
                If ch <> ChrW(0) Then
                    Dim bytes As Byte() = Encoding.UTF8.GetBytes(ch)
                    _stream.Write(bytes, 0, bytes.Length)
                    _stream.Flush()
                End If
            End While

            ' 恢复控制台状态
            Try
                Console.TreatControlCAsInput = oldMode
            Catch ex As Exception
            End Try

        Catch ex As Exception
            If _running Then
                Console.Error.WriteLine($"[输入转发错误] {ex.Message}")
            End If
        End Try
    End Sub

    ''' <summary>
    ''' 动态调整终端窗口大小（如果终端大小发生变化）。
    ''' </summary>
    Public Sub SendWindowResize(cols As Integer, rows As Integer)
        If _stream IsNot Nothing Then
            Try
                _stream.ChangeWindowSize(
                    CUInt(cols), CUInt(rows),
                    0UI, 0UI)
            Catch ex As Exception
                If _verbose Then
                    Console.Error.WriteLine($"[调试] 窗口大小调整失败: {ex.Message}")
                End If
            End Try
        End If
    End Sub

    ''' <summary>
    ''' 监视本地控制台窗口大小变化并通知远程伪终端。
    ''' 运行在后台线程上，周期性轮询 _running 控制生命周期。
    ''' </summary>
    Private Sub WatchResize()
        While _running
            Try
                Dim curCols As Integer = Console.WindowWidth
                Dim curRows As Integer = Console.WindowHeight
                If curCols > 0 AndAlso curRows > 0 AndAlso
                   (curCols <> _lastCols OrElse curRows <> _lastRows) Then
                    _lastCols = curCols
                    _lastRows = curRows
                    SendWindowResize(curCols, curRows)
                    If _verbose Then
                        Console.Error.WriteLine($"[调试] 窗口大小已调整 ({curCols}x{curRows})")
                    End If
                End If
            Catch ex As Exception
                ' 非交互式终端或临时读取失败，忽略并继续轮询
            End Try
            Thread.Sleep(500)
        End While
    End Sub

#Region "IDisposable"

    Private _disposed As Boolean

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                _running = False
                If _stream IsNot Nothing Then
                    Try
                        _stream.Dispose()
                    Catch
                    End Try
                    _stream = Nothing
                End If
            End If
            _disposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

End Class
