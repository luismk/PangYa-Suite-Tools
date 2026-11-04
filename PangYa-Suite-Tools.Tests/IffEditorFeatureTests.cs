using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PangYa_Suite_Tools.Configuration;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

public sealed class IffEditorFeatureTests
{
    [Theory]
    [InlineData(30447, 11, null, null, "Test.iff", false)]
    [InlineData(999, 42, "JP", null, "Test.iff", false)]
    [InlineData(999, 42, null, "TH", "Test.iff", false)]
    [InlineData(999, 42, null, null, "Test-JP.iff", false)]
    [InlineData(999, 42, null, null, "Test.iff", true)]
    public void RegionSelection_IsRequiredOnlyWhenReaderCannotResolveRegion(
        ushort revision,
        byte magic,
        string? selectedRegion,
        string? fallbackRegion,
        string fileName,
        bool expected)
    {
        byte[] bytes = [1, 0, (byte)revision, (byte)(revision >> 8), magic, 0, 0, 0, 0];
        using var stream = new MemoryStream(bytes, writable: false);
        using IffReader reader = IffReader.Open(stream, fileName,
            new(SchemaRegion: selectedRegion, FallbackSchemaRegion: fallbackRegion));

        Assert.Equal(expected, FrmIFFManager.RequiresRegionSelection(reader.Info));
    }

    [Fact]
    public void UnknownRegionDialog_ShowsHeaderAndOffersAllSchemaRegions()
    {
        RunSta(() =>
        {
            var document = new IffDocumentInfo("Test.iff", "Unknown", 516, null,
                new IffHeader(12, 999, 42, [0xAA, 0xBB, 0xCC]));
            using var dialog = new IffUnknownRegionDialog(document);
            TextBox header = dialog.Controls.Find("txtUnknownRegionHeader", true).OfType<TextBox>().Single();
            ComboBox regions = dialog.Controls.Find("cboUnknownRegion", true).OfType<ComboBox>().Single();

            Assert.Contains("12", header.Text);
            Assert.Contains("999", header.Text);
            Assert.Contains("42", header.Text);
            Assert.Contains("AABBCC", header.Text);
            Assert.Contains("516", header.Text);
            Assert.Equal(4, regions.Items.Count);
            regions.SelectedIndex = 2;
            Assert.Equal(IffHeader.Japan983Region, dialog.SelectedRegion);
            regions.SelectedIndex = 3;
            Assert.Equal("Global", dialog.SelectedRegion);
        });
    }

    [Fact]
    public void DescSearch_FiltersByDescriptionAndDisplaysTypeId()
    {
        RunSta(() =>
        {
            IffSchema schema = new("Desc", 36,
            [
                new IffField("TypeID", 0, 4, IffFieldType.UInt32),
                new IffField("Description", 4, 32, IffFieldType.FixedString)
            ]);
            IffRecord first = IffRecord.CreateBlank(0, 36, schema);
            first.SetValue("TypeID", 100u);
            first.SetValue("Description", "ordinary text", Encoding.UTF8);
            IffRecord second = IffRecord.CreateBlank(1, 36, schema);
            second.SetValue("TypeID", 200u);
            second.SetValue("Description", "contains hidden needle", Encoding.UTF8);
            var document = new IffDocumentInfo("Desc.iff", "Global", 36, schema,
                new IffHeader(2, 30447, 11, [0, 0, 0]));

            using var editor = new IffFormRecordEditor();
            editor.LoadDocument(document, new List<IffRecord> { first, second }, Encoding.UTF8);
            TextBox search = editor.Controls.Find("txtFormRecordSearch", true).OfType<TextBox>().Single();
            ListBox list = editor.Controls.Find("lstFormRecords", true).OfType<ListBox>().Single();
            search.Text = "needle";

            Assert.Single(list.Items.Cast<object>());
            Assert.Contains("200", list.Items[0]!.ToString());
        });
    }

