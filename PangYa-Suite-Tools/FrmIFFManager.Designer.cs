namespace PangYa_Suite_Tools
{
    partial class FrmIFFManager
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblLanguage;
        private System.Windows.Forms.ToolStripComboBox cboLanguage;
        private System.Windows.Forms.ToolStripStatusLabel lblStringEncoding;
        private System.Windows.Forms.ToolStripComboBox cboStringEncoding;
        private System.Windows.Forms.ToolStripStatusLabel lblRegion;
        private System.Windows.Forms.ToolStripComboBox cboRegion;
        private System.Windows.Forms.ToolStripStatusLabel lblContainerKey;
        private System.Windows.Forms.ToolStripComboBox cboContainerKey;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.GroupBox grpIffFiles;
        private System.Windows.Forms.ListBox lstIffFiles;
        private System.Windows.Forms.Panel pnlTopBar;
        private System.Windows.Forms.Button btnBrowseIffDir;
        private System.Windows.Forms.TextBox txtIffDirectory;
        private System.Windows.Forms.Label lblIffDir;
        private System.Windows.Forms.Panel pnlEditorContainer;
        private System.Windows.Forms.Label lblNoFileSelected;
        private System.Windows.Forms.DataGridView gridRecords;
        private System.Windows.Forms.Button btnOpenArchive;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAddRow;
        private System.Windows.Forms.Button btnDeleteRows;
        private System.Windows.Forms.Button btnAddColumn;
        private System.Windows.Forms.Label lblSchemaCoverage;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblLanguage = new System.Windows.Forms.ToolStripStatusLabel();
            this.cboLanguage = new System.Windows.Forms.ToolStripComboBox();
            this.lblStringEncoding = new System.Windows.Forms.ToolStripStatusLabel();
            this.cboStringEncoding = new System.Windows.Forms.ToolStripComboBox();
            this.lblRegion = new System.Windows.Forms.ToolStripStatusLabel();
            this.cboRegion = new System.Windows.Forms.ToolStripComboBox();
            this.lblContainerKey = new System.Windows.Forms.ToolStripStatusLabel();
            this.cboContainerKey = new System.Windows.Forms.ToolStripComboBox();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.grpIffFiles = new System.Windows.Forms.GroupBox();
            this.lstIffFiles = new System.Windows.Forms.ListBox();
            this.pnlEditorContainer = new System.Windows.Forms.Panel();
            this.lblNoFileSelected = new System.Windows.Forms.Label();
            this.gridRecords = new System.Windows.Forms.DataGridView();
            this.pnlTopBar = new System.Windows.Forms.Panel();
            this.btnBrowseIffDir = new System.Windows.Forms.Button();
            this.btnOpenArchive = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnAddRow = new System.Windows.Forms.Button();
            this.btnDeleteRows = new System.Windows.Forms.Button();
            this.btnAddColumn = new System.Windows.Forms.Button();
            this.lblSchemaCoverage = new System.Windows.Forms.Label();
            this.txtIffDirectory = new System.Windows.Forms.TextBox();
            this.lblIffDir = new System.Windows.Forms.Label();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.grpIffFiles.SuspendLayout();
            this.pnlEditorContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridRecords)).BeginInit();
            this.pnlTopBar.SuspendLayout();
            this.SuspendLayout();
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.lblStatus, this.lblContainerKey, this.cboContainerKey, this.lblRegion, this.cboRegion, this.lblStringEncoding, this.cboStringEncoding, this.lblLanguage, this.cboLanguage });
            this.statusStrip.Location = new System.Drawing.Point(0, 539);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(984, 22);
            this.statusStrip.TabIndex = 0;
            this.statusStrip.Text = "statusStrip1";
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(230, 17);
            //
            // lblLanguage
            //
            this.lblLanguage.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(47, 17);
            // 
            // cboLanguage
            // 
            this.cboLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLanguage.Name = "cboLanguage";
            this.cboLanguage.Size = new System.Drawing.Size(120, 23);
            this.cboLanguage.SelectedIndexChanged += new System.EventHandler(this.cboLanguage_SelectedIndexChanged);
            //
            // lblStringEncoding
            //
            this.lblStringEncoding.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.lblStringEncoding.Name = "lblStringEncoding";
            //
            // cboStringEncoding
            //
            this.cboStringEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStringEncoding.Name = "cboStringEncoding";
            this.cboStringEncoding.Size = new System.Drawing.Size(180, 23);
            this.cboStringEncoding.SelectedIndexChanged += new System.EventHandler(this.cboStringEncoding_SelectedIndexChanged);
            //
            // lblRegion
            //
            this.lblRegion.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.lblRegion.Name = "lblRegion";
            //
            // cboRegion
            //
            this.cboRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboRegion.Name = "cboRegion";
            this.cboRegion.Size = new System.Drawing.Size(105, 23);
            this.cboRegion.SelectedIndexChanged += new System.EventHandler(this.cboRegion_SelectedIndexChanged);
            //
            // lblContainerKey
            //
            this.lblContainerKey.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.lblContainerKey.Name = "lblContainerKey";
            //
            // cboContainerKey
            //
            this.cboContainerKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboContainerKey.Name = "cboContainerKey";
            this.cboContainerKey.Size = new System.Drawing.Size(130, 23);
            this.cboContainerKey.SelectedIndexChanged += new System.EventHandler(this.cboContainerKey_SelectedIndexChanged);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 95);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.grpIffFiles);
            this.splitContainerMain.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.pnlEditorContainer);
            this.splitContainerMain.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.splitContainerMain.Size = new System.Drawing.Size(984, 444);
            this.splitContainerMain.SplitterDistance = 260;
            this.splitContainerMain.TabIndex = 1;
            // 
            // grpIffFiles
            // 
            this.grpIffFiles.Controls.Add(this.lstIffFiles);
            this.grpIffFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpIffFiles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpIffFiles.Location = new System.Drawing.Point(5, 5);
            this.grpIffFiles.Name = "grpIffFiles";
            this.grpIffFiles.Size = new System.Drawing.Size(250, 469);
            this.grpIffFiles.TabIndex = 0;
            this.grpIffFiles.TabStop = false;
            // 
            // lstIffFiles
            // 
            this.lstIffFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstIffFiles.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lstIffFiles.FormattingEnabled = true;
            this.lstIffFiles.ItemHeight = 17;
            this.lstIffFiles.Location = new System.Drawing.Point(3, 19);
            this.lstIffFiles.Name = "lstIffFiles";
            this.lstIffFiles.Size = new System.Drawing.Size(244, 447);
            this.lstIffFiles.TabIndex = 0;
            this.lstIffFiles.SelectedIndexChanged += new System.EventHandler(this.lstIffFiles_SelectedIndexChanged);
            // 
            // pnlEditorContainer
            // 
            this.pnlEditorContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlEditorContainer.Controls.Add(this.lblNoFileSelected);
            this.pnlEditorContainer.Controls.Add(this.gridRecords);
            this.pnlEditorContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlEditorContainer.Location = new System.Drawing.Point(5, 5);
            this.pnlEditorContainer.Name = "pnlEditorContainer";
            this.pnlEditorContainer.Size = new System.Drawing.Size(710, 469);
            this.pnlEditorContainer.TabIndex = 0;
            // 
            // lblNoFileSelected
            // 
            this.lblNoFileSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNoFileSelected.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Italic);
            this.lblNoFileSelected.ForeColor = System.Drawing.Color.Gray;
            this.lblNoFileSelected.Location = new System.Drawing.Point(0, 0);
            this.lblNoFileSelected.Name = "lblNoFileSelected";
            this.lblNoFileSelected.Size = new System.Drawing.Size(708, 467);
            this.lblNoFileSelected.TabIndex = 0;
            this.lblNoFileSelected.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // gridRecords
            //
            this.gridRecords.AllowUserToAddRows = false;
            this.gridRecords.AllowUserToDeleteRows = false;
            this.gridRecords.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.gridRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridRecords.Name = "gridRecords";
            this.gridRecords.RowHeadersVisible = false;
            this.gridRecords.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridRecords.Size = new System.Drawing.Size(708, 467);
            this.gridRecords.Visible = false;
            //
            // pnlTopBar
            //
            this.pnlTopBar.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pnlTopBar.Controls.Add(this.btnBrowseIffDir);
            this.pnlTopBar.Controls.Add(this.btnOpenArchive);
            this.pnlTopBar.Controls.Add(this.btnSave);
            this.pnlTopBar.Controls.Add(this.btnAddRow);
            this.pnlTopBar.Controls.Add(this.btnDeleteRows);
            this.pnlTopBar.Controls.Add(this.btnAddColumn);
            this.pnlTopBar.Controls.Add(this.lblSchemaCoverage);
            this.pnlTopBar.Controls.Add(this.txtIffDirectory);
            this.pnlTopBar.Controls.Add(this.lblIffDir);
            this.pnlTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopBar.Location = new System.Drawing.Point(0, 0);
            this.pnlTopBar.Name = "pnlTopBar";
            this.pnlTopBar.Size = new System.Drawing.Size(984, 95);
            this.pnlTopBar.TabIndex = 2;
            //
            // btnBrowseIffDir
            //
            this.btnBrowseIffDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseIffDir.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnBrowseIffDir.Location = new System.Drawing.Point(712, 18);
            this.btnBrowseIffDir.Name = "btnBrowseIffDir";
            this.btnBrowseIffDir.Size = new System.Drawing.Size(80, 25);
            this.btnBrowseIffDir.TabIndex = 2;
            this.btnBrowseIffDir.UseVisualStyleBackColor = true;
            this.btnBrowseIffDir.Click += new System.EventHandler(this.btnBrowseIffDir_Click);
            //
            // btnOpenArchive
            //
            this.btnOpenArchive.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnOpenArchive.Location = new System.Drawing.Point(798, 18);
            this.btnOpenArchive.Name = "btnOpenArchive";
            this.btnOpenArchive.Size = new System.Drawing.Size(88, 25);
            this.btnOpenArchive.Click += new System.EventHandler(this.btnOpenArchive_Click);
            //
            // btnSave
            //
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Enabled = false;
            this.btnSave.Location = new System.Drawing.Point(892, 18);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 25);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnAddRow
            //
            this.btnAddRow.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnAddRow.Enabled = false;
            this.btnAddRow.Location = new System.Drawing.Point(766, 55);
            this.btnAddRow.Name = "btnAddRow";
            this.btnAddRow.Size = new System.Drawing.Size(100, 28);
            this.btnAddRow.Click += new System.EventHandler(this.btnAddRow_Click);
            //
            // btnDeleteRows
            //
            this.btnDeleteRows.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnDeleteRows.Enabled = false;
            this.btnDeleteRows.Location = new System.Drawing.Point(872, 55);
            this.btnDeleteRows.Name = "btnDeleteRows";
            this.btnDeleteRows.Size = new System.Drawing.Size(100, 28);
            this.btnDeleteRows.Click += new System.EventHandler(this.btnDeleteRows_Click);
            //
            // btnAddColumn
            //
            this.btnAddColumn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnAddColumn.Enabled = false;
            this.btnAddColumn.Location = new System.Drawing.Point(660, 55);
            this.btnAddColumn.Name = "btnAddColumn";
            this.btnAddColumn.Size = new System.Drawing.Size(100, 28);
            this.btnAddColumn.Click += new System.EventHandler(this.btnAddColumn_Click);
            //
            // lblSchemaCoverage
            //
            this.lblSchemaCoverage.AutoSize = true;
            this.lblSchemaCoverage.Location = new System.Drawing.Point(12, 63);
            this.lblSchemaCoverage.Name = "lblSchemaCoverage";
            this.lblSchemaCoverage.Visible = false;
            //
            // txtIffDirectory
            //
            this.txtIffDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtIffDirectory.Location = new System.Drawing.Point(135, 19);
            this.txtIffDirectory.Name = "txtIffDirectory";
            this.txtIffDirectory.ReadOnly = true;
            this.txtIffDirectory.Size = new System.Drawing.Size(571, 23);
            this.txtIffDirectory.TabIndex = 1;
            // 
            // lblIffDir
            // 
            this.lblIffDir.AutoSize = true;
            this.lblIffDir.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblIffDir.Location = new System.Drawing.Point(12, 22);
            this.lblIffDir.Name = "lblIffDir";
            this.lblIffDir.Size = new System.Drawing.Size(117, 15);
            this.lblIffDir.TabIndex = 0;
            // 
            // FrmIffManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.pnlTopBar);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "FrmIffManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.grpIffFiles.ResumeLayout(false);
            this.pnlEditorContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridRecords)).EndInit();
            this.pnlTopBar.ResumeLayout(false);
            this.pnlTopBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
