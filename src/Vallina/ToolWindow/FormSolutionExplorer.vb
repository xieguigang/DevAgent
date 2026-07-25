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

        Call ApplyVsTheme(ToolStrip1)
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
End Class