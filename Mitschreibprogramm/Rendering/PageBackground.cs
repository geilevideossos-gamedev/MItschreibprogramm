using System.Windows;
using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public sealed class PageBackground : FrameworkElement
{
    private PageStyle _style;
    private LineColor _lineColor;
    private bool _dark;

    public void Update(PageStyle style, LineColor lineColor, bool dark)
    {
        _style = style;
        _lineColor = lineColor;
        _dark = dark;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(new SolidColorBrush(Palette.Page(_dark)), null, new Rect(RenderSize));
        var rows = RuleLines.Rows(ActualHeight, _style).ToList();
        var columns = RuleLines.Columns(ActualWidth, _style).ToList();
        if (rows.Count == 0 && columns.Count == 0)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Palette.RuleLine(_lineColor, _dark)), AppConstants.RuleLineThickness);
        pen.Freeze();
        var half = AppConstants.RuleLineThickness / 2;
        // One guideline per line snaps one edge to a device pixel, so all lines look alike. Snapping both edges
        // would collapse lines to zero size when zoomed out (both edges land on the same pixel).
        var guidelines = new GuidelineSet
        {
            GuidelinesX = new DoubleCollection(columns.Select(x => x - half)),
            GuidelinesY = new DoubleCollection(rows.Select(y => y - half)),
        };
        guidelines.Freeze();
        drawingContext.PushGuidelineSet(guidelines);
        foreach (var y in rows)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }

        foreach (var x in columns)
        {
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }

        drawingContext.Pop();
    }
}
