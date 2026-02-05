using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public interface IClientExcludesRepository
{
    Task<ClientExcludes> LoadOrCreateDefaultAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ClientExcludes config, CancellationToken cancellationToken = default);
}