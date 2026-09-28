using System.Windows;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public partial class RenameDialog : Window
{
    private RenameDialog(string currentName)
    {
        InitializeComponent();
        NameBox.Text = currentName;
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    // Returns the trimmed new name, or null when the dialog was cancelled.
    public static string? Ask(Window owner, string currentName)
    {
        var dialog = new RenameDialog(currentName) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.NameBox.Text.Trim() : null;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Theme.ApplyTitleBar(this);
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text.Trim().Length > 0)
        {
            DialogResult = true;
        }
    }
}
