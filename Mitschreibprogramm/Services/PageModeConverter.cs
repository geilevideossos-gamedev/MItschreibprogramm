using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

public static class PageModeConverter
{
    public static NoteDocument Convert(NoteDocument document, PageMode target)
    {
        if (document.PageMode == target)
        {
            return document;
        }

        return WithPages(document, target, target == PageMode.Endless ? MergePages(document) : SplitSurface(document));
    }

    private static List<NotePage> MergePages(NoteDocument document)
    {
        var surface = new NotePage();
        for (var index = 0; index < document.Pages.Count; index++)
        {
            var offsetY = index * AppConstants.PageHeight;
            surface.Strokes.AddRange(document.Pages[index].Strokes.Select(stroke => Shift(stroke, 0, offsetY)));
            surface.Images.AddRange(document.Pages[index].Images.Select(image => Shift(image, 0, offsetY)));
        }

        return [surface];
    }

    // A stroke belongs to the A4 tile that contains its first point, a picture to the tile of its top left corner.
    // Column 0 always yields a page per row, tiles further right only exist where something was written or pasted,
    // so nothing to the right gets lost.
    private static List<NotePage> SplitSurface(NoteDocument document)
    {
        var tiles = new SortedDictionary<(int Row, int Column), NotePage>();
        NotePage Tile(double x, double y, out double offsetX, out double offsetY)
        {
            var row = Math.Max(0, (int)Math.Floor(y / AppConstants.PageHeight));
            var column = Math.Max(0, (int)Math.Floor(x / AppConstants.PageWidth));
            (offsetX, offsetY) = (-column * AppConstants.PageWidth, -row * AppConstants.PageHeight);
            if (!tiles.TryGetValue((row, column), out var tile))
            {
                tiles[(row, column)] = tile = new NotePage();
            }

            return tile;
        }

        foreach (var stroke in document.Pages.SelectMany(page => page.Strokes).Where(stroke => stroke.Points.Count > 0))
        {
            Tile(stroke.Points[0][0], stroke.Points[0][1], out var offsetX, out var offsetY).Strokes.Add(Shift(stroke, offsetX, offsetY));
        }

        foreach (var image in document.Pages.SelectMany(page => page.Images))
        {
            Tile(image.X, image.Y, out var offsetX, out var offsetY).Images.Add(Shift(image, offsetX, offsetY));
        }

        var lastRow = tiles.Count == 0 ? 0 : tiles.Keys.Max(key => key.Row);
        for (var row = 0; row <= lastRow; row++)
        {
            tiles.TryAdd((row, 0), new NotePage());
        }

        return [.. tiles.Values];
    }

    private static NoteStroke Shift(NoteStroke stroke, double offsetX, double offsetY) => new()
    {
        Color = stroke.Color,
        Width = stroke.Width,
        PressureEnabled = stroke.PressureEnabled,
        FitToCurve = stroke.FitToCurve,
        Points = stroke.Points.Select(point => new[] { point[0] + offsetX, point[1] + offsetY, point[2] }).ToList(),
    };

    private static NoteImage Shift(NoteImage image, double offsetX, double offsetY) => new()
    {
        X = image.X + offsetX,
        Y = image.Y + offsetY,
        Width = image.Width,
        Height = image.Height,
        Png = image.Png,
    };

    private static NoteDocument WithPages(NoteDocument document, PageMode mode, List<NotePage> pages) => new()
    {
        Version = document.Version,
        PageMode = mode,
        PageStyle = document.PageStyle,
        LineColor = document.LineColor,
        Pages = pages,
    };
}
