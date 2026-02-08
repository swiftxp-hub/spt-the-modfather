using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SwiftXP.SPT.TheModfather.Updater.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Logging;
using SwiftXP.SPT.TheModfather.Updater.Services;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Updater.Tests.Services;

public class UpdateManagerTests : IDisposable
{
    private readonly Mock<ISimpleLogger> _loggerMock;
    private readonly Mock<IEFTProcessWatcher> _watcherMock;
    private readonly Mock<IProgress<int>> _progressMock;

    private readonly TestEnvironment _testEnvironment;

    public UpdateManagerTests()
    {
        _loggerMock = new Mock<ISimpleLogger>();
        _watcherMock = new Mock<IEFTProcessWatcher>();
        _progressMock = new Mock<IProgress<int>>();

        _testEnvironment = new TestEnvironment();
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

    {
        _testEnvironment.Dispose();
    }

    [Fact]
    public async Task ProcessUpdatesAsyncThrowsInvalidOperationWhenEftExeIsMissing()
    {
        _testEnvironment.EnsureEftExeMissing();
        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.ProcessUpdatesAsync(_progressMock.Object));

        Assert.Contains("could not be found", ex.Message);
    }

    [Fact]
    public async Task ProcessUpdatesAsyncThrowsInvalidOperationWhenStagingDirIsMissing()
    {
        _testEnvironment.CreateEftExe();
        _testEnvironment.EnsureStagingMissing();

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.ProcessUpdatesAsync(_progressMock.Object));

        Assert.Contains("Staging directory could not be found", ex.Message);
    }

    [Fact]
    public async Task ProcessUpdatesAsyncAbortsWhenProcessWatcherReturnsFalse()
    {
        _testEnvironment.SetupValidEnvironment();
        _watcherMock.Setup(x => x.WaitForProcessToCloseAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        await manager.ProcessUpdatesAsync(_progressMock.Object);

        _progressMock.Verify(x => x.Report(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessUpdatesAsyncMovesFilesHappyPath()
    {
        _testEnvironment.SetupValidEnvironment();
        _testEnvironment.CreateStagingFile("NewMod.dll", "New Content");

        _watcherMock.Setup(x => x.WaitForProcessToCloseAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        await manager.ProcessUpdatesAsync(_progressMock.Object);

        Assert.True(File.Exists(Path.Combine(_testEnvironment.BaseDir, "NewMod.dll")));
        Assert.Equal("New Content", await File.ReadAllTextAsync(Path.Combine(_testEnvironment.BaseDir, "NewMod.dll")));

        Assert.False(File.Exists(Path.Combine(_testEnvironment.StagingDir, "NewMod.dll")));

        _progressMock.Verify(x => x.Report(100), Times.Once);
    }

    [Fact]
    public async Task ProcessUpdatesAsyncHandlesDeleteInstructions()
    {
        _testEnvironment.SetupValidEnvironment();
        _testEnvironment.CreateBaseFile("OldMod.dll", "Old Content");

        string suffix = Constants.DeleteInstructionSuffix;
        string instructionName = $"OldMod.dll{suffix}";

        _testEnvironment.CreateStagingFile(instructionName, "dummy");

        _watcherMock.Setup(x => x.WaitForProcessToCloseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        await manager.ProcessUpdatesAsync(_progressMock.Object);

        string targetPath = Path.Combine(_testEnvironment.BaseDir, "OldMod.dll");

        Assert.False(File.Exists(targetPath), $"File {targetPath} should have been deleted.");
        Assert.False(File.Exists(Path.Combine(_testEnvironment.StagingDir, instructionName)));
    }

    [Fact]
    public async Task ProcessUpdatesAsyncCreateSubdirectoriesWhenMovingFiles()
    {
        _testEnvironment.SetupValidEnvironment();
        _testEnvironment.CreateStagingFile(Path.Combine("BepInEx", "plugins", "MyMod.dll"), "DLL Content");

        _watcherMock.Setup(x => x.WaitForProcessToCloseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        await manager.ProcessUpdatesAsync(_progressMock.Object);

        string expectedPath = Path.Combine(_testEnvironment.BaseDir, "BepInEx", "plugins", "MyMod.dll");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task ProcessUpdatesAsyncCleansUpPayloadDirectory()
    {
        _testEnvironment.SetupValidEnvironment();
        _testEnvironment.CreateStagingFile("test.txt");
        _watcherMock.Setup(x => x.WaitForProcessToCloseAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        UpdateManager manager = new(_loggerMock.Object, _watcherMock.Object);

        await manager.ProcessUpdatesAsync(_progressMock.Object);

        Assert.True(Directory.Exists(_testEnvironment.StagingDir));
        Assert.Empty(Directory.GetFiles(_testEnvironment.StagingDir));
    }

    private sealed class TestEnvironment : IDisposable
    {
        public string BaseDir { get; }
        public string StagingDir { get; }

        private readonly string _exePath;

        public TestEnvironment()
        {
            BaseDir = Path.GetFullPath(AppContext.BaseDirectory);
            StagingDir = Path.Combine(BaseDir, Constants.ModfatherDataDirectory, Constants.StagingDirectory);
            _exePath = Path.Combine(BaseDir, Constants.EscapeFromTarkovExe);
        }

        public void SetupValidEnvironment()
        {
            CreateEftExe();
            CreateStagingDir();
        }

        public void CreateEftExe()
        {
            if (!File.Exists(_exePath))
                File.WriteAllText(_exePath, "Mock EXE");
        }

        public void EnsureEftExeMissing()
        {
            if (File.Exists(_exePath))
                File.Delete(_exePath);
        }

        public void CreateStagingDir()
        {
            if (!Directory.Exists(StagingDir))
                Directory.CreateDirectory(StagingDir);
        }

        public void EnsureStagingMissing()
        {
            if (Directory.Exists(StagingDir))
                Directory.Delete(StagingDir, true);
        }

        public void CreateStagingFile(string relativePath, string content = "test")
        {
            string fullPath = Path.Combine(StagingDir, relativePath);

            EnsureDirectoryExists(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        public void CreateBaseFile(string relativePath, string content = "test")
        {
            string fullPath = Path.Combine(BaseDir, relativePath);

            EnsureDirectoryExists(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        private static void EnsureDirectoryExists(string? path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void Dispose()
        {
            if (File.Exists(_exePath))
                File.Delete(_exePath);

            string modDataDir = Path.Combine(BaseDir, Constants.ModfatherDataDirectory);
            if (Directory.Exists(modDataDir))
                try
                {
                    Directory.Delete(modDataDir, true);
                }
                catch { }

            if (File.Exists(Path.Combine(BaseDir, "NewMod.dll")))
                File.Delete(Path.Combine(BaseDir, "NewMod.dll"));

            if (File.Exists(Path.Combine(BaseDir, "OldMod.dll")))
                File.Delete(Path.Combine(BaseDir, "OldMod.dll"));

            string bepDir = Path.Combine(BaseDir, "BepInEx");
            if (Directory.Exists(bepDir))
                Directory.Delete(bepDir, true);
        }
    }
}