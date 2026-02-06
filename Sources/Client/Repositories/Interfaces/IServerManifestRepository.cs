using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Repositories;

public interface IServerManifestRepository
{
    Task<ServerManifest> LoadAsync(CancellationToken cancellationToken = default);
}