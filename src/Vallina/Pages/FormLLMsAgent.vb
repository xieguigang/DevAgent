Public Class FormLLMsAgent

    Private Sub FormLLMsAgent_Load(sender As Object, e As EventArgs) Handles Me.Load
        ConsoleControl1.StartProcess(App.HOME & "/DevAgent.exe", Nothing)
    End Sub

    Private Sub ConsoleControl1_ProcessExisted() Handles ConsoleControl1.ProcessExisted
        Me.Close()
    End Sub
End Class