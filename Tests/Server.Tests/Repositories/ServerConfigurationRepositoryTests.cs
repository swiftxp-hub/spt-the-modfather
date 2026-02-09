using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.NET9.Json;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.TheModfather.Server.Repositories;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Server.Tests.Repositories;

public class ServerConfigurationRepositoryTests : IDisposable
{
    private readonly Mock<ISptLogger<ServerConfigurationRepository>> _loggerMock;
    private readonly Mock<IBaseDirectoryLocator> _baseDirMock;
    private readonly Mock<IJsonFileSerializer> _serializerMock;

    private readonly TempDirectory _tempDirectory;

    public ServerConfigurationRepositoryTests()
    {
        _loggerMock = new Mock<ISptLogger<ServerConfigurationRepository>>();
        _baseDirMock = new Mock<IBaseDirectoryLocator>();
        _serializerMock = new Mock<IJsonFileSerializer>();

        _tempDirectory = new TempDirectory();

        _baseDirMock.Setup(x => x.GetBaseDirectory())
            .Returns(_tempDirectory.DirInfo.FullName);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        _tempDirectory.Dispose();
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncCreatesFileWhenItDoesNotExist()
    {
        ServerConfigurationRepository repo = CreateRepository();

        _serializerMock.Setup(x => x.SerializeJsonFileAsync(It.IsAny<string>(), It.IsAny<ServerConfiguration>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        ServerConfiguration result = await repo.LoadOrCreateDefaultAsync();

        Assert.NotNull(result);

        _serializerMock.Verify(x => x.SerializeJsonFileAsync(
            It.Is<string>(s => s.Contains(Constants.ServerConfigurationFile)),
            It.IsAny<ServerConfiguration>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("Creating default configuration")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncLoadsFileWhenItExists()
    {
        ServerConfigurationRepository repo = CreateRepository();
        ServerConfiguration existingConfig = new() { ConfigVersion = "TestVersion" };

        _tempDirectory.CreateConfigFile();

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ServerConfiguration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existingConfig);

        ServerConfiguration result = await repo.LoadOrCreateDefaultAsync();

        Assert.Equal("TestVersion", result.ConfigVersion);
        _serializerMock.Verify(x => x.SerializeJsonFileAsync(It.IsAny<string>(), It.IsAny<ServerConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncRestoresDefaultWhenFileIsEmpty()
    {
        ServerConfigurationRepository repo = CreateRepository();
        _tempDirectory.CreateConfigFile();

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ServerConfiguration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((ServerConfiguration?)null);

        ServerConfiguration result = await repo.LoadOrCreateDefaultAsync();

        Assert.NotNull(result);
        _serializerMock.Verify(x => x.SerializeJsonFileAsync(It.IsAny<string>(), It.IsAny<ServerConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("Configuration was empty")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task LoadOrCreateDefaultAsyncThrowsOnJsonSyntaxError()
    {
        ServerConfigurationRepository repo = CreateRepository();
        _tempDirectory.CreateConfigFile();

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ServerConfiguration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new JsonException("Syntax Error"));

        await Assert.ThrowsAsync<JsonException>(() => repo.LoadOrCreateDefaultAsync());

        _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("syntax-error")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task WatchForChangesDetectsFileChangeAndUpdatesCache()
    {
        ServerConfigurationRepository repo = CreateRepository();
        _tempDirectory.CreateConfigFile();

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ServerConfiguration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new ServerConfiguration { ConfigVersion = "V1" });

        await repo.LoadOrCreateDefaultAsync();

        bool eventTriggered = false;
        repo.OnConfigurationChanged += (sender, newConfig) =>
        {
            eventTriggered = true;
            Assert.Equal("V2", newConfig.ConfigVersion);
        };

        repo.WatchForChanges();

        _serializerMock.Setup(x => x.DeserializeJsonFileAsync<ServerConfiguration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ServerConfiguration { ConfigVersion = "V2" });

        await Task.Delay(100);
        File.SetLastWriteTimeUtc(_tempDirectory.ConfigPath, DateTime.UtcNow);

        await Task.Delay(1000);

        Assert.True(eventTriggered, "OnConfigurationChanged event should have fired");
        _loggerMock.Verify(x => x.Info(It.Is<string>(s => s.Contains("cache successfully updated")), It.IsAny<Exception>()), Times.Once);
    }

    private ServerConfigurationRepository CreateRepository()
    {
        return new ServerConfigurationRepository(
            _loggerMock.Object,
            _baseDirMock.Object,
            _serializerMock.Object
        );
    }

    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirInfo { get; }
        public string ConfigPath { get; }

        public TempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DirInfo = Directory.CreateDirectory(path);

            string configDir = Path.Combine(DirInfo.FullName, Constants.ServerConfigurationDirectory);
            Directory.CreateDirectory(configDir);

            ConfigPath = Path.Combine(configDir, Constants.ServerConfigurationFile);
        }

        public void CreateConfigFile()
        {
            File.WriteAllText(ConfigPath, "{}");
        }

        public void Dispose()
        {
            if (DirInfo.Exists)
            {
                try
                {
                    DirInfo.Delete(true);
                }
                catch { }
            }
        }
    }
}