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

public class FileHashBlacklistRepositoryTests
{
    private readonly Mock<ISPTRequestHandler> _requestHandlerMock;

    public FileHashBlacklistRepositoryTests()
    {
        _requestHandlerMock = new Mock<ISPTRequestHandler>();
    }

    [Fact]
    public async Task LoadAsyncReturnsBlacklistWhenJsonIsValid()
    {
        FileHashBlacklist expectedBlacklist = [];
        string json = JsonConvert.SerializeObject(expectedBlacklist);

        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(json);

        FileHashBlacklistRepository repository = new(_requestHandlerMock.Object);

        FileHashBlacklist result = await repository.LoadAsync(CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidOperationWhenJsonIsWhitespace()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("   ");

        FileHashBlacklistRepository repository = new(_requestHandlerMock.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(CancellationToken.None));

        Assert.Equal("Empty JSON-response", exception.Message);
    }

    [Fact]
    public async Task LoadAsyncThrowsInvalidOperationWhenJsonIsNullString()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("null");

        FileHashBlacklistRepository repository = new(_requestHandlerMock.Object);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(CancellationToken.None));

        Assert.Equal("JSON-response could not be deserialized", exception.Message);
    }

    [Fact]
    public async Task LoadAsyncPropagatesJsonExceptionWhenJsonIsMalformed()
    {
        _requestHandlerMock.Setup(x => x.GetJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("{ invalid json");

        FileHashBlacklistRepository repository = new(_requestHandlerMock.Object);

        await Assert.ThrowsAnyAsync<JsonException>(() => repository.LoadAsync(CancellationToken.None));
    }
}