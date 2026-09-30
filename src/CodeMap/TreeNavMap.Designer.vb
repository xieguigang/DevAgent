<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TreeNavMap
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
        Canvas = New Drawing.DirectX.DxScene3DCanvas()
        SuspendLayout()
        ' 
        ' Canvas
        ' 
        Canvas.AutoClear = False
        Canvas.BackColor = Color.White
        Canvas.Dock = DockStyle.Fill
        Canvas.Location = New Point(0, 0)
        Canvas.Name = "Canvas"
        Canvas.Size = New Size(1438, 901)
        Canvas.TabIndex = 1
        ' 
        ' TreeNavMap
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(Canvas)
        Name = "TreeNavMap"
        Size = New Size(1438, 901)
        ResumeLayout(False)
    End Sub

    Friend WithEvents Canvas As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas

End Class
