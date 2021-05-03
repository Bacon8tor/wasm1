<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.btnDisconnect = New System.Windows.Forms.Button()
        Me.richResponse = New System.Windows.Forms.RichTextBox()
        Me.btnConnect = New System.Windows.Forms.Button()
        Me.btnRequestData = New System.Windows.Forms.Button()
        Me.btnSendEvent = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnDisconnect
        '
        Me.btnDisconnect.Location = New System.Drawing.Point(663, 10)
        Me.btnDisconnect.Name = "btnDisconnect"
        Me.btnDisconnect.Size = New System.Drawing.Size(124, 54)
        Me.btnDisconnect.TabIndex = 5
        Me.btnDisconnect.Text = "Disconnect"
        Me.btnDisconnect.UseVisualStyleBackColor = True
        '
        'richResponse
        '
        Me.richResponse.Location = New System.Drawing.Point(13, 70)
        Me.richResponse.Name = "richResponse"
        Me.richResponse.Size = New System.Drawing.Size(774, 371)
        Me.richResponse.TabIndex = 4
        Me.richResponse.Text = ""
        '
        'btnConnect
        '
        Me.btnConnect.Location = New System.Drawing.Point(466, 10)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(124, 54)
        Me.btnConnect.TabIndex = 3
        Me.btnConnect.Text = "Connect"
        Me.btnConnect.UseVisualStyleBackColor = True
        '
        'btnRequestData
        '
        Me.btnRequestData.Location = New System.Drawing.Point(13, 10)
        Me.btnRequestData.Name = "btnRequestData"
        Me.btnRequestData.Size = New System.Drawing.Size(124, 54)
        Me.btnRequestData.TabIndex = 6
        Me.btnRequestData.Text = "Request SimVars"
        Me.btnRequestData.UseVisualStyleBackColor = True
        '
        'btnSendEvent
        '
        Me.btnSendEvent.Location = New System.Drawing.Point(189, 10)
        Me.btnSendEvent.Name = "btnSendEvent"
        Me.btnSendEvent.Size = New System.Drawing.Size(124, 54)
        Me.btnSendEvent.TabIndex = 7
        Me.btnSendEvent.Text = "Send Event"
        Me.btnSendEvent.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(799, 450)
        Me.Controls.Add(Me.btnSendEvent)
        Me.Controls.Add(Me.btnRequestData)
        Me.Controls.Add(Me.btnDisconnect)
        Me.Controls.Add(Me.richResponse)
        Me.Controls.Add(Me.btnConnect)
        Me.Name = "Form1"
        Me.Text = "VB Client"
        Me.ResumeLayout(False)

    End Sub

    Private WithEvents btnDisconnect As Button
    Private WithEvents richResponse As RichTextBox
    Private WithEvents btnConnect As Button
    Private WithEvents btnRequestData As Button
    Private WithEvents btnSendEvent As Button
End Class
