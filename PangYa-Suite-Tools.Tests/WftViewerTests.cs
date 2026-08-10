using PangyaAPI.WFT;
using PangYa_Suite_Tools.Localization;
using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

[Collection("Localization")]
public sealed class WftViewerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(),
        "PangYaWftViewerTests", Guid.NewGuid().ToString("N"));

    public WftViewerTests()
    {
        Directory.CreateDirectory(_directory);
        LocalizationManager.PreferencePathOverride =
            Path.Combine(_directory, "culture.txt");
    }

    [Fact]
    public void GlyphRenderer_PreservesCoverageAndRendersSampleText()
    {
        string path = CreateFont((0x0020, [0, 0], 1), (0x0041, [0x80, 0x40], 2));
        using WftFont font = WftFontReader.Open(path);
        WftGlyph glyph = font.ReadGlyph(0x0041);

        using Bitmap bitmap = WftGlyphRenderer.CreateBitmap(glyph, Color.White);
        Assert.Equal(255, bitmap.GetPixel(0, 0).A);
        Assert.Equal(0, bitmap.GetPixel(1, 0).A);
        Assert.Equal(255, bitmap.GetPixel(1, 1).A);

        using Bitmap sample = WftGlyphRenderer.RenderText(font, "A\tA\nA", 2,
            Color.White, Color.Black, CancellationToken.None);
        Assert.Equal(Color.White.ToArgb(), sample.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.White.ToArgb(), sample.GetPixel(0, 4).ToArgb());
        Assert.True(sample.Width >= 10);
        Assert.Equal(8, sample.Height);
    }

    [Fact]
    public void Viewer_LoadsFontSelectsDefaultGlyphAndReleasesFile()
    {
        string path = CreateFont((0x0020, [0, 0], 1), (0x0041, [0x80, 0x40], 2));

        RunSta(() =>
        {
            using (var viewer = new FrmWftViewer())
            {
                Task load = viewer.LoadFileAsync(path);
                while (!load.IsCompleted)
                {
                    Application.DoEvents();
                    Thread.Sleep(1);
                }
                load.GetAwaiter().GetResult();
                Assert.NotNull(viewer.LoadedFont);
                Assert.Equal((ushort)0x0041, viewer.SelectedCodePoint);
                Assert.Contains(Path.GetFileName(path), viewer.Text);
            }
            string renamed = path + ".renamed";
            File.Move(path, renamed);
            Assert.True(File.Exists(renamed));
        });
    }

    [Fact]
    public void Viewer_DisposeIsIdempotentWithPendingSampleRender()
    {
        string path = CreateFont((0x0020, [0, 0], 1), (0x0041, [0x80, 0x40], 2));

        RunSta(() =>
        {
            var viewer = new FrmWftViewer();
            Task load = viewer.LoadFileAsync(path);
            while (!load.IsCompleted)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            load.GetAwaiter().GetResult();

            viewer.Dispose();
            viewer.Dispose();
        });
    }

    [Fact]
    public void Viewer_ExportsInstallableTrueTypeFontAndReleasesDestination()
    {
        string path = CreateFont((0x0020, [0, 0], 1), (0x0041, [0x80, 0x40], 2));
        string output = Path.Combine(_directory, "TestPixels.ttf");

        RunSta(() =>
        {
            using var viewer = new FrmWftViewer();
            Wait(viewer.LoadFileAsync(path));
            Assert.True(viewer.ExportEnabled);

            Wait(viewer.ExportFileAsync(output,
                new WftTrueTypeExportOptions("PangYa Test Pixels")));

            Assert.True(File.Exists(output));
            Assert.Contains(Path.GetFileName(output), viewer.StatusText);
            using var fonts = new PrivateFontCollection();
            fonts.AddFontFile(output);
            Assert.Contains(fonts.Families,
                family => family.Name.Equals("PangYa Test Pixels", StringComparison.Ordinal));
            Assert.True(AddFontResourceEx(output, 0x10, 0) > 0);
            Assert.True(RemoveFontResourceEx(output, 0x10, 0));
        });

        string renamed = output + ".renamed";
        File.Move(output, renamed);
        Assert.True(File.Exists(renamed));
    }

    [Fact]
    public void ExportDialog_DefaultsFamilyAndRegularStyle()
    {
        RunSta(() =>
        {
            using var dialog = new WftExportOptionsDialog("pangya_font");
            var family = dialog.Controls.Find("txtWftExportFamily", true).OfType<TextBox>().Single();
            var style = dialog.Controls.Find("cboWftExportStyle", true).OfType<ComboBox>().Single();
            Assert.Equal("pangya_font", family.Text);
            Assert.Equal(0, style.SelectedIndex);
            Assert.Equal("Regular", style.Text);
        });
    }

    [Fact]
    public void ViewerAndMenu_RefreshAllSupportedCultures()
    {
        RunSta(() =>
        {
            using var viewer = new FrmWftViewer();
            using var menu = new FrmMenu();
            foreach (string culture in new[]
                     {
                         LocalizationManager.English,
                         LocalizationManager.PortugueseBrazil,
                         LocalizationManager.Swedish,
                         LocalizationManager.Japonese,
                         LocalizationManager.French
                     })
            {
                LocalizationManager.SetCulture(culture);
                Assert.Equal(Strings.WftViewer_Title, viewer.Text);
                Assert.Equal(Strings.Menu_FontViewer,
                    menu.Controls.Find("btnOpenFontViewer", true).Single().Text);
                Assert.Contains("*.wft", Strings.WftViewer_FileFilter,
                    StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(Strings.WftViewer_Export));
                Assert.Contains("*.ttf", Strings.WftViewer_ExportFileFilter,
                    StringComparison.OrdinalIgnoreCase);
            }
        });
    }

    private string CreateFont(params (ushort CodePoint, byte[] Bitmap, ushort Advance)[] glyphs)
    {
        const int cellSize = 2;
        const int bitmapBytes = 2;
        const int recordSize = bitmapBytes + sizeof(ushort);
        string path = Path.Combine(_directory, Guid.NewGuid() + ".wft");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        stream.SetLength(16L + (long)recordSize * WftFont.MaximumGlyphCount);
        Span<byte> header = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x544E4657);
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..], cellSize);
        stream.Write(header);
        Span<byte> advanceBytes = stackalloc byte[2];
        foreach ((ushort codePoint, byte[] bitmap, ushort advance) in glyphs)
        {
            stream.Position = 16L + (long)(codePoint - WftFont.FirstCodePoint) * recordSize;
            stream.Write(bitmap);
            BinaryPrimitives.WriteUInt16LittleEndian(advanceBytes, advance);
            stream.Write(advanceBytes);
        }
        return path;
    }

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

    private static void Wait(Task task)
    {
        while (!task.IsCompleted)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        task.GetAwaiter().GetResult();
    }

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int AddFontResourceEx(string fileName, uint flags, nint reserved);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveFontResourceEx(string fileName, uint flags, nint reserved);

    public void Dispose()
    {
        LocalizationManager.SetCulture(LocalizationManager.English);
        LocalizationManager.PreferencePathOverride = null;
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
