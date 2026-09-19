namespace Mitschreibprogramm.Services;

public sealed class UndoHistory
{
    private readonly Stack<UndoStep> _undo = new();
    private readonly Stack<UndoStep> _redo = new();

    public void Push(UndoStep step)
    {
        _undo.Push(step);
        _redo.Clear();
    }

    public bool Undo()
    {
        if (!_undo.TryPop(out var step))
        {
            return false;
        }

        step.Undo();
        _redo.Push(step);
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var step))
        {
            return false;
        }

        step.Redo();
        _undo.Push(step);
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
