using System.Windows.Ink;
using System.Windows.Input;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Only created when MSP_DEBUG_LOG is set. Shows whether input arrives as stylus (pressure, buttons) or as mouse.
public sealed class StrokeLogger
{
    private readonly DocumentView _document;
    private string _device = "mouse";
    private bool _barrel;

    public StrokeLogger(DocumentView document)
    {
        _document = document;
        document.PreviewStylusDown += OnStylusDown;
        document.PreviewMouseDown += OnMouseDown;
        document.PageAdded += Attach;
        foreach (var page in document.Pages)
        {
            Attach(page);
        }
    }

    private void Attach(PageView page)
    {
        page.Ink.StrokeCollected += (_, e) => Write("ink", page, e.Stroke);
        page.Ink.StrokeErasing += (_, e) => Write("erase", page, e.Stroke);
    }

    private void OnStylusDown(object sender, StylusDownEventArgs e)
    {
        _device = e.Inverted ? "stylus-inverted" : "stylus";
        _barrel = e.StylusDevice.StylusButtons.Any(button =>
            button.Guid == StylusPointProperties.BarrelButton.Id &&
            button.StylusButtonState == StylusButtonState.Down);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice is null)
        {
            _device = "mouse";
            _barrel = false;
        }
    }

    private void Write(string mode, PageView page, Stroke stroke)
    {
        var points = stroke.StylusPoints;
        var first = points[0];
        var last = points[^1];
        var attributes = stroke.DrawingAttributes;
        var pageNumber = _document.Pages.ToList().IndexOf(page) + 1;
        DebugLog.Write($"stroke mode={mode} device={_device} barrel={_barrel} page={pageNumber} zoom={_document.Zoom:0.###} points={points.Count} pmin={points.Min(p => p.PressureFactor):0.###} pmax={points.Max(p => p.PressureFactor):0.###} width={attributes.Width} pressure={!attributes.IgnorePressure} first=({first.X:0.#},{first.Y:0.#}) last=({last.X:0.#},{last.Y:0.#})");
    }
}
