Imports System.Drawing
Imports Microsoft.VisualBasic.Data.Plots
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports FontStyle = System.Drawing.FontStyle
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush
Imports std = System.Math

Namespace TreeMap

    ''' <summary>
    ''' the 2d squarified layout of the code map hierarchy.
    ''' </summary>
    ''' <remarks>
    ''' The layout is the single source of truth of both presentation modes:
    ''' the 2d mode paints the rectangles (with an optional zoom) and the 3d
    ''' mode extrudes the very same [x,y] rectangles into buildings.
    ''' </remarks>
    Public Class TreeMapLayout

        ''' <summary>the measure that drives the area of a rectangle</summary>
        Public Property Metric As TreeMapMetric = TreeMapMetric.Lines
        ''' <summary>
        ''' the deepest hierarchy level that becomes a laid out rectangle: the
        ''' layout descends until a node of this kind is reached, that node is a
        ''' leaf of the layout even when it owns children.
        ''' </summary>
        ''' <remarks>
        ''' The descent is driven by the kind and not by the raw tree depth on
        ''' purpose: the folder chain of a source file is of an arbitrary length,
        ''' so a plain depth limit would swallow the type and the member levels of
        ''' every deeply nested file.
        ''' </remarks>
        Public Property Level As CodeNodeKind = CodeNodeKind.File
        ''' <summary>a safety bound of the recursion depth, normally never reached</summary>
        Public Property MaxDepth As Integer = 64

        ' ---------- the heat map ----------

        ''' <summary>the color scheme of the value heat map</summary>
        Public Property Palette As ScalerPalette = ScalerPalette.viridis
        ''' <summary>map the measure onto a color ramp instead of onto the level colors</summary>
        Public Property UseHeatmap As Boolean = True
        ''' <summary>
        ''' the measure of a code base is heavy tailed, so a logarithmic ramp
        ''' spreads the colors over the small values as well
        ''' </summary>
        Public Property LogScale As Boolean = True
        ''' <summary>the number of the color levels of the ramp</summary>
        Public Property ColorLevels As Integer = 128
        Public Property ShowLegend As Boolean = True

        ' ---------- the 2d view ----------

        ''' <summary>the zoom factor of the 2d view, one means "fit into the canvas"</summary>
        Public Property Zoom As Double = 1.0
        ''' <summary>the horizontal offset of the 2d view, in pixels</summary>
        Public Property OffsetX As Double = 0.0
        ''' <summary>the vertical offset of the 2d view, in pixels</summary>
        Public Property OffsetY As Double = 0.0
        ''' <summary>
        ''' from this zoom factor on the raw source text of a leaf is painted
        ''' inside its rectangle.
        ''' </summary>
        Public Property CodeTextZoom As Double = 2.5
        ''' <summary>
        ''' the host supplied text provider: it resolves the raw source text of a
        ''' leaf node on demand. A type or a member node already carries its text
        ''' in <see cref="CodeNode.Symbol"/>, a file level node reads its file
        ''' through this hook.
        ''' </summary>
        Public Property TextProvider As Func(Of CodeNode, String)

        Dim roots As New List(Of CodeNode)()
        Dim nodeCache As New List(Of TreemapNode)()
        Dim _result As New List(Of TreemapNode)()
        Dim lastWidth As Integer = 0
        Dim lastHeight As Integer = 0
        Dim layoutDirty As Boolean = True
        Dim colorsDirty As Boolean = True

        Dim ramp As Color() = Nothing
        Dim valueMin As Double = 0
        Dim valueMax As Double = 1

        Sub New(Optional theme As PlotTheme = Nothing)
            Me.Theme = If(theme, PlotTheme.Dark())
        End Sub

        Public Property Theme As PlotTheme

        ''' <summary>the nodes of the last layout, in depth first order</summary>
        Public ReadOnly Property Nodes As List(Of TreemapNode)
            Get
                Return _result
            End Get
        End Property

        ''' <summary>the smallest measure of the last layout, for the legend</summary>
        Public ReadOnly Property MinValue As Double
            Get
                Return valueMin
            End Get
        End Property

        ''' <summary>the largest measure of the last layout, for the legend</summary>
        Public ReadOnly Property MaxValue As Double
            Get
                Return valueMax
            End Get
        End Property

        ''' <summary>
        ''' the top level nodes that the layout starts from; setting them
        ''' invalidates the cached rectangles.
        ''' </summary>
        Public Sub SetRoots(nodes As List(Of CodeNode))
            roots.Clear()

            If nodes IsNot Nothing Then
                Call roots.AddRange(nodes)
            End If

            Call Invalidate()
        End Sub

        ''' <summary>force a re-layout on the next render</summary>
        Public Sub Invalidate()
            layoutDirty = True
        End Sub

        ''' <summary>force a re-coloring without running the layout again</summary>
        Public Sub InvalidateColors()
            colorsDirty = True
        End Sub

        ' /********************************************************************************/
        '  the layout
        ' /********************************************************************************/

        ''' <summary>
        ''' run the squarified layout for the given canvas size and return every
        ''' laid out node together with its rectangle.
        ''' </summary>
        Public Function Layout(width As Integer, height As Integer) As List(Of TreemapNode)
            If roots.Count = 0 OrElse width <= 0 OrElse height <= 0 Then
                _result = New List(Of TreemapNode)()
                Return _result
            End If

            nodeCache = BuildNodes()

            Dim plot As New TreemapPlot(width, height, Theme) With {
                .GroupPadding = 2.0F,
                .HeaderHeight = 14.0F,
                .MaxRenderDepth = MaxDepth,
                .ColorByGroup = False,
                .ShowLabels = False,
                .ShowValues = False
            }

            plot.Nodes = nodeCache
            _result = plot.LayoutNodes(width, height)

            lastWidth = width
            lastHeight = height
            layoutDirty = False

            Call ApplyColors()

            Return _result
        End Function

        ''' <summary>
        ''' project the code map hierarchy into the node model of the plot engine.
        ''' </summary>
        Private Function BuildNodes() As List(Of TreemapNode)
            Dim list As New List(Of TreemapNode)()

            For Each n As CodeNode In roots
                Call list.Add(ToPlotNode(n, 0))
            Next

            Return list
        End Function

        Private Function ToPlotNode(n As CodeNode, depth As Integer) As TreemapNode
            Dim node As New TreemapNode With {
                .Label = n.Name,
                .Value = n.Metric(Metric),
                .Group = n.KindName,
                .Depth = depth,
                .Tag = n
            }

            ' Stop at the requested level and at everything below it: a file that
            ' sits directly in the project root has no folder above it, so the
            ' descent is bounded by the level and not by an exact match. The
            ' folder level is transparent on purpose - a folder only names a path
            ' segment, so the whole folder chain is walked down no matter how
            ' deeply the source files are nested.
            Dim atLevel As Boolean = CodeTreeBuilder.KindOrder(n.Kind) >= CodeTreeBuilder.KindOrder(Level)
            Dim transparent As Boolean = (n.Kind = CodeNodeKind.Folder)

            If depth >= MaxDepth OrElse n.IsLeaf OrElse (atLevel AndAlso Not transparent) Then
                Return node
            End If

            For Each child As CodeNode In n.Children
                Call node.Children.Add(ToPlotNode(child, depth + 1))
            Next

            Return node
        End Function

        ' /********************************************************************************/
        '  the heat map colors
        ' /********************************************************************************/

        ''' <summary>
        ''' map the measure of every node onto a color of the heat map ramp.
        ''' </summary>
        Public Sub ApplyColors()
            If _result Is Nothing OrElse _result.Count = 0 Then
                Return
            End If

            If Not UseHeatmap Then
                For Each n As TreemapNode In _result
                    Dim cn As CodeNode = TryCast(n.Tag, CodeNode)

                    n.Color = If(cn Is Nothing, Theme.Palette(0), KindColor(cn.Kind))
                Next

                colorsDirty = False
                Return
            End If

            ramp = Designer.FromSchema(Palette, std.Max(8, ColorLevels))

            valueMin = Double.MaxValue
            valueMax = Double.MinValue

            For Each n As TreemapNode In _result
                Dim v As Double = std.Abs(n.Value)

                If v < valueMin Then valueMin = v
                If v > valueMax Then valueMax = v
            Next

            If valueMin >= valueMax Then
                valueMax = valueMin + 1
            End If

            For Each n As TreemapNode In _result
                n.Color = HeatColor(std.Abs(n.Value), valueMin, valueMax)
            Next

            colorsDirty = False
        End Sub

        ''' <summary>the color of one measure on the current ramp</summary>
        Public Function HeatColor(v As Double, min As Double, max As Double) As Color
            If ramp Is Nothing OrElse ramp.Length = 0 Then
                Return Theme.Palette(0)
            End If

            Dim t As Double

            If max <= min Then
                t = 0.5
            ElseIf LogScale AndAlso min >= 0 Then
                Dim lo As Double = std.Log(min + 1)
                Dim hi As Double = std.Log(max + 1)

                t = If(hi > lo, (std.Log(v + 1) - lo) / (hi - lo), 0.0)
            Else
                t = (v - min) / (max - min)
            End If

            If t < 0 Then t = 0
            If t > 1 Then t = 1

            Dim i As Integer = CInt(std.Floor(t * (ramp.Length - 1)))

            If i < 0 Then i = 0
            If i >= ramp.Length Then i = ramp.Length - 1

            Return ramp(i)
        End Function

        ''' <summary>the color of one hierarchy level, used when the heat map is off</summary>
        Public Shared Function KindColor(kind As CodeNodeKind) As Color
            Select Case kind
                Case CodeNodeKind.Project
                    Return Color.FromArgb(99, 102, 241)
                Case CodeNodeKind.Folder
                    Return Color.FromArgb(14, 165, 233)
                Case CodeNodeKind.File
                    Return Color.FromArgb(45, 212, 191)
                Case CodeNodeKind.Type
                    Return Color.FromArgb(52, 211, 153)
                Case Else
                    Return Color.FromArgb(56, 189, 248)
            End Select
        End Function

        ' /********************************************************************************/
        '  the 2d view transform
        ' /********************************************************************************/

        ''' <summary>project a layout rectangle onto the canvas</summary>
        Public Function ToScreen(r As RectangleF) As RectangleF
            Return New RectangleF(CSng(r.X * Zoom + OffsetX),
                                  CSng(r.Y * Zoom + OffsetY),
                                  CSng(r.Width * Zoom),
                                  CSng(r.Height * Zoom))
        End Function

        ''' <summary>project a canvas position back into the layout coordinates</summary>
        Public Function ToWorld(x As Single, y As Single) As PointF
            Return New PointF(CSng((x - OffsetX) / Zoom), CSng((y - OffsetY) / Zoom))
        End Function

        ''' <summary>
        ''' zoom around the given canvas position, the point under the cursor
        ''' stays where it is.
        ''' </summary>
        Public Sub ZoomAt(x As Single, y As Single, factor As Double)
            Dim nextZoom As Double = std.Max(0.2, std.Min(80.0, Zoom * factor))
            Dim k As Double = nextZoom / Zoom

            OffsetX = x - (x - OffsetX) * k
            OffsetY = y - (y - OffsetY) * k
            Zoom = nextZoom
        End Sub

        ''' <summary>scroll the 2d view by the given canvas delta</summary>
        Public Sub PanBy(dx As Single, dy As Single)
            OffsetX += dx
            OffsetY += dy
        End Sub

        ''' <summary>fit the whole layout into the canvas again</summary>
        Public Sub ResetView()
            Zoom = 1.0
            OffsetX = 0.0
            OffsetY = 0.0
        End Sub

        ''' <summary>
        ''' pick the code map node that covers the given canvas position.
        ''' </summary>
        Public Function HitTest(x As Single, y As Single) As CodeNode
            If _result Is Nothing Then
                Return Nothing
            End If

            Dim w As PointF = ToWorld(x, y)

            For i As Integer = _result.Count - 1 To 0 Step -1
                If _result(i).Rect.Contains(w) Then
                    Return TryCast(_result(i).Tag, CodeNode)
                End If
            Next

            Return Nothing
        End Function

        ' /********************************************************************************/
        '  the rendering
        ' /********************************************************************************/

        ''' <summary>
        ''' draw the treemap onto an external graphics device, for example the
        ''' direct2d gpu canvas of the <c>DxScene3DCanvas</c> control.
        ''' </summary>
        Public Sub Render(g As IGraphics, width As Integer, height As Integer)
            If g Is Nothing OrElse width <= 0 OrElse height <= 0 Then
                Return
            End If

            If roots.Count = 0 Then
                Call DrawEmpty(g, width, height)
                Return
            End If

            If layoutDirty OrElse _result.Count = 0 OrElse
                lastWidth <> width OrElse lastHeight <> height Then

                Call Layout(width, height)
            ElseIf colorsDirty Then
                Call ApplyColors()
            End If

            Call PaintNodes(g, width, height)
        End Sub

        Private Sub DrawEmpty(g As IGraphics, width As Integer, height As Integer)
            Using bg As New SolidBrush(Theme.BackgroundColor)
                Call g.FillRectangle(bg, New RectangleF(0, 0, width, height))
            End Using

            Dim titleFont As New Font("Microsoft YaHei", 12)

            Using br As New SolidBrush(Theme.TextColor)
                Call g.DrawString("正在构建代码索引 …", titleFont, br, 16.0F, 16.0F)
            End Using
        End Sub

        Private Sub PaintNodes(g As IGraphics, width As Integer, height As Integer)
            Using bg As New SolidBrush(Theme.BackgroundColor)
                Call g.FillRectangle(bg, New RectangleF(0, 0, width, height))
            End Using

            Using border As New Pen(Theme.BackgroundColor, 1.0F)
                For Each n As TreemapNode In _result
                    Dim r As RectangleF = ToScreen(n.Rect)

                    If r.Width < 0.5F OrElse r.Height < 0.5F Then
                        Continue For
                    End If

                    If r.Right < 0 OrElse r.Bottom < 0 OrElse r.Left > width OrElse r.Top > height Then
                        Continue For
                    End If

                    Dim color As Color = If(n.Color, Theme.Palette(0))

                    Using br As New SolidBrush(color)
                        Call g.FillRectangle(br, r)
                    End Using

                    Call g.DrawRectangle(border, r.X, r.Y, r.Width, r.Height)
                Next
            End Using

            ' the text pass runs in a second loop so that no rectangle border is
            ' ever painted over a label
            For Each n As TreemapNode In _result
                Dim r As RectangleF = ToScreen(n.Rect)

                If r.Width < 24.0F OrElse r.Height < 12.0F Then
                    Continue For
                End If

                If r.Right < 0 OrElse r.Bottom < 0 OrElse r.Left > width OrElse r.Top > height Then
                    Continue For
                End If

                Call DrawNodeText(g, n, r)
            Next

            Call DrawLegend(g, width, height)
        End Sub

        ''' <summary>resolve the raw source text of a leaf node through the host</summary>
        Private Function ResolveCode(cn As CodeNode) As String
            If cn Is Nothing Then
                Return ""
            End If

            If TextProvider IsNot Nothing Then
                Try
                    Return If(TextProvider(cn), "")
                Catch
                    Return ""
                End Try
            End If

            Return If(cn.Symbol Is Nothing, "", If(cn.Symbol.Code, ""))
        End Function

        Private Sub DrawNodeText(g As IGraphics, n As TreemapNode, r As RectangleF)
            Dim cn As CodeNode = TryCast(n.Tag, CodeNode)
            Dim base_ As Color = If(n.Color, Theme.Palette(0))
            Dim brightness As Double = (0.299 * base_.R + 0.587 * base_.G + 0.114 * base_.B) / 255
            Dim ink As Color = If(brightness > 0.55, Color.Black, Color.White)

            ' zoomed in far enough: show the raw source text of the leaf
            If Zoom >= CodeTextZoom AndAlso cn IsNot Nothing AndAlso
                r.Width > 55.0F AndAlso r.Height > 42.0F Then

                Dim code As String = ResolveCode(cn)

                If code Is Nothing Then
                    code = ""
                End If

                If code.Length > 4000 Then
                    code = code.Substring(0, 4000)
                End If

                Dim head As New RectangleF(r.X + 3.0F, r.Y + 2.0F, r.Width - 6.0F, 14.0F)
                Dim body As New RectangleF(r.X + 3.0F, r.Y + 17.0F, r.Width - 6.0F, r.Height - 20.0F)
                Dim headFont As New Font("Microsoft YaHei", 8, FontStyle.Bold)

                Using br As New SolidBrush(ink)
                    Call g.DrawString(cn.Name, headFont, br, head)
                End Using

                If body.Height > 8.0F AndAlso code.Length > 0 Then
                    Dim codeFont As New Font("Consolas", 8)

                    Using br As New SolidBrush(ink)
                        Call g.DrawString(code, codeFont, br, body)
                    End Using
                End If

                Return
            End If

            If r.Width < 30.0F OrElse r.Height < 16.0F Then
                Return
            End If

            Dim size As Single = CSng(std.Max(6.5, std.Min(11.0, r.Height / 4.0)))
            Dim labelFont As New Font("Microsoft YaHei", size)

            Using br As New SolidBrush(ink)
                Call g.DrawString(n.Label, labelFont, br, New RectangleF(r.X + 2.0F, r.Y + 1.0F, r.Width - 4.0F, r.Height - 2.0F))
            End Using
        End Sub

        ''' <summary>the color ramp of the heat map together with its value range</summary>
        Private Sub DrawLegend(g As IGraphics, width As Integer, height As Integer)
            If Not ShowLegend OrElse Not UseHeatmap OrElse ramp Is Nothing OrElse ramp.Length = 0 Then
                Return
            End If

            Dim barW As Single = 190.0F
            Dim barH As Single = 10.0F
            Dim x0 As Single = 14.0F
            Dim y0 As Single = height - 30.0F
            Dim steps As Integer = std.Min(64, ramp.Length)
            Dim stepW As Single = barW / steps

            For i As Integer = 0 To steps - 1
                Dim idx As Integer = CInt(i * (ramp.Length - 1) / std.Max(1, steps - 1))

                Using br As New SolidBrush(ramp(idx))
                    Call g.FillRectangle(br, x0 + i * stepW, y0, stepW + 0.8F, barH)
                End Using
            Next

            Dim legendFont As New Font("Consolas", 8)
            Dim legendText As String = $"{valueMin:0} · {Palette.ToString()} / {Metric.ToString()} · {valueMax:0}"

            Using br As New SolidBrush(Theme.TextColor)
                Call g.DrawString(legendText, legendFont, br, x0, y0 + barH + 2.0F)
            End Using
        End Sub

    End Class

End Namespace
