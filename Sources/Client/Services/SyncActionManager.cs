// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.FileSystemGlobbing;
// using Newtonsoft.Json;
// using SPT.Common.Http;
// using SwiftXP.SPT.Common.Loggers.Interfaces;
// using SwiftXP.SPT.Common.Services;
// using SwiftXP.SPT.TheModfather.Client.Data;
// using SwiftXP.SPT.TheModfather.Client.Data.Loaders;
// using SwiftXP.SPT.TheModfather.Client.Enums;

// namespace SwiftXP.SPT.TheModfather.Client.Services;

// public class SyncActionManager(ISimpleSptLogger simpleSptLogger,
//     BaseDirectoryUtility baseDirectoryService,
//     ClientExcludesLoader clientExcludesLoader,
//     ClientManifestLoader clientManifestLoader)
// {
//     public async Task ProcessSyncActionsAsync(List<SyncAction> syncActions, CancellationToken cancellationToken)
//     {
//         string baseDirectory = baseDirectoryService.GetEftBaseDirectory();
//         string stagingPath = Path.GetFullPath(Path.Combine(baseDirectory, Constants.StagingPath));

//         if (!stagingPath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
//             throw new InvalidOperationException("Invalid staging path");

//         ClientExcludes clientExcludes = clientExcludesLoader.LoadOrCreate();
//         ClientManifest? currentClientManifest = clientManifestLoader.Load()
//             ?? new ClientManifest(DateTimeOffset.MinValue, RequestHandler.Host);

//         ClientManifest newClientManifest = new(DateTimeOffset.UtcNow, RequestHandler.Host);
//         ServerManifest serverManifest = await GetServerManifest();

//         float updateProgress = 0f;

//         int actionsExecuted = 0;
//         int totalActions = 1 + syncActions.Count;

//         CleanUpStagingDirectory(stagingPath);
//         updateProgress = ++actionsExecuted / totalActions;

//         foreach (SyncAction syncAction in syncActions)
//         {
//             cancellationToken.ThrowIfCancellationRequested();

//             if (!syncAction.IsSelected)
//                 continue;

//             switch (syncAction.Type)
//             {
//                 case SyncActionType.Add:
//                 case SyncActionType.Update:
//                     await DownloadFileAsync();

//                     break;

//                 case SyncActionType.Delete:
//                     CreateDeleteInstruction(GetPayloadPath(baseDir), modSyncAction.Key);

//                     break;
//             }
//         }

//         Dictionary<string, SyncAction> userRejectedActions = syncActions.Where(a => !a.IsSelected).ToDictionary(a => a.RelativeFilePath);
//         Dictionary<string, SyncAction> acceptedActions = syncActions.Where(a => a.IsSelected).ToDictionary(a => a.RelativeFilePath);

//         foreach (ServerFileManifest serverFileManifest in serverManifest.Files)
//         {
//             if (IsExcluded(serverFileManifest.RelativeFilePath, clientExcludes))
//                 continue;

//             if (acceptedActions.TryGetValue(serverFileManifest.RelativeFilePath, out SyncAction? syncAction))
//             {
//                 if (syncAction.Type == SyncActionType.Add ||
//                    syncAction.Type == SyncActionType.Update ||
//                    syncAction.Type == SyncActionType.Adopt)
//                 {
//                     newClientManifest.AddOrUpdateFile(new ClientFileManifest(
//                         serverFileManifest.RelativeFilePath,
//                         serverFileManifest.Hash,
//                         serverFileManifest.SizeInBytes,
//                         DateTimeOffset.UtcNow));
//                 }
//             }
//             else if (userRejectedActions.ContainsKey(serverFileManifest.RelativeFilePath))
//             {
//                 ClientFileManifest oldEntry = currentClientManifest.Files.FirstOrDefault(x => x.RelativeFilePath == serverFileManifest.RelativeFilePath);
//                 if (oldEntry != null)
//                     newClientManifest.AddOrUpdateFile(oldEntry);
//             }
//             else
//             {
//                 newClientManifest.AddOrUpdateFile(new ClientFileManifest(
//                     serverFileManifest.RelativeFilePath,
//                     serverFileManifest.Hash,
//                     serverFileManifest.SizeInBytes,
//                     DateTimeOffset.UtcNow));
//             }
//         }

