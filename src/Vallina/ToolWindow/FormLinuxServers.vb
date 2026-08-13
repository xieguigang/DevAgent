Imports Galaxy.Workbench.CommonDialogs
Imports VallinaDevelopment.Settings

Public Class FormLinuxServers

    Private config As ConfigJSON

    Private Sub FormLinuxServers_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call ApplyVsTheme(ToolStrip1)
        config = ConfigJSON.Load()
        Call RefreshTree()
    End Sub

    ''' <summary>
    ''' Rebuild the two-level TreeView from the in-memory config:
    ''' root nodes = group labels, leaf nodes = SSH connections (Tag = SshConnection).
    ''' </summary>
    Private Sub RefreshTree()
        TreeView1.Nodes.Clear()

        Dim groups = config.sshServers.connections _
            .GroupBy(Function(c) c.group) _
            .OrderBy(Function(g) g.Key)

        For Each g In groups
            Dim groupNode As New TreeNode(g.Key)
            groupNode.Tag = Nothing

            For Each conn In g.OrderBy(Function(c) c.DisplayName)
                Dim leaf As New TreeNode(conn.DisplayName)
                leaf.Tag = conn
                groupNode.Nodes.Add(leaf)
            Next

            TreeView1.Nodes.Add(groupNode)
        Next

        TreeView1.ExpandAll()
    End Sub

    Private Sub SaveAndRefresh()
        If config Is Nothing Then
            config = ConfigJSON.Load()
        End If
        Call config.Save()
        Call RefreshTree()
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles AddConfig.Click
        InputDialog.Input(Of FormEditSsh)(
            Sub(result)
                Dim frm As FormEditSsh = DirectCast(result, FormEditSsh)

                Dim conn As New SshConnection With {
                    .host = frm.host,
                    .port = frm.port,
                    .user = frm.user,
                    .Password = frm.password,
                    .group = frm.group
                }

                If String.IsNullOrWhiteSpace(conn.host) OrElse String.IsNullOrWhiteSpace(conn.user) Then
                    Return
                End If

                config.sshServers.connections.Add(conn)
                Call SaveAndRefresh()
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

        Dim conn As SshConnection = DirectCast(node.Tag, SshConnection)

        Dim editor As New FormEditSsh
        Call editor.SetConfig(conn)

        InputDialog.Input(Of FormEditSsh)(
            Sub(result)
                Dim frm As FormEditSsh = DirectCast(result, FormEditSsh)

                conn.host = frm.host
                conn.port = frm.port
                conn.user = frm.user
                conn.Password = frm.password
                conn.group = frm.group

                If String.IsNullOrWhiteSpace(conn.host) OrElse String.IsNullOrWhiteSpace(conn.user) Then
                    Return
                End If

                Call SaveAndRefresh()
            End Sub,
            config:=editor)
    End Sub

    Private Sub DeleteConfigButton_Click(sender As Object, e As EventArgs) Handles DeleteConfig.Click
        Call DeleteToolStripMenuItem_Click(DeleteConfig, EventArgs.Empty)
    End Sub

    Private Sub DeleteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DeleteToolStripMenuItem.Click
        If TreeView1.SelectedNode Is Nothing Then
            Return
        End If

        Dim node As TreeNode = TreeView1.SelectedNode

        If node.Tag Is Nothing Then
            Return
        End If

        Dim conn As SshConnection = DirectCast(node.Tag, SshConnection)
        config.sshServers.Remove(conn)
        Call SaveAndRefresh()
    End Sub

    Private Sub OpenConnectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenConnectionToolStripMenuItem.Click, TreeView1.DoubleClick
        If TreeView1.SelectedNode Is Nothing Then
            Return
        End If

        Dim node As TreeNode = TreeView1.SelectedNode

        If node.Tag Is Nothing Then
            Return
        End If

        Dim conn As SshConnection = DirectCast(node.Tag, SshConnection)

        Call RibbonMenu.OpenBash().Connection(conn.host, conn.user, conn.Password, conn.port)
    End Sub

End Class
