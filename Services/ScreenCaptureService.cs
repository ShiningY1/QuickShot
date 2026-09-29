using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using QuickShot.Windows;

namespace QuickShot.Services;

public sealed class ScreenCaptureService
{
    public Bitmap? CaptureSelection()
    {
        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        using var desktop = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(desktop))
        {
            graphics.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
        }

        using var overlay = new CaptureOverlayForm(desktop, virtualScreen.Location);
        return overlay.ShowDialog() == DialogResult.OK ? overlay.TakeSelection() : null;
    }
}
