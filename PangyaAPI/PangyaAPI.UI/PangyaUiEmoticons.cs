using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml;

namespace PangyaAPI.UI;

public sealed class PangyaUiEmoticons
{
    private const int CellWidth = 26;
    private const int CellHeight = 24;
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private int _framesPerSecond = 10;

    public static PangyaUiEmoticons Load(string path)
    {
        var catalog = new PangyaUiEmoticons();
        if (!File.Exists(path)) return catalog;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = new StreamReader(path, PangyaXmlCompatibility.DetectEncoding(path));
        using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1024 * 1024
        });
        var document = new XmlDocument { XmlResolver = null };
        document.Load(reader);
        if (document.DocumentElement is not XmlElement root || root.Name != "emoticon") return catalog;
        if (int.TryParse(root.GetAttribute("fps"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int fps) && fps is > 0 and <= 1000)
            catalog._framesPerSecond = fps;
        int index = 0;
        foreach (XmlElement definition in root.ChildNodes.OfType<XmlElement>().Where(node => node.Name == "def"))
        {
            XmlElement? animation = definition["anim"];
            int[] frames = (animation?.GetAttribute("frame") ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int frame) ? frame : 0).ToArray();
            var entry = new Entry(index++, animation?.GetAttribute("res") ?? string.Empty, frames);
            foreach (string attribute in new[] { "name", "alias", "alias2" })
            {
                string name = definition.GetAttribute(attribute);
                if (name.Length > 0) catalog._entries.TryAdd(name, entry);
            }
        }
        return catalog;
    }

    public bool TryGetFrame(string name, TimeSpan? animationElapsed, out string resource, out Rectangle source)
    {
        resource = string.Empty;
        source = Rectangle.Empty;
        _entries.TryGetValue(name, out Entry? entry);
        if (name.StartsWith('-') && _entries.TryGetValue(name[1..], out Entry? shorthand) &&
            (entry is null || shorthand.Index < entry.Index))
            entry = shorthand;
        if (entry is null) return false;
        int index = entry.Index;
        resource = "emoticon.tga";
        if (animationElapsed is TimeSpan elapsed && entry.Frames.Length > 0 && entry.Resource.Length > 0)
        {
            uint elapsedMilliseconds = unchecked((uint)(elapsed.Ticks / TimeSpan.TicksPerMillisecond));
            uint frame = elapsedMilliseconds / (uint)(1000 / _framesPerSecond);
            index = entry.Frames[(int)(frame % entry.Frames.Length)];
            resource = entry.Resource;
        }
        if (index < 0) return false;
        source = new Rectangle(index % 9 * CellWidth, index % 90 / 9 * CellHeight, CellWidth, CellHeight);
        return true;
    }

    private sealed record Entry(int Index, string Resource, int[] Frames);
}
