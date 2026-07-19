
''' <summary>
''' Agent 配置选项。
''' </summary>
Public Class DevAgentOptions

    ''' <summary>编译错误修复最大尝试次数。</summary>
    Public Property MaxBuildFixAttempts As Integer = 8

    ''' <summary>运行时错误修复最大尝试次数。</summary>
    Public Property MaxRuntimeFixAttempts As Integer = 5

    ''' <summary>编译超时（毫秒），默认 180 秒。</summary>
    Public Property BuildTimeoutMs As Integer = 180000

    ''' <summary>程序运行超时（毫秒），默认 30 秒。</summary>
    Public Property RunTimeoutMs As Integer = 30000

    ''' <summary>git 分支前缀。</summary>
    Public Property GitBranchPrefix As String = "dev-agent/"

End Class

''' <summary>
''' 从 LLM 响应中解析出的代码文件。
''' </summary>
Public Class CodeFile

    ''' <summary>相对于项目根目录的文件路径。</summary>
    Public Property RelativePath As String

    ''' <summary>文件内容。</summary>
    Public Property Content As String

End Class