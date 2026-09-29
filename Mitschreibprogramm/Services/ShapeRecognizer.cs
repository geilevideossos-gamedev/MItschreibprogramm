using Mitschreibprogramm.Models;
using static Mitschreibprogramm.Services.Polyline;

namespace Mitschreibprogramm.Services;

// Straight line, circle / ellipse, rectangle or triangle in a freehand stroke, or null. Tuned to miss a shape rather
// than turn handwriting into one; the method and its limits are in docs/decisions-2.md.
public static class ShapeRecognizer
{
    private const double LineDeviation = 0.06;
    private const double ClosureGap = 0.25;
    private const double MaxOvershoot = 0.15;
    private const int Samples = 64;
    private const double CornerTolerance = 0.05;
    private const double CornerMerge = 0.12;
    private const double MinCornerTurn = 30;
    private const double RightAngleTolerance = 25;
    private const double MinTriangleAngle = 15;
    private const double PolygonFit = 0.04;
    private const double EllipseMeanError = 0.07;
    private const double EllipseMaxError = 0.2;
    private const double MinAxisRatio = 0.2;
    private const double CircleAxisRatio = 0.85;
    private const double SnapRadians = 8 * Math.PI / 180;
    private const int EllipseOutlinePoints = 72;

    // minSize: the larger side of the bounding box has to reach it, so ordinary letters and digits never qualify.
    public static RecognizedShape? Recognize(IReadOnlyList<(double X, double Y)> stroke, double minSize)
    {
        var points = stroke.Where((point, index) => index == 0 || point != stroke[index - 1]).ToList();
        if (points.Count < 2)
        {
            return null;
        }

        var width = points.Max(point => point.X) - points.Min(point => point.X);
        var height = points.Max(point => point.Y) - points.Min(point => point.Y);
        var size = Math.Max(width, height);
        if (size < minSize)
        {
            return null;
        }

        return Line(points) ?? (Loop(points, size) is { } loop ? PolygonOrEllipse(loop, Math.Sqrt(Square(width) + Square(height))) : null);
    }

    private static RecognizedShape? Line(List<(double X, double Y)> points)
    {
        var (first, last) = (points[0], points[^1]);
        var chord = Distance(first, last);
        // Distance to the segment, not to the endless line: going back beyond an end counts as deviation too.
        if (chord == 0 || points.Any(point => DistanceToSegment(point, first, last) > LineDeviation * chord))
        {
            return null;
        }

        var center = ((first.X + last.X) / 2, (first.Y + last.Y) / 2);
        var angle = Math.Atan2(last.Y - first.Y, last.X - first.X);
        var snapped = Math.Round(angle / (Math.PI / 2)) * (Math.PI / 2);
        var rotation = Math.Abs(angle - snapped) < SnapRadians ? snapped - angle : 0;
        return new RecognizedShape(ShapeKind.Line, [Rotate(first, center, rotation), Rotate(last, center, rotation)]);
    }

    // The end may overshoot the start; the loop is cut where it comes back closest to it. Only a short overshoot is cut,
    // a longer tail (the stem of an "a", the rest of a spiral) means it is no closed shape.
    private static List<(double X, double Y)>? Loop(List<(double X, double Y)> points, double size)
    {
        var start = points[0];
        var half = points.Count / 2;
        var end = Enumerable.Range(half, points.Count - half).MinBy(index => Distance(points[index], start));
        if (end < 2 || Distance(points[end], start) > ClosureGap * size)
        {
            return null;
        }

        var loop = points.GetRange(0, end + 1);
        return Length(points.GetRange(end, points.Count - end)) <= MaxOvershoot * Length(loop) ? loop : null;
    }

    private static RecognizedShape? PolygonOrEllipse(List<(double X, double Y)> loop, double diagonal)
    {
        var samples = ResampleClosed(loop, Samples);
        var corners = Corners(loop, diagonal);
        var polygon = corners.Count switch
        {
            3 => Triangle(corners, samples, diagonal),
            4 => Rectangle(corners, samples, diagonal),
            _ => null,
        };
        return polygon ?? Ellipse(samples);
    }

    // Douglas-Peucker on both halves of the loop, split at the point farthest from the start. Close vertices then count
    // once and vertices with hardly any turn go, so a start in the middle of an edge or a rounded corner is no corner.
    private static List<(double X, double Y)> Corners(List<(double X, double Y)> loop, double diagonal)
    {
        var far = Enumerable.Range(0, loop.Count).MaxBy(index => Distance(loop[index], loop[0]));
        var tolerance = CornerTolerance * diagonal;
        var vertices = Simplify(loop.GetRange(0, far + 1), tolerance)
            .Concat(Simplify([.. loop.GetRange(far, loop.Count - far), loop[0]], tolerance).Skip(1).SkipLast(1))
            .ToList();
        var changed = true;
        while (changed && vertices.Count > 2)
        {
            changed = false;
            var turns = vertices.Select((vertex, index) => TurnAt(vertices, index)).ToList();
            for (var index = 0; index < vertices.Count && !changed; index++)
            {
                var next = (index + 1) % vertices.Count;
                if (Distance(vertices[index], vertices[next]) < CornerMerge * diagonal)
                {
                    vertices.RemoveAt(turns[index] < turns[next] ? index : next);
                    changed = true;
                }
                else if (turns[index] < MinCornerTurn)
                {
                    vertices.RemoveAt(index);
                    changed = true;
                }
            }
        }

        return vertices;
    }

