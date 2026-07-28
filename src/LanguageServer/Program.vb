Imports Flute.Http.Configurations
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.InteropService.SharedORM
Imports Microsoft.VisualBasic.CommandLine.Reflection

<CLI> Module Program

    Public Function Main(args As String()) As Integer
        Return GetType(Program).RunCLI(App.CommandLine)
    End Function

    <ExportAPI("--listen")>
    <Usage("--listen [--port=8015]")>
    Public Function Run(Optional port As Integer = 8015, Optional args As CommandLine = Nothing) As Integer
        Return New Services(port, New Configuration).Run()
    End Function
End Module
