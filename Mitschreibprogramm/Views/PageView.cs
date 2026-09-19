using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

public sealed class PageView : Grid
{
    public PageView(DrawingAttributes pen)
    {
        Width = AppConstants.PageWidth;
        Height = AppConstants.PageHeight;
        ClipToBounds = true;
        Background = Brushes.White;
        Ink = new InkCanvas
        {
            Background = Brushes.Transparent,
            DefaultDrawingAttributes = pen,
            Focusable = false,
        };
        Children.Add(Ink);
    }

    public InkCanvas Ink { get; }
}
