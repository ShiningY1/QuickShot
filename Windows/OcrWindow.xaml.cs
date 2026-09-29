using System.Windows;
using QuickShot.Services;

namespace QuickShot.Windows;

public partial class OcrWindow : Window
{
    public CancellationTokenSource Cancellation { get; } = new();
    public OcrWindow()
    {
        InitializeComponent();
        WindowLayout.Apply(this, 46);
        // A bounded editor keeps long OCR results scrollable instead of growing the window.
        Loaded += (_, _) => { SizeToContent = SizeToContent.Manual; Height = Math.Min(MaxHeight, FontSize * 34); };
        SizeChanged += (_, _) => StatusScroll.MaxHeight = Math.Max(24, ActualHeight * 0.35);
        Closed += (_, _) => Cancellation.Cancel();
    }
    public void Report(string text) { if (IsVisible) Status.Text = text; }
    public void Complete(OcrTextResult result)
    {
        if (!IsVisible) return;
        ResultText.Text = result.Text;
        Status.Text = $"{result.LanguageTag} · {result.ImageVariant}";
        Finish();
        CopyButton.IsEnabled = true;
        try { System.Windows.Clipboard.SetText(result.Text); Status.Text += " · 已复制"; }
        catch (Exception) { Status.Text += " · 剪贴板忙，请点击复制文字重试"; }
    }
    public void Fail(string message) { if (IsVisible) { Status.Text = message; Finish(); } }
    private void Finish() { Progress.Visibility = Visibility.Collapsed; CancelButton.Content = "关闭"; }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancellation.Cancel();
        Close();
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { System.Windows.Clipboard.SetText(ResultText.Text); }
        catch (Exception ex) { MessageWindow.Show(ex.Message, "复制失败", this); }
    }
}
