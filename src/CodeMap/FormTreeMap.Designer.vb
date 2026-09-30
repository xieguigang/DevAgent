<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTreeMap
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        DxScene3dCanvas1 = New Drawing.DirectX.DxScene3DCanvas()
        SuspendLayout()
        ' 
        ' DxScene3dCanvas1
        ' 
        DxScene3dCanvas1.AutoClear = False
        DxScene3dCanvas1.BackColor = Color.LightSkyBlue
        DxScene3dCanvas1.Dock = DockStyle.Fill
        DxScene3dCanvas1.Location = New Point(0, 0)
        DxScene3dCanvas1.Name = "DxScene3dCanvas1"
        DxScene3dCanvas1.Size = New Size(845, 629)
        DxScene3dCanvas1.TabIndex = 0
        ' 
        ' FormTreeMap
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(845, 629)
        Controls.Add(DxScene3dCanvas1)
        Name = "FormTreeMap"
        Text = "Form1"
        ResumeLayout(False)
    End Sub

    Friend WithEvents DxScene3dCanvas1 As Microsoft.VisualBasic.Drawing.DirectX.DxScene3DCanvas

End Class
