Public Class FormLLMsAgent

    Private Sub FormLLMsAgent_Load(sender As Object, e As EventArgs) Handles Me.Load
        ConsoleControl1.StartProcess("cmd", Nothing)
    End Sub
End Class