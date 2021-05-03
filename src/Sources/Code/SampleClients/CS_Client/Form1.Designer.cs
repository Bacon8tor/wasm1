
namespace CS_Client2
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnConnect = new System.Windows.Forms.Button();
            this.richResponse = new System.Windows.Forms.RichTextBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnSendEvent = new System.Windows.Forms.Button();
            this.btnRequestData = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(465, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(124, 54);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // richResponse
            // 
            this.richResponse.Location = new System.Drawing.Point(12, 72);
            this.richResponse.Name = "richResponse";
            this.richResponse.Size = new System.Drawing.Size(774, 371);
            this.richResponse.TabIndex = 1;
            this.richResponse.Text = "";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(662, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(124, 54);
            this.btnDisconnect.TabIndex = 2;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // btnSendEvent
            // 
            this.btnSendEvent.Location = new System.Drawing.Point(188, 12);
            this.btnSendEvent.Name = "btnSendEvent";
            this.btnSendEvent.Size = new System.Drawing.Size(124, 54);
            this.btnSendEvent.TabIndex = 9;
            this.btnSendEvent.Text = "Send Event";
            this.btnSendEvent.UseVisualStyleBackColor = true;
            this.btnSendEvent.Click += new System.EventHandler(this.btnSendEvent_Click);
            // 
            // btnRequestData
            // 
            this.btnRequestData.Location = new System.Drawing.Point(12, 12);
            this.btnRequestData.Name = "btnRequestData";
            this.btnRequestData.Size = new System.Drawing.Size(124, 54);
            this.btnRequestData.TabIndex = 8;
            this.btnRequestData.Text = "Request SimVars";
            this.btnRequestData.UseVisualStyleBackColor = true;
            this.btnRequestData.Click += new System.EventHandler(this.btnRequestData_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnSendEvent);
            this.Controls.Add(this.btnRequestData);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.richResponse);
            this.Controls.Add(this.btnConnect);
            this.Name = "Form1";
            this.Text = "CS Client";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed_1);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.RichTextBox richResponse;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnSendEvent;
        private System.Windows.Forms.Button btnRequestData;
    }
}

