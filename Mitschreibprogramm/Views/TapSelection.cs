using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

// A pen tap that wanders a few units can end in Select mode as a tiny lasso that selects nothing (seen with the barrel
// button held, docs/status.md). Once InkCanvas has had its turn and left the selection as it was, such a tap does what
// InkCanvas does for a mouse click: select the topmost stroke within reach, otherwise the topmost picture, and end the
// selection when the tap lands beside it.
public sealed class TapSelection
{
    // The diameter InkCanvas hit-tests strokes with on a tap, and the margin of its selection frame.
    private const double StrokeReach = 5;
    private const double FrameMargin = 8;

    private readonly InkCanvas _ink;
    private Point _start;
    private Point _startOnWindow;
    private Stroke[] _strokesBefore = [];
    private UIElement[] _elementsBefore = [];
    private bool _tapping;

    public TapSelection(PageView page)
    {
        _ink = page.Ink;
        _ink.PreviewStylusDown += (_, e) => Begin(e.GetPosition(_ink), e.GetPosition(null));
        _ink.PreviewStylusMove += (_, e) => _tapping &= (e.GetPosition(null) - _startOnWindow).Length <= AppConstants.TapTolerance;
        _ink.PreviewStylusUp += (_, _) => End();
    }

    private void Begin(Point onPage, Point onWindow)
    {
        _tapping = _ink.ActiveEditingMode == InkCanvasEditingMode.Select;
        (_start, _startOnWindow) = (onPage, onWindow);
        (_strokesBefore, _elementsBefore) = ([.. _ink.GetSelectedStrokes()], [.. _ink.GetSelectedElements()]);
    }

    // Background priority runs after InkCanvas has handled the tap, including mouse input promoted from the pen.
    private void End()
    {
        if (_tapping)
        {
            _tapping = false;
            var point = _start;
            _ink.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => SelectAt(point));
        }
    }

    private void SelectAt(Point point)
    {
        var unchanged = _ink.GetSelectedStrokes().SequenceEqual(_strokesBefore) && _ink.GetSelectedElements().SequenceEqual(_elementsBefore);
        if (_ink.ActiveEditingMode != InkCanvasEditingMode.Select || !unchanged || OnSelection(point))
        {
            return;
        }

        var strokes = _ink.Strokes.HitTest(point, StrokeReach);
        if (strokes.Count > 0)
        {
            _ink.Select(new StrokeCollection { strokes[^1] });
        }
        else if (_ink.Children.OfType<Image>().LastOrDefault(image => PageImage.Bounds(image).Contains(point)) is { } image)
        {
            _ink.Select(null, [image]);
        }
        else if (_strokesBefore.Length + _elementsBefore.Length > 0)
        {
            _ink.Select(new StrokeCollection());
        }
    }

    private bool OnSelection(Point point)
    {
        var frame = _ink.GetSelectionBounds();
        if (frame.IsEmpty)
        {
            return false;
        }

        frame.Inflate(FrameMargin, FrameMargin);
        return frame.Contains(point);
    }
}
