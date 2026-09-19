using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

public static class RuleLines
{
    // Lines restart on every A4 height, so the endless surface, its A4 slices and the PDF export share one grid.
    public static IEnumerable<double> Positions(double height)
    {
        for (var pageTop = 0.0; pageTop < height; pageTop += AppConstants.PageHeight)
        {
            for (var offset = AppConstants.LineSpacing; offset < AppConstants.PageHeight; offset += AppConstants.LineSpacing)
            {
                if (pageTop + offset < height)
                {
                    yield return pageTop + offset;
                }
            }
        }
    }
}
