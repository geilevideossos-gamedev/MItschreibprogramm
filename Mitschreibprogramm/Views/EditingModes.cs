using System.Windows.Controls;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

// Which InkCanvas editing mode every page is in. Lasso selection wins while the selection tool is on, while the pen's
// barrel (upper side) button is held, and while something is selected: InkCanvas drops the selection on every mode
// change, so it has to stay in Select mode until the selection ends. An inverted pen (side button set to "Erase" in
// the driver) always erases.
public sealed class EditingModes
{
    private readonly DocumentView _document;
    private readonly SelectionEditor _selection;
    private readonly SideButtonWatcher _sideButton;
    private readonly ToolSwitch _tools = new();
    private bool _panning;

    public EditingModes(DocumentView document, SelectionEditor selection)
    {
        _document = document;
        _selection = selection;
        _sideButton = new SideButtonWatcher(document);
        _sideButton.Changed += Apply;
        // A lasso ends inside InkCanvas's own pen-up handling, where any mode change throws (docs/status.md).
        selection.ActiveChanged += () => document.Dispatcher.BeginInvoke(FollowSelection);
    }

    public event Action? ToolChanged;

    public Tool Tool => _tools.Current;

    public void SetTool(Tool tool)
    {
        _tools.Choose(tool);
        if (tool != Tool.Select)
        {
            _selection.Clear();
        }

        Apply();
    }

    public void SetPanning(bool panning)
    {
        _panning = panning;
        Apply();
    }

    public void Apply()
    {
        var selecting = _tools.Current == Tool.Select || _sideButton.IsHeld || _selection.Active;
        foreach (var page in _document.Pages)
        {
            page.Ink.EditingMode = _panning ? InkCanvasEditingMode.None
                : selecting ? InkCanvasEditingMode.Select
                : _tools.Current == Tool.Eraser ? InkCanvasEditingMode.EraseByStroke : InkCanvasEditingMode.Ink;
            page.Ink.EditingModeInverted = _panning ? InkCanvasEditingMode.None : InkCanvasEditingMode.EraseByStroke;
        }
    }

    private void FollowSelection()
    {
        var before = _tools.Current;
        if (_selection.Active)
        {
            _tools.SelectionStarted();
        }
        else
        {
            _tools.SelectionEnded();
        }

        Apply();
        if (_tools.Current != before)
        {
            DebugLog.Write($"tool now={_tools.Current}");
            ToolChanged?.Invoke();
        }
    }
}
