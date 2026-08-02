' ============================================================================
' SshCommandRunner.vb - 单命令执行模式
'
' 通过 SSH.NET 的 RunCommand 方法在远程主机上执行单条命令，
' 将 stdout/stderr 实时输出到本地控制台，并返回退出码。
' ============================================================================

Imports Renci.SshNet

''' <summary>
''' 单命令执行器。
''' </summary>
Public Class SshCommandRunner

    Private _client As Renci.SshNet.SshClient
    Private _verbose As Boolean

    Public Sub New(client As Renci.SshNet.SshClient, Optional verbose As Boolean = False)
        _client = client
        _verbose = verbose
    End Sub

    ''' <summary>
    ''' 执行单条远程命令并输出结果。
    ''' </summary>
    ''' <param name="command">要执行的命令字符串。</param>
    ''' <returns>远程命令的退出码。连接失败返回 -1。</returns>
    Public Function Run(command As String) As Integer
        If _verbose Then
            Console.Error.WriteLine($"[调试] 执行命令: {command}")
        End If

        Dim cmd As SshCommand = _client.RunCommand(command)

        ' 异步读取输出避免大输出时阻塞
        Dim stdoutTask As System.Threading.Tasks.Task = System.Threading.Tasks.Task.Run(
            Sub()
                Using reader As New System.IO.StreamReader(cmd.OutputStream)
                    Dim buffer(8192) As Char
                    Dim count As Integer
                    Do
                        count = reader.Read(buffer, 0, buffer.Length)
                        If count > 0 Then
                            Console.Out.Write(buffer, 0, count)
                        End If
                    Loop Until count = 0
                    Console.Out.Flush()
                End Using
            End Sub)

        Dim stderrTask As System.Threading.Tasks.Task = System.Threading.Tasks.Task.Run(
            Sub()
                Using reader As New System.IO.StreamReader(cmd.ExtendedOutputStream)
                    Dim buffer(8192) As Char
                    Dim count As Integer
                    Do
                        count = reader.Read(buffer, 0, buffer.Length)
                        If count > 0 Then
                            Console.Error.Write(buffer, 0, count)
                        End If
                    Loop Until count = 0
                    Console.Error.Flush()
                End Using
            End Sub)

        ' 异步执行命令，使后台线程能够持续泵送输出流
        Dim asyncResult As IAsyncResult = cmd.BeginExecute()
        ' 阻塞等待命令执行完成
        asyncResult.AsyncWaitHandle.WaitOne()
        cmd.EndExecute(asyncResult)

        ' 确保输出读取完毕
        stdoutTask.Wait()
        stderrTask.Wait()

        Dim exitCode As Integer = If(cmd.ExitStatus >= 0, cmd.ExitStatus, -1)

        If cmd.Error IsNot Nothing AndAlso Not String.IsNullOrEmpty(cmd.Error) Then
            ' RunCommand 的 Error 属性可能在某些情况下包含额外信息
            ' 但通过 ExtendedOutputStream 已经读取了 stderr，此处仅做备份检查
        End If

        If _verbose Then
            Console.Error.WriteLine($"[调试] 命令退出码: {exitCode}")
        End If

        cmd.Dispose()
        Return exitCode
    End Function

End Class
