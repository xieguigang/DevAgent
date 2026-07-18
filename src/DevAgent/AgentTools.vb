Imports System.IO
Imports System.Text
Imports System.ComponentModel
Imports Microsoft.VisualBasic.CommandLine.Reflection
' 注意: ArgumentAttribute 来自你的 Ollama 模块，请根据实际命名空间添加 Imports

' ============================================================================
' AgentTools.vb - LLM 函数工具
'
' 本文件定义的类包含供 LLM 通过 function calling 调用的方法。
' 这些方法通过 Ollama.AddFunction(Of T)(obj, fun) 注册到 LLM 会话中，
' 使 LLM 能够读取项目文件、浏览目录结构、搜索代码。
'
' 所有路径都相对于项目根目录，确保 LLM 无法访问项目目录之外的文件。
' ============================================================================

''' <summary>
''' 提供给 LLM 的文件系统操作工具。
''' 所有方法返回 String，便于 LLM 直接消费返回结果。
''' </summary>
Public Class AgentTools

    Private ReadOnly _basePath As String

    ''' <param name="basePath">项目根目录的绝对路径。</param>
    Public Sub New(basePath As String)
        _basePath = Path.GetFullPath(basePath)
    End Sub

    ''' <summary>
    ''' 读取指定文件的完整文本内容。
    ''' </summary>
    <Description("Read the full text content of a file from the project. Returns the file content as a string, or an error message if the file is not found.")>
    Public Function read_file(
        <Argument("path", Description:="The file path relative to the project root directory, e.g. 'src/Program.vb' or 'Module1.vb'")> path As String
    ) As String
        Try
            Dim fullPath As String = ResolveSafePath(path)
            If Not File.Exists(fullPath) Then
                Return $"Error: File not found: {path}"
            End If
            Return File.ReadAllText(fullPath)
        Catch ex As Exception
            Return $"Error reading file '{path}': {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' 列出指定目录下的所有文件和子目录。
    ''' </summary>
    <Description("List all files and subdirectories in the given directory. Each entry is on a separate line. Directories are prefixed with [DIR], files with [FILE].")>
    Public Function list_files(
        <Argument("path", Description:="The directory path relative to the project root. Use '.' for the project root directory.")> path As String
    ) As String
        Try
            Dim fullPath As String = ResolveSafePath(path)
            If Not Directory.Exists(fullPath) Then
                Return $"Error: Directory not found: {path}"
            End If

            Dim sb As New StringBuilder()

            For Each dir As String In Directory.GetDirectories(fullPath)
                Dim name As String = dir.FileName
                ' Skip hidden directories and build output
                If Not name.StartsWith(".") AndAlso name <> "bin" AndAlso name <> "obj" AndAlso name <> "test" Then
                    sb.AppendLine("[DIR]  " & name & "/")
                End If
            Next

            For Each file As String In Directory.GetFiles(fullPath)
                Dim name As String = file.FileName
                If Not name.StartsWith(".") Then
                    sb.AppendLine("[FILE] " & name)
                End If
            Next

            Return sb.ToString().TrimEnd()
        Catch ex As Exception
            Return $"Error listing directory '{path}': {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' 检查文件是否存在。
    ''' </summary>
    <Description("Check if a file exists at the given path. Returns 'true' or 'false'.")>
    Public Function file_exists(
        <Argument("path", Description:="The file path relative to the project root directory.")> path As String
    ) As String
        Try
            Dim fullPath As String = ResolveSafePath(path)
            Return If(File.Exists(fullPath), "true", "false")
        Catch
            Return "false"
        End Try
    End Function

    ''' <summary>
    ''' 在项目文件中搜索指定文本。
    ''' </summary>
    <Description("Search for a text pattern in all source files within the project. Returns matching lines with file paths and line numbers. Case-insensitive.")>
    Public Function search_files(
        <Argument("pattern", Description:="The text pattern to search for (case-insensitive).")> pattern As String,
        <Argument("extension", Description:="Optional file extension filter, e.g. '.vb'. Pass empty string or '*' for all files.")> extension As String
    ) As String
        Try
            Dim sb As New StringBuilder()
            Dim searchPattern As String = If(String.IsNullOrEmpty(extension) OrElse extension = "*", "*.*", "*" & extension)
            Dim files() As String = Directory.GetFiles(_basePath, searchPattern, SearchOption.AllDirectories)

            For Each file As String In files
                ' Skip bin/obj/.git directories
                If ContainsExcludedPath(file) Then Continue For

                Dim lines() As String = file.ReadAllLines
                For i As Integer = 0 To lines.Length - 1
                    If lines(i).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        Dim relPath As String = file.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar, "/"c)
                        sb.AppendLine($"{relPath}:{i + 1}: {lines(i).Trim()}")
                    End If
                Next
            Next

            Return If(sb.Length > 0, sb.ToString().TrimEnd(), "No matches found.")
        Catch ex As Exception
            Return $"Error searching: {ex.Message}"
        End Try
    End Function

    ''' <summary>
    ''' 获取项目的完整文件树。
    ''' </summary>
    <Description("Get a tree view of all source files in the project, excluding bin/obj/.git/test directories. Returns the project structure as an indented tree.")>
    Public Function get_project_tree() As String
        Try
            Dim sb As New StringBuilder()
            BuildTree(_basePath, "", sb)
            Return If(sb.Length > 0, sb.ToString().TrimEnd(), "(empty project)")
        Catch ex As Exception
            Return $"Error getting project tree: {ex.Message}"
        End Try
    End Function

    ' -------------------------------------------------------------------
    ' 私有辅助方法
    ' -------------------------------------------------------------------

    Private Sub BuildTree(dir As String, indent As String, sb As StringBuilder)
        Dim dirs() As String = Directory.GetDirectories(dir)
        Dim files() As String = Directory.GetFiles(dir)

        For Each d As String In dirs
            Dim name As String = Path.GetFileName(d)
            If ShouldSkip(name) Then Continue For

            sb.AppendLine(indent & "[DIR]  " & name & "/")
            BuildTree(d, indent & "  ", sb)
        Next

        For Each f As String In files
            Dim name As String = Path.GetFileName(f)
            If name.StartsWith(".") Then Continue For
            sb.AppendLine(indent & "[FILE] " & name)
        Next
    End Sub

    Private Function ShouldSkip(name As String) As Boolean
        Return name.StartsWith(".") OrElse name = "bin" OrElse name = "obj" OrElse name = "test"
    End Function

    Private Function ContainsExcludedPath(fullPath As String) As Boolean
        Dim sep As Char = Path.DirectorySeparatorChar
        Return fullPath.Contains(sep & "bin" & sep) OrElse
               fullPath.Contains(sep & "obj" & sep) OrElse
               fullPath.Contains(sep & ".git" & sep)
    End Function

    ''' <summary>
    ''' 将相对路径解析为绝对路径，并确保路径在项目目录内（安全检查）。
    ''' </summary>
    Private Function ResolveSafePath(relativePath As String) As String
        If String.IsNullOrEmpty(relativePath) Then
            Return _basePath
        End If

        ' Normalize path separators
        relativePath = relativePath.Replace("/"c, Path.DirectorySeparatorChar)

        ' Handle "." as project root
        If relativePath = "." Then
            Return _basePath
        End If

        Dim fullPath As String
        If Path.IsPathRooted(relativePath) Then
            fullPath = Path.GetFullPath(relativePath)
        Else
            fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath))
        End If

        ' Security: ensure the path is within the project directory
        If Not fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase) Then
            Throw New System.Security.SecurityException($"Path '{relativePath}' is outside the project directory.")
        End If

        Return fullPath
    End Function

End Class
