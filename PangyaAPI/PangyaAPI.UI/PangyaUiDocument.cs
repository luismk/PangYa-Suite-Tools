using System.Globalization;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace PangyaAPI.UI;

public enum PangyaUiButtonState
{
    Normal,
    Hover,
    Selected
}

public static class PangyaUiDimensionHelper
{
    public static Point? ParsePoint(string? text)
    {
        int[]? values = ParseNumbers(text, 2);
        return values is null ? null : new Point(values[0], values[1]);
    }

    public static Size? ParseSize(string? text)
    {
        int[]? values = ParseNumbers(text, 2);
        return values is null ? null : new Size(values[0], values[1]);
    }

    public static Rectangle? ParseRectangle(string? text)
    {
        Rectangle? rectangle = ParseWindowRectangle(text);
        return rectangle is { Width: >= 0, Height: >= 0 } ? rectangle : null;
    }

    internal static Rectangle? ParseWindowRectangle(string? text)
    {
        int[]? values = ParseNumbers(text, 4);
        if (values is null) return null;
        long width = (long)values[2] - values[0];
        long height = (long)values[3] - values[1];
        if (width is < int.MinValue or > int.MaxValue || height is < int.MinValue or > int.MaxValue)
            return null;
        // Fresh preserves the origin even when legacy XML has reversed edges.
        // Unstretched images still draw at that origin using their bitmap size.
        return new Rectangle(values[0], values[1], (int)width, (int)height);
    }

    private static int[]? ParseNumbers(string? text, int count)
    {
        MatchCollection matches = Regex.Matches(text ?? string.Empty, @"-?\d+");
        if (matches.Count != count) return null;

        var values = new int[count];
        for (int index = 0; index < count; index++)
        {
            if (!int.TryParse(matches[index].Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out values[index]))
                return null;
        }
        return values;
    }
}

public sealed class PangyaUiNode
{
    private static readonly string[] ImageParameterNames =
        ["normal", "over", "selected", "below_over", "below_selected", "bgimg", "resource", "image", "src"];

    public PangyaUiNode(XmlElement element, PangyaUiNode? parent,
        IReadOnlySet<string>? requiredSymbols = null, bool isPreviewOnly = false)
    {
        Element = element;
        Parent = parent;
        RequiredSymbols = requiredSymbols is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(requiredSymbols, StringComparer.OrdinalIgnoreCase);
        IsPreviewOnly = isPreviewOnly;
        Refresh();
    }

