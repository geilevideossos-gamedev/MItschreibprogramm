namespace Mitschreibprogramm.Models;

public sealed class NotePage
{
    public List<NoteStroke> Strokes { get; set; } = [];

    // Pictures lie under the strokes, in insertion order.
    public List<NoteImage> Images { get; set; } = [];
}
