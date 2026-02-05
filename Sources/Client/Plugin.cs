using BepInEx;
using HarmonyLib;
using SPT.Common.Utils;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Data.Loaders;
using SwiftXP.SPT.TheModfather.Client.Repositories;
using SwiftXP.SPT.TheModfather.Client.Services;
using SwiftXP.SPT.TheModfather.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Client;

[BepInPlugin("com.swiftxp.spt.themodfather", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.SPT.custom", "4.0.11")]
[BepInProcess("EscapeFromTarkov.exe")]
#pragma warning disable CA1001
public class Plugin : BaseUnityPlugin
#pragma warning restore CA1001
{
    // --- FIELDS ---
    private static object? s_tarkovAppInstance;

    // Data
    private IReadOnlyList<SyncAction> _syncActions = [];

    // Logic States
    private bool _isChecking;
    private bool _updateFound;
    private bool _isUpdating;
    private bool _isError;

    // Progress States
    private float _updateProgress;
    private string _updateDetailText = "";

    // Threading & Cancellation
    private bool _readyToStartGame;
    private CancellationTokenSource _cancellationTokenSource;

    private SimpleSptLogger _simpleSptLogger;
    private IClientExcludesRepository _clientExcludesRepository;
    private IClientManifestRepository _clientManifestRepository;
    private IPluginContext _plugInContext;

    private string _statusText = "Consulting the families...";

    // --- PROPERTIES ---
    public static bool IsConfirmed { get; private set; }

    // --- METHODS ---

#pragma warning disable CA1707
    public static bool StartPrefix(object __instance)
#pragma warning restore CA1707
    {
        if (IsConfirmed) return true;
        s_tarkovAppInstance = __instance;
        return false;
    }

    private async Task Awake()
    {
        _simpleSptLogger = new(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);
        _clientExcludesRepository = new ClientExcludesRepository(_simpleSptLogger, new JsonFileSerializer());
        _clientManifestRepository = new ClientManifestRepository(_simpleSptLogger, new JsonFileSerializer());

        InitializeHarmonyPatch();
        _isChecking = true;

        _ = Task.Run(async () => { await InitContext(); await PerformUpdateCheck(); });
    }

    private void InitializeHarmonyPatch()
    {
        Harmony harmony = new("com.swiftxp.spt.themodfather.patch");
        Type appType = AccessTools.TypeByName("EFT.TarkovApplication");
        MethodInfo startMethod = AccessTools.Method(appType, "Start");

        if (startMethod != null)
        {
            MethodInfo prefixMethod = typeof(Plugin).GetMethod(nameof(StartPrefix), BindingFlags.Public | BindingFlags.Static);
            HarmonyMethod harmonyPrefix = new(prefixMethod) { priority = Priority.High };
            harmony.Patch(startMethod, harmonyPrefix);
        }
        else
        {
            Logger.LogError("Critical: Could not find EFT.TarkovApplication.Start!");
        }
    }

    private async Task InitContext()
    {
        BaseDirectoryLocator baseDirectoryLocator = new();
        PluginContextFactory pluginContextFactory = new(baseDirectoryLocator, _clientExcludesRepository, _clientManifestRepository);

        _plugInContext = await pluginContextFactory.CreateAsync();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task PerformUpdateCheck()
    {
        try
        {
            // Initial delay kann auch gecancelt werden, wenn wir den Token übergeben
            await Task.Delay(1000, _cancellationTokenSource.Token);

            _statusText = "Consulting the families...";
            _isError = false;

            UpdateManager updateManager = new(_simpleSptLogger, new XxHash128FileHasher());

            Progress<(float progress, string message)> progressIndicator = new(progress =>
            {
                _updateProgress = progress.progress;
                _updateDetailText = progress.message;
            });

            _syncActions = await updateManager.GetSyncActionsAsync(_plugInContext,
                progressIndicator,
                _cancellationTokenSource.Token);

            _updateFound = _syncActions.Count != 0;

            _statusText = _updateFound
                ? "I have an update you can't refuse."
                : "The books are clean. Business as usual.";
        }
        catch (OperationCanceledException)
        {
            // Abfangen des Abbruchs während des Checks
            Logger.LogWarning("Check cancelled by user.");
            _statusText = "Check cancelled by Don.";
            _isError = true;
            _updateFound = false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Check failed: {ex}");
            _statusText = $"Check failed: {ex.Message}";
            _isError = true;
            _updateFound = false;
        }
        finally
        {
            _isChecking = false;
        }
    }

    private async Task RunUpdateProcess()
    {
        _isUpdating = true;
        _isError = false;
        _statusText = "Settling all family business...";
        _updateProgress = 0f;

        // Falls der Token vom Check verbraucht/cancelled ist, brauchen wir hier ggf. einen neuen,
        // oder wir re-initialisieren ihn vor jedem Start.
        // Sicherheitshalber erneuern wir ihn, falls er disposed wurde (siehe finally).
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        try
        {
            List<SyncAction> actionsToProcess = _syncActions.Where(a => a.IsSelected).ToList();
            if (actionsToProcess.Count == 0)
            {
                Logger.LogInfo("No actions selected.");
                _readyToStartGame = true;
                return;
            }

            // --- SIMULATION START ---
            _updateDetailText = "Preparing...";
            await Task.Delay(500, _cancellationTokenSource.Token);

            for (int i = 0; i <= 100; i++)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                _updateProgress = i / 100f;
                _updateDetailText = i < 50 ? $"Downloading files... {i}% (12.5 MB/s)" : $"Patching files... {i}%";

                await Task.Delay(50, _cancellationTokenSource.Token);
            }
            // --- SIMULATION ENDE ---

            _statusText = "Welcome to the family.";
            _updateDetailText = "Completed.";

            await Task.Delay(800, _cancellationTokenSource.Token);

            Logger.LogInfo("Update complete.");
            _readyToStartGame = true;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Update cancelled by user.");
            _statusText = "Update cancelled by Don.";
            _isError = true;
        }
        catch (Exception ex)
        {
            _statusText = $"Look how they massacred my code: {ex.Message}";
            Logger.LogError($"Update process failed: {ex}");
            _isError = true;
        }
        finally
        {
            // Token aufräumen
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }

    private void CancelUpdate()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    private void Update()
    {
        if (IsConfirmed || s_tarkovAppInstance == null) return;

        if (_readyToStartGame)
        {
            _readyToStartGame = false;
            ContinueGameLoad();
            return;
        }

        if (!_isChecking && !_updateFound && !_isUpdating && !_isError)
        {
            ContinueGameLoad();
        }
    }

    private void OnGUI()
    {
        if (s_tarkovAppInstance == null || IsConfirmed) return;
        if (!_isChecking && !_updateFound && !_isUpdating && !_isError) return;

        // Logik für UI-Zustände
        bool showListAndButtons = _updateFound && !_isChecking && !_isUpdating && !_isError;

        // WICHTIG: Wir zeigen Progress, wenn wir CHECKEN oder UPDATEN (sofern kein Fehler ist)
        bool showProgress = (_isChecking || _isUpdating) && !_isError;

        ModfatherUI.Draw(
            _statusText,
            showListAndButtons,
            showProgress,        // Vereinheitlichter Status
            _isError,
            _updateProgress,
            _updateDetailText,
            _syncActions,
            onAccept: () => Task.Run(RunUpdateProcess),
            onDecline: () =>
            {
                Logger.LogInfo("User declined update.");
                ContinueGameLoad();
            },
            onCancel: () => CancelUpdate(), // Funktioniert jetzt für Check UND Update
            onErrorContinue: () =>
            {
                Logger.LogInfo("User ignored error/cancel.");
                ContinueGameLoad();
            }
        );
    }

    private void ContinueGameLoad()
    {
        IsConfirmed = true;
        if (s_tarkovAppInstance != null)
        {
            try
            {
                Type appType = AccessTools.TypeByName("EFT.TarkovApplication");
                MethodInfo startMethod = AccessTools.Method(appType, "Start");
                startMethod.Invoke(s_tarkovAppInstance, null);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to resume game load: {ex}");
            }
        }
    }
}