using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Tests;

public class RuleLinesTests
{
    private const double Tolerance = 1e-6;
    private const double Millimeter = AppConstants.PixelsPerInch / 25.4;

    [Fact]
    public void BlankPaper_HasNoLines()
    {
        Assert.Empty(RuleLines.Rows(AppConstants.PageHeight, PageStyle.Blank));
        Assert.Empty(RuleLines.Columns(AppConstants.PageWidth, PageStyle.Blank));
    }

    [Fact]
    public void RuledPaper_HasRowsEvery8MillimetersAndNoColumns()
    {
        var rows = RuleLines.Rows(AppConstants.PageHeight, PageStyle.Lined).ToList();

        Assert.Equal(37, rows.Count);
        Assert.Equal(8 * Millimeter, rows[0], Tolerance);
        Assert.Equal(296 * Millimeter, rows[^1], Tolerance);
        Assert.Empty(RuleLines.Columns(AppConstants.PageWidth, PageStyle.Lined));
    }

    [Fact]
    public void SquaredPaper_HasA5MillimeterGridInsideThePage()
    {
        var rows = RuleLines.Rows(AppConstants.PageHeight, PageStyle.Squared).ToList();
        var columns = RuleLines.Columns(AppConstants.PageWidth, PageStyle.Squared).ToList();

        Assert.Equal(59, rows.Count);
        Assert.Equal(5 * Millimeter, rows[0], Tolerance);
        Assert.Equal(295 * Millimeter, rows[^1], Tolerance);
        Assert.Equal(41, columns.Count);
        Assert.Equal(5 * Millimeter, columns[0], Tolerance);
        Assert.Equal(205 * Millimeter, columns[^1], Tolerance);
        Assert.All(columns.Zip(columns.Skip(1)), pair => Assert.Equal(5 * Millimeter, pair.Second - pair.First, Tolerance));
    }

    [Fact]
    public void EndlessSurface_KeepsTheGridContinuousAcrossTheA4WidthAndRestartsRowsPerA4Height()
    {
        var columns = RuleLines.Columns(AppConstants.PageWidth * 1.5, PageStyle.Squared).ToList();
        var rows = RuleLines.Rows(AppConstants.PageHeight * 2, PageStyle.Squared).ToList();

        Assert.Contains(columns, x => Math.Abs(x - (210 * Millimeter)) < Tolerance);
        Assert.All(columns.Zip(columns.Skip(1)), pair => Assert.Equal(5 * Millimeter, pair.Second - pair.First, Tolerance));
        Assert.Equal(118, rows.Count);
        Assert.Equal((297 + 5) * Millimeter, rows[59], Tolerance);
    }
}
