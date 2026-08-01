Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSsh
    Inherits ToolWindow

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Dim SshConnectionOptions1 As Microsoft.VisualBasic.Windows.Forms.SshClient.SshConnectionOptions = New Microsoft.VisualBasic.Windows.Forms.SshClient.SshConnectionOptions()
        SshWinFormConsole1 = New Microsoft.VisualBasic.Windows.Forms.SshClient.SshWinFormConsole()
        SuspendLayout()
        ' 
        ' SshWinFormConsole1
        ' 
        SshWinFormConsole1.ConnectionOptions = SshConnectionOptions1
        SshWinFormConsole1.Dock = DockStyle.Fill
        SshWinFormConsole1.Host = ""
        SshWinFormConsole1.IsInputEnabled = True
        SshWinFormConsole1.Location = New Point(0, 0)
        SshWinFormConsole1.Name = "SshWinFormConsole1"
        SshWinFormConsole1.Password = ""
        SshWinFormConsole1.ReadOnly = True
        SshWinFormConsole1.SendKeyboardCommandsToProcess = True
        SshWinFormConsole1.ShowDiagnostics = False
        SshWinFormConsole1.Size = New Size(800, 450)
        SshWinFormConsole1.TabIndex = 0
        SshWinFormConsole1.UserName = ""
        ' 
        ' FormSsh
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(SshWinFormConsole1)
        DockAreas = Microsoft.VisualStudio.WinForms.Docking.DockAreas.Float Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockLeft Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockRight Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockTop Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockBottom Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.Document
        DoubleBuffered = True
        Name = "FormSsh"
        ShowHint = Microsoft.VisualStudio.WinForms.Docking.DockState.Unknown
        Text = "/bin/bash/"
        ResumeLayout(False)
    End Sub

    Friend WithEvents SshWinFormConsole1 As Microsoft.VisualBasic.Windows.Forms.SshClient.SshWinFormConsole
End Class
