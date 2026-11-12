namespace PangyaAPI.UI;

public enum PangyaUiButtonCursor { RollOver, Select, Info }

/// <summary>Non-animation FrButton input state. The host owns capture and command dispatch.</summary>
public sealed class PangyaUiButtonInputState
{
    private bool _previousPressed;
    public bool Enabled { get; private set; } = true;
    public bool Pressed { get; private set; }
    public bool Captured { get; private set; }
    public bool UseRightButton { get; set; }
    public int PushStyle { get; set; } = 1;
    public PangyaUiButtonState Status { get; private set; }
    public double PushDelay { get; private set; } = 1.2;
    public double SecondsSincePress { get; private set; } = 1.3;

    public void Enable(bool enabled)
    {
        Enabled = enabled;
        if (Status == PangyaUiButtonState.Hover) Status = PangyaUiButtonState.Normal;
    }

    public void SetStatus(PangyaUiButtonState status)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        Status = status;
        if (PushStyle == 3) _previousPressed = status == PangyaUiButtonState.Selected;
    }

    public void SetPushDelay(double delay)
    {
        if (!double.IsFinite(delay) || !double.IsFinite(delay + 0.1))
            throw new ArgumentOutOfRangeException(nameof(delay));
        PushDelay = delay;
        SecondsSincePress = delay + 0.1;
    }

    public void AdvanceTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || !double.IsFinite(SecondsSincePress + seconds))
            throw new ArgumentOutOfRangeException(nameof(seconds));
        SecondsSincePress += seconds;
    }

    /// <returns>Whether to dispatch the corresponding native button-down command.</returns>
    public bool ButtonDown(bool inside, bool captureGranted = true, bool rightButton = false) =>
        ButtonDown(inside, () => captureGranted, rightButton);

    /// <summary>Attempts host capture only after input gating and the native timing reset.</summary>
    public bool ButtonDown(bool inside, Func<bool> tryCapture, bool rightButton = false)
    {
        ArgumentNullException.ThrowIfNull(tryCapture);
        if (!Enabled || rightButton && !UseRightButton || SecondsSincePress <= PushDelay) return false;
        // Native push timing resets before attempting capture, even if capture fails.
        if (PushStyle == 1) SecondsSincePress = 0.1;
        if (!tryCapture()) return false;
        Captured = true;
        if (PushStyle == 3)
        {
            if (_previousPressed) return false;
            _previousPressed = true;
        }
        Pressed = true;
        return inside;
    }

    /// <returns>Whether to dispatch the corresponding native button-up command.</returns>
    public bool ButtonUp(bool inside, bool rightButton = false)
    {
        if (rightButton && !UseRightButton) return false;
        bool dispatch = Pressed && (PushStyle != 1 || inside);
        Pressed = false;
        Captured = false;
        return dispatch;
    }

    public PangyaUiButtonCursor? SetCursor(bool inside)
    {
        if (PushStyle == 2)
        {
            if (Pressed != _previousPressed)
            {
                _previousPressed = Pressed;
                if (Pressed) Status = Status == PangyaUiButtonState.Selected
                    ? PangyaUiButtonState.Normal : PangyaUiButtonState.Selected;
            }
            if (Status != PangyaUiButtonState.Selected)
                Status = inside ? PangyaUiButtonState.Hover : PangyaUiButtonState.Normal;
        }
        else if (PushStyle == 3)
        {
            Status = _previousPressed || Pressed ? PangyaUiButtonState.Selected :
                inside ? PangyaUiButtonState.Hover : PangyaUiButtonState.Normal;
        }
        else Status = inside ? Pressed ? PangyaUiButtonState.Selected : PangyaUiButtonState.Hover
            : PangyaUiButtonState.Normal;
        return !inside ? null : UseRightButton ? PangyaUiButtonCursor.Info :
            Status == PangyaUiButtonState.Selected ? PangyaUiButtonCursor.Select : PangyaUiButtonCursor.RollOver;
    }

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The node must be a button.", nameof(node));
        options ??= new();
        var states = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        states[node] = (states.GetValueOrDefault(node) ?? new()) with
        {
            Enabled = Enabled, Status = Status, PushStyle = PushStyle,
            PushDelay = PushDelay, SecondsSincePress = SecondsSincePress
        };
        var disabled = options.DisabledNodes?.ToHashSet() ?? [];
        if (Enabled) disabled.Remove(node); else disabled.Add(node);
        var ticks = options.ButtonPressTicks?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        ticks.Remove(node); // The state already supplies elapsed time; stale tick overrides must not win.
        return options with { ImageButtonStates = states, DisabledNodes = disabled, ButtonPressTicks = ticks };
    }
}
