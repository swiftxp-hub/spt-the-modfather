using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.NETStd.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Repositories;

public class ClientConfigurationRepositoryTests : IDisposable
{
    private readonly Mock<ISimpleSptLogger> _loggerMock;
    private readonly Mock<IBaseDirectoryLocator> _baseDirectoryLocatorMock;
    private readonly Mock<IJsonFileSerializer> _serializerMock;
    private readonly TempDirectory _tempDirectory;

    public ClientConfigurationRepositoryTests()
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
    public async Task LoadOrCreateDefaultAsyncCreatesAndReturnsDefaultWhenFileMissing()
    {
        ClientConfigurationRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientConfiguration result = await repository.LoadOrCreateDefaultAsync(CancellationToken.None);

        Assert.NotNull(result);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.IsAny<string>(),
            It.IsAny<ClientConfiguration>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncReturnsLoadedConfigWhenFileExists()
    {
        ClientConfiguration expectedConfig = new();

        string configDir = Path.Combine(_tempDirectory.DirectoryPath, Constants.ModfatherDataDirectory);
        Directory.CreateDirectory(configDir);

        string configPath = Path.Combine(configDir, Constants.ClientConfigurationFile);
        File.WriteAllText(configPath, "{}");

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ClientConfiguration>(
            It.Is<string>(s => s == configPath),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        ClientConfigurationRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientConfiguration result = await repository.LoadOrCreateDefaultAsync(CancellationToken.None);

        Assert.Same(expectedConfig, result);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.IsAny<string>(),
            It.IsAny<ClientConfiguration>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncLogsAndRethrowsOnJsonException()
    {
        string configDir = Path.Combine(_tempDirectory.DirectoryPath, Constants.ModfatherDataDirectory);
        Directory.CreateDirectory(configDir);

        string configPath = Path.Combine(configDir, Constants.ClientConfigurationFile);
        File.WriteAllText(configPath, "{ invalid json }");

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ClientConfiguration>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JsonException("Syntax error"));

        ClientConfigurationRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        await Assert.ThrowsAsync<JsonException>(() => repository.LoadOrCreateDefaultAsync(CancellationToken.None));

        _loggerMock.Verify(x => x.LogError(It.Is<string>(s => s.Contains("syntax-error"))), Times.Once);
    }

    [Fact]
    public async Task SaveAsyncDelegatesToSerializer()
    {
        ClientConfigurationRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientConfiguration config = new();

        await repository.SaveAsync(config, CancellationToken.None);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.Is<string>(path => path.EndsWith(Constants.ClientConfigurationFile)),
            config,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SaveToStagingAsyncDelegatesToSerializerWithStagingPath()
    {
        ClientConfigurationRepository repository = new(
            _loggerMock.Object,
            _baseDirectoryLocatorMock.Object,
            _serializerMock.Object);

        ClientConfiguration config = new();

        await repository.SaveToStagingAsync(config, CancellationToken.None);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.Is<string>(path => path.Contains(Constants.StagingDirectory)),
            config,
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