using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Mitschreibprogramm.Views;

// The document point at the top-left of the viewport, in page pixels at 100 %, so a position survives a different zoom.
public static class ScrollPosition
{
    public static Point Capture(ScrollViewer scroller, DocumentView document) => scroller.TranslatePoint(new Point(0, 0), document);

    // Same math as the zoom around the pointer. At startup the window has no size yet, so the first restore waits for the layout.
    public static void Restore(Window owner, ScrollViewer scroller, DocumentView document, double x, double y)
    {
        if (owner.IsLoaded)
        {
            ScrollTo(scroller, document, x, y);
        }
        else
        {
            owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => ScrollTo(scroller, document, x, y));
        }
    }

    // A notebook that was never left has no position yet and starts at the top, margin included.
    private static void ScrollTo(ScrollViewer scroller, DocumentView document, double x, double y)
    {
        scroller.UpdateLayout();
        if (x == 0 && y == 0)
        {
            scroller.ScrollToHorizontalOffset(0);
            scroller.ScrollToVerticalOffset(0);
            return;
        }

        var moved = document.TranslatePoint(new Point(x, y), scroller);
        scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + moved.X);
        scroller.ScrollToVerticalOffset(scroller.VerticalOffset + moved.Y);
    }
}
