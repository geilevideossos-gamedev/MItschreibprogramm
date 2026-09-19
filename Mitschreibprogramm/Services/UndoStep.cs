namespace Mitschreibprogramm.Services;

public sealed record UndoStep(Action Undo, Action Redo);
