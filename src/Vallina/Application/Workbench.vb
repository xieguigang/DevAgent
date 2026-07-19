Module Workbench

    Public ReadOnly Property wwwroot As String
    Public ReadOnly Property port As Integer

    Public Sub StartHttp()

    End Sub

    Private Function GetWebRoot() As String
        If CheckDevelopmentMode() Then
            _wwwroot = "G:\DevAgent\code-editor"
        Else
            _wwwroot = App.HOME & "/apps"
        End If

        Return wwwroot
    End Function

    Private Function CheckDevelopmentMode() As Boolean
        Dim home As String = App.HOME.ToLower.Replace("\", "/").Replace("//", "/")

        If home.StartsWith("g:\devagent") Then
            Return True
        Else
            Return False
        End If
    End Function

End Module
