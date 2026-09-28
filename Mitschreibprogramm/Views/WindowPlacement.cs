using System.Windows;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

public static class WindowPlacement
{
    // A remembered position is only used while a visible part of it still lies on a connected screen.
    public static void Restore(Window window, AppSettings settings)
    {
        if (settings is { WindowLeft: { } left, WindowTop: { } top, WindowWidth: > 0 and { } width, WindowHeight: > 0 and { } height })
        {
            var visible = new Rect(left, top, width, height);
            visible.Intersect(new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight));
            if (visible.Width >= AppConstants.MinVisibleWindowPart && visible.Height >= AppConstants.MinVisibleWindowPart)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                (window.Left, window.Top, window.Width, window.Height) = (left, top, width, height);
            }
        }

        if (settings.WindowMaximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    public static void Store(Window window, AppSettings settings)
    {
        var bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        (settings.WindowLeft, settings.WindowTop, settings.WindowWidth, settings.WindowHeight) =
            (bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        settings.WindowMaximized = window.WindowState == WindowState.Maximized;
    }
}
