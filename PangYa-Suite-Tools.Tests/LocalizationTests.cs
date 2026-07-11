using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
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
        FileDialogFactory.PreferencePathOverride =
            Path.Combine(_tempDirectory, "file-dialog-directories.txt");
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
    public void IffRegionSelector_DisplaysDetectionWithoutCreatingManualOverride()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                var combo = PrivateField<ToolStripComboBox>(form, "cboRegion");
                MethodInfo refresh = typeof(FrmIFFManager).GetMethod("RefreshRegionComboBox",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                PropertyInfo selectedSchema = typeof(FrmIFFManager).GetProperty("SelectedSchemaRegion",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

                refresh.Invoke(form, [null, "Japan_30312"]);
                Assert.Equal("Japan_30312", combo.Text);
                Assert.Null(selectedSchema.GetValue(form));

                refresh.Invoke(form, [null, "Global_57"]);
                Assert.Equal("Global_57", combo.Text);
                Assert.Null(selectedSchema.GetValue(form));

                LocalizationManager.SetCulture(LocalizationManager.Japonese);
                Assert.Equal("Global_57", combo.Text);
                Assert.Null(selectedSchema.GetValue(form));

                refresh.Invoke(form, [null, "Unknown"]);
                Assert.Equal(Strings.IFFManager_RegionAuto, combo.Text);

                refresh.Invoke(form, ["JP", null]);
                Assert.Equal("JP", selectedSchema.GetValue(form));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void FileDialogResources_ExposeExpectedExtensions()
    {
        foreach (string cultureName in new[] { LocalizationManager.English, LocalizationManager.PortugueseBrazil, LocalizationManager.Swedish, LocalizationManager.Japonese, LocalizationManager.French })
        {
            LocalizationManager.SetCulture(cultureName);

            Assert.Contains("*.pak", Strings.Pak_OpenFileFilter);
            Assert.Contains("*.*", Strings.Pak_OpenFileFilter);
            Assert.Contains("*.iff", Strings.IFFManager_OpenArchiveFilter);
            Assert.Contains("*.zip", Strings.IFFManager_OpenArchiveFilter);
            Assert.Contains("*.tga", Strings.Shop_IconFilter);
            Assert.Contains("updatelist*.*", Strings.UpdateList_OpenFileFilter);
            Assert.Contains("*.paksnap", Strings.PakDiff_SnapshotFilter);
            Assert.Contains("*.dat", Strings.PakMaker_InjectFilesFilter);
            Assert.Contains("*.*", Strings.PakMaker_InjectFilesFilter);
        }
    }

    [Fact]
    public void FileDialogFactory_CreatesTypeSpecificOpenDialogs()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                LocalizationManager.SetCulture(LocalizationManager.English);
                FileDialogFactory.ClearRememberedDirectories();
                string iconFallback = Path.Combine(_tempDirectory, "icons-fallback");
                string iconRemembered = Path.Combine(_tempDirectory, "icons-remembered");
                string pakDirectory = Path.Combine(_tempDirectory, "paks");
                string iffDirectory = Path.Combine(_tempDirectory, "iffs");
                string updateListDirectory = Path.Combine(_tempDirectory, "updatelists");
                string snapshotDirectory = Path.Combine(_tempDirectory, "snapshots");
                string deletedDirectory = Path.Combine(_tempDirectory, "deleted");
                Directory.CreateDirectory(iconFallback);
                Directory.CreateDirectory(iconRemembered);
                Directory.CreateDirectory(pakDirectory);
                Directory.CreateDirectory(iffDirectory);
                Directory.CreateDirectory(updateListDirectory);
                Directory.CreateDirectory(snapshotDirectory);
                Directory.CreateDirectory(deletedDirectory);

                using OpenFileDialog pak = FileDialogFactory.CreatePakOpenDialog();
                Assert.Equal(Strings.Pak_OpenFileTitle, pak.Title);
                Assert.Equal(Strings.Pak_OpenFileFilter, pak.Filter);
                Assert.Equal("pak", pak.DefaultExt);
                Assert.False(pak.Multiselect);
                Assert.True(pak.CheckFileExists);
                Assert.True(pak.CheckPathExists);
                Assert.False(string.IsNullOrWhiteSpace(pak.InitialDirectory));
                Assert.True(Directory.Exists(pak.InitialDirectory));

                using OpenFileDialog iff = FileDialogFactory.CreateIffOpenDialog();
                Assert.Equal(Strings.IFFManager_OpenArchiveFilter, iff.Filter);
                Assert.Equal("iff", iff.DefaultExt);
                Assert.False(string.IsNullOrWhiteSpace(iff.InitialDirectory));
                Assert.True(Directory.Exists(iff.InitialDirectory));

                using OpenFileDialog icon = FileDialogFactory.CreateIconOpenDialog(iconFallback);
                Assert.Equal(Strings.Shop_IconFilter, icon.Filter);
                Assert.Equal("tga", icon.DefaultExt);
                Assert.Equal(iconFallback, icon.InitialDirectory);

                using OpenFileDialog iconWithInvalidFallback = FileDialogFactory.CreateIconOpenDialog(
                    Path.Combine(_tempDirectory, "missing-icons"));
                Assert.False(string.IsNullOrWhiteSpace(iconWithInvalidFallback.InitialDirectory));
                Assert.True(Directory.Exists(iconWithInvalidFallback.InitialDirectory));

                using OpenFileDialog inject = FileDialogFactory.CreatePakInjectFilesDialog();
                Assert.Equal(Strings.PakMaker_InjectFilesFilter, inject.Filter);
                Assert.True(inject.Multiselect);
                Assert.False(string.IsNullOrWhiteSpace(inject.InitialDirectory));
                Assert.True(Directory.Exists(inject.InitialDirectory));

                using OpenFileDialog updateList = FileDialogFactory.CreateUpdateListOpenDialog();
                Assert.Equal(Strings.UpdateList_OpenFileFilter, updateList.Filter);
                Assert.Equal(Strings.UpdateList_SelectUpdateListFile, updateList.Title);
                Assert.False(string.IsNullOrWhiteSpace(updateList.InitialDirectory));
                Assert.True(Directory.Exists(updateList.InitialDirectory));

                using OpenFileDialog snapshot = FileDialogFactory.CreateSnapshotOpenDialog();
                Assert.Equal(Strings.PakDiff_SnapshotFilter, snapshot.Filter);
                Assert.Equal("paksnap", snapshot.DefaultExt);
                Assert.False(string.IsNullOrWhiteSpace(snapshot.InitialDirectory));
                Assert.True(Directory.Exists(snapshot.InitialDirectory));

                FileDialogFactory.RememberDirectory(FileDialogKind.Icon, Path.Combine(iconRemembered, "part.tga"));
                FileDialogFactory.RememberDirectory(FileDialogKind.Pak, Path.Combine(pakDirectory, "pangya.pak"));
                FileDialogFactory.RememberDirectory(FileDialogKind.Iff, Path.Combine(iffDirectory, "Item.iff"));
                FileDialogFactory.RememberDirectory(FileDialogKind.UpdateList, Path.Combine(updateListDirectory, "updatelist.txt"));
                FileDialogFactory.RememberDirectory(FileDialogKind.Snapshot, Path.Combine(snapshotDirectory, "before.paksnap"));
                Assert.True(File.Exists(FileDialogFactory.PreferencePathOverride));

                using OpenFileDialog rememberedIcon = FileDialogFactory.CreateIconOpenDialog(iconFallback);
                using OpenFileDialog rememberedPak = FileDialogFactory.CreatePakOpenDialog();
                using OpenFileDialog rememberedIff = FileDialogFactory.CreateIffOpenDialog();
                using OpenFileDialog rememberedUpdateList = FileDialogFactory.CreateUpdateListOpenDialog();
                using OpenFileDialog rememberedSnapshot = FileDialogFactory.CreateSnapshotOpenDialog();
                Assert.Equal(iconFallback, rememberedIcon.InitialDirectory);
                Assert.Equal(pakDirectory, rememberedPak.InitialDirectory);
                Assert.Equal(iffDirectory, rememberedIff.InitialDirectory);
                Assert.Equal(updateListDirectory, rememberedUpdateList.InitialDirectory);
                Assert.Equal(snapshotDirectory, rememberedSnapshot.InitialDirectory);

                FileDialogFactory.ResetRememberedDirectoriesForReload();
                using OpenFileDialog persistedIcon = FileDialogFactory.CreateIconOpenDialog(iconFallback);
                using OpenFileDialog persistedPak = FileDialogFactory.CreatePakOpenDialog();
                using OpenFileDialog persistedIff = FileDialogFactory.CreateIffOpenDialog();
                using OpenFileDialog persistedUpdateList = FileDialogFactory.CreateUpdateListOpenDialog();
                using OpenFileDialog persistedSnapshot = FileDialogFactory.CreateSnapshotOpenDialog();
                Assert.Equal(iconFallback, persistedIcon.InitialDirectory);
                Assert.Equal(pakDirectory, persistedPak.InitialDirectory);
                Assert.Equal(iffDirectory, persistedIff.InitialDirectory);
                Assert.Equal(updateListDirectory, persistedUpdateList.InitialDirectory);
                Assert.Equal(snapshotDirectory, persistedSnapshot.InitialDirectory);

                FileDialogFactory.RememberDirectory(FileDialogKind.Icon, Path.Combine(deletedDirectory, "gone.tga"));
                Directory.Delete(deletedDirectory);
                using OpenFileDialog invalidRememberedIcon = FileDialogFactory.CreateIconOpenDialog(iconFallback);
                Assert.Equal(iconFallback, invalidRememberedIcon.InitialDirectory);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                FileDialogFactory.ClearRememberedDirectories();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffIconPicker_UsesClickedFieldsIconDirectory()
    {
        string dataRoot = Path.Combine(_tempDirectory, "data");
        string iconDirectory = Path.Combine(dataRoot, "ui", "shop_myroom");
        Directory.CreateDirectory(iconDirectory);
        var field = new IffField("Icon", 0, 40, IffFieldType.Icon, IconPath: "ui/shop_myroom");

        string initialDirectory = IffFormRecordEditor.GetIconInitialDirectory(field, dataRoot);

        Assert.Equal(iconDirectory, initialDirectory);
    }

    [Fact]
    public void IffIconPicker_UsesResolvedIconDirectoryForNestedDataRoot()
    {
        string dataRoot = Path.Combine(_tempDirectory, "client");
        string iconDirectory = Path.Combine(dataRoot, "data", "ui", "shop_myroom");
        string iconPath = Path.Combine(iconDirectory, "icon.tga");
        Directory.CreateDirectory(iconDirectory);
        File.WriteAllBytes(iconPath, []);
        var field = new IffField("Icon", 0, 40, IffFieldType.Icon, IconPath: "ui/shop_myroom");

        string initialDirectory = IffFormRecordEditor.GetIconInitialDirectory(field, dataRoot, iconPath);

        Assert.Equal(iconDirectory, initialDirectory);
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
    public void UpdateListMigratedResources_ExistWithMatchingPlaceholdersInEveryCulture()
    {
        string[] keys =
        [
            "UpdateList_DecryptionSuccessFormat", "UpdateList_Scanning", "UpdateList_Done",
            "UpdateList_ErrorLogFormat", "UpdateList_ErrorStatus", "UpdateList_SourceLogFormat",
            "UpdateList_DestinationLogFormat", "UpdateList_KeyLogFormat", "UpdateList_VersionLogFormat",
            "UpdateList_ScanningFiles", "UpdateList_ExistingLoadedFormat",
            "UpdateList_ExistingLoadFailedFormat", "UpdateList_ScanningProgressFormat",
            "UpdateList_NewFileFormat", "UpdateList_ChangedFileFormat",
            "UpdateList_DeltaSummaryFormat", "UpdateList_GeneratedAtFormat",
            "UpdateList_InvalidSourceFolder", "UpdateList_InvalidDestinationFolder",
            "UpdateList_PatchVersionRequired"
        ];
        CultureInfo[] cultures =
        [
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo(LocalizationManager.PortugueseBrazil),
            CultureInfo.GetCultureInfo(LocalizationManager.Swedish),
            CultureInfo.GetCultureInfo(LocalizationManager.Japonese),
            CultureInfo.GetCultureInfo(LocalizationManager.French)
        ];

        foreach (string key in keys)
        {
            string neutral = ResourceValue(CultureInfo.InvariantCulture, key);
            string[] expectedPlaceholders = Placeholders(neutral);

            foreach (CultureInfo culture in cultures)
            {
                string localized = ResourceValue(culture, key);
                Assert.False(string.IsNullOrWhiteSpace(localized), $"{key} is empty for {culture.Name}.");
                Assert.Equal(expectedPlaceholders, Placeholders(localized));
            }
        }
    }

    [Fact]
    public void UpdateList_NoLongerDefinesGetTextHelper()
    {
        Assert.DoesNotContain(typeof(FrmUpdateList).GetMethods(
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            method => method.Name.Equals("Get" + "Text", StringComparison.Ordinal));
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
    public void PakMakerFilenameEncoding_UsesSavedSelection()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        PakFilenameEncodingPreferences.SaveCodePage(932);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmPakMaker();
                var combo = PrivateField<ToolStripComboBox>(form, "cboFilenameEncoding");
                var statusLabel = PrivateField<ToolStripStatusLabel>(form, "_filenameEncodingStatusLabel");

                Assert.Equal(932, PrivateValue<int>(form, "_selectedFilenameEncodingCodePage"));
                Assert.IsType<PakEncodingOption>(combo.SelectedItem);
                Assert.Equal(932, ((PakEncodingOption)combo.SelectedItem!).CodePage);
                Assert.Contains("932", statusLabel.Text);
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
    public void PakMakerFilenameEncodingSelection_FallsBackWhenDefaultIsUnavailable()
    {
        IReadOnlyList<PakEncodingOption> encodings =
        [
            new PakEncodingOption(932, "shift_jis", "Japanese (Shift-JIS)"),
            new PakEncodingOption(65001, "utf-8", "Unicode (UTF-8)")
        ];

        MethodInfo selector = typeof(FrmPakMaker).GetMethod(
            "SelectFilenameEncodingOption",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        var selected = (PakEncodingOption)selector.Invoke(null, [encodings, 1252])!;

        Assert.Equal(932, selected.CodePage);
    }

    [Fact]
    public void PakInjectionItems_UseSelectedFolderAndPreserveDroppedFolderHierarchy()
    {
        string looseFile = Path.Combine(_tempDirectory, "loose.bin");
        string secondFile = Path.Combine(_tempDirectory, "second.bin");
        string droppedFolder = Path.Combine(_tempDirectory, "effects");
        string nestedFolder = Path.Combine(droppedFolder, "particles");
        Directory.CreateDirectory(nestedFolder);
        File.WriteAllBytes(looseFile, [1]);
        File.WriteAllBytes(secondFile, [2]);
        File.WriteAllBytes(Path.Combine(droppedFolder, "root.dat"), [3]);
        File.WriteAllBytes(Path.Combine(nestedFolder, "spark.dat"), [4]);

        MethodInfo builder = typeof(FrmPakMaker).GetMethod(
            "BuildPakInjectItems", BindingFlags.Static | BindingFlags.NonPublic)!;

        var nestedItems = (List<PakInjectItem>)builder.Invoke(
            null, [new[] { looseFile, secondFile, droppedFolder }, "/data\\texture/"])!;

        Assert.Equal("data/texture", nestedItems.Single(item => item.SourcePath == looseFile).RelativeFolder);
        Assert.Equal("data/texture", nestedItems.Single(item => item.SourcePath == secondFile).RelativeFolder);
        Assert.Equal("data/texture/effects",
            nestedItems.Single(item => Path.GetFileName(item.SourcePath) == "root.dat").RelativeFolder);
        Assert.Equal("data/texture/effects/particles",
            nestedItems.Single(item => Path.GetFileName(item.SourcePath) == "spark.dat").RelativeFolder);

        var rootItems = (List<PakInjectItem>)builder.Invoke(null, [new[] { looseFile }, string.Empty])!;
        Assert.Equal(string.Empty, Assert.Single(rootItems).RelativeFolder);
    }

    [Fact]
    public void UpdateListXmlParser_ReadsPatchInfoAndFiles()
    {
        const string xml = """
            <?xml version="1.0" encoding="euc-kr" standalone="yes" ?>
            <patchVer value="JP.R7.983.00" />
            <patchNum value="1" />
            <updatelistVer value="2026070401" />
            <updatefiles count="480">
                <fileinfo fname="bs_notice_popup00.jpg" fdir="\" fsize="58485" fcrc="-19843887" fdate="2025-05-12" ftime="00:21:51" pname="bs_notice_popup00.jpg.zip" psize="717469" />
            </updatefiles>
            """;

        MethodInfo parser = typeof(FrmUpdateList).GetMethod(
            "ParseUpdateListXml",
            BindingFlags.Static | BindingFlags.NonPublic)!;

        object document = parser.Invoke(null, [xml])!;
        Assert.Equal("JP.R7.983.00", document.GetType().GetProperty("PatchVersion")!.GetValue(document));
        Assert.Equal("1", document.GetType().GetProperty("PatchNumber")!.GetValue(document));
        Assert.Equal("2026070401", document.GetType().GetProperty("UpdateListVersion")!.GetValue(document));
        Assert.Equal(480, document.GetType().GetProperty("DeclaredCount")!.GetValue(document));

        var files = (IEnumerable)document.GetType().GetProperty("Files")!.GetValue(document)!;
        object file = files.Cast<object>().Single();
        Assert.Equal("bs_notice_popup00.jpg", file.GetType().GetProperty("FileName")!.GetValue(file));
        Assert.Equal(@"\", file.GetType().GetProperty("Directory")!.GetValue(file));
        Assert.Equal("58485", file.GetType().GetProperty("FileSize")!.GetValue(file));
        Assert.Equal("bs_notice_popup00.jpg.zip", file.GetType().GetProperty("PackageName")!.GetValue(file));
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
                Assert.True(options.Controls.Find("chkRegisterFile", true).Single().Enabled);
                Assert.True(options.Controls.Find("chkShellContext", true).Single().Enabled);
                Assert.False(options.Controls.Find("lblAdminWarning", true).Single().Visible);
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
                        IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32 or
                            IffFieldType.Single or IffFieldType.BitField => 4,
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
                    Assert.Equal(type is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.Sound, encodingCombo.Enabled);
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
    public void SchemaColumnDialog_EditsItemIdReferenceMetadata()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var field = new IffFieldDefinition("Item1", 4, 4, IffFieldType.ItemIdReference,
                    Reference: new IffFieldReferenceDefinition("Item.iff"));
                using var dialog = new CustomIffColumnDialog(32, field);

                Assert.Equal("Item.iff", PrivateField<ComboBox>(dialog, "_referenceTargetFile").Text);
                Assert.Equal("ItemId", PrivateField<TextBox>(dialog, "_referenceTargetKeyField").Text);
                Assert.Equal("Name", PrivateField<TextBox>(dialog, "_referenceDisplayField").Text);
                Assert.Equal("Icon", PrivateField<TextBox>(dialog, "_referenceIconField").Text);
                Assert.True(PrivateField<CheckBox>(dialog, "_referencePickerEnabled").Checked);

                PrivateField<ComboBox>(dialog, "_referenceTargetFile").Text = "Part.iff";
                PrivateField<TextBox>(dialog, "_referenceTargetKeyField").Text = "ItemId";
                PrivateField<TextBox>(dialog, "_referenceDisplayField").Text = "Name";
                PrivateField<TextBox>(dialog, "_referenceIconField").Text = "Icon";
                PrivateField<CheckBox>(dialog, "_referencePickerEnabled").Checked = false;
                typeof(CustomIffColumnDialog).GetMethod("AcceptField", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, null);

                IffFieldDefinition result = dialog.FieldDefinition;
                Assert.Equal(IffFieldType.ItemIdReference, result.Type);
                Assert.Equal(4, result.Width);
                Assert.Equal("Part.iff", result.Reference!.TargetFile);
                Assert.False(result.Reference.PickerEnabled);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumnDialog_ShowsOnlyFieldsForSelectedType()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new CustomIffColumnDialog(32);
                ComboBox type = PrivateField<ComboBox>(dialog, "_type");
                TextBox iconPath = PrivateField<TextBox>(dialog, "_iconPath");
                TextBox soundPath = PrivateField<TextBox>(dialog, "_soundPath");
                TextBox bitMask = PrivateField<TextBox>(dialog, "_bitMask");
                TextBox minimum = PrivateField<TextBox>(dialog, "_minimum");
                TextBox referenceKey = PrivateField<TextBox>(dialog, "_referenceTargetKeyField");
                TableLayoutPanel layout = PrivateField<TableLayoutPanel>(dialog, "_layout");

                type.SelectedItem = IffFieldType.Icon;
                Assert.True(IsRowVisible(layout, 15));
                Assert.True(iconPath.Enabled);
                Assert.False(IsRowVisible(layout, 16));
                Assert.False(soundPath.Enabled);
                Assert.False(IsRowVisible(layout, 9));
                Assert.False(bitMask.Enabled);
                Assert.False(IsRowVisible(layout, 12));
                Assert.False(referenceKey.Enabled);

                type.SelectedItem = IffFieldType.BitField;
                Assert.False(IsRowVisible(layout, 15));
                Assert.False(IsRowVisible(layout, 16));
                Assert.True(IsRowVisible(layout, 9));
                Assert.True(IsRowVisible(layout, 7));

                type.SelectedItem = IffFieldType.ItemIdReference;
                Assert.True(IsRowVisible(layout, 12));
                Assert.True(IsRowVisible(layout, 7));
                Assert.False(IsRowVisible(layout, 16));

                type.SelectedItem = IffFieldType.Sound;
                Assert.False(IsRowVisible(layout, 15));
                Assert.True(IsRowVisible(layout, 16));
                Assert.True(soundPath.Enabled);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumnDialog_PopulatesReferenceIffDropdownAndPreservesUnknownValue()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var field = new IffFieldDefinition("Item1", 4, 4, IffFieldType.ItemIdReference,
                    Reference: new IffFieldReferenceDefinition("Special.iff"));
                using var dialog = new CustomIffColumnDialog(32, field, availableIffFiles: ["Item.iff", "Part.iff"]);

                ComboBox combo = PrivateField<ComboBox>(dialog, "_referenceTargetFile");
                Assert.Contains("Item.iff", combo.Items.Cast<object>().Select(Convert.ToString));
                Assert.Contains("Part.iff", combo.Items.Cast<object>().Select(Convert.ToString));
                Assert.Equal("Special.iff", combo.Text);
                Assert.Contains("Special.iff", combo.Items.Cast<object>().Select(Convert.ToString));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumnDialog_EditsIconFieldPath()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var field = new IffFieldDefinition("Icon", 4, 16, IffFieldType.Icon, IconPath: "ui/shop_myroom");
                using var dialog = new CustomIffColumnDialog(32, field);

                Assert.Equal("ui/shop_myroom", PrivateField<TextBox>(dialog, "_iconPath").Text);
                Assert.True(PrivateField<TextBox>(dialog, "_iconPath").Enabled);

                PrivateField<TextBox>(dialog, "_iconPath").Text = "ui/part";
                typeof(CustomIffColumnDialog).GetMethod("AcceptField", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, null);

                IffFieldDefinition result = dialog.FieldDefinition;
                Assert.Equal(IffFieldType.Icon, result.Type);
                Assert.Equal("ui/part", result.IconPath);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaColumnDialog_EditsSoundFieldPath()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var field = new IffFieldDefinition("Sound", 4, 16, IffFieldType.Sound, SoundPath: "sound/effect");
                using var dialog = new CustomIffColumnDialog(32, field);

                Assert.Equal("sound/effect", PrivateField<TextBox>(dialog, "_soundPath").Text);
                Assert.True(PrivateField<TextBox>(dialog, "_soundPath").Enabled);

                PrivateField<TextBox>(dialog, "_soundPath").Text = "sound/voice";
                typeof(CustomIffColumnDialog).GetMethod("AcceptField", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, null);

                IffFieldDefinition result = dialog.FieldDefinition;
                Assert.Equal(IffFieldType.Sound, result.Type);
                Assert.Equal("sound/voice", result.SoundPath);
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
                var schema = new IffSchema("Test", 4,
                [
                    new IffField("Known", 0, 1, IffFieldType.Byte),
                    new IffField("Custom raw", 1, 2, IffFieldType.Raw),
                    new IffField("Raw record", 0, 4, IffFieldType.Raw, false, IsVisible: true)
                ]);
                var document = new IffDocumentInfo("Test.iff", "TH", 4, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                var coverage = PrivateField<Label>(form, "lblSchemaCoverage");
                Assert.Equal(3, grid.Columns.Count);
                Assert.Equal("Known @0 [1 B]", grid.Columns[1].HeaderText);
                Assert.Equal("Custom raw @1 [2 B]", grid.Columns[2].HeaderText);
                Assert.DoesNotContain(grid.Columns.Cast<DataGridViewColumn>(),
                    column => column.HeaderText.StartsWith("Raw record", StringComparison.Ordinal));
                Assert.Contains("1 / 4", coverage.Text);
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
    public void IffEditor_RawRecordToolbarButtonTracksSelection()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                ToolStripButton rawButton = PrivateField<ToolStripButton>(form, "_toolbarRawRecord");
                Assert.Equal(Strings.IFFManager_RawRecord, rawButton.Text);
                Assert.False(rawButton.Enabled);

                var schema = new IffSchema("Test", 4,
                [
                    new IffField("Value", 0, 4, IffFieldType.UInt32),
                    new IffField("Raw record", 0, 4, IffFieldType.Raw, false, IsVisible: false)
                ]);
                var document = new IffDocumentInfo("Test.iff", "TH", 4, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                PrivateField<List<IffRecord>>(form, "_records").Add(IffRecord.CreateBlank(0, 4, schema));
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);
                typeof(FrmIFFManager).GetMethod("SetEditorView", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, [false]);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                grid.CurrentCell = grid.Rows[0].Cells[0];
                typeof(FrmIFFManager).GetMethod("UpdateToolbarState", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                Assert.True(rawButton.Enabled);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_CreatesFormAndGridViews()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 16,
                    [new IffField("Name", 0, 8, IffFieldType.FixedString)]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                PrivateField<List<IffRecord>>(form, "_records").Add(IffRecord.CreateBlank(0, 16, schema));
                typeof(FrmIFFManager).GetMethod("BuildColumns", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);
                typeof(FrmIFFManager).GetMethod("LoadFormEditor", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);

                var grid = PrivateField<DataGridView>(form, "gridRecords");
                var editor = PrivateField<IffFormRecordEditor>(form, "_formEditor");
                Assert.NotNull(editor);
                Assert.Equal(Strings.IFFManager_FormTabGeneral, Assert.Single(editor.TabNames));
                Assert.Equal(2, grid.Columns.Count);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_SchemaRefreshPreservesManualDataRoot()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 16,
                    [new IffField("Icon", 0, 16, IffFieldType.Icon, IconPath: "ui")]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                typeof(FrmIFFManager).GetField("_dataRootOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, _tempDirectory);
                PrivateField<List<IffRecord>>(form, "_records").Add(IffRecord.CreateBlank(0, 16, schema));

                InvokePrivateTask(form, "RefreshSchemaViewAsync");

                var editor = PrivateField<IffFormRecordEditor>(form, "_formEditor");
                TextBox textBox = editor.Controls.Find("txtIffDataRoot", true).OfType<TextBox>().Single();
                Assert.Equal(_tempDirectory, textBox.Text);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_SchemaRefreshRestoresDetectedDataRootAndIconPreview()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                string dataRoot = Path.Combine(_tempDirectory, "data");
                string iconPath = Path.Combine(dataRoot, "ui", "detected_icon.png");
                Directory.CreateDirectory(Path.GetDirectoryName(iconPath)!);
                using (var bitmap = new Bitmap(4, 4)) bitmap.Save(iconPath);

                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 16,
                    [new IffField("Icon", 0, 16, IffFieldType.Icon, IconPath: "ui")]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 16, schema);
                record.SetValue("Icon", "detected_icon", Encoding.ASCII);
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                PrivateField<TextBox>(form, "txtIffDirectory").Text = Path.Combine(dataRoot, "Test.iff");
                PrivateField<List<IffRecord>>(form, "_records").Add(record);

                InvokePrivateTask(form, "RefreshSchemaViewAsync");

                var editor = PrivateField<IffFormRecordEditor>(form, "_formEditor");
                TextBox dataRootText = editor.Controls.Find("txtIffDataRoot", true).OfType<TextBox>().Single();
                PictureBox picture = editor.Controls.Find("picSetItemRecordIcon", true).OfType<PictureBox>().Single();
                ToolTip toolTip = PrivateField<ToolTip>(editor, "_toolTip");
                Assert.Equal(dataRoot, dataRootText.Text);
                Assert.NotNull(picture.Image);
                Assert.Equal(iconPath, toolTip.GetToolTip(picture));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffEditor_SchemaRefreshRestoresDetectedDataRootForSoundFields()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                string dataRoot = Path.Combine(_tempDirectory, "sound-data");
                string soundPath = Path.Combine(dataRoot, "sound", "effect", "hit.wav");
                Directory.CreateDirectory(Path.GetDirectoryName(soundPath)!);
                File.WriteAllBytes(soundPath, []);

                using var form = new FrmIFFManager();
                var schema = new IffSchema("Test", 16,
                    [new IffField("Sound", 0, 16, IffFieldType.Sound, Encoding: Encoding.ASCII, SoundPath: "sound/effect")]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 16, schema);
                record.SetValue("Sound", "hit", Encoding.ASCII);
                typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(form, document);
                PrivateField<TextBox>(form, "txtIffDirectory").Text = Path.Combine(dataRoot, "Test.iff");
                PrivateField<List<IffRecord>>(form, "_records").Add(record);

                InvokePrivateTask(form, "RefreshSchemaViewAsync");

                var editor = PrivateField<IffFormRecordEditor>(form, "_formEditor");
                TextBox dataRootText = editor.Controls.Find("txtIffDataRoot", true).OfType<TextBox>().Single();
                Button play = editor.Controls.Find("btnPlaySound_Sound", true).OfType<Button>().Single();
                ToolTip toolTip = PrivateField<ToolTip>(editor, "_toolTip");
                Assert.Equal(dataRoot, dataRootText.Text);
                Assert.Equal(soundPath, toolTip.GetToolTip(play));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_AppliesFieldChangesToRecord()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("Test", 16,
                    [new IffField("Name", 0, 8, IffFieldType.FixedString)],
                    Ui: new IffSchemaUiDefinition(
                    [
                        new IffFormTabDefinition("Basic Info",
                        [
                            new IffFormFieldDefinition("Name", "Name", "text")
                        ])
                    ]));
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                var records = new List<IffRecord> { IffRecord.CreateBlank(0, 16, schema) };
                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, records, Encoding.ASCII);

                var textBox = editor.Controls.Find("field_Name", true).OfType<TextBox>().Single();
                textBox.Text = "Club";
                Assert.True(editor.ApplyChanges());

                Assert.Equal("Club", records[0].GetValue("Name", Encoding.ASCII));
                Assert.True(records[0].IsDirty);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_LongStringUsesExpandedMultilineTextBox()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("Test", 520,
                [
                    new IffField("Name", 0, 8, IffFieldType.FixedString),
                    new IffField("Description", 8, 512, IffFieldType.LongString)
                ]);
                var document = new IffDocumentInfo("Test.iff", "TH", 520, schema,
                    new IffHeader(1, 0, 11, [0, 0, 0]));
                var records = new List<IffRecord> { IffRecord.CreateBlank(0, 520, schema) };
                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, records, Encoding.ASCII);

                TextBox normal = editor.Controls.Find("field_Name", true).OfType<TextBox>().Single();
                TextBox longText = editor.Controls.Find("field_Description", true).OfType<TextBox>().Single();
                Assert.False(normal.Multiline);
                Assert.True(longText.Multiline);
                Assert.True(longText.AcceptsReturn);
                Assert.Equal(ScrollBars.Vertical, longText.ScrollBars);
                int expectedHeight = longText.Font.Height * 5 + 8;
                Assert.True(longText.Height >= expectedHeight);
                Assert.True(longText.Right <= longText.Parent!.ClientSize.Width);
                var layout = Assert.IsType<TableLayoutPanel>(longText.Parent);
                Assert.Equal(3, layout.GetColumnSpan(longText));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_LoadsRecordsWithoutThrowingForMissingDisplayFields()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            EventHandler<FirstChanceExceptionEventArgs>? handler = null;
            try
            {
                var schema = new IffSchema("Test", 4,
                    [new IffField("Value", 0, 4, IffFieldType.UInt32)]);
                var document = new IffDocumentInfo("Test.iff", "TH", 4, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                var records = Enumerable.Range(0, 5)
                    .Select(index =>
                    {
                        IffRecord record = IffRecord.CreateBlank(index, 4, schema);
                        record.SetValue("Value", (uint)index, Encoding.ASCII);
                        return record;
                    })
                    .ToList();
                int keyNotFoundCount = 0;
                handler = (_, args) =>
                {
                    if (args.Exception is KeyNotFoundException) keyNotFoundCount++;
                };
                AppDomain.CurrentDomain.FirstChanceException += handler;

                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, records, Encoding.ASCII);

                Assert.Equal(0, keyNotFoundCount);
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                if (handler is not null) AppDomain.CurrentDomain.FirstChanceException -= handler;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_UsesDatePickerForDateTimeFields()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("Test", 16,
                    [new IffField("Created", 0, 16, IffFieldType.DateTime)]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                var records = new List<IffRecord> { IffRecord.CreateBlank(0, 16, schema) };
                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, records, Encoding.ASCII);

                DateTime selected = new(2026, 7, 8, 12, 30, 0);
                DateTimePicker picker = editor.Controls.Find("field_Created", true).OfType<DateTimePicker>().Single();
                picker.Value = selected;
                picker.Checked = true;
                Assert.True(editor.ApplyChanges());

                Assert.Equal(selected, records[0].GetValue("Created", Encoding.ASCII));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_UsesPlayControlForSoundFields()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("Test", 16,
                    [new IffField("Sound", 0, 16, IffFieldType.Sound, Encoding: Encoding.ASCII, SoundPath: "sound/effect")]);
                var document = new IffDocumentInfo("Test.iff", "TH", 16, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                var records = new List<IffRecord> { IffRecord.CreateBlank(0, 16, schema) };
                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, records, Encoding.ASCII);

                Panel panel = editor.Controls.Find("field_Sound", true).OfType<Panel>().Single();
                TextBox textBox = panel.Controls.Find("txtSound_Sound", true).OfType<TextBox>().Single();
                Button play = panel.Controls.Find("btnPlaySound_Sound", true).OfType<Button>().Single();
                Assert.Equal(Strings.IFFManager_PlaySound, play.Text);

                textBox.Text = "effect_01";
                Assert.True(editor.ApplyChanges());

                Assert.Equal("effect_01", records[0].GetValue("Sound", Encoding.ASCII));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_ShowsSetItemPreviewWhenProviderIsAvailable()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("SetItem", 24,
                [
                    new IffField("ItemCount", 0, 4, IffFieldType.UInt32),
                    new IffField("Item1", 4, 4, IffFieldType.ItemIdReference,
                        Reference: new IffFieldReference("Item.iff")),
                    new IffField("Item1Count", 8, 2, IffFieldType.UInt16)
                ]);
                var document = new IffDocumentInfo("SetItem.iff", "TH", 24, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 24, schema);
                record.SetValue("ItemCount", 1u, Encoding.ASCII);
                record.SetValue("Item1", 123u, Encoding.ASCII);
                record.SetValue("Item1Count", (ushort)5, Encoding.ASCII);

                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, [record], Encoding.ASCII);
                editor.SetReferenceResolver(new FakeReferenceResolver());

                GroupBox group = editor.Controls.Find("grpSetItemPreview", true).OfType<GroupBox>().Single();
                Assert.True(group.Visible);
                Assert.Contains(editor.Controls.Find("lblReferenceName", true).OfType<Label>(),
                    label => label.Text == "Preview Item");
                Assert.Contains(editor.Controls.Find("lblReferenceDetails", true).OfType<Label>(),
                    label => label.Text.Contains("123", StringComparison.Ordinal) &&
                             label.Text.Contains("Item.iff", StringComparison.Ordinal));
                Assert.Contains(editor.Controls.Find("lblReferenceStatus", true).OfType<Label>(),
                    label => label.Text == Strings.IFFManager_SetItemMissingIcon);
                Assert.Contains(editor.Controls.Find("lblReference_Item1", true).OfType<Label>(),
                    label => label.Text.Contains("Preview Item", StringComparison.Ordinal));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_CanSetSetItemReferenceFromPickerResult()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("SetItem", 24,
                [
                    new IffField("ItemCount", 0, 4, IffFieldType.UInt32),
                    new IffField("Item1", 4, 4, IffFieldType.ItemIdReference,
                        Reference: new IffFieldReference("Item.iff")),
                    new IffField("Item1Count", 8, 2, IffFieldType.UInt16)
                ]);
                var document = new IffDocumentInfo("SetItem.iff", "TH", 24, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 24, schema);
                record.SetValue("ItemCount", 1u, Encoding.ASCII);
                record.SetValue("Item1", 123u, Encoding.ASCII);
                record.SetValue("Item1Count", (ushort)5, Encoding.ASCII);

                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, [record], Encoding.ASCII);
                editor.SetReferenceResolver(new FakeReferenceResolver());

                Assert.True(editor.TrySetItemReference(1, 456u));

                Assert.Equal(456u, record.GetValue("Item1", Encoding.ASCII));
                Assert.Contains(editor.Controls.Find("lblReferenceName", true).OfType<Label>(),
                    label => label.Text == "Other Item");
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_CanSetSetItemIconFromDataRootPath()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                string iconPath = Path.Combine(_tempDirectory, "ui", "new_icon.png");
                Directory.CreateDirectory(Path.GetDirectoryName(iconPath)!);
                using (var bitmap = new Bitmap(4, 4)) bitmap.Save(iconPath);

                var schema = new IffSchema("SetItem", 32,
                [
                    new IffField("Icon", 0, 16, IffFieldType.Icon, IconPath: "ui"),
                    new IffField("Item1", 16, 4, IffFieldType.UInt32)
                ]);
                var document = new IffDocumentInfo("SetItem.iff", "TH", 32, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 32, schema);
                record.SetValue("Icon", "old_icon", Encoding.ASCII);

                using var editor = new IffFormRecordEditor();
                editor.LoadDocument(document, [record], Encoding.ASCII);
                editor.SetReferenceResolver(new FakeReferenceResolver(_tempDirectory, iconPath));

                Assert.True(editor.TrySetSetItemIconFromPath(iconPath));

                Assert.Equal("new_icon", record.GetValue("Icon", Encoding.ASCII));
                PictureBox picture = editor.Controls.Find("picSetItemRecordIcon", true).OfType<PictureBox>().Single();
                Assert.NotNull(picture.Image);
                ToolTip toolTip = PrivateField<ToolTip>(editor, "_toolTip");
                Assert.Equal(iconPath, toolTip.GetToolTip(picture));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void IffFormEditor_DisplaysDataRootAndRaisesChangeRequest()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var schema = new IffSchema("SetItem", 24,
                [
                    new IffField("Item1", 0, 4, IffFieldType.ItemIdReference,
                        Reference: new IffFieldReference("Item.iff"))
                ]);
                var document = new IffDocumentInfo("SetItem.iff", "TH", 24, schema,
                    new IffHeader(0, 0, 11, [0, 0, 0]));
                IffRecord record = IffRecord.CreateBlank(0, 24, schema);
                using var editor = new IffFormRecordEditor();

                editor.LoadDocument(document, [record], Encoding.ASCII);
                editor.SetDataRootPath(_tempDirectory);

                TextBox textBox = editor.Controls.Find("txtIffDataRoot", true).OfType<TextBox>().Single();
                Button browse = editor.Controls.Find("btnBrowseIffDataRoot", true).OfType<Button>().Single();

                Assert.True(textBox.ReadOnly);
                Assert.Equal(_tempDirectory, textBox.Text);
                Assert.Equal(Strings.Iff_Browse, browse.Text);
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
    public void SchemaManager_SaveValidationKeepsDialogOpenWhenInvalid()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                IffFieldDefinition[] fields =
                [
                    new("Item1", 0, 4, IffFieldType.ItemIdReference)
                ];
                using var dialog = new IffSchemaManagerDialog(32, fields);

                Assert.False(dialog.SaveIfValid(showMessage: false));

                Assert.NotEqual(DialogResult.OK, dialog.DialogResult);
                Assert.False(dialog.IsDisposed);
                Assert.Equal(fields.Select(field => field.Name), dialog.Fields.Select(field => field.Name));
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void SchemaManager_SaveValidationClosesDialogWhenValid()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new IffSchemaManagerDialog(32,
                [
                    new IffFieldDefinition("Value", 0, 4, IffFieldType.UInt32)
                ]);

                Assert.True(dialog.SaveIfValid(showMessage: false));

                Assert.Equal(DialogResult.OK, dialog.DialogResult);
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
    public void SchemaManager_SortsByOffsetStably()
    {
        IffFieldDefinition[] sorted = IffSchemaManagerDialog.SortByOffset(
        [
            new("Later", 12, 2, IffFieldType.UInt16),
            new("First overlay", 4, 4, IffFieldType.UInt32),
            new("Second overlay", 4, 4, IffFieldType.BitField, BitMask: 0x0F),
            new("Earlier", 2, 2, IffFieldType.UInt16)
        ]);

        Assert.Equal(["Earlier", "First overlay", "Second overlay", "Later"],
            sorted.Select(field => field.Name));
    }

    [Fact]
    public void SchemaManager_RemovesLegacyCatchAllRawRecordOnOpen()
    {
        using var dialog = new IffSchemaManagerDialog(32,
        [
            new IffFieldDefinition("Value", 0, 4, IffFieldType.UInt32),
            new IffFieldDefinition("Raw record", 0, 32, IffFieldType.Raw, false, IsVisible: false),
            new IffFieldDefinition("Custom raw", 4, 2, IffFieldType.Raw)
        ]);

        Assert.Equal(["Value", "Custom raw"], dialog.Fields.Select(field => field.Name));
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
    public void RawRecordWindow_InsertFieldDropsLegacyCatchAllAndUsesOffsetOrder()
    {
        IffFieldDefinition[] fields =
        [
            new("Later", 12, 2, IffFieldType.UInt16),
            new("Raw record", 0, 32, IffFieldType.Raw, false, IsVisible: false),
            new("Earlier", 2, 2, IffFieldType.UInt16)
        ];

        IReadOnlyList<IffFieldDefinition> updated = FrmIFFManager.AddFieldFromRawRecordWindow(
            fields, 32, new IffFieldDefinition("Selected", 8, 2, IffFieldType.Raw));

        Assert.Equal(["Earlier", "Selected", "Later"], updated.Select(field => field.Name));
    }

    [Fact]
    public void RawRecordColumnSelection_RequiresContiguousBytesAndUsesAbsoluteOffset()
    {
        Assert.True(RawRecordColumnDialog.TryGetSelection([12, 13, 14], 18,
            out int offset, out int width));
        Assert.Equal(12, offset);
        Assert.Equal(3, width);
        Assert.False(RawRecordColumnDialog.TryGetSelection([11, 13], 18, out _, out _));
        Assert.False(RawRecordColumnDialog.TryGetSelection([18], 18, out _, out _));
    }

    [Fact]
    public void RawRecordWindow_FieldTypeSelectionSelectsFieldWidthAndShowsValue()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var dialog = new RawRecordColumnDialog(8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 3);
                ComboBox type = dialog.Controls.Find("cboRawFieldType", true).OfType<ComboBox>().Single();
                TextBox value = dialog.Controls.Find("txtSelectedRawValue", true).OfType<TextBox>().Single();

                type.SelectedItem = IffFieldType.UInt32;
                typeof(RawRecordColumnDialog).GetMethod("SelectByteRange", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(dialog, [2, RawRecordColumnDialog.PreferredWidth(IffFieldType.UInt32, 3) + 1]);

                DataGridView grid = dialog.Controls.Find("gridRawBytes", true).OfType<DataGridView>().Single();
                int[] selected = grid.SelectedCells.Cast<DataGridViewCell>()
                    .Select(cell => cell.Tag)
                    .OfType<int>()
                    .Order()
                    .ToArray();
                int[] allOffsets = grid.Rows.Cast<DataGridViewRow>()
                    .SelectMany(row => row.Cells.Cast<DataGridViewCell>())
                    .Select(cell => cell.Tag)
                    .OfType<int>()
                    .Order()
                    .ToArray();
                Assert.Equal(Enumerable.Range(0, 8), allOffsets);
                Assert.Equal([2, 3, 4, 5], selected);
                Assert.Contains("03 04 05 06", value.Text, StringComparison.Ordinal);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void RawRecordWindow_StringFieldSelectionsShowDecodedText()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                byte[] record = Encoding.ASCII.GetBytes("Hi\0Z");
                using var dialog = new RawRecordColumnDialog(record.Length, record, 4,
                    stringEncoding: Encoding.ASCII);
                ComboBox type = dialog.Controls.Find("cboRawFieldType", true).OfType<ComboBox>().Single();
                TextBox value = dialog.Controls.Find("txtSelectedRawValue", true).OfType<TextBox>().Single();
                MethodInfo select = typeof(RawRecordColumnDialog).GetMethod("SelectByteRange",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

                foreach (IffFieldType stringType in new[] { IffFieldType.FixedString, IffFieldType.Icon, IffFieldType.Sound })
                {
                    type.SelectedItem = stringType;
                    select.Invoke(dialog, [0, 3]);

                    Assert.Contains("Hi", value.Text, StringComparison.Ordinal);
                    Assert.DoesNotContain("48 69", value.Text, StringComparison.Ordinal);
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
    public void RawRecordWindow_ResetByteCellStyleRestoresNormalFont()
    {
        using var normalFont = new Font(SystemFonts.DefaultFont, FontStyle.Regular);
        using var boldFont = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
        var style = new DataGridViewCellStyle
        {
            Font = boldFont,
            BackColor = Color.Red,
            ForeColor = Color.Yellow,
            SelectionBackColor = Color.Black,
            SelectionForeColor = Color.White
        };

        RawRecordColumnDialog.ResetByteCellStyle(style, normalFont);

        Assert.Same(normalFont, style.Font);
        Assert.False(style.Font!.Bold);
        Assert.Equal(SystemColors.Window, style.BackColor);
        Assert.Equal(SystemColors.WindowText, style.ForeColor);
        Assert.Equal(SystemColors.Highlight, style.SelectionBackColor);
        Assert.Equal(SystemColors.HighlightText, style.SelectionForeColor);
    }

    [Fact]
    public void RawRecordWindow_DetectsDefinedFieldHighlightsAndOverlaps()
    {
        var raw = new IffField("Raw record", 0, 8, IffFieldType.Raw, false);
        var schema = new IffSchema("Test", 8,
        [
            new IffField("First", 2, 3, IffFieldType.Raw),
            new IffField("Second", 4, 2, IffFieldType.Raw),
            raw
        ]);

        Assert.Equal((0, false), RawRecordColumnDialog.RawByteFieldVisual(schema, 8, 3));
        Assert.Equal((0, true), RawRecordColumnDialog.RawByteFieldVisual(schema, 8, 4));
        Assert.Equal((null, false), RawRecordColumnDialog.RawByteFieldVisual(schema, 8, 7));
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

    private static string ResourceValue(CultureInfo culture, string key)
    {
        var set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return Assert.IsType<string>(set.GetObject(key));
    }

    private static string[] Placeholders(string value) => Regex.Matches(value, @"\{\d+(?::[^}]*)?\}")
        .Select(match => match.Value)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

    private static T PrivateField<T>(object instance, string name) where T : class =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!);

    private static T PrivateValue<T>(object instance, string name) where T : struct =>
        (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(instance)!);

    private static bool IsRowVisible(TableLayoutPanel layout, int row) => layout.RowStyles[row].Height > 0;

    private static void InvokePrivateTask(object instance, string name)
    {
        var task = (Task)instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(instance, [CancellationToken.None])!;
        task.GetAwaiter().GetResult();
    }

    private sealed class FakeReferenceResolver : IIffReferenceResolver
    {
        private readonly string? _iconPath;
        private readonly IReadOnlyDictionary<uint, IffReferenceCatalogItem> _items;

        public FakeReferenceResolver(string? dataRoot = null, string? iconPath = null)
        {
            DataRoot = dataRoot;
            _iconPath = iconPath;
            IffReferenceCatalogItem[] catalog =
            [
                new IffReferenceCatalogItem(123, "Preview Item", "missing_icon", null, "Item.iff"),
                new IffReferenceCatalogItem(456, "Other Item", "other_icon", null, "Item.iff")
            ];
            Catalog = catalog;
            _items = catalog.ToDictionary(item => item.Key);
        }

        public IReadOnlyList<IffReferenceCatalogItem> Catalog { get; }

        public string? DataRoot { get; }

        public IReadOnlyList<IffReferenceCatalogItem> GetCatalog(IffField field) =>
            field.Reference?.TargetFile.Equals("Item.iff", StringComparison.OrdinalIgnoreCase) == true ? Catalog : [];

        public IffReferenceDisplay Resolve(IffField field, object? value)
        {
            uint key = IffReferenceResolver.ConvertReferenceKey(value);
            if (_items.TryGetValue(key, out IffReferenceCatalogItem? item))
                return new IffReferenceDisplay(field, key, item.TargetFile, item.Name, item.IconId, item.IconPath,
                    MissingRecord: false, MissingIcon: item.IconPath is null);
            return new IffReferenceDisplay(field, key, field.Reference?.TargetFile ?? string.Empty,
                "Missing", string.Empty, null, MissingRecord: true, MissingIcon: true);
        }

        public string? TryResolveIconPath(IffField? field, string iconId) => _iconPath;
    }

    public void Dispose()
    {
        LocalizationManager.SetCulture(LocalizationManager.English);
        LocalizationManager.PreferencePathOverride = null;
        PakFilenameEncodingPreferences.PreferencePathOverride = null;
        IffStringEncodingPreferences.PreferencePathOverride = null;
        FileDialogFactory.PreferencePathOverride = null;
        FileDialogFactory.ClearRememberedDirectories();
        IffSchemaPreferences.SchemaDirectoryOverride = null;
        Directory.Delete(_tempDirectory, recursive: true);
    }
}
