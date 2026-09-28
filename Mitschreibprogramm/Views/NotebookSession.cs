using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Keeps one notebook of the library open in the DocumentView: switching, autosave and the scroll position per notebook.
public sealed class NotebookSession
{
    private const string UntitledName = "Unbenannt";

    private readonly Window _owner;
    private readonly DocumentView _document;
    private readonly ScrollViewer _scroller;
    private readonly AppSettings _settings;
    private readonly NotebookLibrary _library;
    private readonly NotebookTransfer _transfer;
    private readonly DispatcherTimer _autosave = new() { Interval = TimeSpan.FromSeconds(AppConstants.AutosaveIntervalSeconds) };
    private bool _saving;
    private bool _saveFailed;
    private bool _orphanChanges;

    public NotebookSession(Window owner, DocumentView document, ScrollViewer scroller, AppSettings settings, NotebookLibrary library)
    {
        _owner = owner;
        _document = document;
        _scroller = scroller;
        _settings = settings;
        _library = library;
        _transfer = new NotebookTransfer(owner, document, settings, library);
        document.Changed += OnDocumentChanged;
        _autosave.Tick += (_, _) => Autosave();
    }

    // The list, the open notebook or its name changed.
    public event Action? Changed;

    public event Action? DocumentLoaded;

    public IReadOnlyList<NotebookEntry> Notebooks => _library.Notebooks;

    public string? ActiveId => _library.OpenId;

    public string ActiveName => ActiveId is { } id ? _library.Entry(id).Name : UntitledName;

    // True while changes could not be written; the title shows it, because the error box appears only once per failure.
    public bool SaveFailed => _saveFailed && HasUnsavedChanges;

    // The last open notebook first, then the others newest first, and a fresh one only when none can be read.
    public void Start()
    {
        _library.Load();
        var candidates = _library.Notebooks.Select(entry => entry.Id).OrderBy(id => id == _library.LastOpen ? 0 : 1);
        if (!candidates.Any(Show))
        {
            CreateAndOpen();
        }

        _autosave.Start();
    }

    public void New()
    {
        if (CanLeave())
        {
            CreateAndOpen();
        }
    }

    public void Open(string id)
    {
        if (id == ActiveId)
        {
            return;
        }

        // Without a switch the panel still has to fall back to the notebook that is really open.
        if (Known(id) is null || !CanLeave() || !Show(id))
        {
            Changed?.Invoke();
        }
    }

    public void SaveNow() => SaveActive();

    public void Rename(string id)
    {
        if (Known(id) is { } entry && RenameDialog.Ask(_owner, entry.Name) is { Length: > 0 } name && name != entry.Name
            && ErrorMessage.Try(_owner, () => _library.Rename(id, name), "Der Name konnte nicht gespeichert werden. Ist der Ordner schreibgeschützt?"))
        {
            Changed?.Invoke();
        }
    }

    public void Delete(string id)
    {
        if (Known(id) is not { } entry
            || !ConfirmDialog.Ask(_owner, "Heft löschen", $"Heft \"{entry.Name}\" löschen? Die Datei landet im Papierkorb.", "Löschen"))
        {
            return;
        }

        var wasOpen = id == ActiveId;
        ErrorMessage.Try(_owner, () => _library.Delete(id), "Das Heft konnte nicht gelöscht werden. Ist die Datei gesperrt?");
        // The file may be gone although the index could not be written; the view follows what the library really holds.
        if (_library.Find(id) is not null || !wasOpen)
        {
            Changed?.Invoke();
        }
        else if (_library.Notebooks.FirstOrDefault()?.Id is not { } next || !Show(next))
        {
            CreateAndOpen();
        }
    }

    public void Import()
    {
        if (CanLeave() && _transfer.Import() is { } entry)
        {
            Show(entry.Id);
        }
    }

    public void ExportMsp(string? id)
    {
        if ((id ?? ActiveId) is { } target && Known(target) is not null)
        {
            _transfer.ExportMsp(target);
        }
    }

    public void ExportPdf(string? id)
    {
        if ((id ?? ActiveId) is { } target && Known(target) is not null)
        {
            _transfer.ExportPdf(target);
        }
    }

