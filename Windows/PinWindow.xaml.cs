using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickShot.Services;

namespace QuickShot.Windows;

public partial class PinWindow : Window
{
    private readonly BitmapSource _source;
    private double _zoom = 1;
    private bool _adjusting;
    public string CloseShortcut { get; set; }

    public PinWindow(BitmapSource source, string closeShortcut)
    {
        InitializeComponent();
        _source = source;
        CloseShortcut = closeShortcut;
        PinnedImage.Source = source;
        UseLayoutRounding = true;
        SourceInitialized += (_, _) => FitImage();
        Loaded += (_, _) => FitImage();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(new Action(FitImage));
    }
    private void FitImage()
    {
        if (_adjusting) return;
        _adjusting = true;
        try
        {
            var area = WindowLayout.WorkArea(this);
            var dpi = VisualTreeHelper.GetDpi(this);
            double naturalWidth = _source.PixelWidth / dpi.DpiScaleX;
            double naturalHeight = _source.PixelHeight / dpi.DpiScaleY;
            double scale = Math.Min(_zoom, Math.Min(Math.Max(1, area.Width - 2) / naturalWidth,
                Math.Max(1, area.Height - 2) / naturalHeight));
            Width = naturalWidth * scale + 2;
            Height = naturalHeight * scale + 2;
            if (IsLoaded)
            {
                Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
                Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
            }
        }
        finally { _adjusting = false; }
    }
    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!IsActive || e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase or System.Windows.Controls.PasswordBox) return;
        if (!HotkeyGesture.TryParse(CloseShortcut, out var gesture)) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        uint modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 1;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 2;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 4;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= 8;
        if (key == gesture.Key && modifiers == (uint)(gesture.Modifiers & ~HotkeyModifiers.NoRepeat))
        {
            e.Handled = true;
            Close();
        }
    }
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || e.Handled) return;
        Activate();
        DragMove();
        FitImage();
    }
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.1 : 0.9), 0.1, 8);
        FitImage();
        e.Handled = true;
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetImage(_source); }
        catch (Exception ex) { MessageWindow.Show(ex.Message, "复制失败", this); }
    }
    private void ResetSize_Click(object sender, RoutedEventArgs e) { _zoom = 1; FitImage(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
