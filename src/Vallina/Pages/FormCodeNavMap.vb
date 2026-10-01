Imports CodeMap.CodeIndex
Imports CodeMap.TreeMap
Imports Galaxy.Workbench
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports RibbonLib.Interop

Public Class FormCodeNavMap

    Shared ReadOnly btnFit As RibbonEventBinding
    Shared ReadOnly btnBack As RibbonEventBinding
    Shared ReadOnly btnSnapshot As RibbonEventBinding

    Shared Sub New()
        btnFit = New RibbonEventBinding(RibbonMenu.Ribbon.ButtonFit)
        btnBack = New RibbonEventBinding(RibbonMenu.Ribbon.ButtonBack)
        btnSnapshot = New RibbonEventBinding(RibbonMenu.Ribbon.ButtonMapSnapshot)
    End Sub

    Private Sub FormCodeNavMap_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Call ActivateRibbon()
        Call ApplyVsTheme(ToolStrip1)
    End Sub

    Private Sub ActivateRibbon()
        RibbonMenu.Ribbon.TabCodeNavMap.ContextAvailable = ContextAvailability.Active

        Call btnFit.Addhandler(AddressOf FitView)
        Call btnBack.Addhandler(AddressOf BackToUpLevel)
        Call btnSnapshot.Addhandler(AddressOf MakeSnapshot)
    End Sub

    Public Function LoadIndex(index As CodeMapIndex) As FormCodeNavMap
        Call Canvas.LoadMap(index)
        Call Canvas.RefreshView()

        Return Me
    End Function

    Private Sub cboMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMode.SelectedIndexChanged
        If cboMode.SelectedIndex < 0 Then
            Return
        End If

        Canvas.Mode = If(cboMode.SelectedIndex = 0, TreeMapViewMode.TwoD, TreeMapViewMode.ThreeD)

        cboExtrude.Enabled = (Canvas.Mode = TreeMapViewMode.ThreeD)
        ' nudBuildings.Enabled = (Canvas.Mode = TreeMapViewMode.ThreeD)
        ' "fit view" resets the zoom of the flat map and the camera of the city
        btnFit.Enabled = True

        Call Canvas.RefreshView()
    End Sub

    Private Sub cboMetric_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMetric.SelectedIndexChanged
        Select Case cboMetric.SelectedIndex
            Case 1
                Canvas.treeLayout.Metric = TreeMapMetric.Chars
            Case 2
                Canvas.treeLayout.Metric = TreeMapMetric.Symbols
            Case Else
                Canvas.treeLayout.Metric = TreeMapMetric.Lines
        End Select

        If Canvas.TreeBuilt Then
            Call Canvas.RefreshView()
        End If
    End Sub

    Private Sub cboLevel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLevel.SelectedIndexChanged
        If Canvas.TreeBuilt Then
            Call Canvas.RefreshView()
        End If
    End Sub

    Private Sub cboExtrude_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboExtrude.SelectedIndexChanged
        If Canvas.TreeBuilt AndAlso Canvas.Mode = TreeMapViewMode.ThreeD Then
            Call Canvas.RebuildCity()
        End If
    End Sub

    Private Sub nudBuildings_ValueChanged(sender As Object, e As EventArgs) ' Handles nudBuildings.ValueChanged
        If Canvas.TreeBuilt AndAlso Canvas.Mode = TreeMapViewMode.ThreeD Then
            Call Canvas.RebuildCity()
        End If
    End Sub

    Private Sub BackToUpLevel()
        If Canvas.PathLength = 0 Then
            Return
        End If

        Call Canvas.GoToLevel(Canvas.PathLength - 2)
    End Sub

    Private Sub cboPalette_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboPalette.SelectedIndexChanged
        Select Case cboPalette.SelectedIndex
            Case 0
                Canvas.treeLayout.Palette = ScalerPalette.viridis
                Canvas.treeLayout.UseHeatmap = True
            Case 1
                Canvas.treeLayout.Palette = ScalerPalette.magma
                Canvas.treeLayout.UseHeatmap = True
            Case 2
                Canvas.treeLayout.Palette = ScalerPalette.inferno
                Canvas.treeLayout.UseHeatmap = True
            Case 3
                Canvas.treeLayout.Palette = ScalerPalette.plasma
                Canvas.treeLayout.UseHeatmap = True
            Case 4
                Canvas.treeLayout.Palette = ScalerPalette.turbo
                Canvas.treeLayout.UseHeatmap = True
            Case 5
                Canvas.treeLayout.Palette = ScalerPalette.Jet
                Canvas.treeLayout.UseHeatmap = True
            Case 6
                Canvas.treeLayout.Palette = ScalerPalette.Hot
                Canvas.treeLayout.UseHeatmap = True
            Case 7
                Canvas.treeLayout.Palette = ScalerPalette.Cool
                Canvas.treeLayout.UseHeatmap = True
            Case 8
                Canvas.treeLayout.Palette = ScalerPalette.Rainbow
                Canvas.treeLayout.UseHeatmap = True
            Case Else
                ' 按层级着色
                Canvas.treeLayout.UseHeatmap = False
        End Select

        Call Canvas.treeLayout.InvalidateColors()

        If Canvas.TreeBuilt Then
            If Canvas.Mode = TreeMapViewMode.ThreeD Then
                Call Canvas.RebuildCity()
            Else
                Call Canvas.Invalidate()
            End If
        End If
    End Sub

    Private Sub FitView()
        If Canvas.Mode = TreeMapViewMode.ThreeD Then
            Call Canvas.ResetView()
        Else
            Call Canvas.treeLayout.ResetView()
            Call Canvas.treeLayout.Invalidate()
            Call Canvas.Invalidate()
            Call Canvas.ShowZoom()
        End If
    End Sub

    Private Sub MakeSnapshot()
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
        Canvas.NudBuildings = AddressOf GetNudBuildings

        cboMode.SelectedIndex = 0
        cboMetric.SelectedIndex = 0
        cboLevel.SelectedIndex = 2
        cboExtrude.SelectedIndex = 1
        cboPalette.SelectedIndex = 0

        CommonRuntime.StatusMessage("正在构建代码索引 …")
    End Sub

    Private Function ExtrudeKind() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboExtrude.SelectedIndex < 0, 1, cboExtrude.SelectedIndex) + 1)
    End Function

    Private Function LayoutLevel() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboLevel.SelectedIndex < 0, 2, cboLevel.SelectedIndex))
    End Function

    Private Function GetNudBuildings() As Integer
        Return 10000
    End Function

    Private Sub Canvas_DisplayDetails(details As String) Handles Canvas.DisplayDetails
        Call CommonRuntime.GetOutputWindow.AppendLine(details)
    End Sub

    Private Sub Canvas_DisplayStatus(message As String) Handles Canvas.DisplayStatus
        CommonRuntime.StatusMessage(message)
    End Sub

    Private Sub Canvas_DisplayProgress(p As Double, message As String) Handles Canvas.DisplayProgress
        Call CommonRuntime.StatusMessage(message)
    End Sub

    Private Sub Canvas_BreadcrumbDistory() Handles Canvas.BreadcrumbDistory
        ' Call FlowBreadcrumb.Controls.Clear()
    End Sub

    Private Sub Canvas_BreadcrumbSetup(control As Control) Handles Canvas.BreadcrumbSetup
        ' Call FlowBreadcrumb.Controls.Add(control)
    End Sub

    Private Sub FormCodeNavMap_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        Call ActivateRibbon()
    End Sub

    Private Sub FormCodeNavMap_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        RibbonMenu.Ribbon.TabCodeNavMap.ContextAvailable = ContextAvailability.NotAvailable

        Call btnSnapshot.ClearHook()
        Call btnFit.ClearHook()
        Call btnBack.ClearHook()
    End Sub
End Class