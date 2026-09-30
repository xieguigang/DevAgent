Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.sln
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj
Imports Microsoft.VisualBasic.ApplicationServices.Development.VisualStudio.VBProj.CodeDOM

Namespace CodeIndex

    ''' <summary>
    ''' the kind of the opened code repository workspace.
    ''' </summary>
    Public Enum WorkspaceKind
        ''' <summary>a visual studio solution file (.sln / .slnx)</summary>
        Solution
        ''' <summary>a single visual basic project file (.vbproj)</summary>
        Project
        ''' <summary>a plain source folder, usually a git repository working directory</summary>
        Folder
    End Enum

    ''' <summary>
    ''' one vb.net source file that was enumerated from the workspace.
    ''' </summary>
    Public Class SourceFile

        ''' <summary>the project name that owns this source file</summary>
        Public Property Project As String
        ''' <summary>the absolute path of the vb.net source file</summary>
        Public Property AbsolutePath As String
        ''' <summary>the display path (relative to the workspace root when possible)</summary>
        Public Property RelativePath As String
        ''' <summary>the language symbols parsed from this document; may be nothing for empty files</summary>
        Public Property Types As Dictionary(Of String, LanguageSymbolType)
        ''' <summary>the Imports statements of this document</summary>
        Public Property [Imports] As String()

        Public Overrides Function ToString() As String
            Return $"[{Project}] {RelativePath}"
        End Function

    End Class

    ''' <summary>
    ''' a read only view over an opened code repository.
    ''' </summary>
    Public Class WorkspaceInfo

        Public Property Kind As WorkspaceKind
        Public Property Name As String
        Public Property RootPath As String
        ''' <summary>the project names discovered inside the workspace</summary>
        Public Property Projects As New List(Of String)
        ''' <summary>all vb.net source files that should be indexed</summary>
        Public Property Files As New List(Of SourceFile)
        ''' <summary>the underlying <see cref="IProjectWorkspace"/> handle</summary>
        Public Property Workspace As IProjectWorkspace
        ''' <summary>the parsed visual basic projects (empty in pure folder mode)</summary>
        Public Property VbProjects As New List(Of VBProject)
        ''' <summary>non fatal errors raised while opening the workspace</summary>
        Public Property Errors As New List(Of String)

        Public ReadOnly Property KindName As String
            Get
                Return Kind.ToString()
            End Get
        End Property

        ''' <summary>
        ''' lookup a symbol by its namespace qualified full name through the
        ''' parsed project models; returns nothing when not found.
        ''' </summary>
        Public Function GetSymbol(fullName As String) As LanguageSymbolType
            If String.IsNullOrWhiteSpace(fullName) Then
                Return Nothing
            End If

            For Each proj As VBProject In VbProjects
                Dim sym As LanguageSymbolType = Nothing

                Try
                    sym = proj.GetType(fullName)
                Catch
                    sym = Nothing
                End Try

                If sym IsNot Nothing Then
                    Return sym
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' resolve the project name that owns the given source file path.
        ''' </summary>
        Public Function ProjectOf(path As String) As String
            If String.IsNullOrEmpty(path) Then
                Return ""
            End If

            For Each f As SourceFile In Files
                If String.Equals(f.AbsolutePath, path, StringComparison.OrdinalIgnoreCase) Then
                    Return f.Project
                End If
            Next

            Return ""
        End Function

    End Class

    ''' <summary>
    ''' one indexed code symbol (a type declaration or a type member).
    ''' </summary>
    Public Class CodeSymbol

        Public Property Id As Integer
        ''' <summary>the simple symbol name</summary>
        Public Property Name As String
        ''' <summary>the namespace qualified full name</summary>
        Public Property FullName As String
        ''' <summary>the symbol kind, e.g. Class / Function / Property</summary>
        Public Property Kind As String
        ''' <summary>access and custom modifiers, e.g. "Public Shared"</summary>
        Public Property Modifiers As String
        ''' <summary>the owning project name</summary>
        Public Property Project As String
        ''' <summary>the absolute path of the file that declares this symbol</summary>
        Public Property File As String
        ''' <summary>the display path of the declaring file</summary>
        Public Property RelativeFile As String
        Public Property StartLine As Integer
        Public Property EndLine As Integer
        Public Property DeclarationLine As Integer
        ''' <summary>the xml documentation comment of this symbol</summary>
        Public Property XmlDoc As String
        ''' <summary>the raw vb.net source text of the whole declaration block</summary>
        Public Property Code As String

        Public ReadOnly Property Location As String
            Get
                Return $"{RelativeFile}:{StartLine}-{EndLine}"
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Kind} {FullName} @ {Location}"
        End Function

    End Class

    ''' <summary>
    ''' one keyword search hit: an indexed symbol together with its rank score.
    ''' </summary>
    Public Class QueryHit

        ''' <summary>the raw q-gram similarity reported by the full text engine</summary>
        Public Property rawScore As Double
        ''' <summary>
        ''' the ranking score: the raw similarity scaled by how well the symbol
        ''' name itself matches the query.
        ''' </summary>
        Public Property score As Double
        ''' <summary>the matched code symbol</summary>
        Public Property symbol As CodeSymbol

        Public Overrides Function ToString() As String
            Return $"{score:F4} {symbol}"
        End Function

    End Class

    ''' <summary>
    ''' the runtime state of the in memory symbol index.
    ''' </summary>
    Public Class IndexStatus

        ''' <summary>true when the index has been fully built and can serve queries</summary>
        Public Property ready As Boolean
        ''' <summary>idle / building / ready / failed</summary>
        Public Property phase As String
        Public Property processedFiles As Integer
        Public Property totalFiles As Integer
        Public Property symbols As Integer
        Public Property documents As Integer
        Public Property errors As Integer
        Public Property elapsedMs As Long
        Public Property message As String

        Public ReadOnly Property progress As Double
            Get
                If totalFiles <= 0 Then
                    Return 0
                End If

                Return processedFiles / totalFiles
            End Get
        End Property

        Public Shared Function Idle() As IndexStatus
            Return New IndexStatus With {
                .phase = "idle",
                .ready = False,
                .message = "index has not been built yet"
            }
        End Function

    End Class

End Namespace
