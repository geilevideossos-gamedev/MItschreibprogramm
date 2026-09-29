namespace Mitschreibprogramm.Models;

public sealed class NoteStroke
{
    public PenColor Color { get; set; }

    public double Width { get; set; }

    public bool PressureEnabled { get; set; }

    // False for recognised shapes: WPF's curve fitting would round the corners of a rectangle or triangle.
    public bool FitToCurve { get; set; } = true;

    public List<double[]> Points { get; set; } = [];
}
