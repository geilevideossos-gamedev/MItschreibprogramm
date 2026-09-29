using System.IO;
using System.Text;
using System.Text.Json;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

public static class MspFileService
{
    private const double DefaultPressure = 0.5;
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static void Save(NoteDocument document, string path) =>
        AtomicFile.Write(path, JsonSerializer.Serialize(document, JsonFormat.Compact));

    public static NoteDocument Load(string path)
    {
        var document = JsonSerializer.Deserialize<NoteDocument>(File.ReadAllText(path, Encoding.UTF8), JsonFormat.Compact)
            ?? throw new InvalidDataException("Die Datei ist leer.");
        if (document.Version > AppConstants.FileFormatVersion)
        {
            throw new InvalidDataException($"Die Datei hat Formatversion {document.Version}, dieses Programm kennt nur Version {AppConstants.FileFormatVersion}.");
        }

        document.Pages = (document.Pages ?? []).Where(page => page is not null).ToList();
        foreach (var page in document.Pages)
        {
            page.Images = (page.Images ?? []).Where(IsUsable).Select(Normalize).ToList();
            page.Strokes = (page.Strokes ?? []).Where(stroke => stroke is not null).ToList();
            foreach (var stroke in page.Strokes)
            {
                stroke.Width = Math.Clamp(stroke.Width, AppConstants.MinStrokeWidth, AppConstants.MaxStrokeWidth);
                stroke.Points = (stroke.Points ?? []).Where(point => point is { Length: >= 2 }).Select(Normalize).ToList();
            }
        }

        var strokes = document.Pages.SelectMany(page => page.Strokes);
        if (!Enum.IsDefined(document.PageMode) || !Enum.IsDefined(document.PageStyle) || !Enum.IsDefined(document.LineColor)
            || strokes.Any(stroke => !Enum.IsDefined(stroke.Color)))
        {
            throw new InvalidDataException("Die Datei enthält einen unbekannten Wert für Modus, Stil oder Farbe.");
        }

        // Older files load unchanged; in memory, and when saved again, they are the current version.
        document.Version = AppConstants.FileFormatVersion;
        return document;
    }

    // A damaged picture entry is dropped like a stroke without points; the rest of the note still opens.
    private static bool IsUsable(NoteImage? image)
    {
        if (image is not { Width: > 0, Height: > 0 } || string.IsNullOrEmpty(image.Png))
        {
            return false;
        }

        var bytes = new byte[(image.Png.Length / 4 * 3) + 3];
        return Convert.TryFromBase64String(image.Png, bytes, out var length) && length >= PngSignature.Length && bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
    }

    private static NoteImage Normalize(NoteImage image) => new()
    {
        X = Math.Clamp(image.X, -AppConstants.MaxCoordinate, AppConstants.MaxCoordinate),
        Y = Math.Clamp(image.Y, -AppConstants.MaxCoordinate, AppConstants.MaxCoordinate),
        Width = Math.Min(image.Width, AppConstants.MaxCoordinate),
        Height = Math.Min(image.Height, AppConstants.MaxCoordinate),
        Png = image.Png,
    };

    private static double[] Normalize(double[] point) =>
    [
        Math.Clamp(point[0], -AppConstants.MaxCoordinate, AppConstants.MaxCoordinate),
        Math.Clamp(point[1], -AppConstants.MaxCoordinate, AppConstants.MaxCoordinate),
        Math.Clamp(point.Length > 2 ? point[2] : DefaultPressure, 0, 1),
    ];
}
