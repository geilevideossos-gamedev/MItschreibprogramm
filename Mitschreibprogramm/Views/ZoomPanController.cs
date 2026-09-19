using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

public sealed class ZoomPanController
{
    private readonly ScrollViewer _scroller;
    private readonly DocumentView _document;
    private bool _spaceHeld;
    private Point? _panOrigin;
    private Point _panStartOffset;

    public ZoomPanController(ScrollViewer scroller, DocumentView document)
    {
        _scroller = scroller;
        _document = document;
        scroller.PreviewMouseWheel += OnMouseWheel;
        scroller.PreviewMouseDown += OnMouseDown;
        scroller.PreviewMouseMove += OnMouseMove;
        scroller.PreviewMouseUp += OnMouseUp;
        scroller.LostMouseCapture += (_, _) => _panOrigin = null;
    }

    public event Action? ZoomChanged;

    public void ZoomIn() =>
        ZoomTo(AppConstants.ZoomSteps.FirstOrDefault(step => step > _document.Zoom * (1 + AppConstants.ZoomStepTolerance), AppConstants.MaxZoom));

    public void ZoomOut() =>
        ZoomTo(AppConstants.ZoomSteps.LastOrDefault(step => step < _document.Zoom * (1 - AppConstants.ZoomStepTolerance), AppConstants.MinZoom));

    public void ResetZoom() => ZoomTo(AppConstants.DefaultZoom);

    public void SetSpaceHeld(bool held)
    {
        if (_spaceHeld == held)
        {
            return;
        }

        _spaceHeld = held;
        _document.SetPanning(held);
        _scroller.Cursor = held ? Cursors.Hand : null;
        _scroller.ForceCursor = held;
    }

    private void ZoomTo(double zoom)
    {
        var mouse = Mouse.GetPosition(_scroller);
        var inside = mouse.X >= 0 && mouse.Y >= 0 && mouse.X <= _scroller.ViewportWidth && mouse.Y <= _scroller.ViewportHeight;
        ZoomAround(zoom, inside ? mouse : new Point(_scroller.ViewportWidth / 2, _scroller.ViewportHeight / 2));
    }

    private void ZoomAround(double zoom, Point anchor)
    {
        zoom = Math.Clamp(zoom, AppConstants.MinZoom, AppConstants.MaxZoom);
        if (zoom == _document.Zoom)
        {
            return;
        }

        var fixedPoint = _scroller.TranslatePoint(anchor, _document);
        _document.Zoom = zoom;
        _scroller.UpdateLayout();
        var moved = _document.TranslatePoint(fixedPoint, _scroller);
        _scroller.ScrollToHorizontalOffset(_scroller.HorizontalOffset + moved.X - anchor.X);
        _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset + moved.Y - anchor.Y);
        ZoomChanged?.Invoke();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
            ZoomAround(_document.Zoom * Math.Pow(AppConstants.WheelZoomFactor, notches), e.GetPosition(_scroller));
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _scroller.ScrollToHorizontalOffset(_scroller.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var pans = e.ChangedButton == MouseButton.Middle || (_spaceHeld && e.ChangedButton == MouseButton.Left);
        if (!pans || !_scroller.CaptureMouse())
        {
            return;
        }

        _panOrigin = e.GetPosition(_scroller);
        _panStartOffset = new Point(_scroller.HorizontalOffset, _scroller.VerticalOffset);
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_panOrigin is not { } origin)
        {
            return;
        }

        var position = e.GetPosition(_scroller);
        _scroller.ScrollToHorizontalOffset(_panStartOffset.X - (position.X - origin.X));
        _scroller.ScrollToVerticalOffset(_panStartOffset.Y - (position.Y - origin.Y));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_panOrigin is not null)
        {
            _scroller.ReleaseMouseCapture();
            e.Handled = true;
        }
    }
}
