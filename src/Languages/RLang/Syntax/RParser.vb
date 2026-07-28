Imports System.Collections.Generic
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports VBLang
Imports VBLang.Syntax

Namespace Syntax

    ''' <summary>
    ''' A recursive-descent parser for R source code.
    ''' 
    ''' It mirrors the strategy of <c>VBLang.Syntax.VBParser</c> (scan &rarr; walk &rarr;
    ''' recursive descent) but is adapted to R syntax. The parser keeps a running count of
    ''' parenthesis and brace depth and only recognises assignments / <c>require</c> /
    ''' <c>library</c> calls when both depths are zero, so that:
    ''' <list type="bullet">
    '''   <item>named arguments such as <c>f(a = 1)</c> are never mistaken for variable
    '''   assignments;</item>
    '''   <item>function bodies (and their inner assignments) are scoped to the enclosing
    '''   function via brace matching and recursion.</item>
    ''' </list>
    ''' </summary>
    Public Module RParser

        ''' <summary>
        ''' Parse a complete R source text into its symbols and imports.
        ''' </summary>
        Public Function Parse(source As String) As RParseResult
            Dim tokens As List(Of Token) = RScanner.Scan(source)
            Dim res As New RParseResult()
            Parse(tokens, res, Nothing, 0, tokens.Count - 1, True)
            Return res
        End Function

        ' ------------------------------------------------------------------
        '  top level driver
        ' ------------------------------------------------------------------

        Private Sub Parse(tokens As List(Of Token), res As RParseResult, parent As RFunctionSymbol, fromIdx As Integer, toIdx As Integer, isTopLevel As Boolean)
            If fromIdx > toIdx Then
                Return
            End If

            If isTopLevel Then
                CollectImports(tokens, fromIdx, toIdx, res)
            End If

            Dim assigns = FindAssignments(tokens, fromIdx, toIdx)

            For Each a In assigns
                ProcessAssignment(tokens, a, res, parent, fromIdx, toIdx)
            Next
        End Sub

        ' ------------------------------------------------------------------
        '  require / library  ->  imports
        ' ------------------------------------------------------------------

        Private Sub CollectImports(tokens As List(Of Token), fromIdx As Integer, toIdx As Integer, res As RParseResult)
            Dim i As Integer = fromIdx
            Dim pd As Integer = 0
            Dim bd As Integer = 0

            While i <= toIdx
                Dim t = tokens(i)
                Dim isOpen = t.Text = "("c
                Dim isClose = t.Text = ")"c
                Dim isOB = t.Text = "{"c
                Dim isCB = t.Text = "}"c

                If pd = 0 AndAlso bd = 0 AndAlso t.Kind = TokenKind.Identifier AndAlso
                   (t.Text.Equals("require", StringComparison.OrdinalIgnoreCase) OrElse
                    t.Text.Equals("library", StringComparison.OrdinalIgnoreCase)) AndAlso
                   i + 1 <= toIdx AndAlso tokens(i + 1).Text = "("c Then

                    Dim closeIdx = MatchParen(tokens, i + 1)
                    Dim pkg = FirstArgPackage(tokens, i + 2, closeIdx - 1)

                    If pkg <> "" AndAlso Not res.ImportsList.Contains(pkg) Then
                        res.ImportsList.Add(pkg)
                    End If

                    i = closeIdx + 1
                    Continue While
                End If

                If isOpen Then pd += 1
                If isOB Then bd += 1
                If isClose Then pd -= 1
                If isCB Then bd -= 1
                i += 1
            End While
        End Sub

        ' ------------------------------------------------------------------
        '  assignment detection
        ' ------------------------------------------------------------------

        Private Class AssignRec
            Public opIndex As Integer
            Public isRightAssign As Boolean
            Public isFunction As Boolean
        End Class

        Private Function FindAssignments(tokens As List(Of Token), fromIdx As Integer, toIdx As Integer) As List(Of AssignRec)
            Dim result As New List(Of AssignRec)()
            Dim pd As Integer = 0
            Dim bd As Integer = 0
            Dim i As Integer = fromIdx

            While i <= toIdx
                Dim t = tokens(i)

                If t.Text = "("c Then
                    pd += 1
                ElseIf t.Text = ")"c Then
                    pd -= 1
                ElseIf t.Text = "{"c Then
                    bd += 1
                ElseIf t.Text = "}"c Then
                    bd -= 1
                End If

                If pd = 0 AndAlso bd = 0 AndAlso IsAssignOp(t) Then
                    Dim a As New AssignRec()
                    a.opIndex = i
                    a.isRightAssign = (t.Text = "->" OrElse t.Text = "->>")
                    a.isFunction = Not a.isRightAssign AndAlso IsFunctionRhs(tokens, i + 1, toIdx)
                    result.Add(a)
                End If

                i += 1
            End While

            Return result
        End Function

        Private Sub ProcessAssignment(tokens As List(Of Token), a As AssignRec, res As RParseResult, parent As RFunctionSymbol, fromIdx As Integer, toIdx As Integer)
            Dim lhsName As String = ResolveLhsName(tokens, a, toIdx)

            If a.isFunction Then
                Dim fn As New RFunctionSymbol()
                fn.Name = lhsName
                fn.Parent = parent

                Dim openIdx As Integer = IndexOfFunctionParen(tokens, a.opIndex + 1, toIdx)

                If openIdx >= 0 Then
                    Dim closeIdx As Integer = MatchParen(tokens, openIdx)
                    fn.Signature = Concat(tokens, openIdx, closeIdx)
                    fn.Parameters = ExtractParams(tokens, openIdx + 1, closeIdx - 1)

                    If closeIdx + 1 <= toIdx AndAlso tokens(closeIdx + 1).Text = "{"c Then
                        Dim bodyEnd As Integer = MatchBrace(tokens, closeIdx + 1)
                        Dim bodyStart As Integer = closeIdx + 2
                        Dim bodyTo As Integer = bodyEnd - 1

                        AddFunction(res, parent, fn)

                        If bodyStart <= bodyTo Then
                            Parse(tokens, res, fn, bodyStart, bodyTo, False)
                        End If
                    Else
                        AddFunction(res, parent, fn)
                    End If
                Else
                    AddFunction(res, parent, fn)
                End If
            Else
                Dim v As New VariableSymbol()
                v.Name = lhsName
                v.Parent = parent

                If parent Is Nothing Then
                    If Not res.Variables.Exists(Function(x) x.Name = lhsName) Then
                        res.Variables.Add(v)
                    End If
                Else
                    If parent.Locals Is Nothing Then
                        parent.Locals = New Dictionary(Of String, VariableSymbol)()
                    End If
                    If Not parent.Locals.ContainsKey(lhsName) Then
                        parent.Locals(lhsName) = v
                    End If
                End If
            End If
        End Sub

        Private Sub AddFunction(res As RParseResult, parent As RFunctionSymbol, fn As RFunctionSymbol)
            If parent Is Nothing Then
                If Not res.Functions.Exists(Function(x) x.Name = fn.Name) Then
                    res.Functions.Add(fn)
                End If
            Else
                If parent.NestedFunctions Is Nothing Then
                    parent.NestedFunctions = New List(Of RFunctionSymbol)()
                End If
                If Not parent.NestedFunctions.Exists(Function(x) x.Name = fn.Name) Then
                    parent.NestedFunctions.Add(fn)
                End If
            End If
        End Sub

        ' ------------------------------------------------------------------
        '  helpers
        ' ------------------------------------------------------------------

        Private Function IsAssignOp(t As Token) As Boolean
            Select Case t.Text
                Case "<-", "<<-", "=", "->", "->>"
                    Return True
            End Select
            Return False
        End Function

        Private Function IsFunctionRhs(tokens As List(Of Token), rhsStart As Integer, toIdx As Integer) As Boolean
            If rhsStart > toIdx Then
                Return False
            End If
            Dim t = tokens(rhsStart)
            Return t.Kind = TokenKind.Identifier AndAlso t.Text.Equals("function", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function ResolveLhsName(tokens As List(Of Token), a As AssignRec, toIdx As Integer) As String
            If a.isRightAssign Then
                Dim idx = a.opIndex + 1
                If idx > toIdx Then
                    Return ""
                End If
                Dim name = tokens(idx).Text
                Dim k = idx + 1
                While k <= toIdx AndAlso IsLhsPart(tokens(k))
                    name &= tokens(k).Text
                    k += 1
                End While
                Return name
            Else
                Dim startIdx = FindLhsStart(tokens, a.opIndex - 1)
                Dim sb As New System.Text.StringBuilder()
                For i = startIdx To a.opIndex - 1
                    sb.Append(tokens(i).Text)
                Next
                Return sb.ToString()
            End If
        End Function

        Private Function FindLhsStart(tokens As List(Of Token), idx As Integer) As Integer
            Dim i = idx
            While i - 1 >= 0 AndAlso IsLhsPart(tokens(i - 1))
                ' a name can never start with a digit, so a number that would become the
                ' new leftmost token belongs to a preceding expression, not this name.
                If tokens(i - 1).Kind = TokenKind.Number AndAlso i <= idx Then
                    Exit While
                End If
                i -= 1
            End While
            Return i
        End Function

        Private Function IsLhsPart(t As Token) As Boolean
            If t.Kind = TokenKind.Identifier OrElse t.Kind = TokenKind.Number Then
                Return True
            End If
            Select Case t.Text
                Case "$", "@", ":", "[", "]"
                    Return True
            End Select
            Return False
        End Function

        Private Function IndexOfFunctionParen(tokens As List(Of Token), start As Integer, toIdx As Integer) As Integer
            For i = start To toIdx
                If tokens(i).Text = "("c Then
                    Return i
                End If
            Next
            Return -1
        End Function

        Private Function MatchParen(tokens As List(Of Token), openIdx As Integer) As Integer
            Dim pd As Integer = 0
            Dim bd As Integer = 0
            Dim i = openIdx
            While i < tokens.Count
                Dim t = tokens(i).Text
                If t = "("c Then
                    pd += 1
                ElseIf t = ")"c Then
                    pd -= 1
                ElseIf t = "{"c Then
                    bd += 1
                ElseIf t = "}"c Then
                    bd -= 1
                End If
                If pd = 0 Then
                    Return i
                End If
                i += 1
            End While
            Return tokens.Count - 1
        End Function

        Private Function MatchBrace(tokens As List(Of Token), openIdx As Integer) As Integer
            Dim pd As Integer = 0
            Dim bd As Integer = 0
            Dim i = openIdx
            While i < tokens.Count
                Dim t = tokens(i).Text
                If t = "("c Then
                    pd += 1
                ElseIf t = ")"c Then
                    pd -= 1
                ElseIf t = "{"c Then
                    bd += 1
                ElseIf t = "}"c Then
                    bd -= 1
                End If
                If bd = 0 Then
                    Return i
                End If
                i += 1
            End While
            Return tokens.Count - 1
        End Function

        Private Function ExtractParams(tokens As List(Of Token), from As Integer, [to] As Integer) As Dictionary(Of String, TypeInfo)
            Dim d As New Dictionary(Of String, TypeInfo)()
            If from > [to] Then
                Return d
            End If

            Dim pd As Integer = 0
            Dim segStart = from
            Dim k = from

            While k <= [to]
                Dim t = tokens(k).Text
                If t = "("c Then
                    pd += 1
                ElseIf t = ")"c Then
                    pd -= 1
                End If
                If pd = 0 AndAlso t = ","c Then
                    AddParam(d, tokens, segStart, k - 1)
                    segStart = k + 1
                End If
                k += 1
            End While

            AddParam(d, tokens, segStart, [to])
            Return d
        End Function

        Private Sub AddParam(d As Dictionary(Of String, TypeInfo), tokens As List(Of Token), s As Integer, e As Integer)
            If s > e Then
                Return
            End If
            For i = s To e
                Dim t = tokens(i)
                If t.Kind = TokenKind.Identifier Then
                    If Not d.ContainsKey(t.Text) Then
                        d(t.Text) = Nothing
                    End If
                    Return
                End If
            Next
        End Sub

        Private Function FirstArgPackage(tokens As List(Of Token), from As Integer, [to] As Integer) As String
            If from > [to] Then
                Return ""
            End If

            Dim pd As Integer = 0
            Dim segStart = from
            Dim k = from

            While k <= [to]
                Dim t = tokens(k).Text
                If t = "("c Then
                    pd += 1
                ElseIf t = ")"c Then
                    pd -= 1
                End If
                If pd = 0 AndAlso t = ","c Then
                    Exit While
                End If
                k += 1
            End While

            For i = segStart To k - 1
                Dim t = tokens(i)
                If t.Kind = TokenKind.Identifier OrElse t.Kind = TokenKind.[String] Then
                    Return CleanPackageName(t.Text)
                End If
            Next

            Return ""
        End Function

        Private Function CleanPackageName(s As String) As String
            If s.Length >= 2 Then
                Dim first = s(0)
                Dim last = s(s.Length - 1)
                If (first = """"c AndAlso last = """"c) OrElse
                   (first = "'"c AndAlso last = "'"c) OrElse
                   (first = "`"c AndAlso last = "`"c) Then
                    s = s.Substring(1, s.Length - 2)
                End If
            End If
            Return s.Trim()
        End Function

        Private Function Concat(tokens As List(Of Token), from As Integer, [to] As Integer) As String
            Dim sb As New System.Text.StringBuilder()
            For i = from To [to]
                sb.Append(tokens(i).Text)
            Next
            Return sb.ToString()
        End Function

    End Module

End Namespace
