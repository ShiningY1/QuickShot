using System.Windows.Input;

namespace QuickShot.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000
}

public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, Key Key)
{
    public static bool TryParse(string text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 1)
            return false;

        HotkeyModifiers modifiers = HotkeyModifiers.NoRepeat;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToUpperInvariant())
            {
                case "ALT": modifiers |= HotkeyModifiers.Alt; break;
                case "CTRL":
                case "CONTROL": modifiers |= HotkeyModifiers.Control; break;
                case "SHIFT": modifiers |= HotkeyModifiers.Shift; break;
                case "WIN":
                case "WINDOWS": modifiers |= HotkeyModifiers.Win; break;
                default: return false;
            }
        }

        try
        {
            object? converted = new KeyConverter().ConvertFromString(parts[^1]);
            if (converted is not Key key || key == Key.None || KeyInterop.VirtualKeyFromKey(key) == 0 ||
                key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or
                    Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return false;
            gesture = new HotkeyGesture(modifiers, key);
            return true;
        }
        catch (Exception ex) when (ex is NotSupportedException or FormatException)
        {
            return false;
        }
    }
}
