Imports System.Text
Imports System.Text.Json.Nodes
Imports CodeMap.CodeIndex
Imports Flute.Http.Core.Message
Imports Flute.Http.Core.Message.HttpHeader
Imports Microsoft.VisualBasic.Net.Http

''' <summary>
''' read a request parameter from the url query string, from a posted form or
''' from a posted json object.
''' </summary>
Module RequestArgs

    Public Function Param(req As HttpRequest, name As String) As String
        Return Param(req, name, Nothing)
    End Function

    ''' <summary>read a request parameter, trying several aliases in order.</summary>
    Public Function Param(req As HttpRequest, name As String, ParamArray aliases As String()) As String
        Dim value As String = Nothing

        If aliases IsNot Nothing Then
            For Each name2 As String In aliases
                value = Read(req, name2)

                If value IsNot Nothing Then
                    Return value
                End If
            Next
        End If

        Return Read(req, name)
    End Function

    Private Function Read(req As HttpRequest, name As String) As String
        If req IsNot Nothing AndAlso req.URL IsNot Nothing AndAlso req.URL.query IsNot Nothing Then
            For Each kv As KeyValuePair(Of String, String()) In req.URL.query
                If String.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase) AndAlso
                    kv.Value IsNot Nothing AndAlso kv.Value.Length > 0 AndAlso kv.Value(0) IsNot Nothing Then

                    Return kv.Value(0)
                End If
            Next
        End If

        Dim post As HttpPOSTRequest = TryCast(req, HttpPOSTRequest)

        If post IsNot Nothing AndAlso post.POSTData IsNot Nothing Then
            Try
                Dim form As String = post.POSTData(name)

                If Not String.IsNullOrEmpty(form) Then
                    Return form
                End If
            Catch
            End Try

            Try
                If post.POSTData.Objects IsNot Nothing Then
                    For Each kv As KeyValuePair(Of String, Object) In post.POSTData.Objects
                        If String.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase) AndAlso kv.Value IsNot Nothing Then
                            Return kv.Value.ToString()
                        End If
                    Next
                End If
            Catch
            End Try
        End If

        Return Nothing
    End Function

    ''' <summary>collect the arguments of one tool out of an http request.</summary>
    Public Function Args(req As HttpRequest, ParamArray names As String()) As JsonObject
        Dim collected As New JsonObject()

        For Each name As String In names
            Dim value As String = Param(req, name)

            If value IsNot Nothing Then
                collected(name) = value
            End If
        Next

        Return collected
    End Function

End Module

