Imports Microsoft.VisualBasic.ApplicationServices

Public Class FormConsole

    Public Property workspace As String

    Private Sub FormConsole_Load(sender As Object, e As EventArgs) Handles Me.Load
        If workspace.StringEmpty Then
            Call ConsoleControl1.StartProcess("cmd", Nothing)
        Else
            Call ConsoleControl1.StartProcess("cmd", $"/k cd /d {workspace.CLIPath}")
        End If
    End Sub

    Private Sub ConsoleControl1_ProcessExisted() Handles ConsoleControl1.ProcessExisted
        Call Invoke(Sub() Me.Close())
    End Sub
End Class