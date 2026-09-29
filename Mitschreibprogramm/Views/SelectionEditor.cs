using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// InkCanvas's own Select mode does lasso, move and resize. On top of it: one selection across all pages, undo for
// moving, resizing and deleting, a selection that stays on its A4 page, and pictures that keep their aspect ratio.
public sealed class SelectionEditor
{
    private readonly DocumentView _document;
    private PageView? _page;
    private SelectionSnapshot? _before;

    public SelectionEditor(DocumentView document)
    {
        _document = document;
        document.PageAdded += Attach;
        document.PreviewStylusDown += (_, e) => ClearFromAnotherPage(e);
        document.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (e.StylusDevice is null)
            {
                ClearFromAnotherPage(e);
            }
        };
    }

    public event Action? ActiveChanged;

    public bool Active => _page is not null;

    // InkCanvas.Select switches the canvas to Select mode; ActiveChanged lets the editing modes switch it back.
    public void Clear()
    {
        if (_page is not { } page)
        {
            return;
        }

        _page = null;
        page.Ink.Select(new StrokeCollection());
        ActiveChanged?.Invoke();
    }

    public void Delete()
    {
        if (_page is not { } page)
        {
            return;
        }

        var ink = page.Ink;
        var strokes = ink.GetSelectedStrokes().Select(stroke => (Stroke: stroke, Index: ink.Strokes.IndexOf(stroke))).OrderBy(item => item.Index).ToList();
        var images = ink.GetSelectedElements().OfType<Image>().Select(image => (Image: image, Index: ink.Children.IndexOf(image))).OrderBy(item => item.Index).ToList();
        Clear();
        void Remove()
        {
            strokes.ForEach(item => ink.Strokes.Remove(item.Stroke));
            images.ForEach(item => ink.Children.Remove(item.Image));
        }

        // Ascending indexes put every item back at its old place in the drawing order.
        void Restore()
        {
            strokes.ForEach(item => ink.Strokes.Insert(item.Index, item.Stroke));
            images.ForEach(item => ink.Children.Insert(item.Index, item.Image));
        }

        Remove();
        _document.Record(new UndoStep(Restore, Remove));
        DebugLog.Write($"selection change=deleted strokes={strokes.Count} images={images.Count}");
    }

    public void Record(PageView page, SelectionSnapshot before, SelectionSnapshot after, string change)
    {
        if (_document.Mode == PageMode.Endless)
        {
            page.GrowToFit(page.ContentBounds());
        }

        _document.Record(new UndoStep(before.Apply, after.Apply));
        if (DebugLog.IsEnabled)
        {
            // Pictures report their new size only after the next layout pass.
            page.UpdateLayout();
            var bounds = page.Ink.GetSelectionBounds();
            DebugLog.Write($"selection change={change} page={_document.Pages.ToList().IndexOf(page) + 1} rect=({bounds.X:0.#},{bounds.Y:0.#},{bounds.Width:0.#},{bounds.Height:0.#})");
        }
    }

    public Rect KeepOnPage(PageView page, Rect rect) =>
        SelectionBounds.KeepOnPage(rect, new Size(page.Width, page.Height), _document.Mode == PageMode.Endless);

    private void Attach(PageView page)
    {
        var ink = page.Ink;
        ink.SelectionChanged += (_, _) => OnSelectionChanged(page);
        ink.SelectionMoving += (_, e) => Prepare(page, e, resizing: false);
        ink.SelectionResizing += (_, e) => Prepare(page, e, resizing: true);
        ink.SelectionMoved += (_, _) => Commit(page, "moved");
        ink.SelectionResized += (_, _) => Commit(page, "resized");
        _ = new ImageDrag(page, this);
        _ = new TapSelection(page);
    }

    private void OnSelectionChanged(PageView page)
    {
        var (strokes, images) = (page.Ink.GetSelectedStrokes().Count, page.Ink.GetSelectedElements().Count);
        if (strokes + images > 0 && _page != page)
        {
            var previous = _page;
            _page = page;
            previous?.Ink.Select(new StrokeCollection());
            ActiveChanged?.Invoke();
        }
        else if (strokes + images == 0 && _page == page)
        {
            _page = null;
            ActiveChanged?.Invoke();
        }

        var bounds = page.Ink.GetSelectionBounds();
        DebugLog.Write($"selection page={_document.Pages.ToList().IndexOf(page) + 1} strokes={strokes} images={images} rect=({bounds.X:0.#},{bounds.Y:0.#},{bounds.Width:0.#},{bounds.Height:0.#})");
    }

    // Raised once when the drag ends; the corrected rectangle is what InkCanvas applies.
    private void Prepare(PageView page, InkCanvasSelectionEditingEventArgs e, bool resizing)
    {
        var rect = resizing && page.Ink.GetSelectedElements().Count > 0 ? SelectionBounds.KeepAspect(e.OldRectangle, e.NewRectangle) : e.NewRectangle;
        e.NewRectangle = KeepOnPage(page, rect);
        _before = SelectionSnapshot.Of(page.Ink.GetSelectedStrokes(), page.Ink.GetSelectedElements().OfType<Image>());
    }

    private void Commit(PageView page, string change)
    {
        if (_before is { } before)
        {
            _before = null;
            Record(page, before, SelectionSnapshot.Of(page.Ink.GetSelectedStrokes(), page.Ink.GetSelectedElements().OfType<Image>()), change);
        }
    }

    // A click on another page only ends the selection; it neither draws nor starts a new lasso there.
    private void ClearFromAnotherPage(RoutedEventArgs e)
    {
        if (_page is null)
        {
            return;
        }

        for (var node = e.OriginalSource as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is PageView page)
            {
                if (page != _page)
                {
                    Clear();
                    e.Handled = true;
                }

                return;
            }
        }
    }
}
