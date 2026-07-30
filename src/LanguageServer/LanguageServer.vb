Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.CommandLine.InteropService
Imports Microsoft.VisualBasic.ApplicationServices

' Microsoft VisualBasic CommandLine Code AutoGenerator
' assembly: ..\bin\LanguageServer.dll

' 
'  // 
'  // 
'  // 
'  // VERSION:   1.0.0.0
'  // ASSEMBLY:  LanguageServer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
'  // COPYRIGHT: 
'  // GUID:      
'  // BUILT:     1/1/2000 12:00:00 AM
'  // 
' 
' 
'  < LanguageServer.Program >
' 
' 
' SYNOPSIS
' LanguageServer command [/argument argument-value...] [/@set environment-variable=value...]
' 
' All of the command that available in this program has been list below:
' 
'  --listen:     
' 
' 
' ----------------------------------------------------------------------------------------------------
' 
'    1. You can using "LanguageServer ??<commandName>" for getting more details command help.
'    2. Using command "LanguageServer /CLI.dev [---echo]" for CLI pipeline development.
'    3. Using command "LanguageServer /i" for enter interactive console mode.
'    4. Using command "LanguageServer /STACK:xxMB" for adjust the application stack size, example as '/STACK:64MB'.

Namespace CLI


''' <summary>
''' LanguageServer.Program
''' </summary>
'''
Public Class LanguageServer : Inherits InteropService

    Public Const App$ = "LanguageServer.exe"

    Sub New(App$)
        Call MyBase.New(app:=App$)
    End Sub
        
''' <summary>
''' Create an internal CLI pipeline invoker from a given environment path. 
''' </summary>
''' <param name="directory">A directory path that contains the target application</param>
''' <returns></returns>
     <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function FromEnvironment(directory As String) As LanguageServer
          Return New LanguageServer(App:=directory & "/" & LanguageServer.App)
     End Function

''' <summary>
''' ```bash
''' --listen [--port=8015]
''' ```
''' </summary>
'''

Public Function Run(Optional port As String = "") As Integer
Dim cli = GetRunCommandLine(port:=port, internal_pipelineMode:=True)
    Dim proc As IIORedirectAbstract = RunDotNetApp(cli)
    Return proc.Run()
End Function
Public Function GetRunCommandLine(Optional port As String = "", Optional internal_pipelineMode As Boolean = True) As String
    Dim CLI As New StringBuilder("--listen")
    Call CLI.Append(" ")
    If Not port.StringEmpty Then
            Call CLI.Append("--port " & """" & port & """ ")
    End If
     Call CLI.Append($"/@set internal_pipeline={internal_pipelineMode.ToString.ToUpper()} ")


Return CLI.ToString()
End Function
End Class
End Namespace

