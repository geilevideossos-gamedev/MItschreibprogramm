using System.IO;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class NotebookLibraryTests : IDisposable
{
    private readonly string _folder = Path.Combine(Directory.CreateTempSubdirectory("msp-library-").FullName, "notes");
    private readonly FakeTime _time = new();

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_folder)!, recursive: true);

    [Fact]
    public void Create_WritesTheFileAndTheIndex_AndAnotherInstanceLoadsBoth()
    {
        var library = new NotebookLibrary(_folder, _time);
        var document = new NoteDocument { PageStyle = PageStyle.Squared, Pages = [new NotePage()] };

        var entry = library.Create("Mathe", document);
        library.Open(entry.Id);

        var reloaded = new NotebookLibrary(_folder, _time);
        reloaded.Load();
        var loaded = Assert.Single(reloaded.Notebooks);
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal("Mathe", loaded.Name);
        Assert.Equal(_time.Now, loaded.Modified);
        Assert.Equal(entry.Id, reloaded.LastOpen);
        Assert.Equal(PageStyle.Squared, reloaded.LoadDocument(entry.Id).PageStyle);
        Assert.True(File.Exists(Path.Combine(_folder, entry.Id + ".msp")));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void Notebooks_AreSortedByModifiedNewestFirst()
    {
        var library = new NotebookLibrary(_folder, _time);
        library.Create("Alt", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        var middle = library.Create("Mitte", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        library.Create("Neu", new NoteDocument());

        Assert.Equal(["Neu", "Mitte", "Alt"], library.Notebooks.Select(entry => entry.Name));

        library.Open(middle.Id);
        library.MarkChanged();
        _time.Advance(TimeSpan.FromMinutes(1));
        library.Save(() => new NoteDocument(), 0, 0);

        Assert.Equal(["Mitte", "Neu", "Alt"], library.Notebooks.Select(entry => entry.Name));
    }

    [Fact]
    public void Save_WritesTheFileOnlyAfterAChange_ButAlwaysRecordsTheScrollPosition()
    {
        var library = new NotebookLibrary(_folder, _time);
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

        var reloaded = new NotebookLibrary(_folder, _time);
        reloaded.Load();
        Assert.Equal(40, reloaded.Notebooks[0].ScrollY);
        Assert.Equal(_time.Now, reloaded.Notebooks[0].Modified);
    }

    [Fact]
    public void Save_KeepsTheChangeWhenTheFileCannotBeWritten()
    {
        var library = new NotebookLibrary(_folder, _time);
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
    public void Save_DoesNothingWhileNoNotebookIsOpen()
    {
        var library = new NotebookLibrary(_folder, _time);
        library.MarkChanged();

        Assert.False(library.Save(() => throw new InvalidOperationException("must not be called"), 0, 0));
    }

    [Fact]
    public void Rename_KeepsTheTimestamp_AndDelete_RemovesFileEntryAndLastOpen()
    {
        var library = new NotebookLibrary(_folder, _time);
        var first = library.Create("Eins", new NoteDocument());
        _time.Advance(TimeSpan.FromMinutes(1));
        var second = library.Create("Zwei", new NoteDocument());
        library.Open(second.Id);
        library.MarkChanged();
        _time.Advance(TimeSpan.FromMinutes(1));

        library.Rename(first.Id, "Deutsch");
        Assert.Equal("Deutsch", library.Notebooks.Single(entry => entry.Id == first.Id).Name);
        Assert.Equal(_time.Now - TimeSpan.FromMinutes(2), library.Notebooks.Single(entry => entry.Id == first.Id).Modified);

        library.Delete(second.Id);

        Assert.False(File.Exists(library.PathOf(second.Id)));
        Assert.Null(library.LastOpen);
        Assert.False(library.HasChanges);
        var reloaded = new NotebookLibrary(_folder, _time);
        reloaded.Load();
        Assert.Equal("Deutsch", Assert.Single(reloaded.Notebooks).Name);
        Assert.Null(reloaded.LastOpen);
    }

    [Fact]
    public void Import_CopiesTheFileUnderANewIdInTheCurrentFormat_NamedAfterTheFile()
    {
        var source = Path.Combine(Path.GetDirectoryName(_folder)!, "Physik Notizen.msp");
        File.WriteAllText(source, """{"version":1,"pageStyle":"dashed","pages":[{"strokes":[{"color":"red","width":2,"points":[[1,2,0.5]]}]}]}""");
        var library = new NotebookLibrary(_folder, _time);

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
        var library = new NotebookLibrary(_folder, _time);

        Assert.ThrowsAny<Exception>(() => library.Import(source));

        Assert.Empty(library.Notebooks);
    }

    [Fact]
    public void Load_DropsEntriesWithoutFile_AdoptsOrphanFiles_AndClearsAStaleLastOpen()
    {
        var library = new NotebookLibrary(_folder, _time);
        var kept = library.Create("Bleibt", new NoteDocument());
        var lost = library.Create("Weg", new NoteDocument());
        library.Open(lost.Id);
        File.Delete(library.PathOf(lost.Id));
        MspFileService.Save(new NoteDocument(), Path.Combine(_folder, "Chemie.msp"));
        File.WriteAllText(Path.Combine(_folder, "notizen.txt"), "kein Heft");

        var reloaded = new NotebookLibrary(_folder, _time);
        reloaded.Load();

        Assert.Equal(["Bleibt", "Chemie"], reloaded.Notebooks.Select(entry => entry.Name).OrderBy(name => name));
        Assert.Equal("Chemie", reloaded.Notebooks.Single(entry => entry.Name == "Chemie").Id);
        Assert.Equal(kept.Id, reloaded.Notebooks.Single(entry => entry.Name == "Bleibt").Id);
        Assert.Null(reloaded.LastOpen);
    }

    [Fact]
    public void Load_RebuildsFromTheFolderWhenTheIndexIsCorrupt_AndStartsEmptyWithoutAFolder()
    {
        var library = new NotebookLibrary(_folder, _time);
        var entry = library.Create("Mathe", new NoteDocument());
        File.WriteAllText(Path.Combine(_folder, "index.json"), "{ not json");

        var reloaded = new NotebookLibrary(_folder, _time);
        reloaded.Load();
        var adopted = Assert.Single(reloaded.Notebooks);
        Assert.Equal(entry.Id, adopted.Id);
        Assert.Equal(entry.Id, adopted.Name);

        var empty = new NotebookLibrary(Path.Combine(_folder, "missing"), _time);
        empty.Load();
        Assert.Empty(empty.Notebooks);
        Assert.Null(empty.LastOpen);
    }

    [Fact]
    public void Index_HasTheDocumentedShape()
    {
        var library = new NotebookLibrary(_folder, _time);
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

    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; private set; } = new(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => Now += by;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
