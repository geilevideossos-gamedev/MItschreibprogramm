using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public sealed class FileSession
{
    private const string FileFilter = "Mitschrift (*.msp)|*.msp";
    private const string UntitledName = "Unbenannt";

    private readonly Window _owner;
    private readonly DocumentView _document;
    private readonly AppSettings _settings;

    public FileSession(Window owner, DocumentView document, AppSettings settings)
    {
        _owner = owner;
        _document = document;
        _settings = settings;
        document.Changed += () => SetState(FilePath, dirty: true);
    }

    public event Action? StateChanged;

    public event Action? DocumentLoaded;

    public string? FilePath { get; private set; }

    public bool IsDirty { get; private set; }

    public string DisplayName => FilePath is null ? UntitledName : Path.GetFileName(FilePath);

    public void New()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        Show(new NoteDocument { PageMode = _settings.PageMode, PageStyle = _settings.PageStyle, LineColor = _settings.LineColor }, null);
    }

    public void Open()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        var dialog = new OpenFileDialog { Filter = FileFilter, InitialDirectory = StartFolder() };
        if (dialog.ShowDialog(_owner) != true)
        {
            return;
        }

        try
        {
            Show(MspFileService.Load(dialog.FileName), dialog.FileName);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            ShowError($"Die Datei konnte nicht geöffnet werden.\n\n{e.Message}");
        }
    }

    public bool Save() => FilePath is null ? SaveAs() : Write(FilePath);

    public bool SaveAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            DefaultExt = ".msp",
            AddExtension = true,
            InitialDirectory = StartFolder(),
            FileName = DisplayName,
        };
        return dialog.ShowDialog(_owner) == true && Write(dialog.FileName);
    }

    public bool ConfirmDiscard()
    {
        if (!IsDirty)
        {
            return true;
        }

        return UnsavedChangesDialog.Ask(_owner, DisplayName) switch
        {
            UnsavedChoice.Save => Save(),
            UnsavedChoice.Discard => true,
            _ => false,
        };
    }

    public string StartFolder() => Directory.Exists(_settings.LastFolder) ? _settings.LastFolder : string.Empty;

    private void Show(NoteDocument document, string? path)
    {
        _document.Load(document);
        SetState(path, dirty: false);
        DocumentLoaded?.Invoke();
    }

    private bool Write(string path)
    {
        try
        {
            MspFileService.Save(_document.ToDocument(), path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ShowError($"Die Datei konnte nicht gespeichert werden.\n\n{e.Message}");
            return false;
        }

        SetState(path, dirty: false);
        return true;
    }

    private void SetState(string? path, bool dirty)
    {
        if (path is not null)
        {
            _settings.LastFolder = Path.GetDirectoryName(path);
        }

        if (path == FilePath && dirty == IsDirty)
        {
            return;
        }

        FilePath = path;
        IsDirty = dirty;
        StateChanged?.Invoke();
    }

    private void ShowError(string message) =>
        MessageBox.Show(_owner, message, "Mitschreibprogramm", MessageBoxButton.OK, MessageBoxImage.Warning);
}
