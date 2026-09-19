namespace Mitschreibprogramm.Models;

public sealed class NoteDocument
{
    public int Version { get; set; } = AppConstants.FileFormatVersion;

    public PageMode PageMode { get; set; }

    public PageStyle PageStyle { get; set; }

    public LineColor LineColor { get; set; }

    public List<NotePage> Pages { get; set; } = [];
}