    // Saves before the window closes. Returns false to keep the app open when the notebook could not be written.
    public bool TryClose()
    {
        _autosave.Stop();
        if (CanLeave("Trotzdem beenden"))
        {
            return true;
        }

        _autosave.Start();
        return false;
    }

    private bool HasUnsavedChanges => _library.HasChanges || _orphanChanges;

    // Strokes drawn while no notebook could be opened are not lost: they become a notebook as soon as writing works again.
    private void OnDocumentChanged()
    {
        if (ActiveId is null)
        {
            _orphanChanges = true;
        }
        else
        {
            _library.MarkChanged();
        }
    }

    // Leaving is safe after a save, or when only the index failed; unsaved content needs an explicit decision.
    private bool CanLeave(string confirmLabel = "Trotzdem wechseln") =>
        SaveActive() || !HasUnsavedChanges || ConfirmDialog.Ask(_owner, "Heft nicht gespeichert",
            $"\"{ActiveName}\" konnte nicht gespeichert werden. {confirmLabel}? Die Änderungen gehen verloren. Mit MSP-Export lässt sich das Heft vorher woanders sichern.",
            confirmLabel);

    // An id the panel still shows but the library no longer knows: the panel is rebuilt instead.
    private NotebookEntry? Known(string id)
    {
        var entry = _library.Find(id);
        if (entry is null)
        {
            Changed?.Invoke();
        }

        return entry;
    }

    private void CreateAndOpen()
    {
        var document = new NoteDocument { PageMode = _settings.PageMode, PageStyle = _settings.PageStyle, LineColor = _settings.LineColor };
        ErrorMessage.Try(_owner, () => Show(_library.Create(UntitledName, document).Id), "Das Heft konnte nicht angelegt werden. Ist der Ordner schreibgeschützt?");
    }

    // Loads a notebook into the view; the previous one has been saved by the caller.
    private bool Show(string id)
    {
        NoteDocument document;
        try
        {
            document = _library.LoadDocument(id);
        }
        catch (InvalidDataException e)
        {
            ErrorMessage.Show(_owner, $"Das Heft konnte nicht geöffnet werden.\n\n{e.Message}");
            return false;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            ErrorMessage.Show(_owner, "Das Heft konnte nicht geöffnet werden. Die Datei ist beschädigt, gesperrt oder nicht lesbar.");
            return false;
        }

        _document.Load(document);
        _orphanChanges = false;
        DocumentLoaded?.Invoke();
        var entry = _library.Entry(id);
        ErrorMessage.Try(_owner, () => _library.Open(id), "Das geöffnete Heft konnte nicht gemerkt werden. Ist der Ordner schreibgeschützt?");
        ScrollPosition.Restore(_owner, _scroller, _document, entry.ScrollX, entry.ScrollY);
        Changed?.Invoke();
        return true;
    }

    // A stroke in progress is left alone; the next tick catches up.
    private void Autosave()
    {
        if (HasUnsavedChanges && Mouse.LeftButton != MouseButtonState.Pressed && Stylus.CurrentStylusDevice is not { InAir: false })
        {
            SaveActive();
        }
    }

    // The error box appears once per failure, not on every tick; the title keeps showing the state until a save succeeds.
    private bool SaveActive()
    {
        if (_saving)
        {
            return false;
        }

        _saving = true;
        try
        {
            if (ActiveId is null && _orphanChanges)
            {
                _library.Open(_library.Create(UntitledName, _document.ToDocument()).Id);
                _orphanChanges = false;
                Changed?.Invoke();
            }

            var origin = ScrollPosition.Capture(_scroller, _document);
            if (_library.Save(_document.ToDocument, origin.X, origin.Y))
            {
                Changed?.Invoke();
            }

            if (_saveFailed)
            {
                _saveFailed = false;
                Changed?.Invoke();
            }

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (!_saveFailed)
            {
                _saveFailed = true;
                Changed?.Invoke();
                // Without changes left the notebook itself is on disk and only index.json (names, scroll positions) failed.
                ErrorMessage.Show(_owner, HasUnsavedChanges
                    ? "Das Heft konnte nicht gespeichert werden. Ist der Ordner schreibgeschützt oder die Festplatte voll?"
                    : "Die Heftliste konnte nicht gespeichert werden. Ist der Ordner schreibgeschützt oder die Festplatte voll?");
            }

            return false;
        }
        finally
        {
            _saving = false;
        }
    }
}
