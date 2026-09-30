Imports System.Net
Imports CodeMap.CodeIndex

Namespace HttpService

    ''' <summary>
    ''' the built in single page query console served on <c>GET /</c>.
    ''' </summary>
    Public Module IndexPage

        ReadOnly fallback As String =
            "<!DOCTYPE html><html lang='zh-CN'><head><meta charset='utf-8'>" &
            "<title>CodeMap</title></head><body style='background:#0F172A;color:#E2E8F0;font-family:Noto Sans,sans-serif;padding:32px'>" &
            "<h2 style='color:#2DD4BF'>CodeMap 代码符号索引服务</h2>" &
            "<p>内置查询页模板 (index.html) 未随程序一起部署。</p>" &
            "<p>可直接使用 HTTP 接口：</p><ul>" &
            "<li><code>GET /api/query?q=关键词&amp;top=20&amp;threshold=0.15</code></li>" &
            "<li><code>GET /api/symbol?fullname=命名空间.类型名</code></li>" &
            "<li><code>GET /api/status</code></li>" &
            "<li><code>GET /api/workspace</code></li></ul></body></html>"

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

        Private Function LoadTemplate() As String
            Dim base As String = AppDomain.CurrentDomain.BaseDirectory
            Dim candidates As String() = {
                IO.Path.Combine(base, "index.html"),
                IO.Path.Combine(base, "HttpService", "index.html"),
                IO.Path.Combine(base, "..", "..", "..", "HttpService", "index.html")
            }

            For Each path As String In candidates
                Try
                    If IO.File.Exists(path) Then
                        Return IO.File.ReadAllText(path)
                    End If
                Catch
                End Try
            Next

            Return Nothing
        End Function

    End Module

End Namespace
