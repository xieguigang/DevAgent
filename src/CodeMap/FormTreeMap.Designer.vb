<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTreeMap
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Canvas = New Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas()
        Me.PanelTop = New System.Windows.Forms.Panel()
        Me.FlowTools = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblMode = New System.Windows.Forms.Label()
        Me.cboMode = New System.Windows.Forms.ComboBox()
        Me.lblMetric = New System.Windows.Forms.Label()
        Me.cboMetric = New System.Windows.Forms.ComboBox()
        Me.lblLevel = New System.Windows.Forms.Label()
        Me.cboLevel = New System.Windows.Forms.ComboBox()
        Me.lblExtrude = New System.Windows.Forms.Label()
        Me.cboExtrude = New System.Windows.Forms.ComboBox()
        Me.lblMax = New System.Windows.Forms.Label()
        Me.nudBuildings = New System.Windows.Forms.NumericUpDown()
        Me.btnBack = New System.Windows.Forms.Button()
        Me.btnFit = New System.Windows.Forms.Button()
        Me.btnSnapshot = New System.Windows.Forms.Button()
        Me.lblPalette = New System.Windows.Forms.Label()
        Me.cboPalette = New System.Windows.Forms.ComboBox()
        Me.FlowBreadcrumb = New System.Windows.Forms.FlowLayoutPanel()
        Me.PanelBottom = New System.Windows.Forms.Panel()
        Me.TxtDetail = New System.Windows.Forms.TextBox()
        Me.StatusMain = New System.Windows.Forms.StatusStrip()
        Me.lblStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblProgress = New System.Windows.Forms.ToolStripStatusLabel()
        Me.PanelTop.SuspendLayout()
        Me.FlowTools.SuspendLayout()
        Me.PanelBottom.SuspendLayout()
        Me.StatusMain.SuspendLayout()
        CType(Me.nudBuildings, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Canvas
        '
        Me.Canvas.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.Canvas.BackgroundColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.Canvas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Canvas.Location = New System.Drawing.Point(0, 68)
        Me.Canvas.Name = "Canvas"
        Me.Canvas.Size = New System.Drawing.Size(1280, 435)
        Me.Canvas.TabIndex = 0
        Me.Canvas.VSync = True
        '
        'PanelTop
        '
        Me.PanelTop.BackColor = System.Drawing.Color.FromArgb(CType(CType(17, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(39, Byte), Integer))
        Me.PanelTop.Controls.Add(Me.FlowTools)
        Me.PanelTop.Controls.Add(Me.FlowBreadcrumb)
        Me.PanelTop.Dock = System.Windows.Forms.DockStyle.Top
        Me.PanelTop.Location = New System.Drawing.Point(0, 0)
        Me.PanelTop.Name = "PanelTop"
        Me.PanelTop.Size = New System.Drawing.Size(1280, 68)
        Me.PanelTop.TabIndex = 1
        '
        'FlowTools
        '
        Me.FlowTools.AutoSize = False
        Me.FlowTools.Controls.Add(Me.lblMode)
        Me.FlowTools.Controls.Add(Me.cboMode)
        Me.FlowTools.Controls.Add(Me.lblMetric)
        Me.FlowTools.Controls.Add(Me.cboMetric)
        Me.FlowTools.Controls.Add(Me.lblLevel)
        Me.FlowTools.Controls.Add(Me.cboLevel)
        Me.FlowTools.Controls.Add(Me.lblExtrude)
        Me.FlowTools.Controls.Add(Me.cboExtrude)
        Me.FlowTools.Controls.Add(Me.lblMax)
        Me.FlowTools.Controls.Add(Me.nudBuildings)
        Me.FlowTools.Controls.Add(Me.btnBack)
        Me.FlowTools.Controls.Add(Me.btnFit)
        Me.FlowTools.Controls.Add(Me.btnSnapshot)
        Me.FlowTools.Controls.Add(Me.lblPalette)
        Me.FlowTools.Controls.Add(Me.cboPalette)
        Me.FlowTools.Dock = System.Windows.Forms.DockStyle.Fill
        Me.FlowTools.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight
        Me.FlowTools.Location = New System.Drawing.Point(0, 30)
        Me.FlowTools.Name = "FlowTools"
        Me.FlowTools.Padding = New System.Windows.Forms.Padding(8, 4, 8, 0)
        Me.FlowTools.Size = New System.Drawing.Size(1280, 38)
        Me.FlowTools.TabIndex = 1
        Me.FlowTools.WrapContents = False
        '
        'lblMode
        '
        Me.lblMode.AutoSize = True
        Me.lblMode.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblMode.Location = New System.Drawing.Point(11, 8)
        Me.lblMode.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblMode.Name = "lblMode"
        Me.lblMode.Size = New System.Drawing.Size(32, 15)
        Me.lblMode.TabIndex = 0
        Me.lblMode.Text = "模式"
        '
        'cboMode
        '
        Me.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMode.FormattingEnabled = True
        Me.cboMode.Items.AddRange(New Object() {"2D 平面", "3D 城市"})
        Me.cboMode.Location = New System.Drawing.Point(49, 4)
        Me.cboMode.Name = "cboMode"
        Me.cboMode.Size = New System.Drawing.Size(88, 23)
        Me.cboMode.TabIndex = 1
        '
        'lblMetric
        '
        Me.lblMetric.AutoSize = True
        Me.lblMetric.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblMetric.Location = New System.Drawing.Point(143, 8)
        Me.lblMetric.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblMetric.Name = "lblMetric"
        Me.lblMetric.Size = New System.Drawing.Size(32, 15)
        Me.lblMetric.TabIndex = 2
        Me.lblMetric.Text = "度量"
        '
        'cboMetric
        '
        Me.cboMetric.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMetric.FormattingEnabled = True
        Me.cboMetric.Items.AddRange(New Object() {"代码行数", "字符数", "符号数"})
        Me.cboMetric.Location = New System.Drawing.Point(181, 4)
        Me.cboMetric.Name = "cboMetric"
        Me.cboMetric.Size = New System.Drawing.Size(88, 23)
        Me.cboMetric.TabIndex = 3
        '
        'lblLevel
        '
        Me.lblLevel.AutoSize = True
        Me.lblLevel.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblLevel.Location = New System.Drawing.Point(275, 8)
        Me.lblLevel.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblLevel.Name = "lblLevel"
        Me.lblLevel.Size = New System.Drawing.Size(56, 15)
        Me.lblLevel.TabIndex = 4
        Me.lblLevel.Text = "展开层级"
        '
        'cboLevel
        '
        Me.cboLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboLevel.FormattingEnabled = True
        Me.cboLevel.Items.AddRange(New Object() {"vbproj", "folder", "source file", "type", "member"})
        Me.cboLevel.Location = New System.Drawing.Point(337, 4)
        Me.cboLevel.Name = "cboLevel"
        Me.cboLevel.Size = New System.Drawing.Size(104, 23)
        Me.cboLevel.TabIndex = 5
        '
        'lblExtrude
        '
        Me.lblExtrude.AutoSize = True
        Me.lblExtrude.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblExtrude.Location = New System.Drawing.Point(447, 8)
        Me.lblExtrude.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblExtrude.Name = "lblExtrude"
        Me.lblExtrude.Size = New System.Drawing.Size(56, 15)
        Me.lblExtrude.TabIndex = 6
        Me.lblExtrude.Text = "挤出层"
        '
        'cboExtrude
        '
        Me.cboExtrude.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboExtrude.FormattingEnabled = True
        Me.cboExtrude.Items.AddRange(New Object() {"folder", "source file", "type", "member"})
        Me.cboExtrude.Location = New System.Drawing.Point(509, 4)
        Me.cboExtrude.Name = "cboExtrude"
        Me.cboExtrude.Size = New System.Drawing.Size(104, 23)
        Me.cboExtrude.TabIndex = 7
        '
        'lblMax
        '
        Me.lblMax.AutoSize = True
        Me.lblMax.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblMax.Location = New System.Drawing.Point(619, 8)
        Me.lblMax.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblMax.Name = "lblMax"
        Me.lblMax.Size = New System.Drawing.Size(68, 15)
        Me.lblMax.TabIndex = 8
        Me.lblMax.Text = "建筑上限"
        '
        'nudBuildings
        '
        Me.nudBuildings.Increment = New Decimal(New Integer() {500, 0, 0, 0})
        Me.nudBuildings.Location = New System.Drawing.Point(693, 4)
        Me.nudBuildings.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        Me.nudBuildings.Minimum = New Decimal(New Integer() {100, 0, 0, 0})
        Me.nudBuildings.Name = "nudBuildings"
        Me.nudBuildings.Size = New System.Drawing.Size(80, 23)
        Me.nudBuildings.TabIndex = 9
        Me.nudBuildings.Value = New Decimal(New Integer() {3000, 0, 0, 0})
        '
        'btnBack
        '
        Me.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnBack.ForeColor = System.Drawing.Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.btnBack.Location = New System.Drawing.Point(779, 4)
        Me.btnBack.Name = "btnBack"
        Me.btnBack.Size = New System.Drawing.Size(72, 25)
        Me.btnBack.TabIndex = 10
        Me.btnBack.Text = "返回上级"
        Me.btnBack.UseVisualStyleBackColor = True
        '
        'btnFit
        '
        Me.btnFit.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnFit.ForeColor = System.Drawing.Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.btnFit.Location = New System.Drawing.Point(857, 4)
        Me.btnFit.Name = "btnFit"
        Me.btnFit.Size = New System.Drawing.Size(72, 25)
        Me.btnFit.TabIndex = 11
        Me.btnFit.Text = "适应视图"
        Me.btnFit.UseVisualStyleBackColor = True
        '
        'btnSnapshot
        '
        Me.btnSnapshot.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSnapshot.ForeColor = System.Drawing.Color.FromArgb(CType(CType(226, Byte), Integer), CType(CType(232, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.btnSnapshot.Location = New System.Drawing.Point(935, 4)
        Me.btnSnapshot.Name = "btnSnapshot"
        Me.btnSnapshot.Size = New System.Drawing.Size(72, 25)
        Me.btnSnapshot.TabIndex = 12
        Me.btnSnapshot.Text = "导出快照"
        Me.btnSnapshot.UseVisualStyleBackColor = True
        '
        'lblPalette
        '
        Me.lblPalette.AutoSize = True
        Me.lblPalette.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblPalette.Location = New System.Drawing.Point(1013, 8)
        Me.lblPalette.Margin = New System.Windows.Forms.Padding(3, 8, 3, 0)
        Me.lblPalette.Name = "lblPalette"
        Me.lblPalette.Size = New System.Drawing.Size(32, 15)
        Me.lblPalette.TabIndex = 13
        Me.lblPalette.Text = "配色"
        '
        'cboPalette
        '
        Me.cboPalette.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboPalette.FormattingEnabled = True
        Me.cboPalette.Items.AddRange(New Object() {"viridis", "magma", "inferno", "plasma", "turbo", "Jet", "Hot", "Cool", "Rainbow", "按层级着色"})
        Me.cboPalette.Location = New System.Drawing.Point(1051, 4)
        Me.cboPalette.Name = "cboPalette"
        Me.cboPalette.Size = New System.Drawing.Size(104, 23)
        Me.cboPalette.TabIndex = 14
        '
        'FlowBreadcrumb
        '
        Me.FlowBreadcrumb.AutoSize = False
        Me.FlowBreadcrumb.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.FlowBreadcrumb.Dock = System.Windows.Forms.DockStyle.Top
        Me.FlowBreadcrumb.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight
        Me.FlowBreadcrumb.Location = New System.Drawing.Point(0, 0)
        Me.FlowBreadcrumb.Name = "FlowBreadcrumb"
        Me.FlowBreadcrumb.Padding = New System.Windows.Forms.Padding(8, 4, 8, 0)
        Me.FlowBreadcrumb.Size = New System.Drawing.Size(1280, 30)
        Me.FlowBreadcrumb.TabIndex = 0
        Me.FlowBreadcrumb.WrapContents = False
        '
        'PanelBottom
        '
        Me.PanelBottom.BackColor = System.Drawing.Color.FromArgb(CType(CType(17, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(39, Byte), Integer))
        Me.PanelBottom.Controls.Add(Me.TxtDetail)
        Me.PanelBottom.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.PanelBottom.Location = New System.Drawing.Point(0, 503)
        Me.PanelBottom.Name = "PanelBottom"
        Me.PanelBottom.Padding = New System.Windows.Forms.Padding(8)
        Me.PanelBottom.Size = New System.Drawing.Size(1280, 170)
        Me.PanelBottom.TabIndex = 2
        '
        'TxtDetail
        '
        Me.TxtDetail.BackColor = System.Drawing.Color.FromArgb(CType(CType(2, Byte), Integer), CType(CType(6, Byte), Integer), CType(CType(23, Byte), Integer))
        Me.TxtDetail.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtDetail.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TxtDetail.Font = New System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TxtDetail.ForeColor = System.Drawing.Color.FromArgb(CType(CType(215, Byte), Integer), CType(CType(227, Byte), Integer), CType(CType(244, Byte), Integer))
        Me.TxtDetail.Location = New System.Drawing.Point(8, 8)
        Me.TxtDetail.Multiline = True
        Me.TxtDetail.Name = "TxtDetail"
        Me.TxtDetail.ReadOnly = True
        Me.TxtDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TxtDetail.Size = New System.Drawing.Size(1264, 154)
        Me.TxtDetail.TabIndex = 0
        Me.TxtDetail.WordWrap = False
        '
        'StatusMain
        '
        Me.StatusMain.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.StatusMain.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.lblStatus, Me.lblProgress})
        Me.StatusMain.Location = New System.Drawing.Point(0, 651)
        Me.StatusMain.Name = "StatusMain"
        Me.StatusMain.Size = New System.Drawing.Size(1280, 22)
        Me.StatusMain.TabIndex = 3
        '
        'lblStatus
        '
        Me.lblStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(148, Byte), Integer), CType(CType(163, Byte), Integer), CType(CType(184, Byte), Integer))
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(0, 17)
        '
        'lblProgress
        '
        Me.lblProgress.ForeColor = System.Drawing.Color.FromArgb(CType(CType(45, Byte), Integer), CType(CType(212, Byte), Integer), CType(CType(191, Byte), Integer))
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(0, 17)
        '
        'FormTreeMap
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(42, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1280, 673)
        Me.Controls.Add(Me.Canvas)
        Me.Controls.Add(Me.PanelTop)
        Me.Controls.Add(Me.PanelBottom)
        Me.Controls.Add(Me.StatusMain)
        Me.Name = "FormTreeMap"
        Me.Text = "CodeMap · 代码库树图"
        Me.PanelTop.ResumeLayout(False)
        Me.FlowTools.ResumeLayout(False)
        Me.FlowTools.PerformLayout()
        Me.PanelBottom.ResumeLayout(False)
        Me.PanelBottom.PerformLayout()
        Me.StatusMain.ResumeLayout(False)
        CType(Me.nudBuildings, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents Canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas
    Friend WithEvents PanelTop As System.Windows.Forms.Panel
    Friend WithEvents FlowBreadcrumb As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents FlowTools As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents PanelBottom As System.Windows.Forms.Panel
    Friend WithEvents TxtDetail As System.Windows.Forms.TextBox
    Friend WithEvents StatusMain As System.Windows.Forms.StatusStrip
    Friend WithEvents lblStatus As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents lblProgress As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents lblMode As System.Windows.Forms.Label
    Friend WithEvents cboMode As System.Windows.Forms.ComboBox
    Friend WithEvents lblMetric As System.Windows.Forms.Label
    Friend WithEvents cboMetric As System.Windows.Forms.ComboBox
    Friend WithEvents lblLevel As System.Windows.Forms.Label
    Friend WithEvents cboLevel As System.Windows.Forms.ComboBox
    Friend WithEvents lblExtrude As System.Windows.Forms.Label
    Friend WithEvents cboExtrude As System.Windows.Forms.ComboBox
    Friend WithEvents lblMax As System.Windows.Forms.Label
    Friend WithEvents nudBuildings As System.Windows.Forms.NumericUpDown
    Friend WithEvents btnBack As System.Windows.Forms.Button
    Friend WithEvents btnFit As System.Windows.Forms.Button
    Friend WithEvents btnSnapshot As System.Windows.Forms.Button
    Friend WithEvents lblPalette As System.Windows.Forms.Label
    Friend WithEvents cboPalette As System.Windows.Forms.ComboBox

End Class
