Imports System.Text

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