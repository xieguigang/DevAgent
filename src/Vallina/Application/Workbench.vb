Imports Fluteway

Module Workbench

    Public ReadOnly Property wwwroot As String
    Public ReadOnly Property port As Integer
        Get
            If Not http Is Nothing Then
                Return http.port
            Else
                Return -1
            End If
        End Get
    End Property

    Dim WithEvents http As HttpServices

    Public Sub StartHttp()
        http = New HttpServices(GetWebRoot)
        http.StartHttp()
    End Sub

    Public Sub KillHttp()
        If Not http Is Nothing Then
            Call http.Dispose()
        End If
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
