using PangYa_Suite_Tools.Localization;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using PangYa_Suite_Tools.Shop;

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
            ConfigureShopButtonIcon();
            InitializeLanguageComboBox();
            LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
            Disposed += (_, _) =>
            {
                LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
                btnOpenShop.Image?.Dispose();
            };
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
            btnOpenShop.Text = Strings.Menu_Shop;
            lblLanguage.Text = Strings.Common_Language;
        }

        private void ConfigureShopButtonIcon()
        {
            btnOpenShop.Image = CreateShopButtonIcon();
        }

        private static Bitmap CreateShopButtonIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            using var pen = new Pen(Color.Black, 1.5f);
            using var fill = new SolidBrush(Color.FromArgb(48, Color.Black));

            graphics.DrawArc(pen, 5, 2, 6, 7, 190, 160);
            graphics.FillRoundedRectangle(fill, new Rectangle(3, 6, 10, 8), new Size(2, 2));
            graphics.DrawRoundedRectangle(pen, new Rectangle(3, 6, 10, 8), new Size(2, 2));

            return bitmap;
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

        private async void btnOpenShop_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog { Description = Strings.Shop_SelectDataFolder };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            btnOpenShop.Enabled = false;
            try
            {
                FrmShopMockup shop = await FrmShopMockup.CreateAsync(dialog.SelectedPath);
                OpenToolWindow(shop, hideMenu: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                MessageBox.Show(this, string.Format(LocalizationManager.CurrentCulture, Strings.Shop_LoadFailed, ex.Message),
                    Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btnOpenShop.Enabled = true; }
        }

        private sealed class CenteredImageButton : Button
        {
            private bool _isMouseDown;

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                _isMouseDown = true;
                base.OnMouseDown(mevent);
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                _isMouseDown = false;
                base.OnMouseUp(mevent);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _isMouseDown = false;
                base.OnMouseLeave(e);
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                ButtonState state = !Enabled
                    ? ButtonState.Inactive
                    : _isMouseDown
                        ? ButtonState.Pushed
                        : ButtonState.Normal;
                ControlPaint.DrawButton(pevent.Graphics, ClientRectangle, state);

                Size imageSize = Image?.Size ?? Size.Empty;
                Size textSize = TextRenderer.MeasureText(
                    pevent.Graphics,
                    Text,
                    Font,
                    Size.Empty,
                    TextFormatFlags.NoPadding);
                int gap = imageSize.IsEmpty || string.IsNullOrEmpty(Text) ? 0 : 6;
                int totalWidth = imageSize.Width + gap + textSize.Width;
                int centerOffset = _isMouseDown ? 1 : 0;
                int x = (ClientSize.Width - totalWidth) / 2 + centerOffset;

                if (Image != null)
                {
                    int imageY = (ClientSize.Height - imageSize.Height) / 2 + centerOffset;
                    pevent.Graphics.DrawImage(Image, x, imageY, imageSize.Width, imageSize.Height);
                    x += imageSize.Width + gap;
                }

                Color textColor = Enabled ? ForeColor : SystemColors.GrayText;
                var textBounds = new Rectangle(
                    x,
                    (ClientSize.Height - textSize.Height) / 2 + centerOffset,
                    textSize.Width,
                    textSize.Height);
                TextRenderer.DrawText(
                    pevent.Graphics,
                    Text,
                    Font,
                    textBounds,
                    textColor,
                    TextFormatFlags.NoPadding);

                if (Focused && ShowFocusCues)
                {
                    Rectangle focusBounds = ClientRectangle;
                    focusBounds.Inflate(-4, -4);
                    ControlPaint.DrawFocusRectangle(pevent.Graphics, focusBounds);
                }
            }
        }

        
    }
}
