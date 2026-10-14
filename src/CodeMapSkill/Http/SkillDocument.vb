Imports System.Reflection
Imports System.Text
Imports System.Text.Json.Nodes
Imports Flute.Http.Core.Message
Imports Flute.Http.Core.Message.HttpHeader
Imports Microsoft.VisualBasic.Net.Http

''' <summary>
''' the skill definition files that this service serves: they are shipped inside
''' the repository under <c>skill/vbnet-code-index/</c>, copied next to the
''' executable and embedded into the assembly as a fallback, so the service can
''' always hand out a definition even when it was started from a bare binary.
''' </summary>
Public Module SkillDocument

    Public Const SkillName As String = "vbnet-code-index"
    Public Const SkillVersion As String = "1.0.0"

    ''' <summary>
    ''' read one file of the skill folder: the copy next to the executable wins
    ''' (it can be edited without a rebuild), the embedded copy is the fallback.
    ''' </summary>
    Public Function ReadSkillFile(fileName As String) As String
        Dim dir As String = IO.Path.Combine(AppContext.BaseDirectory, "skill", SkillName)
        Dim disk As String = IO.Path.Combine(dir, fileName)

        If IO.File.Exists(disk) Then
            Try
                Return IO.File.ReadAllText(disk, New UTF8Encoding(False))
            Catch
            End Try
        End If

        Dim resName As String = "CodeMapSkill.skill." & SkillName & "." &
            fileName.Replace("\"c, "."c).Replace("/"c, "."c)
        Dim asm As Assembly = GetType(SkillDocumentController).Assembly

        Try
            Using stream As IO.Stream = asm.GetManifestResourceStream(resName)
                If stream IsNot Nothing Then
                    Using reader As New IO.StreamReader(stream, New UTF8Encoding(False))
                        Return reader.ReadToEnd()
                    End Using
                End If
            End Using
        Catch
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' fill the <c>{{BASE}}</c> / <c>{{PORT}}</c> placeholders of the skill
    ''' definition with the address this service is actually listening on.
    ''' </summary>
    Public Function Render(fileName As String, port As Integer) As String
        Dim text As String = ReadSkillFile(fileName)

        If text Is Nothing Then
            Return Nothing
        End If

        Dim base As String = $"http://localhost:{port}"

        Return text.Replace("{{BASE}}", base) _
                   .Replace("{{PORT}}", port.ToString()) _
                   .Replace("{{VERSION}}", SkillVersion)
    End Function

    ''' <summary>the tool catalogue that <c>/tools.json</c> serves.</summary>
    Public Function Catalogue(tools As SkillTools, port As Integer) As JsonObject
        Dim list As New JsonArray()

        For Each t As ToolDescriptor In tools.All
            Call list.Add(t.ToJson())
        Next

        Dim rest As New JsonArray()

        Call AddRest(rest, "GET", "/api/status", "index_status", "ws")
        Call AddRest(rest, "GET", "/api/workspaces", "list_workspaces", "")
        Call AddRest(rest, "GET|POST", "/api/open", "open_workspace", "path")
        Call AddRest(rest, "GET", "/api/workspace", "-", "ws")
        Call AddRest(rest, "GET|POST", "/api/query", "search_symbols", "q|query, top, threshold, ws")
        Call AddRest(rest, "GET", "/api/symbol", "find_symbol", "fullname, ws")
        Call AddRest(rest, "GET", "/api/members", "list_members", "fullname, ws")
        Call AddRest(rest, "GET", "/api/files", "list_files", "project, pattern, ws")
        Call AddRest(rest, "GET|POST", "/api/source", "get_source", "file|symbol, start, end, maxLines, ws")
        Call AddRest(rest, "GET", "/skill/SKILL.md", "-", "")
        Call AddRest(rest, "GET", "/skill/manifest.yaml", "-", "")
        Call AddRest(rest, "POST", "/mcp", "-", "json-rpc 2.0 body")

        Dim mcp As New JsonObject()

        mcp("endpoint") = "POST /mcp"
        mcp("protocolVersion") = McpJsonRpc.ProtocolVersion
        mcp("methods") = New JsonArray("initialize", "tools/list", "tools/call", "ping")

        Dim doc As New JsonObject()

        doc("service") = "CodeMapSkill"
        doc("skill") = SkillName
        doc("version") = SkillVersion
        doc("base") = $"http://localhost:{port}"
        doc("mcp") = mcp
        doc("toolCount") = tools.All.Length
        doc("tools") = list
        doc("restEndpoints") = rest

        Return doc
    End Function

    Private Sub AddRest(list As JsonArray, method As String, url As String, tool As String, params As String)
        Dim item As New JsonObject()

        item("method") = method
        item("path") = url
        item("tool") = tool
        item("params") = params

        Call list.Add(item)
    End Sub

End Module

''' <summary>
''' the routes that hand out the skill definition and the tool catalogue.
''' </summary>
Public Class SkillDocumentController

    ReadOnly tools As SkillTools
    ReadOnly port As Integer

    Sub New(tools As SkillTools, Optional port As Integer = 8080)
        Me.tools = tools
        Me.port = port
    End Sub

    ''' <summary>the skill definition, ready to be dropped into a skills folder.</summary>
    <HttpGet("/skill/SKILL.md")>
    Public Sub SkillMarkdown(req As HttpRequest, res As HttpResponse)
        Call Serve(res, "SKILL.md", "text/markdown; charset=utf-8")
    End Sub

    ''' <summary>the skill manifest.</summary>
    <HttpGet("/skill/manifest.yaml")>
    Public Sub SkillManifest(req As HttpRequest, res As HttpResponse)
        Call Serve(res, "manifest.yaml", "text/yaml; charset=utf-8")
    End Sub

    ''' <summary>the powershell helper the skill uses to call this service.</summary>
    <HttpGet("/skill/scripts/codemap.ps1")>
    Public Sub SkillScript(req As HttpRequest, res As HttpResponse)
        Call Serve(res, "scripts/codemap.ps1", "text/plain; charset=utf-8")
    End Sub

    ''' <summary>the powershell helper for the mcp endpoint.</summary>
    <HttpGet("/skill/scripts/mcp_call.ps1")>
    Public Sub SkillMcpScript(req As HttpRequest, res As HttpResponse)
        Call Serve(res, "scripts/mcp_call.ps1", "text/plain; charset=utf-8")
    End Sub

    ''' <summary>the reference of the tool arguments.</summary>
    <HttpGet("/skill/references/api.md")>
    Public Sub SkillApiReference(req As HttpRequest, res As HttpResponse)
        Call Serve(res, "references/api.md", "text/markdown; charset=utf-8")
    End Sub

    ''' <summary>the machine readable tool catalogue.</summary>
    <HttpGet("/tools.json")>
    Public Sub ToolsJson(req As HttpRequest, res As HttpResponse)
        Call JsonText.WriteJson(res, SkillDocument.Catalogue(tools, port))
    End Sub

    Private Sub Serve(res As HttpResponse, fileName As String, mime As String)
        Dim text As String = SkillDocument.Render(fileName.Replace("/"c, IO.Path.DirectorySeparatorChar), port)

        If text Is Nothing Then
            res.AccessControlAllowOrigin = "*"
            res.WriteError(HTTP_RFC.RFC_NOT_FOUND, $"the skill file '{fileName}' is not available in this build.")
            Return
        End If

        Call JsonText.WriteText(res, text, mime)
    End Sub

End Class
