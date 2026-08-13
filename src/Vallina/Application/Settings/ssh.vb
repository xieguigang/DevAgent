Imports System.Security.Cryptography
Imports System.Text

Namespace Settings

    ''' <summary>
    ''' A single SSH connection configuration.
    ''' The password is stored on disk only as a DPAPI-encrypted (CurrentUser) base64 string
    ''' via <see cref="passwordProtected"/>. The plaintext <see cref="Password"/> is decrypted
    ''' on read and encrypted on write so the JSON file never contains a plaintext password.
    ''' </summary>
    Public Class SshConnection

        Public Property host As String = ""
        Public Property port As Integer = 22
        Public Property user As String = ""
        ''' <summary>
        ''' Group label used to build the two-level TreeView. Empty falls back to "Default".
        ''' </summary>
        Public Property group As String = "Default"

        ''' <summary>
        ''' DPAPI encrypted password (base64). This is the only password field that is serialized to disk.
        ''' </summary>
        Public Property passwordProtected As String = ""

        ''' <summary>
        ''' Plaintext password accessor. Reading decrypts <see cref="passwordProtected"/>,
        ''' writing encrypts and stores the result in <see cref="passwordProtected"/>.
        ''' </summary>
        Public Property Password As String
            Get
                Return SshCrypto.Unprotect(passwordProtected)
            End Get
            Set(value As String)
                passwordProtected = SshCrypto.Protect(value)
            End Set
        End Property

        ''' <summary>
        ''' Display name used as the TreeView leaf node text, e.g. user@host:port.
        ''' </summary>
        Public ReadOnly Property DisplayName As String
            Get
                Return $"{user}@{host}:{port}"
            End Get
        End Property

    End Class

    ''' <summary>
    ''' Container for all SSH server connections, persisted under <see cref="ConfigJSON.sshServers"/>.
    ''' </summary>
    Public Class SshServerConfig

        Public Property connections As New List(Of SshConnection)

        Public Function Find(predicate As Func(Of SshConnection, Boolean)) As SshConnection
            Return connections.Where(predicate).FirstOrDefault()
        End Function

        Public Sub Remove(conn As SshConnection)
            If conn IsNot Nothing Then
                Call connections.Remove(conn)
            End If
        End Sub

        ''' <summary>
        ''' Normalize the group label so that empty groups are merged into "Default".
        ''' </summary>
        Public Shared Function NormalizeGroup(group As String) As String
            If String.IsNullOrWhiteSpace(group) Then
                Return "Default"
            End If
            Return group.Trim()
        End Function

    End Class

    ''' <summary>
    ''' Helper for protecting/unprotecting the SSH password with Windows DPAPI (CurrentUser scope).
    ''' </summary>
    Friend Module SshCrypto

        Private ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("VallinaSshConfig")

        ''' <summary>
        ''' Encrypt a plaintext string and return its base64 representation.
        ''' Returns empty string when input is null/empty.
        ''' </summary>
        Public Function Protect(plainText As String) As String
            If String.IsNullOrEmpty(plainText) Then
                Return ""
            End If

            Dim data As Byte() = Encoding.UTF8.GetBytes(plainText)
            Dim [protected] As Byte() = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser)
            Return Convert.ToBase64String([protected])
        End Function

        ''' <summary>
        ''' Decrypt a base64 DPAPI blob back to the plaintext string.
        ''' Returns empty string on failure (e.g. blob was produced by another user/machine).
        ''' </summary>
        Public Function Unprotect(protectedBase64 As String) As String
            If String.IsNullOrEmpty(protectedBase64) Then
                Return ""
            End If

            Try
                Dim data As Byte() = Convert.FromBase64String(protectedBase64)
                Dim unprotectedData As Byte() = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser)
                Return Encoding.UTF8.GetString(unprotectedData)
            Catch
                ' Corrupted or inaccessible blob: fail safe with empty password.
                Return ""
            End Try
        End Function

    End Module

End Namespace
