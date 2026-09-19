using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public sealed class DocumentView : StackPanel
{
    private readonly DrawingAttributes _pen = new() { FitToCurve = true };
    private readonly List<PageView> _pages = [];
    private readonly UndoHistory _history = new();
    private PageStyle _pageStyle;
    private LineColor _lineColor;
    private bool _eraser;
    private bool _barrelHeld;

    public DocumentView()
    {
        AddPage();
        PreviewStylusInRange += (_, e) => SyncBarrel(e.StylusDevice);
        PreviewStylusInAirMove += (_, e) => SyncBarrel(e.StylusDevice);
        PreviewStylusButtonDown += (_, e) => SyncBarrel(e.StylusDevice);
        PreviewStylusButtonUp += (_, e) => SyncBarrel(e.StylusDevice);
        PreviewStylusDown += (_, e) => SyncBarrel(e.StylusDevice);
        PreviewStylusOutOfRange += (_, _) => SetBarrelHeld(false);
        if (DebugLog.IsEnabled)
        {
            _ = new StrokeLogger(this);
        }
    }

    public event Action<PageView>? PageAdded;

    public IReadOnlyList<PageView> Pages => _pages;

    public void SetPenColor(PenColor color) => _pen.Color = Palette.Pen(color);

    public void SetPenWidth(double width)
    {
        _pen.Width = width;
        _pen.Height = width;
    }

    public void SetPressureEnabled(bool enabled) => _pen.IgnorePressure = !enabled;

    public void SetEraser(bool eraser)
    {
        _eraser = eraser;
        UpdateEditingMode();
    }

    public void SetPageStyle(PageStyle style, LineColor lineColor)
    {
        _pageStyle = style;
        _lineColor = lineColor;
        foreach (var page in _pages)
        {
            page.Paper.Update(style, lineColor);
        }
    }

    public void Undo() => OnHistoryApplied("undo", _history.Undo());

    public void Redo() => OnHistoryApplied("redo", _history.Redo());

    private void AddPage()
    {
        var page = new PageView(_pen);
        page.Paper.Update(_pageStyle, _lineColor);
        page.Ink.StrokeCollected += (_, e) => OnStrokeCollected(page.Ink.Strokes, e.Stroke);
        page.Ink.StrokeErasing += (_, e) => OnStrokeErasing(page.Ink.Strokes, e.Stroke);
        _pages.Add(page);
        Children.Add(page);
        UpdateEditingMode();
        PageAdded?.Invoke(page);
    }

    private void OnHistoryApplied(string kind, bool applied)
    {
        if (applied && DebugLog.IsEnabled)
        {
            DebugLog.Write($"{kind} strokes={_pages.Sum(page => page.Ink.Strokes.Count)}");
        }
    }

    private void OnStrokeCollected(StrokeCollection strokes, Stroke stroke) =>
        _history.Push(new UndoStep(() => strokes.Remove(stroke), () => strokes.Add(stroke)));

    // Raised before the stroke leaves the collection, so its z-order position is still known.
    private void OnStrokeErasing(StrokeCollection strokes, Stroke stroke)
    {
        var index = strokes.IndexOf(stroke);
        _history.Push(new UndoStep(() => strokes.Insert(index, stroke), () => strokes.Remove(stroke)));
    }

    // The driver reports a held side button as barrel button; an inverted pen is handled by InkCanvas itself
    // (EditingModeInverted defaults to EraseByStroke).
    private void SyncBarrel(StylusDevice device)
    {
        var held = device.StylusButtons.Any(button =>
            button.Guid == StylusPointProperties.BarrelButton.Id &&
            button.StylusButtonState == StylusButtonState.Down);
        SetBarrelHeld(held);
    }

    private void SetBarrelHeld(bool held)
    {
        if (_barrelHeld == held)
        {
            return;
        }

        _barrelHeld = held;
        UpdateEditingMode();
    }

    private void UpdateEditingMode()
    {
        var mode = _eraser || _barrelHeld ? InkCanvasEditingMode.EraseByStroke : InkCanvasEditingMode.Ink;
        foreach (var page in _pages)
        {
            page.Ink.EditingMode = mode;
        }
    }
}
