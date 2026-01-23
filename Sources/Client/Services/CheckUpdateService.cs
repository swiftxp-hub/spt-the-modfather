using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class CheckUpdateService(
    ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryService baseDirectoryService,
    IFileSearchService fileSearchService,
    IFileHashingService fileHashingService) : ICheckUpdateService
{
    private const string RemotePathToGetFileHashes = "/theModfather/getFileHashes";

    private const string RemotePathToGetServerConfiguration = "/theModfather/getServerConfiguration";
    
    public async Task<Dictionary<string, ModSyncActionEnum>> CheckForUpdatesAsync()
    {
        try
        {
            ServerConfiguration serverConfiguration = await GetServerConfigurationAsync();
            Dictionary<string, string> serverFileHashes = await GetServerFileHashesAsync();

            string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
            string[] pathsToSearch = serverConfiguration.SyncedPaths;
            string[] pathsToExclude = [.. serverConfiguration.ExcludedPaths.Union(Plugin.Configuration!.GetExcludedPaths())];

            IEnumerable<string> filePathsToHash = fileSearchService.GetFiles(baseDirectory, pathsToSearch, pathsToExclude);
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

    private async Task<ServerConfiguration> GetServerConfigurationAsync()
    {
        string json = await RequestHandler.GetJsonAsync(RemotePathToGetServerConfiguration);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception("Empty JSON-response");
        }

        ServerConfiguration? serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(json);
        if (serverConfiguration == null)
        {
            throw new Exception("JSON-response could not be deserialized");
        }

        return serverConfiguration;
    }

    private async Task<Dictionary<string, string>> GetServerFileHashesAsync()
    {
        string json = await RequestHandler.GetJsonAsync(RemotePathToGetFileHashes);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception("Empty JSON-response");
        }

        Dictionary<string, string>? serverHashesRaw = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        if (serverHashesRaw == null)
        {
            throw new Exception("JSON-response could not be deserialized");
        }

        Dictionary<string, string> serverFileHashes = new(serverHashesRaw, StringComparer.OrdinalIgnoreCase);

        return serverFileHashes;
    }
}