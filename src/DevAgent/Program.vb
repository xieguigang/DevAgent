Imports System.IO
Imports System.Threading.Tasks

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

        ' --- 默认参数 ---
        Dim projectPath As String = Nothing
        Dim requirements As String = Nothing
        Dim model As String = "llama3.2"
        Dim ollamaUrl As String = "http://localhost:11434"
        Dim maxBuildFix As Integer = 8
        Dim maxRunFix As Integer = 5

        ' --- 解析命令行参数 ---
        Dim i As Integer = 0
        Do While i < args.Length
            Select Case args(i).ToLowerInvariant()
                Case "--project", "-p"
                    i += 1
                    If i < args.Length Then projectPath = args(i)

                Case "--requirements", "-r"
                    i += 1
                    If i < args.Length Then requirements = args(i)

                Case "--requirements-file", "-rf"
                    i += 1
                    If i < args.Length Then
                        requirements = File.ReadAllText(args(i))
                    End If

                Case "--model", "-m"
                    i += 1
                    If i < args.Length Then model = args(i)

                Case "--url", "-u"
                    i += 1
                    If i < args.Length Then ollamaUrl = args(i)

                Case "--max-build-fix"
                    i += 1
                    If i < args.Length Then Integer.TryParse(args(i), maxBuildFix)

                Case "--max-run-fix"
                    i += 1
                    If i < args.Length Then Integer.TryParse(args(i), maxRunFix)

                Case "--help", "-h"
                    PrintUsage()
                    Return

                Case Else
                    ' 如果是第一个参数且不是选项，当作 project path
                    If i = 0 AndAlso Not args(i).StartsWith("-") Then
                        projectPath = args(i)
                    ElseIf projectPath IsNot Nothing AndAlso requirements Is Nothing Then
                        ' 第二个非选项参数当作 requirements
                        requirements = args(i)
                    End If
            End Select
            i += 1
        Loop

        ' --- 验证参数 ---
        If String.IsNullOrEmpty(projectPath) OrElse String.IsNullOrEmpty(requirements) Then
            PrintUsage()
            Return
        End If

        ' 打印配置信息
        Console.WriteLine("=== DevAgent Configuration ===")
        Console.WriteLine($"  Project Path: {projectPath}")
        Console.WriteLine($"  Requirements: {requirements.Substring(0, Math.Min(80, requirements.Length))}...")
        Console.WriteLine($"  Model:        {model}")
        Console.WriteLine($"  Ollama URL:   {ollamaUrl}")
        Console.WriteLine($"  Max Build Fix Attempts: {maxBuildFix}")
        Console.WriteLine($"  Max Run Fix Attempts:   {maxRunFix}")
        Console.WriteLine()

        ' --- 创建 Ollama 客户端 ---
        ' 注意: 请根据你的 Ollama 模块的实际构造函数调整以下代码
        Dim ollama As Ollama = Nothing
        Try
            ' 假设 Ollama 构造函数接受 URL 和模型名
            ' 如果你的构造函数不同，请修改此处
            ollama = CreateOllamaClient(ollamaUrl, model)
        Catch ex As Exception
            Console.WriteLine("[ERROR] Failed to create Ollama client: " & ex.Message)
            Console.WriteLine("Please ensure Ollama service is running at: " & ollamaUrl)
            Return
        End Try

        ' --- 创建配置 ---
        Dim options As New DevAgentOptions With {
            .MaxBuildFixAttempts = maxBuildFix,
            .MaxRuntimeFixAttempts = maxRunFix
        }

        ' --- 创建并运行 Agent ---
        Using ollama
            Dim agent As New DevAgent(
                ollama,
                projectPath,
                requirements,
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
    Private Function CreateOllamaClient(url As String, model As String) As Ollama
        ' === 请根据你的 Ollama 模块的实际构造函数调整以下代码 ===
        '
        ' 可能的构造方式：
        ' 1. New Ollama(url, model)
        ' 2. New Ollama()  ' 使用默认配置
        ' 3. New Ollama(model)
        ' 4. Ollama.Create(url, model)
        '
        ' 以下假设构造函数为 New Ollama(url, model)：
        ' ---------------------------------------------------------
        ' Return New Ollama(url, model)
        ' ---------------------------------------------------------
        '
        ' 如果你的构造函数不同，请修改此处。
        ' 作为示例，这里使用反射来兼容不同的构造函数签名：

        Dim ollamaType As Type = GetType(Ollama)

        ' 尝试 (url, model) 构造函数
        Dim ctor2 = ollamaType.GetConstructor({GetType(String), GetType(String)})
        If ctor2 IsNot Nothing Then
            Return DirectCast(ctor2.Invoke({url, model}), Ollama)
        End If

        ' 尝试 (model) 构造函数
        Dim ctor1 = ollamaType.GetConstructor({GetType(String)})
        If ctor1 IsNot Nothing Then
            Return DirectCast(ctor1.Invoke({model}), Ollama)
        End If

        ' 尝试无参构造函数
        Dim ctor0 = ollamaType.GetConstructor({})
        If ctor0 IsNot Nothing Then
            Return DirectCast(ctor0.Invoke({}), Ollama)
        End If

        ' 如果都失败，抛出异常
        Throw New InvalidOperationException(
            "Cannot find a suitable constructor for Ollama class. " &
            "Please modify CreateOllamaClient() in Program.vb to match your Ollama module's API.")
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
