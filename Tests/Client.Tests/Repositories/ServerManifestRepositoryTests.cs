using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Repositories;

public class ServerManifestRepositoryTests
{
    private readonly Mock<ISPTRequestHandler> _requestHandlerMock;

    public ServerManifestRepositoryTests()
    {
        _requestHandlerMock = new Mock<ISPTRequestHandler>();
    }

    [Fact]
    public async Task LoadAsyncReturnsManifestWhenJsonIsValid()
    {
        ServerManifest expectedManifest = new([], [], []);
        string json = JsonConvert.SerializeObject(expectedManifest);

        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(json);

        ServerManifestRepository repository = new(_requestHandlerMock.Object);

        ServerManifest result = await repository.LoadAsync(CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidOperationWhenJsonIsWhitespace()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("   ");

        ServerManifestRepository repository = new(_requestHandlerMock.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(CancellationToken.None));

        Assert.Equal("Empty JSON-response", exception.Message);
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidOperationWhenJsonIsNullString()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("null");

        ServerManifestRepository repository = new(_requestHandlerMock.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(CancellationToken.None));

        Assert.Equal("JSON-response could not be deserialized", exception.Message);
    }

    [Fact]
    public async Task LoadAsyncPropagatesJsonExceptionWhenJsonIsMalformed()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("{ invalid json");

        ServerManifestRepository repository = new(_requestHandlerMock.Object);

        await Assert.ThrowsAnyAsync<JsonException>(() => repository.LoadAsync(CancellationToken.None));
    }
}