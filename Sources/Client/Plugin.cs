using BepInEx;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.NETStd.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Client.Patches;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using SwiftXP.SPT.TheModfather.Client.Services;
using SwiftXP.SPT.TheModfather.Client.UI;
using SwiftXP.SPT.TheModfather.Server.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SwiftXP.SPT.TheModfather.Client;

[BepInPlugin("com.swiftxp.spt.themodfather", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.SPT.custom", "4.0.12")]
[BepInProcess("EscapeFromTarkov.exe")]
#pragma warning disable CA1001
public class Plugin : BaseUnityPlugin
#pragma warning restore CA1001
{
    private volatile bool _shouldStartGameOnNextFrame;
    private PluginState _currentState = PluginState.Initializing;
    private readonly UpdateUiState _uiState = new();
    private CancellationTokenSource? _cancellationTokenSource;

    private ClientState? _clientState;
    private SyncProposal? _syncProposal;

    private SimpleSptLogger? _logger;
    private ClientConfigurationRepository? _configRepo;
    private ClientManifestRepository? _manifestRepo;
    private UpdateManager? _updateManager;
    private SyncActionManager? _syncActionManager;

    private void Awake()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        _currentState = PluginState.Initializing;
        UpdateUiStatus("Initializing Modfather...", 0f);

        GameStartPatch.Initialize(Logger);

        _ = Task.Run(async () =>
            {
                await InitializeAsync(_cancellationTokenSource.Token);
                await CheckForUpdatesAsync(_cancellationTokenSource.Token);
            },
            _cancellationTokenSource.Token);
    }

    private void Update()
    {
        if (_shouldStartGameOnNextFrame)
        {
            _shouldStartGameOnNextFrame = false;

            ResumeGameLoading();
        }
    }

    private void OnGUI()
    {
        if (GameStartPatch.IsConfirmed || _currentState == PluginState.ReadyToGame)
            return;

        ModfatherUI.Draw(
            _currentState,
            _uiState,
            onAccept: () => Task.Run(InstallUpdatesAsync),
            onDecline: () => _shouldStartGameOnNextFrame = true,
            onCancel: () => _cancellationTokenSource?.Cancel(),
            onErrorContinue: () => _shouldStartGameOnNextFrame = true
        );
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Dispose();
    }

    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger = new(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);

            BaseDirectoryLocator baseDirLocator = new();
            JsonFileSerializer jsonSerializer = new();
            ServerManifestRepository serverManifestRepo = new();
            FileHashBlacklistRepository blacklistRepo = new();

            _configRepo = new(_logger, baseDirLocator, jsonSerializer);
            _manifestRepo = new(_logger, baseDirLocator, jsonSerializer);

            _clientState = new(
                baseDirLocator.GetBaseDirectory(),
                await _configRepo.LoadOrCreateDefaultAsync(cancellationToken),
                await _manifestRepo.LoadAsync(cancellationToken),
                await serverManifestRepo.LoadAsync(cancellationToken),
                await blacklistRepo.LoadAsync(cancellationToken)
            );

            _updateManager = new UpdateManager(_logger, new XxHash128FileHasher());
            _syncActionManager = new SyncActionManager(_logger, _manifestRepo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            HandleError($"Initialization failed: {ex.Message}");
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken token)
    {
        _currentState = PluginState.CheckingForUpdates;

        try
        {
            UpdateUiStatus("Consulting the families...", 0);

            Progress<(float val, string msg)> progressReporter = new(p => UpdateUiStatus(p.msg, p.val));

            _syncProposal = await _updateManager!.GetSyncActionsAsync(_clientState!, progressReporter, token);

            if (_syncProposal.SyncActions.Any())
            {
                _uiState.SyncActions = _syncProposal.SyncActions;
                _currentState = PluginState.UpdateAvailable;

                UpdateUiStatus("I have an offer you can't refuse.");
            }
            else
            {
                _currentState = PluginState.NoUpdatesFound;

                UpdateUiStatus("The books are clean. Business as usual.");

                await Task.Delay(2000, token);

                _shouldStartGameOnNextFrame = true;
            }
        }
        catch (OperationCanceledException)
        {
            HandleError("Check cancelled by user.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            HandleError($"Check failed: {ex.Message}");
        }
    }

    private void UpdateUiStatus(string statusText, float? progress = null, string? detail = null)
    {
        _uiState.StatusText = statusText;

        if (progress.HasValue)
            _uiState.Progress = progress.Value;

        if (detail != null)
            _uiState.ProgressDetail = detail;
    }

    private void HandleError(string message)
    {
        _currentState = PluginState.Error;
        _uiState.IsError = true;
        _uiState.StatusText = message;
    }

    private void ResumeGameLoading()
    {
        _currentState = PluginState.ReadyToGame;

        GameStartPatch.ResumeGame();
    }

    private async Task InstallUpdatesAsync()
    {
        _currentState = PluginState.Updating;

        UpdateUiStatus("Settling all family business...");

        if (_cancellationTokenSource?.IsCancellationRequested == true)
            _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await HandleUserExclusionsAsync(_cancellationTokenSource!.Token);

            if (_syncProposal!.SyncActions.Count == 0 || !_syncProposal!.SyncActions.Any(x => x.IsSelected))
            {
                _shouldStartGameOnNextFrame = true;

                return;
            }

            Progress<(float progress, string message)> progressReporter = new(p => UpdateUiStatus("Settling all family business...", p.progress, p.message));

            await _syncActionManager!.ProcessSyncActionsAsync(
                _clientState!,
                _syncProposal!,
                progressReporter,
                _cancellationTokenSource!.Token);

            _currentState = PluginState.UpdateComplete;

            UpdateUiStatus("Welcome to the family.", 1f, "Closing Escape From Tarkov... the update will be finished by an external process.");

            await Task.Delay(2000);

            LaunchExternalUpdaterAndQuit();
        }
        catch (OperationCanceledException)
        {
            HandleError("Update cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            HandleError($"Update failed: {ex.Message}");
        }
    }

    private async Task HandleUserExclusionsAsync(CancellationToken token)
    {
        if (!_syncProposal!.SyncActions.Any(x => !x.IsSelected))
            return;

        List<string> newExcludePatterns = _clientState!.ClientConfiguration.ExcludePatterns.ToList();
        IEnumerable<SyncAction> rejectedActions = _syncProposal.SyncActions.Where(x => !x.IsSelected);

        foreach (SyncAction action in rejectedActions)
        {
            newExcludePatterns.Add(action.RelativeFilePath);
        }

        ClientConfiguration configToSave = new()

        {
            ConfigVersion = _clientState.ClientConfiguration.ConfigVersion,
            ExcludePatterns = [.. newExcludePatterns]
        };

        bool anyActionSelected = _syncProposal.SyncActions.Any(x => x.IsSelected);

        if (!anyActionSelected)
            await _configRepo!.SaveAsync(configToSave, token);
        else
            await _configRepo!.SaveToStagingAsync(configToSave, token);
    }

    private void LaunchExternalUpdaterAndQuit()
    {
        string updaterPath = Path.Combine(_clientState!.BaseDirectory, Constants.UpdaterExecutable);

        if (!File.Exists(updaterPath))
        {
            _logger!.LogError($"Updater executable not found at: {updaterPath}");

            HandleError("Updater executable not found.");

            return;
        }

        List<string> args = [$"--{Constants.ProcessIdParameter} {Process.GetCurrentProcess().Id}"];

        if (EFTGameExtensions.IsFikaHeadlessInstalled())
            args.Add($"--{Constants.SilentParameter} true");

        ProcessStartInfo startInfo = new()
        {
            FileName = updaterPath,
            Arguments = string.Join(" ", args),
            WorkingDirectory = _clientState!.BaseDirectory,
            UseShellExecute = true,
            CreateNoWindow = false
        };

        try
        {
            Process.Start(startInfo);
            Application.Quit();
        }
        catch (Exception ex)
        {
            _logger!.LogException(ex);
        }
    }
}