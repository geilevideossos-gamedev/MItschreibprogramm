using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Mitschreibprogramm.Views;

// Where the selected strokes and pictures are, so a move or resize can be undone. InkCanvas transforms the points of
// a stroke in place and never its pen tip, so the points alone restore a stroke.
public sealed class SelectionSnapshot
{
    private readonly List<(Stroke Stroke, StylusPointCollection Points)> _strokes;
    private readonly List<(Image Image, Rect Bounds)> _images;

    private SelectionSnapshot(List<(Stroke, StylusPointCollection)> strokes, List<(Image, Rect)> images)
    {
        _strokes = strokes;
        _images = images;
    }

    public static SelectionSnapshot Of(IEnumerable<Stroke> strokes, IEnumerable<Image> images) => new(
        strokes.Select(stroke => (stroke, stroke.StylusPoints.Clone())).ToList(),
        images.Select(image => (image, PageImage.Bounds(image))).ToList());

    public void Apply()
    {
        foreach (var (stroke, points) in _strokes)
        {
            stroke.StylusPoints = points.Clone();
        }

        foreach (var (image, bounds) in _images)
        {
            InkCanvas.SetLeft(image, bounds.X);
            InkCanvas.SetTop(image, bounds.Y);
            image.Width = bounds.Width;
            image.Height = bounds.Height;
        }
    }
}
