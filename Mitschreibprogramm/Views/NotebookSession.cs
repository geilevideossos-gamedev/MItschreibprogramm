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

    public NotebookSession(Window owner, DocumentView document, ScrollViewer scroller, AppSettings settings, NotebookLibrary library)
    {
        _owner = owner;
        _document = document;
        _scroller = scroller;
        _settings = settings;
        _library = library;
        _transfer = new NotebookTransfer(owner, document, settings, library);
        document.Changed += library.MarkChanged;
        _autosave.Tick += (_, _) => Autosave();
    }

    // The list, the open notebook or its name changed.
    public event Action? Changed;

    public event Action? DocumentLoaded;

    public IReadOnlyList<NotebookEntry> Notebooks => _library.Notebooks;

    public string? ActiveId => _library.OpenId;

    public string ActiveName => ActiveId is { } id ? _library.Entry(id).Name : UntitledName;

    // True while changes could not be written; the title shows it, because the error box appears only once per failure.
    public bool SaveFailed => _saveFailed && _library.HasChanges;

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
        if (!CanLeave() || !Show(id))
        {
            Changed?.Invoke();
        }
    }

    public void SaveNow() => SaveActive();

    public void Rename(string id)
    {
        var entry = _library.Entry(id);
        if (RenameDialog.Ask(_owner, entry.Name) is { Length: > 0 } name && name != entry.Name
            && ErrorMessage.Try(_owner, () => _library.Rename(id, name), "Der Name konnte nicht gespeichert werden. Ist der Ordner schreibgeschützt?"))
        {
            Changed?.Invoke();
        }
    }

    public void Delete(string id)
    {
        var entry = _library.Entry(id);
        var wasOpen = id == ActiveId;
        if (!ConfirmDialog.Ask(_owner, "Heft löschen", $"Heft \"{entry.Name}\" löschen? Die Datei landet im Papierkorb.", "Löschen")
            || !ErrorMessage.Try(_owner, () => _library.Delete(id), "Das Heft konnte nicht gelöscht werden. Ist die Datei gesperrt?"))
        {
            return;
        }

        if (!wasOpen)
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
        if ((id ?? ActiveId) is { } target)
        {
            _transfer.ExportMsp(target);
        }
    }

    public void ExportPdf(string? id)
    {
        if ((id ?? ActiveId) is { } target)
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

    // Leaving is safe after a save, or when only the index failed; unsaved content needs an explicit decision.
    private bool CanLeave(string confirmLabel = "Trotzdem wechseln") =>
        SaveActive() || !_library.HasChanges || ConfirmDialog.Ask(_owner, "Heft nicht gespeichert",
            $"\"{ActiveName}\" konnte nicht gespeichert werden. {confirmLabel}? Die Änderungen gehen verloren. Mit MSP-Export lässt sich das Heft vorher woanders sichern.",
            confirmLabel);

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
        DocumentLoaded?.Invoke();
        var entry = _library.Entry(id);
        ErrorMessage.Try(_owner, () => _library.Open(id), "Das geöffnete Heft konnte nicht gemerkt werden. Ist der Ordner schreibgeschützt?");
        RestoreScroll(entry.ScrollX, entry.ScrollY);
        Changed?.Invoke();
        return true;
    }

    // A stroke in progress is left alone; the next tick catches up.
    private void Autosave()
    {
        if (_library.HasChanges && Mouse.LeftButton != MouseButtonState.Pressed && Stylus.CurrentStylusDevice is not { InAir: false })
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
            var origin = _scroller.TranslatePoint(new Point(0, 0), _document);
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
                ErrorMessage.Show(_owner, _library.HasChanges
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

    // Same math as the zoom around the pointer. At startup the window has no size yet, so the first restore waits for the layout.
    private void RestoreScroll(double x, double y)
    {
        if (_owner.IsLoaded)
        {
            ScrollTo(x, y);
        }
        else
        {
            _owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => ScrollTo(x, y));
        }
    }

    private void ScrollTo(double x, double y)
    {
        _scroller.UpdateLayout();
        var moved = _document.TranslatePoint(new Point(x, y), _scroller);
        _scroller.ScrollToHorizontalOffset(_scroller.HorizontalOffset + moved.X);
        _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset + moved.Y);
    }
}
