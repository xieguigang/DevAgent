Imports System.Globalization
Imports System.Text
Imports System.Text.Json.Nodes
Imports CodeMap.CodeIndex
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM
Imports Microsoft.VisualBasic.Scripting.MetaData

''' <summary>
''' the result of one tool invocation.
''' </summary>
''' <remarks>
''' A tool never throws: every failure comes back as an outcome so that the rest
''' endpoint can map it onto an http status code and the mcp endpoint onto a
''' json-rpc error object.
''' </remarks>
Public Class ToolOutcome

    Public Property Ok As Boolean
    ''' <summary>the payload that is serialised into the response</summary>
    Public Property Payload As Object
    Public Property ErrorMessage As String
    ''' <summary>true when the index of the requested workspace is still building</summary>
    Public Property NotReady As Boolean

    Public Shared Function Success(payload As Object) As ToolOutcome
        Return New ToolOutcome With {.Ok = True, .Payload = payload}
    End Function

    Public Shared Function Fail(message As String) As ToolOutcome
        Return New ToolOutcome With {.Ok = False, .ErrorMessage = message}
    End Function

    Public Shared Function Busy(status As StatusResponse) As ToolOutcome
        Return New ToolOutcome With {
            .Ok = False,
            .NotReady = True,
            .Payload = status,
            .ErrorMessage = $"the index of workspace '{status.workspaceId}' is not ready yet ({status.phase})."
        }
    End Function

End Class

