using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Configuration;

using System.ComponentModel;
using System.Globalization;
using System.Text; 
using System.Xml.Linq;
using PangyaAPI.UpdateList.Flags; 
using PangyaAPI.UpdateList.Models;
using PangyaAPI.Utilities.Cryptography;

namespace PangYa_Suite_Tools
{
    public partial class FrmUpdateList : Form
    {
        // ── Estado interno ───────────────────────────────────────────────────
        private readonly Dictionary<string, FileStateApp> _fileCache = new(StringComparer.OrdinalIgnoreCase);
        private UpdateMaker? _updateMaker;
        private List<UpdateEntry> _updateEntries = new();

        private FileSystemWatcher? _watcher;
        private readonly Lock _generatorLock = new();
        private string _currentRawXml = string.Empty;
        private UpdateListDocument? _currentUpdateListDocument;
        private IReadOnlyList<UpdateFileVerificationResult> _verificationResults = [];
        private CancellationTokenSource? _verificationCancellation;
        private bool _isMonitoring = false;
        // private bool _isInitializingLanguages = true;
        private enum UpdateToolbarIconKind
        {
            Generate,
            MonitorStart,
            MonitorStop,
            RawXml,
            Verify,
            Cancel
        }

        // ── Construtor ───────────────────────────────────────────────────────
        public FrmUpdateList()
        {
            InitializeComponent();
            // InitializeLanguageComboBox();
            SetupComponents();
            RestorePreferences();
            ConfigureToolbars();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            FormClosing += FrmUpdateList_FormClosing;
            ApplyLocalization();
            Disposed += (_, _) =>
            {
                LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
                btnGenerateNow.Image?.Dispose();
                btnToggleWatch.Image?.Dispose();
                btnShowRawXml.Image?.Dispose();
                btnVerifyFiles.Image?.Dispose();
                btnCancelVerification.Image?.Dispose();
                _verificationCancellation?.Cancel();
                _verificationCancellation?.Dispose();
            };
        }

        public FrmUpdateList(string idiomaAtual) : this()
        {
            LocalizationManager.SetCulture(idiomaAtual);
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e) => ApplyLocalization();

        private void ApplyLocalization()
        {
            Text = Strings.Update_Title;
            tabDecrypt.Text = Strings.Update_DecryptTab;
            tabGenerator.Text = Strings.Update_GeneratorTab;
            grpConfig.Text = Strings.Update_Config;
            lblPangyaPath.Text = Strings.Update_Source;
            lblUpdatePath.Text = Strings.Update_Destination;
            lblFileKey.Text = Strings.Update_Key;
            lblPatchVersion.Text = Strings.Update_PatchVersion;
            lblUpdateListVer.Text = Strings.Update_ListVersion;
            lblClientPatchNum.Text = Strings.Update_PatchNumber;
            chkCreateZipPackages.Text = Strings.UpdateList_CreateZipPackages;
            txtViewerFilePath.PlaceholderText = Strings.UpdateList_ViewerPathHint;
            txtVerificationRootPath.PlaceholderText = Strings.UpdateList_VerificationRootHint;
            btnBrowsePangya.Text = Strings.Pak_Browse;
            btnBrowseUpdate.Text = Strings.Pak_Browse;
            btnBrowseViewer.Text = Strings.Pak_Browse;
            btnBrowseVerificationRoot.Text = Strings.Pak_Browse;
            btnGenerateNow.Text = Strings.UpdateList_GenerateNow;
            btnShowRawXml.Text = Strings.UpdateList_RawXml;
            btnVerifyFiles.Text = Strings.UpdateList_VerifyFiles;
            btnCancelVerification.Text = Strings.Common_Cancel;
            lblVerificationFilter.Text = Strings.UpdateList_Filter;
            bool showIssuesOnly = cboVerificationFilter.SelectedIndex == 1;
            cboVerificationFilter.Items.Clear();
            cboVerificationFilter.Items.AddRange([Strings.UpdateList_FilterAll, Strings.UpdateList_FilterIssues]);
            cboVerificationFilter.SelectedIndex = showIssuesOnly ? 1 : 0;
            colFileName.Text = Strings.UpdateList_ColumnFileName;
            colDirectory.Text = Strings.UpdateList_ColumnDirectory;
            colFileSize.Text = Strings.UpdateList_ColumnFileSize;
            colCrc.Text = Strings.UpdateList_ColumnCrc;
            colActualCrc.Text = Strings.UpdateList_ColumnActualCrc;
            colVerificationStatus.Text = Strings.UpdateList_ColumnVerificationStatus;
            colDate.Text = Strings.UpdateList_ColumnDate;
            colTime.Text = Strings.UpdateList_ColumnTime;
            colPackageName.Text = Strings.UpdateList_ColumnPackageName;
            colPackageSize.Text = Strings.UpdateList_ColumnPackageSize;
            lblLog.Text = Strings.Update_Log;
            if (_currentUpdateListDocument is not null)
                PopulateUpdateFileList(_currentUpdateListDocument);
            UpdatePatchSummary(_currentUpdateListDocument);

            // Estados dinâmicos: só atualiza se não houver monitoramento/drop em andamento, para não confundir o usuário no meio de uma operação
            if (!_isMonitoring)
            {
                btnToggleWatch.Text = Strings.UpdateList_StartMonitoring;
                SetToolbarIcon(btnToggleWatch, UpdateToolbarIconKind.MonitorStart);
                lblWatchStatus.Text = Strings.UpdateList_INACTIVE;
            }
            else
            {
                btnToggleWatch.Text = Strings.UpdateList_StopMonitoring;
                SetToolbarIcon(btnToggleWatch, UpdateToolbarIconKind.MonitorStop);
                lblWatchStatus.Text = Strings.UpdateList_ACTIVELYMONITORING;
            }

            if (string.IsNullOrEmpty(txtXmlViewer.Text))
            {
                lblDropHint.Text = Strings.UpdateList_DragAndDropAnEncryptedUpdatelist;
            }
        }

private void SetupComponents()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Aba 1 — Drag-and-Drop
            // pnlCryptoDrop.AllowDrop = true;
            // pnlCryptoDrop.DragEnter += PnlCryptoDrop_DragEnter;
            // pnlCryptoDrop.DragDrop += pnlCryptoDrop_DragDrop;

