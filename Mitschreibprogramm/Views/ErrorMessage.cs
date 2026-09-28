using System.IO;
using System.Text.Json;
using System.Windows;

namespace Mitschreibprogramm.Views;

public static class ErrorMessage
{
    public static void Show(Window owner, string message) =>
        MessageBox.Show(owner, message, "Mitschreibprogramm", MessageBoxButton.OK, MessageBoxImage.Warning);

    // Framework messages are English, so only the app's own German reason is shown.
    public static bool Try(Window owner, Action action, string failure)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or OperationCanceledException)
        {
            Show(owner, failure);
            return false;
        }
    }
}
