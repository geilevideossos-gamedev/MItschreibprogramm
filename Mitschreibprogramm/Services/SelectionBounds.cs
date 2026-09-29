using System.Windows;

namespace Mitschreibprogramm.Services;

// Corrections of the rectangle InkCanvas proposes when a selection is moved or resized.
public static class SelectionBounds
{
    private const double SameEdge = 0.5;

    // Scales by the dimension that changed more; the side or corner that was not dragged stays where it is.
    public static Rect KeepAspect(Rect old, Rect proposed)
    {
        if (old.Width <= 0 || old.Height <= 0)
        {
            return proposed;
        }

        var (scaleX, scaleY) = (proposed.Width / old.Width, proposed.Height / old.Height);
        var scale = Math.Abs(scaleX - 1) >= Math.Abs(scaleY - 1) ? scaleX : scaleY;
        var (width, height) = (old.Width * scale, old.Height * scale);
        var left = Math.Abs(proposed.Left - old.Left) < SameEdge ? old.Left : old.Right - width;
        var top = Math.Abs(proposed.Top - old.Top) < SameEdge ? old.Top : old.Bottom - height;
        return new Rect(left, top, width, height);
    }

    // On an A4 page the selection stays on it, shrunk if it grew larger than the page. The endless surface only has a
    // top and a left edge; it grows to the right and downwards by itself.
    public static Rect KeepOnPage(Rect rect, Size page, bool endless)
    {
        if (endless)
        {
            return new Rect(Math.Max(0, rect.X), Math.Max(0, rect.Y), rect.Width, rect.Height);
        }

        var fit = rect.Width > 0 && rect.Height > 0 ? Math.Min(1, Math.Min(page.Width / rect.Width, page.Height / rect.Height)) : 1;
        var (width, height) = (rect.Width * fit, rect.Height * fit);
        return new Rect(Math.Clamp(rect.X, 0, page.Width - width), Math.Clamp(rect.Y, 0, page.Height - height), width, height);
    }
}
