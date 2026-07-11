namespace PangYa_Suite_Tools
{
    partial class FrmOptions
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            groupGlobalSettings = new GroupBox();
            lblLanguage = new Label();
            cboLanguage = new ComboBox();
            groupRegister = new GroupBox();
            lblAdminWarning = new Label();
            chkRegisterFile = new CheckBox();
            chkShellContext = new CheckBox();
            btnCancel = new Button();
            btnOK = new Button();
            groupGlobalSettings.SuspendLayout();
            groupRegister.SuspendLayout();
            SuspendLayout();
            // 
            // groupGlobalSettings
            // 
            groupGlobalSettings.Controls.Add(lblLanguage);
            groupGlobalSettings.Controls.Add(cboLanguage);
            groupGlobalSettings.Location = new Point(12, 12);
            groupGlobalSettings.Name = "groupGlobalSettings";
            groupGlobalSettings.Size = new Size(360, 75);
            groupGlobalSettings.TabIndex = 0;
            groupGlobalSettings.TabStop = false;
            // 
            // lblLanguage
            // 
            lblLanguage.Location = new Point(15, 32);
            lblLanguage.Name = "lblLanguage";
            lblLanguage.Size = new Size(105, 23);
            lblLanguage.TabIndex = 0;
            lblLanguage.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cboLanguage
            // 
            cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLanguage.FormattingEnabled = true;
            cboLanguage.Location = new Point(126, 31);
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new Size(219, 23);
            cboLanguage.TabIndex = 1;
            cboLanguage.SelectedIndexChanged += cboLanguage_SelectedIndexChanged;
            // 
            // groupRegister
            // 
            groupRegister.Controls.Add(lblAdminWarning);
            groupRegister.Controls.Add(chkRegisterFile);
            groupRegister.Controls.Add(chkShellContext);
            groupRegister.Location = new Point(12, 96);
            groupRegister.Name = "groupRegister";
            groupRegister.Size = new Size(360, 135);
            groupRegister.TabIndex = 1;
            groupRegister.TabStop = false;
            // 
            // lblAdminWarning
            // 
            lblAdminWarning.ForeColor = Color.DarkRed;
            lblAdminWarning.Location = new Point(15, 23);
            lblAdminWarning.Name = "lblAdminWarning";
            lblAdminWarning.Size = new Size(330, 40);
            lblAdminWarning.TabIndex = 0;
            // 
            // chkRegisterFile
            // 
            chkRegisterFile.AutoSize = true;
            chkRegisterFile.Location = new Point(18, 72);
            chkRegisterFile.Name = "chkRegisterFile";
            chkRegisterFile.Size = new Size(206, 19);
            chkRegisterFile.TabIndex = 1;
            chkRegisterFile.UseVisualStyleBackColor = true;
            // 
            // chkShellContext
            // 
            chkShellContext.AutoSize = true;
            chkShellContext.Location = new Point(38, 97);
            chkShellContext.Name = "chkShellContext";
            chkShellContext.Size = new Size(271, 19);
            chkShellContext.TabIndex = 2;
            chkShellContext.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(216, 241);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 2;
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(297, 241);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 25);
            btnOK.TabIndex = 3;
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // FrmOptions
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(384, 278);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            Controls.Add(groupRegister);
            Controls.Add(groupGlobalSettings);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmOptions";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Options";
            groupGlobalSettings.ResumeLayout(false);
            groupRegister.ResumeLayout(false);
            groupRegister.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupGlobalSettings;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.ComboBox cboLanguage;
        private System.Windows.Forms.GroupBox groupRegister;
        private System.Windows.Forms.Label lblAdminWarning;
        private System.Windows.Forms.CheckBox chkRegisterFile;
        private System.Windows.Forms.CheckBox chkShellContext;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
    }
}
