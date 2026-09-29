using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace QuickShot.Services;

public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();

    public GlobalHotkeyManager(nint windowHandle)
    {
        _source = HwndSource.FromHwnd(windowHandle)
                  ?? throw new InvalidOperationException("无法取得主窗口句柄。");
        _source.AddHook(WndProc);
    }

    public void Register(int id, string shortcut, Action action)
    {
        if (!HotkeyGesture.TryParse(shortcut, out HotkeyGesture gesture) ||
            (gesture.Modifiers & ~HotkeyModifiers.NoRepeat) == 0)
            throw new ArgumentException($"快捷键格式不正确：{shortcut}");

        uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);
        if (!RegisterHotKey(_source.Handle, id, (uint)gesture.Modifiers, virtualKey))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"快捷键 {shortcut} 已被其他程序占用。");

        _actions[id] = action;
    }

    public void UnregisterAll()
    {
        foreach (int id in _actions.Keys)
            UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && _actions.TryGetValue(wParam.ToInt32(), out Action? action))
        {
            handled = true;
            action();
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);
}
