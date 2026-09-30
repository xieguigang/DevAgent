Imports System.Linq
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.sln
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.sln.File
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM.Syntax

Namespace CodeIndex

    ''' <summary>
    ''' open a code repository in read only mode and enumerate all of its
    ''' vb.net source files together with the parsed symbol trees.
    ''' </summary>
    Public Module WorkspaceLoader

        ''' <summary>directories that never contain hand written source code</summary>
        ReadOnly excludeDirs As String() = {
            "bin", "obj", ".git", ".vs", ".svn", ".hg", ".idea", ".vscode",
            "node_modules", "packages", "My Project", ".cache", "dist", "wwwroot"
        }

        ''' <summary>
        ''' open the given path: a folder (git repository), a .vbproj file or a
        ''' .sln / .slnx solution file.
        ''' </summary>
        Public Function Open(path As String) As WorkspaceInfo
            If String.IsNullOrWhiteSpace(path) Then
                Throw New ArgumentNullException(NameOf(path), "a workspace path is required.")
            End If

            path = IO.Path.GetFullPath(path)

            If IO.Directory.Exists(path) Then
                ' a git repository folder may carry one or more solution files;
                ' prefer the new xml solution format when both exist.
                Dim slnx As String() = IO.Directory.GetFiles(path, "*.slnx")
                Dim sln As String() = IO.Directory.GetFiles(path, "*.sln")

                If slnx.Length > 0 Then
                    Return OpenSolution(slnx(0))
                ElseIf sln.Length > 0 Then
                    Return OpenSolution(sln(0))
                Else
                    Return OpenFolder(path)
                End If
            ElseIf IO.File.Exists(path) Then
                Select Case IO.Path.GetExtension(path).ToLowerInvariant()
                    Case ".sln", ".slnx"
                        Return OpenSolution(path)
                    Case ".vbproj"
                        Return OpenProject(path)
                    Case Else
                        Throw New NotSupportedException($"unsupported workspace file '{path}', expecting .sln/.slnx/.vbproj or a folder.")
                End Select
            Else
                Throw New IO.DirectoryNotFoundException($"workspace '{path}' does not exist.")
            End If
        End Function

        Private Function OpenSolution(path As String) As WorkspaceInfo
            Dim sln As Solution = Solution.Load(path)
            Dim info As New WorkspaceInfo With {
                .Kind = WorkspaceKind.Solution,
                .Name = IO.Path.GetFileNameWithoutExtension(path),
                .RootPath = IO.Path.GetDirectoryName(path)
            }

            info.Workspace = sln.LoadWorkspace()

            For Each p As Project In sln.Projects
                If p Is Nothing OrElse p.IsFolder Then
                    Continue For
                End If

                Dim projFile As String = p.FullPath

                If String.IsNullOrEmpty(projFile) Then
                    projFile = sln.GetProjectFullPath(p)
                End If

                If String.IsNullOrEmpty(projFile) Then
                    Continue For
                End If

                projFile = IO.Path.GetFullPath(projFile)

                If Not IO.File.Exists(projFile) Then
                    Call info.Errors.Add($"project file is missing: {projFile}")
                    Continue For
                End If

                If Not String.Equals(IO.Path.GetExtension(projFile), ".vbproj", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Try
                    Dim proj As VBProject = VBProject.Load(projFile, parseDoc:=True)

                    ' .slnx files do not carry a display name, so the project
                    ' entry name falls back to the relative project file path.
                    Dim displayName As String = p.Name

                    If String.IsNullOrEmpty(displayName) OrElse
                        displayName.IndexOfAny(New Char() {IO.Path.DirectorySeparatorChar, "/"c, "\"c}) >= 0 Then

                        displayName = IO.Path.GetFileNameWithoutExtension(projFile)
                    End If

                    Call info.VbProjects.Add(proj)
                    Call info.Projects.Add(displayName)
                    Call AddProjectFiles(info, proj, IO.Path.GetDirectoryName(projFile))
                Catch ex As Exception
                    Call info.Errors.Add($"[{IO.Path.GetFileName(projFile)}] {ex.Message}")
                End Try
            Next

            Return info
        End Function

        Private Function OpenProject(path As String) As WorkspaceInfo
            Dim proj As VBProject = VBProject.Load(path, parseDoc:=True)
            Dim info As New WorkspaceInfo With {
                .Kind = WorkspaceKind.Project,
                .Name = If(String.IsNullOrEmpty(proj.AssemblyName), IO.Path.GetFileNameWithoutExtension(path), proj.AssemblyName),
                .RootPath = IO.Path.GetDirectoryName(path)
            }

            info.Workspace = proj
            info.VbProjects.Add(proj)
            info.Projects.Add(info.Name)

            Call AddProjectFiles(info, proj, IO.Path.GetDirectoryName(path))

            Return info
        End Function

        Private Sub AddProjectFiles(info As WorkspaceInfo, proj As VBProject, projDir As String)
            If proj.CompileFiles Is Nothing Then
                Return
            End If

            Dim projName As String = If(String.IsNullOrEmpty(proj.AssemblyName), proj.RootNamespace, proj.AssemblyName)

            For Each doc As VBDocument In proj.CompileFiles
                If doc Is Nothing OrElse String.IsNullOrEmpty(doc.FileName) Then
                    Continue For
                End If

                Dim rel As String = doc.FileName.Replace("/", "\")
                Dim abs As String

                Try
                    abs = IO.Path.GetFullPath(IO.Path.Combine(projDir, rel))
                Catch
                    Continue For
                End Try

                If Not IO.File.Exists(abs) Then
                    Continue For
                End If

                Call info.Files.Add(New SourceFile With {
                    .Project = projName,
                    .AbsolutePath = abs,
                    .RelativePath = rel,
                    .Types = doc.Types,
                    .Imports = doc.Imports
                })
            Next
        End Sub

        ''' <summary>
        ''' plain folder mode: walk the directory tree, parse every *.vb file
        ''' that is not inside a generated / vcs folder.
        ''' </summary>
        Private Function OpenFolder(path As String) As WorkspaceInfo
            Dim info As New WorkspaceInfo With {
                .Kind = WorkspaceKind.Folder,
                .Name = New IO.DirectoryInfo(path).Name,
                .RootPath = path
            }

            info.Workspace = New FolderWorkspace(path)

            For Each abs As String In EnumerateVbFiles(path)
                Dim rel As String = abs.Substring(path.Length).TrimStart("\"c, "/"c)

                Try
                    Dim code As String = IO.File.ReadAllText(abs)
                    Dim root As TypeContainerSymbol = VBParser.Parse(code)

                    Call SymbolCollector.FillSourceFilePath(root, abs)

                    Call info.Files.Add(New SourceFile With {
                        .Project = info.Name,
                        .AbsolutePath = abs,
                        .RelativePath = rel,
                        .Types = If(root.InternalNested, New Dictionary(Of String, LanguageSymbolType)())
                    })
                Catch ex As Exception
                    Call info.Errors.Add($"[{rel}] {ex.Message}")
                End Try
            Next

            Return info
        End Function

        ''' <summary>
        ''' depth first enumeration of all *.vb files below the given root,
        ''' skipping generated and version control folders.
        ''' </summary>
        Private Iterator Function EnumerateVbFiles(root As String) As IEnumerable(Of String)
            Dim stack As New Stack(Of String)()

            Call stack.Push(root)

            While stack.Count > 0
                Dim dir As String = stack.Pop()
                Dim files As String() = Nothing

                Try
                    files = IO.Directory.GetFiles(dir, "*.vb")
                Catch
                    files = New String() {}
                End Try

                For Each file As String In files
                    Yield file
                Next

                Dim dirs As String() = Nothing

                Try
                    dirs = IO.Directory.GetDirectories(dir)
                Catch
                    dirs = New String() {}
                End Try

                For Each subDir As String In dirs
                    Dim name As String = New IO.DirectoryInfo(subDir).Name

                    If name.StartsWith(".") Then
                        Continue For
                    End If

                    If excludeDirs.Contains(name, StringComparer.OrdinalIgnoreCase) Then
                        Continue For
                    End If

                    Call stack.Push(subDir)
                Next
            End While
        End Function

    End Module

End Namespace
