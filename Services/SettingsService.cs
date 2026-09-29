using System.IO;
using System.Text.Json;
using QuickShot.Models;

namespace QuickShot.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuickShot", "settings.json");
    public string? LoadWarning { get; private set; }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath)) return new AppSettings();
        try
        {
            var result = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath))
                ?? throw new JsonException("配置内容为空。");
            var defaults = new AppSettings();
            if (!HotkeyGesture.TryParse(result.ClosePinHotkey, out _)) result.ClosePinHotkey = defaults.ClosePinHotkey;
            if (string.IsNullOrWhiteSpace(result.DefaultSaveDirectory)) result.DefaultSaveDirectory = defaults.DefaultSaveDirectory;
            result.DefaultSaveDirectory = SaveDirectory.Normalize(result.DefaultSaveDirectory);
            result.OcrInputMode = Math.Clamp(result.OcrInputMode, 0, 3);
            return result;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            LoadWarning = $"设置读取失败，已使用默认设置。原文件未覆盖：{_settingsPath}\n{ex.Message}";
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        string temporaryPath = _settingsPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _settingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