''' <summary>
''' the rest api of the code index skill.
''' </summary>
''' <remarks>
''' Every route is a thin adapter over the tool registry: it collects the
''' request parameters, calls the tool and renders the outcome. The mcp endpoint
''' calls the very same tools, so the two protocols can not drift apart.
''' </remarks>
Public Class SkillController

    ReadOnly tools As SkillTools
    ReadOnly pool As WorkspacePool
    ReadOnly port As Integer

    Sub New(tools As SkillTools, pool As WorkspacePool, Optional port As Integer = 8080)
        Me.tools = tools
        Me.pool = pool
        Me.port = port
    End Sub

    ''' <summary>a small landing page with the endpoint table.</summary>
    <HttpGet("/")>
    Public Sub Home(req As HttpRequest, res As HttpResponse)
        res.AccessControlAllowOrigin = "*"
        res.WriteHTML(RenderHome())
    End Sub

    ''' <summary>the build progress of one workspace index.</summary>
    <HttpGet("/api/status")>
    Public Sub Status(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "index_status", RequestArgs.Args(req, "ws"))
    End Sub

    ''' <summary>every workspace that is currently open.</summary>
    <HttpGet("/api/workspaces")>
    Public Sub Workspaces(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "list_workspaces", New JsonObject())
    End Sub

    ''' <summary>open a repository (query string form).</summary>
    <HttpGet("/api/open")>
    Public Sub OpenGet(req As HttpRequest, res As HttpResponse)
        Call OpenWorkspace(req, res)
    End Sub

    ''' <summary>open a repository (form or json body).</summary>
    <HttpPost("/api/open")>
    Public Sub OpenPost(req As HttpPOSTRequest, res As HttpResponse)
        Call OpenWorkspace(req, res)
    End Sub

    ''' <summary>the description of the opened workspace.</summary>
    <HttpGet("/api/workspace")>
    Public Sub Workspace(req As HttpRequest, res As HttpResponse)
        Dim entry As WorkspaceEntry = pool.Resolve(RequestArgs.Param(req, "ws"))

        If entry Is Nothing Then
            Call Fail(res, HTTP_RFC.RFC_NOT_FOUND, "no workspace is open; call /api/open?path=<repo> first.")
            Return
        End If

        Dim ws As WorkspaceInfo = entry.Index.Workspace

        If ws Is Nothing Then
            Call Fail(res, HTTP_RFC.RFC_SERVICE_UNAVAILABLE, $"workspace '{entry.Id}' is still opening.")
            Return
        End If

        res.AccessControlAllowOrigin = "*"
        res.WriteContent(Encoding.UTF8.GetBytes(JsonText.ToJson(WorkspacePool.Summarize(ws))),
                         "application/json; charset=utf-8")
    End Sub

    ''' <summary>fuzzy keyword search over the indexed symbols.</summary>
    <HttpGet("/api/query")>
    Public Sub Query(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "search_symbols", BuildArgs(req, "query", "q:query", "keyword:query", "ws", "top", "threshold"))
    End Sub

    ''' <summary>the same query, accepting a form or json encoded body.</summary>
    <HttpPost("/api/query")>
    Public Sub QueryPost(req As HttpPOSTRequest, res As HttpResponse)
        Call Run(req, res, "search_symbols", BuildArgs(req, "query", "q:query", "keyword:query", "ws", "top", "threshold"))
    End Sub

    ''' <summary>resolve one symbol by its namespace qualified full name.</summary>
    <HttpGet("/api/symbol")>
    Public Sub Symbol(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "find_symbol", SymbolArgs(req))
    End Sub

    ''' <summary>every member of a type.</summary>
    <HttpGet("/api/members")>
    Public Sub Members(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "list_members", SymbolArgs(req))
    End Sub

    ''' <summary>the vb.net source files of the workspace.</summary>
    <HttpGet("/api/files")>
    Public Sub Files(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "list_files", BuildArgs(req, "project", "pattern", "ws"))
    End Sub

    ''' <summary>raw source text of a file range or of a symbol block.</summary>
    <HttpGet("/api/source")>
    Public Sub Source(req As HttpRequest, res As HttpResponse)
        Call Run(req, res, "get_source", SourceArgs(req))
    End Sub

    ''' <summary>the same, with a form or json encoded body.</summary>
    <HttpPost("/api/source")>
    Public Sub SourcePost(req As HttpPOSTRequest, res As HttpResponse)
        Call Run(req, res, "get_source", SourceArgs(req))
    End Sub

    ' ========================================================================
    ' internals
    ' ========================================================================

    Private Sub OpenWorkspace(req As HttpRequest, res As HttpResponse)
        Dim args As New JsonObject()
        Dim wsPath As String = RequestArgs.Param(req, "path")

        If wsPath IsNot Nothing Then
            args("path") = wsPath
        End If

        Call Run(req, res, "open_workspace", args)
    End Sub

    Private Shared Function SymbolArgs(req As HttpRequest) As JsonObject
        Dim args As New JsonObject()
        Dim fullName As String = RequestArgs.Param(req, "fullname", "fullName", "name")
        Dim ws As String = RequestArgs.Param(req, "ws")

        If fullName IsNot Nothing Then
            args("fullName") = fullName
        End If

        If ws IsNot Nothing Then
            args("workspaceId") = ws
        End If

        Return args
    End Function

    Private Shared Function SourceArgs(req As HttpRequest) As JsonObject
        Dim args As New JsonObject()

        Call Put(args, "file", RequestArgs.Param(req, "file", "path"))
        Call Put(args, "symbol", RequestArgs.Param(req, "symbol", "fullname", "fullName"))
        Call Put(args, "startLine", RequestArgs.Param(req, "startLine", "start"))
        Call Put(args, "endLine", RequestArgs.Param(req, "endLine", "end"))
        Call Put(args, "maxLines", RequestArgs.Param(req, "maxLines", "lines"))
        Call Put(args, "workspaceId", RequestArgs.Param(req, "ws"))

        Return args
    End Function

    ''' <summary>
    ''' collect the tool arguments out of a request.
    ''' </summary>
    ''' <remarks>
    ''' A name may be written as <c>queryName:argName</c> when the query string
    ''' parameter is an alias of the tool argument, e.g. <c>q:query</c> for
    ''' <c>/api/query?q=</c>; <c>ws</c> is always mapped onto <c>workspaceId</c>.
    ''' </remarks>
    Private Shared Function BuildArgs(req As HttpRequest, ParamArray names As String()) As JsonObject
        Dim args As New JsonObject()

        For Each spec As String In names
            Dim queryName As String = spec
            Dim argName As String = spec
            Dim sep As Integer = spec.IndexOf(":"c)

            If sep > 0 Then
                queryName = spec.Substring(0, sep)
                argName = spec.Substring(sep + 1)
            End If

            Dim value As String = RequestArgs.Param(req, queryName)

            If value Is Nothing Then
                Continue For
            End If

            If String.Equals(argName, "ws", StringComparison.OrdinalIgnoreCase) Then
                argName = "workspaceId"
            End If

            args(argName) = value
        Next

        Return args
    End Function

    Private Shared Sub Put(args As JsonObject, key As String, value As String)
        If value IsNot Nothing Then
            args(key) = value
        End If
    End Sub

    Private Sub Run(req As HttpRequest, res As HttpResponse, tool As String, args As JsonObject)
        Dim outcome As ToolOutcome = tools.CallTool(tool, args)

        If outcome.Ok Then
            Call JsonText.WriteJson(res, outcome.Payload)
            Return
        End If

        Dim message As String = If(outcome.ErrorMessage, "the request failed.")

        If outcome.NotReady Then
            Call Fail(res, HTTP_RFC.RFC_SERVICE_UNAVAILABLE, message)
        ElseIf message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            message.IndexOf("is not open", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
            message.IndexOf("was not found", StringComparison.OrdinalIgnoreCase) >= 0 Then

            Call Fail(res, HTTP_RFC.RFC_NOT_FOUND, message)
        Else
            Call Fail(res, HTTP_RFC.RFC_BAD_REQUEST, message)
        End If
    End Sub

    Private Shared Sub Fail(res As HttpResponse, code As HTTP_RFC, message As String)
        res.AccessControlAllowOrigin = "*"
        res.WriteError(code, message)
    End Sub

    Private Function RenderHome() As String
        Dim sb As New StringBuilder()
        Dim base As String = $"http://localhost:{port}"

        Call sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'>")
        Call sb.AppendLine("<title>CodeMapSkill - vb.net code index</title>")
        Call sb.AppendLine("<style>body{font-family:Consolas,monospace;background:#111827;color:#e5e7eb;margin:2em;}")
        Call sb.AppendLine("h1{color:#38bdf8;}h2{color:#a3e635;font-size:1.05em;}code{color:#fbbf24;}")
        Call sb.AppendLine("table{border-collapse:collapse;}td,th{border:1px solid #374151;padding:4px 8px;text-align:left;}")
        Call sb.AppendLine("th{background:#1f2937;}</style></head><body>")
        Call sb.AppendLine("<h1>CodeMapSkill</h1>")
        Call sb.AppendLine("<p>a read only vb.net code index exposed as an agent skill (rest + mcp).</p>")

        Call sb.AppendLine("<h2>workspaces</h2>")
        Call sb.AppendLine("<table><tr><th>id</th><th>path</th><th>phase</th><th>ready</th><th>symbols</th></tr>")

        For Each e As WorkspaceEntry In pool.List()
            Dim st As StatusResponse = WorkspacePool.StatusOf(e)

            Call sb.AppendLine($"<tr><td>{Html(e.Id)}</td><td>{Html(e.Path)}</td><td>{Html(st.phase)}</td><td>{st.ready}</td><td>{st.symbols}</td></tr>")
        Next

        Call sb.AppendLine("</table>")

        Call sb.AppendLine("<h2>rest endpoints</h2>")
        Call sb.AppendLine("<table><tr><th>method</th><th>path</th><th>purpose</th></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/status?ws=</code></td><td>index build progress</td></tr>")
        Call sb.AppendLine($"<tr><td>GET/POST</td><td><code>/api/open?path=</code></td><td>open a solution, project or folder</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/workspaces</code></td><td>list the open workspaces</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/query?q=&amp;top=&amp;ws=</code></td><td>fuzzy symbol search</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/symbol?fullname=&amp;ws=</code></td><td>resolve one symbol</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/members?fullname=&amp;ws=</code></td><td>list the members of a type</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/api/files?project=&amp;pattern=&amp;ws=</code></td><td>list the source files</td></tr>")
        Call sb.AppendLine($"<tr><td>GET/POST</td><td><code>/api/source?file=&amp;start=&amp;end=&amp;ws=</code></td><td>read raw source text</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/skill/SKILL.md</code></td><td>the skill definition</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/skill/manifest.yaml</code></td><td>the skill manifest</td></tr>")
        Call sb.AppendLine($"<tr><td>GET</td><td><code>/tools.json</code></td><td>the tool catalogue</td></tr>")
        Call sb.AppendLine("</table>")

        Call sb.AppendLine("<h2>mcp endpoint</h2>")
        Call sb.AppendLine($"<p><code>POST {base}/mcp</code> with a json-rpc 2.0 body: <code>initialize</code>, <code>tools/list</code>, <code>tools/call</code>, <code>ping</code>.</p>")

        Call sb.AppendLine("</body></html>")

        Return sb.ToString()
    End Function

    Private Shared Function Html(text As String) As String
        If String.IsNullOrEmpty(text) Then
            Return ""
        End If

        Return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
    End Function

End Class
