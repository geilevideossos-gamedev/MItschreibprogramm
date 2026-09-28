namespace Mitschreibprogramm.Models;

public sealed class AppSettings
{
    public PenColor PenColor { get; set; } = PenColor.Black;

    public double StrokeWidth { get; set; } = AppConstants.MediumStrokeWidth;

    public bool PressureEnabled { get; set; } = true;

    public PageStyle PageStyle { get; set; } = PageStyle.Lined;

    public LineColor LineColor { get; set; } = LineColor.Blue;

    public PageMode PageMode { get; set; } = PageMode.Pages;

    public bool DarkMode { get; set; }

    public bool NotebookPanelVisible { get; set; } = true;

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public bool WindowMaximized { get; set; }

    public string? LastFolder { get; set; }
}
