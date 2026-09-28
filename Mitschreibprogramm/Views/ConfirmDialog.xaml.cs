using System.Windows;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public partial class ConfirmDialog : Window
{
    private ConfirmDialog(string title, string message, string confirmLabel)
    {
        InitializeComponent();
        Title = title;
        Message.Text = message;
        ConfirmButton.Content = confirmLabel;
    }

    public static bool Ask(Window owner, string title, string message, string confirmLabel) =>
        new ConfirmDialog(title, message, confirmLabel) { Owner = owner }.ShowDialog() == true;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Theme.ApplyTitleBar(this);
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
