namespace Mitschreibprogramm.Models;

public sealed class NotebookIndex
{
    public string? LastOpen { get; set; }

    public List<NotebookEntry> Notebooks { get; set; } = [];
}
