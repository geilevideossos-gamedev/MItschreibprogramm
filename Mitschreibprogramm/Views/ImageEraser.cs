using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// EraseByStroke only knows strokes. The eraser (tool, or the pen's lower button set to "Erase") removes a whole picture
// as soon as it touches it, the way it removes a whole stroke, one undo step per picture.
public sealed class ImageEraser
{
    // Half the size of InkCanvas's default eraser shape.
    private const double Reach = 4;

    private readonly DocumentView _document;

    public ImageEraser(DocumentView document)
    {
        _document = document;
        document.PageAdded += Attach;
    }

    private void Attach(PageView page)
    {
        var ink = page.Ink;
        ink.PreviewStylusDown += (_, e) => Erase(page, e.Inverted, e.GetStylusPoints(ink).Select(point => point.ToPoint()));
        ink.PreviewStylusMove += (_, e) => Erase(page, e.Inverted, e.GetStylusPoints(ink).Select(point => point.ToPoint()));
        ink.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (e.StylusDevice is null)
            {
                Erase(page, inverted: false, [e.GetPosition(ink)]);
            }
        };
        ink.PreviewMouseMove += (_, e) =>
        {
            if (e.StylusDevice is null && e.LeftButton == MouseButtonState.Pressed)
            {
                Erase(page, inverted: false, [e.GetPosition(ink)]);
            }
        };
    }

    private void Erase(PageView page, bool inverted, IEnumerable<Point> points)
    {
        var ink = page.Ink;
        var erasing = ink.ActiveEditingMode == InkCanvasEditingMode.EraseByStroke
            || (inverted && ink.EditingModeInverted == InkCanvasEditingMode.EraseByStroke);
        if (!erasing)
        {
            return;
        }

        var touched = points.ToList();
        var children = ink.Children;
        foreach (var image in children.OfType<Image>().Where(image => Touches(image, touched)).ToList())
        {
            var index = children.IndexOf(image);
            children.Remove(image);
            _document.Record(new UndoStep(() => children.Insert(index, image), () => children.Remove(image)));
            DebugLog.Write($"image erased page={_document.Pages.ToList().IndexOf(page) + 1}");
        }
    }

    private static bool Touches(Image image, List<Point> points)
    {
        var bounds = PageImage.Bounds(image);
        bounds.Inflate(Reach, Reach);
        return points.Any(bounds.Contains);
    }
}
