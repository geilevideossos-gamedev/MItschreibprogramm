using System.Windows.Controls;

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
    private bool _eraser;
    private bool _select;
    private bool _panning;

    public EditingModes(DocumentView document, SelectionEditor selection)
    {
        _document = document;
        _selection = selection;
        _sideButton = new SideButtonWatcher(document);
        _sideButton.Changed += Apply;
        selection.ActiveChanged += Apply;
    }

    public void SetTool(bool eraser, bool select)
    {
        (_eraser, _select) = (eraser, select);
        if (!select)
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
        var selecting = _select || _sideButton.IsHeld || _selection.Active;
        foreach (var page in _document.Pages)
        {
            page.Ink.EditingMode = _panning ? InkCanvasEditingMode.None
                : selecting ? InkCanvasEditingMode.Select
                : _eraser ? InkCanvasEditingMode.EraseByStroke : InkCanvasEditingMode.Ink;
            page.Ink.EditingModeInverted = _panning ? InkCanvasEditingMode.None : InkCanvasEditingMode.EraseByStroke;
        }
    }
}
