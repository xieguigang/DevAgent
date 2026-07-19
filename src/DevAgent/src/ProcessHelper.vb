Imports System.Diagnostics
Imports System.Text

' ============================================================================
' ProcessHelper.vb - 进程执行工具模块
'
' 提供同步的进程执行能力，捕获 stdout 和 stderr，
' 用于执行 git、dotnet 等命令行工具。
' ============================================================================

''' <summary>
''' 表示进程执行的结果。
''' </summary>
Public Class ProcessResult

    ''' <summary>进程退出码。0 通常表示成功。</summary>
    Public Property ExitCode As Integer

    ''' <summary>标准输出内容。</summary>
    Public Property StdOut As String

    ''' <summary>标准错误输出内容。</summary>
    Public Property StdErr As String

    ''' <summary>进程是否成功退出（ExitCode = 0）。</summary>
    Public ReadOnly Property Success As Boolean
        Get
            Return ExitCode = 0
        End Get
    End Property

    ''' <summary>合并的输出内容（stdout + stderr）。</summary>
    Public ReadOnly Property CombinedOutput As String
        Get
            Dim sb As New StringBuilder()
            If Not String.IsNullOrEmpty(StdOut) Then
                sb.Append(StdOut)
            End If
            If Not String.IsNullOrEmpty(StdErr) Then
                If sb.Length > 0 Then sb.AppendLine()
                sb.Append(StdErr)
            End If
            Return sb.ToString()
        End Get
    End Property

End Class

''' <summary>
''' 进程执行辅助模块，提供运行外部命令的能力。
''' </summary>
Public Module ProcessHelper

    ''' <summary>
    ''' 执行一个外部命令并捕获输出。
    ''' </summary>
    ''' <param name="workingDir">工作目录。</param>
    ''' <param name="fileName">可执行文件路径或名称。</param>
    ''' <param name="arguments">命令行参数。</param>
    ''' <param name="timeoutMs">超时时间（毫秒），默认 120 秒。</param>
    Public Function Run(
        workingDir As String,
        fileName As String,
        Optional arguments As String = "",
        Optional timeoutMs As Integer = 120000
    ) As ProcessResult

        Dim psi As New ProcessStartInfo()
        psi.FileName = fileName
        psi.Arguments = arguments
        psi.WorkingDirectory = If(workingDir, Environment.CurrentDirectory)
        psi.UseShellExecute = False
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.RedirectStandardInput = True
        psi.CreateNoWindow = True

        Using p As New Process()
            p.StartInfo = psi

            Dim stdout As New StringBuilder()
            Dim stderr As New StringBuilder()

            AddHandler p.OutputDataReceived,
                Sub(s As Object, e As DataReceivedEventArgs)
                    If e.Data IsNot Nothing Then
                        stdout.AppendLine(e.Data)
                    End If
                End Sub

            AddHandler p.ErrorDataReceived,
                Sub(s As Object, e As DataReceivedEventArgs)
                    If e.Data IsNot Nothing Then
                        stderr.AppendLine(e.Data)
                    End If
                End Sub

            Try
                p.Start()
            Catch ex As Exception
                Return New ProcessResult With {
                    .ExitCode = -1,
                    .StdOut = "",
                    .StdErr = "Failed to start process '" & fileName & "': " & ex.Message
                }
            End Try

            ' Close stdin to prevent the process from waiting for input
            Try
                p.StandardInput.Close()
            Catch
            End Try

            p.BeginOutputReadLine()
            p.BeginErrorReadLine()

            If Not p.WaitForExit(timeoutMs) Then
                Try
                    p.Kill()
                Catch
                End Try
                Return New ProcessResult With {
                    .ExitCode = -1,
                    .StdOut = stdout.ToString(),
                    .StdErr = "Process timed out after " & timeoutMs & "ms" &
                              If(stderr.Length > 0, Environment.NewLine & stderr.ToString(), "")
                }
            End If

            ' Ensure async read callbacks complete
            p.WaitForExit()

            Return New ProcessResult With {
                .ExitCode = p.ExitCode,
                .StdOut = stdout.ToString(),
                .StdErr = stderr.ToString()
            }
        End Using
    End Function

    ''' <summary>
    ''' 执行 git 命令。
    ''' </summary>
    Public Function Git(workingDir As String, args As String, Optional timeoutMs As Integer = 120000) As ProcessResult
        Return Run(workingDir, "git", args, timeoutMs)
    End Function

    ''' <summary>
    ''' 执行 dotnet 命令。
    ''' </summary>
    Public Function DotNet(workingDir As String, args As String, Optional timeoutMs As Integer = 120000) As ProcessResult
        Return Run(workingDir, "dotnet", args, timeoutMs)
    End Function

    ''' <summary>
    ''' 检查指定命令是否可用。
    ''' </summary>
    Public Function CommandExists(commandName As String) As Boolean
        Try
            Dim result As ProcessResult = Run(Environment.CurrentDirectory, commandName, "--version", 10000)
            ' Some tools return non-zero exit code for --version, but still produce output
            Return result.ExitCode = 0 OrElse result.StdOut.Length > 0 OrElse result.StdErr.Length > 0
        Catch
            Return False
        End Try
    End Function

End Module
