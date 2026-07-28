Public Structure LanguageMemberInfo
    Public Label As String
    Public Kind As String
    Public Detail As String
    Public InsertText As String

    Public Sub New(label As String, kind As String, detail As String, insertText As String)
        Me.Label = label
        Me.Kind = kind
        Me.Detail = detail
        Me.InsertText = insertText
    End Sub
End Structure
