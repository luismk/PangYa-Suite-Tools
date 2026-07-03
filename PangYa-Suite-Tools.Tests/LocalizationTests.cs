using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using PangYa_Suite_Tools.Configuration;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.PAK.Models;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

public sealed class LocalizationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "PangYaLocalizationTests", Guid.NewGuid().ToString("N"));

    public LocalizationTests()
    {
        Directory.CreateDirectory(_tempDirectory);
        LocalizationManager.PreferencePathOverride = Path.Combine(_tempDirectory, "culture.txt");
        PakFilenameEncodingPreferences.PreferencePathOverride =
            Path.Combine(_tempDirectory, "filename-encoding.txt");
    }

    [Fact]
    public void Resources_ResolveEnglishAndPortuguese()
    {
        LocalizationManager.SetCulture(LocalizationManager.English);
        Assert.Equal("Language:", Strings.Common_Language);

        LocalizationManager.SetCulture(LocalizationManager.PortugueseBrazil);
        Assert.Equal("Idioma:", Strings.Common_Language);
    }

    [Fact]
    public void CompositeResource_FormatsInBothCultures()
    {
        foreach (string cultureName in new[] { LocalizationManager.English, LocalizationManager.PortugueseBrazil })
        {
            LocalizationManager.SetCulture(cultureName);
            string result = string.Format(LocalizationManager.CurrentCulture, Strings.Pak_RemoveFilesConfirmation, 3);
            Assert.Contains("3", result);
            Assert.DoesNotContain("{0}", result);
        }
    }

    [Fact]
    public void Preference_IsSavedReloadedAndInvalidValuesFallBackToEnglish()
    {
        LocalizationManager.SetCulture(LocalizationManager.PortugueseBrazil);
        LocalizationManager.Initialize();
        Assert.Equal(LocalizationManager.PortugueseBrazil, LocalizationManager.CurrentCulture.Name);

        File.WriteAllText(LocalizationManager.PreferencePathOverride!, "not-a-culture");
        LocalizationManager.Initialize();
        Assert.Equal(LocalizationManager.English, LocalizationManager.CurrentCulture.Name);
    }

    [Fact]
    public void PortugueseResource_HasEveryNeutralKey()
    {
        var neutral = KeysFor(CultureInfo.InvariantCulture);
        var portuguese = KeysFor(CultureInfo.GetCultureInfo(LocalizationManager.PortugueseBrazil));
        Assert.Empty(neutral.Except(portuguese));
    }

    [Fact]
    public void FilenameEncodingPreference_PersistsAndInvalidValuesFallBackToEucKr()
    {
        PakFilenameEncodingPreferences.SaveCodePage(932);
        Assert.Equal(932, PakFilenameEncodingPreferences.LoadCodePage());

        File.WriteAllText(PakFilenameEncodingPreferences.PreferencePathOverride!, "invalid");
        Assert.Equal(PakFilenameEncodingPreferences.DefaultCodePage,
            PakFilenameEncodingPreferences.LoadCodePage());

        File.WriteAllText(PakFilenameEncodingPreferences.PreferencePathOverride!, "123456789");
        Assert.Equal(PakFilenameEncodingPreferences.DefaultCodePage,
            PakFilenameEncodingPreferences.LoadCodePage());
    }

    [Fact]
    public void FilenameEncodingChange_AppliesOnlyToTheNextPakLoad()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding shiftJis = Encoding.GetEncoding(932);
        string source = Path.Combine(_tempDirectory, "source");
        string pakPath = Path.Combine(_tempDirectory, "shift-jis.pak");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "日本.txt"), "JP");
        new PakWriter { FileNameEncoding = shiftJis }
            .CreateFromDirectoryContents(source, pakPath);
        PakFilenameEncodingPreferences.SaveCodePage(shiftJis.CodePage);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmPakMaker();
                MethodInfo loadPak = typeof(FrmPakMaker).GetMethod(
                    "LoadPak", BindingFlags.Instance | BindingFlags.NonPublic)!;
                loadPak.Invoke(form, [pakPath, null]);

                PakReader reader = PrivateField<PakReader>(form, "_currentReader");
                Assert.Equal(shiftJis.CodePage, reader.FileNameEncoding.CodePage);
                Assert.Contains(reader.Entries, entry => entry.Name == "日本.txt");

                var combo = PrivateField<ToolStripComboBox>(form, "cboFilenameEncoding");
                PakEncodingOption utf8 = combo.Items.Cast<PakEncodingOption>()
                    .Single(option => option.CodePage == Encoding.UTF8.CodePage);
                combo.SelectedItem = utf8;

                Assert.Equal(shiftJis.CodePage, reader.FileNameEncoding.CodePage);
                Assert.Equal(Encoding.UTF8.CodePage,
                    PakFilenameEncodingPreferences.LoadCodePage());
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void EveryForm_AcceptsLiveCultureChanges()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var menu = new FrmMenu();
                using var pak = new FrmPakMaker();
                using var update = new FrmUpdateList();
                using var iff = new FrmIFFManager();
                using var options = new FrmOptions();

                LocalizationManager.SetCulture(LocalizationManager.PortugueseBrazil);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.Common_OK, options.Controls.Find("btnOK", true).Single().Text);
                Assert.Equal(Strings.PakMaker_Author, pak.Controls.Find("label1", true).Single().Text);
                Assert.Equal(Strings.PakMaker_Author, pak.Controls.Find("label2", true).Single().Text);
                Assert.Equal(Strings.Pak_SecurityPak, pak.Controls.Find("ckSecurityPak", true).Single().Text);
                Assert.Contains("*.pak", Strings.Pak_OpenFileFilter);
                Assert.Contains("*.pak", Strings.Pak_SaveFileFilter);
                var encodingCombo = PrivateField<ToolStripComboBox>(pak, "cboFilenameEncoding");
                var encodingOptions = encodingCombo.Items.Cast<PakEncodingOption>().ToList();
                Assert.Contains(encodingOptions,
                    option => option.CodePage == PakFilenameEncodingPreferences.DefaultCodePage);
                PakEncodingOption utf8 = encodingOptions.Single(option => option.CodePage == 65001);
                encodingCombo.SelectedItem = utf8;
                Assert.Equal(65001, PakFilenameEncodingPreferences.LoadCodePage());

                LocalizationManager.SetCulture(LocalizationManager.English);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Common_OK, options.Controls.Find("btnOK", true).Single().Text);
                Assert.Equal(Strings.Pak_SecurityPak, pak.Controls.Find("ckSecurityPak", true).Single().Text);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    private static HashSet<string> KeysFor(CultureInfo culture)
    {
        var set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return set.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet(StringComparer.Ordinal);
    }

    private static T PrivateField<T>(object instance, string name) where T : class =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!);

    public void Dispose()
    {
        LocalizationManager.SetCulture(LocalizationManager.English);
        LocalizationManager.PreferencePathOverride = null;
        PakFilenameEncodingPreferences.PreferencePathOverride = null;
        Directory.Delete(_tempDirectory, recursive: true);
    }
}
