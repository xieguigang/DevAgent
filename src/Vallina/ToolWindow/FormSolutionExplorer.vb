Imports Galaxy.Workbench
Imports Galaxy.Workbench.CommonDialogs
Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.sln
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VersionControl.Git

Public Class FormSolutionExplorer

    Public Property ProjectFile As String

    Public ReadOnly Property Workspace As String
        Get
            If proj Is Nothing Then
                Return Nothing
            End If

            If TypeOf proj Is VBProject Then
                Return ProjectFile.ParentPath.GetDirectoryFullPath
            ElseIf TypeOf proj Is FolderWorkspace Then
                Return ProjectFile
            Else
                Throw New NotImplementedException(proj.GetType.FullName)
            End If
        End Get
    End Property

    Dim proj As IProjectWorkspace

    Private Sub FormSolutionExplorer_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call ApplyVsTheme(ToolStrip1, ContextMenuStrip1)
    End Sub

    Public Sub Reload() Handles ToolStripButton3.Click
        Dim oldName As String = If(proj Is Nothing, "", proj.Name)

        TabText = $"Project Explorer [{ProjectFile.FileName}]"

        Select Case ProjectFile.ExtensionSuffix
            Case "vbproj"
                proj = VBProject.Load(ProjectFile)
                Call LoadVBProjectFileTree()
            Case Else
                proj = New FolderWorkspace(ProjectFile)
                Call LoadFolderProjectTree()
        End Select

        ' switch to new workspace context for LLM
        If proj.Name <> oldName Then
            RibbonMenu.OpenLLMsChat.Clear()
        End If
    End Sub

    Private Sub LoadFolderProjectTree()
        Call LoadFolderFileTree(files:=DirectCast(proj, FolderWorkspace).GetCompileFiles _
            .Select(Function(file)
                        Return file.GetFullPath _
                            .Replace(Workspace, "/") _
                            .Replace("//", "/")
                    End Function))
    End Sub

    Private Sub LoadFolderFileTree(files As IEnumerable(Of String))
        Dim tree = FileSystemTree.BuildTree(files)

        TreeView1.LoadFileSystemTree(tree)
        TreeView1.Nodes(0).Text = proj.Name
    End Sub

    Private Sub LoadVBProjectFileTree()
        Dim ws As String = Workspace
        Dim files As String() = DirectCast(proj, VBProject).EnumerateSourceFiles(skipAssmInfo:=False, fullName:=True) _
            .Select(Function(file)
                        Return file.GetFullPath _
                            .Replace(ws, "/") _
                            .Replace("//", "/")
                    End Function) _
            .ToArray

        Call LoadFolderFileTree(files)
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

    ''' <summary>
    ''' open source file
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub OpenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenToolStripMenuItem.Click
        Dim sourceFile As String = GetRequestSourceFile()

        If Not sourceFile.StringEmpty(, True) Then
            Call RibbonMenu.OpenFileEdit(sourceFile)
        End If
    End Sub

    Public Function GetRequestSourceFile() As String
        Dim node As TreeNode = TreeView1.SelectedNode

        If node Is Nothing Then
            Return Nothing
        End If

        Dim file As FileSystemTree = node.Tag

        If file.IsDirectory Then
            Return Nothing
        Else
            Return $"{Workspace}/{ file.FullName}".GetFullPath
        End If
    End Function

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

    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        Dim gitdiff As DiffResult = TaskProgress.LoadData(Function(p As ITaskProgress) diff.GetDiff(Workspace), title:="Git diff", info:="Run git diff for get code edit different details.", canbeCancel:=True, host:=Me)
        Dim viewer As New FormGitDiff With {.GitDiff = gitdiff}

        If gitdiff IsNot Nothing Then
            Call InputDialog.ShowDialog(viewer)
        End If
    End Sub

    Private Async Sub LLMExplainToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LLMExplainToolStripMenuItem.Click
        Dim sourceFile As String = GetRequestSourceFile()

        If Not sourceFile.StringEmpty(, True) Then
            Dim llmbox = RibbonMenu.OpenLLMsChat

            Await llmbox.ClearFileReference()
            Await llmbox.SetFileReference(sourceFile)
            Await llmbox.SendMessage(promptText:="请帮助我理解当前的这个源代码：解释代码所实现的功能，以及该功能是如何实现的？")
        End If
    End Sub
End Class