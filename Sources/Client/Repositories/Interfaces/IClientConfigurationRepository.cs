using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public interface IClientConfigurationRepository
{
    Task<ClientConfiguration> LoadOrCreateDefaultAsync(CancellationToken cancellationToken);

    Task SaveAsync(ClientConfiguration config, CancellationToken cancellationToken);

    Task SaveToStagingAsync(ClientConfiguration config, CancellationToken cancellationToken);
}