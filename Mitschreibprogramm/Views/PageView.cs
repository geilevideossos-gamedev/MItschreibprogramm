using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public sealed class PageView : Grid
{
    public PageView(DrawingAttributes pen, NotePage content)
    {
        Width = AppConstants.PageWidth;
        Height = AppConstants.PageHeight;
        Margin = new Thickness(0, 0, 0, AppConstants.PageGap);
        ClipToBounds = true;
        Ink = new InkCanvas
        {
            Background = Brushes.Transparent,
            DefaultDrawingAttributes = pen,
            Focusable = false,
            Strokes = new StrokeCollection(content.Strokes.Where(stroke => stroke.Points.Count > 0).Select(StrokeMapper.ToStroke)),
        };
        // Press-and-hold (right-click emulation) delays the start of every pen stroke.
        Stylus.SetIsPressAndHoldEnabled(Ink, false);
        Stylus.SetIsFlicksEnabled(Ink, false);
        Stylus.SetIsTapFeedbackEnabled(Ink, false);
        foreach (var image in content.Images.Select(PageImage.Create).OfType<Image>())
        {
            Ink.Children.Add(image);
        }

        Children.Add(Paper);
        Children.Add(Ink);
    }

    public PageBackground Paper { get; } = new();

    public InkCanvas Ink { get; }

    public NotePage ToModel() => new()
    {
        Strokes = Ink.Strokes.Select(StrokeMapper.ToModel).ToList(),
        Images = Ink.Children.OfType<Image>().Select(PageImage.ToModel).ToList(),
    };

    public Rect ContentBounds() => Ink.Children.OfType<Image>().Select(PageImage.Bounds).Aggregate(Ink.Strokes.GetBounds(), Rect.Union);

    // Also covers strokes that come back through undo and still carry the other theme's colours.
    public void ApplyTheme(PageStyle style, LineColor lineColor, bool dark)
    {
        Paper.Update(style, lineColor, dark);
        foreach (var stroke in Ink.Strokes)
        {
            stroke.DrawingAttributes.Color = Palette.PenOnPage(Palette.LogicalPen(stroke.DrawingAttributes.Color), dark);
        }
    }

    public void GrowToFit(Rect bounds)
    {
        while (bounds.Right > Width - AppConstants.EndlessEdgeMargin)
        {
            Width += AppConstants.EndlessGrowStepX;
        }

        while (bounds.Bottom > Height - AppConstants.EndlessEdgeMargin)
        {
            Height += AppConstants.EndlessGrowStepY;
        }
    }
}
