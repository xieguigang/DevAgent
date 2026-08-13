Imports Galaxy.Workbench.CommonDialogs

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormEditSsh
    Inherits InputDialog

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
        components = New System.ComponentModel.Container()
        hostLabel = New System.Windows.Forms.Label()
        hostTextBox = New System.Windows.Forms.TextBox()
        portLabel = New System.Windows.Forms.Label()
        portTextBox = New System.Windows.Forms.TextBox()
        userLabel = New System.Windows.Forms.Label()
        userTextBox = New System.Windows.Forms.TextBox()
        passwordLabel = New System.Windows.Forms.Label()
        passwordMaskedTextBox = New System.Windows.Forms.MaskedTextBox()
        groupLabel = New System.Windows.Forms.Label()
        groupTextBox = New System.Windows.Forms.TextBox()
        okButton = New System.Windows.Forms.Button()
        cancelButton = New System.Windows.Forms.Button()
        SuspendLayout()
        ' 
        ' hostLabel
        ' 
        hostLabel.AutoSize = True
        hostLabel.Location = New System.Drawing.Point(20, 20)
        hostLabel.Name = "hostLabel"
        hostLabel.Size = New System.Drawing.Size(35, 15)
        hostLabel.TabIndex = 0
        hostLabel.Text = "Host:"
        ' 
        ' hostTextBox
        ' 
        hostTextBox.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        hostTextBox.Location = New System.Drawing.Point(120, 17)
        hostTextBox.Name = "hostTextBox"
        hostTextBox.Size = New System.Drawing.Size(340, 23)
        hostTextBox.TabIndex = 1
        ' 
        ' portLabel
        ' 
        portLabel.AutoSize = True
        portLabel.Location = New System.Drawing.Point(20, 56)
        portLabel.Name = "portLabel"
        portLabel.Size = New System.Drawing.Size(32, 15)
        portLabel.TabIndex = 2
        portLabel.Text = "Port:"
        ' 
        ' portTextBox
        ' 
        portTextBox.Location = New System.Drawing.Point(120, 53)
        portTextBox.Name = "portTextBox"
        portTextBox.Size = New System.Drawing.Size(120, 23)
        portTextBox.TabIndex = 3
        portTextBox.Text = "22"
        ' 
        ' userLabel
        ' 
        userLabel.AutoSize = True
        userLabel.Location = New System.Drawing.Point(20, 92)
        userLabel.Name = "userLabel"
        userLabel.Size = New System.Drawing.Size(35, 15)
        userLabel.TabIndex = 4
        userLabel.Text = "User:"
        ' 
        ' userTextBox
        ' 
        userTextBox.Location = New System.Drawing.Point(120, 89)
        userTextBox.Name = "userTextBox"
        userTextBox.Size = New System.Drawing.Size(340, 23)
        userTextBox.TabIndex = 5
        ' 
        ' passwordLabel
        ' 
        passwordLabel.AutoSize = True
        passwordLabel.Location = New System.Drawing.Point(20, 128)
        passwordLabel.Name = "passwordLabel"
        passwordLabel.Size = New System.Drawing.Size(59, 15)
        passwordLabel.TabIndex = 6
        passwordLabel.Text = "Password:"
        ' 
        ' passwordMaskedTextBox
        ' 
        passwordMaskedTextBox.Location = New System.Drawing.Point(120, 125)
        passwordMaskedTextBox.Name = "passwordMaskedTextBox"
        passwordMaskedTextBox.Size = New System.Drawing.Size(340, 23)
        passwordMaskedTextBox.TabIndex = 7
        passwordMaskedTextBox.UseSystemPasswordChar = True
        ' 
        ' groupLabel
        ' 
        groupLabel.AutoSize = True
        groupLabel.Location = New System.Drawing.Point(20, 164)
        groupLabel.Name = "groupLabel"
        groupLabel.Size = New System.Drawing.Size(44, 15)
        groupLabel.TabIndex = 8
        groupLabel.Text = "Group:"
        ' 
        ' groupTextBox
        ' 
        groupTextBox.Location = New System.Drawing.Point(120, 161)
        groupTextBox.Name = "groupTextBox"
        groupTextBox.Size = New System.Drawing.Size(340, 23)
        groupTextBox.TabIndex = 9
        ' 
        ' okButton
        ' 
        okButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        okButton.DialogResult = System.Windows.Forms.DialogResult.OK
        okButton.Location = New System.Drawing.Point(280, 460)
        okButton.Name = "okButton"
        okButton.Size = New System.Drawing.Size(90, 28)
        okButton.TabIndex = 10
        okButton.Text = "OK"
        okButton.UseVisualStyleBackColor = True
        ' 
        ' cancelButton
        ' 
        cancelButton.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
        cancelButton.Location = New System.Drawing.Point(376, 460)
        cancelButton.Name = "cancelButton"
        cancelButton.Size = New System.Drawing.Size(90, 28)
        cancelButton.TabIndex = 11
        cancelButton.Text = "Cancel"
        cancelButton.UseVisualStyleBackColor = True
        ' 
        ' FormEditSsh
        ' 
        AcceptButton = okButton
        AutoScaleDimensions = New System.Drawing.SizeF(7F, 15F)
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        CancelButton = cancelButton
        ClientSize = New System.Drawing.Size(495, 499)
        Controls.Add(hostLabel)
        Controls.Add(hostTextBox)
        Controls.Add(portLabel)
        Controls.Add(portTextBox)
        Controls.Add(userLabel)
        Controls.Add(userTextBox)
        Controls.Add(passwordLabel)
        Controls.Add(passwordMaskedTextBox)
        Controls.Add(groupLabel)
        Controls.Add(groupTextBox)
        Controls.Add(okButton)
        Controls.Add(cancelButton)
        Name = "FormEditSsh"
        Text = "Edit SSH Connection"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents hostLabel As System.Windows.Forms.Label
    Friend WithEvents hostTextBox As System.Windows.Forms.TextBox
    Friend WithEvents portLabel As System.Windows.Forms.Label
    Friend WithEvents portTextBox As System.Windows.Forms.TextBox
    Friend WithEvents userLabel As System.Windows.Forms.Label
    Friend WithEvents userTextBox As System.Windows.Forms.TextBox
    Friend WithEvents passwordLabel As System.Windows.Forms.Label
    Friend WithEvents passwordMaskedTextBox As System.Windows.Forms.MaskedTextBox
    Friend WithEvents groupLabel As System.Windows.Forms.Label
    Friend WithEvents groupTextBox As System.Windows.Forms.TextBox
    Friend WithEvents okButton As System.Windows.Forms.Button
    Friend WithEvents cancelButton As System.Windows.Forms.Button
End Class
