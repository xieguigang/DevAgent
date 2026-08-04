Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VersionControl.Git

Module GitWeeklyLog

    ''' <summary>
    ''' 根据git代码库的一周变更历史，通过LLM模型做工作总结，生成团队的工作周报
    ''' </summary>
    ''' <param name="config"></param>
    ''' <param name="ws">the workspace folder</param>
    ''' <returns>the weekly log result text</returns>
    Public Async Function GenerateWeeklyLog(config As AppConfig, ws As String) As Task(Of String)
        Dim teamjobs = weeklyLog.GetWeeklyLog(directory:=ws, since:="1 week ago").ToArray
        Dim authors = teamjobs.GroupBy(Function(l) l.meta.author).ToDictionary(Function(a) a.Key, Function(a) a.ToArray)

    End Function
End Module
