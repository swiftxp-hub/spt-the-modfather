using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public class ServerManifestRepository(ISPTRequestHandler requestHandler) : IServerManifestRepository
{
    public async Task<ServerManifest> LoadAsync(CancellationToken cancellationToken = default)
    {
        string json = await requestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetServerManifest}");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Empty JSON-response");

        ServerManifest? serverManifest = await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<ServerManifest>(json);
        }, cancellationToken).ConfigureAwait(false);

        return serverManifest ?? throw new InvalidOperationException("JSON-response could not be deserialized");
    }
}