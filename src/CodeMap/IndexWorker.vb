Imports System.Threading
Imports CodeMap.CodeIndex

Public Module IndexWorker

    Public Sub IndexInBackground(index As CodeMapIndex, workspacePath As String)
        ' both the workspace parsing and the index building run on a background
        ' thread so that the http service starts listening right away; queries
        ' are answered with HTTP 503 until the index is ready.
        Call New Thread(Sub() RunIndex(index, workspacePath)) With {
            .IsBackground = True,
            .Name = "codemap-index"
        }.Start()
    End Sub

    Private Sub RunIndex(index As CodeMapIndex, workspacePath As String)
        Dim ws As WorkspaceInfo = Nothing

        Try
            Console.WriteLine($"[CodeMap] opening workspace (read only): {workspacePath}")
            ws = WorkspaceLoader.Open(workspacePath)
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMap] cannot open workspace: {ex.Message}")
            Call index.MarkFailed(ex.Message)
            Return
        End Try

        Console.WriteLine($"[CodeMap] workspace  : {ws.Name} ({ws.KindName})")
        Console.WriteLine($"[CodeMap] root       : {ws.RootPath}")
        Console.WriteLine($"[CodeMap] projects   : {ws.Projects.Count}")
        Console.WriteLine($"[CodeMap] source file: {ws.Files.Count}")

        If ws.Errors.Count > 0 Then
            Console.WriteLine($"[CodeMap] warnings   : {ws.Errors.Count} (see /api/workspace)")

            For Each err As String In ws.Errors.Take(5)
                Console.WriteLine($"            ! {err}")
            Next
        End If

        If ws.Files.Count = 0 Then
            Call index.MarkFailed("no vb.net source file found in the given workspace.")
            Return
        End If

        Try
            Call index.Build(ws)
            Console.WriteLine($"[CodeMap] index ready: {index.Status.symbols} symbols, {index.Status.elapsedMs} ms")
        Catch ex As Exception
            Console.Error.WriteLine($"[CodeMap] index build failed: {ex.Message}")
            Call index.MarkFailed(ex.Message)
        End Try
    End Sub
End Module
