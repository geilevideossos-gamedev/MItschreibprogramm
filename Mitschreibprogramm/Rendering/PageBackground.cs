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
        // All lines go into one geometry: the pen is translucent, and separate DrawLine calls would blend every
        // crossing of the grid twice and leave a darker dot there.
        var lines = new StreamGeometry();
        using (var context = lines.Open())
        {
            foreach (var y in rows)
            {
                context.BeginFigure(new Point(0, y), isFilled: false, isClosed: false);
                context.LineTo(new Point(ActualWidth, y), isStroked: true, isSmoothJoin: false);
            }

            foreach (var x in columns)
            {
                context.BeginFigure(new Point(x, 0), isFilled: false, isClosed: false);
                context.LineTo(new Point(x, ActualHeight), isStroked: true, isSmoothJoin: false);
            }
        }

        lines.Freeze();
        drawingContext.PushGuidelineSet(guidelines);
        drawingContext.DrawGeometry(null, pen, lines);
        drawingContext.Pop();
    }
}
