using System.IO;

namespace Mitschreibprogramm.Services;

public static class AppPaths
{
    // With MSP_DEBUG_LOG set everything lives next to the log, so self-tests never touch the real settings or notebooks.
    private static readonly string Folder = DebugLog.Folder
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mitschreibprogramm");

    public static string SettingsFile => Path.Combine(Folder, "settings.json");

    public static string NotesFolder => Path.Combine(Folder, "notes");
}
