Imports System.Net
Imports CodeMap.CodeIndex

Namespace HttpService

    ''' <summary>
    ''' the built in single page query console served on <c>GET /</c>.
    ''' </summary>
    ''' <remarks>
    ''' The page template is an <b>embedded resource</b> of this assembly
    ''' (<c>CodeMap.index.html</c>), so no extra file has to be deployed next to
    ''' the executable.
    ''' </remarks>
    Public Module IndexPage

        ''' <summary>the manifest resource name of the query console template</summary>
        Public Const ResourceName As String = "CodeMap.index.html"

        ''' <summary>a minimal page that is served when the template is missing</summary>
        ReadOnly fallback As String =
            "<!DOCTYPE html><html lang='zh-CN'><head><meta charset='utf-8'>" &
            "<title>CodeMap</title></head><body style='background:#ffffff;color:#1e1e1e;font-family:Segoe UI,sans-serif;padding:32px'>" &
            "<h2 style='color:#007acc'>CodeMap 代码符号索引服务</h2>" &
            "<p>内置查询页模板未嵌入到程序集中。</p>" &
            "<p>可直接使用 HTTP 接口：</p><ul>" &
            "<li><code>GET /api/query?q=关键词&amp;top=20&amp;threshold=0.15</code></li>" &
            "<li><code>GET /api/symbol?fullname=命名空间.类型名</code></li>" &
            "<li><code>GET /api/status</code></li>" &
            "<li><code>GET /api/workspace</code></li></ul></body></html>"

        ''' <summary>the template is read once and then kept in memory</summary>
        Dim cached As String = Nothing

        ''' <summary>
        ''' render the query console for the currently opened workspace.
        ''' </summary>
        Public Function Render(ws As WorkspaceInfo, port As Integer) As String
            Dim html As String = LoadTemplate()

            If html Is Nothing Then
                Return fallback
            End If

            Dim name As String = If(ws Is Nothing, "(none)", ws.Name)
            Dim kind As String = If(ws Is Nothing, "-", ws.KindName)
            Dim root As String = If(ws Is Nothing, "", ws.RootPath)
            Dim projects As Integer = If(ws Is Nothing, 0, ws.Projects.Count)
            Dim files As Integer = If(ws Is Nothing, 0, ws.Files.Count)

            html = html.Replace("{{WORKSPACE}}", WebUtility.HtmlEncode(name))
            html = html.Replace("{{KIND}}", WebUtility.HtmlEncode(kind))
            html = html.Replace("{{ROOT}}", WebUtility.HtmlEncode(root))
            html = html.Replace("{{PROJECTS}}", projects.ToString())
            html = html.Replace("{{FILES}}", files.ToString())
            html = html.Replace("{{PORT}}", port.ToString())

            Return html
        End Function

        ''' <summary>
        ''' read the embedded page template out of the executing assembly.
        ''' </summary>
        ''' <returns>the template text, or nothing when it is not embedded.</returns>
        Public Function LoadTemplate() As String
            If cached IsNot Nothing Then
                Return cached
            End If

            Dim asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
            Dim name As String = ResolveResourceName(asm)

            If name Is Nothing Then
                Return Nothing
            End If

            Dim stream As IO.Stream = Nothing

            Try
                stream = asm.GetManifestResourceStream(name)

                If stream Is Nothing Then
                    Return Nothing
                End If

                Using reader As New IO.StreamReader(stream, Text.Encoding.UTF8)
                    stream = Nothing
                    cached = reader.ReadToEnd()
                End Using

                Return cached
            Catch
                Return Nothing
            Finally
                If stream IsNot Nothing Then
                    Call stream.Dispose()
                End If
            End Try
        End Function

        ''' <summary>
        ''' prefer the explicit logical name, otherwise fall back to any embedded
        ''' resource that ends with <c>index.html</c>.
        ''' </summary>
        Private Function ResolveResourceName(asm As Reflection.Assembly) As String
            Dim names As String()

            Try
                names = asm.GetManifestResourceNames()
            Catch
                Return Nothing
            End Try

            If names Is Nothing OrElse names.Length = 0 Then
                Return Nothing
            End If

            For Each n As String In names
                If String.Equals(n, ResourceName, StringComparison.OrdinalIgnoreCase) Then
                    Return n
                End If
            Next

            For Each n As String In names
                If n.EndsWith("index.html", StringComparison.OrdinalIgnoreCase) Then
                    Return n
                End If
            Next

            Return Nothing
        End Function

    End Module

End Namespace
