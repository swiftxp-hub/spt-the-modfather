using System;
using System.IO;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Updater.Logging;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Updater.Tests.Logging;

public class SimpleLoggerTests
{
    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirInfo { get; }

        public TempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DirInfo = Directory.CreateDirectory(path);
        }

        public void Dispose()
        {
            if (DirInfo.Exists)
            {
                try { DirInfo.Delete(true); } catch { }
            }
        }
    }

    private static string GetExpectedLogPath(string baseDir)
    {
        return Path.Combine(baseDir, $"SwiftXP.SPT.TheModfather.Updater.{DateTime.UtcNow:yyyy-MM-dd}.log");
    }

    [Fact]
    public async Task WriteMessageAsyncCreatesFileAndAppendsText()
    {
        using TempDirectory temp = new();
        using SimpleLogger logger = new(temp.DirInfo.FullName);

        string message = "Test Message Content";
        string logPath = GetExpectedLogPath(temp.DirInfo.FullName);

        await logger.WriteMessageAsync(message);

        Assert.True(File.Exists(logPath), "Log file should be created");

        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains(message, content);
        Assert.Contains("SwiftXP.SPT.TheModfather.Updater |", content);
    }

    [Fact]
    public async Task WriteErrorAsyncPrefixesErrorMessage()
    {
        using TempDirectory temp = new();
        using SimpleLogger logger = new(temp.DirInfo.FullName);
        string logPath = GetExpectedLogPath(temp.DirInfo.FullName);

        await logger.WriteErrorAsync("Something went wrong");

        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("ERROR: Something went wrong", content);
    }

    [Fact]
    public async Task WriteErrorAsyncWithExceptionFormatsStacktrace()
    {
        using TempDirectory temp = new();
        using SimpleLogger logger = new(temp.DirInfo.FullName);
        string logPath = GetExpectedLogPath(temp.DirInfo.FullName);

        InvalidOperationException exception = new("Crash!");

        await logger.WriteErrorAsync("Context info", exception);

        string content = await File.ReadAllTextAsync(logPath);
        Assert.Contains("ERROR: Context info -> Crash!", content);
    }

    [Fact]
    public async Task StartNewFileDeletesOldFile()
    {
        using TempDirectory temp = new();
        using SimpleLogger logger = new(temp.DirInfo.FullName);
        string logPath = GetExpectedLogPath(temp.DirInfo.FullName);

        await File.WriteAllTextAsync(logPath, "OLD CONTENT");

        await logger.StartNewFile();

        Assert.False(File.Exists(logPath));

        await logger.WriteMessageAsync("New Content");
        string content = await File.ReadAllTextAsync(logPath);

        Assert.DoesNotContain("OLD CONTENT", content);
        Assert.Contains("New Content", content);
    }

    [Fact]
    public async Task ConcurrencyMultipleWritesDoNotCrash()
    {
        using TempDirectory temp = new();
        using SimpleLogger logger = new(temp.DirInfo.FullName);
        string logPath = GetExpectedLogPath(temp.DirInfo.FullName);

        int numberOfTasks = 50;
        Task[] tasks = new Task[numberOfTasks];

        for (int i = 0; i < numberOfTasks; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() => logger.WriteMessageAsync($"Line {index}"));
        }

        await Task.WhenAll(tasks);

        string[] lines = await File.ReadAllLinesAsync(logPath);
        Assert.True(lines.Length >= numberOfTasks);
    }

    [Fact]
    public async Task DisposeCanBeCalledMultipleTimes()
    {
        using TempDirectory temp = new();
        SimpleLogger logger = new(temp.DirInfo.FullName);

        logger.Dispose();
        logger.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => logger.WriteMessageAsync("Test"));
    }
}