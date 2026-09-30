Imports System.Drawing
Imports Microsoft.VisualBasic.Data.Plots
Imports Microsoft.VisualBasic.Imaging
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports SolidBrush = Microsoft.VisualBasic.Imaging.SolidBrush

Namespace TreeMap

    ''' <summary>
    ''' the 2d squarified layout of the code map hierarchy.
    ''' </summary>
    ''' <remarks>
    ''' The layout is the single source of truth of both presentation modes:
    ''' the 2d mode paints the rectangles, and the 3d mode extrudes the very
    ''' same [x,y] rectangles into buildings.
    ''' </remarks>
    Public Class TreeMapLayout

        ''' <summary>the measure that drives the area of a rectangle</summary>
        Public Property Metric As TreeMapMetric = TreeMapMetric.Lines
        ''' <summary>the deepest hierarchy level that is laid out, 0 = projects only</summary>
        Public Property MaxDepth As Integer = 3

        Dim roots As New List(Of CodeNode)()
        Dim nodeCache As New List(Of TreemapNode)()
        Dim _result As New List(Of TreemapNode)()
        Dim lastWidth As Integer = 0
        Dim lastHeight As Integer = 0
        Dim dirty As Boolean = True

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
            dirty = True
        End Sub

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
                .ShowLabels = True,
                .ShowValues = False
            }

            plot.Nodes = nodeCache
            _result = plot.LayoutNodes(width, height)

            lastWidth = width
            lastHeight = height
            dirty = False

            Return _result
        End Function

        ''' <summary>
        ''' draw the treemap onto an external graphics device, for example the
        ''' direct2d gpu canvas of the <c>DxScene3DCanvas</c> control.
        ''' </summary>
        ''' <remarks>
        ''' The first call after a change runs the full plot pipeline of the
        ''' engine (which lays out and paints in one go); every later frame only
        ''' repaints the cached rectangles, so resizing and orbiting stay cheap
        ''' even for tens of thousands of nodes.
        ''' </remarks>
        Public Sub Render(g As IGraphics, width As Integer, height As Integer)
            If g Is Nothing OrElse width <= 0 OrElse height <= 0 Then
                Return
            End If

            If roots.Count = 0 Then
                Using bg As New SolidBrush(Theme.BackgroundColor)
                    Call g.FillRectangle(bg, New RectangleF(0, 0, width, height))
                End Using

                Return
            End If

            If Not dirty AndAlso _result.Count > 0 AndAlso
                lastWidth = width AndAlso lastHeight = height Then

                Call DrawCached(g)
                Return
            End If

            nodeCache = BuildNodes()

            Dim plot As New TreemapPlot(g, Theme) With {
                .GroupPadding = 2.0F,
                .HeaderHeight = 14.0F,
                .MaxRenderDepth = MaxDepth,
                .ColorByGroup = False,
                .ShowLabels = True,
                .ShowValues = False
            }

            plot.Nodes = nodeCache

            Call plot.Plot()

            _result = plot.Layout
            lastWidth = width
            lastHeight = height
            dirty = False
        End Sub

        ''' <summary>repaint the cached rectangles without running the layout again</summary>
        Private Sub DrawCached(g As IGraphics)
            Using bg As New SolidBrush(Theme.BackgroundColor)
                Call g.FillRectangle(bg, New RectangleF(0, 0, lastWidth, lastHeight))
            End Using

            Using pen As New Pen(Theme.BackgroundColor, 1.0F)
                For Each n As TreemapNode In _result
                    If n.Rect.Width < 1.0F OrElse n.Rect.Height < 1.0F Then
                        Continue For
                    End If

                    Dim color As Color = If(n.Color, Theme.Palette(0))

                    Using br As New SolidBrush(color)
                        Call g.FillRectangle(br, n.Rect)
                    End Using

                    Call g.DrawRectangle(pen, n.Rect.X, n.Rect.Y, n.Rect.Width, n.Rect.Height)
                Next
            End Using
        End Sub

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
                .Color = KindColor(n.Kind),
                .Depth = depth,
                .Tag = n
            }

            If depth >= MaxDepth OrElse n.IsLeaf Then
                Return node
            End If

            For Each child As CodeNode In n.Children
                Call node.Children.Add(ToPlotNode(child, depth + 1))
            Next

            Return node
        End Function

        ''' <summary>
        ''' pick the code map node that covers the given canvas position.
        ''' </summary>
        Public Function HitTest(x As Single, y As Single) As CodeNode
            If _result Is Nothing Then
                Return Nothing
            End If

            For i As Integer = _result.Count - 1 To 0 Step -1
                If _result(i).Rect.Contains(x, y) Then
                    Return TryCast(_result(i).Tag, CodeNode)
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>the color of one hierarchy level</summary>
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

    End Class

End Namespace
