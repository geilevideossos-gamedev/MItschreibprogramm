using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mitschreibprogramm.Models;

namespace Mitschreibprogramm.Views;

public partial class NotebookPanel : UserControl
{
    private bool _refreshing;

    public NotebookPanel()
    {
        InitializeComponent();
    }

    public event Action? NewRequested;

    public event Action<string>? OpenRequested;

    public event Action<string>? RenameRequested;

    public event Action<string>? DeleteRequested;

    public event Action<string>? ExportPdfRequested;

    public event Action<string>? ExportMspRequested;

    // The entries carry no change notification, so the whole list is set again after every change.
    public void Show(IReadOnlyList<NotebookEntry> notebooks, string? activeId)
    {
        _refreshing = true;
        NotebookList.ItemsSource = notebooks;
        NotebookList.SelectedItem = notebooks.FirstOrDefault(entry => entry.Id == activeId);
        _refreshing = false;
    }

    private void OnNewClick(object sender, RoutedEventArgs e) => NewRequested?.Invoke();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_refreshing && NotebookList.SelectedItem is NotebookEntry entry)
        {
            OpenRequested?.Invoke(entry.Id);
        }
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && EntryOf(e.OriginalSource) is { } entry)
        {
            RenameRequested?.Invoke(entry.Id);
        }
    }

    // A right-click would select and therefore open the item; it should only show the menu for it.
    private void OnItemRightButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void OnRenameMenu(object sender, RoutedEventArgs e) => Raise(sender, RenameRequested);

    private void OnDeleteMenu(object sender, RoutedEventArgs e) => Raise(sender, DeleteRequested);

    private void OnExportPdfMenu(object sender, RoutedEventArgs e) => Raise(sender, ExportPdfRequested);

    private void OnExportMspMenu(object sender, RoutedEventArgs e) => Raise(sender, ExportMspRequested);

    // The context menu inherits the right-clicked item's entry as DataContext; the selection is left alone.
    private static void Raise(object sender, Action<string>? handler)
    {
        if (EntryOf(sender) is { } entry)
        {
            handler?.Invoke(entry.Id);
        }
    }

    private static NotebookEntry? EntryOf(object source) => (source as FrameworkElement)?.DataContext as NotebookEntry;
}
