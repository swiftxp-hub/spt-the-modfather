using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class CheckUpdateService(
    ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryService baseDirectoryService,
    IFileSearchService fileSearchService,
    IFileHashingService fileHashingService) : ICheckUpdateService
{
    private const string RemotePathToGetFileHashes = "/theModfather/getFileHashes";

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
    
    public async Task<Dictionary<string, ModSyncActionEnum>> CheckForUpdatesAsync()
    {
        try
        {
            string json = await RequestHandler.GetJsonAsync(RemotePathToGetFileHashes);
            if (string.IsNullOrWhiteSpace(json))
            {
                simpleSptLogger.LogError("[The Modfather] Empty JSON-response");
                return [];
            }

            Dictionary<string, string>? serverHashesRaw = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (serverHashesRaw == null)
            {
                simpleSptLogger.LogError("[The Modfather] JSON-response could not be deserialized");
                return [];
            }

            Dictionary<string, string> serverFileHashes = new Dictionary<string, string>(serverHashesRaw, StringComparer.OrdinalIgnoreCase);

            string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
            IEnumerable<string> filePathsToHash = fileSearchService.GetFiles(baseDirectory, PathsToSearch, PathsToExclude);
            Dictionary<string, string> absolutePathHashes = fileHashingService.GetFileHashes(filePathsToHash);

            Dictionary<string, string> clientFileHashes = new Dictionary<string, string>(absolutePathHashes.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in absolutePathHashes)
            {
                string relativePath = Path.GetRelativePath(baseDirectory, kvp.Key);
                string normalizedKey = relativePath.Replace('\\', '/');
                
                clientFileHashes[normalizedKey] = kvp.Value;
            }

            Dictionary<string, ModSyncActionEnum> result = new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> serverEntry in serverFileHashes)
            {
                if (!clientFileHashes.ContainsKey(serverEntry.Key))
                    result.Add(serverEntry.Key, ModSyncActionEnum.Add);
            }

            foreach (KeyValuePair<string, string> clientEntry in clientFileHashes)
            {
                if (!serverFileHashes.TryGetValue(clientEntry.Key, out string? serverHash))
                {
                    result.Add(clientEntry.Key, ModSyncActionEnum.Delete);
                }
                else if (!string.Equals(serverHash, clientEntry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(clientEntry.Key, ModSyncActionEnum.Update);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogException(ex);
            return [];
        }
    }
}