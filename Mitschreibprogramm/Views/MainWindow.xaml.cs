using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        foreach (var dot in new[] { ColorBlack, ColorBlue, ColorRed, ColorGreen })
        {
            dot.Background = new SolidColorBrush(Palette.Pen((PenColor)dot.Tag));
        }

        WidthSlider.ValueChanged += (_, e) => ApplyPenWidth(e.NewValue);
        WidthSlider.Value = AppConstants.MediumStrokeWidth;
        ColorBlack.IsChecked = true;
        PressureCheck.IsChecked = true;
        ToolPen.IsChecked = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.P:
                ToolPen.IsChecked = true;
                break;
            case Key.E:
                (ToolEraser.IsChecked == true ? ToolPen : ToolEraser).IsChecked = true;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void OnColorChecked(object sender, RoutedEventArgs e) =>
        Document.SetPenColor((PenColor)((RadioButton)sender).Tag);

    private void OnPresetClick(object sender, RoutedEventArgs e) =>
        WidthSlider.Value = (double)((Button)sender).Tag;

    private void OnPressureToggled(object sender, RoutedEventArgs e) =>
        Document.SetPressureEnabled(PressureCheck.IsChecked == true);

    private void OnToolChecked(object sender, RoutedEventArgs e) =>
        Document.SetEraser(ToolEraser.IsChecked == true);

    private void ApplyPenWidth(double width)
    {
        Document.SetPenWidth(width);
        WidthLabel.Text = $"{width:0.0} px";
    }
}