    public XmlElement Element { get; }
    public PangyaUiNode? Parent { get; }
    public List<PangyaUiNode> Children { get; } = [];
    public string Type { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Rectangle Bounds { get; private set; }
    public IReadOnlySet<string> RequiredSymbols { get; }
    public bool IsPreviewOnly { get; }
    public bool IsEditable => !IsPreviewOnly;
    public IReadOnlyDictionary<string, string> Parameters { get; private set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    internal bool IsLayout => Type.Equals("LAYOUT", StringComparison.OrdinalIgnoreCase);
    internal XmlElement? LayoutBase => IsLayout ? Element["base"] : null;
    internal string StaticCaption => Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase)
        ? Attribute("caption") : string.Empty;
    internal string TextButtonCaption => Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase)
        ? Attribute("caption") : string.Empty;
    internal string ContextMenuCaption => Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase)
        ? Attribute("caption") : string.Empty;
    public bool IsForm => IsLayout || Type.Equals("FORM", StringComparison.OrdinalIgnoreCase) ||
                          Element.LocalName.Equals("form", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? string.IsNullOrWhiteSpace(Type) ? Element.LocalName : Type
        : string.IsNullOrWhiteSpace(Type) ? Name : $"{Name} [{Type}]";

    public PangyaUiNode? FindContainingForm()
    {
        PangyaUiNode? form = null;
        for (PangyaUiNode? node = this; node is not null; node = node.Parent)
        {
            if (node.IsLayout) return node;
            if (node.IsForm) form ??= node;
        }
        return form;
    }

    public bool IsWithin(PangyaUiNode ancestor)
    {
        for (PangyaUiNode? node = this; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }

    public bool IsVisible(IReadOnlySet<string> enabledSymbols) =>
        RequiredSymbols.All(enabledSymbols.Contains);

    public bool IsRenderVisible =>
        !Parameters.TryGetValue("visible", out string? value) ||
        (Type.Equals("AREA", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int visible) && visible == 1
            : !value.Trim().Equals("0", StringComparison.OrdinalIgnoreCase));

    public bool IsExplicitlyStretched =>
        Parameters.TryGetValue("stretch", out string? value) &&
        (Type.Equals("AREA", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stretch) && stretch != 0
            : value.Trim().Equals("1", StringComparison.OrdinalIgnoreCase));

    public void SetName(string value)
    {
        EnsureEditable();
        Element.SetAttribute("name", value.Trim());
        Refresh();
    }

    public void SetType(string value)
    {
        EnsureEditable();
        Element.SetAttribute("type", value.Trim());
        Refresh();
    }

    public void SetBounds(Rectangle bounds)
    {
        EnsureEditable();
        if (LayoutBase is XmlElement layoutBase)
        {
            layoutBase.SetAttribute("width", Math.Max(0, bounds.Width).ToString(CultureInfo.InvariantCulture));
            layoutBase.SetAttribute("height", Math.Max(0, bounds.Height).ToString(CultureInfo.InvariantCulture));
        }
        else if (HasAttribute("rect") || HasAttribute("bounds"))
        {
            string attribute = HasAttribute("rect") ? "rect" : "bounds";
            long right = (long)bounds.X + bounds.Width;
            long bottom = (long)bounds.Y + bounds.Height;
            if (right is < int.MinValue or > int.MaxValue || bottom is < int.MinValue or > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bounds), "Rectangle edges exceed the supported coordinate range.");
            Element.SetAttribute(attribute,
                $"{bounds.Left} {bounds.Top} {right} {bottom}");
            if (Element.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase) &&
                attribute == "rect")
            {
                foreach (XmlAttribute position in Element.Attributes.OfType<XmlAttribute>()
                             .Where(value => value.LocalName.Equals("pos", StringComparison.OrdinalIgnoreCase)).ToArray())
                    Element.Attributes.Remove(position);
            }
        }
        else
        {
            Element.SetAttribute("pos", $"{bounds.X} {bounds.Y}");
            Element.SetAttribute("size", $"{Math.Max(0, bounds.Width)} {Math.Max(0, bounds.Height)}");
        }
        Refresh();
    }

    public string GetResource(PangyaUiButtonState state)
    {
        if (Type.Equals("VIEWER", StringComparison.OrdinalIgnoreCase))
            return Parameters.GetValueOrDefault("image") ?? string.Empty;
        if (UsesDefinitionResource)
            return Parameters.GetValueOrDefault("resource") ?? string.Empty;
        if (Type.Equals("AREA", StringComparison.OrdinalIgnoreCase))
            state = PangyaUiButtonState.Normal;

        string[] candidates = state switch
        {
            PangyaUiButtonState.Hover => ["over", "below_over", "normal", "bgimg", "resource", "image", "src"],
            PangyaUiButtonState.Selected => ["selected", "below_selected", "normal", "bgimg", "resource", "image", "src"],
            _ => Type.Equals("AREA", StringComparison.OrdinalIgnoreCase)
                ? ["bgimg", "normal", "resource", "image", "src"]
                : ["normal", "bgimg", "resource", "image", "src"]
        };
        foreach (string candidate in candidates)
            if (Parameters.TryGetValue(candidate, out string? value) && !string.IsNullOrWhiteSpace(value))
                return value;
        return string.Empty;
    }

    public void SetResource(string value)
    {
        EnsureEditable();
        string parameterName = Type.Equals("VIEWER", StringComparison.OrdinalIgnoreCase) ? "image" :
            UsesDefinitionResource ? "resource" :
            ImageParameterNames.FirstOrDefault(Parameters.ContainsKey) ??
            (Type.Equals("AREA", StringComparison.OrdinalIgnoreCase) ? "bgimg" : "normal");
        XmlElement? parameter = Element.ChildNodes.OfType<XmlElement>()
            .LastOrDefault(child =>
                child.LocalName.Equals("param", StringComparison.OrdinalIgnoreCase) &&
                child.GetAttribute("name").Equals(parameterName, StringComparison.OrdinalIgnoreCase));
        if (parameter is null)
        {
            parameter = Element.OwnerDocument!.CreateElement("param");
            parameter.SetAttribute("name", parameterName);
            Element.AppendChild(parameter);
        }
        parameter.SetAttribute("var", value.Trim());
        Refresh();
    }

    private bool UsesDefinitionResource =>
        Type.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase) ||
        Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) ||
        Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase);

    private void EnsureEditable()
    {
        if (IsPreviewOnly)
            throw new InvalidOperationException("Conditional preview elements are read-only.");
    }

    private void Refresh()
    {
        Type = Attribute("type");
        Name = Attribute("name");
        if (string.IsNullOrWhiteSpace(Name)) Name = Attribute("id");
        Bounds = ParseBounds(Element);
        if (LayoutBase is XmlElement layoutBase)
            Bounds = ParseBounds(layoutBase);
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (XmlAttribute attribute in Element.Attributes)
        {
            if (ImageParameterNames.Contains(attribute.LocalName, StringComparer.OrdinalIgnoreCase))
                parameters[attribute.LocalName] = attribute.Value;
        }
        foreach (XmlElement child in Element.ChildNodes.OfType<XmlElement>()
                     .Where(child => child.LocalName.Equals("param", StringComparison.OrdinalIgnoreCase)))
        {
            string name = child.GetAttribute("name");
            if (!string.IsNullOrWhiteSpace(name)) parameters[name] = child.GetAttribute("var");
        }
        Parameters = parameters;
    }

    private bool HasAttribute(string name) => Element.Attributes.OfType<XmlAttribute>()
        .Any(attribute => attribute.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));

    private string Attribute(string name) => Element.Attributes.OfType<XmlAttribute>()
        .FirstOrDefault(attribute => attribute.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?.Value ?? string.Empty;

    public static Rectangle ParseBounds(XmlElement element)
    {
        string rect = FirstAttribute(element, "rect", "bounds");
        string pos = FirstAttribute(element, "pos");
        if (element.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase) &&
            FirstAttribute(element, "rect").Length > 0 &&
            PangyaUiDimensionHelper.ParsePoint(pos) is Point position &&
            position.X <= int.MaxValue - 10 && position.Y <= int.MaxValue - 10)
            return new Rectangle(position, new Size(10, 10));
        if (PangyaUiDimensionHelper.ParseWindowRectangle(rect) is Rectangle rectangle)
            return rectangle;

        Point? point = PangyaUiDimensionHelper.ParsePoint(
            pos.Length > 0 ? pos : FirstAttribute(element, "position"));
        string size = FirstAttribute(element, "size");
        Size? dimensions = PangyaUiDimensionHelper.ParseSize(size);
        if (dimensions is null)
        {
            string width = FirstAttribute(element, "width", "w", "cx");
            string height = FirstAttribute(element, "height", "h", "cy");
            if (int.TryParse(width, out int parsedWidth) && int.TryParse(height, out int parsedHeight))
                dimensions = new Size(parsedWidth, parsedHeight);
        }
        // Fresh in JP 983 gives position-only items a 10 by 10 rectangle.
        if (dimensions is null && pos.Length > 0 && point is Point origin &&
            !FirstAttribute(element, "type").Equals("MACROITEM", StringComparison.OrdinalIgnoreCase) &&
            origin.X <= int.MaxValue - 10 && origin.Y <= int.MaxValue - 10)
            dimensions = new Size(10, 10);

        return new Rectangle(point ?? Point.Empty,
            new Size(Math.Max(0, dimensions?.Width ?? 0), Math.Max(0, dimensions?.Height ?? 0)));
    }

    private static string FirstAttribute(XmlElement element, params string[] names)
    {
        foreach (string name in names)
        {
            string value = element.Attributes.OfType<XmlAttribute>()
                .FirstOrDefault(attribute => attribute.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return string.Empty;
    }

}

public sealed class PangyaUiDocument
{
    private const int MaximumXmlBytes = 16 * 1024 * 1024;
    private const int MaximumNodes = 10_000;
    private readonly XmlDocument _document;
    private readonly Encoding _encoding;

    private PangyaUiDocument(string path, XmlDocument document, Encoding encoding,
        IReadOnlyList<PangyaUiNode> roots, IReadOnlyList<PangyaUiNode> nodes, Size canvasSize,
        IReadOnlySet<string> conditionalSymbols, IReadOnlyList<string> warnings)
    {
        Path = path;
        _document = document;
        _encoding = encoding;
        Roots = roots;
        Nodes = nodes;
        CanvasSize = canvasSize;
        ConditionalSymbols = conditionalSymbols;
        Warnings = warnings;
    }

    public string Path { get; }
    public IReadOnlyList<PangyaUiNode> Roots { get; }
    public IReadOnlyList<PangyaUiNode> Nodes { get; }
    public Size CanvasSize { get; }
    public IReadOnlySet<string> ConditionalSymbols { get; }
    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<PangyaUiNode> GetVisibleRoots(IReadOnlySet<string>? enabledSymbols = null)
    {
        IReadOnlySet<string> enabled = enabledSymbols ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Roots.Where(node => node.IsVisible(enabled)).ToArray();
    }

    public bool IsVisible(PangyaUiNode node, IReadOnlySet<string>? enabledSymbols = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        IReadOnlySet<string> enabled = enabledSymbols ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Nodes.Contains(node) && node.IsVisible(enabled);
    }

    public IReadOnlyList<PangyaUiNode> GetNodesForForm(PangyaUiNode form,
        IReadOnlySet<string>? enabledSymbols = null)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!form.IsForm || !Nodes.Contains(form))
            throw new ArgumentException("The selected node is not a form in this document.", nameof(form));
        IReadOnlySet<string> enabled = enabledSymbols ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Nodes.Where(node => node.IsWithin(form) && node.IsVisible(enabled)).ToArray();
    }

    public static PangyaUiDocument Load(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists) throw new FileNotFoundException("The PangYa UI XML file was not found.", fullPath);
        if (info.Length > MaximumXmlBytes) throw new InvalidDataException("The PangYa UI XML file is too large.");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = PangyaXmlCompatibility.DetectEncoding(fullPath);
        string xml = File.ReadAllText(fullPath, encoding);
        xml = NormalizeLegacyMultilineComments(xml);
        xml = PangyaXmlCompatibility.EscapeBareAmpersands(xml);
        xml = NormalizeLegacyAdjacentAttributes(xml);
        var warnings = new List<string>();
        xml = NormalizeLegacyDuplicateAttributes(xml, warnings);
        xml = NormalizeLegacyElementCasing(xml);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumXmlBytes,
            IgnoreComments = false
        };
        using var textReader = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(textReader, settings, fullPath);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);

        var roots = new List<PangyaUiNode>();
        var nodes = new List<PangyaUiNode>();
        var conditionalSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (document.DocumentElement is not null)
            AddNodes(document.DocumentElement, null, [], isPreviewOnly: false,
                roots, nodes, conditionalSymbols, warnings);
        if (nodes.Count > MaximumNodes) throw new InvalidDataException("The PangYa UI document contains too many elements.");
        Size canvasSize = CalculateCanvasSize(nodes);
        return new PangyaUiDocument(fullPath, document, encoding, roots, nodes, canvasSize,
            conditionalSymbols, warnings);
    }

    public async Task SaveAtomicAsync(CancellationToken cancellationToken = default)
    {
        string? directory = System.IO.Path.GetDirectoryName(Path);
        if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("The UI document has no parent directory.");
        string temporaryPath = System.IO.Path.Combine(directory,
            $".{System.IO.Path.GetFileName(Path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var settings = new XmlWriterSettings
                {
                    Encoding = _encoding,
                    Indent = false,
                    NewLineHandling = NewLineHandling.None
                };
                using (XmlWriter writer = XmlWriter.Create(temporaryPath, settings))
                {
                    _document.Save(writer);
                    writer.Flush();
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, Path, overwrite: true);
            }, cancellationToken);
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static IReadOnlyList<string> FindUiFiles(string dataRoot)
    {
        string uiDirectory = System.IO.Path.Combine(System.IO.Path.GetFullPath(dataRoot), "ui");
        if (!Directory.Exists(uiDirectory)) throw new DirectoryNotFoundException(uiDirectory);
        return Directory.EnumerateFiles(uiDirectory, "*.xml", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumNodes + 1)
            .ToArray();
    }

    private static void AddNodes(XmlElement element, PangyaUiNode? parent,
        IReadOnlyList<string> inheritedSymbols, bool isPreviewOnly,
        List<PangyaUiNode> roots, List<PangyaUiNode> nodes,
        HashSet<string> conditionalSymbols, List<string> warnings)
    {
        bool renderable = IsRenderable(element);
        PangyaUiNode? current = parent;
        if (renderable)
        {
            current = new PangyaUiNode(element, parent,
                new HashSet<string>(inheritedSymbols, StringComparer.OrdinalIgnoreCase), isPreviewOnly);
            if (parent is null) roots.Add(current);
            else parent.Children.Add(current);
            nodes.Add(current);
        }

        var activeSymbols = new List<string>(inheritedSymbols);
        int inheritedCount = activeSymbols.Count;
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlComment comment)
            {
                ProcessConditionalComment(comment, current, activeSymbols, inheritedCount,
                    isPreviewOnly, roots, nodes, conditionalSymbols, warnings);
                continue;
            }
            if (child is not XmlElement childElement ||
                childElement.LocalName.Equals("param", StringComparison.OrdinalIgnoreCase))
                continue;
            AddNodes(childElement, current, activeSymbols, isPreviewOnly,
                roots, nodes, conditionalSymbols, warnings);
        }
        if (activeSymbols.Count > inheritedCount)
            warnings.Add($"Ignored unterminated #ifdef block for '{activeSymbols[^1]}'.");
    }

    private static void ProcessConditionalComment(XmlComment comment, PangyaUiNode? parent,
        List<string> activeSymbols, int inheritedCount, bool parentIsPreviewOnly,
        List<PangyaUiNode> roots, List<PangyaUiNode> nodes,
        HashSet<string> conditionalSymbols, List<string> warnings)
    {
        const string symbolPattern = @"[A-Za-z_][A-Za-z0-9_]*";
        string commentText = comment.Value ?? string.Empty;
        Match complete = Regex.Match(commentText,
            $@"^\s*#ifdef\s+(?<symbol>{symbolPattern})(?<body>[\s\S]*?)#endif\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (complete.Success)
        {
            string symbol = complete.Groups["symbol"].Value;
            conditionalSymbols.Add(symbol);
            var required = new List<string>(activeSymbols);
            required.Add(symbol);
            try
            {
                XmlElement fragmentRoot = LoadConditionalFragment(complete.Groups["body"].Value);
                AddNodes(fragmentRoot, parent, required, isPreviewOnly: true,
                    roots, nodes, conditionalSymbols, warnings);
            }
            catch (XmlException ex)
            {
                warnings.Add($"Ignored malformed #ifdef fragment for '{symbol}': {ex.Message}");
            }
            return;
        }

        Match start = Regex.Match(commentText,
            $@"^\s*#ifdef\s+(?<symbol>{symbolPattern})\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (start.Success)
        {
            string symbol = start.Groups["symbol"].Value;
            conditionalSymbols.Add(symbol);
            activeSymbols.Add(symbol);
            return;
        }

        if (!Regex.IsMatch(commentText, @"^\s*#endif\s*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return;

        if (activeSymbols.Count > inheritedCount)
            activeSymbols.RemoveAt(activeSymbols.Count - 1);
        else if (!parentIsPreviewOnly)
            warnings.Add("Ignored unmatched #endif directive.");
    }

    private static XmlElement LoadConditionalFragment(string fragment)
    {
        string xml = "<conditional-root>" + fragment + "</conditional-root>";
        xml = PangyaXmlCompatibility.EscapeBareAmpersands(xml);
        xml = NormalizeLegacyAdjacentAttributes(xml);
        xml = NormalizeLegacyDuplicateAttributes(xml);
        xml = NormalizeLegacyElementCasing(xml);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumXmlBytes
        };
        using var textReader = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(textReader, settings);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);
        return document.DocumentElement!;
    }

    private static bool IsRenderable(XmlElement element)
    {
        string localName = element.LocalName;
        if (localName.Equals("item", StringComparison.OrdinalIgnoreCase) ||
            localName.Equals("element", StringComparison.OrdinalIgnoreCase) ||
            localName.Equals("form", StringComparison.OrdinalIgnoreCase) ||
            localName.Equals("layer", StringComparison.OrdinalIgnoreCase))
            return true;
        return element.HasAttribute("type") &&
               (element.HasAttribute("rect") || element.HasAttribute("pos") ||
                element.HasAttribute("size") || element.HasAttribute("name"));
    }

    private static Size CalculateCanvasSize(IReadOnlyList<PangyaUiNode> nodes)
    {
        PangyaUiNode? form = nodes.FirstOrDefault(node => node.IsForm);
        if (form?.Bounds.Size is { Width: > 0, Height: > 0 } formSize) return Limit(formSize);
        int width = nodes.Count == 0 ? 1024 : nodes.Max(node => node.Bounds.Right);
        int height = nodes.Count == 0 ? 768 : nodes.Max(node => node.Bounds.Bottom);
        return Limit(new Size(Math.Max(width, 640), Math.Max(height, 480)));
    }

    private static Size Limit(Size size) =>
        new(Math.Clamp(size.Width, 1, 8192), Math.Clamp(size.Height, 1, 8192));

    private static string NormalizeLegacyMultilineComments(string xml) =>
        Regex.Replace(xml, @"<!--(?<content>.*?)-->",
            match => "<!--" +
                     Regex.Replace(match.Groups["content"].Value, "-(?=-|$)", "- ",
                         RegexOptions.CultureInvariant) +
                     "-->",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static string NormalizeLegacyAdjacentAttributes(string xml)
    {
        var normalized = new StringBuilder(xml.Length);
        bool inStartTag = false;
        char quote = '\0';

        for (int index = 0; index < xml.Length; index++)
        {
            char current = xml[index];
            normalized.Append(current);

            if (!inStartTag)
            {
                if (current == '<' && index + 1 < xml.Length && IsXmlNameStart(xml[index + 1]))
                    inStartTag = true;
                continue;
            }

            if (quote == '\0')
            {
                if (current is '"' or '\'') quote = current;
                else if (current == '>') inStartTag = false;
                continue;
            }

            if (current != quote) continue;
            quote = '\0';
            int next = index + 1;
            if (next < xml.Length && IsXmlNameStart(xml[next]) && IsAttributeAssignment(xml, next))
                normalized.Append(' ');
        }

        return normalized.ToString();
    }

    private static bool IsAttributeAssignment(string xml, int start)
    {
        int index = start + 1;
        while (index < xml.Length && IsXmlNameCharacter(xml[index])) index++;
        while (index < xml.Length && char.IsWhiteSpace(xml[index])) index++;
        return index < xml.Length && xml[index] == '=';
    }

    private static bool IsXmlNameStart(char value) =>
        value is '_' or ':' || char.IsLetter(value);

    private static bool IsXmlNameCharacter(char value) =>
        IsXmlNameStart(value) || value is '-' or '.' || char.IsDigit(value);

    private static string NormalizeLegacyDuplicateAttributes(string xml,
        ICollection<string>? warnings = null)
    {
        var normalized = new StringBuilder(xml.Length);
        for (int index = 0; index < xml.Length;)
        {
            if (xml.AsSpan(index).StartsWith("<!--", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "-->");
                continue;
            }
            if (xml.AsSpan(index).StartsWith("<![CDATA[", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "]]>");
                continue;
            }
            if (xml.AsSpan(index).StartsWith("<?", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "?>");
                continue;
            }
            if (xml[index] != '<' || index + 1 >= xml.Length ||
                !IsXmlNameStart(xml[index + 1]))
            {
                normalized.Append(xml[index++]);
                continue;
            }

            int tagEnd = FindTagEnd(xml, index);
            if (tagEnd < 0)
            {
                normalized.Append(xml, index, xml.Length - index);
                break;
            }

            NormalizeStartTagDuplicateAttributes(xml.AsSpan(index, tagEnd - index + 1),
                normalized, warnings);
            index = tagEnd + 1;
        }

        return normalized.ToString();
    }

    private static void NormalizeStartTagDuplicateAttributes(ReadOnlySpan<char> tag,
        StringBuilder normalized, ICollection<string>? warnings)
    {
        int position = 1;
        while (position < tag.Length && IsXmlNameCharacter(tag[position])) position++;

        int copiedThrough = 0;
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (position < tag.Length)
        {
            int whitespaceStart = position;
            while (position < tag.Length && char.IsWhiteSpace(tag[position])) position++;
            if (position >= tag.Length || tag[position] is '/' or '>') break;

            int nameStart = position;
            if (!IsXmlNameStart(tag[position])) break;
            position++;
            while (position < tag.Length && IsXmlNameCharacter(tag[position])) position++;
            int nameEnd = position;
            while (position < tag.Length && char.IsWhiteSpace(tag[position])) position++;
            if (position >= tag.Length || tag[position] != '=') break;
            position++;
            while (position < tag.Length && char.IsWhiteSpace(tag[position])) position++;
            if (position >= tag.Length || tag[position] is not ('"' or '\'')) break;

            char quote = tag[position++];
            while (position < tag.Length && tag[position] != quote) position++;
            if (position >= tag.Length) break;
            position++;

            string name = tag[nameStart..nameEnd].ToString();
            bool isNamespaceDeclaration = name.Equals("xmlns", StringComparison.Ordinal) ||
                                          name.StartsWith("xmlns:", StringComparison.Ordinal);
            if (isNamespaceDeclaration || names.Add(name)) continue;

            normalized.Append(tag[copiedThrough..whitespaceStart]);
            copiedThrough = position;
            warnings?.Add($"Ignored duplicate XML attribute '{name}' and kept its first value.");
        }

        normalized.Append(tag[copiedThrough..]);
    }

    private static string NormalizeLegacyElementCasing(string xml)
    {
        var normalized = new StringBuilder(xml.Length);
        var openElements = new Stack<string>();

        for (int index = 0; index < xml.Length;)
        {
            if (xml[index] != '<')
            {
                normalized.Append(xml[index++]);
                continue;
            }

            if (xml.AsSpan(index).StartsWith("<!--", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "-->");
                continue;
            }
            if (xml.AsSpan(index).StartsWith("<![CDATA[", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "]]>");
                continue;
            }
            if (xml.AsSpan(index).StartsWith("<?", StringComparison.Ordinal))
            {
                CopyMarkupBlock(xml, normalized, ref index, "?>");
                continue;
            }
            if (xml.AsSpan(index).StartsWith("<!", StringComparison.Ordinal))
            {
                int declarationEnd = FindTagEnd(xml, index);
                if (declarationEnd < 0)
                {
                    normalized.Append(xml, index, xml.Length - index);
                    break;
                }
                normalized.Append(xml, index, declarationEnd - index + 1);
                index = declarationEnd + 1;
                continue;
            }

            int tagEnd = FindTagEnd(xml, index);
            if (tagEnd < 0)
            {
                normalized.Append(xml, index, xml.Length - index);
                break;
            }

            bool closing = index + 1 < xml.Length && xml[index + 1] == '/';
            int nameStart = index + (closing ? 2 : 1);
            while (nameStart < tagEnd && char.IsWhiteSpace(xml[nameStart])) nameStart++;
            int nameEnd = nameStart;
            while (nameEnd < tagEnd && IsXmlNameCharacter(xml[nameEnd])) nameEnd++;
            if (nameEnd == nameStart)
            {
                normalized.Append(xml, index, tagEnd - index + 1);
                index = tagEnd + 1;
                continue;
            }

            string name = xml[nameStart..nameEnd];
            if (closing && openElements.TryPeek(out string? openingName) &&
                name.Equals(openingName, StringComparison.OrdinalIgnoreCase))
            {
                normalized.Append(xml, index, nameStart - index);
                normalized.Append(openingName);
                normalized.Append(xml, nameEnd, tagEnd - nameEnd + 1);
                openElements.Pop();
            }
            else
            {
                normalized.Append(xml, index, tagEnd - index + 1);
                if (!closing && !IsSelfClosingTag(xml, index, tagEnd))
                    openElements.Push(name);
            }
            index = tagEnd + 1;
        }

        return normalized.ToString();
    }

    private static void CopyMarkupBlock(string xml, StringBuilder destination,
        ref int index, string terminator)
    {
        int end = xml.IndexOf(terminator, index, StringComparison.Ordinal);
        if (end < 0)
        {
            destination.Append(xml, index, xml.Length - index);
            index = xml.Length;
            return;
        }
        int length = end - index + terminator.Length;
        destination.Append(xml, index, length);
        index += length;
    }

    private static int FindTagEnd(string xml, int start)
    {
        char quote = '\0';
        for (int index = start + 1; index < xml.Length; index++)
        {
            char current = xml[index];
            if (quote == '\0')
            {
                if (current is '"' or '\'') quote = current;
                else if (current == '>') return index;
            }
            else if (current == quote)
            {
                quote = '\0';
            }
        }
        return -1;
    }

    private static bool IsSelfClosingTag(string xml, int start, int end)
    {
        int index = end - 1;
        while (index > start && char.IsWhiteSpace(xml[index])) index--;
        return xml[index] == '/';
    }
}

public sealed class PangyaUiResourceCatalog
{
    internal PangyaUiEmoticons? Emoticons { get; private init; }
    public IReadOnlySet<string> ConditionalSymbols { get; private init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ResourceDefinition>> _definitions;

    private PangyaUiResourceCatalog(Dictionary<string, List<ResourceDefinition>> definitions)
    {
        _definitions = definitions;
    }

    public static PangyaUiResourceCatalog Empty { get; } = new(
        new Dictionary<string, List<ResourceDefinition>>(StringComparer.Ordinal));

    public static PangyaUiResourceCatalog Load(IEnumerable<string> paths, string? includeRoot = null)
        => LoadCore(paths, includeRoot, entryPath: null);

    public static PangyaUiResourceCatalog LoadEntry(string entryPath,
        IEnumerable<string>? availablePaths = null, string? includeRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);
        return LoadCore((availablePaths ?? []).Append(entryPath), includeRoot, Path.GetFullPath(entryPath));
    }

    private static PangyaUiResourceCatalog LoadCore(IEnumerable<string> paths, string? includeRoot, string? entryPath)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var definitions = new Dictionary<string, List<ResourceDefinition>>(
            StringComparer.Ordinal);
        string[] orderedPaths = paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => !Path.GetFileName(path).Equals("all.xml", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        var knownPaths = new HashSet<string>(orderedPaths, StringComparer.OrdinalIgnoreCase);
        string[] includeDirectories = (includeRoot is null
            ? orderedPaths.Select(path => Path.GetDirectoryName(path) ?? path)
            : new[] { Path.GetFullPath(includeRoot) })
            .Select(directory => Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var visited = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
        var activePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var documents = new Dictionary<string, PangyaUiDocument?>(StringComparer.OrdinalIgnoreCase);
        var conditionalSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in entryPath is null ? orderedPaths : new[] { entryPath }) AddDocument(path, [], 0);
        PangyaUiEmoticons? emoticons = null;
        string? emoticonPath = orderedPaths.FirstOrDefault(path =>
            Path.GetFileName(path).Equals("emoticon.xml", StringComparison.OrdinalIgnoreCase));
        if (emoticonPath is not null)
        {
            try { emoticons = PangyaUiEmoticons.Load(emoticonPath); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
            {
                // Emoticons are optional preview resources.
            }
        }
        return new PangyaUiResourceCatalog(definitions)
            { Emoticons = emoticons, ConditionalSymbols = conditionalSymbols };

        void AddDocument(string path, string[] requiredSymbols, int depth)
        {
            if (depth >= 64 || activePaths.Contains(path)) return;
            if (!visited.TryGetValue(path, out List<string[]>? earlierConditions))
            {
                earlierConditions = [];
                visited.Add(path, earlierConditions);
            }
            // An earlier, less restrictive visit already wins whenever this one would be visible.
            if (earlierConditions.Any(earlier =>
                earlier.All(symbol => requiredSymbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))))
                return;
            earlierConditions.Add(requiredSymbols);
            activePaths.Add(path);
            try
            {
                if (!documents.TryGetValue(path, out PangyaUiDocument? document))
                {
                    documents[path] = null;
                    documents[path] = document = PangyaUiDocument.Load(path);
                }
                if (document is null) return;
                conditionalSymbols.UnionWith(document.ConditionalSymbols);
                foreach (PangyaUiNode root in document.Roots.Where(root =>
                             !string.IsNullOrWhiteSpace(root.Name)))
                {
                    if (root.Type.Equals("INCLUDE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.IsPathRooted(root.Name)) continue;
                        string include;
                        try { include = Path.GetFullPath(root.Name, Path.GetDirectoryName(path) ?? path); }
                        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                        {
                            continue;
                        }
                        if (knownPaths.Contains(include) || includeDirectories.Any(directory =>
                                include.StartsWith(directory, StringComparison.OrdinalIgnoreCase)))
                        {
                            string[] symbols = requiredSymbols.Concat(root.RequiredSymbols)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase).ToArray();
                            AddDocument(include, symbols, depth + 1);
                        }
                        continue;
                    }
                    if (!definitions.TryGetValue(root.Name, out List<ResourceDefinition>? matches))
                    {
                        matches = [];
                        definitions[root.Name] = matches;
                    }
                    matches.Add(new ResourceDefinition(root, requiredSymbols, path));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                       XmlException or InvalidDataException or ArgumentException)
            {
                // Some data trees mix legacy and newer XML dialects. Unsupported files do not
                // prevent valid reusable definitions from being cataloged.
            }
            finally { activePaths.Remove(path); }
        }
    }

    public PangyaUiNode? TryResolve(string resourceId, string instanceType,
        IReadOnlySet<string> enabledSymbols)
    {
        if (string.IsNullOrWhiteSpace(resourceId) ||
            !_definitions.TryGetValue(resourceId, out List<ResourceDefinition>? matches))
            return null;

        ResourceDefinition[] visible = matches
            .Where(match => match.Node.IsVisible(enabledSymbols) &&
                match.IncludeSymbols.All(enabledSymbols.Contains))
            .ToArray();
        if (visible.Length == 0) return null;

        ResourceDefinition? sameType = visible.FirstOrDefault(match =>
            match.Node.Type.Equals(instanceType, StringComparison.OrdinalIgnoreCase));
        if (sameType is not null) return sameType.Node;
        if (instanceType.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase) ||
            instanceType.Equals("BITMAP", StringComparison.OrdinalIgnoreCase)) return null;

        if (instanceType.Equals("FORM", StringComparison.OrdinalIgnoreCase) ||
            instanceType.Equals("FRAME", StringComparison.OrdinalIgnoreCase) ||
            instanceType.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase))
        {
            ResourceDefinition? frame = visible.FirstOrDefault(match =>
                match.Node.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase));
            if (frame is not null) return frame.Node;
        }

        if (instanceType.Equals("AREA", StringComparison.OrdinalIgnoreCase) ||
            instanceType.Equals("BUTTON", StringComparison.OrdinalIgnoreCase))
            return visible.FirstOrDefault(match =>
                match.Node.Type.Equals("BITMAP", StringComparison.OrdinalIgnoreCase))?.Node;

        return null;
    }

    public string? GetSourcePath(PangyaUiNode definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return _definitions.Values.SelectMany(matches => matches)
            .FirstOrDefault(match => ReferenceEquals(match.Node, definition))?.SourcePath;
    }

    private sealed record ResourceDefinition(PangyaUiNode Node, string[] IncludeSymbols, string SourcePath);
}
