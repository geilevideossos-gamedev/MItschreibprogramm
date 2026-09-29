using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mitschreibprogramm.Tests;

internal static class TestImages
{
    // A small opaque PNG in one colour, base64-encoded as in the .msp file.
    public static string Png(int width, int height, Color color)
    {
        var pixels = Enumerable.Range(0, width * height).SelectMany(_ => new[] { color.B, color.G, color.R, (byte)255 }).ToArray();
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }
}
