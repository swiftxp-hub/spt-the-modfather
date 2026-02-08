using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Updater.Logging;

public class SimpleLogger(string baseDirectory) : ISimpleLogger, IDisposable
{
    private readonly string _logPath = Path.Combine(baseDirectory, $"SwiftXP.SPT.TheModfather.Updater.{DateTime.Now:yyyy-MM-dd}.log");

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task StartNewFile(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(_logPath))
                File.Delete(_logPath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            string logLine = $"SwiftXP.SPT.TheModfather.Updater | {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | {message}{System.Environment.NewLine}";
            await File.AppendAllTextAsync(_logPath, logLine, cancellationToken);
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Error writing to log: {exception.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task WriteErrorAsync(string message, CancellationToken cancellationToken = default) => await WriteMessageAsync($"ERROR: {message}", cancellationToken);

    public async Task WriteErrorAsync(string message, Exception exception, CancellationToken cancellationToken = default) => await WriteMessageAsync($"ERROR: {message} -> {exception.Message}\nStacktrace: {exception.StackTrace}", cancellationToken);

    public void Dispose()
    {
        _lock.Dispose();

        GC.SuppressFinalize(this);
    }
}