using System.Collections.Generic;
using System.IO;
using SPTarkov.DI.Annotations;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Server.Services;

[Injectable(InjectionType.Scoped)]
public class ServerFilesHashingService(
    IServerConfigurationLoader serverConfigurationLoader,
    IBaseDirectoryService baseDirectoryService,
    IFileSearchService fileSearchService,
    IFileHashingService fileHashingService) : IServerFilesHashingService
{
    public Dictionary<string, string> Get()
    {
        ServerConfiguration serverConfiguration = serverConfigurationLoader.LoadOrCreate();
        string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
        
        IEnumerable<string> filePathsToHash = fileSearchService.GetFiles(baseDirectory, serverConfiguration.SyncedPaths, serverConfiguration.ExcludedPaths);
        Dictionary<string, string> absolutePathHashes = fileHashingService.GetFileHashes(filePathsToHash);

        Dictionary<string, string> relativePathHashes = new(absolutePathHashes.Count);

        foreach (KeyValuePair<string, string> kvp in absolutePathHashes)
        {
            string relativePath = Path.GetRelativePath(baseDirectory, kvp.Key);
            string webFriendlyPath = relativePath.Replace('\\', '/');

            relativePathHashes[webFriendlyPath] = kvp.Value;
        }

        return relativePathHashes;
    }
}