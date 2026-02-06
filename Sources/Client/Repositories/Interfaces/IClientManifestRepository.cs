using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public interface IClientManifestRepository
{
    Task<ClientManifest?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(ClientManifest config, CancellationToken cancellationToken);

    Task SaveToStagingAsync(ClientManifest config, CancellationToken cancellationToken);
}