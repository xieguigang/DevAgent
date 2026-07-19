Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.Serialization.JSON
Imports VallinaDevelopment.Settings

Namespace Javascript

    <ComVisible(True)>
    Public Class SettingsPage : Inherits BasePage

        ReadOnly page As FormSettingsPage

        Sub New(page As FormSettingsPage)
            Me.page = page
        End Sub

        Public Async Function SaveAndClose(json As String) As Task
            Await Save(json)
            Await page.InvokeAsync(Sub() page.Close())
        End Function

        Public Async Function Save(json As String) As Task(Of Boolean)
            Dim flag As Boolean = Await Task.Run(Function() json.LoadJSON(Of ConfigJSON).Save())
            Call Workbench.LoadConfig()
            Return flag
        End Function
    End Class
End Namespace