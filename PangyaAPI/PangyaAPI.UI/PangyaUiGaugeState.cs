namespace PangyaAPI.UI;

/// <summary>Non-animation gauge values and extended-gauge step operations.</summary>
public sealed class PangyaUiGaugeState
{
    private float _stepPosition;
    private ulong _geometryVersion;
    public int Lower { get; private set; }
    public int Upper { get; private set; } = 100;
    public int Origin { get; private set; }
    public float Position { get; private set; }
    public float Destination { get; private set; }
    public float ColorPosition { get; private set; }
    public int Expansion { get; private set; }
    public int Steps { get; private set; } = 1;

    public void SetRange(int lower, int upper, int origin = 0)
    {
        if (upper <= lower) throw new ArgumentOutOfRangeException(nameof(upper), "The gauge range must have a positive span.");
        Lower = lower;
        Upper = upper;
        Origin = Math.Clamp(origin, lower, upper);
        Position = Math.Clamp(Position, lower, upper);
        Destination = Math.Clamp(Destination, lower, upper);
        RecalculateGeometry();
    }

    public void SetPosition(int position)
    {
        Position = Destination = Math.Clamp(position, Lower, Upper);
        // FrGaugeBar::SetPos invokes virtual CalcBarRect before the extended
        // override assigns m_fPos, so color initially uses the previous accumulator.
        RecalculateGeometry();
        _stepPosition = Position;
    }
    public void SetDestination(int position) => Destination = Math.Clamp(position, Lower, Upper);
    internal void ResetImagePosition() => Position = 0;
    public void SetExpansion(int expansion) => Expansion = expansion;
    public void SetSteps(int steps) => Steps = Math.Max(1, steps);

    public void StepLeft()
    {
        float step = (float)((long)Upper - Lower) / Steps;
        _stepPosition = Position = Destination = Math.Max(Lower, _stepPosition - step);
        RecalculateGeometry();
    }

    public void StepRight()
    {
        float step = (float)((long)Upper - Lower) / Steps;
        _stepPosition = Position = Destination = Math.Min(Upper, _stepPosition + step);
        RecalculateGeometry();
    }

    /// <summary>Refreshes cached geometry/color without advancing position or animating.</summary>
    public void RecalculateGeometry()
    {
        ColorPosition = _stepPosition;
        unchecked { _geometryVersion++; }
    }

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.Type.Equals("GAUGEBAR", StringComparison.OrdinalIgnoreCase) &&
            !node.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase) &&
            !node.Type.Equals("GAUGEBARIMAGE", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The node must be a gauge.", nameof(node));
        options ??= new();
        var positions = options.GaugePositions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var colorPositions = options.GaugeColorPositions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var ranges = options.GaugeRanges?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var expansions = options.GaugeExpansions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var counts = options.GaugeStepCounts?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var steps = options.ExtendedGaugeSteps?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var transitions = options.GaugeTransitions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var bars = options.GaugeBars?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var tracks = options.GaugeTracks?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var versions = options.GaugeGeometryVersions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        positions[node] = Position;
        if (node.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase)) colorPositions[node] = ColorPosition;
        ranges[node] = new(Lower, Upper, Origin);
        expansions[node] = Expansion;
        counts[node] = Steps;
        steps.Remove(node);
        transitions.Remove(node);
        if (!versions.TryGetValue(node, out var previous) || !ReferenceEquals(previous.State, this) ||
            previous.Version != _geometryVersion)
        {
            bars.Remove(node);
            if (!node.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase)) tracks.Remove(node);
        }
        versions[node] = (this, _geometryVersion);
        return options with
        {
            GaugePositions = positions, GaugeColorPositions = colorPositions, GaugeRanges = ranges, GaugeExpansions = expansions,
            GaugeStepCounts = counts, ExtendedGaugeSteps = steps, GaugeTransitions = transitions,
            GaugeBars = bars, GaugeTracks = tracks, GaugeGeometryVersions = versions
        };
    }
}
