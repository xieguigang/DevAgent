Imports System.Text.Json
Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VersionControl.Git
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.Web.WebView2.Core
Imports VallinaDevelopment.Javascript

Public Class FormGitDiff

    Public Property GitDiff As DiffResult

    Private Async Sub FormGitDiff_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebViewLoader.DeveloperOptions(WebView21, enable:=True, TabText:="Git Diff")

        Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, New GitDiffPage)
        Call WebView21.CoreWebView2.Navigate($"http://localhost:{Workbench.port}/git.html")
    End Sub

    Private Async Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        ' 1. 构造一个匿名对象，包含需要传递的数据
        Dim payload = New With {
            .type = "loadFile",
            .text = GitDiff.GetJson
        }
        ' 2. 序列化为 JSON 字符串
        Dim jsonPayload As String = JsonSerializer.Serialize(payload)

        Await WebView21.ExecuteScriptAsync("$('footer').style.display = 'none';")
        Await WebView21.ExecuteScriptAsync("$('topbar').style.display = 'none';")
        Await WebView21.ExecuteScriptAsync("$('btn-open').style.display = 'none';")
        Await WebView21.ExecuteScriptAsync("$('btn-open-2').style.display = 'none';")

        ' 3. 通过消息通道发送（不会作为脚本执行，性能极高且安全）
        WebView21.CoreWebView2.PostWebMessageAsJson(jsonPayload)
    End Sub
End Class