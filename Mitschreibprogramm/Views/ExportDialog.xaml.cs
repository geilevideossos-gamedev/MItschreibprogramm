using System.Windows;

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

    private void OnExportClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
