using System.IO;
using System.Text;
using System.Text.Json;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

public static class MspFileService
{
    private const double DefaultPressure = 0.5;

    public static void Save(NoteDocument document, string path)
    {
        // Written next to the target and moved over it, so a crash never leaves a half-written note behind.
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(document, JsonFormat.Compact), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    public static NoteDocument Load(string path)
    {
        var document = JsonSerializer.Deserialize<NoteDocument>(File.ReadAllText(path, Encoding.UTF8), JsonFormat.Compact)
            ?? throw new InvalidDataException("Die Datei ist leer.");
        if (document.Version > AppConstants.FileFormatVersion)
        {
            throw new InvalidDataException($"Die Datei hat Formatversion {document.Version}, dieses Programm kennt nur Version {AppConstants.FileFormatVersion}.");
        }

        foreach (var stroke in document.Pages.SelectMany(page => page.Strokes))
        {
            stroke.Width = Math.Clamp(stroke.Width, AppConstants.MinStrokeWidth, AppConstants.MaxStrokeWidth);
            stroke.Points = stroke.Points.Where(point => point.Length >= 2).Select(Normalize).ToList();
        }

        return document;
    }

    private static double[] Normalize(double[] point) =>
        [point[0], point[1], Math.Clamp(point.Length > 2 ? point[2] : DefaultPressure, 0, 1)];
}