    private static RecognizedShape? Triangle(List<(double X, double Y)> corners, List<(double X, double Y)> samples, double diagonal)
    {
        var sharpEnough = Enumerable.Range(0, 3).All(index => 180 - TurnAt(corners, index) >= MinTriangleAngle);
        var outline = Closed(corners);
        return sharpEnough && Fits(samples, outline, diagonal) ? new RecognizedShape(ShapeKind.Triangle, outline) : null;
    }

    private static RecognizedShape? Rectangle(List<(double X, double Y)> corners, List<(double X, double Y)> samples, double diagonal)
    {
        var rightAngles = Enumerable.Range(0, 4).All(index => Math.Abs(TurnAt(corners, index) - 90) <= RightAngleTolerance);
        if (!rightAngles || !Convex(corners))
        {
            return null;
        }

        // Edge directions folded onto a quarter turn and averaged by length give the orientation.
        var (sin, cos) = (0.0, 0.0);
        for (var index = 0; index < 4; index++)
        {
            var (from, to) = (corners[index], corners[(index + 1) % 4]);
            var direction = 4 * Math.Atan2(to.Y - from.Y, to.X - from.X);
            sin += Distance(from, to) * Math.Sin(direction);
            cos += Distance(from, to) * Math.Cos(direction);
        }

        var rotation = Math.Atan2(sin, cos) / 4;
        rotation = Math.Abs(rotation) < SnapRadians ? 0 : rotation;
        var center = (corners.Average(point => point.X), corners.Average(point => point.Y));
        var upright = corners.Select(point => Rotate(point, center, -rotation)).ToList();
        var xs = upright.Select(point => point.X).Order().ToList();
        var ys = upright.Select(point => point.Y).Order().ToList();
        var (left, right, top, bottom) = ((xs[0] + xs[1]) / 2, (xs[2] + xs[3]) / 2, (ys[0] + ys[1]) / 2, (ys[2] + ys[3]) / 2);
        var outline = Closed([(left, top), (right, top), (right, bottom), (left, bottom)])
            .Select(point => Rotate(point, center, rotation)).ToList();
        return Fits(samples, outline, diagonal) ? new RecognizedShape(ShapeKind.Rectangle, outline) : null;
    }

    // Axes from the second moments, radii from the extent along them, then the mean and worst radial error decide.
    private static RecognizedShape? Ellipse(List<(double X, double Y)> samples)
    {
        var pivot = (samples.Average(point => point.X), samples.Average(point => point.Y));
        var xx = samples.Average(point => Square(point.X - pivot.Item1));
        var yy = samples.Average(point => Square(point.Y - pivot.Item2));
        var xy = samples.Average(point => (point.X - pivot.Item1) * (point.Y - pivot.Item2));
        var axis = 0.5 * Math.Atan2(2 * xy, xx - yy);
        var snapped = Math.Round(axis / (Math.PI / 2)) * (Math.PI / 2);
        var rotation = Math.Abs(axis - snapped) < SnapRadians ? snapped : axis;
        var upright = samples.Select(point => Rotate(point, pivot, -rotation)).ToList();
        var (minX, maxX) = (upright.Min(point => point.X), upright.Max(point => point.X));
        var (minY, maxY) = (upright.Min(point => point.Y), upright.Max(point => point.Y));
        var (a, b) = ((maxX - minX) / 2, (maxY - minY) / 2);
        var center = ((minX + maxX) / 2, (minY + maxY) / 2);
        if (Math.Min(a, b) < MinAxisRatio * Math.Max(a, b))
        {
            return null;
        }

        var errors = upright.Select(point => Math.Abs(Math.Sqrt(Square((point.X - center.Item1) / a) + Square((point.Y - center.Item2) / b)) - 1)).ToList();
        if (errors.Average() > EllipseMeanError || errors.Max() > EllipseMaxError)
        {
            return null;
        }

        var circle = Math.Min(a, b) >= CircleAxisRatio * Math.Max(a, b);
        (a, b) = circle ? ((a + b) / 2, (a + b) / 2) : (a, b);
        var outline = Enumerable.Range(0, EllipseOutlinePoints + 1)
            .Select(index => 2 * Math.PI * index / EllipseOutlinePoints)
            .Select(t => Rotate((center.Item1 + (a * Math.Cos(t)), center.Item2 + (b * Math.Sin(t))), pivot, rotation))
            .ToList();
        return new RecognizedShape(circle ? ShapeKind.Circle : ShapeKind.Ellipse, outline);
    }

    private static double TurnAt(List<(double X, double Y)> polygon, int index) =>
        Turn(polygon[(index + polygon.Count - 1) % polygon.Count], polygon[index], polygon[(index + 1) % polygon.Count]);

    private static bool Convex(List<(double X, double Y)> polygon)
    {
        var signs = Enumerable.Range(0, polygon.Count)
            .Select(index => Math.Sign(Cross(polygon[(index + polygon.Count - 1) % polygon.Count], polygon[index], polygon[(index + 1) % polygon.Count])))
            .ToList();
        return signs.All(sign => sign > 0) || signs.All(sign => sign < 0);
    }

    private static bool Fits(List<(double X, double Y)> samples, List<(double X, double Y)> outline, double diagonal) =>
        samples.Average(point => DistanceToOutline(point, outline)) <= PolygonFit * diagonal;

    private static List<(double X, double Y)> Closed(List<(double X, double Y)> corners) => [.. corners, corners[0]];
}