    [Fact]
    public void FormRecordList_IsWiderAndPreviewsDirtyNameAndItemIdEdits()
    {
        RunSta(() =>
        {
            IffSchema schema = new("Item", 40,
            [
                new IffField("ItemId", 0, 4, IffFieldType.UInt32),
                new IffField("Name", 4, 32, IffFieldType.FixedString),
                new IffField("Price", 36, 4, IffFieldType.UInt32)
            ]);
            IffRecord record = IffRecord.CreateBlank(0, 40, schema);
            record.SetValue("ItemId", 100u);
            record.SetValue("Name", "Original", Encoding.UTF8);
            record.AcceptChanges();
            var document = new IffDocumentInfo("Item.iff", "Global", 40, schema,
                new IffHeader(1, 30447, 11, [0, 0, 0]));

            using var editor = new IffFormRecordEditor();
            int pendingStateChanges = 0;
            editor.PendingChangesChanged += (_, _) => pendingStateChanges++;
            editor.LoadDocument(document, new List<IffRecord> { record }, Encoding.UTF8);

            SplitContainer split = editor.Controls.OfType<SplitContainer>().Single();
            ListBox list = editor.Controls.Find("lstFormRecords", true).OfType<ListBox>().Single();
            TextBox name = editor.Controls.Find("field_Name", true).OfType<TextBox>().Single();
            NumericUpDown itemId = editor.Controls.Find("field_ItemId", true).OfType<NumericUpDown>().Single();

            Assert.Equal(320, split.SplitterDistance);
            Assert.Equal(280, split.Panel1MinSize);
            Assert.True(list.HorizontalScrollbar);
            Assert.False(list.Items[0]!.ToString()!.StartsWith("* ", StringComparison.Ordinal));

            name.Text = "Updated";
            itemId.Value = 200;

            Assert.True(editor.HasPendingChanges);
            Assert.Equal(1, pendingStateChanges);
            Assert.StartsWith("* ", list.Items[0]!.ToString());
            Assert.Contains("200", list.Items[0]!.ToString());
            Assert.Contains("Updated", list.Items[0]!.ToString());

            Assert.True(editor.ApplyChanges());
            Assert.False(editor.HasPendingChanges);
            Assert.True(record.IsDirty);
            Assert.StartsWith("* ", list.Items[0]!.ToString());

            record.AcceptChanges();
            editor.RefreshRecords();
            Assert.False(list.Items[0]!.ToString()!.StartsWith("* ", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void IffManager_TreatsStagedFormEditsAsUnsavedAndCommitsThem()
    {
        RunSta(() =>
        {
            IffSchema schema = new("Item", 36,
            [
                new IffField("ItemId", 0, 4, IffFieldType.UInt32),
                new IffField("Name", 4, 32, IffFieldType.FixedString)
            ]);
            IffRecord record = IffRecord.CreateBlank(0, 36, schema);
            record.AcceptChanges();
            var document = new IffDocumentInfo("Item.iff", "Global", 36, schema,
                new IffHeader(1, 30447, 11, [0, 0, 0]));

            using var form = new FrmIFFManager();
            typeof(FrmIFFManager).GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, document);
            Field<List<IffRecord>>(form, "_records").Add(record);
            typeof(FrmIFFManager).GetMethod("LoadFormEditor", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            IffFormRecordEditor editor = Field<IffFormRecordEditor>(form, "_formEditor");
            TextBox name = editor.Controls.Find("field_Name", true).OfType<TextBox>().Single();

            name.Text = "Staged";

            Assert.True(editor.HasPendingChanges);
            Assert.EndsWith(" *", form.Text);
            Assert.True((bool)typeof(FrmIFFManager).GetMethod("CommitPendingEdit",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null)!);
            Assert.False(editor.HasPendingChanges);
            Assert.True(record.IsDirty);
            Assert.Equal("Staged", record.GetValue("Name", Encoding.UTF8));
        });
    }

    [Fact]
    public void PatchSelection_StartsWithEveryCandidateChecked()
    {
        RunSta(() =>
        {
            var candidates = new[]
            {
                Candidate(1, 0),
                Candidate(2, 1)
            };
            using var dialog = new IffPatchSelectionDialog(new IffPatchAnalysis(candidates, ["Value"], 0, 0));
            CheckedListBox items = dialog.Controls.Find("lstPatchItems", true).OfType<CheckedListBox>().Single();

            Assert.Equal(2, items.CheckedItems.Count);
            Assert.Equal(new uint[] { 1, 2 }, dialog.SelectedItemIds);
        });
    }

    [Fact]
    public void PatchFieldSelection_StartsWithEveryCompatibleFieldChecked()
    {
        RunSta(() =>
        {
            var analysis = new IffPatchAnalysis(
                [Candidate(1, 0)],
                ["Name", "Price"],
                0,
                0);
            using var dialog = new IffPatchFieldSelectionDialog(analysis, [1u]);
            CheckedListBox fields = dialog.Controls.Find("lstPatchFields", true).OfType<CheckedListBox>().Single();

            Assert.Equal(2, fields.CheckedItems.Count);
            Assert.Equal(new[] { "Name", "Price" }, dialog.SelectedFieldNames);
        });
    }

    [Fact]
    public void PatchEncodingDialog_IdentifiesPatchFileAndEncodingPurpose()
    {
        RunSta(() =>
        {
            using var dialog = new IffPatchSourceOptionsDialog(Encoding.UTF8.CodePage, "Item.iff");

            Assert.Equal(Strings.IFFManager_PatchSourceOptions, dialog.Text);
            Assert.Contains(dialog.Controls.OfType<Label>(), label =>
                label.Text.Contains("Item.iff", StringComparison.Ordinal));
            Assert.Contains(dialog.Controls.OfType<Label>(), label =>
                label.Text == Strings.IFFManager_PatchSourceEncoding);
        });
    }

    [Fact]
    public void IffToolbar_ExposesActionsAndValueBearingSelectors()
    {
        RunSta(() =>
        {
            using var form = new FrmIFFManager();
            ToolStrip toolbar = Field<ToolStrip>(form, "_editorToolbar");
            ToolStripButton extract = Field<ToolStripButton>(form, "_toolbarExtract");
            ToolStripButton extractAll = Field<ToolStripButton>(form, "_toolbarExtractAll");
            ToolStripButton patch = Field<ToolStripButton>(form, "_toolbarPatch");
            ToolStripDropDownButton region = Field<ToolStripDropDownButton>(form, "_toolbarRegion");
            ToolStripDropDownButton key = Field<ToolStripDropDownButton>(form, "_toolbarContainerKey");
            ToolStripDropDownButton encoding = Field<ToolStripDropDownButton>(form, "_toolbarStringEncoding");
            StatusStrip status = Field<StatusStrip>(form, "statusStrip");

            Assert.Same(extract, toolbar.Items.Cast<ToolStripItem>().Single(item => item == extract));
            Assert.Equal(Strings.IFFManager_Extract, extract.Text);
            Assert.False(extract.Enabled);
            Assert.Same(extractAll, toolbar.Items.Cast<ToolStripItem>().Single(item => item == extractAll));
            Assert.Equal(Strings.IFFManager_ExtractAll, extractAll.Text);
            Assert.False(extractAll.Enabled);
            Assert.Same(patch, toolbar.Items.Cast<ToolStripItem>().Single(item => item == patch));
            Assert.Equal(Strings.IFFManager_Patch, patch.Text);
            Assert.False(patch.Enabled);
            Assert.Contains(region.DropDownItems.OfType<ToolStripMenuItem>(),
                item => item.Text == Strings.IFFManager_RegionGlobal);
            Assert.Contains(Strings.IFFManager_RegionAuto, region.Text);
            Assert.Contains(Strings.IFFManager_KeyNone, key.Text);
            Assert.Contains(region, toolbar.Items.Cast<ToolStripItem>());
            Assert.Contains(key, toolbar.Items.Cast<ToolStripItem>());
            Assert.Contains(encoding, toolbar.Items.Cast<ToolStripItem>());
            Assert.NotEmpty(encoding.DropDownItems);
            Assert.DoesNotContain(region, status.Items.Cast<ToolStripItem>());
            Assert.DoesNotContain(key, status.Items.Cast<ToolStripItem>());
            Assert.DoesNotContain(encoding, status.Items.Cast<ToolStripItem>());
            Assert.DoesNotContain(status.Items.Cast<ToolStripItem>(),
                item => item.Name is "lblStringEncoding" or "cboStringEncoding");

            int originalCodePage = IffStringEncodingPreferences.LoadCodePage();
            try
            {
                ToolStripMenuItem alternateEncoding = encoding.DropDownItems.OfType<ToolStripMenuItem>()
                    .First(item => item.Tag is PakEncodingOption option && option.CodePage != originalCodePage);
                var option = (PakEncodingOption)alternateEncoding.Tag!;
                alternateEncoding.PerformClick();
                Assert.Equal(option.CodePage, IffStringEncodingPreferences.LoadCodePage());
                Assert.Contains(option.Label, encoding.Text);
            }
            finally
            {
                IffStringEncodingPreferences.SaveCodePage(originalCodePage);
            }

            region.DropDownItems.OfType<ToolStripMenuItem>()
                .Single(item => item.Text == Strings.IFFManager_RegionGlobal).PerformClick();
            Assert.Contains(Strings.IFFManager_RegionGlobal, region.Text);
            Assert.Equal("Global", typeof(FrmIFFManager).GetProperty("SelectedSchemaRegion",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form));

            typeof(FrmIFFManager).GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, new IffContainerEntry("Test.iff", 0,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream())));
            typeof(FrmIFFManager).GetMethod("UpdateToolbarState",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            Assert.True(extract.Enabled);

            typeof(FrmIFFManager).GetField("_isExtracting", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, true);
            typeof(FrmIFFManager).GetMethod("UpdateToolbarState",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            Assert.False(extract.Enabled);
        });
    }

    [Fact]
    public void IffToolbar_ExtractAllRequiresAnIdleArchiveContainer()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-toolbar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            byte[] emptyIff = [0, 0, 0xEF, 0x76, 11, 0, 0, 0];
            string loosePath = Path.Combine(directory, "Loose.iff");
            File.WriteAllBytes(loosePath, emptyIff);
            string archivePath = Path.Combine(directory, "Archive.zip");
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                using Stream output = archive.CreateEntry("nested/Item.iff").Open();
                output.Write(emptyIff);
            }

            RunSta(() =>
            {
                using var form = new FrmIFFManager();
                ToolStripButton extractAll = Field<ToolStripButton>(form, "_toolbarExtractAll");
                FieldInfo containerField = typeof(FrmIFFManager).GetField(
                    "_container", BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo updateToolbar = typeof(FrmIFFManager).GetMethod(
                    "UpdateToolbarState", BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo refreshKeys = typeof(FrmIFFManager).GetMethod(
                    "RefreshContainerKeyComboBox", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ToolStripDropDownButton key = Field<ToolStripDropDownButton>(form, "_toolbarContainerKey");

                IffContainer loose = IffContainer.OpenAsync(loosePath).GetAwaiter().GetResult();
                containerField.SetValue(form, loose);
                updateToolbar.Invoke(form, null);
                Assert.False(extractAll.Enabled);
                loose.Dispose();

                IffContainer archive = IffContainer.OpenAsync(archivePath).GetAwaiter().GetResult();
                containerField.SetValue(form, archive);
                refreshKeys.Invoke(form, [false]);
                updateToolbar.Invoke(form, null);
                Assert.True(extractAll.Enabled);
                Assert.True(key.Enabled);
                Assert.True(key.DropDownItems.Count > 1);
                string encryptedKey = key.DropDownItems[1].Text!;
                ((ToolStripMenuItem)key.DropDownItems[1]).PerformClick();
                Assert.Contains(encryptedKey, key.Text);
                Assert.True((bool)typeof(FrmIFFManager).GetField("_containerEncodingDirty",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!);

                typeof(FrmIFFManager).GetField("_isSaving",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, true);
                updateToolbar.Invoke(form, null);
                Assert.False(extractAll.Enabled);

                typeof(FrmIFFManager).GetField("_isSaving",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, false);
                typeof(FrmIFFManager).GetField("_isExtracting",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, true);
                updateToolbar.Invoke(form, null);
                Assert.False(extractAll.Enabled);
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void NewIffResources_ExistWithMatchingPlaceholdersInEveryCulture()
    {
        string[] keys =
        [
            nameof(Strings.IFFManager_RegionGlobal),
            nameof(Strings.IFFManager_ToolbarRegionFormat),
            nameof(Strings.IFFManager_ToolbarKeyFormat),
            nameof(Strings.IFFManager_ToolbarStringEncodingFormat),
            nameof(Strings.IFFManager_UnknownRegionHeaderFormat),
            nameof(Strings.IFFManager_Extract),
            nameof(Strings.IFFManager_ExtractAll),
            nameof(Strings.IFFManager_ExtractAllTitle),
            nameof(Strings.IFFManager_ExtractTitle),
            nameof(Strings.IFFManager_ExtractFilter),
            nameof(Strings.IFFManager_ExtractedFormat),
            nameof(Strings.IFFManager_ExtractedAllFormat),
            nameof(Strings.IFFManager_ExtractCancelled),
            nameof(Strings.IFFManager_ExtractSourceConflict),
            nameof(Strings.IFFManager_ExtractAllNameCollisionFormat),
            nameof(Strings.IFFManager_ExtractAllInvalidNameFormat),
            nameof(Strings.IFFManager_ExtractAllFailedFormat),
            nameof(Strings.IFFManager_Patch),
            nameof(Strings.IFFManager_PatchEncodingDescriptionFormat),
            nameof(Strings.IFFManager_PatchItemFormat),
            nameof(Strings.IFFManager_PatchNoCompatibleFields),
            nameof(Strings.IFFManager_PatchSelectFields),
            nameof(Strings.IFFManager_PatchFieldSelectionDescription),
            nameof(Strings.IFFManager_PatchNoFieldsSelected),
            nameof(Strings.IFFManager_PatchNoChangesSelected),
            nameof(Strings.IFFManager_PatchSummaryFormat),
            nameof(Strings.IFFManager_PatchAppliedFormat)
        ];
        CultureInfo[] cultures =
        [
            CultureInfo.GetCultureInfo("pt-BR"),
            CultureInfo.GetCultureInfo("sv"),
            CultureInfo.GetCultureInfo("ja"),
            CultureInfo.GetCultureInfo("fr")
        ];

        foreach (string key in keys)
        {
            string neutral = Resource(CultureInfo.InvariantCulture, key);
            string[] placeholders = Placeholders(neutral);
            foreach (CultureInfo culture in cultures)
            {
                string localized = Resource(culture, key);
                Assert.False(string.IsNullOrWhiteSpace(localized));
                Assert.Equal(placeholders, Placeholders(localized));
            }
        }
    }

    [Fact]
    public async Task ExtractEntryAsync_PreservesOriginalBytesAndReplacesDestination()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-extract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            byte[] expected = Enumerable.Range(0, 1024).Select(value => (byte)(value % 251)).ToArray();
            string destination = Path.Combine(directory, "exported.iff");
            await File.WriteAllBytesAsync(destination, [0xFF]);
            var entry = new IffContainerEntry("nested/source.iff", expected.Length,
                _ => ValueTask.FromResult<Stream>(new MemoryStream(expected, writable: false)));

            await FrmIFFManager.ExtractEntryAsync(entry, destination,
                Path.Combine(directory, "container.zip"));

            Assert.Equal(expected, await File.ReadAllBytesAsync(destination));
            Assert.Empty(Directory.EnumerateFiles(directory, ".*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractEntryAsync_RejectsSourceAndCleansCancelledTemporaryOutput()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-extract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, "source.iff");
            string destination = Path.Combine(directory, "cancelled.iff");
            await File.WriteAllBytesAsync(source, [1, 2, 3]);
            await File.WriteAllBytesAsync(destination, [9, 8, 7]);
            byte[] originalDestination = await File.ReadAllBytesAsync(destination);
            var entry = new IffContainerEntry("source.iff", 3,
                _ => ValueTask.FromResult<Stream>(new MemoryStream([1, 2, 3], writable: false)));

            IOException conflict = await Assert.ThrowsAsync<IOException>(() =>
                FrmIFFManager.ExtractEntryAsync(entry, source, source));
            Assert.Equal(Strings.IFFManager_ExtractSourceConflict, conflict.Message);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                FrmIFFManager.ExtractEntryAsync(entry, destination, source, cancellation.Token));
            Assert.Equal(originalDestination, await File.ReadAllBytesAsync(destination));
            Assert.Empty(Directory.EnumerateFiles(directory, ".*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAllEntriesAsync_FlattensNamesAndAtomicallyReplacesDestinations()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-extract-all-{Guid.NewGuid():N}");
        string destination = Path.Combine(directory, "output");
        Directory.CreateDirectory(destination);
        try
        {
            string source = Path.Combine(directory, "container.zip");
            await File.WriteAllBytesAsync(source, [0x50, 0x4B]);
            await File.WriteAllBytesAsync(Path.Combine(destination, "Item.iff"), [0xFF]);
            IffContainerEntry[] entries =
            [
                new("nested/Item.iff", 3,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream([1, 2, 3], writable: false))),
                new(@"other\Character.iff", 2,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream([4, 5], writable: false)))
            ];

            int count = await FrmIFFManager.ExtractAllEntriesAsync(entries, destination, source);

            Assert.Equal(2, count);
            Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(destination, "Item.iff")));
            Assert.Equal([4, 5], await File.ReadAllBytesAsync(Path.Combine(destination, "Character.iff")));
            Assert.False(Directory.Exists(Path.Combine(destination, "nested")));
            Assert.Empty(Directory.EnumerateFiles(destination, ".*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAllEntriesAsync_PreflightsCollisionsAndSourceConflicts()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-extract-all-{Guid.NewGuid():N}");
        string destination = Path.Combine(directory, "output");
        Directory.CreateDirectory(destination);
        try
        {
            string source = Path.Combine(directory, "container.iff");
            byte[] sourceBytes = [7, 8, 9];
            await File.WriteAllBytesAsync(source, sourceBytes);
            IffContainerEntry[] collisions =
            [
                new("one/Same.iff", 1,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream([1], writable: false))),
                new("two/same.IFF", 1,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream([2], writable: false)))
            ];

            InvalidDataException collision = await Assert.ThrowsAsync<InvalidDataException>(() =>
                FrmIFFManager.ExtractAllEntriesAsync(collisions, destination, source));
            Assert.Contains("Same.iff", collision.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.EnumerateFiles(destination));

            var conflict = new IffContainerEntry("nested/container.iff", 1,
                _ => ValueTask.FromResult<Stream>(new MemoryStream([3], writable: false)));
            IOException sourceConflict = await Assert.ThrowsAsync<IOException>(() =>
                FrmIFFManager.ExtractAllEntriesAsync([conflict], directory, source));
            Assert.Equal(Strings.IFFManager_ExtractSourceConflict, sourceConflict.Message);
            Assert.Equal(sourceBytes, await File.ReadAllBytesAsync(source));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAllEntriesAsync_CancellationKeepsCompletedFilesAndPreservesCurrentDestination()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"iff-extract-all-{Guid.NewGuid():N}");
        string destination = Path.Combine(directory, "output");
        Directory.CreateDirectory(destination);
        try
        {
            string source = Path.Combine(directory, "container.zip");
            await File.WriteAllBytesAsync(source, [0x50, 0x4B]);
            string secondDestination = Path.Combine(destination, "Second.iff");
            byte[] existingSecond = [9, 8, 7];
            await File.WriteAllBytesAsync(secondDestination, existingSecond);
            using var cancellation = new CancellationTokenSource();
            IffContainerEntry[] entries =
            [
                new("First.iff", 2,
                    _ => ValueTask.FromResult<Stream>(new MemoryStream([1, 2], writable: false))),
                new("Second.iff", 2, _ =>
                {
                    cancellation.Cancel();
                    return ValueTask.FromResult<Stream>(new MemoryStream([3, 4], writable: false));
                })
            ];

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                FrmIFFManager.ExtractAllEntriesAsync(entries, destination, source, cancellation.Token));

            Assert.Equal([1, 2], await File.ReadAllBytesAsync(Path.Combine(destination, "First.iff")));
            Assert.Equal(existingSecond, await File.ReadAllBytesAsync(secondDestination));
            Assert.Empty(Directory.EnumerateFiles(destination, ".*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IffPatchCandidate Candidate(uint id, int index) =>
        new(id, index, $"Target {id}", $"Source {id}", new byte[8],
            [new IffPatchFieldResult("Value", true, null, false)]);

    private static T Field<T>(object instance, string name) where T : class =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static string Resource(CultureInfo culture, string key)
    {
        var set = Strings.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return (string)set.GetObject(key)!;
    }

    private static string[] Placeholders(string value) =>
        Regex.Matches(value, @"\{\d+(?::[^}]*)?\}").Select(match => match.Value).ToArray();

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
