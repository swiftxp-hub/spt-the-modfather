using System;
using System.IO;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public static class SimpleLogService
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "SwiftXP.SPT.TheModfather.Updater.log");
    
    private static readonly object _lock = new();

    public static void StartNewFile()
    {
        if(File.Exists(LogPath))
            File.Delete(LogPath);
    }

    public static void Write(string message)
    {
        lock (_lock) 
        {
            try
            {
                string logLine = $"SwiftXP.SPT.TheModfather.Updater | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logLine);
            }
            catch {}
        }
    }

    public static void Error(string message)
    {
        Write($"ERROR: {message}");
    }

    public static void Error(string message, Exception ex)
    {
        Write($"ERROR: {message} -> {ex.Message}\nStacktrace: {ex.StackTrace}");
    }
}