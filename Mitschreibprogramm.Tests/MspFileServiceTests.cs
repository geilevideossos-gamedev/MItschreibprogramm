using System.IO;
using System.Text;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class MspFileServiceTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("msp-tests-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void SaveAndLoad_RoundTripsEveryField()
    {
        var path = Path.Combine(_folder, "note.msp");
        var document = new NoteDocument
        {
            PageMode = PageMode.Endless,
            PageStyle = PageStyle.Squared,
            LineColor = LineColor.Blue,
            Pages =
            [
                new NotePage
                {
                    Strokes =
                    [
                        new NoteStroke { Color = PenColor.Red, Width = 4.5, PressureEnabled = true, Points = [[1.25, 2.5, 0.1], [30, 40.75, 0.875]] },
                        new NoteStroke { Color = PenColor.Green, Width = 1.5, PressureEnabled = false, FitToCurve = false, Points = [[5, 6, 0.5]] },
                    ],
                },
                new NotePage(),
            ],
        };

        MspFileService.Save(document, path);
        var loaded = MspFileService.Load(path);

        Assert.Equal(AppConstants.FileFormatVersion, loaded.Version);
        Assert.Equal(PageMode.Endless, loaded.PageMode);
        Assert.Equal(PageStyle.Squared, loaded.PageStyle);
        Assert.Equal(LineColor.Blue, loaded.LineColor);
        Assert.Equal(2, loaded.Pages.Count);
        Assert.Empty(loaded.Pages[1].Strokes);
        var first = loaded.Pages[0].Strokes[0];
        Assert.Equal(PenColor.Red, first.Color);
        Assert.Equal(4.5, first.Width);
        Assert.True(first.PressureEnabled);
        Assert.Equal([1.25, 2.5, 0.1], first.Points[0]);
        Assert.Equal([30, 40.75, 0.875], first.Points[1]);
        var second = loaded.Pages[0].Strokes[1];
        Assert.Equal(PenColor.Green, second.Color);
        Assert.False(second.PressureEnabled);
        Assert.False(second.FitToCurve);
        Assert.True(first.FitToCurve);
    }

    [Fact]
    public void Save_WritesDocumentedJsonShapeAsUtf8WithoutBom()
    {
        var path = Path.Combine(_folder, "shape.msp");
        var document = new NoteDocument
        {
            PageStyle = PageStyle.Lined,
            Pages = [new NotePage { Strokes = [new NoteStroke { Color = PenColor.Blue, Width = 3, PressureEnabled = true, Points = [[10, 20, 0.5]] }] }],
        };

        MspFileService.Save(document, path);

        var bytes = File.ReadAllBytes(path);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal(
            """{"version":1,"pageMode":"pages","pageStyle":"lined","lineColor":"black","pages":[{"strokes":[{"color":"blue","width":3,"pressureEnabled":true,"fitToCurve":true,"points":[[10,20,0.5]]}]}]}""",
            Encoding.UTF8.GetString(bytes));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Save_LeavesNoTemporaryFileBehindWhenTheTargetCannotBeReplaced()
    {
        var target = Path.Combine(_folder, "folder.msp");
        Directory.CreateDirectory(target);

        Assert.ThrowsAny<Exception>(() => MspFileService.Save(new NoteDocument(), target));

        Assert.False(File.Exists(target + ".tmp"));
    }

    [Fact]
    public void Load_RepairsPointsWithoutPressureAndClampsWidth()
    {
        var path = Path.Combine(_folder, "lenient.msp");
        File.WriteAllText(path, """{"version":1,"pageMode":"pages","pages":[{"strokes":[{"color":"black","width":99,"points":[[1,2],[3],[4,5,7]]}]}]}""");

        var stroke = MspFileService.Load(path).Pages[0].Strokes[0];

        Assert.Equal(AppConstants.MaxStrokeWidth, stroke.Width);
        Assert.Equal(2, stroke.Points.Count);
        Assert.Equal([1, 2, 0.5], stroke.Points[0]);
        Assert.Equal([4, 5, 1], stroke.Points[1]);
    }

    [Fact]
    public void Load_SurvivesNullCollectionsAndBoundsCoordinates()
    {
        var path = Path.Combine(_folder, "nulls.msp");
        File.WriteAllText(path, """{"version":1,"pages":[null,{"strokes":null},{"strokes":[null,{"color":"red","width":2,"points":null},{"color":"red","width":2,"points":[null,[5e12,-5e12,0.5]]}]}]}""");

        var document = MspFileService.Load(path);

        Assert.Equal(2, document.Pages.Count);
        Assert.Empty(document.Pages[0].Strokes);
        Assert.Equal(2, document.Pages[1].Strokes.Count);
        Assert.Empty(document.Pages[1].Strokes[0].Points);
        Assert.Equal([AppConstants.MaxCoordinate, -AppConstants.MaxCoordinate, 0.5], Assert.Single(document.Pages[1].Strokes[1].Points));
    }

    [Fact]
    public void Load_TreatsMissingPagesAsEmptyDocument()
    {
        var path = Path.Combine(_folder, "nopages.msp");
        File.WriteAllText(path, """{"version":1,"pages":null}""");

        Assert.Empty(MspFileService.Load(path).Pages);
    }

    [Fact]
    public void Load_RejectsEnumValuesOutsideTheFormat()
    {
        var numeric = Path.Combine(_folder, "numeric.msp");
        var numericText = Path.Combine(_folder, "numeric-text.msp");
        File.WriteAllText(numericText, """{"version":1,"pages":[{"strokes":[{"color":"9","width":2,"points":[[1,2,0.5]]}]}]}""");
        Assert.Throws<InvalidDataException>(() => MspFileService.Load(numericText));
        var unknown = Path.Combine(_folder, "unknown.msp");
        File.WriteAllText(numeric, """{"version":1,"pageStyle":7,"pages":[]}""");
        File.WriteAllText(unknown, """{"version":1,"pageStyle":"dotted","pages":[]}""");

        Assert.Throws<System.Text.Json.JsonException>(() => MspFileService.Load(numeric));
        Assert.Throws<System.Text.Json.JsonException>(() => MspFileService.Load(unknown));
    }

    [Fact]
    public void Load_ReadsTheFormerDashedStyleAsSquaredAndSavesTheNewName()
    {
        var path = Path.Combine(_folder, "legacy.msp");
        File.WriteAllText(path, """{"version":1,"pageStyle":"dashed","pages":[]}""");

        var document = MspFileService.Load(path);
        MspFileService.Save(document, path);

        Assert.Equal(PageStyle.Squared, document.PageStyle);
        Assert.Contains("\"pageStyle\":\"squared\"", File.ReadAllText(path));
    }

    [Theory]
    [InlineData("Lined", PageStyle.Lined)]
    [InlineData("SQUARED", PageStyle.Squared)]
    [InlineData("Dashed", PageStyle.Squared)]
    public void Load_ReadsPageStyleNamesInAnyCase(string written, PageStyle expected)
    {
        var path = Path.Combine(_folder, "case.msp");
        File.WriteAllText(path, $$"""{"version":1,"pageStyle":"{{written}}","pages":[]}""");

        Assert.Equal(expected, MspFileService.Load(path).PageStyle);
    }

    [Fact]
    public void Load_RejectsNewerFormatVersion()
    {
        var path = Path.Combine(_folder, "future.msp");
        File.WriteAllText(path, """{"version":2,"pages":[]}""");

        Assert.Throws<InvalidDataException>(() => MspFileService.Load(path));
    }
}
