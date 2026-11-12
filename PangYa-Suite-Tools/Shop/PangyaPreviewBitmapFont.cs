using PangyaAPI.WFT;
using System.Text;

namespace PangYa_Suite_Tools.Shop;

internal sealed class PangyaPreviewBitmapFont
{
    private readonly WftGlyph[] _glyphs;
    private readonly WftGlyph[]? _koreanGlyphs;
    private readonly Color?[] _colorSet = [null, Color.Red, Color.Lime, Color.Blue, null, null, null, null, null, null];
    private static readonly Encoding ShiftJis = CodePagesEncodingProvider.Instance.GetEncoding(932,
        new EncoderReplacementFallback("?"), new DecoderReplacementFallback("?"))
        ?? throw new InvalidOperationException("Shift-JIS encoding is unavailable.");
    private static readonly Encoding Korean = CodePagesEncodingProvider.Instance.GetEncoding(949,
        new EncoderReplacementFallback("?"), new DecoderReplacementFallback("?"))
        ?? throw new InvalidOperationException("Korean code page 949 is unavailable.");
    public int Space { get; private set; } = 1;
    public bool FixedWidth { get; private set; }

    public void SetFixedWidth(bool fixedWidth) => FixedWidth = fixedWidth;

    public void SetColorSet(int index, Color color)
    {
        if (index is < 1 or > 9) throw new ArgumentOutOfRangeException(nameof(index));
        _colorSet[index] = color;
    }

    public int SetSpace(int space)
    {
        int previous = Space;
        Space = space;
        return previous;
    }

    public PangyaPreviewBitmapFont(WftFont font, CancellationToken cancellationToken, WftFont? koreanFallback = null)
    {
        Height = font.CellSize;
        _glyphs = ReadGlyphs(font, cancellationToken);
        if (koreanFallback is not null) _koreanGlyphs = ReadGlyphs(koreanFallback, cancellationToken);
    }

    private static WftGlyph[] ReadGlyphs(WftFont font, CancellationToken cancellationToken)
    {
        var glyphs = new WftGlyph[font.GlyphCount];
        for (int index = 0; index < glyphs.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            glyphs[index] = font.ReadGlyph((ushort)(WftFont.FirstCodePoint + index));
        }
        return glyphs;
    }

    public int Height { get; }

    public float Measure(string text)
    {
        float width = 0;
        foreach ((Rune character, _) in Characters(text))
        {
            if (character.Value == 0) break;
            width += FixedWidth ? Height : character.Value == ' ' ? Height * 14 / 36 :
                GetGlyph(character, out bool multibyte) is { } glyph ? Advance(glyph, multibyte) : 0;
        }
        return width;
    }

    public void Draw(Graphics graphics, string text, Brush brush, PointF origin)
    {
        Color color = brush is SolidBrush solid ? solid.Color : Color.Empty;
        using var coverageBrush = brush is SolidBrush ? new SolidBrush(color) : null;
        // WFont sets coordinate mode 0x400. WOverlay floors the destination
        // before rendering, including half-pixel origins from centered text.
        float x = MathF.Floor(origin.X);
        float top = MathF.Floor(origin.Y);
        foreach ((Rune character, int colorIndex) in Characters(text))
        {
            if (character.Value == 0) break;
            // GetTexture handles spaces before the fixed-width branch, unlike
            // GetTextWidthInside: retain that native measurement/drawing difference.
            if (character.Value == ' ') { x += Height * 14 / 36; continue; }
            if (GetGlyph(character, out bool multibyte) is not { } glyph) continue;
            float advance = Advance(glyph, multibyte);
            // Fresh writes whole cells at the pen position. The next glyph or
            // space overwrites the overhang, and the final texture ends at the pen.
            int visibleWidth = (int)Math.Clamp(advance, 0, Height);
            Color glyphColor = _colorSet[colorIndex] is Color palette
                ? Color.FromArgb(color.A, palette.R * color.R >> 8,
                    palette.G * color.G >> 8, palette.B * color.B >> 8)
                : color;
            ReadOnlySpan<byte> coverage = glyph.Coverage.Span;
            for (int y = 0; y < Height; y++)
            {
                int sourceY = SampleCoordinate(y, Height, glyph.CellHeight);
                ReadOnlySpan<byte> row = coverage.Slice(sourceY * glyph.CellWidth, glyph.CellWidth);
                int column = 0;
                while (column < visibleWidth)
                {
                    byte alpha = row[SampleCoordinate(column, Height, glyph.CellWidth)];
                    if (alpha == 0)
                    {
                        column++;
                        continue;
                    }
                    int start = column++;
                    while (column < visibleWidth && row[SampleCoordinate(column, Height, glyph.CellWidth)] == alpha)
                        column++;
                    if (coverageBrush is not null)
                        coverageBrush.Color = Color.FromArgb(glyphColor.A * alpha / byte.MaxValue, glyphColor);
                    graphics.FillRectangle(coverageBrush ?? brush, x + start, top + y, column - start, 1);
                }
            }
            x += advance;
        }
    }

    private float Advance(WftGlyph glyph, bool multibyte)
    {
        int width = (glyph.AdvanceWidth * Height + glyph.CellWidth / 2) / glyph.CellWidth;
        return FixedWidth ? Height : (float)((long)Space + width - (multibyte ? 1 : 0));
    }

    private static int SampleCoordinate(int pixel, int destinationSize, int sourceSize) =>
        (2 * pixel + 1) * sourceSize / (2 * destinationSize);

    private static IEnumerable<(Rune Character, int ColorIndex)> Characters(string text)
    {
        int colorIndex = 0;
        bool escape = false;
        foreach (Rune character in text.EnumerateRunes())
        {
            if (character.Value == 0) yield break;
            if (escape)
            {
                escape = false;
                if (character.Value is >= '0' and <= '9')
                {
                    colorIndex = character.Value - '0';
                    continue;
                }
                // PrintOut discards the introducer of an unknown escape and
                // processes the following character normally on the next pass.
            }
            if (character.Value == '\\') { escape = true; continue; }
            yield return (character, colorIndex);
        }
    }

    private WftGlyph? GetGlyph(Rune character, out bool multibyte)
    {
        Span<char> unicode = stackalloc char[2];
        // Code-page fallback can emit one '?' per UTF-16 surrogate. Replace an
        // unsupported supplementary rune once so it cannot look like a two-byte glyph.
        int chars = (character.IsBmp ? character : new Rune('?')).EncodeToUtf16(unicode);
        Span<byte> bytes = stackalloc byte[8];
        int count = ShiftJis.GetBytes(unicode[..chars], bytes);
        WftGlyph[] glyphs = _glyphs;
        if (_koreanGlyphs is not null && count == 1 && bytes[0] == '?' && character.IsBmp && character.Value != '?')
        {
            count = Korean.GetBytes(unicode[..chars], bytes);
            if (count == 2) glyphs = _koreanGlyphs;
        }
        multibyte = count == 2;
        int code = multibyte ? bytes[0] << 8 | bytes[1] : bytes[0];
        if (code < 0x20) code = '$';
        int index = code - WftFont.FirstCodePoint;
        return (uint)index < (uint)glyphs.Length ? glyphs[index] : null;
    }
}
