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
    private readonly List<PageView> _created = [];
    private readonly UndoHistory _history = new();
    private readonly SideButtonWatcher _sideButton;
    private readonly ShapeAssist _shapes;
    private double _zoom = AppConstants.DefaultZoom;
    private PenColor _penColor;
    private bool _dark;
    private bool _eraser;
    private bool _panning;

    public DocumentView()
    {
        _sideButton = new SideButtonWatcher(this);
        _sideButton.Changed += UpdateEditingMode;
        _shapes = new ShapeAssist(this);
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

    public void SetPenColor(PenColor color)
    {
        _penColor = color;
        _pen.Color = Palette.PenOnPage(color, _dark);
    }

    public void SetDarkMode(bool dark)
    {
        _dark = dark;
        SetPenColor(_penColor);
        ApplyTheme();
    }

    public void SetPenWidth(double width)
    {
        _pen.Width = width;
        _pen.Height = width;
    }

    public void SetPressureEnabled(bool enabled) => _pen.IgnorePressure = !enabled;

    public void SetShapesAlwaysOn(bool on) => _shapes.AlwaysOn = on;

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
        ApplyTheme();
        Changed?.Invoke();
    }

    public void Load(NoteDocument document)
    {
        _history.Clear();
        // InkCanvas listens to its DefaultDrawingAttributes; while it holds the shared pen, the pen keeps the page alive.
        foreach (var page in _created)
        {
            page.Ink.DefaultDrawingAttributes = new DrawingAttributes();
        }

        _created.Clear();
        PageStyle = document.PageStyle;
        LineColor = document.LineColor;
        Show(document.PageMode, BuildPages(document));
    }

    public NoteDocument ToDocument() => new()
    {
        PageMode = Mode,
        PageStyle = PageStyle,
        LineColor = LineColor,
        Pages = _pages.Select(page => page.ToModel()).ToList(),
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
        // Each side is captured again when it is left, so pages appended in the meantime survive undo and redo.
        _history.Push(new UndoStep(
            () =>
            {
                pages = _pages.ToList();
                Show(previousMode, previousPages);
            },
            () =>
            {
                previousPages = _pages.ToList();
                Show(mode, pages);
            }));
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

    // For changes that do not come from the ink canvas events themselves (shapes, images, selection).
    public void Record(UndoStep step)
    {
        _history.Push(step);
        Changed?.Invoke();
    }

    public void Undo() => OnHistoryApplied("undo", _history.Undo());

    public void Redo() => OnHistoryApplied("redo", _history.Redo());

    private List<PageView> BuildPages(NoteDocument document)
    {
        var models = document.PageMode == PageMode.Endless
            ? [new NotePage { Strokes = document.Pages.SelectMany(page => page.Strokes).ToList(), Images = document.Pages.SelectMany(page => page.Images).ToList() }]
            : document.Pages.DefaultIfEmpty(new NotePage()).ToList();
        var pages = models.Select(CreatePage).ToList();
        if (document.PageMode == PageMode.Endless)
        {
            pages[0].GrowToFit(pages[0].ContentBounds());
        }

        return pages;
    }

    private PageView CreatePage(NotePage model)
    {
        var page = new PageView(_pen, model);
        page.Ink.StrokeCollected += (_, e) => OnStrokeCollected(page, e.Stroke);
        page.Ink.StrokeErasing += (_, e) => OnStrokeErasing(page.Ink.Strokes, e.Stroke);
        _created.Add(page);
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
            Children.Add(page);
        }

        ApplyTheme();
        UpdateEditingMode();
        PagesChanged?.Invoke();
    }

    private PageView AppendPage()
    {
        var page = CreatePage(new NotePage());
        page.ApplyTheme(PageStyle, LineColor, _dark);
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

        ApplyTheme();
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
        var bounds = _shapes.Apply(strokes, stroke).GetBounds();
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

    private void ApplyTheme()
    {
        foreach (var page in _pages)
        {
            page.ApplyTheme(PageStyle, LineColor, _dark);
        }
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
