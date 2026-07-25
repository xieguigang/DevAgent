Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.vbproj
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.vbproj.Xml

Public Class FormSolutionExplorer

    Public Property ProjectFile As String

    Public ReadOnly Property Workspace As String
        Get
            Return ProjectFile.ParentPath.GetDirectoryFullPath
        End Get
    End Property

    Dim proj As Project

    Private Sub FormSolutionExplorer_Load(sender As Object, e As EventArgs) Handles Me.Load
        TabText = $"Project Explorer [{ProjectFile.FileName}]"
        proj = Project.Load(ProjectFile)

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

        Call TreeView1.LoadFileSystemTree(tree)
    End Sub

    Private Sub TreeView1_NodeMouseDoubleClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseDoubleClick
        Dim node As FileSystemTree = e.Node.Tag

        If node.IsDirectory Then
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
End Class