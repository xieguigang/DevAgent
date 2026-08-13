Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLinuxServers
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormLinuxServers))
        ToolStrip1 = New ToolStrip()
        AddConfig = New ToolStripButton()
        DeleteConfig = New ToolStripButton()
        TreeView1 = New TreeView()
        ContextMenuStrip1 = New ContextMenuStrip(components)
        OpenConnectionToolStripMenuItem = New ToolStripMenuItem()
        EditToolStripMenuItem = New ToolStripMenuItem()
        DeleteToolStripMenuItem = New ToolStripMenuItem()
        ToolStrip1.SuspendLayout()
        ContextMenuStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Items.AddRange(New ToolStripItem() {AddConfig, DeleteConfig})
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(403, 25)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' AddConfig
        ' 
        AddConfig.DisplayStyle = ToolStripItemDisplayStyle.Image
        AddConfig.Image = CType(resources.GetObject("AddConfig.Image"), Image)
        AddConfig.ImageTransparentColor = Color.Magenta
        AddConfig.Name = "AddConfig"
        AddConfig.Size = New Size(23, 22)
        AddConfig.Text = "Add"
        ' 
        ' DeleteConfig
        ' 
        DeleteConfig.DisplayStyle = ToolStripItemDisplayStyle.Image
        DeleteConfig.Image = CType(resources.GetObject("DeleteConfig.Image"), Image)
        DeleteConfig.ImageTransparentColor = Color.Magenta
        DeleteConfig.Name = "DeleteConfig"
        DeleteConfig.Size = New Size(23, 22)
        DeleteConfig.Text = "Delete"
        ' 
        ' TreeView1
        ' 
        TreeView1.ContextMenuStrip = ContextMenuStrip1
        TreeView1.Dock = DockStyle.Fill
        TreeView1.Location = New Point(0, 25)
        TreeView1.Name = "TreeView1"
        TreeView1.Size = New Size(403, 604)
        TreeView1.TabIndex = 1
        ' 
        ' ContextMenuStrip1
        ' 
        ContextMenuStrip1.Items.AddRange(New ToolStripItem() {OpenConnectionToolStripMenuItem, EditToolStripMenuItem, DeleteToolStripMenuItem})
        ContextMenuStrip1.Name = "ContextMenuStrip1"
        ContextMenuStrip1.Size = New Size(169, 48)
        ' 
        ' OpenConnectionToolStripMenuItem
        ' 
        OpenConnectionToolStripMenuItem.Name = "OpenConnectionToolStripMenuItem"
        OpenConnectionToolStripMenuItem.Size = New Size(168, 22)
        OpenConnectionToolStripMenuItem.Text = "Open Connection"
        ' 
        ' EditToolStripMenuItem
        ' 
        EditToolStripMenuItem.Name = "EditToolStripMenuItem"
        EditToolStripMenuItem.Size = New Size(168, 22)
        EditToolStripMenuItem.Text = "Edit"
        ' 
        ' DeleteToolStripMenuItem
        ' 
        DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem"
        DeleteToolStripMenuItem.Size = New Size(168, 22)
        DeleteToolStripMenuItem.Text = "Delete"
        ' 
        ' FormLinuxServers
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(403, 629)
        Controls.Add(TreeView1)
        Controls.Add(ToolStrip1)
        DockAreas = Microsoft.VisualStudio.WinForms.Docking.DockAreas.Float Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockLeft Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockRight Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockTop Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockBottom Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.Document
        DoubleBuffered = True
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormLinuxServers"
        ShowHint = Microsoft.VisualStudio.WinForms.Docking.DockState.Unknown
        Text = "Linux Server Sessions"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        ContextMenuStrip1.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents AddConfig As ToolStripButton
    Friend WithEvents DeleteConfig As ToolStripButton
    Friend WithEvents ContextMenuStrip1 As ContextMenuStrip
    Friend WithEvents OpenConnectionToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents EditToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DeleteToolStripMenuItem As ToolStripMenuItem
End Class
