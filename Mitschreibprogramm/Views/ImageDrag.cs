using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mitschreibprogramm.Views;

// When a single picture is the whole selection, InkCanvas lets clicks inside it through to the picture (meant for
// text boxes) and only moves it by its frame. Dragging the middle of the picture would start a new lasso, so that
// drag is done here, for pen and mouse.
public sealed class ImageDrag
{
    private readonly PageView _page;
    private readonly SelectionEditor _selection;
    private Image? _image;
    private Point _start;
    private SelectionSnapshot? _before;
    private Rect _origin;

    public ImageDrag(PageView page, SelectionEditor selection)
    {
        _page = page;
        _selection = selection;
        var ink = page.Ink;
        ink.PreviewStylusDown += (_, e) => Handle(e, Begin(e.GetPosition(ink)) && ink.CaptureStylus());
        ink.PreviewStylusMove += (_, e) => Handle(e, Drag(e.GetPosition(ink)));
        ink.PreviewStylusUp += (_, e) => Handle(e, End(() => ink.ReleaseStylusCapture()));
        ink.PreviewMouseLeftButtonDown += (_, e) => Handle(e, e.StylusDevice is null && Begin(e.GetPosition(ink)) && ink.CaptureMouse());
        ink.PreviewMouseMove += (_, e) => Handle(e, e.StylusDevice is null && Drag(e.GetPosition(ink)));
        ink.PreviewMouseLeftButtonUp += (_, e) => Handle(e, e.StylusDevice is null && End(ink.ReleaseMouseCapture));
    }

    private static void Handle(RoutedEventArgs e, bool handled)
    {
        if (handled)
        {
            e.Handled = true;
        }
    }

    private bool Begin(Point position)
    {
        var ink = _page.Ink;
        if (ink.EditingMode != InkCanvasEditingMode.Select || ink.GetSelectedStrokes().Count > 0
            || ink.GetSelectedElements() is not [Image image] || !PageImage.Bounds(image).Contains(position))
        {
            return false;
        }

        (_image, _start, _origin) = (image, position, PageImage.Bounds(image));
        _before = SelectionSnapshot.Of([], [image]);
        return true;
    }

    private bool Drag(Point position)
    {
        if (_image is null)
        {
            return false;
        }

        var moved = _selection.KeepOnPage(_page, new Rect(_origin.X + position.X - _start.X, _origin.Y + position.Y - _start.Y, _origin.Width, _origin.Height));
        InkCanvas.SetLeft(_image, moved.X);
        InkCanvas.SetTop(_image, moved.Y);
        return true;
    }

    private bool End(Action release)
    {
        if (_image is not { } image || _before is not { } before)
        {
            return false;
        }

        _image = null;
        release();
        if (PageImage.Bounds(image) != _origin)
        {
            // Selecting it again moves the selection frame along.
            _page.Ink.Select(null, [image]);
            _selection.Record(_page, before, SelectionSnapshot.Of([], [image]), "moved");
        }

        return true;
    }
}
