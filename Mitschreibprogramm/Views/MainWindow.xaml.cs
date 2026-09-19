using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;

namespace Mitschreibprogramm.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<(ModifierKeys, Key), Action> _shortcuts = [];
    private LineColor _lineColor = LineColor.Blue;

    public MainWindow()
    {
        InitializeComponent();
        RegisterShortcuts();

        foreach (var dot in new[] { ColorBlack, ColorBlue, ColorRed, ColorGreen })
        {
            dot.Background = new SolidColorBrush(Palette.Pen((PenColor)dot.Tag));
        }

        WidthSlider.ValueChanged += (_, e) => ApplyPenWidth(e.NewValue);
        WidthSlider.Value = AppConstants.MediumStrokeWidth;
        ColorBlack.IsChecked = true;
        PressureCheck.IsChecked = true;
        ToolPen.IsChecked = true;
        PageStyleBox.SelectedIndex = (int)PageStyle.Lined;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (_shortcuts.TryGetValue((Keyboard.Modifiers, e.Key), out var action))
        {
            action();
            e.Handled = true;
        }
    }

    private void RegisterShortcuts()
    {
        void Add(ModifierKeys modifiers, Action action, params Key[] keys)
        {
            foreach (var key in keys)
            {
                _shortcuts[(modifiers, key)] = action;
            }
        }

        Add(ModifierKeys.None, () => ToolPen.IsChecked = true, Key.P);
        Add(ModifierKeys.None, () => (ToolEraser.IsChecked == true ? ToolPen : ToolEraser).IsChecked = true, Key.E);
        Add(ModifierKeys.Control, Document.Undo, Key.Z);
        Add(ModifierKeys.Control, Document.Redo, Key.Y);
        Add(ModifierKeys.Control, () => PageStyleBox.SelectedIndex = (PageStyleBox.SelectedIndex + 1) % PageStyleBox.Items.Count, Key.L);
    }

    private void OnColorChecked(object sender, RoutedEventArgs e) =>
        Document.SetPenColor((PenColor)((RadioButton)sender).Tag);

    private void OnPresetClick(object sender, RoutedEventArgs e) =>
        WidthSlider.Value = (double)((Button)sender).Tag;

    private void OnPressureToggled(object sender, RoutedEventArgs e) =>
        Document.SetPressureEnabled(PressureCheck.IsChecked == true);

    private void OnToolChecked(object sender, RoutedEventArgs e) =>
        Document.SetEraser(ToolEraser.IsChecked == true);

    private void OnPageStyleChanged(object sender, SelectionChangedEventArgs e) => ApplyPageStyle();

    private void OnLineColorClick(object sender, RoutedEventArgs e)
    {
        _lineColor = _lineColor == LineColor.Blue ? LineColor.Black : LineColor.Blue;
        ApplyPageStyle();
    }

    private void ApplyPageStyle()
    {
        var style = (PageStyle)((ComboBoxItem)PageStyleBox.SelectedItem).Tag;
        Document.SetPageStyle(style, _lineColor);
        LineColorButton.Content = _lineColor == LineColor.Blue ? "Linien: Blau" : "Linien: Schwarz";
    }

    private void ApplyPenWidth(double width)
    {
        Document.SetPenWidth(width);
        WidthLabel.Text = $"{width:0.0} px";
    }
}
