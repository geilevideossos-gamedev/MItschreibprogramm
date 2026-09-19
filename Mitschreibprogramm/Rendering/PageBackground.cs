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
        // Guidelines snap every line to device pixels, otherwise 1 px lines blur at fractional positions.
        var guidelines = new GuidelineSet { GuidelinesY = new DoubleCollection(positions.SelectMany(y => new[] { y - half, y + half })) };
        guidelines.Freeze();
        drawingContext.PushGuidelineSet(guidelines);
        foreach (var y in positions)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }

        drawingContext.Pop();
    }
}
