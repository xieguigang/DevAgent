Imports Microsoft.VisualBasic.CommandLine.Reflection

Public Class Opts

    <Opt("--project", "-p")>
    Public Property projectPath As String

    <Opt("--requirements", "-r")>
    Public Property requirements As String

    <Opt("--requirements-file", "-rf")>
    Public Property requirementFile As String

    <Opt("--model", "-m")>
    Public Property model As String = "llama3.2"

    <Opt("--url", "-u")>
    Public Property ollamaUrl As String = "http://localhost:11434"

    <Opt("--key", "-k")>
    Public Property apikey As String

    <Opt("--max-build-fix")>
    Public Property maxBuildFix As Integer = 8

    <Opt("--max-run-fix")>
    Public Property maxRunFix As Integer = 5

    <Opt("--help", "-h")>
    Public Property help As Boolean = False

    Public Function ResolveFile() As Opts
        If Not requirementFile.StringEmpty Then
            requirements = requirementFile.ReadAllText
        End If

        Return Me
    End Function

End Class
