Imports Galaxy.Workbench
Imports Microsoft.Web.WebView2.Core
Imports VallinaDevelopment.Javascript

Public Class FormHtmlViewer

    Dim url As String
    Dim interop As BasePage

    Private Async Sub FormHtmlViewer_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Public Function SetUrl(url As String, interop As BasePage) As FormHtmlViewer
        Me.url = url
        Me.interop = interop

        Return Me
    End Function

    Public Function SetTitle(title As String) As FormHtmlViewer
        Me.TabText = title
        Return Me
    End Function

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        If interop IsNot Nothing Then
            Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, interop)
        End If

        Call WebView21.CoreWebView2.Navigate(url)
    End Sub
End Class