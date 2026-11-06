using PangyaAPI.WFT;
using System.Text;

namespace PangYa_Suite_Tools.Shop;

internal sealed class PangyaPreviewBitmapFont
{
    private readonly WftGlyph[] _glyphs;
    private static readonly Encoding ShiftJis = CodePagesEncodingProvider.Instance.GetEncoding(932,
        new EncoderReplacementFallback("?"), new DecoderReplacementFallback("?"))
        ?? throw new InvalidOperationException("Shift-JIS encoding is unavailable.");
    public int Space { get; private set; } = 1;

    public int SetSpace(int space)
    {
        int previous = Space;
        Space = space;
        return previous;
    }

    public PangyaPreviewBitmapFont(WftFont font, CancellationToken cancellationToken)
    {
        Height = font.CellSize;
        _glyphs = new WftGlyph[font.GlyphCount];
        for (int index = 0; index < _glyphs.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _glyphs[index] = font.ReadGlyph((ushort)(WftFont.FirstCodePoint + index));
        }
    }

    public int Height { get; }

    public float Measure(string text)
    {
        float width = 0;
        foreach (Rune character in text.EnumerateRunes())
            width += character.Value == ' ' ? Height * 14 / 36 :
                GetGlyph(character, out bool multibyte) is { } glyph ? Advance(glyph, multibyte) : 0;
        return width;
    }

    public void Draw(Graphics graphics, string text, Brush brush, PointF origin)
    {
        Color color = brush is SolidBrush solid ? solid.Color : Color.Empty;
        using var coverageBrush = brush is SolidBrush ? new SolidBrush(color) : null;
        float x = origin.X;
        foreach (Rune character in text.EnumerateRunes())
        {
            if (character.Value == ' ') { x += Height * 14 / 36; continue; }
            if (GetGlyph(character, out bool multibyte) is not { } glyph) continue;
            ReadOnlySpan<byte> coverage = glyph.Coverage.Span;
            for (int y = 0; y < glyph.CellHeight; y++)
            {
                ReadOnlySpan<byte> row = coverage.Slice(y * glyph.CellWidth, glyph.CellWidth);
                int column = 0;
                while (column < row.Length)
                {
                    if (row[column] == 0)
                    {
                        column++;
                        continue;
                    }
                    byte alpha = row[column];
                    int start = column++;
                    while (column < row.Length && row[column] == alpha) column++;
                    if (coverageBrush is not null)
                        coverageBrush.Color = Color.FromArgb(color.A * alpha / byte.MaxValue, color);
                    graphics.FillRectangle(coverageBrush ?? brush, x + start, origin.Y + y, column - start, 1);
                }
            }
            x += Advance(glyph, multibyte);
        }
    }

    private float Advance(WftGlyph glyph, bool multibyte) => (float)((long)Space + glyph.AdvanceWidth - (multibyte ? 1 : 0));

    private WftGlyph? GetGlyph(Rune character, out bool multibyte)
    {
        Span<char> unicode = stackalloc char[2];
        // Code-page fallback can emit one '?' per UTF-16 surrogate. Replace an
        // unsupported supplementary rune once so it cannot look like a two-byte glyph.
        int chars = (character.IsBmp ? character : new Rune('?')).EncodeToUtf16(unicode);
        Span<byte> bytes = stackalloc byte[8];
        int count = ShiftJis.GetBytes(unicode[..chars], bytes);
        multibyte = count == 2;
        int code = multibyte ? bytes[0] << 8 | bytes[1] : bytes[0];
        if (code < 0x20) code = '$';
        int index = code - WftFont.FirstCodePoint;
        return (uint)index < (uint)_glyphs.Length ? _glyphs[index] : null;
    }
}
