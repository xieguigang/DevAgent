Imports Galaxy.Workbench.CommonDialogs

Public Class FormLinuxServers

    Private Sub FormLinuxServers_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call ApplyVsTheme(ToolStrip1)
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles AddConfig.Click
        InputDialog.Input(Of FormEditSsh)(
            Sub(result)
                Dim frm As FormEditSsh = DirectCast(result, FormEditSsh)
                Dim host As String = frm.host
                Dim password As String = frm.password
                Dim port As Integer = frm.port
                Dim user As String = frm.user
                Dim group As String = frm.group

                ' add tree node
                ' save connection parameter to config file
                ' add config reference to the treenode tag 
            End Sub)
    End Sub

    Private Sub EditToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EditToolStripMenuItem.Click
        If TreeView1.SelectedNode Is Nothing Then
            Return
        End If

        Dim node As TreeNode = TreeView1.SelectedNode

        If node.Tag Is Nothing Then
            Return
        End If


    End Sub

    Private Sub OpenConnectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenConnectionToolStripMenuItem.Click, TreeView1.DoubleClick
        If TreeView1.SelectedNode Is Nothing Then
            Return
        End If

        Dim node As TreeNode = TreeView1.SelectedNode

        If node.Tag Is Nothing Then
            Return
        End If

        ' get connection parameter from tag
        Dim host As String
        Dim port As Integer
        Dim user As String
        Dim password As String

        Call RibbonMenu.OpenBash().Connection(host, user, password, port)
    End Sub
End Class