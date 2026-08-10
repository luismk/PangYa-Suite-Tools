using System.Buffers.Binary;
using System.Text;

namespace PangyaAPI.WFT;

public static class WftTrueTypeExporter
{
    private const int TargetUnitsPerEm = 1024;
    private const int MaximumGlyphDataBytes = 128 * 1024 * 1024;
    private const ulong DeterministicFontTimestamp = 3850070400;

    public static void Export(WftFont font, Stream destination,
        WftTrueTypeExportOptions options, CancellationToken cancellationToken = default,
        IProgress<WftTrueTypeExportProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(options);
        if (!destination.CanWrite)
            throw new ArgumentException("The destination stream must be writable.", nameof(destination));
        ValidateOptions(options);

        var sources = new List<SourceGlyph>();
        int maximumAdvance = 0;
        for (int index = 0; index < font.GlyphCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ushort codePoint = checked((ushort)(WftFont.FirstCodePoint + index));
            WftGlyph glyph = font.ReadGlyph(codePoint);
            if (!char.IsSurrogate((char)codePoint))
            {
                bool hasVisiblePixel = false;
                foreach (byte value in glyph.Coverage.Span)
                {
                    if (value < options.CoverageThreshold) continue;
                    hasVisiblePixel = true;
                    break;
                }
                if (hasVisiblePixel || glyph.AdvanceWidth != 0)
                {
                    sources.Add(new SourceGlyph(codePoint, glyph.AdvanceWidth));
                    maximumAdvance = Math.Max(maximumAdvance, glyph.AdvanceWidth);
                }
            }
            if ((index & 0xFF) == 0 || index + 1 == font.GlyphCount)
                progress?.Report(new WftTrueTypeExportProgress(index + 1, font.GlyphCount));
        }

        int scale = ChooseScale(font.CellSize, maximumAdvance);
        byte[] bytes = BuildFont(font, scale, sources, options, cancellationToken);
        destination.Write(bytes);
    }

