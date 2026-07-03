using PangYa_Suite_Tools.Localization;
using System.ComponentModel;

namespace PangYa_Suite_Tools
{
    public partial class FrmIFFManager : Form
    {
        private Form? _activeEditorForm = null;
        private bool isInitializingLanguages = true;

        public FrmIFFManager()
        {
            InitializeComponent();
            InitializeLanguageComboBox();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            Disposed += (_, _) => LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
        }

        public FrmIFFManager(string idiomaAtual) : this() =>
            LocalizationManager.SetCulture(idiomaAtual);

        private void InitializeLanguageComboBox()
        {
            cboLanguage.ComboBox.DisplayMember = "Key";
            cboLanguage.ComboBox.ValueMember = "Value";

            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_PortugueseBrazil, LocalizationManager.PortugueseBrazil));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_EnglishUS, LocalizationManager.English));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Swedish, LocalizationManager.Swedish));
            cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;

            isInitializingLanguages = false;
            ApplyLocalization();
        }

        private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializingLanguages) return;

            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                LocalizationManager.SetCulture(selectedItem.Value);
            }
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
        {
            isInitializingLanguages = true;
            cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;
            isInitializingLanguages = false;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Text = Strings.Iff_Title;
            lblIffDir.Text = Strings.Iff_Directory;
            btnBrowseIffDir.Text = Strings.Iff_Browse;
            grpIffFiles.Text = Strings.Iff_Files;
            lblLanguage.Text = Strings.Common_Language;

            // Apenas atualiza o texto de status/aviso padrão se nenhum diretório foi carregado ainda,
            // para não sobrescrever um estado dinâmico (ex: editor aberto) ao trocar o idioma.
            if (string.IsNullOrEmpty(txtIffDirectory.Text))
            {
                lblStatus.Text = Strings.IFFManager_ReadySelectTheIFFFilesDirectory;
                lblNoFileSelected.Text = Strings.IFFManager_SelectAnIffFileFromThe;
            }
        }
private void btnBrowseIffDir_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = Strings.IFFManager_SelectTheExtractedFolderContainingThe
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtIffDirectory.Text = fbd.SelectedPath;
                LoadIffFiles(fbd.SelectedPath);
            }
        }

        private void LoadIffFiles(string directoryPath)
        {
            lstIffFiles.Items.Clear();
            CloseActiveEditor();

            try
            {
                // Busca por todos os arquivos .iff na pasta selecionada
                string[] files = Directory.GetFiles(directoryPath, "*.iff", SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    lstIffFiles.Items.Add(Path.GetFileName(file));
                }

                lblStatus.Text = $"{Strings.IFFManager_ScanComplete} {lstIffFiles.Items.Count} {Strings.IFFManager_IffFileSFound}";

                if (lstIffFiles.Items.Count == 0)
                {
                    MessageBox.Show(Strings.IFFManager_NoFileWithTheIffExtension, Strings.IFFManager_Warning, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.IFFManager_ErrorListingDirectory} {ex.Message}", Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lstIffFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstIffFiles.SelectedItem == null) return;

            string selectedFileName = lstIffFiles.SelectedItem.ToString()!;
            string fullPath = Path.Combine(txtIffDirectory.Text, selectedFileName);

            LoadSpecificIffEditor(selectedFileName, fullPath);
        }

        private void LoadSpecificIffEditor(string filename, string fullPath)
        {
            CloseActiveEditor();
            lblNoFileSelected.Visible = false;

            Form? targetForm = null;

            // Roteamento inteligente baseado no nome do arquivo IFF
            switch (filename.ToLower())
            {
                case "character.iff":
                    // targetForm = new FrmIffCharacter(fullPath); 
                    break;

                case "item.iff":
                    // targetForm = new FrmIffItem(fullPath);
                    break;

                // Adicione os novos cases conforme for criando as views de cada struct
                default:
                    break;
            }

            if (targetForm != null)
            {
                _activeEditorForm = targetForm;

                // Configura o Form para se comportar como um controle comum de painel (Injeção de View)
                targetForm.TopLevel = false;
                targetForm.FormBorderStyle = FormBorderStyle.None;
                targetForm.Dock = DockStyle.Fill;

                pnlEditorContainer.Controls.Add(targetForm);
                pnlEditorContainer.Tag = targetForm;
                targetForm.Show();

                lblStatus.Text = $"{Strings.IFFManager_EditingStructureOf} {filename}";
            }
            else
            {
                lblNoFileSelected.Text = $"{Strings.IFFManager_TheEditorLayoutStructureForThe} '{filename}'\n{Strings.IFFManager_HasNotYetBeenImplementedOr}";
                lblNoFileSelected.Visible = true;
                lblStatus.Text = $"{Strings.IFFManager_EditorNotAvailableFor} {filename}";
            }
        }

        private void CloseActiveEditor()
        {
            if (_activeEditorForm != null)
            {
                _activeEditorForm.Close();
                _activeEditorForm.Dispose();
                _activeEditorForm = null;
            }
            lblNoFileSelected.Text = Strings.IFFManager_SelectAnIffFileFromThe;
            lblNoFileSelected.Visible = true;
        }
    }
}
