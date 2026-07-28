Imports System.Collections.Generic
Imports VBLang

Namespace Syntax

    ''' <summary>
    ''' Represents an R function declaration of the form
    ''' <c>name &lt;- function(args) { ... }</c>.
    ''' 
    ''' It is mapped onto VB's <see cref="CallableMemberSymbol"/> so that it can live
    ''' inside a <see cref="VBLang.VBDocument.Types"/> dictionary just like any other
    ''' VB symbol, while still carrying R-specific information (the captured signature
    ''' text and the list of nested functions defined inside its body).
    ''' </summary>
    Public Class RFunctionSymbol : Inherits CallableMemberSymbol

        Public Overrides ReadOnly Property Type As SymbolType = SymbolType.[Function]

        ''' <summary>
        ''' Functions that are defined inside this function's body (closures).
        ''' </summary>
        Public Property NestedFunctions As List(Of RFunctionSymbol)

        ''' <summary>
        ''' The raw signature text (e.g. <c>function(a, b = 1)</c>) captured from the
        ''' source, useful for tooling / display purposes.
        ''' </summary>
        Public Property Signature As String

        Public Sub New()
            ' R has no static types: parameters and return value are unknown.
            Parameters = New Dictionary(Of String, Microsoft.VisualBasic.Scripting.MetaData.TypeInfo)()
        End Sub

    End Class

    ''' <summary>
    ''' The result of parsing a single R source text: the function symbols, the
    ''' variable symbols and the package names imported through top-level
    ''' <c>require</c> / <c>library</c> calls.
    ''' </summary>
    Public Class RParseResult

        ''' <summary>Top-level function declarations.</summary>
        Public Functions As New List(Of RFunctionSymbol)()

        ''' <summary>Top-level variable assignments.</summary>
        Public Variables As New List(Of VariableSymbol)()

        ''' <summary>Package names imported via top-level require/library calls.</summary>
        Public ImportsList As New List(Of String)()

    End Class

End Namespace
