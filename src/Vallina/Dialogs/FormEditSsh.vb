Imports Galaxy.Workbench.CommonDialogs
Imports Vallina.Application.Settings

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
        groupTextBox.Text = conn.group
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
