using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using System.Text;
using System.Text.Json;
using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;

namespace PangYa_Suite_Tools
{
    public partial class FrmPakDiff : Form
    {
        private List<FileDiffEntry> _comparisonResults = new();

        // ── Snapshot state ───────────────────────────────────────────────────
        private PakSnapshot? _snapshotA;
        private PakSnapshot? _snapshotB;
        private bool _isInitializingLanguages = true;

        public FrmPakDiff(string? currentLanguage = null)
        {
            if (!string.IsNullOrWhiteSpace(currentLanguage))
                LocalizationManager.SetCulture(currentLanguage);
            InitializeComponent();
            InitializeLanguageComboBox();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            Disposed += (_, _) => LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
        }

        private void InitializeLanguageComboBox()
        {
            cboLanguage.ComboBox.DisplayMember = "Key";
            cboLanguage.ComboBox.ValueMember = "Value";
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_PortugueseBrazil, LocalizationManager.PortugueseBrazil));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_EnglishUS, LocalizationManager.English));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Swedish, LocalizationManager.Swedish));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Japonese, LocalizationManager.Japonese));
			cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_French, LocalizationManager.French));
            cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;
            _isInitializingLanguages = false;
            ApplyLocalization();
        }

        private void cboLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializingLanguages) return;
            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selected)
                LocalizationManager.SetCulture(selected.Value);
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
        {
            _isInitializingLanguages = true;
            cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;
            _isInitializingLanguages = false;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Text = Strings.PakDiff_Title;
            tabLog.Text = Strings.PakDiff_TabLog;
            grpSnapshotA.Text = Strings.PakDiff_SnapshotABefore;
            grpSnapshotB.Text = Strings.PakDiff_SnapshotBAfter;
            lblSnapshotAPath.Text = lblSnapshotBPath.Text = Strings.PakDiff_PakFolder;
            btnBrowseSnapshotA.Text = btnBrowseSnapshotB.Text = Strings.PakDiff_Browse;
            btnTakeSnapshotA.Text = btnTakeSnapshotB.Text = Strings.PakDiff_TakeSnapshot;
            btnSaveSnapshotA.Text = btnSaveSnapshotB.Text = Strings.PakDiff_Save;
            btnLoadSnapshotA.Text = btnLoadSnapshotB.Text = Strings.PakDiff_Load;
            if (_snapshotA == null) lblSnapshotAStatus.Text = Strings.PakDiff_NotTaken;
            if (_snapshotB == null) lblSnapshotBStatus.Text = Strings.PakDiff_NotTaken;
            btnCompareSnapshots.Text = Strings.PakDiff_CompareSnapshots;
            chkLogSelectAll.Text = chkSelectAll.Text = Strings.PakDiff_SelectAll;
            colLogPak.Text = Strings.PakDiff_ColumnPakFile;
            colLogFile.Text = Strings.PakDiff_ColumnInternalFile;
            colLogStatus.Text = colStatus.Text = Strings.PakDiff_ColumnStatus;
            colLogOldSize.Text = Strings.PakDiff_ColumnOldSize;
            colLogNewSize.Text = Strings.PakDiff_ColumnNewSize;
            btnSaveLog.Text = Strings.PakDiff_SaveLog;
            tabDiff.Text = Strings.PakDiff_TabClientCompare;
            grpDirectories.Text = Strings.PakDiff_ClientDirectories;
            lblSource.Text = Strings.PakDiff_TargetSource;
            lblCompare.Text = Strings.PakDiff_CompareClient;
            btnBrowseSource.Text = btnBrowseCompare.Text = Strings.PakDiff_Browse;
            grpMode.Text = Strings.PakDiff_ComparisonMode;
            rbDifferences.Text = Strings.PakDiff_Differences;
            rbIdentical.Text = Strings.PakDiff_IdenticalFiles;
            btnCompare.Text = Strings.PakDiff_Compare;
            colFile.Text = Strings.PakDiff_ColumnFilePath;
            colPak.Text = Strings.PakDiff_ColumnSourcePak;
            btnExtractSelected.Text = Strings.PakDiff_ExtractSelected;
            lblLanguage.Text = Strings.Common_Language;
        }

        // ════════════════════════════════════════════════════════════════════
        // ABA 1 — CHANGE HISTORY / LOG
        // ════════════════════════════════════════════════════════════════════

        // ── Snapshot A (Before / Base) ───────────────────────────────────────
        private void btnBrowseSnapshotA_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.PakDiff_SelectBaseFolder
            };
            if (fbd.ShowDialog() == DialogResult.OK)
                txtSnapshotAPath.Text = fbd.SelectedPath;
        }

        private async void btnTakeSnapshotA_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtSnapshotAPath.Text))
            {
                MessageBox.Show(Strings.PakDiff_SelectValidFolder, Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnTakeSnapshotA.Enabled = false;
            lblSnapshotAStatus.Text = Strings.PakDiff_ReadingPaks;
            lblSnapshotAStatus.ForeColor = Color.DarkOrange;

            _snapshotA = await TakeSnapshotAsync(txtSnapshotAPath.Text, prgBarLog);

            lblSnapshotAStatus.Text = $"✅ {_snapshotA.PakFiles.Count} PAKs | {_snapshotA.TotalEntries} {Strings.PakDiff_Files} @ {_snapshotA.TakenAt:HH:mm:ss}";
            lblSnapshotAStatus.ForeColor = Color.Green;
            btnTakeSnapshotA.Enabled = true;
            UpdateCompareSnapshotButton();
        }

        private void btnSaveSnapshotA_Click(object sender, EventArgs e) => SaveSnapshot(_snapshotA, "A");
        private void btnLoadSnapshotA_Click(object sender, EventArgs e)
        {
            _snapshotA = LoadSnapshot();
            if (_snapshotA != null)
            {
                lblSnapshotAStatus.Text = $"📂 {_snapshotA.PakFiles.Count} PAKs | {_snapshotA.TotalEntries} {Strings.PakDiff_Files} @ {_snapshotA.TakenAt:yyyy-MM-dd HH:mm}";
                lblSnapshotAStatus.ForeColor = Color.SteelBlue;
                UpdateCompareSnapshotButton();
            }
        }

        // ── Snapshot B (After / New) ─────────────────────────────────────────
        private void btnBrowseSnapshotB_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.PakDiff_SelectAfterFolder
            };
            if (fbd.ShowDialog() == DialogResult.OK)
                txtSnapshotBPath.Text = fbd.SelectedPath;
        }

        private async void btnTakeSnapshotB_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(txtSnapshotBPath.Text))
            {
                MessageBox.Show(Strings.PakDiff_SelectValidFolder, Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnTakeSnapshotB.Enabled = false;
            lblSnapshotBStatus.Text = Strings.PakDiff_ReadingPaks;
            lblSnapshotBStatus.ForeColor = Color.DarkOrange;

            _snapshotB = await TakeSnapshotAsync(txtSnapshotBPath.Text, prgBarLog);

            lblSnapshotBStatus.Text = $"✅ {_snapshotB.PakFiles.Count} PAKs | {_snapshotB.TotalEntries} {Strings.PakDiff_Files} @ {_snapshotB.TakenAt:HH:mm:ss}";
            lblSnapshotBStatus.ForeColor = Color.Green;
            btnTakeSnapshotB.Enabled = true;
            UpdateCompareSnapshotButton();
        }

        private void btnSaveSnapshotB_Click(object sender, EventArgs e) => SaveSnapshot(_snapshotB, "B");
        private void btnLoadSnapshotB_Click(object sender, EventArgs e)
        {
            _snapshotB = LoadSnapshot();
            if (_snapshotB != null)
            {
                lblSnapshotBStatus.Text = $"📂 {_snapshotB.PakFiles.Count} PAKs | {_snapshotB.TotalEntries} {Strings.PakDiff_Files} @ {_snapshotB.TakenAt:yyyy-MM-dd HH:mm}";
                lblSnapshotBStatus.ForeColor = Color.SteelBlue;
                UpdateCompareSnapshotButton();
            }
        }

        private void UpdateCompareSnapshotButton() =>
            btnCompareSnapshots.Enabled = _snapshotA != null && _snapshotB != null;

        // ── Comparar snapshots e gerar log ───────────────────────────────────
        private void btnCompareSnapshots_Click(object sender, EventArgs e)
        {
            if (_snapshotA == null || _snapshotB == null) return;

            lstLogChanges.BeginUpdate();
            lstLogChanges.Items.Clear();

            var log = new StringBuilder();
            log.AppendLine("═══════════════════════════════════════════════════════════════");
            log.AppendLine($"  Pangya Suite — PAK Change Log");
            log.AppendLine($"  Generated : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"  Snapshot A: {_snapshotA.SourceFolder} @ {_snapshotA.TakenAt:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"  Snapshot B: {_snapshotB.SourceFolder} @ {_snapshotB.TakenAt:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine("═══════════════════════════════════════════════════════════════");
            log.AppendLine();

            // Índice por PAK relativo → {filename → entry}
            var indexA = BuildIndex(_snapshotA);
            var indexB = BuildIndex(_snapshotB);

            // Todos os PAKs que aparecem em qualquer um dos dois snapshots
            var allPaks = indexA.Keys.Union(indexB.Keys, StringComparer.OrdinalIgnoreCase)
                                     .OrderBy(p => p)
                                     .ToList();

            int totalAdded = 0, totalRemoved = 0, totalModified = 0;

            foreach (string pakRel in allPaks)
            {
                bool inA = indexA.TryGetValue(pakRel, out var entriesA);
                bool inB = indexB.TryGetValue(pakRel, out var entriesB);

                var pakChanges = new List<(string Status, string File, uint OldSize, uint NewSize)>();

                if (!inA && inB)
                {
                    // PAK novo (não existia em A)
                    foreach (var _e in entriesB!.Values)
                        pakChanges.Add(("NEW_PAK_FILE", _e.Name, 0, _e.Size));
                }
                else if (inA && !inB)
                {
                    // PAK removido
                    foreach (var _e in entriesA!.Values)
                        pakChanges.Add(("REMOVED_PAK_FILE", _e.Name, _e.Size, 0));
                }
                else if (inA && inB)
                {
                    // PAK existe nos dois — compara entries internas
                    var allFiles = entriesA!.Keys.Union(entriesB!.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                    foreach (string fileName in allFiles)
                    {
                        bool hasA = entriesA.TryGetValue(fileName, out var eA);
                        bool hasB = entriesB.TryGetValue(fileName, out var eB);

                        if (!hasA && hasB)
                            pakChanges.Add(("ADDED", eB!.Name, 0, eB.Size));
                        else if (hasA && !hasB)
                            pakChanges.Add(("REMOVED", eA!.Name, eA.Size, 0));
                        else if (hasA && hasB && (eA!.Size != eB!.Size || eA.CompressSize != eB.CompressSize))
                            pakChanges.Add(("MODIFIED", eA.Name, eA.Size, eB.Size));
                    }
                }

                if (pakChanges.Count == 0) continue;

                log.AppendLine($"[{pakRel}]");

                foreach (var (status, file, oldSz, newSz) in pakChanges)
                {
                    string symbol, label;
                    Color color;

                    switch (status)
                    {
                        case "ADDED":
                        case "NEW_PAK_FILE":
                            symbol = "+"; label = Strings.PakDiff_AddedUpper;
                            color = Color.LimeGreen; totalAdded++;
                            log.AppendLine($"  + [{label}] {file}  ({newSz:N0} bytes)");
                            break;
                        case "REMOVED":
                        case "REMOVED_PAK_FILE":
                            symbol = "-"; label = Strings.PakDiff_RemovedUpper;
                            color = Color.Tomato; totalRemoved++;
                            log.AppendLine($"  - [{label}] {file}  ({oldSz:N0} bytes)");
                            break;
                        default:
                            symbol = "~"; label = Strings.PakDiff_ModifiedUpper;
                            color = Color.Gold; totalModified++;
                            log.AppendLine($"  ~ [{label}] {file}  ({oldSz:N0} → {newSz:N0} bytes)");
                            break;
                    }

                    var lvi = new ListViewItem(symbol) { ForeColor = color };
                    lvi.SubItems.Add(pakRel);
                    lvi.SubItems.Add(file);
                    lvi.SubItems.Add(label);
                    lvi.SubItems.Add(oldSz > 0 ? $"{oldSz:N0}" : "—");
                    lvi.SubItems.Add(newSz > 0 ? $"{newSz:N0}" : "—");
                    lvi.Tag = status;
                    lstLogChanges.Items.Add(lvi);
                }

                log.AppendLine();
            }

            lstLogChanges.EndUpdate();

            log.AppendLine("───────────────────────────────────────────────────────────────");
            log.AppendLine($"  TOTAL: ➕ {totalAdded} {Strings.PakDiff_Added} | "
                         + $"🔄 {totalModified} {Strings.PakDiff_Modified} | "
                         + $"❌ {totalRemoved} {Strings.PakDiff_Removed}");
            log.AppendLine("═══════════════════════════════════════════════════════════════");

            txtChangeLog.Text = log.ToString();

            MessageBox.Show(
                $"➕ {totalAdded} {Strings.PakDiff_Added} | 🔄 {totalModified} {Strings.PakDiff_Modified} | ❌ {totalRemoved} {Strings.PakDiff_Removed}",
                Strings.PakDiff_ComparisonComplete, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtChangeLog.Text))
            {
                MessageBox.Show(Strings.PakDiff_NoLogToSave,
                    Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = Strings.PakDiff_SaveChangeLog,
                Filter = Strings.PakDiff_TextLogFilter,
                FileName = $"pak_changelog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, txtChangeLog.Text, Encoding.UTF8);
                MessageBox.Show(string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_LogSavedTo, sfd.FileName),
                    Strings.PakDiff_Saved, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void chkLogSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            lstLogChanges.BeginUpdate();
            foreach (ListViewItem item in lstLogChanges.Items)
                item.Checked = chkLogSelectAll.Checked;
            lstLogChanges.EndUpdate();
        }

        // ── Snapshot helpers ─────────────────────────────────────────────────
        private static Task<PakSnapshot> TakeSnapshotAsync(string folder, ProgressBar bar)
        {
            return Task.Run(() =>
            {
                var snapshot = new PakSnapshot
                {
                    SourceFolder = folder,
                    TakenAt = DateTime.Now
                };

                var pakFiles = Directory.GetFiles(folder, "*.pak", SearchOption.AllDirectories);

                bar.Invoke(() =>
                {
                    bar.Maximum = pakFiles.Length;
                    bar.Value = 0;
                    bar.Visible = true;
                });

                int done = 0;
                foreach (var pakPath in pakFiles)
                {
                    string relativePath = Path.GetRelativePath(folder, pakPath);
                    var pakSnap = new PakSnapshotFile { RelativePath = relativePath };

                    try
                    {
                        using var reader = new PakReader(pakPath, logSink: AppLogger.Instance);
                        reader.Parse();
                        foreach (var entry in reader.Entries)
                        {
                            if (entry.Type == PakFileEntryType.Directory) continue;
                            pakSnap.Entries.Add(new PakSnapshotEntry
                            {
                                Name = entry.Name,
                                Size = entry.Size,
                                CompressSize = entry.CompressSize
                            });
                        }
                    }
                    catch { /* PAK corrompido — inclui sem entries */ }

                    snapshot.PakFiles.Add(pakSnap);

                    done++;
                    bar.Invoke(() => bar.Value = done);
                }

                bar.Invoke(() => bar.Visible = false);
                return snapshot;
            });
        }

        private static Dictionary<string, Dictionary<string, PakSnapshotEntry>> BuildIndex(PakSnapshot snap)
        {
            var index = new Dictionary<string, Dictionary<string, PakSnapshotEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pak in snap.PakFiles)
            {
                var entryMap = new Dictionary<string, PakSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in pak.Entries)
                    entryMap[e.Name] = e;
                index[pak.RelativePath] = entryMap;
            }
            return index;
        }

        private static void SaveSnapshot(PakSnapshot? snapshot, string label)
        {
            if (snapshot == null)
            {
                MessageBox.Show(string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_SnapshotNotTaken, label),
                    Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Title = string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_SaveSnapshot, label),
                Filter = Strings.PakDiff_SnapshotFilter,
                FileName = $"snapshot_{label}_{snapshot.TakenAt:yyyyMMdd_HHmmss}.paksnap"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sfd.FileName, json, Encoding.UTF8);
                MessageBox.Show(string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_SnapshotSaved, sfd.FileName),
                    Strings.PakDiff_Saved, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static PakSnapshot? LoadSnapshot()
        {
            using var ofd = new OpenFileDialog
            {
                Title = Strings.PakDiff_LoadSnapshot,
                Filter = Strings.PakDiff_SnapshotFilter
            };
            if (ofd.ShowDialog() != DialogResult.OK) return null;

            try
            {
                string json = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                return JsonSerializer.Deserialize<PakSnapshot>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.PakDiff_LoadSnapshotFailed} {ex.Message}",
                    Strings.PakDiff_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // ABA 2 — MULTI-CLIENT COMPARE & EXTRACT
        // ════════════════════════════════════════════════════════════════════

        private void BtnBrowseSource_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.PakDiff_SelectTargetClient
            };
            if (fbd.ShowDialog() == DialogResult.OK) txtSourceClient.Text = fbd.SelectedPath;
        }

        private void BtnBrowseCompare_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.PakDiff_SelectBaseClient
            };
            if (fbd.ShowDialog() == DialogResult.OK) txtCompareClient.Text = fbd.SelectedPath;
        }

        private async void BtnCompare_Click(object sender, EventArgs e)
        {
            string mode = rbDifferences.Checked ? "diff" : "equal";
            await PerformMultiPakComparisonAsync(mode);
        }

        private async Task PerformMultiPakComparisonAsync(string compareMode)
        {
            string sourceDir = txtSourceClient.Text;
            string compareDir = txtCompareClient.Text;

            if (!Directory.Exists(sourceDir) || !Directory.Exists(compareDir))
            {
                MessageBox.Show(
                    Strings.PakDiff_SelectValidClientFolders,
                    Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnCompare.Enabled = false;
            lstDiffFiles.Items.Clear();
            _comparisonResults.Clear();

            var sourcePaks = Directory.GetFiles(sourceDir, "*.pak", SearchOption.AllDirectories);
            prgBarDiff.Maximum = sourcePaks.Length;
            prgBarDiff.Value = 0;
            prgBarDiff.Visible = true;

            try
            {
                await Task.Run(() =>
                {
                    int done = 0;
                    foreach (var sourcePakPath in sourcePaks)
                    {
                        string relativePakPath = Path.GetRelativePath(sourceDir, sourcePakPath);
                        string matchingComparePakPath = Path.Combine(compareDir, relativePakPath);

                        // Mapa de entries do cliente de comparação (o seu cliente)
                        var compareEntriesMap = new Dictionary<string, PakFileEntry>(StringComparer.OrdinalIgnoreCase);
                        if (File.Exists(matchingComparePakPath))
                        {
                            try
                            {
                                using var compReader = new PakReader(matchingComparePakPath, logSink: AppLogger.Instance);
                                compReader.Parse();
                                foreach (var entry in compReader.Entries)
                                    compareEntriesMap[entry.Name] = entry;
                            }
                            catch { }
                        }

                        try
                        {
                            using var srcReader = new PakReader(sourcePakPath, logSink: AppLogger.Instance);
                            srcReader.Parse();

                            foreach (var srcEntry in srcReader.Entries)
                            {
                                if (srcEntry.Type == PakFileEntryType.Directory) continue;

                                bool existsInCompare = compareEntriesMap.TryGetValue(srcEntry.Name, out var compEntry);

                                if (compareMode == "diff")
                                {
                                    bool isNew = !existsInCompare;
                                    bool isModified = existsInCompare &&
                                                      (srcEntry.Size != compEntry!.Size ||
                                                       srcEntry.CompressSize != compEntry.CompressSize);

                                    if (isNew || isModified)
                                    {
                                        _comparisonResults.Add(new FileDiffEntry
                                        {
                                            FileName = srcEntry.Name,
                                            SourcePakPath = sourcePakPath,
                                            PakEntry = srcEntry,
                                            Reason = isNew ? "New File" : "Modified"
                                        });
                                    }
                                }
                                else if (compareMode == "equal")
                                {
                                    if (existsInCompare &&
                                        srcEntry.Size == compEntry!.Size &&
                                        srcEntry.CompressSize == compEntry.CompressSize)
                                    {
                                        _comparisonResults.Add(new FileDiffEntry
                                        {
                                            FileName = srcEntry.Name,
                                            SourcePakPath = sourcePakPath,
                                            PakEntry = srcEntry,
                                            Reason = "Identical"
                                        });
                                    }
                                }
                            }
                        }
                        catch { }

                        done++;
                        this.Invoke(() => prgBarDiff.Value = done);
                    }
                });

                lstDiffFiles.BeginUpdate();
                foreach (var item in _comparisonResults)
                {
                    string statusLabel = item.Reason switch
                    {
                        "New File" => Strings.PakDiff_New,
                        "Modified" => Strings.PakDiff_ModifiedLabel,
                        _ => Strings.PakDiff_Identical
                    };
                    Color color = item.Reason switch
                    {
                        "New File" => Color.LimeGreen,
                        "Modified" => Color.Gold,
                        _ => Color.SteelBlue
                    };

                    var lvi = new ListViewItem(item.FileName) { ForeColor = color };
                    lvi.SubItems.Add(Path.GetFileName(item.SourcePakPath));
                    lvi.SubItems.Add(statusLabel);
                    lvi.Tag = item;
                    lstDiffFiles.Items.Add(lvi);
                }
                lstDiffFiles.EndUpdate();

                MessageBox.Show(
                    string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_ComparisonFound, _comparisonResults.Count),
                    Strings.PakDiff_Done, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.PakDiff_ComparisonError} {ex.Message}",
                    Strings.PakDiff_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCompare.Enabled = true;
                prgBarDiff.Visible = false;
            }
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            lstDiffFiles.BeginUpdate();
            foreach (ListViewItem item in lstDiffFiles.Items)
                item.Checked = chkSelectAll.Checked;
            lstDiffFiles.EndUpdate();
        }

        private async void BtnExtractSelected_Click(object sender, EventArgs e)
        {
            if (lstDiffFiles.CheckedItems.Count == 0)
            {
                MessageBox.Show(
                    Strings.PakDiff_SelectAtLeastOne,
                    Strings.PakDiff_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.PakDiff_SelectOutputFolder
            };
            if (fbd.ShowDialog() != DialogResult.OK) return;

            string outputDir = fbd.SelectedPath;
            btnExtractSelected.Enabled = false;
            prgBarDiff.Visible = true;

            // Agrupa por PAK físico para abrir cada arquivo apenas uma vez
            var groups = lstDiffFiles.CheckedItems
                .Cast<ListViewItem>()
                .Select(lvi => lvi.Tag)
                .OfType<FileDiffEntry>()
                .GroupBy(entry => entry.SourcePakPath)
                .ToList();

            prgBarDiff.Maximum = groups.Sum(g => g.Count());
            prgBarDiff.Value = 0;
            int totalDone = 0;

            try
            {
                await Task.Run(() =>
                {
                    foreach (var pakGroup in groups)
                    {
                        using var reader = new PakReader(pakGroup.Key, logSink: AppLogger.Instance);
                        reader.Parse();

                        foreach (var diffEntry in pakGroup)
                        {
                            string outPath = Path.Combine(outputDir, diffEntry.PakEntry.Name.Replace('/', '\\'));
                            reader.ExtractEntry(diffEntry.PakEntry, outPath);

                            totalDone++;
                            this.Invoke(() => prgBarDiff.Value = totalDone);
                        }
                    }
                });

                MessageBox.Show(
                    string.Format(LocalizationManager.CurrentCulture, Strings.PakDiff_ExtractionComplete, totalDone, outputDir),
                    Strings.PakDiff_Done, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.PakDiff_ExtractionFailed} {ex.Message}",
                    Strings.PakDiff_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExtractSelected.Enabled = true;
                prgBarDiff.Visible = false;
            }
        }

        // ── Modelos ──────────────────────────────────────────────────────────
        public class FileDiffEntry
        {
            public string FileName { get; set; } = "";
            public string SourcePakPath { get; set; } = "";
            public PakFileEntry PakEntry { get; set; } = null!;
            public string Reason { get; set; } = "";
        }

        public class PakSnapshot
        {
            public string SourceFolder { get; set; } = "";
            public DateTime TakenAt { get; set; } = DateTime.Now;
            public List<PakSnapshotFile> PakFiles { get; set; } = new();
            public int TotalEntries => PakFiles.Sum(p => p.Entries.Count);
        }

        public class PakSnapshotFile
        {
            public string RelativePath { get; set; } = "";
            public List<PakSnapshotEntry> Entries { get; set; } = new();
        }

        public class PakSnapshotEntry
        {
            public string Name { get; set; } = "";
            public uint Size { get; set; }
            public uint CompressSize { get; set; }
        }
    }
}
