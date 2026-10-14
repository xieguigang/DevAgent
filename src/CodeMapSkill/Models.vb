Imports System.Text
Imports System.Text.Encodings.Web
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports CodeMap.CodeIndex
Imports Flute.Http.Core.Message

''' <summary>
''' the json serialisation settings shared by the rest endpoints and the mcp
''' endpoint: both protocols must render the very same payload, so they also
''' share the very same writer options.
''' </summary>
Module JsonText

    ''' <summary>
    ''' relaxed escaping keeps the vb.net source code that is embedded in the
    ''' responses readable (no \uXXXX for chinese comments, no escaped '+').
    ''' </summary>
    Public ReadOnly Options As New JsonSerializerOptions With {
        .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        .Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        .WriteIndented = False
    }

    Public Function ToJson(payload As Object) As String
        Return JsonSerializer.Serialize(payload, Options)
    End Function

    ''' <summary>
    ''' write a json payload with an explicit content type; <c>WriteJSON</c> would
    ''' run the payload through a second serialiser, so every endpoint goes
    ''' through this helper instead.
    ''' </summary>
    Public Sub WriteJson(res As HttpResponse, payload As Object)
        Dim bytes As Byte() = Encoding.UTF8.GetBytes(ToJson(payload))

        res.AccessControlAllowOrigin = "*"
        res.WriteContent(bytes, "application/json; charset=utf-8")
    End Sub

    ''' <summary>write a plain text payload (markdown, yaml, ...).</summary>
    Public Sub WriteText(res As HttpResponse, text As String, mime As String)
        Dim bytes As Byte() = Encoding.UTF8.GetBytes(If(text, ""))

        res.AccessControlAllowOrigin = "*"
        res.WriteContent(bytes, mime)
    End Sub

    ''' <summary>
    ''' fold a path onto backslashes: the parser stores source locations with a
    ''' mixed separator style (<c>G:/repo/src\File.vb</c>) while the workspace file
    ''' list uses native paths, so the two only compare equal after folding.
    ''' </summary>
    Public Function NormPath(text As String) As String
        If String.IsNullOrEmpty(text) Then
            Return ""
        End If

        Return text.Replace("/"c, "\"c)
    End Function

End Module

''' <summary>
''' a compact view over an indexed symbol: the raw <c>CodeSymbol</c> carries up
''' to <c>maxIndexChars</c> characters of source text, which is far too much for
''' an llm context when a query returns twenty hits.
''' </summary>
Public Class SymbolView

    Public Property id As Integer
    Public Property name As String
    Public Property fullName As String
    Public Property kind As String
    Public Property modifiers As String
    Public Property project As String
    Public Property file As String
    Public Property relativeFile As String
    Public Property startLine As Integer
    Public Property endLine As Integer
    Public Property declarationLine As Integer
    Public Property score As Double
    Public Property xmlDoc As String
    Public Property code As String
    Public Property truncated As Boolean

    ''' <summary>
    ''' project a symbol into its transport view, clipping the source text to
    ''' <paramref name="maxCodeChars"/> characters.
    ''' </summary>
    Public Shared Function From(sym As CodeSymbol,
                                Optional score As Double = 0,
                                Optional maxCodeChars As Integer = 1200) As SymbolView

        If sym Is Nothing Then
            Return Nothing
        End If

        Dim code As String = If(sym.Code, "")
        Dim clipped As Boolean = False

        If maxCodeChars > 0 AndAlso code.Length > maxCodeChars Then
            code = code.Substring(0, maxCodeChars)
            clipped = True
        End If

        Dim doc As String = If(sym.XmlDoc, "")
        Dim clippedDoc As Boolean = False

        If doc.Length > 600 Then
            doc = doc.Substring(0, 600)
            clippedDoc = True
        End If

        Return New SymbolView With {
            .id = sym.Id,
            .name = sym.Name,
            .fullName = sym.FullName,
            .kind = sym.Kind,
            .modifiers = sym.Modifiers,
            .project = sym.Project,
            .file = NormPath(sym.File),
            .relativeFile = sym.RelativeFile,
            .startLine = sym.StartLine,
            .endLine = sym.EndLine,
            .declarationLine = sym.DeclarationLine,
            .score = System.Math.Round(score, 6),
            .xmlDoc = If(clippedDoc, doc & " ...", doc),
            .code = code,
            .truncated = clipped OrElse clippedDoc
        }
    End Function

End Class

''' <summary>the json payload returned by <c>/api/query</c></summary>
Public Class QueryResponse
    Public Property workspaceId As String
    Public Property query As String
    Public Property top As Integer
    Public Property threshold As Double
    Public Property count As Integer
    Public Property elapsedMs As Long
    Public Property hits As SymbolView()
End Class

''' <summary>the json payload returned by <c>/api/symbol</c></summary>
Public Class SymbolResponse
    Public Property workspaceId As String
    Public Property fullName As String
    Public Property count As Integer
    Public Property symbols As SymbolView()
End Class

''' <summary>a compact workspace description</summary>
Public Class WorkspaceSummary
    Public Property kind As String
    Public Property name As String
    Public Property rootPath As String
    Public Property projectCount As Integer
    Public Property fileCount As Integer
    Public Property projects As String()
    Public Property errors As String()
End Class

''' <summary>the runtime state of one indexed workspace</summary>
Public Class StatusResponse
    Public Property workspaceId As String
    Public Property path As String
    Public Property ready As Boolean
    Public Property phase As String
    Public Property processedFiles As Integer
    Public Property totalFiles As Integer
    Public Property symbols As Integer
    Public Property documents As Integer
    Public Property errors As Integer
    Public Property elapsedMs As Long
    Public Property message As String
    Public Property progress As Double
    Public Property workspace As WorkspaceSummary
End Class

''' <summary>one entry of the workspace pool</summary>
Public Class WorkspaceEntryView
    Public Property id As String
    Public Property path As String
    Public Property isDefault As Boolean
    Public Property name As String
    Public Property kind As String
    Public Property projectCount As Integer
    Public Property fileCount As Integer
    Public Property ready As Boolean
    Public Property phase As String
    Public Property symbols As Integer
    Public Property openedAt As String
    Public Property lastAccessAt As String
End Class

Public Class WorkspacesResponse
    Public Property defaultId As String
    Public Property capacity As Integer
    Public Property count As Integer
    Public Property workspaces As WorkspaceEntryView()
End Class

Public Class OpenWorkspaceResponse
    Public Property workspaceId As String
    Public Property path As String
    Public Property name As String
    Public Property isNew As Boolean
    Public Property status As StatusResponse
End Class

''' <summary>the json payload returned by <c>/api/source</c></summary>
Public Class SourceResponse
    Public Property workspaceId As String
    Public Property project As String
    Public Property file As String
    Public Property relativeFile As String
    Public Property startLine As Integer
    Public Property endLine As Integer
    Public Property totalLines As Integer
    Public Property truncated As Boolean
    Public Property code As String
End Class

''' <summary>one member of a type: a method, a property, a field, ...</summary>
Public Class MemberView
    Public Property name As String
    Public Property kind As String
    Public Property modifiers As String
    Public Property signature As String
    Public Property returnType As String
    Public Property parameters As String()
    Public Property startLine As Integer
    Public Property endLine As Integer
    Public Property declarationLine As Integer
    Public Property file As String
    Public Property relativeFile As String
    Public Property xmlDoc As String
End Class

Public Class MembersResponse
    Public Property workspaceId As String
    Public Property fullName As String
    Public Property kind As String
    Public Property project As String
    Public Property file As String
    Public Property relativeFile As String
    Public Property startLine As Integer
    Public Property endLine As Integer
    Public Property xmlDoc As String
    Public Property memberCount As Integer
    Public Property members As MemberView()
    Public Property nestedTypeCount As Integer
    Public Property nestedTypes As MemberView()
End Class

Public Class FileView
    Public Property project As String
    Public Property relativePath As String
    Public Property absolutePath As String
End Class

Public Class FilesResponse
    Public Property workspaceId As String
    Public Property project As String
    Public Property pattern As String
    Public Property count As Integer
    Public Property files As FileView()
End Class

''' <summary>
''' the error payload; the rest endpoints answer with an http error code and
''' this body, the mcp endpoint answers with a json-rpc error object.
''' </summary>
Public Class ErrorResponse
    Public Property [error] As String
    Public Property detail As String
    Public Property workspaceId As String
End Class
