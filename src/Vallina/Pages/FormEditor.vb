Imports Galaxy.Workbench
Imports Microsoft.Web.WebView2.Core

Public Class FormEditor

    Private Async Sub FormEditor_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebView21.CoreWebView2.Navigate($"http://localhost:{Workbench.port}/index.html")
    End Sub
End Class