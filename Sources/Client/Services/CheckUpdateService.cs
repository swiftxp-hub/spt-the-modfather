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
    public async Task<Dictionary<string, ModSyncAction>> CheckForUpdatesAsync()
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

            Dictionary<string, ModSyncAction> result = new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, string> serverEntry in serverFileHashes)
            {
                if (!clientFileHashes.ContainsKey(serverEntry.Key)
                    && !IsFikaHeadlessFile(serverEntry.Key)
                    && IsHeadlessWhitelisted(serverEntry, clientConfiguration.HeadlessWhitelist))
                {
                    result.Add(serverEntry.Key, ModSyncAction.Add);
                }
            }

            foreach (KeyValuePair<string, string> clientEntry in clientFileHashes)
            {
                bool existsOnServer = serverFileHashes.TryGetValue(clientEntry.Key, out string? serverHash);

                if (!existsOnServer)
                {
                    if (!IsFikaHeadlessFile(clientEntry.Key)
                        && !IsModFile(clientEntry.Key)) // Prevent self-delete
                    {
                        result.Add(clientEntry.Key, ModSyncAction.Delete);
                    }
                }
                else if (!string.Equals(serverHash, clientEntry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(clientEntry.Key, ModSyncAction.Update);
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

    private static bool IsModFile(string key)
    {
        return key.EndsWith(Constants.ModDllPath, StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(Constants.UpdaterExecutableName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFikaHeadlessFile(string key)
    {
        return key.EndsWith(Constants.FikaHeadlessDll, StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(Constants.LicenseHeadlessMd, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHeadlessWhitelisted(KeyValuePair<string, string> entry, string[] headlessWhitelist)
    {
        if (PluginInfoHelper.IsFikaHeadlessInstalled())
        {
            Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
            matcher.AddIncludePatterns(headlessWhitelist);

            return matcher.Match(entry.Key).HasMatches;
        }

        return true;
    }
}