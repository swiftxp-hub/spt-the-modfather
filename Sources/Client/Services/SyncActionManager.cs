using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Client.Repositories;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class SyncActionManager(ISimpleSptLogger simpleSptLogger,
    IClientManifestRepository clientManifestRepository,
    ISPTRequestHandler sptRequestHandler) : ISyncActionManager
{
    public async Task ProcessSyncActionsAsync(
        ClientState clientState,
        SyncProposal syncProposal,
        IProgress<(float progress, string message, string detail)>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        string stagingDirectory = GetAndValidateStagingDirectory(clientState.BaseDirectory);

        CleanUpStagingDirectory(stagingDirectory);

        await ExecuteSyncOperationsAsync(
            syncProposal.SyncActions,
            stagingDirectory,
            progressCallback,
            cancellationToken);

        ClientManifest newManifest = BuildNewClientManifest(
            clientState.ServerManifest,
            syncProposal,
            clientState.ClientConfiguration,
            sptRequestHandler.Host);

        await clientManifestRepository.SaveToStagingAsync(newManifest, cancellationToken);
    }

    private async Task ExecuteSyncOperationsAsync(
        IReadOnlyList<SyncAction> syncActions,
        string stagingDirectory,
        IProgress<(float progress, string message, string detail)>? progressReporter,
        CancellationToken cancellationToken)
    {
        List<SyncAction> selectedActions = [.. syncActions.Where(x => x.IsSelected)];
        float total = selectedActions.Count + 1;
        float current = 1;

        progressReporter?.Report((current / total, "Starting synchronization...", string.Empty));

        foreach (SyncAction syncAction in selectedActions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;

            switch (syncAction.Type)
            {
                case SyncActionType.Add:
                case SyncActionType.Update:
                    progressReporter?.Report((current / total, $"Downloading: {syncAction.RelativeFilePath}...", string.Empty));
                    await DownloadFileWithTimeoutAsync(stagingDirectory, syncAction.RelativeFilePath, current / total, progressReporter, cancellationToken);

                    break;

                case SyncActionType.Delete:
                case SyncActionType.Blacklist:
                    progressReporter?.Report((current / total, $"Staging removal for {syncAction.RelativeFilePath}...", string.Empty));
                    await CreateDeleteInstruction(stagingDirectory, syncAction.RelativeFilePath, cancellationToken);

                    break;
            }
        }
    }

    private async Task DownloadFileWithTimeoutAsync(string stagingDir, string relativePath, float progress,
        IProgress<(float progress, string message, string detail)>? progressReporter, CancellationToken cancellationToken)
    {
        TimeSpan originalTimeout = sptRequestHandler.HttpClient.HttpClient.Timeout;
        try
        {
            sptRequestHandler.HttpClient.HttpClient.Timeout = TimeSpan.FromMinutes(15);

            string url = $"{Constants.RoutePrefix}{Constants.RouteGetFile}/{Uri.EscapeDataString(relativePath)}";
            string destPath = Path.Combine(stagingDir, relativePath);

            ValidatePathSecurity(stagingDir, destPath);
            EnsureDirectoryExists(destPath);

            await sptRequestHandler.HttpClient.DownloadWithCancellationAsync(url, destPath, (downloadProgress) =>
            {
                string downloadText = $"Download Speed: {downloadProgress.DownloadSpeed} | Progress: {downloadProgress.FileSizeInfo}";
                progressReporter?.Report((progress, $"Downloading: {relativePath}...", downloadText));
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogError($"Failed to download '{relativePath}': {ex.Message}");

            throw;
        }
        finally
        {
            sptRequestHandler.HttpClient.HttpClient.Timeout = originalTimeout;
        }
    }

    private static async Task CreateDeleteInstruction(string stagingDir, string relativePath, CancellationToken cancellationToken = default)
    {
        string path = Path.Combine(stagingDir, relativePath + Constants.DeleteInstructionExtension);
        EnsureDirectoryExists(path);

        await File.WriteAllTextAsync(path, string.Empty, cancellationToken);
    }

    private static ClientManifest BuildNewClientManifest(
        ServerManifest serverManifest,
        SyncProposal syncProposal,
        ClientConfiguration clientConfiguration,
        string serverUrl)
    {
        ClientManifest newManifest = new(DateTimeOffset.UtcNow, serverUrl);

        Dictionary<string, SyncAction> acceptedMap = syncProposal.SyncActions.Where(a => a.IsSelected).ToDictionary(a => a.RelativeFilePath);
        Dictionary<string, SyncAction> rejectedMap = syncProposal.SyncActions.Where(a => !a.IsSelected).ToDictionary(a => a.RelativeFilePath);

        foreach (ServerFileManifest serverFileManifest in serverManifest.Files)
        {
            if (serverFileManifest.RelativeFilePath.IsExcludedByPatterns(clientConfiguration.ExcludePatterns))
                continue;

            if (acceptedMap.TryGetValue(serverFileManifest.RelativeFilePath, out SyncAction? action))
            {
                if (action.Type is SyncActionType.Add or SyncActionType.Update or SyncActionType.Adopt)
                    newManifest.AddOrUpdateFile(ClientFileManifest.ToClientManifestEntry(serverFileManifest));

                continue;
            }

            if (rejectedMap.ContainsKey(serverFileManifest.RelativeFilePath))
            {
                ClientFileManifest oldEntry = syncProposal.ClientManifest.Files.FirstOrDefault(x => x.RelativeFilePath == serverFileManifest.RelativeFilePath);
                if (oldEntry != null)
                    newManifest.AddOrUpdateFile(oldEntry);

                continue;
            }

            newManifest.AddOrUpdateFile(ClientFileManifest.ToClientManifestEntry(serverFileManifest));
        }

        return newManifest;
    }

    private static string GetAndValidateStagingDirectory(string baseDirectory)
    {
        string path = Path.GetFullPath(Path.Combine(baseDirectory, Constants.ModfatherDataDirectory, Constants.StagingDirectory));

        if (!path.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid staging path configuration.");

        return path;
    }

    private static void ValidatePathSecurity(string rootDir, string fullPath)
    {
        string normalizedRoot = Path.GetFullPath(rootDir) + Path.DirectorySeparatorChar;

        if (!Path.GetFullPath(fullPath).StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Security Alert: Path traversal attempt blocked: {fullPath}");
    }

    private static void CleanUpStagingDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}