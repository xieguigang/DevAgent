Imports Galaxy.Workbench
Imports VallinaDevelopment.RibbonLib.Controls

Module RibbonMenu

    Public ReadOnly Property Ribbon As RibbonItems

    Public Sub Hook(ribbon As RibbonItems, host As FormMain)
        _Ribbon = ribbon

        AddHandler ribbon.ButtonExit.ExecuteEvent, Sub() Call host.Close()
        AddHandler ribbon.ButtonAbout.ExecuteEvent, Sub() Call OpenAboutPage()
        AddHandler ribbon.ButtonStartPage.ExecuteEvent, Sub() Call OpenStartupPage()
        AddHandler ribbon.ButtonNew.ExecuteEvent, Sub() Call OpenEditor()
    End Sub

    Public Sub OpenEditor()
        Call CommonRuntime.ShowDocument(Of FormHtmlViewer)(title:="New File").SetUrl($"http://localhost:{Workbench.port}/index.html")
    End Sub

    Public Sub OpenAboutPage()
        Call CommonRuntime.ShowDocument(Of FormHtmlViewer)(title:="About").SetUrl($"http://localhost:{Workbench.port}/about.html")
    End Sub

    Public Sub OpenStartupPage()
        Call CommonRuntime.ShowSingleDocument(Of FormStartPage)()
    End Sub
End Module
