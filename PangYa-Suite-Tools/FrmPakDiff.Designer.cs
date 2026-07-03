namespace PangYa_Suite_Tools
{
    partial class FrmPakDiff
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tabMain = new TabControl();
            tabLog = new TabPage();
            grpSnapshotA = new GroupBox();
            lblSnapshotAPath = new Label();
            txtSnapshotAPath = new TextBox();
            btnBrowseSnapshotA = new Button();
            btnTakeSnapshotA = new Button();
            btnSaveSnapshotA = new Button();
            btnLoadSnapshotA = new Button();
            lblSnapshotAStatus = new Label();
            grpSnapshotB = new GroupBox();
            lblSnapshotBPath = new Label();
            txtSnapshotBPath = new TextBox();
            btnBrowseSnapshotB = new Button();
            btnTakeSnapshotB = new Button();
            btnSaveSnapshotB = new Button();
            btnLoadSnapshotB = new Button();
            lblSnapshotBStatus = new Label();
            btnCompareSnapshots = new Button();
            chkLogSelectAll = new CheckBox();
            lstLogChanges = new ListView();
            colLogSymbol = new ColumnHeader();
            colLogPak = new ColumnHeader();
            colLogFile = new ColumnHeader();
            colLogStatus = new ColumnHeader();
            colLogOldSize = new ColumnHeader();
            colLogNewSize = new ColumnHeader();
            txtChangeLog = new TextBox();
            btnSaveLog = new Button();
            prgBarLog = new ProgressBar();
            tabDiff = new TabPage();
            grpDirectories = new GroupBox();
            lblSource = new Label();
            txtSourceClient = new TextBox();
            btnBrowseSource = new Button();
            lblCompare = new Label();
            txtCompareClient = new TextBox();
            btnBrowseCompare = new Button();
            grpMode = new GroupBox();
            rbDifferences = new RadioButton();
            rbIdentical = new RadioButton();
            btnCompare = new Button();
            lstDiffFiles = new ListView();
            colFile = new ColumnHeader();
            colPak = new ColumnHeader();
            colStatus = new ColumnHeader();
            btnExtractSelected = new Button();
            chkSelectAll = new CheckBox();
            prgBarDiff = new ProgressBar();
            statusStrip = new StatusStrip();
            lblLanguage = new ToolStripStatusLabel();
            cboLanguage = new ToolStripComboBox();
            tabMain.SuspendLayout();
            tabLog.SuspendLayout();
            grpSnapshotA.SuspendLayout();
            grpSnapshotB.SuspendLayout();
            tabDiff.SuspendLayout();
            grpDirectories.SuspendLayout();
            grpMode.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabLog);
            tabMain.Controls.Add(tabDiff);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(940, 624);
            tabMain.TabIndex = 0;
            // 
            // tabLog
            // 
            tabLog.Controls.Add(grpSnapshotA);
            tabLog.Controls.Add(grpSnapshotB);
            tabLog.Controls.Add(btnCompareSnapshots);
            tabLog.Controls.Add(chkLogSelectAll);
            tabLog.Controls.Add(lstLogChanges);
            tabLog.Controls.Add(txtChangeLog);
            tabLog.Controls.Add(btnSaveLog);
            tabLog.Controls.Add(prgBarLog);
            tabLog.Location = new Point(4, 24);
            tabLog.Name = "tabLog";
            tabLog.Padding = new Padding(8);
            tabLog.Size = new Size(932, 596);
            tabLog.TabIndex = 0;
            tabLog.Text = " 📋 Change History / Log ";
            tabLog.UseVisualStyleBackColor = true;
            // 
            // grpSnapshotA
            // 
            grpSnapshotA.Controls.Add(lblSnapshotAPath);
            grpSnapshotA.Controls.Add(txtSnapshotAPath);
            grpSnapshotA.Controls.Add(btnBrowseSnapshotA);
            grpSnapshotA.Controls.Add(btnTakeSnapshotA);
            grpSnapshotA.Controls.Add(btnSaveSnapshotA);
            grpSnapshotA.Controls.Add(btnLoadSnapshotA);
            grpSnapshotA.Controls.Add(lblSnapshotAStatus);
            grpSnapshotA.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpSnapshotA.Location = new Point(8, 8);
            grpSnapshotA.Name = "grpSnapshotA";
            grpSnapshotA.Size = new Size(456, 115);
            grpSnapshotA.TabIndex = 0;
            grpSnapshotA.TabStop = false;
            grpSnapshotA.Text = " 📸 Snapshot A — Before / Base ";
            // 
            // lblSnapshotAPath
            // 
            lblSnapshotAPath.AutoSize = true;
            lblSnapshotAPath.Font = new Font("Segoe UI", 9F);
            lblSnapshotAPath.Location = new Point(10, 24);
            lblSnapshotAPath.Name = "lblSnapshotAPath";
            lblSnapshotAPath.Size = new Size(67, 15);
            lblSnapshotAPath.TabIndex = 0;
            lblSnapshotAPath.Text = "PAK Folder:";
            // 
            // txtSnapshotAPath
            // 
            txtSnapshotAPath.Font = new Font("Segoe UI", 9F);
            txtSnapshotAPath.Location = new Point(10, 42);
            txtSnapshotAPath.Name = "txtSnapshotAPath";
            txtSnapshotAPath.Size = new Size(280, 23);
            txtSnapshotAPath.TabIndex = 0;
            // 
            // btnBrowseSnapshotA
            // 
            btnBrowseSnapshotA.Font = new Font("Segoe UI", 9F);
            btnBrowseSnapshotA.Location = new Point(296, 41);
            btnBrowseSnapshotA.Name = "btnBrowseSnapshotA";
            btnBrowseSnapshotA.Size = new Size(60, 25);
            btnBrowseSnapshotA.TabIndex = 1;
            btnBrowseSnapshotA.Text = "📁...";
            btnBrowseSnapshotA.UseVisualStyleBackColor = true;
            btnBrowseSnapshotA.Click += btnBrowseSnapshotA_Click;
            // 
            // btnTakeSnapshotA
            // 
            btnTakeSnapshotA.BackColor = Color.FromArgb(0, 122, 204);
            btnTakeSnapshotA.FlatAppearance.BorderSize = 0;
            btnTakeSnapshotA.FlatStyle = FlatStyle.Flat;
            btnTakeSnapshotA.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnTakeSnapshotA.ForeColor = Color.White;
            btnTakeSnapshotA.Location = new Point(10, 75);
            btnTakeSnapshotA.Name = "btnTakeSnapshotA";
            btnTakeSnapshotA.Size = new Size(130, 28);
            btnTakeSnapshotA.TabIndex = 2;
            btnTakeSnapshotA.Text = "📸 Take Snapshot";
            btnTakeSnapshotA.UseVisualStyleBackColor = false;
            btnTakeSnapshotA.Click += btnTakeSnapshotA_Click;
            // 
            // btnSaveSnapshotA
            // 
            btnSaveSnapshotA.Font = new Font("Segoe UI", 9F);
            btnSaveSnapshotA.Location = new Point(148, 75);
            btnSaveSnapshotA.Name = "btnSaveSnapshotA";
            btnSaveSnapshotA.Size = new Size(100, 28);
            btnSaveSnapshotA.TabIndex = 3;
            btnSaveSnapshotA.Text = "💾 Save";
            btnSaveSnapshotA.UseVisualStyleBackColor = true;
            btnSaveSnapshotA.Click += btnSaveSnapshotA_Click;
            // 
            // btnLoadSnapshotA
            // 
            btnLoadSnapshotA.Font = new Font("Segoe UI", 9F);
            btnLoadSnapshotA.Location = new Point(256, 75);
            btnLoadSnapshotA.Name = "btnLoadSnapshotA";
            btnLoadSnapshotA.Size = new Size(100, 28);
            btnLoadSnapshotA.TabIndex = 4;
            btnLoadSnapshotA.Text = "📂 Load";
            btnLoadSnapshotA.UseVisualStyleBackColor = true;
            btnLoadSnapshotA.Click += btnLoadSnapshotA_Click;
            // 
            // lblSnapshotAStatus
            // 
            lblSnapshotAStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblSnapshotAStatus.ForeColor = Color.DimGray;
            lblSnapshotAStatus.Location = new Point(362, 75);
            lblSnapshotAStatus.Name = "lblSnapshotAStatus";
            lblSnapshotAStatus.Size = new Size(85, 28);
            lblSnapshotAStatus.TabIndex = 5;
            lblSnapshotAStatus.Text = "(not taken)";
            lblSnapshotAStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // grpSnapshotB
            // 
            grpSnapshotB.Controls.Add(lblSnapshotBPath);
            grpSnapshotB.Controls.Add(txtSnapshotBPath);
            grpSnapshotB.Controls.Add(btnBrowseSnapshotB);
            grpSnapshotB.Controls.Add(btnTakeSnapshotB);
            grpSnapshotB.Controls.Add(btnSaveSnapshotB);
            grpSnapshotB.Controls.Add(btnLoadSnapshotB);
            grpSnapshotB.Controls.Add(lblSnapshotBStatus);
            grpSnapshotB.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpSnapshotB.Location = new Point(472, 8);
            grpSnapshotB.Name = "grpSnapshotB";
            grpSnapshotB.Size = new Size(452, 115);
            grpSnapshotB.TabIndex = 1;
            grpSnapshotB.TabStop = false;
            grpSnapshotB.Text = " 📸 Snapshot B — After / New ";
            // 
            // lblSnapshotBPath
            // 
            lblSnapshotBPath.AutoSize = true;
            lblSnapshotBPath.Font = new Font("Segoe UI", 9F);
            lblSnapshotBPath.Location = new Point(10, 24);
            lblSnapshotBPath.Name = "lblSnapshotBPath";
            lblSnapshotBPath.Size = new Size(67, 15);
            lblSnapshotBPath.TabIndex = 0;
            lblSnapshotBPath.Text = "PAK Folder:";
            // 
            // txtSnapshotBPath
            // 
            txtSnapshotBPath.Font = new Font("Segoe UI", 9F);
            txtSnapshotBPath.Location = new Point(10, 42);
            txtSnapshotBPath.Name = "txtSnapshotBPath";
            txtSnapshotBPath.Size = new Size(276, 23);
            txtSnapshotBPath.TabIndex = 0;
            // 
            // btnBrowseSnapshotB
            // 
            btnBrowseSnapshotB.Font = new Font("Segoe UI", 9F);
            btnBrowseSnapshotB.Location = new Point(292, 41);
            btnBrowseSnapshotB.Name = "btnBrowseSnapshotB";
            btnBrowseSnapshotB.Size = new Size(60, 25);
            btnBrowseSnapshotB.TabIndex = 1;
            btnBrowseSnapshotB.Text = "📁...";
            btnBrowseSnapshotB.UseVisualStyleBackColor = true;
            btnBrowseSnapshotB.Click += btnBrowseSnapshotB_Click;
            // 
            // btnTakeSnapshotB
            // 
            btnTakeSnapshotB.BackColor = Color.FromArgb(40, 167, 69);
            btnTakeSnapshotB.FlatAppearance.BorderSize = 0;
            btnTakeSnapshotB.FlatStyle = FlatStyle.Flat;
            btnTakeSnapshotB.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnTakeSnapshotB.ForeColor = Color.White;
            btnTakeSnapshotB.Location = new Point(10, 75);
            btnTakeSnapshotB.Name = "btnTakeSnapshotB";
            btnTakeSnapshotB.Size = new Size(130, 28);
            btnTakeSnapshotB.TabIndex = 2;
            btnTakeSnapshotB.Text = "📸 Take Snapshot";
            btnTakeSnapshotB.UseVisualStyleBackColor = false;
            btnTakeSnapshotB.Click += btnTakeSnapshotB_Click;
            // 
            // btnSaveSnapshotB
            // 
            btnSaveSnapshotB.Font = new Font("Segoe UI", 9F);
            btnSaveSnapshotB.Location = new Point(148, 75);
            btnSaveSnapshotB.Name = "btnSaveSnapshotB";
            btnSaveSnapshotB.Size = new Size(100, 28);
            btnSaveSnapshotB.TabIndex = 3;
            btnSaveSnapshotB.Text = "💾 Save";
            btnSaveSnapshotB.UseVisualStyleBackColor = true;
            btnSaveSnapshotB.Click += btnSaveSnapshotB_Click;
            // 
            // btnLoadSnapshotB
            // 
            btnLoadSnapshotB.Font = new Font("Segoe UI", 9F);
            btnLoadSnapshotB.Location = new Point(256, 75);
            btnLoadSnapshotB.Name = "btnLoadSnapshotB";
            btnLoadSnapshotB.Size = new Size(100, 28);
            btnLoadSnapshotB.TabIndex = 4;
            btnLoadSnapshotB.Text = "📂 Load";
            btnLoadSnapshotB.UseVisualStyleBackColor = true;
            btnLoadSnapshotB.Click += btnLoadSnapshotB_Click;
            // 
            // lblSnapshotBStatus
            // 
            lblSnapshotBStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblSnapshotBStatus.ForeColor = Color.DimGray;
            lblSnapshotBStatus.Location = new Point(358, 75);
            lblSnapshotBStatus.Name = "lblSnapshotBStatus";
            lblSnapshotBStatus.Size = new Size(85, 28);
            lblSnapshotBStatus.TabIndex = 5;
            lblSnapshotBStatus.Text = "(not taken)";
            lblSnapshotBStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnCompareSnapshots
            // 
            btnCompareSnapshots.BackColor = Color.FromArgb(111, 66, 193);
            btnCompareSnapshots.Enabled = false;
            btnCompareSnapshots.FlatAppearance.BorderSize = 0;
            btnCompareSnapshots.FlatStyle = FlatStyle.Flat;
            btnCompareSnapshots.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnCompareSnapshots.ForeColor = Color.White;
            btnCompareSnapshots.Location = new Point(8, 130);
            btnCompareSnapshots.Name = "btnCompareSnapshots";
            btnCompareSnapshots.Size = new Size(916, 38);
            btnCompareSnapshots.TabIndex = 2;
            btnCompareSnapshots.Text = "🔍 COMPARE SNAPSHOTS / COMPARAR SNAPSHOTS";
            btnCompareSnapshots.UseVisualStyleBackColor = false;
            btnCompareSnapshots.Click += btnCompareSnapshots_Click;
            // 
            // chkLogSelectAll
            // 
            chkLogSelectAll.AutoSize = true;
            chkLogSelectAll.Font = new Font("Segoe UI", 9F);
            chkLogSelectAll.Location = new Point(8, 382);
            chkLogSelectAll.Name = "chkLogSelectAll";
            chkLogSelectAll.Size = new Size(170, 19);
            chkLogSelectAll.TabIndex = 4;
            chkLogSelectAll.Text = "Select All / Selecionar Tudo";
            chkLogSelectAll.UseVisualStyleBackColor = true;
            chkLogSelectAll.CheckedChanged += chkLogSelectAll_CheckedChanged;
            // 
            // lstLogChanges
            // 
            lstLogChanges.CheckBoxes = true;
            lstLogChanges.Columns.AddRange(new ColumnHeader[] { colLogSymbol, colLogPak, colLogFile, colLogStatus, colLogOldSize, colLogNewSize });
            lstLogChanges.FullRowSelect = true;
            lstLogChanges.GridLines = true;
            lstLogChanges.Location = new Point(8, 176);
            lstLogChanges.Name = "lstLogChanges";
            lstLogChanges.Size = new Size(916, 200);
            lstLogChanges.TabIndex = 3;
            lstLogChanges.UseCompatibleStateImageBehavior = false;
            lstLogChanges.View = View.Details;
            // 
            // colLogSymbol
            // 
            colLogSymbol.Text = "±";
            colLogSymbol.Width = 28;
            // 
            // colLogPak
            // 
            colLogPak.Text = "PAK File";
            colLogPak.Width = 200;
            // 
            // colLogFile
            // 
            colLogFile.Text = "Internal File";
            colLogFile.Width = 380;
            // 
            // colLogStatus
            // 
            colLogStatus.Text = "Status";
            colLogStatus.Width = 90;
            // 
            // colLogOldSize
            // 
            colLogOldSize.Text = "Old Size";
            colLogOldSize.Width = 90;
            // 
            // colLogNewSize
            // 
            colLogNewSize.Text = "New Size";
            colLogNewSize.Width = 90;
            // 
            // txtChangeLog
            // 
            txtChangeLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtChangeLog.BackColor = Color.FromArgb(20, 20, 30);
            txtChangeLog.Font = new Font("Consolas", 8.5F);
            txtChangeLog.ForeColor = Color.LightGreen;
            txtChangeLog.Location = new Point(8, 406);
            txtChangeLog.Multiline = true;
            txtChangeLog.Name = "txtChangeLog";
            txtChangeLog.ReadOnly = true;
            txtChangeLog.ScrollBars = ScrollBars.Both;
            txtChangeLog.Size = new Size(806, 156);
            txtChangeLog.TabIndex = 5;
            txtChangeLog.WordWrap = false;
            // 
            // btnSaveLog
            // 
            btnSaveLog.BackColor = Color.FromArgb(108, 117, 125);
            btnSaveLog.FlatAppearance.BorderSize = 0;
            btnSaveLog.FlatStyle = FlatStyle.Flat;
            btnSaveLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSaveLog.ForeColor = Color.White;
            btnSaveLog.Location = new Point(820, 406);
            btnSaveLog.Name = "btnSaveLog";
            btnSaveLog.Size = new Size(104, 132);
            btnSaveLog.TabIndex = 6;
            btnSaveLog.Text = "💾 Save\nLog\n.txt";
            btnSaveLog.UseVisualStyleBackColor = false;
            btnSaveLog.Click += btnSaveLog_Click;
            // 
            // prgBarLog
            // 
            prgBarLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            prgBarLog.Location = new Point(8, 568);
            prgBarLog.Name = "prgBarLog";
            prgBarLog.Size = new Size(916, 16);
            prgBarLog.Style = ProgressBarStyle.Continuous;
            prgBarLog.TabIndex = 7;
            prgBarLog.Visible = false;
            // 
            // tabDiff
            // 
            tabDiff.Controls.Add(grpDirectories);
            tabDiff.Controls.Add(grpMode);
            tabDiff.Controls.Add(lstDiffFiles);
            tabDiff.Controls.Add(btnExtractSelected);
            tabDiff.Controls.Add(chkSelectAll);
            tabDiff.Controls.Add(prgBarDiff);
            tabDiff.Location = new Point(4, 24);
            tabDiff.Name = "tabDiff";
            tabDiff.Padding = new Padding(8);
            tabDiff.Size = new Size(932, 596);
            tabDiff.TabIndex = 1;
            tabDiff.Text = " 🔍 Client Compare & Extract ";
            tabDiff.UseVisualStyleBackColor = true;
            // 
            // grpDirectories
            // 
            grpDirectories.Controls.Add(lblSource);
            grpDirectories.Controls.Add(txtSourceClient);
            grpDirectories.Controls.Add(btnBrowseSource);
            grpDirectories.Controls.Add(lblCompare);
            grpDirectories.Controls.Add(txtCompareClient);
            grpDirectories.Controls.Add(btnBrowseCompare);
            grpDirectories.Location = new Point(8, 8);
            grpDirectories.Name = "grpDirectories";
            grpDirectories.Size = new Size(916, 115);
            grpDirectories.TabIndex = 0;
            grpDirectories.TabStop = false;
            grpDirectories.Text = " Client Directories / Diretórios dos Clientes ";
            // 
            // lblSource
            // 
            lblSource.AutoSize = true;
            lblSource.Location = new Point(10, 28);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(167, 15);
            lblSource.TabIndex = 0;
            lblSource.Text = "Target / Source (Extract From):";
            // 
            // txtSourceClient
            // 
            txtSourceClient.Location = new Point(10, 46);
            txtSourceClient.Name = "txtSourceClient";
            txtSourceClient.Size = new Size(812, 23);
            txtSourceClient.TabIndex = 0;
            // 
            // btnBrowseSource
            // 
            btnBrowseSource.Location = new Point(828, 45);
            btnBrowseSource.Name = "btnBrowseSource";
            btnBrowseSource.Size = new Size(74, 25);
            btnBrowseSource.TabIndex = 1;
            btnBrowseSource.Text = "📁...";
            btnBrowseSource.UseVisualStyleBackColor = true;
            btnBrowseSource.Click += BtnBrowseSource_Click;
            // 
            // lblCompare
            // 
            lblCompare.AutoSize = true;
            lblCompare.Location = new Point(10, 76);
            lblCompare.Name = "lblCompare";
            lblCompare.Size = new Size(172, 15);
            lblCompare.TabIndex = 2;
            lblCompare.Text = "Your Client (To Compare With):";
            // 
            // txtCompareClient
            // 
            txtCompareClient.Location = new Point(10, 84);
            txtCompareClient.Name = "txtCompareClient";
            txtCompareClient.Size = new Size(812, 23);
            txtCompareClient.TabIndex = 2;
            // 
            // btnBrowseCompare
            // 
            btnBrowseCompare.Location = new Point(828, 83);
            btnBrowseCompare.Name = "btnBrowseCompare";
            btnBrowseCompare.Size = new Size(74, 25);
            btnBrowseCompare.TabIndex = 3;
            btnBrowseCompare.Text = "📁...";
            btnBrowseCompare.UseVisualStyleBackColor = true;
            btnBrowseCompare.Click += BtnBrowseCompare_Click;
            // 
            // grpMode
            // 
            grpMode.Controls.Add(rbDifferences);
            grpMode.Controls.Add(rbIdentical);
            grpMode.Controls.Add(btnCompare);
            grpMode.Location = new Point(8, 130);
            grpMode.Name = "grpMode";
            grpMode.Size = new Size(210, 160);
            grpMode.TabIndex = 1;
            grpMode.TabStop = false;
            grpMode.Text = " Comparison Mode ";
            // 
            // rbDifferences
            // 
            rbDifferences.AutoSize = true;
            rbDifferences.Checked = true;
            rbDifferences.Location = new Point(15, 32);
            rbDifferences.Name = "rbDifferences";
            rbDifferences.Size = new Size(145, 19);
            rbDifferences.TabIndex = 0;
            rbDifferences.TabStop = true;
            rbDifferences.Text = "Differences / New Files";
            rbDifferences.UseVisualStyleBackColor = true;
            // 
            // rbIdentical
            // 
            rbIdentical.AutoSize = true;
            rbIdentical.Location = new Point(15, 60);
            rbIdentical.Name = "rbIdentical";
            rbIdentical.Size = new Size(141, 19);
            rbIdentical.TabIndex = 1;
            rbIdentical.Text = "Identical Files (Equals)";
            rbIdentical.UseVisualStyleBackColor = true;
            // 
            // btnCompare
            // 
            btnCompare.BackColor = Color.FromArgb(255, 193, 7);
            btnCompare.FlatAppearance.BorderSize = 0;
            btnCompare.FlatStyle = FlatStyle.Flat;
            btnCompare.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCompare.ForeColor = Color.Black;
            btnCompare.Location = new Point(15, 95);
            btnCompare.Name = "btnCompare";
            btnCompare.Size = new Size(180, 40);
            btnCompare.TabIndex = 2;
            btnCompare.Text = "🔍 Compare / Comparar";
            btnCompare.UseVisualStyleBackColor = false;
            btnCompare.Click += BtnCompare_Click;
            // 
            // lstDiffFiles
            // 
            lstDiffFiles.CheckBoxes = true;
            lstDiffFiles.Columns.AddRange(new ColumnHeader[] { colFile, colPak, colStatus });
            lstDiffFiles.FullRowSelect = true;
            lstDiffFiles.GridLines = true;
            lstDiffFiles.Location = new Point(226, 136);
            lstDiffFiles.Name = "lstDiffFiles";
            lstDiffFiles.Size = new Size(698, 370);
            lstDiffFiles.TabIndex = 2;
            lstDiffFiles.UseCompatibleStateImageBehavior = false;
            lstDiffFiles.View = View.Details;
            // 
            // colFile
            // 
            colFile.Text = "File Path / Caminho do Arquivo";
            colFile.Width = 380;
            // 
            // colPak
            // 
            colPak.Text = "Source PAK";
            colPak.Width = 180;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 110;
            // 
            // btnExtractSelected
            // 
            btnExtractSelected.BackColor = Color.FromArgb(40, 167, 69);
            btnExtractSelected.FlatAppearance.BorderSize = 0;
            btnExtractSelected.FlatStyle = FlatStyle.Flat;
            btnExtractSelected.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExtractSelected.ForeColor = Color.White;
            btnExtractSelected.Location = new Point(8, 430);
            btnExtractSelected.Name = "btnExtractSelected";
            btnExtractSelected.Size = new Size(210, 55);
            btnExtractSelected.TabIndex = 3;
            btnExtractSelected.Text = "📦 Extract Selected\nExtrair Selecionados";
            btnExtractSelected.UseVisualStyleBackColor = false;
            btnExtractSelected.Click += BtnExtractSelected_Click;
            // 
            // chkSelectAll
            // 
            chkSelectAll.AutoSize = true;
            chkSelectAll.Font = new Font("Segoe UI", 9F);
            chkSelectAll.Location = new Point(226, 514);
            chkSelectAll.Name = "chkSelectAll";
            chkSelectAll.Size = new Size(170, 19);
            chkSelectAll.TabIndex = 4;
            chkSelectAll.Text = "Select All / Selecionar Tudo";
            chkSelectAll.UseVisualStyleBackColor = true;
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
            // 
            // prgBarDiff
            // 
            prgBarDiff.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            prgBarDiff.Location = new Point(8, 568);
            prgBarDiff.Name = "prgBarDiff";
            prgBarDiff.Size = new Size(916, 16);
            prgBarDiff.Style = ProgressBarStyle.Continuous;
            prgBarDiff.TabIndex = 5;
            prgBarDiff.Visible = false;
            //
            // statusStrip
            //
            statusStrip.Items.AddRange(new ToolStripItem[] { lblLanguage, cboLanguage });
            statusStrip.Location = new Point(0, 602);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(940, 22);
            statusStrip.TabIndex = 1;
            //
            // lblLanguage
            //
            lblLanguage.Name = "lblLanguage";
            //
            // cboLanguage
            //
            cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new Size(150, 23);
            cboLanguage.SelectedIndexChanged += cboLanguage_SelectedIndexChanged;
            // 
            // FrmPakDiff
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(940, 624);
            Controls.Add(tabMain);
            Controls.Add(statusStrip);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimumSize = new Size(956, 660);
            Name = "FrmPakDiff";
            StartPosition = FormStartPosition.CenterParent;
            Text = "PAK Diff — Change History & Multi-Client Sync";
            tabMain.ResumeLayout(false);
            tabLog.ResumeLayout(false);
            tabLog.PerformLayout();
            grpSnapshotA.ResumeLayout(false);
            grpSnapshotA.PerformLayout();
            grpSnapshotB.ResumeLayout(false);
            grpSnapshotB.PerformLayout();
            tabDiff.ResumeLayout(false);
            tabDiff.PerformLayout();
            grpDirectories.ResumeLayout(false);
            grpDirectories.PerformLayout();
            grpMode.ResumeLayout(false);
            grpMode.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ── Aba 1 controles ──────────────────────────────────────────────────
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TabPage tabDiff;
        private System.Windows.Forms.GroupBox grpSnapshotA;
        private System.Windows.Forms.Label lblSnapshotAPath;
        private System.Windows.Forms.TextBox txtSnapshotAPath;
        private System.Windows.Forms.Button btnBrowseSnapshotA;
        private System.Windows.Forms.Button btnTakeSnapshotA;
        private System.Windows.Forms.Button btnSaveSnapshotA;
        private System.Windows.Forms.Button btnLoadSnapshotA;
        private System.Windows.Forms.Label lblSnapshotAStatus;
        private System.Windows.Forms.GroupBox grpSnapshotB;
        private System.Windows.Forms.Label lblSnapshotBPath;
        private System.Windows.Forms.TextBox txtSnapshotBPath;
        private System.Windows.Forms.Button btnBrowseSnapshotB;
        private System.Windows.Forms.Button btnTakeSnapshotB;
        private System.Windows.Forms.Button btnSaveSnapshotB;
        private System.Windows.Forms.Button btnLoadSnapshotB;
        private System.Windows.Forms.Label lblSnapshotBStatus;
        private System.Windows.Forms.Button btnCompareSnapshots;
        private System.Windows.Forms.CheckBox chkLogSelectAll;
        private System.Windows.Forms.ListView lstLogChanges;
        private System.Windows.Forms.ColumnHeader colLogSymbol;
        private System.Windows.Forms.ColumnHeader colLogPak;
        private System.Windows.Forms.ColumnHeader colLogFile;
        private System.Windows.Forms.ColumnHeader colLogStatus;
        private System.Windows.Forms.ColumnHeader colLogOldSize;
        private System.Windows.Forms.ColumnHeader colLogNewSize;
        private System.Windows.Forms.TextBox txtChangeLog;
        private System.Windows.Forms.Button btnSaveLog;
        private System.Windows.Forms.ProgressBar prgBarLog;

        // ── Aba 2 controles ──────────────────────────────────────────────────
        private System.Windows.Forms.GroupBox grpDirectories;
        private System.Windows.Forms.Label lblSource;
        private System.Windows.Forms.TextBox txtSourceClient;
        private System.Windows.Forms.Button btnBrowseSource;
        private System.Windows.Forms.Label lblCompare;
        private System.Windows.Forms.TextBox txtCompareClient;
        private System.Windows.Forms.Button btnBrowseCompare;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton rbDifferences;
        private System.Windows.Forms.RadioButton rbIdentical;
        private System.Windows.Forms.Button btnCompare;
        private System.Windows.Forms.ListView lstDiffFiles;
        private System.Windows.Forms.ColumnHeader colFile;
        private System.Windows.Forms.ColumnHeader colPak;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.Button btnExtractSelected;
        private System.Windows.Forms.CheckBox chkSelectAll;
        private System.Windows.Forms.ProgressBar prgBarDiff;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblLanguage;
        private System.Windows.Forms.ToolStripComboBox cboLanguage;
    }
}
