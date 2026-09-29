using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace QuickShot.Services;

public static class WindowLayout
{
    public static Rect WorkArea(Window window)
    {
        var screen = WinForms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
        var dpi = VisualTreeHelper.GetDpi(window);
        var area = screen.WorkingArea;
        return new Rect(area.X / dpi.DpiScaleX, area.Y / dpi.DpiScaleY,
            area.Width / dpi.DpiScaleX, area.Height / dpi.DpiScaleY);
    }

    public static void Apply(Window window, double widthInEm)
    {
        window.UseLayoutRounding = true;
        window.SnapsToDevicePixels = true;
        window.SourceInitialized += (_, _) => Update(true);
        window.DpiChanged += (_, _) => window.Dispatcher.BeginInvoke(new Action(() => Update(false)));
        window.LocationChanged += (_, _) => Update(false);
        void Update(bool initial)
        {
            Rect area = WorkArea(window);
            window.MaxWidth = area.Width;
            window.MaxHeight = area.Height;
            window.MinWidth = Math.Min(22 * window.FontSize, area.Width);
            window.MinHeight = Math.Min(8 * window.FontSize, area.Height);
            if (initial) window.Width = Math.Min(widthInEm * window.FontSize, area.Width);
        }
    }
}
