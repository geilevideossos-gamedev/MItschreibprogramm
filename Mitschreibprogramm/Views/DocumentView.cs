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

    private void AddPage()
    {
        var page = new PageView(_pen);
        _pages.Add(page);
        Children.Add(page);
        UpdateEditingMode();
        PageAdded?.Invoke(page);
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
