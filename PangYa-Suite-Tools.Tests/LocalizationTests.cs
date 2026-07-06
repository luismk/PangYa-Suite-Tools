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

        LocalizationManager.SetCulture(LocalizationManager.Japonese);
        Assert.Equal("言語 (Language):", Strings.Common_Language);

        LocalizationManager.SetCulture(LocalizationManager.French);
        Assert.Equal("Langue :", Strings.Common_Language);
    }

    [Fact]
    public void CompositeResource_FormatsInBothCultures()
    {
        foreach (string cultureName in new[] { LocalizationManager.English, LocalizationManager.PortugueseBrazil, LocalizationManager.Swedish, LocalizationManager.Japonese, LocalizationManager.French })
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
                Assert.Equal(Strings.Menu_Shop, menu.Controls.Find("btnOpenShop", true).Single().Text);
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
                Assert.Empty(iff.Controls.Find("chkShowRawRecord", true));
                Assert.Equal(Strings.IFFManager_ContainerKey,
                    PrivateField<ToolStripStatusLabel>(iff, "lblContainerKey").Text);
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
                Assert.Equal(Strings.Menu_Shop, menu.Controls.Find("btnOpenShop", true).Single().Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Common_OK, options.Controls.Find("btnOK", true).Single().Text);
                Assert.Equal(Strings.Pak_SecurityPak, pak.Controls.Find("ckSecurityPak", true).Single().Text);

                LocalizationManager.SetCulture(LocalizationManager.Swedish);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Menu_Shop, menu.Controls.Find("btnOpenShop", true).Single().Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.PakDiff_Title, diff.Text);
                Assert.Equal(Strings.Log_Title, log.Text);
                Assert.Equal(LocalizationManager.Swedish,
                    ((KeyValuePair<string, string>)PrivateField<ToolStripComboBox>(diff, "cboLanguage").SelectedItem!).Value);

                LocalizationManager.SetCulture(LocalizationManager.Japonese);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Menu_Shop, menu.Controls.Find("btnOpenShop", true).Single().Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.PakDiff_Title, diff.Text);
                Assert.Equal(LocalizationManager.Japonese,
                    ((KeyValuePair<string, string>)PrivateField<ToolStripComboBox>(diff, "cboLanguage").SelectedItem!).Value);

                LocalizationManager.SetCulture(LocalizationManager.French);
                Assert.Equal(Strings.Menu_Title, menu.Text);
                Assert.Equal(Strings.Pak_Title, pak.Text);
                Assert.Equal(Strings.Update_Title, update.Text);
                Assert.Equal(Strings.Iff_Title, iff.Text);
                Assert.Equal(Strings.Options_Title, options.Text);
                Assert.Equal(Strings.PakDiff_Title, diff.Text);
                Assert.Equal(LocalizationManager.French,
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
                    Assert.Equal(field.IsVisible ?? true, PrivateField<CheckBox>(dialog, "_visible").Checked);
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
    public void NewSchemaColumn_UsesRequestedInitialOffset()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new CustomIffColumnDialog(32, initialOffset: 9);
                Assert.Equal(9, PrivateField<NumericUpDown>(dialog, "_offset").Value);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumn_PreviousFieldButtonUsesPreviousOffsetAndWidth()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new CustomIffColumnDialog(32, initialOffset: 5, previousFieldEnd: 12);
                typeof(CustomIffColumnDialog).GetMethod("UsePreviousFieldEnd",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dialog, null);

                Assert.Equal(12, PrivateField<NumericUpDown>(dialog, "_offset").Value);
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
                    new IffField("Raw record", 0, 2, IffFieldType.Raw, false, IsVisible: false)
                ]);
                var document = new IffDocumentInfo("Test.iff", "TH", 2, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                var coverage = PrivateField<Label>(form, "lblSchemaCoverage");
                Assert.Equal(2, grid.Columns.Count);
                Assert.Equal("Known @0 [1 B]", grid.Columns[1].HeaderText);
                Assert.Contains("1 / 2", coverage.Text);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_UsesDatePickerColumnForDateTimeFields()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 16,
                    [new IffField("Created", 0, 16, IffFieldType.DateTime)]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                Assert.IsType<DataGridViewDateTimePickerColumn>(grid.Columns[1]);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void DatePickerEditingControl_DisplaysAssignedDateTimeValue()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var picker = new DataGridViewDateTimePickerEditingControl();
                var selected = new DateTime(2026, 7, 5, 14, 33, 0);

                picker.InitializeValue(selected);

                Assert.True(picker.Checked);
                Assert.Equal(selected, picker.Value);
                Assert.NotEqual("g", picker.CustomFormat);
                Assert.False(picker.EditingControlValueChanged);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaManager_ReordersFieldsAndKeepsStringDefault()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new IffSchemaManagerDialog(32,
                [
                    new IffFieldDefinition("First", 0, 4, IffFieldType.UInt32),
                    new IffFieldDefinition("Second", 4, 4, IffFieldType.UInt32)
                ], defaultStringSize: 12);
                ListBox list = PrivateField<ListBox>(dialog, "_list");
                list.SelectedIndex = 1;
                typeof(IffSchemaManagerDialog).GetMethod("MoveField", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, [-1]);

                Assert.Equal(["Second", "First"], dialog.Fields.Select(field => field.Name));
                Assert.Equal(12, dialog.DefaultStringSize);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaManager_WidthChangeShiftsFollowingFieldsButKeepsOverlaysAndRawAnchored()
    {
        IffFieldDefinition[] fields =
        [
            new("Value", 4, 4, IffFieldType.UInt32),
            new("Value bits", 4, 4, IffFieldType.BitField, BitMask: 0x0F),
            new("Following", 8, 2, IffFieldType.UInt16),
            new("Raw record", 0, 32, IffFieldType.Raw, false, IsVisible: false)
        ];

        IReadOnlyList<IffFieldDefinition> adjusted = IffSchemaManagerDialog.AdjustFollowingOffsets(
            fields, 0, fields[0] with { Type = IffFieldType.Raw, Width = 6 }, 32, 12);

        Assert.Equal(4, adjusted[1].Offset);
        Assert.Equal(10, adjusted[2].Offset);
        Assert.Equal(0, adjusted[3].Offset);
    }

    [Fact]
    public void SchemaManager_WidthIncreaseReducesTrailingFieldToKeepRecordSize()
    {
        IffFieldDefinition[] fields =
        [
            new("Value", 0, 4, IffFieldType.Raw),
            new("Following", 28, 4, IffFieldType.UInt32)
        ];

        IReadOnlyList<IffFieldDefinition> adjusted = IffSchemaManagerDialog.AdjustFollowingOffsets(
            fields, 0, fields[0] with { Width = 5 }, 32, 12);

        Assert.Equal(29, adjusted[1].Offset);
        Assert.Equal(3, adjusted[1].Width);
        Assert.Equal(IffFieldType.Raw, adjusted[1].Type);
    }

    [Fact]
    public void SchemaManager_OffsetScrollMovesSelectedAndFollowingFieldsTogether()
    {
        IffFieldDefinition[] fields =
        [
            new("Before", 0, 2, IffFieldType.UInt16),
            new("Selected", 4, 4, IffFieldType.UInt32),
            new("Selected bits", 4, 4, IffFieldType.BitField, BitMask: 0x0F),
            new("Following", 8, 2, IffFieldType.UInt16),
            new("Raw record", 0, 32, IffFieldType.Raw, false, IsVisible: false)
        ];

        IReadOnlyList<IffFieldDefinition> adjusted = IffSchemaManagerDialog.MoveFieldAndFollowingOffsets(
            fields, 1, 1, 32, 12);

        Assert.Equal(0, adjusted[0].Offset);
        Assert.Equal(5, adjusted[1].Offset);
        Assert.Equal(5, adjusted[2].Offset);
        Assert.Equal(9, adjusted[3].Offset);
        Assert.Equal(0, adjusted[4].Offset);
    }

    [Fact]
    public void SchemaManager_ShiftScrollChangesOnlySelectedField()
    {
        IffFieldDefinition[] fields =
        [
            new("Selected", 4, 4, IffFieldType.Raw),
            new("Following", 8, 4, IffFieldType.UInt32)
        ];

        IReadOnlyList<IffFieldDefinition> adjusted = IffSchemaManagerDialog.ReplaceFieldWithoutAdjustingFollowing(
            fields, 0, fields[0] with { Offset = 5, Width = 5 }, 32, 12);

        Assert.Equal(5, adjusted[0].Offset);
        Assert.Equal(5, adjusted[0].Width);
        Assert.Equal(8, adjusted[1].Offset);
        Assert.Equal(4, adjusted[1].Width);
    }

    [Fact]
    public void SchemaManager_SortsByOffsetStablyAndKeepsRawRecordLast()
    {
        IffFieldDefinition[] sorted = IffSchemaManagerDialog.SortByOffset(
        [
            new("Later", 12, 2, IffFieldType.UInt16),
            new("Raw record", 0, 32, IffFieldType.Raw, false),
            new("First overlay", 4, 4, IffFieldType.UInt32),
            new("Second overlay", 4, 4, IffFieldType.BitField, BitMask: 0x0F),
            new("Earlier", 2, 2, IffFieldType.UInt16)
        ]);

        Assert.Equal(["Earlier", "First overlay", "Second overlay", "Later", "Raw record"],
            sorted.Select(field => field.Name));
    }

    [Fact]
    public void SchemaManager_DetectsOverlapsButIgnoresCatchAllRawRecord()
    {
        IffFieldDefinition[] fields =
        [
            new("Value", 4, 4, IffFieldType.UInt32),
            new("Value bits", 4, 4, IffFieldType.BitField, BitMask: 0x0F),
            new("Separate", 8, 2, IffFieldType.UInt16),
            new("Raw record", 0, 32, IffFieldType.Raw, false)
        ];

        Assert.Equal([true, true, false, false],
            IffSchemaManagerDialog.FindOverlappingFields(fields, 32));
    }

    [Fact]
    public void RawRecordColumnSelection_RequiresContiguousBytesAndUsesAbsoluteOffset()
    {
        var raw = new IffField("Raw range", 10, 8, IffFieldType.Raw);

        Assert.True(RawRecordColumnDialog.TryGetSelection([2, 3, 4], raw,
            out int offset, out int width));
        Assert.Equal(12, offset);
        Assert.Equal(3, width);
        Assert.False(RawRecordColumnDialog.TryGetSelection([1, 3], raw, out _, out _));
        Assert.False(RawRecordColumnDialog.TryGetSelection([8], raw, out _, out _));
    }

    [Fact]
    public void SchemaTemplateCatalog_IncludesEmbeddedDefaultsWithoutUserFiles()
    {
        IReadOnlyList<IffSchemaDefinition> schemas = IffSchemaPreferences.LoadTemplateSchemas();

        IffSchemaDefinition character = schemas.Single(schema =>
            schema.FileName == "Character.iff" && schema.Region == "TH");
        Assert.Equal(40, character.DefaultStringSize);
        Assert.NotEmpty(character.Fields);
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
        Directory.Delete(_tempDirectory, recursive: true);
    }
}
