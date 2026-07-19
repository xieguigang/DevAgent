Imports Galaxy.Workbench
Imports Microsoft.Web.WebView2.Core

Public Class FormHtmlViewer

    Dim url As String

    Private Async Sub FormHtmlViewer_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Public Function SetUrl(url As String) As FormHtmlViewer
        Me.url = url
        Return Me
    End Function

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebView21.CoreWebView2.Navigate(url)
    End Sub
End Class