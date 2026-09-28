using System.IO;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class LibraryLockTests : IDisposable
{
    private readonly string _folder = Path.Combine(Directory.CreateTempSubdirectory("msp-lock-").FullName, "notes");

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_folder)!, recursive: true);

    [Fact]
    public void Acquire_RefusesASecondHolder_UntilTheFirstIsDisposed()
    {
        using (var first = LibraryLock.Acquire(_folder))
        {
            Assert.NotNull(first);
            Assert.Null(LibraryLock.Acquire(_folder));
        }

        using var again = LibraryLock.Acquire(_folder);
        Assert.NotNull(again);
    }

    [Fact]
    public void Acquire_LeavesTheLibraryReadableAndIgnoredByTheIndex()
    {
        using var held = LibraryLock.Acquire(_folder);
        var library = new NotebookLibrary(_folder);

        library.Load();

        Assert.Empty(library.Notebooks);
        Assert.True(File.Exists(Path.Combine(_folder, ".lock")));
    }
}
