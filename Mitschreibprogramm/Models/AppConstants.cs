namespace Mitschreibprogramm.Models;

public static class AppConstants
{
    public const int FileFormatVersion = 1;

    private const double PixelsPerMillimeter = 96.0 / 25.4;

    public const double PageWidth = 210 * PixelsPerMillimeter;
    public const double PageHeight = 297 * PixelsPerMillimeter;
    public const double PageGap = 24;
    public const double AutoPageZone = 0.85;
    public const double EndlessEdgeMargin = 200;
    public const double EndlessGrowStepX = PageWidth / 2;
    public const double EndlessGrowStepY = PageHeight / 2;
    public const double LineSpacing = 8 * PixelsPerMillimeter;
    public const double RuleLineThickness = 1;
    public const double RuleDashLength = 6;
    public const double RuleDashGap = 4;

    public const double MinStrokeWidth = 1;
    public const double MaxStrokeWidth = 12;
    public const double StrokeWidthStep = 0.5;
    public const double ThinStrokeWidth = 1.5;
    public const double MediumStrokeWidth = 3;
    public const double ThickStrokeWidth = 6;

    public const double MinVisibleWindowPart = 100;

    public const double MinZoom = 0.25;
    public const double MaxZoom = 4;
    public const double DefaultZoom = 1;
    public const double WheelZoomFactor = 1.1;
    public static readonly double[] ZoomSteps = [MinZoom, 0.5, 0.75, DefaultZoom, 1.25, 1.5, 2, 3, MaxZoom];
}
