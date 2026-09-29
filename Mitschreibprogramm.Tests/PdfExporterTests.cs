using System.IO;
using System.Text;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Mitschreibprogramm.Tests;

public sealed class PdfExporterTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("msp-pdf-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void PagesDocument_ExportsOneA4PagePerPage()
    {
        var document = new NoteDocument
        {
            PageMode = PageMode.Pages,
            PageStyle = PageStyle.Lined,
            Pages = [Page(Stroke(10, 10, 300, 200)), new NotePage(), Page(Stroke(50, 900, 400, 950))],
        };

        var path = Export(document, includeRuleLines: true);

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(File.ReadAllBytes(path), 0, 5));
        using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        Assert.Equal(3, pdf.PageCount);
        Assert.Equal(210, pdf.Pages[0].Width.Millimeter, 1);
        Assert.Equal(297, pdf.Pages[0].Height.Millimeter, 1);
    }

    [Fact]
    public void EndlessDocument_IsCutIntoA4Heights()
    {
        var document = new NoteDocument
        {
            PageMode = PageMode.Endless,
            Pages = [Page(Stroke(10, 10, 300, 200), Stroke(10, (2 * AppConstants.PageHeight) + 40, 300, (2 * AppConstants.PageHeight) + 90))],
        };

        using var pdf = PdfReader.Open(Export(document, includeRuleLines: false), PdfDocumentOpenMode.Import);

        Assert.Equal(3, pdf.PageCount);
    }

    [Fact]
    public void EndlessDocument_ContentRightOfA4WidthGetsItsOwnPage()
    {
        var document = new NoteDocument
        {
            PageMode = PageMode.Endless,
            Pages = [Page(Stroke(10, 10, 300, 200), Stroke(AppConstants.PageWidth + 30, 50, AppConstants.PageWidth + 200, 80))],
        };

        using var pdf = PdfReader.Open(Export(document, includeRuleLines: false), PdfDocumentOpenMode.Import);

        Assert.Equal(2, pdf.PageCount);
    }

    [Fact]
    public void DocumentWithoutPages_StillExportsOnePage()
    {
        using var pdf = PdfReader.Open(Export(new NoteDocument(), includeRuleLines: true), PdfDocumentOpenMode.Import);

        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void RuleLines_AreOnlyDrawnWhenRequested()
    {
        var document = new NoteDocument { PageStyle = PageStyle.Squared, Pages = [Page(Stroke(10, 10, 300, 200))] };

        var withLines = new FileInfo(Export(document, includeRuleLines: true)).Length;
        var withoutLines = new FileInfo(Export(document, includeRuleLines: false)).Length;

        Assert.True(withLines > withoutLines);
    }

    [Fact]
    public void SquaredGrid_DrawsMoreLinesThanRuledPaper()
    {
        long Size(PageStyle style) => new FileInfo(Export(new NoteDocument { PageStyle = style, Pages = [new NotePage()] }, includeRuleLines: true)).Length;

        Assert.True(Size(PageStyle.Squared) > Size(PageStyle.Lined));
        Assert.True(Size(PageStyle.Lined) > Size(PageStyle.Blank));
    }

    [Fact]
    public void SinglePointAndConstantWidthStrokes_AreExported()
    {
        var dot = new NoteStroke { Color = PenColor.Red, Width = 4, PressureEnabled = true, Points = [[100, 100, 0.8]] };
        var constant = new NoteStroke { Color = PenColor.Green, Width = 2, PressureEnabled = false, Points = [[10, 10, 0.5], [60, 40, 0.5], [120, 10, 0.5]] };

        using var pdf = PdfReader.Open(Export(new NoteDocument { Pages = [Page(dot, constant)] }, includeRuleLines: false), PdfDocumentOpenMode.Import);

        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void PressureSegments_FollowThePressureEvenOnAStraightTwoPointStroke()
    {
        var stroke = new NoteStroke { Color = PenColor.Black, Width = 4, PressureEnabled = true, Points = [[0, 0, 0], [400, 0, 1]] };

        var segments = PdfExporter.PressureSegments(stroke).ToList();

        Assert.True(segments.Count >= 20);
        Assert.InRange(segments[0].Width, 1.0, 1.25);
        Assert.InRange(segments[^1].Width, 6.75, 7.0);
        Assert.All(segments.Zip(segments.Skip(1)), pair => Assert.InRange(pair.Second.Width - pair.First.Width, 0, 0.2501));
        Assert.Equal(0, segments[0].From.X, 6);
        Assert.Equal(400, segments[^1].To.X, 6);
        Assert.All(segments.Zip(segments.Skip(1)), pair => Assert.Equal(pair.First.To.X, pair.Second.From.X, 6));
    }

    [Fact]
    public void Images_AreEmbeddedInThePdf()
    {
        var png = TestImages.Png(40, 30, System.Windows.Media.Colors.OrangeRed);
        var document = new NoteDocument
        {
            Pages = [new NotePage { Images = [new NoteImage { X = 100, Y = 200, Width = 400, Height = 300, Png = png }], Strokes = [Stroke(150, 250, 300, 280)] }],
        };

        var path = Export(document, includeRuleLines: true);

        using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        Assert.Equal(1, pdf.PageCount);
        var objects = pdf.Pages[0].Elements.GetDictionary("/Resources")!.Elements.GetDictionary("/XObject")!;
        var image = Assert.Single(
            objects.Elements.Values.Select(value => value is PdfReference reference ? reference.Value : value).OfType<PdfDictionary>(),
            candidate => candidate.Elements.GetName("/Subtype") == "/Image");
        Assert.Equal((40, 30), (image.Elements.GetInteger("/Width"), image.Elements.GetInteger("/Height")));
    }

    [Fact]
    public void ShapeStrokes_KeepTheirCornersInsteadOfTheCurveFit()
    {
        var rectangle = new NoteStroke
        {
            Color = PenColor.Black,
            Width = 3,
            PressureEnabled = true,
            FitToCurve = false,
            Points = [[100, 100, 0.5], [300, 100, 0.5], [300, 200, 0.5], [100, 200, 0.5], [100, 100, 0.5]],
        };

        var segments = PdfExporter.PressureSegments(rectangle).ToList();

        Assert.Equal(4, segments.Count);
        Assert.Equal((300.0, 100.0), (segments[0].To.X, segments[0].To.Y));
        Assert.Equal((300.0, 200.0), (segments[1].To.X, segments[1].To.Y));
        Assert.All(segments, segment => Assert.Equal(3, segment.Width, 6));
    }

    private string Export(NoteDocument document, bool includeRuleLines)
    {
        var path = Path.Combine(_folder, $"{Guid.NewGuid():N}.pdf");
        PdfExporter.Export(document, path, includeRuleLines);
        return path;
    }

    private static NotePage Page(params NoteStroke[] strokes) => new() { Strokes = [.. strokes] };

    private static NoteStroke Stroke(double x1, double y1, double x2, double y2) => new()
    {
        Color = PenColor.Blue,
        Width = 3,
        PressureEnabled = true,
        Points = [[x1, y1, 0.2], [(x1 + x2) / 2, (y1 + y2) / 2, 0.6], [x2, y2, 0.9]],
    };
}
