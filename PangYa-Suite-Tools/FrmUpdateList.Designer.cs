using System.Xml.Linq;
namespace PangYa_Suite_Tools
{
    partial class FrmUpdateList
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tabMain = new TabControl();
            tabDecrypt = new TabPage();
            viewerToolbar = new ToolStrip();
            btnShowRawXml = new ToolStripButton();
            txtViewerFilePath = new TextBox();
            btnBrowseViewer = new Button();
            //pnlCryptoDrop = new Panel();
            lblDropHint = new Label();
            lstUpdateFiles = new ListView();
            colFileName = new ColumnHeader();
            colDirectory = new ColumnHeader();
            colFileSize = new ColumnHeader();
            colCrc = new ColumnHeader();
            colDate = new ColumnHeader();
            colTime = new ColumnHeader();
            colPackageName = new ColumnHeader();
            colPackageSize = new ColumnHeader();
            txtXmlViewer = new TextBox();
            tabGenerator = new TabPage();
            generatorToolbar = new ToolStrip();
            grpConfig = new GroupBox();
            lblPangyaPath = new Label();
            txtPangyaPath = new TextBox();
            btnBrowsePangya = new Button();
            lblUpdatePath = new Label();
            txtUpdatePath = new TextBox();
            btnBrowseUpdate = new Button();
            lblExistingList = new Label();
            txtExistingList = new TextBox();
            btnBrowseExisting = new Button();
            lblFileKey = new Label();
            cboFileKey = new ComboBox();
            lblPatchVersion = new Label();
            txtPatchVersion = new TextBox();
            lblUpdateListVer = new Label();
            txtUpdateListVer = new TextBox();
            lblClientPatchNum = new Label();
            txtClientPatchNum = new TextBox();
            btnGenerateNow = new ToolStripButton();
            btnToggleWatch = new ToolStripButton();
            lblWatchStatus = new ToolStripLabel();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            lblLog = new Label();
            txtLog = new TextBox();
            statusStrip1 = new StatusStrip();
            // lblLanguage = new ToolStripStatusLabel();
            // cboLanguage = new ToolStripComboBox();
            lblUpdateListSummary = new ToolStripStatusLabel();

            tabMain.SuspendLayout();
            tabDecrypt.SuspendLayout();
            viewerToolbar.SuspendLayout();
            //pnlCryptoDrop.SuspendLayout();
            tabGenerator.SuspendLayout();
            generatorToolbar.SuspendLayout();
            grpConfig.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            tabMain.Controls.Add(tabDecrypt);
            tabMain.Controls.Add(tabGenerator);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(784, 572);
            tabMain.TabIndex = 0;
            tabDecrypt.Controls.Add(viewerToolbar);
            tabDecrypt.Controls.Add(txtViewerFilePath);
            tabDecrypt.Controls.Add(btnBrowseViewer);
            //tabDecrypt.Controls.Add(pnlCryptoDrop);
            tabDecrypt.Controls.Add(lstUpdateFiles);
            tabDecrypt.Controls.Add(txtXmlViewer);
            tabDecrypt.Location = new Point(4, 24);
            tabDecrypt.Name = "tabDecrypt";
            tabDecrypt.Padding = new Padding(3);
            tabDecrypt.Size = new Size(776, 544);
            tabDecrypt.TabIndex = 0;
            tabDecrypt.UseVisualStyleBackColor = true;

            // viewerToolbar
            viewerToolbar.AutoSize = false;
            viewerToolbar.GripStyle = ToolStripGripStyle.Hidden;
            viewerToolbar.ImageScalingSize = new Size(32, 32);
            viewerToolbar.Items.AddRange(new ToolStripItem[] { btnShowRawXml });
            viewerToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            viewerToolbar.Location = new Point(3, 3);
            viewerToolbar.Name = "viewerToolbar";
            viewerToolbar.Padding = new Padding(3, 3, 3, 4);
            viewerToolbar.Size = new Size(770, 69);
            viewerToolbar.TabIndex = 0;

            // btnShowRawXml
            btnShowRawXml.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnShowRawXml.Name = "btnShowRawXml";
            btnShowRawXml.Size = new Size(92, 62);
            btnShowRawXml.TextImageRelation = TextImageRelation.ImageAboveText;
            btnShowRawXml.Click += btnShowRawXml_Click;

