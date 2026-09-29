namespace Mitschreibprogramm.Models;

// Position and size in page pixels at 100 %, the picture itself as base64-encoded PNG.
public sealed class NoteImage
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public string Png { get; set; } = string.Empty;
}
