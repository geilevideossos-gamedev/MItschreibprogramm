using System.Windows;

namespace Mitschreibprogramm.Views;

public partial class UnsavedChangesDialog : Window
{
    private UnsavedChoice _choice = UnsavedChoice.Cancel;

    private UnsavedChangesDialog(string fileName)
    {
        InitializeComponent();
        Message.Text = $"\"{fileName}\" hat ungespeicherte Änderungen. Vor dem Fortfahren speichern?";
    }

    public static UnsavedChoice Ask(Window owner, string fileName)
    {
        var dialog = new UnsavedChangesDialog(fileName) { Owner = owner };
        dialog.ShowDialog();
        return dialog._choice;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) => Close(UnsavedChoice.Save);

    private void OnDiscardClick(object sender, RoutedEventArgs e) => Close(UnsavedChoice.Discard);

    private void Close(UnsavedChoice choice)
    {
        _choice = choice;
        Close();
    }
}
