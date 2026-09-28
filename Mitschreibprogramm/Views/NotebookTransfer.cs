using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Import of external .msp files into the library and export of notebooks as .msp or PDF through the Windows dialogs.
public sealed class NotebookTransfer(Window owner, DocumentView document, AppSettings settings, NotebookLibrary library)
{
    private const string FileFilter = "Mitschrift (*.msp)|*.msp";
    private const string PdfFilter = "PDF (*.pdf)|*.pdf";
    private const string FallbackFileName = "Heft";
    private const string ExportError = "Das Heft konnte nicht exportiert werden. Ist die Zieldatei in einem anderen Programm geöffnet oder das Heft beschädigt?";

    // Returns the new notebook, or null when the dialog was cancelled or the file was refused.
    public NotebookEntry? Import()
    {
        var dialog = new OpenFileDialog { Filter = FileFilter, InitialDirectory = StartFolder() };
        if (dialog.ShowDialog(owner) != true)
        {
            return null;
        }

        RememberFolder(dialog.FileName);
        try
        {
            return library.Import(dialog.FileName);
        }
        catch (InvalidDataException e)
        {
            ErrorMessage.Show(owner, $"Die Datei konnte nicht importiert werden.\n\n{e.Message}");
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            ErrorMessage.Show(owner, "Die Datei konnte nicht importiert werden. Sie ist beschädigt, gesperrt oder nicht lesbar.");
        }

        return null;
    }

    public void ExportMsp(string id)
    {
        if (AskSavePath(id, FileFilter, ".msp") is { } path)
        {
            ErrorMessage.Try(owner, () => MspFileService.Save(Snapshot(id), path), ExportError);
        }
    }

    public void ExportPdf(string id)
    {
        if (ExportDialog.Ask(owner) is { } includeRuleLines && AskSavePath(id, PdfFilter, ".pdf") is { } path)
        {
            ErrorMessage.Try(owner, () => PdfExporter.Export(Snapshot(id), path, includeRuleLines), ExportError);
        }
    }

    // The open notebook is taken from the view, so it can be exported even while saving to the library fails.
    private NoteDocument Snapshot(string id) => id == library.LastOpen ? document.ToDocument() : library.LoadDocument(id);

    private string? AskSavePath(string id, string filter, string extension)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = extension,
            AddExtension = true,
            InitialDirectory = StartFolder(),
            FileName = SafeFileName(library.Entry(id).Name) + extension,
        };
        if (dialog.ShowDialog(owner) != true)
        {
            return null;
        }

        RememberFolder(dialog.FileName);
        return dialog.FileName;
    }

    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim().TrimEnd('.', ' ');
        return safe.Length > 0 ? safe : FallbackFileName;
    }

    private string StartFolder() => Directory.Exists(settings.LastFolder) ? settings.LastFolder : string.Empty;

    private void RememberFolder(string path) => settings.LastFolder = Path.GetDirectoryName(path);
}
