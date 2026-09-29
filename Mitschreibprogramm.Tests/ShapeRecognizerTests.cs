using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class ShapeRecognizerTests
{
    private const double Step = 4;

    [Fact]
    public void CleanLine_BecomesALineBetweenItsEnds()
    {
        var shape = Recognize(Path((100, 100), (400, 180)));

        Assert.Equal(ShapeKind.Line, shape?.Kind);
        Assert.Equal(2, shape!.Outline.Count);
        Near((100, 100), shape.Outline[0], 0.5);
        Near((400, 180), shape.Outline[1], 0.5);
    }

    [Fact]
    public void ScribbledLine_BecomesALine()
    {
        var shape = Recognize(Noisy(Path((100, 100), (420, 150)), 3));

        Assert.Equal(ShapeKind.Line, shape?.Kind);
        Near((100, 100), shape!.Outline[0], 5);
        Near((420, 150), shape.Outline[1], 5);
    }

    [Fact]
    public void AlmostHorizontalLine_IsLevelled()
    {
        var shape = Recognize(Noisy(Path((100, 200), (400, 212)), 2));

        Assert.Equal(ShapeKind.Line, shape?.Kind);
        Assert.Equal(shape!.Outline[0].Y, shape.Outline[1].Y, 6);
    }

    [Fact]
    public void RoughCircleWithOvershoot_BecomesACircle()
    {
        var shape = Recognize(Noisy(Arc((300, 300), 80, 80, -10, 385), 3));

        Assert.Equal(ShapeKind.Circle, shape?.Kind);
        var center = (shape!.Outline.Average(point => point.X), shape.Outline.Average(point => point.Y));
        Near((300, 300), center, 4);
        Assert.All(shape.Outline, point => Assert.InRange(Polyline.Distance(point, center), 76, 86));
        Assert.Equal(shape.Outline[0], shape.Outline[^1]);
    }

    [Fact]
    public void FlatOval_BecomesAnUprightEllipse()
    {
        var shape = Recognize(Noisy(Arc((300, 300), 130, 60, 90, 450), 2));

        Assert.Equal(ShapeKind.Ellipse, shape?.Kind);
        Assert.InRange(shape!.Outline.Max(point => point.X) - shape.Outline.Min(point => point.X), 250, 275);
        Assert.InRange(shape.Outline.Max(point => point.Y) - shape.Outline.Min(point => point.Y), 112, 132);
    }

    [Fact]
    public void RectangleDrawnFromACornerWithOvershoot_BecomesAnUprightRectangle()
    {
        var shape = Recognize(Noisy(Path((100, 100), (300, 100), (300, 220), (100, 220), (100, 100), (125, 102)), 2));

        Assert.Equal(ShapeKind.Rectangle, shape?.Kind);
        Assert.Equal(5, shape!.Outline.Count);
        AssertCorners(shape, (100, 100), (300, 100), (300, 220), (100, 220));
        Assert.Equal(shape.Outline[0].Y, shape.Outline[1].Y, 6);
        Assert.Equal(shape.Outline[1].X, shape.Outline[2].X, 6);
    }

    [Fact]
    public void RectangleStartedInTheMiddleOfAnEdge_BecomesARectangle()
    {
        var shape = Recognize(Noisy(Path((200, 100), (300, 100), (300, 220), (100, 220), (100, 100), (205, 100)), 2));

        Assert.Equal(ShapeKind.Rectangle, shape?.Kind);
        AssertCorners(shape!, (100, 100), (300, 100), (300, 220), (100, 220));
    }

    [Fact]
    public void TiltedSquare_KeepsItsTilt()
    {
        var corners = new[] { (-80.0, -80.0), (80.0, -80.0), (80.0, 80.0), (-80.0, 80.0) }
            .Select(point => Polyline.Rotate((300 + point.Item1, 300 + point.Item2), (300, 300), 25 * Math.PI / 180)).ToList();
        var shape = Recognize(Noisy(Path([.. corners, corners[0]]), 2));

        Assert.Equal(ShapeKind.Rectangle, shape?.Kind);
        AssertCorners(shape!, [.. corners]);
    }

    [Fact]
    public void RoughTriangle_BecomesATriangle()
    {
        var shape = Recognize(Noisy(Path((200, 100), (320, 300), (80, 300), (200, 100)), 2));

        Assert.Equal(ShapeKind.Triangle, shape?.Kind);
        Assert.Equal(4, shape!.Outline.Count);
        AssertCorners(shape, (200, 100), (320, 300), (80, 300));
    }

    // Handwriting at the size it has on 8 mm ruled paper: about 20 px for small letters, 30 px for digits.
    [Fact]
    public void LettersAndDigitsAtWritingSize_StayFreehand()
    {
        var o = Arc((50, 50), 10, 11, 60, 440);
        var zero = Arc((50, 50), 9, 15, -80, 290);
        var a = Arc((50, 50), 10, 10, -30, 330).Concat(Path((59, 44), (60, 62))).ToList();
        var fourFirst = Path((68, 30), (50, 52), (74, 52));
        var fourSecond = Path((66, 36), (66, 66));

        Assert.All(new[] { o, zero, a, fourFirst, fourSecond }, stroke => Assert.Null(Recognize(Noisy(stroke, 0.8))));
    }

    [Fact]
    public void LargeHandwritingStrokes_StayFreehand()
    {
        var a = Scaled(Arc((50, 50), 10, 10, -30, 330).Concat(Path((59, 44), (60, 62))).ToList(), 4);
        var four = Scaled(Path((68, 30), (50, 52), (74, 52)), 4);
        var wave = Enumerable.Range(0, 80).Select(index => (100 + (index * 4.0), 200 + (30 * Math.Sin(index * 4.0 / 20)))).ToList();
        var spiral = Enumerable.Range(0, 120).Select(index => index * 0.15)
            .Select(t => (300 + (t * 12 * Math.Cos(t)), 300 + (t * 12 * Math.Sin(t)))).ToList();
        var backAndForth = Path((100, 100), (400, 104), (102, 108));
        var u = Path((100, 100), (100, 250), (250, 250), (250, 100));
        var zigzag = Path((100, 100), (150, 200), (200, 100), (250, 200), (300, 100));

        Assert.All(new[] { a, four, wave, spiral, backAndForth, u, zigzag }, stroke => Assert.Null(Recognize(Noisy(stroke, 1))));
    }

    [Fact]
    public void SmallCircle_StaysFreehand() => Assert.Null(Recognize(Arc((100, 100), 15, 15, 0, 365)));

    [Fact]
    public void DotAndSinglePoint_StayFreehand()
    {
        Assert.Null(Recognize([(10, 10)]));
        Assert.Null(Recognize([(10, 10), (10, 10)]));
    }

    [Fact]
    public void MinimumSizeIsMeasuredOnTheLargerSide()
    {
        var line = Path((100, 100), (100 + AppConstants.MinShapeSize - 2, 100));

        Assert.Null(Recognize(line));
        Assert.NotNull(ShapeRecognizer.Recognize(line, AppConstants.MinShapeSize / 2));
    }

    private static RecognizedShape? Recognize(IReadOnlyList<(double X, double Y)> points) =>
        ShapeRecognizer.Recognize(points, AppConstants.MinShapeSize);

    private static List<(double X, double Y)> Path(params (double X, double Y)[] vertices)
    {
        var points = new List<(double X, double Y)> { vertices[0] };
        for (var index = 1; index < vertices.Length; index++)
        {
            var (from, to) = (vertices[index - 1], vertices[index]);
            var steps = Math.Max(1, (int)Math.Ceiling(Polyline.Distance(from, to) / Step));
            points.AddRange(Enumerable.Range(1, steps).Select(step => (from.X + ((to.X - from.X) * step / steps), from.Y + ((to.Y - from.Y) * step / steps))));
        }

        return points;
    }

    private static List<(double X, double Y)> Arc((double X, double Y) center, double radiusX, double radiusY, double fromDegrees, double toDegrees)
    {
        var count = (int)Math.Ceiling((toDegrees - fromDegrees) / 4);
        return Enumerable.Range(0, count + 1)
            .Select(index => (fromDegrees + ((toDegrees - fromDegrees) * index / count)) * Math.PI / 180)
            .Select(angle => (center.X + (radiusX * Math.Cos(angle)), center.Y + (radiusY * Math.Sin(angle))))
            .ToList();
    }

    // Deterministic tremor, so a failing case fails the same way every run.
    private static List<(double X, double Y)> Noisy(List<(double X, double Y)> points, double amplitude) =>
        points.Select((point, index) => (point.X + (amplitude * Math.Sin(index * 1.7)), point.Y + (amplitude * Math.Cos(index * 2.3)))).ToList();

    private static List<(double X, double Y)> Scaled(List<(double X, double Y)> points, double factor) =>
        points.Select(point => (point.X * factor, point.Y * factor)).ToList();

    private static void AssertCorners(RecognizedShape shape, params (double X, double Y)[] expected)
    {
        Assert.Equal(expected.Length + 1, shape.Outline.Count);
        Assert.All(expected, corner => Assert.True(
            shape.Outline.Any(point => Polyline.Distance(point, corner) <= 8),
            $"corner ({corner.X:0},{corner.Y:0}) missing in {string.Join(" ", shape.Outline.Select(point => $"({point.X:0},{point.Y:0})"))}"));
    }

    private static void Near((double X, double Y) expected, (double X, double Y) actual, double tolerance) =>
        Assert.True(Polyline.Distance(expected, actual) <= tolerance, $"expected ({expected.X:0.#},{expected.Y:0.#}), got ({actual.X:0.#},{actual.Y:0.#})");
}
