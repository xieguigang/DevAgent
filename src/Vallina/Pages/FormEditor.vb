Imports Galaxy.Workbench

Public Class FormEditor

    Private Async Sub FormEditor_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub
End Class