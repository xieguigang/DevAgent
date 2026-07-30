Public Module Interop

    Public Function CreateServer() As CLI.LanguageServer
        Return CLI.LanguageServer.FromEnvironment(App.HOME)
    End Function
End Module
