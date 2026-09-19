namespace Mitschreibprogramm.Models;

public sealed class NoteStroke
{
    public PenColor Color { get; set; }

    public double Width { get; set; }

    public bool PressureEnabled { get; set; }

    public List<double[]> Points { get; set; } = [];
}
