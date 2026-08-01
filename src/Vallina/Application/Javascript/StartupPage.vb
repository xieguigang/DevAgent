Imports System.Runtime.InteropServices
Imports Galaxy.Workbench

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
            Using wd As New FolderBrowserDialog With {.ShowNewFolderButton = True}
                If wd.ShowDialog = DialogResult.OK Then
                    Await Task.Run(Sub() Call RibbonMenu.LaunchLLMAgent(wd.SelectedPath))
                End If
            End Using
        End Function

        Public Async Function newDocument() As Task
            Await Task.Run(
                Sub()
                    Call DirectCast(CommonRuntime.AppHost, Form).Invoke(Sub() Call RibbonMenu.OpenEditor())
                End Sub)
        End Function

        Public Async Function openDocument() As Task
            Using file As New OpenFileDialog With {.Filter = "VisualBasic(*.vb);Rscript(*.r)|*.vb;*.r"}
                If file.ShowDialog = DialogResult.OK Then
                    Await Task.Run(Sub() Call RibbonMenu.OpenFileEdit(file.FileName))
                End If
            End Using
        End Function

        Public Async Function newProject() As Task

        End Function

        Public Async Function openFolder() As Task
            Using folder As New FolderBrowserDialog With {.ShowNewFolderButton = True}
                If folder.ShowDialog = DialogResult.OK Then

                End If
            End Using
        End Function
    End Class
End Namespace