Imports Flute.Http.Configurations

Module Program
    Sub Main(args As String())
        Call New Services(8015, New Configuration).Run()
    End Sub
End Module
