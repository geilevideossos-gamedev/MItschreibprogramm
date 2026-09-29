using System.Windows.Ink;
using System.Windows.Input;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Services;

public static class StrokeMapper
{
    private const int CoordinateDigits = 2;
    private const int PressureDigits = 3;

    public static NoteStroke ToModel(Stroke stroke) => new()
    {
        Color = Palette.LogicalPen(stroke.DrawingAttributes.Color),
        Width = stroke.DrawingAttributes.Width,
        PressureEnabled = !stroke.DrawingAttributes.IgnorePressure,
        FitToCurve = stroke.DrawingAttributes.FitToCurve,
        Points = stroke.StylusPoints.Select(point => new[]
        {
            Math.Round(point.X, CoordinateDigits),
            Math.Round(point.Y, CoordinateDigits),
            Math.Round(point.PressureFactor, PressureDigits),
        }).ToList(),
    };

    public static Stroke ToStroke(NoteStroke model)
    {
        var points = new StylusPointCollection(model.Points.Select(point => new StylusPoint(point[0], point[1], (float)point[2])));
        return new Stroke(points, new DrawingAttributes
        {
            Color = Palette.Pen(model.Color),
            Width = model.Width,
            Height = model.Width,
            IgnorePressure = !model.PressureEnabled,
            FitToCurve = model.FitToCurve,
        });
    }
}
