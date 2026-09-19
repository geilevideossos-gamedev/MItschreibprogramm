using System.Windows.Controls;
using System.Windows.Ink;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public sealed class DocumentView : StackPanel
{
    private readonly DrawingAttributes _pen = new();

    public DocumentView()
    {
        Children.Add(new PageView(_pen));
    }

    public void SetPenColor(PenColor color) => _pen.Color = Palette.Pen(color);

    public void SetPenWidth(double width)
    {
        _pen.Width = width;
        _pen.Height = width;
    }
}
