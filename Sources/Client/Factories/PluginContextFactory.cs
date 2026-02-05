using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Repositories;

namespace SwiftXP.SPT.TheModfather.Client.Contexts;

public class PluginContextFactory(IBaseDirectoryLocator baseDirectoryLocator,
    IClientExcludesRepository clientExcludesRepository,
    IClientManifestRepository clientManifestRepository)
{
    public async Task<IPluginContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        string baseDirectory = baseDirectoryLocator.GetBaseDirectory();
        ClientExcludes clientExcludes = await clientExcludesRepository.LoadOrCreateDefaultAsync(cancellationToken);
        ClientManifest? clientManifest = await clientManifestRepository.LoadAsync(cancellationToken);

        return new PluginContext(baseDirectory, clientExcludes, clientManifest);
    }
}