''' <summary>
''' one callable capability of the skill: the name, the description, the json
''' schema of its arguments and the executor.
''' </summary>
Public Class ToolDescriptor

    Public Property Name As String
    Public Property Description As String
    Public Property InputSchema As JsonObject
    Public Property Invoke As Func(Of JsonObject, ToolOutcome)

    ''' <summary>the <c>tools/list</c> entry of this tool.</summary>
    Public Function ToJson() As JsonObject
        Dim o As New JsonObject()

        o("name") = Name
        o("description") = Description
        o("inputSchema") = InputSchema

        Return o
    End Function

End Class

''' <summary>
''' the single source of truth of everything the skill can do.
''' </summary>
''' <remarks>
''' The rest endpoints, the mcp <c>tools/list</c> answer and <c>/tools.json</c>
''' are all rendered from this registry, and both protocols call the very same
''' executor, so a capability can never behave differently depending on the
''' protocol that was used to reach it.
''' </remarks>
Public Class SkillTools

    ReadOnly pool As WorkspacePool
    ReadOnly maxCodeChars As Integer
    ReadOnly defaultTop As Integer
    ReadOnly defaultThreshold As Double
    ReadOnly tools As New List(Of ToolDescriptor)()

    Sub New(pool As WorkspacePool,
            Optional maxCodeChars As Integer = 1200,
            Optional defaultTop As Integer = 20,
            Optional defaultThreshold As Double = 0.15)

        Me.pool = pool
        Me.maxCodeChars = maxCodeChars
        Me.defaultTop = If(defaultTop <= 0, 20, defaultTop)
        Me.defaultThreshold = If(defaultThreshold < 0, 0.15, defaultThreshold)

        Call Register("index_status",
                      "Report the build progress of a workspace index: ready flag, phase, indexed files and symbol count. Call this first after open_workspace and poll it until ready is true.",
                      Schema("{""type"":""object"",""properties"":{""workspaceId"":{""type"":""string"",""description"":""the workspace handle, e.g. ws1; empty means the default workspace""}}}"),
                      AddressOf Tool_IndexStatus)

        Call Register("list_workspaces",
                      "List every workspace that is currently open in the service, together with its handle, path and index state.",
                      Schema("{""type"":""object"",""properties"":{}}"),
                      AddressOf Tool_ListWorkspaces)

        Call Register("open_workspace",
                      "Open a vb.net code repository for indexing: a .sln, a .slnx, a .vbproj or a plain source folder. Returns a workspace handle immediately; the index is built in the background, poll index_status until it is ready.",
                      Schema("{""type"":""object"",""properties"":{""path"":{""type"":""string"",""description"":""absolute or relative path of the solution, project or folder""}},""required"":[""path""]}"),
                      AddressOf Tool_OpenWorkspace)

        Call Register("search_symbols",
                      "Fuzzy keyword search over the indexed vb.net symbols (types, methods, properties, fields). Matches names, full names, xml docs and the source text; use it to find where something is declared when you only know part of the name.",
                      Schema("{""type"":""object"",""properties"":{""query"":{""type"":""string"",""description"":""the keyword to look for""},""workspaceId"":{""type"":""string"",""description"":""the workspace handle, empty means the default workspace""},""top"":{""type"":""integer"",""description"":""max number of hits, default 20""},""threshold"":{""type"":""number"",""description"":""minimum q-gram similarity of a matched word, default 0.15""}},""required"":[""query""]}"),
                      AddressOf Tool_SearchSymbols)

        Call Register("find_symbol",
                      "Resolve a symbol by its namespace qualified full name (exact match, then suffix match, then simple name) and return its declaration site and source text.",
                      Schema("{""type"":""object"",""properties"":{""fullName"":{""type"":""string"",""description"":""e.g. CodeIndex.CodeMapIndex or CodeMapIndex.Query""},""workspaceId"":{""type"":""string"",""description"":""the workspace handle, empty means the default workspace""}},""required"":[""fullName""]}"),
                      AddressOf Tool_FindSymbol)

        Call Register("list_members",
                      "List every member (method, property, field, event, delegate, nested type) of a class, module, structure, interface or enum, with its signature and declaration line.",
                      Schema("{""type"":""object"",""properties"":{""fullName"":{""type"":""string"",""description"":""the namespace qualified type name, e.g. CodeIndex.CodeMapIndex""},""workspaceId"":{""type"":""string"",""description"":""the workspace handle, empty means the default workspace""}},""required"":[""fullName""]}"),
                      AddressOf Tool_ListMembers)

        Call Register("list_files",
                      "List the vb.net source files of a workspace, optionally filtered by project name and by a wildcard pattern on the relative path.",
                      Schema("{""type"":""object"",""properties"":{""project"":{""type"":""string"",""description"":""optional project name filter""},""pattern"":{""type"":""string"",""description"":""optional wildcard filter on the relative path, e.g. *Index*.vb""},""workspaceId"":{""type"":""string"",""description"":""the workspace handle, empty means the default workspace""}}}"),
                      AddressOf Tool_ListFiles)

        Call Register("get_source",
                      "Read raw vb.net source text: either a file plus a line range, or the declaration block of a symbol given by its full name.",
                      Schema("{""type"":""object"",""properties"":{""file"":{""type"":""string"",""description"":""a workspace relative or absolute file path""},""symbol"":{""type"":""string"",""description"":""alternative to file: the full name of a symbol whose block should be returned""},""startLine"":{""type"":""integer"",""description"":""1-based first line, default 1""},""endLine"":{""type"":""integer"",""description"":""1-based last line, default end of file""},""maxLines"":{""type"":""integer"",""description"":""max number of lines to return, default 200, hard limit 5000""},""workspaceId"":{""type"":""string"",""description"":""the workspace handle, empty means the default workspace""}}}"),
                      AddressOf Tool_GetSource)
    End Sub

    Private Sub Register(name As String,
                         description As String,
                         schema As JsonObject,
                         invoke As Func(Of JsonObject, ToolOutcome))

        Call tools.Add(New ToolDescriptor With {
            .Name = name,
            .Description = description,
            .InputSchema = schema,
            .Invoke = invoke
        })
    End Sub

    Public ReadOnly Property All As ToolDescriptor()
        Get
            Return tools.ToArray()
        End Get
    End Property

    Public Function Find(name As String) As ToolDescriptor
        If String.IsNullOrWhiteSpace(name) Then
            Return Nothing
        End If

        For Each t As ToolDescriptor In tools
            If String.Equals(t.Name, name.Trim(), StringComparison.OrdinalIgnoreCase) Then
                Return t
            End If
        Next

        Return Nothing
    End Function

    ''' <summary>run one tool by name; an unknown name is a failure, not an exception.</summary>
    Public Function CallTool(name As String, args As JsonObject) As ToolOutcome
        Dim tool As ToolDescriptor = Find(name)

        If tool Is Nothing Then
            Return ToolOutcome.Fail($"unknown tool '{name}'. call the list_workspaces/index_status tools or GET /tools.json to see what is available.")
        End If

        Try
            Return tool.Invoke(If(args, New JsonObject()))
        Catch ex As Exception
            Return ToolOutcome.Fail($"{name} failed: {ex.Message}")
        End Try
    End Function

    ' ========================================================================
    ' the executors
    ' ========================================================================

    Private Function Tool_IndexStatus(args As JsonObject) As ToolOutcome
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        Return ToolOutcome.Success(WorkspacePool.StatusOf(entry))
    End Function

    Private Function Tool_ListWorkspaces(args As JsonObject) As ToolOutcome
        Dim views As New List(Of WorkspaceEntryView)()

        For Each e As WorkspaceEntry In pool.List()
            Dim st As StatusResponse = WorkspacePool.StatusOf(e)

            Call views.Add(New WorkspaceEntryView With {
                .id = e.Id,
                .path = e.Path,
                .isDefault = String.Equals(e.Id, pool.DefaultId, StringComparison.OrdinalIgnoreCase),
                .name = If(st.workspace Is Nothing, "", st.workspace.name),
                .kind = If(st.workspace Is Nothing, "", st.workspace.kind),
                .projectCount = If(st.workspace Is Nothing, 0, st.workspace.projectCount),
                .fileCount = If(st.workspace Is Nothing, 0, st.workspace.fileCount),
                .ready = st.ready,
                .phase = st.phase,
                .symbols = st.symbols,
                .openedAt = e.OpenedAt.ToString("o"),
                .lastAccessAt = e.LastAccess.ToString("o")
            })
        Next

        Return ToolOutcome.Success(New WorkspacesResponse With {
            .defaultId = pool.DefaultId,
            .capacity = pool.Capacity,
            .count = views.Count,
            .workspaces = views.ToArray()
        })
    End Function

    Private Function Tool_OpenWorkspace(args As JsonObject) As ToolOutcome
        Dim wsPath As String = Str(args, "path")

        If String.IsNullOrWhiteSpace(wsPath) Then
            Return ToolOutcome.Fail("the 'path' argument is required.")
        End If

        Try
            Dim entry As WorkspaceEntry = pool.Open(wsPath)

            Return ToolOutcome.Success(New OpenWorkspaceResponse With {
                .workspaceId = entry.Id,
                .path = entry.Path,
                .name = IO.Path.GetFileName(entry.Path.TrimEnd("\"c, "/"c)),
                .isNew = entry.IsNew,
                .status = WorkspacePool.StatusOf(entry)
            })
        Catch ex As Exception
            Return ToolOutcome.Fail($"cannot open workspace '{wsPath}': {ex.Message}")
        End Try
    End Function

    Private Function Tool_SearchSymbols(args As JsonObject) As ToolOutcome
        Dim keyword As String = Str(args, "query")
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        If Not entry.Index.Status.ready Then
            Return ToolOutcome.Busy(WorkspacePool.StatusOf(entry))
        End If

        If String.IsNullOrWhiteSpace(keyword) Then
            Return ToolOutcome.Fail("the 'query' argument is required.")
        End If

        Dim top As Integer = Int(args, "top", defaultTop)
        Dim threshold As Double = Dbl(args, "threshold", defaultThreshold)
        Dim sw As Stopwatch = Stopwatch.StartNew()
        Dim hits As QueryHit() = entry.Index.Query(keyword, top, threshold)

        sw.Stop()

        If hits Is Nothing Then
            Return ToolOutcome.Busy(WorkspacePool.StatusOf(entry))
        End If

        Dim views As New List(Of SymbolView)()

        For Each h As QueryHit In hits
            Call views.Add(SymbolView.From(h.symbol, h.score, maxCodeChars))
        Next

        Return ToolOutcome.Success(New QueryResponse With {
            .workspaceId = entry.Id,
            .query = keyword,
            .top = top,
            .threshold = threshold,
            .count = views.Count,
            .elapsedMs = sw.ElapsedMilliseconds,
            .hits = views.ToArray()
        })
    End Function

    Private Function Tool_FindSymbol(args As JsonObject) As ToolOutcome
        Dim fullName As String = Str(args, "fullName")
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        If Not entry.Index.Status.ready Then
            Return ToolOutcome.Busy(WorkspacePool.StatusOf(entry))
        End If

        If String.IsNullOrWhiteSpace(fullName) Then
            Return ToolOutcome.Fail("the 'fullName' argument is required.")
        End If

        Dim symbols As CodeSymbol() = entry.Index.FindSymbol(fullName)
        Dim views As New List(Of SymbolView)()

        If symbols IsNot Nothing Then
            For Each s As CodeSymbol In symbols
                Call views.Add(SymbolView.From(s, 0, maxCodeChars))
            Next
        End If

        Return ToolOutcome.Success(New SymbolResponse With {
            .workspaceId = entry.Id,
            .fullName = fullName,
            .count = views.Count,
            .symbols = views.ToArray()
        })
    End Function

    Private Function Tool_ListMembers(args As JsonObject) As ToolOutcome
        Dim fullName As String = Str(args, "fullName")
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        If Not entry.Index.Status.ready Then
            Return ToolOutcome.Busy(WorkspacePool.StatusOf(entry))
        End If

        If String.IsNullOrWhiteSpace(fullName) Then
            Return ToolOutcome.Fail("the 'fullName' argument is required.")
        End If

        Dim ws As WorkspaceInfo = entry.Index.Workspace

        If ws Is Nothing Then
            Return ToolOutcome.Fail("the workspace model is not available.")
        End If

        Dim hit As SymbolResolver.TypeHit = SymbolResolver.FindContainer(ws, fullName)

        If hit Is Nothing Then
            Return ToolOutcome.Fail($"type '{fullName}' was not found in workspace '{entry.Id}'. use search_symbols to find the exact full name.")
        End If

        Dim container As TypeContainerSymbol = hit.Container
        Dim members As New List(Of MemberView)()
        Dim nested As New List(Of MemberView)()

        If container.Members IsNot Nothing Then
            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In container.Members
                If kv.Value Is Nothing Then
                    Continue For
                End If

                Call members.Add(Describe(kv.Value, hit.File))
            Next

            Call members.Sort(Function(a, b) a.declarationLine.CompareTo(b.declarationLine))
        End If

        If container.InternalNested IsNot Nothing Then
            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In container.InternalNested
                If kv.Value Is Nothing OrElse Not TypeOf kv.Value Is TypeContainerSymbol Then
                    Continue For
                End If

                Call nested.Add(Describe(kv.Value, hit.File))
            Next
        End If

        Dim loc As Source = FirstSource(container)

        Return ToolOutcome.Success(New MembersResponse With {
            .workspaceId = entry.Id,
            .fullName = fullName,
            .kind = container.Type.ToString(),
            .project = If(hit.File Is Nothing, "", hit.File.Project),
            .file = JsonText.NormPath(If(loc Is Nothing, If(hit.File Is Nothing, "", hit.File.AbsolutePath), loc.FilePath)),
            .relativeFile = If(hit.File Is Nothing, "", hit.File.RelativePath),
            .startLine = If(loc Is Nothing, 0, LineOf(loc, False)),
            .endLine = If(loc Is Nothing, 0, LineOf(loc, True)),
            .xmlDoc = TrimDoc(container.XmlDoc),
            .memberCount = members.Count,
            .members = members.ToArray(),
            .nestedTypeCount = nested.Count,
            .nestedTypes = nested.ToArray()
        })
    End Function

    Private Function Tool_ListFiles(args As JsonObject) As ToolOutcome
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        Dim ws As WorkspaceInfo = entry.Index.Workspace

        If ws Is Nothing Then
            Return ToolOutcome.Fail($"workspace '{entry.Id}' is still opening, there is no file list yet.")
        End If

        Dim project As String = Str(args, "project")
        Dim pattern As String = Str(args, "pattern")
        Dim views As New List(Of FileView)()

        For Each f As SourceFile In ws.Files
            If Not String.IsNullOrWhiteSpace(project) Then
                Dim owner As String = If(f.Project, "")

                If Not String.Equals(owner, project, StringComparison.OrdinalIgnoreCase) AndAlso
                    owner.IndexOf(project, StringComparison.OrdinalIgnoreCase) < 0 Then

                    Continue For
                End If
            End If

            If Not String.IsNullOrWhiteSpace(pattern) Then
                Dim rel As String = If(f.RelativePath, "")

                If Not (rel Like pattern) AndAlso rel.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
            End If

            Call views.Add(New FileView With {
                .project = If(f.Project, ""),
                .relativePath = f.RelativePath,
                .absolutePath = f.AbsolutePath
            })
        Next

        Return ToolOutcome.Success(New FilesResponse With {
            .workspaceId = entry.Id,
            .project = project,
            .pattern = pattern,
            .count = views.Count,
            .files = views.ToArray()
        })
    End Function

    Private Function Tool_GetSource(args As JsonObject) As ToolOutcome
        Dim entry As WorkspaceEntry = pool.Resolve(WsId(args))

        If entry Is Nothing Then
            Return NoWorkspace(WsId(args))
        End If

        Dim ws As WorkspaceInfo = entry.Index.Workspace

        If ws Is Nothing Then
            Return ToolOutcome.Fail($"workspace '{entry.Id}' is still opening, there is nothing to read yet.")
        End If

        Dim file As String = Str(args, "file")
        Dim symbol As String = Str(args, "symbol")
        Dim maxLines As Integer = Int(args, "maxLines", 200)

        If maxLines <= 0 Then
            maxLines = 200
        End If

        If maxLines > 5000 Then
            maxLines = 5000
        End If

        Dim target As String = Nothing
        Dim project As String = ""
        Dim relative As String = ""
        Dim startLine As Integer = Int(args, "startLine", 0)
        Dim endLine As Integer = Int(args, "endLine", 0)

        If Not String.IsNullOrWhiteSpace(symbol) Then
            If Not entry.Index.Status.ready Then
                Return ToolOutcome.Busy(WorkspacePool.StatusOf(entry))
            End If

            Dim found As CodeSymbol() = entry.Index.FindSymbol(symbol)

            If found Is Nothing OrElse found.Length = 0 Then
                Return ToolOutcome.Fail($"symbol '{symbol}' was not found in workspace '{entry.Id}'.")
            End If

            Dim sym As CodeSymbol = found(0)

            target = JsonText.NormPath(sym.File)
            project = If(sym.Project, "")
            relative = sym.RelativeFile

            If startLine <= 0 Then
                startLine = sym.StartLine
            End If

            If endLine <= 0 Then
                endLine = sym.EndLine
            End If
        ElseIf Not String.IsNullOrWhiteSpace(file) Then
            Dim resolved As ResolvedFile = ResolveFile(ws, file)

            If resolved.Error IsNot Nothing Then
                Return ToolOutcome.Fail(resolved.Error)
            End If

            target = resolved.AbsolutePath
            project = resolved.Project
            relative = resolved.RelativePath
        Else
            Return ToolOutcome.Fail("either the 'file' or the 'symbol' argument is required.")
        End If

        If Not IO.File.Exists(target) Then
            Return ToolOutcome.Fail($"the source file does not exist: {target}")
        End If

        Dim lines As String()

        Try
            lines = IO.File.ReadAllLines(target)
        Catch ex As Exception
            Return ToolOutcome.Fail($"cannot read '{target}': {ex.Message}")
        End Try

        Dim total As Integer = lines.Length

        If startLine <= 0 Then
            startLine = 1
        End If

        If endLine <= 0 OrElse endLine > total Then
            endLine = total
        End If

        If startLine > total Then
            startLine = total
        End If

        Dim truncated As Boolean = False

        If endLine - startLine + 1 > maxLines Then
            endLine = startLine + maxLines - 1
            truncated = True
        End If

        Dim count As Integer = endLine - startLine + 1
        Dim buf As String()

        If count <= 0 Then
            buf = New String() {}
        Else
            buf = New String(count - 1) {}

            Call Array.Copy(lines, startLine - 1, buf, 0, count)
        End If

        Return ToolOutcome.Success(New SourceResponse With {
            .workspaceId = entry.Id,
            .project = project,
            .file = target,
            .relativeFile = relative,
            .startLine = startLine,
            .endLine = endLine,
            .totalLines = total,
            .truncated = truncated,
            .code = String.Join(vbLf, buf)
        })
    End Function

    ' ========================================================================
    ' helpers
    ' ========================================================================

    Private Function NoWorkspace(id As String) As ToolOutcome
        If String.IsNullOrWhiteSpace(id) Then
            Return ToolOutcome.Fail("no workspace is open yet. call open_workspace with the path of the vb.net solution, project or folder first.")
        End If

        Return ToolOutcome.Fail($"workspace '{id}' is not open. call list_workspaces to see the open workspaces.")
    End Function

    Private Shared Function Schema(json As String) As JsonObject
        Return TryCast(JsonNode.Parse(json), JsonObject)
    End Function

    Public Shared Function WsId(args As JsonObject) As String
        Return Str(args, "workspaceId")
    End Function

    Public Shared Function Str(args As JsonObject, key As String, Optional def As String = Nothing) As String
        If args Is Nothing Then
            Return def
        End If

        Dim node As JsonNode = Nothing

        If Not args.TryGetPropertyValue(key, node) OrElse node Is Nothing Then
            Return def
        End If

        Try
            Dim text As String = node.GetValue(Of String)()

            If text Is Nothing Then
                Return def
            End If

            Return text
        Catch
            Try
                Return node.ToJsonString().Trim(""""c)
            Catch
                Return def
            End Try
        End Try
    End Function

    Public Shared Function Int(args As JsonObject, key As String, Optional def As Integer = 0) As Integer
        Dim text As String = Str(args, key)
        Dim value As Integer

        If text IsNot Nothing AndAlso
            Integer.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, value) Then

            Return value
        End If

        Return def
    End Function

    Public Shared Function Dbl(args As JsonObject, key As String, Optional def As Double = 0) As Double
        Dim text As String = Str(args, key)
        Dim value As Double

        If text IsNot Nothing AndAlso
            Double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, value) Then

            Return value
        End If

        Return def
    End Function

    ''' <summary>
    ''' resolve a file argument against the workspace: an exact member of the
    ''' workspace file list wins, otherwise the path is resolved against the
    ''' workspace root and must not escape it.
    ''' </summary>
    Private Shared Function ResolveFile(ws As WorkspaceInfo, file As String) As ResolvedFile
        Dim needle As String = file.Replace("/", "\").Trim()
        Dim rooted As Boolean = IO.Path.IsPathRooted(needle)

        For Each f As SourceFile In ws.Files
            Dim rel As String = If(f.RelativePath, "").Replace("/", "\")

            If String.Equals(rel, needle, StringComparison.OrdinalIgnoreCase) OrElse
                String.Equals(JsonText.NormPath(f.AbsolutePath), needle, StringComparison.OrdinalIgnoreCase) OrElse
                (Not rooted AndAlso rel.EndsWith("\" & needle, StringComparison.OrdinalIgnoreCase)) Then

                Return New ResolvedFile With {
                    .AbsolutePath = f.AbsolutePath,
                    .RelativePath = f.RelativePath,
                    .Project = f.Project
                }
            End If
        Next

        Dim full As String

        Try
            If rooted Then
                full = IO.Path.GetFullPath(needle)
            Else
                full = IO.Path.GetFullPath(IO.Path.Combine(ws.RootPath, needle))
            End If
        Catch ex As Exception
            Return New ResolvedFile With {.Error = $"'{file}' is not a usable path: {ex.Message}"}
        End Try

        Dim root As String = IO.Path.GetFullPath(ws.RootPath).TrimEnd("\"c, "/"c)

        If Not full.StartsWith(root, StringComparison.OrdinalIgnoreCase) Then
            Return New ResolvedFile With {
                .Error = $"the path escapes the workspace root: {file}"
            }
        End If

        Return New ResolvedFile With {
            .AbsolutePath = full,
            .RelativePath = full.Substring(System.Math.Min(root.Length + 1, full.Length)),
            .Project = ws.ProjectOf(full)
        }
    End Function

    Private Class ResolvedFile
        Public Property AbsolutePath As String
        Public Property RelativePath As String
        Public Property Project As String
        Public Property [Error] As String
    End Class

    Private Shared Function FirstSource(sym As LanguageSymbolType) As Source
        If sym Is Nothing OrElse sym.Source Is Nothing Then
            Return Nothing
        End If

        Return sym.Source.First
    End Function

    Private Shared Function LineOf(loc As Source, last As Boolean) As Integer
        If loc Is Nothing OrElse loc.LineRange Is Nothing Then
            Return 0
        End If

        Return If(last, loc.LineRange.Max, loc.LineRange.Min)
    End Function

    Private Shared Function TrimDoc(doc As String) As String
        If String.IsNullOrEmpty(doc) Then
            Return ""
        End If

        If doc.Length > 800 Then
            Return doc.Substring(0, 800) & " ..."
        End If

        Return doc
    End Function

    ''' <summary>
    ''' project one parsed symbol into its transport view, including a readable
    ''' signature (the parsed model does not carry the declaration text itself).
    ''' </summary>
    Private Shared Function Describe(sym As LanguageSymbolType, file As SourceFile) As MemberView
        Dim loc As Source = FirstSource(sym)
        Dim kindWord As String = sym.Type.ToString()
        Dim parameters As String() = New String() {}
        Dim returnType As String = Nothing
        Dim signature As String

        If TypeOf sym Is CallableMemberSymbol Then
            Dim callable As CallableMemberSymbol = DirectCast(sym, CallableMemberSymbol)

            parameters = ParamsOf(callable.Parameters)
            returnType = If(callable.ReturnType Is Nothing, Nothing, TypeName(callable.ReturnType))
        ElseIf TypeOf sym Is DelegateSymbol Then
            Dim d As DelegateSymbol = DirectCast(sym, DelegateSymbol)

            parameters = ParamsOf(d.Parameters)
            returnType = If(d.ValueType Is Nothing, Nothing, TypeName(d.ValueType))
        ElseIf TypeOf sym Is EventSymbol Then
            Dim ev As EventSymbol = DirectCast(sym, EventSymbol)

            returnType = If(ev.DelegateType Is Nothing, Nothing, TypeName(ev.DelegateType))
        ElseIf TypeOf sym Is VariableSymbol Then
            Dim v As VariableSymbol = DirectCast(sym, VariableSymbol)

            returnType = If(v.ValueType Is Nothing, Nothing, TypeName(v.ValueType))
        End If

        Dim sb As New Text.StringBuilder()
        Dim modifiers As String = If(sym.Modifiers, "").Trim()

        If modifiers.Length > 0 Then
            Call sb.Append(modifiers).Append(" "c)
        End If

        Call sb.Append(kindWord).Append(" "c).Append(sym.Name)

        If parameters.Length > 0 Then
            Call sb.Append("("c).Append(String.Join(", ", parameters)).Append(")"c)
        End If

        If returnType IsNot Nothing Then
            Call sb.Append(" As ").Append(returnType)
        End If

        signature = sb.ToString()

        Return New MemberView With {
            .name = sym.Name,
            .kind = kindWord,
            .modifiers = modifiers,
            .signature = signature,
            .returnType = returnType,
            .parameters = parameters,
            .startLine = LineOf(loc, False),
            .endLine = LineOf(loc, True),
            .declarationLine = If(loc Is Nothing, 0, loc.DeclarationLine),
            .file = If(loc Is Nothing, If(file Is Nothing, "", file.AbsolutePath), loc.FilePath),
            .relativeFile = If(file Is Nothing, "", file.RelativePath),
            .xmlDoc = TrimDoc(sym.XmlDoc)
        }
    End Function

    Private Shared Function ParamsOf(parameters As Dictionary(Of String, TypeInfo)) As String()
        If parameters Is Nothing OrElse parameters.Count = 0 Then
            Return New String() {}
        End If

        Dim out As New List(Of String)()

        For Each kv As KeyValuePair(Of String, TypeInfo) In parameters
            If kv.Value Is Nothing Then
                Call out.Add(kv.Key)
            Else
                Call out.Add($"{kv.Key} As {TypeName(kv.Value)}")
            End If
        Next

        Return out.ToArray()
    End Function

    Private Shared Function TypeName(type As TypeInfo) As String
        If type Is Nothing Then
            Return Nothing
        End If

        Return If(type.fullName, "Object")
    End Function

End Class

''' <summary>
''' locate a type declaration inside an opened workspace.
''' </summary>
Public Class SymbolResolver

    ''' <summary>a located type together with the source file that declares it.</summary>
    Public Class TypeHit
        Public Property Container As TypeContainerSymbol
        Public Property File As SourceFile
    End Class

    ''' <summary>
    ''' find a type container by its namespace qualified name: the parsed project
    ''' models know how to walk a dotted name, the folder mode has to fall back
    ''' to a recursive scan of the parsed documents.
    ''' </summary>
    Public Shared Function FindContainer(ws As WorkspaceInfo, fullName As String) As TypeHit
        If ws Is Nothing OrElse String.IsNullOrWhiteSpace(fullName) Then
            Return Nothing
        End If

        Dim needle As String = fullName.Trim().Trim("."c)

        For Each proj As VBProject In ws.VbProjects
            If proj Is Nothing Then
                Continue For
            End If

            Dim sym As LanguageSymbolType = Nothing

            Try
                sym = proj.GetType(needle)
            Catch
                sym = Nothing
            End Try

            Dim container As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

            If container IsNot Nothing Then
                Dim file As SourceFile = FileOf(ws, container)

                Return New TypeHit With {.Container = container, .File = file}
            End If
        Next

        Dim suffix As TypeHit = Nothing

        For Each f As SourceFile In ws.Files
            If f Is Nothing OrElse f.Types Is Nothing Then
                Continue For
            End If

            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In f.Types
                Dim exact As TypeHit = Scan(kv.Value, "", needle, f, exactOnly:=True)

                If exact IsNot Nothing Then
                    Return exact
                End If

                If suffix Is Nothing Then
                    suffix = Scan(kv.Value, "", needle, f, exactOnly:=False)
                End If
            Next
        Next

        Return suffix
    End Function

    Private Shared Function Scan(sym As LanguageSymbolType,
                                 parentName As String,
                                 needle As String,
                                 file As SourceFile,
                                 exactOnly As Boolean) As TypeHit

        If sym Is Nothing Then
            Return Nothing
        End If

        Dim name As String = If(sym.Name, "").Trim()
        Dim full As String

        If name.Length = 0 Then
            full = parentName
        ElseIf parentName.Length = 0 Then
            full = name
        Else
            full = parentName & "." & name
        End If

        Dim matched As Boolean

        If exactOnly Then
            matched = String.Equals(full, needle, StringComparison.OrdinalIgnoreCase)
        Else
            matched = String.Equals(full, needle, StringComparison.OrdinalIgnoreCase) OrElse
                full.EndsWith("." & needle, StringComparison.OrdinalIgnoreCase)
        End If

        Dim container As TypeContainerSymbol = TryCast(sym, TypeContainerSymbol)

        If matched AndAlso container IsNot Nothing Then
            Return New TypeHit With {.Container = container, .File = file}
        End If

        If container Is Nothing Then
            Return Nothing
        End If

        If container.InternalNested IsNot Nothing Then
            For Each kv As KeyValuePair(Of String, LanguageSymbolType) In container.InternalNested
                Dim hit As TypeHit = Scan(kv.Value, full, needle, file, exactOnly)

                If hit IsNot Nothing Then
                    Return hit
                End If
            Next
        End If

        Return Nothing
    End Function

    ''' <summary>the workspace file that declares the given symbol.</summary>
    Private Shared Function FileOf(ws As WorkspaceInfo, sym As LanguageSymbolType) As SourceFile
        Dim loc As Source = If(sym Is Nothing OrElse sym.Source Is Nothing, Nothing, sym.Source.First)

        If loc Is Nothing OrElse String.IsNullOrEmpty(loc.FilePath) Then
            Return Nothing
        End If

        Dim wanted As String = JsonText.NormPath(loc.FilePath)

        For Each f As SourceFile In ws.Files
            If String.Equals(JsonText.NormPath(f.AbsolutePath), wanted, StringComparison.OrdinalIgnoreCase) Then
                Return f
            End If
        Next

        Return Nothing
    End Function

End Class
