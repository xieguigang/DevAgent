Imports System.Globalization
Imports System.Threading
Imports Flute.Http.Configurations
Imports Flute.Http.Core
Imports Flute.Http.Core.Message

' ============================================================================
' CodeMapSkill - the vb.net code index of CodeMap, exposed as an agent skill.
'
' usage:
'   CodeMapSkill [--workspace <path>] [--port 8080] [--q 3] [--top 20]
'                [--threshold 0.15] [--max-lines 200] [--max-workspaces 4]
'                [--silent] [--open <path>]
'
' the service speaks two protocols over the very same tool set:
'   * REST   GET/POST /api/...    (see /tools.json)
'   * MCP    POST /mcp            (json-rpc 2.0: initialize/tools/list/tools/call)
'
' and it serves its own skill definition:
'   * GET /skill/SKILL.md
'   * GET /skill/manifest.yaml
'   * GET /tools.json
'
' example:
'   CodeMapSkill --workspace "G:\DevAgent\src\DevAgent.slnx" --port 8080
' ============================================================================
Module Program

    <STAThread()>
    Public Function Main(args As String()) As Integer
        Dim opts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim flags As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim extra As New List(Of String)()

        Call ParseArgs(args, opts, flags, extra)

        If flags.Contains("help") OrElse flags.Contains("h") OrElse flags.Contains("?") Then
            Call Usage()
            Return 0
        End If

        Dim port As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "port", "p"), 8080)))
        Dim qgram As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "q", "qgram"), 3)))
        Dim top As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "top", "count"), 20)))
        Dim threshold As Double = ParseNumber(GetValue(opts, "threshold", "thr"), 0.15)
        Dim maxLines As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "max-lines", "maxlines"), 200)))
        Dim maxWorkspaces As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "max-workspaces", "maxws"), 4)))
        Dim maxCodeChars As Integer = CInt(System.Math.Round(ParseNumber(GetValue(opts, "max-chars", "maxchars"), 1200)))
        Dim silent As Boolean = flags.Contains("silent")

        If port <= 0 OrElse port > 65535 Then
            Console.Error.WriteLine($"[CodeMapSkill] invalid port: {port}")
            Return 1
        End If

        Dim pool As New WorkspacePool(maxWorkspaces:=If(maxWorkspaces <= 0, 0, maxWorkspaces),
                                      q:=If(qgram <= 0, 3, qgram),
                                      maxCodeLines:=If(maxLines <= 0, 200, maxLines))

        ' the workspace given on the command line becomes the default one
        For Each wsPath As String In WorkspacePaths(opts, extra)
            Try
                Call pool.Open(wsPath)
            Catch ex As Exception
                Console.Error.WriteLine($"[CodeMapSkill] cannot open '{wsPath}': {ex.Message}")
            End Try
        Next

        Dim tools As New SkillTools(pool,
                                    maxCodeChars:=If(maxCodeChars <= 0, 1200, maxCodeChars),
                                    defaultTop:=If(top <= 0, 20, top),
                                    defaultThreshold:=If(threshold < 0, 0.15, threshold))
        Dim router As New HttpRouter()

        Call router.RegisterController(New SkillController(tools, pool, port))
        Call router.RegisterController(New McpController(tools, port))
        Call router.RegisterController(New SkillDocumentController(tools, port))

        Dim settings As New Configuration With {
            .silent = silent,
            .shutdown_token = ""
        }
        Dim server As HttpSocket

        Try
            server = New HttpSocket(router, port, configs:=settings)
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMapSkill] cannot listen on port {port}: {ex.Message}")
            Return 4
        End Try

        AddHandler Console.CancelKeyPress,
            Sub(sender As Object, e As ConsoleCancelEventArgs)
                e.Cancel = True
                Console.WriteLine("[CodeMapSkill] shutting down ...")
                Call server.Shutdown()
            End Sub

        Console.WriteLine()
        Console.WriteLine($"[CodeMapSkill] listening on http://localhost:{port}/")
        Console.WriteLine($"[CodeMapSkill]   skill    : http://localhost:{port}/skill/SKILL.md")
        Console.WriteLine($"[CodeMapSkill]   tools    : http://localhost:{port}/tools.json")
        Console.WriteLine($"[CodeMapSkill]   mcp      : POST http://localhost:{port}/mcp")
        Console.WriteLine($"[CodeMapSkill]   query    : http://localhost:{port}/api/query?q=QGram&top={top}")
        Console.WriteLine($"[CodeMapSkill]   status   : http://localhost:{port}/api/status")
        Console.WriteLine($"[CodeMapSkill]   workspaces: http://localhost:{port}/api/workspaces")
        Console.WriteLine("[CodeMapSkill] press CTRL+C to stop the service.")

        Dim code As Integer = server.Run()

        Console.WriteLine($"[CodeMapSkill] service stopped ({code}).")

        Return 0
    End Function

    ''' <summary>
    ''' every workspace path given on the command line: the first one is the
    ''' default workspace of the pool.
    ''' </summary>
    Private Function WorkspacePaths(opts As Dictionary(Of String, String), extra As List(Of String)) As List(Of String)
        Dim paths As New List(Of String)()

        For Each key As String In New String() {"workspace", "w", "open"}
            Dim value As String = Nothing

            If opts.TryGetValue(key, value) AndAlso Not String.IsNullOrWhiteSpace(value) Then
                ' --workspace may be repeated; the parser keeps the last one,
                ' so a comma separated list is accepted as well
                For Each part As String In value.Split(","c)
                    If Not String.IsNullOrWhiteSpace(part) Then
                        Call paths.Add(part.Trim())
                    End If
                Next
            End If
        Next

        For Each bare As String In extra
            If Not String.IsNullOrWhiteSpace(bare) Then
                Call paths.Add(bare)
            End If
        Next

        Return paths
    End Function

    Private Sub ParseArgs(args As String(),
                          opts As Dictionary(Of String, String),
                          flags As HashSet(Of String),
                          extra As List(Of String))

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
                    ' a repeated --workspace becomes a comma separated list
                    If (key = "workspace" OrElse key = "w" OrElse key = "open") AndAlso opts.ContainsKey(key) Then
                        opts(key) = opts(key) & "," & args(i + 1)
                    Else
                        opts(key) = args(i + 1)
                    End If

                    i += 2
                Else
                    Call flags.Add(key)
                    i += 1
                End If
            Else
                ' a bare value is treated as a workspace path
                Call extra.Add(arg)
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

        If Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, value) Then
            Return value
        End If

        Return def
    End Function

    Private Sub Usage()
        Console.WriteLine("CodeMapSkill - the vb.net code index of CodeMap, exposed as an agent skill")
        Console.WriteLine()
        Console.WriteLine("  CodeMapSkill [--workspace <path>] [options]")
        Console.WriteLine()
        Console.WriteLine("  --workspace, -w   the repository to open on startup (.sln/.slnx/.vbproj or a folder);")
        Console.WriteLine("                    repeat the option (or use --open) to open several repositories")
        Console.WriteLine("  --port, -p        the tcp port of the http service, default 8080")
        Console.WriteLine("  --q               the q-gram width of the full text index, default 3")
        Console.WriteLine("  --top             the default number of returned hits, default 20")
        Console.WriteLine("  --threshold       the default minimum similarity of a matched word, default 0.15")
        Console.WriteLine("  --max-lines       the max number of source lines kept per symbol, default 200")
        Console.WriteLine("  --max-chars       the max number of source characters returned per symbol, default 1200")
        Console.WriteLine("  --max-workspaces  the max number of workspaces kept in memory, default 4 (0 = no limit)")
        Console.WriteLine("  --silent          turn off the verbose http server log")
        Console.WriteLine("  --help            show this message")
        Console.WriteLine()
        Console.WriteLine("skill endpoints:")
        Console.WriteLine("  GET  /skill/SKILL.md            the skill definition")
        Console.WriteLine("  GET  /skill/manifest.yaml       the skill manifest")
        Console.WriteLine("  GET  /skill/scripts/*.ps1       the powershell helpers")
        Console.WriteLine("  GET  /tools.json                the tool catalogue")
        Console.WriteLine()
        Console.WriteLine("rest endpoints:")
        Console.WriteLine("  GET    /api/status?ws=          index build progress")
        Console.WriteLine("  GET    /api/workspaces          the open workspaces")
        Console.WriteLine("  GET/POST /api/open?path=        open a repository")
        Console.WriteLine("  GET    /api/workspace?ws=       the workspace summary")
        Console.WriteLine("  GET/POST /api/query?q=&top=&ws= fuzzy symbol search")
        Console.WriteLine("  GET    /api/symbol?fullname=&ws=")
        Console.WriteLine("  GET    /api/members?fullname=&ws=")
        Console.WriteLine("  GET    /api/files?project=&pattern=&ws=")
        Console.WriteLine("  GET/POST /api/source?file=&start=&end=&ws=")
        Console.WriteLine()
        Console.WriteLine("mcp endpoint:")
        Console.WriteLine("  POST   /mcp                     json-rpc 2.0: initialize, tools/list, tools/call, ping")
    End Sub

End Module
