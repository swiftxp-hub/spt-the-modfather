using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class LogService : ILogService
{
    private static readonly string s_logPath = Path.Combine(AppContext.BaseDirectory, $"SwiftXP.SPT.TheModfather.Updater.{DateTime.Now:yyyy-MM-dd}.log");

    private static readonly Lock s_logServiceLock = new();

    public void StartNewFile()
    {
        if (File.Exists(s_logPath))
            File.Delete(s_logPath);
    }

    public void WriteMessage(string message)
    {
        lock (s_logServiceLock)
        {
            try
            {
                string logLine = $"SwiftXP.SPT.TheModfather.Updater | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}";
                File.AppendAllText(s_logPath, logLine);
            }
            catch { }
        }
    }

    public void WriteError(string message) => WriteMessage($"ERROR: {message}");

    public void WriteError(string message, Exception ex) => WriteMessage($"ERROR: {message} -> {ex.Message}\nStacktrace: {ex.StackTrace}");
}
