using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.Common.NETStd.Json;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Repositories;

public class ClientManifestRepositoryTests : IDisposable
{
    private readonly Mock<ISimpleSptLogger> _loggerMock;
    private readonly Mock<IBaseDirectoryLocator> _baseDirectoryLocatorMock;
    private readonly Mock<IJsonFileSerializer> _serializerMock;
    private readonly TempDirectory _tempDirectory;

    public ClientManifestRepositoryTests()
    {
        _loggerMock = new Mock<ISimpleSptLogger>();
        _baseDirectoryLocatorMock = new Mock<IBaseDirectoryLocator>();
        _serializerMock = new Mock<IJsonFileSerializer>();
        _tempDirectory = new TempDirectory();

        _baseDirectoryLocatorMock.Setup(x => x.GetBaseDirectory())
            .Returns(_tempDirectory.DirectoryPath);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _tempDirectory.Dispose();
    }

    [Fact]
    public async Task LoadAsyncReturnsNullWhenFileDoesNotExist()
    {
        ClientManifestRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientManifest? result = await repository.LoadAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadAsyncReturnsManifestWhenFileExists()
    {
        string dir = Path.Combine(_tempDirectory.DirectoryPath, Constants.ModfatherDataDirectory);
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, Constants.ClientManifestFile);
        File.WriteAllText(filePath, "{}");

        ClientManifest expectedManifest = new(DateTimeOffset.UtcNow, "http://url");
        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ClientManifest>(It.Is<string>(s => s == filePath), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedManifest);

        ClientManifestRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientManifest? result = await repository.LoadAsync(CancellationToken.None);

        Assert.Same(expectedManifest, result);
    }

    [Fact]
    public async Task LoadAsyncReturnsNullAndLogsErrorOnJsonException()
    {
        string dir = Path.Combine(_tempDirectory.DirectoryPath, Constants.ModfatherDataDirectory);
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, Constants.ClientManifestFile);
        File.WriteAllText(filePath, "{ invalid }");

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ClientManifest>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Syntax error"));

        ClientManifestRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientManifest? result = await repository.LoadAsync(CancellationToken.None);

        Assert.Null(result);
        _loggerMock.Verify(x => x.LogError(It.Is<string>(s => s.Contains("syntax-error"))), Times.Once);
    }

    [Fact]
    public async Task SaveAsyncDelegatesToSerializer()
    {
        ClientManifestRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://url");

        await repository.SaveAsync(manifest, CancellationToken.None);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.Is<string>(path => path.EndsWith(Constants.ClientManifestFile) && !path.Contains(Constants.StagingDirectory)),
            manifest,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SaveToStagingAsyncDelegatesToSerializerWithStagingPath()
    {
        ClientManifestRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://url");

        await repository.SaveToStagingAsync(manifest, CancellationToken.None);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.Is<string>(path => path.Contains(Constants.StagingDirectory) && path.EndsWith(Constants.ClientManifestFile)),
            manifest,
            CancellationToken.None), Times.Once);
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