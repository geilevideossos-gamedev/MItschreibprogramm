using System.IO;
using System.Text;

namespace Mitschreibprogramm.Services;

public static class AtomicFile
{
    // Written next to the target and moved over it, so a crash never leaves a half-written file behind.
    public static void Write(string path, string text)
    {
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, text, new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
