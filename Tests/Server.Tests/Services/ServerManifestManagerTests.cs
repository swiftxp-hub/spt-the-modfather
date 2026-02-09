using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.TheModfather.Server.Repositories;
using SwiftXP.SPT.TheModfather.Server.Services;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Server.Tests.Services;

public class ServerManifestManagerTests : IDisposable
{
    private readonly Mock<ISptLogger<ServerManifestManager>> _loggerMock;
    private readonly Mock<IBaseDirectoryLocator> _baseDirectoryLocatorMock;
    private readonly Mock<IServerConfigurationRepository> _configRepositoryMock;
    private readonly Mock<IXxHash128FileHasher> _hasherMock;
    private readonly TempDirectory _tempDirectory;

    public ServerManifestManagerTests()
    {
        _loggerMock = new Mock<ISptLogger<ServerManifestManager>>();
        _baseDirectoryLocatorMock = new Mock<IBaseDirectoryLocator>();
        _configRepositoryMock = new Mock<IServerConfigurationRepository>();
        _hasherMock = new Mock<IXxHash128FileHasher>();
        _tempDirectory = new TempDirectory();

        _baseDirectoryLocatorMock.Setup(x => x.GetBaseDirectory())
            .Returns(_tempDirectory.DirectoryPath);

        _configRepositoryMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerConfiguration());
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _tempDirectory.Dispose();
    }

    [Fact]
    public async Task GetServerManifestAsyncBuildsManifestWhenCacheIsNull()
    {
        string filePath = _tempDirectory.CreateFile("test.txt", "content");

        Dictionary<string, string> hashes = new()
        {
            { filePath, "hash123" }
        };

        _hasherMock.Setup(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashes);

        ServerConfiguration permissiveConfig = new()
        {
            IncludePatterns = ["**/*"],
            ExcludePatterns = []
        };

        _configRepositoryMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissiveConfig);

        ServerManifestManager manager = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _configRepositoryMock.Object,
            _hasherMock.Object);

        ServerManifest manifest = await manager.GetServerManifestAsync(CancellationToken.None);

        Assert.NotNull(manifest);
        Assert.True(manifest.ContainsFile("test.txt"), "Manifest does not contain 'test.txt'. Likely filtered out by IncludePatterns.");

        bool found = manifest.TryGetFile("test.txt", out ServerFileManifest? fileManifest);
        Assert.True(found);
        Assert.Equal("hash123", fileManifest!.Hash);

        manager.Dispose();
    }

    [Fact]
    public async Task GetServerManifestAsyncReturnsCachedManifestOnSecondCall()
    {
        _hasherMock.Setup(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        ServerManifestManager manager = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _configRepositoryMock.Object,
            _hasherMock.Object);

        ServerManifest manifest1 = await manager.GetServerManifestAsync(CancellationToken.None);
        ServerManifest manifest2 = await manager.GetServerManifestAsync(CancellationToken.None);

        Assert.Same(manifest1, manifest2);

        _hasherMock.Verify(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()), Times.Once);

        manager.Dispose();
    }

    [Fact]
    public async Task WatchForChangesUpdatesManifestWhenFileIsCreated()
    {
        ServerConfiguration testConfig = new()
        {
            IncludePatterns = ["**/*"],
            ExcludePatterns = []
        };

        _configRepositoryMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testConfig);

        _hasherMock.Setup(x => x.GetFileHashAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("newhash_123");

        _hasherMock.Setup(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ServerManifestManager manager = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _configRepositoryMock.Object,
            _hasherMock.Object);

        await manager.GetServerManifestAsync(CancellationToken.None);

        manager.WatchForChanges();

        await Task.Delay(100);
        _tempDirectory.CreateFile("newfile.txt", "content");

        await WaitForConditionAsync(async () =>
            {
                ServerManifest manifest = await manager.GetServerManifestAsync(CancellationToken.None);
                return manifest != null && manifest.ContainsFile("newfile.txt");
            },
            timeoutMs: 5000, failureMessage: "File 'newfile.txt' was ignored or not detected.");

        manager.Dispose();
    }

    [Fact]
    public async Task WatchForChangesUpdatesManifestWhenFileIsDeleted()
    {
        string filePath = _tempDirectory.CreateFile("todelete.txt", "content");
        Dictionary<string, string> hashes = new()
        {
            { filePath, "hash1" }
        };

        _hasherMock.Setup(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashes);

        ServerManifestManager manager = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _configRepositoryMock.Object,
            _hasherMock.Object);

        await manager.GetServerManifestAsync(CancellationToken.None);

        manager.WatchForChanges();

        File.Delete(filePath);

        await Task.Delay(1000);

        ServerManifest manifest = await manager.GetServerManifestAsync(CancellationToken.None);
        Assert.False(manifest.ContainsFile("todelete.txt"));

        manager.Dispose();
    }

    [Fact]
    public void HandleConfigurationChangedTriggersManifestRebuild()
    {
        _hasherMock.Setup(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ServerManifestManager manager = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _configRepositoryMock.Object,
            _hasherMock.Object);

        ServerConfiguration newConfig = new();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _configRepositoryMock.Raise(x => x.OnConfigurationChanged += null, null, newConfig);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        Thread.Sleep(500);

        _hasherMock.Verify(x => x.GetFileHashesAsync(It.IsAny<IEnumerable<FileInfo>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        manager.Dispose();
    }

    private static async Task WaitForConditionAsync(Func<Task<bool>> condition, int timeoutMs, string failureMessage)
    {
        Stopwatch sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (await condition())
            {
                return;
            }
            await Task.Delay(100);
        }

        throw new Xunit.Sdk.XunitException(failureMessage);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string DirectoryPath { get; }

        public TempDirectory()
        {
            string tempPath = Path.GetTempPath();
            string dirName = Guid.NewGuid().ToString();

            DirectoryPath = Path.Combine(tempPath, dirName);
            Directory.CreateDirectory(DirectoryPath);
        }

        public string CreateFile(string relativePath, string content)
        {
            string fullPath = Path.Combine(DirectoryPath, relativePath);

            string? directory = Path.GetDirectoryName(fullPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);

            return fullPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                try
                {
                    Directory.Delete(DirectoryPath, true);
                }
                catch { }
            }
        }
    }
}