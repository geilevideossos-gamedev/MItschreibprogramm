using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Mitschreibprogramm.Services;

public static class PdfExporter
{
    private const double PixelsToPoints = 72.0 / 96.0;
    private const double A4WidthMillimeters = 210;
    private const double A4HeightMillimeters = 297;
    private const double MaxWidthStep = 0.25;

    // Always exports the logical colours on white paper, whatever theme the screen shows.
    public static void Export(NoteDocument document, string path, bool includeRuleLines)
    {
        using var pdf = new PdfDocument();
        foreach (var notePage in PageModeConverter.Convert(document, PageMode.Pages).Pages.DefaultIfEmpty(new NotePage()))
        {
            var page = pdf.AddPage();
            page.Width = XUnit.FromMillimeter(A4WidthMillimeters);
            page.Height = XUnit.FromMillimeter(A4HeightMillimeters);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.ScaleTransform(PixelsToPoints);
            if (includeRuleLines && document.PageStyle != PageStyle.Blank)
            {
                DrawRuleLines(graphics, document);
            }

            foreach (var stroke in notePage.Strokes.Where(stroke => stroke.Points.Count > 0))
            {
                DrawStroke(graphics, stroke);
            }
        }

        pdf.Save(path);
    }

    private static void DrawRuleLines(XGraphics graphics, NoteDocument document)
    {
        var pen = new XPen(ToXColor(Palette.RuleLineOnPaper(document.LineColor)), AppConstants.RuleLineThickness) { LineCap = XLineCap.Flat };
        if (document.PageStyle == PageStyle.Dashed)
        {
            pen.DashPattern = [AppConstants.RuleDashLength, AppConstants.RuleDashGap];
        }

        foreach (var y in RuleLines.Positions(AppConstants.PageHeight))
        {
            graphics.DrawLine(pen, 0, y, AppConstants.PageWidth, y);
        }
    }

    // Straight pieces of a pressure stroke, each with one width. WPF's Bezier fit can reduce a straight line to two
    // points, so a piece is subdivided until its width changes by at most MaxWidthStep from one part to the next.
    public static IEnumerable<(XPoint From, XPoint To, double Width)> PressureSegments(NoteStroke stroke)
    {
        var points = StrokeMapper.ToStroke(stroke).GetBezierStylusPoints();
        for (var index = 1; index < points.Count; index++)
        {
            var (from, to) = (points[index - 1], points[index]);
            var (fromWidth, toWidth) = (WidthAt(stroke, from.PressureFactor), WidthAt(stroke, to.PressureFactor));
            var parts = Math.Max(1, (int)Math.Ceiling(Math.Abs(toWidth - fromWidth) / MaxWidthStep));
            for (var part = 0; part < parts; part++)
            {
                var (start, end) = ((double)part / parts, (double)(part + 1) / parts);
                yield return (
                    new XPoint(from.X + ((to.X - from.X) * start), from.Y + ((to.Y - from.Y) * start)),
                    new XPoint(from.X + ((to.X - from.X) * end), from.Y + ((to.Y - from.Y) * end)),
                    fromWidth + ((toWidth - fromWidth) * (start + end) / 2));
            }
        }
    }

    // Same curve as on screen: the Bezier fit WPF computes from the raw points, with interpolated pressure.
    private static void DrawStroke(XGraphics graphics, NoteStroke stroke)
    {
        var color = ToXColor(Palette.Pen(stroke.Color));
        var pen = new XPen(color, stroke.Width) { LineCap = XLineCap.Round, LineJoin = XLineJoin.Round };
        var points = StrokeMapper.ToStroke(stroke).GetBezierStylusPoints();
        if (points.Count < 2)
        {
            var diameter = WidthAt(stroke, stroke.Points[0][2]);
            graphics.DrawEllipse(new XSolidBrush(color), stroke.Points[0][0] - (diameter / 2), stroke.Points[0][1] - (diameter / 2), diameter, diameter);
        }
        else if (!stroke.PressureEnabled)
        {
            graphics.DrawLines(pen, points.Select(point => new XPoint(point.X, point.Y)).ToArray());
        }
        else
        {
            foreach (var (from, to, width) in PressureSegments(stroke))
            {
                pen.Width = width;
                graphics.DrawLine(pen, from, to);
            }
        }
    }

    // Mapping used by WPF itself (StrokeNodeIterator.GetNormalizedPressureFactor): pressure 0..1 scales the width by 0.25..1.75.
    private static double WidthAt(NoteStroke stroke, double pressure) =>
        stroke.PressureEnabled ? stroke.Width * ((1.5 * pressure) + 0.25) : stroke.Width;

    private static XColor ToXColor(System.Windows.Media.Color color) => XColor.FromArgb(color.R, color.G, color.B);
}
