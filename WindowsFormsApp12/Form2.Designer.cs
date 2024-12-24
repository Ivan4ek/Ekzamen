namespace WindowsFormsApp12
{
    partial class Form2
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
            this.btnLog = new System.Windows.Forms.Button();
            this.btnReg = new System.Windows.Forms.Button();
            this.tbLog = new System.Windows.Forms.TextBox();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.chbUsers = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnLog
            // 
            this.btnLog.Location = new System.Drawing.Point(83, 157);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(121, 41);
            this.btnLog.TabIndex = 0;
            this.btnLog.Text = "Log In";
            this.btnLog.UseVisualStyleBackColor = true;
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            // 
            // btnReg
            // 
            this.btnReg.Location = new System.Drawing.Point(224, 157);
            this.btnReg.Name = "btnReg";
            this.btnReg.Size = new System.Drawing.Size(121, 41);
            this.btnReg.TabIndex = 1;
            this.btnReg.Text = "Registration";
            this.btnReg.UseVisualStyleBackColor = true;
            this.btnReg.Click += new System.EventHandler(this.btnReg_Click);
            // 
            // tbLog
            // 
            this.tbLog.Location = new System.Drawing.Point(83, 80);
            this.tbLog.Name = "tbLog";
            this.tbLog.Size = new System.Drawing.Size(262, 20);
            this.tbLog.TabIndex = 2;
            // 
            // tbPass
            // 
            this.tbPass.Location = new System.Drawing.Point(83, 121);
            this.tbPass.Name = "tbPass";
            this.tbPass.Size = new System.Drawing.Size(262, 20);
            this.tbPass.TabIndex = 3;
            // 
            // chbUsers
            // 
            this.chbUsers.AutoSize = true;
            this.chbUsers.Location = new System.Drawing.Point(83, 204);
            this.chbUsers.Name = "chbUsers";
            this.chbUsers.Size = new System.Drawing.Size(174, 18);
            this.chbUsers.TabIndex = 4;
            this.chbUsers.Text = "Отобразить таблицу данных";
            this.chbUsers.UseCompatibleTextRendering = true;
            this.chbUsers.UseVisualStyleBackColor = true;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 244);
            this.Controls.Add(this.chbUsers);
            this.Controls.Add(this.tbPass);
            this.Controls.Add(this.tbLog);
            this.Controls.Add(this.btnReg);
            this.Controls.Add(this.btnLog);
            this.Name = "Form2";
            this.Text = "Form2";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Button btnReg;
        private System.Windows.Forms.TextBox tbLog;
        private System.Windows.Forms.TextBox tbPass;
        private System.Windows.Forms.CheckBox chbUsers;
    }
}