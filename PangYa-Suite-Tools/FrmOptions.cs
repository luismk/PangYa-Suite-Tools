using PangYa_Suite_Tools.Localization;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PangYa_Suite_Tools
{
    public partial class FrmOptions : Form
    {
        private readonly string _exePath;
        private const string ProgramId = "PangYaSuiteTools.PAK";
        private const string ClassesPath = @"Software\Classes";
        private const string PakExtensionPath = $@"{ClassesPath}\.pak";
        private const string ProgramPath = $@"{ClassesPath}\{ProgramId}";
        private const string ContextMenuPath = $@"{ClassesPath}\*\shell\{ProgramId}";
        private bool _isInitializingLanguage = true;

        public FrmOptions()
        {
            _exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;

            InitializeComponent();
            InitializeLanguageComboBox();
            ApplyLocalization();
            CheckCurrentRegistryState();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            Disposed += (_, _) => LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
        }

        private void InitializeLanguageComboBox()
        {
            cboLanguage.DisplayMember = "Key";
            cboLanguage.ValueMember = "Value";
            PopulateLanguageComboBox();
            _isInitializingLanguage = false;
        }

        private void PopulateLanguageComboBox()
        {
            string selectedCulture = LocalizationManager.CurrentCulture.Name;

            _isInitializingLanguage = true;
            cboLanguage.Items.Clear();
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_PortugueseBrazil, LocalizationManager.PortugueseBrazil));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_EnglishUS, LocalizationManager.English));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Swedish, LocalizationManager.Swedish));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Japonese, LocalizationManager.Japonese));
            cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_French, LocalizationManager.French));

            int selectedIndex = cboLanguage.Items.Cast<KeyValuePair<string, string>>()
                .Select((item, index) => new { item, index })
                .FirstOrDefault(pair => string.Equals(pair.item.Value, selectedCulture, StringComparison.OrdinalIgnoreCase))
                ?.index ?? LocalizationManager.CurrentCultureIndex;
            cboLanguage.SelectedIndex = selectedIndex;
            _isInitializingLanguage = false;
        }

        private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
        {
            PopulateLanguageComboBox();
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            Text = Strings.Options_Title;
            groupGlobalSettings.Text = Strings.Options_Options;
            lblLanguage.Text = Strings.Common_Language;
            groupRegister.Text = Strings.Options_Group;
            btnCancel.Text = Strings.Options_Cancel;
            btnOK.Text = Strings.Common_OK;

            // Labels e Opções
            chkRegisterFile.Text = Strings.Options_Register;
            chkShellContext.Text = Strings.Options_Shell;
            lblAdminWarning.Visible = false;
            chkRegisterFile.Enabled = true;
            chkShellContext.Enabled = true;
        }

        private void cboLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializingLanguage) return;

            if (cboLanguage.SelectedItem is KeyValuePair<string, string> selectedItem)
            {
                LocalizationManager.SetCulture(selectedItem.Value);
            }
        }

        private void CheckCurrentRegistryState()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(PakExtensionPath))
                {
                    if (key != null && string.Equals(key.GetValue("")?.ToString(), ProgramId, StringComparison.OrdinalIgnoreCase))
                    {
                        chkRegisterFile.Checked = true;
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(ContextMenuPath))
                {
                    if (key != null)
                    {
                        chkShellContext.Checked = true;
                    }
                }
            }
            catch
            {
                // Ignora falhas de leitura silenciosamente
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // --- REGISTRO DA ASSOCIAÇÃO DIRETA (.pak executar o app) ---
                if (chkRegisterFile.Checked)
                {
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(PakExtensionPath))
                    {
                        extKey.SetValue("", ProgramId);
                    }

                    using (RegistryKey progKey = Registry.CurrentUser.CreateSubKey(ProgramPath))
                    {
                        progKey.SetValue("", Strings.Options_ArchiveDescription);
                    }

                    using (RegistryKey commandKey = Registry.CurrentUser.CreateSubKey($@"{ProgramPath}\shell\open\command"))
                    {
                        commandKey.SetValue("", $"\"{_exePath}\" \"%1\"");
                    }
                }
                else
                {
                    RemoveFileAssociation();
                }

                // --- REGISTRO DO MENU DE CONTEXTO (Botão Direito no Windows Explorer) ---
                if (chkShellContext.Checked)
                {
                    using (RegistryKey contextKey = Registry.CurrentUser.CreateSubKey(ContextMenuPath))
                    {
                        contextKey.SetValue("", Strings.Options_OpenWithPakMaker);
                    }

                    using (RegistryKey commandKey = Registry.CurrentUser.CreateSubKey($@"{ContextMenuPath}\command"))
                    {
                        commandKey.SetValue("", $"\"{_exePath}\" \"%1\"");
                    }
                }
                else
                {
                    Registry.CurrentUser.DeleteSubKeyTree(ContextMenuPath, false);
                }

                // Avisa o Windows Explorer para atualizar os ícones
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Strings.Options_FailedToApplyChanges} {ex.Message}", Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private static void RemoveFileAssociation()
        {
            using (RegistryKey? extensionKey = Registry.CurrentUser.OpenSubKey(PakExtensionPath))
            {
                if (!string.Equals(extensionKey?.GetValue("")?.ToString(), ProgramId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            Registry.CurrentUser.DeleteSubKeyTree(PakExtensionPath, false);
            Registry.CurrentUser.DeleteSubKeyTree(ProgramPath, false);
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
