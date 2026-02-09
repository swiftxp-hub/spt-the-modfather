using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class UpdateManager(ISimpleSptLogger simpleSptLogger,
    IXxHash128FileHasher xxHash128FileHasher,
    ISPTRequestHandler sptRequestHandler) : IUpdateManager
{
    public async Task<SyncProposal> GetSyncActionsAsync(
        ClientState clientState,
        IProgress<(float progress, string message)>? progressCallback,
        CancellationToken cancellationToken = default)
    {
        List<SyncAction> syncActions = [];

        Report(progressCallback, 0.1f, "Loading resources...");

        string baseDirectory = clientState.BaseDirectory;
        ClientConfiguration clientConfig = clientState.ClientConfiguration;
        ServerManifest serverManifest = clientState.ServerManifest;
        FileHashBlacklist blacklist = clientState.FileHashBlacklist;

        ClientManifest clientManifest = clientState.ClientManifest
                             ?? new ClientManifest(DateTimeOffset.UtcNow, sptRequestHandler.Host);

        Report(progressCallback, 0.3f, "Processing server-manifest...");
        HashSet<string> processedServerPaths = [];

        await AnalyzeServerFilesAsync(
            baseDirectory,
            serverManifest,
            clientManifest,
            clientConfig,
            syncActions,
            processedServerPaths,
            cancellationToken);

        Report(progressCallback, 0.6f, "Processing client-manifest...");

        await AnalyzeLocalFiles(
            baseDirectory,
            clientManifest,
            clientConfig,
            syncActions,
            processedServerPaths,
            cancellationToken);

        Report(progressCallback, 0.8f, "Processing blacklist...");

        await AnalyzeBlacklistAsync(
            baseDirectory,
            blacklist,
            syncActions,
            cancellationToken);

        Report(progressCallback, 1.0f, "Finished update-check...");

        syncActions = [.. syncActions.OrderBy(x => x.Type).ThenBy(x => x.RelativeFilePath)];

        return new SyncProposal(clientManifest, syncActions);
    }

    private async Task AnalyzeServerFilesAsync(
        string baseDirectory,
        ServerManifest serverManifest,
        ClientManifest clientManifest,
        ClientConfiguration clientConfig,
        List<SyncAction> syncActions,
        HashSet<string> processedPaths,
        CancellationToken cancellationToken = default)
    {
        foreach (ServerFileManifest serverFile in serverManifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedPaths.Add(serverFile.RelativeFilePath);

            if (serverFile.RelativeFilePath.IsExcludedByPatterns(clientConfig.ExcludePatterns))
            {
                simpleSptLogger.LogInfo($"Ignoring server-update for file (excluded): {serverFile.RelativeFilePath}");
                continue;
            }

            string localFullPath = Path.Combine(baseDirectory, serverFile.RelativeFilePath);
            SyncActionType? actionType = await DetermineServerFileActionAsync(localFullPath, serverFile, clientManifest, cancellationToken);

            if (actionType.HasValue)
            {
                syncActions.Add(new SyncAction(
                    serverFile.RelativeFilePath,
                    actionType.Value,
                    serverFile.Hash,
                    serverFile.SizeInBytes));
            }
        }
    }

#pragma warning disable CS1998
    private async static Task AnalyzeLocalFiles(
        string baseDirectory,
        ClientManifest clientManifest,
        ClientConfiguration clientConfig,
        List<SyncAction> syncActions,
        HashSet<string> processedServerPaths,
        CancellationToken cancellationToken = default)
    {
#pragma warning restore CS1998
        foreach (ClientFileManifest clientFile in clientManifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (processedServerPaths.Contains(clientFile.RelativeFilePath))
                continue;

            if (clientFile.RelativeFilePath.IsExcludedByPatterns(clientConfig.ExcludePatterns))
            {
                syncActions.Add(new SyncAction(clientFile.RelativeFilePath, SyncActionType.Untrack));

                continue;
            }

            string fullPath = Path.Combine(baseDirectory, clientFile.RelativeFilePath);
            bool exists = File.Exists(fullPath);

            SyncActionType type = exists ? SyncActionType.Delete : SyncActionType.Untrack;

            syncActions.Add(new SyncAction(clientFile.RelativeFilePath, type));
        }
    }

    private async Task AnalyzeBlacklistAsync(
        string baseDirectory,
        FileHashBlacklist blacklist,
        List<SyncAction> syncActions,
        CancellationToken cancellationToken)
    {
        if (!blacklist.Any())
            return;

        string pluginsDir = Path.GetFullPath(Path.Combine(baseDirectory, Constants.BepInExDirectory));
        IEnumerable<FileInfo> dllFiles = pluginsDir.FindFilesByPattern(["**/*.dll"], ["cache/*"]);

        foreach (FileInfo fileInfo in dllFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? hash = await xxHash128FileHasher.GetFileHashAsync(fileInfo, cancellationToken);

            if (hash != null && blacklist.Contains(hash))
            {
                string relPath = Path.GetRelativePath(baseDirectory, fileInfo.FullName).GetWebFriendlyPath();
                syncActions.Add(new SyncAction(relPath, SyncActionType.Blacklist));
            }
        }
    }

    private async Task<SyncActionType?> DetermineServerFileActionAsync(
        string localPath,
        ServerFileManifest serverFile,
        ClientManifest clientManifest,
        CancellationToken cancellationToken = default)
    {
        FileInfo fileInfo = new(localPath);
        if (!fileInfo.Exists)
            return SyncActionType.Add;

        bool hashMismatch = false;

        if (fileInfo.Length != serverFile.SizeInBytes)
        {
            hashMismatch = true;
        }
        else
        {
            string? localHash = await xxHash128FileHasher.GetFileHashAsync(fileInfo, cancellationToken);
            if (localHash != serverFile.Hash)
            {
                hashMismatch = true;
            }
        }

        if (hashMismatch)
            return SyncActionType.Update;

        bool isTracked = clientManifest.Files.Any(x => x.RelativeFilePath == serverFile.RelativeFilePath);
        if (!isTracked)
            return SyncActionType.Adopt;

        return null;
    }

    private static void Report(IProgress<(float, string)>? progress, float value, string message)
    {
        progress?.Report((value, message));
    }
}