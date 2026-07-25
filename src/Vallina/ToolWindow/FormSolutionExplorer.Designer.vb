Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSolutionExplorer
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormSolutionExplorer))
        TreeView1 = New TreeView()
        ImageList1 = New ImageList(components)
        ToolStrip1 = New ToolStrip()
        SuspendLayout()
        ' 
        ' TreeView1
        ' 
        TreeView1.Dock = DockStyle.Fill
        TreeView1.ImageIndex = 0
        TreeView1.ImageList = ImageList1
        TreeView1.Location = New Point(0, 25)
        TreeView1.Name = "TreeView1"
        TreeView1.SelectedImageIndex = 0
        TreeView1.Size = New Size(403, 600)
        TreeView1.TabIndex = 0
        ' 
        ' ImageList1
        ' 
        ImageList1.ColorDepth = ColorDepth.Depth32Bit
        ImageList1.ImageStream = CType(resources.GetObject("ImageList1.ImageStream"), ImageListStreamer)
        ImageList1.TransparentColor = Color.Transparent
        ImageList1.Images.SetKeyName(0, "icons8-visual-studio-96.png")
        ImageList1.Images.SetKeyName(1, "icons8-open-folder-in-new-tab-96.png")
        ImageList1.Images.SetKeyName(2, "icons8-code-file-96.png")
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(403, 25)
        ToolStrip1.TabIndex = 1
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' FormSolutionExplorer
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(403, 625)
        Controls.Add(TreeView1)
        Controls.Add(ToolStrip1)
        DockAreas = Microsoft.VisualStudio.WinForms.Docking.DockAreas.Float Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockLeft Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockRight Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockTop Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockBottom Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.Document
        DoubleBuffered = True
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormSolutionExplorer"
        ShowHint = Microsoft.VisualStudio.WinForms.Docking.DockState.Unknown
        Text = "Solution Explorer"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents ImageList1 As ImageList
End Class
