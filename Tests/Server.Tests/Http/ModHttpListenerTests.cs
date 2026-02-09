using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Newtonsoft.Json;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.TheModfather.Server.Http;
using SwiftXP.SPT.TheModfather.Server.IO;
using SwiftXP.SPT.TheModfather.Server.Repositories;
using SwiftXP.SPT.TheModfather.Server.Services;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Server.Tests.Http;

public class ModHttpListenerTests
{
    private readonly Mock<ISptLogger<ModHttpListener>> _loggerMock;
    private readonly Mock<IServerConfigurationRepository> _configRepoMock;
    private readonly Mock<IServerManifestManager> _manifestManagerMock;
    private readonly Mock<IServerFileResolver> _fileResolverMock;

    public ModHttpListenerTests()
    {
        _loggerMock = new Mock<ISptLogger<ModHttpListener>>();
        _configRepoMock = new Mock<IServerConfigurationRepository>();
        _manifestManagerMock = new Mock<IServerManifestManager>();
        _fileResolverMock = new Mock<IServerFileResolver>();
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        DefaultHttpContext context = new();
        context.Request.Path = new PathString(path);
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body);

        return await reader.ReadToEndAsync();
    }

    [Fact]
    public void CanHandleReturnsTrueForMatchingPrefix()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + "/some-action");
        MongoId sessionId = new("54f0e5aa313f5d824680d6c9");

        bool result = listener.CanHandle(sessionId, context);

        Assert.True(result);
    }

    [Fact]
    public void CanHandleReturnsFalseForDifferentPrefix()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext("/other-api/action");

        bool result = listener.CanHandle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.False(result);
    }

    [Fact]
    public async Task HandleGetServerManifestReturnsJson()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetServerManifest);

        ServerManifest dummyManifest = new([], []) { };

        _manifestManagerMock.Setup(x => x.GetServerManifestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyManifest);

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(200, context.Response.StatusCode);
        string body = await ReadResponseBodyAsync(context);
        Assert.Contains("{", body);
    }

    [Fact]
    public async Task HandleGetFileHashBlacklistReturnsJson()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetFileHashBlacklist);

        ServerConfiguration dummyConfig = new()
        {
            FileHashBlacklist = ["hash1", "hash2"]
        };

        _configRepoMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyConfig);

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(200, context.Response.StatusCode);
        string body = await ReadResponseBodyAsync(context);
        Assert.Contains("hash1", body);
    }

    [Fact]
    public async Task HandleGetFileReturns404WhenFileNotFound()
    {
        ModHttpListener listener = CreateListener();
        string fileName = "missing.txt";
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetFile + "/" + fileName);

        _configRepoMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerConfiguration());

        _fileResolverMock.Setup(x => x.GetFileInfo(fileName, It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns((FileInfo?)null);

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(404, context.Response.StatusCode);
        _loggerMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("File not found")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task HandleGetFileReturns400WhenPathSuspicious()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetFile + "/ ");

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(400, context.Response.StatusCode);
        _loggerMock.Verify(x => x.Warning(It.Is<string>(s => s.Contains("suspicious")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task HandleGetFileReturns200WhenFileFound()
    {
        using TempFile tempFile = new("test.txt", "Content");
        ModHttpListener listener = CreateListener();
        string fileName = "test.txt";

        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetFile + "/" + fileName);

        _configRepoMock.Setup(x => x.LoadOrCreateDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerConfiguration());

        FileInfo fileInfo = new(tempFile.Path);

        _fileResolverMock.Setup(x => x.GetFileInfo(fileName, It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(fileInfo);

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(fileInfo.Length, context.Response.ContentLength);
    }

    [Fact]
    public async Task HandleCatchesExceptionAndReturns500()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + Constants.RouteGetServerManifest);

#pragma warning disable CA2201 // Do not raise reserved exception types

        _manifestManagerMock.Setup(x => x.GetServerManifestAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database explosion"));
#pragma warning restore CA2201 // Do not raise reserved exception types

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(500, context.Response.StatusCode);
        _loggerMock.Verify(x => x.Error(It.Is<string>(s => s.Contains("Database explosion")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task HandleUnknownRouteReturns404()
    {
        ModHttpListener listener = CreateListener();
        DefaultHttpContext context = CreateContext(Constants.RoutePrefix + "/unknown-endpoint");

        await listener.Handle(new MongoId("54f0e5aa313f5d824680d6c9"), context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    private ModHttpListener CreateListener()
    {
        return new ModHttpListener(
            _loggerMock.Object,
            _configRepoMock.Object,
            _manifestManagerMock.Object,
            _fileResolverMock.Object
        );
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile(string name, string content)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
            File.WriteAllText(Path, content);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}