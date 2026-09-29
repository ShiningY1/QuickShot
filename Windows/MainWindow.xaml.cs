using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using QuickShot.Models;
using QuickShot.Services;
using DrawingIcon = System.Drawing.Icon;

namespace QuickShot.Windows;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly ScreenCaptureService _captureService = new();
    private readonly OcrService _ocrService = new();
    private AppSettings _settings;
    private GlobalHotkeyManager? _hotkeys;
    private readonly DrawingIcon _applicationIcon;
    private TrayService? _trayIcon;
    private bool _captureInProgress, _allowExit, _exiting;
    private Task? _ocrTask;
    private OcrWindow? _ocrWindow;

    public MainWindow()
    {
        InitializeComponent();
        WindowLayout.Apply(this, 28);
        _settings = _settingsService.Load();
        _applicationIcon = LoadApplicationIcon();
        Loaded += (_, _) =>
        {
            if (_settingsService.LoadWarning is { } warning) MessageWindow.Show(warning, "设置读取提示", this);
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        nint handle = new WindowInteropHelper(this).Handle;
        _hotkeys = new GlobalHotkeyManager(handle);
        _trayIcon = new TrayService(handle, _applicationIcon, ShowMainWindow, ShowTrayMenu);
        if (!_trayIcon.IsAvailable)
            Dispatcher.BeginInvoke(new Action(() => MessageWindow.Show("无法创建系统托盘图标。主窗口会保持可见；请使用菜单退出。", "托盘不可用", this)));
        try { RegisterHotkeys(_settings); }
        catch (Exception ex) { Dispatcher.BeginInvoke(new Action(() => MessageWindow.Show(ex.Message, "快捷键注册失败", this))); }
    }

    private void RegisterHotkeys(AppSettings settings)
    {
        if (_hotkeys is null) return;
        _hotkeys.UnregisterAll();
        try
        {
            _hotkeys.Register(101, settings.SaveHotkey, () => StartCapture(CaptureAction.Save));
            _hotkeys.Register(102, settings.CopyHotkey, () => StartCapture(CaptureAction.Copy));
            _hotkeys.Register(103, settings.OcrHotkey, () => StartCapture(CaptureAction.Ocr));
            _hotkeys.Register(104, settings.PinHotkey, () => StartCapture(CaptureAction.Pin));
        }
        catch { _hotkeys.UnregisterAll(); throw; }
    }

    private string? ApplySettings(AppSettings candidate)
    {
        try
        {
            RegisterHotkeys(candidate);
            _settingsService.Save(candidate);
            _settings = candidate; // Commit only after both registration and persistent save succeed.
            foreach (var pin in System.Windows.Application.Current.Windows.OfType<PinWindow>())
                pin.CloseShortcut = candidate.ClosePinHotkey;
            return null;
        }
        catch (Exception ex)
        {
            string error = ex.Message;
            try { RegisterHotkeys(_settings); }
            catch (Exception rollback) { error += "\n原快捷键恢复失败：" + rollback.Message; }
            return error;
        }
    }

    private async void StartCapture(CaptureAction action)
    {
        if (_captureInProgress || _exiting) return;
        if (action == CaptureAction.Ocr && _ocrTask is { IsCompleted: false })
        {
            if (_ocrWindow is { IsVisible: true }) _ocrWindow.Activate();
            else MessageWindow.Show("上一次 OCR 正在结束，请稍候。其他截图功能可以继续使用。", "OCR 正在处理");
            return;
        }
        _captureInProgress = true;
        bool wasVisible = IsVisible;
        Hide();
        try
        {
            await Task.Delay(120);
            if (_exiting) return;
            using Bitmap? bitmap = _captureService.CaptureSelection();
            if (bitmap is null || _exiting) return;
            switch (action)
            {
                case CaptureAction.Save: SaveBitmap(bitmap); break;
                case CaptureAction.Copy:
                    System.Windows.Clipboard.SetImage(BitmapInterop.ToBitmapSource(bitmap));
                    Notify("截图已复制到剪贴板"); break;
                case CaptureAction.Ocr:
                    // Own a separate bitmap until the background OCR task has completed.
                    _ocrTask = RunOcrAsync((Bitmap)bitmap.Clone()); break;
                case CaptureAction.Pin:
                    new PinWindow(BitmapInterop.ToBitmapSource(bitmap), _settings.ClosePinHotkey).Show(); break;
            }
        }
        catch (Exception ex) { MessageWindow.Show(ex.Message, "操作失败"); }
        finally
        {
            _captureInProgress = false;
            if (wasVisible && !_exiting) ShowMainWindow();
            if (action == CaptureAction.Ocr && _ocrWindow is { IsVisible: true } && !_exiting) _ocrWindow.Activate();
        }
    }

    private async Task RunOcrAsync(Bitmap bitmap)
    {
        using (bitmap)
        {
            var window = new OcrWindow();
            _ocrWindow = window;
            window.Show();
            try
            {
                var result = await _ocrService.RecognizeAsync(bitmap, _settings.OcrInputMode,
                    _settings.OcrDetectRotation, new Progress<string>(window.Report), window.Cancellation.Token);
                if (!_exiting) window.Complete(result);
            }
            catch (OperationCanceledException) { window.Fail("识别已取消。"); }
            catch (Exception ex) { window.Fail(ex.Message); }
            finally { StatusText.Text = "OCR 已结束；可再次框选识别。"; }
        }
    }

    private void SaveBitmap(Bitmap bitmap)
    {
        string directory = SaveDirectory.Ensure(_settings.DefaultSaveDirectory);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存截图", InitialDirectory = directory,
            FileName = Path.Combine(directory, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"),
            DefaultExt = ".png", Filter = "PNG 图片 (*.png)|*.png", AddExtension = true, OverwritePrompt = true
        };
        if (dialog.ShowDialog() == true)
        {
            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            Notify($"截图已保存：{dialog.FileName}");
        }
    }

    private void OpenSettings()
    {
        if (_exiting) return;
        ShowMainWindow();
        new SettingsWindow(_settings, ApplySettings) { Owner = this }.ShowDialog();
    }
    private System.Windows.Controls.ContextMenu BuildMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();
        void Add(string title, Action action)
        {
            var item = new System.Windows.Controls.MenuItem { Header = title };
            item.Click += (_, _) => action(); menu.Items.Add(item);
        }
        Add("显示主窗口", ShowMainWindow);
        Add("打开截图目录", OpenDirectory);
        Add("设置", OpenSettings);
        Add("关于", () => MessageWindow.Show("QuickShot\n截图、OCR 与贴图工具\n本地 PaddleOCR 中文 V5", "关于 QuickShot", this));
        menu.Items.Add(new System.Windows.Controls.Separator());
        Add("退出", ExitApplication);
        return menu;
    }
    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        var menu = BuildMenu(); menu.PlacementTarget = MenuButton; menu.IsOpen = true;
    }
    private void ShowTrayMenu()
    {
        // Foreground ownership lets Windows dismiss the menu on an outside click.
        SetForegroundWindow(new WindowInteropHelper(this).Handle);
        var menu = BuildMenu();
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }
    private void Notify(string message)
    {
        StatusText.Text = message;
        if (_settings.ShowNotifications) _trayIcon?.Notify(message);
    }
    private static DrawingIcon LoadApplicationIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/QuickShot.ico"))
            ?? throw new FileNotFoundException("缺少 QuickShot 图标资源。");
        using (resource.Stream)
        using (var icon = new DrawingIcon(resource.Stream, new System.Drawing.Size(32, 32)))
            return (DrawingIcon)icon.Clone();
    }
    private void ShowMainWindow()
    {
        if (_exiting) return;
        Show(); WindowState = WindowState.Normal; Activate();
    }
    private async void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        _hotkeys?.UnregisterAll();
        Show();
        StatusText.Text = "正在退出；等待当前 OCR 本机调用安全结束…";
        _ocrWindow?.Cancellation.Cancel();
        if (_ocrTask is not null) await _ocrTask;
        await _ocrService.DisposeAsync();
        _allowExit = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            if (!_exiting && _trayIcon is { IsAvailable: true }) { Hide(); Notify("QuickShot 已在后台运行"); }
            else if (!_exiting) MessageWindow.Show("系统托盘不可用，请使用菜单中的“退出”。", "QuickShot", this);
            return;
        }
        _hotkeys?.Dispose(); _trayIcon?.Dispose(); _applicationIcon.Dispose();
    }
    private void OpenDirectory()
    {
        try { SaveDirectory.Open(_settings.DefaultSaveDirectory); }
        catch (Exception ex) { MessageWindow.Show($"无法打开截图目录：\n{_settings.DefaultSaveDirectory}\n{ex.Message}", "目录错误"); }
    }
    private void OpenDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory();
    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void Save_Click(object sender, RoutedEventArgs e) => StartCapture(CaptureAction.Save);
    private void Copy_Click(object sender, RoutedEventArgs e) => StartCapture(CaptureAction.Copy);
    private void Ocr_Click(object sender, RoutedEventArgs e) => StartCapture(CaptureAction.Ocr);
    private void Pin_Click(object sender, RoutedEventArgs e) => StartCapture(CaptureAction.Pin);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);
}