    private static void ValidateOptions(WftTrueTypeExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FamilyName))
            throw new ArgumentException("The font family name cannot be empty.", nameof(options));
        if (options.FamilyName.Length > 128 || options.FamilyName.Any(char.IsControl))
            throw new ArgumentException(
                "The font family name must contain at most 128 characters and no control characters.",
                nameof(options));
        if (!Enum.IsDefined(options.Style))
            throw new ArgumentOutOfRangeException(nameof(options), "The font style is not supported.");
        if (options.CoverageThreshold == 0)
            throw new ArgumentOutOfRangeException(nameof(options),
                "The coverage threshold must be greater than zero.");
    }

    private static int ChooseScale(int cellSize, int maximumAdvance)
    {
        int minimumScale = Math.Max(1, (16 + cellSize - 1) / cellSize);
        int maximumScale = 16384 / cellSize;
        if (maximumAdvance != 0) maximumScale = Math.Min(maximumScale, ushort.MaxValue / maximumAdvance);
        if (maximumScale < minimumScale)
            throw new InvalidDataException(
                "The WFT cell size and advance widths cannot be represented by a TrueType font.");
        int preferredScale = Math.Max(minimumScale, TargetUnitsPerEm / cellSize);
        return Math.Min(preferredScale, maximumScale);
    }

    private static List<List<GridPoint>> FindContours(WftGlyph glyph, byte threshold)
    {
        ReadOnlySpan<byte> coverage = glyph.Coverage.Span;
        bool[] filled = new bool[coverage.Length];
        for (int i = 0; i < coverage.Length; i++) filled[i] = coverage[i] >= threshold;
        var edges = new Dictionary<GridPoint, List<GridPoint>>();
        for (int y = 0; y < glyph.CellHeight; y++)
        {
            for (int x = 0; x < glyph.CellWidth; x++)
            {
                if (!filled[y * glyph.CellWidth + x]) continue;
                int top = glyph.CellHeight - y;
                int bottom = top - 1;
                if (x == 0 || !filled[y * glyph.CellWidth + x - 1])
                    AddEdge(edges, new GridPoint(x, bottom), new GridPoint(x, top));
                if (y == 0 || !filled[(y - 1) * glyph.CellWidth + x])
                    AddEdge(edges, new GridPoint(x, top), new GridPoint(x + 1, top));
                if (x + 1 == glyph.CellWidth || !filled[y * glyph.CellWidth + x + 1])
                    AddEdge(edges, new GridPoint(x + 1, top), new GridPoint(x + 1, bottom));
                if (y + 1 == glyph.CellHeight || !filled[(y + 1) * glyph.CellWidth + x])
                    AddEdge(edges, new GridPoint(x + 1, bottom), new GridPoint(x, bottom));
            }
        }

        var contours = new List<List<GridPoint>>();
        int totalPoints = 0;
        while (edges.Count != 0)
        {
            GridPoint start = edges.Keys.OrderBy(point => point.Y).ThenBy(point => point.X).First();
            GridPoint current = start;
            GridPoint next = edges[start].OrderBy(point => point.Y).ThenBy(point => point.X).First();
            var contour = new List<GridPoint>();
            do
            {
                contour.Add(current);
                RemoveEdge(edges, current, next);
                GridPoint previous = current;
                current = next;
                if (current == start) break;
                if (!edges.TryGetValue(current, out List<GridPoint>? candidates))
                    throw new InvalidDataException($"Glyph U+{glyph.CodePoint:X4} has an open bitmap contour.");
                next = candidates.OrderBy(candidate => TurnRank(previous, current, candidate))
                    .ThenBy(candidate => candidate.Y).ThenBy(candidate => candidate.X).First();
            } while (true);
            contour = RemoveCollinearPoints(contour);
            if (contour.Count < 3)
                throw new InvalidDataException($"Glyph U+{glyph.CodePoint:X4} has a degenerate bitmap contour.");
            totalPoints = checked(totalPoints + contour.Count);
            contours.Add(contour);
        }
        if (contours.Count > short.MaxValue || totalPoints > ushort.MaxValue)
            throw new InvalidDataException(
                $"Glyph U+{glyph.CodePoint:X4} contains too many bitmap contours or points for TrueType.");
        return contours;
    }

    private static void AddEdge(Dictionary<GridPoint, List<GridPoint>> edges, GridPoint start,
        GridPoint end)
    {
        if (!edges.TryGetValue(start, out List<GridPoint>? values))
        {
            values = [];
            edges.Add(start, values);
        }
        values.Add(end);
    }

    private static void RemoveEdge(Dictionary<GridPoint, List<GridPoint>> edges, GridPoint start,
        GridPoint end)
    {
        List<GridPoint> values = edges[start];
        values.Remove(end);
        if (values.Count == 0) edges.Remove(start);
    }

    private static int TurnRank(GridPoint previous, GridPoint current, GridPoint next)
    {
        int incomingX = current.X - previous.X;
        int incomingY = current.Y - previous.Y;
        int outgoingX = next.X - current.X;
        int outgoingY = next.Y - current.Y;
        int cross = incomingX * outgoingY - incomingY * outgoingX;
        int dot = incomingX * outgoingX + incomingY * outgoingY;
        if (cross < 0) return 0;
        if (dot > 0) return 1;
        if (cross > 0) return 2;
        return 3;
    }

    private static List<GridPoint> RemoveCollinearPoints(List<GridPoint> points)
    {
        var result = new List<GridPoint>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            GridPoint previous = points[(i + points.Count - 1) % points.Count];
            GridPoint current = points[i];
            GridPoint next = points[(i + 1) % points.Count];
            int firstX = current.X - previous.X;
            int firstY = current.Y - previous.Y;
            int secondX = next.X - current.X;
            int secondY = next.Y - current.Y;
            if (firstX * secondY - firstY * secondX != 0) result.Add(current);
        }
        return result;
    }

    private static byte[] BuildFont(WftFont font, int scale, List<SourceGlyph> sources,
        WftTrueTypeExportOptions options, CancellationToken cancellationToken)
    {
        int unitsPerEm = checked(font.CellSize * scale);
        var glyphs = new List<EncodedGlyph>(sources.Count + 1) { EncodedGlyph.Empty };
        var mappings = new List<(ushort CodePoint, ushort GlyphIndex)>(sources.Count);
        int maximumAdvance = 0;
        int maximumPoints = 0;
        int maximumContours = 0;
        long glyphDataBytes = 0;
        foreach (SourceGlyph source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (glyphs.Count > ushort.MaxValue)
                throw new InvalidDataException("The WFT contains too many glyphs for TrueType.");
            WftGlyph sourceGlyph = font.ReadGlyph(source.CodePoint);
            List<List<GridPoint>> contours = FindContours(sourceGlyph, options.CoverageThreshold);
            EncodedGlyph glyph = EncodeGlyph(source.AdvanceWidth, contours, scale);
            glyphDataBytes = checked(glyphDataBytes + Align4(glyph.Data.Length));
            if (glyphDataBytes > MaximumGlyphDataBytes)
                throw new InvalidDataException(
                    "The converted glyph data exceeds the supported TrueType export size.");
            ushort glyphIndex = checked((ushort)glyphs.Count);
            glyphs.Add(glyph);
            mappings.Add((source.CodePoint, glyphIndex));
            maximumAdvance = Math.Max(maximumAdvance, source.AdvanceWidth * scale);
            maximumPoints = Math.Max(maximumPoints, glyph.PointCount);
            maximumContours = Math.Max(maximumContours, glyph.ContourCount);
        }

        (byte[] glyf, byte[] loca) = BuildGlyphTables(glyphs);
        ushort firstCharacter = mappings.Count == 0 ? (ushort)0 : mappings[0].CodePoint;
        ushort lastCharacter = mappings.Count == 0 ? (ushort)0 : mappings[^1].CodePoint;
        string styleName = StyleName(options.Style);
        string fullName = options.Style == WftFontStyle.Regular
            ? options.FamilyName.Trim()
            : $"{options.FamilyName.Trim()} {styleName}";
        var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["OS/2"] = BuildOs2(unitsPerEm, maximumAdvance, firstCharacter, lastCharacter,
                options.Style),
            ["cmap"] = BuildCmap(mappings),
            ["glyf"] = glyf,
            ["head"] = BuildHead(unitsPerEm, options.Style),
            ["hhea"] = BuildHhea(unitsPerEm, maximumAdvance, glyphs.Count),
            ["hmtx"] = BuildHmtx(glyphs),
            ["loca"] = loca,
            ["maxp"] = BuildMaxp(glyphs.Count, maximumPoints, maximumContours),
            ["name"] = BuildName(options.FamilyName.Trim(), styleName, fullName),
            ["post"] = BuildPost(options.Style)
        };
        return Assemble(tables);
    }

    private static EncodedGlyph EncodeGlyph(ushort sourceAdvanceWidth,
        List<List<GridPoint>> contours, int scale)
    {
        if (contours.Count == 0)
            return new EncodedGlyph([], checked((ushort)(sourceAdvanceWidth * scale)), 0, 0);
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        int contourCount = contours.Count;
        int pointCount = contours.Sum(contour => contour.Count);
        List<GridPoint> unscaledPoints = contours.SelectMany(contour => contour).ToList();
        writer.WriteInt16(checked((short)contourCount));
        writer.WriteInt16(checked((short)(unscaledPoints.Min(point => point.X) * scale)));
        writer.WriteInt16(checked((short)(unscaledPoints.Min(point => point.Y) * scale)));
        writer.WriteInt16(checked((short)(unscaledPoints.Max(point => point.X) * scale)));
        writer.WriteInt16(checked((short)(unscaledPoints.Max(point => point.Y) * scale)));
        int endPoint = -1;
        foreach (List<GridPoint> contour in contours)
        {
            endPoint += contour.Count;
            writer.WriteUInt16(checked((ushort)endPoint));
        }
        writer.WriteUInt16(0);
        for (int i = 0; i < pointCount; i++) writer.WriteByte(1);

        var points = unscaledPoints.Select(point =>
            (X: checked((short)(point.X * scale)), Y: checked((short)(point.Y * scale)))).ToArray();
        short previous = 0;
        foreach ((short x, _) in points)
        {
            writer.WriteInt16(checked((short)(x - previous)));
            previous = x;
        }
        previous = 0;
        foreach ((_, short y) in points)
        {
            writer.WriteInt16(checked((short)(y - previous)));
            previous = y;
        }
        return new EncodedGlyph(stream.ToArray(), checked((ushort)(sourceAdvanceWidth * scale)),
            pointCount, contourCount);
    }

    private static (byte[] Glyf, byte[] Loca) BuildGlyphTables(List<EncodedGlyph> glyphs)
    {
        using var glyf = new MemoryStream();
        using var loca = new MemoryStream();
        using var locaWriter = new BigEndianWriter(loca);
        foreach (EncodedGlyph glyph in glyphs)
        {
            locaWriter.WriteUInt32(checked((uint)glyf.Length));
            glyf.Write(glyph.Data);
            while ((glyf.Length & 3) != 0) glyf.WriteByte(0);
        }
        locaWriter.WriteUInt32(checked((uint)glyf.Length));
        return (glyf.ToArray(), loca.ToArray());
    }

    private static byte[] BuildCmap(List<(ushort CodePoint, ushort GlyphIndex)> mappings)
    {
        byte[] format4 = BuildFormat4Cmap(mappings);
        using var format12Stream = new MemoryStream();
        using (var writer = new BigEndianWriter(format12Stream))
        {
            var groups = new List<(uint Start, uint End, uint Glyph)>();
            foreach ((ushort codePoint, ushort glyphIndex) in mappings)
            {
                if (groups.Count != 0)
                {
                    var last = groups[^1];
                    if (codePoint == last.End + 1 && glyphIndex == last.Glyph + last.End - last.Start + 1)
                    {
                        groups[^1] = (last.Start, codePoint, last.Glyph);
                        continue;
                    }
                }
                groups.Add((codePoint, codePoint, glyphIndex));
            }
            writer.WriteUInt16(12);
            writer.WriteUInt16(0);
            writer.WriteUInt32(checked((uint)(16 + groups.Count * 12)));
            writer.WriteUInt32(0);
            writer.WriteUInt32(checked((uint)groups.Count));
            foreach (var group in groups)
            {
                writer.WriteUInt32(group.Start);
                writer.WriteUInt32(group.End);
                writer.WriteUInt32(group.Glyph);
            }
        }
        byte[] format12 = format12Stream.ToArray();
        using var table = new MemoryStream();
        using var tableWriter = new BigEndianWriter(table);
        tableWriter.WriteUInt16(0);
        tableWriter.WriteUInt16(3);
        tableWriter.WriteUInt16(0);
        tableWriter.WriteUInt16(4);
        tableWriter.WriteUInt32(checked((uint)(28 + format4.Length)));
        tableWriter.WriteUInt16(3);
        tableWriter.WriteUInt16(1);
        tableWriter.WriteUInt32(28);
        tableWriter.WriteUInt16(3);
        tableWriter.WriteUInt16(10);
        tableWriter.WriteUInt32(checked((uint)(28 + format4.Length)));
        table.Write(format4);
        table.Write(format12);
        return table.ToArray();
    }

    private static byte[] BuildFormat4Cmap(List<(ushort CodePoint, ushort GlyphIndex)> mappings)
    {
        var groups = new List<(ushort Start, ushort End, ushort Glyph)>();
        foreach ((ushort codePoint, ushort glyphIndex) in mappings.Where(mapping =>
                     mapping.CodePoint != ushort.MaxValue))
        {
            if (groups.Count != 0)
            {
                var last = groups[^1];
                if (codePoint == last.End + 1 &&
                    glyphIndex == last.Glyph + last.End - last.Start + 1)
                {
                    groups[^1] = (last.Start, codePoint, last.Glyph);
                    continue;
                }
            }
            groups.Add((codePoint, codePoint, glyphIndex));
        }

        const int maximumMappedSegments = (ushort.MaxValue - 24) / 8;
        if (groups.Count > maximumMappedSegments)
        {
            var basicGroups = groups.Where(group => group.Start <= 0x00FF).ToList();
            groups = basicGroups.Count != 0 ? basicGroups : groups.Take(1).ToList();
        }
        int segmentCount = groups.Count + 1;
        int entrySelector = (int)Math.Floor(Math.Log2(segmentCount));
        int searchRange = 2 * (1 << entrySelector);
        int length = checked(16 + segmentCount * 8);
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt16(4);
        writer.WriteUInt16(checked((ushort)length));
        writer.WriteUInt16(0);
        writer.WriteUInt16(checked((ushort)(segmentCount * 2)));
        writer.WriteUInt16(checked((ushort)searchRange));
        writer.WriteUInt16(checked((ushort)entrySelector));
        writer.WriteUInt16(checked((ushort)(segmentCount * 2 - searchRange)));
        foreach (var group in groups) writer.WriteUInt16(group.End);
        writer.WriteUInt16(ushort.MaxValue);
        writer.WriteUInt16(0);
        foreach (var group in groups) writer.WriteUInt16(group.Start);
        writer.WriteUInt16(ushort.MaxValue);
        foreach (var group in groups)
            writer.WriteUInt16(unchecked((ushort)(group.Glyph - group.Start)));
        writer.WriteUInt16(1);
        for (int i = 0; i < segmentCount; i++) writer.WriteUInt16(0);
        return stream.ToArray();
    }

    private static byte[] BuildHead(int unitsPerEm, WftFontStyle style)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt32(0x00010000); writer.WriteUInt32(0x00010000); writer.WriteUInt32(0);
        writer.WriteUInt32(0x5F0F3CF5); writer.WriteUInt16(0x000B); writer.WriteUInt16((ushort)unitsPerEm);
        writer.WriteUInt64(DeterministicFontTimestamp); writer.WriteUInt64(DeterministicFontTimestamp);
        writer.WriteInt16(0); writer.WriteInt16(0); writer.WriteInt16((short)unitsPerEm);
        writer.WriteInt16((short)unitsPerEm); writer.WriteUInt16(MacStyle(style));
        writer.WriteUInt16(1); writer.WriteInt16(2); writer.WriteInt16(1); writer.WriteInt16(0);
        return stream.ToArray();
    }

    private static byte[] BuildHhea(int unitsPerEm, int maximumAdvance, int glyphCount)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt32(0x00010000); writer.WriteInt16((short)unitsPerEm); writer.WriteInt16(0);
        writer.WriteInt16(0); writer.WriteUInt16((ushort)maximumAdvance); writer.WriteInt16(0);
        writer.WriteInt16(0); writer.WriteInt16((short)unitsPerEm); writer.WriteInt16(1);
        writer.WriteInt16(0); writer.WriteInt16(0);
        for (int i = 0; i < 4; i++) writer.WriteInt16(0);
        writer.WriteInt16(0); writer.WriteUInt16(checked((ushort)glyphCount));
        return stream.ToArray();
    }

    private static byte[] BuildHmtx(List<EncodedGlyph> glyphs)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        foreach (EncodedGlyph glyph in glyphs)
        {
            writer.WriteUInt16(glyph.AdvanceWidth);
            writer.WriteInt16(0);
        }
        return stream.ToArray();
    }

    private static byte[] BuildMaxp(int glyphCount, int maximumPoints, int maximumContours)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt32(0x00010000); writer.WriteUInt16(checked((ushort)glyphCount));
        writer.WriteUInt16(checked((ushort)maximumPoints));
        writer.WriteUInt16(checked((ushort)maximumContours));
        writer.WriteUInt16(0); writer.WriteUInt16(0); writer.WriteUInt16(1);
        for (int i = 0; i < 8; i++) writer.WriteUInt16(0);
        return stream.ToArray();
    }

    private static byte[] BuildOs2(int unitsPerEm, int maximumAdvance, ushort firstCharacter,
        ushort lastCharacter, WftFontStyle style)
    {
        bool bold = style is WftFontStyle.Bold or WftFontStyle.BoldItalic;
        bool italic = style is WftFontStyle.Italic or WftFontStyle.BoldItalic;
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt16(4); writer.WriteInt16((short)Math.Min(maximumAdvance, short.MaxValue));
        writer.WriteUInt16((ushort)(bold ? 700 : 400)); writer.WriteUInt16(5); writer.WriteUInt16(0);
        for (int i = 0; i < 11; i++) writer.WriteInt16(0);
        for (int i = 0; i < 10; i++) writer.WriteByte(0);
        writer.WriteUInt32(1); writer.WriteUInt32(0); writer.WriteUInt32(0); writer.WriteUInt32(0);
        writer.WriteAscii("PYWT");
        ushort selection = (ushort)((italic ? 1 : 0) | (bold ? 32 : 0) | (!bold && !italic ? 64 : 0));
        writer.WriteUInt16(selection); writer.WriteUInt16(firstCharacter); writer.WriteUInt16(lastCharacter);
        writer.WriteInt16((short)unitsPerEm); writer.WriteInt16(0); writer.WriteInt16(0);
        writer.WriteUInt16((ushort)unitsPerEm); writer.WriteUInt16(0);
        writer.WriteUInt32(0); writer.WriteUInt32(0);
        writer.WriteInt16(0); writer.WriteInt16(0); writer.WriteUInt16(0); writer.WriteUInt16(0x0020);
        writer.WriteUInt16(0);
        return stream.ToArray();
    }

    private static byte[] BuildName(string familyName, string styleName, string fullName)
    {
        string postScriptName = SanitizePostScriptName(fullName);
        var names = new (ushort Id, string Value)[]
        {
            (0, "Generated from a PangYa WFT bitmap font"), (1, familyName), (2, styleName),
            (3, $"1.000;PYWT;{postScriptName}"), (4, fullName), (5, "Version 1.000"),
            (6, postScriptName)
        };
        using var strings = new MemoryStream();
        var records = new List<(ushort Id, ushort Length, ushort Offset)>();
        foreach (var name in names)
        {
            byte[] value = Encoding.BigEndianUnicode.GetBytes(name.Value);
            if (value.Length > ushort.MaxValue || strings.Length > ushort.MaxValue)
                throw new ArgumentException("The generated font name metadata is too long.");
            records.Add((name.Id, (ushort)value.Length, (ushort)strings.Length));
            strings.Write(value);
        }
        using var table = new MemoryStream();
        using var writer = new BigEndianWriter(table);
        writer.WriteUInt16(0); writer.WriteUInt16((ushort)records.Count);
        writer.WriteUInt16(checked((ushort)(6 + records.Count * 12)));
        foreach (var record in records)
        {
            writer.WriteUInt16(3); writer.WriteUInt16(1); writer.WriteUInt16(0x0409);
            writer.WriteUInt16(record.Id); writer.WriteUInt16(record.Length); writer.WriteUInt16(record.Offset);
        }
        table.Write(strings.ToArray());
        return table.ToArray();
    }

    private static byte[] BuildPost(WftFontStyle style)
    {
        using var stream = new MemoryStream();
        using var writer = new BigEndianWriter(stream);
        writer.WriteUInt32(0x00030000);
        writer.WriteUInt32(style is WftFontStyle.Italic or WftFontStyle.BoldItalic
            ? unchecked((uint)(-12 << 16)) : 0);
        writer.WriteInt16(0); writer.WriteInt16(0); writer.WriteUInt32(0);
        writer.WriteUInt32(0); writer.WriteUInt32(0); writer.WriteUInt32(0); writer.WriteUInt32(0);
        return stream.ToArray();
    }

    private static byte[] Assemble(SortedDictionary<string, byte[]> tables)
    {
        int count = tables.Count;
        int entrySelector = (int)Math.Floor(Math.Log2(count));
        int searchRange = (1 << entrySelector) * 16;
        int offset = 12 + count * 16;
        var entries = new List<(string Tag, uint Checksum, int Offset, int Length, byte[] Data)>();
        foreach ((string tag, byte[] data) in tables)
        {
            entries.Add((tag, Checksum(data), offset, data.Length, data));
            offset = checked(offset + Align4(data.Length));
        }
        byte[] font = new byte[offset];
        using (var stream = new MemoryStream(font, writable: true))
        using (var writer = new BigEndianWriter(stream))
        {
            writer.WriteUInt32(0x00010000); writer.WriteUInt16((ushort)count);
            writer.WriteUInt16((ushort)searchRange); writer.WriteUInt16((ushort)(count * 16 - searchRange));
            writer.WriteUInt16((ushort)entrySelector);
            foreach (var entry in entries)
            {
                writer.WriteAscii(entry.Tag); writer.WriteUInt32(entry.Checksum);
                writer.WriteUInt32((uint)entry.Offset); writer.WriteUInt32((uint)entry.Length);
                entry.Data.CopyTo(font, entry.Offset);
            }
        }
        var head = entries.Single(e => e.Tag == "head");
        uint adjustment = unchecked(0xB1B0AFBA - Checksum(font));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(head.Offset + 8), adjustment);
        return font;
    }

    private static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        for (int i = 0; i < Align4(data.Length); i += 4)
        {
            uint value = 0;
            for (int j = 0; j < 4; j++)
                value = (value << 8) | (uint)(i + j < data.Length ? data[i + j] : 0);
            sum = unchecked(sum + value);
        }
        return sum;
    }

    private static int Align4(int value) => checked((value + 3) & ~3);
    private static string StyleName(WftFontStyle style) => style switch
    {
        WftFontStyle.Regular => "Regular", WftFontStyle.Bold => "Bold",
        WftFontStyle.Italic => "Italic", WftFontStyle.BoldItalic => "Bold Italic",
        _ => throw new ArgumentOutOfRangeException(nameof(style))
    };
    private static ushort MacStyle(WftFontStyle style) => (ushort)(
        (style is WftFontStyle.Bold or WftFontStyle.BoldItalic ? 1 : 0) |
        (style is WftFontStyle.Italic or WftFontStyle.BoldItalic ? 2 : 0));
    private static string SanitizePostScriptName(string value)
    {
        string result = new(value.Where(c => c is >= '!' and <= '~' &&
            c is not '(' and not ')' and not '<' and not '>' and not '[' and not ']' and
            not '{' and not '}' and not '/' and not '%' && !char.IsWhiteSpace(c)).ToArray());
        if (result.Length == 0) result = "PangYaWftFont";
        return result.Length <= 63 ? result : result[..63];
    }

    private readonly record struct GridPoint(int X, int Y);
    private sealed record SourceGlyph(ushort CodePoint, ushort AdvanceWidth);
    private sealed record EncodedGlyph(byte[] Data, ushort AdvanceWidth, int PointCount,
        int ContourCount)
    {
        public static EncodedGlyph Empty { get; } = new([], 0, 0, 0);
    }

    private sealed class BigEndianWriter : IDisposable
    {
        private readonly Stream _stream;
        public BigEndianWriter(Stream stream) => _stream = stream;
        public void WriteByte(byte value) => _stream.WriteByte(value);
        public void WriteAscii(string value) => _stream.Write(Encoding.ASCII.GetBytes(value));
        public void WriteInt16(short value) => WriteUInt16(unchecked((ushort)value));
        public void WriteUInt16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value); _stream.Write(buffer);
        }
        public void WriteUInt32(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value); _stream.Write(buffer);
        }
        public void WriteUInt64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, value); _stream.Write(buffer);
        }
        public void Dispose() { }
    }
}
