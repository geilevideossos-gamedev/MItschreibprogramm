namespace Mitschreibprogramm.Models;

// The outline is the replacement stroke's point list; closed shapes end on their first point.
public sealed record RecognizedShape(ShapeKind Kind, IReadOnlyList<(double X, double Y)> Outline);
