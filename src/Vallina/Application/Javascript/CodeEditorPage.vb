Imports System.Runtime.InteropServices
Imports Galaxy.Workbench

Namespace Javascript

    <ComVisible(True)>
    Public Class CodeEditorPage : Inherits BasePage

        Public Async Function updateStatus(line As String, col As String, lang As String, file As String) As Task
            Await DirectCast(CommonRuntime.AppHost, FormMain).UpdateEditorStatus(line, col, lang, file)
        End Function
    End Class
End Namespace