            // Aba 2 — ComboBox de região
            cboFileKey.Items.Clear();
            foreach (var (label, _) in UpdateKeys.All)
                cboFileKey.Items.Add(label);
            cboFileKey.SelectedIndex = 0;

            // Defaults dos campos de versão
            txtPatchVersion.Text = "JP.R7.983.00";
            txtUpdateListVer.Text = DateTime.Now.ToString("yyyyMMdd01");
            txtClientPatchNum.Text = "1";

            Log(Strings.UpdateList_InterfaceInitializedInMultiTabMode);
        }

        private void RestorePreferences()
        {
            txtViewerFilePath.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.UpdateListViewerFile);
            txtVerificationRootPath.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.UpdateListVerificationRoot);
            txtPangyaPath.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.UpdateListSourceFolder);
            txtUpdatePath.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.UpdateListDestinationFolder);
            txtExistingList.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.UpdateListExistingFile);

            UpdateListGeneratorSettings? settings = UpdateListGeneratorPreferences.Load();
            if (settings is null) return;

            string? keyLabel = settings.KeyLabel?.Trim();
            if (!string.IsNullOrEmpty(keyLabel))
            {
                int selectedIndex = cboFileKey.Items.Cast<object>()
                    .Select((item, index) => (Label: cboFileKey.GetItemText(item), Index: index))
                    .FirstOrDefault(item => string.Equals(
                        item.Label, keyLabel, StringComparison.OrdinalIgnoreCase))
                    .Index;
                if (selectedIndex >= 0 &&
                    string.Equals(cboFileKey.GetItemText(cboFileKey.Items[selectedIndex]),
                        keyLabel, StringComparison.OrdinalIgnoreCase))
                {
                    cboFileKey.SelectedIndex = selectedIndex;
                }
            }

            RestoreTextPreference(txtPatchVersion, settings.PatchVersion);
            RestoreTextPreference(txtUpdateListVer, settings.UpdateListVersion);
            RestoreTextPreference(txtClientPatchNum, settings.PatchNumber);
            chkCreateZipPackages.Checked = settings.CreateZipPackages;
        }

        private static void RestoreTextPreference(TextBox textBox, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) textBox.Text = value.Trim();
        }

        private void FrmUpdateList_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _verificationCancellation?.Cancel();
            PathTextBoxPreferences.SavePaths(new Dictionary<PathTextBoxKind, string?>
            {
                [PathTextBoxKind.UpdateListViewerFile] = txtViewerFilePath.Text,
                [PathTextBoxKind.UpdateListVerificationRoot] = txtVerificationRootPath.Text,
                [PathTextBoxKind.UpdateListSourceFolder] = txtPangyaPath.Text,
                [PathTextBoxKind.UpdateListDestinationFolder] = txtUpdatePath.Text,
                [PathTextBoxKind.UpdateListExistingFile] = txtExistingList.Text
            });
            UpdateListGeneratorPreferences.Save(new UpdateListGeneratorSettings(
                cboFileKey.SelectedItem?.ToString(),
                txtPatchVersion.Text,
                txtUpdateListVer.Text,
                txtClientPatchNum.Text,
                chkCreateZipPackages.Checked));
        }

        private void ConfigureToolbars()
        {
            SetToolbarIcon(btnGenerateNow, UpdateToolbarIconKind.Generate);
            SetToolbarIcon(btnToggleWatch, UpdateToolbarIconKind.MonitorStart);
            SetToolbarIcon(btnShowRawXml, UpdateToolbarIconKind.RawXml);
            SetToolbarIcon(btnVerifyFiles, UpdateToolbarIconKind.Verify);
            SetToolbarIcon(btnCancelVerification, UpdateToolbarIconKind.Cancel);
            foreach (ToolStripButton button in new[] { btnGenerateNow, btnToggleWatch, btnShowRawXml })
            {
                button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                button.TextImageRelation = TextImageRelation.ImageAboveText;
                button.Size = new Size(92, 62);
            }

            foreach (ToolStripButton button in new[] { btnShowRawXml, btnVerifyFiles, btnCancelVerification })
            {
                button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                button.TextImageRelation = TextImageRelation.ImageAboveText;
                button.Size = new Size(92, 62);
            }

            AutoSizeUpdateFileColumns();
            UpdateVerifyButtonState();
        }

        private static void SetToolbarIcon(ToolStripButton button, UpdateToolbarIconKind iconKind)
        {
            Image? oldImage = button.Image;
            button.Image = CreateToolbarIcon(iconKind);
            oldImage?.Dispose();
        }

        private static Bitmap CreateToolbarIcon(UpdateToolbarIconKind iconKind)
        {
            var bitmap = new Bitmap(32, 32);
            using Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var primary = new SolidBrush(Color.FromArgb(0, 122, 204));
            using var green = new SolidBrush(Color.FromArgb(40, 167, 69));
            using var red = new SolidBrush(Color.FromArgb(220, 53, 69));
            using var white = new SolidBrush(Color.White);
            switch (iconKind)
            {
                case UpdateToolbarIconKind.Generate:
                    g.FillRectangle(primary, 7, 4, 18, 24);
                    g.FillRectangle(white, 11, 10, 10, 2);
                    g.FillRectangle(white, 11, 15, 10, 2);
                    g.FillRectangle(white, 11, 20, 7, 2);
                    break;
                case UpdateToolbarIconKind.MonitorStart:
                    g.FillEllipse(green, 5, 5, 22, 22);
                    g.FillPolygon(white, [new Point(14, 11), new Point(14, 21), new Point(23, 16)]);
                    break;
                case UpdateToolbarIconKind.MonitorStop:
                    g.FillEllipse(red, 5, 5, 22, 22);
                    g.FillRectangle(white, 12, 12, 8, 8);
                    break;
                case UpdateToolbarIconKind.RawXml:
                    g.FillRectangle(primary, 5, 7, 22, 18);
                    using (var font = new Font("Consolas", 8F, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        g.DrawString("</>", font, white, new PointF(8, 13));
                    }
                    break;
                case UpdateToolbarIconKind.Verify:
                    g.FillEllipse(green, 5, 5, 22, 22);
                    using (var checkPen = new Pen(Color.White, 3F))
                    {
                        g.DrawLines(checkPen, [new Point(10, 16), new Point(15, 21), new Point(23, 11)]);
                    }
                    break;
                case UpdateToolbarIconKind.Cancel:
                    g.FillEllipse(red, 5, 5, 22, 22);
                    using (var cancelPen = new Pen(Color.White, 3F))
                    {
                        g.DrawLine(cancelPen, 11, 11, 21, 21);
                        g.DrawLine(cancelPen, 21, 11, 11, 21);
                    }
                    break;
            }

            return bitmap;
        }

        // ════════════════════════════════════════════════════════════════════
        // ABA 1 — VISUALIZADOR / DECRYPT DE UPDATELIST
        // ════════════════════════════════════════════════════════════════════

        // private void PnlCryptoDrop_DragEnter(object? sender, DragEventArgs e)
        // {
        //     if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
        //         e.Effect = DragDropEffects.Copy;
        // }

        // private async void pnlCryptoDrop_DragDrop(object? sender, DragEventArgs e)
        // {
        //     if (e.Data == null) return;
        //     string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        //     if (files.Length == 0) return;

        //     await ProcessViewerFileAsync(files[0]);
        // }

        private async void btnBrowseViewer_Click(object? sender, EventArgs e)
        {
            using var ofd = FileDialogFactory.CreateUpdateListOpenDialog();

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                FileDialogFactory.RememberDirectory(FileDialogKind.UpdateList, ofd.FileName);
                await ProcessViewerFileAsync(ofd.FileName);
            }
        }

        private async Task ProcessViewerFileAsync(string targetFile)
        {
            _verificationCancellation?.Cancel();
            txtViewerFilePath.Text = targetFile;
            txtXmlViewer.Clear();
            _currentRawXml = string.Empty;
            _currentUpdateListDocument = null;
            ResetVerificationResults();
            UpdatePatchSummary(null);
            lblDropHint.Text = $"{Strings.UpdateList_Processing} {Path.GetFileName(targetFile)}...";

            string selectedKeyName = string.Empty;
            this.Invoke(() => selectedKeyName = cboFileKey.SelectedItem!.ToString()!);

            await Task.Run(() =>
            {
                try
                {
                    var operacao = UpdateKeyDetector.IsFileCrypt(targetFile);

                    if (operacao == OperacaoEnum.Decrypt)
                    {
                        this.Invoke(() => Log($"🔒 {Strings.UpdateList_ProtectedFileDetectedTestingKey} [{selectedKeyName}]..."));

                        uint[] selectedKey = GetKeysByLabel(selectedKeyName);
                        var reader = new UpdateReader(selectedKey);

                        try
                        {
                            var (header, entries) = reader.ReadUpdateList(targetFile);
                            if (entries != null && entries.Count > 0)
                            {
                                byte[] rawDoc = reader.XteaDecrypt(targetFile);
                                int num = Array.IndexOf(rawDoc, (byte)0);
                                string xmlText = Encoding.GetEncoding("euc-kr").GetString(rawDoc, 0, num == -1 ? rawDoc.Length : num);

                                // Embeleza o XML antes de mandar para a tela
                                string formattedXml = FormatXml(xmlText);

                                this.Invoke(() => {
                                    DisplayUpdateListXml(xmlText, formattedXml);
                                    lblDropHint.Text = Strings.UpdateList_DragAndDropAnEncryptedUpdatelist;
                                    Log($"✅ [{Strings.UpdateList_SUCCESS}] {Strings.UpdateList_DecryptedWithKey} {selectedKeyName}!");
                                });
                                return;
                            }
                        }
                        catch
                        {
                            this.Invoke(() => Log($"⚠️ {Strings.UpdateList_FailedWithKey} [{selectedKeyName}]. {Strings.UpdateList_StartingAutomaticBruteForceScanner}"));
                        }

                        // Fallback: testa todas as chaves conhecidas
                        var result = UpdateKeyDetector.DetectAndSetKey(targetFile, out _, out byte[]? decryptedData, out _);
                        if (result == UpdateResult.Sucess && decryptedData != null)
                        {
                            int num = Array.IndexOf(decryptedData, (byte)0);
                            string xmlText = Encoding.GetEncoding("euc-kr").GetString(decryptedData, 0, num == -1 ? decryptedData.Length : num);

                            string formattedXml = FormatXml(xmlText);

                            this.Invoke(() => {
                                DisplayUpdateListXml(xmlText, formattedXml);
                                lblDropHint.Text = Strings.UpdateList_DragAndDropAnEncryptedUpdatelist;
                                Log($"✅ [{Strings.UpdateList_BRUTEFORCESUCCESS}] {Strings.UpdateList_KeyIdentifiedSuccessfully}");
                            });
                        }
                        else
                        {
                            this.Invoke(() => {
                                lblDropHint.Text = Strings.UpdateList_ErrorNoKeyDecodedTheStructure;
                                Log($"❌ [{Strings.UpdateList_TOTALFAILURE}] {Strings.UpdateList_NoKeyFromTheKnownDatabase}");
                            });
                        }
                    }
                    else if (operacao == OperacaoEnum.Encrypt)
                    {
                        // Arquivo já está em texto puro
                        string xmlText = File.ReadAllText(targetFile, Encoding.GetEncoding("euc-kr"));
                        string formattedXml = FormatXml(xmlText);

                        this.Invoke(() => {
                            DisplayUpdateListXml(xmlText, formattedXml);
                            lblDropHint.Text = Strings.UpdateList_DragAndDropAnEncryptedUpdatelist;
                            Log($"📋 {Strings.UpdateList_TheDroppedFileIsAlreadyIn}");
                        });
                    }
                    else
                    {
                        this.Invoke(() => {
                            lblDropHint.Text = Strings.UpdateList_InvalidOrCorruptedFile;
                            Log(Strings.UpdateList_InvalidOrCorruptedFile);
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(() => {
                        lblDropHint.Text = Strings.UpdateList_CriticalFailureWhileParsingFile;
                        Log($"❌ [{Strings.UpdateList_PARSEERROR}] {ex.Message}");
                    });
                }
            });
        }

        private void ShowDecryptedXml(byte[] rawDoc, string keyLabel)
        {
            int nullIdx = Array.IndexOf(rawDoc, (byte)0);
            string xmlText = Encoding.GetEncoding("euc-kr").GetString(rawDoc, 0, nullIdx == -1 ? rawDoc.Length : nullIdx);
            this.Invoke(() =>
            {
                DisplayUpdateListXml(xmlText, FormatXml(xmlText));
                lblDropHint.Text = Strings.UpdateList_DragAndDropAnEncryptedUpdatelist;
                Log(string.Format(LocalizationManager.CurrentCulture,
                    Strings.UpdateList_DecryptionSuccessFormat, keyLabel));
            });
        }

        private void DisplayUpdateListXml(string rawXml, string formattedXml)
        {
            _currentRawXml = formattedXml;
            txtXmlViewer.Text = formattedXml;
            UpdateListDocument document = ParseUpdateListXml(rawXml);
            _currentUpdateListDocument = document;
            ResetVerificationResults(repopulate: false);
            PopulateUpdateFileList(document);
            UpdatePatchSummary(document);
            UpdateVerifyButtonState();
        }

        private static UpdateListDocument ParseUpdateListXml(string xmlText)
        {
            string trimmed = xmlText.Trim('\uFEFF', '\0', ' ', '\r', '\n', '\t');
            if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                int endDeclaration = trimmed.IndexOf("?>", StringComparison.Ordinal);
                if (endDeclaration >= 0)
                    trimmed = trimmed[(endDeclaration + 2)..].TrimStart();
            }

            XDocument document = XDocument.Parse($"<root>{trimmed}</root>", LoadOptions.PreserveWhitespace);
            XElement root = document.Root ?? throw new InvalidDataException(Strings.UpdateList_InvalidOrCorruptedFile);

            string patchVersion = root.Element("patchVer")?.Attribute("value")?.Value ?? string.Empty;
            string patchNumber = root.Element("patchNum")?.Attribute("value")?.Value ?? string.Empty;
            string updateListVersion = root.Element("updatelistVer")?.Attribute("value")?.Value ?? string.Empty;
            XElement? updateFiles = root.Element("updatefiles");
            int declaredCount = int.TryParse(updateFiles?.Attribute("count")?.Value, out int count) ? count : 0;

            List<UpdateListFileInfo> files = updateFiles?
                .Elements("fileinfo")
                .Select(file => new UpdateListFileInfo(
                    file.Attribute("fname")?.Value ?? string.Empty,
                    file.Attribute("fdir")?.Value ?? string.Empty,
                    file.Attribute("fsize")?.Value ?? string.Empty,
                    file.Attribute("fcrc")?.Value ?? string.Empty,
                    file.Attribute("fdate")?.Value ?? string.Empty,
                    file.Attribute("ftime")?.Value ?? string.Empty,
                    file.Attribute("pname")?.Value ?? string.Empty,
                    file.Attribute("psize")?.Value ?? string.Empty))
                .ToList() ?? [];

            return new UpdateListDocument(patchVersion, patchNumber, updateListVersion, declaredCount, files);
        }

        private void PopulateUpdateFileList(UpdateListDocument document)
        {
            lstUpdateFiles.BeginUpdate();
            try
            {
                lstUpdateFiles.Items.Clear();
                for (int index = 0; index < document.Files.Count; index++)
                {
                    UpdateListFileInfo file = document.Files[index];
                    UpdateFileVerificationResult? result = index < _verificationResults.Count
                        ? _verificationResults[index]
                        : null;
                    if (cboVerificationFilter.SelectedIndex == 1
                        && result?.Status == UpdateFileVerificationStatus.Match)
                    {
                        continue;
                    }

                    var item = new ListViewItem(file.FileName);
                    item.Tag = index;
                    item.SubItems.Add(file.Directory);
                    item.SubItems.Add(file.FileSize);
                    item.SubItems.Add(FormatCrcForDisplay(file.Crc));
                    item.SubItems.Add(result?.ActualCrc is int actualCrc
                        ? FormatCrcForDisplay(actualCrc.ToString(CultureInfo.InvariantCulture))
                        : string.Empty);
                    item.SubItems.Add(result is null ? string.Empty : GetVerificationStatusText(result.Status));
                    item.SubItems.Add(file.Date);
                    item.SubItems.Add(file.Time);
                    item.SubItems.Add(file.PackageName);
                    item.SubItems.Add(file.PackageSize);
                    ApplyVerificationStyle(item, result);
                    lstUpdateFiles.Items.Add(item);
                }
            }
            finally
            {
                lstUpdateFiles.EndUpdate();
            }

            AutoSizeUpdateFileColumns();
        }

        internal static string FormatCrcForDisplay(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int crc)
                ? $"0x{unchecked((uint)crc):X8}"
                : value;
        }

        private void AutoSizeUpdateFileColumns()
        {
            foreach (ColumnHeader column in lstUpdateFiles.Columns)
                column.Width = -2;
        }

        private void UpdatePatchSummary(UpdateListDocument? document)
        {
            if (document == null)
            {
                lblUpdateListSummary.Text = string.Empty;
                return;
            }

            string patchSummary = string.Format(
                LocalizationManager.CurrentCulture,
                Strings.UpdateList_StatusSummary,
                document.PatchVersion,
                document.PatchNumber,
                document.UpdateListVersion,
                document.Files.Count,
                document.DeclaredCount);
            lblUpdateListSummary.Text = _verificationResults.Count == 0
                ? patchSummary
                : $"{patchSummary} | {BuildVerificationSummary(_verificationResults)}";
        }

        private async void btnVerifyFiles_Click(object? sender, EventArgs e)
        {
            UpdateListDocument? document = _currentUpdateListDocument;
            string rootPath = txtVerificationRootPath.Text;
            if (document is null || !Directory.Exists(rootPath))
            {
                MessageBox.Show(this, Strings.UpdateList_InvalidVerificationRoot,
                    Strings.UpdateList_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _verificationCancellation?.Dispose();
            var cancellation = new CancellationTokenSource();
            _verificationCancellation = cancellation;
            ResetVerificationResults();
            SetVerificationRunning(true, document.Files.Count);
            Log(string.Format(LocalizationManager.CurrentCulture,
                Strings.UpdateList_VerificationStartedFormat, rootPath, document.Files.Count));

            try
            {
                List<UpdateEntry> entries = document.Files.Select(ToUpdateEntry).ToList();
                IReadOnlyList<UpdateFileVerificationResult> results = await Task.Run(() =>
                    new UpdateListVerifier().VerifyDirectory(
                        rootPath,
                        entries,
                        (completed, total, _) => ReportVerificationProgress(completed, total),
                        cancellation.Token),
                    cancellation.Token);

                _verificationResults = results;
                PopulateUpdateFileList(document);
                UpdatePatchSummary(document);
                Log(BuildVerificationSummary(results));
                foreach (UpdateFileVerificationResult result in results.Where(
                             result => result.Status != UpdateFileVerificationStatus.Match))
                {
                    Log(string.Format(LocalizationManager.CurrentCulture,
                        Strings.UpdateList_VerificationIssueFormat,
                        result.Entry.fname,
                        GetVerificationStatusText(result.Status),
                        result.ErrorMessage ?? string.Empty));
                }
            }
            catch (OperationCanceledException)
            {
                Log(Strings.UpdateList_VerificationCancelled);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log(string.Format(LocalizationManager.CurrentCulture,
                    Strings.UpdateList_VerificationFailedFormat, ex.Message));
                MessageBox.Show(this, ex.Message, Strings.Common_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (ReferenceEquals(_verificationCancellation, cancellation))
                {
                    _verificationCancellation = null;
                    cancellation.Dispose();
                }
                SetVerificationRunning(false, document.Files.Count);
            }
        }

        private void btnCancelVerification_Click(object? sender, EventArgs e) =>
            _verificationCancellation?.Cancel();

        private void btnBrowseVerificationRoot_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = Strings.UpdateList_SelectVerificationRoot,
                SelectedPath = Directory.Exists(txtVerificationRootPath.Text)
                    ? txtVerificationRootPath.Text
                    : string.Empty
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            txtVerificationRootPath.Text = dialog.SelectedPath;
            PathTextBoxPreferences.SavePath(PathTextBoxKind.UpdateListVerificationRoot, dialog.SelectedPath);
            ResetVerificationResults();
            UpdateVerifyButtonState();
        }

        private void cboVerificationFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_currentUpdateListDocument is not null)
                PopulateUpdateFileList(_currentUpdateListDocument);
        }

        private void ResetVerificationResults(bool repopulate = true)
        {
            _verificationResults = [];
            viewerProgressBar.Value = 0;
            viewerProgressBar.Visible = false;
            if (repopulate)
            {
                if (_currentUpdateListDocument is null) lstUpdateFiles.Items.Clear();
                else PopulateUpdateFileList(_currentUpdateListDocument);
                UpdatePatchSummary(_currentUpdateListDocument);
            }
            UpdateVerifyButtonState();
        }

        private void SetVerificationRunning(bool running, int total)
        {
            viewerProgressBar.Visible = running;
            viewerProgressBar.Minimum = 0;
            viewerProgressBar.Maximum = Math.Max(1, total);
            if (running) viewerProgressBar.Value = 0;
            btnCancelVerification.Enabled = running;
            btnBrowseViewer.Enabled = !running;
            btnBrowseVerificationRoot.Enabled = !running;
            if (running) btnVerifyFiles.Enabled = false;
            else UpdateVerifyButtonState();
        }

        private void UpdateVerifyButtonState()
        {
            btnVerifyFiles.Enabled = _verificationCancellation is null
                                     && _currentUpdateListDocument is not null
                                     && Directory.Exists(txtVerificationRootPath.Text);
        }

        private void ReportVerificationProgress(int completed, int total)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            BeginInvoke(() =>
            {
                if (IsDisposed || viewerProgressBar.IsDisposed) return;
                viewerProgressBar.Maximum = Math.Max(1, total);
                viewerProgressBar.Value = Math.Min(completed, viewerProgressBar.Maximum);
                viewerProgressBar.ToolTipText = string.Format(
                    LocalizationManager.CurrentCulture,
                    Strings.UpdateList_VerificationProgressFormat,
                    completed,
                    total);
            });
        }

        private static UpdateEntry ToUpdateEntry(UpdateListFileInfo file) => new()
        {
            fname = file.FileName,
            fdir = file.Directory,
            fsize = long.TryParse(file.FileSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size) ? size : 0,
            fcrc = int.TryParse(file.Crc, NumberStyles.Integer, CultureInfo.InvariantCulture, out int crc) ? crc : 0,
            fdate = file.Date,
            ftime = file.Time,
            pname = file.PackageName,
            psize = int.TryParse(file.PackageSize, NumberStyles.Integer, CultureInfo.InvariantCulture, out int packageSize)
                ? packageSize
                : 0
        };

        private static void ApplyVerificationStyle(
            ListViewItem item,
            UpdateFileVerificationResult? result)
        {
            if (result is null) return;
            item.ToolTipText = result.ErrorMessage ?? string.Empty;
            item.BackColor = result.Status switch
            {
                UpdateFileVerificationStatus.Match => Color.Honeydew,
                UpdateFileVerificationStatus.ChecksumMismatch => Color.MistyRose,
                UpdateFileVerificationStatus.Missing => Color.LemonChiffon,
                UpdateFileVerificationStatus.InvalidPath => Color.LightSalmon,
                UpdateFileVerificationStatus.ReadError => Color.LightSalmon,
                _ => SystemColors.Window
            };
        }

        private static string GetVerificationStatusText(UpdateFileVerificationStatus status) => status switch
        {
            UpdateFileVerificationStatus.Match => Strings.UpdateList_VerificationMatch,
            UpdateFileVerificationStatus.ChecksumMismatch => Strings.UpdateList_VerificationMismatch,
            UpdateFileVerificationStatus.Missing => Strings.UpdateList_VerificationMissing,
            UpdateFileVerificationStatus.InvalidPath => Strings.UpdateList_VerificationInvalidPath,
            UpdateFileVerificationStatus.ReadError => Strings.UpdateList_VerificationReadError,
            _ => status.ToString()
        };

        private static string BuildVerificationSummary(IReadOnlyList<UpdateFileVerificationResult> results) =>
            string.Format(
                LocalizationManager.CurrentCulture,
                Strings.UpdateList_VerificationSummaryFormat,
                results.Count(result => result.Status == UpdateFileVerificationStatus.Match),
                results.Count(result => result.Status == UpdateFileVerificationStatus.ChecksumMismatch),
                results.Count(result => result.Status == UpdateFileVerificationStatus.Missing),
                results.Count(result => result.Status == UpdateFileVerificationStatus.InvalidPath),
                results.Count(result => result.Status == UpdateFileVerificationStatus.ReadError));

        private void btnShowRawXml_Click(object? sender, EventArgs e)
        {
            using var dialog = new Form
            {
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(760, 520),
                StartPosition = FormStartPosition.CenterParent,
                Text = Strings.UpdateList_RawXml
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.75F),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Text = string.IsNullOrWhiteSpace(_currentRawXml)
                    ? Strings.UpdateList_NoRawXmlLoaded
                    : _currentRawXml,
                WordWrap = false
            };

            dialog.Controls.Add(textBox);
            dialog.ShowDialog(this);
        }

        private sealed record UpdateListDocument(
            string PatchVersion,
            string PatchNumber,
            string UpdateListVersion,
            int DeclaredCount,
            IReadOnlyList<UpdateListFileInfo> Files);

        private sealed record UpdateListFileInfo(
            string FileName,
            string Directory,
            string FileSize,
            string Crc,
            string Date,
            string Time,
            string PackageName,
            string PackageSize);

        // ════════════════════════════════════════════════════════════════════
        // ABA 2 — GERADOR & MONITOR DE UPDATELIST
        // ════════════════════════════════════════════════════════════════════

        // ── Browse buttons ───────────────────────────────────────────────────
        private void btnBrowsePangya_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog { Description = Strings.UpdateList_SelectTheRootPangyaFolderWhere };
            if (fbd.ShowDialog() == DialogResult.OK) txtPangyaPath.Text = fbd.SelectedPath;
        }

        private void btnBrowseUpdate_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog { Description = Strings.UpdateList_SelectTheDestinationWebServerFolderFor };
            if (fbd.ShowDialog() == DialogResult.OK) txtUpdatePath.Text = fbd.SelectedPath;
        }

        private void btnBrowseExisting_Click(object sender, EventArgs e)
        {
            using var ofd = FileDialogFactory.CreateExistingUpdateListOpenDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FileDialogFactory.RememberDirectory(FileDialogKind.ExistingUpdateList, ofd.FileName);
                txtExistingList.Text = ofd.FileName;
            }
        }

        // ── GERAR AGORA (equivalente ao BtnStart_Click do sistema antigo) ───
        private async void btnGenerateNow_Click(object sender, EventArgs e)
        {
            if (!ValidatePaths()) return;

            btnGenerateNow.Enabled = false;
            btnToggleWatch.Enabled = false;
            progressBar.Value = 0;
            progressBar.Visible = true;
            lblStatus.Text = Strings.UpdateList_Scanning;

            try
            {
                await RunGenerationAsync(isMonitoringTrigger: false);
                lblStatus.Text = Strings.UpdateList_Done;
            }
            catch (Exception ex)
            {
                Log(string.Format(LocalizationManager.CurrentCulture,
                    Strings.UpdateList_ErrorLogFormat, ex.Message));
                lblStatus.Text = Strings.UpdateList_ErrorStatus;
                MessageBox.Show(
                    this,
                    ex.Message,
                    Strings.UpdateList_ErrorStatus,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
                btnGenerateNow.Enabled = true;
                btnToggleWatch.Enabled = true;
            }
        }

        // ── MONITORAMENTO ────────────────────────────────────────────────────
        private async void btnToggleWatch_Click(object sender, EventArgs e)
        {
            if (_isMonitoring) StopMonitoring();
            else await StartMonitoringAsync();
        }

        private async Task StartMonitoringAsync()
        {
            string pangyaPath = txtPangyaPath.Text;
            string destPath = txtUpdatePath.Text;

            if (!Directory.Exists(pangyaPath) || !Directory.Exists(destPath))
            {
                MessageBox.Show(Strings.UpdateList_CheckWhetherTheSourceAndWebServer, Strings.UpdateList_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnToggleWatch.Enabled = false;
            lblWatchStatus.Text = Strings.UpdateList_Initializing;

            try
            {
                await RunGenerationAsync(isMonitoringTrigger: false);

                _watcher = new FileSystemWatcher(pangyaPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };
                _watcher.Changed += OnFileChanged;
                _watcher.Created += OnFileChanged;
                _watcher.EnableRaisingEvents = true;

                _isMonitoring = true;
                btnToggleWatch.Text = Strings.UpdateList_StopMonitoring;
                SetToolbarIcon(btnToggleWatch, UpdateToolbarIconKind.MonitorStop);
                btnToggleWatch.BackColor = Color.Tomato;
                lblWatchStatus.Text = Strings.UpdateList_ACTIVELYMONITORING;
                lblWatchStatus.ForeColor = Color.Green;
                Log($"[{Strings.UpdateList_SERVICE}] {Strings.UpdateList_FileSystemWatcherActiveOnFolder} {pangyaPath}");
            }
            catch (Exception ex)
            {
                Log($"[{Strings.UpdateList_INITERROR}] {ex.Message}");
                MessageBox.Show(
                    this,
                    ex.Message,
                    Strings.UpdateList_ErrorStatus,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                StopMonitoring();
            }
            finally
            {
                progressBar.Visible = false;
                btnToggleWatch.Enabled = true;
            }
        }

        private void StopMonitoring()
        {
            _watcher?.Dispose();
            _watcher = null;

            _isMonitoring = false;
            btnToggleWatch.Text = Strings.UpdateList_StartMonitoring;
            SetToolbarIcon(btnToggleWatch, UpdateToolbarIconKind.MonitorStart);
            btnToggleWatch.BackColor = Color.LightGreen;
            lblWatchStatus.Text = Strings.UpdateList_INACTIVE;
            lblWatchStatus.ForeColor = Color.DimGray;
            Log(Strings.UpdateList_BackgroundMonitoringHasBeenStopped);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (ext != ".pak" && ext != ".exe" && ext != ".dll") return;

            lock (_generatorLock)
            {
                Thread.Sleep(1000); // Buffer para o Windows liberar o lock do arquivo

                if (!File.Exists(e.FullPath)) return;

                var info = new FileInfo(e.FullPath);
                var currentState = new FileStateApp { Length = info.Length, LastWriteTime = info.LastWriteTime };

                if (_fileCache.TryGetValue(e.FullPath, out var last) &&
                    last.Length == currentState.Length &&
                    last.LastWriteTime == currentState.LastWriteTime) return;

                this.Invoke(() => Log($"[{Strings.UpdateList_DETECTED}] {Strings.UpdateList_FileModification} {e.Name}"));

                string pangyaPath = string.Empty;
                string destPath = string.Empty;
                string selectedKeyName = string.Empty;
                string patchVersion = string.Empty;
                string updateVersion = string.Empty;
                string patchNum = string.Empty;

                this.Invoke(() => {
                    pangyaPath = txtPangyaPath.Text;
                    destPath = txtUpdatePath.Text;
                    selectedKeyName = cboFileKey.SelectedItem!.ToString()!;
                    patchVersion = txtPatchVersion.Text;
                    updateVersion = txtUpdateListVer.Text;
                    patchNum = txtClientPatchNum.Text;
                });

                try
                {
                    // Copia o arquivo modificado para a pasta de destino
                    string relative = Path.GetRelativePath(pangyaPath, e.FullPath);
                    string destFile = Path.Combine(destPath, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    File.Copy(e.FullPath, destFile, true);

                    // Regenera o updatelist completo refletindo a mudança
                    Task.Run(() => RunGenerationAsync(isMonitoringTrigger: true)).Wait();

                    this.Invoke(() => Log($"✨ [{Strings.UpdateList_COMPILED}] {Strings.UpdateList_UpdatelistSignedSuccessfullyTrigger} {e.Name}"));
                }
                catch (Exception ex)
                {
                    this.Invoke(() =>
                    {
                        Log($"[{Strings.UpdateList_IOERROR}] {Strings.UpdateList_CouldNotManageTheFile} {e.Name}: {ex.Message}");
                        MessageBox.Show(
                            this,
                            ex.Message,
                            Strings.UpdateList_ErrorStatus,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    });
                }
            }
        }

        // ── LÓGICA CENTRAL DE GERAÇÃO (inclui delta comparison) ─────────────
        /// <summary>
        /// Executa a varredura + geração do updatelist, com delta comparison
        /// contra um updatelist existente (se informado). Equivale ao fluxo
        /// do BtnStart_Click do sistema antigo: escaneia, compara CRC, loga
        /// o que mudou e gera o arquivo final criptografado.
        /// </summary>
        private async Task RunGenerationAsync(bool isMonitoringTrigger)
        {
            string pangyaPath, destPath, existingPath, keyLabel,
                   patchVersion, updateVersion, patchNum;

            this.Invoke(() =>
            {
                pangyaPath = txtPangyaPath.Text;
                destPath = txtUpdatePath.Text;
                existingPath = txtExistingList.Text;
                keyLabel = cboFileKey.SelectedItem!.ToString()!;
                patchVersion = txtPatchVersion.Text;
                updateVersion = txtUpdateListVer.Text;
                patchNum = txtClientPatchNum.Text;
            });

            // Captura local pra usar na Task (evita acesso cross-thread)
            string _pangyaPath = string.Empty;
            string _destPath = string.Empty;
            string _existingPath = string.Empty;
            string _keyLabel = string.Empty;
            string _patchVersion = string.Empty;
            string _updateVersion = string.Empty;
            string _patchNum = string.Empty;
            bool _createZipPackages = false;

            this.Invoke(() =>
            {
                _pangyaPath = txtPangyaPath.Text;
                _destPath = txtUpdatePath.Text;
                _existingPath = txtExistingList.Text;
                _keyLabel = cboFileKey.SelectedItem!.ToString()!;
                _patchVersion = txtPatchVersion.Text;
                _updateVersion = txtUpdateListVer.Text;
                _patchNum = txtClientPatchNum.Text;
                _createZipPackages = chkCreateZipPackages.Checked;
            });

            uint[] regionKeys = GetKeysByLabel(_keyLabel);
            string outputPath = Path.Combine(_destPath, "updatelist");
            int totalFiles = 0;

            // Conta arquivos para a barra de progresso
            await Task.Run(() =>
            {
                totalFiles = Directory.EnumerateFiles(_pangyaPath, "*", SearchOption.AllDirectories).Count();
            });

            this.Invoke(() =>
            {
                progressBar.Maximum = Math.Max(totalFiles, 1);
                progressBar.Value = 0;
                Log("─────────────────────────────────────────────────────");
                Log(string.Format(LocalizationManager.CurrentCulture, Strings.UpdateList_SourceLogFormat, _pangyaPath));
                Log(string.Format(LocalizationManager.CurrentCulture, Strings.UpdateList_DestinationLogFormat, outputPath));
                Log(string.Format(LocalizationManager.CurrentCulture, Strings.UpdateList_KeyLogFormat, _keyLabel));
                Log(string.Format(LocalizationManager.CurrentCulture, Strings.UpdateList_VersionLogFormat, _patchVersion, _patchNum));
                Log(Strings.UpdateList_ScanningFiles);
                if (_createZipPackages) Log(Strings.UpdateList_PackagingFiles);
            });

            // ── Carrega updatelist existente para delta comparison ────────────
            List<UpdateEntry>? existingEntries = null;
            if (!string.IsNullOrEmpty(_existingPath) && File.Exists(_existingPath))
            {
                try
                {
                    existingEntries = await Task.Run(() =>
                    {
                        var detectedKey = regionKeys;
                        var result = UpdateKeyDetector.DetectAndSetKey(
                            _existingPath, out uint[]? autoKey, out _, out _);
                        if (result == UpdateResult.Sucess && autoKey != null)
                            detectedKey = autoKey;

                        var reader = new UpdateReader(detectedKey);
                        return reader.ReadUpdateList(_existingPath).Entries;
                    });
                    this.Invoke(() => Log(string.Format(LocalizationManager.CurrentCulture,
                        Strings.UpdateList_ExistingLoadedFormat, existingEntries.Count)));
                }
                catch (Exception ex)
                {
                    this.Invoke(() => Log(string.Format(LocalizationManager.CurrentCulture,
                        Strings.UpdateList_ExistingLoadFailedFormat, ex.Message)));
                }
            }

            // ── Geração + progress bar ────────────────────────────────────────
            _updateMaker = new UpdateMaker();

            await Task.Run(() =>
            {
                _updateMaker.GenerateFromDirectory(
                    _pangyaPath,
                    outputPath,
                    regionKeys,
                    _patchVersion,
                    _updateVersion,
                    _patchNum,
                    onProgress: (done, total) =>
                    {
                        this.Invoke(() =>
                        {
                            progressBar.Maximum = Math.Max(total, 1);
                            progressBar.Value = Math.Min(done, total);
                            int fileCount = _createZipPackages ? total / 2 : total;
                            lblStatus.Text = _createZipPackages && done > fileCount
                                ? string.Format(LocalizationManager.CurrentCulture,
                                    Strings.UpdateList_PackagingProgressFormat,
                                    done - fileCount, fileCount)
                                : string.Format(LocalizationManager.CurrentCulture,
                                    Strings.UpdateList_ScanningProgressFormat, done, fileCount);
                        });
                    },
                    createZipPackages: _createZipPackages,
                    onFileProcessing: relativePath =>
                    {
                        this.Invoke(() => Log(string.Format(
                            LocalizationManager.CurrentCulture,
                            Strings.UpdateList_ProcessingFileFormat,
                            relativePath)));
                    }
                );
            });

            List<UpdateEntry> scanned = await Task.Run(() =>
                new UpdateReader(regionKeys).ReadUpdateList(outputPath).Entries);

            if (_createZipPackages)
            {
                this.Invoke(() => Log(string.Format(
                    LocalizationManager.CurrentCulture,
                    Strings.UpdateList_PackagingSummaryFormat,
                    scanned.Count)));
            }

            // ── Delta comparison — igual ao Update() do sistema antigo ────────
            if (existingEntries != null && existingEntries.Count > 0)
            {
                var existingByCrc = existingEntries.ToDictionary(
                    e => e.fname.ToLowerInvariant(),
                    e => e.fcrc);

                int newFiles = 0;
                int changedFiles = 0;
                int unchangedFiles = 0;

                foreach (var entry in scanned)
                {
                    string key = entry.fname.ToLowerInvariant();
                    if (!existingByCrc.TryGetValue(key, out int existingCrc))
                    {
                        newFiles++;
                        this.Invoke(() => Log(string.Format(LocalizationManager.CurrentCulture,
                            Strings.UpdateList_NewFileFormat, entry.fname)));
                    }
                    else if (existingCrc != entry.fcrc)
                    {
                        changedFiles++;
                        this.Invoke(() => Log(string.Format(LocalizationManager.CurrentCulture,
                            Strings.UpdateList_ChangedFileFormat, entry.fname, existingCrc, entry.fcrc)));
                    }
                    else
                    {
                        unchangedFiles++;
                    }
                }

                this.Invoke(() =>
                {
                    Log("─────────────────────────────────────────────────────");
                    Log(string.Format(LocalizationManager.CurrentCulture,
                        Strings.UpdateList_DeltaSummaryFormat, newFiles, changedFiles, unchangedFiles));
                });
            }

            this.Invoke(() =>
            {
                Log(string.Format(LocalizationManager.CurrentCulture,
                    Strings.UpdateList_GeneratedAtFormat, outputPath));
                Log("─────────────────────────────────────────────────────");
            });
        }

        // ── Validação de campos ──────────────────────────────────────────────
        private bool ValidatePaths()
        {
            if (!Directory.Exists(txtPangyaPath.Text))
            {
                MessageBox.Show(
                    Strings.UpdateList_InvalidSourceFolder,
                    Strings.UpdateList_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!Directory.Exists(txtUpdatePath.Text))
            {
                MessageBox.Show(
                    Strings.UpdateList_InvalidDestinationFolder,
                    Strings.UpdateList_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtPatchVersion.Text))
            {
                MessageBox.Show(
                    Strings.UpdateList_PatchVersionRequired,
                    Strings.UpdateList_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // ════════════════════════════════════════════════════════════════════
        // MÉTODOS AUXILIARES
        // ════════════════════════════════════════════════════════════════════

        private uint[] GetKeysByLabel(string label) =>
            UpdateKeys.All.FirstOrDefault(k => string.Equals(k.Label, label, StringComparison.OrdinalIgnoreCase)).Keys
            ?? UpdateKeys.JP;

        private void Log(string text)
        {
            AppLogger.Instance.Log("Update List", text);
            string line = $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}";
            if (InvokeRequired) this.Invoke(() => AppendLog(line));
            else AppendLog(line);
        }

        private void AppendLog(string line)
        {
            txtLog.AppendText(line);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private static string FormatXml(string rawXml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawXml)) return string.Empty;
                rawXml = rawXml.Trim();
                var doc = System.Xml.Linq.XDocument.Parse(rawXml);
                var settings = new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "    ",
                    NewLineOnAttributes = false,
                    OmitXmlDeclaration = false,
                    NewLineHandling = System.Xml.NewLineHandling.Replace,
                    NewLineChars = "\r\n"
                };
                using var sw = new StringWriter();
                using (var xw = System.Xml.XmlWriter.Create(sw, settings))
                    doc.Save(xw);
                return sw.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            }
            catch
            {
                return rawXml.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            }
        }
    }

    public class FileStateApp
    {
        public long Length { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
