using System;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Server.Repositories;

public interface IServerConfigurationRepository
{
    event EventHandler<ServerConfiguration> OnConfigurationChanged;

    Task<ServerConfiguration> LoadOrCreateDefaultAsync(CancellationToken cancellationToken = default);

    void WatchForChanges();
}