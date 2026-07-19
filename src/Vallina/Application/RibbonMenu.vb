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
            .Filter = "VisualBasic(*.vb);Rscript(*.r)|*.vb;*.r"
        }
            If file.ShowDialog = DialogResult.OK Then
                Call CommonRuntime.ShowDocument(Of FormEditor)(title:=file.FileName.FileName).SetCodeFile(file.FileName)
            End If
        End Using
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
        Call CommonRuntime.ShowSingleDocument(Of FormHtmlViewer)().SetUrl($"http://127.0.0.1:{Workbench.port}/settings.html", New SettingsPage).SetTitle("Settings")
    End Sub

    Public Sub OpenEditor()
        Call CommonRuntime.ShowDocument(Of FormEditor)(title:="New File")
    End Sub

    Public Sub OpenAboutPage()
        Call CommonRuntime.ShowDocument(Of FormHtmlViewer)(title:="About").SetUrl($"http://127.0.0.1:{Workbench.port}/about.html", New AboutPage)
    End Sub

    Public Sub OpenStartupPage()
        Call CommonRuntime.ShowSingleDocument(Of FormStartPage)()
    End Sub
End Module
