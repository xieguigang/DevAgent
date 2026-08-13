Imports Galaxy.Workbench.CommonDialogs
Imports VallinaDevelopment.Settings

Public Class FormEditSsh

    ''' <summary>
    ''' Fill the dialog inputs from an existing connection (used when editing).
    ''' </summary>
    Public Sub SetConfig(conn As SshConnection)
        If conn Is Nothing Then
            Return
        End If

        hostTextBox.Text = conn.host
        portTextBox.Text = conn.port.ToString()
        userTextBox.Text = conn.user
        passwordMaskedTextBox.Text = conn.Password
        groupComboBox.Text = conn.group
    End Sub

    ''' <summary>
    ''' Populate the group ComboBox with existing group labels loaded from the config.
    ''' This allows the user to either pick an existing group or type a new one.
    ''' </summary>
    Private Sub LoadExistingGroups()
        Dim existing = ConfigJSON _
            .Load() _
            .sshServers _
            .connections _
            .Select(Function(c) c.group) _
            .Where(Function(g) Not String.IsNullOrWhiteSpace(g)) _
            .Distinct() _
            .OrderBy(Function(g) g) _
            .ToArray()

        groupComboBox.Items.Clear()
        groupComboBox.Items.AddRange(existing)
    End Sub

    Private Sub FormEditSsh_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call LoadExistingGroups()
    End Sub

    Private Sub okButton_Click(sender As Object, e As EventArgs) Handles okButton.Click
        Me.DialogResult = DialogResult.OK
    End Sub

    Public ReadOnly Property host As String
        Get
            Return If(hostTextBox?.Text, "").Trim()
        End Get
    End Property

    Public ReadOnly Property port As Integer
        Get
            Dim p As Integer
            If Integer.TryParse(If(portTextBox?.Text, "").Trim(), p) Then
                Return If(p > 0, p, 22)
            End If
            Return 22
        End Get
    End Property

    Public ReadOnly Property user As String
        Get
            Return If(userTextBox?.Text, "").Trim()
        End Get
    End Property

    Public ReadOnly Property password As String
        Get
            Return If(passwordMaskedTextBox?.Text, "")
        End Get
    End Property

    Public ReadOnly Property group As String
        Get
            Dim g = If(groupTextBox?.Text, "").Trim()
            Return SshServerConfig.NormalizeGroup(g)
        End Get
    End Property

End Class
