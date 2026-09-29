using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;

namespace QuickShot.Services;

public sealed record OcrTextResult(string Text, string LanguageTag, string ImageVariant);

public sealed class OcrService : IAsyncDisposable
{
    private readonly SemaphoreSlim _recognitionLock = new(1, 1);
    private PaddleOcrAll? _engine;
    private volatile bool _disposed;

    public async Task<OcrTextResult> RecognizeAsync(Bitmap bitmap, int mode, bool rotate,
        IProgress<string> progress, CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _recognitionLock.WaitAsync(cancellation);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return await Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                progress.Report("读取原始截图…");
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png); // lossless; capture coordinates are physical pixels
                using Mat image = Cv2.ImDecode(stream.ToArray(), ImreadModes.Color);
                if (image.Empty()) throw new InvalidOperationException("截图图像为空。");
                var engine = GetOrCreateEngine(progress);
                cancellation.ThrowIfCancellationRequested();
                engine.AllowRotateDetection = rotate;
                using Mat input = Prepare(image, mode, progress);
                progress.Report($"正在检测与识别文字（{input.Width} × {input.Height}）…");
                var result = engine.Run(input);
                cancellation.ThrowIfCancellationRequested();
                // Preserve all Unicode, punctuation and whitespace produced by the model.
                string text = result.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("未检测到文字。请扩大框选范围；小字可在设置中选择放大，黑底文字可选择反色。");
                double confidence = result.Regions.Length == 0 ? 0 : result.Regions.Average(r => r.Score);
                string variant = mode switch { 1 => "小字放大", 2 => "反色", 3 => "反色与放大", _ => "原图" };
                return new OcrTextResult(text, "PaddleOCR 中文 V5", $"{variant} · 模型平均分 {confidence:P0}");
            }, cancellation);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "OCR 失败：" + ex.GetBaseException().Message +
                "\n如提示模型或 DLL 加载失败，请重新还原依赖并发布完整 win-x64 目录；不要只复制 EXE。截图、复制、保存和贴图仍可使用。", ex);
        }
        finally { _recognitionLock.Release(); }
    }

    private static Mat Prepare(Mat image, int mode, IProgress<string> progress)
    {
        Mat input = image.Clone();
        try
        {
            if (mode is 2 or 3) Cv2.BitwiseNot(input, input);
            if (mode is 1 or 3)
            {
                // Bound extra preprocessing memory; never reduce original screenshot resolution.
                if ((long)image.Width * image.Height <= 4_000_000)
                    Cv2.Resize(input, input, new OpenCvSharp.Size(image.Width * 2, image.Height * 2), 0, 0, InterpolationFlags.Cubic);
                else progress.Report("截图较大，保留原始分辨率，跳过额外放大…");
            }
            return input;
        }
        catch { input.Dispose(); throw; }
    }

    private PaddleOcrAll GetOrCreateEngine(IProgress<string> progress)
    {
        if (_engine is not null) return _engine;
        progress.Report("首次加载本地中文 V5 检测与识别模型…无需下载，请稍候。");
        PaddleOcrAll? engine = null;
        try
        {
            engine = new PaddleOcrAll(LocalFullModels.ChineseV5, PaddleDevice.Mkldnn())
            {
                AllowRotateDetection = false,
                Enable180Classification = false
            };
            engine.Detector.MaxSize = null;
            _engine = engine;
            return engine;
        }
        catch { engine?.Dispose(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await _recognitionLock.WaitAsync();
        try { _engine?.Dispose(); _engine = null; }
        finally { _recognitionLock.Release(); }
        // Keep the small managed semaphore alive for any waiter already admitted before shutdown.
    }
}
