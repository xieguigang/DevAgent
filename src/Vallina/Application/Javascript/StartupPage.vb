Imports System.Runtime.InteropServices

Namespace Javascript

    <ComVisible(True)>
    Public Class StartupPage : Inherits BasePage

        Public Async Function OpenProject() As Task
            Using file As New OpenFileDialog With {
               .Filter = "VisualBasic Project(*.vbproj)|*.vbproj"
           }
                If file.ShowDialog = DialogResult.OK Then
                    Await Task.Run(Sub() Call RibbonMenu.OpenSolutionExplorer(file.FileName))
                End If
            End Using
        End Function

    End Class
End Namespace