Imports Microsoft.VisualBasic.Windows.Forms.SshClient

Public Class FormSsh

    Public Sub Connection(host As String, user As String, passwd As String, port As Integer)
        Dim SshConsole As SshWinFormConsole = SshWinFormConsole1

        SshConsole.ConnectionOptions = New SshConnectionOptions() With {
            .Host = host,
            .Port = port,
            .UserName = user,
            .Password = passwd
        }
        SshConsole.Connect()
        SshConsole.Focus()
    End Sub

    Private Sub FormSsh_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        SshWinFormConsole1.Disconnect()
    End Sub
End Class