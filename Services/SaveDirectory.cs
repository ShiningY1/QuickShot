using System.Diagnostics;
using System.IO;

namespace QuickShot.Services;

public static class SaveDirectory
{
    public static string Normalize(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("截图保存目录必须是完整路径，例如 D:\\secreenshot。");
        return Path.GetFullPath(path);
    }
    public static string Ensure(string path)
    {
        string fullPath = Normalize(path);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
    public static void Open(string path)
    {
        string fullPath = Ensure(path);
        Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
    }
}
