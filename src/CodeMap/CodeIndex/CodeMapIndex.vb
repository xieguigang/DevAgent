Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel.Repository

Namespace CodeIndex

    ''' <summary>
    ''' the in memory code symbol index of an opened workspace.
    ''' </summary>
    ''' <remarks>
    ''' The q-gram full text engine does not hand out document ids, so every
    ''' indexed text is mirrored into a reverse lookup dictionary that maps the
    ''' text back onto the <see cref="CodeSymbol"/> instances that produced it.
    ''' The whole index is rebuilt in memory whenever <see cref="Build"/> runs;
    ''' queries keep working against the previous snapshot until the new one is
    ''' swapped in atomically.
    ''' </remarks>
    Public Class CodeMapIndex

        ReadOnly lock As New Object()
        ''' <summary>the q-gram width of the underlying full text engine</summary>
        ReadOnly q As Integer
        ReadOnly maxCodeLines As Integer
        ReadOnly maxIndexChars As Integer
        ReadOnly collector As SymbolCollector

        Dim _workspace As WorkspaceInfo
        Dim engine As QGramFullText
        Dim byText As Dictionary(Of String, List(Of CodeSymbol))
        Dim byFullName As Dictionary(Of String, List(Of CodeSymbol))
        Dim byName As Dictionary(Of String, List(Of CodeSymbol))
        Dim symbols As List(Of CodeSymbol)
        Dim documents As Integer
        Dim errorCount As Integer
        Dim _status As IndexStatus = IndexStatus.Idle()
        Dim startedAt As Stopwatch

        Sub New(Optional q As Integer = 3,
                Optional maxCodeLines As Integer = 200,
                Optional maxIndexChars As Integer = 8000)

            Me.q = q
            Me.maxCodeLines = maxCodeLines
            Me.maxIndexChars = maxIndexChars
            Me.collector = New SymbolCollector(maxCodeLines)
        End Sub

        ''' <summary>
        ''' a snapshot of the current index state; safe to read from any thread.
        ''' </summary>
        Public ReadOnly Property Status As IndexStatus
            Get
                SyncLock lock
                    Return Clone(_status)
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' the workspace that is currently indexed.
        ''' </summary>
        Public ReadOnly Property Workspace As WorkspaceInfo
            Get
                SyncLock lock
                    Return workspace
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' build the whole index on the calling thread. Callers are expected to
        ''' run this on a background thread; the http service stays responsive
        ''' in the meantime and reports <see cref="Status"/> progress.
        ''' </summary>
        Public Sub Build(ws As WorkspaceInfo)
            Dim sw As Stopwatch = Stopwatch.StartNew()

            SyncLock lock
                workspace = ws
                startedAt = sw
                errorCount = 0
                status = New IndexStatus With {
                    .phase = "building",
                    .ready = False,
                    .totalFiles = If(ws Is Nothing, 0, ws.Files.Count),
                    .message = "opening workspace ..."
                }
            End SyncLock

            Dim newEngine As New QGramFullText(q)
            Dim newByText As New Dictionary(Of String, List(Of CodeSymbol))()
            Dim newByFullName As New Dictionary(Of String, List(Of CodeSymbol))(StringComparer.OrdinalIgnoreCase)
            Dim newByName As New Dictionary(Of String, List(Of CodeSymbol))(StringComparer.OrdinalIgnoreCase)
            Dim all As New List(Of CodeSymbol)()
            Dim docs As Integer = 0
            Dim errs As Integer = 0
            Dim nextId As Integer = 0
            Dim files As List(Of SourceFile) = If(ws Is Nothing, New List(Of SourceFile)(), ws.Files)

            For i As Integer = 0 To files.Count - 1
                Dim file As SourceFile = files(i)

                Try
                    For Each sym As CodeSymbol In collector.Collect(file)
                        nextId += 1
                        sym.Id = nextId

                        Call all.Add(sym)
                        Call Add(newByFullName, sym.FullName, sym)
                        Call Add(newByName, sym.Name, sym)

                        Dim text As String = IndexText(sym)

                        If text.Length > 0 AndAlso Not newByText.ContainsKey(text) Then
                            Call newEngine.Add(text)
                            Call Add(newByText, text, sym)
                            docs += 1
                        End If
                    Next
                Catch ex As Exception
                    errs += 1
                End Try

                If (i + 1) Mod 25 = 0 OrElse i = files.Count - 1 Then
                    SyncLock lock
                        status = New IndexStatus With {
                            .phase = "building",
                            .ready = False,
                            .processedFiles = i + 1,
                            .totalFiles = files.Count,
                            .symbols = all.Count,
                            .documents = docs,
                            .errors = errs,
                            .elapsedMs = sw.ElapsedMilliseconds,
                            .message = $"indexing {file.RelativePath}"
                        }
                    End SyncLock
                End If
            Next

            SyncLock lock
                engine = newEngine
                byText = newByText
                byFullName = newByFullName
                byName = newByName
                symbols = all
                documents = docs
                errorCount = errs
                status = New IndexStatus With {
                    .phase = "ready",
                    .ready = True,
                    .processedFiles = files.Count,
                    .totalFiles = files.Count,
                    .symbols = all.Count,
                    .documents = docs,
                    .errors = errs,
                    .elapsedMs = sw.ElapsedMilliseconds,
                    .message = $"{all.Count} symbols indexed from {files.Count} files"
                }
            End SyncLock
        End Sub

        ''' <summary>
        ''' run a fuzzy keyword query over the indexed symbol texts.
        ''' </summary>
        ''' <param name="query">the raw query text typed by the user.</param>
        ''' <param name="top">max number of symbols to return.</param>
        ''' <param name="threshold">the minimum q-gram similarity of a matched word.</param>
        ''' <returns>
        ''' nothing when the index is not ready yet; an empty array when the
        ''' query produced no candidate at all.
        ''' </returns>
        Public Function Query(query As String, Optional top As Integer = 20, Optional threshold As Double = 0.15) As QueryHit()
            Dim eng As QGramFullText = Nothing
            Dim map As Dictionary(Of String, List(Of CodeSymbol)) = Nothing
            Dim ready As Boolean = False

            SyncLock lock
                ready = status.ready
                eng = engine
                map = byText
            End SyncLock

            If Not ready OrElse eng Is Nothing OrElse map Is Nothing Then
                Return Nothing
            End If

            If String.IsNullOrWhiteSpace(query) Then
                Return New QueryHit() {}
            End If

            If top <= 0 Then
                top = 20
            End If

            ' the engine matches word by word; very short words explode the
            ' candidate set without adding any recall, so drop them and keep
            ' only the most selective words of a long query.
            Dim words As String() = eng.Tokenize(query.Trim().ToLowerInvariant()) _
                .Where(Function(w) w IsNot Nothing AndAlso w.Length >= 3) _
                .Distinct() _
                .OrderByDescending(Function(w) w.Length) _
                .Take(8) _
                .ToArray()

            If words.Length = 0 Then
                Return New QueryHit() {}
            End If

            Dim finds As FindResult()

            Try
                finds = eng.Search(words, top:=Math.Max(top * 4, 40), threshold:=threshold).ToArray()
            Catch ex As Exception
                Return New QueryHit() {}
            End Try

            Dim best As New Dictionary(Of Integer, QueryHit)()

            For Each find As FindResult In finds
                If find Is Nothing OrElse find.text Is Nothing Then
                    Continue For
                End If

                Dim hits As List(Of CodeSymbol) = Nothing

                If Not map.TryGetValue(find.text, hits) Then
                    Continue For
                End If

                For Each sym As CodeSymbol In hits
                    If best.ContainsKey(sym.Id) Then
                        If find.similarity > best(sym.Id).score Then
                            best(sym.Id).score = find.similarity
                        End If
                    Else
                        best(sym.Id) = New QueryHit With {
                            .score = find.similarity,
                            .symbol = sym
                        }
                    End If
                Next
            Next

            Return best.Values _
                .OrderByDescending(Function(h) h.score) _
                .Take(top) _
                .ToArray()
        End Function

        ''' <summary>
        ''' resolve a symbol by its namespace qualified full name, by its simple
        ''' name, or as a last resort through the parsed project models.
        ''' </summary>
        Public Function FindSymbol(fullName As String) As CodeSymbol()
            If String.IsNullOrWhiteSpace(fullName) Then
                Return New CodeSymbol() {}
            End If

            Dim needle As String = fullName.Trim()
            Dim allRef As List(Of CodeSymbol) = Nothing
            Dim fullMap As Dictionary(Of String, List(Of CodeSymbol)) = Nothing
            Dim wsRef As WorkspaceInfo = Nothing

            SyncLock lock
                allRef = symbols
                fullMap = byFullName
                wsRef = workspace
            End SyncLock

            If fullMap IsNot Nothing Then
                Dim exact As List(Of CodeSymbol) = Nothing

                If fullMap.TryGetValue(needle, exact) Then
                    Return exact.ToArray()
                End If
            End If

            If allRef IsNot Nothing Then
                Dim tail As List(Of CodeSymbol) = allRef _
                    .Where(Function(s) s.FullName IsNot Nothing AndAlso s.FullName.EndsWith(needle, StringComparison.OrdinalIgnoreCase)) _
                    .ToList()

                If tail.Count > 0 Then
                    Return tail.Take(50).ToArray()
                End If

                Dim bySimpleName As List(Of CodeSymbol) = allRef _
                    .Where(Function(s) s.Name IsNot Nothing AndAlso String.Equals(s.Name, needle, StringComparison.OrdinalIgnoreCase)) _
                    .ToList()

                If bySimpleName.Count > 0 Then
                    Return bySimpleName.Take(50).ToArray()
                End If
            End If

            ' the symbol may live in a document that was skipped while indexing
            ' (an empty block, a namespace, ...), so ask the project models too.
            Dim sym As LanguageSymbolType = If(wsRef Is Nothing, Nothing, wsRef.GetSymbol(needle))

            If sym IsNot Nothing Then
                Dim loc As Source = If(sym.Source Is Nothing, Nothing, sym.Source.First)
                Dim path As String = If(loc Is Nothing, "", loc.FilePath)
                Dim entry As CodeSymbol = collector.Project(sym, needle, New SourceFile With {
                    .AbsolutePath = path,
                    .RelativePath = path,
                    .Project = ""
                })

                If entry IsNot Nothing Then
                    Return New CodeSymbol() {entry}
                End If
            End If

            Return New CodeSymbol() {}
        End Function

        ''' <summary>
        ''' the text that is handed to the q-gram engine for one symbol.
        ''' </summary>
        Private Function IndexText(sym As CodeSymbol) As String
            Dim sb As New StringBuilder()

            Call sb.Append(sym.FullName).Append(" "c)
            Call sb.Append(sym.Name).Append(" "c)
            Call sb.Append(sym.Kind).Append(" "c)

            If Not String.IsNullOrWhiteSpace(sym.Modifiers) Then
                Call sb.Append(sym.Modifiers).Append(" "c)
            End If

            If Not String.IsNullOrWhiteSpace(sym.XmlDoc) Then
                Call sb.Append(sym.XmlDoc).Append(" "c)
            End If

            If Not String.IsNullOrWhiteSpace(sym.Code) Then
                Call sb.Append(sym.Code)
            End If

            Dim text As String = sb.ToString().Trim()

            If maxIndexChars > 0 AndAlso text.Length > maxIndexChars Then
                text = text.Substring(0, maxIndexChars)
            End If

            Return text
        End Function

        Private Shared Sub Add(map As Dictionary(Of String, List(Of CodeSymbol)), key As String, sym As CodeSymbol)
            If String.IsNullOrEmpty(key) Then
                Return
            End If

            Dim list As List(Of CodeSymbol) = Nothing

            If Not map.TryGetValue(key, list) Then
                list = New List(Of CodeSymbol)()
                map(key) = list
            End If

            Call list.Add(sym)
        End Sub

        Private Shared Function Clone(src As IndexStatus) As IndexStatus
            If src Is Nothing Then
                Return IndexStatus.Idle()
            End If

            Return New IndexStatus With {
                .ready = src.ready,
                .phase = src.phase,
                .processedFiles = src.processedFiles,
                .totalFiles = src.totalFiles,
                .symbols = src.symbols,
                .documents = src.documents,
                .errors = src.errors,
                .elapsedMs = src.elapsedMs,
                .message = src.message
            }
        End Function

    End Class

End Namespace
