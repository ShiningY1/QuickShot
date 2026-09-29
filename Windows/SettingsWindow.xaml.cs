using System.IO;
using System.Windows;
using QuickShot.Models;
using QuickShot.Services;
using WinForms = System.Windows.Forms;

namespace QuickShot.Windows;

public partial class SettingsWindow : Window
{
    private readonly Func<AppSettings, string?> _apply;
    public SettingsWindow(AppSettings settings, Func<AppSettings, string?> apply)
    {
        InitializeComponent();
        WindowLayout.Apply(this, 38);
        _apply = apply;
        SaveHotkeyBox.Text = settings.SaveHotkey;
        CopyHotkeyBox.Text = settings.CopyHotkey;
        OcrHotkeyBox.Text = settings.OcrHotkey;
        PinHotkeyBox.Text = settings.PinHotkey;
        ClosePinHotkeyBox.Text = settings.ClosePinHotkey;
        SaveDirectoryBox.Text = settings.DefaultSaveDirectory;
        NotificationBox.IsChecked = settings.ShowNotifications;
        OcrModeBox.SelectedIndex = settings.OcrInputMode;
        RotateBox.IsChecked = settings.OcrDetectRotation;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string[] shortcuts = [ SaveHotkeyBox.Text.Trim(), CopyHotkeyBox.Text.Trim(),
            OcrHotkeyBox.Text.Trim(), PinHotkeyBox.Text.Trim(), ClosePinHotkeyBox.Text.Trim() ];
        var gestures = new List<HotkeyGesture>();
        for (int i = 0; i < shortcuts.Length; i++)
        {
            if (!HotkeyGesture.TryParse(shortcuts[i], out var gesture) ||
                (i < 4 && (gesture.Modifiers & ~HotkeyModifiers.NoRepeat) == 0))
            {
                MessageWindow.Show("截图快捷键需要修饰键，例如 Alt+A；关闭贴图可以使用 Delete。请勿使用单独的修饰键。", "快捷键格式不正确", this);
                return;
            }
            gestures.Add(gesture);
        }
        if (gestures.Distinct().Count() != gestures.Count)
        {
            MessageWindow.Show("快捷键不能重复（Ctrl 与 Control 等同）。", "快捷键冲突", this);
            return;
        }
        try
        {
            string directory = SaveDirectory.Ensure(SaveDirectoryBox.Text);
            var result = new AppSettings
            {
                SaveHotkey = shortcuts[0], CopyHotkey = shortcuts[1], OcrHotkey = shortcuts[2],
                PinHotkey = shortcuts[3], ClosePinHotkey = shortcuts[4],
                DefaultSaveDirectory = directory, ShowNotifications = NotificationBox.IsChecked == true,
                OcrInputMode = Math.Max(0, OcrModeBox.SelectedIndex), OcrDetectRotation = RotateBox.IsChecked == true
            };
            string? error = _apply(result);
            if (error is not null) { MessageWindow.Show(error, "设置未保存", this); return; }
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageWindow.Show($"无法保存设置或访问目录：\n{ex.Message}", "设置未保存", this);
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "选择截图保存目录", UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(SaveDirectoryBox.Text) ? SaveDirectoryBox.Text : string.Empty
        };
        if (dialog.ShowDialog() == WinForms.DialogResult.OK) SaveDirectoryBox.Text = dialog.SelectedPath;
    }
    private void OpenDirectory_Click(object sender, RoutedEventArgs e)
    {
        try { SaveDirectory.Open(SaveDirectoryBox.Text); }
        catch (Exception ex) { MessageWindow.Show(ex.Message, "无法打开目录", this); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
