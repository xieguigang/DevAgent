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
        hostLabel = New Label()
        hostTextBox = New TextBox()
        portLabel = New Label()
        portTextBox = New TextBox()
        userLabel = New Label()
        userTextBox = New TextBox()
        passwordLabel = New Label()
        passwordMaskedTextBox = New MaskedTextBox()
        groupLabel = New Label()
        groupComboBox = New ComboBox()
        okButton = New Button()
        GroupBox1 = New GroupBox()
        GroupBox1.SuspendLayout()
        SuspendLayout()
        ' 
        ' hostLabel
        ' 
        hostLabel.AutoSize = True
        hostLabel.Location = New Point(39, 35)
        hostLabel.Name = "hostLabel"
        hostLabel.Size = New Size(35, 15)
        hostLabel.TabIndex = 0
        hostLabel.Text = "Host:"
        ' 
        ' hostTextBox
        ' 
        hostTextBox.Location = New Point(89, 32)
        hostTextBox.Name = "hostTextBox"
        hostTextBox.Size = New Size(240, 23)
        hostTextBox.TabIndex = 1
        ' 
        ' portLabel
        ' 
        portLabel.AutoSize = True
        portLabel.Location = New Point(42, 71)
        portLabel.Name = "portLabel"
        portLabel.Size = New Size(32, 15)
        portLabel.TabIndex = 2
        portLabel.Text = "Port:"
        ' 
        ' portTextBox
        ' 
        portTextBox.Location = New Point(89, 68)
        portTextBox.Name = "portTextBox"
        portTextBox.Size = New Size(120, 23)
        portTextBox.TabIndex = 3
        portTextBox.Text = "22"
        ' 
        ' userLabel
        ' 
        userLabel.AutoSize = True
        userLabel.Location = New Point(41, 107)
        userLabel.Name = "userLabel"
        userLabel.Size = New Size(33, 15)
        userLabel.TabIndex = 4
        userLabel.Text = "User:"
        ' 
        ' userTextBox
        ' 
        userTextBox.Location = New Point(89, 104)
        userTextBox.Name = "userTextBox"
        userTextBox.Size = New Size(240, 23)
        userTextBox.TabIndex = 5
        ' 
        ' passwordLabel
        ' 
        passwordLabel.AutoSize = True
        passwordLabel.Location = New Point(14, 143)
        passwordLabel.Name = "passwordLabel"
        passwordLabel.Size = New Size(60, 15)
        passwordLabel.TabIndex = 6
        passwordLabel.Text = "Password:"
        ' 
        ' passwordMaskedTextBox
        ' 
        passwordMaskedTextBox.Location = New Point(89, 140)
        passwordMaskedTextBox.Name = "passwordMaskedTextBox"
        passwordMaskedTextBox.Size = New Size(240, 23)
        passwordMaskedTextBox.TabIndex = 7
        passwordMaskedTextBox.UseSystemPasswordChar = True
        ' 
        ' groupLabel
        ' 
        groupLabel.AutoSize = True
        groupLabel.Location = New Point(31, 179)
        groupLabel.Name = "groupLabel"
        groupLabel.Size = New Size(43, 15)
        groupLabel.TabIndex = 8
        groupLabel.Text = "Group:"
        ' 
        ' groupComboBox
        ' 
        groupComboBox.DropDownStyle = ComboBoxStyle.DropDown
        groupComboBox.Location = New Point(89, 176)
        groupComboBox.Name = "groupComboBox"
        groupComboBox.Size = New Size(240, 23)
        groupComboBox.TabIndex = 9
        ' 
        ' okButton
        ' 
        okButton.DialogResult = DialogResult.OK
        okButton.Location = New Point(265, 231)
        okButton.Name = "okButton"
        okButton.Size = New Size(90, 28)
        okButton.TabIndex = 10
        okButton.Text = "OK"
        okButton.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(hostTextBox)
        GroupBox1.Controls.Add(hostLabel)
        GroupBox1.Controls.Add(groupComboBox)
        GroupBox1.Controls.Add(groupLabel)
        GroupBox1.Controls.Add(portLabel)
        GroupBox1.Controls.Add(passwordMaskedTextBox)
        GroupBox1.Controls.Add(portTextBox)
        GroupBox1.Controls.Add(passwordLabel)
        GroupBox1.Controls.Add(userLabel)
        GroupBox1.Controls.Add(userTextBox)
        GroupBox1.Location = New Point(12, 12)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(343, 213)
        GroupBox1.TabIndex = 11
        GroupBox1.TabStop = False
        GroupBox1.Text = "SSH Connection"
        ' 
        ' FormEditSsh
        ' 
        AcceptButton = okButton
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(364, 267)
        Controls.Add(GroupBox1)
        Controls.Add(okButton)
        Name = "FormEditSsh"
        Text = "Edit SSH Connection"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        ResumeLayout(False)
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
    Friend WithEvents groupComboBox As System.Windows.Forms.ComboBox
    Friend WithEvents okButton As System.Windows.Forms.Button
    Friend WithEvents GroupBox1 As GroupBox
End Class
