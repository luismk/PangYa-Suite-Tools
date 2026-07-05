using PangYa_Suite_Tools.Localization;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;

namespace PangYa_Suite_Tools
{
    public partial class FrmMenu : Form
    {
        private bool isInitializingLanguages = true;
        private FrmLog? _logWindow;

        // Caminho no registro para salvar as configurações da Suite
        private const string RegistryKeyPath = @"Software\PangYaSuiteTools";
        private const string LanguageValueName = "Language";
        public FrmMenu()
        {
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
            cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;

            isInitializingLanguages = false;
            //init lang
            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                LocalizationManager.SetCulture(selectedItem.Value);
            }
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
            Text = Strings.Menu_Title;
            lblTitle.Text = Strings.Menu_Title;
            btnOpenPakMaker.Text = Strings.Menu_PakManager;
            btnOpenUpdateList.Text = Strings.Menu_UpdateList;
            btnOpenIffManager.Text = Strings.Menu_IffManager;
            btnOpenOptions.Text = Strings.Menu_Options;
            btnOpenPakDiff.Text = Strings.Menu_PakDiff;
            btnOpenLog.Text = Strings.Menu_Log;
            lblLanguage.Text = Strings.Common_Language;
        }

        private void btnOpenPakMaker_Click(object sender, EventArgs e)
        {
            string idiomaAtual = "en";
            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                idiomaAtual = selectedItem.Value;
            }

            OpenToolWindow(new FrmPakMaker(idiomaAtual, ""), hideMenu: true);
        }

        private void btnOpenUpdateList_Click(object sender, EventArgs e)
        {
            string idiomaAtual = "en";
            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                idiomaAtual = selectedItem.Value;
            }

            OpenToolWindow(new FrmUpdateList(idiomaAtual), hideMenu: true);
        }

        private void btnOpenIffManager_Click(object sender, EventArgs e)
        {
            string idiomaAtual = "en";
            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                idiomaAtual = selectedItem.Value;
            }

            OpenToolWindow(new FrmIFFManager(idiomaAtual), hideMenu: true);
        }

        private void btnOpenOptions_Click(object sender, EventArgs e)
        {
            // Obtém o idioma selecionado em tempo real no menu principal ('br' ou 'en')
            OpenToolWindow(new FrmOptions(), hideMenu: false);
        }

        private void btnOpenPakDiff_Click(object sender, EventArgs e)
        {
            string idiomaAtual = cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem
                ? selectedItem.Value
                : LocalizationManager.English;

            OpenToolWindow(new FrmPakDiff(idiomaAtual), hideMenu: true);
        }

        private void OpenToolWindow(Form tool, bool hideMenu)
        {
            if (hideMenu) Hide();
            tool.FormClosed += (_, _) =>
            {
                tool.Dispose();
                if (hideMenu && !IsDisposed) Show();
            };
            tool.Show();
        }

        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            if (_logWindow is null || _logWindow.IsDisposed)
            {
                _logWindow = new FrmLog();
                _logWindow.FormClosed += (_, _) => _logWindow = null;
                _logWindow.Show();
                return;
            }

            if (_logWindow.WindowState == FormWindowState.Minimized)
            {
                _logWindow.WindowState = FormWindowState.Normal;
            }

            _logWindow.Activate();
        }

        
    }
}
