namespace Mitschreibprogramm.Models;

public static class AppConstants
{
    private const double PixelsPerMillimeter = 96.0 / 25.4;

    public const double PageWidth = 210 * PixelsPerMillimeter;
    public const double PageHeight = 297 * PixelsPerMillimeter;
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
}
