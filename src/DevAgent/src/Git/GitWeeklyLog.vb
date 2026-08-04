Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VersionControl.Git
Imports Ollama
Imports System.Text

Module GitWeeklyLog

    ' 控制发送给 LLM 的上下文长度，避免超出模型上下文窗口
    Private Const MaxDiffChars As Integer = 6000
    Private Const MaxMemberPromptChars As Integer = 12000

    ''' <summary>
    ''' 根据git代码库的一周变更历史，通过LLM模型做工作总结，生成团队的工作周报
    ''' </summary>
    ''' <param name="config">包含 LLM 服务地址、模型、密钥的应用配置</param>
    ''' <param name="ws">the workspace folder</param>
    ''' <returns>the weekly log result text</returns>
    Public Async Function GenerateWeeklyLog(config As AppConfig, ws As String) As Task(Of String)
        ' 解析过去一周内的全部提交记录
        Dim teamjobs = weeklyLog.GetWeeklyLog(directory:=ws, since:="1 week ago").ToArray

        If teamjobs.IsNullOrEmpty Then
            Return $"该工作区（{ws}）在过去一周内没有检测到任何 git 提交记录，无法生成工作周报。"
        End If

        ' 按提交作者（团队成员）对提交记录分组
        Dim authors = teamjobs _
            .GroupBy(Function(l) l.meta.author) _
            .ToDictionary(Function(a) a.Key, Function(a) a.ToArray)

        If authors.Count = 0 Then
            Return $"该工作区（{ws}）在过去一周内没有检测到任何 git 提交记录，无法生成工作周报。"
        End If

        ' 基于应用配置在 Using 内构造 LLM 客户端，调用结束后自动释放
        Using client As LLMClient = CreateClient(config)
            Dim report As New StringBuilder()

            report.AppendLine("# 团队工作周报")
            report.AppendLine()
            report.AppendLine($"统计周期：过去一周（{teamjobs.Min(Function(c) c.meta.date):yyyy-MM-dd} ~ {teamjobs.Max(Function(c) c.meta.date):yyyy-MM-dd}）")
            report.AppendLine($"参与成员：{authors.Count} 人")
            report.AppendLine($"提交总数：{teamjobs.Length} 次")
            report.AppendLine()

            ' ===== 阶段一：逐成员生成工作描述 =====
            report.AppendLine("## 一、团队成员工作内容")
            report.AppendLine()

            Dim memberSummaries As New List(Of String)()

            For Each kvp In authors
                Dim author As String = kvp.Key
                Dim commits = kvp.Value

                Dim ctx As String = RenderMemberContext(author, commits)
                Dim prompt As String = BuildMemberPrompt(author, commits.Length, ctx)

                report.AppendLine($"### {author}")
                Dim description = Await SafeChat(client, prompt, $"（{author} 的工作总结生成失败，请稍后重试）")
                report.AppendLine(description)
                report.AppendLine()

                memberSummaries.Add($"## {author}{vbCrLf}{description}")
            Next

            ' ===== 阶段二：汇总生成团队整体总结 =====
            report.AppendLine("## 二、团队工作任务总结")
            report.AppendLine()

            Dim teamContext As New StringBuilder()
            For Each s In memberSummaries
                teamContext.AppendLine(s)
                teamContext.AppendLine()
            Next

            Dim teamPrompt As String = BuildTeamPrompt(teamContext.ToString())
            Dim teamSummary = Await SafeChat(client, teamPrompt, "（团队工作总结生成失败，请稍后重试）")
            report.AppendLine(teamSummary)

            Return report.ToString()
        End Using
    End Function

    ''' <summary>
    ''' 基于应用配置创建 LLM 客户端实例
    ''' </summary>
    Private Function CreateClient(config As AppConfig) As LLMClient
        Dim server As ILLMProvider = LLMUrl.Create(config.Url, config.ApiKey)
        Return New LLMClient(server, config.Model)
    End Function

    ''' <summary>
    ''' 将单个成员一周内的提交素材（提交说明 + 改动文件清单 + 关键 diff 行）渲染为 LLM 可读文本
    ''' </summary>
    Private Function RenderMemberContext(author As String, commits As commitEntry()) As String
        Dim sb As New StringBuilder()
        Dim diffBudget As Integer = MaxDiffChars

        For Each c In commits
            sb.AppendLine($"提交 {c.meta.commit}")
            sb.AppendLine($"时间：{c.meta.date:yyyy-MM-dd HH:mm}")
            sb.AppendLine($"说明：{c.meta.message}")
            sb.AppendLine($"改动统计：新增 {c.AddedLines} 行，删除 {c.DeletedLines} 行")

            If c.changes IsNot Nothing AndAlso c.changes.Files IsNot Nothing Then
                sb.AppendLine("改动文件：")
                For Each file In c.changes.Files
                    sb.AppendLine($"  - {file.ToString()}")
                Next

                ' 追加该提交的关键 diff 行（仅新增/删除行），并受总长度预算约束
                If diffBudget > 0 Then
                    sb.AppendLine("关键代码差异：")
                    For Each file In c.changes.Files
                        If file.Hunks Is Nothing Then Continue For
                        For Each hunk In file.Hunks
                            If hunk.Lines Is Nothing Then Continue For
                            For Each line In hunk.Lines
                                If line.Type <> DiffLineType.Added AndAlso line.Type <> DiffLineType.Deleted Then Continue For
                                Dim prefix As String = If(line.Type = DiffLineType.Added, "+", "-")
                                Dim text As String = $"    {prefix}{line.Content}"
                                If sb.Length + text.Length > MaxMemberPromptChars Then Exit For
                                sb.AppendLine(text)
                                diffBudget -= text.Length
                                If diffBudget <= 0 Then Exit For
                            Next
                            If diffBudget <= 0 Then Exit For
                        Next
                        If diffBudget <= 0 Then Exit For
                    Next
                End If
            End If

            sb.AppendLine()
        Next

        Return Truncate(sb.ToString(), MaxMemberPromptChars)
    End Function

    ''' <summary>
    ''' 构造阶段一（单成员工作描述）的 prompt
    ''' </summary>
    Private Function BuildMemberPrompt(author As String, commitCount As Integer, context As String) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("你是一名资深技术团队负责人，擅长根据 git 提交记录总结开发人员的工作内容。")
        sb.AppendLine($"下面是一位团队成员「{author}」在过去一周内的 git 提交记录（共 {commitCount} 次提交），")
        sb.AppendLine("包含提交说明、改动文件清单以及关键代码差异。")
        sb.AppendLine()
        sb.AppendLine("【要求】")
        sb.AppendLine("1. 用简洁、专业的中文描述该成员本周完成的工作内容；")
        sb.AppendLine("2. 按工作主题/模块归类，突出实际完成的功能、修复的问题与涉及的关键文件；")
        sb.AppendLine("3. 不要复述提交哈希，不要罗列每一行 diff，只概括工作要点；")
        sb.AppendLine("4. 使用要点列表（bullet）组织内容，控制在 200 字以内。")
        sb.AppendLine()
        sb.AppendLine("【该成员提交记录】")
        sb.AppendLine(context)
        sb.AppendLine()
        sb.AppendLine("请直接输出该成员的工作描述（不要加标题与成员名，标题由外层统一添加）：")
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' 构造阶段二（团队整体总结）的 prompt
    ''' </summary>
    Private Function BuildTeamPrompt(memberContext As String) As String
        Dim sb As New StringBuilder()
        sb.AppendLine("你是一名技术团队负责人，需要基于各成员的工作描述，输出团队整体的工作周报总结。")
        sb.AppendLine("下面是团队成员各自的工作描述：")
        sb.AppendLine()
        sb.AppendLine(memberContext)
        sb.AppendLine()
        sb.AppendLine("【要求】")
        sb.AppendLine("1. 用专业的中文进行团队层面的总结，而非简单拼接个人描述；")
        sb.AppendLine("2. 归纳本周整体工作进度、完成的主要成果与亮点；")
        sb.AppendLine("3. 指出跨成员的协作、共性工作、潜在的风险或依赖关系（如有）；")
        sb.AppendLine("4. 使用段落 + 要点列表组织，条理清晰，控制在 400 字以内。")
        sb.AppendLine()
        sb.AppendLine("请直接输出团队整体工作总结：")
        Return sb.ToString()
    End Function

    ''' <summary>
    ''' 带容错的 LLM 调用封装：失败时返回降级文本，不向上抛出异常，保证周报其余部分可用
    ''' </summary>
    Private Async Function SafeChat(client As LLMClient, prompt As String, fallback As String) As Task(Of String)
        Try
            Dim response = Await client.Chat(prompt)
            If response IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(response.output) Then
                Return response.output.Trim()
            End If
            Return fallback
        Catch ex As Exception
            ' 单成员调用失败不应中断整体周报生成
            Return fallback
        End Try
    End Function

    ''' <summary>
    ''' 将文本裁剪到指定最大长度，超出时以省略号结尾
    ''' </summary>
    Private Function Truncate(text As String, maxChars As Integer) As String
        If String.IsNullOrEmpty(text) OrElse text.Length <= maxChars Then
            Return text
        End If
        Return text.Substring(0, maxChars) & vbCrLf & "...（内容过长已截断）"
    End Function

End Module
