namespace Mitschreibprogramm.Models;

public sealed class NotebookEntry
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset Modified { get; set; }

    public double ScrollX { get; set; }

    public double ScrollY { get; set; }
}
