using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Rendering;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Views;

public partial class MainWindow : Window
{
    // A held key may repeat width, zoom and undo steps, but must not flicker toggles such as E or Ctrl+D.
    private static readonly HashSet<Key> RepeatableKeys = [Key.OemPlus, Key.Add, Key.OemMinus, Key.Subtract, Key.Z, Key.Y];

    private readonly Dictionary<(ModifierKeys, Key), Action> _shortcuts = [];
    private readonly SettingsService _settingsService = new(AppPaths.SettingsFile);

    private readonly AppSettings _settings;
    private readonly ZoomPanController _zoomPan;
    private readonly NotebookSession _session;

    public MainWindow()
    {
        _settings = _settingsService.Load();
        Theme.Apply(_settings.DarkMode);
        InitializeComponent();
        RestoreWindowPlacement();
        _zoomPan = new ZoomPanController(Scroller, Document);
        _zoomPan.ZoomChanged += UpdateStatus;
        Deactivated += (_, _) => _zoomPan.SetSpaceHeld(false);
        Document.PagesChanged += OnPagesChanged;
        _session = new NotebookSession(this, Document, Scroller, _settings, new NotebookLibrary(AppPaths.NotesFolder));
        _session.Changed += OnSessionChanged;
        _session.DocumentLoaded += OnDocumentLoaded;
        Notebooks.NewRequested += _session.New;
        Notebooks.OpenRequested += _session.Open;
        Notebooks.RenameRequested += _session.Rename;
        Notebooks.DeleteRequested += _session.Delete;
        Notebooks.ExportPdfRequested += _session.ExportPdf;
        Notebooks.ExportMspRequested += _session.ExportMsp;
        RegisterShortcuts();

        ApplyDarkMode();
        WidthSlider.ValueChanged += (_, e) => ApplyPenWidth(e.NewValue);
        WidthSlider.Value = _settings.StrokeWidth;
        ApplyPenWidth(WidthSlider.Value);
        new[] { ColorBlack, ColorBlue, ColorRed, ColorGreen }[(int)_settings.PenColor].IsChecked = true;
        PressureCheck.IsChecked = _settings.PressureEnabled;
        Document.SetPressureEnabled(_settings.PressureEnabled);
        ToolPen.IsChecked = true;
        NotebookPanelButton.IsChecked = _settings.NotebookPanelVisible;
        ApplyNotebookPanel();
        _session.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Theme.ApplyTitleBar(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        e.Cancel = !_session.TryClose();
        if (!e.Cancel)
        {
            StoreWindowPlacement();
            _settingsService.Save(_settings);
        }
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
            if (!e.IsRepeat || RepeatableKeys.Contains(e.Key))
            {
                action();
            }

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

        Add(ModifierKeys.None, () => ColorBlack.IsChecked = true, Key.D1, Key.NumPad1);
        Add(ModifierKeys.None, () => ColorBlue.IsChecked = true, Key.D2, Key.NumPad2);
        Add(ModifierKeys.None, () => ColorRed.IsChecked = true, Key.D3, Key.NumPad3);
        Add(ModifierKeys.None, () => ColorGreen.IsChecked = true, Key.D4, Key.NumPad4);
        Add(ModifierKeys.None, () => WidthSlider.Value += AppConstants.StrokeWidthStep, Key.OemPlus, Key.Add);
        Add(ModifierKeys.None, () => WidthSlider.Value -= AppConstants.StrokeWidthStep, Key.OemMinus, Key.Subtract);
        Add(ModifierKeys.None, () => ToolPen.IsChecked = true, Key.P);
        Add(ModifierKeys.None, () => (ToolEraser.IsChecked == true ? ToolPen : ToolEraser).IsChecked = true, Key.E);
        Add(ModifierKeys.Control, _session.New, Key.N);
        Add(ModifierKeys.Control, _session.Import, Key.O);
        Add(ModifierKeys.Control, _session.SaveNow, Key.S);
        Add(ModifierKeys.Control | ModifierKeys.Shift, () => _session.ExportMsp(null), Key.S);
        Add(ModifierKeys.Control, () => _session.ExportPdf(null), Key.E);
        Add(ModifierKeys.Control, () => NotebookPanelButton.IsChecked = NotebookPanelButton.IsChecked != true, Key.B);
        Add(ModifierKeys.Control, Document.Undo, Key.Z);
        Add(ModifierKeys.Control, Document.Redo, Key.Y);
        Add(ModifierKeys.Control, _zoomPan.ZoomIn, Key.OemPlus, Key.Add);
        Add(ModifierKeys.Control, _zoomPan.ZoomOut, Key.OemMinus, Key.Subtract);
        Add(ModifierKeys.Control, _zoomPan.ResetZoom, Key.D0, Key.NumPad0);
        Add(ModifierKeys.Control, Document.AddPage, Key.Enter);
        Add(ModifierKeys.Control, () => SetDarkMode(!_settings.DarkMode), Key.D);
        Add(ModifierKeys.Control, () => PageStyleBox.SelectedIndex = (PageStyleBox.SelectedIndex + 1) % PageStyleBox.Items.Count, Key.L);
    }

    private void OnColorChecked(object sender, RoutedEventArgs e)
    {
        _settings.PenColor = (PenColor)((RadioButton)sender).Tag;
        Document.SetPenColor(_settings.PenColor);
    }

    private void OnPresetClick(object sender, RoutedEventArgs e) =>
        WidthSlider.Value = (double)((Button)sender).Tag;

    private void OnPressureToggled(object sender, RoutedEventArgs e)
    {
        _settings.PressureEnabled = PressureCheck.IsChecked == true;
        Document.SetPressureEnabled(_settings.PressureEnabled);
    }

    private void OnToolChecked(object sender, RoutedEventArgs e) =>
        Document.SetEraser(ToolEraser.IsChecked == true);

    private void OnNewClick(object sender, RoutedEventArgs e) => _session.New();

    private void OnImportClick(object sender, RoutedEventArgs e) => _session.Import();

    private void OnExportMspClick(object sender, RoutedEventArgs e) => _session.ExportMsp(null);

    private void OnExportClick(object sender, RoutedEventArgs e) => _session.ExportPdf(null);

    private void OnNotebookPanelToggled(object sender, RoutedEventArgs e)
    {
        _settings.NotebookPanelVisible = NotebookPanelButton.IsChecked == true;
        ApplyNotebookPanel();
    }

    private void ApplyNotebookPanel() =>
        Notebooks.Visibility = _settings.NotebookPanelVisible ? Visibility.Visible : Visibility.Collapsed;

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

    private void OnDarkModeToggled(object sender, RoutedEventArgs e) => SetDarkMode(DarkModeButton.IsChecked == true);

    private void SetDarkMode(bool dark)
    {
        if (_settings.DarkMode == dark)
        {
            return;
        }

        _settings.DarkMode = dark;
        Theme.Apply(dark);
        ApplyDarkMode();
    }

    private void ApplyDarkMode()
    {
        DarkModeButton.IsChecked = _settings.DarkMode;
        Document.SetDarkMode(_settings.DarkMode);
        foreach (var dot in new[] { ColorBlack, ColorBlue, ColorRed, ColorGreen })
        {
            dot.Background = new SolidColorBrush(Palette.PenOnPage((PenColor)dot.Tag, _settings.DarkMode));
        }
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

    private void OnSessionChanged()
    {
        Notebooks.Show(_session.Notebooks, _session.ActiveId);
        Title = $"{_session.ActiveName}{(_session.SaveFailed ? " (nicht gespeichert)" : string.Empty)} - Mitschreibprogramm";
        FileText.Text = _session.ActiveName;
    }

    private void ApplyPenWidth(double width)
    {
        _settings.StrokeWidth = width;
        Document.SetPenWidth(width);
        WidthLabel.Text = $"{width:0.0} px";
    }

    private void RestoreWindowPlacement()
    {
        if (_settings is { WindowLeft: { } left, WindowTop: { } top, WindowWidth: > 0 and { } width, WindowHeight: > 0 and { } height })
        {
            var visible = new Rect(left, top, width, height);
            visible.Intersect(new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight));
            if (visible.Width >= AppConstants.MinVisibleWindowPart && visible.Height >= AppConstants.MinVisibleWindowPart)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                (Left, Top, Width, Height) = (left, top, width, height);
            }
        }

        if (_settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void StoreWindowPlacement()
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        (_settings.WindowLeft, _settings.WindowTop, _settings.WindowWidth, _settings.WindowHeight) =
            (bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        _settings.WindowMaximized = WindowState == WindowState.Maximized;
    }

    private void UpdateStatus()
    {
        ZoomText.Text = $"Zoom {Document.Zoom:P0}";
        PageText.Text = Document.Mode == PageMode.Pages
            ? $"Seite {Document.PageNumberAt(Scroller, Scroller.ViewportHeight / 2)} von {Document.Pages.Count}"
            : "Endlos";
    }
}
