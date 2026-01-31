using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class ModSyncActionDecisionService()
{
    public static Dictionary<string, ModSyncAction> DecideOnActions(
        Dictionary<string, string> clientFileHashes,
        Dictionary<string, string> serverFileHashes,
        string[]? whitelist = null)
    {
        Dictionary<string, ModSyncAction> result = new(StringComparer.OrdinalIgnoreCase);

        Matcher? matcher = null;
        if (whitelist != null)
        {
            matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddIncludePatterns(whitelist);
        }

        foreach (KeyValuePair<string, string> serverEntry in serverFileHashes)
        {
            bool isAllowed = matcher == null || matcher.Match(serverEntry.Key).HasMatches;

            if (!clientFileHashes.ContainsKey(serverEntry.Key)
                && !IsFikaHeadlessFile(serverEntry.Key)
                && isAllowed)
            {
                result.Add(serverEntry.Key, ModSyncAction.Add);
            }
        }

        foreach (KeyValuePair<string, string> clientEntry in clientFileHashes)
        {
            bool existsOnServer = serverFileHashes.TryGetValue(clientEntry.Key, out string? serverHash);
            bool isAllowed = matcher == null || matcher.Match(clientEntry.Key).HasMatches;

            if (!existsOnServer || !isAllowed)
            {
                // Prevent deletion of fika-headless and self
                if (!IsFikaHeadlessFile(clientEntry.Key) && !IsModFile(clientEntry.Key))
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

    private static bool IsModFile(string key)
    {
        return key.EndsWith(Constants.ModDllPath, StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(Constants.UpdaterExecutableName, StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(Constants.MsGlobbingDllPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFikaHeadlessFile(string key)
    {
        return key.EndsWith(Constants.FikaHeadlessDll, StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(Constants.LicenseHeadlessMd, StringComparison.OrdinalIgnoreCase);
    }
}