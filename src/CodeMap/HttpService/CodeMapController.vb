Imports System.Globalization
Imports CodeMap.CodeIndex
Imports Flute.Http.Core.Message
Imports Flute.Http.Core.Message.HttpHeader
Imports Microsoft.VisualBasic.Net.Http

Namespace HttpService

    ''' <summary>the json payload returned by <c>/api/query</c></summary>
    Public Class QueryResponse
        Public Property query As String
        Public Property top As Integer
        Public Property threshold As Double
        Public Property count As Integer
        Public Property elapsedMs As Long
        Public Property hits As QueryHit()
    End Class

    ''' <summary>the json payload returned by <c>/api/symbol</c></summary>
    Public Class SymbolResponse
        Public Property fullName As String
        Public Property count As Integer
        Public Property symbols As CodeSymbol()
    End Class

    ''' <summary>a compact workspace description for <c>/api/workspace</c></summary>
    Public Class WorkspaceSummary
        Public Property kind As String
        Public Property name As String
        Public Property rootPath As String
        Public Property projectCount As Integer
        Public Property fileCount As Integer
        Public Property projects As String()
        Public Property errors As String()
    End Class

    ''' <summary>the json payload returned by <c>/api/status</c></summary>
    Public Class StatusResponse
        Public Property ready As Boolean
        Public Property phase As String
        Public Property processedFiles As Integer
        Public Property totalFiles As Integer
        Public Property symbols As Integer
        Public Property documents As Integer
        Public Property errors As Integer
        Public Property elapsedMs As Long
        Public Property message As String
        Public Property progress As Double
        Public Property workspace As WorkspaceSummary
    End Class

    ''' <summary>
    ''' the http route controller of the code map index service.
    ''' </summary>
    ''' <remarks>
    ''' Every route method must keep the exact signature
    ''' <c>Sub(HttpRequest, HttpResponse)</c> so that <c>HttpRouter</c> can
    ''' discover it through reflection.
    ''' </remarks>
    Public Class CodeMapController

        ReadOnly index As CodeMapIndex
        ReadOnly port As Integer

        ''' <summary>the default number of returned hits.</summary>
        Public Property DefaultTop As Integer = 20
        ''' <summary>the default minimum q-gram similarity of a matched word.</summary>
        Public Property DefaultThreshold As Double = 0.15

        Sub New(index As CodeMapIndex, Optional port As Integer = 8080)
            Me.index = index
            Me.port = port
        End Sub

        ''' <summary>the built in query console.</summary>
        <HttpGet("/")>
        Public Sub Home(req As HttpRequest, res As HttpResponse)
            res.AccessControlAllowOrigin = "*"
            res.WriteHTML(IndexPage.Render(index.Workspace, port))
        End Sub

        ''' <summary>fuzzy keyword search over the indexed code symbols.</summary>
        <HttpGet("/api/query")>
        Public Sub Query(req As HttpRequest, res As HttpResponse)
            res.AccessControlAllowOrigin = "*"

            Dim status As IndexStatus = index.Status

            If Not status.ready Then
                Call Unavailable(res, status)
                Return
            End If

            Dim q As String = Param(req, "q")

            If String.IsNullOrWhiteSpace(q) Then
                res.WriteError(HTTP_RFC.RFC_BAD_REQUEST, "missing required parameter 'q'.")
                Return
            End If

            Dim top As Integer = ParseInt(Param(req, "top"), DefaultTop)
            Dim threshold As Double = ParseDouble(Param(req, "threshold"), DefaultThreshold)

            If top <= 0 Then
                top = DefaultTop
            End If

            If threshold < 0 Then
                threshold = 0
            End If

            Dim sw As Stopwatch = Stopwatch.StartNew()
            Dim hits As QueryHit() = index.Query(q, top, threshold)
            sw.Stop()

            If hits Is Nothing Then
                Call Unavailable(res, index.Status)
                Return
            End If

            res.WriteJSON(New QueryResponse With {
                .query = q,
                .top = top,
                .threshold = threshold,
                .count = hits.Length,
                .elapsedMs = sw.ElapsedMilliseconds,
                .hits = hits
            })
        End Sub

        ''' <summary>the same query, accepting a form or json encoded body.</summary>
        <HttpPost("/api/query")>
        Public Sub QueryPost(req As HttpPOSTRequest, res As HttpResponse)
            Call Query(req, res)
        End Sub

        ''' <summary>resolve one symbol by its namespace qualified full name.</summary>
        <HttpGet("/api/symbol")>
        Public Sub Symbol(req As HttpRequest, res As HttpResponse)
            res.AccessControlAllowOrigin = "*"

            Dim status As IndexStatus = index.Status

            If Not status.ready Then
                Call Unavailable(res, status)
                Return
            End If

            Dim fullName As String = Param(req, "fullname")

            If String.IsNullOrWhiteSpace(fullName) Then
                res.WriteError(HTTP_RFC.RFC_BAD_REQUEST, "missing required parameter 'fullname'.")
                Return
            End If

            Dim symbols As CodeSymbol() = index.FindSymbol(fullName)

            If symbols Is Nothing OrElse symbols.Length = 0 Then
                res.WriteError(HTTP_RFC.RFC_NOT_FOUND, $"symbol '{fullName}' was not found in the workspace.")
                Return
            End If

            res.WriteJSON(New SymbolResponse With {
                .fullName = fullName,
                .count = symbols.Length,
                .symbols = symbols
            })
        End Sub

        ''' <summary>the runtime state of the in memory index.</summary>
        <HttpGet("/api/status")>
        Public Sub Status(req As HttpRequest, res As HttpResponse)
            res.AccessControlAllowOrigin = "*"

            Dim st As IndexStatus = index.Status

            res.WriteJSON(New StatusResponse With {
                .ready = st.ready,
                .phase = st.phase,
                .processedFiles = st.processedFiles,
                .totalFiles = st.totalFiles,
                .symbols = st.symbols,
                .documents = st.documents,
                .errors = st.errors,
                .elapsedMs = st.elapsedMs,
                .message = st.message,
                .progress = st.progress,
                .workspace = Summarize(index.Workspace)
            })
        End Sub

        ''' <summary>the description of the opened read only workspace.</summary>
        <HttpGet("/api/workspace")>
        Public Sub Workspace(req As HttpRequest, res As HttpResponse)
            res.AccessControlAllowOrigin = "*"
            res.WriteJSON(Summarize(index.Workspace))
        End Sub

        Private Shared Sub Unavailable(res As HttpResponse, st As IndexStatus)
            res.WriteError(HTTP_RFC.RFC_SERVICE_UNAVAILABLE,
                $"index is not ready yet ({st.phase}): {st.processedFiles}/{st.totalFiles} files, {st.symbols} symbols.")
        End Sub

        Private Shared Function Summarize(ws As WorkspaceInfo) As WorkspaceSummary
            If ws Is Nothing Then
                Return New WorkspaceSummary With {
                    .kind = "none",
                    .name = "(not opened)"
                }
            End If

            Dim errs As String() = If(ws.Errors.Count > 20,
                ws.Errors.Take(20).ToArray(),
                ws.Errors.ToArray())

            Return New WorkspaceSummary With {
                .kind = ws.KindName,
                .name = ws.Name,
                .rootPath = ws.RootPath,
                .projectCount = ws.Projects.Count,
                .fileCount = ws.Files.Count,
                .projects = ws.Projects.ToArray(),
                .errors = errs
            }
        End Function

        ''' <summary>
        ''' read a request parameter from the url query string, the posted form
        ''' or the posted json object.
        ''' </summary>
        Private Shared Function Param(req As HttpRequest, name As String) As String
            If req IsNot Nothing AndAlso req.URL IsNot Nothing AndAlso req.URL.query IsNot Nothing Then
                For Each kv As KeyValuePair(Of String, String()) In req.URL.query
                    If String.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase) AndAlso
                        kv.Value IsNot Nothing AndAlso kv.Value.Length > 0 AndAlso
                        kv.Value(0) IsNot Nothing Then

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

        Private Shared Function ParseInt(text As String, def As Integer) As Integer
            Dim value As Integer

            If Integer.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, value) Then
                Return value
            End If

            Return def
        End Function

        Private Shared Function ParseDouble(text As String, def As Double) As Double
            Dim value As Double

            If Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, value) Then
                Return value
            End If

            Return def
        End Function

    End Class

End Namespace
