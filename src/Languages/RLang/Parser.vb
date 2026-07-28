Imports System.Collections.Generic
Imports System.IO
Imports VBLang
Imports RLang.Syntax

''' <summary>
''' R language document / project parser.
'''
''' Mirrors the role of <c>VBLang.Syntax.VBParser</c> + <c>VBLang.VBProject.Load</c>
''' but for R code:
''' <list type="bullet">
'''   <item><see cref="ParseDocument"/> turns a single R script into a virtual
'''   <see cref="VBDocument"/>: the top-level <c>require</c>/<c>library</c> calls become
'''   <see cref="VBDocument.Imports"/>, while the functions and variables become the symbol
'''   list stored in <see cref="VBDocument.Types"/>.</item>
'''   <item><see cref="ParseRproj"/> turns a folder of R scripts (an R package's <c>R/</c>
'''   directory) into a virtual <see cref="VBProject"/> whose <c>CompileFiles</c> are the
'''   parsed documents, and whose <c>DESCRIPTION</c> file is mapped onto the project's
'''   metadata (Metadata / NuGet) and dependency list (PackageReferences).</item>
''' </list>
''' </summary>
Public Module Parser

    ' ------------------------------------------------------------------
    '  single document
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Parse a single R script into a virtual <see cref="VBDocument"/>.
    ''' Top-level <c>require</c>/<c>library</c> calls are mapped to imports and
    ''' functions / variables are stored as symbols.
    ''' </summary>
    Public Function ParseDocument(rscript As String) As VBDocument
        Dim res = RParser.Parse(rscript)
        Dim doc As New VBDocument()
        doc.FileName = ""
        doc.Imports = res.ImportsList.ToArray()
        doc.Types = New Dictionary(Of String, LanguageSymbolType)()

        For Each fn In res.Functions
            If Not doc.Types.ContainsKey(fn.Name) Then
                doc.Types(fn.Name) = fn
            End If
        Next

        For Each v In res.Variables
            If Not doc.Types.ContainsKey(v.Name) Then
                doc.Types(v.Name) = v
            End If
        Next

        Return doc
    End Function

    ''' <summary>
    ''' Get the list of function symbols from a parsed R document.
    ''' </summary>
    Public Function GetFunctions(doc As VBDocument) As List(Of RFunctionSymbol)
        Dim list As New List(Of RFunctionSymbol)()
        If doc.Types IsNot Nothing Then
            For Each kv In doc.Types
                If TypeOf kv.Value Is RFunctionSymbol Then
                    list.Add(DirectCast(kv.Value, RFunctionSymbol))
                End If
            Next
        End If
        Return list
    End Function

    ''' <summary>
    ''' Get the list of variable symbols from a parsed R document.
    ''' </summary>
    Public Function GetVariables(doc As VBDocument) As List(Of VariableSymbol)
        Dim list As New List(Of VariableSymbol)()
        If doc.Types IsNot Nothing Then
            For Each kv In doc.Types
                If TypeOf kv.Value Is VariableSymbol AndAlso Not TypeOf kv.Value Is RFunctionSymbol Then
                    list.Add(DirectCast(kv.Value, VariableSymbol))
                End If
            Next
        End If
        Return list
    End Function

    ' ------------------------------------------------------------------
    '  project (R package)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Parse an R package project. <paramref name="R"/> is the path of the package's
    ''' <c>R/</c> folder. Every <c>*.R</c> file under it is parsed into a
    ''' <see cref="VBDocument"/> (stored as <see cref="VBProject.CompileFiles"/>) and the
    ''' package <c>DESCRIPTION</c> file is mapped onto the virtual project's metadata.
    ''' </summary>
    Public Function ParseRproj(R As String) As VBProject
        Dim proj As New VBProject()
        proj.Sdk = "R.Package"
        proj.OutputType = "Library"

        Dim descPath = FindDescription(R)
        If Not String.IsNullOrEmpty(descPath) AndAlso File.Exists(descPath) Then
            ParseDescription(descPath, proj)
        Else
            proj.Metadata = New VBProjectMetadata()
            proj.NuGet = New VBNuGetMetadata()
        End If

        Dim docs As New List(Of VBDocument)()
        If Directory.Exists(R) Then
            For Each f In Directory.GetFiles(R, "*.R", SearchOption.AllDirectories)
                Try
                    Dim code = File.ReadAllText(f)
                    Dim doc = ParseDocument(code)
                    doc.FileName = GetRelativePath(R, f)
                    docs.Add(doc)
                Catch
                    ' skip files that cannot be read
                End Try
            Next
        End If

        proj.CompileFiles = docs.ToArray()
        Return proj
    End Function

    ''' <summary>
    ''' Locate the package <c>DESCRIPTION</c> file. It normally lives in the parent
    ''' directory of the <c>R/</c> folder; if not found there we walk up the tree.
    ''' </summary>
    Private Function FindDescription(R As String) As String
        Dim dir = Path.GetFullPath(R)

        Dim cand = Path.Combine(dir, "DESCRIPTION")
        If File.Exists(cand) Then
            Return cand
        End If

        Dim d = dir
        While d IsNot Nothing AndAlso d.Length > 0
            cand = Path.Combine(d, "DESCRIPTION")
            If File.Exists(cand) Then
                Return cand
            End If
            d = Path.GetDirectoryName(d)
        End While

        Return ""
    End Function

    ''' <summary>
    ''' Parse an R package <c>DESCRIPTION</c> file and map its key/value metadata onto a
    ''' virtual <see cref="VBProject"/>.
    ''' </summary>
    Private Sub ParseDescription(path As String, proj As VBProject)
        proj.Metadata = New VBProjectMetadata()
        proj.Metadata.Other = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        proj.NuGet = New VBNuGetMetadata()
        proj.NuGet.Other = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        Dim lines = File.ReadAllLines(path)
        Dim map As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim currentKey As String = ""
        Dim currentVal As String = ""

        For Each raw In lines
            If String.IsNullOrWhiteSpace(raw) Then
                Continue For
            End If

            Dim isContinuation = Char.IsWhiteSpace(raw(0)) AndAlso currentKey <> ""

            If isContinuation Then
                currentVal &= " " & raw.Trim()
            Else
                If currentKey <> "" Then
                    map(currentKey) = currentVal.Trim()
                End If
                Dim colon = raw.IndexOf(":"c)
                If colon < 0 Then
                    currentKey = ""
                    currentVal = ""
                    Continue For
                End If
                currentKey = raw.Substring(0, colon).Trim()
                currentVal = raw.Substring(colon + 1).Trim()
            End If
        Next

        If currentKey <> "" Then
            map(currentKey) = currentVal.Trim()
        End If

        Dim getField = Function(k As String) As String
                           Dim v As String = Nothing
                           If map.TryGetValue(k, v) Then
                               Return v
                           End If
                           Return ""
                       End Function

        Dim pkg = getField("Package")
        proj.RootNamespace = pkg
        proj.AssemblyName = pkg
        proj.NuGet.PackageId = pkg

        proj.NuGet.Version = getField("Version")
        proj.Metadata.Other("Title") = getField("Title")
        proj.Metadata.Other("Type") = getField("Type")
        proj.Metadata.Other("Encoding") = getField("Encoding")
        proj.Metadata.Other("RoxygenNote") = getField("RoxygenNote")
        proj.Metadata.Other("URL") = getField("URL")
        proj.Metadata.Other("BugReports") = getField("BugReports")
        proj.Metadata.Other("Language") = getField("Language")

        proj.NuGet.Authors = getField("Author")
        proj.NuGet.Description = getField("Description")
        proj.NuGet.Other("License") = getField("License")
        proj.NuGet.Other("Maintainer") = getField("Maintainer")
        proj.NuGet.Other("Copyright") = getField("Copyright")

        Dim deps As New List(Of VBPackageReference)()
        AddDeps(deps, getField("Depends"))
        AddDeps(deps, getField("Imports"))
        AddDeps(deps, getField("Suggests"))
        proj.PackageReferences = deps.ToArray()
    End Sub

    ''' <summary>
    ''' Turn a comma separated dependency field (possibly spanning several indented lines)
    ''' into <see cref="VBPackageReference"/> entries, stripping version constraints and
    ''' ignoring the implicit <c>R</c> dependency.
    ''' </summary>
    Private Sub AddDeps(list As List(Of VBPackageReference), field As String)
        If String.IsNullOrWhiteSpace(field) Then
            Return
        End If

        Dim parts = field.Split(","c)
        For Each p In parts
            Dim s = p.Trim()
            If s.Length = 0 Then
                Continue For
            End If

            Dim name = s
            Dim version = ""
            Dim ob = s.IndexOf("("c)
            If ob >= 0 Then
                name = s.Substring(0, ob).Trim()
                Dim cb = s.IndexOf(")"c)
                If cb > ob Then
                    version = s.Substring(ob + 1, cb - ob - 1).Trim()
                End If
            End If
            name = name.Trim()

            If name.Length = 0 OrElse name.Equals("R", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            If list.Exists(Function(x) x.Id.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                Continue For
            End If

            list.Add(New VBPackageReference() With {.Id = name, .Version = version})
        Next
    End Sub

    Private Function GetRelativePath(baseDir As String, file As String) As String
        Dim base = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        Dim full = Path.GetFullPath(file)
        If full.StartsWith(base & Path.DirectorySeparatorChar) Then
            Return full.Substring(base.Length + 1)
        End If
        Return Path.GetFileName(file)
    End Function

End Module