            // txtViewerFilePath
            txtViewerFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtViewerFilePath.Location = new Point(8, 80);
            txtViewerFilePath.Name = "txtViewerFilePath";
            txtViewerFilePath.ReadOnly = true;
            txtViewerFilePath.Size = new Size(638, 23);
            txtViewerFilePath.TabIndex = 1;
            // 
            // btnBrowseViewer
            btnBrowseViewer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseViewer.BackColor = Color.FromArgb(0, 122, 204);
            btnBrowseViewer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBrowseViewer.ForeColor = Color.White;
            btnBrowseViewer.Location = new Point(656, 78);
            btnBrowseViewer.Name = "btnBrowseViewer";
            btnBrowseViewer.Size = new Size(104, 28);
            btnBrowseViewer.TabIndex = 2;
            btnBrowseViewer.UseVisualStyleBackColor = false;
            btnBrowseViewer.Click += btnBrowseViewer_Click;

            // pnlCryptoDrop
            // pnlCryptoDrop.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // pnlCryptoDrop.BackColor = Color.GhostWhite;
            // pnlCryptoDrop.BorderStyle = BorderStyle.FixedSingle;
            // pnlCryptoDrop.Controls.Add(lblDropHint);
            // pnlCryptoDrop.Location = new Point(8, 116);
            // pnlCryptoDrop.Name = "pnlCryptoDrop";
            // pnlCryptoDrop.Size = new Size(760, 100);
            // pnlCryptoDrop.TabIndex = 3;

            // lblDropHint
            lblDropHint.Dock = DockStyle.Fill;
            lblDropHint.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            lblDropHint.ForeColor = Color.RoyalBlue;
            lblDropHint.Location = new Point(0, 0);
            lblDropHint.Name = "lblDropHint";
            lblDropHint.Size = new Size(758, 98);
            lblDropHint.TabIndex = 0;
            lblDropHint.TextAlign = ContentAlignment.MiddleCenter;

            // lstUpdateFiles
            lstUpdateFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstUpdateFiles.Columns.AddRange(new ColumnHeader[] { colFileName, colDirectory, colFileSize, colCrc, colDate, colTime, colPackageName, colPackageSize });
            lstUpdateFiles.FullRowSelect = true;
            lstUpdateFiles.GridLines = true;
            lstUpdateFiles.Location = new Point(8, 112);
            lstUpdateFiles.Name = "lstUpdateFiles";
            lstUpdateFiles.Size = new Size(760, 415);
            lstUpdateFiles.TabIndex = 4;
            lstUpdateFiles.UseCompatibleStateImageBehavior = false;
            lstUpdateFiles.View = View.Details;

