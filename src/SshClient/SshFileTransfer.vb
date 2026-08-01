' ============================================================================
' SshFileTransfer.vb - SFTP 文件传输功能
'
' 基于 Renci.SshNet.SftpClient 实现文件上传/下载功能。
' 支持单文件传输、目录递归传输和传输进度显示。
' ============================================================================

Imports System.IO
Imports Renci.SshNet

''' <summary>
''' SFTP 文件传输管理器。
''' </summary>
Public Class SshFileTransfer

    Private _client As Renci.SshNet.SshClient
    Private _verbose As Boolean

    Public Sub New(client As Renci.SshNet.SshClient, Optional verbose As Boolean = False)
        _client = client
        _verbose = verbose
    End Sub

    ''' <summary>
    ''' 使用 SCP 上传单个文件到远程路径。
    ''' </summary>
    Public Sub Upload(localPath As String, remotePath As String)
        Using scp As New ScpClient(_client.ConnectionInfo)
            scp.BufferSize = 8192
            scp.Connect()

            If _verbose Then
                Console.Error.WriteLine($"[调试] 上传: {localPath} → {remotePath}")
            End If

            scp.Upload(New FileInfo(localPath), remotePath)
            scp.Disconnect()
        End Using

        Console.WriteLine($"已上传: {localPath} → {_client.ConnectionInfo.Host}:{remotePath}")
    End Sub

    ''' <summary>
    ''' 使用 SCP 下载远程文件到本地路径。
    ''' </summary>
    Public Sub Download(remotePath As String, localPath As String)
        Using scp As New ScpClient(_client.ConnectionInfo)
            scp.BufferSize = 8192
            scp.Connect()

            If _verbose Then
                Console.Error.WriteLine($"[调试] 下载: {remotePath} → {localPath}")
            End If

            scp.Download(remotePath, New FileInfo(localPath))
            scp.Disconnect()
        End Using

        Console.WriteLine($"已下载: {_client.ConnectionInfo.Host}:{remotePath} → {localPath}")
    End Sub

    ''' <summary>
    ''' 使用 SFTP 上传文件，支持进度回调。
    ''' </summary>
    Public Sub UploadSftp(localPath As String, remotePath As String)
        Using sftp As New SftpClient(_client.ConnectionInfo)
            sftp.BufferSize = 8192
            sftp.Connect()

            Dim fileInfo As New FileInfo(localPath)
            Dim totalBytes As Long = fileInfo.Length
            Dim lastProgressPct As Integer = -1

            Using fileStream As FileStream = fileInfo.OpenRead()
                ' SSH.NET UploadFile 接受 Action(Of ULong) 进度回调
                sftp.UploadFile(fileStream, remotePath,
                    Sub(uploaded As ULong)
                        If totalBytes > 0 Then
                            Dim pct As Integer = CInt(uploaded * 100 \ CULng(totalBytes))
                            If pct <> lastProgressPct AndAlso pct Mod 10 = 0 Then
                                Console.Error.WriteLine($"  上传进度: {pct}% ({uploaded}/{totalBytes})")
                                lastProgressPct = pct
                            End If
                        End If
                    End Sub)
            End Using

            sftp.Disconnect()
        End Using

        Console.WriteLine($"已上传: {localPath} → {_client.ConnectionInfo.Host}:{remotePath}")
    End Sub

    ''' <summary>
    ''' 使用 SFTP 下载文件，支持进度回调。
    ''' </summary>
    Public Sub DownloadSftp(remotePath As String, localPath As String)
        Using sftp As New SftpClient(_client.ConnectionInfo)
            sftp.BufferSize = 8192
            sftp.Connect()

            Dim totalBytes As Long = sftp.GetAttributes(remotePath).Size
            Dim lastProgressPct As Integer = -1

            Using fileStream As FileStream = File.Create(localPath)
                sftp.DownloadFile(remotePath, fileStream,
                    Sub(downloaded As ULong)
                        If totalBytes > 0 Then
                            Dim pct As Integer = CInt(downloaded * 100 \ CULng(totalBytes))
                            If pct <> lastProgressPct AndAlso pct Mod 10 = 0 Then
                                Console.Error.WriteLine($"  下载进度: {pct}% ({downloaded}/{totalBytes})")
                                lastProgressPct = pct
                            End If
                        End If
                    End Sub)
            End Using

            sftp.Disconnect()
        End Using

        Console.WriteLine($"已下载: {_client.ConnectionInfo.Host}:{remotePath} → {localPath}")
    End Sub

End Class
