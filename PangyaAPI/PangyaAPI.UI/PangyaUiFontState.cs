using System.Drawing;

namespace PangyaAPI.UI;

/// <summary>Applies one font's scale and clipping to renderer font callbacks.</summary>
public sealed class PangyaUiFontState
{
    private readonly Func<string, float> _measure;
    private readonly Action<Graphics, string, Brush, PointF> _draw;
    private readonly int _height;
    public float Scale { get; private set; } = 1;
    public Rectangle? ClippingArea { get; private set; }

    /// <summary>Sets an absolute clip rectangle; null disables font clipping.</summary>
    public void SetClippingArea(Rectangle? area) => ClippingArea = area;

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
        Rectangle? clip = ClippingArea;
        int height = _height; // WFixedFont::GetFontHeight returns the unscaled cell height.
        float Measure(string text) => _measure(text) * scale;
        void Draw(Graphics graphics, string text, Brush brush, PointF origin)
        {
            if (scale == 1 && clip is null) { _draw(graphics, text, brush, origin); return; }
            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
            try
            {
                // Native overlay clips are absolute, independent of font scale.
                // Apply before changing the transform and preserve the caller's clip.
                if (clip is Rectangle area)
                    graphics.SetClip(area.Width > 0 && area.Height > 0 ? area : Rectangle.Empty,
                        System.Drawing.Drawing2D.CombineMode.Intersect);
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
