Public Class FormLLMsTool

    Private Sub FormLLMsTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        WebView2llmui1.SetHost(Workbench.CreateLLM)
    End Sub

    Public Async Function SetFileReference(filepath As String) As Task
        Await WebView2llmui1.SetFileReference(filepath)
    End Function
End Class