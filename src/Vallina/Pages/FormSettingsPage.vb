Imports System.Text.Json
Imports DevAgent
Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.Web.WebView2.Core
Imports VallinaDevelopment.Javascript
Imports VallinaDevelopment.Settings

Public Class FormSettingsPage

    Private Async Sub FormStartPage_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, New SettingsPage(Me))
        Call WebView21.CoreWebView2.Navigate($"http://127.0.0.1:{Workbench.port}/settings.html")
    End Sub

    Private Async Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        Dim payload = New With {
                .type = "loadConfig",
                .text = Workbench.config.GetJson,
                .filename = Nothing
        }
        ' 2. 序列化为 JSON 字符串
        Dim jsonPayload As String = JsonSerializer.Serialize(payload)

        ' 3. 通过消息通道发送（不会作为脚本执行，性能极高且安全）
        Await Task.Run(Sub() WebView21.CoreWebView2.PostWebMessageAsJson(jsonPayload))

        Call CommonRuntime.GetOutputWindow.AddLog("load config", "load config json file for settings page: " & ConfigJSON.defaultFile)
    End Sub
End Class