using System.Windows;
using System.Windows.Input;

namespace Mitschreibprogramm.Views;

// InkCanvas never looks at the pen's side button (barrel), so its state is tracked here.
// An inverted pen needs no code: InkCanvas switches to EditingModeInverted on its own.
public sealed class SideButtonWatcher
{
    public SideButtonWatcher(UIElement source)
    {
        source.PreviewStylusInRange += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusInAirMove += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusButtonDown += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusButtonUp += (_, e) => Sync(e.StylusDevice);
        // The button event can arrive after StylusDown when both change in one packet; the live state is already right here.
        source.PreviewStylusDown += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusOutOfRange += (_, _) => Set(false);
        // Lifting the pen over the toolbar never reaches this element; without this the mouse would keep selecting.
        source.StylusLeave += (_, _) => Set(false);
    }

    public event Action? Changed;

    public bool IsHeld { get; private set; }

    public static bool IsBarrelDown(StylusDevice device) => device.StylusButtons.Any(button =>
        button.Guid == StylusPointProperties.BarrelButton.Id &&
        button.StylusButtonState == StylusButtonState.Down);

    private void Sync(StylusDevice device) => Set(IsBarrelDown(device));

    private void Set(bool held)
    {
        if (IsHeld == held)
        {
            return;
        }

        IsHeld = held;
        Changed?.Invoke();
    }
}
