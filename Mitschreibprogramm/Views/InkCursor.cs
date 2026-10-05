using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// InkCanvas's own pen cursor is exactly as wide as the stroke, so thin pens are almost invisible. Pen and eraser get
// their own cursors; Select and None keep the ones from InkCanvas (arrow, move, resize). ActiveEditingMode already
// includes the inverted pen (lower side button) and the lasso of the upper one.
public sealed class InkCursor
{
    private readonly DocumentView _document;
    private readonly DrawingAttributes _pen;
    private Cursor? _penCursor;
    private Cursor? _eraserCursor;
    private bool _dark;
    private double _dpiScale;

    public InkCursor(DocumentView document, DrawingAttributes pen)
    {
        _document = document;
        _pen = pen;
        _dpiScale = VisualTreeHelper.GetDpi(document).DpiScaleX;
        document.PageAdded += page => page.Ink.ActiveEditingModeChanged += (_, _) => Show(page.Ink);
        document.PagesChanged += ShowAll;
        document.Loaded += (_, _) =>
        {
            SetDpiScale(VisualTreeHelper.GetDpi(document).DpiScaleX);
            if (Window.GetWindow(document) is { } window)
            {
                window.DpiChanged += (_, e) => SetDpiScale(e.NewDpi.DpiScaleX);
            }
        };
    }

    public void Update(bool dark)
    {
        _dark = dark;
        var penSize = Math.Max(AppConstants.MinCursorSize, _pen.Width * _document.Zoom);
        var eraserSize = Math.Max(AppConstants.MinCursorSize, AppConstants.EraserSize * _document.Zoom);
        var rim = Palette.CursorRim(dark);
        _penCursor = CursorImage.Dot(_pen.Color, rim, penSize, _dpiScale);
        _eraserCursor = CursorImage.Ring(Palette.PenOnPage(PenColor.Black, dark), rim, eraserSize, _dpiScale);
        ShowAll();
        DebugLog.Write($"cursor pen={penSize:0.##} eraser={eraserSize:0.##} color={_pen.Color} rim={rim} dpi={_dpiScale:0.##}");
    }

    // DpiChanged also bubbles up from every picture that gets added to a page.
    private void SetDpiScale(double dpiScale)
    {
        if (dpiScale == _dpiScale)
        {
            return;
        }

        _dpiScale = dpiScale;
        Update(_dark);
    }

    private void ShowAll()
    {
        foreach (var page in _document.Pages)
        {
            Show(page.Ink);
        }
    }

    private void Show(InkCanvas ink)
    {
        var cursor = ink.ActiveEditingMode switch
        {
            InkCanvasEditingMode.Ink => _penCursor,
            InkCanvasEditingMode.EraseByStroke => _eraserCursor,
            _ => null,
        };
        ink.UseCustomCursor = cursor is not null;
        ink.Cursor = cursor;
    }
}
