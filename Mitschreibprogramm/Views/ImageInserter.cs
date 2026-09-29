using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Ctrl+V: the clipboard picture goes onto the page in the middle of the view, at most 80 % of the page wide.
public sealed class ImageInserter(DocumentView document, ScrollViewer scroller)
{
    private const string PngFormat = "PNG";

    public void Paste()
    {
        if (ReadClipboardPng() is not { } png || PageImage.TryDecode(png) is not { } bitmap)
        {
            return;
        }

        var page = document.Pages[document.PageNumberAt(scroller, scroller.ViewportHeight / 2) - 1];
        var scale = Math.Min(1, Math.Min(
            AppConstants.PastedImageMaxShare * AppConstants.PageWidth / bitmap.Width,
            AppConstants.PastedImageMaxShare * AppConstants.PageHeight / bitmap.Height));
        var (width, height) = (bitmap.Width * scale, bitmap.Height * scale);
        var center = scroller.TranslatePoint(new Point(scroller.ViewportWidth / 2, scroller.ViewportHeight / 2), page);
        var left = Math.Clamp(center.X - (width / 2), 0, Math.Max(0, page.Width - width));
        var top = Math.Clamp(center.Y - (height / 2), 0, Math.Max(0, page.Height - height));
        if (PageImage.Create(new NoteImage { X = left, Y = top, Width = width, Height = height, Png = Convert.ToBase64String(png) }) is not { } image)
        {
            return;
        }

        var children = page.Ink.Children;
        children.Add(image);
        if (document.Mode == PageMode.Endless)
        {
            page.GrowToFit(PageImage.Bounds(image));
        }

        document.Record(new UndoStep(() => children.Remove(image), () => children.Add(image)));
        DebugLog.Write($"image pasted page={document.Pages.ToList().IndexOf(page) + 1} x={left:0.#} y={top:0.#} width={width:0.#} height={height:0.#}");
    }

    // PNG as the clipboard offers it (Snipping Tool, browsers: keeps transparency), otherwise the bitmap re-encoded.
    // Null when there is no picture or another program holds the clipboard.
    private static byte[]? ReadClipboardPng()
    {
        try
        {
            if (Clipboard.GetData(PngFormat) is MemoryStream stream && stream.ToArray() is var png && IsPng(png))
            {
                return png;
            }

            if (!Clipboard.ContainsImage() || Clipboard.GetImage() is not { } bitmap)
            {
                return null;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = new MemoryStream();
            encoder.Save(output);
            return output.ToArray();
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static bool IsPng(byte[] bytes) => bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
}
