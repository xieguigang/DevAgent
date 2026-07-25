Imports System.Net.Sockets
Imports Flute.Http.Configurations
Imports Flute.Http.Core
Imports Flute.Http.Core.Message
Imports Microsoft.VisualBasic.Serialization.JSON

Public Class Services : Inherits HttpServer

    Sub New(port As Integer, config As Configuration)
        Call MyBase.New(port, -1, config)
    End Sub

    Private Function parseJSON(json_str As String) As Dictionary(Of String, Object)
        Static knows As Type() = {
            GetType(Dictionary(Of String, Object)),
            GetType(String()),
            GetType(Double()),
            GetType(Double),
            GetType(String),
            GetType(Dictionary(Of String, String)),
            GetType(Dictionary(Of String, String()))
        }

        Return json_str.LoadJSON(Of Dictionary(Of String, Object))(knownTypes:=knows)
    End Function

    Public Overrides Sub handleGETRequest(p As HttpProcessor)
        Dim [get] As New HttpRequest(p)
        Dim response As New HttpResponse(p.outputStream, AddressOf p.writeFailure, _settings)

    End Sub

    Public Overrides Sub handlePOSTRequest(p As HttpProcessor, inputData As String)
        Dim post As New HttpPOSTRequest(p, inputData, AddressOf parseJSON)
        Dim response As New HttpResponse(p.outputStream, AddressOf p.writeFailure, _settings)

    End Sub

    Public Overrides Sub handleOtherMethod(p As HttpProcessor)
        Dim req As New HttpRequest(p)
        Dim response As New HttpResponse(p.outputStream, AddressOf p.writeFailure, _settings)

        If req.HTTPMethod = "OPTIONS" AndAlso req.URL.path.Trim("/"c) = "ctrl/kill" Then
            Call response.WriteHTML("OK!")
            Call Me.Shutdown()
        Else
            Call response.WriteHTML("OK!")
        End If
    End Sub

    Protected Overrides Function getHttpProcessor(client As TcpClient, bufferSize As Integer) As HttpProcessor
        Return New HttpProcessor(client, Me, MAX_POST_SIZE:=bufferSize * 4, _settings)
    End Function
End Class
