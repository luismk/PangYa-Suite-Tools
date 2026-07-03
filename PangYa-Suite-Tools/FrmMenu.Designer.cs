namespace PangYa_Suite_Tools
{
    partial class FrmMenu
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnOpenPakMaker;
        private System.Windows.Forms.Button btnOpenUpdateList;
        private System.Windows.Forms.Button btnOpenIffManager;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblLanguage;
        private System.Windows.Forms.ToolStripComboBox cboLanguage;
        private System.Windows.Forms.Button btnOpenOptions;
        private System.Windows.Forms.Button btnOpenPakDiff;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnOpenPakMaker = new Button();
            btnOpenUpdateList = new Button();
            btnOpenIffManager = new Button();
            btnOpenPakDiff = new Button();
            btnOpenOptions = new Button();
            lblTitle = new Label();
            statusStrip1 = new StatusStrip();
            lblLanguage = new ToolStripStatusLabel();
            cboLanguage = new ToolStripComboBox();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btnOpenPakMaker
            // 
            btnOpenPakMaker.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenPakMaker.Location = new Point(42, 70);
            btnOpenPakMaker.Name = "btnOpenPakMaker";
            btnOpenPakMaker.Size = new Size(300, 50);
            btnOpenPakMaker.TabIndex = 1;
            btnOpenPakMaker.UseVisualStyleBackColor = true;
            btnOpenPakMaker.Click += btnOpenPakMaker_Click;
            // 
            // btnOpenUpdateList
            // 
            btnOpenUpdateList.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenUpdateList.Location = new Point(42, 135);
            btnOpenUpdateList.Name = "btnOpenUpdateList";
            btnOpenUpdateList.Size = new Size(300, 50);
            btnOpenUpdateList.TabIndex = 2;
            btnOpenUpdateList.UseVisualStyleBackColor = true;
            btnOpenUpdateList.Click += btnOpenUpdateList_Click;
            // 
            // btnOpenIffManager
            // 
            btnOpenIffManager.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenIffManager.Location = new Point(42, 200);
            btnOpenIffManager.Name = "btnOpenIffManager";
            btnOpenIffManager.Size = new Size(300, 50);
            btnOpenIffManager.TabIndex = 3;
            btnOpenIffManager.UseVisualStyleBackColor = true;
            btnOpenIffManager.Click += btnOpenIffManager_Click;
            // 
            // btnOpenPakDiff
            // 
            btnOpenPakDiff.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenPakDiff.Location = new Point(42, 265);
            btnOpenPakDiff.Name = "btnOpenPakDiff";
            btnOpenPakDiff.Size = new Size(300, 50);
            btnOpenPakDiff.TabIndex = 4;
            btnOpenPakDiff.Text = "🔍 Comparador PAK (Diff)";
            btnOpenPakDiff.UseVisualStyleBackColor = true;
            btnOpenPakDiff.Click += btnOpenPakDiff_Click;
            // 
            // btnOpenOptions
            // 
            btnOpenOptions.Location = new Point(103, 330);
            btnOpenOptions.Name = "btnOpenOptions";
            btnOpenOptions.Size = new Size(180, 35);
            btnOpenOptions.TabIndex = 5;
            btnOpenOptions.Text = "Options";
            btnOpenOptions.UseVisualStyleBackColor = true;
            btnOpenOptions.Click += btnOpenOptions_Click;
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 19);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(360, 30);
            lblTitle.TabIndex = 0;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblLanguage, cboLanguage });
            statusStrip1.Location = new Point(0, 385);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(402, 23);
            statusStrip1.TabIndex = 6;
            // 
            // lblLanguage
            // 
            lblLanguage.Name = "lblLanguage";
            lblLanguage.Size = new Size(47, 18);
            // 
            // cboLanguage
            // 
            cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new Size(120, 23);
            cboLanguage.SelectedIndexChanged += cboLanguage_SelectedIndexChanged;
            // 
            // FrmMenu
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(402, 408);
            Controls.Add(btnOpenOptions);
            Controls.Add(btnOpenPakDiff);
            Controls.Add(btnOpenIffManager);
            Controls.Add(btnOpenUpdateList);
            Controls.Add(btnOpenPakMaker);
            Controls.Add(lblTitle);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmMenu";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Pangya Studio - Menu Principal";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
