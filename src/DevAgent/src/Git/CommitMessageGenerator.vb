Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VersionControl.Git
Imports Ollama

''' <summary>
''' 基于 LLM 的 git commit 信息生成模块。
''' 读取指定工作区的 git diff（通过 GCModeller 的 <see cref="diff.GetDiff"/>），
''' 交由 LLM 总结编辑内容，返回结构化的 <see cref="CommitSummary"/>。
''' </summary>
Public Module CommitMessageGenerator

    ''' <summary>
    ''' 渲染后的 diff 文本最大长度，超出则截断，避免 token 消耗过高导致 LLM 调用失败或成本过大。
    ''' </summary>
    Private ReadOnly MaxDiffLength As Integer = 8000

    ''' <summary>
    ''' 解析指定工作区的编辑差异，并交由 LLM 生成 git commit 的 summary 与 description。
    ''' </summary>
    ''' <param name="ollama">已构造好的 LLM 客户端（Ollama.LLMClient）。注意：本次调用会把 diff 与回复追加到该客户端的对话记忆中，调用方如需隔离可在调用后通过 <c>ollama.Clear()</c> 清理。</param>
    ''' <param name="workspace">git 工作区（仓库）目录路径。</param>
    ''' <param name="cached">若为 <c>True</c>，则解析已暂存（<c>git diff --cached</c>）的差异；否则解析未暂存的改动。</param>
    ''' <returns>包含 <see cref="CommitSummary.Summary"/> 与 <see cref="CommitSummary.Description"/> 的 <see cref="CommitSummary"/> 对象。</returns>
    Public Async Function GenerateCommitMessage(
        ollama As LLMClient,
        workspace As String,
        Optional cached As Boolean = False
    ) As Task(Of CommitSummary)

        If ollama Is Nothing Then
            Throw New ArgumentNullException(NameOf(ollama), "必须传入已构造的 LLMClient 实例")
        End If
        If String.IsNullOrWhiteSpace(workspace) Then
            Throw New ArgumentException("工作区路径不能为空", NameOf(workspace))
        Else
            Return Await diff.GetDiff(workspace, cached).GenerateCommitMessage(ollama)
        End If
    End Function

    ''' <summary>
    ''' 解析指定工作区的编辑差异，并交由 LLM 生成 git commit 的 summary 与 description。
    ''' </summary>
    ''' <param name="ollama">已构造好的 LLM 客户端（Ollama.LLMClient）。注意：本次调用会把 diff 与回复追加到该客户端的对话记忆中，调用方如需隔离可在调用后通过 <c>ollama.Clear()</c> 清理。</param>
    ''' <returns>包含 <see cref="CommitSummary.Summary"/> 与 <see cref="CommitSummary.Description"/> 的 <see cref="CommitSummary"/> 对象。</returns>
    ''' 
    <Extension>
    Public Async Function GenerateCommitMessage(diffResult As DiffResult, ollama As LLMClient) As Task(Of CommitSummary)
        If diffResult Is Nothing OrElse diffResult.Files.IsNullOrEmpty Then
            Call "工作区无尚未提交的改动，无法生成 commit 信息".warning

            Return New CommitSummary With {
               .Summary = String.Empty,
               .Description = String.Empty
            }
        Else
            Dim diffText As String = RenderDiff(diffResult)

            If diffText.StringEmpty Then
                Throw New InvalidOperationException("工作区无尚未提交的改动，无法生成 commit 信息")
            End If

            Dim prompt As String = BuildPrompt(diffText)
            Dim response As LLMsResponse = Await ollama.Chat(prompt)

            If response Is Nothing OrElse response.output.StringEmpty Then
                Throw New InvalidOperationException("LLM 未返回有效内容，无法解析 commit 信息")
            End If

            Return ParseResponse(response.output)
        End If
    End Function

    ''' <summary>
    ''' 将结构化的 <see cref="DiffResult"/> 渲染为可读的文本差异（含文件变更类型与各 hunk 的增删内容）。
    ''' </summary>
    Private Function RenderDiff(result As DiffResult) As String
        Dim sb As New StringBuilder

        For Each file As FileChange In result.Files
            Call sb.AppendLine($"{file.ChangeKind}: {file.FilePath}")

            If file.Hunks Is Nothing Then
                Call sb.AppendLine()
                Continue For
            End If

            For Each hunk As DiffHunk In file.Hunks
                Call sb.AppendLine($"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@")

                If hunk.Lines IsNot Nothing Then
                    For Each line As DiffLine In hunk.Lines
                        Call sb.AppendLine(line.ToString())
                    Next
                End If
            Next

            Call sb.AppendLine()
        Next

        Dim text As String = sb.ToString().Trim()

        If text.Length > MaxDiffLength Then
            text = text.Substring(0, MaxDiffLength) & vbCrLf & "...(差异过长已截断)..."
        End If

        Return text
    End Function

    ''' <summary>
    ''' 构造发送给 LLM 的提示词，要求模型以 <c>SUMMARY:</c> / <c>DESCRIPTION:</c> 固定格式输出。
    ''' </summary>
    Private Function BuildPrompt(diffText As String) As String
        Dim sb As New StringBuilder

        Call sb.AppendLine("你是一名资深的软件工程师，擅长为 git 提交撰写清晰、规范的提交信息。")
        Call sb.AppendLine("下面是一份 git diff 输出，描述了对工作区所做的编辑变更。")
        Call sb.AppendLine("请阅读并理解这些变更，然后总结本次编辑的内容。")
        Call sb.AppendLine()
        Call sb.AppendLine("要求：")
        Call sb.AppendLine("1. 用一句话（尽量不超过 72 个字符）概括本次改动的核心目的，作为提交标题（summary）。")
        Call sb.AppendLine("2. 用若干段落（必要时使用列表）详细说明改动的内容、动机与影响，作为提交说明（description）。")
        Call sb.AppendLine("3. summary 使用祈使句、简洁的英文（例如 'Add ...'、'Fix ...'、'Refactor ...'）；description 的语言与 diff 中代码注释/变更内容保持一致（若是中文改动则使用中文）。")
        Call sb.AppendLine("4. 严格按照下面的格式输出，不要输出任何额外内容，也不要使用代码块标记：")
        Call sb.AppendLine()
        Call sb.AppendLine("SUMMARY: <一句话标题>")
        Call sb.AppendLine("DESCRIPTION:")
        Call sb.AppendLine("<多行详细说明>")
        Call sb.AppendLine()
        Call sb.AppendLine("===== git diff 开始 =====")
        Call sb.AppendLine(diffText)
        Call sb.AppendLine("===== git diff 结束 =====")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' 从 LLM 的 output 中解析出 SUMMARY 与 DESCRIPTION。
    ''' 若模型未严格遵守格式，则回退：首行非空内容作为 summary，整段作为 description。
    ''' </summary>
    Private Function ParseResponse(output As String) As CommitSummary
        Dim summary As String = ""
        Dim description As String = ""

        Dim summaryIdx As Integer = output.IndexOf("summary:", StringComparison.OrdinalIgnoreCase)
        Dim descIdx As Integer = output.IndexOf("description:", StringComparison.OrdinalIgnoreCase)

        If summaryIdx >= 0 AndAlso descIdx > summaryIdx Then
            ' 标准格式：取 summary: 与 description: 之间的文本作为 summary
            Dim betweenStart As Integer = summaryIdx + "summary:".Length
            summary = output.Substring(betweenStart, descIdx - betweenStart).Trim()
            summary = summary.Replace(vbCrLf, " ").Replace(vbLf, " ").Replace(vbCr, " ").Trim()
            description = output.Substring(descIdx + "description:".Length).Trim()
        ElseIf summaryIdx >= 0 Then
            ' 只有 SUMMARY: 标记
            Dim after As String = output.Substring(summaryIdx + "summary:".Length).Trim()
            Dim nl As Integer = after.IndexOf(vbCrLf)

            If nl < 0 Then
                nl = after.IndexOf(vbLf)
            End If

            If nl < 0 Then
                summary = after
                description = ""
            Else
                summary = after.Substring(0, nl).Trim()
                description = after.Substring(nl).Trim()
            End If
        Else
            ' 未遵守格式：首行非空内容作为 summary，整段作为 description
            summary = output.LineTokens.FirstOrDefault(Function(s) Not s.StringEmpty)?.Trim()
            description = output.Trim()
        End If

        If summary.StringEmpty Then
            If description.StringEmpty Then
                summary = "update"
            Else
                summary = description.LineTokens.FirstOrDefault(Function(s) Not s.StringEmpty)?.Trim()

                If summary.StringEmpty Then
                    summary = "update"
                End If
            End If
        End If

        Return New CommitSummary(summary, description)
    End Function
End Module
