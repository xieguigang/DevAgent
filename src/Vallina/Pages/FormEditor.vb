Imports Galaxy.Workbench
Imports Microsoft.Web.WebView2.Core
Imports RibbonLib.Interop

Public Class FormEditor

    Shared btnGotoLine As RibbonEventBinding
    Shared btnShowSymbols As RibbonEventBinding
    Shared btnShowDiffs As RibbonEventBinding

    Shared Sub New()
        btnGotoLine = New RibbonEventBinding(Ribbon.ButtonGotoLine)
        btnShowDiffs = New RibbonEventBinding(Ribbon.ButtonEditorDiff)
        btnShowSymbols = New RibbonEventBinding(Ribbon.ButtonEditorSymbols)
    End Sub

    Private Async Sub FormEditor_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebView21.CoreWebView2.Navigate($"http://localhost:{Workbench.port}/index.html")
    End Sub

    Private Async Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        Await WebView21.ExecuteScriptAsync("$('statusbar').style.display='none';")
        Await WebView21.ExecuteScriptAsync("$('toolbar').style.display='none';")
    End Sub

    Private Async Function GotoLine() As Task
        Await WebView21.ExecuteScriptAsync("$('btn-goto-line').click();")
    End Function

    Private Async Function ShowSymbols() As Task
        Await WebView21.ExecuteScriptAsync("$('btn-toggle-symbols').click();")
    End Function

    Private Async Function ShowDiffs() As Task
        Await WebView21.ExecuteScriptAsync("$('btn-toggle-diff').click();")
    End Function

    Private Sub ActivateRibbon()
        Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Active

        Call btnGotoLine.Addhandler(Async Sub() Await GotoLine())
        Call btnShowDiffs.Addhandler(Async Sub() Await ShowDiffs())
        Call btnShowSymbols.Addhandler(Async Sub() Await ShowSymbols())
    End Sub

    Private Sub UnloadRibbonHook()
        Dim otherEditor As FormEditor = CommonRuntime.AppHost.GetDocuments.OfType(Of FormEditor).Where(Function(e) e IsNot Me).FirstOrDefault

        If otherEditor Is Nothing Then
            Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.NotAvailable
        Else
            Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Available
        End If
    End Sub

    Private Sub FormEditor_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        Call ActivateRibbon()
    End Sub

    Private Sub FormEditor_GotFocus(sender As Object, e As EventArgs) Handles Me.GotFocus
        Call ActivateRibbon()
    End Sub

    Private Sub FormEditor_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Call ActivateRibbon()
    End Sub

    Private Sub FormEditor_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Call UnloadRibbonHook()
    End Sub

    Private Sub FormEditor_LostFocus(sender As Object, e As EventArgs) Handles Me.LostFocus
        Call UnloadRibbonHook()
    End Sub

    Private Sub FormEditor_Deactivate(sender As Object, e As EventArgs) Handles Me.Deactivate
        Call UnloadRibbonHook()
    End Sub
End Class