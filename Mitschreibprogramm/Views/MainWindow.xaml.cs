using System.Windows;
using System.Windows.Controls;
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
    }

    private void OnColorChecked(object sender, RoutedEventArgs e) =>
        Document.SetPenColor((PenColor)((RadioButton)sender).Tag);

    private void OnPresetClick(object sender, RoutedEventArgs e) =>
        WidthSlider.Value = (double)((Button)sender).Tag;

    private void ApplyPenWidth(double width)
    {
        Document.SetPenWidth(width);
        WidthLabel.Text = $"{width:0.0} px";
    }
}
