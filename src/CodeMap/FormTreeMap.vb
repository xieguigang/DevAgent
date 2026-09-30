Imports System.Text
Imports CodeMap.CodeIndex
Imports CodeMap.TreeMap
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports std = System.Math

''' <summary>
''' the code map explorer: a directx accelerated treemap of an opened code
''' repository, presented either flat (2d) or as a code city (3d).
''' </summary>
Public Class FormTreeMap

    ReadOnly index As CodeMapIndex
    ReadOnly treeLayout As New TreeMapLayout()
    ReadOnly tip As New ToolTip()
    ReadOnly poll As New Timer()

    Dim projects As New List(Of CodeNode)()
    ''' <summary>the drill down path, the last entry owns the current roots</summary>
    Dim path As New List(Of CodeNode)()
    Dim mode As TreeMapViewMode = TreeMapViewMode.TwoD
    Dim buildings As New List(Of CityBuilding)()
    ''' <summary>the memoized file text of the zoomed in treemap nodes</summary>
    ReadOnly textCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    ''' <summary>the node whose source text is currently shown in the detail pane</summary>
    Dim focusShown As CodeNode = Nothing
    Dim hovered As CodeNode = Nothing
    Dim downAt As Point
    Dim panning As Boolean = False
    Dim panAt As Point
    Dim treeBuilt As Boolean = False

    ''' <summary>
    ''' the default instance constructor that the winforms infrastructure of the
    ''' project asks for; the explorer needs an index, so the view stays empty
    ''' until one is supplied through the other constructor.
    ''' </summary>
    Sub New()
        Me.New(Nothing)
    End Sub

    Sub New(index As CodeMapIndex)
        Me.index = index

        Call InitializeComponent()

        cboMode.SelectedIndex = 0
        cboMetric.SelectedIndex = 0
        cboLevel.SelectedIndex = 2
        cboExtrude.SelectedIndex = 1
        cboPalette.SelectedIndex = 0

        treeLayout.Metric = TreeMapMetric.Lines
        treeLayout.Level = CodeNodeKind.File
        treeLayout.TextProvider = AddressOf ResolveNodeText

        tip.AutoPopDelay = 8000
        tip.InitialDelay = 150
        tip.ReshowDelay = 100

        poll.Interval = 250

        AddHandler poll.Tick, AddressOf OnPoll
        AddHandler Canvas.Render, AddressOf OnCanvasRender
        AddHandler Canvas.MouseMove, AddressOf OnCanvasMouseMove
        AddHandler Canvas.MouseDown, AddressOf OnCanvasMouseDown
        AddHandler Canvas.MouseUp, AddressOf OnCanvasMouseUp
        AddHandler Canvas.MouseWheel, AddressOf OnCanvasMouseWheel
        AddHandler Canvas.Resize, AddressOf OnCanvasResize

        Call poll.Start()

        lblStatus.Text = "正在构建代码索引 …"
    End Sub

    ' /********************************************************************************/
    '  the index state
    ' /********************************************************************************/

    Private Sub OnPoll(sender As Object, e As EventArgs)
        If index Is Nothing Then
            Call poll.Stop()
            Return
        End If

        Dim st As IndexStatus = index.Status

        If st.phase = "failed" Then
            lblStatus.Text = "索引失败：" & st.message
            lblProgress.Text = ""

            Call poll.Stop()
            Return
        End If

        If st.ready Then
            If Not treeBuilt Then
                treeBuilt = True

                Call BuildTree()

                lblStatus.Text = $"{projects.Count} 个项目 · {st.symbols} 个符号 · {st.elapsedMs} ms"
                lblProgress.Text = ""
            End If

            Call poll.Stop()
            Return
        End If

        lblProgress.Text = $"{st.phase} {st.processedFiles}/{st.totalFiles}"
    End Sub

    Private Sub BuildTree()
        Dim ws As WorkspaceInfo = index.Workspace

        If ws Is Nothing Then
            Return
        End If

        projects = CodeTreeBuilder.Build(ws)
        path.Clear()

        Call treeLayout.SetRoots(projects)
        Call RebuildBreadcrumb()
        Call RefreshView()
    End Sub

    ' /********************************************************************************/
    '  the rendering
    ' /********************************************************************************/

    Private Sub OnCanvasRender(sender As Object, e As DxRenderEventArgs)
        If mode <> TreeMapViewMode.TwoD Then
            ' the 3d city is rendered by the scene pipeline of the canvas itself
            Return
        End If

        If e.Graphics Is Nothing Then
            Return
        End If

        Try
            Call treeLayout.Render(e.Graphics, e.Width, e.Height)
        Catch ex As Exception
            Call Console.Error.WriteLine("[CodeMap] treemap render failed: " & ex.Message)
        End Try
    End Sub

    ''' <summary>re-layout and repaint the current view</summary>
    Private Sub RefreshView()
        If mode = TreeMapViewMode.TwoD Then
            ' a stale city must not be painted underneath the flat treemap
            Call Canvas.ClearScene()

            treeLayout.Level = LayoutLevel()
            Call treeLayout.ResetView()
            Call treeLayout.Invalidate()
            Call Canvas.Invalidate()
        Else
            Call RebuildCity()
        End If
    End Sub

    Private Sub RebuildCity()
        Dim w As Integer = Canvas.Width
        Dim h As Integer = Canvas.Height

        If w <= 0 OrElse h <= 0 Then
            Return
        End If

        ' the city can only extrude a level that the layout actually reached
        If CodeTreeBuilder.KindOrder(LayoutLevel()) >= CodeTreeBuilder.KindOrder(ExtrudeKind()) Then
            treeLayout.Level = LayoutLevel()
        Else
            treeLayout.Level = ExtrudeKind()
        End If

        Try
            ' the very same squarified layout as the 2d view drives the city
            Call treeLayout.Layout(w, h)

            Dim maxHeight As Double = 0.35 * std.Min(w, h)
            Dim faces As Microsoft.VisualBasic.Imaging.Drawing3D.Surface() =
                CityModelBuilder.Build(treeLayout.Nodes,
                                       ExtrudeKind(),
                                       CInt(nudBuildings.Value),
                                       maxHeight,
                                       0.18F,
                                       w, h,
                                       buildings)

            Call Canvas.LoadSurfaces(faces)

            lblProgress.Text = $"{buildings.Count} 栋建筑 · {Canvas.SurfaceCount} 个面"
        Catch ex As Exception
            lblProgress.Text = "3D 城市构建失败：" & ex.Message
        End Try
    End Sub

    ' /********************************************************************************/
    '  the interaction
    ' /********************************************************************************/

    Private Sub OnCanvasMouseMove(sender As Object, e As MouseEventArgs)
        If panning Then
            Call treeLayout.PanBy(e.X - panAt.X, e.Y - panAt.Y)

            panAt = e.Location

            Call Canvas.Invalidate()
            Call UpdateFocusDetail()
            Return
        End If

        Dim node As CodeNode = PickNode(e.X, e.Y)

        If node Is Nothing Then
            hovered = Nothing
            Call tip.SetToolTip(Canvas, "")
            Return
        End If

        If hovered IsNot Nothing AndAlso hovered Is node Then
            Return
        End If

        hovered = node

        Call tip.SetToolTip(Canvas, Describe(node))
    End Sub

    Private Sub OnCanvasMouseDown(sender As Object, e As MouseEventArgs)
        downAt = e.Location

        ' in the flat view the right button scrolls the canvas, the orbit camera
        ' of the 3d view is not involved there
        If e.Button = MouseButtons.Right AndAlso mode = TreeMapViewMode.TwoD Then
            panning = True
            panAt = e.Location
        End If
    End Sub

    ''' <summary>
    ''' the wheel zooms the flat treemap around the cursor: the level stays the
    ''' same, only the rectangles grow, so a node can be inspected without
    ''' drilling down into it.
    ''' </summary>
    Private Sub OnCanvasMouseWheel(sender As Object, e As MouseEventArgs)
        If mode <> TreeMapViewMode.TwoD Then
            ' the 3d view uses the wheel for the camera distance of the canvas
            Return
        End If

        Dim factor As Double = If(e.Delta > 0, 1.25, 1.0 / 1.25)

        Call treeLayout.ZoomAt(e.X, e.Y, factor)
        Call Canvas.Invalidate()
        Call ShowZoom()
        Call UpdateFocusDetail()
    End Sub

    ''' <summary>
    ''' the detail pane follows the node that sits under the center of the
    ''' viewport, so zooming and panning reads the code around the view center.
    ''' </summary>
    Private Sub UpdateFocusDetail()
        If mode <> TreeMapViewMode.TwoD OrElse Not treeBuilt Then
            Return
        End If

        Dim f As CodeNode = treeLayout.FocusAt(Canvas.Width, Canvas.Height)

        If f Is Nothing OrElse f Is focusShown Then
            Return
        End If

        focusShown = f

        Call ShowDetail(f)
    End Sub

    Private Sub ShowZoom()
        lblProgress.Text = $"zoom {treeLayout.Zoom:F2}x · {treeLayout.Nodes.Count} 个节点 · 度量 {treeLayout.MinValue:0}-{treeLayout.MaxValue:0}"
    End Sub

    Private Sub OnCanvasMouseUp(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            panning = False
            Return
        End If

        If e.Button <> MouseButtons.Left Then
            Return
        End If

        ' a drag belongs to the camera of the canvas, not to a selection
        If std.Abs(e.X - downAt.X) > 4 OrElse std.Abs(e.Y - downAt.Y) > 4 Then
            Return
        End If

        Dim node As CodeNode = PickNode(e.X, e.Y)

        If node Is Nothing Then
            Return
        End If

        Call ShowDetail(node)

        If Not node.IsLeaf Then
            Call path.Add(node)
            Call treeLayout.SetRoots(node.Children)
            Call RebuildBreadcrumb()
            Call RefreshView()
        End If
    End Sub

    Private Sub OnCanvasResize(sender As Object, e As EventArgs)
        Call Canvas.UpdateViewport()

        If mode = TreeMapViewMode.ThreeD Then
            Return
        End If

        Call treeLayout.Invalidate()
    End Sub

    Private Function PickNode(x As Integer, y As Integer) As CodeNode
        If mode = TreeMapViewMode.TwoD Then
            Return treeLayout.HitTest(x, y)
        End If

        Dim b As CityBuilding = CityModelBuilder.Pick(Canvas, buildings, x, y)

        Return If(b Is Nothing, Nothing, b.Node)
    End Function

    Private Shared Function Describe(node As CodeNode) As String
        Dim sb As New StringBuilder()

        Call sb.AppendLine($"[{node.KindName}] {node.FullName}")

        If Not String.IsNullOrEmpty(node.Path) Then
            Call sb.AppendLine("路径：" & node.Path)
        End If

        Call sb.AppendLine($"行数 {node.Lines} · 字符 {node.Chars} · 符号 {node.SymbolCount}")

        If node.Symbol IsNot Nothing Then
            Call sb.AppendLine($"{node.Symbol.RelativeFile}:{node.Symbol.StartLine}-{node.Symbol.EndLine}")
        End If

        Return sb.ToString().Trim()
    End Function

    Private Sub ShowDetail(node As CodeNode)
        Dim sb As New StringBuilder()

        Call sb.AppendLine($"[{node.KindName}] {node.FullName}")
        Call sb.AppendLine($"path    : {node.Path}")
        Call sb.AppendLine($"metrics : lines={node.Lines} chars={node.Chars} symbols={node.SymbolCount}")

        If node.Symbol IsNot Nothing Then
            Call sb.AppendLine($"location: {node.Symbol.File}:{node.Symbol.StartLine}-{node.Symbol.EndLine}")
            Call sb.AppendLine($"project : {node.Symbol.Project}")
            Call sb.AppendLine()
            Call sb.AppendLine(node.Symbol.Code)
            Return
        End If

        Dim text As String = ResolveNodeText(node)

        If text.Length > 0 Then
            If text.Length > 8000 Then
                text = text.Substring(0, 8000) & vbCrLf & "... (truncated)"
            End If

            Call sb.AppendLine()
            Call sb.AppendLine(text)
        Else
            Call sb.AppendLine()
            Call sb.AppendLine("(容器节点，双击可继续下钻)")
        End If

        TxtDetail.Text = sb.ToString()
    End Sub

    ' /********************************************************************************/
    '  the breadcrumb
    ' /********************************************************************************/

    Private Sub RebuildBreadcrumb()
        Call FlowBreadcrumb.Controls.Clear()

        Dim root As New LinkLabel() With {
            .Text = "workspace",
            .AutoSize = True,
            .LinkColor = Color.FromArgb(45, 212, 191)
        }

        AddHandler root.Click, Sub(s, e) Call GoToLevel(-1)

        Call FlowBreadcrumb.Controls.Add(root)

        For i As Integer = 0 To path.Count - 1
            Dim sep As New Label() With {
                .Text = " › ",
                .AutoSize = True,
                .ForeColor = Color.FromArgb(148, 163, 184)
            }

            Call FlowBreadcrumb.Controls.Add(sep)

            Dim level As Integer = i
            Dim link As New LinkLabel() With {
                .Text = path(i).Name,
                .AutoSize = True,
                .LinkColor = Color.FromArgb(56, 189, 248)
            }

            AddHandler link.Click, Sub(s, e) Call GoToLevel(level)

            Call FlowBreadcrumb.Controls.Add(link)
        Next
    End Sub

    Private Sub GoToLevel(level As Integer)
        While path.Count > level + 1
            Call path.RemoveAt(path.Count - 1)
        End While

        Dim roots As List(Of CodeNode) = If(path.Count = 0, projects, path(path.Count - 1).Children)

        Call treeLayout.SetRoots(roots)
        Call RebuildBreadcrumb()
        Call RefreshView()
    End Sub

    ' /********************************************************************************/
    '  the toolbar
    ' /********************************************************************************/

    ''' <summary>
    ''' resolve the raw source text of a leaf node for the zoomed in treemap.
    ''' A type or a member carries its text in the symbol, a file level node
    ''' reads its physical file on demand and the result is memoized.
    ''' </summary>
    Private Function ResolveNodeText(node As CodeNode) As String
        If node Is Nothing Then
            Return ""
        End If

        If node.Symbol IsNot Nothing Then
            Return If(node.Symbol.Code, "")
        End If

        If String.IsNullOrEmpty(node.SourceFile) Then
            Return ""
        End If

        Dim cached As String = Nothing

        If textCache.TryGetValue(node.SourceFile, cached) Then
            Return cached
        End If

        Try
            If Not IO.File.Exists(node.SourceFile) Then
                Return ""
            End If

            Dim text As String = IO.File.ReadAllText(node.SourceFile)

            If textCache.Count > 64 Then
                Call textCache.Clear()
            End If

            textCache(node.SourceFile) = text

            Return text
        Catch
            Return ""
        End Try
    End Function

    ''' <summary>the deepest hierarchy level that the 2d layout descends to</summary>
    Private Function LayoutLevel() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboLevel.SelectedIndex < 0, 2, cboLevel.SelectedIndex))
    End Function

    ''' <summary>the hierarchy level that becomes one building of the 3d city</summary>
    Private Function ExtrudeKind() As CodeNodeKind
        Return CodeTreeBuilder.KindOfIndex(If(cboExtrude.SelectedIndex < 0, 1, cboExtrude.SelectedIndex) + 1)
    End Function

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
        Call poll.Stop()
        MyBase.OnFormClosed(e)
    End Sub

End Class
