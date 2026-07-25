Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormConsole
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
        ConsoleControl1 = New Microsoft.VisualBasic.Windows.Forms.ConsoleControl()
        SuspendLayout()
        ' 
        ' ConsoleControl1
        ' 
        ConsoleControl1.BackColor = Color.DimGray
        ConsoleControl1.Dock = DockStyle.Fill
        ConsoleControl1.ForeColor = Color.White
        ConsoleControl1.IsInputEnabled = True
        ConsoleControl1.Location = New Point(0, 0)
        ConsoleControl1.Margin = New Padding(4, 4, 4, 4)
        ConsoleControl1.Name = "ConsoleControl1"
        ConsoleControl1.ReadOnly = True
        ConsoleControl1.SendKeyboardCommandsToProcess = False
        ConsoleControl1.ShowDiagnostics = False
        ConsoleControl1.Size = New Size(1106, 706)
        ConsoleControl1.TabIndex = 0
        ' 
        ' FormConsole
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1106, 706)
        Controls.Add(ConsoleControl1)
        DockAreas = Microsoft.VisualStudio.WinForms.Docking.DockAreas.Float Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockLeft Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockRight Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockTop Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockBottom Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.Document
        DoubleBuffered = True
        Name = "FormConsole"
        ShowHint = Microsoft.VisualStudio.WinForms.Docking.DockState.Unknown
        ResumeLayout(False)
    End Sub

    Friend WithEvents ConsoleControl1 As Microsoft.VisualBasic.Windows.Forms.ConsoleControl
End Class
