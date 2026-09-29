using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

// A picture on a page is an Image child of the page's InkCanvas; its PNG travels along in Tag for saving.
public static class PageImage
{
    // Null when the PNG cannot be decoded; the picture is then left out like a damaged stroke.
    public static Image? Create(NoteImage model)
    {
        if (TryDecode(Convert.FromBase64String(model.Png)) is not { } bitmap)
        {
            return null;
        }

        var image = new Image { Source = bitmap, Stretch = Stretch.Fill, Width = model.Width, Height = model.Height, Tag = model.Png };
        // Screenshots are usually shown smaller than they were taken; the better filter keeps their text readable.
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        InkCanvas.SetLeft(image, model.X);
        InkCanvas.SetTop(image, model.Y);
        return image;
    }

    public static BitmapImage? TryDecode(byte[] png)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(png);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception e) when (e is NotSupportedException or IOException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    public static NoteImage ToModel(Image image) => new()
    {
        X = Math.Round(Left(image), 2),
        Y = Math.Round(Top(image), 2),
        Width = Math.Round(image.Width, 2),
        Height = Math.Round(image.Height, 2),
        Png = (string)image.Tag,
    };

    public static Rect Bounds(Image image) => new(Left(image), Top(image), image.Width, image.Height);

    private static double Left(Image image) => InkCanvas.GetLeft(image) is var left && double.IsNaN(left) ? 0 : left;

    private static double Top(Image image) => InkCanvas.GetTop(image) is var top && double.IsNaN(top) ? 0 : top;
}
