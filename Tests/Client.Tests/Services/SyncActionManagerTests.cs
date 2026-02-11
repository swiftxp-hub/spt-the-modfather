using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using SwiftXP.SPT.TheModfather.Client.Services;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Services;

public class SyncActionManagerTests : IDisposable
{
    private readonly Mock<ISimpleSptLogger> _loggerMock;
    private readonly Mock<IClientManifestRepository> _manifestRepoMock;
    private readonly Mock<ISPTRequestHandler> _requestHandlerMock;
    private readonly Mock<ISPTHttpClient> _httpClientMock;
    private readonly TempDirectory _tempDirectory;
    private readonly SyncActionManager _manager;

    public SyncActionManagerTests()
    {
        _loggerMock = new Mock<ISimpleSptLogger>();
        _manifestRepoMock = new Mock<IClientManifestRepository>();
        _requestHandlerMock = new Mock<ISPTRequestHandler>();
        _httpClientMock = new Mock<ISPTHttpClient>();
        _tempDirectory = new TempDirectory();

        _requestHandlerMock.Setup(x => x.Host).Returns("http://localhost");

        HttpClient realHttpClient = new();
        _httpClientMock.Setup(x => x.HttpClient).Returns(realHttpClient);
        _requestHandlerMock.Setup(x => x.HttpClient).Returns(_httpClientMock.Object);

        _manager = new SyncActionManager(
            _loggerMock.Object,
            _manifestRepoMock.Object,
            _requestHandlerMock.Object);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _tempDirectory.Dispose();
    }

    [Fact]
    public async Task ProcessSyncActionsAsyncDownloadsFilesWhenActionTypeIsAdd()
    {
        ClientState state = CreateState();
        string relativePath = "BepInEx/plugins/test.dll";
        SyncAction action = new(relativePath, SyncActionType.Add, null, null) { IsSelected = true };
        SyncProposal proposal = CreateProposal([action]);

        await _manager.ProcessSyncActionsAsync(state, proposal);

        _httpClientMock.Verify(x => x.DownloadWithCancellationAsync(
            It.Is<string>(s => s.Contains(Uri.EscapeDataString(relativePath))),
            It.Is<string>(p => p.EndsWith(relativePath)),
            It.IsAny<Action<DownloadProgress>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSyncActionsAsyncCreatesDeleteInstructionWhenActionTypeIsDelete()
    {
        ClientState state = CreateState();
        string relativePath = "BepInEx/plugins/old.dll";
        SyncAction action = new(relativePath, SyncActionType.Delete, null, null) { IsSelected = true };
        SyncProposal proposal = CreateProposal([action]);

        await _manager.ProcessSyncActionsAsync(state, proposal);

        string stagingPath = Path.Combine(_tempDirectory.Path, Constants.ModfatherDataDirectory, Constants.StagingDirectory);
        string expectedInstructionPath = Path.Combine(stagingPath, relativePath + Constants.DeleteInstructionExtension);

        Assert.True(File.Exists(expectedInstructionPath));
        _httpClientMock.Verify(x => x.DownloadWithCancellationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<DownloadProgress>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSyncActionsAsyncReportsProgress()
    {
        ClientState state = CreateState();
        SyncAction action = new("file.dll", SyncActionType.Add, null, null) { IsSelected = true };
        SyncProposal proposal = CreateProposal([action]);
        Mock<IProgress<(float progress, string message, string detail)>> progressMock = new();

        await _manager.ProcessSyncActionsAsync(state, proposal, progressMock.Object);

        progressMock.Verify(x => x.Report(It.IsAny<(float, string, string)>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessSyncActionsAsyncCleansStagingBeforeExecution()
    {
        ClientState state = CreateState();
        SyncProposal proposal = CreateProposal([]);
        string stagingPath = Path.Combine(_tempDirectory.Path, Constants.ModfatherDataDirectory, Constants.StagingDirectory);

        Directory.CreateDirectory(stagingPath);
        string dummyFile = Path.Combine(stagingPath, "should_be_deleted.txt");
        File.WriteAllText(dummyFile, "test");

        await _manager.ProcessSyncActionsAsync(state, proposal);

        Assert.False(File.Exists(dummyFile));
        Assert.True(Directory.Exists(stagingPath));
    }

    [Fact]
    public async Task ProcessSyncActionsAsyncThrowsUnauthorizedWhenPathTraversalDetected()
    {
        ClientState state = CreateState();
        string maliciousPath = "../unsafe.dll";
        SyncAction action = new(maliciousPath, SyncActionType.Add, null, null) { IsSelected = true };
        SyncProposal proposal = CreateProposal([action]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _manager.ProcessSyncActionsAsync(state, proposal));
    }

    private ClientState CreateState()
    {
        return new ClientState(
            _tempDirectory.Path,
            new ClientConfiguration(),
            new ClientManifest(DateTimeOffset.UtcNow, "http://localhost"),
            new ServerManifest([], [], []),
            new FileHashBlacklist());
    }

    private static SyncProposal CreateProposal(List<SyncAction> actions)
    {
        return new SyncProposal(
            new ClientManifest(DateTimeOffset.UtcNow, "http://localhost"),
            actions);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}