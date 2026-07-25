Imports Microsoft.VisualBasic.ApplicationServices.Debugging.Logging
Imports Microsoft.VisualBasic.CommandLine
Imports Ollama

' ============================================================================
' Program.vb - DevAgent 控制台入口
'
' 用法:
'   DevAgent --project <path> --requirements <text> [options]
'   DevAgent --project <path> --requirements-file <file> [options]
'
' 示例:
'   DevAgent -p C:\Projects\MyApp -r "Create a todo list console app"
'   DevAgent -p ./mylib -rf requirements.txt --model qwen2.5-coder
'
' 注意:
'   - Ollama 构造函数请根据你的模块 API 调整
'   - ArgumentAttribute 和 OllamaResponse 等类型来自你的 Ollama 模块
' ============================================================================

Module Program

    ''' <summary>
    ''' 程序入口。
    ''' 使用 .GetAwaiter().GetResult() 避免异步 Main 的兼容性问题。
    ''' </summary>
    Public Function Main(args As String()) As Integer
        Try
            Return RunAsync(args).GetAwaiter().GetResult()
        Catch ex As Exception
            Console.WriteLine("[FATAL] " & ex.Message)
            Console.WriteLine(ex.StackTrace)
            Environment.Exit(1)
        End Try

        Return 1
    End Function

    Private Async Function RunAsync(args As String()) As Task(Of Integer)
        Dim opt As Opts = CommandLine.BuildFromArguments(args, NoSubCommand:=True).CreateOpts(Of Opts).ResolveFile

        ' --- --help 优先：直接打印用法，不做 INI 副作用 ---
        If opt.help Then
            Return PrintUsage()
        End If

        ' --- 加载配置（CLI > INI > 内置默认）---
        Dim config As AppConfig = AppConfig.Load(args, opt)

        ' --- REPL 模式：无参数或 --repl ---
        Dim isRepl As Boolean = args.Length = 0 OrElse opt.repl
        If isRepl Then
            Return Await RunRepl(config)
        End If

        ' --- 验证 CLI 参数 ---
        If String.IsNullOrEmpty(opt.projectPath) OrElse
            String.IsNullOrEmpty(opt.requirements) Then

            Return PrintUsage()
        End If

        ' 打印配置信息
        Console.WriteLine("=== DevAgent Configuration ===")
        Console.WriteLine($"  Project Path: {opt.projectPath}")
        Console.WriteLine($"  Requirements: {opt.requirements.Substring(0, Math.Min(80, opt.requirements.Length))}...")
        Console.WriteLine($"  INI:          {config.IniPath} ({If(config.IniExists, "loaded", "not found")})")
        Console.WriteLine(config.SourceBanner)
        Console.WriteLine()

        ' --- 创建 Ollama 客户端 ---
        Dim ollama As LLMClient = Nothing
        Try
            ollama = CreateOllamaClient(config.Url, config.Model, config.ApiKey)
        Catch ex As Exception
            Console.WriteLine("[ERROR] Failed to create Ollama client: " & ex.Message)
            Console.WriteLine("Please ensure Ollama service is running at: " & config.Url)
            Return 2
        End Try

        ' --- 创建配置 ---
        Dim options As New DevAgentOptions With {
            .MaxBuildFixAttempts = config.MaxBuildFix,
            .MaxRuntimeFixAttempts = config.MaxRunFix
        }
        Dim logger As Action(Of String) = AddressOf Console.WriteLine

        If Not opt.logfile.StringEmpty Then
            logger = AddressOf LogFile _
                .Open(opt.logfile, split:=Sub(id, s, level) Console.WriteLine($"[{level.Description}] {s}")) _
                .WriteLine
        End If

        ' --- 创建并运行 Agent ---
        Using ollama
            Dim agent As New DevAgent(
                ollama,
                opt.projectPath,
                opt.requirements,
                options,
                logger:=logger)

            Await agent.Run()
        End Using

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()

        Return 0
    End Function

    ''' <summary>
    ''' 启动 REPL 交互模式。工作区为当前工作目录。
    ''' </summary>
    Private Async Function RunRepl(config As AppConfig) As Task(Of Integer)
        Console.WriteLine("=== DevAgent REPL Configuration ===")
        Console.WriteLine($"  INI:          {config.IniPath} ({If(config.IniExists, "loaded", "not found")})")
        Console.WriteLine(config.SourceBanner)
        Console.WriteLine()

        Dim ollama As LLMClient = Nothing
        Try
            ollama = CreateOllamaClient(config.Url, config.Model, config.ApiKey)
        Catch ex As Exception
            Console.WriteLine("[ERROR] Failed to create Ollama client: " & ex.Message)
            Console.WriteLine("Please ensure LLM service is running at: " & config.Url)
            Return 3
        End Try

        Dim logger As Action(Of String) = AddressOf Console.WriteLine
        Dim workspace As String = Environment.CurrentDirectory

        Using ollama
            Dim session As New ReplSession(ollama, workspace, logger)
            Await session.Run()
        End Using

        Return 0
    End Function

    ''' <summary>
    ''' 创建 Ollama 客户端实例。
    ''' 请根据你的 Ollama 模块的实际 API 修改此方法。
    ''' </summary>
    Private Function CreateOllamaClient(url As String, model As String, apikey As String) As LLMClient
        Dim server As ILLMProvider = LLMUrl.Create(url, apikey)
        Dim llms As New LLMClient(server, model)

        Return llms
    End Function

    ''' <summary>
    ''' 打印使用说明。
    ''' </summary>
    Private Function PrintUsage() As Integer
        Console.WriteLine()
        Console.WriteLine("DevAgent - VB.NET Automated Development Agent")
        Console.WriteLine("Powered by Ollama LLM And .NET 10 SDK")
        Console.WriteLine()
        Console.WriteLine("Usage:")
        Console.WriteLine("  DevAgent --project <path> --requirements <text> [options]")
        Console.WriteLine("  DevAgent --project <path> --requirements-file <file> [options]")
        Console.WriteLine("  DevAgent [--repl]             Start interactive REPL (workspace = current dir)")
        Console.WriteLine()
        Console.WriteLine("Required (CLI mode):")
        Console.WriteLine("  --project, -p <path>          Project directory path")
        Console.WriteLine("  --requirements, -r <text>     Development requirements (text)")
        Console.WriteLine("  --requirements-file, -rf <f>  Read requirements from file")
        Console.WriteLine()
        Console.WriteLine("Optional:")
        Console.WriteLine("  --repl                        Start interactive REPL mode (also default when no args)")
        Console.WriteLine("  --config, -c <file>           Path to INI config file (default: <exe>/devagent.ini)")
        Console.WriteLine("  --model, -m <name>            Ollama model name (default: llama3.2)")
        Console.WriteLine("  --url, -u <url>               Ollama API URL (default: http://localhost:11434)")
        Console.WriteLine("  --key, -k <apikey>            Api key for call the external LLMs services")
        Console.WriteLine("  --max-build-fix <n>           Max build fix attempts (default: 8)")
        Console.WriteLine("  --max-run-fix <n>             Max runtime fix attempts (default: 5)")
        Console.WriteLine("  --help, -h                    Show this help")
        Console.WriteLine()
        Console.WriteLine("Configuration (INI):")
        Console.WriteLine("  Optional params (model, url, key, max-build-fix, max-run-fix) can be set in")
        Console.WriteLine("  an INI file under the [devagent] section. A template is auto-generated on first run.")
        Console.WriteLine("  Priority: command-line args > INI > built-in defaults.")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  DevAgent -p C:\Projects\Calculator -r ""Build a console calculator app""")
        Console.WriteLine("  DevAgent -p ./mylib -rf requirements.txt --model qwen2.5-coder")
        Console.WriteLine("  DevAgent -p ./webapi -r ""Create a REST API for todo items"" -m deepseek-coder-v2")
        Console.WriteLine("  DevAgent --repl                       Interactive REPL in current directory")
        Console.WriteLine("  DevAgent --config D:\cfg.ini --repl   REPL with a custom INI config")
        Console.WriteLine()
        Console.WriteLine("Prerequisites:")
        Console.WriteLine("  - Ollama service running locally (ollama serve)")
        Console.WriteLine("  - .NET 10 SDK installed")
        Console.WriteLine("  - git installed and in PATH")
        Console.WriteLine("  - api key for openai LLMs will be read from file '<mydocument>/.openai.key' by default if apikey parameter is missing.")
        Console.WriteLine("  - An Ollama model pulled (e.g., ollama pull llama3.2)")
        Console.WriteLine()

        Return 0
    End Function

End Module
