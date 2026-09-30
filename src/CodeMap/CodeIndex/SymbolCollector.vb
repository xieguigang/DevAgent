Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM.Syntax

Namespace CodeIndex

    ''' <summary>
    ''' walk the parsed symbol tree of a vb.net document and project it into a
    ''' flat list of <see cref="CodeSymbol"/> entries, slicing the declaring
    ''' source text out of the physical file.
    ''' </summary>
    ''' <remarks>
    ''' The source text is read through a single entry file cache so that all
    ''' symbols of one document cost exactly one file read; the cache is
    ''' discarded as soon as the next document is processed, which keeps the
    ''' peak memory flat even for solutions with thousands of files.
    ''' </remarks>
    Public Class SymbolCollector

        ReadOnly maxCodeLines As Integer
        ReadOnly maxCodeChars As Integer

        Dim cachePath As String
        Dim cacheLines As String()

        Sub New(Optional maxCodeLines As Integer = 200, Optional maxCodeChars As Integer = 8000)
            Me.maxCodeLines = maxCodeLines
            Me.maxCodeChars = maxCodeChars
        End Sub

        ''' <summary>
        ''' true for the symbol kinds that should get their own index document;
        ''' namespaces are only structural (their text is covered by the nested
        ''' types) and variables/locals are already part of their owner code block.
        ''' </summary>
        Public Shared Function IsIndexable(type As SymbolType) As Boolean
            Select Case type
                Case SymbolType.Namespace, SymbolType.Variable
                    Return False
                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' flatten one source document into its symbol list.
        ''' </summary>
        Public Function Collect(file As SourceFile) As List(Of CodeSymbol)
            Dim out As New List(Of CodeSymbol)

            If file Is Nothing OrElse file.Types Is Nothing Then
                Return out
            End If

            Dim visited As New HashSet(Of LanguageSymbolType)()

            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In file.Types
                Call Walk(kv.Value, "", file, out, visited)
            Next

            Return out
        End Function

        Private Sub Walk(sym As LanguageSymbolType,
                         parentName As String,
                         file As SourceFile,
                         out As List(Of CodeSymbol),
                         visited As HashSet(Of LanguageSymbolType))

            If sym Is Nothing OrElse visited.Contains(sym) Then
                Return
            End If

            visited.Add(sym)

            Dim name As String = If(sym.Name, "").Trim()
            Dim fullName As String

            If name.Length = 0 Then
                fullName = parentName
            ElseIf parentName.Length = 0 Then
                fullName = name
            Else
                fullName = parentName & "." & name
            End If

            If IsIndexable(sym.Type) Then
                Dim entry As CodeSymbol = Project(sym, fullName, file)

                If entry IsNot Nothing Then
                    Call out.Add(entry)
                End If
            End If

            Dim container As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

            If container IsNot Nothing Then
                If container.InternalNested IsNot Nothing Then
                    For Each kv In container.InternalNested
                        Call Walk(kv.Value, fullName, file, out, visited)
                    Next
                End If

                If container.Members IsNot Nothing Then
                    For Each kv In container.Members
                        Call Walk(kv.Value, fullName, file, out, visited)
                    Next
                End If
            End If

            ' local variables of a callable member are intentionally skipped:
            ' their identifiers are already contained in the member code block.
        End Sub

        ''' <summary>
        ''' build a <see cref="CodeSymbol"/> from a parsed symbol; returns nothing
        ''' when the symbol carries no usable source location.
        ''' </summary>
        Public Function Project(sym As LanguageSymbolType, fullName As String, file As SourceFile) As CodeSymbol
            If sym Is Nothing OrElse sym.Source Is Nothing Then
                Return Nothing
            End If

            Dim loc As Source = sym.Source.First

            If loc Is Nothing Then
                Return Nothing
            End If

            Dim path As String = If(String.IsNullOrEmpty(loc.FilePath), file.AbsolutePath, loc.FilePath)
            Dim startLine As Integer = 0
            Dim endLine As Integer = 0

            If loc.LineRange IsNot Nothing Then
                startLine = loc.LineRange.Min
                endLine = loc.LineRange.Max
            End If

            If startLine <= 0 Then
                startLine = If(loc.DeclarationLine > 0, loc.DeclarationLine, 1)
            End If

            If endLine <= 0 Then
                endLine = startLine
            End If

            Dim code As String = Slice(path, startLine, endLine)

            Return New CodeSymbol With {
                .Name = If(sym.Name, ""),
                .FullName = If(fullName, ""),
                .Kind = sym.Type.ToString(),
                .Modifiers = If(sym.Modifiers, ""),
                .Project = If(file.Project, ""),
                .File = path,
                .RelativeFile = If(file.RelativePath, path),
                .StartLine = startLine,
                .EndLine = endLine,
                .DeclarationLine = If(loc.DeclarationLine > 0, loc.DeclarationLine, startLine),
                .XmlDoc = If(sym.XmlDoc, ""),
                .Code = code
            }
        End Function

        ''' <summary>
        ''' read the source text of the given 1-based line range out of the file.
        ''' </summary>
        Public Function Slice(path As String, startLine As Integer, endLine As Integer) As String
            If String.IsNullOrEmpty(path) OrElse Not IO.File.Exists(path) Then
                Return ""
            End If

            Dim lines As String() = GetLines(path)

            If lines Is Nothing Then
                Return ""
            End If

            If startLine <= 0 Then
                startLine = 1
            End If

            If endLine <= 0 OrElse endLine > lines.Length Then
                endLine = lines.Length
            End If

            If maxCodeLines > 0 AndAlso endLine - startLine + 1 > maxCodeLines Then
                endLine = startLine + maxCodeLines - 1
            End If

            If startLine > lines.Length Then
                Return ""
            End If

            Dim sb As New StringBuilder()

            For i As Integer = startLine - 1 To endLine - 1
                Call sb.AppendLine(lines(i))
            Next

            Dim text As String = sb.ToString()

            If maxCodeChars > 0 AndAlso text.Length > maxCodeChars Then
                text = text.Substring(0, maxCodeChars)
            End If

            Return text
        End Function

        ''' <summary>
        ''' a one slot file cache: the current document is kept in memory while
        ''' its symbols are projected, then replaced by the next document.
        ''' </summary>
        Private Function GetLines(path As String) As String()
            If cacheLines IsNot Nothing AndAlso String.Equals(cachePath, path, StringComparison.OrdinalIgnoreCase) Then
                Return cacheLines
            End If

            Try
                cacheLines = IO.File.ReadAllLines(path)
                cachePath = path
            Catch
                cacheLines = Nothing
                cachePath = Nothing
            End Try

            Return cacheLines
        End Function

        ''' <summary>
        ''' backfill the absolute file path into every source location of the
        ''' symbol tree. Required in folder mode, where the parser only records
        ''' line ranges and no file path at all.
        ''' </summary>
        Public Shared Sub FillSourceFilePath(sym As LanguageSymbolType, filePath As String)
            Call FillSourceFilePath(sym, filePath, New HashSet(Of LanguageSymbolType)())
        End Sub

        Private Shared Sub FillSourceFilePath(sym As LanguageSymbolType,
                                              filePath As String,
                                              visited As HashSet(Of LanguageSymbolType))

            If sym Is Nothing OrElse visited.Contains(sym) Then
                Return
            End If

            visited.Add(sym)

            If sym.Source IsNot Nothing Then
                For Each loc As Source In sym.Source.ToArray()
                    If String.IsNullOrEmpty(loc.FilePath) Then
                        loc.FilePath = filePath
                    End If
                Next
            End If

            Dim container As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

            If container IsNot Nothing Then
                If container.InternalNested IsNot Nothing Then
                    For Each kv In container.InternalNested
                        Call FillSourceFilePath(kv.Value, filePath, visited)
                    Next
                End If

                If container.Members IsNot Nothing Then
                    For Each kv In container.Members
                        Call FillSourceFilePath(kv.Value, filePath, visited)
                    Next
                End If
            End If

            Dim callable As CallableMemberSymbol = TryCast(sym, CallableMemberSymbol)

            If callable IsNot Nothing AndAlso callable.Locals IsNot Nothing Then
                For Each kv In callable.Locals
                    Call FillSourceFilePath(kv.Value, filePath, visited)
                Next
            End If
        End Sub

    End Class

End Namespace
