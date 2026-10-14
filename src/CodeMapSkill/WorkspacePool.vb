Imports System.Threading
Imports CodeMap.CodeIndex

''' <summary>
''' one opened code repository: the read only workspace model plus the in
''' memory symbol index that is built from it.
''' </summary>
Public Class WorkspaceEntry

    ''' <summary>the pool local handle of this workspace, e.g. <c>ws1</c></summary>
    Public Property Id As String
    ''' <summary>the workspace path exactly as it was opened (a full path)</summary>
    Public Property Path As String
    ''' <summary>the symbol index of this workspace</summary>
    Public Property Index As CodeMapIndex
    Public Property OpenedAt As DateTime
    Public Property LastAccess As DateTime
    ''' <summary>true when this call created the entry (false when it was reused)</summary>
    Public Property IsNew As Boolean

    Public Overrides Function ToString() As String
        Return $"{Id} => {Path}"
    End Function

End Class

''' <summary>
''' a thread safe pool of indexed workspaces.
''' </summary>
''' <remarks>
''' Every workspace owns its own <see cref="CodeMapIndex"/>, and the index is
''' built on a background thread: <see cref="Open"/> returns as soon as the path
''' has been validated so that the agent is not blocked by a large solution.
''' Queries are answered with an error while the index of the requested
''' workspace is still building.
'''
''' The pool is bounded: opening a workspace beyond <c>capacity</c> evicts the
''' least recently used entry, because a full symbol index of a large solution
''' is a sizeable chunk of memory that should not be kept around forever.
''' </remarks>
Public Class WorkspacePool

    ReadOnly lock As New Object()
    ReadOnly entries As New Dictionary(Of String, WorkspaceEntry)(StringComparer.OrdinalIgnoreCase)
    ReadOnly q As Integer
    ReadOnly maxCodeLines As Integer

    Dim seq As Integer = 0
    Dim currentDefaultId As String = ""

    ''' <param name="maxWorkspaces">the max number of workspaces kept in memory (0 disables the limit).</param>
    Sub New(Optional maxWorkspaces As Integer = 4,
            Optional q As Integer = 3,
            Optional maxCodeLines As Integer = 200)

        Me.Capacity = If(maxWorkspaces <= 0, Integer.MaxValue, maxWorkspaces)
        Me.q = q
        Me.maxCodeLines = maxCodeLines
    End Sub

    Public ReadOnly Property Capacity As Integer

    Public Property DefaultId As String
        Get
            SyncLock lock
                Return currentDefaultId
            End SyncLock
        End Get
        Set(value As String)
            SyncLock lock
                If value Is Nothing OrElse entries.ContainsKey(value) Then
                    currentDefaultId = If(value, "")
                End If
            End SyncLock
        End Set
    End Property

    ''' <summary>
    ''' open a code repository. The returned entry is indexed in the background;
    ''' poll <c>/api/status</c> (or the <c>index_status</c> tool) until ready.
    ''' </summary>
    ''' <param name="wsPath">a .sln / .slnx / .vbproj file or a source folder.</param>
    Public Function Open(wsPath As String) As WorkspaceEntry
        If String.IsNullOrWhiteSpace(wsPath) Then
            Throw New ArgumentException("a workspace path is required.")
        End If

        Dim full As String

        Try
            full = IO.Path.GetFullPath(wsPath.Trim().Trim(""""c))
        Catch ex As Exception
            Throw New ArgumentException($"'{wsPath}' is not a usable path: {ex.Message}")
        End Try

        If Not IO.Directory.Exists(full) AndAlso Not IO.File.Exists(full) Then
            Throw New IO.DirectoryNotFoundException($"workspace path does not exist: {full}")
        End If

        Dim entry As WorkspaceEntry = Nothing
        Dim created As Boolean = False

        SyncLock lock
            ' the very same repository must never be indexed twice
            For Each existing As WorkspaceEntry In entries.Values
                If String.Equals(existing.Path, full, StringComparison.OrdinalIgnoreCase) Then
                    entry = existing
                    Exit For
                End If
            Next

            If entry Is Nothing Then
                seq += 1

                entry = New WorkspaceEntry With {
                    .Id = "ws" & seq,
                    .Path = full,
                    .Index = New CodeMapIndex(q:=q, maxCodeLines:=maxCodeLines),
                    .OpenedAt = DateTime.Now,
                    .LastAccess = DateTime.Now,
                    .IsNew = True
                }

                created = True

                If entries.Count >= Capacity Then
                    Call EvictLocked(entry.Id)
                End If

                entries(entry.Id) = entry

                If String.IsNullOrEmpty(currentDefaultId) Then
                    currentDefaultId = entry.Id
                End If
            Else
                entry.IsNew = False
            End If

            entry.LastAccess = DateTime.Now
        End SyncLock

        If created Then
            Call BuildInBackground(entry)
        End If

        Return entry
    End Function

    ''' <summary>
    ''' resolve a workspace: an empty or missing id falls back to the default
    ''' workspace of the pool.
    ''' </summary>
    Public Function Resolve(id As String) As WorkspaceEntry
        SyncLock lock
            Dim key As String = If(String.IsNullOrWhiteSpace(id), currentDefaultId, id)
            Dim entry As WorkspaceEntry = Nothing

            If key IsNot Nothing AndAlso entries.TryGetValue(key, entry) Then
                entry.LastAccess = DateTime.Now
                Return entry
            End If

            Return Nothing
        End SyncLock
    End Function

    Public Function List() As WorkspaceEntry()
        SyncLock lock
            Return entries.Values _
                .OrderByDescending(Function(e) e.LastAccess) _
                .ToArray()
        End SyncLock
    End Function

    Public Function Count As Integer
        SyncLock lock
            Return entries.Count
        End SyncLock
    End Function

    ''' <summary>
    ''' drop the least recently used workspace that is not the default one and
    ''' that is not the entry that is being opened right now.
    ''' </summary>
    Private Sub EvictLocked(exceptId As String)
        Dim victim As WorkspaceEntry = Nothing

        For Each e As WorkspaceEntry In entries.Values
            If String.Equals(e.Id, exceptId, StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            If String.Equals(e.Id, currentDefaultId, StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            If victim Is Nothing OrElse e.LastAccess < victim.LastAccess Then
                victim = e
            End If
        Next

        If victim IsNot Nothing Then
            Call entries.Remove(victim.Id)
            Console.WriteLine($"[CodeMapSkill] workspace {victim.Id} evicted ({victim.Path})")
        End If
    End Sub

    Private Sub BuildInBackground(entry As WorkspaceEntry)
        Dim t As New Thread(Sub() Build(entry)) With {
            .IsBackground = True,
            .Name = "codemap-skill-index-" & entry.Id
        }

        Call t.Start()
    End Sub

    Private Sub Build(entry As WorkspaceEntry)
        Dim ws As WorkspaceInfo = Nothing

        Console.WriteLine($"[CodeMapSkill] [{entry.Id}] opening workspace (read only): {entry.Path}")

        Try
            ws = WorkspaceLoader.Open(entry.Path)
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMapSkill] [{entry.Id}] cannot open workspace: {ex.Message}")
            Call entry.Index.MarkFailed(ex.Message)
            Return
        End Try

        Console.WriteLine($"[CodeMapSkill] [{entry.Id}] {ws.Name} ({ws.KindName}): {ws.Files.Count} source files, {ws.Projects.Count} projects")

        If ws.Files.Count = 0 Then
            Call entry.Index.MarkFailed("no vb.net source file found in the given workspace.")
            Return
        End If

        Try
            Call entry.Index.Build(ws)
            Console.WriteLine($"[CodeMapSkill] [{entry.Id}] index ready: {entry.Index.Status.symbols} symbols, {entry.Index.Status.elapsedMs} ms")
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMapSkill] [{entry.Id}] index build failed: {ex.Message}")
            Call entry.Index.MarkFailed(ex.Message)
        End Try
    End Sub

    ''' <summary>the runtime state of one workspace entry.</summary>
    Public Shared Function StatusOf(entry As WorkspaceEntry) As StatusResponse
        Dim st As IndexStatus = entry.Index.Status

        Return New StatusResponse With {
            .workspaceId = entry.Id,
            .path = entry.Path,
            .ready = st.ready,
            .phase = st.phase,
            .processedFiles = st.processedFiles,
            .totalFiles = st.totalFiles,
            .symbols = st.symbols,
            .documents = st.documents,
            .errors = st.errors,
            .elapsedMs = st.elapsedMs,
            .message = st.message,
            .progress = System.Math.Round(st.progress, 4),
            .workspace = Summarize(entry.Index.Workspace)
        }
    End Function

    Public Shared Function Summarize(ws As WorkspaceInfo) As WorkspaceSummary
        If ws Is Nothing Then
            Return New WorkspaceSummary With {
                .kind = "none",
                .name = "(not opened)"
            }
        End If

        Dim errs As String()

        If ws.Errors.Count > 20 Then
            errs = ws.Errors.Take(20).ToArray()
        Else
            errs = ws.Errors.ToArray()
        End If

        Return New WorkspaceSummary With {
            .kind = ws.KindName,
            .name = ws.Name,
            .rootPath = ws.RootPath,
            .projectCount = ws.Projects.Count,
            .fileCount = ws.Files.Count,
            .projects = ws.Projects.ToArray(),
            .errors = errs
        }
    End Function

End Class
