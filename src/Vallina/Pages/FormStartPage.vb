Imports Galaxy.Workbench
Imports Microsoft.Web.WebView2.Core
Imports VallinaDevelopment.Javascript

Public Class FormStartPage

    Private Async Sub FormStartPage_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, New StartupPage)
        Call WebView21.CoreWebView2.Navigate($"http://127.0.0.1:{Workbench.port}/startpage.html")
    End Sub
End Class