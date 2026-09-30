Imports System.Threading
Imports CodeMap.CodeIndex
Imports CodeMap.HttpService
Imports Flute.Http.Configurations
Imports Flute.Http.Core

' ============================================================================
' CodeMap - a read only code map / symbol index http service.
'
' usage:
'   CodeMap --workspace <path> [--port 8080] [--q 3] [--top 20]
'           [--threshold 0.15] [--max-lines 200] [--silent]
'
' <path> may point to
'   * a visual studio solution file (.sln / .slnx)
'   * a visual basic project file (.vbproj)
'   * a plain folder (usually a git repository working directory); when the
'     folder contains a solution file that solution is used instead.
'
' example:
'   CodeMap --workspace "G:\GCModeller\src\runtime\sciBASIC#\nuget.slnx" --port 8080
' ============================================================================
Module Program

    Public Function Main(args As String()) As Integer
        Dim opts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim flags As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Call ParseArgs(args, opts, flags)

        If flags.Contains("help") OrElse flags.Contains("h") OrElse flags.Contains("?") Then
            Call Usage()
            Return 0
        End If

        Dim workspacePath As String = GetValue(opts, "workspace", "w")

        If String.IsNullOrWhiteSpace(workspacePath) Then
            Call Console.Error.WriteLine("[CodeMap] a --workspace argument is required." & vbCrLf)
            Call Usage()
            Return 1
        End If

        Dim port As Integer = CInt(Math.Round(ParseNumber(GetValue(opts, "port", "p"), 8080)))
        Dim qgram As Integer = CInt(Math.Round(ParseNumber(GetValue(opts, "q", "qgram"), 3)))
        Dim top As Integer = CInt(Math.Round(ParseNumber(GetValue(opts, "top", "count"), 20)))
        Dim threshold As Double = ParseNumber(GetValue(opts, "threshold", "thr"), 0.15)
        Dim maxLines As Integer = CInt(Math.Round(ParseNumber(GetValue(opts, "max-lines", "maxlines"), 200)))
        Dim silent As Boolean = flags.Contains("silent")

        If port <= 0 OrElse port > 65535 Then
            Call Console.Error.WriteLine($"[CodeMap] invalid port: {port}")
            Return 1
        End If

        If Not IO.Directory.Exists(workspacePath) AndAlso Not IO.File.Exists(workspacePath) Then
            Console.Error.WriteLine($"[CodeMap] workspace path does not exist: {workspacePath}")
            Return 2
        End If

        Dim index As New CodeMapIndex(q:=qgram, maxCodeLines:=maxLines)

        ' both the workspace parsing and the index building run on a background
        ' thread so that the http service starts listening right away; queries
        ' are answered with HTTP 503 until the index is ready.
        Dim worker As New Thread(Sub()
                                     Dim ws As WorkspaceInfo = Nothing

                                     Try
                                         Console.WriteLine($"[CodeMap] opening workspace (read only): {workspacePath}")
                                         ws = WorkspaceLoader.Open(workspacePath)
                                     Catch ex As Exception
                                         Console.Error.WriteLine($"[CodeMap] cannot open workspace: {ex.Message}")
                                         Call index.MarkFailed(ex.Message)
                                         Return
                                     End Try

                                     Console.WriteLine($"[CodeMap] workspace  : {ws.Name} ({ws.KindName})")
                                     Console.WriteLine($"[CodeMap] root       : {ws.RootPath}")
                                     Console.WriteLine($"[CodeMap] projects   : {ws.Projects.Count}")
                                     Console.WriteLine($"[CodeMap] source file: {ws.Files.Count}")

                                     If ws.Errors.Count > 0 Then
                                         Console.WriteLine($"[CodeMap] warnings   : {ws.Errors.Count} (see /api/workspace)")

                                         For Each err As String In ws.Errors.Take(5)
                                             Console.WriteLine($"            ! {err}")
                                         Next
                                     End If

                                     If ws.Files.Count = 0 Then
                                         Call index.MarkFailed("no vb.net source file found in the given workspace.")
                                         Return
                                     End If

                                     Try
                                         Call index.Build(ws)
                                         Console.WriteLine($"[CodeMap] index ready: {index.Status.symbols} symbols, {index.Status.elapsedMs} ms")
                                     Catch ex As Exception
                                         Console.Error.WriteLine($"[CodeMap] index build failed: {ex.Message}")
                                         Call index.MarkFailed(ex.Message)
                                     End Try
                                 End Sub) With {
            .IsBackground = True,
            .Name = "codemap-index"
        }

        Call worker.Start()

        Dim controller As New CodeMapController(index, port) With {
            .DefaultTop = top,
            .DefaultThreshold = threshold
        }
        Dim router As New HttpRouter(controller)
        Dim settings As New Configuration With {
            .silent = silent,
            .shutdown_token = ""
        }
        Dim server As HttpSocket

        Try
            server = New HttpSocket(router, port, configs:=settings)
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMap] cannot listen on port {port}: {ex.Message}")
            Return 4
        End Try

        AddHandler Console.CancelKeyPress,
            Sub(sender As Object, e As ConsoleCancelEventArgs)
                e.Cancel = True
                Console.WriteLine("[CodeMap] shutting down ...")
                Call server.Shutdown()
            End Sub

        Console.WriteLine()
        Console.WriteLine($"[CodeMap] listening on http://localhost:{port}/")
        Console.WriteLine($"[CodeMap]   query  : http://localhost:{port}/api/query?q=QGram&top={top}")
        Console.WriteLine($"[CodeMap]   symbol : http://localhost:{port}/api/symbol?fullname=<namespace>.<type>")
        Console.WriteLine($"[CodeMap]   status : http://localhost:{port}/api/status")
        Console.WriteLine("[CodeMap] press CTRL+C to stop the service.")

        Dim code As Integer = server.Run()

        Console.WriteLine($"[CodeMap] service stopped ({code}).")

        Return 0
    End Function

    Private Sub ParseArgs(args As String(),
                          opts As Dictionary(Of String, String),
                          flags As HashSet(Of String))

        If args Is Nothing Then
            Return
        End If

        Dim i As Integer = 0

        While i < args.Length
            Dim arg As String = args(i)

            If arg Is Nothing OrElse arg.Length = 0 Then
                i += 1
                Continue While
            End If

            If arg.StartsWith("-") Then
                Dim key As String = arg.TrimStart("-"c).ToLowerInvariant()

                If i + 1 < args.Length AndAlso Not args(i + 1).StartsWith("-") Then
                    opts(key) = args(i + 1)
                    i += 2
                Else
                    flags.Add(key)
                    i += 1
                End If
            Else
                ' a bare value is treated as the workspace path
                If Not opts.ContainsKey("workspace") Then
                    opts("workspace") = arg
                End If

                i += 1
            End If
        End While
    End Sub

    Private Function GetValue(opts As Dictionary(Of String, String), ParamArray names As String()) As String
        For Each name As String In names
            Dim value As String = Nothing

            If opts.TryGetValue(name, value) AndAlso Not String.IsNullOrWhiteSpace(value) Then
                Return value
            End If
        Next

        Return Nothing
    End Function

    Private Function ParseNumber(text As String, def As Double) As Double
        Dim value As Double

        If Double.TryParse(text,
                           Globalization.NumberStyles.Float,
                           Globalization.CultureInfo.InvariantCulture,
                           value) Then
            Return value
        End If

        Return def
    End Function

    Private Sub Usage()
        Console.WriteLine("CodeMap - read only code map / symbol index http service")
        Console.WriteLine()
        Console.WriteLine("  CodeMap --workspace <path> [options]")
        Console.WriteLine()
        Console.WriteLine("  --workspace, -w   the code repository to open (.sln/.slnx/.vbproj or a folder)")
        Console.WriteLine("  --port, -p        the tcp port of the http service, default 8080")
        Console.WriteLine("  --q               the q-gram width of the full text index, default 3")
        Console.WriteLine("  --top             the default number of returned hits, default 20")
        Console.WriteLine("  --threshold       the default minimum similarity of a matched word, default 0.15")
        Console.WriteLine("  --max-lines       the max number of source lines kept per symbol, default 200")
        Console.WriteLine("  --silent          turn off the verbose http server log")
        Console.WriteLine("  --help            show this message")
        Console.WriteLine()
        Console.WriteLine("http endpoints:")
        Console.WriteLine("  GET  /                       the built in query console")
        Console.WriteLine("  GET  /api/query?q=&top=&threshold=")
        Console.WriteLine("  POST /api/query              same, with a form or json body")
        Console.WriteLine("  GET  /api/symbol?fullname=   resolve a symbol definition")
        Console.WriteLine("  GET  /api/status             index build progress")
        Console.WriteLine("  GET  /api/workspace          the opened workspace summary")
    End Sub

End Module
