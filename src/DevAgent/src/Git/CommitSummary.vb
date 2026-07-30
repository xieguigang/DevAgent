''' <summary>
''' git commit 信息的数据结构，对应 { summary, description }。
''' </summary>
Public Class CommitSummary

    ''' <summary>
    ''' 简短的一行提交标题，用于 git commit 的 summary（如 <c>git commit -m</c> 的参数）。
    ''' </summary>
    Public Property Summary As String

    ''' <summary>
    ''' 详细的提交说明，用于 git commit 的 description / body。
    ''' </summary>
    Public Property Description As String

    Public Sub New()
    End Sub

    Public Sub New(summary As String, description As String)
        Me.Summary = summary
        Me.Description = description
    End Sub

End Class