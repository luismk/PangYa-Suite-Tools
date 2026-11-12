using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.UI;

namespace PangYa_Suite_Tools;

internal sealed class PakFilePreviewData : IDisposable
{
    private const int TextLimit = 2 * 1024 * 1024;
    private const int GraphicsLimit = 64 * 1024 * 1024;
    private const long TexturePixelBudget = 16 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly string[] TextExtensions =
        [".txt", ".xml", ".log", ".ini", ".cfg", ".csv", ".tsv", ".json", ".yaml", ".yml", ".md", ".html", ".css", ".lua", ".sql", ".nut"];
    private static readonly string[] ImageExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".tga"];

    public byte[]? TextBytes { get; private init; }
    public string? Text { get; private init; }
    public Bitmap? Image { get; private init; }
    public Model3DGroup? Model { get; private init; }
    public bool MissingTextures { get; private init; }
    public string[]? Sections { get; private init; }
    public int LegacyCodePage { get; private init; }

    static PakFilePreviewData() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static PakFilePreviewData Load(PakReader reader, PakFileEntry entry, CancellationToken token)
    {
        string extension = Path.GetExtension(entry.Name).ToLowerInvariant();
        bool image = ImageExtensions.Contains(extension);
        bool puppet = extension is ".mpet" or ".pet" or ".apet" or ".bpet";
        byte[] bytes = ReadEntry(reader, entry, image || puppet ? GraphicsLimit : TextLimit, token);
        if (image)
            return new PakFilePreviewData { Image = DecodeImage(bytes, extension, TexturePixelBudget) };
        if (puppet)
        {
            var parsed = PakPuppetPreview.Parse(bytes, extension == ".mpet", reader.FileNameEncoding, token);
            if (parsed.Materials.Length == 0)
                return new PakFilePreviewData { Sections = parsed.Sections };
            var (model, missing) = BuildModel(reader, entry, parsed, token);
            return new PakFilePreviewData { Model = model, MissingTextures = missing };
        }

        int legacy = reader.FileNameEncoding.CodePage is 932 or 949 ? reader.FileNameEncoding.CodePage : 1252;
        string decoded = DecodeText(bytes, null, legacy);
        if (decoded.Contains('\0') ||
            (!TextExtensions.Contains(extension) && decoded.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t' or '\f'))))
            throw new InvalidDataException();
        return new PakFilePreviewData { TextBytes = bytes, Text = decoded, LegacyCodePage = legacy };
    }

    public static string DecodeText(byte[] bytes, int? codePage, int legacyCodePage)
    {
        if (codePage.HasValue)
            return Encoding.GetEncoding(codePage.Value).GetString(bytes).TrimStart('\uFEFF');

        Encoding[] bomEncodings = [new UTF32Encoding(true, true), Encoding.UTF32, Encoding.BigEndianUnicode,
            Encoding.Unicode, Encoding.UTF8];
        foreach (Encoding encoding in bomEncodings)
        {
            if (bytes.AsSpan().StartsWith(encoding.Preamble))
                return encoding.GetString(bytes.AsSpan(encoding.Preamble.Length));
        }
        try { return StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.GetEncoding(legacyCodePage).GetString(bytes); }
    }

    private static byte[] ReadEntry(PakReader reader, PakFileEntry entry, int limit, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (entry.Size > limit || entry.CompressSize > GraphicsLimit)
            throw new InvalidDataException(Strings.PakFile_PreviewLimit);
        byte[]? bytes = reader.ExtractEntryToBytes(entry);
        if (bytes == null || bytes.Length != entry.Size)
            throw new InvalidDataException();
        token.ThrowIfCancellationRequested();
        return bytes;
    }

    private static Bitmap DecodeImage(byte[] bytes, string extension, long pixelLimit)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        if (extension.Equals(".tga", StringComparison.OrdinalIgnoreCase))
        {
            if (bytes.Length < 18)
                throw new InvalidDataException();
            int width = bytes[12] | (bytes[13] << 8);
            int height = bytes[14] | (bytes[15] << 8);
            ValidateImageSize(width, height, pixelLimit);
            return PangyaImageLoader.LoadTga(stream);
        }

        using System.Drawing.Image source = System.Drawing.Image.FromStream(stream, false, false);
        ValidateImageSize(source.Width, source.Height, pixelLimit);
        return new Bitmap(source);
    }

    private static void ValidateImageSize(int width, int height, long pixelLimit)
    {
        if (width <= 0 || height <= 0 || width > 8192 || height > 8192 || (long)width * height > pixelLimit)
            throw new InvalidDataException(Strings.PakFile_PreviewLimit);
    }

    private static (Model3DGroup Model, bool MissingTextures) BuildModel(PakReader reader, PakFileEntry entry,
        PakPuppetPreview puppet, CancellationToken token)
    {
        var files = reader.Entries.Where(candidate => candidate.Type != PakFileEntryType.Directory).ToArray();
        var byPath = files.ToLookup(candidate => candidate.Name.Replace('\\', '/').TrimStart('/'), StringComparer.OrdinalIgnoreCase);
        var byName = files.ToLookup(candidate => Path.GetFileName(candidate.Name.Replace('\\', '/')), StringComparer.OrdinalIgnoreCase);
        string modelPath = entry.Name.Replace('\\', '/').TrimStart('/');
        string folder = modelPath[..(modelPath.LastIndexOf('/') + 1)];
        var model = new Model3DGroup();
        long remainingPixels = TexturePixelBudget;
        bool missingTextures = false;

        foreach (var group in Enumerable.Range(0, puppet.Materials.Length).GroupBy(index => puppet.Materials[index]))
        {
            token.ThrowIfCancellationRequested();
            System.Windows.Media.Brush brush = System.Windows.Media.Brushes.LightGray;
            if (group.Key != byte.MaxValue)
            {
                string textureName = group.Key < puppet.Textures.Length
                    ? puppet.Textures[group.Key].Replace('\\', '/').TrimStart('/')
                    : string.Empty;
                var nearby = byPath[folder + textureName].Take(2).ToArray();
                var matches = nearby.Length == 0
                    ? byName[Path.GetFileName(textureName)].Take(2).ToArray()
                    : nearby;
                if (matches.Length == 1 && remainingPixels > 0)
                {
                    PakFileEntry texture = matches[0];
                    string extension = Path.GetExtension(texture.Name).ToLowerInvariant();
                    try
                    {
                        if (ImageExtensions.Contains(extension))
                        {
                            using Bitmap bitmap = DecodeImage(ReadEntry(reader, texture, GraphicsLimit, token),
                                extension, remainingPixels);
                            using var stream = new MemoryStream();
                            bitmap.Save(stream, ImageFormat.Png);
                            stream.Position = 0;
                            var image = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                            brush = new ImageBrush(image)
                            {
                                ViewportUnits = BrushMappingMode.Absolute,
                                Viewport = new System.Windows.Rect(0, 0, 1, 1),
                                TileMode = TileMode.Tile
                            };
                            remainingPixels -= (long)bitmap.Width * bitmap.Height;
                        }
                    }
                    catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or
                        NotSupportedException or System.Runtime.InteropServices.ExternalException) { }
                }
                missingTextures |= brush is not ImageBrush;
            }

            var mesh = new MeshGeometry3D();
            foreach (int face in group)
            {
                token.ThrowIfCancellationRequested();
                for (int index = face * 3; index < face * 3 + 3; index++)
                {
                    var corner = puppet.Corners[index];
                    mesh.TriangleIndices.Add(mesh.Positions.Count);
                    mesh.Positions.Add(puppet.Positions[corner.Vertex]);
                    mesh.Normals.Add(corner.Normal);
                    mesh.TextureCoordinates.Add(corner.UV);
                }
            }
            var material = new DiffuseMaterial(brush);
            model.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
        }
        model.Freeze();
        return (model, missingTextures);
    }

    public void Dispose() => Image?.Dispose();
}
