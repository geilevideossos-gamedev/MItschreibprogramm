using System.Windows;
using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public sealed class PageBackground : FrameworkElement
{
    private PageStyle _style;
    private LineColor _lineColor;

    public void Update(PageStyle style, LineColor lineColor)
    {
        _style = style;
        _lineColor = lineColor;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(new SolidColorBrush(Palette.Page()), null, new Rect(RenderSize));
        if (_style == PageStyle.Blank)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Palette.RuleLine(_lineColor)), AppConstants.RuleLineThickness);
        if (_style == PageStyle.Dashed)
        {
            pen.DashStyle = new DashStyle([AppConstants.RuleDashLength, AppConstants.RuleDashGap], 0);
        }

        pen.Freeze();
        var positions = RuleLines.Positions(ActualHeight).ToList();
        var half = AppConstants.RuleLineThickness / 2;
        // One guideline per line snaps its upper edge to a device pixel, so all lines look alike. Snapping both
        // edges would collapse lines to zero height when zoomed out (both edges land on the same pixel).
        var guidelines = new GuidelineSet { GuidelinesY = new DoubleCollection(positions.Select(y => y - half)) };
        guidelines.Freeze();
        drawingContext.PushGuidelineSet(guidelines);
        foreach (var y in positions)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }

        drawingContext.Pop();
    }
}
