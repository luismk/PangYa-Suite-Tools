namespace PangyaAPI.UI;

/// <summary>Image-gauge reset and counter state without destination animation.</summary>
public sealed class PangyaUiImageGaugeState
{
    private readonly int _maskWidth;
    public PangyaUiGaugeState Gauge { get; } = new();
    public int PixelCount { get; private set; }
    // The pinned JP983 implementation initializes m_complete to zero and never updates it.
    public float Complete => 0;

    public PangyaUiImageGaugeState(int skinWidth, int maskWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(skinWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maskWidth);
        _maskWidth = maskWidth;
        Gauge.SetRange(0, skinWidth);
    }

    public void ResetGauge()
    {
        PixelCount = 0;
        // Unlike SetPosition(0), ResetGauge does not change the retained destination.
        Gauge.ResetImagePosition();
    }

    /// <summary>Refreshes the native pixel counter without moving the current position.</summary>
    public void RefreshPixelCount()
    {
        float delta = Gauge.Position - Gauge.Lower;
        float ratio = delta == 0 ? 0 : delta / (float)((long)Gauge.Upper - Gauge.Lower);
        PixelCount = checked((int)(_maskWidth * ratio));
    }

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.Type.Equals("GAUGEBARIMAGE", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The node must be an image gauge.", nameof(node));
        return Gauge.ApplyTo(node, options);
    }
}
