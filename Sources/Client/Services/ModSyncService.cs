using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Helpers;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.UI;
using TMPro;
using UnityEngine;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class ModSyncService(
    ISimpleSptLogger simpleSptLogger,
    IBaseDirectoryService baseDirectoryService,
    ICheckUpdateService checkUpdateService,
    IDownloadUpdateService downloadUpdateService) : IModSyncService
{
    private GClass3834? _messageWindow;

    private ModUpdaterUI? _modUpdaterWindow;

    public void ShowUpdateNotification(Dictionary<string, ModSyncAction> modSyncActions)
    {
        if (_messageWindow != null)
            CloseMessageWindow();

        try
        {
            ShowUpdateNotificationWindow(modSyncActions);
        }
        catch (Exception ex)
        {
            CloseMessageWindow();
            simpleSptLogger.LogException(ex);
        }
    }

    public IEnumerator SyncMods(Action<Dictionary<string, ModSyncAction>> onCompleted)
    {
        Task<Dictionary<string, ModSyncAction>> checkAsyncTask = checkUpdateService.CheckForUpdatesAsync();
        yield return new WaitUntil(() => checkAsyncTask.IsCompleted);

        if (checkAsyncTask.IsFaulted)
        {
            simpleSptLogger.LogError($"Update check failed: {checkAsyncTask.Exception?.InnerException?.Message}");

            onCompleted([]);
        }
        else
        {
            onCompleted(checkAsyncTask.Result);
        }
    }

    private void ShowUpdateNotificationWindow(Dictionary<string, ModSyncAction> modSyncActions)
    {
        int countAdd = modSyncActions.Count(x => x.Value == ModSyncAction.Add);
        int countUpd = modSyncActions.Count(x => x.Value == ModSyncAction.Update);
        int countDel = modSyncActions.Count(x => x.Value == ModSyncAction.Delete);

        string message = $"Mod updates available...\n\n" +
            $"... {countAdd} files added\n" +
            $"... {countUpd} files updated\n" +
            $"... {countDel} files removed\n\n" +
            "Do you want to update now?";

        Singleton<PreloaderUI>.Instance.StartCoroutine(ShowMessageWindow(message, modSyncActions));
    }

    private IEnumerator ShowMessageWindow(string message, Dictionary<string, ModSyncAction> modSyncActions)
    {
        yield return null;

        _messageWindow = ItemUiContext.Instance.ShowMessageWindow(
            message,
            () => OnContinue(modSyncActions),
            () => { _messageWindow = null; },
            "The Modfather found some updates...",
            0f,
            true,
            TextAlignmentOptions.Left
        );
    }

    private void OnContinue(Dictionary<string, ModSyncAction> modSyncActions)
    {
        GameObject gameObject = new("ModUpdaterUI");

        _modUpdaterWindow = gameObject.AddComponent<ModUpdaterUI>();
        _modUpdaterWindow.UpdateProgress(0.0f, $"Settling all family business (0/{modSyncActions.Count})...");

        Plugin.Instance!.StartCoroutine(UpdateModsCoroutine(modSyncActions));
    }

    private void CloseMessageWindow()
    {
        if (_messageWindow == null)
            return;

        _messageWindow.CloseSilent();
        _messageWindow = null;
    }

    public IEnumerator UpdateModsCoroutine(Dictionary<string, ModSyncAction> modSyncActions)
    {
        int actionCount = 0;
        int totalActions = modSyncActions.Count;
        string baseDir = baseDirectoryService.GetEftBaseDirectory();

        Task ensurePayloadTask = EnsurePayloadExistsAndIsEmpty(baseDir);
        yield return new WaitUntil(() => ensurePayloadTask.IsCompleted);

        bool success = true;
        foreach (KeyValuePair<string, ModSyncAction> modSyncAction in modSyncActions)
        {
            if (modSyncAction.Key.Contains("..") || Path.IsPathRooted(modSyncAction.Key))
            {
                simpleSptLogger.LogError($"Security Alert: Skipped action for invalid path: {modSyncAction.Key}");
                success = false;
                actionCount++;

                UpdateProgress(actionCount, totalActions);

                continue;
            }

            if (modSyncAction.Value == ModSyncAction.Add || modSyncAction.Value == ModSyncAction.Update)
            {
                Task downloadTask = downloadUpdateService.DownloadAsync(Constants.DataDirectoryName, Constants.PayloadDirectoryName, modSyncAction.Key, UpdateDownloadProgress);
                yield return new WaitUntil(() => downloadTask.IsCompleted);

                if (downloadTask.IsFaulted)
                {
                    simpleSptLogger.LogError($"Failed to download {modSyncAction.Key}: {downloadTask.Exception?.InnerException?.Message}");
                    success = false;

                    break;
                }

                try
                {
                    // Exception for "SwiftXP.SPT.TheModfather.Updater.exe" - self-update.
                    if (modSyncAction.Key.EndsWith(Constants.UpdaterExecutableName, StringComparison.OrdinalIgnoreCase))
                    {
                        string sourceFilePath = GetPayloadPath(baseDir, modSyncAction.Key);
                        string targetFilePath = Path.GetFullPath(Path.Combine(baseDir, Constants.UpdaterExecutableName));

                        if (File.Exists(targetFilePath))
                            File.Delete(targetFilePath);

                        File.Move(sourceFilePath, targetFilePath);
                    }
                }
                catch (Exception ex)
                {
                    simpleSptLogger.LogException(ex);
                    throw;
                }
            }
            else
            {
                if (modSyncAction.Key.EndsWith(Constants.UpdaterExecutableName, StringComparison.OrdinalIgnoreCase))
                {
                    simpleSptLogger.LogError($"Warning: Blocked deletion of updater executable: {modSyncAction.Key}");
                    continue;
                }

                try
                {
                    string absolutePath = Path.GetFullPath(Path.Combine(baseDir, modSyncAction.Key));
                    if (!absolutePath.StartsWith(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        simpleSptLogger.LogError($"Security: Blocked deletion of external path: {absolutePath}");
                    }
                    else if (File.Exists(absolutePath))
                    {
                        CreateDeleteInstruction(GetPayloadPath(baseDir), modSyncAction.Key);
                        UpdateFooter($"Created delete instruction for: {modSyncAction.Key}");
                    }
                }
                catch (Exception ex)
                {
                    simpleSptLogger.LogError($"Failed to delete {modSyncAction.Key}: {ex.Message}");
                    success = false;
                }
            }

            actionCount++;

            UpdateProgress(actionCount, totalActions);
        }

        if (success)
        {
            UpdateFooter("The game will close automatically. An external tool will finish the update.");

            yield return StartUpdaterAndQuit(baseDir);
        }
    }

    private async Task EnsurePayloadExistsAndIsEmpty(string baseDir)
    {
        string payloadPath = GetPayloadPath(baseDir);

        if (!Directory.Exists(payloadPath))
        {
            Directory.CreateDirectory(payloadPath);
            return;
        }

        IEnumerable<string> files = Directory.EnumerateFiles(payloadPath, "*", SearchOption.AllDirectories);
        if (files.Any())
        {
            simpleSptLogger.LogError($"Payload path is not empty. Failed update? Emptying payload directory...");

            foreach (string file in files)
            {
                File.Delete(file);
            }

            await Task.Delay(200);

            Directory.Delete(payloadPath);
            Directory.CreateDirectory(payloadPath);
        }
    }

    private IEnumerator StartUpdaterAndQuit(string baseDir)
    {
        for (int i = 3; i > 0; i--)
        {
            _modUpdaterWindow?.UpdateProgress(1f, $"Closing Escape From Tarkov in {i} second(s)...");
            yield return new WaitForSeconds(1f);
        }

        string updaterPath = Path.Combine(baseDir, Constants.UpdaterExecutableName);

        if (!File.Exists(updaterPath))
        {
            simpleSptLogger.LogError($"Updater executable not found at: {updaterPath}");

            yield break;
        }

        string[] startOptions =
        [
            PluginInfoHelper.IsFikaHeadlessInstalled() ? "--silent true" : string.Empty,
            $"--processid {Process.GetCurrentProcess().Id}"
        ];

        ProcessStartInfo updaterStartInfo = new()
        {
            FileName = updaterPath,
            Arguments = string.Join(" ", startOptions).Trim(),
            WorkingDirectory = baseDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            Process.Start(updaterStartInfo);
            Application.Quit();
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogException(ex);
        }
    }

    private static void CreateDeleteInstruction(string payloadDirectory, string relativeFilePath)
    {
        string normalizedPath = relativeFilePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        string instructionPath = Path.Combine(payloadDirectory, normalizedPath + Constants.DeleteExtension);

        string? directory = Path.GetDirectoryName(instructionPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(instructionPath, string.Empty);
    }

    private static string GetPayloadPath(string baseDir)
    {
        return Path.GetFullPath(Path.Combine(baseDir, Constants.DataDirectoryName, Constants.PayloadDirectoryName));
    }

    private static string GetPayloadPath(string baseDir, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(baseDir, Constants.DataDirectoryName, Constants.PayloadDirectoryName, relativePath));
    }

    private void UpdateDownloadProgress(DownloadProgress downloadProgress)
    {
        string downloadText = $"Download Speed: {downloadProgress.DownloadSpeed} | Progress: {downloadProgress.FileSizeInfo}";
        UpdateFooter(downloadText);
    }

    private void UpdateProgress(int current, int total)
    {
        if (_modUpdaterWindow == null)
            return;

        float progress = (float)current / total;
        _modUpdaterWindow.UpdateProgress(progress, $"Settling all family business ({current}/{total})...");
    }

    private void UpdateFooter(string text)
    {
        if (_modUpdaterWindow == null)
            return;

        _modUpdaterWindow.UpdateFooter(text);
    }
}