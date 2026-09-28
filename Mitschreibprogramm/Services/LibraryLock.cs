using System.IO;

namespace Mitschreibprogramm.Services;

// Held for the lifetime of the process: two instances on one library would overwrite each other's index and notebooks.
public sealed class LibraryLock : IDisposable
{
    private const string LockFileName = ".lock";
    private const int SharingViolation = unchecked((int)0x80070020);

    private readonly FileStream? _stream;

    private LibraryLock(FileStream? stream)
    {
        _stream = stream;
    }

    // Null when another instance holds the library. A folder that cannot be written stays unlocked; saving reports that itself.
    public static LibraryLock? Acquire(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            return new LibraryLock(new FileStream(Path.Combine(folder, LockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException e) when (e.HResult == SharingViolation)
        {
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new LibraryLock(null);
        }
    }

    public void Dispose() => _stream?.Dispose();
}
