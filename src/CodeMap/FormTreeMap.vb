Imports System.Text
Imports CodeMap.CodeIndex
Imports CodeMap.TreeMap
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.PostScript.Elements
Imports std = System.Math

''' <summary>
''' the code map explorer: a directx accelerated treemap of an opened code
''' repository, presented either flat (2d) or as a code city (3d).
''' </summary>
Public Class FormTreeMap



    ''' <summary>
    ''' the default instance constructor that the winforms infrastructure of the
    ''' project asks for; the explorer needs an index, so the view stays empty
    ''' until one is supplied through the other constructor.
    ''' </summary>
    Sub New()
        Me.New(Nothing)
    End Sub

    Sub New(index As CodeMapIndex)
        Call InitializeComponent()

        cboMode.SelectedIndex = 0
        cboMetric.SelectedIndex = 0
        cboLevel.SelectedIndex = 2
        cboExtrude.SelectedIndex = 1
        cboPalette.SelectedIndex = 0

        lblStatus.Text = "正在构建代码索引 …"
    End Sub

    Private Sub cboMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMode.SelectedIndexChanged
        If cboMode.SelectedIndex < 0 Then
            Return
        End If

        mode = If(cboMode.SelectedIndex = 0, TreeMapViewMode.TwoD, TreeMapViewMode.ThreeD)

        cboExtrude.Enabled = (mode = TreeMapViewMode.ThreeD)
        nudBuildings.Enabled = (mode = TreeMapViewMode.ThreeD)
        ' "fit view" resets the zoom of the flat map and the camera of the city
        btnFit.Enabled = True

        Call RefreshView()
    End Sub

    Private Sub cboMetric_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMetric.SelectedIndexChanged
        Select Case cboMetric.SelectedIndex
            Case 1
                treeLayout.Metric = TreeMapMetric.Chars
            Case 2
                treeLayout.Metric = TreeMapMetric.Symbols
            Case Else
                treeLayout.Metric = TreeMapMetric.Lines
        End Select

        If treeBuilt Then
            Call RefreshView()
        End If
    End Sub

    Private Sub cboLevel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLevel.SelectedIndexChanged
        If treeBuilt Then
            Call RefreshView()
        End If
    End Sub

    Private Sub cboExtrude_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboExtrude.SelectedIndexChanged
        If treeBuilt AndAlso mode = TreeMapViewMode.ThreeD Then
            Call RebuildCity()
        End If
    End Sub

    Private Sub nudBuildings_ValueChanged(sender As Object, e As EventArgs) Handles nudBuildings.ValueChanged
        If treeBuilt AndAlso mode = TreeMapViewMode.ThreeD Then
            Call RebuildCity()
        End If
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        If path.Count = 0 Then
            Return
        End If

        Call GoToLevel(path.Count - 2)
    End Sub

    Private Sub cboPalette_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboPalette.SelectedIndexChanged
        Select Case cboPalette.SelectedIndex
            Case 0
                treeLayout.Palette = ScalerPalette.viridis
                treeLayout.UseHeatmap = True
            Case 1
                treeLayout.Palette = ScalerPalette.magma
                treeLayout.UseHeatmap = True
            Case 2
                treeLayout.Palette = ScalerPalette.inferno
                treeLayout.UseHeatmap = True
            Case 3
                treeLayout.Palette = ScalerPalette.plasma
                treeLayout.UseHeatmap = True
            Case 4
                treeLayout.Palette = ScalerPalette.turbo
                treeLayout.UseHeatmap = True
            Case 5
                treeLayout.Palette = ScalerPalette.Jet
                treeLayout.UseHeatmap = True
            Case 6
                treeLayout.Palette = ScalerPalette.Hot
                treeLayout.UseHeatmap = True
            Case 7
                treeLayout.Palette = ScalerPalette.Cool
                treeLayout.UseHeatmap = True
            Case 8
                treeLayout.Palette = ScalerPalette.Rainbow
                treeLayout.UseHeatmap = True
            Case Else
                ' 按层级着色
                treeLayout.UseHeatmap = False
        End Select

        Call treeLayout.InvalidateColors()

        If treeBuilt Then
            If mode = TreeMapViewMode.ThreeD Then
                Call RebuildCity()
            Else
                Call Canvas.Invalidate()
            End If
        End If
    End Sub

    Private Sub btnFit_Click(sender As Object, e As EventArgs) Handles btnFit.Click
        If mode = TreeMapViewMode.ThreeD Then
            Call Canvas.ResetView()
        Else
            Call treeLayout.ResetView()
            Call treeLayout.Invalidate()
            Call Canvas.Invalidate()
            Call ShowZoom()
        End If
    End Sub

    Private Sub btnSnapshot_Click(sender As Object, e As EventArgs) Handles btnSnapshot.Click
        Using dlg As New SaveFileDialog() With {
            .Filter = "png image|*.png",
            .FileName = "codemap.png"
        }
            If dlg.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If

            Try
                Call Canvas.SaveSnapshot(dlg.FileName)
            Catch ex As Exception
                Call MessageBox.Show(Me, ex.Message, "CodeMap", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        Canvas.Close()
        MyBase.OnFormClosed(e)
    End Sub

    Private Sub FormTreeMap_Load(sender As Object, e As EventArgs) Handles Me.Load
        Canvas.ExtrudeKind = AddressOf ExtrudeKind
        Canvas.LayoutLevel = AddressOf LayoutLevel
    End Sub

    Private Function ExtrudeKind() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboExtrude.SelectedIndex < 0, 1, cboExtrude.SelectedIndex) + 1)
    End Function

    Private Function LayoutLevel() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboLevel.SelectedIndex < 0, 2, cboLevel.SelectedIndex))
    End Function

    Private Function GetNudBuildings() As Integer
        Return nudBuildings.Value
    End Function

    Private Sub Canvas_DisplayDetails(details As String) Handles Canvas.DisplayDetails
        TxtDetail.Text = details
    End Sub

    Private Sub Canvas_DisplayStatus(message As String) Handles Canvas.DisplayStatus
        lblStatus.Text = message
    End Sub

    Private Sub Canvas_DisplayProgress(p As Double, message As String) Handles Canvas.DisplayProgress
        lblProgress.Text = message
    End Sub

    Private Sub Canvas_BreadcrumbDistory() Handles Canvas.BreadcrumbDistory
        Call FlowBreadcrumb.Controls.Clear()
    End Sub

    Private Sub Canvas_BreadcrumbSetup(control As Control) Handles Canvas.BreadcrumbSetup
        Call FlowBreadcrumb.Controls.Add(control)
    End Sub
End Class
