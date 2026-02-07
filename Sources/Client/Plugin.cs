using BepInEx;
using SwiftXP.SPT.Common.EFT;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using SwiftXP.SPT.TheModfather.Client.Patches;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using SwiftXP.SPT.TheModfather.Client.Services;
using SwiftXP.SPT.TheModfather.Client.UI;
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
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public class Plugin : BaseUnityPlugin
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private PluginState _currentState = PluginState.Initializing;
    private readonly UpdateUiState _uiState = new();

    private SimpleSptLogger? _simpleSptLogger;
    private ClientConfigurationRepository? _clientConfigurationRepository;
    private ClientManifestRepository? _clientManifestRepository;
    private UpdateManager? _updateManager;
    private SyncActionManager? _syncActionManager;

    private CancellationTokenSource? _cancellationTokenSource;

    private ClientState? _clientState;
    private SyncProposal? _syncProposal;


    private async Task Awake()
    {
        _simpleSptLogger = new(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);

        BaseDirectoryLocator baseDirectoryLocator = new();
        JsonFileSerializer jsonFileSerializer = new();

        _clientConfigurationRepository = new(_simpleSptLogger, baseDirectoryLocator, jsonFileSerializer);
        _clientManifestRepository = new(_simpleSptLogger, baseDirectoryLocator, jsonFileSerializer);
        ServerManifestRepository serverManifestRepository = new();
        FileHashBlacklistRepository fileHashBlacklistRepository = new();

        GameStartPatch.Initialize(Logger);

        _clientState = new(
            baseDirectoryLocator.GetBaseDirectory(),
            await _clientConfigurationRepository.LoadOrCreateDefaultAsync(),
            await _clientManifestRepository.LoadAsync(),
            await serverManifestRepository.LoadAsync(),
            await fileHashBlacklistRepository.LoadAsync()
        );

        _updateManager = new UpdateManager(_simpleSptLogger, new XxHash128FileHasher());
        _syncActionManager = new SyncActionManager(_simpleSptLogger, _clientManifestRepository);

        _currentState = PluginState.CheckingForUpdates;
        _cancellationTokenSource = new CancellationTokenSource();

        _ = Task.Run(() => PerformUpdateCheck(_cancellationTokenSource.Token));
    }

    private async Task PerformUpdateCheck(CancellationToken cancellationToken)
    {
        try
        {
            _uiState.StatusText = "Consulting the families...";

            Progress<(float val, string msg)> progress = new(progress =>
            {
                _uiState.Progress = progress.val;
                _uiState.ProgressDetail = progress.msg;
            });

            _syncProposal = await _updateManager!.GetSyncActionsAsync(_clientState!, progress, cancellationToken);

            if (_syncProposal.SyncActions.Any())
            {
                _uiState.SyncActions = _syncProposal.SyncActions;
                _currentState = PluginState.UpdateAvailable;
                _uiState.StatusText = "I have an offer you can't refuse.";
            }
            else
            {
                _currentState = PluginState.NoUpdatesFound;
                _uiState.StatusText = "The books are clean. Business as usual.";

                await Task.Delay(500, cancellationToken); // Kurze Pause für Lesbarkeit
                StartGame();
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

    private async Task RunUpdateProcess()
    {
        _currentState = PluginState.Updating;
        _uiState.StatusText = "Settling all family business...";

        if (_cancellationTokenSource?.IsCancellationRequested == true)
            _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            if (_syncProposal!.SyncActions.Any(x => !x.IsSelected))
            {
                List<string> newExcludePatterns = [.. _clientState!.ClientConfiguration.ExcludePatterns];

                IEnumerable<SyncAction> rejectedActions = _syncProposal!.SyncActions.Where(x => !x.IsSelected);
                foreach (SyncAction rejectedAction in rejectedActions)
                {
                    newExcludePatterns.Add(rejectedAction.RelativeFilePath);
                }

                if (!_syncProposal!.SyncActions.Any(x => x.IsSelected))
                {
                    await _clientConfigurationRepository!.SaveAsync(new()
                    {
                        ConfigVersion = _clientState!.ClientConfiguration.ConfigVersion,
                        ExcludePatterns = [.. newExcludePatterns]
                    }, _cancellationTokenSource!.Token);
                }
                else
                {
                    await _clientConfigurationRepository!.SaveToStagingAsync(new()
                    {
                        ConfigVersion = _clientState!.ClientConfiguration.ConfigVersion,
                        ExcludePatterns = [.. newExcludePatterns]
                    }, _cancellationTokenSource!.Token);
                }
            }

            if (_syncProposal!.SyncActions.Count == 0 || !_syncProposal!.SyncActions.Any(x => x.IsSelected == true))
            {
                StartGame();

                return;
            }

            await _syncActionManager!.ProcessSyncActionsAsync(_clientState!, _syncProposal!,
                new Progress<(float progress, string message)>(progress =>
                {
                    _uiState.Progress = progress.progress;
                    _uiState.ProgressDetail = progress.message;
                }),
                _cancellationTokenSource!.Token);

            _currentState = PluginState.UpdateComplete;
            _uiState.StatusText = "Welcome to the family.";

            await Task.Delay(1000);

            StartUpdaterAndQuit();
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

    private void StartGame()
    {
        _currentState = PluginState.ReadyToGame;

        GameStartPatch.ResumeGame();
    }

    private void StartUpdaterAndQuit()
    {
        string updaterPath = Path.Combine(_clientState!.BaseDirectory, Constants.UpdaterExecutable);

        if (!File.Exists(updaterPath))
        {
            _simpleSptLogger!.LogError($"Updater executable not found at: {updaterPath}");

            HandleError("Updater executable not found.");

            return;
        }

        string[] startOptions =
        [
            EFTGameExtensions.IsFikaHeadlessInstalled() ? "--silent true" : string.Empty,
                $"--processid {Process.GetCurrentProcess().Id}"
        ];

        ProcessStartInfo updaterStartInfo = new()
        {
            FileName = updaterPath,
            Arguments = string.Join(" ", startOptions).Trim(),
            WorkingDirectory = _clientState!.BaseDirectory,
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
            _simpleSptLogger!.LogException(ex);
        }
    }

    private void HandleError(string message)
    {
        _currentState = PluginState.Error;
        _uiState.IsError = true;
        _uiState.StatusText = message;
    }

    private void OnGUI()
    {
        // Wenn das Spiel läuft oder wir fertig sind, keine GUI mehr
        if (GameStartPatch.IsConfirmed || _currentState == PluginState.ReadyToGame) return;

        ModfatherUI.Draw(
            _currentState,
            _uiState,
            onAccept: () => Task.Run(RunUpdateProcess),
            onDecline: () => StartGame(),
            onCancel: () => _cancellationTokenSource?.Cancel(),
            onErrorContinue: () => StartGame()
        );
    }

    private void OnDestroy()
    {
        _cancellationTokenSource?.Dispose();
    }
}