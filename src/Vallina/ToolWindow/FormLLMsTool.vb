Imports Galaxy.Workbench
Imports Microsoft.VisualStudio.WinForms.Docking

''' <summary>
''' LLM chatbox for project source file
''' </summary>
Public Class FormLLMsTool

    Private Sub FormLLMsTool_Load(sender As Object, e As EventArgs) Handles Me.Load
        WebView2llmui1.SetHost(Workbench.CreateLLM,
            callback:=Sub(res)
                          CommonRuntime.GetOutputWindow.AppendLine($"<think>{res.think}</think>" & vbCrLf & vbCrLf)
                          CommonRuntime.GetOutputWindow.AppendLine(res.output)
                      End Sub)

        CommonRuntime.GetOutputWindow.AddLog("open llm", "load llm model: " & WebView2llmui1.llm)
    End Sub

    Public Async Function SetFileReference(filepath As String) As Task
        Await WebView2llmui1.SetFileReference(filepath)
    End Function

    Public Async Function SetFileReference(text As Func(Of Task(Of String))) As Task
        Await WebView2llmui1.SetFileReferenceHandle(text, App.NextTempName & ".vb")
    End Function

    Public Async Function ClearFileReference() As Task
        Await WebView2llmui1.ClearFileReference
    End Function

    Public Async Function HandleCurrentCodeDocument() As Task
        Dim editor As FormEditor = TryCast(DirectCast(CommonRuntime.AppHost.GetDockPanel, DockPanel).ActiveDocument, FormEditor)

        If editor Is Nothing Then
            Return
        End If

        Await ClearFileReference()

        If editor.codefile.StringEmpty(, True) Then
            Await SetFileReference(Function()
                                       Return editor.GetCodeText
                                   End Function)
        Else
            Await SetFileReference(filepath:=editor.codefile)
        End If

        TabText = $"LLMs Chat [{editor.codefile.FileName}]"
    End Function
End Class