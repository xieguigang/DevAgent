Public Class FormConsole

    Private Sub FormConsole_Load(sender As Object, e As EventArgs) Handles Me.Load
        ConsoleControl1.StartProcess("cmd", Nothing)
    End Sub
End Class