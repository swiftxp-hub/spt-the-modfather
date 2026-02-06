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
        float totalSteps = 3;

        progressCallback?.Report((currentProgress, $"Loading resources..."));

        string baseDirectory = clientState.BaseDirectory;

        ClientConfiguration clientConfiguration = clientState.ClientConfiguration;
        ClientManifest? clientManifest = clientState.ClientManifest;
        ServerManifest serverManifest = clientState.ServerManifest;

        bool isTemporaryClientManifest = false;
        if (clientManifest == null)
        {
            progressCallback?.Report((currentProgress, $"Client-Manifest missing. Creating temporary manifest..."));
            clientManifest = await BuildTempClientManifest(baseDirectory, serverManifest, cancellationToken);
            isTemporaryClientManifest = true;
        }

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
                else if (!isTrackedInManifest || isTemporaryClientManifest)
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
        progressCallback?.Report((currentProgress, $"Finished update-check..."));

        return new(clientManifest!, syncActions, serverManifest);
    }

    private async Task<ClientManifest> BuildTempClientManifest(string baseDirectory, ServerManifest serverManifest,
        CancellationToken cancellationToken = default)
    {
        ClientManifest clientManifest = new(DateTimeOffset.MinValue, RequestHandler.Host);

        IEnumerable<FileInfo> fileInfos = baseDirectory.FindFilesByPattern(serverManifest.IncludePatterns, serverManifest.ExcludePatterns);

        foreach (FileInfo fileInfo in fileInfos)
        {
            string? hash = await xxHash128FileHasher.GetFileHashAsync(fileInfo, cancellationToken);

            ClientFileManifest clientFileManifest = new(
                Path.GetRelativePath(baseDirectory, fileInfo.FullName),
                hash ?? string.Empty,
                fileInfo.Length,
                fileInfo.LastWriteTimeUtc);

            clientManifest.AddOrUpdateFile(clientFileManifest);
        }

        return clientManifest;
    }
}