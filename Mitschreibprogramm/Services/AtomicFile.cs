using System.IO;
using System.Text;

namespace Mitschreibprogramm.Services;

public static class AtomicFile
{
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);
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
            try
            {
                File.Delete(temporary);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }

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
            catch (Exception e) when (attempt < MoveRetryDelays.Length
                && (e is UnauthorizedAccessException || e is IOException { HResult: SharingViolation or LockViolation }))
            {
                Thread.Sleep(MoveRetryDelays[attempt]);
            }
        }
    }
}
