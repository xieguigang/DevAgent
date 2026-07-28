Imports System.IO
Imports System.Text.RegularExpressions
Imports VBLang.Reflection
Imports VBLang.Syntax

Public Class VBDocument

    ''' <summary>
    ''' relative path to the vbproj file
    ''' </summary>
    ''' <returns></returns>
    Public Property FileName As String
    ''' <summary>
    ''' namespace imports list
    ''' </summary>
    ''' <returns></returns>
    Public Property [Imports] As String()
    ''' <summary>
    ''' language symbols that parsed from current vb.net source file document text
    ''' </summary>
    ''' <returns></returns>
    Public Property Types As Dictionary(Of String, LanguageSymbolType)

End Class

Public Class [Imports]

    ''' <summary>
    ''' Imports XXX
    ''' </summary>
    ''' <returns></returns>
    Public Property [Namespace] As String
    ''' <summary>
    ''' Imports X = XXX
    ''' </summary>
    ''' <returns></returns>
    Public Property [Alias] As String

    Public Overrides Function ToString() As String
        If [Alias].StringEmpty(, True) Then
            Return $"Imports {[Namespace]}"
        Else
            Return $"Imports {[Alias]} = {[Namespace]}"
        End If
    End Function

End Class

Public Class VBProject

    Public Property RootNamespace As String
    Public Property AssemblyName As String
    Public Property OutputType As String
    Public Property CompileFiles As VBDocument()

    ''' <summary>
    ''' Get symbol via fullname
    ''' </summary>
    ''' <param name="fullName">namespace + type symbol name</param>
    ''' <returns></returns>
    Public Overloads Function [GetType](fullName As String) As LanguageSymbolType
        If String.IsNullOrWhiteSpace(fullName) OrElse CompileFiles Is Nothing Then
            Return Nothing
        End If

        Dim clean As String = StripGenerics(fullName).Trim()

        ' Candidate full names to try: the name as-is and, when a project
        ' RootNamespace is declared, the name with the RootNamespace prefix
        ' stripped or added. The real VB full name equals
        ' RootNamespace + namespace + type name.
        Dim candidates As New List(Of String) From {clean}
        If Not String.IsNullOrWhiteSpace(RootNamespace) Then
            Dim sep As String = RootNamespace & "."
            If clean.StartsWith(sep, StringComparison.OrdinalIgnoreCase) Then
                candidates.Add(clean.Substring(sep.Length))
            Else
                candidates.Add(sep & clean)
            End If
        End If

        For Each cand In candidates
            Dim segs = cand.Split(New Char() {"."c}, StringSplitOptions.RemoveEmptyEntries)
            If segs.Length = 0 Then Continue For
            For Each doc In CompileFiles
                If doc IsNot Nothing AndAlso doc.Types IsNot Nothing Then
                    Dim hit = FindInContainer(doc.Types, segs, 0)
                    If hit IsNot Nothing Then Return hit
                End If
            Next
        Next

        ' Fallback: match by the trailing type name anywhere in the tree.
        Dim lastName As String = clean.Split(New Char() {"."c}, StringSplitOptions.RemoveEmptyEntries).Last()
        For Each doc In CompileFiles
            If doc IsNot Nothing AndAlso doc.Types IsNot Nothing Then
                Dim hit = FindByLastName(doc.Types, lastName)
                If hit IsNot Nothing Then Return hit
            End If
        Next

        Return Nothing
    End Function

    ' Walk a container's symbol dictionary segment by segment (case
    ' insensitive, generics stripped) looking for the requested type.
    Private Shared Function FindInContainer(children As Dictionary(Of String, LanguageSymbolType), segs As String(), index As Integer) As LanguageSymbolType
        If children Is Nothing Then Return Nothing
        Dim key As String = CleanName(segs(index))
        Dim sym As LanguageSymbolType = Nothing
        For Each kv In children
            If String.Equals(CleanName(kv.Key), key, StringComparison.OrdinalIgnoreCase) Then
                sym = kv.Value
                Exit For
            End If
        Next
        If sym Is Nothing Then Return Nothing

        If index = segs.Length - 1 Then Return sym

        Dim ct = TryCast(sym, TypeContainerSymbol)
        If ct Is Nothing Then Return Nothing

        Dim deeper = FindInContainer(ct.InternalNested, segs, index + 1)
        If deeper IsNot Nothing Then Return deeper
        Return FindInContainer(ct.Members, segs, index + 1)
    End Function

    ' Depth-first search the whole tree for the first symbol whose simple
    ' name (generics stripped) equals lastName.
    Private Shared Function FindByLastName(children As Dictionary(Of String, LanguageSymbolType), lastName As String) As LanguageSymbolType
        If children Is Nothing Then Return Nothing
        For Each kv In children
            Dim sym = kv.Value
            If String.Equals(CleanName(sym.Name), lastName, StringComparison.OrdinalIgnoreCase) Then
                Return sym
            End If
            Dim ct = TryCast(sym, TypeContainerSymbol)
            If ct IsNot Nothing Then
                Dim deeper = FindByLastName(ct.InternalNested, lastName)
                If deeper IsNot Nothing Then Return deeper
                deeper = FindByLastName(ct.Members, lastName)
                If deeper IsNot Nothing Then Return deeper
            End If
        Next
        Return Nothing
    End Function

    Private Shared Function CleanName(name As String) As String
        If name Is Nothing Then Return ""
        Dim m As Match = Regex.Match(name, "\(Of[^)]*\)", RegexOptions.IgnoreCase)
        If m.Success Then name = name.Remove(m.Index, m.Length)
        Return name.Trim()
    End Function

    Private Shared Function StripGenerics(fullName As String) As String
        If fullName Is Nothing Then Return ""
        Return Regex.Replace(fullName, "\(Of[^)]*\)", "", RegexOptions.IgnoreCase).Trim()
    End Function

    ''' <summary>
    ''' Load a .NET assembly (dll) via reflection and map its symbols into a
    ''' virtual VBProject. The assembly metadata is read only (no execution).
    ''' </summary>
    ''' <param name="dllPath">path to the target dll file.</param>
    Public Shared Function LoadAssembly(dllPath As String) As VBProject
        Return AssemblySymbolLoader.LoadAssembly(dllPath)
    End Function

    ''' <summary>
    ''' Parse vbproj xml file and the vb source files
    ''' </summary>
    ''' <param name="vbproj"></param>
    ''' <returns></returns>
    Public Shared Function Load(vbproj As String) As VBProject
        Dim projDir As String = Path.GetDirectoryName(Path.GetFullPath(vbproj))

        Dim doc As XDocument = XDocument.Load(vbproj)
        Dim ns As XNamespace = If(doc.Root Is Nothing, "", doc.Root.Name.Namespace)

        Dim proj As New VBProject()
        proj.RootNamespace = ReadProperty(doc, ns, "RootNamespace")
        proj.AssemblyName = ReadProperty(doc, ns, "AssemblyName")
        proj.OutputType = ReadProperty(doc, ns, "OutputType")

        Dim files As String() = CollectCompileFiles(doc, ns, projDir)
        Dim docs As New List(Of VBDocument)

        For Each rel In files
            Dim full As String = Path.Combine(projDir, rel)

            If Not File.Exists(full) Then
                Continue For
            End If

            Dim code As String = Nothing
            Try
                code = File.ReadAllText(full)
            Catch
                Continue For
            End Try

            Dim vbdoc As New VBDocument()
            vbdoc.FileName = rel
            vbdoc.Imports = ExtractImports(code)

            Try
                Dim root As TypeContainerSymbol = VBParser.Parse(code)
                If root.InternalNested IsNot Nothing Then
                    vbdoc.Types = New Dictionary(Of String, LanguageSymbolType)(root.InternalNested)
                Else
                    vbdoc.Types = New Dictionary(Of String, LanguageSymbolType)()
                End If
            Catch
                vbdoc.Types = New Dictionary(Of String, LanguageSymbolType)()
            End Try

            docs.Add(vbdoc)
        Next

        proj.CompileFiles = docs.ToArray()
        Return proj
    End Function

    Private Shared Function ReadProperty(doc As XDocument, ns As XNamespace, name As String) As String
        If doc.Root Is Nothing Then Return ""
        For Each pg In doc.Root.Elements(ns + "PropertyGroup")
            Dim el = pg.Element(ns + name)
            If el IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(el.Value) Then
                Return el.Value.Trim()
            End If
        Next
        Return ""
    End Function

    Private Shared Function CollectCompileFiles(doc As XDocument, ns As XNamespace, projDir As String) As String()
        Dim includes As New List(Of String)
        Dim removes As New List(Of String)

        If doc.Root IsNot Nothing Then
            For Each ig In doc.Root.Elements(ns + "ItemGroup")
                For Each c In ig.Elements(ns + "Compile")
                    Dim inc = c.Attribute("Include")?.Value
                    If inc IsNot Nothing Then includes.Add(NormalizePath(inc))
                    Dim remAttr = c.Attribute("Remove")?.Value
                    If remAttr IsNot Nothing Then removes.Add(NormalizePath(remAttr))
                Next
            Next
        End If

        Dim defaultDisabled As Boolean = ReadProperty(doc, ns, "EnableDefaultCompileItems").Equals("false", StringComparison.OrdinalIgnoreCase)

        Dim result As New List(Of String)

        If includes.Count = 0 AndAlso Not defaultDisabled Then
            If Directory.Exists(projDir) Then
                Try
                    For Each f In Directory.GetFiles(projDir, "*.vb", SearchOption.AllDirectories)
                        Dim rel = GetRelativePath(projDir, f)
                        If Not IsExcludedByDefault(rel) Then
                            result.Add(rel)
                        End If
                    Next
                Catch
                End Try
            End If
        Else
            result.AddRange(includes)
        End If

        If removes.Count > 0 Then
            result.RemoveAll(Function(p) removes.Any(Function(r) GlobMatch(r, p)))
        End If

        Return result.ToArray()
    End Function

    Private Shared Function NormalizePath(p As String) As String
        Dim s = p.Trim()
        While s.StartsWith(".\") OrElse s.StartsWith("./")
            s = s.Substring(2)
        End While
        Return s.Replace("/", "\")
    End Function

    Private Shared Function GetRelativePath(baseDir As String, file As String) As String
        Dim b = Path.GetFullPath(baseDir).TrimEnd("\"c, "/"c) & "\"
        Dim f = Path.GetFullPath(file)
        Dim uriB = New Uri(b)
        Dim uriF = New Uri(f)
        Dim rel = Uri.UnescapeDataString(uriB.MakeRelativeUri(uriF).ToString())
        Return rel.Replace("/", "\")
    End Function

    Private Shared Function IsExcludedByDefault(rel As String) As Boolean
        Dim lower = rel.Replace("\", "/").ToLowerInvariant()
        Return lower.Contains("/obj/") OrElse lower.Contains("/bin/") OrElse lower.StartsWith("obj/") OrElse lower.StartsWith("bin/")
    End Function

    Private Shared Function GlobMatch(pattern As String, path As String) As Boolean
        Dim p = pattern.Replace("\", "/").ToLowerInvariant()
        Dim s = path.Replace("\", "/").ToLowerInvariant()
        Dim rx As String = "^"
        Dim i As Integer = 0
        While i < p.Length
            Dim c As Char = p(i)
            If c = "*"c Then
                If i + 1 < p.Length AndAlso p(i + 1) = "*"c Then
                    rx &= ".*"
                    i += 1
                    If i + 1 < p.Length AndAlso p(i + 1) = "/"c Then i += 1
                Else
                    rx &= "[^/]*"
                End If
            ElseIf c = "?"c Then
                rx &= "."
            Else
                rx &= Regex.Escape(c.ToString())
            End If
            i += 1
        End While
        rx &= "$"
        Return Regex.IsMatch(s, rx)
    End Function

    ' Extract Imports statements that VBParser.Parse silently ignores.
    Private Shared Function ExtractImports(source As String) As String()
        Dim scanner As New VBScanner()
        Dim stmts = scanner.Scan(source)
        Dim list As New List(Of String)

        For Each stmt In stmts
            If stmt.Tokens.Count = 0 Then Continue For
            If Not stmt.Tokens(0).Text.Equals("imports", StringComparison.OrdinalIgnoreCase) Then Continue For

            Dim rest As New List(Of Token)
            For k As Integer = 1 To stmt.Tokens.Count - 1
                rest.Add(stmt.Tokens(k))
            Next

            ' skip xml namespace imports : Imports <xmlns:...>
            If rest.Count > 0 AndAlso rest(0).Text = "<"c Then Continue For

            For Each seg In SplitImports(rest)
                Dim txt = String.Join("", seg.[Select](Function(t) t.Text).ToArray()).Trim()
                If txt.Length > 0 Then list.Add(txt)
            Next
        Next

        Return list.ToArray()
    End Function

    Private Shared Function SplitImports(tokens As List(Of Token)) As List(Of List(Of Token))
        Dim result As New List(Of List(Of Token))
        Dim cur As New List(Of Token)
        Dim depth As Integer = 0

        For Each t In tokens
            If t.Text = "("c Then
                depth += 1
                cur.Add(t)
            ElseIf t.Text = ")"c Then
                depth -= 1
                cur.Add(t)
            ElseIf t.Text = ","c AndAlso depth = 0 Then
                result.Add(cur)
                cur = New List(Of Token)()
            Else
                cur.Add(t)
            End If
        Next

        result.Add(cur)
        Return result
    End Function


End Class