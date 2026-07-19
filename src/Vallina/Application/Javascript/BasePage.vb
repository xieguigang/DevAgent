Imports System.Runtime.InteropServices
Imports Galaxy.Workbench

Namespace Javascript

    <ComVisible(True)>
    Public Class BasePage

        Public Const HostObject As String = "devkit"

        Public Async Function OpenEditor() As Task
            Await DirectCast(CommonRuntime.AppHost, Form).InvokeAsync(Sub() Call RibbonMenu.OpenEditor())
        End Function
    End Class
End Namespace