Imports CodeMap.CodeIndex

Namespace TreeMap

    ''' <summary>
    ''' the level of one node of the code map hierarchy:
    ''' vbproj -&gt; folder -&gt; source file -&gt; type -&gt; function/sub/property
    ''' </summary>
    Public Enum CodeNodeKind
        ''' <summary>a visual basic project (.vbproj)</summary>
        Project
        ''' <summary>a source folder below the project</summary>
        Folder
        ''' <summary>one vb.net source file</summary>
        File
        ''' <summary>a type declaration (class / module / structure / enum / interface)</summary>
        Type
        ''' <summary>a type member (function / sub / property / event / delegate / new / operator)</summary>
        Member
    End Enum

    ''' <summary>
    ''' the measure that drives the area (2d) and the height (3d) of a node.
    ''' </summary>
    Public Enum TreeMapMetric
        ''' <summary>the number of the physical source lines</summary>
        Lines
        ''' <summary>the number of the characters of the source text</summary>
        Chars
        ''' <summary>the number of the language symbols</summary>
        Symbols
    End Enum

    ''' <summary>
    ''' one node of the read only code map hierarchy.
    ''' </summary>
    ''' <remarks>
    ''' The three measures are stored pre aggregated: a leaf carries its own
    ''' value and a container carries the sum of all of its descendants, so
    ''' that a nested treemap never double counts the area of a parent.
    ''' </remarks>
    Public Class CodeNode

        Public Property Name As String
        Public Property FullName As String
        Public Property Kind As CodeNodeKind
        ''' <summary>the relative path inside the workspace, or the absolute path of the file</summary>
        Public Property Path As String
        ''' <summary>the pre aggregated number of the source lines</summary>
        Public Property Lines As Integer
        ''' <summary>the pre aggregated number of the characters</summary>
        Public Property Chars As Integer
        ''' <summary>the pre aggregated number of the language symbols</summary>
        Public Property SymbolCount As Integer
        Public Property Children As New List(Of CodeNode)()
        ''' <summary>the code symbol behind a type or a member node; nothing for the containers</summary>
        Public Property Symbol As CodeSymbol
        Public Property Depth As Integer
        Public Property Parent As CodeNode

        Public ReadOnly Property IsLeaf As Boolean
            Get
                Return Children Is Nothing OrElse Children.Count = 0
            End Get
        End Property

        ''' <summary>the value of the requested measure of this node</summary>
        Public Function Metric(m As TreeMapMetric) As Double
            Select Case m
                Case TreeMapMetric.Chars
                    Return Chars
                Case TreeMapMetric.Symbols
                    Return SymbolCount
                Case Else
                    Return Lines
            End Select
        End Function

        ''' <summary>the human readable display name of the node kind</summary>
        Public ReadOnly Property KindName As String
            Get
                Return Kind.ToString()
            End Get
        End Property

        ''' <summary>the path from the workspace root down to this node</summary>
        Public Function Breadcrumb() As String
            Dim parts As New List(Of String)()
            Dim cur As CodeNode = Me

            While cur IsNot Nothing
                Call parts.Add(cur.Name)
                cur = cur.Parent
            End While

            Call parts.Reverse()

            Return String.Join(" › ", parts)
        End Function

        Public Overrides Function ToString() As String
            Return $"[{KindName}] {Name} ({Lines} lines)"
        End Function

    End Class

End Namespace
