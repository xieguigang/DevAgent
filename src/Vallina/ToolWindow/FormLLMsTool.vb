Imports Galaxy.Workbench

Public Class FormLLMsTool

    Private Sub FormLLMsTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        WebView2llmui1.SetHost(Workbench.CreateLLM)
        CommonRuntime.GetOutputWindow.AddLog("open llm", "load llm model: " & WebView2llmui1.llm)
    End Sub

    Public Async Function SetFileReference(filepath As String) As Task
        Await WebView2llmui1.SetFileReference(filepath)
    End Function

    Public Async Function ClearFileReference() As Task
        Await WebView2llmui1.ClearFileReference
    End Function
End Class