using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Mitschreibprogramm.Rendering;

// UI colours for both themes. XAML only refers to the keys through DynamicResource.
public static class Theme
{
    private const int DwmUseImmersiveDarkMode = 20;

    private static readonly (string Key, string Light, string Dark)[] Brushes =
    [
        ("ChromeBrush", "#F3F3F3", "#2D2D30"),
        ("ChromeTextBrush", "#1B1B1B", "#F1F1F1"),
        ("ChromeBorderBrush", "#D0D0D0", "#4A4A4F"),
        ("HoverBrush", "#E2E2E2", "#3E3E42"),
        ("CheckedBrush", "#D4E4FA", "#264F78"),
        ("AccentBrush", "#3A78D8", "#4C9BFF"),
        ("WorkspaceBrush", "#C9CCD1", "#1E1E1E"),
        ("ScrollThumbBrush", "#A3A7AD", "#5C5C62"),
        ("ScrollThumbHoverBrush", "#7D8187", "#7A7A82"),
    ];

    public static bool IsDark { get; private set; }

    // Brushes in application resources get frozen, so they are replaced instead of changed in place.
    public static void Apply(bool dark)
    {
        IsDark = dark;
        foreach (var (key, light, darkHex) in Brushes)
        {
            Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? darkHex : light));
        }

        foreach (Window window in Application.Current.Windows)
        {
            ApplyTitleBar(window);
        }
    }

    // Needs the window handle, so windows call this from OnSourceInitialized.
    public static void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            var enabled = IsDark ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
