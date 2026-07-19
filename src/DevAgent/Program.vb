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
    Sub Main(args As String())
        Try
            RunAsync(args).GetAwaiter().GetResult()
        Catch ex As Exception
            Console.WriteLine("[FATAL] " & ex.Message)
            Console.WriteLine(ex.StackTrace)
            Environment.Exit(1)
        End Try
    End Sub

    Private Async Function RunAsync(args As String()) As Task
        Dim opt As Opts = CommandLine.BuildFromArguments(args).CreateOpts(Of Opts).ResolveFile

        ' --- 验证参数 ---
        If String.IsNullOrEmpty(opt.projectPath) OrElse
            String.IsNullOrEmpty(opt.requirements) OrElse
            opt.help Then

            PrintUsage()
            Return
        End If

        ' 打印配置信息
        Console.WriteLine("=== DevAgent Configuration ===")
        Console.WriteLine($"  Project Path: {opt.projectPath}")
        Console.WriteLine($"  Requirements: {opt.requirements.Substring(0, Math.Min(80, opt.requirements.Length))}...")
        Console.WriteLine($"  Model:        {opt.model}")
        Console.WriteLine($"  Ollama URL:   {opt.ollamaUrl}")
        Console.WriteLine($"  Max Build Fix Attempts: {opt.maxBuildFix}")
        Console.WriteLine($"  Max Run Fix Attempts:   {opt.maxRunFix}")
        Console.WriteLine()

        ' --- 创建 Ollama 客户端 ---
        ' 注意: 请根据你的 Ollama 模块的实际构造函数调整以下代码
        Dim ollama As LLMClient = Nothing
        Try
            ' 假设 Ollama 构造函数接受 URL 和模型名
            ' 如果你的构造函数不同，请修改此处
            ollama = CreateOllamaClient(opt.ollamaUrl, opt.model, opt.apikey)
        Catch ex As Exception
            Console.WriteLine("[ERROR] Failed to create Ollama client: " & ex.Message)
            Console.WriteLine("Please ensure Ollama service is running at: " & opt.ollamaUrl)
            Return
        End Try

        ' --- 创建配置 ---
        Dim options As New DevAgentOptions With {
            .MaxBuildFixAttempts = opt.maxBuildFix,
            .MaxRuntimeFixAttempts = opt.maxRunFix
        }

        ' --- 创建并运行 Agent ---
        Using ollama
            Dim agent As New DevAgent(
                ollama,
                opt.projectPath,
                opt.requirements,
                options,
                logger:=AddressOf Console.WriteLine)

            Await agent.Run()
        End Using

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
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
    Private Sub PrintUsage()
        Console.WriteLine()
        Console.WriteLine("DevAgent - VB.NET Automated Development Agent")
        Console.WriteLine("Powered by Ollama LLM and .NET 10 SDK")
        Console.WriteLine()
        Console.WriteLine("Usage:")
        Console.WriteLine("  DevAgent --project <path> --requirements <text> [options]")
        Console.WriteLine("  DevAgent --project <path> --requirements-file <file> [options]")
        Console.WriteLine()
        Console.WriteLine("Required:")
        Console.WriteLine("  --project, -p <path>          Project directory path")
        Console.WriteLine("  --requirements, -r <text>     Development requirements (text)")
        Console.WriteLine("  --requirements-file, -rf <f>  Read requirements from file")
        Console.WriteLine()
        Console.WriteLine("Optional:")
        Console.WriteLine("  --model, -m <name>            Ollama model name (default: llama3.2)")
        Console.WriteLine("  --url, -u <url>               Ollama API URL (default: http://localhost:11434)")
        Console.WriteLine("  --key, -k <apikey>            Api key for call the external LLMs services")
        Console.WriteLine("  --max-build-fix <n>           Max build fix attempts (default: 8)")
        Console.WriteLine("  --max-run-fix <n>             Max runtime fix attempts (default: 5)")
        Console.WriteLine("  --help, -h                    Show this help")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  DevAgent -p C:\Projects\Calculator -r ""Build a console calculator app""")
        Console.WriteLine("  DevAgent -p ./mylib -rf requirements.txt --model qwen2.5-coder")
        Console.WriteLine("  DevAgent -p ./webapi -r ""Create a REST API for todo items"" -m deepseek-coder-v2")
        Console.WriteLine()
        Console.WriteLine("Prerequisites:")
        Console.WriteLine("  - Ollama service running locally (ollama serve)")
        Console.WriteLine("  - .NET 10 SDK installed")
        Console.WriteLine("  - git installed and in PATH")
        Console.WriteLine("  - An Ollama model pulled (e.g., ollama pull llama3.2)")
        Console.WriteLine()
    End Sub

End Module
