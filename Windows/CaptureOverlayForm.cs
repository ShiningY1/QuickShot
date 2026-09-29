using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuickShot.Windows;

public sealed class CaptureOverlayForm : Form
{
    private readonly Bitmap _desktop;
    private Point _start;
    private Point _current;
    private bool _dragging;
    private Bitmap? _selection;

    public CaptureOverlayForm(Bitmap desktop, Point virtualOrigin)
    {
        _desktop = (Bitmap)desktop.Clone();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;
        Bounds = new Rectangle(virtualOrigin, desktop.Size);
        Cursor = Cursors.Cross;
    }

    public Bitmap? TakeSelection()
    {
        Bitmap? result = _selection;
        _selection = null;
        return result;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        _start = _current = e.Location;
        _dragging = true;
        Capture = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging)
            return;
        _current = Clamp(e.Location);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left)
            return;

        _dragging = false;
        Capture = false;
        _current = Clamp(e.Location);
        Rectangle area = GetSelectionRectangle();
        if (area.Width < 2 || area.Height < 2)
            return;

        _selection = _desktop.Clone(area, _desktop.PixelFormat);
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(_desktop, 0, 0);
        using (var shade = new SolidBrush(Color.FromArgb(115, 0, 0, 0)))
            e.Graphics.FillRectangle(shade, ClientRectangle);

        Rectangle area = GetSelectionRectangle();
        if (!_dragging || area.Width == 0 || area.Height == 0)
            return;

        e.Graphics.CompositingMode = CompositingMode.SourceCopy;
        e.Graphics.DrawImage(_desktop, area, area, GraphicsUnit.Pixel);
        e.Graphics.CompositingMode = CompositingMode.SourceOver;
        using var pen = new Pen(Color.FromArgb(91, 110, 245), 2);
        e.Graphics.DrawRectangle(pen, area.X, area.Y, area.Width - 1, area.Height - 1);

        string size = $"{area.Width} × {area.Height}";
        SizeF textSize = e.Graphics.MeasureString(size, Font);
        var label = new RectangleF(area.X, Math.Max(0, area.Y - textSize.Height - 8), textSize.Width + 12, textSize.Height + 6);
        using var labelBrush = new SolidBrush(Color.FromArgb(220, 35, 38, 48));
        e.Graphics.FillRectangle(labelBrush, label);
        e.Graphics.DrawString(size, Font, Brushes.White, label.X + 6, label.Y + 3);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _desktop.Dispose();
            _selection?.Dispose();
        }
        base.Dispose(disposing);
    }

    private Point Clamp(Point point) => new(
        Math.Clamp(point.X, 0, ClientSize.Width),
        Math.Clamp(point.Y, 0, ClientSize.Height));

    private Rectangle GetSelectionRectangle() => Rectangle.FromLTRB(
        Math.Min(_start.X, _current.X),
        Math.Min(_start.Y, _current.Y),
        Math.Max(_start.X, _current.X),
        Math.Max(_start.Y, _current.Y));
}
