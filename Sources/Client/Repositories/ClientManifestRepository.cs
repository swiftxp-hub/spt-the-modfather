using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Repositories;

namespace SwiftXP.SPT.TheModfather.Client.Data.Loaders;

public class ClientManifestRepository(ISimpleSptLogger simpleSptLogger, IJsonFileSerializer jsonFileSerializer) : IClientManifestRepository
{
    private static readonly string s_filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Constants.ClientManifestFilePath));

    public async Task<ClientManifest?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(s_filePath))
            return null;

        try
        {
            ClientManifest? loadedClientManifest = await jsonFileSerializer.DeserializeJsonFileAsync<ClientManifest>(s_filePath, cancellationToken);

            return loadedClientManifest;
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Client-Manifest is invalid (syntax-error): {s_filePath}");

            return null;
        }
    }

    public async Task SaveAsync(ClientManifest config, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(s_filePath, config, cancellationToken);
    }
}