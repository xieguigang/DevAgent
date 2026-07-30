Imports System.IO
Imports System.Text
Imports Ollama

' ============================================================================
' ReplSession.vb - 交互式 REPL 会话
'
' 当程序以无参数或 --repl 启动时进入。以当前工作目录为工作区，借助 LLMClient
' 原生多轮对话记忆（preserveMemory=True，自动入队与 token 裁剪）进行连续上下文
' 对话式开发辅助。LLM 通过 function calling 自主调用 read_file/list_files/
' file_exists/search_files/get_project_tree/write_file 来探索并修改工作区文件。
'
' ChatRound 内部已将思考与正文流式打印到控制台，本会话不再重复打印响应正文。
' ============================================================================

''' <summary>
''' 交互式 REPL 开发会话。
''' </summary>
Public Class ReplSession

    Private ReadOnly _ollama As LLMClient
    Private ReadOnly _workspace As String
    Private ReadOnly _tools As AgentTools

    ''' <param name="ollama">已配置的 LLMClient（preserveMemory 默认开启）。</param>
    ''' <param name="workspace">工作区绝对路径（当前工作目录）。</param>
    ''' <param name="logger">可选日志回调，用于工具动作反馈。</param>
    Public Sub New(ollama As LLMClient, workspace As String, Optional logger As Action(Of String) = Nothing)
        _ollama = ollama
        _workspace = Path.GetFullPath(workspace)
        _tools = New AgentTools(_workspace, logger)

        ' 注册 LLM 函数工具（含新增的 write_file）
        _ollama.AddFunction(_tools, "read_file")
        _ollama.AddFunction(_tools, "list_files")
        _ollama.AddFunction(_tools, "file_exists")
        _ollama.AddFunction(_tools, "search_files")
        _ollama.AddFunction(_tools, "get_project_tree")
        _ollama.AddFunction(_tools, "write_file")

        ' 设置系统提示
        _ollama.AddSystemPrompt(BuildSystemPrompt())
    End Sub

    ''' <summary>
    ''' 启动 REPL 主循环，直到用户输入 /exit 或 EOF。
    ''' </summary>
    Public Async Function Run() As Task
        PrintWelcome()

        Do
            Console.WriteLine()
            Console.Write(">>> ")
            Dim input As String = Console.ReadLine()

            ' EOF（Ctrl+Z）
            If input Is Nothing Then
                Console.WriteLine()
                Exit Do
            Else
                input = input.Trim()
            End If

            If input.Length = 0 Then
                Continue Do
            End If

            ' 斜杠命令
            If input.StartsWith("/"c) Then
                If Await HandleSlashCommand(input) Then
                    Exit Do
                End If

                Continue Do
            End If

            ' 普通对话：交给 LLM（流式自动打印）
            Try
                Await _ollama.Chat(input)
            Catch ex As Exception
                Call App.LogException(ex)

                Console.WriteLine()
                Console.WriteLine("[ERROR] " & ex.Message)
            End Try
        Loop

        Console.WriteLine("Bye Bye!")
    End Function

    ' ========================================================================
    ' 斜杠命令
    ' ========================================================================

    ''' <returns>True 表示请求退出会话。</returns>
    Private Function HandleSlashCommand(input As String) As Task(Of Boolean)
        Dim cmd As String = input.ToLowerInvariant()
        Dim parts() As String = cmd.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
        Dim name As String = parts(0)

        Select Case name
            Case "/exit", "/quit"
                Return Task.FromResult(True)

            Case "/clear", "/reset"
                _ollama.Clear()
                Console.WriteLine("(conversation context cleared)")

            Case "/cwd"
                Console.WriteLine(_workspace)

            Case "/tree"
                Dim dirname As String = parts.Skip(1).JoinBy("/")
                dirname = If(dirname = "", "/", dirname)
                Console.WriteLine(_tools.fs_tree(dirname))

            Case "/help", "/?"
                PrintHelp()

            Case Else
                Console.WriteLine($"Unknown command: {input}  (try /help)")
        End Select

        Return Task.FromResult(False)
    End Function

    ' ========================================================================
    ' 输出
    ' ========================================================================

    Private Sub PrintWelcome()
        Console.WriteLine("========================================")
        Console.WriteLine("  DevAgent REPL - Interactive Dev Mode")
        Console.WriteLine("========================================")
        Console.WriteLine($"  Workspace: {_workspace}")
        Console.WriteLine()
        Console.WriteLine("  Type your development request in natural-language and press Enter.")
        Console.WriteLine("  /help for commands, /exit to quit.")
        Console.WriteLine("----------------------------------------")
    End Sub

    Private Sub PrintHelp()
        Console.WriteLine("Commands:")
        Console.WriteLine("  /exit, /quit   Exit the REPL")
        Console.WriteLine("  /clear, /reset Clear conversation context")
        Console.WriteLine("  /cwd           Show current workspace path")
        Console.WriteLine("  /tree [/]      Show workspace file tree, default list the file tree of the workspace root.")
        Console.WriteLine("  /help, /?      Show this help")
        Console.WriteLine()
        Console.WriteLine("Otherwise: type a natural-language request and press Enter.")
        Console.WriteLine("The agent can read, search and write files in the workspace via tools.")
    End Sub

    ' ========================================================================
    ' 系统提示
    ' ========================================================================

    Private Function BuildSystemPrompt() As String
        Dim sb As New StringBuilder()
        sb.AppendLine("You are DevAgent, an interactive VB.NET / .NET 10 development assistant running in REPL mode.")
        sb.AppendLine("You help the user develop their project in the current workspace.")
        sb.AppendLine()
        sb.AppendLine($"Workspace root: {_workspace}")
        sb.AppendLine()
        sb.AppendLine("You have access to these function tools:")
        sb.AppendLine("- read_file(path): Read a file's full text content")
        sb.AppendLine("- list_files(path): List files and subdirectories in a directory ('.' for root)")
        sb.AppendLine("- file_exists(path): Check if a file exists")
        sb.AppendLine("- search_files(pattern, extension): Search text across workspace files (case-insensitive)")
        sb.AppendLine("- get_project_tree(): Get the workspace file tree")
        sb.AppendLine("- write_file(path, content): Write/overwrite a file in the workspace (creates parent dirs; path must be inside the workspace)")
        sb.AppendLine()
        sb.AppendLine("Guidelines:")
        sb.AppendLine("- Explore the workspace (list_files, get_project_tree, read_file) before proposing changes.")
        sb.AppendLine("- When the user asks for code/file changes, USE write_file to apply them directly. Do not just paste code in chat unless the user only wants an explanation.")
        sb.AppendLine("- When using write_file, always output the COMPLETE file content.")
        sb.AppendLine("- Use VB.NET .NET 10 syntax, PascalCase for public members, and add XML comments for public members.")
        sb.AppendLine("- Paths are relative to the workspace root. Never write outside the workspace.")
        sb.AppendLine("- Be concise: briefly explain your reasoning, then act with tools.")
        sb.AppendLine("- The conversation is multi-turn; you can remember previous context.")
        Return sb.ToString()
    End Function

End Class
