Imports System.Threading
Imports Microsoft.VisualBasic.ApplicationServices

Namespace My
    ' The following events are available for MyApplication:
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed. This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active.
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.

    ' **NEW** ApplyApplicationDefaults: Raised when the application queries default values to be set for the application.

    ' Example:
    ' Private Sub MyApplication_ApplyApplicationDefaults(sender As Object, e As ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
    '
    '   ' Setting the application-wide default Font:
    '   e.Font = New Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular)
    '
    '   ' Setting the HighDpiMode for the Application:
    '   e.HighDpiMode = HighDpiMode.PerMonitorV2
    '
    '   ' If a splash dialog is used, this sets the minimum display time:
    '   e.MinimumSplashScreenDisplayTime = 4000
    ' End Sub

    Partial Friend Class MyApplication

        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            ' a failure inside a paint handler must stay visible on the console
            ' instead of silently freezing the explorer window
            AddHandler System.Windows.Forms.Application.ThreadException, AddressOf HandleThreadException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf HandlingUnhandledException
        End Sub

        Private Sub HandlingUnhandledException(sender As Object, e As System.UnhandledExceptionEventArgs)
            Dim ex As Exception = TryCast(e.ExceptionObject, Exception)

            Console.Error.WriteLine($"[CodeMap] fatal: {If(ex Is Nothing, e.ExceptionObject.ToString(), ex.Message)}")

            If ex IsNot Nothing Then
                Console.Error.WriteLine(ex.StackTrace)
            End If
        End Sub

        Private Sub HandleThreadException(sender As Object, e As ThreadExceptionEventArgs)
            Console.Error.WriteLine($"[CodeMap] ui error: {e.Exception.Message}")
            Console.Error.WriteLine(e.Exception.StackTrace)
        End Sub
    End Class
End Namespace
