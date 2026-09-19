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
        }

        return [surface];
    }

    // A stroke belongs to the A4 tile that contains its first point. Column 0 always yields a page per row,
    // tiles further right only exist where something was written, so nothing drawn to the right gets lost.
    private static List<NotePage> SplitSurface(NoteDocument document)
    {
        var tiles = new SortedDictionary<(int Row, int Column), NotePage>();
        foreach (var stroke in document.Pages.SelectMany(page => page.Strokes).Where(stroke => stroke.Points.Count > 0))
        {
            var row = Math.Max(0, (int)Math.Floor(stroke.Points[0][1] / AppConstants.PageHeight));
            var column = Math.Max(0, (int)Math.Floor(stroke.Points[0][0] / AppConstants.PageWidth));
            if (!tiles.TryGetValue((row, column), out var tile))
            {
                tiles[(row, column)] = tile = new NotePage();
            }

            tile.Strokes.Add(Shift(stroke, -column * AppConstants.PageWidth, -row * AppConstants.PageHeight));
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
        Points = stroke.Points.Select(point => new[] { point[0] + offsetX, point[1] + offsetY, point[2] }).ToList(),
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
