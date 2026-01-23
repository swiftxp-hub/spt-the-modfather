using System.Collections.Generic;
using System.IO;
using System.Linq;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.Common.Services;
using SwiftXP.SPT.Common.Extensions;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Server.Services;

[Injectable(InjectionType.Scoped)]
public class ServerFilesHashingService(
    IBaseDirectoryService baseDirectoryService,
    IFileSearchService fileSearchService,
    IFileHashingService fileHashingService) : IServerFilesHashingService
{
    private static readonly string[] PathsToSearch = 
    [
        "BepInEx/patchers",
        "BepInEx/plugins"
    ];

    private static readonly string[] PathsToExclude = 
    [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];

    public Dictionary<string, string> Get()
    {
        string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
        
        IEnumerable<string> filePathsToHash = fileSearchService.GetFiles(baseDirectory, PathsToSearch, PathsToExclude);
        Dictionary<string, string> absolutePathHashes = fileHashingService.GetFileHashes(filePathsToHash);

        Dictionary<string, string> relativePathHashes = new Dictionary<string, string>(absolutePathHashes.Count);

        foreach (KeyValuePair<string, string> kvp in absolutePathHashes)
        {
            string relativePath = Path.GetRelativePath(baseDirectory, kvp.Key);
            string webFriendlyPath = relativePath.Replace('\\', '/');

            relativePathHashes[webFriendlyPath] = kvp.Value;
        }

        return relativePathHashes;
    }
}