using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Repositories;

namespace SwiftXP.SPT.TheModfather.Client.Data.Loaders;

public class ClientExcludesRepository(ISimpleSptLogger simpleSptLogger, IJsonFileSerializer jsonFileSerializer) : IClientExcludesRepository
{
    private static readonly string s_filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Constants.ClientExcludesFilePath));

    public async Task<ClientExcludes> LoadOrCreateDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(s_filePath))
        {
            ClientExcludes defaultConfig = [];
            await SaveAsync(defaultConfig, cancellationToken);

            return defaultConfig;
        }

        try
        {
            ClientExcludes? loadedConfig = await jsonFileSerializer.DeserializeJsonFileAsync<ClientExcludes>(s_filePath, cancellationToken);
            loadedConfig ??= [];

            return loadedConfig;
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Configuration is invalid (syntax-error): {s_filePath}");

            throw;
        }
    }

    public async Task SaveAsync(ClientExcludes config, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(s_filePath, config, cancellationToken);
    }
}