namespace Mitschreibprogramm.Models;

public static class AppConstants
{
    public const int FileFormatVersion = 2;

    // Bounds coordinates read from a file, so a damaged file cannot ask for millions of pages.
    public const double MaxCoordinate = 1_000_000;

    public const double PixelsPerInch = 96;
    private const double PixelsPerMillimeter = PixelsPerInch / 25.4;

    public const double PageWidthMillimeters = 210;
    public const double PageHeightMillimeters = 297;
    public const double PageWidth = PageWidthMillimeters * PixelsPerMillimeter;
    public const double PageHeight = PageHeightMillimeters * PixelsPerMillimeter;
    public const double PageGap = 24;
    public const double AutoPageZone = 0.85;
    public const double EndlessEdgeMargin = 200;
    public const double EndlessGrowStepX = PageWidth / 2;
    public const double EndlessGrowStepY = PageHeight / 2;
    public const double LineSpacing = 8 * PixelsPerMillimeter;
    public const double GridSpacing = 5 * PixelsPerMillimeter;
    public const double RuleLineThickness = 1;

    public const double MinStrokeWidth = 1;
    public const double MaxStrokeWidth = 12;
    public const double StrokeWidthStep = 0.5;
    public const double ThinStrokeWidth = 1.5;
    public const double MediumStrokeWidth = 3;
    public const double ThickStrokeWidth = 6;

    public const double MinVisibleWindowPart = 100;
    public const double PastedImageMaxShare = 0.8;
    public const int AutosaveIntervalSeconds = 60;

    // Shape recognition: sizes are screen units, so handwriting counts the same at every zoom.
    public const double MinShapeSize = 40;
    public const double ShapeHoldTolerance = 4;
    public const int ShapeHoldMilliseconds = 500;

    public const double MinZoom = 0.25;
    public const double MaxZoom = 4;
    public const double DefaultZoom = 1;
    public const double WheelZoomFactor = 1.1;
    public const double ZoomStepTolerance = 0.001;
    public static readonly double[] ZoomSteps = [MinZoom, 0.5, 0.75, DefaultZoom, 1.25, 1.5, 2, 3, MaxZoom];
}
