Imports System.Drawing
Imports Microsoft.VisualBasic.Data.Plots
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Models
Imports std = System.Math

Namespace TreeMap

    ''' <summary>
    ''' one extruded building of the 3d city view.
    ''' </summary>
    Public Class CityBuilding

        ''' <summary>the code map node behind this building</summary>
        Public Property Node As CodeNode
        ''' <summary>the 2d layout rectangle that this building was extruded from</summary>
        Public Property Rect As RectangleF
        ''' <summary>the height of the building, in world units</summary>
        Public Property Height As Double
        ''' <summary>the center of the footprint, in world coordinates</summary>
        Public Property Center As Point3D

        Public Overrides Function ToString() As String
            Return $"{Node.Name} h={Height:F2}"
        End Function

    End Class

    ''' <summary>
    ''' turn the 2d treemap layout into a 3d "code city": every rectangle is
    ''' extruded into a box whose height follows the measure of the symbol, and
    ''' the gaps between the boxes read as the streets of the city.
    ''' </summary>
    Public Module CityModelBuilder

        ''' <summary>
        ''' build the geometry of the city.
        ''' </summary>
        ''' <param name="layout">the laid out nodes of the 2d treemap.</param>
        ''' <param name="target">the hierarchy level that becomes a building.</param>
        ''' <param name="maxBuildings">the upper bound of the number of the buildings.</param>
        ''' <param name="maxHeight">the world space height of the tallest building.</param>
        ''' <param name="gapRatio">the share of the rectangle that is kept free as the street.</param>
        ''' <param name="canvasWidth">the width of the layout canvas, used to center the city.</param>
        ''' <param name="canvasHeight">the height of the layout canvas, used to center the city.</param>
        ''' <param name="buildings">returns the buildings in the very same order as their faces.</param>
        ''' <returns>the faces of all of the buildings; six faces per building.</returns>
        Public Function Build(layout As IEnumerable(Of TreemapNode),
                              target As CodeNodeKind,
                              maxBuildings As Integer,
                              maxHeight As Double,
                              gapRatio As Single,
                              canvasWidth As Integer,
                              canvasHeight As Integer,
                              ByRef buildings As List(Of CityBuilding)) As Surface()

            buildings = New List(Of CityBuilding)()

            If layout Is Nothing Then
                Return New Surface() {}
            End If

            Dim selected As New List(Of TreemapNode)()

            For Each n As TreemapNode In layout
                Dim cn As CodeNode = TryCast(n.Tag, CodeNode)

                If cn Is Nothing Then
                    Continue For
                End If

                If cn.Kind = target Then
                    Call selected.Add(n)
                End If
            Next

            ' the requested level may be missing (the user drilled below it), so
            ' the leaves of the current layout are extruded instead
            If selected.Count = 0 Then
                For Each n As TreemapNode In layout
                    If n.IsLeaf Then
                        Call selected.Add(n)
                    End If
                Next
            End If

            If selected.Count = 0 Then
                Return New Surface() {}
            End If

            If maxBuildings > 0 AndAlso selected.Count > maxBuildings Then
                selected = selected _
                    .OrderByDescending(Function(n) std.Abs(n.Value)) _
                    .Take(maxBuildings) _
                    .ToList()
            End If

            Dim maxV As Double = selected.Max(Function(n) std.Abs(n.Value))
            Dim minV As Double = selected.Min(Function(n) std.Abs(n.Value))

            If maxV <= 0 Then
                maxV = 1
            End If

            Dim span As Double = std.Max(maxV - minV, 0.000001)
            Dim gap As Single = std.Max(0.0F, std.Min(0.6F, gapRatio))
            Dim cx As Double = canvasWidth / 2.0
            Dim cy As Double = canvasHeight / 2.0
            Dim faces As New List(Of Surface)(selected.Count * 6)

            For Each n As TreemapNode In selected
                Dim rect As RectangleF = n.Rect

                If rect.Width <= 0 OrElse rect.Height <= 0 Then
                    Continue For
                End If

                ' the street: shrink the footprint around its own center
                Dim w As Single = rect.Width * (1.0F - gap)
                Dim h As Single = rect.Height * (1.0F - gap)

                If w <= 0 OrElse h <= 0 Then
                    Continue For
                End If

                Dim v As Double = std.Abs(n.Value)
                Dim ratio As Double = (v - minV) / span
                Dim height As Double = maxHeight * (0.03 + 0.97 * ratio)

                If height <= 0 Then
                    height = maxHeight * 0.03
                End If

                Dim mx As Double = rect.X + rect.Width / 2.0 - cx
                Dim my As Double = rect.Y + rect.Height / 2.0 - cy
                Dim x0 As Double = mx - w / 2.0
                Dim x1 As Double = mx + w / 2.0
                Dim y0 As Double = my - h / 2.0
                Dim y1 As Double = my + h / 2.0

                ' the vertex order is the one of the cube model:
                ' 0..3 are the corners of the bottom face, 4..7 the top face
                Dim vertices As Point3D() = {
                    New Point3D(x0, y1, 0.0),
                    New Point3D(x1, y1, 0.0),
                    New Point3D(x1, y0, 0.0),
                    New Point3D(x0, y0, 0.0),
                    New Point3D(x0, y1, height),
                    New Point3D(x1, y1, height),
                    New Point3D(x1, y0, height),
                    New Point3D(x0, y0, height)
                }

                Dim base_ As Color = If(n.Color, Color.SteelBlue)
                Dim box As New Cube(vertices, New Microsoft.VisualBasic.Imaging.Brush() {
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 0.45)),  ' bottom
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 0.72)),  ' +x
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 1.12)),  ' top
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 0.58)),  ' -x
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 0.92)),  ' +y
                    New Microsoft.VisualBasic.Imaging.SolidBrush(Shade(base_, 0.66))   ' -y
                })

                If box.faces IsNot Nothing Then
                    Call faces.AddRange(box.faces)
                End If

                Call buildings.Add(New CityBuilding With {
                    .Node = TryCast(n.Tag, CodeNode),
                    .Rect = rect,
                    .Height = height,
                    .Center = New Point3D(mx, my, height)
                })
            Next

            Return faces.ToArray()
        End Function

        ''' <summary>
        ''' pick the building whose projected top center is closest to the given
        ''' canvas position.
        ''' </summary>
        Public Function Pick(canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas,
                             buildings As List(Of CityBuilding),
                             x As Single,
                             y As Single,
                             Optional radius As Single = 24.0F) As CityBuilding

            If canvas Is Nothing OrElse buildings Is Nothing Then
                Return Nothing
            End If

            Dim best As CityBuilding = Nothing
            Dim bestDist As Single = radius
            Dim screen As PointF = Nothing

            For Each b As CityBuilding In buildings
                If b Is Nothing OrElse Not canvas.TryProjectPoint(b.Center, screen) Then
                    Continue For
                End If

                Dim dx As Single = screen.X - x
                Dim dy As Single = screen.Y - y
                Dim d As Single = CSng(std.Sqrt(dx * dx + dy * dy))

                If d <= bestDist Then
                    bestDist = d
                    best = b
                End If
            Next

            Return best
        End Function

        Private Function Shade(c As Color, factor As Double) As Color
            Dim r As Integer = CInt(std.Max(0, std.Min(255, c.R * factor)))
            Dim g As Integer = CInt(std.Max(0, std.Min(255, c.G * factor)))
            Dim b As Integer = CInt(std.Max(0, std.Min(255, c.B * factor)))

            Return Color.FromArgb(c.A, r, g, b)
        End Function

    End Module

End Namespace
