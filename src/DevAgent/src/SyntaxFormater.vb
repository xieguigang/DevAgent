Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic

Public Module SyntaxFormater

    Public Async Function FormatVBCode(sourceCode As String) As Task(Of String)
        ' 创建 VB 的工作区
        Dim workspace = New AdhocWorkspace()
        ' 解析 VB 代码
        Dim syntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode)
        ' 获取格式化后的语法树根节点
        Dim root = Await syntaxTree.GetRootAsync()
        Dim formattedRoot = Formatter.Format(root, workspace)

        sourceCode = formattedRoot.ToFullString

        Return sourceCode
    End Function

End Module
