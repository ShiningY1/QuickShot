using System.Windows;
using QuickShot.Windows;

namespace QuickShot;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0 && e.Args[0] == "--verify-ocr")
        {
            if (e.Args.Length != 2) { Shutdown(2); return; }
            try { Shutdown(await Diagnostics.OcrVerification.RunAsync(e.Args[1])); }
            catch (Exception ex)
            {
                MessageWindow.Show(ex.ToString(), "OCR 验证无法完成");
                Shutdown(1);
            }
            return;
        }
        new MainWindow().Show();
    }
}
