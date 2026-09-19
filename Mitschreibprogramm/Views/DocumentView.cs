using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public sealed class DocumentView : StackPanel
{
    private readonly DrawingAttributes _pen = new() { FitToCurve = true };
    private readonly List<PageView> _pages = [];
    private readonly UndoHistory _history = new();
    private readonly SideButtonWatcher _sideButton;
    private double _zoom = AppConstants.DefaultZoom;
    private bool _eraser;
    private bool _panning;

    public DocumentView()
    {
        _sideButton = new SideButtonWatcher(this);
        _sideButton.Changed += UpdateEditingMode;
        if (DebugLog.IsEnabled)
        {
            _ = new StrokeLogger(this);
        }

        Load(new NoteDocument());
    }

    public event Action<PageView>? PageAdded;

    public event Action? PagesChanged;

    public event Action? Changed;

    public IReadOnlyList<PageView> Pages => _pages;

    public PageMode Mode { get; private set; }

    public PageStyle PageStyle { get; private set; }

    public LineColor LineColor { get; private set; }

    public double Zoom
    {
        get => _zoom;
        set
        {
            _zoom = value;
            LayoutTransform = new ScaleTransform(value, value);
        }
    }

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

    public void SetPanning(bool panning)
    {
        _panning = panning;
        UpdateEditingMode();
    }

    public void SetPageStyle(PageStyle style, LineColor lineColor)
    {
        if (style == PageStyle && lineColor == LineColor)
        {
            return;
        }

        PageStyle = style;
        LineColor = lineColor;
        foreach (var page in _pages)
        {
            page.Paper.Update(style, lineColor);
        }

        Changed?.Invoke();
    }

    public void Load(NoteDocument document)
    {
        _history.Clear();
        PageStyle = document.PageStyle;
        LineColor = document.LineColor;
        Show(document.PageMode, BuildPages(document));
    }

    public NoteDocument ToDocument() => new()
    {
        PageMode = Mode,
        PageStyle = PageStyle,
        LineColor = LineColor,
        Pages = _pages.Select(page => new NotePage { Strokes = page.Ink.Strokes.Select(StrokeMapper.ToModel).ToList() }).ToList(),
    };

    // The old page views stay alive inside the undo step, so earlier steps that point at them remain valid.
    public void SetMode(PageMode mode)
    {
        if (mode == Mode)
        {
            return;
        }

        var previousMode = Mode;
        var previousPages = _pages.ToList();
        var pages = BuildPages(PageModeConverter.Convert(ToDocument(), mode));
        _history.Push(new UndoStep(() => Show(previousMode, previousPages), () => Show(mode, pages)));
        Show(mode, pages);
        Changed?.Invoke();
    }

    public void AddPage()
    {
        if (Mode != PageMode.Pages)
        {
            return;
        }

        var page = AppendPage();
        UpdateLayout();
        page.BringIntoView();
    }

    public int PageNumberAt(UIElement viewport, double viewportY)
    {
        var index = _pages.FindIndex(page => page.TranslatePoint(new Point(0, page.ActualHeight), viewport).Y >= viewportY);
        return index < 0 ? _pages.Count : index + 1;
    }

    public void Undo() => OnHistoryApplied("undo", _history.Undo());

    public void Redo() => OnHistoryApplied("redo", _history.Redo());

    private List<PageView> BuildPages(NoteDocument document)
    {
        var models = document.PageMode == PageMode.Endless
            ? [new NotePage { Strokes = document.Pages.SelectMany(page => page.Strokes).ToList() }]
            : document.Pages.DefaultIfEmpty(new NotePage()).ToList();
        var pages = models.Select(CreatePage).ToList();
        if (document.PageMode == PageMode.Endless)
        {
            pages[0].GrowToFit(pages[0].Ink.Strokes.GetBounds());
        }

        return pages;
    }

    private PageView CreatePage(NotePage model)
    {
        var page = new PageView(_pen);
        page.Ink.Strokes = new StrokeCollection(model.Strokes.Where(stroke => stroke.Points.Count > 0).Select(StrokeMapper.ToStroke));
        page.Ink.StrokeCollected += (_, e) => OnStrokeCollected(page, e.Stroke);
        page.Ink.StrokeErasing += (_, e) => OnStrokeErasing(page.Ink.Strokes, e.Stroke);
        PageAdded?.Invoke(page);
        return page;
    }

    private void Show(PageMode mode, List<PageView> pages)
    {
        Mode = mode;
        _pages.Clear();
        _pages.AddRange(pages);
        Children.Clear();
        foreach (var page in pages)
        {
            page.Paper.Update(PageStyle, LineColor);
            Children.Add(page);
        }

        UpdateEditingMode();
        PagesChanged?.Invoke();
    }

    private PageView AppendPage()
    {
        var page = CreatePage(new NotePage());
        page.Paper.Update(PageStyle, LineColor);
        _pages.Add(page);
        Children.Add(page);
        UpdateEditingMode();
        PagesChanged?.Invoke();
        Changed?.Invoke();
        return page;
    }

    private void OnHistoryApplied(string kind, bool applied)
    {
        if (!applied)
        {
            return;
        }

        Changed?.Invoke();
        if (DebugLog.IsEnabled)
        {
            DebugLog.Write($"{kind} strokes={_pages.Sum(page => page.Ink.Strokes.Count)}");
        }
    }

    private void OnStrokeCollected(PageView page, Stroke stroke)
    {
        var strokes = page.Ink.Strokes;
        _history.Push(new UndoStep(() => strokes.Remove(stroke), () => strokes.Add(stroke)));
        var bounds = stroke.GetBounds();
        if (Mode == PageMode.Endless)
        {
            page.GrowToFit(bounds);
        }
        else if (page == _pages[^1] && bounds.Bottom > AppConstants.PageHeight * AppConstants.AutoPageZone)
        {
            AppendPage();
        }

        Changed?.Invoke();
    }

    // Raised before the stroke leaves the collection, so its z-order position is still known.
    private void OnStrokeErasing(StrokeCollection strokes, Stroke stroke)
    {
        var index = strokes.IndexOf(stroke);
        _history.Push(new UndoStep(() => strokes.Insert(index, stroke), () => strokes.Remove(stroke)));
        Changed?.Invoke();
    }

    private void UpdateEditingMode()
    {
        var erasing = _eraser || _sideButton.IsHeld;
        foreach (var page in _pages)
        {
            page.Ink.EditingMode = _panning ? InkCanvasEditingMode.None
                : erasing ? InkCanvasEditingMode.EraseByStroke : InkCanvasEditingMode.Ink;
            page.Ink.EditingModeInverted = _panning ? InkCanvasEditingMode.None : InkCanvasEditingMode.EraseByStroke;
        }
    }
}
