using System.IO;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class NotebookLibraryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Directory.CreateTempSubdirectory("msp-library-").FullName, "notes");
    private readonly FakeTime _time = new();

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_folder)!, recursive: true);

    // Tests delete permanently, so runs do not fill the recycle bin of the machine.
    private NotebookLibrary Library(string? folder = null) => new(folder ?? _folder, _time, File.Delete);

    [Fact]
    public void Create_WritesTheFileAndTheIndex_AndAnotherInstanceLoadsBoth()
    {
        var library = Library();
        var document = new NoteDocument { PageStyle = PageStyle.Squared, Pages = [new NotePage()] };

        var entry = library.Create("Mathe", document);
        library.Open(entry.Id);

        var reloaded = Library();
        reloaded.Load();
        var loaded = Assert.Single(reloaded.Notebooks);
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal("Mathe", loaded.Name);
        Assert.Equal(_time.Now, loaded.Modified);
        Assert.Equal(entry.Id, reloaded.LastOpen);
        Assert.Null(reloaded.OpenId);
        Assert.Equal(PageStyle.Squared, reloaded.LoadDocument(entry.Id).PageStyle);
        Assert.True(File.Exists(Path.Combine(_folder, entry.Id + ".msp")));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void Notebooks_AreSortedByModifiedNewestFirst_AndAChangeMovesTheOpenOneUpBeforeItIsSaved()
    {
        var library = Library();
        library.Create("Alt", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        var middle = library.Create("Mitte", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        library.Create("Neu", new NoteDocument());

        Assert.Equal(["Neu", "Mitte", "Alt"], library.Notebooks.Select(entry => entry.Name));

        library.Open(middle.Id);
        Assert.Equal(["Neu", "Mitte", "Alt"], library.Notebooks.Select(entry => entry.Name));
        _time.Advance(TimeSpan.FromMinutes(1));
        library.MarkChanged();

        Assert.Equal(["Mitte", "Neu", "Alt"], library.Notebooks.Select(entry => entry.Name));
        Assert.Equal(_time.Now, library.Entry(middle.Id).Modified);
    }

    [Fact]
    public void Save_WritesTheFileOnlyAfterAChange_ButAlwaysRecordsTheScrollPosition()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        var path = library.PathOf(entry.Id);
        var before = File.ReadAllText(path);
        library.Open(entry.Id);
        _time.Advance(TimeSpan.FromMinutes(5));

        Assert.False(library.Save(() => new NoteDocument { Pages = [new NotePage(), new NotePage()] }, 12.5, 987.25));

        Assert.Equal(before, File.ReadAllText(path));
        Assert.Equal(entry.Modified, library.Notebooks[0].Modified);
        Assert.Equal(12.5, library.Notebooks[0].ScrollX);
        Assert.Equal(987.25, library.Notebooks[0].ScrollY);

        library.MarkChanged();
        Assert.True(library.HasChanges);
        Assert.True(library.Save(() => new NoteDocument { Pages = [new NotePage(), new NotePage()] }, 0, 40));

        Assert.False(library.HasChanges);
        Assert.Equal(2, MspFileService.Load(path).Pages.Count);
        Assert.Equal(_time.Now, library.Notebooks[0].Modified);
        Assert.Equal(40, library.Notebooks[0].ScrollY);

        var reloaded = Library();
        reloaded.Load();
        Assert.Equal(40, reloaded.Notebooks[0].ScrollY);
        Assert.Equal(_time.Now, reloaded.Notebooks[0].Modified);
    }

    [Fact]
    public void Save_KeepsTheChangeWhenTheFileCannotBeWritten()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        library.Open(entry.Id);
        library.MarkChanged();
        var path = library.PathOf(entry.Id);
        File.Delete(path);
        Directory.CreateDirectory(path);

        Assert.ThrowsAny<Exception>(() => library.Save(() => new NoteDocument(), 0, 0));

        Assert.True(library.HasChanges);
        Assert.False(File.Exists(path + ".tmp"));
        Directory.Delete(path);
        Assert.True(library.Save(() => new NoteDocument(), 0, 0));
        Assert.False(library.HasChanges);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_AndMarkChanged_DoNothingWhileNoNotebookIsOpen()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        library.MarkChanged();

        Assert.False(library.HasChanges);
        Assert.False(library.Save(() => throw new InvalidOperationException("must not be called"), 0, 0));
        Assert.Equal(_time.Now, library.Entry(entry.Id).Modified);
    }

    [Fact]
    public void Rename_KeepsTheTimestamp_AndDelete_RemovesFileEntryAndOpenState()
    {
        var library = Library();
        var first = library.Create("Eins", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        var second = library.Create("Zwei", new NoteDocument());
        library.Open(second.Id);
        library.MarkChanged();
        _time.Advance(TimeSpan.FromMinutes(1));

        library.Rename(first.Id, "Deutsch");
        Assert.Equal("Deutsch", library.Entry(first.Id).Name);
        Assert.Equal(_time.Now - TimeSpan.FromMinutes(2), library.Entry(first.Id).Modified);

        library.Delete(second.Id);

        Assert.False(File.Exists(library.PathOf(second.Id)));
        Assert.Null(library.OpenId);
        Assert.Null(library.LastOpen);
        Assert.False(library.HasChanges);
        Assert.False(library.Save(() => new NoteDocument(), 0, 0));
        Assert.False(File.Exists(library.PathOf(second.Id)));
        var reloaded = Library();
        reloaded.Load();
        Assert.Equal("Deutsch", Assert.Single(reloaded.Notebooks).Name);
        Assert.Null(reloaded.LastOpen);
    }

    [Fact]
    public void Delete_LeavesTheEntryWhenTheFileIsLocked()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        var index = File.ReadAllText(Path.Combine(_folder, "index.json"));
        using (new FileStream(library.PathOf(entry.Id), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<Exception>(() => library.Delete(entry.Id));
        }

        Assert.Single(library.Notebooks);
        Assert.Equal(index, File.ReadAllText(Path.Combine(_folder, "index.json")));
    }

    [Fact]
    public void Import_CopiesTheFileUnderANewIdInTheCurrentFormat_NamedAfterTheFile()
    {
        var source = Path.Combine(Path.GetDirectoryName(_folder)!, "Physik Notizen.msp");
        File.WriteAllText(source, """{"version":1,"pageStyle":"dashed","pages":[{"strokes":[{"color":"red","width":2,"points":[[1,2,0.5]]}]}]}""");
        var library = Library();

        var entry = library.Import(source);

        Assert.Equal("Physik Notizen", entry.Name);
        Assert.NotEqual("Physik Notizen", entry.Id);
        Assert.Contains("\"pageStyle\":\"squared\"", File.ReadAllText(library.PathOf(entry.Id)));
        Assert.True(File.Exists(source));
        Assert.Single(library.LoadDocument(entry.Id).Pages[0].Strokes);
    }

    [Fact]
    public void Import_RefusesADamagedFileWithoutCreatingAnEntry()
    {
        var source = Path.Combine(Path.GetDirectoryName(_folder)!, "kaputt.msp");
        File.WriteAllText(source, "{ not json");
        var library = Library();

        Assert.ThrowsAny<Exception>(() => library.Import(source));

        Assert.Empty(library.Notebooks);
    }

    [Fact]
    public void Load_DropsEntriesWithoutFile_AdoptsOrphanFiles_IgnoresOtherFiles_AndWritesNothing()
    {
        var library = Library();
        var kept = library.Create("Bleibt", new NoteDocument());
        var lost = library.Create("Weg", new NoteDocument());
        library.Open(lost.Id);
        File.Delete(library.PathOf(lost.Id));
        MspFileService.Save(new NoteDocument(), Path.Combine(_folder, "Chemie.msp"));
        File.WriteAllText(Path.Combine(_folder, "notizen.txt"), "kein Heft");
        File.WriteAllText(Path.Combine(_folder, "halb.msp.tmp"), "{}");
        File.WriteAllText(Path.Combine(_folder, "index.json.tmp"), "{}");
        var index = File.ReadAllText(Path.Combine(_folder, "index.json"));

        var reloaded = Library();
        reloaded.Load();

        Assert.Equal(["Bleibt", "Chemie"], reloaded.Notebooks.Select(entry => entry.Name).OrderBy(name => name));
        Assert.Equal("Chemie", reloaded.Notebooks.Single(entry => entry.Name == "Chemie").Id);
        Assert.Equal(kept.Id, reloaded.Notebooks.Single(entry => entry.Name == "Bleibt").Id);
        Assert.Null(reloaded.LastOpen);
        Assert.Equal(index, File.ReadAllText(Path.Combine(_folder, "index.json")));
    }

    [Fact]
    public void Load_RebuildsFromTheFolderWhenTheIndexIsCorrupt_NamesOwnFilesRecovered_AndStartsEmptyWithoutAFolder()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        File.SetLastWriteTime(library.PathOf(entry.Id), new DateTime(2026, 9, 28, 14, 5, 0));
        File.WriteAllText(Path.Combine(_folder, "index.json"), "{ not json");

        var reloaded = Library();
        reloaded.Load();
        var adopted = Assert.Single(reloaded.Notebooks);
        Assert.Equal(entry.Id, adopted.Id);
        Assert.Equal("Wiederhergestellt 28.09.2026 14:05", adopted.Name);

        var empty = Library(Path.Combine(_folder, "missing"));
        empty.Load();
        Assert.Empty(empty.Notebooks);
        Assert.Null(empty.LastOpen);
    }

    [Fact]
    public void Load_RepairsScrollValuesThatCannotBeWrittenBack()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        File.WriteAllText(Path.Combine(_folder, "index.json"),
            $$"""{"lastOpen":"{{entry.Id}}","notebooks":[{"id":"{{entry.Id}}","name":"Mathe","modified":"2026-09-28T10:00:00+00:00","scrollX":1e309,"scrollY":-5e12}]}""");

        var reloaded = Library();
        reloaded.Load();
        reloaded.Open(entry.Id);

        Assert.Equal(0, reloaded.Entry(entry.Id).ScrollX);
        Assert.Equal(0, reloaded.Entry(entry.Id).ScrollY);
        Assert.Contains("\"scrollX\": 0", File.ReadAllText(Path.Combine(_folder, "index.json")));
    }

    [Fact]
    public void Index_HasTheDocumentedShape()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        library.Open(entry.Id);
        library.Save(() => new NoteDocument(), 1.5, 2);

        var json = File.ReadAllText(Path.Combine(_folder, "index.json"));

        Assert.Contains($"\"lastOpen\": \"{entry.Id}\"", json);
        Assert.Contains("\"notebooks\": [", json);
        Assert.Contains($"\"id\": \"{entry.Id}\"", json);
        Assert.Contains("\"name\": \"Mathe\"", json);
        Assert.Contains("\"modified\": \"2026-09-28T10:00:00+00:00\"", json);
        Assert.Contains("\"scrollX\": 1.5", json);
        Assert.Contains("\"scrollY\": 2", json);
    }

    [Fact]
    public void Contains_RecognisesPathsInsideTheLibraryOnly()
    {
        var library = Library();

        Assert.True(library.Contains(Path.Combine(_folder, "abc.msp")));
        Assert.True(library.Contains(Path.Combine(_folder, "index.json")));
        Assert.False(library.Contains(Path.Combine(Path.GetDirectoryName(_folder)!, "notes-backup", "abc.msp")));
        Assert.False(library.Contains(Path.Combine(Path.GetDirectoryName(_folder)!, "Mathe.msp")));
    }

    [Fact]
    public void Rename_RollsBackWhenTheIndexCannotBeWritten()
    {
        var library = Library();
        var entry = library.Create("Mathe", new NoteDocument());
        using (new FileStream(Path.Combine(_folder, "index.json"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<Exception>(() => library.Rename(entry.Id, "Physik"));
        }

        Assert.Equal("Mathe", library.Entry(entry.Id).Name);
        Assert.Null(library.Find("unknown"));
    }
}
