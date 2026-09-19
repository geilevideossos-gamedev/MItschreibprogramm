using System.IO;

namespace Mitschreibprogramm.Services;

public static class DebugLog
{
    private static readonly string? LogPath = Environment.GetEnvironmentVariable("MSP_DEBUG_LOG");

    public static bool IsEnabled => !string.IsNullOrEmpty(LogPath);

    public static void Write(FormattableString line)
    {
        if (!IsEnabled)
        {
            return;
        }

        File.AppendAllText(LogPath!, $"{DateTime.Now:HH:mm:ss.fff} {FormattableString.Invariant(line)}{Environment.NewLine}");
    }
}
