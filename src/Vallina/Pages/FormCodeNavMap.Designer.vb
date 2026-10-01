Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCodeNavMap
    Inherits DocumentWindow

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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormCodeNavMap))
        ToolStrip1 = New ToolStrip()
        ToolStripLabel1 = New ToolStripLabel()
        cboMode = New ToolStripComboBox()
        ToolStripLabel2 = New ToolStripLabel()
        cboMetric = New ToolStripComboBox()
        ToolStripLabel3 = New ToolStripLabel()
        cboLevel = New ToolStripComboBox()
        ToolStripLabel4 = New ToolStripLabel()
        cboExtrude = New ToolStripComboBox()
        ToolStripLabel5 = New ToolStripLabel()
        cboPalette = New ToolStripComboBox()
        Canvas = New CodeMap.TreeNavMap()
        ToolStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Items.AddRange(New ToolStripItem() {ToolStripLabel1, cboMode, ToolStripLabel2, cboMetric, ToolStripLabel3, cboLevel, ToolStripLabel4, cboExtrude, ToolStripLabel5, cboPalette})
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(1520, 25)
        ToolStrip1.TabIndex = 1
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' ToolStripLabel1
        ' 
        ToolStripLabel1.Name = "ToolStripLabel1"
        ToolStripLabel1.Size = New Size(81, 22)
        ToolStripLabel1.Text = "Render Mode:"
        ' 
        ' cboMode
        ' 
        cboMode.DropDownStyle = ComboBoxStyle.DropDownList
        cboMode.Items.AddRange(New Object() {"2D 平面", "3D 城市"})
        cboMode.Name = "cboMode"
        cboMode.Size = New Size(121, 25)
        ' 
        ' ToolStripLabel2
        ' 
        ToolStripLabel2.Name = "ToolStripLabel2"
        ToolStripLabel2.Size = New Size(44, 22)
        ToolStripLabel2.Text = "Metric:"
        ' 
        ' cboMetric
        ' 
        cboMetric.DropDownStyle = ComboBoxStyle.DropDownList
        cboMetric.Items.AddRange(New Object() {"代码行数", "字符数", "符号数"})
        cboMetric.Name = "cboMetric"
        cboMetric.Size = New Size(121, 25)
        ' 
        ' ToolStripLabel3
        ' 
        ToolStripLabel3.Name = "ToolStripLabel3"
        ToolStripLabel3.Size = New Size(80, 22)
        ToolStripLabel3.Text = "Symbol Level:"
        ' 
        ' cboLevel
        ' 
        cboLevel.DropDownStyle = ComboBoxStyle.DropDownList
        cboLevel.Items.AddRange(New Object() {"vbproj", "folder", "source file", "type", "member"})
        cboLevel.Name = "cboLevel"
        cboLevel.Size = New Size(121, 25)
        ' 
        ' ToolStripLabel4
        ' 
        ToolStripLabel4.Name = "ToolStripLabel4"
        ToolStripLabel4.Size = New Size(80, 22)
        ToolStripLabel4.Text = "Extrude Level:"
        ' 
        ' cboExtrude
        ' 
        cboExtrude.DropDownStyle = ComboBoxStyle.DropDownList
        cboExtrude.Items.AddRange(New Object() {"folder", "source file", "type", "member"})
        cboExtrude.Name = "cboExtrude"
        cboExtrude.Size = New Size(121, 25)
        ' 
        ' ToolStripLabel5
        ' 
        ToolStripLabel5.Name = "ToolStripLabel5"
        ToolStripLabel5.Size = New Size(78, 22)
        ToolStripLabel5.Text = "Color Palette:"
        ' 
        ' cboPalette
        ' 
        cboPalette.DropDownStyle = ComboBoxStyle.DropDownList
        cboPalette.Items.AddRange(New Object() {"viridis", "magma", "inferno", "plasma", "turbo", "Jet", "Hot", "Cool", "Rainbow", "按层级着色"})
        cboPalette.Name = "cboPalette"
        cboPalette.Size = New Size(121, 25)
        ' 
        ' Canvas
        ' 
        Canvas.Dock = DockStyle.Fill
        Canvas.ExtrudeKind = Nothing
        Canvas.LayoutLevel = Nothing
        Canvas.Location = New Point(0, 25)
        Canvas.Mode = CodeMap.TreeMap.TreeMapViewMode.TwoD
        Canvas.Name = "Canvas"
        Canvas.NudBuildings = Nothing
        Canvas.Size = New Size(1520, 839)
        Canvas.TabIndex = 2
        ' 
        ' FormCodeNavMap
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1520, 864)
        Controls.Add(Canvas)
        Controls.Add(ToolStrip1)
        DockAreas = Microsoft.VisualStudio.WinForms.Docking.DockAreas.Float Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockLeft Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockRight Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockTop Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.DockBottom Or Microsoft.VisualStudio.WinForms.Docking.DockAreas.Document
        DoubleBuffered = True
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormCodeNavMap"
        ShowHint = Microsoft.VisualStudio.WinForms.Docking.DockState.Unknown
        TabPageContextMenuStrip = DockContextMenuStrip1
        Text = "Project Navigation Map"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents Canvas As CodeMap.TreeNavMap
    Friend WithEvents ToolStripLabel1 As ToolStripLabel
    Friend WithEvents cboMode As ToolStripComboBox
    Friend WithEvents ToolStripLabel2 As ToolStripLabel
    Friend WithEvents cboMetric As ToolStripComboBox
    Friend WithEvents ToolStripLabel3 As ToolStripLabel
    Friend WithEvents cboLevel As ToolStripComboBox
    Friend WithEvents ToolStripLabel4 As ToolStripLabel
    Friend WithEvents cboExtrude As ToolStripComboBox
    Friend WithEvents ToolStripLabel5 As ToolStripLabel
    Friend WithEvents cboPalette As ToolStripComboBox
End Class
