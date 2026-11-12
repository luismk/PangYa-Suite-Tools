using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

[Collection("Localization")]
public sealed class PakFilePreviewTests : IDisposable
{
    [Theory]
    [InlineData(Keys.X)]
    [InlineData(Keys.Y)]
    [InlineData(Keys.Z)]
    public void LockedModelOrbitRotatesOnlyAroundSelectedAxis(Keys axis)
    {
        var orbit = new PakModelOrbit();
        Vector3D initial = orbit.Direction;
        Assert.True(orbit.ToggleAxis(axis));
        Assert.Equal(axis, orbit.LockedAxis);
        orbit.Drag(axis == Keys.X ? 40 : 0, axis == Keys.X ? 0 : 40);
        Assert.True((orbit.Direction - initial).Length < 1e-12);
        orbit.Drag(axis == Keys.X ? 0 : 40, axis == Keys.X ? 40 : 0);
        Assert.True((orbit.Direction - initial).Length > 0.1);
        Assert.Equal(Component(initial), Component(orbit.Direction), 12);
        Assert.Equal(0, Vector3D.DotProduct(orbit.Direction, orbit.Up), 12);
        Assert.True(orbit.ToggleAxis(axis));
        Assert.Equal(Keys.None, orbit.LockedAxis);
        orbit.Drag(20, 20);
        Assert.Equal(0, Vector3D.DotProduct(orbit.Direction, orbit.Up), 12);

        double Component(Vector3D vector) => axis switch { Keys.X => vector.X, Keys.Y => vector.Y, _ => vector.Z };
    }

    private readonly DirectoryInfo _temp = Directory.CreateTempSubdirectory("PangyaPakPreviewTests-");

    [Fact]
    public void TextDecodesUnicodeAndLegacyAndRejectsBinaryAndOversizedFiles()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var japanese = Encoding.GetEncoding(932);
        byte[] unicode = [.. Encoding.Unicode.Preamble, .. Encoding.Unicode.GetBytes("first\n한글")];
        Assert.Equal("first\n한글", PakFilePreviewData.DecodeText(unicode, null, 1252));
        Assert.Equal("日本語", PakFilePreviewData.DecodeText(japanese.GetBytes("日本語"), 932, 1252));
        Assert.Equal("räksmörgås", PakFilePreviewData.DecodeText(Encoding.GetEncoding(1252).GetBytes("räksmörgås"), null, 1252));

        using var reader = OpenArchive(source =>
        {
            File.WriteAllBytes(Path.Combine(source, "notes.xml"), unicode);
            File.WriteAllBytes(Path.Combine(source, "unknown"), [0, 1, 2]);
            File.WriteAllText(Path.Combine(source, "large.txt"), new string('x', 2 * 1024 * 1024 + 1));
        });
        using var preview = PakFilePreviewData.Load(reader, Find(reader, "notes.xml"), default);
        Assert.Equal("first\n한글", preview.Text);
        Assert.Throws<InvalidDataException>(() => PakFilePreviewData.Load(reader, Find(reader, "unknown"), default));
        Assert.Throws<InvalidDataException>(() => PakFilePreviewData.Load(reader, Find(reader, "large.txt"), default));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public void PuppetReadsPerCornerUvsAndFaceMaterial(bool mpet, byte minor)
    {
        var puppet = PakPuppetPreview.Parse(MakePuppet(mpet, minor), mpet, Encoding.ASCII, default);
        Assert.Equal("texture.png", Assert.Single(puppet.Textures));
        Assert.Equal(3, puppet.Positions.Length);
        Assert.Equal(3, puppet.Corners.Length);
        Assert.Equal(new System.Windows.Point(1, 0), puppet.Corners[1].UV);
        Assert.Equal(0, Assert.Single(puppet.Materials));
        Assert.Throws<InvalidDataException>(() =>
            PakPuppetPreview.Parse(MakePuppet(mpet, minor)[..^1], mpet, Encoding.ASCII, default));
    }

