Imports System.Text
Imports VBLang.Syntax

Namespace Syntax

    ''' <summary>
    ''' Lexical scanner for R source code.
    ''' 
    ''' Produces a flat, line-numbered list of <see cref="VBLang.Syntax.Token"/> 
    ''' values (reusing the token model from <c>VBLang</c>) so that the downstream 
    ''' <see cref="RParser"/> can work without re-inventing a token structure.
    ''' 
    ''' The scanner:
    ''' <list type="bullet">
    '''   <item>skips <c>#</c> comments (to the end of the line);</item>
    '''   <item>treats <c>"</c>, <c>'</c> and backtick strings as single tokens
    '''   (<c>\</c> escapes, double/single quoted strings may span multiple lines);</item>
    '''   <item>recognises the R assignment operators <c>&lt;-</c>, <c>&lt;&lt;-</c>,
    '''   <c>=</c>, <c>-&gt;</c>, <c>-&gt;&gt;</c> plus the usual punctuation;</item>
    '''   <item>never emits whitespace / newline tokens, so identifiers that live
    '''   inside strings or comments can never be mistaken for symbols.</item>
    ''' </list>
    ''' </summary>
    Public Module RScanner

        ''' <summary>
        ''' Tokenize a complete R source text into a flat token list.
        ''' </summary>
        Public Function Scan(source As String) As List(Of Token)
            If source Is Nothing Then
                Return New List(Of Token)()
            End If

            Dim normalised = source.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
            Dim lines As String() = normalised.Split(vbLf)
            Dim toks As New List(Of Token)()

            Dim lineNo As Integer = 0
            ' carry-over string state across lines: "" means "not inside a string",
            ' otherwise it holds the quote character we are still waiting to close.
            Dim strState As String = ""

            For Each line As String In lines
                lineNo += 1
                TokenizeLine(line, lineNo, toks, strState)
                ' emit a newline token as a statement separator so that the parser can
                ' bound assignments / names to their own line (R statements are
                ' newline separated). It is skipped while a multi-line string is open.
                If strState = "" Then
                    toks.Add(New Token With {.Kind = TokenKind.Punctuation, .Text = vbLf, .Line = lineNo})
                End If
            Next

            Return toks
        End Function

        Private Sub TokenizeLine(line As String, lineNo As Integer, toks As List(Of Token), ByRef strState As String)
            Dim n As Integer = line.Length
            Dim j As Integer = 0
            Dim sb As New StringBuilder()

            Dim flush = Sub()
                If sb.Length > 0 Then
                    Dim word = sb.ToString()
                    sb.Clear()
                    toks.Add(New Token With {.Kind = TokenKind.Identifier, .Text = word, .Line = lineNo})
                End If
            End Sub

            ' continue an open multi-line string started on a previous line
            If strState <> "" Then
                Dim r = ReadString(line, 0, strState(0), sb, toks, lineNo)
                If r < 0 Then
                    ' still not closed -> keep the state and stop for this line
                    strState = strState(0).ToString()
                    Return
                End If
                strState = ""
                j = r
                If j >= n Then
                    Return
                End If
            End If

            While j < n
                Dim c As Char = line(j)

                ' comment: ignore the rest of the line
                If c = "#"c Then
                    Exit While
                End If

                ' quoted strings
                If c = """"c OrElse c = "'"c Then
                    flush()
                    j = ReadString(line, j, c, sb, toks, lineNo)
                    Continue While
                End If

                ' backtick strings (no escape, single line)
                If c = "`"c Then
                    flush()
                    sb.Append(c)
                    j += 1
                    While j < n AndAlso line(j) <> "`"c
                        sb.Append(line(j))
                        j += 1
                    End While
                    If j < n Then
                        sb.Append("`"c)
                        j += 1
                    End If
                    toks.Add(New Token With {.Kind = TokenKind.[String], .Text = sb.ToString(), .Line = lineNo})
                    sb.Clear()
                    Continue While
                End If

                If Char.IsWhiteSpace(c) Then
                    flush()
                    j += 1
                    Continue While
                End If

                ' identifiers: letters, '.', '_' and digits (digits only when already inside a word)
                If Char.IsLetter(c) OrElse c = "."c OrElse c = "_"c OrElse (Char.IsDigit(c) AndAlso sb.Length > 0) Then
                    sb.Append(c)
                    j += 1
                    Continue While
                End If

                ' numbers (start with a digit)
                If Char.IsDigit(c) Then
                    flush()
                    While j < n AndAlso IsNumberChar(line(j))
                        sb.Append(line(j))
                        j += 1
                    End While
                    toks.Add(New Token With {.Kind = TokenKind.Number, .Text = sb.ToString(), .Line = lineNo})
                    sb.Clear()
                    Continue While
                End If

                flush()

                ' operators: try three-char, then two-char, then single-char
                Dim three As String = If(j + 2 < n, c & line(j + 1) & line(j + 2), "")
                If three = "<<-" OrElse three = "->>" Then
                    toks.Add(New Token With {.Kind = TokenKind.Punctuation, .Text = three, .Line = lineNo})
                    j += 3
                    Continue While
                End If

                If c = "%"c Then
                    ' arbitrary infix operator %xxx%
                    sb.Append(c)
                    j += 1
                    While j < n AndAlso line(j) <> "%"c
                        sb.Append(line(j))
                        j += 1
                    End While
                    If j < n Then
                        sb.Append("%"c)
                        j += 1
                    End If
                    toks.Add(New Token With {.Kind = TokenKind.Punctuation, .Text = sb.ToString(), .Line = lineNo})
                    sb.Clear()
                    Continue While
                End If

                Dim two As String = If(j + 1 < n, c & line(j + 1), c.ToString())
                Select Case two
                    Case "<-", "<<", "->", ">>", "<=", ">=", "==", "!=", "%%", "%*%", "%/%"
                        toks.Add(New Token With {.Kind = TokenKind.Punctuation, .Text = two, .Line = lineNo})
                        j += 2
                        Continue While
                End Select

                toks.Add(New Token With {.Kind = TokenKind.Punctuation, .Text = c.ToString(), .Line = lineNo})
                j += 1
            End While

            flush()
        End Sub

        ''' <summary>
        ''' Read a quoted string beginning at index <paramref name="start"/> (which points at
        ''' the opening quote). Returns the index just past the closing quote, or <paramref name="n"/>
        ''' when the string is not closed on this line (caller keeps the state open).
        ''' </summary>
        Private Function ReadString(line As String, start As Integer, quote As Char, sb As StringBuilder, toks As List(Of Token), lineNo As Integer) As Integer
            Dim n As Integer = line.Length
            sb.Append(quote)
            Dim j As Integer = start + 1

            While j < n
                Dim d As Char = line(j)

                If d = "\"c AndAlso j + 1 < n Then
                    sb.Append(d)
                    sb.Append(line(j + 1))
                    j += 2
                    Continue While
                End If

                sb.Append(d)

                If d = quote Then
                    j += 1
                    toks.Add(New Token With {.Kind = TokenKind.[String], .Text = sb.ToString(), .Line = lineNo})
                    sb.Clear()
                    Return j
                End If

                j += 1
            End While

            ' string not closed on this line: do not emit a token, signal the
            ' caller to keep the open-string state for the next line (-1).
            sb.Clear()
            Return -1
        End Function

        Private Function IsNumberChar(c As Char) As Boolean
            Return Char.IsDigit(c) OrElse
                   c = "."c OrElse
                   c = "e"c OrElse c = "E"c OrElse
                   c = "x"c OrElse c = "X"c OrElse
                   c = "+"c OrElse c = "-"c
        End Function

    End Module

End Namespace
