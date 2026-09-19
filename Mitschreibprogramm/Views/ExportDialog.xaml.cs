using System.Windows;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public partial class ExportDialog : Window
{
    private ExportDialog()
    {
        InitializeComponent();
    }

    // Returns whether rule lines are wanted, or null when the dialog was cancelled.
    public static bool? Ask(Window owner)
    {
        var dialog = new ExportDialog { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.RuleLinesCheck.IsChecked == true : null;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Theme.ApplyTitleBar(this);
    }

    private void OnExportClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
