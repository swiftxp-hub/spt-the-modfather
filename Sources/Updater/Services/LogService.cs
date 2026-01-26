using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class LogService : ILogService
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, $"SwiftXP.SPT.TheModfather.Updater.{DateTime.Now:yyyy-MM-dd}.log");

    private static readonly object _lock = new();

    public void StartNewFile()
    {
        if (File.Exists(LogPath))
            File.Delete(LogPath);
    }

    public void Write(string message)
    {
        lock (_lock)
        {
            try
            {
                string logLine = $"SwiftXP.SPT.TheModfather.Updater | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logLine);
            }
            catch { }
        }
    }

    public void Error(string message) => Write($"ERROR: {message}");

    public void Error(string message, Exception ex) => Write($"ERROR: {message} -> {ex.Message}\nStacktrace: {ex.StackTrace}");
}
