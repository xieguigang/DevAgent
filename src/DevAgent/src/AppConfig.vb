Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.ComponentModel.Settings.Inf

' ============================================================================
' AppConfig.vb - INI 配置模块
'
' 通过读取 INI 文件配置 Program.PrintUsage() 中所打印的可选命令行参数：
'   model / url / apikey / max-build-fix / max-run-fix
'
' 三级优先级（高 → 低）：
'   1. 命令行显式提供的参数（CLI）
'   2. INI 文件中的配置值
'   3. Opts 中内置的默认值
'
' INI 文件路径：
'   - 由 --config <path>（-c）指定
'   - 未指定时默认为程序所在目录（AppContext.BaseDirectory）下的 devagent.ini
'   - 首次运行若文件不存在，自动生成带注释的默认模板
'
' INI 结构（单 [devagent] 段，键名与长 flag 一致）：
'   [devagent]
'   model=llama3.2
'   url=http://localhost:11434
'   apikey=
'   max-build-fix=8
'   max-run-fix=5
' ============================================================================

''' <summary>
''' 程序运行期解析后的配置集合。
''' 所有值均按「CLI > INI > 内置默认」的优先级解析完毕。
''' </summary>
Public Class AppConfig

    ' --- LLM 配置 ---
    Public Property Model As String
    Public Property Url As String
    Public Property ApiKey As String

    ' --- Agent 配置 ---
    Public Property MaxBuildFix As Integer
    Public Property MaxRunFix As Integer

    ' --- 元信息 ---
    Public Property IniPath As String
    Public Property IniExists As Boolean

    ''' <summary>启动横幅用的各值来源描述。</summary>
    Public Property SourceBanner As String

    ' ========================================================================
    ' 加载入口
    ' ========================================================================

    ''' <summary>
    ''' 按三级优先级解析配置。
    ''' </summary>
    ''' <param name="args">原始命令行参数数组，用于判断某 flag 是否被显式提供。</param>
    ''' <param name="opt">已由 CommandLine 解析并 ResolveFile 后的 Opts 实例。</param>
    Public Shared Function Load(args As String(), opt As Opts) As AppConfig

        ' 1. 定位 INI 路径
        Dim iniPath As String
        If Not String.IsNullOrEmpty(opt.configFile) Then
            iniPath = Path.GetFullPath(opt.configFile)
        Else
            iniPath = Path.Combine(AppContext.BaseDirectory, "devagent.ini")
        End If

        ' 2. 文件不存在则生成默认模板
        Dim iniExists As Boolean = File.Exists(iniPath)
        If Not iniExists Then
            Try
                WriteTemplate(iniPath)
                iniExists = True
            Catch ex As Exception
                ' 模板生成失败不致命，继续以默认值运行
                iniExists = False
            End Try
        End If

        ' 3. 读取 INI（不 Dispose/Flush，避免回写时丢失注释）
        Dim ini As IniFile = Nothing
        If iniExists Then
            Try
                ini = New IniFile(iniPath)
            Catch ex As Exception
                ini = Nothing
            End Try
        End If

        Dim cfg As New AppConfig With {
            .IniPath = iniPath,
            .IniExists = (ini IsNot Nothing)
        }

        Dim banner As New List(Of String)()

        ' 4. model
        Dim modelCli As Boolean = HasFlag(args, "--model", "-m")
        Dim modelIni As String = If(ini IsNot Nothing, ini.ReadValue("devagent", "model", Nothing), Nothing)
        If modelCli Then
            cfg.Model = opt.model
            banner.Add($"  Model:        {cfg.Model}  [cli]")
        ElseIf Not String.IsNullOrWhiteSpace(modelIni) Then
            cfg.Model = modelIni
            banner.Add($"  Model:        {cfg.Model}  [ini]")
        Else
            cfg.Model = opt.model
            banner.Add($"  Model:        {cfg.Model}  [default]")
        End If

        ' 5. url
        Dim urlCli As Boolean = HasFlag(args, "--url", "-u")
        Dim urlIni As String = If(ini IsNot Nothing, ini.ReadValue("devagent", "url", Nothing), Nothing)
        If urlCli Then
            cfg.Url = opt.ollamaUrl
            banner.Add($"  Ollama URL:   {cfg.Url}  [cli]")
        ElseIf Not String.IsNullOrWhiteSpace(urlIni) Then
            cfg.Url = urlIni
            banner.Add($"  Ollama URL:   {cfg.Url}  [ini]")
        Else
            cfg.Url = opt.ollamaUrl
            banner.Add($"  Ollama URL:   {cfg.Url}  [default]")
        End If

        ' 6. apikey
        '    注意：Opts.ResolveFile 已将空 apikey 默认为 MyDocuments/.openai.key 路径，
        '    LLMUrl.Create 会自动读取该文件首行。INI 提供 apikey 时优先用 INI 值。
        Dim keyCli As Boolean = HasFlag(args, "--key", "-k")
        Dim keyIni As String = If(ini IsNot Nothing, ini.ReadValue("devagent", "apikey", Nothing), Nothing)
        If keyCli Then
            cfg.ApiKey = opt.apikey
            banner.Add($"  ApiKey:       (from cli)  [cli]")
        ElseIf Not String.IsNullOrWhiteSpace(keyIni) Then
            cfg.ApiKey = keyIni
            banner.Add($"  ApiKey:       (from ini)  [ini]")
        Else
            cfg.ApiKey = opt.apikey
            banner.Add($"  ApiKey:       (from ~/.openai.key)  [default]")
        End If

        ' 7. max-build-fix
        Dim mbfCli As Boolean = HasFlag(args, "--max-build-fix", Nothing)
        Dim mbfIni As String = If(ini IsNot Nothing, ini.ReadValue("devagent", "max-build-fix", Nothing), Nothing)
        Dim mbfParsed As Integer
        If mbfCli Then
            cfg.MaxBuildFix = opt.maxBuildFix
            banner.Add($"  MaxBuildFix:  {cfg.MaxBuildFix}  [cli]")
        ElseIf Integer.TryParse(mbfIni, mbfParsed) Then
            cfg.MaxBuildFix = mbfParsed
            banner.Add($"  MaxBuildFix:  {cfg.MaxBuildFix}  [ini]")
        Else
            cfg.MaxBuildFix = opt.maxBuildFix
            banner.Add($"  MaxBuildFix:  {cfg.MaxBuildFix}  [default]")
        End If

        ' 8. max-run-fix
        Dim mrfCli As Boolean = HasFlag(args, "--max-run-fix", Nothing)
        Dim mrfIni As String = If(ini IsNot Nothing, ini.ReadValue("devagent", "max-run-fix", Nothing), Nothing)
        Dim mrfParsed As Integer
        If mrfCli Then
            cfg.MaxRunFix = opt.maxRunFix
            banner.Add($"  MaxRunFix:    {cfg.MaxRunFix}  [cli]")
        ElseIf Integer.TryParse(mrfIni, mrfParsed) Then
            cfg.MaxRunFix = mrfParsed
            banner.Add($"  MaxRunFix:    {cfg.MaxRunFix}  [ini]")
        Else
            cfg.MaxRunFix = opt.maxRunFix
            banner.Add($"  MaxRunFix:    {cfg.MaxRunFix}  [default]")
        End If

        cfg.SourceBanner = String.Join(Environment.NewLine, banner)

        Return cfg
    End Function

    ' ========================================================================
    ' 辅助
    ' ========================================================================

    ''' <summary>
    ''' 判断原始 args 中是否显式提供了某个 flag。
    ''' 同时支持空格分隔（--model x）与等号形式（--model=x）。
    ''' </summary>
    Private Shared Function HasFlag(args As String(), longFlag As String, shortFlag As String) As Boolean
        If args Is Nothing Then Return False

        For Each a As String In args
            If a Is Nothing Then Continue For
            Dim tok As String = a.Trim()
            If tok.Length = 0 Then Continue For

            If tok.Equals(longFlag, StringComparison.OrdinalIgnoreCase) Then Return True
            If tok.StartsWith(longFlag & "=", StringComparison.OrdinalIgnoreCase) Then Return True

            If Not String.IsNullOrEmpty(shortFlag) Then
                If tok.Equals(shortFlag, StringComparison.OrdinalIgnoreCase) Then Return True
                If tok.StartsWith(shortFlag & "=", StringComparison.OrdinalIgnoreCase) Then Return True
            End If
        Next

        Return False
    End Function

    ''' <summary>
    ''' 生成带注释的默认 INI 模板。
    ''' </summary>
    Public Shared Sub WriteTemplate(iniPath As String)
        Dim dir As String = System.IO.Path.GetDirectoryName(iniPath)
        If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        Dim sb As New StringBuilder()
        sb.AppendLine("; DevAgent configuration file")
        sb.AppendLine("; Priority: command-line args > INI here > built-in defaults")
        sb.AppendLine("; Edit values below and restart. Keys match the long CLI flags.")
        sb.AppendLine()
        sb.AppendLine("[devagent]")
        sb.AppendLine("; Ollama/LLM model name (CLI --model / -m overrides)")
        sb.AppendLine("model=llama3.2")
        sb.AppendLine("; LLM API URL (CLI --url / -u overrides)")
        sb.AppendLine("url=http://localhost:11434")
        sb.AppendLine("; API key: a key string or path to a key file (CLI --key / -k overrides)")
        sb.AppendLine("; Leave empty to use <MyDocuments>/.openai.key")
        sb.AppendLine("apikey=")
        sb.AppendLine("; Max build fix attempts (CLI --max-build-fix overrides)")
        sb.AppendLine("max-build-fix=8")
        sb.AppendLine("; Max runtime fix attempts (CLI --max-run-fix overrides)")
        sb.AppendLine("max-run-fix=5")

        File.WriteAllText(iniPath, sb.ToString())
    End Sub

End Class
