using System.Text;

namespace PangyaAPI.UI;

/// <summary>Fresh's paired Korean source and Japanese replacement strings.</summary>
public sealed class PangyaUiLanguageTable
{
    private const int MaximumTableBytes = 16 * 1024 * 1024;
    private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);

    public static PangyaUiLanguageTable Load(string koreanPath, string japanesePath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] korean = ReadTable(koreanPath);
        byte[] japanese = ReadTable(japanesePath);
        Encoding source = Encoding.GetEncoding(949);
        Encoding target = Encoding.GetEncoding(932);
        var table = new PangyaUiLanguageTable();
        int k = 0, j = 0;
        while (k < korean.Length)
            table._entries.TryAdd(ReadString(korean, ref k, source), ReadString(japanese, ref j, target));
        if (j != japanese.Length)
            throw new InvalidDataException("The Fresh language tables have different entry counts.");
        return table;
    }

    public string Translate(string text) => _entries.GetValueOrDefault(text) ?? text;

    private static byte[] ReadTable(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > MaximumTableBytes)
            throw new InvalidDataException("The Fresh language table exceeds the supported size.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static string ReadString(byte[] data, ref int offset, Encoding encoding)
    {
        int end = Array.IndexOf(data, (byte)0, offset);
        if (end < 0)
            throw new InvalidDataException("The Fresh language table contains an unterminated or missing entry.");
        string value = encoding.GetString(data, offset, end - offset);
        offset = end + 1;
        return value;
    }
}
