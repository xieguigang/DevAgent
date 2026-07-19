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
    End Sub

    Public Sub OpenFileEdit()
        Using file As New OpenFileDialog With {
            .Filter = "VisualBasic(*.vb);Project(*.vbproj);Rscript(*.r)|*.vb;*.r;*.vbproj"
        }
            If file.ShowDialog = DialogResult.OK Then
                If file.FileName.ExtensionSuffix("vbproj") Then
                    Call OpenSolutionExplorer()
                Else
                    Call CommonRuntime.ShowDocument(Of FormEditor)(title:=file.FileName.FileName).SetCodeFile(file.FileName)
                    Call OpenLLMsChat()
                End If
            End If
        End Using
    End Sub

    Public Sub OpenLLMsChat()
        Dim chatbox As FormLLMsTool = CommonRuntime.TryGetToolWindow("llms")

        If chatbox Is Nothing Then
            chatbox = New FormLLMsTool With {.Name = "llms"}
        End If

        Call CommonRuntime.RegisterToolWindow(chatbox, DockState.DockRight)
    End Sub

    Public Sub OpenSolutionExplorer()
        Dim explorer As FormSolutionExplorer = CommonRuntime.TryGetToolWindow("solution_explorer")

        If explorer Is Nothing Then
            explorer = New FormSolutionExplorer With {.Name = "solution_explorer"}
        End If

        Call CommonRuntime.RegisterToolWindow(explorer, DockState.DockRightAutoHide)
    End Sub

    Public Sub OpenLicenseDialog()
        Call InputDialog.Input(Of FormLicenseDialog)()
    End Sub

    Public Sub OpenSettingsPage()
        Call CommonRuntime.ShowSingleDocument(Of FormSettingsPage)()
    End Sub

    Public Sub OpenEditor()
        Call CommonRuntime.ShowDocument(Of FormEditor)(title:="New File")
        Call OpenLLMsChat()
    End Sub

    Public Sub OpenAboutPage()
        Call CommonRuntime.ShowDocument(Of FormHtmlViewer)(title:="About").SetUrl($"http://127.0.0.1:{Workbench.port}/about.html", New AboutPage)
    End Sub

    Public Sub OpenStartupPage()
        Call CommonRuntime.ShowSingleDocument(Of FormStartPage)()
    End Sub
End Module
