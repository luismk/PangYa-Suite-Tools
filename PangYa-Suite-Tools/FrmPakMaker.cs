using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using System.ComponentModel;
using System.Data;

namespace PangYa_Suite_Tools
{
    public partial class FrmPakMaker : Form
    {
        private PakReader? _currentReader;

        // Tag do nó raiz virtual da árvore = "ver todos os arquivos" (modo lista completa)
        private const string RootFolderTag = "";

        // Mapa caminho-da-pasta -> TreeNode, para navegação rápida (duplo clique em uma pasta na lista)
        private readonly Dictionary<string, TreeNode> _folderNodes = new(StringComparer.OrdinalIgnoreCase);

        // Conjunto de entries atualmente "no escopo" (pasta selecionada na árvore), antes do filtro de pesquisa
        private List<PakFileEntry> _scopedEntries = new();
        private bool isInitializingLanguages = true;
        public FrmPakMaker()
        {
            InitializeComponent();
            InitializeLanguageComboBox();
            SetupCustomComponents();
            LoadSetupOptions();
            SetupContextMenu(); // Inicializa o menu de contexto da ListView
        }

        private void InitializeLanguageComboBox()
        {
            cboLanguage.ComboBox.DisplayMember = "Key";
            cboLanguage.ComboBox.ValueMember = "Value";

            // Usando KeyValuePair para garantir tipagem forte e evitar bugs no ToolStrip
            cboLanguage.Items.Add(new KeyValuePair<string, string>("Português (BR)", "br"));
            cboLanguage.Items.Add(new KeyValuePair<string, string>("English (US)", "en"));
            cboLanguage.SelectedIndex = 1;

            isInitializingLanguages = false;

            // Executa a primeira tradução com base na seleção inicial
            ApplyLocalization("en");
        }

