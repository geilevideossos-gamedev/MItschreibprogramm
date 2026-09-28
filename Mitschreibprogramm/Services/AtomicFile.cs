using System.IO;
using System.Text;

namespace Mitschreibprogramm.Services;

public static class AtomicFile
{
    private static readonly TimeSpan[] MoveRetryDelays = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(400)];

    // Written next to the target and moved over it, so a crash never leaves a half-written file behind.
    public static void Write(string path, string text)
    {
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, text, new UTF8Encoding(false));
            Move(temporary, path);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }

    // A virus scanner may still hold the fresh file for a moment; that must not turn an autosave into an error.
    private static void Move(string temporary, string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.Move(temporary, path, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < MoveRetryDelays.Length)
            {
                Thread.Sleep(MoveRetryDelays[attempt]);
            }
        }
    }
}
