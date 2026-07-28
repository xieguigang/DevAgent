
''' <summary>
''' 成员信息结构
''' </summary>
Public Class LanguageMemberInfo

    Public ReadOnly Label As String
    Public ReadOnly Kind As String
    Public ReadOnly Detail As String
    Public ReadOnly InsertText As String

    Public Sub New(label As String, kind As String, detail As String, insertText As String)
        Me.Label = label
        Me.Kind = kind
        Me.Detail = detail
        Me.InsertText = If(insertText, label)
    End Sub
End Class