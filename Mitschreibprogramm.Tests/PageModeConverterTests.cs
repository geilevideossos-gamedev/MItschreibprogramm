using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public class PageModeConverterTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void PagesToEndless_OffsetsEachPageByItsIndexTimesPageHeight()
    {
        var document = PagesDocument(Stroke(10, 20, 30, 40), Stroke(50, 60, 70, 80), Stroke(5, 6, 7, 8));

        var endless = PageModeConverter.Convert(document, PageMode.Endless);

        Assert.Equal(PageMode.Endless, endless.PageMode);
        var surface = Assert.Single(endless.Pages);
        Assert.Equal(3, surface.Strokes.Count);
        Assert.Equal(20, surface.Strokes[0].Points[0][1], Tolerance);
        Assert.Equal(60 + AppConstants.PageHeight, surface.Strokes[1].Points[0][1], Tolerance);
        Assert.Equal(6 + (2 * AppConstants.PageHeight), surface.Strokes[2].Points[0][1], Tolerance);
        Assert.Equal(50, surface.Strokes[1].Points[0][0], Tolerance);
    }

    [Fact]
    public void Conversion_KeepsTheCurveFitSetting()
    {
        var shape = Stroke(10, 20, 30, 40);
        shape.FitToCurve = false;

        var endless = PageModeConverter.Convert(PagesDocument(Stroke(1, 2, 3, 4), shape), PageMode.Endless);
        var back = PageModeConverter.Convert(endless, PageMode.Pages);

        Assert.Equal([true, false], endless.Pages[0].Strokes.Select(stroke => stroke.FitToCurve));
        Assert.Equal([true, false], back.Pages.SelectMany(page => page.Strokes).Select(stroke => stroke.FitToCurve));
    }

    [Fact]
    public void PagesToEndlessAndBack_RestoresEveryPoint()
    {
        var document = PagesDocument(Stroke(10, 20, 30, 1100), Stroke(50, 60, 70, 80), Stroke(5, 1000, 7, 8));
        document.PageStyle = PageStyle.Squared;
        document.LineColor = LineColor.Blue;

        var back = PageModeConverter.Convert(PageModeConverter.Convert(document, PageMode.Endless), PageMode.Pages);

        Assert.Equal(PageMode.Pages, back.PageMode);
        Assert.Equal(PageStyle.Squared, back.PageStyle);
        Assert.Equal(LineColor.Blue, back.LineColor);
        Assert.Equal(3, back.Pages.Count);
        for (var page = 0; page < 3; page++)
        {
            var expected = Assert.Single(document.Pages[page].Strokes);
            var actual = Assert.Single(back.Pages[page].Strokes);
            Assert.Equal(expected.Color, actual.Color);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.PressureEnabled, actual.PressureEnabled);
            for (var point = 0; point < expected.Points.Count; point++)
            {
                Assert.Equal(expected.Points[point][0], actual.Points[point][0], Tolerance);
                Assert.Equal(expected.Points[point][1], actual.Points[point][1], Tolerance);
                Assert.Equal(expected.Points[point][2], actual.Points[point][2], Tolerance);
            }
        }
    }

    [Fact]
    public void EndlessToPages_AssignsStrokeToThePageOfItsFirstPoint()
    {
        var crossing = Stroke(100, AppConstants.PageHeight - 10, 100, AppConstants.PageHeight + 50);
        var document = EndlessDocument(crossing);

        var pages = PageModeConverter.Convert(document, PageMode.Pages);

        Assert.Single(pages.Pages);
        var stroke = Assert.Single(pages.Pages[0].Strokes);
        Assert.Equal(AppConstants.PageHeight + 50, stroke.Points[1][1], Tolerance);
    }

    [Fact]
    public void EndlessToPages_KeepsEmptyPagesBetweenWrittenRows()
    {
        var document = EndlessDocument(Stroke(10, 10, 20, 20), Stroke(10, (2 * AppConstants.PageHeight) + 30, 20, (2 * AppConstants.PageHeight) + 40));

        var pages = PageModeConverter.Convert(document, PageMode.Pages);

        Assert.Equal(3, pages.Pages.Count);
        Assert.Empty(pages.Pages[1].Strokes);
        Assert.Equal(30, Assert.Single(pages.Pages[2].Strokes).Points[0][1], Tolerance);
    }

    [Fact]
    public void EndlessToPages_PutsContentRightOfA4WidthOnItsOwnPageAfterTheRow()
    {
        var right = Stroke(AppConstants.PageWidth + 40, 100, AppConstants.PageWidth + 90, 100);
        var below = Stroke(10, AppConstants.PageHeight + 10, 20, AppConstants.PageHeight + 20);
        var document = EndlessDocument(Stroke(10, 10, 20, 20), right, below);

        var pages = PageModeConverter.Convert(document, PageMode.Pages);

        Assert.Equal(3, pages.Pages.Count);
        Assert.Equal(40, Assert.Single(pages.Pages[1].Strokes).Points[0][0], Tolerance);
        Assert.Equal(10, Assert.Single(pages.Pages[2].Strokes).Points[0][1], Tolerance);
    }

    [Fact]
    public void EmptyEndlessDocument_BecomesOneEmptyPage()
    {
        var pages = PageModeConverter.Convert(EndlessDocument(), PageMode.Pages);

        Assert.Empty(Assert.Single(pages.Pages).Strokes);
    }

    [Fact]
    public void SameMode_ReturnsTheDocumentUnchanged()
    {
        var document = PagesDocument(Stroke(1, 2, 3, 4));

        Assert.Same(document, PageModeConverter.Convert(document, PageMode.Pages));
    }

    private static NoteStroke Stroke(double x1, double y1, double x2, double y2) => new()
    {
        Color = PenColor.Blue,
        Width = 3,
        PressureEnabled = true,
        Points = [[x1, y1, 0.25], [x2, y2, 0.75]],
    };

    private static NoteDocument PagesDocument(params NoteStroke[] onePerPage) => new()
    {
        PageMode = PageMode.Pages,
        Pages = onePerPage.Select(stroke => new NotePage { Strokes = [stroke] }).ToList(),
    };

    private static NoteDocument EndlessDocument(params NoteStroke[] strokes) => new()
    {
        PageMode = PageMode.Endless,
        Pages = [new NotePage { Strokes = [.. strokes] }],
    };
}