        private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializingLanguages) return;

            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                string targetLanguage = selectedItem.Value;
                ApplyLocalization(targetLanguage);
            }
        }

        private void ApplyLocalization(string lang)
        {
            ComponentResourceManager res = new ComponentResourceManager(typeof(FrmPakMaker));
            string suffix = (lang == "en") ? "_en" : "_br";

            // Janela Principal e Label do Combo
            this.Text = res.GetString($"FrmPakMaker{suffix}") ?? this.Text;
            lblLanguage.Text = res.GetString($"lblLanguage{suffix}") ?? lblLanguage.Text;

            // --- ABA 1: LEITOR & MODIFICAÇÕES ---
            tabExtract.Text = res.GetString($"tabExtract{suffix}") ?? tabExtract.Text;
            btnBrowsePak.Text = res.GetString($"btnBrowsePak{suffix}") ?? btnBrowsePak.Text;
            txtPakPath.PlaceholderText = res.GetString($"txtPakPathHint{suffix}") ?? txtPakPath.PlaceholderText;

            // Grupo Cabeçalho
            groupHeader.Text = res.GetString($"groupHeader{suffix}") ?? groupHeader.Text;
            lblAuthor.Text = res.GetString($"lblAuthor{suffix}") ?? lblAuthor.Text;
            lblVersion.Text = res.GetString($"lblVersion{suffix}") ?? lblVersion.Text;
            lblEntries.Text = res.GetString($"lblEntries{suffix}") ?? lblEntries.Text;

            // Pesquisa e Listagem
            lblSearch.Text = res.GetString($"lblSearch{suffix}") ?? lblSearch.Text;
            txtSearch.PlaceholderText = res.GetString($"txtSearchHint{suffix}") ?? txtSearch.PlaceholderText;
            lblCurrentPath.Text = res.GetString($"lblCurrentPath{suffix}") ?? lblCurrentPath.Text;

            // Botões de Ação
            btnExtractSelected.Text = res.GetString($"btnExtractSelected{suffix}") ?? btnExtractSelected.Text;
            btnRemoveSelected.Text = res.GetString($"btnRemoveSelected{suffix}") ?? btnRemoveSelected.Text;
            btnBatchExtract.Text = res.GetString($"btnBatchExtract{suffix}") ?? btnBatchExtract.Text;
            btnUpdatePak.Text = res.GetString($"btnUpdatePak{suffix}") ?? btnUpdatePak.Text;
            btnExtractAll.Text = res.GetString($"btnExtractAll{suffix}") ?? btnExtractAll.Text;

            // Colunas de Exibição
            colName.Text = res.GetString($"colName{suffix}") ?? colName.Text;
            colType.Text = res.GetString($"colType{suffix}") ?? colType.Text;
            colSize.Text = res.GetString($"colSize{suffix}") ?? colSize.Text;
            colCompSize.Text = res.GetString($"colCompSize{suffix}") ?? colCompSize.Text;

            // Painel Inferior XTEA
            lblNewKey.Text = res.GetString($"lblNewKey{suffix}") ?? lblNewKey.Text;
            btnChangeKey.Text = res.GetString($"btnChangeKey{suffix}") ?? btnChangeKey.Text;


            // --- ABA 2: CRIAR NOVO PAK ---
            tabCreate.Text = res.GetString($"tabCreate{suffix}") ?? tabCreate.Text;
            txtSourceFolder.PlaceholderText = res.GetString($"txtSourceFolderHint{suffix}") ?? txtSourceFolder.PlaceholderText;
            btnBrowseFolder.Text = res.GetString($"btnBrowseFolder{suffix}") ?? btnBrowseFolder.Text;

            lblVol.Text = res.GetString($"lblVol{suffix}") ?? lblVol.Text;
            lblComp.Text = res.GetString($"lblComp{suffix}") ?? lblComp.Text;
            lblLevel.Text = res.GetString($"lblLevel{suffix}") ?? lblLevel.Text;
            lblReg.Text = res.GetString($"lblReg{suffix}") ?? lblReg.Text;
            btnCreatePak.Text = res.GetString($"btnCreatePak{suffix}") ?? btnCreatePak.Text;


            // --- COMPONENTES GLOBAIS ---
            lblStatus.Text = res.GetString($"lblStatus{suffix}") ?? lblStatus.Text;
        }

        private void SetupCustomComponents()
        {
            // Ativa o Drag-and-Drop no formulário principal e nas caixas de texto
            this.AllowDrop = true;
            this.DragEnter += FrmPakMaker_DragEnter;
            this.DragLeave += FrmPakMaker_DragLeave;
            this.DragDrop += FrmPakMaker_DragDrop;

            lstEntries.MultiSelect = true;
            lstEntries.DoubleClick += LstEntries_DoubleClick;
            tvFolders.AfterSelect += TvFolders_AfterSelect;
            txtSearch.TextChanged += (s, e) => ApplyDisplayFilter();

            // Permite arrastar arquivos para dentro da lista de entries, para injetar/atualizar no PAK já carregado
            lstEntries.AllowDrop = true;
            lstEntries.DragEnter += LstEntries_DragEnter;
            lstEntries.DragDrop += LstEntries_DragDrop;

        }

        private void SetupContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuExtractSingle = new ToolStripMenuItem("📁 Extrair selecionado(s)...");
            menuExtractSingle.Click += async (s, e) => await ExtractSelectedAsync();

            ToolStripMenuItem menuRemoveSingle = new ToolStripMenuItem("🗑️ Remover selecionado(s) do PAK...");
            menuRemoveSingle.Click += async (s, e) => await RemoveSelectedAsync();

            contextMenu.Items.Add(menuExtractSingle);
            contextMenu.Items.Add(menuRemoveSingle);
            lstEntries.ContextMenuStrip = contextMenu; // Vincula o menu à ListView
        }

        private void LoadSetupOptions()
        {
            // Popula os seletores usando os Enums e Listas da sua PangyaAPI
            cboVersion.DataSource = Enum.GetValues(typeof(PakFileEntryVersion));
            cboVersion.SelectedItem = PakFileEntryVersion.V3;

            cboCompressType.DataSource = Enum.GetValues(typeof(PakFileEntryType));
            cboCompressType.SelectedItem = PakFileEntryType.LZ772;

            cboRegion.DataSource = PakKeys.All
                .Select(x => new { Label = x.Label, Keys = x.Keys })
                .ToList();
            cboRegion.DisplayMember = "Label";
            cboRegion.SelectedIndex = 0;

            // Combo de chave/região destino, usado na troca de chave XTEA (aba de extração)
            cboNewRegion.DataSource = PakKeys.All
                .Select(x => new { Label = x.Label, Keys = x.Keys })
                .ToList();
            cboNewRegion.DisplayMember = "Label";
            cboNewRegion.SelectedIndex = 0;
        }

        private void FrmPakMaker_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                txtPakPath.BackColor = Color.LightCyan;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FrmPakMaker_DragLeave(object? sender, EventArgs e)
        {
            txtPakPath.BackColor = SystemColors.Control;
        }

        private void FrmPakMaker_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string path = files[0];
                txtPakPath.BackColor = SystemColors.Control;

                // Se for um arquivo .pak, carrega no leitor
                if (File.Exists(path) && path.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                {
                    txtPakPath.Text = path;
                    tabControl1.SelectedIndex = 0; // Foca na aba de extração
                    LoadPak(path);
                }
                // Se for uma pasta, joga para a aba de criação
                else if (Directory.Exists(path))
                {
                    txtSourceFolder.Text = path;
                    tabControl1.SelectedIndex = 1; // Foca na aba de criação
                }
            }
        }

        // ─── ABA 1: LEITURA E EXTRAÇÃO ─────────────────────────────────────────
        private void btnBrowsePak_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog { Filter = "Pangya PAK Files (*.pak)|*.pak" };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtPakPath.Text = openFileDialog.FileName;
                LoadPak(openFileDialog.FileName);
            }
        }

        private void LoadPak(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                _currentReader?.Dispose();
                _currentReader = new PakReader(path);
                _currentReader.Parse();

                // Atualiza as Labels de informação do Header
                lblAuthor.Text = $"Autor: {_currentReader.Header.Author}";
                lblVersion.Text = $"Versão: 0x{_currentReader.Header.Version:X2}";
                lblEntries.Text = $"Entradas: {_currentReader.Header.NumFileEntry}";

                txtSearch.Text = "";
                BuildFolderTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o arquivo PAK:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─── NAVEGAÇÃO POR PASTAS ───────────────────────────────────────────────

        /// <summary>
        /// Monta a árvore de pastas a partir dos nomes internos das entries, e mantém
        /// um nó raiz virtual "Todos os Arquivos" que funciona como a visão de lista completa.
        /// </summary>
        private void BuildFolderTree()
        {
            tvFolders.BeginUpdate();
            tvFolders.Nodes.Clear();
            _folderNodes.Clear();

            var rootNode = new TreeNode("🗂 Todos os Arquivos") { Tag = RootFolderTag };
            tvFolders.Nodes.Add(rootNode);
            _folderNodes[RootFolderTag] = rootNode;

            if (_currentReader != null)
            {
                // Garante que toda pasta (mesmo sem uma entry "Directory" explícita) exista na árvore
                foreach (var entry in _currentReader.Entries)
                {
                    if (entry.Type == PakFileEntryType.Directory) continue;

                    string folder = Path.GetDirectoryName(entry.Name.Replace('/', '\\')) ?? "";
                    EnsureFolderNode(folder);
                }
            }

            tvFolders.EndUpdate();
            tvFolders.SelectedNode = rootNode; // Dispara TvFolders_AfterSelect → popula a lista com tudo
        }

        /// <summary>
        /// Garante que o caminho de pasta (e todos os seus pais) existam como TreeNode,
        /// criando-os recursivamente se necessário. Retorna o TreeNode correspondente.
        /// </summary>
        private TreeNode EnsureFolderNode(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return _folderNodes[RootFolderTag];

            if (_folderNodes.TryGetValue(folderPath, out var existing))
                return existing;

            string parentPath = Path.GetDirectoryName(folderPath) ?? "";
            TreeNode parentNode = EnsureFolderNode(parentPath);

            string folderName = Path.GetFileName(folderPath);
            var node = new TreeNode("📁 " + folderName) { Tag = folderPath };
            parentNode.Nodes.Add(node);
            _folderNodes[folderPath] = node;
            return node;
        }

        private void TvFolders_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (_currentReader == null) return;

            string folderTag = e.Node?.Tag as string ?? RootFolderTag;

            _scopedEntries = string.IsNullOrEmpty(folderTag)
                ? _currentReader.Entries.Where(en => en.Type != PakFileEntryType.Directory).ToList()
                : _currentReader.Entries
                    .Where(en => string.Equals(
                        Path.GetDirectoryName(en.Name.Replace('/', '\\')) ?? "",
                        folderTag,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            lblCurrentPath.Text = string.IsNullOrEmpty(folderTag)
                ? "📂 Caminho: (todos os arquivos)"
                : $"📂 Caminho: {folderTag.Replace('\\', '/')}";

            ApplyDisplayFilter();
        }

        /// <summary>Aplica o texto de pesquisa por cima do escopo de pasta atual e repopula a ListView.</summary>
        private void ApplyDisplayFilter()
        {
            string term = txtSearch.Text.Trim();

            IEnumerable<PakFileEntry> filtered = string.IsNullOrEmpty(term)
                ? _scopedEntries
                : _scopedEntries.Where(en => en.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            PopulateList(filtered);
        }

        /// <summary>Preenche a ListView com o conjunto de entries fornecido (sem tocar no escopo/pesquisa).</summary>
        private void PopulateList(IEnumerable<PakFileEntry> entries)
        {
            lstEntries.BeginUpdate();
            lstEntries.Items.Clear();

            foreach (var entry in entries)
            {
                var item = new ListViewItem(entry.Name);
                item.SubItems.Add(entry.Type.ToString());
                item.SubItems.Add($"0x{entry.Size:X8}");
                item.SubItems.Add($"0x{entry.CompressSize:X8}");

                item.Tag = entry;

                if (entry.Type == PakFileEntryType.Directory)
                    item.ForeColor = Color.DarkCyan;

                lstEntries.Items.Add(item);
            }

            lstEntries.EndUpdate();
        }

        /// <summary>Duplo clique numa pasta dentro da lista navega para ela na árvore.</summary>
        private void LstEntries_DoubleClick(object? sender, EventArgs e)
        {
            if (lstEntries.SelectedItems.Count == 0) return;
            if (lstEntries.SelectedItems[0].Tag is not PakFileEntry entry) return;
            if (entry.Type != PakFileEntryType.Directory) return;

            string folderPath = entry.Name.Replace('/', '\\');
            if (_folderNodes.TryGetValue(folderPath, out var node))
                tvFolders.SelectedNode = node;
        }

        // ─── PROGRESSO / STATUS ─────────────────────────────────────────────────

        /// <summary>Helper thread-safe para reportar progresso (0-100%) na status bar a partir de uma Task de fundo.</summary>
        private void ReportProgress(int done, int total, string? prefix = null)
        {
            void Apply()
            {
                progressBar1.Visible = true;
                progressBar1.Maximum = 100;
                progressBar1.Value = total > 0 ? Math.Clamp((done * 100) / total, 0, 100) : 0;
                if (prefix != null)
                    lblStatus.Text = $"{prefix} ({done}/{total})";
            }

            if (InvokeRequired) Invoke(Apply);
            else Apply();
        }

        private void HideProgress()
        {
            void Apply() => progressBar1.Visible = false;
            if (InvokeRequired) Invoke(Apply);
            else Apply();
        }

        // ─── EXTRAIR TUDO / SELECIONADO(S) / LOTE ──────────────────────────────

        private async void btnExtractAll_Click(object sender, EventArgs e)
        {
            if (_currentReader == null)
            {
                MessageBox.Show("Por favor, carregue um arquivo .pak primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string destination = folderDialog.SelectedPath;
                btnExtractAll.Enabled = false;
                lblStatus.Text = "Extraindo arquivos...";

                try
                {
                    await Task.Run(() =>
                    {
                        _currentReader.Extract("*", destination, msg => { },
                            (done, total) => ReportProgress(done, total, "Extraindo"));
                    });

                    lblStatus.Text = "Pronto";
                    MessageBox.Show("Todos os arquivos foram extraídos com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Erro na extração";
                    MessageBox.Show($"Erro ao extrair: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    HideProgress();
                    btnExtractAll.Enabled = true;
                }
            }
        }

        private async void btnExtractSelected_Click(object sender, EventArgs e) => await ExtractSelectedAsync();

        /// <summary>
        /// Extrai apenas os itens selecionados na ListView, usando o caminho rápido
        /// (PakReader.ExtractEntry), que reaproveita o stream já aberto — sem reabrir
        /// e reanalisar o .pak inteiro como acontecia antes.
        /// </summary>
        private async Task ExtractSelectedAsync()
        {
            if (_currentReader == null || lstEntries.SelectedItems.Count == 0) return;

            var selectedEntries = lstEntries.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => (PakFileEntry)i.Tag)
                .Where(en => en.Type != PakFileEntryType.Directory)
                .ToList();

            if (selectedEntries.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um arquivo (pastas não podem ser extraídas diretamente).",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string destinationDir;

            if (selectedEntries.Count == 1)
            {
                string suggestedName = Path.GetFileName(selectedEntries[0].Name.Replace('/', '\\'));
                using var saveFileDialog = new SaveFileDialog
                {
                    FileName = suggestedName,
                    Title = $"Extrair {suggestedName}"
                };
                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

                destinationDir = Path.GetDirectoryName(saveFileDialog.FileName) ?? "./";
                await RunExtractionAsync(selectedEntries, destinationDir, saveFileDialog.FileName);
            }
            else
            {
                using var folderDialog = new FolderBrowserDialog { Description = "Selecione a pasta de destino para os arquivos selecionados" };
                if (folderDialog.ShowDialog() != DialogResult.OK) return;

                destinationDir = folderDialog.SelectedPath;
                await RunExtractionAsync(selectedEntries, destinationDir, null);
            }
        }

        private async Task RunExtractionAsync(List<PakFileEntry> entries, string destinationDir, string? exactPathForSingle)
        {
            btnExtractSelected.Enabled = false;
            lblStatus.Text = "Extraindo selecionado(s)...";

            try
            {
                await Task.Run(() =>
                {
                    int total = entries.Count;
                    int done = 0;

                    foreach (var entry in entries)
                    {
                        string outPath = exactPathForSingle ?? Path.Combine(destinationDir, entry.Name.Replace('/', '\\'));
                        _currentReader!.ExtractEntry(entry, outPath);

                        done++;
                        ReportProgress(done, total, "Extraindo selecionado(s)");
                    }
                });

                lblStatus.Text = "Pronto";
                MessageBox.Show("Arquivo(s) extraído(s) com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro na extração";
                MessageBox.Show($"Erro ao extrair: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnExtractSelected.Enabled = true;
            }
        }

        private async void btnBatchExtract_Click(object sender, EventArgs e)
        {
            using var sourceFolderDialog = new FolderBrowserDialog { Description = "Selecione a pasta que CONTÉM os arquivos .pak" };
            if (sourceFolderDialog.ShowDialog() != DialogResult.OK) return;

            string sourceDir = sourceFolderDialog.SelectedPath;
            string[] pakFiles = Directory.GetFiles(sourceDir, "*.pak", SearchOption.TopDirectoryOnly);

            if (pakFiles.Length == 0)
            {
                MessageBox.Show("Nenhum arquivo .pak foi encontrado na pasta selecionada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var destFolderDialog = new FolderBrowserDialog { Description = "Selecione a pasta de DESTINO para a extração" };
            if (destFolderDialog.ShowDialog() != DialogResult.OK) return;

            string targetBaseDir = destFolderDialog.SelectedPath;

            // Mesma região/chave do PAK atualmente carregado (se houver), evita ficar perguntando por console.
            uint[]? sharedKeys = _currentReader?.LocationKeys;

            btnBatchExtract.Enabled = false;
            progressBar1.Visible = true;
            progressBar1.Maximum = pakFiles.Length;
            progressBar1.Value = 0;

            int paksProcessados = 0;

            foreach (var pakPath in pakFiles)
            {
                string pakName = Path.GetFileNameWithoutExtension(pakPath);
                string specificDestFolder = Path.Combine(targetBaseDir, pakName);

                lblStatus.Text = $"Processando ({paksProcessados + 1}/{pakFiles.Length}): {pakName}.pak...";

                try
                {
                    await Task.Run(() =>
                    {
                        if (!Directory.Exists(specificDestFolder))
                            Directory.CreateDirectory(specificDestFolder);

                        using var batchReader = new PakReader(pakPath);
                        batchReader.Parse(sharedKeys);
                        batchReader.Extract("*", specificDestFolder);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Falha ao extrair {pakName}.pak:\n{ex.Message}", "Erro no Lote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                paksProcessados++;
                progressBar1.Value = paksProcessados;
            }

            lblStatus.Text = "Extração em lote concluída!";
            progressBar1.Visible = false;
            btnBatchExtract.Enabled = true;

            MessageBox.Show($"{paksProcessados} pacotes PAK extraídos com sucesso em:\n{targetBaseDir}", "Processamento Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─── INJETAR / ATUALIZAR ────────────────────────────────────────────────

        private PakRebuildOptions BuildRebuildOptionsForCurrentPak()
        {
            var selectedRegion = (dynamic)cboRegion.SelectedItem;

            return new PakRebuildOptions(
                EntryVersion: (PakFileEntryVersion)cboVersion.SelectedItem,
                EntryType: (PakFileEntryType)cboCompressType.SelectedItem,
                CompressLevel: (byte)numCompressLevel.Value,
                LocationKeys: _currentReader?.LocationKeys ?? (uint[])selectedRegion.Keys,
                Author: _currentReader?.Header.Author ?? "PangYaSuiteTools");
        }

        private async void btnUpdatePak_Click(object sender, EventArgs e)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show("Selecione um arquivo .pak ativo primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var openFileDialog = new OpenFileDialog
            {
                Title = "Selecione os arquivos para atualizar/injetar",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            var items = openFileDialog.FileNames.Select(f => new PakInjectItem(f, null)).ToList();
            await InjectFilesIntoCurrentPakAsync(items);

        }

        /// <summary>
        /// Lógica comum de injeção/atualização do PAK atualmente carregado.
        /// Usada tanto pelo botão "Atualizar PAK" quanto pelo drag-and-drop na lista de entries.
        /// </summary>
        private async Task InjectFilesIntoCurrentPakAsync(List<PakInjectItem> items)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show("Selecione um arquivo .pak ativo primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (items == null || items.Count == 0) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            lblStatus.Text = "Mesclando e reconstruindo PAK...";
            btnUpdatePak.Enabled = false;

            try
            {
                var options = BuildRebuildOptionsForCurrentPak();

                await Task.Run(() =>
                {
                    PakManager.InjectFiles(pakPath, reader, items, options,
                        log: msg => { },
                        onProgress: (done, total) => ReportProgress(done, total, "Reconstruindo PAK"));
                });

                lblStatus.Text = "PAK atualizado com sucesso!";
                MessageBox.Show($"{items.Count} arquivo(s) injetado(s) e o PAK foi reconstruído!",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadPak(pakPath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro ao atualizar";
                MessageBox.Show($"Falha na reconstrução: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnUpdatePak.Enabled = true;
            }
        }

        private void LstEntries_DragEnter(object? sender, DragEventArgs e)
        {
            if (_currentReader != null && e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void LstEntries_DragDrop(object? sender, DragEventArgs e)
        {
            if (_currentReader == null || e.Data == null) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            var items = new List<PakInjectItem>();
            foreach (var path in dropped)
            {
                if (File.Exists(path))
                {
                    // Arquivo solto: sem pasta explícita, cai no fallback de busca por nome existente
                    items.Add(new PakInjectItem(path, null));
                }
                else if (Directory.Exists(path))
                {
                    try
                    {
                        string baseFolder = path; // a própria pasta arrastada é a "raiz" da estrutura relativa
                        foreach (var filePath in Directory.GetFiles(baseFolder, "*", SearchOption.AllDirectories))
                        {
                            string relativeToBase = Path.GetRelativePath(baseFolder, filePath);
                            string relFolder = Path.GetDirectoryName(relativeToBase) ?? "";

                            items.Add(new PakInjectItem(filePath, relFolder));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao ler a pasta '{path}':\n{ex.Message}",
                            "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            if (items.Count == 0)
            {
                MessageBox.Show("Nenhum arquivo válido encontrado para injetar.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await InjectFilesIntoCurrentPakAsync(items);
        }

        // ─── REMOVER ─────────────────────────────────────────────────────────────

        private async void btnRemoveSelected_Click(object sender, EventArgs e) => await RemoveSelectedAsync();

        private async Task RemoveSelectedAsync()
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show("Selecione um arquivo .pak ativo primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lstEntries.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um arquivo para remover.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var namesToRemove = lstEntries.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => (PakFileEntry)i.Tag)
                .Where(en => en.Type != PakFileEntryType.Directory)
                .Select(en => en.Name)
                .ToList();

            if (namesToRemove.Count == 0)
            {
                MessageBox.Show("Pastas não podem ser removidas diretamente; selecione os arquivos dentro dela.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Remover {namesToRemove.Count} arquivo(s) do PAK?\nO PAK será reconstruído e um backup (.bak) será criado.",
                "Confirmar remoção", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            lblStatus.Text = "Removendo e reconstruindo PAK...";
            btnRemoveSelected.Enabled = false;

            try
            {
                var options = BuildRebuildOptionsForCurrentPak();

                await Task.Run(() =>
                {
                    PakManager.RemoveFiles(pakPath, reader, namesToRemove, options,
                        log: msg => { },
                        onProgress: (done, total) => ReportProgress(done, total, "Reconstruindo PAK"));
                });

                lblStatus.Text = "Arquivo(s) removido(s) com sucesso!";
                MessageBox.Show("O(s) arquivo(s) selecionado(s) foram removidos e o PAK foi reconstruído!",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadPak(pakPath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro ao remover";
                MessageBox.Show($"Falha na remoção: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnRemoveSelected.Enabled = true;
            }
        }

        // ─── ABA 2: CRIAÇÃO DE PAK ─────────────────────────────────────────────
        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtSourceFolder.Text = folderDialog.SelectedPath;
            }
        }

        private async void btnCreatePak_Click(object sender, EventArgs e)
        {
            string source = txtSourceFolder.Text;
            if (string.IsNullOrEmpty(source) || !Directory.Exists(source))
            {
                MessageBox.Show("Selecione um diretório de origem válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var saveFileDialog = new SaveFileDialog { Filter = "Pangya PAK Files (*.pak)|*.pak", FileName = "ProjectGxxx.pak" };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var selectedItem = cboRegion.SelectedItem as dynamic;
                    if (selectedItem != null)
                    {
                        btnCreatePak.Enabled = false;
                        lblStatus.Text = "Compilando PAK...";
                        uint[] selectedKeys = selectedItem.Keys;//name is keys, label is name key 
                        var selectedVersion = (PakFileEntryVersion)cboVersion.SelectedItem;
                        //minha tecnica antiga para criar paks raw
                        if (selectedVersion == PakFileEntryVersion.Raw)
                        {
                            selectedKeys = Array.Empty<uint>(); // Ou mantenha null se o Writer aceitar
                        }

                        //tambem tem a versao raw ou universal key, que nao inserimos chave, pois ela se trata de dados brutos diferentes
                        var writer = new PakWriter
                        {
                            EntryVersion = selectedVersion,
                            EntryType = (PakFileEntryType)cboCompressType.SelectedItem,
                            CompressLevel = (byte)numCompressLevel.Value, 
                            // Se não for Raw e selectedKeys vier nulo por falha de seleção, aplica o fallback JP
                            LocationKeys = selectedKeys ?? (selectedVersion == PakFileEntryVersion.Raw ? Array.Empty<uint>() : PakKeys.JP),
                            Author = "PakToolWinForms" // Assinatura do PAK
                        };
                        //inicia a criacao do pak
                        await Task.Run(() => writer.CreateFromDirectory(source, saveFileDialog.FileName));
                        //terminou
                        lblStatus.Text = "Pronto";
                        btnCreatePak.Enabled = true;
                        MessageBox.Show("Arquivo .pak gerado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Por favor, selecione uma região válida antes de continuar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    } 
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Erro";
                    btnCreatePak.Enabled = true;
                    MessageBox.Show($"Erro ao criar o pacote:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnChangeKey_Click(object sender, EventArgs e)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show("Selecione um arquivo .pak ativo primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRegion = cboNewRegion.SelectedItem as dynamic;
            if (selectedRegion == null)
            {
                MessageBox.Show("Selecione a região/chave de destino.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            uint[] newKeys = selectedRegion.Keys;

            // Evita reconstrução desnecessária se a chave de destino já for a mesma do PAK carregado
            if (_currentReader.LocationKeys != null && newKeys.SequenceEqual(_currentReader.LocationKeys))
            {
                MessageBox.Show("O PAK já está usando essa chave.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Trocar a chave do PAK para \"{selectedRegion.Label}\"?\nO PAK será reconstruído e um backup (.bak) será criado.",
                "Confirmar troca de chave", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            // Mantém versão/compressão/autor atuais do PAK, só troca a LocationKeys
            var currentOptions = BuildRebuildOptionsForCurrentPak();
            var newOptions = currentOptions with { LocationKeys = newKeys };

            lblStatus.Text = "Trocando chave e reconstruindo PAK...";
            btnChangeKey.Enabled = false;

            try
            {
                await Task.Run(() =>
                {
                    PakManager.ChangeEncryptionKey(pakPath, reader, newOptions,
                        log: msg => { },
                        onProgress: (done, total) => ReportProgress(done, total, "Reconstruindo PAK"));
                });

                lblStatus.Text = "Chave trocada com sucesso!";
                MessageBox.Show($"O PAK foi reconstruído com a chave de \"{selectedRegion.Label}\"!",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadPak(pakPath);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro ao trocar chave";
                MessageBox.Show($"Falha na reconstrução: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnChangeKey.Enabled = true;
            }
        }
    }
}