    [Fact]
    public void ImageAndTexturedModelLoadFromArchiveAndOpenInPreviewWindow()
    {
        using var reader = OpenArchive(source =>
        {
            using var image = new Bitmap(4, 2);
            image.SetPixel(0, 0, System.Drawing.Color.Red);
            image.Save(Path.Combine(source, "texture.png"), ImageFormat.Png);
            File.WriteAllBytes(Path.Combine(source, "model.mpet"), MakePuppet(true, 3));
            File.WriteAllBytes(Path.Combine(source, "invalid.png"), [1, 2, 3]);
            string missing = Directory.CreateDirectory(Path.Combine(source, "missing")).FullName;
            File.WriteAllBytes(Path.Combine(missing, "model.mpet"), MakePuppet(true, 3, "missing.png"));
            File.WriteAllBytes(Path.Combine(source, "motion.apet"), "ANIM\0\0\0\0"u8.ToArray());
            File.WriteAllText(Path.Combine(source, "log.txt"), "one\ntwo");
        });

        using var imagePreview = PakFilePreviewData.Load(reader, Find(reader, "texture.png"), default);
        Assert.Equal(new Size(4, 2), imagePreview.Image?.Size);
        Assert.Throws<ArgumentException>(() => PakFilePreviewData.Load(reader, Find(reader, "invalid.png"), default));
        using var modelPreview = PakFilePreviewData.Load(reader, Find(reader, "model.mpet"), default);
        var model = Assert.IsType<GeometryModel3D>(Assert.Single(modelPreview.Model!.Children));
        Assert.IsType<ImageBrush>(Assert.IsType<DiffuseMaterial>(model.Material).Brush);
        Assert.False(modelPreview.MissingTextures);
        using var missingPreview = PakFilePreviewData.Load(reader,
            Find(reader, @"missing\model.mpet"), default);
        Assert.True(missingPreview.MissingTextures);
        using var sectionPreview = PakFilePreviewData.Load(reader, Find(reader, "motion.apet"), default);
        Assert.Equal("ANIM", Assert.Single(Assert.IsType<string[]>(sectionPreview.Sections)));
        using var textPreview = PakFilePreviewData.Load(reader, Find(reader, "log.txt"), default);

        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var imageWindow = new PakFilePreviewWindow("texture.png", imagePreview);
                imageWindow.Show();
                Assert.IsType<PictureBox>(Assert.Single(imageWindow.Controls));
                using var textWindow = new PakFilePreviewWindow("log.txt", textPreview);
                textWindow.Show();
                var textBox = Assert.Single(textWindow.Controls.OfType<TextBox>());
                Assert.True(textBox.ReadOnly);
                Assert.Equal("one\r\ntwo", textBox.Text);
                using var modelWindow = new PakFilePreviewWindow("model.mpet", modelPreview);
                modelWindow.Show();
                Application.DoEvents();
                Assert.True(modelWindow.Visible);
                var host = Assert.Single(modelWindow.Controls.OfType<System.Windows.Forms.Integration.ElementHost>());
                var background = Assert.IsType<System.Windows.Controls.Border>(host.Child);
                var layers = Assert.IsType<System.Windows.Controls.Grid>(background.Child);
                var axes = Assert.Single(layers.Children.OfType<PakModelAxisIndicator>());
                Assert.False(axes.IsHitTestVisible);
                Assert.Equal(new[] { "X", "Y", "Z" },
                    axes.Children.OfType<System.Windows.Controls.TextBlock>().Select(label => label.Text));
                var lines = axes.Children.OfType<System.Windows.Shapes.Line>().ToArray();
                var up = new Vector3D(0, 1, 0);
                axes.SetCameraDirection(new Vector3D(0, 0, 1), up);
                Assert.True(lines[0].X2 > lines[0].X1);
                Assert.True(lines[1].Y2 < lines[1].Y1);
                Assert.Equal(lines[2].X1, lines[2].X2);
                axes.SetCameraDirection(new Vector3D(1, 0, 0), up);
                Assert.Equal(lines[0].X1, lines[0].X2);
                Assert.True(lines[2].X2 < lines[2].X1);

                var keyHandler = typeof(PakFilePreviewWindow).GetMethod("ProcessCmdKey",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
                var orbit = (PakModelOrbit)typeof(PakFilePreviewWindow).GetField("_orbit",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(modelWindow)!;
                Assert.Equal(true, keyHandler.Invoke(modelWindow, [new Message(), Keys.X]));
                Assert.Equal(Keys.X, orbit.LockedAxis);
                Assert.Equal(true, keyHandler.Invoke(modelWindow, [new Message { LParam = (IntPtr)(1 << 30) }, Keys.X]));
                Assert.Equal(Keys.X, orbit.LockedAxis);
                Assert.Equal(true, keyHandler.Invoke(modelWindow, [new Message(), Keys.Z]));
                Assert.Equal(Keys.Z, orbit.LockedAxis);
                Assert.Equal(true, keyHandler.Invoke(modelWindow, [new Message(), Keys.Z]));
                Assert.Equal(Keys.None, orbit.LockedAxis);
            }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    [Fact]
    public async Task WatcherCapturesAtomicAndOrdinarySavesAndDetectsChangedArchive()
    {
        using var reader = OpenArchive(source => File.WriteAllText(Path.Combine(source, "note.txt"), "initial"));
        string working = Path.Combine(Directory.CreateDirectory(Path.Combine(_temp.FullName, "working")).FullName, "note.txt");
        File.WriteAllText(working, "initial");
        var initial = SHA256.HashData(File.ReadAllBytes(working));
        using var session = new PakFileEditSession(reader, 0, working, PakFileEditSession.CaptureArchive(reader), initial);
        Assert.True(session.CanCleanWorkingCopy());

        string editorTemp = Path.Combine(session.DirectoryPath, "editor.tmp");
        File.WriteAllText(editorTemp, "second");
        File.Move(editorTemp, working, overwrite: true);
        await WaitForSaveAsync(session);
        var revision = session.CaptureRevision();
        Assert.Equal("second", File.ReadAllText(revision.Path));
        Assert.True(session.Observe(revision));
        Assert.False(session.Observe(revision));
        Assert.False(session.CanCleanWorkingCopy());

        using (var stream = new FileStream(working, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write("third"u8);
            Assert.ThrowsAny<IOException>(() => session.CaptureRevision());
        }
        await WaitForSaveAsync(session);
        var next = session.CaptureRevision();
        Assert.Equal("third", File.ReadAllText(next.Path));
        Assert.Equal("second", File.ReadAllText(revision.Path));
        Assert.True(session.Observe(next));
        session.AppliedHash = next.Hash;
        Assert.True(session.CanCleanWorkingCopy());
        Assert.True(session.MatchesArchive());

        File.SetLastWriteTimeUtc(reader.ArchivePath, DateTime.UtcNow.AddMinutes(-5));
        Assert.False(session.MatchesArchive());
    }

    private static async Task WaitForSaveAsync(PakFileEditSession session)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!session.IsReady)
            await Task.Delay(50, timeout.Token);
    }

    private PakReader OpenArchive(Action<string> create)
    {
        string source = Directory.CreateDirectory(Path.Combine(_temp.FullName, "source")).FullName;
        create(source);
        string path = Path.Combine(_temp.FullName, "preview.pak");
        new PakWriter { EntryVersion = PakFileEntryVersion.V3, EntryType = PakFileEntryType.Raw }
            .CreateFromDirectoryContents(source, path);
        var reader = new PakReader(path);
        reader.Parse();
        return reader;
    }

    private static PakFileEntry Find(PakReader reader, string name) =>
        Assert.Single(reader.Entries, entry => entry.Name == name);

    private static byte[] MakePuppet(bool mpet, byte minor, string textureName = "texture.png")
    {
        using var file = new MemoryStream();
        using var output = new BinaryWriter(file);
        WriteSection("VERS", writer =>
        {
            writer.Write(minor);
            writer.Write((byte)1);
            writer.Write((ushort)0xFFFE);
        });
        WriteSection("TEXT", writer =>
        {
            writer.Write(1u);
            byte[] name = Encoding.ASCII.GetBytes(textureName);
            writer.Write(name);
            writer.Write(new byte[44 - name.Length]);
        });
        WriteSection("MESH", writer =>
        {
            if (mpet)
            {
                writer.Write((byte)1);
                writer.Write(new byte[20]);
            }
            writer.Write(3u);
            for (int i = 0; i < 3; i++)
            {
                writer.Write(i == 1 ? 1f : 0f);
                writer.Write(i == 2 ? 1f : 0f);
                writer.Write(0f);
                if (mpet)
                    writer.Write(1f);
                writer.Write(new byte[] { 255, 0, 0, 0 });
            }
            writer.Write(1u);
            for (int i = 0; i < 3; i++)
            {
                writer.Write((uint)i);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);
                if (minor >= 2)
                    writer.Write((byte)1);
                writer.Write(i == 1 ? 1f : 0f);
                writer.Write(i == 2 ? 1f : 0f);
            }
            writer.Write((byte)0);
            if (minor >= 2)
                writer.Write(byte.MaxValue);
        });
        return file.ToArray();

        void WriteSection(string name, Action<BinaryWriter> write)
        {
            using var payload = new MemoryStream();
            using var writer = new BinaryWriter(payload);
            write(writer);
            output.Write(Encoding.ASCII.GetBytes(name));
            output.Write((uint)payload.Length);
            output.Write(payload.ToArray());
        }
    }

    public void Dispose() => _temp.Delete(recursive: true);
}
