Imports System.Globalization
Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.Language
Imports Ollama

' ============================================================================
' DevAgent.vb - 自动化开发 Agent
'
' 基于 Ollama LLM 和 .NET 10 SDK，实现自动化的 VB.NET 项目开发流程。
'
' 工作流：
'   1. 验证输入和工具链
'   2. 通过 LLM 创建开发计划
'   3. 创建 git 分支
'   4. 创建或继续 VB.NET 项目
'   5. 逐个执行开发任务，每个任务完成后 git commit
'   6. 编译项目，修复编译错误（循环直到通过）
'   7. git commit
'   8. 运行测试（console: 直接运行; library: 创建测试项目）
'   9. 最终 git commit
'
' 依赖：
'   - Ollama 客户端模块（用户的自定义模块）
'   - .NET 10 SDK
'   - git
' ============================================================================

''' <summary>
''' 自动化开发 Agent。
''' 通过 LLM 驱动 VB.NET 项目的创建、编码、编译、测试全流程。
''' </summary>
Public Class DevAgent

    ' --- 字段 ---

    Private ReadOnly _ollama As LLMClient
    Private ReadOnly _projectPath As String
    Private ReadOnly _requirements As String
    Private ReadOnly _options As DevAgentOptions
    Private ReadOnly _log As Action(Of String)

    Private _projectName As String
    Private _projectType As String        ' "console" 或 "library"
    Private _gitBranch As String
    Private _plan As New List(Of String)

    ' --- 系统提示词 ---

    Private ReadOnly SystemPrompt As String =
        "You are a senior VB.NET software engineer working with .NET 10." & Environment.NewLine &
        "You write clean, idiomatic, well-commented VB.NET code." & Environment.NewLine &
        Environment.NewLine &
        "When asked to write or modify code, output each file using this EXACT format:" & Environment.NewLine &
        "### FILE: relative/path/to/file.vb" & Environment.NewLine &
        "```vb.net" & Environment.NewLine &
        "' your code here" & Environment.NewLine &
        "```" & Environment.NewLine &
        Environment.NewLine &
        "You have access to these function tools:" & Environment.NewLine &
        "- read_file(path): Read a file from the project" & Environment.NewLine &
        "- list_files(path): List files and directories" & Environment.NewLine &
        "- file_exists(path): Check if a file exists" & Environment.NewLine &
        "- search_files(pattern, extension): Search for text in project files" & Environment.NewLine &
        "- get_project_tree(): Get the project file tree" & Environment.NewLine &
        Environment.NewLine &
        "Use these tools to explore the project structure and read existing code before writing." & Environment.NewLine &
        "Rules for code output:" & Environment.NewLine &
        "- Output the COMPLETE file content, not just the changes" & Environment.NewLine &
        "- Include all necessary Imports statements" & Environment.NewLine &
        "- Use proper VB.NET .NET 10 syntax" & Environment.NewLine &
        "- Follow VB.NET naming conventions (PascalCase for public members)" & Environment.NewLine &
        "- Add XML comments for public members" & Environment.NewLine &
        "- Each ### FILE: block must contain exactly one file"

    ' --- 构造函数 ---

    ''' <summary>
    ''' 创建 DevAgent 实例。
    ''' </summary>
    ''' <param name="ollama">已配置的 Ollama 客户端实例。</param>
    ''' <param name="projectPath">项目目录路径。</param>
    ''' <param name="requirements">开发需求描述。</param>
    ''' <param name="options">配置选项，为 Nothing 时使用默认值。</param>
    ''' <param name="logger">日志输出回调，为 Nothing 时使用 Console.WriteLine。</param>
    Public Sub New(
        ollama As LLMClient,
        projectPath As String,
        requirements As String,
        Optional options As DevAgentOptions = Nothing,
        Optional logger As Action(Of String) = Nothing
    )
        _ollama = ollama
        _projectPath = Path.GetFullPath(projectPath)
        _requirements = requirements
        _options = If(options, New DevAgentOptions())
        _log = If(logger, AddressOf Console.WriteLine)

        ' 提取项目名
        _projectName = SanitizeProjectName(New DirectoryInfo(_projectPath).Name)

        ' 注册 LLM 函数工具
        Dim tools As New AgentTools(_projectPath)
        _ollama.AddFunction(tools, "read_file")
        _ollama.AddFunction(tools, "list_files")
        _ollama.AddFunction(tools, "file_exists")
        _ollama.AddFunction(tools, "search_files")
        _ollama.AddFunction(tools, "get_project_tree")
    End Sub

    ' ========================================================================
    ' 主工作流
    ' ========================================================================

    ''' <summary>
    ''' 运行完整的开发工作流。
    ''' </summary>
    Public Async Function Run() As Task
        Log("")
        Log("========================================")
        Log("       DevAgent Started")
        Log("========================================")
        Log($"Project Path: {_projectPath}")
        Log($"Requirements: {_requirements}")
        Log("")

        Try
            Await RunAgentWorkflow()
        Catch ex As Exception
            Log("")
            Log("[FATAL] Agent stopped due to error:")
            Log("  " & ex.Message)
            If ex.StackTrace IsNot Nothing Then
                Log(ex.StackTrace)
            End If
            Log("========================================")
            Log("       DevAgent Stopped with Errors")
            Log("========================================")
        End Try
    End Function

    Private Async Function RunAgentWorkflow() As Task
        ' Step 1: 验证输入
        LogStep(1, "Validating inputs and toolchain")
        ValidateInputs()

        ' Step 2: 创建开发计划
        LogStep(2, "Creating development plan via LLM")
        Await CreateDevelopmentPlan()

        ' Step 3: 创建 git 分支
        LogStep(3, "Creating git branch")
        CreateGitBranch()

        ' Step 4: 设置项目
        LogStep(4, "Setting up VB.NET project")
        Await SetupProject()

        ' Step 5: 逐个执行开发任务
        For i As Integer = 0 To _plan.Count - 1
            LogStep(5, $"Task {i + 1}/{_plan.Count}: {_plan(i)}")
            Await ExecuteTask(i)

            ' 每个任务完成后 git commit
            GitCommit($"Task {i + 1}/{_plan.Count}: {_plan(i)}")
        Next

        ' Step 6-7: 编译并修复错误
        LogStep(6, "Building project and fixing compilation errors")
        Await BuildAndFixErrors()

        ' Step 8: 编译通过后 git commit
        LogStep(7, "Committing after successful build")
        GitCommit("Fix compilation errors")

        ' Step 9-10: 运行和测试
        LogStep(8, "Running and testing project")
        Await RunAndTest()

        ' Step 11: 最终 commit
        LogStep(9, "Final git commit")
        Dim commitMsg As String = "Complete development: " &
            _requirements.Substring(0, Math.Min(60, _requirements.Length))
        GitCommit(commitMsg)

        Log("")
        Log("========================================")
        Log("       DevAgent Completed Successfully")
        Log("========================================")
    End Function

    ' ========================================================================
    ' Step 1: 验证输入
    ' ========================================================================

    Private Sub ValidateInputs()
        ' 检查项目路径
        If String.IsNullOrEmpty(_projectPath) Then
            Throw New ArgumentException("Project path is not specified.")
        End If

        ' 创建项目目录（如果不存在）
        If Not Directory.Exists(_projectPath) Then
            Directory.CreateDirectory(_projectPath)
            Log($"  Created project directory: {_projectPath}")
        End If

        ' 检查 git 是否可用
        If Not ProcessHelper.CommandExists("git") Then
            Throw New InvalidOperationException("git is not available. Please install git and ensure it is in PATH.")
        End If
        Log("  git: available")

        ' 检查 dotnet 是否可用
        If Not ProcessHelper.CommandExists("dotnet") Then
            Throw New InvalidOperationException("dotnet is not available. Please install .NET SDK and ensure it is in PATH.")
        End If
        Log("  dotnet: available")

        ' 检查 Ollama 连接（通过一次简单调用）
        Log("  Ollama: configured (will verify on first chat)")
    End Sub

    ' ========================================================================
    ' Step 2: 创建开发计划
    ' ========================================================================

    Private Async Function CreateDevelopmentPlan() As Task
        Log("  Asking LLM to create development plan...")

        Dim prompt As New StringBuilder()
        prompt.AppendLine("Create a detailed development plan for a VB.NET .NET 10 project.")
        prompt.AppendLine()
        prompt.AppendLine("## Project Information")
        prompt.AppendLine($"- Name: {_projectName}")
        prompt.AppendLine($"- Type: {If(_projectType, "(to be determined)")}")
        prompt.AppendLine()
        prompt.AppendLine("## Requirements")
        prompt.AppendLine(_requirements)
        prompt.AppendLine()
        prompt.AppendLine("## Instructions")
        prompt.AppendLine("Break down the requirements into 5-15 specific, actionable development tasks.")
        prompt.AppendLine("Each task should be small enough to implement in one coding step.")
        prompt.AppendLine("Tasks should build on each other logically (e.g., data models before services, services before UI).")
        prompt.AppendLine()
        prompt.AppendLine("Output format - one task per line, no other text:")
        prompt.AppendLine("TASK: <task description>")

        Dim response As String = Await ChatWithLLM(prompt.ToString())

        ' 解析任务列表
        _plan.Clear()
        Dim lines() As String = response.Split({Environment.NewLine, vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
        For Each line As String In lines
            Dim trimmed As String = line.Trim()
            If trimmed.StartsWith("TASK:", StringComparison.OrdinalIgnoreCase) Then
                Dim taskDesc As String = trimmed.Substring("TASK:".Length).Trim()
                ' 移除前导编号（如 "1. Create..." -> "Create..."）
                If taskDesc.Length > 3 AndAlso Char.IsDigit(taskDesc(0)) Then
                    Dim dotIdx As Integer = taskDesc.IndexOf("."c)
                    If dotIdx > 0 AndAlso dotIdx < 5 Then
                        taskDesc = taskDesc.Substring(dotIdx + 1).Trim()
                    End If
                End If
                If taskDesc.Length > 0 Then
                    _plan.Add(taskDesc)
                End If
            End If
        Next

        If _plan.Count = 0 Then
            Log("  [WARN] No tasks parsed from LLM response. Using single-task fallback.")
            _plan.Add("Implement the project according to the requirements.")
        End If

        Log($"  Development plan created with {_plan.Count} task(s):")
        For i As Integer = 0 To _plan.Count - 1
            Log($"    {i + 1}. {_plan(i)}")
        Next
    End Function

    ' ========================================================================
    ' Step 3: 创建 git 分支
    ' ========================================================================

    Private Sub CreateGitBranch()
        ' 检查是否已有 git 仓库
        Dim gitDir As String = Path.Combine(_projectPath, ".git")
        If Not Directory.Exists(gitDir) Then
            Log("  Initializing git repository...")
            Dim initResult As ProcessResult = ProcessHelper.Git(_projectPath, "init")
            If Not initResult.Success Then
                Log("  [WARN] git init failed: " & initResult.StdErr)
            End If
        End If

        ' 创建并切换到新分支
        _gitBranch = _options.GitBranchPrefix & DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
        Log($"  Creating branch: {_gitBranch}")

        Dim result As ProcessResult = ProcessHelper.Git(_projectPath, $"checkout -b ""{_gitBranch}""")
        If Not result.Success Then
            ' 分支可能已存在，尝试直接切换
            Log("  [WARN] Could not create new branch, trying checkout existing...")
            result = ProcessHelper.Git(_projectPath, $"checkout ""{_gitBranch}""")
            If Not result.Success Then
                Log("  [WARN] Git branch setup failed: " & result.StdErr)
                Log("  Continuing on current branch...")
            End If
        End If
    End Sub

    ' ========================================================================
    ' Step 4: 设置项目
    ' ========================================================================

    Private Async Function CreateProject() As Task
        ' 创建新项目
        _projectType = Await DetermineProjectType()
        Log($"  LLM determined project type: {_projectType}")

        Dim template As String = If(_projectType = "library", "classlib", "console")
        Log($"  Creating new {template} project...")

        ' 尝试在当前目录创建项目
        Dim result As ProcessResult = ProcessHelper.DotNet(_projectPath, $"new {template} -lang VB")

        If Not result.Success Then
            ' 如果失败，尝试在子目录创建后移动文件
            Log("  Retrying with explicit project name...")
            Dim tempDir As String = Path.Combine(_projectPath, "__temp_create__")

            result = ProcessHelper.DotNet(
                _projectPath,
                $"new {template} -lang VB -n ""{_projectName}"" -o ""{tempDir}""")

            If result.Success Then
                ' 将文件从临时目录移动到项目根目录
                For Each f As String In Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories)
                    Dim relPath As String = f.Substring(tempDir.Length).TrimStart(Path.DirectorySeparatorChar, "/"c)
                    Dim destPath As String = Path.Combine(_projectPath, relPath)
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath))
                    File.Move(f, destPath)
                Next
                Try
                    Directory.Delete(tempDir, recursive:=True)
                Catch
                End Try
            Else
                ' 最终回退：手动创建 .vbproj
                Log("  [WARN] dotnet new failed, creating minimal project file manually...")
                CreateMinimalProject(template)
            End If
        End If

        ' 处理可能创建的子目录（dotnet new -n 会创建以项目名命名的子目录）
        Dim possibleSubDir As String = Path.Combine(_projectPath, _projectName)
        If Directory.Exists(possibleSubDir) AndAlso possibleSubDir <> _projectPath Then
            Log("  Moving files from subdirectory to project root...")
            For Each f As String In Directory.GetFiles(possibleSubDir, "*", SearchOption.AllDirectories)
                Dim relPath As String = f.Substring(possibleSubDir.Length).TrimStart(Path.DirectorySeparatorChar, "/"c)
                Dim destPath As String = Path.Combine(_projectPath, relPath)
                Directory.CreateDirectory(Path.GetDirectoryName(destPath))
                File.Move(f, destPath)
            Next
            Try
                Directory.Delete(possibleSubDir, recursive:=True)
            Catch
            End Try
        End If

        Log("  Project created successfully.")
    End Function

    Private Async Function SetupProject() As Task
        ' 检查是否已有 .vbproj 文件
        Dim vbprojFile As String = FindProjectFile()

        If vbprojFile IsNot Nothing Then
            ' 继续已有项目
            Log($"  Found existing project: {Path.GetFileName(vbprojFile)}")
            _projectType = DetectProjectType(vbprojFile)
            Log($"  Project type: {_projectType}")
        Else
            Await CreateProject()
        End If

        ' 初次提交项目骨架
        GitCommit("Initialize project structure")
    End Function

    ''' <summary>
    ''' 通过 LLM 确定项目类型（console 或 library）。
    ''' </summary>
    Private Async Function DetermineProjectType() As Task(Of String)
        Dim prompt As String =
            "Based on the following requirements, should the VB.NET project be a console application or a class library?" & Environment.NewLine &
            "Reply with exactly 'console' or 'library', nothing else." & Environment.NewLine & Environment.NewLine &
            "Requirements: " & _requirements

        Dim response As String = Await ChatWithLLM(prompt)

        If response.Trim().ToLowerInvariant().Contains("library") Then
            Return "library"
        Else
            Return "console"
        End If
    End Function

    ''' <summary>
    ''' 手动创建最小化的 .vbproj 文件（当 dotnet new 失败时的回退方案）。
    ''' </summary>
    Private Sub CreateMinimalProject(template As String)
        Dim outputType As String = If(template = "classlib", "Library", "Exe")

        Dim vbprojContent As String =
            "<Project Sdk=""Microsoft.NET.Sdk"">" & Environment.NewLine &
            "  <PropertyGroup>" & Environment.NewLine &
            $"    <OutputType>{outputType}</OutputType>" & Environment.NewLine &
            "    <TargetFramework>net10.0</TargetFramework>" & Environment.NewLine &
            $"    <RootNamespace>{_projectName}</RootNamespace>" & Environment.NewLine &
            "  </PropertyGroup>" & Environment.NewLine &
            "</Project>"

        File.WriteAllText(Path.Combine(_projectPath, _projectName & ".vbproj"), vbprojContent)

        ' 创建一个空的入口文件
        If template = "console" Then
            Dim programContent As String =
                "Module Program" & Environment.NewLine &
                "    Sub Main(args As String())" & Environment.NewLine &
                "        Console.WriteLine(""Hello World!"")" & Environment.NewLine &
                "    End Sub" & Environment.NewLine &
                "End Module"
            File.WriteAllText(Path.Combine(_projectPath, "Program.vb"), programContent)
        Else
            Dim classContent As String =
                "Public Class Class1" & Environment.NewLine &
                "End Class"
            File.WriteAllText(Path.Combine(_projectPath, "Class1.vb"), classContent)
        End If
    End Sub

    ' ========================================================================
    ' Step 5: 执行单个开发任务
    ' ========================================================================

    Private Async Function ExecuteTask(taskIndex As Integer) As Task
        Dim taskDesc As String = _plan(taskIndex)
        Log($"  Task: {taskDesc}")

        ' 构建提示词
        Dim prompt As New StringBuilder()
        prompt.AppendLine("You are working on a VB.NET .NET 10 project.")
        prompt.AppendLine()
        prompt.AppendLine("## Project Information")
        prompt.AppendLine($"- Name: {_projectName}")
        prompt.AppendLine($"- Type: {_projectType}")
        prompt.AppendLine()
        prompt.AppendLine("## Requirements")
        prompt.AppendLine(_requirements)
        prompt.AppendLine()
        prompt.AppendLine("## Development Plan")
        For i As Integer = 0 To _plan.Count - 1
            If i = taskIndex Then
                prompt.AppendLine($"  >>> {i + 1}. {_plan(i)}  [CURRENT TASK] <<<")
            Else
                prompt.AppendLine($"      {i + 1}. {_plan(i)}")
            End If
        Next
        prompt.AppendLine()
        prompt.AppendLine($"## Current Task ({taskIndex + 1}/{_plan.Count})")
        prompt.AppendLine(taskDesc)
        prompt.AppendLine()
        prompt.AppendLine("## Instructions")
        prompt.AppendLine("Write the code needed to complete this task.")
        prompt.AppendLine("Use list_files and get_project_tree to explore the project structure.")
        prompt.AppendLine("Use read_file to read existing files before modifying them.")
        prompt.AppendLine("Output each file using the ### FILE: format.")
        prompt.AppendLine("Make sure to output ALL files that need to be created or modified.")

        Dim response As String = Await ChatWithLLM(prompt.ToString())

        ' 解析并写入代码文件
        Dim files As List(Of CodeFile) = ParseCodeBlocks(response)

        If files.Count = 0 Then
            Log("  [WARN] No code files found in LLM response.")
            Log("  [LLM Response Preview] " & response.Substring(0, Math.Min(500, response.Length)))
            Return
        End If

        WriteCodeFiles(files)
        Log($"  Wrote {files.Count} file(s):")
        For Each f As CodeFile In files
            Log($"    - {f.RelativePath} ({f.Content.Length} chars)")
        Next
    End Function

    ' ========================================================================
    ' Step 6-7: 编译并修复错误
    ' ========================================================================

    Private Async Function BuildAndFixErrors() As Task
        Dim attempts As i32 = 0
        Dim result As ProcessResult

        Call Log("  Building project...")

        Do
            result = Await DebugLoop(++attempts)

            If attempts >= _options.MaxBuildFixAttempts Then
                Log("  [ERROR] Max build fix attempts reached. Giving up.")
                Log("  Last build output:")
                Log(result.CombinedOutput.Substring(0, Math.Min(2000, result.CombinedOutput.Length)))
                Return
            End If
        Loop
    End Function

    Private Async Function DebugLoop(attempts As Integer) As Task(Of ProcessResult)
        Log($"  Build attempt {attempts}/{_options.MaxBuildFixAttempts}...")

        Dim result As ProcessResult = ProcessHelper.DotNet(
            _projectPath, "build", _options.BuildTimeoutMs)

        If result.Success Then
            Log("  Build succeeded!")
            ' 输出编译警告（如果有）
            If result.CombinedOutput.Contains("warning", StringComparison.OrdinalIgnoreCase) Then
                Dim warningLines() As String = result.CombinedOutput.Split({Environment.NewLine}, StringSplitOptions.None)
                Dim warningCount As Integer = 0
                For Each line As String In warningLines
                    If line.Contains("warning", StringComparison.OrdinalIgnoreCase) Then
                        warningCount += 1
                        If warningCount <= 5 Then
                            Log("    " & line.Trim())
                        End If
                    End If
                Next
                If warningCount > 5 Then
                    Log($"    ... and {warningCount - 5} more warning(s)")
                End If
            End If
            Return result
        End If

        Log($"  Build failed (exit code: {result.ExitCode})")

        ' 将编译错误发送给 LLM 修复
        Log("  Asking LLM to fix build errors...")

        Dim buildOutput As String = result.CombinedOutput
        ' 截断过长的输出
        If buildOutput.Length > 8000 Then
            buildOutput = buildOutput.Substring(0, 8000) & Environment.NewLine & "... (truncated)"
        End If

        Dim prompt As New StringBuilder()
        prompt.AppendLine("The VB.NET project failed to build. Fix ALL compilation errors.")
        prompt.AppendLine()
        prompt.AppendLine("## Build Output (errors)")
        prompt.AppendLine("---BUILD OUTPUT---")
        prompt.AppendLine(buildOutput)
        prompt.AppendLine("---END BUILD OUTPUT---")
        prompt.AppendLine()
        prompt.AppendLine("## Instructions")
        prompt.AppendLine("1. Use read_file to read the files that have errors")
        prompt.AppendLine("2. Analyze each error carefully")
        prompt.AppendLine("3. Output the corrected files using the ### FILE: format")
        prompt.AppendLine("4. Output the COMPLETE file content for each file that needs to be modified")
        prompt.AppendLine("5. Make sure to fix ALL errors, not just the first one")

        Dim response As String = Await ChatWithLLM(prompt.ToString())

        Dim files As List(Of CodeFile) = ParseCodeBlocks(response)
        If files.Count > 0 Then
            WriteCodeFiles(files)
            Log($"  Applied fixes to {files.Count} file(s).")
        Else
            Log("  [WARN] No code files in LLM fix response. Will retry build...")
        End If

        Return result
    End Function

    ' ========================================================================
    ' Step 9-10: 运行和测试
    ' ========================================================================

    Private Async Function RunAndTest() As Task
        If _projectType = "library" Then
            Await TestLibrary()
        Else
            Await TestConsoleApp()
        End If
    End Function

    ''' <summary>
    ''' 测试 console 类型项目：直接运行，检查输出。
    ''' </summary>
    Private Async Function TestConsoleApp() As Task
        Log("  Running console application...")

        Dim attempts As Integer = 0

        Do
            attempts += 1
            Log($"  Run attempt {attempts}/{_options.MaxRuntimeFixAttempts}...")

            Dim result As ProcessResult = ProcessHelper.Run(
                _projectPath, "dotnet", "run --no-build", _options.RunTimeoutMs)

            Dim output As String = result.CombinedOutput
            Log($"  Program output ({output.Length} chars):")
            Dim previewLen As Integer = Math.Min(800, output.Length)
            For Each line As String In output.Substring(0, previewLen).Split({Environment.NewLine}, StringSplitOptions.None)
                Log("    " & line)
            Next
            If output.Length > 800 Then Log("    ... (truncated)")

            ' 如果程序崩溃（非零退出码且没有有意义输出）
            If Not result.Success AndAlso String.IsNullOrWhiteSpace(output) Then
                Log("  [WARN] Program crashed with no output. Exit code: " & result.ExitCode)
            End If

            ' 让 LLM 检查输出是否正确
            Dim prompt As New StringBuilder()
            prompt.AppendLine("The project compiled and ran. Analyze the program output.")
            prompt.AppendLine()
            prompt.AppendLine("## Requirements")
            prompt.AppendLine(_requirements)
            prompt.AppendLine()
            prompt.AppendLine("## Program Output")
            prompt.AppendLine("---PROGRAM OUTPUT---")
            prompt.AppendLine(output)
            prompt.AppendLine("---END OUTPUT---")
            prompt.AppendLine()
            prompt.AppendLine("## Exit Code")
            prompt.AppendLine(result.ExitCode.ToString())
            prompt.AppendLine()
            prompt.AppendLine("## Instructions")
            prompt.AppendLine("If the program output is correct and meets the requirements, reply with exactly:")
            prompt.AppendLine("RUNTIME_OK")
            prompt.AppendLine()
            prompt.AppendLine("If the output is incorrect or there are runtime errors:")
            prompt.AppendLine("1. Use read_file to read the relevant source files")
            prompt.AppendLine("2. Fix the code")
            prompt.AppendLine("3. Output the corrected files using the ### FILE: format")

            Dim response As String = Await ChatWithLLM(prompt.ToString())

            If response.Trim().StartsWith("RUNTIME_OK", StringComparison.OrdinalIgnoreCase) Then
                Log("  Runtime check passed!")
                Return
            End If

            If attempts >= _options.MaxRuntimeFixAttempts Then
                Log("  [WARN] Max runtime fix attempts reached. Continuing anyway.")
                Return
            End If

            Log("  Runtime issues detected. Applying fixes...")

            Dim files As List(Of CodeFile) = ParseCodeBlocks(response)
            If files.Count > 0 Then
                WriteCodeFiles(files)
                Log($"  Applied fixes to {files.Count} file(s).")

                ' 修复后重新编译
                Log("  Rebuilding after runtime fix...")
                Dim buildResult As ProcessResult = ProcessHelper.DotNet(
                    _projectPath, "build", _options.BuildTimeoutMs)

                If Not buildResult.Success Then
                    Log("  [WARN] Build failed after runtime fix. Running build fix loop...")
                    Await BuildAndFixErrors()
                End If
            Else
                Log("  [WARN] No code files in LLM response. Retrying...")
            End If

        Loop
    End Function

    ''' <summary>
    ''' 测试 library 类型项目：创建测试 console 项目，编写测试代码，运行验证。
    ''' </summary>
    Private Async Function TestLibrary() As Task
        Log("  Testing library project...")

        ' 设置测试项目目录
        Dim testDir As String = Path.Combine(_projectPath, "test")
        Dim testProjName As String = _projectName & ".Tests"

        ' 清理已有的测试目录
        If Directory.Exists(testDir) Then
            Try
                Directory.Delete(testDir, recursive:=True)
            Catch ex As Exception
                Log("  [WARN] Could not clean test directory: " & ex.Message)
            End Try
        End If
        Directory.CreateDirectory(testDir)

        ' 创建测试 console 项目
        Log($"  Creating test project: {testProjName}...")
        Dim createResult As ProcessResult = ProcessHelper.DotNet(
            testDir, $"new console -lang VB -n ""{testProjName}""")

        If Not createResult.Success Then
            Log("  [WARN] dotnet new for test project failed, creating manually...")
            CreateMinimalTestProject(testDir, testProjName)
        End If

        ' 查找测试项目文件路径
        Dim testProjFile As String = Path.Combine(testDir, testProjName & ".vbproj")
        If Not File.Exists(testProjFile) Then
            ' 搜索 .vbproj
            Dim vbprojs() As String = Directory.GetFiles(testDir, "*.vbproj", SearchOption.AllDirectories)
            If vbprojs.Length > 0 Then
                testProjFile = vbprojs(0)
                testDir = Path.GetDirectoryName(testProjFile)
            End If
        End If

        ' 添加对主项目的引用
        Dim mainProjFile As String = FindProjectFile()
        If mainProjFile IsNot Nothing AndAlso File.Exists(testProjFile) Then
            Log("  Adding project reference...")
            Dim refResult As ProcessResult = ProcessHelper.DotNet(
                testDir, $"add ""{testProjFile}"" reference ""{mainProjFile}""")
            If Not refResult.Success Then
                Log("  [WARN] Failed to add project reference: " & refResult.CombinedOutput)
            End If
        End If

        ' 获取库代码信息供 LLM 参考
        Dim fileList As String = GetProjectFileList()

        ' 让 LLM 编写测试代码
        Log("  Generating test code via LLM...")

        Dim prompt As New StringBuilder()
        prompt.AppendLine("You are a VB.NET developer writing test code for a .NET 10 class library.")
        prompt.AppendLine()
        prompt.AppendLine("## Library Information")
        prompt.AppendLine($"- Name: {_projectName}")
        prompt.AppendLine($"- Requirements: {_requirements}")
        prompt.AppendLine()
        prompt.AppendLine("## Library Source Files")
        prompt.AppendLine(fileList)
        prompt.AppendLine()
        prompt.AppendLine("## Instructions")
        prompt.AppendLine("Use read_file to read the library's source files and understand the API.")
        prompt.AppendLine("Then write a comprehensive console test program that:")
        prompt.AppendLine("1. Imports the library's namespace")
        prompt.AppendLine("2. Creates instances of the library's classes")
        prompt.AppendLine("3. Calls the library's methods with test data")
        prompt.AppendLine("4. Prints clear test results (PASS/FAIL) to console")
        prompt.AppendLine("5. Covers the main functionality described in the requirements")
        prompt.AppendLine()
        prompt.AppendLine("Output the test program using the ### FILE: format:")
        prompt.AppendLine("### FILE: Program.vb")
        prompt.AppendLine("```vb.net")
        prompt.AppendLine("' test code")
        prompt.AppendLine("```")

        Dim response As String = Await ChatWithLLM(prompt.ToString())

        Dim files As List(Of CodeFile) = ParseCodeBlocks(response)
        If files.Count = 0 Then
            Log("  [ERROR] No test code generated. Skipping library tests.")
            Return
        End If

        ' 写入测试文件
        For Each f As CodeFile In files
            Dim fullPath As String = Path.Combine(testDir, f.RelativePath)
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
            File.WriteAllText(fullPath, f.Content)
            Log($"  Wrote test file: {f.RelativePath}")
        Next

        ' 编译并运行测试项目
        Dim attempts As Integer = 0

        Do
            attempts += 1
            Log($"  Test attempt {attempts}/{_options.MaxRuntimeFixAttempts}...")

            ' 编译测试项目
            Dim buildResult As ProcessResult = ProcessHelper.DotNet(
                testDir, "build", _options.BuildTimeoutMs)

            If Not buildResult.Success Then
                Log("  Test build failed. Asking LLM to fix...")

                If attempts >= _options.MaxRuntimeFixAttempts Then
                    Log("  [WARN] Max test fix attempts reached. Skipping.")
                    Return
                End If

                Dim buildOutput As String = buildResult.CombinedOutput
                If buildOutput.Length > 6000 Then
                    buildOutput = buildOutput.Substring(0, 6000) & "... (truncated)"
                End If

                Dim fixPrompt As String =
                    "The test project failed to build:" & Environment.NewLine &
                    "---BUILD OUTPUT---" & Environment.NewLine &
                    buildOutput & Environment.NewLine &
                    "---END OUTPUT---" & Environment.NewLine &
                    "Use read_file to read the test files and fix the errors." & Environment.NewLine &
                    "Output the corrected files using the ### FILE: format." & Environment.NewLine &
                    "Note: test files are in the 'test/' directory."

                Dim fixResponse As String = Await ChatWithLLM(fixPrompt)
                Dim fixFiles As List(Of CodeFile) = ParseCodeBlocks(fixResponse)

                For Each f As CodeFile In fixFiles
                    Dim fullPath As String = Path.Combine(testDir, f.RelativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
                    File.WriteAllText(fullPath, f.Content)
                    Log($"  Fixed test file: {f.RelativePath}")
                Next

                Continue Do
            End If

            ' 运行测试项目
            Log("  Running test program...")
            Dim runResult As ProcessResult = ProcessHelper.Run(
                testDir, "dotnet", "run --no-build", _options.RunTimeoutMs)

            Dim testOutput As String = runResult.CombinedOutput
            Log("  Test output:")
            Dim previewLen As Integer = Math.Min(800, testOutput.Length)
            For Each line As String In testOutput.Substring(0, previewLen).Split({Environment.NewLine}, StringSplitOptions.None)
                Log("    " & line)
            Next

            ' 让 LLM 检查测试结果
            Dim checkPrompt As New StringBuilder()
            checkPrompt.AppendLine("Analyze the test program output for the library.")
            checkPrompt.AppendLine()
            checkPrompt.AppendLine("## Library Requirements")
            checkPrompt.AppendLine(_requirements)
            checkPrompt.AppendLine()
            checkPrompt.AppendLine("## Test Output")
            checkPrompt.AppendLine("---TEST OUTPUT---")
            checkPrompt.AppendLine(testOutput)
            checkPrompt.AppendLine("---END OUTPUT---")
            checkPrompt.AppendLine()
            checkPrompt.AppendLine("If all tests pass and the library works correctly, reply with exactly:")
            checkPrompt.AppendLine("RUNTIME_OK")
            checkPrompt.AppendLine()
            checkPrompt.AppendLine("If there are issues:")
            checkPrompt.AppendLine("1. Use read_file to read the library source files (not the test files)")
            checkPrompt.AppendLine("2. Fix the library code")
            checkPrompt.AppendLine("3. Output the corrected library files using the ### FILE: format")
            checkPrompt.AppendLine("Note: library source files are in the project root, NOT in the test/ directory.")

            Dim checkResponse As String = Await ChatWithLLM(checkPrompt.ToString())

            If checkResponse.Trim().StartsWith("RUNTIME_OK", StringComparison.OrdinalIgnoreCase) Then
                Log("  Tests passed!")
                Return
            End If

            If attempts >= _options.MaxRuntimeFixAttempts Then
                Log("  [WARN] Max test attempts reached. Continuing anyway.")
                Return
            End If

            Log("  Test issues detected. Applying fixes...")
            Dim fixFiles2 As List(Of CodeFile) = ParseCodeBlocks(checkResponse)

            For Each f As CodeFile In fixFiles2
                ' 库文件写入项目根目录，不是 test 目录
                Dim fullPath As String = ResolvePath(f.RelativePath)
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath))
                File.WriteAllText(fullPath, f.Content)
                Log($"  Fixed library file: {f.RelativePath}")
            Next

            ' 重新编译主项目
            Log("  Rebuilding library...")
            Dim rebuildResult As ProcessResult = ProcessHelper.DotNet(
                _projectPath, "build", _options.BuildTimeoutMs)

            If Not rebuildResult.Success Then
                Log("  [WARN] Library rebuild failed. Running build fix loop...")
                Await BuildAndFixErrors()
            End If

        Loop
    End Function

    ''' <summary>
    ''' 手动创建最小化测试项目（当 dotnet new 失败时的回退方案）。
    ''' </summary>
    Private Sub CreateMinimalTestProject(testDir As String, testProjName As String)
        Dim vbprojContent As String =
            "<Project Sdk=""Microsoft.NET.Sdk"">" & Environment.NewLine &
            "  <PropertyGroup>" & Environment.NewLine &
            "    <OutputType>Exe</OutputType>" & Environment.NewLine &
            "    <TargetFramework>net10.0</TargetFramework>" & Environment.NewLine &
            $"    <RootNamespace>{testProjName}</RootNamespace>" & Environment.NewLine &
            "  </PropertyGroup>" & Environment.NewLine &
            "</Project>"

        File.WriteAllText(Path.Combine(testDir, testProjName & ".vbproj"), vbprojContent)

        Dim programContent As String =
            "Module Program" & Environment.NewLine &
            "    Sub Main(args As String())" & Environment.NewLine &
            "        Console.WriteLine(""Test runner ready"")" & Environment.NewLine &
            "    End Sub" & Environment.NewLine &
            "End Module"
        File.WriteAllText(Path.Combine(testDir, "Program.vb"), programContent)
    End Sub

    ' ========================================================================
    ' Git 操作
    ' ========================================================================

    Private Sub GitCommit(message As String)
        ' 检查是否有变更
        Dim status As ProcessResult = ProcessHelper.Git(_projectPath, "status --porcelain")
        If status.CombinedOutput.Trim().Length = 0 Then
            Log("  No changes to commit, skipping.")
            Return
        End If

        ' 暂存所有变更
        ProcessHelper.Git(_projectPath, "add -A")

        ' 提交
        Dim escapedMsg As String = message.Replace("""", "\""")
        Dim result As ProcessResult = ProcessHelper.Git(_projectPath, $"commit -m ""{escapedMsg}""")

        If result.Success Then
            Log($"  Committed: {message}")
        Else
            Log($"  [WARN] Commit failed: {result.StdErr.Trim()}")
        End If
    End Sub

    ' ========================================================================
    ' LLM 交互
    ' ========================================================================

    ''' <summary>
    ''' 与 LLM 对话，自动附加系统提示词。
    ''' </summary>
    Private Async Function ChatWithLLM(prompt As String) As Task(Of String)
        Dim fullPrompt As String = SystemPrompt & Environment.NewLine & Environment.NewLine & prompt

        Try
            Dim response As LLMsResponse = Await _ollama.Chat(fullPrompt)

            If response Is Nothing Then
                Log("  [WARN] LLM returned null response")
                Return ""
            End If

            ' 记录思考过程（截断预览）
            If Not String.IsNullOrEmpty(response.think) Then
                Dim thinkPreview As String = response.think.Substring(0, Math.Min(300, response.think.Length))
                Log("  [LLM Think] " & thinkPreview & "...")
            End If

            Return If(response.output, "")

        Catch ex As Exception
            Log("  [ERROR] LLM communication failed: " & ex.Message)
            Return ""
        End Try
    End Function

    ' ========================================================================
    ' 文件操作
    ' ========================================================================

    ''' <summary>
    ''' 将解析出的代码文件写入磁盘。
    ''' </summary>
    Private Sub WriteCodeFiles(files As List(Of CodeFile))
        For Each f As CodeFile In files
            Try
                Dim fullPath As String = ResolvePath(f.RelativePath)
                Dim dir As String = Path.GetDirectoryName(fullPath)
                If Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
                File.WriteAllText(fullPath, f.Content)
            Catch ex As Exception
                Log($"  [ERROR] Failed to write {f.RelativePath}: {ex.Message}")
            End Try
        Next
    End Sub

    ''' <summary>
    ''' 从 LLM 响应中解析代码文件块。
    ''' 支持格式:
    ''' ### FILE: relative/path.vb
    ''' ```vb.net
    ''' code...
    ''' ```
    ''' </summary>
    Private Function ParseCodeBlocks(response As String) As List(Of CodeFile)
        Dim files As New List(Of CodeFile)

        If String.IsNullOrEmpty(response) Then Return files

        Dim lines() As String = response.Split({Environment.NewLine, vbLf}, StringSplitOptions.None)
        Dim i As Integer = 0

        Do While i < lines.Length
            Dim line As String = lines(i).Trim()

            ' 查找 ### FILE: 标记
            If line.StartsWith("### FILE:", StringComparison.OrdinalIgnoreCase) Then
                Dim filePath As String = line.Substring("### FILE:".Length).Trim()
                i += 1

                ' 查找代码块开始 (```)
                Dim codeStarted As Boolean = False
                Do While i < lines.Length
                    If lines(i).Trim().StartsWith("```") Then
                        codeStarted = True
                        i += 1
                        Exit Do
                    End If
                    ' 允许 FILE 标记和代码块之间有空行，但不能有其他内容
                    If Not String.IsNullOrWhiteSpace(lines(i)) Then
                        ' 如果标记和代码块之间有其他内容，跳过
                        Exit Do
                    End If
                    i += 1
                Loop

                If Not codeStarted Then Continue Do

                ' 收集代码直到结束的 ```
                Dim code As New StringBuilder()
                Do While i < lines.Length
                    If lines(i).Trim().StartsWith("```") Then
                        i += 1
                        Exit Do
                    End If
                    code.AppendLine(lines(i))
                    i += 1
                Loop

                If filePath.Length > 0 AndAlso code.Length > 0 Then
                    files.Add(New CodeFile With {
                        .RelativePath = filePath,
                        .Content = code.ToString().TrimEnd()
                    })
                End If
            Else
                i += 1
            End If
        Loop

        Return files
    End Function

    ''' <summary>
    ''' 获取项目中所有 .vb 源文件的相对路径列表。
    ''' </summary>
    Private Function GetProjectFileList() As String
        Dim sb As New StringBuilder()
        Try
            Dim files() As String = Directory.GetFiles(_projectPath, "*.vb", SearchOption.AllDirectories)
            For Each f As String In files
                If ContainsExcludedPath(f) Then Continue For
                Dim relPath As String = f.Substring(_projectPath.Length).TrimStart(Path.DirectorySeparatorChar, "/"c)
                sb.AppendLine(relPath)
            Next
        Catch ex As Exception
            sb.AppendLine("Error listing files: " & ex.Message)
        End Try
        Return sb.ToString().TrimEnd()
    End Function

    Private Function ContainsExcludedPath(fullPath As String) As Boolean
        Dim sep As Char = Path.DirectorySeparatorChar
        Return fullPath.Contains(sep & "bin" & sep) OrElse
               fullPath.Contains(sep & "obj" & sep) OrElse
               fullPath.Contains(sep & ".git" & sep) OrElse
               fullPath.Contains(sep & "test" & sep)
    End Function

    ''' <summary>
    ''' 查找项目根目录下的 .vbproj 文件。
    ''' </summary>
    Private Function FindProjectFile() As String
        Try
            Dim files() As String = Directory.GetFiles(_projectPath, "*.vbproj", SearchOption.TopDirectoryOnly)
            If files.Length > 0 Then Return files(0)
        Catch
        End Try
        Return Nothing
    End Function

    ''' <summary>
    ''' 从 .vbproj 内容检测项目类型。
    ''' </summary>
    Private Function DetectProjectType(vbprojPath As String) As String
        Try
            Dim content As String = File.ReadAllText(vbprojPath)
            If content.Contains("<OutputType>Exe</OutputType>") Then
                Return "console"
            ElseIf content.Contains("<OutputType>Library</OutputType>") Then
                Return "library"
            End If
        Catch
        End Try
        ' 默认为 console
        Return "console"
    End Function

    ''' <summary>
    ''' 将相对路径解析为绝对路径，包含安全检查。
    ''' </summary>
    Private Function ResolvePath(relativePath As String) As String
        relativePath = relativePath.Replace("/"c, Path.DirectorySeparatorChar)

        Dim fullPath As String
        If Path.IsPathRooted(relativePath) Then
            fullPath = Path.GetFullPath(relativePath)
        Else
            fullPath = Path.GetFullPath(Path.Combine(_projectPath, relativePath))
        End If

        ' 安全检查：确保路径在项目目录内
        Dim basePath As String = Path.GetFullPath(_projectPath)
        If Not fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) Then
            Throw New System.Security.SecurityException(
                $"Path '{relativePath}' is outside the project directory.")
        End If

        Return fullPath
    End Function

    ' ========================================================================
    ' 辅助方法
    ' ========================================================================

    Private Function SanitizeProjectName(name As String) As String
        Dim sb As New StringBuilder()
        For Each c As Char In name
            If Char.IsLetterOrDigit(c) OrElse c = "_"c OrElse c = "-"c OrElse c = "."c Then
                sb.Append(c)
            ElseIf c = " "c Then
                sb.Append("_"c)
            End If
        Next
        If sb.Length = 0 Then Return "Project"
        If Char.IsDigit(sb(0)) Then sb.Insert(0, "_"c)
        Return sb.ToString()
    End Function

    Private Sub Log(message As String)
        If _log IsNot Nothing Then
            _log(message)
        End If
    End Sub

    Private Sub LogStep(stepNum As Integer, description As String)
        Log("")
        Log($"--- Step {stepNum}: {description} ---")
    End Sub

End Class
