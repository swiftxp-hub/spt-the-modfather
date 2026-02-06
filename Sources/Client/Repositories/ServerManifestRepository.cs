using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public class ServerManifestRepository(ISimpleSptLogger simpleSptLogger) : IServerManifestRepository
{
    public async Task<ServerManifest> LoadAsync(CancellationToken cancellationToken = default)
    {
        string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetServerManifest}");

        simpleSptLogger.LogInfo($"Server-Manifest: {json}");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Empty JSON-response");

        ServerManifest? serverManifest = await Task.Run(() =>
        {
            return JsonConvert.DeserializeObject<ServerManifest>(json);
        }, cancellationToken).ConfigureAwait(false);

        return serverManifest ?? throw new InvalidOperationException("JSON-response could not be deserialized");
    }
}