Imports System.Text.Json
Imports DevAgent
Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.Web.WebView2.Core
Imports RibbonLib.Interop
Imports VallinaDevelopment.Javascript

Public Class FormEditor

    Shared ReadOnly btnReload As RibbonEventBinding
    Shared ReadOnly btnFormatted As RibbonEventBinding
    Shared ReadOnly btnSave As RibbonEventBinding
    Shared ReadOnly btnSaveAs As RibbonEventBinding

    Shared ReadOnly btnGotoLine As RibbonEventBinding
    Shared ReadOnly btnShowSymbols As RibbonEventBinding
    Shared ReadOnly btnShowDiffs As RibbonEventBinding

    Shared ReadOnly btnTheme As RibbonEventBinding
    Shared ReadOnly btnMinimap As RibbonEventBinding
    Shared ReadOnly btnDeepSeek As RibbonEventBinding

    Public ReadOnly Property codefile As String

    Shared Sub New()
        btnReload = New RibbonEventBinding(Ribbon.ButtonEditorReload)
        btnFormatted = New RibbonEventBinding(Ribbon.ButtonCodeFormatted)
        btnSave = New RibbonEventBinding(Ribbon.ButtonSaveCodeFile)
        btnSaveAs = New RibbonEventBinding(Ribbon.ButtonSaveAsCodeFile)

        btnGotoLine = New RibbonEventBinding(Ribbon.ButtonGotoLine)
        btnShowDiffs = New RibbonEventBinding(Ribbon.ButtonEditorDiff)
        btnShowSymbols = New RibbonEventBinding(Ribbon.ButtonEditorSymbols)

        btnMinimap = New RibbonEventBinding(Ribbon.ButtonEditorMiniMap)
        btnTheme = New RibbonEventBinding(Ribbon.ButtonEditorTheme)
        btnDeepSeek = New RibbonEventBinding(Ribbon.ButtonDeepSeekLLM)
    End Sub

    Private Async Sub FormEditor_Load(sender As Object, e As EventArgs) Handles Me.Load
        Await WebViewLoader.Init(WebView21)
    End Sub

    Public Function SetCodeFile(filepath As String) As FormEditor
        _codefile = filepath
        Return Me
    End Function

    ' 在类级别（或者窗体顶部）声明一个变量，用于记录 Ctrl+K 是否已被按下
    Private isCtrlKPressed As Boolean = False

    Private Async Sub WebView21_KeyDown(sender As Object, e As KeyEventArgs) Handles WebView21.KeyDown
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

                ' --- 新增：捕捉 Ctrl+K ---
            Case Keys.K
                ' 检查是否按下了 Ctrl 键
                If e.Control Then
                    ' 记录 Ctrl+K 已按下，并拦截该按键，防止 WebView2 处理
                    isCtrlKPressed = True
                    e.Handled = True
                Else
                    isCtrlKPressed = False
                End If

                 ' --- 新增：捕捉 Ctrl+D (在 Ctrl+K 之后) ---
            Case Keys.D
                ' 如果之前按下了 Ctrl+K，并且现在按下了 Ctrl+D
                If e.Control AndAlso isCtrlKPressed Then
                    ' 成功捕捉到 Ctrl+K+D ！
                    e.Handled = True

                    ' 在这里添加你想要执行的代码，例如：
                    Await FormatCode()

                    ' 执行完毕后，重置状态
                    isCtrlKPressed = False
                End If

                ' --- 其他按键处理 ---
            Case Else
                ' 如果按下了其他键（且不是组合键的一部分），重置 Ctrl+K 的状态
                isCtrlKPressed = False
        End Select
    End Sub

    Private Sub WebView21_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WebView21.CoreWebView2InitializationCompleted
        Call WebViewLoader.DeveloperOptions(WebView21, enable:=True, TabText:="Code Edit")

        Call WebView21.CoreWebView2.AddHostObjectToScript(BasePage.HostObject, New CodeEditorPage)
        Call WebView21.CoreWebView2.Navigate($"http://localhost:{Workbench.port}/index.html")
    End Sub

    Private Async Sub WebView21_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles WebView21.NavigationCompleted
        Await WebView21.ExecuteScriptAsync("$('statusbar').style.display='none';")
        Await WebView21.ExecuteScriptAsync("$('toolbar').style.display='none';")
        Await ReloadCodeText()
    End Sub

    Private Async Function SetCodeText(codetext As String) As Task
        Dim filename As String = JsonSerializer.Serialize(If(codefile.StringEmpty, App.NextTempName & "." & Await GetLanguageFileSuffix(), codefile.FileName))
        ' 1. 构造一个匿名对象，包含需要传递的数据
        Dim payload = New With {
            .type = "loadFile",
            .text = JsonSerializer.Serialize(codetext),
            .filename = filename
        }
        ' 2. 序列化为 JSON 字符串
        Dim jsonPayload As String = JsonSerializer.Serialize(payload)

        ' 3. 通过消息通道发送（不会作为脚本执行，性能极高且安全）
        WebView21.CoreWebView2.PostWebMessageAsJson(jsonPayload)
    End Function

    Private Async Function ReloadCodeText() As Task
        If codefile.FileExists Then
            Await SetCodeText(codefile.ReadAllText)
            Call CommonRuntime.GetOutputWindow.AddLog("open file", "editor open code file: " & codefile)
        End If
    End Function

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

    ''' <summary>
    ''' get source code text from the editor ui
    ''' </summary>
    ''' <returns></returns>
    Public Async Function GetCodeText() As Task(Of String)
        Return (Await WebView21.ExecuteScriptAsync("codeEditor.getCodeText()")).LoadJSON(Of String)
    End Function

    Public Async Function GetLanguageFileSuffix() As Task(Of String)
        Dim lang As String = (Await WebView21.ExecuteScriptAsync("codeEditor.getCodeLanguage()")).LoadJSON(Of String)

        lang = Strings.Trim(lang).ToLower

        Select Case lang
            Case "vbnet" : Return "vb"
            Case Else
                Return lang
        End Select
    End Function

    Private Async Function FormatVBCode() As Task
        Try
            Dim code As String = Await GetCodeText()
            code = Await SyntaxFormater.FormatVBCode(code)
            Await SetCodeText(code)
            CommonRuntime.GetOutputWindow.AddLog("format vb code", "formatted current visualbasic.net source code: " & codefile.FileName)
        Catch ex As Exception
            Call App.LogException(ex)
        End Try
    End Function

    Private Async Function FormatCode() As Task
        Select Case Await GetLanguageFileSuffix()
            Case "vb" : Await FormatVBCode()
            Case "r"
        End Select
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

    Private Sub OpenDeepSeekLLMTool()
        Dim deepseek As FormLLMsTool = RibbonMenu.OpenLLMsChat

        If Not deepseek Is Nothing Then
            Call deepseek.HandleCurrentCodeDocument()
        End If
        If Not _codefile Is Nothing Then
            CommonRuntime.AppHost.SetTitle($"Vallina Basic [{_codefile.GetFullPath}]")
        End If
    End Sub

    Private Sub ActivateRibbon()
        Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Active

        Call btnReload.Addhandler(Async Sub() Await ReloadCodeText())
        Call btnFormatted.Addhandler(Async Sub() Await FormatCode())
        Call btnSave.Addhandler(Async Sub() Await SaveCodeFile())
        Call btnSaveAs.Addhandler(Async Sub() Await SaveAsCodeFile())

        Call btnGotoLine.Addhandler(Async Sub() Await GotoLine())
        Call btnShowDiffs.Addhandler(Async Sub() Await ShowDiffs())
        Call btnShowSymbols.Addhandler(Async Sub() Await ShowSymbols())

        Call btnMinimap.Addhandler(Async Sub() Await ToggleMinimap())
        Call btnTheme.Addhandler(Async Sub() Await ToggleTheme())
        Call btnDeepSeek.Addhandler(AddressOf OpenDeepSeekLLMTool)

        Call OpenDeepSeekLLMTool()
    End Sub

    Private Sub UnloadRibbonHook()
        Dim otherEditor As FormEditor = CommonRuntime.AppHost _
            .GetDocuments _
            .OfType(Of FormEditor) _
            .Where(Function(e) e IsNot Me) _
            .FirstOrDefault

        If Not TypeOf CommonRuntime.AppHost.ActiveDocument Is FormEditor Then
            If otherEditor Is Nothing Then
                Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.NotAvailable
            Else
                Ribbon.RibbonEditor.ContextAvailable = ContextAvailability.Available
            End If
        End If

        DirectCast(CommonRuntime.AppHost, FormMain).ResetEditorStatus()
    End Sub

    Private Sub FormEditor_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        ActivateRibbon()
    End Sub

    Private Sub FormEditor_GotFocus(sender As Object, e As EventArgs) Handles Me.GotFocus
        ActivateRibbon()
    End Sub

    Private Sub FormEditor_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ActivateRibbon()
    End Sub

    Private Sub FormEditor_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Call UnloadRibbonHook()

        If codefile.StringEmpty Then
            Call CommonRuntime.GetOutputWindow.AddLog("close editor", "close editor for code.")
        Else
            Call CommonRuntime.GetOutputWindow.AddLog("close editor", "close editor for code file: " & codefile)
        End If
    End Sub
End Class