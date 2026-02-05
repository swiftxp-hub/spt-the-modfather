using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json;
using SPT.Common.Http;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class UpdateManager(ISimpleSptLogger simpleSptLogger, IXxHash128FileHasher xxHash128FileHasher)
{
    public async Task<IReadOnlyList<SyncAction>> GetSyncActionsAsync(
        IPluginContext pluginContext,
        IProgress<(float progress, string message)>? progressCallback,
        CancellationToken? cancellationToken)
    {
        List<SyncAction> syncActions = [];

        float currentProgress = 0f;

        int stepsTaken = 0;
        int totalSteps = 4;

        progressCallback?.Report((currentProgress, $"Loading client resources..."));

        string baseDirectory = pluginContext.BaseDirectory;

        ClientExcludes clientExcludes = pluginContext.ClientExcludes;
        ClientManifest? clientManifest = pluginContext.ClientManifest;

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Loading server resources..."));

        ServerManifest serverManifest = await GetServerManifest();

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Processing server-manifest..."));

        HashSet<string> processedPaths = [];

        foreach (ServerFileManifest serverFileManifest in serverManifest.Files)
        {
            processedPaths.Add(serverFileManifest.RelativeFilePath);

            if (IsExcluded(serverFileManifest.RelativeFilePath, clientExcludes))
            {
                simpleSptLogger.LogInfo($"Ignoring server-update for file (excluded): {serverFileManifest.RelativeFilePath}");
                continue;
            }

            FileInfo? fileInfo = GetFileInfo(baseDirectory, serverFileManifest.RelativeFilePath);
            bool isTrackedInManifest = clientManifest?.Files.Any(x => x.RelativeFilePath == serverFileManifest.RelativeFilePath) ?? false;

            if (fileInfo != null)
            {
                if (fileInfo.Length != serverFileManifest.SizeInBytes
                    || (await xxHash128FileHasher.GetFileHashAsync(fileInfo, CancellationToken.None)) != serverFileManifest.Hash)
                {
                    syncActions.Add(new() { RelativeFilePath = serverFileManifest.RelativeFilePath, Type = SyncActionType.Update });
                }
                else if (!isTrackedInManifest)
                {
                    syncActions.Add(new() { RelativeFilePath = serverFileManifest.RelativeFilePath, Type = SyncActionType.Adopt });
                }
            }
            else
            {
                syncActions.Add(new() { RelativeFilePath = serverFileManifest.RelativeFilePath, Type = SyncActionType.Add });
            }
        }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Processing client-manifest..."));

        foreach (ClientFileManifest clientFileManifest in clientManifest?.Files ?? [])
        {
            if (processedPaths.Contains(clientFileManifest.RelativeFilePath))
                continue;

            if (IsExcluded(clientFileManifest.RelativeFilePath, clientExcludes))
            {
                syncActions.Add(new() { RelativeFilePath = clientFileManifest.RelativeFilePath, Type = SyncActionType.Untrack });

                continue;
            }

            FileInfo? fileInfo = GetFileInfo(baseDirectory, clientFileManifest.RelativeFilePath);
            if (fileInfo != null)
            {
                syncActions.Add(new() { RelativeFilePath = clientFileManifest.RelativeFilePath, Type = SyncActionType.Delete });
            }
            else
            {
                syncActions.Add(new() { RelativeFilePath = clientFileManifest.RelativeFilePath, Type = SyncActionType.Untrack });
            }
        }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Finished update-check..."));

        return syncActions;
    }

    private async Task<ServerManifest> GetServerManifest()
    {
        string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetServerManifest}");

        simpleSptLogger.LogInfo($"Server-Manifest: {json}");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Empty JSON-response");

        ServerManifest? serverManifest = JsonConvert.DeserializeObject<ServerManifest>(json)
            ?? throw new InvalidOperationException("JSON-response could not be deserialized");

        return serverManifest;
    }

    private static FileInfo? GetFileInfo(string baseDirectory, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return null;

        string requestedFullPath;
        try
        {
            requestedFullPath = Path.GetFullPath(Path.Combine(baseDirectory, relativeFilePath));
        }
        catch (Exception)
        {
            return null;
        }

        FileInfo fileInfo = new(requestedFullPath);

        return fileInfo.Exists ? fileInfo : null;
    }

    private static bool IsExcluded(string relativePath, ClientExcludes clientExcludes)
    {
        Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(clientExcludes);

        return matcher.Match(relativePath).HasMatches;
    }
}