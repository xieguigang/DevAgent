Imports Galaxy.Workbench
Imports Microsoft.VisualStudio.WinForms.Docking
Imports VallinaDevelopment.RibbonLib.Controls

Public Class FormMain : Implements AppHost

    Public ReadOnly Property ActiveDocument As Form Implements AppHost.ActiveDocument
        Get
            Return DockPanel1.ActiveDocument
        End Get
    End Property

    Private ReadOnly Property AppHost_ClientRectangle As Rectangle Implements AppHost.ClientRectangle
        Get
            Return New Rectangle(Location, Size)
        End Get
    End Property

    Public Event ResizeForm As AppHost.ResizeFormEventHandler Implements AppHost.ResizeForm
    Public Event CloseWorkbench As AppHost.CloseWorkbenchEventHandler Implements AppHost.CloseWorkbench

    ReadOnly _toolStripProfessionalRenderer As New ToolStripProfessionalRenderer()

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles Me.Load
        DockPanel1.Theme = VS2015LightTheme1
        DockPanel1.ShowDocumentIcon = True
        VisualStudioToolStripExtender1.SetStyle(StatusStrip1, VisualStudioToolStripExtender.VsVersion.Vs2015, VS2015LightTheme1)

        If DockPanel1.Theme.ColorPalette IsNot Nothing Then
            StatusStrip1.BackColor = DockPanel1.Theme.ColorPalette.MainWindowStatusBarDefault.Background
        End If

        Call Workbench.StartHttp()
        Call RibbonMenu.Hook(New RibbonItems(Ribbon1), Me)
        Call CommonRuntime.Hook(Me)
        Call StatusMessage("Ready", Icons8.Information)
        Call RibbonMenu.OpenStartupPage()
        Call CommonRuntime.GetOutputWindow.AddLog("startup", "code editor is ready")
    End Sub

    Public Sub SetWorkbenchVisible(visible As Boolean) Implements AppHost.SetWorkbenchVisible
        Me.Visible = visible
    End Sub

    Public Sub SetWindowState(stat As FormWindowState) Implements AppHost.SetWindowState
        WindowState = stat
    End Sub

    Public Async Function UpdateEditorStatus(line As String, col As String, lang As String, file As String) As Task
        Await InvokeAsync(Sub() ToolStripStatusLabel3.Text = $"File: {file} Language: {lang} Line: {line} Col: {col}")
    End Function

    Public Sub ResetEditorStatus()
        ToolStripStatusLabel3.Text = "{}"
    End Sub

    Public Sub SetTitle(title As String) Implements AppHost.SetTitle
        Call Invoke(Sub() Text = title & " - Vallina Development")
    End Sub

    Public Sub StatusMessage(msg As String, Optional icon As Image = Nothing) Implements AppHost.StatusMessage
        Call Invoke(Sub()
                        ToolStripStatusLabel1.Text = msg
                        ToolStripStatusLabel1.Image = icon
                    End Sub)
    End Sub

    Public Sub Warning(msg As String) Implements AppHost.Warning
        Call StatusMessage(msg, Icons8.Warning)
    End Sub

    Public Sub LogText(text As String) Implements AppHost.LogText
        Call CommonRuntime.GetOutputWindow.AppendLine(text)
    End Sub

    Public Sub ShowProperties(obj As Object) Implements AppHost.ShowProperties
        Call CommonRuntime.GetPropertyWindow.SetObject(obj)
    End Sub

    Public Function GetDesktopLocation() As Point Implements AppHost.GetDesktopLocation
        Return Location
    End Function

    Public Function GetClientSize() As Size Implements AppHost.GetClientSize
        Return Size
    End Function

    Public Function GetDocuments() As IEnumerable(Of Form) Implements AppHost.GetDocuments
        Return DockPanel1.Documents.OfType(Of Form)
    End Function

    Public Function GetDockPanel() As Control Implements AppHost.GetDockPanel
        Return DirectCast(DockPanel1, Control)
    End Function

    Public Function GetWindowState() As FormWindowState Implements AppHost.GetWindowState
        Return WindowState
    End Function

    Private Sub FormMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Call Workbench.KillHttp()
        Call CommonRuntime.SaveUISettings()

        RaiseEvent CloseWorkbench(e)
    End Sub

    Private Sub FormMain_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        RaiseEvent ResizeForm(Location, Size)
    End Sub
End Class