//         string json = JsonConvert.SerializeObject(newClientManifest, Formatting.Indented);
//         await File.WriteAllTextAsync(Path.Combine(stagingPath, "clientManifest.json.new"), json, cancellationToken);
//     }

//     private static void CleanUpStagingDirectory(string stagingPath)
//     {
//         if (Directory.Exists(stagingPath))
//             Directory.Delete(stagingPath, true);

//         Directory.CreateDirectory(stagingPath);
//     }

//     private async Task<ServerManifest> GetServerManifest()
//     {
//         string json = await RequestHandler.GetJsonAsync($"{Constants.RoutePrefix}{Constants.RouteGetServerManifest}");

//         simpleSptLogger.LogInfo($"Server-Manifest: {json}");

//         if (string.IsNullOrWhiteSpace(json))
//             throw new InvalidOperationException("Empty JSON-response");

//         ServerManifest? serverManifest = JsonConvert.DeserializeObject<ServerManifest>(json)
//             ?? throw new InvalidOperationException("JSON-response could not be deserialized");

//         return serverManifest;
//     }

//     private async Task DownloadFileAsync(string dataDirectory, string payloadDirectory, string relativeFilePath, Action<DownloadProgress>? progressCallback = null)
//     {
//         TimeSpan defaultTimeout = RequestHandler.HttpClient.HttpClient.Timeout;
//         RequestHandler.HttpClient.HttpClient.Timeout = TimeSpan.FromMinutes(15);

//         try
//         {
//             string urlPath = $"{Constants.RoutePrefix}{Constants.RouteGetFile}/" + Uri.EscapeDataString(relativeFilePath);

//             string baseDir = baseDirectoryService.GetEftBaseDirectory();
//             string payloadBaseDir = Path.GetFullPath(Path.Combine(baseDir, dataDirectory, payloadDirectory));
//             string destinationPath = Path.GetFullPath(Path.Combine(payloadBaseDir, relativeFilePath));

//             if (!destinationPath.StartsWith(payloadBaseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
//             {
//                 throw new UnauthorizedAccessException($"Security Alert: Blocked attempt to write outside payload directory: {destinationPath}");
//             }

//             string? directoryPath = Path.GetDirectoryName(destinationPath);
//             if (!string.IsNullOrEmpty(directoryPath))
//             {
//                 Directory.CreateDirectory(directoryPath);
//             }

//             await RequestHandler.HttpClient.DownloadAsync(urlPath, destinationPath, progressCallback);
//         }
//         catch (Exception ex)
//         {
//             simpleSptLogger.LogError($"Failed to download '{relativeFilePath}': {ex.Message}");

//             throw;
//         }
//         finally
//         {
//             RequestHandler.HttpClient.HttpClient.Timeout = defaultTimeout;
//         }
//     }

//     private static void CreateDeleteInstruction(string payloadDirectory, string relativeFilePath)
//     {
//         string normalizedPath = relativeFilePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
//         string instructionPath = Path.Combine(payloadDirectory, normalizedPath + Constants.DeleteInstructionExtension);

//         string? directory = Path.GetDirectoryName(instructionPath);
//         if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//         {
//             Directory.CreateDirectory(directory);
//         }

//         File.WriteAllText(instructionPath, string.Empty);
//     }

//     private static bool IsExcluded(string relativePath, ClientExcludes clientExcludes)
//     {
//         Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
//         matcher.AddIncludePatterns(clientExcludes);

//         return matcher.Match(relativePath).HasMatches;
//     }
// }