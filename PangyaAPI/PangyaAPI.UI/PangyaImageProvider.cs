using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PangyaAPI.UI;

public interface IPangyaImageProvider
{
    Image? GetImage(string resourceId);
    Image? GetImageByPath(string path);
    string? TryResolvePath(string resourceId);
}

public sealed class PangyaFileImageProvider : IPangyaImageProvider, IDisposable
{
    private static readonly string[] PreferredSegments =
        ["ui\\shop_myroom", "ui\\frames", "ui\\buttons"];
    private static readonly string[] FreshBitmapExtensions = [".tga", ".png", ".bmp"];
    private readonly Dictionary<string, List<string>> _files;
    private readonly Dictionary<string, Image> _images =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public PangyaFileImageProvider(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        DataRoot = Path.GetFullPath(dataRoot);
        _files = Directory.EnumerateFiles(DataRoot, "*", SearchOption.AllDirectories)
            .Where(PangyaImageLoader.IsSupportedPath)
            .Select(path => (Path: path, Name: Path.GetFileNameWithoutExtension(path)))
            .Where(file => !string.IsNullOrEmpty(file.Name))
            .GroupBy(file => file.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                group => group.Select(file => file.Path)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public string DataRoot { get; }

    public string ResolvePath(string resourceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        string? path = FindPath(resourceId, out IReadOnlyList<string>? ambiguous);
        if (path is not null) return path;
        if (ambiguous is not null)
            throw new InvalidDataException(
                $"The PangYa image resource '{resourceId}' is ambiguous: {string.Join(", ", ambiguous)}");
        throw new FileNotFoundException($"The PangYa image resource '{resourceId}' was not found.");
    }

    private string? FindPath(string resourceId, out IReadOnlyList<string>? ambiguous)
    {
        ambiguous = null;
        string filename = resourceId.Trim();
        string lookupKey = Path.GetFileNameWithoutExtension(filename);
        if (string.IsNullOrWhiteSpace(lookupKey) ||
            !_files.TryGetValue(lookupKey, out List<string>? matches) || matches.Count == 0)
            return null;
        if (matches.Count == 1) return matches[0];
        if (PangyaImageLoader.IsSupportedPath(filename))
        {
            string extension = Path.GetExtension(filename);
            List<string> exactFormat = matches.Where(path =>
                Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)).ToList();
            if (exactFormat.Count == 1) return exactFormat[0];
            if (exactFormat.Count > 1) matches = exactFormat;
        }
        foreach (string segment in PreferredSegments)
        {
            List<string> preferred = matches.Where(path =>
                path.Contains(segment, StringComparison.OrdinalIgnoreCase)).ToList();
            if (preferred.Count == 1) return preferred[0];
        }
        List<string> uiMatches = matches.Where(path =>
            Path.GetRelativePath(DataRoot, path).StartsWith(
                $"ui{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)).ToList();
        if (uiMatches.Count == 1) return uiMatches[0];
        if (uiMatches.Count > 1) matches = uiMatches;

        if (!Path.HasExtension(filename))
        {
            foreach (string extension in FreshBitmapExtensions)
            {
                List<string> formatMatches = matches.Where(path =>
                    Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)).ToList();
                if (formatMatches.Count == 0) continue;
                if (formatMatches.Count == 1) return formatMatches[0];
                matches = formatMatches;
                break;
            }
        }

        if (matches.Select(HashFile).Distinct(StringComparer.Ordinal).Count() == 1)
            return matches[0];
        ambiguous = matches;
        return null;
    }

    public string? TryResolvePath(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId)) return null;
        ObjectDisposedException.ThrowIf(_disposed, this);
        try { return FindPath(resourceId, out _); }
        catch (ArgumentException) { return null; }
    }

    public Image? GetImage(string resourceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(resourceId)) return null;
        if (_images.TryGetValue(resourceId, out Image? cached)) return cached;
        string? path = TryResolvePath(resourceId);
        if (path is null) return null;
        Image? image = PangyaImageLoader.Load(path);
        if (image is not null) _images[resourceId] = image;
        return image;
    }

    public Image? GetImageByPath(string path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        string fullPath = Path.GetFullPath(path);
        if (_images.TryGetValue(fullPath, out Image? cached)) return cached;
        Image? image = PangyaImageLoader.Load(fullPath);
        if (image is not null) _images[fullPath] = image;
        return image;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (Image image in _images.Values) image.Dispose();
        _images.Clear();
    }

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

public static class PangyaImageLoader
{
    public static IReadOnlyList<string> SupportedExtensions { get; } =
    [
        ".tga", ".png", ".jpg", ".jpeg", ".jpe", ".jfif", ".bmp", ".dib", ".rle",
        ".gif", ".tif", ".tiff", ".ico", ".wmf", ".emf", ".exif"
    ];

    public static bool IsSupportedPath(string path) => SupportedExtensions.Contains(
        Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static Image? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            if (path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                return LoadTga(path);
            using Image source = Image.FromFile(path);
            return new Bitmap(source);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   ArgumentException or InvalidDataException)
        {
            return null;
        }
    }

    public static Bitmap LoadTga(string path)
    {
        using FileStream input = File.OpenRead(path);
        Span<byte> header = stackalloc byte[18];
        input.ReadExactly(header);
        int idLength = header[0];
        int imageType = header[2];
        int width = BinaryPrimitives.ReadUInt16LittleEndian(header[12..]);
        int height = BinaryPrimitives.ReadUInt16LittleEndian(header[14..]);
        int bitsPerPixel = header[16];
        if (imageType != 2 || bitsPerPixel is not (24 or 32))
            throw new InvalidDataException(
                "Only uncompressed 24-bit and 32-bit TGA images are supported.");
        if (width <= 0 || height <= 0 || width > 8192 || height > 8192)
            throw new InvalidDataException("The TGA dimensions are invalid.");
        int bytesPerPixel = bitsPerPixel / 8;
        int sourceStride = checked(width * bytesPerPixel);
        int byteCount = checked(sourceStride * height);
        if (input.Length - 18 - idLength < byteCount)
            throw new InvalidDataException("The TGA pixel data is truncated.");
        input.Position = 18 + idLength;
        byte[] source = GC.AllocateUninitializedArray<byte>(byteCount);
        input.ReadExactly(source);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            byte[] destination =
                GC.AllocateUninitializedArray<byte>(checked(data.Stride * height));
            bool topOrigin = (header[17] & 0x20) != 0;
            bool rightOrigin = (header[17] & 0x10) != 0;
            bool hasAlpha = bytesPerPixel == 4 && (header[17] & 0x0F) != 0;
            for (int sourceY = 0; sourceY < height; sourceY++)
            {
                int destinationY = topOrigin ? sourceY : height - 1 - sourceY;
                for (int sourceX = 0; sourceX < width; sourceX++)
                {
                    int destinationX = rightOrigin ? width - 1 - sourceX : sourceX;
                    int sourceOffset =
                        sourceY * sourceStride + sourceX * bytesPerPixel;
                    int targetOffset = destinationY * data.Stride + destinationX * 4;
                    destination[targetOffset] = source[sourceOffset];
                    destination[targetOffset + 1] = source[sourceOffset + 1];
                    destination[targetOffset + 2] = source[sourceOffset + 2];
                    destination[targetOffset + 3] =
                        hasAlpha ? source[sourceOffset + 3] : byte.MaxValue;
                }
            }
            Marshal.Copy(destination, 0, data.Scan0, destination.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }
}
