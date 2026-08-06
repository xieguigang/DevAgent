Imports Galaxy.Workbench
Imports Galaxy.Workbench.CommonDialogs
Imports Microsoft.VisualStudio.WinForms.Docking
Imports VallinaDevelopment.Javascript
Imports VallinaDevelopment.RibbonLib.Controls

Module RibbonMenu

    Public ReadOnly Property Ribbon As RibbonItems

    Public Sub Hook(ribbon As RibbonItems, host As FormMain)
        _Ribbon = ribbon

        AddHandler ribbon.ButtonExit.ExecuteEvent, Sub() Call host.Close()
        AddHandler ribbon.ButtonAbout.ExecuteEvent, Sub() Call OpenAboutPage()
        AddHandler ribbon.ButtonStartPage.ExecuteEvent, Sub() Call OpenStartupPage()
        AddHandler ribbon.ButtonNew.ExecuteEvent, Sub() Call OpenEditor()
        AddHandler ribbon.ButtonOpen.ExecuteEvent, Sub() Call OpenFileEdit()
        AddHandler ribbon.ButtonSettings.ExecuteEvent, Sub() Call OpenSettingsPage()
        AddHandler ribbon.ButtonLicense.ExecuteEvent, Sub() Call OpenLicenseDialog()
        AddHandler ribbon.ButtonConsole.ExecuteEvent, Sub() Call OpenConsole()
        AddHandler ribbon.ButtonOutputTool.ExecuteEvent, Sub() Call OpenOutputWindows()
        AddHandler ribbon.ButtonRemoteSessions.ExecuteEvent, Sub() Call OpenLinuxSessions()
        AddHandler ribbon.ButtonSsh.ExecuteEvent, Sub() Call OpenBash()
        AddHandler ribbon.ButtonOpenFolder.ExecuteEvent, Sub() Call OpenFolder()
        AddHandler ribbon.ButtonDevAgent.ExecuteEvent, Sub() Call OpenDevAgent()
    End Sub

    Public Sub OpenDevAgent()
        Using folder As New FolderBrowserDialog
            If folder.ShowDialog = DialogResult.OK Then
                Call LaunchLLMAgent(folder.SelectedPath)
            End If
        End Using
    End Sub

    Public Sub OpenFolder()
        Using folder As New FolderBrowserDialog With {.ShowNewFolderButton = True}
            If folder.ShowDialog = DialogResult.OK Then
                Call RibbonMenu.OpenSolutionExplorer(folder.SelectedPath)
            End If
        End Using
    End Sub

    Public Sub OpenBash()
        Dim bash As New FormSsh

        bash.Show(CommonRuntime.AppHost.GetDockPanel)
        bash.DockState = DockState.DockBottom
    End Sub

    Public Sub OpenLinuxSessions()
        Dim servers = CommonRuntime.TryGetToolWindow("linux_servers")

        If servers Is Nothing Then
            servers = New FormLinuxServers With {.Name = "linux_servers"}
        End If

        Call CommonRuntime.RegisterToolWindow(servers, DockState.DockLeft)
    End Sub

    Public Sub OpenOutputWindows()
        Call CommonRuntime.GetOutputWindow.Show(CommonRuntime.AppHost.GetDockPanel, DockState.DockBottom)
    End Sub

    Public Sub OpenConsole()
        Dim console As New FormConsole

        console.Show(CommonRuntime.AppHost.GetDockPanel)
        console.DockState = DockState.DockBottom
    End Sub

    Public Sub OpenFileEdit()
        Using file As New OpenFileDialog With {
            .Filter = "VisualBasic(*.vb);Project(*.vbproj);Rscript(*.r)|*.vb;*.r;*.vbproj"
        }
            If file.ShowDialog = DialogResult.OK Then
                If file.FileName.ExtensionSuffix("vbproj") Then
                    Call OpenSolutionExplorer(file.FileName)
                Else
                    Call OpenFileEdit(file.FileName)
                    Call OpenLLMsChat()
                End If
            End If
        End Using
    End Sub

    Public Sub OpenFileEdit(filepath As String)
        Call DirectCast(CommonRuntime.AppHost, Form).Invoke(Sub() CommonRuntime.ShowDocument(Of FormEditor)(title:=filepath.FileName).SetCodeFile(filepath))
    End Sub

    ''' <summary>
    ''' request open the llm chatbox
    ''' </summary>
    ''' <returns></returns>
    Public Function OpenLLMsChat() As FormLLMsTool
        Dim chatbox As FormLLMsTool = CommonRuntime.TryGetToolWindow("llms")

        If chatbox Is Nothing Then
            chatbox = New FormLLMsTool With {.Name = "llms"}
        End If

        Call CommonRuntime.RegisterToolWindow(chatbox, DockState.DockRight)

        Return chatbox
    End Function

    Public Sub OpenSolutionExplorer(proj As String)
        Dim explorer As FormSolutionExplorer = CommonRuntime.TryGetToolWindow("solution_explorer")

        If explorer Is Nothing Then
            explorer = New FormSolutionExplorer With {
                .Name = "solution_explorer",
                .ProjectFile = proj
            }
        Else
            explorer.ProjectFile = proj
        End If

        Call DirectCast(CommonRuntime.AppHost, Form).Invoke(Sub() CommonRuntime.RegisterToolWindow(explorer, DockState.DockRightAutoHide))
        Call explorer.Invoke(Sub() explorer.Reload())
    End Sub

    Public Sub OpenLicenseDialog()
        Call InputDialog.Input(Of FormLicenseDialog)()
    End Sub

    Public Sub OpenSettingsPage()
        Call DirectCast(CommonRuntime.AppHost, Form).Invoke(Sub() CommonRuntime.ShowSingleDocument(Of FormSettingsPage)())
    End Sub

    Public Sub OpenEditor()
        Call CommonRuntime.ShowDocument(Of FormEditor)(title:="New File")
        Call OpenLLMsChat()
    End Sub

    Public Sub LaunchLLMAgent(wd As String)
        Dim agent As New FormLLMsAgent With {.Workspace = wd}
        Dim host As Form = DirectCast(CommonRuntime.AppHost, Form)
        Dim panel As DockPanel = DirectCast(CommonRuntime.AppHost.GetDockPanel, DockPanel)

        agent.Show(panel, DockState.Document)
        agent.TabText = $"LLM DevAgent [{wd.BaseName} - {wd.ParentPath}]"
    End Sub

    Public Sub OpenAboutPage()
        Call DirectCast(CommonRuntime.AppHost, Form).Invoke(Sub() CommonRuntime.ShowDocument(Of FormHtmlViewer)(title:="About").SetUrl($"http://127.0.0.1:{Workbench.port}/about.html", New AboutPage))
    End Sub

    Public Sub OpenStartupPage()
        Call CommonRuntime.ShowSingleDocument(Of FormStartPage)()
    End Sub
End Module
