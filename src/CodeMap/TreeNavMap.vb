Imports System.Runtime.CompilerServices
Imports System.Text
Imports CodeMap.CodeIndex
Imports CodeMap.TreeMap
Imports Microsoft.VisualBasic.Drawing.DirectX
Imports std = System.Math

Public Class TreeNavMap

    Dim WithEvents index As CodeMapIndex
    ReadOnly treeLayout As New TreeMapLayout()
    ReadOnly tip As New ToolTip()
    Dim WithEvents poll As New Timer()

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

    Public Overloads Sub LoadMap(index As CodeMapIndex)
        Me.index = index

        treeLayout.Metric = TreeMapMetric.Lines
        treeLayout.Level = CodeNodeKind.File
        treeLayout.TextProvider = AddressOf ResolveNodeText

        tip.AutoPopDelay = 8000
        tip.InitialDelay = 150
        tip.ReshowDelay = 100

        poll.Interval = 250
        poll.Start()
    End Sub

    ' /********************************************************************************/
    '  the index state
    ' /********************************************************************************/

    Private Sub OnPoll(sender As Object, e As EventArgs) Handles poll.Tick
        If index Is Nothing Then
            Call poll.Stop()
            Return
        End If

        Dim st As IndexStatus = index.Status

        If st.phase = "failed" Then
            RaiseEvent DisplayStatus("索引失败：" & st.message)
            RaiseEvent DisplayProgress(1, "")

            Call poll.Stop()
            Return
        End If

        If st.ready Then
            If Not treeBuilt Then
                treeBuilt = True

                Call BuildTree()

                RaiseEvent DisplayStatus($"{projects.Count} 个项目 · {st.symbols} 个符号 · {st.elapsedMs} ms")
                RaiseEvent DisplayProgress(1, "")
            End If

            Call poll.Stop()
            Return
        End If

        RaiseEvent DisplayProgress(st.processedFiles / st.totalFiles, $"{st.phase} {st.processedFiles}/{st.totalFiles}")
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

    Private Sub OnCanvasRender(sender As Object, e As DxRenderEventArgs) Handles Canvas.Render
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

            treeLayout.Level = _LayoutLevel()
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
        If CodeTreeBuilder.KindOrder(_LayoutLevel()) >= CodeTreeBuilder.KindOrder(_ExtrudeKind()) Then
            treeLayout.Level = _LayoutLevel()
        Else
            treeLayout.Level = _ExtrudeKind()
        End If

        Try
            ' the very same squarified layout as the 2d view drives the city
            Call treeLayout.Layout(w, h)

            Dim maxHeight As Double = 0.35 * std.Min(w, h)
            Dim faces As Microsoft.VisualBasic.Imaging.Drawing3D.Surface() =
                CityModelBuilder.Build(treeLayout.Nodes,
                                       _ExtrudeKind(),
                                       _NudBuildings(),
                                       maxHeight,
                                       0.18F,
                                       w, h,
                                       buildings)

            Call Canvas.LoadSurfaces(faces)

            RaiseEvent DisplayStatus($"{buildings.Count} 栋建筑 · {Canvas.SurfaceCount} 个面")
        Catch ex As Exception
            RaiseEvent DisplayStatus("3D 城市构建失败：" & ex.Message)
        End Try
    End Sub

    Public Property NudBuildings As Func(Of Integer)

    ' /********************************************************************************/
    '  the interaction
    ' /********************************************************************************/

    Private Sub OnCanvasMouseMove(sender As Object, e As MouseEventArgs) Handles Canvas.MouseMove
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

    Private Sub OnCanvasMouseDown(sender As Object, e As MouseEventArgs) Handles Canvas.MouseMove
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
    Private Sub OnCanvasMouseWheel(sender As Object, e As MouseEventArgs) Handles Canvas.MouseWheel
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
        RaiseEvent DisplayStatus($"zoom {treeLayout.Zoom:F2}x · {treeLayout.Nodes.Count} 个节点 · 度量 {treeLayout.MinValue:0}-{treeLayout.MaxValue:0}")
    End Sub

    Public Event DisplayStatus(message As String)
    Public Event DisplayProgress(p As Double, message As String)

    Private Sub OnCanvasMouseUp(sender As Object, e As MouseEventArgs) Handles Canvas.MouseUp
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

    Private Sub OnCanvasResize(sender As Object, e As EventArgs) Handles Canvas.Resize
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

        RaiseEvent DisplayDetails(sb.ToString)
    End Sub

    Public Event DisplayDetails(details As String)

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
    Public Property LayoutLevel As Func(Of CodeNodeKind)

    ''' <summary>the hierarchy level that becomes one building of the 3d city</summary>
    Public Property ExtrudeKind As Func(Of CodeNodeKind)

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub SaveSnapshot(filename As String)
        Call Canvas.SaveSnapshot(filename)
    End Sub

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Sub Close()
        Call poll.Stop()
    End Sub
End Class
