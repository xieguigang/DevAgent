Imports System.Runtime.InteropServices

Namespace Javascript

    <ComVisible(True)>
    Public Class StartupPage : Inherits BasePage

        Public Async Function openProject() As Task
            Using file As New OpenFileDialog With {
               .Filter = "VisualBasic Project(*.vbproj)|*.vbproj"
           }
                If file.ShowDialog = DialogResult.OK Then
                    Await Task.Run(Sub() Call RibbonMenu.OpenSolutionExplorer(file.FileName))
                End If
            End Using
        End Function

        Public Async Function openSettings() As Task
            Await Task.Run(Sub() Call RibbonMenu.OpenSettingsPage())
        End Function

        Public Async Function openLLMAgent() As Task
            Await Task.Run(Sub() Call RibbonMenu.LaunchLLMAgent())
        End Function

        Public Async Function newDocument() As Task

        End Function

        Public Async Function openDocument() As Task

        End Function

        Public Async Function newProject() As Task

        End Function
    End Class
End Namespace