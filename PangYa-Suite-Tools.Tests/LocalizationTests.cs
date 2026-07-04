using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using PangYa_Suite_Tools.Configuration;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.PAK.Models;
using PangyaAPI.IFF;
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
        IffStringEncodingPreferences.PreferencePathOverride =
            Path.Combine(_tempDirectory, "iff-string-encoding.txt");
        IffSchemaPreferences.SchemaDirectoryOverride = Path.Combine(_tempDirectory, "schemas");
        IffRawRecordPreferences.PreferencePathOverride = Path.Combine(_tempDirectory, "show-raw-record.txt");
    }

    [Fact]
    public void Resources_ResolveEnglishAndPortuguese()
    {
        LocalizationManager.SetCulture(LocalizationManager.English);
        Assert.Equal("Language:", Strings.Common_Language);

        LocalizationManager.SetCulture(LocalizationManager.PortugueseBrazil);
        Assert.Equal("Idioma:", Strings.Common_Language);

        LocalizationManager.SetCulture(LocalizationManager.Swedish);
        Assert.Equal("Språk:", Strings.Common_Language);
        Assert.Equal("Svenska", Strings.Common_Swedish);
    }

    [Fact]
    public void CompositeResource_FormatsInBothCultures()
    {
        foreach (string cultureName in new[] { LocalizationManager.English, LocalizationManager.PortugueseBrazil, LocalizationManager.Swedish })
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
    public void IffStringEncodingPreference_PersistsAndInvalidValuesFallBackToEucKr()
    {
        IffStringEncodingPreferences.SaveCodePage(932);
        Assert.Equal(932, IffStringEncodingPreferences.LoadCodePage());

        File.WriteAllText(IffStringEncodingPreferences.PreferencePathOverride!, "invalid");
        Assert.Equal(IffStringEncodingPreferences.DefaultCodePage,
            IffStringEncodingPreferences.LoadCodePage());
    }

    [Fact]
    public void RawRecordVisibilityPreference_DefaultsHiddenAndPersists()
    {
        Assert.False(IffRawRecordPreferences.LoadShowRawRecord());
        IffRawRecordPreferences.SaveShowRawRecord(true);
        Assert.True(IffRawRecordPreferences.LoadShowRawRecord());
        IffRawRecordPreferences.SaveShowRawRecord(false);
        Assert.False(IffRawRecordPreferences.LoadShowRawRecord());
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
                using var diff = new FrmPakDiff();
                using var log = new FrmLog();

                LocalizationManager.SetCulture(LocalizationManager.PortugueseBrazil);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.PakDiff_Title, diff.Text);
                Assert.Equal(Strings.Log_Title, log.Text);
                Assert.Equal(Strings.Log_ToFile, log.Controls.Find("chkLogToFile", true).Single().Text);
                Assert.NotEmpty(PrivateField<ToolStripComboBox>(iff, "cboStringEncoding").Items);
                var regionCombo = PrivateField<ToolStripComboBox>(iff, "cboRegion");
                Assert.Equal(3, regionCombo.Items.Count);
                Assert.Contains(Strings.IFFManager_RegionAuto, regionCombo.Items[0]!.ToString());
                Assert.Equal(Strings.IFFManager_AddRow, iff.Controls.Find("btnAddRow", true).Single().Text);
                Assert.Equal(Strings.IFFManager_DeleteRows, iff.Controls.Find("btnDeleteRows", true).Single().Text);
                Assert.Equal(Strings.IFFManager_ManageColumns, iff.Controls.Find("btnAddColumn", true).Single().Text);
                Assert.Equal(Strings.IFFManager_ShowRawRecord, iff.Controls.Find("chkShowRawRecord", true).Single().Text);
                Assert.False(((CheckBox)iff.Controls.Find("chkShowRawRecord", true).Single()).Checked);
                Assert.Equal(Strings.Common_OK, options.Controls.Find("btnOK", true).Single().Text);
                Assert.Equal(Strings.PakMaker_Author, pak.Controls.Find("label1", true).Single().Text);
                Assert.Equal(Strings.PakMaker_Author, pak.Controls.Find("label2", true).Single().Text);
                Assert.Equal(Strings.Pak_SecurityPak, pak.Controls.Find("ckSecurityPak", true).Single().Text);
                Assert.Equal(LocalizationManager.PortugueseBrazil,
                    ((KeyValuePair<string, string>)PrivateField<ToolStripComboBox>(diff, "cboLanguage").SelectedItem!).Value);
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

                LocalizationManager.SetCulture(LocalizationManager.Swedish);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.PakDiff_Title, diff.Text);
                Assert.Equal(Strings.Log_Title, log.Text);
                Assert.Equal(LocalizationManager.Swedish,
                    ((KeyValuePair<string, string>)PrivateField<ToolStripComboBox>(diff, "cboLanguage").SelectedItem!).Value);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumnDialog_LoadsEveryFieldTypeForEditing()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                foreach (IffFieldType type in Enum.GetValues<IffFieldType>())
                {
                    int width = type switch
                    {
                        IffFieldType.Boolean or IffFieldType.Byte => 1,
                        IffFieldType.UInt16 or IffFieldType.Int16 => 2,
                        IffFieldType.UInt32 or IffFieldType.Int32 or IffFieldType.Single or IffFieldType.BitField => 4,
                        IffFieldType.DateTime => 16,
                        IffFieldType.BooleanBitField or IffFieldType.ZeroBoolean => 1,
                        _ => 8
                    };
                    uint? mask = type is IffFieldType.BitField or IffFieldType.BooleanBitField ? 1u : null;
                    var field = new IffFieldDefinition(type.ToString(), 0, width, type, BitMask: mask);
                    using var dialog = new CustomIffColumnDialog(32, field);
                    ComboBox typeCombo = PrivateField<ComboBox>(dialog, "_type");
                    ComboBox encodingCombo = PrivateField<ComboBox>(dialog, "_encoding");
                    Assert.Equal(type, typeCombo.SelectedItem);
                    Assert.Equal(Enum.GetValues<IffFieldType>().Length, typeCombo.Items.Count);
                    Assert.Equal(type == IffFieldType.FixedString, encodingCombo.Enabled);
                    Assert.NotEmpty(encodingCombo.Items);
                }
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_ShowsCoverageAndHidesOnlyCatchAllRawColumn()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 2,
                [
                    new IffField("Known", 0, 1, IffFieldType.Byte),
                    new IffField("Raw record", 0, 2, IffFieldType.Raw, false)
                ]);
                var document = new IffDocumentInfo("Test.iff", "TH", 2, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                var coverage = PrivateField<Label>(form, "lblSchemaCoverage");
                var showRaw = PrivateField<CheckBox>(form, "chkShowRawRecord");
                Assert.Equal(2, grid.Columns.Count);
                Assert.Contains("1 / 2", coverage.Text);

                showRaw.Checked = true;
                Assert.Equal(3, grid.Columns.Count);
                Assert.Contains("1 / 2", coverage.Text);
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
        IffStringEncodingPreferences.PreferencePathOverride = null;
        IffSchemaPreferences.SchemaDirectoryOverride = null;
        IffRawRecordPreferences.PreferencePathOverride = null;
        Directory.Delete(_tempDirectory, recursive: true);
    }
}
