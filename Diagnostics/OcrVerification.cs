using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Text.Json;
using QuickShot.Services;

namespace QuickShot.Diagnostics;

// Opt-in Windows verification: QuickShot.exe --verify-ocr <output-directory>.
// Produces both the exact images supplied to OCR and machine-readable results.
internal static class OcrVerification
{
    public static async Task<int> RunAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        var samples = new[]
        {
            new Sample("chinese", "中文截图识别：保存、复制与贴图。", "Microsoft YaHei", 26, false, 0),
            new Sample("english", "QuickShot screenshot OCR: Hello, world!", "Segoe UI", 24, false, 0),
            new Sample("mixed", "截图 OCR 测试：温度 36.5°C，编号 A-1024。", "Microsoft YaHei", 24, false, 0),
            new Sample("numbers", "0123456789  3.14159  -12.50%  2026/09/29", "Segoe UI", 24, false, 0),
            new Sample("symbols", "!? @ # $ % & + = / \\ ( ) [ ] { } < > ± × ÷ ℃", "Microsoft YaHei", 24, false, 0),
            new Sample("japanese", "日本語の文字認識：こんにちは、世界！", "Yu Gothic", 26, false, 0),
            new Sample("small", "小字体 OCR 123.45，Hello!", "Microsoft YaHei", 13, false, 1),
            new Sample("dark", "黑底白字：QuickShot 123.45!", "Microsoft YaHei", 18, true, 3),
            new Sample("code", "if (value != nullptr) return count + 123;", "Consolas", 18, false, 0)
        };
        var results = new List<object>();
        await using var ocr = new OcrService();
        foreach (var sample in samples)
        {
            using var font = new Font(sample.Font, sample.Size, GraphicsUnit.Pixel);
            using var measure = new Bitmap(1, 1);
            using var measureGraphics = Graphics.FromImage(measure);
            SizeF size = measureGraphics.MeasureString(sample.Text, font);
            using var bitmap = new Bitmap((int)Math.Ceiling(size.Width) + 40, (int)Math.Ceiling(size.Height) + 40);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(sample.Dark ? Color.FromArgb(30, 30, 30) : Color.White);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.DrawString(sample.Text, font, sample.Dark ? Brushes.White : Brushes.Black, 20, 20);
            }
            bitmap.Save(Path.Combine(directory, sample.Name + ".png"), ImageFormat.Png);
            try
            {
                var result = await ocr.RecognizeAsync(bitmap, sample.Mode, false, new Progress<string>(), CancellationToken.None);
                int distance = Distance(sample.Text, result.Text);
                results.Add(new { sample.Name, Expected = sample.Text, Actual = result.Text,
                    RequestedFont = sample.Font, ActualFont = font.Name, Status = "recognized",
                    ExactMatch = sample.Text == result.Text, CharacterEdits = distance,
                    CharacterErrorRate = (double)distance / sample.Text.EnumerateRunes().Count(), result.ImageVariant });
            }
            catch (Exception ex)
            {
                results.Add(new { sample.Name, Expected = sample.Text, Status = "error", Error = ex.ToString() });
            }
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "ocr-results.json"),
            JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        return 0; // Report generation is not an assertion that recognition is perfect.
    }
    private static int Distance(string expected, string actual)
    {
        var a = expected.EnumerateRunes().ToArray();
        var b = actual.EnumerateRunes().ToArray();
        int[] previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (int i = 1; i <= a.Length; i++)
        {
            int[] current = new int[b.Length + 1]; current[0] = i;
            for (int j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[b.Length];
    }
    private sealed record Sample(string Name, string Text, string Font, int Size, bool Dark, int Mode);
}
