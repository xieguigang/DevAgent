Imports Galaxy.Workbench.CommonDialogs

Public Class FormLinuxServers

    Private Sub FormLinuxServers_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call ApplyVsTheme(ToolStrip1)
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        InputDialog.Input(Of FormEditSsh)(
            Sub(result)

            End Sub)
    End Sub
End Class