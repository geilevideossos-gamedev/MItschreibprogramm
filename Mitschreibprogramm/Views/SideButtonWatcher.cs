using System.Windows;
using System.Windows.Input;

namespace Mitschreibprogramm.Views;

// InkCanvas never looks at the pen's side button (barrel), so its state is tracked here.
// An inverted pen needs no code: InkCanvas switches to EditingModeInverted on its own.
public sealed class SideButtonWatcher
{
    private bool _touching;

    public SideButtonWatcher(UIElement source)
    {
        source.PreviewStylusInRange += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusInAirMove += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusButtonDown += (_, e) => Sync(e.StylusDevice);
        source.PreviewStylusButtonUp += (_, e) => Sync(e.StylusDevice);
        // The button event can arrive after StylusDown when both change in one packet; the live state is already right here.
        source.PreviewStylusDown += (_, e) =>
        {
            Sync(e.StylusDevice);
            _touching = true;
        };
        // Letting go of the button just before lifting the pen must not turn the lasso into ink, so a release only
        // counts after the pen-up has been handled.
        source.PreviewStylusUp += (_, e) =>
        {
            _touching = false;
            source.Dispatcher.BeginInvoke(() => Sync(e.StylusDevice));
        };
        source.PreviewStylusOutOfRange += (_, _) => Release();
        // Lifting the pen over the toolbar never reaches this element; without this the mouse would keep selecting.
        source.StylusLeave += (_, _) => Release();
    }

    public event Action? Changed;

    public bool IsHeld { get; private set; }

    public static bool IsBarrelDown(StylusDevice device) => device.StylusButtons.Any(button =>
        button.Guid == StylusPointProperties.BarrelButton.Id &&
        button.StylusButtonState == StylusButtonState.Down);

    private void Sync(StylusDevice device) => Set(IsBarrelDown(device));

    private void Release()
    {
        _touching = false;
        Set(false);
    }

    private void Set(bool held)
    {
        if (IsHeld == held || (!held && _touching))
        {
            return;
        }

        IsHeld = held;
        Changed?.Invoke();
    }
}
