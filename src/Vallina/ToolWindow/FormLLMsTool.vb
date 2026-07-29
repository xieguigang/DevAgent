Public Class FormLLMsTool

    Private Sub FormLLMsTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        WebView2llmui1.SetHost(Workbench.CreateLLM)
    End Sub
End Class