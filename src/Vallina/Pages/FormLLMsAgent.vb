Imports Microsoft.VisualBasic.ApplicationServices

Public Class FormLLMsAgent

    Private Sub FormLLMsAgent_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim llmconfig = Workbench.config.llm
        Dim config As New DevAgent.AppConfig With {
            .ApiKey = llmconfig.apiKey,
            .MaxBuildFix = 15,
            .MaxRunFix = 30
        }
        Dim tempfile As String = TempFileSystem.GetAppSysTempFile(".ini", sessionID:=App.PID, prefix:="appconfig-")

        Call config.Save(tempfile)

        ConsoleControl1.StartProcess(App.HOME & "/DevAgent.exe", $"--repl -c={tempfile.CLIPath} --url={llmconfig.endpoint.CLIToken} --model={llmconfig.model}")
    End Sub

    Private Sub ConsoleControl1_ProcessExisted() Handles ConsoleControl1.ProcessExisted
        Call Invoke(Sub() Me.Close())
    End Sub
End Class