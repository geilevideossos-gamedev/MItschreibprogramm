using System.Windows;
using Mitschreibprogramm.Services;
using Mitschreibprogramm.Views;

namespace Mitschreibprogramm;

public partial class App : Application
{
    private LibraryLock? _libraryLock;

    // The window is created here and not through StartupUri, so a second instance never touches the library.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _libraryLock = LibraryLock.Acquire(AppPaths.NotesFolder);
        if (_libraryLock is null)
        {
            MessageBox.Show("Mitschreibprogramm läuft bereits. Bitte das offene Fenster benutzen.", "Mitschreibprogramm", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _libraryLock?.Dispose();
        base.OnExit(e);
    }
}
