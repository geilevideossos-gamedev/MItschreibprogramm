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
            """{"version":2,"pageMode":"pages","pageStyle":"lined","lineColor":"black","pages":[{"strokes":[{"color":"blue","width":3,"pressureEnabled":true,"fitToCurve":true,"points":[[10,20,0.5]]}],"images":[]}]}""",
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
        File.WriteAllText(path, $$"""{"version":{{AppConstants.FileFormatVersion + 1}},"pages":[]}""");

        Assert.Throws<InvalidDataException>(() => MspFileService.Load(path));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsImages()
    {
        var path = Path.Combine(_folder, "images.msp");
        var png = TestImages.Png(4, 3, System.Windows.Media.Colors.OrangeRed);
        var document = new NoteDocument
        {
            Pages = [new NotePage(), new NotePage { Images = [new NoteImage { X = 12.5, Y = 40, Width = 300, Height = 225, Png = png }] }],
        };

        MspFileService.Save(document, path);
        var image = Assert.Single(MspFileService.Load(path).Pages[1].Images);

        Assert.Equal((12.5, 40.0, 300.0, 225.0), (image.X, image.Y, image.Width, image.Height));
        Assert.Equal(png, image.Png);
        Assert.Contains("\"images\":[{\"x\":12.5,\"y\":40,\"width\":300,\"height\":225,\"png\":\"", File.ReadAllText(path));
    }

    // A file as version 1 wrote it: no fitToCurve, no images.
    [Fact]
    public void Load_ReadsVersionOneFilesAndSavesThemAsTheCurrentVersion()
    {
        var path = Path.Combine(_folder, "version1.msp");
        File.WriteAllText(path, """{"version":1,"pageMode":"pages","pageStyle":"lined","lineColor":"blue","pages":[{"strokes":[{"color":"black","width":3,"pressureEnabled":true,"points":[[112.5,241,0.098],[122.5,241,0.12]]}]}]}""");

        var document = MspFileService.Load(path);
        MspFileService.Save(document, path);

        var stroke = Assert.Single(Assert.Single(document.Pages).Strokes);
        Assert.True(stroke.FitToCurve);
        Assert.Empty(document.Pages[0].Images);
        Assert.Equal(AppConstants.FileFormatVersion, document.Version);
        Assert.StartsWith($"{{\"version\":{AppConstants.FileFormatVersion},", File.ReadAllText(path));
    }

    [Fact]
    public void Load_DropsImagesThatAreNoPngOrHaveNoSize()
    {
        var path = Path.Combine(_folder, "badimages.msp");
        var png = TestImages.Png(2, 2, System.Windows.Media.Colors.Blue);
        var notPng = Convert.ToBase64String("GIF89a"u8.ToArray());
        File.WriteAllText(path, $$"""{"version":2,"pages":[{"strokes":[],"images":[null,{"x":1,"y":2,"width":0,"height":5,"png":"{{png}}"},{"x":1,"y":2,"width":5,"height":5,"png":"no base64!"},{"x":1,"y":2,"width":5,"height":5,"png":"{{notPng}}"},{"x":-5e12,"y":3,"width":7,"height":8,"png":"{{png}}"}]}]}""");

        var image = Assert.Single(MspFileService.Load(path).Pages[0].Images);

        Assert.Equal((-AppConstants.MaxCoordinate, 3.0, 7.0, 8.0), (image.X, image.Y, image.Width, image.Height));
    }
}
