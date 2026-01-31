using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations.Models;
using SwiftXP.SPT.TheModfather.Client.Helpers;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class CheckUpdateService(
    ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryService baseDirectoryService,
    IClientConfigurationLoader clientConfigurationLoader,
    IFileSearchService fileSearchService,
    IFileHashingService fileHashingService) : ICheckUpdateService
{
    public Task<Dictionary<string, ModSyncAction>> CheckForUpdatesAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                ClientConfiguration clientConfiguration = clientConfigurationLoader.LoadOrCreate();
                ServerConfiguration serverConfiguration = await GetServerConfigurationAsync();
                Dictionary<string, string> serverFileHashes = await GetServerFileHashesAsync();

                string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
                string[] pathsToSearch = serverConfiguration.SyncedPaths;
                string[] pathsToExclude = [.. serverConfiguration.ExcludedPaths.Union(clientConfiguration.ExcludedPaths)];

                IEnumerable<string> filePathsToHash = fileSearchService.GetFiles(baseDirectory, pathsToSearch, pathsToExclude);
                Dictionary<string, string> absolutePathHashes = await fileHashingService.GetFileHashes(filePathsToHash);

                Dictionary<string, string> clientFileHashes = new(absolutePathHashes.Count, StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> kvp in absolutePathHashes)
                {
                    string relativePath = Path.GetRelativePath(baseDirectory, kvp.Key);
                    string normalizedKey = relativePath.Replace('\\', '/');

                    clientFileHashes[normalizedKey] = kvp.Value;
                }

                Dictionary<string, ModSyncAction> result;

                if (clientConfiguration.UseHeadlessWhitelist && PluginInfoHelper.IsHeadlessInstalled())
                    result = ModSyncActionDecisionService.DecideOnActions(clientFileHashes, serverFileHashes, clientConfiguration.HeadlessWhitelist);
                else
                    result = ModSyncActionDecisionService.DecideOnActions(clientFileHashes, serverFileHashes);

                return result;
            }
            catch (Exception ex)
            {
                simpleSptLogger.LogException(ex);

                return [];
            }
        });
    }

    private static async Task<ServerConfiguration> GetServerConfigurationAsync()
    {
        string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetServerConfiguration}");
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty JSON-response");
        }

        ServerConfiguration? serverConfiguration = JsonConvert.DeserializeObject<ServerConfiguration>(json);
        if (serverConfiguration == null)
        {
            throw new InvalidOperationException("JSON-response could not be deserialized");
        }

        return serverConfiguration;
    }

    private static async Task<Dictionary<string, string>> GetServerFileHashesAsync()
    {
        string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetHashes}");
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty JSON-response");
        }

        Dictionary<string, string>? serverHashesRaw = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        if (serverHashesRaw == null)
        {
            throw new InvalidOperationException("JSON-response could not be deserialized");
        }

        Dictionary<string, string> serverFileHashes = new(serverHashesRaw, StringComparer.OrdinalIgnoreCase);

        return serverFileHashes;
    }
}