            // txtXmlViewer
            txtXmlViewer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtXmlViewer.BackColor = Color.White;
            txtXmlViewer.Font = new Font("Consolas", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtXmlViewer.ForeColor = Color.DarkBlue;
            txtXmlViewer.Location = new Point(8, 226);
            txtXmlViewer.Multiline = true;
            txtXmlViewer.Name = "txtXmlViewer";
            txtXmlViewer.ReadOnly = true;
            txtXmlViewer.ScrollBars = ScrollBars.Both;
            txtXmlViewer.Size = new Size(760, 310);
            txtXmlViewer.TabIndex = 5;
            txtXmlViewer.Visible = false;
            tabGenerator.Controls.Add(generatorToolbar);
            tabGenerator.Controls.Add(grpConfig);
            tabGenerator.Controls.Add(progressBar);
            tabGenerator.Controls.Add(lblStatus);
            tabGenerator.Controls.Add(lblLog);
            tabGenerator.Controls.Add(txtLog);
            tabGenerator.Location = new Point(4, 24);
            tabGenerator.Name = "tabGenerator";
            tabGenerator.Padding = new Padding(3);
            tabGenerator.Size = new Size(776, 544);
            tabGenerator.TabIndex = 1;
            tabGenerator.UseVisualStyleBackColor = true;
            generatorToolbar.AutoSize = false;
            generatorToolbar.GripStyle = ToolStripGripStyle.Hidden;
            generatorToolbar.ImageScalingSize = new Size(32, 32);
            generatorToolbar.Items.AddRange(new ToolStripItem[] { btnGenerateNow, btnToggleWatch, lblWatchStatus });
            generatorToolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            generatorToolbar.Location = new Point(3, 3);
            generatorToolbar.Name = "generatorToolbar";
            generatorToolbar.Padding = new Padding(3, 3, 3, 4);
            generatorToolbar.Size = new Size(770, 69);
            generatorToolbar.TabIndex = 0;
            grpConfig.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpConfig.Controls.Add(lblPangyaPath);
            grpConfig.Controls.Add(txtPangyaPath);
            grpConfig.Controls.Add(btnBrowsePangya);
            grpConfig.Controls.Add(lblUpdatePath);
            grpConfig.Controls.Add(txtUpdatePath);
            grpConfig.Controls.Add(btnBrowseUpdate);
            grpConfig.Controls.Add(lblExistingList);
            grpConfig.Controls.Add(txtExistingList);
            grpConfig.Controls.Add(btnBrowseExisting);
            grpConfig.Controls.Add(lblFileKey);
            grpConfig.Controls.Add(cboFileKey);
            grpConfig.Controls.Add(lblPatchVersion);
            grpConfig.Controls.Add(txtPatchVersion);
            grpConfig.Controls.Add(lblUpdateListVer);
            grpConfig.Controls.Add(txtUpdateListVer);
            grpConfig.Controls.Add(lblClientPatchNum);
            grpConfig.Controls.Add(txtClientPatchNum);
            grpConfig.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpConfig.Location = new Point(8, 78);
            grpConfig.Name = "grpConfig";
            grpConfig.Size = new Size(760, 235);   // +50 pra nova linha
            grpConfig.TabIndex = 1;
            grpConfig.TabStop = false;
            // 
            // txtClientPatchNum
            // 
            txtClientPatchNum.Font = new Font("Segoe UI", 9F);
            txtClientPatchNum.Location = new Point(570, 149);
            txtClientPatchNum.Name = "txtClientPatchNum";
            txtClientPatchNum.Size = new Size(174, 23);
            txtClientPatchNum.TabIndex = 13;
            // 
            // lblClientPatchNum
            // 
            lblClientPatchNum.AutoSize = true;
            lblClientPatchNum.Font = new Font("Segoe UI", 9F);
            lblClientPatchNum.Location = new Point(570, 131);
            lblClientPatchNum.Name = "lblClientPatchNum";
            lblClientPatchNum.Size = new Size(87, 15);
            lblClientPatchNum.TabIndex = 12;
            // 
            // txtUpdateListVer
            // 
            txtUpdateListVer.Font = new Font("Segoe UI", 9F);
            txtUpdateListVer.Location = new Point(380, 149);
            txtUpdateListVer.Name = "txtUpdateListVer";
            txtUpdateListVer.Size = new Size(174, 23);
            txtUpdateListVer.TabIndex = 11;
            // 
            // lblUpdateListVer
            // 
            lblUpdateListVer.AutoSize = true;
            lblUpdateListVer.Font = new Font("Segoe UI", 9F);
            lblUpdateListVer.Location = new Point(380, 131);
            lblUpdateListVer.Name = "lblUpdateListVer";
            lblUpdateListVer.Size = new Size(107, 15);
            lblUpdateListVer.TabIndex = 10;
            // 
            // txtPatchVersion
            // 
            txtPatchVersion.Font = new Font("Segoe UI", 9F);
            txtPatchVersion.Location = new Point(190, 149);
            txtPatchVersion.Name = "txtPatchVersion";
            txtPatchVersion.Size = new Size(174, 23);
            txtPatchVersion.TabIndex = 9;
            // 
            // lblPatchVersion
            // 
            lblPatchVersion.AutoSize = true;
            lblPatchVersion.Font = new Font("Segoe UI", 9F);
            lblPatchVersion.Location = new Point(190, 131);
            lblPatchVersion.Name = "lblPatchVersion";
            lblPatchVersion.Size = new Size(94, 15);
            lblPatchVersion.TabIndex = 8;
            // 
            // cboFileKey
            // 
            cboFileKey.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFileKey.Font = new Font("Segoe UI", 9F);
            cboFileKey.FormattingEnabled = true;
            cboFileKey.Location = new Point(15, 149);
            cboFileKey.Name = "cboFileKey";
            cboFileKey.Size = new Size(160, 23);
            cboFileKey.TabIndex = 7;
            // 
            // lblFileKey
            // 
            lblFileKey.AutoSize = true;
            lblFileKey.Font = new Font("Segoe UI", 9F);
            lblFileKey.Location = new Point(15, 131);
            lblFileKey.Name = "lblFileKey";
            lblFileKey.Size = new Size(90, 15);
            lblFileKey.TabIndex = 6;
            // 
            // btnBrowseUpdate
            // 
            btnBrowseUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseUpdate.Font = new Font("Segoe UI", 9F);
            btnBrowseUpdate.Location = new Point(659, 95);
            btnBrowseUpdate.Name = "btnBrowseUpdate";
            btnBrowseUpdate.Size = new Size(85, 25);
            btnBrowseUpdate.TabIndex = 5;
            btnBrowseUpdate.UseVisualStyleBackColor = true;
            btnBrowseUpdate.Click += btnBrowseUpdate_Click;
            // 
            // txtUpdatePath
            // 
            txtUpdatePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUpdatePath.Font = new Font("Segoe UI", 9F);
            txtUpdatePath.Location = new Point(15, 96);
            txtUpdatePath.Name = "txtUpdatePath";
            txtUpdatePath.Size = new Size(638, 23);
            txtUpdatePath.TabIndex = 4;
            // 
            // lblUpdatePath
            // 
            lblUpdatePath.AutoSize = true;
            lblUpdatePath.Font = new Font("Segoe UI", 9F);
            lblUpdatePath.Location = new Point(15, 78);
            lblUpdatePath.Name = "lblUpdatePath";
            lblUpdatePath.Size = new Size(165, 15);
            lblUpdatePath.TabIndex = 3;
            // 
            // btnBrowsePangya
            // 
            btnBrowsePangya.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowsePangya.Font = new Font("Segoe UI", 9F);
            btnBrowsePangya.Location = new Point(659, 45);
            btnBrowsePangya.Name = "btnBrowsePangya";
            btnBrowsePangya.Size = new Size(85, 25);
            btnBrowsePangya.TabIndex = 2;
            btnBrowsePangya.UseVisualStyleBackColor = true;
            btnBrowsePangya.Click += btnBrowsePangya_Click;
            // 
            // txtPangyaPath
            // 
            txtPangyaPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPangyaPath.Font = new Font("Segoe UI", 9F);
            txtPangyaPath.Location = new Point(15, 46);
            txtPangyaPath.Name = "txtPangyaPath";
            txtPangyaPath.Size = new Size(638, 23);
            txtPangyaPath.TabIndex = 1;
            // 
            // lblPangyaPath
            // 
            lblPangyaPath.AutoSize = true;
            lblPangyaPath.Font = new Font("Segoe UI", 9F);
            lblPangyaPath.Location = new Point(15, 28);
            lblPangyaPath.Name = "lblPangyaPath";
            lblPangyaPath.Size = new Size(148, 15);
            lblPangyaPath.TabIndex = 0;
            // 
            // btnGenerateNow
            // 
            btnGenerateNow.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnGenerateNow.Name = "btnGenerateNow";
            btnGenerateNow.Size = new Size(92, 62);
            btnGenerateNow.TextImageRelation = TextImageRelation.ImageAboveText;
            btnGenerateNow.Click += btnGenerateNow_Click;
            // 
            // btnToggleWatch
            // 
            btnToggleWatch.BackColor = Color.LightGreen;
            btnToggleWatch.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnToggleWatch.Name = "btnToggleWatch";
            btnToggleWatch.Size = new Size(92, 62);
            btnToggleWatch.TextImageRelation = TextImageRelation.ImageAboveText;
            btnToggleWatch.Click += btnToggleWatch_Click;
            lblWatchStatus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblWatchStatus.ForeColor = Color.DimGray;
            lblWatchStatus.Margin = new Padding(12, 1, 0, 2);
            lblWatchStatus.Name = "lblWatchStatus";
            lblWatchStatus.Size = new Size(77, 62);
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(8, 325);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(680, 18);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.TabIndex = 4;
            progressBar.Visible = false;

            lblStatus.AutoSize = false;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatus.Font = new Font("Segoe UI", 8.5F);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Location = new Point(694, 325);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(74, 18);
            lblStatus.TabIndex = 5;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            lblLog.AutoSize = true;
            lblLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLog.Location = new Point(8, 351);
            lblLog.Name = "lblLog";
            lblLog.Size = new Size(142, 15);
            lblLog.TabIndex = 6;
            lblLog.Text = "Log / Terminal:";

            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.Black;
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.ForeColor = Color.Cyan;
            txtLog.Location = new Point(8, 369);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(760, 208);
            txtLog.TabIndex = 4;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblUpdateListSummary });
            statusStrip1.Location = new Point(0, 572);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(784, 23);
            statusStrip1.TabIndex = 1;

            lblUpdateListSummary.Name = "lblUpdateListSummary";
            lblUpdateListSummary.Spring = true;
            lblUpdateListSummary.TextAlign = ContentAlignment.MiddleLeft;

            // lblLanguage.Name = "lblLanguage";
            // lblLanguage.Size = new Size(47, 18);
            // 
            // cboLanguage
            // 
            // cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            // cboLanguage.Name = "cboLanguage";
            // cboLanguage.Size = new Size(120, 23);
            // cboLanguage.SelectedIndexChanged += cboLanguage_SelectedIndexChanged;

            // ─────────────────────────────────────────────────────────────────
            // FrmUpdateList
            // ─────────────────────────────────────────────────────────────────
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 595); 
            Controls.Add(tabMain);
            Controls.Add(statusStrip1);
            MinimumSize = new Size(800, 634);
            Name = "FrmUpdateList";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UpdateList Manager";

            tabMain.ResumeLayout(false);
            tabDecrypt.ResumeLayout(false);
            tabDecrypt.PerformLayout();
            viewerToolbar.ResumeLayout(false);
            viewerToolbar.PerformLayout();
            //pnlCryptoDrop.ResumeLayout(false);
            tabGenerator.ResumeLayout(false);
            tabGenerator.PerformLayout();
            generatorToolbar.ResumeLayout(false);
            generatorToolbar.PerformLayout();
            grpConfig.ResumeLayout(false);
            grpConfig.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ── Controles — Aba 1 ────────────────────────────────────────────────
        private TabControl tabMain;
        private TabPage tabDecrypt;
        private ToolStrip viewerToolbar;
        private ToolStripButton btnShowRawXml;
        private TextBox txtViewerFilePath;
        private Button btnBrowseViewer;
        //private Panel pnlCryptoDrop;
        private Label lblDropHint;
        private ListView lstUpdateFiles;
        private ColumnHeader colFileName;
        private ColumnHeader colDirectory;
        private ColumnHeader colFileSize;
        private ColumnHeader colCrc;
        private ColumnHeader colDate;
        private ColumnHeader colTime;
        private ColumnHeader colPackageName;
        private ColumnHeader colPackageSize;
        private TextBox txtXmlViewer;

        // ── Controles — Aba 2 ────────────────────────────────────────────────
        private TabPage tabGenerator;
        private ToolStrip generatorToolbar;
        private GroupBox grpConfig;

        // linha 1: Pangya
        private Label lblPangyaPath;
        private TextBox txtPangyaPath;
        private Button btnBrowsePangya;

        // linha 2: WebServer
        private Label lblUpdatePath;
        private TextBox txtUpdatePath;
        private Button btnBrowseUpdate;

        // linha 3: Updatelist existente (NOVO)
        private Label lblExistingList;
        private TextBox txtExistingList;
        private Button btnBrowseExisting;

        // linha 4: Região + versões
        private Label lblFileKey;
        private ComboBox cboFileKey;
        private Label lblPatchVersion;
        private TextBox txtPatchVersion;
        private Label lblUpdateListVer;
        private TextBox txtUpdateListVer;
        private Label lblClientPatchNum;
        private TextBox txtClientPatchNum;

        // botões de ação
        private ToolStripButton btnGenerateNow;
        private ToolStripButton btnToggleWatch;
        private ToolStripLabel lblWatchStatus;

        // progresso (NOVOS)
        private ProgressBar progressBar;
        private Label lblStatus;

        // log
        private Label lblLog;
        private TextBox txtLog;

        // status strip
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblUpdateListSummary;
        // private ToolStripStatusLabel lblLanguage;
        // private ToolStripComboBox cboLanguage;
    }
}
