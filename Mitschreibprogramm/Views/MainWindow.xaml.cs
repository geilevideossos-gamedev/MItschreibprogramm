using System.ComponentModel;
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
    private readonly AppSettings _settings = new();
    private readonly ZoomPanController _zoomPan;
    private readonly FileSession _files;

    public MainWindow()
    {
        InitializeComponent();
        _zoomPan = new ZoomPanController(Scroller, Document);
        _zoomPan.ZoomChanged += UpdateStatus;
        Deactivated += (_, _) => _zoomPan.SetSpaceHeld(false);
        Document.PagesChanged += OnPagesChanged;
        _files = new FileSession(this, Document, _settings);
        _files.StateChanged += UpdateTitle;
        _files.DocumentLoaded += OnDocumentLoaded;
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
        _files.New();
        UpdateTitle();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        e.Cancel = !_files.ConfirmDiscard();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Space)
        {
            _zoomPan.SetSpaceHeld(true);
            e.Handled = true;
        }
        else if (_shortcuts.TryGetValue((Keyboard.Modifiers, e.Key), out var action))
        {
            action();
            e.Handled = true;
        }
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        base.OnPreviewKeyUp(e);
        if (e.Key == Key.Space)
        {
            _zoomPan.SetSpaceHeld(false);
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
        Add(ModifierKeys.Control, _files.New, Key.N);
        Add(ModifierKeys.Control, _files.Open, Key.O);
        Add(ModifierKeys.Control, () => _files.Save(), Key.S);
        Add(ModifierKeys.Control | ModifierKeys.Shift, () => _files.SaveAs(), Key.S);
        Add(ModifierKeys.Control, Document.Undo, Key.Z);
        Add(ModifierKeys.Control, Document.Redo, Key.Y);
        Add(ModifierKeys.Control, _zoomPan.ZoomIn, Key.OemPlus, Key.Add);
        Add(ModifierKeys.Control, _zoomPan.ZoomOut, Key.OemMinus, Key.Subtract);
        Add(ModifierKeys.Control, _zoomPan.ResetZoom, Key.D0, Key.NumPad0);
        Add(ModifierKeys.Control, Document.AddPage, Key.Enter);
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

    private void OnNewClick(object sender, RoutedEventArgs e) => _files.New();

    private void OnOpenClick(object sender, RoutedEventArgs e) => _files.Open();

    private void OnSaveClick(object sender, RoutedEventArgs e) => _files.Save();

    private void OnSaveAsClick(object sender, RoutedEventArgs e) => _files.SaveAs();

    private void OnPageStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        _settings.PageStyle = (PageStyle)((ComboBoxItem)PageStyleBox.SelectedItem).Tag;
        ApplyPageStyle();
    }

    private void OnLineColorClick(object sender, RoutedEventArgs e)
    {
        _settings.LineColor = _settings.LineColor == LineColor.Blue ? LineColor.Black : LineColor.Blue;
        ApplyPageStyle();
    }

    private void OnPageModeClick(object sender, RoutedEventArgs e) =>
        Document.SetMode(Document.Mode == PageMode.Pages ? PageMode.Endless : PageMode.Pages);

    private void OnAddPageClick(object sender, RoutedEventArgs e) => Document.AddPage();

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateStatus();

    private void OnPagesChanged()
    {
        var pages = Document.Mode == PageMode.Pages;
        _settings.PageMode = Document.Mode;
        PageModeButton.Content = pages ? "Modus: Seiten" : "Modus: Endlos";
        AddPageButton.IsEnabled = pages;
        UpdateStatus();
    }

    private void ApplyPageStyle()
    {
        Document.SetPageStyle(_settings.PageStyle, _settings.LineColor);
        LineColorButton.Content = _settings.LineColor == LineColor.Blue ? "Linien: Blau" : "Linien: Schwarz";
    }

    private void OnDocumentLoaded()
    {
        _settings.LineColor = Document.LineColor;
        PageStyleBox.SelectedIndex = (int)Document.PageStyle;
        ApplyPageStyle();
    }

    private void UpdateTitle()
    {
        Title = $"{_files.DisplayName}{(_files.IsDirty ? "*" : string.Empty)} - Mitschreibprogramm";
        FileText.Text = _files.FilePath ?? _files.DisplayName;
    }

    private void ApplyPenWidth(double width)
    {
        Document.SetPenWidth(width);
        WidthLabel.Text = $"{width:0.0} px";
    }

    private void UpdateStatus()
    {
        ZoomText.Text = $"Zoom {Document.Zoom:P0}";
        PageText.Text = Document.Mode == PageMode.Pages
            ? $"Seite {Document.PageNumberAt(Scroller, Scroller.ViewportHeight / 2)} von {Document.Pages.Count}"
            : "Endlos";
    }
}
