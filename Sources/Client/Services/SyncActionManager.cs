using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.Http;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class SyncActionManager(ISimpleSptLogger simpleSptLogger,
    IJsonFileSerializer jsonFileSerializer) : ISyncActionManager
{
    public async Task ProcessSyncActionsAsync(ClientState clientState, IReadOnlyList<SyncAction> syncActions,
        Progress<(float progress, string message)>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        string baseDirectory = clientState.BaseDirectory;
        string stagingDirectory = Path.GetFullPath(Path.Combine(baseDirectory, Constants.StagingDirectory));

        if (!stagingDirectory.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid staging path");

        ClientConfiguration clientConfiguration = clientState.ClientConfiguration;
        ClientManifest currentClientManifest = clientState.ClientManifest!;

        ClientManifest newClientManifest = new(DateTimeOffset.UtcNow, RequestHandler.Host);
        ServerManifest serverManifest = clientState.ServerManifest;

        float updateProgress = 0f;

        int actionsExecuted = 0;
        int totalActions = 1 + syncActions.Count;

        CleanUpStagingDirectory(stagingDirectory);
        updateProgress = ++actionsExecuted / totalActions;

        foreach (SyncAction syncAction in syncActions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!syncAction.IsSelected)
            {
                clientConfiguration.ExcludePatterns = [.. clientConfiguration.ExcludePatterns, syncAction.RelativeFilePath];

                continue;
            }

            switch (syncAction.Type)
            {
                case SyncActionType.Add:
                case SyncActionType.Update:
                    await DownloadFileAsync(stagingDirectory, syncAction.RelativeFilePath, progressCallback, cancellationToken);

                    break;

                case SyncActionType.Delete:
                    CreateDeleteInstruction(stagingDirectory, syncAction.RelativeFilePath);

                    break;
            }
        }

        Dictionary<string, SyncAction> userRejectedActions = syncActions.Where(a => !a.IsSelected).ToDictionary(a => a.RelativeFilePath);
        Dictionary<string, SyncAction> acceptedActions = syncActions.Where(a => a.IsSelected).ToDictionary(a => a.RelativeFilePath);

        foreach (ServerFileManifest serverFileManifest in serverManifest.Files)
        {
            if (serverFileManifest.RelativeFilePath.IsExcludedByPatterns(clientConfiguration.ExcludePatterns))
                continue;

            if (acceptedActions.TryGetValue(serverFileManifest.RelativeFilePath, out SyncAction? syncAction))
            {
                if (syncAction.Type == SyncActionType.Add ||
                   syncAction.Type == SyncActionType.Update ||
                   syncAction.Type == SyncActionType.Adopt)
                {
                    newClientManifest.AddOrUpdateFile(new ClientFileManifest(
                        serverFileManifest.RelativeFilePath,
                        serverFileManifest.Hash,
                        serverFileManifest.SizeInBytes,
                        DateTimeOffset.UtcNow));
                }
            }
            else if (userRejectedActions.ContainsKey(serverFileManifest.RelativeFilePath))
            {
                ClientFileManifest oldEntry = currentClientManifest.Files.FirstOrDefault(x => x.RelativeFilePath == serverFileManifest.RelativeFilePath);

                if (oldEntry != null)
                    newClientManifest.AddOrUpdateFile(oldEntry);
            }
            else
            {
                newClientManifest.AddOrUpdateFile(new ClientFileManifest(
                    serverFileManifest.RelativeFilePath,
                    serverFileManifest.Hash,
                    serverFileManifest.SizeInBytes,
                    DateTimeOffset.UtcNow));
            }
        }

        await jsonFileSerializer.SerializeJsonFileAsync(Path.Combine(stagingDirectory, "clientExcludes.json.new"), clientConfiguration.ExcludePatterns, cancellationToken);
        await jsonFileSerializer.SerializeJsonFileAsync(Path.Combine(stagingDirectory, "clientManifest.json.new"), newClientManifest, cancellationToken);
    }

    private static void CleanUpStagingDirectory(string stagingPath)
    {
        if (Directory.Exists(stagingPath))
            Directory.Delete(stagingPath, true);

        Directory.CreateDirectory(stagingPath);
    }

    private async Task DownloadFileAsync(string stagingDirectory, string relativeFilePath,
        Progress<(float progress, string message)>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        TimeSpan defaultTimeout = RequestHandler.HttpClient.HttpClient.Timeout;
        RequestHandler.HttpClient.HttpClient.Timeout = TimeSpan.FromMinutes(15);

        try
        {
            string urlPath = $"{Constants.RoutePrefix}{Constants.RouteGetFile}/" + Uri.EscapeDataString(relativeFilePath);
            string destinationPath = Path.GetFullPath(Path.Combine(stagingDirectory, relativeFilePath));

            if (!destinationPath.StartsWith(stagingDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"Security Alert: Blocked attempt to write outside payload directory: {destinationPath}");

            string? directoryPath = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            await RequestHandler.HttpClient.DownloadWithCancellationAsync(urlPath, destinationPath, (downloadProgress) =>
            {

            }, cancellationToken);
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogError($"Failed to download '{relativeFilePath}': {ex.Message}");

            throw;
        }
        finally
        {
            RequestHandler.HttpClient.HttpClient.Timeout = defaultTimeout;
        }
    }

    private static void CreateDeleteInstruction(string stagingDirectory, string relativeFilePath)
    {
        string normalizedPath = relativeFilePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        string instructionPath = Path.Combine(stagingDirectory, normalizedPath + Constants.DeleteInstructionExtension);

        string? directory = Path.GetDirectoryName(instructionPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(instructionPath, string.Empty);
    }
}