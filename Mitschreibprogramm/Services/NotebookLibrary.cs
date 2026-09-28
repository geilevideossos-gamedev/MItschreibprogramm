using System.IO;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

// One folder holds every notebook as <id>.msp plus index.json with names, timestamps and scroll positions (docs/file-format.md).
public sealed class NotebookLibrary(string folder, TimeProvider? time = null, Action<string>? deleteFile = null)
{
    private const string IndexFileName = "index.json";
    private const string Extension = ".msp";
    private const string RecoveredName = "Wiederhergestellt";

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Action<string> _deleteFile = deleteFile ?? RecycleFile;
    private NotebookIndex _index = new();

    public IReadOnlyList<NotebookEntry> Notebooks => _index.Notebooks
        .OrderByDescending(entry => entry.Modified)
        .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    // The notebook the index remembers from the last run; the notebook this instance has actually opened is OpenId.
    public string? LastOpen => _index.LastOpen;

    public string? OpenId { get; private set; }

    public bool HasChanges { get; private set; }

    public string PathOf(string id) => Path.Combine(folder, id + Extension);

    public NotebookEntry Entry(string id) => Find(id) ?? throw new InvalidOperationException($"Unknown notebook '{id}'.");

    public NotebookEntry? Find(string id) => _index.Notebooks.FirstOrDefault(entry => entry.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    // True for any path inside the library, so an export can never overwrite a notebook or the index.
    public bool Contains(string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    // Entries without a file are dropped and files without an entry are adopted, so a damaged index never hides a notebook.
    public void Load()
    {
        _index = ReadIndex();
        OpenId = null;
        HasChanges = false;
        var files = ListNotebookFiles();
        var ids = files.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = (_index.Notebooks ?? [])
            .Where(entry => entry is { Id.Length: > 0 } && ids.Contains(entry.Id))
            .DistinctBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var file in files)
        {
            var id = Path.GetFileNameWithoutExtension(file);
            if (entries.All(entry => !entry.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                entries.Add(new NotebookEntry { Id = id, Name = RecoveredNameFor(file), Modified = File.GetLastWriteTimeUtc(file) });
            }
        }

        foreach (var entry in entries)
        {
            entry.Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Id : entry.Name;
            entry.ScrollX = Sane(entry.ScrollX);
            entry.ScrollY = Sane(entry.ScrollY);
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
        OpenId = Entry(id).Id;
        _index.LastOpen = OpenId;
        HasChanges = false;
        WriteIndex();
    }

    // The sort key moves at the first change, not at the save, so the open notebook is already on top before the list is rebuilt.
    public void MarkChanged()
    {
        if (OpenId is { } id)
        {
            HasChanges = true;
            Entry(id).Modified = _time.GetUtcNow();
        }
    }

    // Autosave of the open notebook: the file is written only after a change, the scroll position is recorded every time.
    public bool Save(Func<NoteDocument> document, double scrollX, double scrollY)
    {
        if (OpenId is not { } id)
        {
            return false;
        }

        var entry = Entry(id);
        var written = HasChanges;
        if (written)
        {
            Directory.CreateDirectory(folder);
            MspFileService.Save(document(), PathOf(id));
            HasChanges = false;
        }

        (entry.ScrollX, entry.ScrollY) = (scrollX, scrollY);
        WriteIndex();
        return written;
    }

    // Memory follows the disk: a name the index could not take is rolled back.
    public void Rename(string id, string name)
    {
        var entry = Entry(id);
        var previous = entry.Name;
        entry.Name = name;
        try
        {
            WriteIndex();
        }
        catch
        {
            entry.Name = previous;
            throw;
        }
    }

    public void Delete(string id)
    {
        _deleteFile(PathOf(id));
        _index.Notebooks.RemoveAll(entry => entry.Id == id);
        if (OpenId == id)
        {
            OpenId = null;
            HasChanges = false;
        }

        if (_index.LastOpen == id)
        {
            _index.LastOpen = null;
        }

        WriteIndex();
    }

    // The recycle bin keeps a confirmed click on the wrong notebook from being the end of a subject.
    private static void RecycleFile(string path) =>
        FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

    // A folder that cannot be listed counts as empty; the app still starts and reports the problem at the first save.
    private List<string> ListNotebookFiles()
    {
        try
        {
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder).Where(file => Path.GetExtension(file).Equals(Extension, StringComparison.OrdinalIgnoreCase)).ToList()
                : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    // A file that carries one of the app's own ids as name was written by the app; the id would say nothing to the user.
    private static string RecoveredNameFor(string file)
    {
        var stem = Path.GetFileNameWithoutExtension(file);
        return Guid.TryParseExact(stem, "N", out _) ? $"{RecoveredName} {File.GetLastWriteTime(file):dd.MM.yyyy HH:mm}" : stem;
    }

    // A hand-edited index must not crash the next write (Infinity is not JSON) or push the view off the page.
    private static double Sane(double value) =>
        double.IsFinite(value) && Math.Abs(value) <= AppConstants.MaxCoordinate ? value : 0;

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
