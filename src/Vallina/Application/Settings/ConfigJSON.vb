Imports Microsoft.VisualBasic.Serialization.JSON

Namespace Settings

    Public Class ConfigJSON

        Public Property appearance As appearance
        Public Property llm As llm
        Public Property devTools As devTools

        Shared ReadOnly Property defaultFile As String = App.ProductProgramData & "/dev-config.json"

        Public Shared Function Load() As ConfigJSON
            Dim json As ConfigJSON = defaultFile.LoadJsonFile(Of ConfigJSON)(throwEx:=False)

            If json Is Nothing Then
                json = New ConfigJSON With {.appearance = New appearance, .devTools = New devTools, .llm = New llm}
            End If

            If json.appearance Is Nothing Then json.appearance = New appearance
            If json.llm Is Nothing Then json.llm = New llm
            If json.devTools Is Nothing Then json.devTools = New devTools

            Return json
        End Function

        Public Function Save() As Boolean
            Return Me.GetJson.SaveTo(defaultFile)
        End Function

    End Class
End Namespace