using System.Drawing;

namespace PangyaAPI.UI;

/// <summary>Applies one font's scale consistently to renderer font callbacks.</summary>
public sealed class PangyaUiFontState
{
    private readonly Func<string, float> _measure;
    private readonly Action<Graphics, string, Brush, PointF> _draw;
    private readonly int _height;
    public float Scale { get; private set; } = 1;

    public PangyaUiFontState(int height, Func<string, float> measure,
        Action<Graphics, string, Brush, PointF> draw)
    {
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        ArgumentNullException.ThrowIfNull(measure);
        ArgumentNullException.ThrowIfNull(draw);
        _height = height;
        _measure = measure;
        _draw = draw;
    }

    public void SetScale(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(_height * scale))
            throw new ArgumentOutOfRangeException(nameof(scale));
        Scale = scale;
    }

    public PangyaUiRenderOptions ApplyTo(PangyaUiRenderOptions? options = null, bool smallFont = false)
    {
        options ??= new();
        float scale = Scale;
        int height = _height; // WFixedFont::GetFontHeight returns the unscaled cell height.
        float Measure(string text) => _measure(text) * scale;
        void Draw(Graphics graphics, string text, Brush brush, PointF origin)
        {
            if (scale == 1) { _draw(graphics, text, brush, origin); return; }
            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
            try
            {
                graphics.TranslateTransform(origin.X, origin.Y);
                graphics.ScaleTransform(scale, scale);
                graphics.TranslateTransform(-origin.X, -origin.Y);
                _draw(graphics, text, brush, origin);
            }
            finally { graphics.Restore(state); }
        }
        return smallFont ? options with
        {
            TooltipFontHeight = height, MeasureTooltipText = Measure, DrawTooltipText = Draw
        } : options with
        {
            CaptionFontHeight = height, MeasureCaptionText = Measure, DrawCaptionText = Draw
        };
    }
}
