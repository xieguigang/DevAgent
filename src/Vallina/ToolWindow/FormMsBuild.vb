Imports Microsoft.VisualBasic.ApplicationServices

Public Class FormMsBuild

    Public Property proj As String

    Private Sub FormMsBuild_Load(sender As Object, e As EventArgs) Handles Me.Load
        Call WebViewConsole1.StartProcess("dotnet", $"build {proj.CLIPath}", proj.ParentPath)
    End Sub
End Class