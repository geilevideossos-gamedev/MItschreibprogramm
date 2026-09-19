namespace Mitschreibprogramm.Models;

public static class RuleLines
{
    private const double Tolerance = 1e-6;

    public static IEnumerable<double> Rows(double height, PageStyle style) => style switch
    {
        PageStyle.Lined => Positions(height, AppConstants.LineSpacing, AppConstants.PageHeight),
        PageStyle.Squared => Positions(height, AppConstants.GridSpacing, AppConstants.PageHeight),
        _ => [],
    };

    public static IEnumerable<double> Columns(double width, PageStyle style) =>
        style == PageStyle.Squared ? Positions(width, AppConstants.GridSpacing, AppConstants.PageWidth) : [];

    // The pattern restarts on every A4 period, so the endless surface, its A4 slices and the PDF export share one
    // grid. A line that falls exactly on a period end (210 mm is a multiple of 5 mm) belongs to the surface, but
    // not to a page that ends there.
    private static IEnumerable<double> Positions(double length, double spacing, double period)
    {
        for (var start = 0.0; start < length; start += period)
        {
            for (var index = 1; index * spacing <= period + Tolerance; index++)
            {
                var position = start + (index * spacing);
                if (position < length - Tolerance)
                {
                    yield return position;
                }
            }
        }
    }
}
