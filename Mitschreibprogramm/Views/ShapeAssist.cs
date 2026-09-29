using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Decides whether a finished stroke becomes a shape: always while "Formen" is on, otherwise when the pen rested at its end.
public sealed class ShapeAssist
{
    private readonly HoldDetector _hold = new(AppConstants.ShapeHoldTolerance, AppConstants.ShapeHoldMilliseconds);
    private readonly DocumentView _document;
    private int _endTime;

    public ShapeAssist(DocumentView source)
    {
        _document = source;
        source.PreviewStylusDown += (_, e) => Start(e.GetPosition(null), e.Timestamp);
        source.PreviewStylusMove += (_, e) => Move(e.GetPosition(null), e.Timestamp);
        source.PreviewStylusUp += (_, e) => _endTime = e.Timestamp;
        // A pen also raises promoted mouse events; only the real mouse is tracked through them.
        source.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (e.StylusDevice is null)
            {
                Start(e.GetPosition(null), e.Timestamp);
            }
        };
        source.PreviewMouseMove += (_, e) =>
        {
            if (e.StylusDevice is null && e.LeftButton == MouseButtonState.Pressed)
            {
                Move(e.GetPosition(null), e.Timestamp);
            }
        };
        source.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (e.StylusDevice is null)
            {
                _endTime = e.Timestamp;
            }
        };
    }

    public bool AlwaysOn { get; set; }

    // Replaces a freshly collected stroke by its shape and returns what is on the page now. The replacement is its own
    // undo step after the one that added the stroke, so the first undo brings back the freehand stroke.
    public Stroke Apply(StrokeCollection strokes, Stroke freehand)
    {
        if (ShapeFor(freehand, _document.Zoom) is not { } shape)
        {
            return freehand;
        }

        strokes.Replace(freehand, new StrokeCollection { shape });
        _document.Record(new UndoStep(
            () => strokes.Replace(shape, new StrokeCollection { freehand }),
            () => strokes.Replace(freehand, new StrokeCollection { shape })));
        return shape;
    }

    // Colour and width come from the stroke itself (the current pen), the points carry the default pressure, and
    // there is no curve fitting that would round the corners.
    private Stroke? ShapeFor(Stroke stroke, double zoom)
    {
        var trigger = AlwaysOn ? "toggle" : _hold.HeldAt(_endTime) ? "hold" : null;
        if (trigger is null)
        {
            return null;
        }

        var shape = ShapeRecognizer.Recognize(stroke.StylusPoints.Select(point => (point.X, point.Y)).ToList(), AppConstants.MinShapeSize / zoom);
        DebugLog.Write($"shape trigger={trigger} result={(shape is null ? "none" : shape.Kind.ToString().ToLowerInvariant())} points={shape?.Outline.Count ?? 0}");
        if (shape is null)
        {
            return null;
        }

        var attributes = stroke.DrawingAttributes.Clone();
        attributes.FitToCurve = false;
        return new Stroke(new StylusPointCollection(shape.Outline.Select(point => new StylusPoint(point.X, point.Y))), attributes);
    }

    private void Start(Point position, int time) => _hold.Start(position.X, position.Y, time);

    private void Move(Point position, int time) => _hold.Move(position.X, position.Y, time);
}
