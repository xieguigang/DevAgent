Module Commit

    ' ========================================================================
    ' Git 操作
    ' ========================================================================

    Public Sub GitCommit(message As String, workspace As String, log As Action(Of String))
        ' 检查是否有变更
        Dim status As ProcessResult = ProcessHelper.Git(workspace, "status --porcelain")
        If status.CombinedOutput.Trim().Length = 0 Then
            log("  No changes to commit, skipping.")
            Return
        End If

        ' 暂存所有变更
        ProcessHelper.Git(workspace, "add -A")

        ' 提交
        Dim escapedMsg As String = message.Replace("""", "\""")
        Dim result As ProcessResult = ProcessHelper.Git(workspace, $"commit -m ""{escapedMsg}""")

        If result.Success Then
            log($"  Committed: {message}")
        Else
            log($"  [WARN] Commit failed: {result.StdErr.Trim()}")
        End If
    End Sub

    ''' <summary>
    ''' 将特定文件夹中的所有改动进行 commit。
    ''' </summary>
    ''' <param name="repoRootPath">Git 仓库的根目录路径（用于执行 git 命令的工作目录）。</param>
    ''' <param name="targetFolder">需要提交的特定文件夹路径（相对路径或绝对路径均可）。</param>
    ''' <param name="commitInfo">包含提交信息的 CommitSummary 对象。</param>
    ''' <param name="outputMessage">返回执行过程中的标准输出和错误输出信息。</param>
    ''' <returns>返回布尔值，表示是否成功执行且无致命错误。</returns>
    Public Function CommitFolderChanges(
        repoRootPath As String,
        targetFolder As String,
        commitInfo As CommitSummary,
        ByRef outputMessage As String) As Boolean

        outputMessage = String.Empty

        ' 校验参数
        If String.IsNullOrWhiteSpace(repoRootPath) Then
            outputMessage = "Git 仓库根目录路径不能为空。"
            Return False
        End If

        If commitInfo Is Nothing OrElse String.IsNullOrWhiteSpace(commitInfo.Summary) Then
            outputMessage = "CommitSummary 不能为空，且 Summary 至少需要一行内容。"
            Return False
        End If

        Try
            ' ========================================================
            ' 1. 执行 git add 命令，暂存特定文件夹的改动
            ' 使用 --all 参数确保删除的文件也被纳入暂存区
            ' ========================================================
            Dim addArgs As String = $"add --all ""{targetFolder}"""
            Dim addOutput As String = String.Empty
            Dim addSuccess As Boolean = RunGitProcess(repoRootPath, addArgs, addOutput)

            outputMessage &= "[git add 输出]" & Environment.NewLine & addOutput & Environment.NewLine

            ' 如果 add 失败（例如路径不存在），直接返回
            If Not addSuccess Then
                Return False
            End If

            ' ========================================================
            ' 2. 构建 git commit 命令参数
            ' ========================================================
            ' 转义 summary 和 description 中的双引号，防止破坏命令行参数结构
            Dim safeSummary As String = commitInfo.Summary.Replace("""", "\""")
            Dim commitArgs As String = $"commit -m ""{safeSummary}"""

            ' 如果存在 description，则追加第二个 -m 参数（git 会将其作为 body）
            If Not String.IsNullOrWhiteSpace(commitInfo.Description) Then
                Dim safeDesc As String = commitInfo.Description.Replace("""", "\""")
                commitArgs &= $" -m ""{safeDesc}"""
            End If

            ' ========================================================
            ' 3. 执行 git commit 命令
            ' ========================================================
            Dim commitOutput As String = String.Empty
            Dim commitSuccess As Boolean = RunGitProcess(repoRootPath, commitArgs, commitOutput)

            outputMessage &= "[git commit 输出]" & Environment.NewLine & commitOutput

            ' 检查结果：如果没有改动，git 会返回非零状态码，但输出 "nothing to commit"
            If commitOutput.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase) OrElse
               commitOutput.Contains("no changes added to commit", StringComparison.OrdinalIgnoreCase) Then
                outputMessage = "目标文件夹没有需要提交的改动。"
                ' 视业务逻辑而定，没有改动通常被认为是可接受的状态，这里返回 True
                Return True
            End If

            Return commitSuccess

        Catch ex As Exception
            outputMessage &= "执行过程中发生异常: " & ex.Message
            Return False
        End Try

    End Function

    ''' <summary>
    ''' 核心执行函数：启动 git.exe 进程并传入参数
    ''' </summary>
    Private Function RunGitProcess(workingDir As String, arguments As String, ByRef processOutput As String) As Boolean
        Dim psi As New ProcessStartInfo With {
            .FileName = "git",
            .Arguments = arguments,
            .WorkingDirectory = workingDir,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .UseShellExecute = False,
            .CreateNoWindow = True
        }

        Using proc As New Process()
            proc.StartInfo = psi

            ' 同步读取输出流，避免进程死锁
            Dim standardOutput As String = String.Empty
            Dim standardError As String = String.Empty

            ' 开始进程
            proc.Start()

            ' 读取输出（必须在 WaitForExit 之前读取完毕以防止死锁）
            standardOutput = proc.StandardOutput.ReadToEnd()
            standardError = proc.StandardError.ReadToEnd()

            proc.WaitForExit()

            ' 合并输出信息
            If Not String.IsNullOrEmpty(standardOutput) Then
                processOutput &= standardOutput
            End If
            If Not String.IsNullOrEmpty(standardError) Then
                processOutput &= Environment.NewLine & "[Error] " & standardError
            End If

            ' 退出代码为 0 表示成功
            Return proc.ExitCode = 0
        End Using
    End Function
End Module
