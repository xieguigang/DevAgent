Imports Galaxy.Workbench.DockDocument

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMsBuild
    Inherits ToolWindow

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormMsBuild))
        WebViewConsole1 = New Microsoft.VisualBasic.Windows.Forms.WebViewConsole()
        SuspendLayout()
        ' 
        ' WebViewConsole1
        ' 
        WebViewConsole1.Dock = DockStyle.Fill
        WebViewConsole1.Location = New Point(0, 0)
        WebViewConsole1.Name = "WebViewConsole1"
        WebViewConsole1.Size = New Size(800, 450)
        WebViewConsole1.TabIndex = 0
        ' 
        ' FormMsBuild
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(WebViewConsole1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormMsBuild"
        Text = "MSBuild"
        ResumeLayout(False)
    End Sub

    Friend WithEvents WebViewConsole1 As Microsoft.VisualBasic.Windows.Forms.WebViewConsole
End Class
