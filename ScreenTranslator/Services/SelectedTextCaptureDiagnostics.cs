using System.IO;

namespace ScreenTranslator.Services;

internal static class SelectedTextCaptureDiagnostics
{
  private const string AppFolderName = "transtools";
  private const string LogFileName = "selected-text-capture.log";
  private const long MaxLogBytes = 256 * 1024;
  private static readonly object Sync = new();

  public static void Log(string message)
  {
    try
    {
      var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      var logDir = Path.Combine(appDataPath, AppFolderName, "logs");
      Directory.CreateDirectory(logDir);

      var logPath = Path.Combine(logDir, LogFileName);
      lock (Sync)
      {
        if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxLogBytes)
          File.WriteAllText(logPath, string.Empty);

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
        File.AppendAllText(logPath, line);
      }
    }
    catch
    {
      // Diagnostics logging is best effort only.
    }
  }
}
