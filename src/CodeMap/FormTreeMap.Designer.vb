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
        PanelTop = New Panel()
        FlowTools = New FlowLayoutPanel()
        lblMode = New Label()
        cboMode = New ComboBox()
        lblMetric = New Label()
        cboMetric = New ComboBox()
        lblLevel = New Label()
        cboLevel = New ComboBox()
        lblExtrude = New Label()
        cboExtrude = New ComboBox()
        lblMax = New Label()
        nudBuildings = New NumericUpDown()
        btnBack = New Button()
        btnFit = New Button()
        btnSnapshot = New Button()
        lblPalette = New Label()
        cboPalette = New ComboBox()
        FlowBreadcrumb = New FlowLayoutPanel()
        PanelBottom = New Panel()
        TxtDetail = New TextBox()
        StatusMain = New StatusStrip()
        lblStatus = New ToolStripStatusLabel()
        lblProgress = New ToolStripStatusLabel()
        Canvas = New TreeNavMap()
        PanelTop.SuspendLayout()
        FlowTools.SuspendLayout()
        CType(nudBuildings, ComponentModel.ISupportInitialize).BeginInit()
        PanelBottom.SuspendLayout()
        StatusMain.SuspendLayout()
        SuspendLayout()
        ' 
        ' PanelTop
        ' 
        PanelTop.BackColor = Color.FromArgb(17, 24, 39)
        PanelTop.Controls.Add(FlowTools)
        PanelTop.Controls.Add(FlowBreadcrumb)
        PanelTop.Dock = DockStyle.Top
        PanelTop.Location = New Point(0, 0)
        PanelTop.Name = "PanelTop"
        PanelTop.Size = New Size(1280, 68)
        PanelTop.TabIndex = 1
        ' 
        ' FlowTools
        ' 
        FlowTools.Controls.Add(lblMode)
        FlowTools.Controls.Add(cboMode)
        FlowTools.Controls.Add(lblMetric)
        FlowTools.Controls.Add(cboMetric)
        FlowTools.Controls.Add(lblLevel)
        FlowTools.Controls.Add(cboLevel)
        FlowTools.Controls.Add(lblExtrude)
        FlowTools.Controls.Add(cboExtrude)
        FlowTools.Controls.Add(lblMax)
        FlowTools.Controls.Add(nudBuildings)
        FlowTools.Controls.Add(btnBack)
        FlowTools.Controls.Add(btnFit)
        FlowTools.Controls.Add(btnSnapshot)
        FlowTools.Controls.Add(lblPalette)
        FlowTools.Controls.Add(cboPalette)
        FlowTools.Dock = DockStyle.Fill
        FlowTools.Location = New Point(0, 30)
        FlowTools.Name = "FlowTools"
        FlowTools.Padding = New Padding(8, 4, 8, 0)
        FlowTools.Size = New Size(1280, 38)
        FlowTools.TabIndex = 1
        FlowTools.WrapContents = False
        ' 
        ' lblMode
        ' 
        lblMode.AutoSize = True
        lblMode.ForeColor = Color.FromArgb(148, 163, 184)
        lblMode.Location = New Point(11, 12)
        lblMode.Margin = New Padding(3, 8, 3, 0)
        lblMode.Name = "lblMode"
        lblMode.Size = New Size(33, 15)
        lblMode.TabIndex = 0
        lblMode.Text = "模式"
        ' 
        ' cboMode
        ' 
        cboMode.DropDownStyle = ComboBoxStyle.DropDownList
        cboMode.FormattingEnabled = True
        cboMode.Items.AddRange(New Object() {"2D 平面", "3D 城市"})
        cboMode.Location = New Point(50, 7)
        cboMode.Name = "cboMode"
        cboMode.Size = New Size(88, 23)
        cboMode.TabIndex = 1
        ' 
        ' lblMetric
        ' 
        lblMetric.AutoSize = True
        lblMetric.ForeColor = Color.FromArgb(148, 163, 184)
        lblMetric.Location = New Point(144, 12)
        lblMetric.Margin = New Padding(3, 8, 3, 0)
        lblMetric.Name = "lblMetric"
        lblMetric.Size = New Size(33, 15)
        lblMetric.TabIndex = 2
        lblMetric.Text = "度量"
        ' 
        ' cboMetric
        ' 
        cboMetric.DropDownStyle = ComboBoxStyle.DropDownList
        cboMetric.FormattingEnabled = True
        cboMetric.Items.AddRange(New Object() {"代码行数", "字符数", "符号数"})
        cboMetric.Location = New Point(183, 7)
        cboMetric.Name = "cboMetric"
        cboMetric.Size = New Size(88, 23)
        cboMetric.TabIndex = 3
        ' 
        ' lblLevel
        ' 
        lblLevel.AutoSize = True
        lblLevel.ForeColor = Color.FromArgb(148, 163, 184)
        lblLevel.Location = New Point(277, 12)
        lblLevel.Margin = New Padding(3, 8, 3, 0)
        lblLevel.Name = "lblLevel"
        lblLevel.Size = New Size(59, 15)
        lblLevel.TabIndex = 4
        lblLevel.Text = "展开层级"
        ' 
        ' cboLevel
        ' 
        cboLevel.DropDownStyle = ComboBoxStyle.DropDownList
        cboLevel.FormattingEnabled = True
        cboLevel.Items.AddRange(New Object() {"vbproj", "folder", "source file", "type", "member"})
        cboLevel.Location = New Point(342, 7)
        cboLevel.Name = "cboLevel"
        cboLevel.Size = New Size(104, 23)
        cboLevel.TabIndex = 5
        ' 
        ' lblExtrude
        ' 
        lblExtrude.AutoSize = True
        lblExtrude.ForeColor = Color.FromArgb(148, 163, 184)
        lblExtrude.Location = New Point(452, 12)
        lblExtrude.Margin = New Padding(3, 8, 3, 0)
        lblExtrude.Name = "lblExtrude"
        lblExtrude.Size = New Size(46, 15)
        lblExtrude.TabIndex = 6
        lblExtrude.Text = "挤出层"
        ' 
        ' cboExtrude
        ' 
        cboExtrude.DropDownStyle = ComboBoxStyle.DropDownList
        cboExtrude.FormattingEnabled = True
        cboExtrude.Items.AddRange(New Object() {"folder", "source file", "type", "member"})
        cboExtrude.Location = New Point(504, 7)
        cboExtrude.Name = "cboExtrude"
        cboExtrude.Size = New Size(104, 23)
        cboExtrude.TabIndex = 7
        ' 
        ' lblMax
        ' 
        lblMax.AutoSize = True
        lblMax.ForeColor = Color.FromArgb(148, 163, 184)
        lblMax.Location = New Point(614, 12)
        lblMax.Margin = New Padding(3, 8, 3, 0)
        lblMax.Name = "lblMax"
        lblMax.Size = New Size(59, 15)
        lblMax.TabIndex = 8
        lblMax.Text = "建筑上限"
        ' 
        ' nudBuildings
        ' 
        nudBuildings.Increment = New Decimal(New Integer() {500, 0, 0, 0})
        nudBuildings.Location = New Point(679, 7)
        nudBuildings.Maximum = New Decimal(New Integer() {100000, 0, 0, 0})
        nudBuildings.Minimum = New Decimal(New Integer() {100, 0, 0, 0})
        nudBuildings.Name = "nudBuildings"
        nudBuildings.Size = New Size(80, 23)
        nudBuildings.TabIndex = 9
        nudBuildings.Value = New Decimal(New Integer() {3000, 0, 0, 0})
        ' 
        ' btnBack
        ' 
        btnBack.FlatStyle = FlatStyle.Flat
        btnBack.ForeColor = Color.FromArgb(226, 232, 240)
        btnBack.Location = New Point(765, 7)
        btnBack.Name = "btnBack"
        btnBack.Size = New Size(72, 25)
        btnBack.TabIndex = 10
        btnBack.Text = "返回上级"
        btnBack.UseVisualStyleBackColor = True
        ' 
        ' btnFit
        ' 
        btnFit.FlatStyle = FlatStyle.Flat
        btnFit.ForeColor = Color.FromArgb(226, 232, 240)
        btnFit.Location = New Point(843, 7)
        btnFit.Name = "btnFit"
        btnFit.Size = New Size(72, 25)
        btnFit.TabIndex = 11
        btnFit.Text = "适应视图"
        btnFit.UseVisualStyleBackColor = True
        ' 
        ' btnSnapshot
        ' 
        btnSnapshot.FlatStyle = FlatStyle.Flat
        btnSnapshot.ForeColor = Color.FromArgb(226, 232, 240)
        btnSnapshot.Location = New Point(921, 7)
        btnSnapshot.Name = "btnSnapshot"
        btnSnapshot.Size = New Size(72, 25)
        btnSnapshot.TabIndex = 12
        btnSnapshot.Text = "导出快照"
        btnSnapshot.UseVisualStyleBackColor = True
        ' 
        ' lblPalette
        ' 
        lblPalette.AutoSize = True
        lblPalette.ForeColor = Color.FromArgb(148, 163, 184)
        lblPalette.Location = New Point(999, 12)
        lblPalette.Margin = New Padding(3, 8, 3, 0)
        lblPalette.Name = "lblPalette"
        lblPalette.Size = New Size(33, 15)
        lblPalette.TabIndex = 13
        lblPalette.Text = "配色"
        ' 
        ' cboPalette
        ' 
        cboPalette.DropDownStyle = ComboBoxStyle.DropDownList
        cboPalette.FormattingEnabled = True
        cboPalette.Items.AddRange(New Object() {"viridis", "magma", "inferno", "plasma", "turbo", "Jet", "Hot", "Cool", "Rainbow", "按层级着色"})
        cboPalette.Location = New Point(1038, 7)
        cboPalette.Name = "cboPalette"
        cboPalette.Size = New Size(104, 23)
        cboPalette.TabIndex = 14
        ' 
        ' FlowBreadcrumb
        ' 
        FlowBreadcrumb.BackColor = Color.FromArgb(15, 23, 42)
        FlowBreadcrumb.Dock = DockStyle.Top
        FlowBreadcrumb.Location = New Point(0, 0)
        FlowBreadcrumb.Name = "FlowBreadcrumb"
        FlowBreadcrumb.Padding = New Padding(8, 4, 8, 0)
        FlowBreadcrumb.Size = New Size(1280, 30)
        FlowBreadcrumb.TabIndex = 0
        FlowBreadcrumb.WrapContents = False
        ' 
        ' PanelBottom
        ' 
        PanelBottom.BackColor = Color.FromArgb(17, 24, 39)
        PanelBottom.Controls.Add(TxtDetail)
        PanelBottom.Dock = DockStyle.Bottom
        PanelBottom.Location = New Point(0, 481)
        PanelBottom.Name = "PanelBottom"
        PanelBottom.Padding = New Padding(8)
        PanelBottom.Size = New Size(1280, 170)
        PanelBottom.TabIndex = 2
        ' 
        ' TxtDetail
        ' 
        TxtDetail.BackColor = Color.FromArgb(2, 6, 23)
        TxtDetail.BorderStyle = BorderStyle.None
        TxtDetail.Dock = DockStyle.Fill
        TxtDetail.Font = New Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TxtDetail.ForeColor = Color.FromArgb(215, 227, 244)
        TxtDetail.Location = New Point(8, 8)
        TxtDetail.Multiline = True
        TxtDetail.Name = "TxtDetail"
        TxtDetail.ReadOnly = True
        TxtDetail.ScrollBars = ScrollBars.Both
        TxtDetail.Size = New Size(1264, 154)
        TxtDetail.TabIndex = 0
        TxtDetail.WordWrap = False
        ' 
        ' StatusMain
        ' 
        StatusMain.BackColor = Color.FromArgb(15, 23, 42)
        StatusMain.Items.AddRange(New ToolStripItem() {lblStatus, lblProgress})
        StatusMain.Location = New Point(0, 651)
        StatusMain.Name = "StatusMain"
        StatusMain.Size = New Size(1280, 22)
        StatusMain.TabIndex = 3
        ' 
        ' lblStatus
        ' 
        lblStatus.ForeColor = Color.FromArgb(148, 163, 184)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(0, 17)
        ' 
        ' lblProgress
        ' 
        lblProgress.ForeColor = Color.FromArgb(45, 212, 191)
        lblProgress.Name = "lblProgress"
        lblProgress.Size = New Size(0, 17)
        ' 
        ' Canvas
        ' 
        Canvas.Dock = DockStyle.Fill
        Canvas.Location = New Point(0, 68)
        Canvas.Name = "Canvas"
        Canvas.Size = New Size(1280, 413)
        Canvas.TabIndex = 4
        ' 
        ' FormTreeMap
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(15, 23, 42)
        ClientSize = New Size(1280, 673)
        Controls.Add(Canvas)
        Controls.Add(PanelTop)
        Controls.Add(PanelBottom)
        Controls.Add(StatusMain)
        Name = "FormTreeMap"
        Text = "CodeMap · 代码库树图"
        PanelTop.ResumeLayout(False)
        FlowTools.ResumeLayout(False)
        FlowTools.PerformLayout()
        CType(nudBuildings, ComponentModel.ISupportInitialize).EndInit()
        PanelBottom.ResumeLayout(False)
        PanelBottom.PerformLayout()
        StatusMain.ResumeLayout(False)
        StatusMain.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub
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
    Friend WithEvents Canvas As TreeNavMap

End Class
