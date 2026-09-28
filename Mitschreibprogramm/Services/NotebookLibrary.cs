using System.IO;
using System.Text.Json;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

// One folder holds every notebook as <id>.msp plus index.json with names, timestamps and scroll positions (docs/file-format.md).
public sealed class NotebookLibrary(string folder, TimeProvider? time = null)
{
    private const string IndexFileName = "index.json";
    private const string Extension = ".msp";

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private NotebookIndex _index = new();

    public IReadOnlyList<NotebookEntry> Notebooks => _index.Notebooks
        .OrderByDescending(entry => entry.Modified)
        .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public string? LastOpen => _index.LastOpen;

    public bool HasChanges { get; private set; }

    public string PathOf(string id) => Path.Combine(folder, id + Extension);

    public NotebookEntry Entry(string id) => _index.Notebooks.First(entry => entry.Id == id);

    // Entries without a file are dropped and files without an entry are adopted, so a damaged index never hides a notebook.
    public void Load()
    {
        _index = ReadIndex();
        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder).Where(file => Path.GetExtension(file).Equals(Extension, StringComparison.OrdinalIgnoreCase)).ToList()
            : [];
        var ids = files.Select(Path.GetFileNameWithoutExtension).ToHashSet();
        var entries = (_index.Notebooks ?? [])
            .Where(entry => entry is { Id.Length: > 0 } && ids.Contains(entry.Id))
            .DistinctBy(entry => entry.Id)
            .ToList();
        foreach (var file in files)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (entries.All(entry => entry.Id != id))
            {
                entries.Add(new NotebookEntry { Id = id, Name = id, Modified = File.GetLastWriteTimeUtc(file) });
            }
        }

        foreach (var entry in entries)
        {
            entry.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Id : entry.Name;
        }

        _index.Notebooks = entries;
        if (entries.All(entry => entry.Id != _index.LastOpen))
        {
            _index.LastOpen = null;
        }
    }

    public NotebookEntry Create(string name, NoteDocument document)
    {
        var entry = new NotebookEntry { Id = Guid.NewGuid().ToString("N"), Name = name, Modified = _time.GetUtcNow() };
        Directory.CreateDirectory(folder);
        MspFileService.Save(document, PathOf(entry.Id));
        _index.Notebooks.Add(entry);
        WriteIndex();
        return entry;
    }

    // The source is parsed first, so a damaged file is refused before anything is copied, and the copy is written in the current format.
    public NotebookEntry Import(string sourcePath) =>
        Create(Path.GetFileNameWithoutExtension(sourcePath), MspFileService.Load(sourcePath));

    public NoteDocument LoadDocument(string id) => MspFileService.Load(PathOf(id));

    public void Open(string id)
    {
        _index.LastOpen = Entry(id).Id;
        HasChanges = false;
        WriteIndex();
    }

    public void MarkChanged() => HasChanges = true;

    // Autosave of the open notebook: the file is written only after a change, the scroll position is recorded every time.
    public bool Save(Func<NoteDocument> document, double scrollX, double scrollY)
    {
        if (_index.LastOpen is not { } id)
        {
            return false;
        }

        var entry = Entry(id);
        var written = HasChanges;
        if (written)
        {
            Directory.CreateDirectory(folder);
            MspFileService.Save(document(), PathOf(id));
            entry.Modified = _time.GetUtcNow();
            HasChanges = false;
        }

        (entry.ScrollX, entry.ScrollY) = (scrollX, scrollY);
        WriteIndex();
        return written;
    }

    public void Rename(string id, string name)
    {
        Entry(id).Name = name;
        WriteIndex();
    }

    public void Delete(string id)
    {
        File.Delete(PathOf(id));
        _index.Notebooks.RemoveAll(entry => entry.Id == id);
        if (_index.LastOpen == id)
        {
            _index.LastOpen = null;
            HasChanges = false;
        }

        WriteIndex();
    }

    private NotebookIndex ReadIndex()
    {
        try
        {
            return JsonSerializer.Deserialize<NotebookIndex>(File.ReadAllText(Path.Combine(folder, IndexFileName)), JsonFormat.Indented) ?? new NotebookIndex();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new NotebookIndex();
        }
    }

    private void WriteIndex()
    {
        Directory.CreateDirectory(folder);
        AtomicFile.Write(Path.Combine(folder, IndexFileName), JsonSerializer.Serialize(_index, JsonFormat.Indented));
    }
}
