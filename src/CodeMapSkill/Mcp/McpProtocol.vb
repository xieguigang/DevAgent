Imports System.Text
Imports System.Text.Json
Imports System.Text.Json.Nodes
Imports Flute.Http.Core.Message
Imports Flute.Http.Core.Message.HttpHeader

''' <summary>
''' the json-rpc 2.0 plumbing of the mcp endpoint.
''' </summary>
''' <remarks>
''' named <c>McpJsonRpc</c> on purpose: the json library that comes with the
''' framework already owns the <c>JsonRpc</c> name, and a module with the same
''' name would shadow it.
''' </remarks>
Public Module McpJsonRpc

    Public Const ProtocolVersion As String = "2025-06-18"

    ' the standard json-rpc error codes
    Public Const ParseError As Integer = -32700
    Public Const InvalidRequest As Integer = -32600
    Public Const MethodNotFound As Integer = -32601
    Public Const InvalidParams As Integer = -32602
    Public Const InternalError As Integer = -32603

    ''' <summary>
    ''' a tool specific error code: the index of the requested workspace is
    ''' still building, so the call should be retried later.
    ''' </summary>
    Public Const IndexNotReady As Integer = -32002

    Private Const ServerName As String = "codemap-skill"
    Private Const ServerVersion As String = "1.0.0"

    ''' <summary>
    ''' handle one json-rpc request.
    ''' </summary>
    ''' <returns>
    ''' the response text, or <c>Nothing</c> when the request was a notification
    ''' (a notification is never answered).
    ''' </returns>
    Public Function Handle(raw As String, tools As SkillTools, port As Integer) As String
        Dim request As JsonObject = Nothing

        Try
            request = TryCast(JsonNode.Parse(If(raw, "")), JsonObject)
        Catch ex As Exception
            request = Nothing
        End Try

        If request Is Nothing Then
            Return MakeError(Nothing, ParseError, "the request body is not valid json.")
        End If

        Dim id As JsonNode = If(request("id"), Nothing)
        Dim method As String = Nothing

        If request("method") IsNot Nothing Then
            method = request("method").GetValue(Of String)()
        End If

        If method IsNot Nothing Then
            method = method.Trim()
        End If

        If String.IsNullOrWhiteSpace(method) Then
            Return MakeError(id, InvalidRequest, "the 'method' member is required.")
        End If

        Dim params As JsonObject = TryCast(request("params"), JsonObject)

        Select Case method
            Case "initialize"
                Return OkResult(id, Initialize(port))
            Case "ping"
                Return OkResult(id, New JsonObject())
            Case "tools/list"
                Return OkResult(id, ListTools(tools))
            Case "tools/call"
                Return CallTool(id, params, tools)
            Case Else
                If method.StartsWith("notifications/", StringComparison.OrdinalIgnoreCase) Then
                    ' a notification is fire and forget: answer with an empty body
                    Return Nothing
                End If

                Return MakeError(id, MethodNotFound, $"method '{method}' is not supported. available: initialize, tools/list, tools/call, ping.")
        End Select
    End Function

    Private Function Initialize(port As Integer) As JsonObject
        Dim capabilities As New JsonObject()
        Dim toolCaps As New JsonObject()

        toolCaps("listChanged") = False
        capabilities("tools") = toolCaps

        Dim info As New JsonObject()

        info("name") = ServerName
        info("version") = ServerVersion

        Dim result As New JsonObject()

        result("protocolVersion") = ProtocolVersion
        result("capabilities") = capabilities
        result("serverInfo") = info
        result("instructions") =
            "A read only vb.net code index. Call open_workspace with the path of a .sln, .slnx, .vbproj or source folder, " &
            "then poll index_status until ready is true, then use search_symbols / find_symbol / list_members / list_files / get_source. " &
            $"The same capabilities are available over REST at http://localhost:{port}/api/."

        Return result
    End Function

    Private Function ListTools(tools As SkillTools) As JsonObject
        Dim list As New JsonArray()

        For Each t As ToolDescriptor In tools.All
            Call list.Add(t.ToJson())
        Next

        Dim result As New JsonObject()

        result("tools") = list

        Return result
    End Function

    Private Function CallTool(id As JsonNode, params As JsonObject, tools As SkillTools) As String
        Dim name As String = Nothing

        If params IsNot Nothing AndAlso params("name") IsNot Nothing Then
            name = params("name").GetValue(Of String)()
        End If

        If String.IsNullOrWhiteSpace(name) Then
            Return MakeError(id, InvalidParams, "tools/call requires a 'name' argument.")
        End If

        Dim args As JsonObject = Nothing

        If params IsNot Nothing Then
            args = TryCast(params("arguments"), JsonObject)
        End If

        Dim outcome As ToolOutcome = tools.CallTool(name, If(args, New JsonObject()))

        If outcome.Ok Then
            Dim content As New JsonArray()
            Dim block As New JsonObject()
            Dim text As String

            If outcome.Payload Is Nothing Then
                text = "{}"
            ElseIf TypeOf outcome.Payload Is String Then
                text = DirectCast(outcome.Payload, String)
            Else
                text = JsonText.ToJson(outcome.Payload)
            End If

            block("type") = "text"
            block("text") = text
            content.Add(block)

            Dim result As New JsonObject()

            result("content") = content
            result("isError") = False

            If outcome.Payload IsNot Nothing AndAlso Not TypeOf outcome.Payload Is String Then
                result("structuredContent") = JsonSerializer.SerializeToNode(
                    outcome.Payload, outcome.Payload.GetType(), JsonText.Options)
            End If

            Return OkResult(id, result)
        End If

        Dim code As Integer = If(outcome.NotReady, IndexNotReady, InternalError)
        Dim detail As New JsonObject()

        detail("message") = If(outcome.ErrorMessage, "the tool call failed.")

        If outcome.Payload IsNot Nothing Then
            detail("status") = JsonSerializer.SerializeToNode(
                outcome.Payload, outcome.Payload.GetType(), JsonText.Options)
        End If

        Return MakeError(id, code, If(outcome.ErrorMessage, "the tool call failed."), detail)
    End Function

    Private Function OkResult(id As JsonNode, payload As JsonObject) As String
        Dim response As New JsonObject()

        response("jsonrpc") = "2.0"
        response("id") = If(id, Nothing)
        response("result") = payload

        Return response.ToJsonString(JsonText.Options)
    End Function

    Private Function MakeError(id As JsonNode,
                               code As Integer,
                               message As String,
                               Optional data As JsonObject = Nothing) As String

        Dim err As New JsonObject()

        err("code") = code
        err("message") = message

        If data IsNot Nothing Then
            err("data") = data
        End If

        Dim response As New JsonObject()

        response("jsonrpc") = "2.0"
        response("id") = If(id, Nothing)
        response("error") = err

        Return response.ToJsonString(JsonText.Options)
    End Function

End Module

''' <summary>
''' the mcp endpoint: a single <c>POST /mcp</c> route that speaks json-rpc 2.0.
''' </summary>
Public Class McpController

    ReadOnly tools As SkillTools
    ReadOnly port As Integer

    Sub New(tools As SkillTools, Optional port As Integer = 8080)
        Me.tools = tools
        Me.port = port
    End Sub

    ''' <summary>
    ''' the streamable http transport of mcp: one json-rpc message per request.
    ''' </summary>
    <HttpPost("/mcp")>
    Public Sub Mcp(req As HttpPOSTRequest, res As HttpResponse)
        Dim raw As String = ReadBody(req)
        Dim response As String

        Try
            response = McpJsonRpc.Handle(raw, tools, port)
        Catch ex As Exception
            ' an unexpected failure is still reported as a json-rpc error so
            ' that the agent sees a structured answer instead of a broken pipe
            Dim fallback As New JsonObject()
            Dim err As New JsonObject()

            err("code") = McpJsonRpc.InternalError
            err("message") = ex.Message

            fallback("jsonrpc") = "2.0"
            fallback("error") = err

            response = fallback.ToJsonString(JsonText.Options)
        End Try

        res.AccessControlAllowOrigin = "*"

        If response Is Nothing Then
            ' a notification: 200 with an empty body
            res.WriteContent(New Byte() {}, "application/json")
        Else
            res.WriteContent(Encoding.UTF8.GetBytes(response), "application/json; charset=utf-8")
        End If
    End Sub

    ''' <summary>a friendly answer for a browser that opens the endpoint.</summary>
    <HttpGet("/mcp")>
    Public Sub McpInfo(req As HttpRequest, res As HttpResponse)
        Dim info As New JsonObject()

        info("endpoint") = "POST /mcp"
        info("protocol") = "json-rpc 2.0 (mcp streamable http, stateless)"
        info("protocolVersion") = McpJsonRpc.ProtocolVersion
        info("methods") = New JsonArray("initialize", "tools/list", "tools/call", "ping")
        info("toolCount") = tools.All.Length

        Call JsonText.WriteJson(res, info)
    End Sub

    ''' <summary>
    ''' the framework hands the posted payload over as a temp file, so the raw
    ''' json-rpc text is read back from that file inside the handler.
    ''' </summary>
    Private Shared Function ReadBody(req As HttpPOSTRequest) As String
        If req Is Nothing OrElse req.POSTData Is Nothing Then
            Return ""
        End If

        Dim file As String = req.POSTData.InputStream

        If String.IsNullOrEmpty(file) OrElse Not IO.File.Exists(file) Then
            ' no payload at all: fall back to the parsed form/object values
            Return ""
        End If

        Try
            Dim encoding As Encoding = If(req.POSTData.ContentEncoding, Encoding.UTF8)

            Return IO.File.ReadAllText(file, encoding)
        Catch
            Return ""
        End Try
    End Function

End Class
