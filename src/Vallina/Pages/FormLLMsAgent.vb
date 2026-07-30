Public Class FormLLMsAgent

    Private Sub FormLLMsAgent_Load(sender As Object, e As EventArgs) Handles Me.Load
        ConsoleControl1.StartProcess(App.HOME & "/DevAgent.exe", Nothing)
    End Sub
End Class