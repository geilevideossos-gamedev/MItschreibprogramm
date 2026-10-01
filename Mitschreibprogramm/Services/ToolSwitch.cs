using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Services;

// A selection made from the pen or eraser (lasso with the side button) switches to the selection tool until the
// selection ends, then the tool from before comes back. A tool chosen in between replaces it.
public sealed class ToolSwitch
{
    private Tool? _before;

    public Tool Current { get; private set; } = Tool.Pen;

    public void Choose(Tool tool)
    {
        if (tool != Current)
        {
            (Current, _before) = (tool, null);
        }
    }

    public void SelectionStarted()
    {
        if (Current != Tool.Select)
        {
            (_before, Current) = (Current, Tool.Select);
        }
    }

    public void SelectionEnded()
    {
        if (_before is { } tool)
        {
            (Current, _before) = (tool, null);
        }
    }
}
