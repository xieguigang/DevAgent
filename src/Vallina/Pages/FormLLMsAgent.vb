Imports Microsoft.VisualBasic.ApplicationServices

Public Class FormLLMsAgent

    Public Property Workspace As String

    Private Sub FormLLMsAgent_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim llmconfig = Workbench.config.llm
        Dim config As New DevAgent.AppConfig With {
            .ApiKey = llmconfig.apiKey,
            .MaxBuildFix = 15,
            .MaxRunFix = 30
        }
        Dim tempfile As String = TempFileSystem.GetAppSysTempFile(".ini", sessionID:=App.PID, prefix:="appconfig-")

        Call config.Save(tempfile)

        ConsoleControl1.StartProcess(App.HOME & "/DevAgent.exe", $"--repl --project {Workspace.CLIPath} -c={tempfile.CLIPath} --url={llmconfig.endpoint.CLIToken} --model={llmconfig.model}")
    End Sub

    Private Sub ConsoleControl1_ProcessExisted() Handles ConsoleControl1.ProcessExisted
        Call Invoke(Sub() Me.Close())
    End Sub

    Protected Overrides Sub CopyFullPath()
        Call Clipboard.SetText(Workspace)
    End Sub

    Protected Overrides Sub OpenContainingFolder()
        Call Process.Start("explorer.exe", Workspace)
    End Sub
End Class