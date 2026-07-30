Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj

Public Class FormSolutionExplorer

    Public Property ProjectFile As String

    Public ReadOnly Property Workspace As String
        Get
            Return ProjectFile.ParentPath.GetDirectoryFullPath
        End Get
    End Property

    Dim proj As VBProject

    Private Sub FormSolutionExplorer_Load(sender As Object, e As EventArgs) Handles Me.Load
        TabText = $"Project Explorer [{ProjectFile.FileName}]"
        proj = VBProject.Load(ProjectFile)

        Call ApplyVsTheme(ToolStrip1, ContextMenuStrip1)
        Call LoadProjectFileTree()
    End Sub

    Private Sub LoadProjectFileTree()
        Dim ws As String = Workspace
        Dim files As String() = proj.EnumerateSourceFiles(skipAssmInfo:=False, fullName:=True) _
            .Select(Function(file)
                        Return file.GetFullPath _
                            .Replace(ws, "/") _
                            .Replace("//", "/")
                    End Function) _
            .ToArray
        Dim tree = FileSystemTree.BuildTree(files)

        TreeView1.LoadFileSystemTree(tree)
        TreeView1.Nodes(0).Text = proj.AssemblyName
    End Sub

    Private Sub TreeView1_NodeMouseDoubleClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseDoubleClick
        Dim node As FileSystemTree = e.Node.Tag

        If node.IsDirectory Then
            Return
        ElseIf node.FullName = "/" Then
            Return
        End If

        Call RibbonMenu.OpenFileEdit($"{Workspace}/{node.FullName}".GetFullPath)
    End Sub

    Private Sub OpenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenToolStripMenuItem.Click
        Dim node = TreeView1.SelectedNode

        If node Is Nothing Then
            Return
        End If

        Dim file As FileSystemTree = node.Tag

        If file.IsDirectory Then
        Else
            Call RibbonMenu.OpenFileEdit($"{Workspace}/{ file.FullName}".GetFullPath)
        End If
    End Sub

    ''' <summary>
    ''' open llm agent
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        Call RibbonMenu.LaunchLLMAgent(Workspace)
    End Sub

    Private Sub CopyFilePathToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopyFilePathToolStripMenuItem.Click
        Dim node = TreeView1.SelectedNode

        If node Is Nothing Then
            Return
        End If

        Dim file As FileSystemTree = node.Tag
        Dim fullname As String = Workspace & "/" & file.FullName

        Call Clipboard.SetText(fullname.GetFullPath)
    End Sub

    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        proj = VBProject.Load(ProjectFile)
        LoadProjectFileTree()
    End Sub
End Class