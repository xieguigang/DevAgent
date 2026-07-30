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
End Module
