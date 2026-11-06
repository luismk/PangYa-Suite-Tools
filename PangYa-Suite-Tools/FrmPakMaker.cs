using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Configuration;
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;
using System.ComponentModel;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace PangYa_Suite_Tools
{
    public partial class FrmPakMaker : Form
    {
        private const int VerticalScrollBar = 1;
        private const int WindowStyleIndex = -16;
        private const int VerticalScrollBarStyle = 0x00200000;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(
            IntPtr windowHandle, int bar, [MarshalAs(UnmanagedType.Bool)] bool show);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr windowHandle, int index);

        private PakReader? _currentReader;

        // Tag do nó raiz virtual da árvore = "ver todos os arquivos" (modo lista completa)
        private const string RootFolderTag = "";

        // Mapa caminho-da-pasta -> TreeNode, para navegação rápida (duplo clique em uma pasta na lista)
        private readonly Dictionary<string, TreeNode> _folderNodes = new(StringComparer.OrdinalIgnoreCase);
        private ToolStripMenuItem? _menuCreateFolder;
        private TreeNode? _pendingFolderNode;

        // Conjunto de entries atualmente "no escopo" (pasta selecionada na árvore), antes do filtro de pesquisa
        private List<PakFileEntry> _scopedEntries = [];
        private sealed record RegionOption(string Label, uint[] Keys);
        private PakCreationOptions _creationOptions = PakCreationOptions.Default;
        private byte? _knownCompressionLevel;
        private int _selectedFilenameEncodingCodePage = PakFilenameEncodingPreferences.DefaultCodePage;
        private bool _isInitializingFilenameEncoding;
        private CancellationTokenSource? _operationCancellation;
        private TableLayoutPanel? _rootLayout;
        private TableLayoutPanel? _toolbarLayout;
        private ToolStrip? _pakOperationsToolbar;
        private ToolStrip? _openedPakFileToolbar;
        private ToolStripLabel? _pakOperationsLabel;
        private ToolStripLabel? _fileOperationsLabel;
        private ToolStripButton? _toolbarNewPak;
        private ToolStripButton? _toolbarFilenameEncoding;
        private ToolStripButton? _toolbarBatchExtract;
        private ToolStripButton? _toolbarUpdatePak;
        private ToolStripButton? _toolbarEditPakSettings;
        private ToolStripButton? _toolbarExtractAll;
        private ToolStripButton? _toolbarChangePakKey;
        private ToolStripButton? _toolbarExtractSelected;
        private ToolStripButton? _toolbarRenameSelected;
        private ToolStripButton? _toolbarRemoveSelected;
        private readonly ToolStripStatusLabel _filenameEncodingStatusLabel = new()
        {
            Margin = new Padding(10, 0, 0, 0)
        };
        private readonly List<ToolStripItem> _operationToolbarItems = [];
        private readonly List<ToolStripButton> _officeToolbarButtons = [];
        private readonly List<ToolStripItem> _requiresLoadedPakToolbarItems = [];
        private readonly HashSet<PakFileEntry> _unusualNameEntries = [];
        private bool _showToolbarText = true;
        private bool _operationInProgress;
        private enum ToolbarIconKind
        {
            New,
            Folder,
            Update,
            Settings,
            ExtractAll,
            Key,
            Encoding,
            ExtractSelected,
            Rename,
            Remove
        }
        public FrmPakMaker()
        {
            InitializeComponent();
            txtPakPath.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.PakArchive);
            SetupCustomComponents();
            InitializeFilenameEncodingComboBox();
            SetupContextMenu(); // Inicializa o menu de contexto da ListView
            SetupToolbarLayout();
            ApplyLocalization();
            CleanupOldTempDragFolders(); // Remove resíduos de exportações de drag-out de execuções anteriores
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            Disposed += (_, _) =>
            {
                PathTextBoxPreferences.SavePaths(new Dictionary<PathTextBoxKind, string?>
                {
                    [PathTextBoxKind.PakArchive] = txtPakPath.Text
                });
                LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
                _operationCancellation?.Cancel();
                _operationCancellation?.Dispose();
                _currentReader?.Dispose();
            };
        }


        public FrmPakMaker(string idiomaAtual, string initialPakPath) : this()
        {
            LocalizationManager.SetCulture(idiomaAtual);
            this.Shown += (s, e) =>
            {
                if (!string.IsNullOrEmpty(initialPakPath) && File.Exists(initialPakPath))
                {
                    LoadPak(initialPakPath);
                }
            };
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
        {
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Text = Strings.Pak_Title;
            lblFilenameEncoding.Text = Strings.Pak_FilenameEncoding;
            lblFilenameEncoding.ToolTipText = Strings.Pak_FilenameEncodingTooltip;
            cboFilenameEncoding.ToolTipText = Strings.Pak_FilenameEncodingTooltip;

            // --- LEITOR & MODIFICAÇÕES ---
            btnBrowsePak.Text = Strings.Pak_Browse;
            txtPakPath.PlaceholderText = Strings.Pak_PathHint;

            // Grupo Cabeçalho
            groupHeader.Text = Strings.Pak_Header;
            lblAuthor.Text = Strings.Pak_Author;
            lblVersion.Text = Strings.Pak_Version;
            lblEntries.Text = Strings.Pak_Entries;

            // Pesquisa e Listagem
            lblSearch.Text = Strings.Pak_Search;
            txtSearch.PlaceholderText = Strings.Pak_SearchHint;
            lblCurrentPath.Text = Strings.Pak_CurrentPath;

            // Botões de Ação
            btnExtractSelected.Text = Strings.Pak_ExtractSelected;
            btnRemoveSelected.Text = Strings.Pak_RemoveSelected;
            btnBatchExtract.Text = Strings.Pak_BatchExtract;
            btnUpdatePak.Text = Strings.Pak_Update;
            btnExtractAll.Text = Strings.Pak_ExtractAll;
            btnCancelOperation.Text = Strings.Pak_CancelOperation;

            // Colunas de Exibição
            colName.Text = Strings.Pak_ColumnName;
            colType.Text = Strings.Pak_ColumnType;
            colSize.Text = Strings.Pak_ColumnSize;
            colCompSize.Text = Strings.Pak_ColumnCompressed;

            // Painel Inferior XTEA
            lblNewKey.Text = Strings.Pak_NewKey;
            btnChangeKey.Text = Strings.Pak_ChangeKey;


            // --- COMPONENTES GLOBAIS ---
            if (string.IsNullOrWhiteSpace(lblStatus.Text)) lblStatus.Text = Strings.Pak_Ready;

            if (_currentReader != null)
            {
                lblAuthor.Text = $"{Strings.PakMaker_Author} {_currentReader.Header.Author}";
                lblVersion.Text = $"{Strings.PakMaker_Version} 0x{_currentReader.Header.Version:X2}";
                lblEntries.Text = $"{Strings.PakMaker_Entries} {_currentReader.Header.NumFileEntry}";
            }
            UpdateDisplayedCompressionLevel();
            UpdateDisplayedPakKey();

            // Menu de contexto (criado dinamicamente em código, não pelo Designer)
            if (_menuExtractSingle != null)
                _menuExtractSingle.Text = Strings.PakMaker_ExtractSelectedItemS;
            if (_menuRenameSingle != null)
                _menuRenameSingle.Text = Strings.PakMaker_RenameSelectedFile;
            if (_menuRemoveSingle != null)
                _menuRemoveSingle.Text = Strings.PakMaker_RemoveSelectedItemSFromPAK;
            if (_menuExtractFolder != null)
                _menuExtractFolder.Text = Strings.PakMaker_ExtractThisFolder;
            if (_menuRemoveFolder != null)
                _menuRemoveFolder.Text = Strings.PakMaker_RemoveThisFolderFromPAK;
            if (_menuRename != null)
                _menuRename.Text = Strings.PakMaker_RenameThisPAK;
            if (_menuCreateFolder != null)
                _menuCreateFolder.Text = Strings.PakMaker_CreateFolder;

            ApplyToolbarLocalization();
        }

        private void SetupCustomComponents()
        {
            // Ativa o Drag-and-Drop no formulário principal e nas caixas de texto
            this.AllowDrop = true;
            this.DragEnter += FrmPakMaker_DragEnter;
            this.DragLeave += FrmPakMaker_DragLeave;
            this.DragDrop += FrmPakMaker_DragDrop;
            tvFolders.CheckBoxes = false;
            lstEntries.MultiSelect = true;
            lstEntries.LabelEdit = true;
            lstEntries.DoubleClick += LstEntries_DoubleClick;
            lstEntries.AfterLabelEdit += LstEntries_AfterLabelEdit;
            lstEntries.KeyDown += LstEntries_KeyDown;
            lstEntries.ItemSelectionChanged += LstEntries_ItemSelectionChanged;
            tvFolders.AfterSelect += TvFolders_AfterSelect;
            txtSearch.TextChanged += (s, e) => ApplyDisplayFilter();

            // Permite arrastar arquivos para dentro da lista de entries, para injetar/atualizar no PAK já carregado
            lstEntries.AllowDrop = true;
            lstEntries.DragEnter += LstEntries_DragEnter;
            lstEntries.DragDrop += LstEntries_DragDrop;

            tvFolders.KeyDown += TvFolders_KeyDown; // Detecta a tecla Delete
            tvFolders.NodeMouseClick += TvFolders_NodeMouseClick; // Seleciona com botão direito e abre o menu
            SetupFolderContextMenu(); // Inicializa o menu de contexto da árvore de pastas

            // Permite arrastar os itens selecionados (ou uma pasta) PARA FORA do app, extraindo-os direto no Explorer
            lstEntries.ItemDrag += LstEntries_ItemDrag;
            tvFolders.ItemDrag += TvFolders_ItemDrag;

            // Permite renomear arquivos ou pastas
            tvFolders.LabelEdit = true; // Permite alterar o texto do nó nativamente
            tvFolders.AfterLabelEdit += TvFolders_AfterLabelEdit; // Evento executado pós-edição 
        }

        private void SetupToolbarLayout()
        {
            SuspendLayout();

            int originalClientHeight = ClientSize.Height;
            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _toolbarLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = 1
            };
            _toolbarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int row = 0; row < _toolbarLayout.RowCount; row++)
                _toolbarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _pakOperationsToolbar = CreateToolbar(officeStyle: true);
            _openedPakFileToolbar = CreateToolbar(officeStyle: true);

            _pakOperationsLabel = AddSectionLabel(_pakOperationsToolbar);
            _toolbarNewPak = AddOperationButton(
                _pakOperationsToolbar,
                async (_, _) => await CreateEmptyPakAsync(),
                ToolbarIconKind.New,
                requiresLoadedPak: false);
            _toolbarBatchExtract = AddOperationButton(_pakOperationsToolbar, btnBatchExtract_Click, ToolbarIconKind.Folder, requiresLoadedPak: false);
            _toolbarExtractAll = AddOperationButton(_pakOperationsToolbar, btnExtractAll_Click, ToolbarIconKind.ExtractAll);
            _toolbarChangePakKey = AddOperationButton(_pakOperationsToolbar, btnChangeKey_Click, ToolbarIconKind.Key);
            _toolbarFilenameEncoding = AddOperationButton(_pakOperationsToolbar, (_, _) => ShowFilenameEncodingDialog(), ToolbarIconKind.Encoding, requiresLoadedPak: false);

            _fileOperationsLabel = AddSectionLabel(_openedPakFileToolbar);
            _toolbarUpdatePak = AddOperationButton(_openedPakFileToolbar, btnUpdatePak_Click, ToolbarIconKind.Update);
            _toolbarEditPakSettings = AddOperationButton(
                _openedPakFileToolbar,
                async (_, _) => await EditPakSettingsAsync(),
                ToolbarIconKind.Settings);
            _toolbarExtractSelected = AddOperationButton(_openedPakFileToolbar, btnExtractSelected_Click, ToolbarIconKind.ExtractSelected);
            _toolbarRenameSelected = AddOperationButton(_openedPakFileToolbar, (s, e) => BeginSelectedFileRename(), ToolbarIconKind.Rename);
            _toolbarRemoveSelected = AddOperationButton(_openedPakFileToolbar, btnRemoveSelected_Click, ToolbarIconKind.Remove);

            txtUpdateAuthor.Visible = false;

            lblNewKey.Visible = false;
            cboNewRegion.Visible = false;

            statusStrip1.Items.Remove(lblFilenameEncoding);
            statusStrip1.Items.Remove(cboFilenameEncoding);
            if (!statusStrip1.Items.Contains(_filenameEncodingStatusLabel))
            {
                int pakKeyIndex = statusStrip1.Items.IndexOf(lblPakKey);
                statusStrip1.Items.Insert(pakKeyIndex + 1, _filenameEncodingStatusLabel);
            }

            _toolbarLayout.Controls.Add(_pakOperationsToolbar, 0, 0);

            Controls.Remove(readerPanel);
            Controls.Remove(statusStrip1);

            readerPanel.Dock = DockStyle.Fill;
            statusStrip1.Dock = DockStyle.Fill;
            _rootLayout.Controls.Add(_toolbarLayout, 0, 0);
            _rootLayout.Controls.Add(readerPanel, 0, 1);
            _rootLayout.Controls.Add(statusStrip1, 0, 2);
            Controls.Add(_rootLayout);

            EmphasizeOpenPakEntryPoint();
            InstallOpenedPakFileToolbar();
            HideLegacyActionButtons();
            UpdateToolbarEnabledState();
            _toolbarLayout.PerformLayout();
            ClientSize = new Size(ClientSize.Width, originalClientHeight + _toolbarLayout.PreferredSize.Height);
            PerformLayout();
            ResizeExtractListArea();
            ResumeLayout(true);
        }

        private void EmphasizeOpenPakEntryPoint()
        {
            txtPakPath.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnBrowsePak.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowsePak.Size = new Size(104, 28);
            btnBrowsePak.BackColor = Color.FromArgb(0, 122, 204);
            btnBrowsePak.ForeColor = Color.White;
            btnBrowsePak.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnBrowsePak.UseVisualStyleBackColor = false;
            readerPanel.Resize += ReaderPanel_Resize;
            LayoutOpenPakEntryPoint();

            groupHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupHeader.Width = readerPanel.ClientSize.Width - (groupHeader.Left * 2);
            lblCurrentPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lstEntries.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tvFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        }

        private void ReaderPanel_Resize(object? sender, EventArgs e)
        {
            LayoutOpenPakEntryPoint();
            groupHeader.Width = Math.Max(1,
                readerPanel.ClientSize.Width - (groupHeader.Left * 2));
            if (_openedPakFileToolbar != null)
                _openedPakFileToolbar.Width = Math.Max(1,
                    readerPanel.ClientSize.Width - (txtPakPath.Left * 2));
            ResizeExtractListArea();
        }

        private void LayoutOpenPakEntryPoint()
        {
            const int gap = 10;
            int visibleRight = readerPanel.ClientSize.Width - readerPanel.Padding.Right;
            int buttonLeft = Math.Max(readerPanel.Padding.Left, visibleRight - btnBrowsePak.Width);
            btnBrowsePak.Location = new Point(buttonLeft, txtPakPath.Top - 2);
            txtPakPath.Width = Math.Max(40, buttonLeft - gap - txtPakPath.Left);
        }

        private void InstallOpenedPakFileToolbar()
        {
            if (_openedPakFileToolbar == null)
                return;

            _openedPakFileToolbar.Dock = DockStyle.None;
            _openedPakFileToolbar.Location = new Point(txtPakPath.Left, txtPakPath.Bottom + 8);
            _openedPakFileToolbar.Width = readerPanel.ClientSize.Width - (txtPakPath.Left * 2);
            _openedPakFileToolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            readerPanel.Controls.Add(_openedPakFileToolbar);
            readerPanel.Controls.SetChildIndex(_openedPakFileToolbar, 0);

            _openedPakFileToolbar.PerformLayout();
            int toolbarBottom = _openedPakFileToolbar.Bottom + 8;
            int delta = Math.Max(0, toolbarBottom - groupHeader.Top);
            if (delta == 0)
                return;

            MoveExtractControlDown(groupHeader, delta);
            MoveExtractControlDown(lblSearch, delta);
            MoveExtractControlDown(txtSearch, delta);
            MoveExtractControlDown(lblCurrentPath, delta);
            MoveExtractControlDown(tvFolders, delta);
            MoveExtractControlDown(lstEntries, delta);
        }

        private static void MoveExtractControlDown(Control control, int delta)
        {
            control.Location = new Point(control.Left, control.Top + delta);
        }

        private static ToolStrip CreateToolbar(bool officeStyle) => new()
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = officeStyle ? new Size(32, 32) : new Size(20, 20),
            LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
            Padding = officeStyle ? new Padding(3, 3, 3, 4) : Padding.Empty,
            Stretch = true
        };

        private ToolStripLabel AddSectionLabel(ToolStrip toolbar)
        {
            var label = new ToolStripLabel
            {
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(8, 1, 4, 2)
            };
            toolbar.Items.Add(label);
            return label;
        }

        private ToolStripButton AddOperationButton(
            ToolStrip toolbar,
            EventHandler clickHandler,
            ToolbarIconKind iconKind,
            bool requiresLoadedPak = true)
        {
            var button = new ToolStripButton
            {
                AutoSize = false,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = CreateToolbarIcon(iconKind),
                ImageScaling = ToolStripItemImageScaling.None,
                Margin = new Padding(2, 1, 2, 2),
                TextImageRelation = TextImageRelation.ImageAboveText
            };
            button.Click += clickHandler;
            toolbar.Items.Add(button);
            _operationToolbarItems.Add(button);
            _officeToolbarButtons.Add(button);
            if (requiresLoadedPak)
                _requiresLoadedPakToolbarItems.Add(button);
            ApplyToolbarButtonDisplay(button);
            return button;
        }

        private void ApplyToolbarTextVisibility()
        {
            foreach (ToolStripButton button in _officeToolbarButtons)
                ApplyToolbarButtonDisplay(button);

            _pakOperationsToolbar?.PerformLayout();
            _openedPakFileToolbar?.PerformLayout();
            if (_openedPakFileToolbar != null)
                _openedPakFileToolbar.Width = readerPanel.ClientSize.Width - (txtPakPath.Left * 2);
        }

        private void ApplyToolbarButtonDisplay(ToolStripButton button)
        {
            button.DisplayStyle = _showToolbarText
                ? ToolStripItemDisplayStyle.ImageAndText
                : ToolStripItemDisplayStyle.Image;
            button.TextImageRelation = TextImageRelation.ImageAboveText;
            button.Size = _showToolbarText ? new Size(92, 62) : new Size(54, 48);
        }

        private static Bitmap CreateToolbarIcon(ToolbarIconKind iconKind)
        {
            var bitmap = new Bitmap(32, 32);
            using Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var primary = new SolidBrush(Color.FromArgb(0, 122, 204));
            using var green = new SolidBrush(Color.FromArgb(40, 167, 69));
            using var amber = new SolidBrush(Color.FromArgb(255, 193, 7));
            using var red = new SolidBrush(Color.FromArgb(220, 53, 69));
            using var purple = new SolidBrush(Color.FromArgb(111, 66, 193));
            using var white = new SolidBrush(Color.White);
            using var dark = new Pen(Color.FromArgb(70, 70, 70), 2F);
            using var whitePen = new Pen(Color.White, 2.3F)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };

            switch (iconKind)
            {
                case ToolbarIconKind.New:
                    g.FillRectangle(white, 7, 3, 18, 26);
                    g.DrawRectangle(dark, 7, 3, 18, 26);
                    g.DrawLine(dark, 19, 3, 25, 9);
                    g.DrawLine(dark, 19, 3, 19, 9);
                    g.DrawLine(dark, 19, 9, 25, 9);
                    g.DrawLine(whitePen, 16, 14, 16, 24);
                    g.DrawLine(whitePen, 11, 19, 21, 19);
                    using (var newPen = new Pen(Color.FromArgb(40, 167, 69), 3F))
                    {
                        g.DrawLine(newPen, 16, 14, 16, 24);
                        g.DrawLine(newPen, 11, 19, 21, 19);
                    }
                    break;
                case ToolbarIconKind.Folder:
                    g.FillRectangle(amber, 3, 10, 26, 17);
                    g.FillRectangle(amber, 5, 6, 10, 6);
                    g.DrawRectangle(dark, 3, 10, 26, 17);
                    break;
                case ToolbarIconKind.Update:
                    g.FillEllipse(amber, 5, 5, 22, 22);
                    g.DrawArc(whitePen, 8, 8, 16, 16, 30, 260);
                    g.FillPolygon(white, [new Point(22, 8), new Point(27, 8), new Point(24, 13)]);
                    break;
                case ToolbarIconKind.Settings:
                    g.FillEllipse(purple, 5, 5, 22, 22);
                    g.FillEllipse(white, 11, 11, 10, 10);
                    using (var settingsPen = new Pen(Color.FromArgb(111, 66, 193), 3F))
                    {
                        for (int angle = 0; angle < 360; angle += 45)
                        {
                            double radians = angle * Math.PI / 180D;
                            g.DrawLine(settingsPen,
                                16 + (float)(Math.Cos(radians) * 9),
                                16 + (float)(Math.Sin(radians) * 9),
                                16 + (float)(Math.Cos(radians) * 13),
                                16 + (float)(Math.Sin(radians) * 13));
                        }
                    }
                    break;
                case ToolbarIconKind.ExtractAll:
                    g.FillRectangle(green, 5, 5, 22, 22);
                    g.DrawLine(whitePen, 16, 8, 16, 22);
                    g.DrawLine(whitePen, 10, 16, 16, 22);
                    g.DrawLine(whitePen, 22, 16, 16, 22);
                    break;
                case ToolbarIconKind.Key:
                    g.FillEllipse(purple, 5, 10, 12, 12);
                    g.DrawEllipse(whitePen, 8, 13, 6, 6);
                    g.DrawLine(whitePen, 16, 16, 28, 16);
                    g.DrawLine(whitePen, 23, 16, 23, 21);
                    break;
                case ToolbarIconKind.Encoding:
                    g.FillRectangle(primary, 5, 6, 22, 20);
                    using (var font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Pixel))
                    {
                        g.DrawString("ABC", font, white, new PointF(8, 9));
                        g.DrawString("01", font, white, new PointF(11, 18));
                    }
                    break;
                case ToolbarIconKind.ExtractSelected:
                    g.FillRectangle(primary, 7, 4, 18, 24);
                    g.FillPolygon(white, [new Point(16, 23), new Point(9, 15), new Point(14, 15), new Point(14, 8), new Point(18, 8), new Point(18, 15), new Point(23, 15)]);
                    break;
                case ToolbarIconKind.Rename:
                    g.FillRectangle(primary, 5, 9, 22, 14);
                    g.DrawLine(whitePen, 9, 16, 23, 16);
                    g.DrawLine(whitePen, 11, 12, 11, 20);
                    break;
                case ToolbarIconKind.Remove:
                    g.FillEllipse(red, 5, 5, 22, 22);
                    g.DrawLine(whitePen, 11, 11, 21, 21);
                    g.DrawLine(whitePen, 21, 11, 11, 21);
                    break;
            }

            return bitmap;
        }

        private void HideLegacyActionButtons()
        {
            btnExtractSelected.Visible = false;
            btnRemoveSelected.Visible = false;
            btnBatchExtract.Visible = false;
            btnUpdatePak.Visible = false;
            btnExtractAll.Visible = false;
            btnChangeKey.Visible = false;
        }

        private void ResizeExtractListArea()
        {
            int bottom = readerPanel.ClientSize.Height - readerPanel.Padding.Bottom;
            int visibleRight = readerPanel.ClientSize.Width - readerPanel.Padding.Right;
            int listLeft = tvFolders.Right + 6;
            int availableHeight = Math.Max(1, bottom - lstEntries.Top);

            tvFolders.Height = Math.Max(1, bottom - tvFolders.Top);
            lstEntries.Bounds = new Rectangle(
                listLeft,
                lstEntries.Top,
                Math.Max(1, visibleRight - listLeft),
                availableHeight);
            lstEntries.BringToFront();
            EnsureEntryListScrollBar();
        }

        private void ApplyToolbarLocalization()
        {
            if (_pakOperationsLabel != null) _pakOperationsLabel.Text = Strings.PakMaker_PakOperations;
            if (_fileOperationsLabel != null) _fileOperationsLabel.Text = Strings.PakMaker_FileOperations;
            if (_toolbarNewPak != null)
            {
                _toolbarNewPak.Text = Strings.Pak_New;
                _toolbarNewPak.ToolTipText = Strings.Pak_CreateEmpty;
            }
            if (_toolbarFilenameEncoding != null)
            {
                _toolbarFilenameEncoding.Text = TrimToolbarLabel(Strings.Pak_FilenameEncoding);
                _toolbarFilenameEncoding.ToolTipText = Strings.Pak_FilenameEncodingTooltip;
            }
            if (_toolbarBatchExtract != null) _toolbarBatchExtract.Text = Strings.Pak_BatchExtract;
            if (_toolbarUpdatePak != null) _toolbarUpdatePak.Text = Strings.Pak_Update;
            if (_toolbarEditPakSettings != null) _toolbarEditPakSettings.Text = Strings.Pak_Settings;
            if (_toolbarExtractAll != null) _toolbarExtractAll.Text = Strings.Pak_ExtractAll;
            if (_toolbarChangePakKey != null) _toolbarChangePakKey.Text = Strings.Pak_ChangeKey;
            if (_toolbarExtractSelected != null) _toolbarExtractSelected.Text = Strings.Pak_ExtractSelected;
            if (_toolbarRenameSelected != null) _toolbarRenameSelected.Text = Strings.Pak_RenameSelected;
            if (_toolbarRemoveSelected != null) _toolbarRemoveSelected.Text = Strings.Pak_RemoveSelected;
            foreach (ToolStripButton button in _officeToolbarButtons)
            {
                if (!ReferenceEquals(button, _toolbarNewPak))
                    button.ToolTipText = button.Text;
            }
        }

        private static string TrimToolbarLabel(string text) => text.Trim().TrimEnd(':');

        /// <summary>
        /// Inicializa o menu de contexto específico para a TreeView de pastas.
        /// </summary>
        private void SetupFolderContextMenu()
        {
            ContextMenuStrip folderContextMenu = new ContextMenuStrip();
            _menuCreateFolder = new ToolStripMenuItem(Strings.PakMaker_CreateFolder);
            _menuCreateFolder.Click += (_, _) => BeginCreateFolder();
            _menuExtractFolder = new ToolStripMenuItem(Strings.PakMaker_ExtractThisFolder);
            _menuExtractFolder.Click += async (s, e) => await ExtractSelectedFolderAsync();

            _menuRemoveFolder = new ToolStripMenuItem(Strings.PakMaker_RemoveThisFolderFromPAK);
            _menuRemoveFolder.Click += async (s, e) => await RemoveFolderFromTreeAsync(); 
            _menuRename = new ToolStripMenuItem(Strings.PakMaker_RenameThisPAK);
            _menuRename.Click += (s, e) => {
                if (tvFolders.SelectedNode != null)
                    tvFolders.SelectedNode.BeginEdit();
            };
            folderContextMenu.Items.Add(_menuCreateFolder);
            folderContextMenu.Items.Add(new ToolStripSeparator());
            folderContextMenu.Items.Add(_menuRename); 
            folderContextMenu.Items.Add(_menuExtractFolder);
            folderContextMenu.Items.Add(_menuRemoveFolder);
            tvFolders.ContextMenuStrip = folderContextMenu; 
        }

        // ─── ARRASTAR PARA FORA (DRAG-OUT / EXPORTAÇÃO RÁPIDA) ─────────────────

        /// <summary>
        /// Extrai as entries fornecidas para uma pasta temporária e devolve os caminhos de
        /// nível superior (arquivos soltos na raiz da pasta temp), prontos para iniciar um
        /// DoDragDrop nativo com DataFormats.FileDrop.
        /// </summary>
        private string[]? ExtractEntriesToTempForDrag(List<PakFileEntry> entries, string rootToStrip)
        {
            if (_currentReader == null || entries.Count == 0) return null;

            string tempDir = Path.Combine(Path.GetTempPath(), "PangYaSuiteTools_DragExport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            foreach (var entry in entries)
            {
                string relativePath = _currentReader.GetExtractionName(entry).Replace('\\', '/');
                if (!string.IsNullOrEmpty(rootToStrip) && relativePath.StartsWith(rootToStrip, StringComparison.OrdinalIgnoreCase))
                    relativePath = relativePath.Substring(rootToStrip.Length);

                string outPath = Path.Combine(tempDir, relativePath.Replace('/', '\\'));
                string? fileDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                _currentReader.ExtractEntry(entry, outPath, writeManifest: false);
            }

            // Devolve apenas os itens que ficaram no nível raiz da pasta temp (arquivos e/ou subpastas)
            return Directory.GetFileSystemEntries(tempDir);
        }

        /// <summary>Apaga pastas temporárias de drag-out deixadas por execuções anteriores (best-effort).</summary>
        private static void CleanupOldTempDragFolders()
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(Path.GetTempPath(), "PangYaSuiteTools_DragExport_*"))
                {
                    try { Directory.Delete(dir, true); } catch { /* ainda em uso, ignora */ }
                }
            }
            catch { /* ignora falhas de acesso ao %TEMP% */ }
        }

        /// <summary>Arrastar arquivo(s) selecionado(s) na ListView para fora do app (Explorer, etc.) extrai-os direto no destino.</summary>
        private async void LstEntries_ItemDrag(object? sender, ItemDragEventArgs e)
        {

            if (_currentReader == null || lstEntries.SelectedItems.Count == 0) return;

            // Filtra as entradas válidas que estão selecionadas
            var selectedEntries = lstEntries.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag)
                .OfType<PakFileEntry>()
                .Where(en => en.Type != PakFileEntryType.Directory)
                .ToList();

            if (selectedEntries.Count == 0) return;

            // Pasta temporária para onde faremos a extração rápida antes de entregar ao Windows Explorer
            string tempSessionDir = Path.Combine(Path.GetTempPath(), "PangYaSuiteTools_DragDrop", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempSessionDir);

            List<string> filesToDrop = new();

            // Extrai rapidamente em background
            lblStatus.Text = Strings.PakMaker_PreparingFilesForDragging;
            await Task.Run(() =>
            {
                foreach (var entry in selectedEntries)
                {
                    // Salva na pasta temporária mantendo apenas o nome do arquivo
                    string suggestedName = Path.GetFileName(entry.Name.Replace('/', '\\'));
                    string outPath = Path.Combine(tempSessionDir, suggestedName);

                    _currentReader.ExtractEntry(entry, outPath, writeManifest: false);
                    filesToDrop.Add(outPath);
                }
            });
            lblStatus.Text = Strings.PakMaker_Ready;

            // Executa a operação nativa do Windows de arrastar e soltar arquivos físicos
            var dataObject = new DataObject(DataFormats.FileDrop, filesToDrop.ToArray());
            DoDragDrop(dataObject, DragDropEffects.Copy);
        }

        /// <summary>Arrastar uma pasta da TreeView para fora do app extrai a pasta inteira (com subpastas) direto no destino.</summary>
        private async void TvFolders_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (_currentReader == null || tvFolders.SelectedNode == null) return;

            // Usa o Tag do nó (caminho real da pasta, sem emojis/localização) em vez de
            // fazer parsing do texto exibido — muito mais confiável.
            string folderTag = tvFolders.SelectedNode.Tag as string ?? "";
            string cleanPath = folderTag.Replace('\\', '/'); // "" = raiz (todos os arquivos)

            List<PakFileEntry> entriesToExtract;
            string rootToStrip = "";

            if (string.IsNullOrWhiteSpace(cleanPath))
            {
                entriesToExtract = _currentReader.Entries.Where(en => en.Type != PakFileEntryType.Directory).ToList();
            }
            else
            {
                string prefix = cleanPath.Trim('/') + "/";

                // rootToStrip = tudo até a pasta selecionada (inclusive), preservando só
                // o que vem DEPOIS dela. Ex.: selecionado "data/map" -> rootToStrip = "data/map/"
                rootToStrip = prefix;

                entriesToExtract = _currentReader.Entries
                    .Where(en => en.Type != PakFileEntryType.Directory &&
                                 en.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (entriesToExtract.Count == 0) return;

            string tempSessionDir = Path.Combine(Path.GetTempPath(), "PangYaSuiteTools_DragDrop", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempSessionDir);

            List<string> directoriesToDrop = new();

            // Nome da pasta raiz a ser arrastada (só o último segmento, ex.: "map")
            string selectedFolderName = string.IsNullOrEmpty(cleanPath) ? "" : Path.GetFileName(cleanPath.TrimEnd('/'));

            lblStatus.Text = Strings.PakMaker_PreparingFolderStructureForDragging;
            await Task.Run(() =>
            {
                foreach (var entry in entriesToExtract)
                {
                    string relativePath = _currentReader.GetExtractionName(entry).Replace('\\', '/');

                    if (!string.IsNullOrEmpty(rootToStrip) && relativePath.StartsWith(rootToStrip, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = relativePath.Substring(rootToStrip.Length);
                    }

                    // Reanexa o nome da pasta selecionada como raiz da estrutura arrastada
                    // (ex.: "map/arquivos/file.ext" em vez de só "arquivos/file.ext")
                    if (!string.IsNullOrEmpty(selectedFolderName))
                    {
                        relativePath = selectedFolderName + "/" + relativePath;
                    }

                    string localRelativePath = relativePath.Replace('/', '\\');
                    string outPath = Path.Combine(tempSessionDir, localRelativePath);

                    string? fileDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                    {
                        Directory.CreateDirectory(fileDir);
                    }

                    _currentReader.ExtractEntry(entry, outPath, writeManifest: false);
                }

                string firstLevelDir = Path.Combine(tempSessionDir, selectedFolderName);
                if (!string.IsNullOrEmpty(selectedFolderName) && Directory.Exists(firstLevelDir))
                {
                    directoriesToDrop.Add(firstLevelDir);
                }
                else
                {
                    // Raiz "Todos os Arquivos" — pega tudo que foi extraído direto no tempSessionDir
                    directoriesToDrop.AddRange(Directory.GetDirectories(tempSessionDir));
                    directoriesToDrop.AddRange(Directory.GetFiles(tempSessionDir));
                }
            });
            lblStatus.Text = Strings.PakMaker_Ready;

            if (directoriesToDrop.Count > 0)
            {
                var dataObject = new DataObject(DataFormats.FileDrop, directoriesToDrop.ToArray());
                DoDragDrop(dataObject, DragDropEffects.Copy);
            }
        }

        private void SetupContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            _menuExtractSingle = new ToolStripMenuItem(Strings.PakMaker_ExtractSelectedItemS);
            _menuExtractSingle.Click += async (s, e) => await ExtractSelectedAsync();

            _menuRenameSingle = new ToolStripMenuItem(Strings.PakMaker_RenameSelectedFile);
            _menuRenameSingle.Click += (s, e) => BeginSelectedFileRename();

            _menuRemoveSingle = new ToolStripMenuItem(Strings.PakMaker_RemoveSelectedItemSFromPAK);
            _menuRemoveSingle.Click += async (s, e) => await RemoveSelectedAsync();

            contextMenu.Items.Add(_menuExtractSingle);
            contextMenu.Items.Add(_menuRenameSingle);
            contextMenu.Items.Add(_menuRemoveSingle);
            lstEntries.ContextMenuStrip = contextMenu; // Vincula o menu à ListView
        }

        private void BeginSelectedFileRename()
        {
            if (lstEntries.SelectedItems.Count != 1)
            {
                MessageBox.Show(Strings.PakMaker_SelectSingleFileToRename, Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem item = lstEntries.SelectedItems[0];
            if (item.Tag is not PakFileEntry entry || entry.Type == PakFileEntryType.Directory)
            {
                MessageBox.Show(Strings.PakMaker_SelectSingleFileToRename, Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            item.BeginEdit();
        }

        private void LstEntries_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F2) return;

            e.Handled = true;
            BeginSelectedFileRename();
        }

        private async void LstEntries_AfterLabelEdit(object? sender, LabelEditEventArgs e)
        {
            if (e.Label == null) return;
            e.CancelEdit = true;

            if (e.Item < 0 || e.Item >= lstEntries.Items.Count) return;
            ListViewItem item = lstEntries.Items[e.Item];
            if (item.Tag is not PakFileEntry entry || entry.Type == PakFileEntryType.Directory) return;

            string newName = e.Label.Trim();
            string oldName = Path.GetFileName(entry.Name.Replace('/', '\\'));
            if (string.Equals(oldName, newName, StringComparison.Ordinal)) return;

            if (!IsValidLeafFileName(newName))
            {
                MessageBox.Show(Strings.PakMaker_InvalidRenameName, Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await ExecuteFileRename(entry, newName);
        }

        private static bool IsValidLeafFileName(string fileName) =>
            !string.IsNullOrWhiteSpace(fileName) &&
            string.Equals(fileName, fileName.Trim(), StringComparison.Ordinal) &&
            fileName is not "." and not ".." &&
            !fileName.Contains('/') &&
            !fileName.Contains('\\') &&
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        private void InitializeFilenameEncodingComboBox()
        {
            List<PakEncodingOption> encodings =
                PakFilenameEncodingPreferences.GetAvailableEncodings().ToList();
            PakEncodingOption selectedEncoding = SelectFilenameEncodingOption(
                encodings,
                PakFilenameEncodingPreferences.LoadCodePage());
            _selectedFilenameEncodingCodePage = selectedEncoding.CodePage;

            _isInitializingFilenameEncoding = true;
            cboFilenameEncoding.ComboBox.DisplayMember = nameof(PakEncodingOption.Label);
            cboFilenameEncoding.ComboBox.ValueMember = nameof(PakEncodingOption.CodePage);
            cboFilenameEncoding.ComboBox.DataSource = null;
            cboFilenameEncoding.ComboBox.Items.Clear();
            cboFilenameEncoding.ComboBox.Items.AddRange(encodings.Cast<object>().ToArray());
            SelectFilenameEncodingComboItem(cboFilenameEncoding.ComboBox, selectedEncoding.CodePage);
            _isInitializingFilenameEncoding = false;
        }

        private Encoding SelectedFilenameEncoding =>
            PakFilenameEncodingPreferences.GetEncoding(_selectedFilenameEncodingCodePage);

        private void cboFilenameEncoding_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializingFilenameEncoding ||
                cboFilenameEncoding.SelectedItem is not PakEncodingOption option)
                return;

            SetFilenameEncoding(option);
        }

        private void ShowFilenameEncodingDialog()
        {
            List<PakEncodingOption> encodings =
                PakFilenameEncodingPreferences.GetAvailableEncodings().ToList();
            _selectedFilenameEncodingCodePage = PakFilenameEncodingPreferences.LoadCodePage();
            PakEncodingOption selectedEncoding = SelectFilenameEncodingOption(
                encodings,
                _selectedFilenameEncodingCodePage);

            using var dialog = new Form
            {
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(430, 126),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                Text = TrimToolbarLabel(Strings.Pak_FilenameEncoding)
            };

            var label = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(406, 34),
                Text = Strings.Pak_FilenameEncodingTooltip
            };

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 390,
                Location = new Point(12, 52),
                Size = new Size(406, 23),
                DisplayMember = nameof(PakEncodingOption.Label),
                ValueMember = nameof(PakEncodingOption.CodePage)
            };
            combo.Items.AddRange(encodings.Cast<object>().ToArray());
            SelectFilenameEncodingComboItem(combo, selectedEncoding.CodePage);

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(262, 88),
                Size = new Size(75, 26),
                Text = Strings.Options_Cancel
            };

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(343, 88),
                Size = new Size(75, 26),
                Text = Strings.Common_OK
            };

            dialog.Controls.Add(label);
            dialog.Controls.Add(combo);
            dialog.Controls.Add(cancelButton);
            dialog.Controls.Add(okButton);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) == DialogResult.OK &&
                combo.SelectedItem is PakEncodingOption selected)
            {
                SetFilenameEncoding(selected);
            }
        }

        private void SetFilenameEncoding(PakEncodingOption option)
        {
            _selectedFilenameEncodingCodePage = option.CodePage;
            PakFilenameEncodingPreferences.SaveCodePage(option.CodePage);

            if (cboFilenameEncoding.SelectedItem is not PakEncodingOption selected ||
                selected.CodePage != option.CodePage)
            {
                _isInitializingFilenameEncoding = true;
                SelectFilenameEncodingComboItem(cboFilenameEncoding.ComboBox, option.CodePage);
                _isInitializingFilenameEncoding = false;
            }

            if (_currentReader != null)
                lblStatus.Text = Strings.Pak_EncodingAppliesNextLoad;
            UpdateDisplayedFilenameEncoding();
        }

        private static void SelectFilenameEncodingComboItem(ComboBox combo, int codePage)
        {
            int fallbackIndex = -1;
            for (int index = 0; index < combo.Items.Count; index++)
            {
                if (combo.Items[index] is not PakEncodingOption option)
                    continue;

                if (option.CodePage == PakFilenameEncodingPreferences.DefaultCodePage)
                    fallbackIndex = index;

                if (option.CodePage == codePage)
                {
                    combo.SelectedIndex = index;
                    return;
                }
            }

            if (fallbackIndex >= 0)
                combo.SelectedIndex = fallbackIndex;
            else if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private static PakEncodingOption SelectFilenameEncodingOption(
            IReadOnlyList<PakEncodingOption> encodings,
            int codePage)
        {
            return encodings.FirstOrDefault(option => option.CodePage == codePage)
                ?? encodings.FirstOrDefault(option => option.CodePage == PakFilenameEncodingPreferences.DefaultCodePage)
                ?? encodings.First();
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

        private async void FrmPakMaker_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null) return;

            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                txtPakPath.BackColor = SystemColors.Control;
                if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
                {
                    string firstPath = files[0];
                    if (File.Exists(firstPath) && firstPath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                    {
                        txtPakPath.Text = firstPath;
                        LoadPak(firstPath);
                    }
                    else
                    {
                        MessageBox.Show(
                            Strings.PakMaker_PleaseDragAValidPakFile,
                            Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    List<PakInjectItem> itemsToInject = BuildPakInjectItems(files, GetSelectedArchiveFolder());

                    if (itemsToInject.Count == 0)
                    {
                        MessageBox.Show(
                            Strings.PakMaker_NoValidFilesOrFoldersWere,
                            Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    await InjectFilesIntoCurrentPakAsync(itemsToInject);
                }
            }
        }

        // ─── ABA 1: LEITURA E EXTRAÇÃO ─────────────────────────────────────────
        private void btnBrowsePak_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = FileDialogFactory.CreatePakOpenDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileDialogFactory.RememberDirectory(FileDialogKind.Pak, openFileDialog.FileName);
                txtPakPath.Text = openFileDialog.FileName;
                LoadPak(openFileDialog.FileName);
            }
        }

        private void LoadPak(string path, Encoding? filenameEncoding = null) =>
            LoadPakCore(path, filenameEncoding, forceKeys: null);

        private void LoadPakWithKeys(string path, Encoding filenameEncoding, uint[] forceKeys) =>
            LoadPakCore(path, filenameEncoding, forceKeys);

        private void ReloadPak(string path, Encoding filenameEncoding,
                               byte? knownCompressionLevel) =>
            LoadPakCore(path, filenameEncoding, forceKeys: null,
                knownCompressionLevel: knownCompressionLevel);

        private void LoadPakCore(string path, Encoding? filenameEncoding, uint[]? forceKeys,
                                 byte? knownCompressionLevel = null)
        {
            AppLogger.Instance.Log("PAK Manager", $"PAK load requested: '{path}'.");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                AppLogger.Instance.Log("PAK Manager", $"PAK load stopped because the file does not exist: '{path}'.", AppLogLevel.Warning);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(txtPakPath.Text))
                    txtPakPath.Text = path;

                _currentReader?.Dispose();
                var reader = new PakReader(path, filenameEncoding ?? SelectedFilenameEncoding, AppLogger.Instance);
                _currentReader = reader;
                reader.Parse(forceKeys);
                _knownCompressionLevel = knownCompressionLevel;
                UpdateUnusualNameDiagnostics(reader);
                UpdateDisplayedPakKey();
                UpdateDisplayedCompressionLevel();
                UpdateToolbarEnabledState();
                if (reader.LocationKeys?.SequenceEqual(PakKeys.SS) == true)
                {
                    AppLogger.Instance.Log("PAK Manager",
                        $"Loaded '{path}' with the unsupported pakkeys.ss key.", AppLogLevel.Warning);
                    MessageBox.Show(Strings.PakMaker_SSKeyNotSupported,
                        Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                // Atualiza as Labels de informação do Header
                txtUpdateAuthor.Text = reader.Header.Author;
                lblAuthor.Text = $"{Strings.PakMaker_Author} {reader.Header.Author}";
                lblVersion.Text = $"{Strings.PakMaker_Version} 0x{reader.Header.Version:X2}";
                lblEntries.Text = $"{Strings.PakMaker_Entries} {reader.Header.NumFileEntry}";

                txtSearch.Text = "";
                BuildFolderTree();
                AppLogger.Instance.Log("PAK Manager", $"Loaded '{path}' successfully with {reader.Entries.Count} entries.");
                ShowUnusualNameWarning();
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"Failed to load '{path}': {ex}", AppLogLevel.Error);
                MessageBox.Show($"{Strings.PakMaker_ErrorOpeningPAKFile}\n{ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _currentReader?.Dispose();
                _currentReader = null;
                _knownCompressionLevel = null;
                UpdateDisplayedPakKey();
                UpdateDisplayedCompressionLevel();
                UpdateToolbarEnabledState();
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

            var rootNode = new TreeNode(Strings.PakMaker_AllFiles) { Tag = RootFolderTag };
            tvFolders.Nodes.Add(rootNode);
            _folderNodes[RootFolderTag] = rootNode;

            if (_currentReader != null)
            {
                // Garante que toda pasta (mesmo sem uma entry "Directory" explícita) exista na árvore
                foreach (var entry in _currentReader.Entries)
                {
                    string folder = entry.Type == PakFileEntryType.Directory
                        ? entry.Name.Replace('/', '\\').TrimEnd('\\')
                        : Path.GetDirectoryName(entry.Name.Replace('/', '\\')) ?? "";
                    EnsureFolderNode(folder);
                }
            }

            tvFolders.EndUpdate();
            tvFolders.SelectedNode = rootNode;
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

        /// <summary>
        /// Garante que o clique com o botão direito selecione o nó antes de abrir o menu suspenso.
        /// </summary>
        private void TvFolders_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            tvFolders.SelectedNode = e.Node;
        }

        /// <summary>
        /// Captura o botão "Delete" do teclado para acionar a remoção da pasta selecionada.
        /// </summary>
        private async void TvFolders_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true; // Evita bipes do Windows
                await RemoveFolderFromTreeAsync();
            }
        }

        private void TvFolders_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (_currentReader == null) return;

            string folderTag = e.Node?.Tag as string ?? RootFolderTag;
            string normalizedFolder = folderTag.Replace('\\', '/').Trim('/');
            string prefix = string.IsNullOrEmpty(normalizedFolder) ? string.Empty : normalizedFolder + "/";

            _scopedEntries = string.IsNullOrEmpty(folderTag)
                ? [.. _currentReader.Entries.Where(en => en.Type != PakFileEntryType.Directory)]
                : _currentReader.Entries
                    .Where(en => en.Type != PakFileEntryType.Directory &&
                        en.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            lblCurrentPath.Text = string.IsNullOrEmpty(folderTag)
                ? Strings.PakMaker_PathAllFiles
                : $"{Strings.PakMaker_Path} {folderTag.Replace('\\', '/')}";

            txtSearch.Clear();
            ApplyDisplayFilter();
            foreach (ListViewItem item in lstEntries.Items) item.Selected = true;
            UpdateToolbarEnabledState();
            AppLogger.Instance.Log("PAK Manager",
                $"Selected folder '{(string.IsNullOrEmpty(folderTag) ? "/" : folderTag)}' and {_scopedEntries.Count} files below it.");
        }

        private void LstEntries_ItemSelectionChanged(object? sender, ListViewItemSelectionChangedEventArgs e)
        {
            UpdateToolbarEnabledState();
        }

        private async void TvFolders_AfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
        {
            TreeNode? pendingFolderNode = _pendingFolderNode;
            if (pendingFolderNode != null && ReferenceEquals(e.Node, pendingFolderNode))
            {
                e.CancelEdit = true;
                TreeNode pendingNode = pendingFolderNode;
                _pendingFolderNode = null;

                if (string.IsNullOrWhiteSpace(e.Label))
                {
                    TreeNode? parent = pendingNode.Parent;
                    pendingNode.Remove();
                    if (parent != null) tvFolders.SelectedNode = parent;
                    return;
                }

                string folderName = e.Label.Trim();
                if (!IsValidFolderLeafName(folderName))
                {
                    MessageBox.Show(Strings.PakMaker_InvalidFolderName,
                        Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    pendingNode.Remove();
                    return;
                }

                string parentPath = pendingNode.Parent?.Tag as string ?? RootFolderTag;
                string targetPath = CombineArchiveFolders(parentPath, folderName);
                bool siblingExists = pendingNode.Parent?.Nodes.Cast<TreeNode>()
                    .Where(node => !ReferenceEquals(node, pendingNode))
                    .Any(node => NormalizeArchiveFolder(node.Tag as string ?? string.Empty)
                        .Equals(targetPath, StringComparison.OrdinalIgnoreCase)) == true;
                if (siblingExists)
                {
                    MessageBox.Show(Strings.PakMaker_FolderAlreadyExists,
                        Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    pendingNode.Remove();
                    return;
                }

                pendingNode.Text = "📁 " + folderName;
                pendingNode.Tag = targetPath;
                await CreateFolderInCurrentPakAsync(targetPath);
                return;
            }

            // Se o usuário cancelou a digitação ou enviou vazio, ignora
            if (string.IsNullOrWhiteSpace(e.Label) || _currentReader == null)
            {
                e.CancelEdit = true;
                return;
            }

            string newName = e.Label.Trim();

            // Valida caracteres proibidíssimos que quebrariam a indexação do PAK
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.Contains("/") || newName.Contains("\\"))
            {
                MessageBox.Show(Strings.PakMaker_Error, Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.CancelEdit = true;
                return;
            }

            TreeNode? node = e.Node;
            if (node == null)
            {
                e.CancelEdit = true;
                return;
            }

            // Evita renomear o nó raiz principal caso ele esteja selecionado
            if (node.Parent == null && (node.Tag?.ToString() == "" || node.FullPath == ""))
            {
                e.CancelEdit = true;
                return;
            }

            // Cancela o comportamento visual padrão do WinForms porque nossa função 
            // própria já reconstrói e recarrega toda a árvore de forma limpa direto do disco
            e.CancelEdit = true;

            // Dispara a nossa assinatura dedicada
            await ExecuteItemRename(node, newName);
        }

        private void BeginCreateFolder()
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show(Strings.PakMaker_SelectAnActivePakFileFirst,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_pendingFolderNode != null)
            {
                _pendingFolderNode.Remove();
                _pendingFolderNode = null;
            }

            TreeNode parent = tvFolders.SelectedNode ?? tvFolders.Nodes[0];
            var node = new TreeNode(Strings.PakMaker_NewFolderDefaultName)
            {
                Tag = CombineArchiveFolders(parent.Tag as string ?? RootFolderTag,
                    Strings.PakMaker_NewFolderDefaultName)
            };
            parent.Nodes.Add(node);
            parent.Expand();
            _pendingFolderNode = node;
            tvFolders.SelectedNode = node;
            node.BeginEdit();
        }

        private async Task CreateFolderInCurrentPakAsync(string targetPath)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text)) return;

            string pakPath = txtPakPath.Text;
            PakReader reader = _currentReader;
            PakRebuildOptions options = BuildRebuildOptionsForCurrentPak();
            lblStatus.Text = Strings.PakMaker_CreatingFolder;
            tvFolders.Enabled = false;
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                bool created = await Task.Run(() => PakManager.CreateDirectory(
                    pakPath, reader, targetPath, options,
                    log: message => AppLogger.Instance.Log("PAK Manager", message),
                    onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK),
                    SaveBck: false, cancellationToken: operation.Token));

                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
                TreeNode? targetNode = FindFolderNode(targetPath);
                if (targetNode != null)
                {
                    tvFolders.SelectedNode = targetNode;
                    targetNode.EnsureVisible();
                }

                if (created)
                    lblStatus.Text = Strings.PakMaker_FolderCreated;
                else
                    MessageBox.Show(Strings.PakMaker_FolderAlreadyExists,
                        Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"Folder creation failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_CreateFolderFailed;
                MessageBox.Show($"{Strings.PakMaker_CreateFolderFailed} {ex.Message}",
                    Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            finally
            {
                HideProgress();
                tvFolders.Enabled = true;
                EndOperation(operation);
            }
        }

        private TreeNode? FindFolderNode(string folderPath)
        {
            string normalized = NormalizeArchiveFolder(folderPath);
            return _folderNodes.Values.FirstOrDefault(node =>
                NormalizeArchiveFolder(node.Tag as string ?? string.Empty)
                    .Equals(normalized, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsValidFolderLeafName(string name) =>
            !string.IsNullOrWhiteSpace(name) &&
            string.Equals(name, name.Trim(), StringComparison.Ordinal) &&
            name is not "." and not ".." &&
            !name.Contains('/') &&
            !name.Contains('\\') &&
            name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

        private void UpdateChildNodeTags(TreeNode parentNode, string newPrefix)
        {
            foreach (TreeNode child in parentNode.Nodes)
            {
                string currentPath = child.Tag?.ToString() ?? child.FullPath;
                string fileName = Path.GetFileName(currentPath);
                child.Tag = newPrefix + fileName;

                if (child.Nodes.Count > 0)
                {
                    UpdateChildNodeTags(child, child.Tag.ToString() + "/");
                }
            }
        } 

        private void tvFolders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2 && tvFolders.SelectedNode != null)
            {
                tvFolders.SelectedNode.BeginEdit();
            }
        }

        /// <summary>
        /// Executa a lógica de remoção da pasta atualmente selecionada na TreeView.
        /// </summary>
        private async Task RemoveFolderFromTreeAsync()
        {
            AppLogger.Instance.Log("PAK Manager", "Folder removal requested.");
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                AppLogger.Instance.Log("PAK Manager", "Folder removal stopped because no active PAK is loaded.", AppLogLevel.Warning);
                MessageBox.Show(Strings.PakMaker_SelectAnActivePakFileFirst,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tvFolders.SelectedNode == null)
            {
                AppLogger.Instance.Log("PAK Manager", "Folder removal stopped because no folder is selected.", AppLogLevel.Warning);
                return;
            }

            string folderTag = tvFolders.SelectedNode.Tag as string ?? "";

            // Evita deletar tudo caso esteja no nó raiz virtual sem querer
            if (string.IsNullOrEmpty(folderTag))
            {
                AppLogger.Instance.Log("PAK Manager", "Root-folder removal was rejected.", AppLogLevel.Warning);
                MessageBox.Show(Strings.PakMaker_YouCannotDeleteTheRootFolder,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string prefix = folderTag.Replace('\\', '/').Trim('/') + "/";

            // Coleta todos os arquivos que pertencem àquela estrutura de pasta
            var filesInFolder = _currentReader.Entries
                .Where(en => en.Type != PakFileEntryType.Directory &&
                             en.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(en => en.Name)
                .ToList();

            // Mensagem de confirmação específica para a pasta selecionada
            string folderName = tvFolders.SelectedNode.Text.Replace("📁 ", "");
            string confirmationMessage = string.Format(LocalizationManager.CurrentCulture,
                Strings.Pak_RemoveFolderConfirmation, folderName, filesInFolder.Count);

            var confirm = MessageBox.Show(
                $"{confirmationMessage}\n\n{Strings.PakMaker_ThePAKWillBeRebuiltAnd}",
                Strings.PakMaker_ConfirmRemoval, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                AppLogger.Instance.Log("PAK Manager", $"Removal of folder '{folderTag}' was cancelled.", AppLogLevel.Warning);
                return;
            }

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;
            using CancellationTokenSource operation = BeginOperation();

            lblStatus.Text = Strings.PakMaker_RemovingAndRebuildingPAK;
            tvFolders.Enabled = false; // Bloqueia a árvore durante o processo

            try
            {
                var options = BuildRebuildOptionsForCurrentPak();
                AppLogger.Instance.Log("PAK Manager",
                    $"Removing {filesInFolder.Count} files from folder '{folderTag}' in '{pakPath}'.");

                await Task.Run(() =>
                {
                    PakManager.RemoveDirectory(pakPath, reader, folderTag, options,
                        log: msg => AppLogger.Instance.Log("PAK Manager", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                lblStatus.Text = Strings.PakMaker_RemovalCompleted;
                AppLogger.Instance.Log("PAK Manager",
                    $"Removed folder '{folderTag}' and {filesInFolder.Count} files successfully.");
                MessageBox.Show(
                    Strings.PakMaker_TheFolderAndItsItemsWere,
                    Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"Folder removal failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_ErrorRemoving;
                MessageBox.Show($"{Strings.PakMaker_Failure} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                tvFolders.Enabled = true;
                EndOperation(operation);
            }
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

        private List<PakFileEntry> GetEntriesForFolder(string folderPath)
        {
            if (_currentReader == null) return new List<PakFileEntry>();

            // Padroniza as barras para a filtragem interna
            string cleanPath = folderPath.Replace('\\', '/').Trim('/');

            // Se estiver na raiz ("Todos os Arquivos" ou string vazia), retorna TUDO do PAK
            if (string.IsNullOrWhiteSpace(cleanPath) || cleanPath.Equals(Strings.PakMaker_AllFiles_2, StringComparison.OrdinalIgnoreCase))
            {
                return _currentReader.Entries
                    .Where(en => en.Type != PakFileEntryType.Directory)
                    .ToList();
            }

            // Se for uma pasta específica, garante o caractere '/' no final (Ex: "data/round20_abbot/")
            string prefix = cleanPath + "/";

            // Retorna apenas as entradas que começam com o caminho daquela pasta
            return [.. _currentReader.Entries
                .Where(en => en.Type != PakFileEntryType.Directory &&
                             en.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))];
        }

        /// <summary>Preenche a ListView com o conjunto de entries fornecido (sem tocar no escopo/pesquisa).</summary>
        private void PopulateList(IEnumerable<PakFileEntry> entries)
        {
            lstEntries.BeginUpdate();
            lstEntries.Items.Clear();
            lstEntries.ShowItemToolTips = true;
            foreach (var entry in entries)
            {
                // CORREÇÃO: Padroniza as barras e extrai apenas o nome do arquivo puro
                string cleanName = entry.Name.Replace('/', '\\');
                string displayName = Path.GetFileName(cleanName);

                // Caso seja uma pasta pura (se o seu PAK listar diretórios como entradas vazias)
                if (string.IsNullOrEmpty(displayName) && entry.Type == PakFileEntryType.Directory)
                {
                    displayName = Path.GetFileName(cleanName.TrimEnd('\\'));
                }

                var item = new ListViewItem(displayName); // Exibe apenas "data.iff"
                item.SubItems.Add(entry.Type.ToString());
                item.SubItems.Add($"0x{entry.Size:X8}");
                item.SubItems.Add($"0x{entry.CompressSize:X8}");

                item.Tag = entry; // O Tag continua guardando o objeto completo intacto (com o Name original do PAK)
                if (_unusualNameEntries.Contains(entry))
                {
                    item.BackColor = Color.LemonChiffon;
                    item.ForeColor = Color.DarkOrange;
                    item.ToolTipText = Strings.Pak_UnusualNamesTitle;
                }
                else if (entry.Type == PakFileEntryType.Directory)
                    item.ForeColor = Color.DarkCyan; // Pastas mantêm o tom ciano escuro
                else if (entry.Type == PakFileEntryType.LZ77)
                    item.ForeColor = Color.ForestGreen; // Arquivos compactados em LZ77 (Verde)
                else if (entry.Type == PakFileEntryType.Raw)
                    item.ForeColor = Color.DimGray; // Arquivos sem compressão / Brutos (Cinza discreto)
                else if (entry.Type == PakFileEntryType.LZ772)
                    item.ForeColor = Color.ForestGreen;  // Arquivos compactados do jogo em Verde

                lstEntries.Items.Add(item);
            }

            lstEntries.EndUpdate();
            EnsureEntryListScrollBar();
            UpdateToolbarEnabledState();
        }

        private void EnsureEntryListScrollBar()
        {
            if (!lstEntries.IsHandleCreated)
                _ = lstEntries.Handle;
            _ = ShowScrollBar(lstEntries.Handle, VerticalScrollBar, show: true);
        }

        private bool IsEntryListScrollBarVisible()
        {
            if (!lstEntries.IsHandleCreated) return false;
            return (GetWindowLong(lstEntries.Handle, WindowStyleIndex) & VerticalScrollBarStyle) != 0;
        }

        private void UpdateUnusualNameDiagnostics(PakReader reader)
        {
            _unusualNameEntries.Clear();
            IReadOnlyList<PakEntryNameIssue> issues = PakEntryNameDiagnostics.FindIssues(reader);
            if (issues.Count == 0) return;

            var names = new HashSet<string>(issues.Select(issue => issue.Name), StringComparer.Ordinal);
            foreach (PakFileEntry entry in reader.Entries)
            {
                string decoded = reader.FileNameEncoding.GetString(entry.NameRaw).Replace('\\', '/');
                if (names.Contains(decoded)) _unusualNameEntries.Add(entry);
            }

            foreach (PakEntryNameIssue issue in issues)
            {
                AppLogger.Instance.Log(
                    "PAK Manager",
                    $"Unusual entry name '{issue.Name}': {issue.Kind}.",
                    AppLogLevel.Warning);
            }
        }

        private void ShowUnusualNameWarning()
        {
            if (_unusualNameEntries.Count == 0) return;
            MessageBox.Show(
                string.Format(LocalizationManager.CurrentCulture,
                    Strings.Pak_UnusualNamesSummary, _unusualNameEntries.Count),
                Strings.Pak_UnusualNamesTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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

        private void UpdateDisplayedPakKey()
        {
            if (_currentReader == null)
            {
                lblPakKey.Text = string.Empty;
                lblPakKeySummary.Text = string.Empty;
                toolTip1.SetToolTip(lblPakKeySummary, null);
                UpdateDisplayedFilenameEncoding();
                return;
            }

            PakReader currentReader = _currentReader;
            uint[]? keys = currentReader.LocationKeys;
            if (keys is not { Length: > 0 })
            {
                PakFileEntryVersion? entryVersion = currentReader.Entries.FirstOrDefault()?.Version;
                string fallbackKeyName = entryVersion == PakFileEntryVersion.Raw
                    ? Strings.Pak_NoKey
                    : "XOR 0x71";
                string fallbackKeyText = $"{Strings.Pak_KeyUsed} {fallbackKeyName}";
                lblPakKey.Text = fallbackKeyText;
                lblPakKeySummary.Text = fallbackKeyText;
                toolTip1.SetToolTip(lblPakKeySummary, fallbackKeyText);
                UpdateDisplayedFilenameEncoding();
                return;
            }

            string keyName = PakKeys.All
                .FirstOrDefault(candidate => candidate.Keys.SequenceEqual(keys)).Label
                ?? Strings.Pak_CustomKey;
            string keyText = $"{Strings.Pak_KeyUsed} {keyName}";
            string keyValues = string.Join(" ", keys.Select(key => $"0x{key:X8}"));
            lblPakKey.Text = keyText;
            lblPakKeySummary.Text = keyText;
            toolTip1.SetToolTip(lblPakKeySummary, $"{keyText}: {keyValues}");
            UpdateDisplayedFilenameEncoding();
        }

        private void UpdateDisplayedCompressionLevel()
        {
            string value = _knownCompressionLevel.HasValue
                ? _knownCompressionLevel.Value.ToString(LocalizationManager.CurrentCulture)
                : Strings.Pak_CompressionLevelUnknown;
            string summary = string.Format(LocalizationManager.CurrentCulture,
                Strings.Pak_CompressionLevelSummaryFormat, value);
            lblCompressionLevelSummary.Text = summary;
            toolTip1.SetToolTip(lblCompressionLevelSummary, summary);
        }

        private void UpdateDisplayedFilenameEncoding()
        {
            Encoding encoding = _currentReader?.FileNameEncoding ?? SelectedFilenameEncoding;
            _filenameEncodingStatusLabel.Text =
                $"{TrimToolbarLabel(Strings.Pak_FilenameEncoding)}: {encoding.WebName} ({encoding.CodePage})";
        }

        private CancellationTokenSource BeginOperation()
        {
            _operationCancellation?.Dispose();
            _operationCancellation = new CancellationTokenSource();
            btnCancelOperation.Enabled = true;
            _operationInProgress = true;
            UpdateToolbarEnabledState();
            return _operationCancellation;
        }

        private void EndOperation(CancellationTokenSource operation)
        {
            if (!ReferenceEquals(_operationCancellation, operation)) return;
            btnCancelOperation.Enabled = false;
            _operationInProgress = false;
            _operationCancellation.Dispose();
            _operationCancellation = null;
            UpdateToolbarEnabledState();
        }

        private void UpdateToolbarEnabledState()
        {
            void Apply()
            {
                bool hasLoadedPak = _currentReader != null;
                bool canUsePak = hasLoadedPak && !_operationInProgress;
                bool canEditMetadata = canUsePak && _currentReader?.IsPspArchive != true;
                bool hasSelection = lstEntries.SelectedItems.Count > 0;
                bool hasSingleSelection = lstEntries.SelectedItems.Count == 1;

                foreach (ToolStripItem item in _requiresLoadedPakToolbarItems)
                    item.Enabled = canUsePak;

                if (_toolbarNewPak != null) _toolbarNewPak.Enabled = !_operationInProgress;
                if (_toolbarBatchExtract != null) _toolbarBatchExtract.Enabled = !_operationInProgress;
                btnBatchExtract.Enabled = !_operationInProgress;
                if (_toolbarExtractSelected != null) _toolbarExtractSelected.Enabled = canUsePak && hasSelection;
                if (_toolbarRenameSelected != null) _toolbarRenameSelected.Enabled = canEditMetadata && hasSingleSelection;
                if (_toolbarRemoveSelected != null) _toolbarRemoveSelected.Enabled = canEditMetadata && hasSelection;
                if (_toolbarEditPakSettings != null) _toolbarEditPakSettings.Enabled = canEditMetadata;
                if (_toolbarChangePakKey != null) _toolbarChangePakKey.Enabled = canEditMetadata;

            }

            if (InvokeRequired) Invoke(Apply);
            else Apply();
        }

        private void btnCancelOperation_Click(object? sender, EventArgs e)
        {
            if (_operationCancellation is null) return;
            btnCancelOperation.Enabled = false;
            _operationCancellation.Cancel();
            lblStatus.Text = Strings.Pak_OperationCancelled;
            AppLogger.Instance.Log("PAK Manager", "Cancellation requested by the user.", AppLogLevel.Warning);
        }

        private void HideProgress()
        {
            void Apply() => progressBar1.Visible = false;
            if (InvokeRequired) Invoke(Apply);
            else Apply();
        }

        // ─── EXTRAIR TUDO / SELECIONADO(S) / LOTE ──────────────────────────────

        private async void btnExtractAll_Click(object? sender, EventArgs e)
        {
            if (_currentReader == null)
            {
                MessageBox.Show(Strings.PakMaker_PleaseLoadAPakFileFirst, Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var folderDialog = new FolderBrowserDialog();
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string destination = folderDialog.SelectedPath;
                using CancellationTokenSource operation = BeginOperation();
                btnExtractAll.Enabled = false;
                lblStatus.Text = Strings.PakMaker_ExtractingFiles;

                try
                {
                    await Task.Run(() =>
                    {
                        _currentReader.Extract("*", destination, msg => AppLogger.Instance.Log("PAK Reader", msg),
                            (done, total) => ReportProgress(done, total, Strings.PakMaker_Extracting), operation.Token);
                    });

                    lblStatus.Text = Strings.PakMaker_Ready;
                    MessageBox.Show(Strings.PakMaker_AllFilesWereExtractedSuccessfully, Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = Strings.PakMaker_ExtractionError;
                    MessageBox.Show($"{Strings.PakMaker_ErrorExtracting} {ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    HideProgress();
                    btnExtractAll.Enabled = true;
                    EndOperation(operation);
                }
            }
        }

        private async void btnExtractSelected_Click(object? sender, EventArgs e) => await ExtractSelectedAsync();

        /// <summary>
        /// Extrai apenas os itens selecionados na ListView, usando o caminho rápido
        /// (PakReader.ExtractEntry), que reaproveita o stream já aberto — sem reabrir
        /// e reanalisar o .pak inteiro como acontecia antes.
        /// </summary>
        private async Task ExtractSelectedAsync()
        {
            if (_currentReader == null) return;

            // Se houver itens na lista da direita, prioriza a extração de arquivos individuais
            if (lstEntries.SelectedItems.Count > 0)
            {
                await ExtractOnlySelectedFilesAsync();
            }
            // Se não houver nada na lista, mas houver uma pasta na árvore, extrai a pasta
            else if (tvFolders.SelectedNode != null)
            {
                await ExtractSelectedFolderAsync();
            }
            else
            {
                MessageBox.Show(Strings.PakMaker_SelectFilesFromTheListOr,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Opção 1: Extrai estritamente a pasta selecionada na TreeView (e suas subpastas)
        /// </summary>
        private async Task ExtractSelectedFolderAsync()
        {
            if (_currentReader == null || tvFolders.SelectedNode == null) return;
            string cleanPath = (tvFolders.SelectedNode.Tag as string ?? string.Empty)
                .Replace('\\', '/').Trim('/');

            List<PakFileEntry> entriesToExtract;
            string dialogTitle;
            string rootToStrip = ""; // Guardará o que precisamos remover do caminho do arquivo

            if (string.IsNullOrWhiteSpace(cleanPath))
            {
                entriesToExtract = _currentReader.Entries
                    .Where(en => en.Type != PakFileEntryType.Directory)
                    .ToList();
                dialogTitle = Strings.PakMaker_EntirePAK;
            }
            else
            {
                string prefix = cleanPath.Trim('/') + "/";

                // Guardamos o caminho da pasta pai para remover da estrutura final
                // Se selecionou 'data/round20_abbot/ase', queremos manter apenas o que está de 'ase' para frente
                int lastSlash = cleanPath.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    rootToStrip = cleanPath.Substring(0, lastSlash + 1); // Ex: "data/round20_abbot/"
                }

                entriesToExtract = _currentReader.Entries
                    .Where(en => en.Type != PakFileEntryType.Directory &&
                                 en.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                dialogTitle = $"{Strings.PakMaker_Folder} {tvFolders.SelectedNode.Text.Replace("📁 ", "")}";
            }

            if (entriesToExtract.Count == 0)
            {
                MessageBox.Show($"{Strings.PakMaker_NoFileFoundForPath} {cleanPath}", Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = $"{Strings.PakMaker_SelectTheDestinationToExtract} {dialogTitle}"
            };
            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            // Passamos o 'rootToStrip' para o método que grava os arquivos
            await RunExtractionWithStripAsync(entriesToExtract, folderDialog.SelectedPath, rootToStrip);
        }

        private async Task RunExtractionWithStripAsync(List<PakFileEntry> entries, string destinationDir, string rootToStrip)
        {
            using CancellationTokenSource operation = BeginOperation();
            btnExtractSelected.Enabled = false;
            lblStatus.Text = Strings.PakMaker_ExtractingSelectedItemS;

            try
            {
                await Task.Run(() =>
                {
                    int total = entries.Count;
                    int done = 0;
                    var extractedFiles = new List<(PakFileEntry Entry, string OutputPath)>();

                    foreach (var entry in entries)
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        // Padroniza o nome do arquivo interno
                        string relativePath = _currentReader!.GetExtractionName(entry).Replace('\\', '/');

                        // Se o arquivo começar com a árvore de pastas que queremos cortar, removemos ela
                        if (!string.IsNullOrEmpty(rootToStrip) && relativePath.StartsWith(rootToStrip, StringComparison.OrdinalIgnoreCase))
                        {
                            relativePath = relativePath.Substring(rootToStrip.Length);
                        }

                        // Converte de volta para o padrão de barras do Windows (\)
                        string localRelativePath = relativePath.Replace('/', '\\');
                        string outPath = Path.Combine(destinationDir, localRelativePath);

                        // Garante que se houver subpastas internas a partir dali, elas sejam criadas
                        string? fileDir = Path.GetDirectoryName(outPath);
                        if (!string.IsNullOrEmpty(fileDir) && !Directory.Exists(fileDir))
                        {
                            Directory.CreateDirectory(fileDir);
                        }

                        // Extrai o arquivo na sua nova posição simplificada
                        _currentReader!.ExtractEntry(entry, outPath, writeManifest: false);
                        extractedFiles.Add((entry, outPath));

                        done++;
                        ReportProgress(done, total, Strings.PakMaker_ExtractingSelectedItemS_2);
                    }

                    PakExtractionSidecar.WriteManifest(extractedFiles);
                });

                lblStatus.Text = Strings.PakMaker_Ready;
                MessageBox.Show(Strings.PakMaker_FolderExtractedRespectingTheSelectedLevel, Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = Strings.PakMaker_ExtractionError;
                MessageBox.Show($"{Strings.PakMaker_ErrorExtracting} {ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnExtractSelected.Enabled = true;
                EndOperation(operation);
            }
        }

        /// <summary>
        /// Opção 2: Extrai estritamente os arquivos que estão marcados/selecionados na ListView
        /// </summary>
        private async Task ExtractOnlySelectedFilesAsync()
        {
            if (_currentReader == null || lstEntries.SelectedItems.Count == 0) return;

            var selectedEntries = lstEntries.SelectedItems
                .Cast<ListViewItem>()
                .Select(i => i.Tag)
                .OfType<PakFileEntry>()
                .Where(en => en.Type != PakFileEntryType.Directory)
                .ToList();

            if (selectedEntries.Count == 0) return;

            string destinationDir;

            // Se for apenas um arquivo, permite escolher o nome exato do arquivo (SaveFileDialog)
            if (selectedEntries.Count == 1)
            {
                string suggestedName = Path.GetFileName(selectedEntries[0].Name.Replace('/', '\\'));
                using var saveFileDialog = new SaveFileDialog
                {
                    FileName = suggestedName,
                    Title = $"{Strings.PakMaker_Extract} {suggestedName}"
                };
                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

                destinationDir = Path.GetDirectoryName(saveFileDialog.FileName) ?? "./";
                await RunExtractionAsync(selectedEntries, destinationDir, saveFileDialog.FileName);
            }
            // Se forem vários arquivos da lista, pede a pasta de destino
            else
            {
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = Strings.PakMaker_SelectTheDestinationFolderForThe
                };
                if (folderDialog.ShowDialog() != DialogResult.OK) return;

                destinationDir = folderDialog.SelectedPath;
                await RunExtractionAsync(selectedEntries, destinationDir, null);
            }
        }

        private async Task RunExtractionAsync(List<PakFileEntry> entries, string destinationDir, string? exactPathForSingle)
        {
            using CancellationTokenSource operation = BeginOperation();
            btnExtractSelected.Enabled = false;
            lblStatus.Text = Strings.PakMaker_ExtractingSelectedItemS;

            try
            {
                await Task.Run(() =>
                {
                    int total = entries.Count;
                    int done = 0;
                    var extractedFiles = new List<(PakFileEntry Entry, string OutputPath)>();

                    foreach (var entry in entries)
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        string outPath = exactPathForSingle ?? Path.Combine(destinationDir, _currentReader!.GetExtractionName(entry));
                        _currentReader!.ExtractEntry(entry, outPath, writeManifest: false);
                        extractedFiles.Add((entry, outPath));

                        done++;
                        ReportProgress(done, total, Strings.PakMaker_ExtractingSelectedItemS_2);
                    }

                    PakExtractionSidecar.WriteManifest(extractedFiles);
                });

                lblStatus.Text = Strings.PakMaker_Ready;
                MessageBox.Show(Strings.PakMaker_FileSExtractedSuccessfully, Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = Strings.PakMaker_ExtractionError;
                MessageBox.Show($"{Strings.PakMaker_ErrorExtracting} {ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnExtractSelected.Enabled = true;
                EndOperation(operation);
            }
        }

        private async void btnBatchExtract_Click(object? sender, EventArgs e)
        {
            using var sourceFolderDialog = new FolderBrowserDialog { Description = Strings.PakMaker_SelectTheFolderThatCONTAINSThe };
            if (sourceFolderDialog.ShowDialog() != DialogResult.OK) return;

            string sourceDir = sourceFolderDialog.SelectedPath;
            string[] pakFiles = Directory.GetFiles(sourceDir, "*.pak", SearchOption.TopDirectoryOnly);

            if (pakFiles.Length == 0)
            {
                MessageBox.Show(Strings.PakMaker_NoPakFilesWereFoundIn, Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var destFolderDialog = new FolderBrowserDialog { Description = Strings.PakMaker_SelectTheDESTINATIONFolderForExtraction };
            if (destFolderDialog.ShowDialog() != DialogResult.OK) return;

            string targetBaseDir = destFolderDialog.SelectedPath;
            DialogResult structureChoice = MessageBox.Show(
                Strings.PakMaker_BatchFolderStructureChoice,
                Strings.PakMaker_BatchFolderStructureTitle,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (structureChoice == DialogResult.Cancel) return;

            bool createPakFolders = structureChoice == DialogResult.Yes;
            using CancellationTokenSource operation = BeginOperation();

            // Mesma região/chave do PAK atualmente carregado (se houver), evita ficar perguntando por console.
            uint[]? sharedKeys = _currentReader?.LocationKeys;
            Encoding filenameEncoding = SelectedFilenameEncoding;

            btnBatchExtract.Enabled = false;
            progressBar1.Visible = true;
            progressBar1.Maximum = pakFiles.Length;
            progressBar1.Value = 0;

            int paksProcessados = 0;
            List<(PakFileEntry Entry, string OutputPath)> extractedFiles = [];

            foreach (var pakPath in pakFiles)
            {
                if (operation.IsCancellationRequested) break;
                string pakName = Path.GetFileNameWithoutExtension(pakPath);
                string specificDestFolder = createPakFolders
                    ? Path.Combine(targetBaseDir, pakName)
                    : targetBaseDir;

                lblStatus.Text = $"{Strings.PakMaker_Processing} ({paksProcessados + 1}/{pakFiles.Length}): {pakName}.pak...";

                try
                {
                    await Task.Run(() =>
                    {
                        if (!Directory.Exists(specificDestFolder))
                            Directory.CreateDirectory(specificDestFolder);

                        using var batchReader = new PakReader(pakPath, filenameEncoding, AppLogger.Instance);
                        batchReader.Parse(sharedKeys);
                        batchReader.Extract("*", specificDestFolder, cancellationToken: operation.Token, writeManifest: false);
                        lock (extractedFiles)
                        {
                            extractedFiles.AddRange(batchReader.Entries
                                .Where(entry => entry.Type != PakFileEntryType.Directory)
                                .Select(entry => (entry, Path.Combine(specificDestFolder, batchReader.GetExtractionName(entry)))));
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    lblStatus.Text = Strings.Pak_OperationCancelled;
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Strings.PakMaker_FailedToExtract} {pakName}.pak:\n{ex.Message}", Strings.PakMaker_BatchError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                paksProcessados++;
                progressBar1.Value = paksProcessados;
            }

            lblStatus.Text = Strings.PakMaker_BatchExtractionCompleted;
            progressBar1.Visible = false;
            btnBatchExtract.Enabled = true;
            bool wasCancelled = operation.IsCancellationRequested;
            EndOperation(operation);

            if (!wasCancelled)
                PakExtractionSidecar.WriteManifest(extractedFiles);

            if (!wasCancelled)
                MessageBox.Show($"{paksProcessados} {Strings.PakMaker_PAKPackagesExtractedSuccessfullyTo}\n{targetBaseDir}", Strings.PakMaker_ProcessingComplete, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ─── INJETAR / ATUALIZAR ────────────────────────────────────────────────

        private PakRebuildOptions BuildRebuildOptionsForCurrentPak()
        {
            PakFileEntryVersion entryVersion = GetLoadedEntryVersion();
            return new PakRebuildOptions(
                EntryVersion: entryVersion,
                EntryType: GetPreferredCompressionType(),
                CompressLevel: _creationOptions.CompressLevel,
                LocationKeys: _currentReader?.LocationKeys ?? [.. _creationOptions.LocationKeys],
                Author: _currentReader?.Header.Author ?? "PakMaker"
            )
            {
                FileNameEncoding = _currentReader?.FileNameEncoding ?? SelectedFilenameEncoding,
                PakVersion = _currentReader?.Header.Version
            };
        }

        private PakFileEntryVersion GetLoadedEntryVersion() =>
            _currentReader?.Entries.FirstOrDefault()?.Version ?? _creationOptions.EntryVersion;

        private PakFileEntryType GetPreferredCompressionType()
        {
            if (_currentReader == null) return _creationOptions.EntryType;
            return _currentReader.Entries
                .Where(entry => entry.Type is PakFileEntryType.Raw or PakFileEntryType.LZ77 or PakFileEntryType.LZ772)
                .Where(entry => entry.Size > 0)
                .GroupBy(entry => entry.Type)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault(_creationOptions.EntryType);
        }

        private async Task EditPakSettingsAsync()
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show(Strings.PakMaker_SelectAnActivePakFileFirst,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PakReader reader = _currentReader;
            string pakPath = txtPakPath.Text;
            var initial = new PakSettingsOptions(
                reader.Header.Version,
                reader.Header.Author,
                false,
                GetPreferredCompressionType(),
                _creationOptions.CompressLevel);
            using var dialog = new PakSettingsDialog(initial);
            if (dialog.ShowDialog(this) != DialogResult.OK ||
                !dialog.TryGetSelectedOptions(out PakSettingsOptions selected, out _))
                return;

            bool metadataChanged = selected.PakVersion != reader.Header.Version ||
                !string.Equals(selected.Author, reader.Header.Author, StringComparison.Ordinal);
            if (!metadataChanged && !selected.RecompressEntries)
            {
                MessageBox.Show(Strings.Pak_NoSettingsChanges, Strings.PakMaker_Information,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmation = Strings.Pak_ConfirmSettingsUpdate;
            if (selected.RecompressEntries)
                confirmation += $"{Environment.NewLine}{Environment.NewLine}{Strings.Pak_ConfirmRecompression}";
            if (MessageBox.Show(confirmation, Strings.Pak_SettingsTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            PakRebuildOptions options = BuildRebuildOptionsForCurrentPak() with
            {
                PakVersion = selected.PakVersion,
                Author = selected.Author,
                EntryType = selected.CompressionType,
                CompressLevel = selected.CompressionLevel
            };
            PakArchiveUpdateMode mode = selected.RecompressEntries
                ? PakArchiveUpdateMode.RecompressEntries
                : PakArchiveUpdateMode.PreservePayloads;
            Encoding encoding = options.FileNameEncoding;
            uint[] keys = [.. options.LocationKeys];
            PakFileEntryVersion entryVersion = options.EntryVersion;

            lblStatus.Text = Strings.Pak_UpdatingSettings;
            using CancellationTokenSource operation = BeginOperation();
            try
            {
                await Task.Run(() => PakManager.UpdateArchive(
                    pakPath,
                    reader,
                    options,
                    mode,
                    msg => AppLogger.Instance.Log("PAK Writer", msg),
                    (done, total) => ReportProgress(done, total, Strings.Pak_UpdatingSettings),
                    SaveBck: true,
                    cancellationToken: operation.Token));

                _creationOptions = _creationOptions with
                {
                    EntryVersion = entryVersion,
                    EntryType = selected.CompressionType,
                    CompressLevel = selected.CompressionLevel
                };
                byte? resultingCompressionLevel = selected.RecompressEntries
                    ? selected.CompressionLevel
                    : _knownCompressionLevel;
                LoadPakCore(pakPath, encoding,
                    entryVersion == PakFileEntryVersion.V3 ? keys : null,
                    resultingCompressionLevel);
                lblStatus.Text = Strings.Pak_SettingsUpdated;
                MessageBox.Show(Strings.Pak_SettingsUpdated, Strings.PakMaker_Success,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
            }
            catch (Exception ex)
            {
                lblStatus.Text = Strings.Pak_SettingsUpdateFailed;
                AppLogger.Instance.Log("PAK Writer", $"PAK settings update failed: {ex}", AppLogLevel.Error);
                MessageBox.Show($"{Strings.Pak_SettingsUpdateFailed} {ex.Message}",
                    Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (File.Exists(pakPath))
                    LoadPakCore(pakPath, encoding,
                        entryVersion == PakFileEntryVersion.V3 ? keys : null,
                        _knownCompressionLevel);
            }
            finally
            {
                HideProgress();
                EndOperation(operation);
            }
        }


        private async void btnUpdatePak_Click(object? sender, EventArgs e)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show(
                                    Strings.PakMaker_SelectAnActivePakFileFirst,
                                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var openFileDialog = FileDialogFactory.CreatePakInjectFilesDialog();

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            FileDialogFactory.RememberDirectory(FileDialogKind.PakInject, openFileDialog.FileNames.FirstOrDefault());

            List<PakInjectItem> items = BuildPakInjectItems(openFileDialog.FileNames, GetSelectedArchiveFolder());
            await InjectFilesIntoCurrentPakAsync(items);

        }

        /// <summary>
        /// Lógica comum de injeção/atualização do PAK atualmente carregado.
        /// Usada tanto pelo botão "Atualizar PAK" quanto pelo drag-and-drop na lista de entries.
        /// </summary>
        private async Task InjectFilesIntoCurrentPakAsync(List<PakInjectItem> items)
        {
            AppLogger.Instance.Log("PAK Writer", $"Injection requested for {items?.Count ?? 0} items.");
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show(
                    Strings.PakMaker_SelectAnActivePakFileFirst,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (items == null || items.Count == 0) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            lblStatus.Text = Strings.PakMaker_MergingAndRebuildingPAK;
            btnUpdatePak.Enabled = false;
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                var options = BuildRebuildOptionsForCurrentPak();

                await Task.Run(() =>
                {
                    PakManager.InjectFiles(pakPath, reader, items, options,
                        defaultRelativeFolder: string.Empty,
                        log: msg => AppLogger.Instance.Log("PAK Writer", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                lblStatus.Text = Strings.PakMaker_PAKUpdatedSuccessfully;
                AppLogger.Instance.Log("PAK Writer", $"Injected {items.Count} items into '{pakPath}' successfully.");
                MessageBox.Show(
                      $"{items.Count} {Strings.PakMaker_FileSInjectedAndThePAK}",
                      Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (Exception ex)
            {
                lblStatus.Text = Strings.PakMaker_ErrorInjecting;
                MessageBox.Show($"{Strings.PakMaker_InjectionFailed} {ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnUpdatePak.Enabled = true;
                EndOperation(operation);
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

            List<PakInjectItem> items;
            try
            {
                items = BuildPakInjectItems(dropped, GetSelectedArchiveFolder());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.PakMaker_ErrorReadingFolder}\n{ex.Message}",
                    Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (items.Count == 0)
            {
                MessageBox.Show(Strings.PakMaker_NoValidFileWasFoundTo,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await InjectFilesIntoCurrentPakAsync(items);
        }

        private string GetSelectedArchiveFolder()
        {
            string folderTag = tvFolders.SelectedNode?.Tag as string ?? string.Empty;
            return NormalizeArchiveFolder(folderTag);
        }

        private static List<PakInjectItem> BuildPakInjectItems(IEnumerable<string> paths, string selectedFolder)
        {
            string targetFolder = NormalizeArchiveFolder(selectedFolder);
            var items = new List<PakInjectItem>();

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    items.Add(new PakInjectItem(path, targetFolder));
                    continue;
                }

                if (!Directory.Exists(path)) continue;

                string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string rootFolderName = Path.GetFileName(trimmedPath);
                string injectedRoot = CombineArchiveFolders(targetFolder, rootFolderName);

                foreach (string filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(path, filePath);
                    string relativeFolder = Path.GetDirectoryName(relativePath) ?? string.Empty;
                    items.Add(new PakInjectItem(filePath,
                        CombineArchiveFolders(injectedRoot, relativeFolder)));
                }
            }

            return items;
        }

        private static string CombineArchiveFolders(string left, string right)
        {
            string normalizedLeft = NormalizeArchiveFolder(left);
            string normalizedRight = NormalizeArchiveFolder(right);
            if (string.IsNullOrEmpty(normalizedLeft)) return normalizedRight;
            if (string.IsNullOrEmpty(normalizedRight)) return normalizedLeft;
            return $"{normalizedLeft}/{normalizedRight}";
        }

        private static string NormalizeArchiveFolder(string folder) =>
            (folder ?? string.Empty).Replace('\\', '/').Trim('/');

        // ─── REMOVER ─────────────────────────────────────────────────────────────

        private async void btnRemoveSelected_Click(object? sender, EventArgs e) => await RemoveSelectedAsync();

        private async Task RemoveSelectedAsync()
        {
            AppLogger.Instance.Log("PAK Manager", "Selected-file removal requested.");
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                AppLogger.Instance.Log("PAK Manager", "Removal stopped because no active PAK is loaded.", AppLogLevel.Warning);
                MessageBox.Show(
                    Strings.PakMaker_SelectAnActivePakFileFirst,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            HashSet<string> namesToRemove = new(StringComparer.OrdinalIgnoreCase);

            // --- 1. COLETAR ARQUIVOS SELECIONADOS NO LISTVIEW (MULTIPLE SELECTION) ---
            if (lstEntries.SelectedItems.Count > 0)
            {
                var selectedFiles = lstEntries.SelectedItems
                    .Cast<ListViewItem>()
                    .Select(i => i.Tag)
                    .OfType<PakFileEntry>()
                    .Where(en => en.Type != PakFileEntryType.Directory)
                    .Select(en => en.Name);

                foreach (var file in selectedFiles)
                {
                    namesToRemove.Add(file);
                }
            }

            // Se nenhum arquivo foi selecionado
            if (namesToRemove.Count == 0)
            {
                AppLogger.Instance.Log("PAK Manager", "Removal stopped because no files or folders were selected.", AppLogLevel.Warning);
                MessageBox.Show(
                    Strings.PakMaker_SelectFilesFromTheListOr_2,
                    Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string confirmationMessage = string.Format(LocalizationManager.CurrentCulture,
                Strings.Pak_RemoveFilesConfirmation, namesToRemove.Count);

            var confirm = MessageBox.Show(
                $"{confirmationMessage}\n\n{Strings.PakMaker_ThePAKWillBeRebuiltAnd}",
                Strings.PakMaker_ConfirmRemoval, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                AppLogger.Instance.Log("PAK Manager",
                    $"Removal of {namesToRemove.Count} selected files was cancelled.", AppLogLevel.Warning);
                return;
            }

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            lblStatus.Text = Strings.PakMaker_RemovingAndRebuildingPAK;
            btnRemoveSelected.Enabled = false;
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                var options = BuildRebuildOptionsForCurrentPak();
                AppLogger.Instance.Log("PAK Manager",
                    $"Removing {namesToRemove.Count} selected files from '{pakPath}'.");

                await Task.Run(() =>
                {
                    PakManager.RemoveFiles(pakPath, reader, namesToRemove.ToList(), options,
                        log: msg => AppLogger.Instance.Log("PAK Manager", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                lblStatus.Text = Strings.PakMaker_RemovalCompleted;
                AppLogger.Instance.Log("PAK Manager",
                    $"Removed {namesToRemove.Count} selected files successfully.");
                MessageBox.Show(
                    Strings.PakMaker_TheSelectedItemsWereRemovedSuccessfully,
                    Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"Selected-file removal failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_ErrorRemoving;
                MessageBox.Show($"{Strings.PakMaker_Failure} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnRemoveSelected.Enabled = true;
                EndOperation(operation);
            }
        }

        private async Task CreateEmptyPakAsync()
        {
            using var optionsDialog = new PakCreationOptionsDialog(_creationOptions);
            if (optionsDialog.ShowDialog(this) != DialogResult.OK)
                return;

            PakCreationOptions selectedOptions = optionsDialog.SelectedOptions;
            AppLogger.Instance.Log("PAK Writer", "Create empty PAK requested.");

            using var saveFileDialog = new SaveFileDialog
            {
                Filter = Strings.Pak_SaveFileFilter,
                Title = Strings.Pak_SaveEmptyFileTitle
            };
            if (saveFileDialog.ShowDialog(this) != DialogResult.OK) return;

            _creationOptions = selectedOptions;
            uint[] selectedKeys = selectedOptions.EntryVersion == PakFileEntryVersion.Raw
                ? Array.Empty<uint>()
                : [.. selectedOptions.LocationKeys];
            Encoding filenameEncoding = SelectedFilenameEncoding;
            var writer = new PakWriter
            {
                EntryVersion = selectedOptions.EntryVersion,
                EntryType = selectedOptions.EntryType,
                CompressLevel = selectedOptions.CompressLevel,
                LocationKeys = selectedKeys,
                Author = txtUpdateAuthor.Text,
                FileNameEncoding = filenameEncoding,
                LogSink = AppLogger.Instance
            };

            using CancellationTokenSource operation = BeginOperation();
            lblStatus.Text = Strings.Pak_CreatingEmpty;
            try
            {
                await Task.Run(() => writer.CreateEmpty(
                    saveFileDialog.FileName, log: null, operation.Token));

                uint[]? forceKeys = selectedOptions.EntryVersion switch
                {
                    PakFileEntryVersion.V3 => selectedKeys,
                    PakFileEntryVersion.Raw => Array.Empty<uint>(),
                    _ => null
                };
                txtPakPath.Text = saveFileDialog.FileName;
                if (forceKeys != null)
                    LoadPakCore(saveFileDialog.FileName, filenameEncoding, forceKeys,
                        selectedOptions.CompressLevel);
                else
                    ReloadPak(saveFileDialog.FileName, filenameEncoding, selectedOptions.CompressLevel);

                lblStatus.Text = Strings.PakMaker_Ready;
                MessageBox.Show(
                    Strings.Pak_EmptyCreated,
                    Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Writer", $"Create PAK failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_ErrorCreatingPAK;
                MessageBox.Show($"{Strings.PakMaker_CompilationError} {ex.Message}",
                    Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndOperation(operation);
            }
        }

        private async void btnChangeKey_Click(object? sender, EventArgs e)
        {
            AppLogger.Instance.Log("PAK Writer", "Encryption-key change requested.");
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text) || !File.Exists(txtPakPath.Text))
            {
                MessageBox.Show(Strings.PakMaker_SelectAnActivePakFileFirst, Strings.PakMaker_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RegionOption? selectedRegion = ShowChangeKeyDialog();
            if (selectedRegion == null)
                return;

            uint[] newKeys = selectedRegion.Keys;

            // Evita reconstrução desnecessária se a chave de destino já for a mesma do PAK carregado
            if (_currentReader.LocationKeys != null && newKeys.SequenceEqual(_currentReader.LocationKeys))
            {
                MessageBox.Show(Strings.PakMaker_ThePAKIsAlreadyUsingThat, Strings.PakMaker_Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"{Strings.PakMaker_ChangeThePAKKeyTo} \"{selectedRegion.Label}\"?\n{Strings.PakMaker_ThePAKWillBeRebuiltAnd_2}",
                Strings.PakMaker_ConfirmKeyChange, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                AppLogger.Instance.Log("PAK Writer", "Encryption-key change was cancelled.", AppLogLevel.Warning);
                return;
            }

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;

            // Mantém versão/compressão/autor atuais do PAK, só troca a LocationKeys
            var currentOptions = BuildRebuildOptionsForCurrentPak();
            var newOptions = currentOptions with { LocationKeys = newKeys };

            lblStatus.Text = Strings.PakMaker_ChangingKeyAndRebuildingPAK;
            btnChangeKey.Enabled = false;
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                await Task.Run(() =>
                {
                    PakManager.ChangeEncryptionKey(pakPath, reader, newOptions,
                        log: msg => AppLogger.Instance.Log("PAK Writer", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RebuildingPAK),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                lblStatus.Text = Strings.PakMaker_KeyChangedSuccessfully;
                AppLogger.Instance.Log("PAK Writer", $"Changed the encryption key for '{pakPath}' to '{selectedRegion.Label}'.");
                MessageBox.Show($"{Strings.PakMaker_ThePAKWasRebuiltWithThe} \"{selectedRegion.Label}\"!",
                    Strings.PakMaker_Success, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ReloadPak(pakPath, newOptions.FileNameEncoding, _knownCompressionLevel);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Writer", $"Encryption-key change failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_ErrorChangingKey;
                MessageBox.Show($"{Strings.PakMaker_RebuildFailed} {ex.Message}", Strings.PakMaker_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideProgress();
                btnChangeKey.Enabled = true;
                EndOperation(operation);
            }
        }

        private RegionOption? ShowChangeKeyDialog()
        {
            using var dialog = new Form
            {
                AutoScaleMode = AutoScaleMode.Font,
                ClientSize = new Size(360, 118),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                Text = Strings.Pak_ChangeKey
            };

            var label = new Label
            {
                AutoSize = false,
                Location = new Point(12, 14),
                Size = new Size(336, 20),
                Text = Strings.PakMaker_SelectTheTargetRegionKey
            };

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, 40),
                Size = new Size(336, 23),
                DisplayMember = nameof(RegionOption.Label),
                DataSource = PakKeys.All.Select(key => new RegionOption(key.Label, key.Keys)).ToList()
            };

            var cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(192, 80),
                Size = new Size(75, 26),
                Text = Strings.Options_Cancel
            };

            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(273, 80),
                Size = new Size(75, 26),
                Text = Strings.Common_OK
            };

            dialog.Controls.Add(label);
            dialog.Controls.Add(combo);
            dialog.Controls.Add(cancelButton);
            dialog.Controls.Add(okButton);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            return dialog.ShowDialog(this) == DialogResult.OK
                ? combo.SelectedItem as RegionOption
                : null;
        }


        /// <summary>
        /// Executa a rotina isolada de validação, alteração lógica e gravação física do item renomeado.
        /// </summary>
        private async Task ExecuteFileRename(PakFileEntry entry, string newName)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text)) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;
            var options = BuildRebuildOptionsForCurrentPak();

            lblStatus.Text = Strings.PakMaker_RenameProcess;
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                bool success = false;
                string oldPath = entry.Name;
                AppLogger.Instance.Log("PAK Manager", $"File rename requested: '{oldPath}' -> '{newName}'.");

                await Task.Run(() =>
                {
                    success = PakManager.Rename(pakPath, reader, oldPath, newName, options,
                        log: msg => AppLogger.Instance.Log("PAK Manager", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RenameWriter),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                if (!success)
                {
                    MessageBox.Show(Strings.PakMaker_RenameNotFound, Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
                    return;
                }

                lblStatus.Text = Strings.PakMaker_RenameSuccess;
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (ArgumentException ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"File rename validation failed: {ex}", AppLogLevel.Warning);
                lblStatus.Text = Strings.PakMaker_RenameError;
                MessageBox.Show($"{Strings.PakMaker_InvalidRenameName} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (InvalidDataException ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"File rename failed validation: {ex}", AppLogLevel.Warning);
                lblStatus.Text = Strings.PakMaker_RenameError;
                MessageBox.Show($"{Strings.PakMaker_RenamePhysicalFailed} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"File rename failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_RenameError;
                MessageBox.Show($"{Strings.PakMaker_RenamePhysicalFailed} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            finally
            {
                HideProgress();
                EndOperation(operation);
            }
        }

        private async Task ExecuteItemRename(TreeNode selectedNode, string newName)
        {
            if (_currentReader == null || string.IsNullOrEmpty(txtPakPath.Text)) return;

            string pakPath = txtPakPath.Text;
            var reader = _currentReader;
            string oldPath = selectedNode.Tag?.ToString() ?? selectedNode.FullPath;
            var options = BuildRebuildOptionsForCurrentPak();

            lblStatus.Text = Strings.PakMaker_RenameProcess; 
            using CancellationTokenSource operation = BeginOperation();

            try
            {
                bool success = false;
                AppLogger.Instance.Log("PAK Manager", $"Folder rename requested: '{oldPath}' -> '{newName}'.");

                // Executa a operação
                await Task.Run(() =>
                {
                    success = PakManager.Rename(pakPath, reader, oldPath, newName.Replace("📁 ", ""), options,
                        log: msg => AppLogger.Instance.Log("PAK Manager", msg),
                        onProgress: (done, total) => ReportProgress(done, total, Strings.PakMaker_RenameWriter),
                        SaveBck: false, cancellationToken: operation.Token);
                });

                if (!success)
                {
                    MessageBox.Show(Strings.PakMaker_RenameNotFound, Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
                    return;
                }

                lblStatus.Text = Strings.PakMaker_RenameSuccess;

                // Recarrega a UI com os novos ponteiros persistidos no arquivo final
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = Strings.Pak_OperationCancelled;
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Log("PAK Manager", $"Folder rename failed: {ex}", AppLogLevel.Error);
                lblStatus.Text = Strings.PakMaker_RenameError;
                MessageBox.Show($"{Strings.PakMaker_RenamePhysicalFailed} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ReloadPak(pakPath, options.FileNameEncoding, _knownCompressionLevel);
            }
            finally
            {
                HideProgress();
                EndOperation(operation);
            }
        }
    } 
}
