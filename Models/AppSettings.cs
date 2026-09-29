namespace QuickShot.Models;

public sealed class AppSettings
{
    public string SaveHotkey { get; set; } = "Alt+A";
    public string CopyHotkey { get; set; } = "Alt+C";
    public string OcrHotkey { get; set; } = "Alt+T";
    public string PinHotkey { get; set; } = "Alt+P";
    public string DefaultSaveDirectory { get; set; } = @"D:\secreenshot";
    public string ClosePinHotkey { get; set; } = "Delete";
    public int OcrInputMode { get; set; }
    public bool OcrDetectRotation { get; set; }
    public bool ShowNotifications { get; set; } = true;
}
