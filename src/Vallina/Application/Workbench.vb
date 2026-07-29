Imports Fluteway
Imports Ollama
Imports VallinaDevelopment.Settings

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

    Public ReadOnly Property config As ConfigJSON

    Public Sub LoadConfig()
        _config = ConfigJSON.Load
    End Sub

    Public Function CreateLLM() As LLMClient
        Dim config As llm = _config.llm
        Dim provider As ILLMProvider = LLMUrl.Create(config.endpoint, config.apiKey)

        Return New LLMClient(provider, config.model) With {
            .max_context_tokens = config.maxTokens,
            .temperature = config.temperature
        }
    End Function

    Public Sub SaveConfig()
        _config.Save()
    End Sub

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

        If home.StartsWith("g:/devagent") Then
            Return True
        Else
            Return False
        End If
    End Function

End Module
