Imports System
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.CodeSign

Module Program
    Sub Main(args As String())
        Dim test = VBCodeSignature.SummaryModules("G:\DevAgent\src\DevAgent\src\Configs.vb".ReadAllText, New CodeStatics)

        Pause()
    End Sub
End Module
