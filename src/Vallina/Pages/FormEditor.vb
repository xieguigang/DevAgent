Imports System.Text.Json
Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.Web.WebView2.Core
Imports RibbonLib.Interop
Imports VallinaDevelopment.Javascript

Public Class FormEditor

    Shared ReadOnly btnSave As RibbonEventBinding
    Shared ReadOnly btnSaveAs As RibbonEventBinding

    Shared ReadOnly btnGotoLine As RibbonEventBinding
    Shared ReadOnly btnShowSymbols As RibbonEventBinding
    Shared ReadOnly btnShowDiffs As RibbonEventBinding

    Shared ReadOnly btnTheme As RibbonEventBinding
    Shared ReadOnly btnMinimap As RibbonEventBinding

    Public ReadOnly Property codefile As String

    Shared Sub New()
        btnSave = New RibbonEventBinding(Ribbon.ButtonSaveCodeFile)
        btnSaveAs = New RibbonEventBinding(Ribbon.ButtonSaveAsCodeFile)

        btnGotoLine = New RibbonEventBinding(Ribbon.ButtonGotoLine)
        btnShowDiffs = New RibbonEventBinding(Ribbon.ButtonEditorDiff)
        btnShowSymbols = New RibbonEventBinding(Ribbon.ButtonEditorSymbols)

        btnMinimap = New RibbonEventBinding(Ribbon.ButtonEditorMiniMap)
        btnTheme = New RibbonEventBinding(Ribbon.ButtonEditorTheme)
    End Sub

    Private Async Sub FormEditor_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Public Function SetCodeFile(filepath As String) As FormEditor
        _codefile = filepath
        Return Me
    End Function

    Private Sub WebView21_KeyDown(sender As Object, e As KeyEventArgs) Handles WebView21.KeyDown
        ' 将 VirtualKey 转换为 WinForms 的 Keys 枚举方便判断
        Dim key As Keys = e.KeyCode

        Select Case key
            Case Keys.F1, Keys.F11, Keys.F12, Keys.F5
                ' 将 Handled 设置为 True，WebView2 将不会执行这些键的默认行为
                e.Handled = True

                ' （可选）如果你希望在这些键被按下时执行你自己的 WinForm 逻辑
                ' 可以在这里添加代码，例如：
                ' If key = Keys.F5 Then
                '     Me.Refresh() ' 执行窗体刷新而不是网页刷新
                ' End If

            ' 如果你还想禁用 Ctrl+F5、Ctrl+R 等刷新组合键
            Case Keys.R
                ' 检查是否按下了 Ctrl 键
                If (Control.ModifierKeys And Keys.Control) = Keys.Control Then
                    e.Handled = True
                End If
        End Select
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebViewLoader.DeveloperOptions(WebView21, enable:=False, TabText:="Code Edit")

        Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, New CodeEditorPage)
        Call WebView21.CoreWebView2.Navigate($"http://localhost:{Workbench.port}/index.html")
    End Sub

    Private Async Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        Await WebView21.ExecuteScriptAsync("$('statusbar').style.display='none';")
        Await WebView21.ExecuteScriptAsync("$('toolbar').style.display='none';")

        If codefile.FileExists Then
            Dim filename As String = JsonSerializer.Serialize(codefile.FileName)
            Dim codetext As String = JsonSerializer.Serialize(codefile.ReadAllText)
            ' 1. 构造一个匿名对象，包含需要传递的数据
            Dim payload = New With {
                .type = "loadFile",
                .text = codetext,
                .filename = filename
            }
            ' 2. 序列化为 JSON 字符串
            Dim jsonPayload As String = JsonSerializer.Serialize(payload)

            ' 3. 通过消息通道发送（不会作为脚本执行，性能极高且安全）
            WebView21.CoreWebView2.PostWebMessageAsJson(jsonPayload)
            CommonRuntime.GetOutputWindow.AddLog("open file", "editor open code file: " & codefile)
        End If
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

    Private Async Function ToggleTheme() As Task
        Await WebView21.ExecuteScriptAsync("codeEditor.toggleTheme();")
    End Function

    Private Async Function ToggleMinimap() As Task
        Await WebView21.ExecuteScriptAsync("codeEditor.toggleMinimap();")
    End Function

    Protected Overrides Async Sub SaveDocument()
        Await SaveCodeFile()
    End Sub

    Private Async Function GetCodeText() As Task(Of String)
        Return (Await WebView21.ExecuteScriptAsync("codeEditor.getCodeText()")).LoadJSON(Of String)
    End Function

    Private Async Function SaveCodeFile() As Task
        If codefile.StringEmpty Then
            Await SaveAsCodeFile()
        Else
            Call (Await GetCodeText()).SaveTo(codefile)
        End If
    End Function

    Private Async Function SaveAsCodeFile() As Task
        Using file As New SaveFileDialog With {.Filter = "VisualBasic(*.vb)|*.vb|Rscript(*.r)|*.r"}
            If file.ShowDialog = DialogResult.OK Then
                Call SetCodeFile(file.FileName)
                Call (Await GetCodeText()).SaveTo(file.FileName)
            End If
        End Using
    End Function

    Private Sub ActivateRibbon()
        Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Active

        Call btnSave.Addhandler(Async Sub() Await SaveCodeFile())
        Call btnSaveAs.Addhandler(Async Sub() Await SaveAsCodeFile())

        Call btnGotoLine.Addhandler(Async Sub() Await GotoLine())
        Call btnShowDiffs.Addhandler(Async Sub() Await ShowDiffs())
        Call btnShowSymbols.Addhandler(Async Sub() Await ShowSymbols())

        Call btnMinimap.Addhandler(Async Sub() Await ToggleMinimap())
        Call btnTheme.Addhandler(Async Sub() Await ToggleTheme())
    End Sub

    Private Sub UnloadRibbonHook()
        Dim otherEditor As FormEditor = CommonRuntime.AppHost.GetDocuments.OfType(Of FormEditor).Where(Function(e) e IsNot Me).FirstOrDefault

        If otherEditor Is Nothing Then
            Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.NotAvailable
        Else
            Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Available
        End If

        DirectCast(CommonRuntime.AppHost, FormMain).ResetEditorStatus()
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

        If codefile.StringEmpty Then
            Call CommonRuntime.GetOutputWindow.AddLog("close editor", "close editor for code.")
        Else
            Call CommonRuntime.GetOutputWindow.AddLog("close editor", "close editor for code file: " & codefile)
        End If
    End Sub

    Private Sub FormEditor_LostFocus(sender As Object, e As EventArgs) Handles Me.LostFocus
        Call UnloadRibbonHook()
    End Sub

    Private Sub FormEditor_Deactivate(sender As Object, e As EventArgs) Handles Me.Deactivate
        Call UnloadRibbonHook()
    End Sub
End Class