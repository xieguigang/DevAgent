Imports CodeMap.CodeIndex

Public Class FormCodeNavMap

    Private Sub FormCodeNavMap_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Call ApplyVsTheme(ToolStrip1)
    End Sub

    Public Function LoadIndex(index As CodeMapIndex) As FormCodeNavMap
        Call Canvas.LoadMap(index)
        Call Canvas.RefreshView()

        Return Me
    End Function
End Class