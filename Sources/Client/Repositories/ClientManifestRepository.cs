using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public class ClientManifestRepository(ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryLocator baseDirectoryLocator,
    IJsonFileSerializer jsonFileSerializer) : IClientManifestRepository
{
    private readonly string _filePath = Path.GetFullPath(Path.Combine(baseDirectoryLocator.GetBaseDirectory(), Constants.ClientManifestFilePath));

    public async Task<ClientManifest?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            ClientManifest? loadedClientManifest = await jsonFileSerializer.DeserializeJsonFileAsync<ClientManifest>(_filePath, cancellationToken);

            return loadedClientManifest;
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Client-Manifest is invalid (syntax-error): {_filePath}");

            return null;
        }
    }

    public async Task SaveAsync(ClientManifest config, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(_filePath, config, cancellationToken);
    }
}