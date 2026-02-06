using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class UpdateManager(ISimpleSptLogger simpleSptLogger,
    IXxHash128FileHasher xxHash128FileHasher) : IUpdateManager
{
    public async Task<SyncProposal> GetSyncActionsAsync(
        ClientState clientState,
        IProgress<(float progress, string message)>? progressCallback,
        CancellationToken cancellationToken = default)
    {
        List<SyncAction> syncActions = [];

        float currentProgress = 0f;

        float stepsTaken = 0;
        float totalSteps = 4;

        progressCallback?.Report((currentProgress, $"Loading resources..."));

        string baseDirectory = clientState.BaseDirectory;

        ClientConfiguration clientConfiguration = clientState.ClientConfiguration;
        ClientManifest? clientManifest = clientState.ClientManifest ?? new ClientManifest(DateTimeOffset.UtcNow, RequestHandler.Host);
        ServerManifest serverManifest = clientState.ServerManifest;
        FileHashBlacklist fileHashBlacklist = clientState.FileHashBlacklist;

        // if (clientManifest == null)
        // {
        //     progressCallback?.Report((currentProgress, $"Client-Manifest missing. Creating temporary manifest..."));
        //     clientManifest = await BuildTempClientManifest(baseDirectory, serverManifest, cancellationToken);
        // }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Processing server-manifest..."));

        HashSet<string> processedPaths = [];

        foreach (ServerFileManifest serverFileManifest in serverManifest.Files)
        {
            processedPaths.Add(serverFileManifest.RelativeFilePath);

            if (serverFileManifest.RelativeFilePath.IsExcludedByPatterns(clientConfiguration.ExcludePatterns))
            {
                simpleSptLogger.LogInfo($"Ignoring server-update for file (excluded): {serverFileManifest.RelativeFilePath}");
                continue;
            }

            string filePath = Path.Combine(baseDirectory, serverFileManifest.RelativeFilePath);

            FileInfo? fileInfo = filePath.GetFileInfo();
            bool isTrackedInManifest = clientManifest?.Files.Any(x => x.RelativeFilePath == serverFileManifest.RelativeFilePath) ?? false;

            if (fileInfo != null)
            {
                if (fileInfo.Length != serverFileManifest.SizeInBytes
                    || (await xxHash128FileHasher.GetFileHashAsync(fileInfo, CancellationToken.None)) != serverFileManifest.Hash)
                {
                    syncActions.Add(new(serverFileManifest.RelativeFilePath, SyncActionType.Update,
                        serverFileManifest.Hash, serverFileManifest.SizeInBytes));
                }
                else if (!isTrackedInManifest)
                {
                    syncActions.Add(new(serverFileManifest.RelativeFilePath, SyncActionType.Adopt,
                        serverFileManifest.Hash, serverFileManifest.SizeInBytes));
                }
            }
            else
            {
                syncActions.Add(new(serverFileManifest.RelativeFilePath, SyncActionType.Add,
                    serverFileManifest.Hash, serverFileManifest.SizeInBytes));
            }
        }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Processing client-manifest..."));

        foreach (ClientFileManifest clientFileManifest in clientManifest?.Files ?? [])
        {
            if (processedPaths.Contains(clientFileManifest.RelativeFilePath))
                continue;

            if (clientFileManifest.RelativeFilePath.IsExcludedByPatterns(clientConfiguration.ExcludePatterns))
            {
                syncActions.Add(new(clientFileManifest.RelativeFilePath, SyncActionType.Untrack));

                continue;
            }

            string filePath = Path.Combine(baseDirectory, clientFileManifest.RelativeFilePath);
            FileInfo? fileInfo = filePath.GetFileInfo();

            if (fileInfo != null)
            {
                syncActions.Add(new(clientFileManifest.RelativeFilePath, SyncActionType.Delete));
            }
            else
            {
                syncActions.Add(new(clientFileManifest.RelativeFilePath, SyncActionType.Untrack));
            }
        }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Processing blacklist..."));

        if (fileHashBlacklist.Any())
        {
            IEnumerable<FileInfo> filesInfos = Path.GetFullPath(Path.Combine(baseDirectory, Constants.BepInExDirectory))
                .FindFilesByPattern(["**/*.dll"], ["cache/*"]);

            foreach (FileInfo fileInfo in filesInfos)
            {
                string? hash = await xxHash128FileHasher.GetFileHashAsync(fileInfo, CancellationToken.None);
                if (hash != null && fileHashBlacklist.Contains(hash))
                {
                    string relativeFilePath = Path.GetRelativePath(baseDirectory, fileInfo.FullName).GetWebFriendlyPath();
                    syncActions.Add(new(relativeFilePath, SyncActionType.Blacklist));
                }
            }
        }

        currentProgress = ++stepsTaken / totalSteps;
        progressCallback?.Report((currentProgress, $"Finished update-check..."));

        return new(clientManifest!, syncActions);
    }
}