using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Server.States;

public interface IServerManifestManager
{
    Task<ServerManifest> GetServerManifestAsync(CancellationToken cancellationToken = default);

    void WatchForChanges();
}
