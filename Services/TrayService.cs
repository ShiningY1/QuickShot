using System.Runtime.InteropServices;
using System.Windows.Interop;
using DrawingIcon = System.Drawing.Icon;

namespace QuickShot.Services;

// Native notification flags explicitly use our application icon, including the balloon icon.
public sealed class TrayService : IDisposable
{
    private const int CallbackMessage = 0x8001;
    private readonly HwndSource _source;
    private readonly DrawingIcon _icon;
    private readonly Action _show;
    private readonly Action _menu;
    private readonly uint _taskbarCreated;
    private bool _added;
    public bool IsAvailable => _added;

    public TrayService(nint handle, DrawingIcon icon, Action show, Action menu)
    {
        _source = HwndSource.FromHwnd(handle)!;
        _icon = icon; _show = show; _menu = menu;
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        _source.AddHook(WndProc);
        Add();
    }
    private NotifyIconData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = _source.Handle, Id = 1,
        Callback = CallbackMessage, Icon = _icon.Handle, Tip = "QuickShot",
        Info = string.Empty, InfoTitle = "QuickShot", BalloonIcon = _icon.Handle
    };
    private void Add()
    {
        var data = Data(); data.Flags = 1 | 2 | 4; // message, icon, tooltip
        _added = Shell_NotifyIcon(0, ref data);
    }
    public bool Notify(string message)
    {
        if (!_added) Add();
        var data = Data(); data.Flags = 0x10; // NIF_INFO
        data.Info = message.Length > 255 ? message[..252] + "…" : message;
        data.InfoFlags = 4 | 0x20; // NIIF_USER | NIIF_LARGE_ICON
        data.Timeout = 5000;
        return _added && Shell_NotifyIcon(1, ref data);
    }
    private nint WndProc(nint hwnd, int msg, nint wp, nint lp, ref bool handled)
    {
        if ((uint)msg == _taskbarCreated) Add();
        if (msg == CallbackMessage)
        {
            if (lp.ToInt32() is 0x203 or 0x405) _show(); // double-click or balloon click
            if (lp.ToInt32() == 0x205) _menu();
            handled = true;
        }
        return nint.Zero;
    }
    public void Dispose()
    {
        var data = Data();
        if (_added) Shell_NotifyIcon(2, ref data);
        _source.RemoveHook(WndProc);
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public nint Window;
        public uint Id, Flags, Callback;
        public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Timeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public nint BalloonIcon;
    }
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);
}
