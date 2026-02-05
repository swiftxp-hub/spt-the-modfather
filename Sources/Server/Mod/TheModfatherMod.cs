using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.External;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Server.Http;
using SwiftXP.SPT.TheModfather.Server.Repositories;
using SwiftXP.SPT.TheModfather.Server.States;

namespace SwiftXP.SPT.TheModfather.Server;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 1)]

#pragma warning disable CS9113 // Parameter is unread.
public class TheModfatherMod(ISptLogger<TheModfatherMod> sptLogger,
    IServerConfigurationRepository serverConfigurationRepository,
    IServerManifestManager serverManifestManager,
    IModHttpListener httpListener) : IPreSptLoadModAsync
#pragma warning restore CS9113 // Parameter is unread.
{
    public async Task PreSptLoadAsync()
    {
        // Init server-configuration
        _ = await serverConfigurationRepository.LoadOrCreateDefaultAsync();
        serverConfigurationRepository.WatchForChanges();

        // Init server-manifest
        _ = await serverManifestManager.GetServerManifestAsync();
        serverManifestManager.WatchForChanges();
    }
}