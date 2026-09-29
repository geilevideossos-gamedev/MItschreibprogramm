namespace Mitschreibprogramm.Services;

// Plane geometry on point lists for the shape recognition.
public static class Polyline
{
    public static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt(Square(a.X - b.X) + Square(a.Y - b.Y));

    public static double Length(IReadOnlyList<(double X, double Y)> points) =>
        Enumerable.Range(1, Math.Max(0, points.Count - 1)).Sum(index => Distance(points[index - 1], points[index]));

    public static double DistanceToSegment((double X, double Y) point, (double X, double Y) from, (double X, double Y) to)
    {
        var (dx, dy) = (to.X - from.X, to.Y - from.Y);
        var lengthSquared = Square(dx) + Square(dy);
        var t = lengthSquared == 0 ? 0 : Math.Clamp((((point.X - from.X) * dx) + ((point.Y - from.Y) * dy)) / lengthSquared, 0, 1);
        return Distance(point, (from.X + (t * dx), from.Y + (t * dy)));
    }

    public static double DistanceToOutline((double X, double Y) point, IReadOnlyList<(double X, double Y)> outline) =>
        Enumerable.Range(1, outline.Count - 1).Min(index => DistanceToSegment(point, outline[index - 1], outline[index]));

    // Douglas-Peucker: keeps the points that stick out more than the tolerance from the line between their neighbours.
    public static List<(double X, double Y)> Simplify(IReadOnlyList<(double X, double Y)> points, double tolerance)
    {
        if (points.Count < 3)
        {
            return [.. points];
        }

        var keep = new bool[points.Count];
        keep[0] = keep[^1] = true;
        Mark(points, 0, points.Count - 1, tolerance, keep);
        return points.Where((_, index) => keep[index]).ToList();
    }

    // Evenly spaced along the path, the segment back to the start included, so every part of a loop weighs the same.
    public static List<(double X, double Y)> ResampleClosed(IReadOnlyList<(double X, double Y)> points, int count)
    {
        var loop = points.Append(points[0]).ToList();
        var step = Length(loop) / count;
        var result = new List<(double X, double Y)>(count);
        var (segment, travelled) = (1, 0.0);
        for (var sample = 0; sample < count; sample++)
        {
            var target = sample * step;
            while (segment < loop.Count - 1 && travelled + Distance(loop[segment - 1], loop[segment]) < target)
            {
                travelled += Distance(loop[segment - 1], loop[segment]);
                segment++;
            }

            var length = Distance(loop[segment - 1], loop[segment]);
            var t = length == 0 ? 0 : Math.Clamp((target - travelled) / length, 0, 1);
            var (from, to) = (loop[segment - 1], loop[segment]);
            result.Add((from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t)));
        }

        return result;
    }

    // Change of direction at a vertex in degrees: 0 = straight on, 180 = back.
    public static double Turn((double X, double Y) previous, (double X, double Y) vertex, (double X, double Y) next)
    {
        var (ax, ay, bx, by) = (vertex.X - previous.X, vertex.Y - previous.Y, next.X - vertex.X, next.Y - vertex.Y);
        var lengths = Math.Sqrt(Square(ax) + Square(ay)) * Math.Sqrt(Square(bx) + Square(by));
        return lengths == 0 ? 0 : Math.Acos(Math.Clamp(((ax * bx) + (ay * by)) / lengths, -1, 1)) * 180 / Math.PI;
    }

    public static double Cross((double X, double Y) previous, (double X, double Y) vertex, (double X, double Y) next) =>
        ((vertex.X - previous.X) * (next.Y - vertex.Y)) - ((vertex.Y - previous.Y) * (next.X - vertex.X));

    public static (double X, double Y) Rotate((double X, double Y) point, (double X, double Y) pivot, double radians)
    {
        var (sin, cos) = Math.SinCos(radians);
        var (dx, dy) = (point.X - pivot.X, point.Y - pivot.Y);
        return (pivot.X + (dx * cos) - (dy * sin), pivot.Y + (dx * sin) + (dy * cos));
    }

    public static double Square(double value) => value * value;

    private static void Mark(IReadOnlyList<(double X, double Y)> points, int first, int last, double tolerance, bool[] keep)
    {
        var (farthest, distance) = (-1, 0.0);
        for (var index = first + 1; index < last; index++)
        {
            var candidate = DistanceToSegment(points[index], points[first], points[last]);
            if (candidate > distance)
            {
                (farthest, distance) = (index, candidate);
            }
        }

        if (farthest < 0 || distance <= tolerance)
        {
            return;
        }

        keep[farthest] = true;
        Mark(points, first, farthest, tolerance, keep);
        Mark(points, farthest, last, tolerance, keep);
    }
}
