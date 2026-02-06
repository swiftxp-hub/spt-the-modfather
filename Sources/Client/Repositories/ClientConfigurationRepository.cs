using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public class ClientConfigurationRepository(ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryLocator baseDirectoryLocator,
    IJsonFileSerializer jsonFileSerializer) : IClientConfigurationRepository
{
    private readonly string _filePath = Path.GetFullPath(Path.Combine(baseDirectoryLocator.GetBaseDirectory(),
        Constants.ModfatherDataDirectory, Constants.ClientConfigurationFile));

    private readonly string _stagingFilePath = Path.GetFullPath(Path.Combine(baseDirectoryLocator.GetBaseDirectory(),
        Constants.ModfatherDataDirectory, Constants.StagingDirectory, Constants.ClientConfigurationFile));

    public async Task<ClientConfiguration> LoadOrCreateDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            ClientConfiguration defaultConfig = new();
            await SaveAsync(defaultConfig, cancellationToken);

            return defaultConfig;
        }

        try
        {
            ClientConfiguration? loadedConfig = await jsonFileSerializer.DeserializeJsonFileAsync<ClientConfiguration>(_filePath, cancellationToken);
            loadedConfig ??= new();

            return loadedConfig;
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Configuration is invalid (syntax-error): {_filePath}");

            throw;
        }
    }

    public async Task SaveAsync(ClientConfiguration config, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(_filePath, config, cancellationToken);
    }

    public async Task SaveToStagingAsync(ClientConfiguration config, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(_stagingFilePath, config, cancellationToken);
    }
}