using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Rendering;

// Sizes are DIPs, the image is drawn in physical pixels. The cursor goes through a .cur file with one PNG image,
// because Cursor(Stream) only takes cursor files (LoadImage reads PNG entries since Windows Vista).
public static class CursorImage
{
    private const double RimThickness = 1;
    private const double RingThickness = 1.5;

    public static Cursor Dot(Color fill, Color rim, double diameter, double dpiScale) =>
        Create(diameter + 2 * RimThickness, dpiScale, (context, center) =>
        {
            context.DrawEllipse(new SolidColorBrush(fill), null, center, diameter / 2, diameter / 2);
            var rimRadius = (diameter + RimThickness) / 2;
            context.DrawEllipse(null, new Pen(new SolidColorBrush(rim), RimThickness), center, rimRadius, rimRadius);
        });

    public static Cursor Ring(Color line, Color rim, double diameter, double dpiScale) =>
        Create(diameter + RingThickness + 2 * RimThickness, dpiScale, (context, center) =>
        {
            context.DrawEllipse(null, new Pen(new SolidColorBrush(rim), RingThickness + 2 * RimThickness), center, diameter / 2, diameter / 2);
            context.DrawEllipse(null, new Pen(new SolidColorBrush(line), RingThickness), center, diameter / 2, diameter / 2);
        });

    private static Cursor Create(double size, double dpiScale, Action<DrawingContext, Point> draw)
    {
        // Odd, so the hotspot pixel lies exactly in the middle of the circle.
        var pixels = (int)Math.Ceiling(size * dpiScale);
        if (pixels % 2 == 0)
        {
            pixels++;
        }

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var middle = pixels / dpiScale / 2;
            draw(context, new Point(middle, middle));
        }

        var dpi = AppConstants.PixelsPerInch * dpiScale;
        var bitmap = new RenderTargetBitmap(pixels, pixels, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream();
        encoder.Save(png);

        // Header: reserved, type 2 = cursor, one image. Entry: width, height, palette, reserved, hotspot x and y,
        // data length, data offset (6 + 16 bytes of header).
        using var file = new MemoryStream();
        using var writer = new BinaryWriter(file);
        writer.Write((short)0);
        writer.Write((short)2);
        writer.Write((short)1);
        writer.Write((byte)pixels);
        writer.Write((byte)pixels);
        writer.Write((short)0);
        writer.Write((short)(pixels / 2));
        writer.Write((short)(pixels / 2));
        writer.Write((int)png.Length);
        writer.Write(22);
        writer.Write(png.ToArray());
        file.Position = 0;
        return new Cursor(file);
    }
}
