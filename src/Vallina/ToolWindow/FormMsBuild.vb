Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.ApplicationServices

Public Class FormMsBuild

    Public Property proj As String

    Private Sub FormMsBuild_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call RunDotNETBuild()
    End Sub

    Public Sub RunDotNETBuild()
        Call WebViewConsole1.ClearOutput()
        Call WebViewConsole1.StartProcess("dotnet", $"build {proj.CLIPath}", proj.ParentPath)
        Call CommonRuntime.GetOutputWindow.AddLog("msbuild", "build project " & proj)
    End Sub
End Class