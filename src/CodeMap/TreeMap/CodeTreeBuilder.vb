Imports System.IO
Imports CodeMap.CodeIndex
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM
Imports std = System.Math

Namespace TreeMap

    ''' <summary>
    ''' project the flat source file list of an opened workspace into the five
    ''' level code map hierarchy: vbproj -&gt; folder -&gt; source file -&gt; type
    ''' -&gt; function/sub/property.
    ''' </summary>
    Public Module CodeTreeBuilder

        ''' <summary>
        ''' build the project level nodes of the hierarchy; every node already
        ''' carries its aggregated measures.
        ''' </summary>
        Public Function Build(ws As WorkspaceInfo) As List(Of CodeNode)
            Dim projects As New List(Of CodeNode)()
            Dim projectIndex As New Dictionary(Of String, CodeNode)(StringComparer.OrdinalIgnoreCase)
            Dim collector As New SymbolCollector()

            If ws Is Nothing OrElse ws.Files Is Nothing Then
                Return projects
            End If

            For Each file As SourceFile In ws.Files
                Dim proj As CodeNode = GetProject(projects, projectIndex, If(file.Project, "(unknown)"))
                Dim folder As CodeNode = GetFolder(proj, file.RelativePath)
                Dim fileNode As New CodeNode With {
                    .Name = IO.Path.GetFileName(If(file.RelativePath, file.AbsolutePath)),
                    .Kind = CodeNodeKind.File,
                    .FullName = If(file.RelativePath, file.AbsolutePath),
                    .Path = If(file.RelativePath, file.AbsolutePath),
                    .SourceFile = file.AbsolutePath,
                    .Parent = folder,
                    .Depth = folder.Depth + 1,
                    .Lines = 0,
                    .Chars = 0,
                    .SymbolCount = 0
                }

                Call folder.Children.Add(fileNode)

                Try
                    Call AddSymbols(fileNode, file.Types, file, collector)
                Catch
                    ' a single broken document must not abort the whole tree
                End Try

                ' a document without any parsed symbol still has to be visible,
                ' so the raw file size is used as its measure
                If fileNode.Children.Count = 0 Then
                    Call FallbackFileMetrics(fileNode, file.AbsolutePath)
                End If
            Next

            Call Aggregate(projects)

            Return projects
        End Function

        ''' <summary>the position of one hierarchy level inside the five level chain</summary>
        Public Function KindOrder(k As CodeNodeKind) As Integer
            Select Case k
                Case CodeNodeKind.Project
                    Return 0
                Case CodeNodeKind.Folder
                    Return 1
                Case CodeNodeKind.File
                    Return 2
                Case CodeNodeKind.Type
                    Return 3
                Case Else
                    Return 4
            End Select
        End Function

        ''' <summary>the hierarchy level of the given position; out of range clamps to member</summary>
        Public Function KindOfIndex(i As Integer) As CodeNodeKind
            If i <= 0 Then
                Return CodeNodeKind.Project
            End If

            If i = 1 Then
                Return CodeNodeKind.Folder
            End If

            If i = 2 Then
                Return CodeNodeKind.File
            End If

            If i = 3 Then
                Return CodeNodeKind.Type
            End If

            Return CodeNodeKind.Member
        End Function

        ''' <summary>
        ''' replace the measures of every container by the sum of its children.
        ''' </summary>
        Public Sub Aggregate(nodes As List(Of CodeNode))
            If nodes Is Nothing Then
                Return
            End If

            For Each n As CodeNode In nodes
                Call AggregateNode(n)
            Next
        End Sub

        Private Sub AggregateNode(n As CodeNode)
            If n Is Nothing OrElse n.IsLeaf Then
                Return
            End If

            Dim lines As Integer = 0
            Dim chars As Integer = 0
            Dim symbols As Integer = 0

            For Each child As CodeNode In n.Children
                Call AggregateNode(child)

                lines += child.Lines
                chars += child.Chars
                symbols += child.SymbolCount
            Next

            n.Lines = If(lines > 0, lines, 1)
            n.Chars = If(chars > 0, chars, 1)
            n.SymbolCount = symbols
        End Sub

        Private Function GetProject(projects As List(Of CodeNode),
                                    index As Dictionary(Of String, CodeNode),
                                    name As String) As CodeNode

            Dim hit As CodeNode = Nothing

            If index.TryGetValue(name, hit) Then
                Return hit
            End If

            Dim node As New CodeNode With {
                .Name = name,
                .FullName = name,
                .Kind = CodeNodeKind.Project,
                .Path = name,
                .Depth = 0
            }

            Call projects.Add(node)
            index(name) = node

            Return node
        End Function

        ''' <summary>
        ''' walk down (and create on demand) the folder chain of a source file.
        ''' </summary>
        Private Function GetFolder(proj As CodeNode, relPath As String) As CodeNode
            Dim dir As String = Nothing

            Try
                dir = IO.Path.GetDirectoryName(relPath)
            Catch
                dir = Nothing
            End Try

            If String.IsNullOrEmpty(dir) Then
                Return proj
            End If

            Dim segments As String() = dir.Split(New Char() {"\"c, "/"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim cur As CodeNode = proj

            For Each seg As String In segments
                cur = GetChild(cur, seg, CodeNodeKind.Folder)
            Next

            Return cur
        End Function

        Private Function GetChild(parent As CodeNode, name As String, kind As CodeNodeKind) As CodeNode
            For Each c As CodeNode In parent.Children
                If c.Kind = kind AndAlso String.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) Then
                    Return c
                End If
            Next

            Dim nodePath As String = CombinePath(parent.Path, name)
            Dim node As New CodeNode With {
                .Name = name,
                .Kind = kind,
                .Path = nodePath,
                .FullName = nodePath,
                .Parent = parent,
                .Depth = parent.Depth + 1
            }

            Call parent.Children.Add(node)

            Return node
        End Function

        ''' <summary>
        ''' project the parsed symbol tree of one document into the type and the
        ''' member level of the hierarchy.
        ''' </summary>
        Private Sub AddSymbols(fileNode As CodeNode,
                               types As Dictionary(Of String, LanguageSymbolType),
                               file As SourceFile,
                               collector As SymbolCollector)

            If types Is Nothing Then
                Return
            End If

            Dim visited As New HashSet(Of LanguageSymbolType)()

            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In types
                Call AddSymbol(fileNode, kv.Value, "", file, collector, visited)
            Next
        End Sub

        Private Sub AddSymbol(parent As CodeNode,
                              sym As LanguageSymbolType,
                              prefix As String,
                              file As SourceFile,
                              collector As SymbolCollector,
                              visited As HashSet(Of LanguageSymbolType))

            If sym Is Nothing OrElse visited.Contains(sym) Then
                Return
            End If

            visited.Add(sym)

            If sym.Type = SymbolType.Variable Then
                Return
            End If

            If sym.Type = SymbolType.Namespace Then
                ' a namespace is structure only: its types are attached to the
                ' very same parent instead of adding a level of their own
                Dim ns As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

                If ns IsNot Nothing AndAlso ns.InternalNested IsNot Nothing Then
                    For Each kv As KeyValuePair(Of String, LanguageSymbolType) In ns.InternalNested
                        Call AddSymbol(parent, kv.Value, prefix, file, collector, visited)
                    Next
                End If

                Return
            End If

            Dim name As String = If(sym.Name, "").Trim()

            If name.Length = 0 Then
                name = sym.Type.ToString()
            End If

            Dim fullName As String = CombineName(prefix, name)
            Dim node As New CodeNode With {
                .Name = name,
                .FullName = fullName,
                .Kind = If(IsMember(sym.Type), CodeNodeKind.Member, CodeNodeKind.Type),
                .Parent = parent,
                .Depth = parent.Depth + 1,
                .Path = file.RelativePath
            }

            Dim cs As CodeSymbol = collector.Project(sym, fullName, file)

            If cs IsNot Nothing Then
                node.Symbol = cs
                node.Path = cs.File
                node.Lines = std.Max(1, cs.EndLine - cs.StartLine + 1)
                node.Chars = If(cs.Code, "").Length
            Else
                node.Lines = 1
                node.Chars = name.Length
            End If

            node.SymbolCount = 1

            Call parent.Children.Add(node)

            Dim container As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

            If container IsNot Nothing Then
                If container.InternalNested IsNot Nothing Then
                    For Each kv As KeyValuePair(Of String, LanguageSymbolType) In container.InternalNested
                        Call AddSymbol(node, kv.Value, fullName, file, collector, visited)
                    Next
                End If

                If container.Members IsNot Nothing Then
                    For Each kv As KeyValuePair(Of String, LanguageSymbolType) In container.Members
                        Call AddSymbol(node, kv.Value, fullName, file, collector, visited)
                    Next
                End If
            End If
        End Sub

        Private Function IsMember(kind As SymbolType) As Boolean
            Select Case kind
                Case SymbolType.Function, SymbolType.Sub, SymbolType.Property,
                     SymbolType.Event, SymbolType.Delegate, SymbolType.New, SymbolType.Operator
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Private Sub FallbackFileMetrics(fileNode As CodeNode, path As String)
            Try
                If String.IsNullOrEmpty(path) OrElse Not IO.File.Exists(path) Then
                    Return
                End If

                Dim text As String = IO.File.ReadAllText(path)

                fileNode.Chars = text.Length
                fileNode.Lines = std.Max(1, text.Split(New String() {vbLf}, StringSplitOptions.None).Length)
            Catch
                fileNode.Chars = 1
                fileNode.Lines = 1
            End Try
        End Sub

        Private Function CombineName(prefix As String, name As String) As String
            If String.IsNullOrEmpty(prefix) Then
                Return name
            End If

            If String.IsNullOrEmpty(name) Then
                Return prefix
            End If

            Return prefix & "." & name
        End Function

        Private Function CombinePath(prefix As String, name As String) As String
            If String.IsNullOrEmpty(prefix) Then
                Return name
            End If

            Return prefix & "\" & name
        End Function

    End Module

End Namespace
