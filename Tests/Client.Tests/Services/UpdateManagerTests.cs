using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Client.Services;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Services;

public class UpdateManagerTests : IDisposable
{
    private readonly Mock<ISimpleSptLogger> _loggerMock;
    private readonly Mock<IXxHash128FileHasher> _hasherMock;
    private readonly Mock<ISPTRequestHandler> _requestHandlerMock;
    private readonly TempDirectory _tempDir;
    private readonly UpdateManager _manager;

    public UpdateManagerTests()
    {
        _loggerMock = new Mock<ISimpleSptLogger>();
        _hasherMock = new Mock<IXxHash128FileHasher>();
        _requestHandlerMock = new Mock<ISPTRequestHandler>();
        _tempDir = new TempDirectory();

        _requestHandlerMock.Setup(x => x.Host).Returns("http://localhost");

        _manager = new UpdateManager(
            _loggerMock.Object,
            _hasherMock.Object,
            _requestHandlerMock.Object);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _tempDir.Dispose();
    }

    [Fact]
    public async Task GetSyncActionsAsyncReturnsAddWhenFileIsMissingLocally()
    {
        List<ServerFileManifest> files =
        [
            new ServerFileManifest("new.dll", "hash1", 100, DateTime.UtcNow)
        ];

        ServerManifest serverManifest = new(files, ["**/*"], []);

        ClientState state = CreateState(serverManifest, new ClientManifest(DateTimeOffset.UtcNow, "http://localhost"));

        SyncProposal result = await _manager.GetSyncActionsAsync(state, null);

        Assert.Contains(result.SyncActions, x => x.Type == SyncActionType.Add && x.RelativeFilePath == "new.dll");
    }

    [Fact]
    public async Task GetSyncActionsAsyncReturnsUpdateWhenHashMismatches()
    {
        string fileName = "update.dll";
        string localPath = _tempDir.CreateFile(fileName, "local content");

        List<ServerFileManifest> files =
        [
            new ServerFileManifest(fileName, "server_hash", 100, DateTime.UtcNow)
        ];
        ServerManifest serverManifest = new(files, ["**/*"], []);

        _hasherMock.Setup(x => x.GetFileHashAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("different_hash");

        ClientState state = CreateState(serverManifest, new ClientManifest(DateTimeOffset.UtcNow, "http://localhost"));

        SyncProposal result = await _manager.GetSyncActionsAsync(state, null);

        Assert.Contains(result.SyncActions, x => x.Type == SyncActionType.Update && x.RelativeFilePath == fileName);
    }

    [Fact]
    public async Task GetSyncActionsAsyncReturnsAdoptWhenFileExistsButNotTracked()
    {
        string fileName = "existing.dll";
        _tempDir.CreateFile(fileName, "content");

        List<ServerFileManifest> files =
        [
            new ServerFileManifest(fileName, "match_hash", 7, DateTime.UtcNow)
        ];

        ServerManifest serverManifest = new(files, ["**/*"], []);

        _hasherMock.Setup(x => x.GetFileHashAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("match_hash");

        ClientManifest emptyClientManifest = new ClientManifest(DateTimeOffset.UtcNow, "http://localhost");
        ClientState state = CreateState(serverManifest, emptyClientManifest);

        SyncProposal result = await _manager.GetSyncActionsAsync(state, null);

        Assert.Contains(result.SyncActions, x => x.Type == SyncActionType.Adopt && x.RelativeFilePath == fileName);
    }

    [Fact]
    public async Task GetSyncActionsAsyncReturnsDeleteWhenFileInClientManifestButNotOnServer()
    {
        string fileName = "legacy.dll";
        _tempDir.CreateFile(fileName, "content");

        ClientManifest clientManifest = new(DateTimeOffset.UtcNow, "http://localhost");
        clientManifest.AddOrUpdateFile(new ClientFileManifest(fileName, string.Empty, 0, DateTimeOffset.UtcNow));

        ServerManifest emptyServerManifest = new ServerManifest([], [], []);
        ClientState state = CreateState(emptyServerManifest, clientManifest);

        SyncProposal result = await _manager.GetSyncActionsAsync(state, null);

        Assert.Contains(result.SyncActions, x => x.Type == SyncActionType.Delete && x.RelativeFilePath == fileName);
    }

    [Fact]
    public async Task GetSyncActionsAsyncReturnsBlacklistWhenFileHashIsBlacklisted()
    {
        string dllPath = _tempDir.CreateFile(Path.Combine(Constants.BepInExDirectory, "cheater.dll"), "bad content");

        FileHashBlacklist blacklist = ["bad_hash"];

        _hasherMock.Setup(x => x.GetFileHashAsync(It.IsAny<FileInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("bad_hash");

        ServerManifest emptyServerManifest = new ServerManifest([], [], []);
        ClientState state = CreateState(emptyServerManifest, new ClientManifest(DateTimeOffset.UtcNow, "http://localhost"));
        state = state with { FileHashBlacklist = blacklist };

        SyncProposal result = await _manager.GetSyncActionsAsync(state, null);

        Assert.Contains(result.SyncActions, x => x.Type == SyncActionType.Blacklist);
    }

    private ClientState CreateState(ServerManifest serverManifest, ClientManifest clientManifest)
    {
        return new ClientState(
            _tempDir.Path,
            new ClientConfiguration(),
            clientManifest,
            serverManifest,
            []);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
            Directory.CreateDirectory(System.IO.Path.Combine(Path, Constants.BepInExDirectory));
        }

        public string CreateFile(string relativePath, string content)
        {
            string fullPath = System.IO.Path.Combine(Path, relativePath);
            string? dir = System.IO.Path.GetDirectoryName(fullPath);

            if (dir != null)
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content);

            return fullPath;
        }
        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}