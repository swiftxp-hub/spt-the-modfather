using System.Collections.Generic;
using BepInEx;
using SPT.Reflection.Patching;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.Common.Services;
using SwiftXP.SPT.TheModfather.Client.Configuration;
using SwiftXP.SPT.TheModfather.Client.Configurations;
using SwiftXP.SPT.TheModfather.Client.Helpers;
using SwiftXP.SPT.TheModfather.Client.Patches;
using SwiftXP.SPT.TheModfather.Client.Services;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Client;

[BepInPlugin("com.swiftxp.spt.themodfather", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.SPT.custom", "4.0.11")]
[BepInProcess("EscapeFromTarkov.exe")]
public class Plugin : BaseUnityPlugin
{
    private static ModulePatch? s_menuScreenShowPatch;

    public static void DisableMenuScreenShowPatch()
    {
        s_menuScreenShowPatch!.Disable();
    }

    private void Awake()
    {
        Instance = this;
        Configuration = new PluginConfiguration(Config);

        if (Configuration.EnablePlugin.GetValue() && !PluginInfoHelper.IsHeadlessInstalled())
        {
            s_menuScreenShowPatch = new MenuScreenShowPatch();
            s_menuScreenShowPatch.Enable();
        }

        SimpleSptLogger simpleSptLogger = new(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);
        ClientConfigurationLoader clientConfigurationLoader = new(simpleSptLogger);
        BaseDirectoryService baseDirectoryService = new();
        FileSearchService fileSearchService = new();
        FileHashingService fileHashingService = new();

        CheckUpdateService checkUpdateService = new(simpleSptLogger, baseDirectoryService, clientConfigurationLoader,
            fileSearchService, fileHashingService);

        DownloadUpdateService downloadUpdateService = new(simpleSptLogger, baseDirectoryService);

        ModSyncService = new ModSyncService(simpleSptLogger, baseDirectoryService, clientConfigurationLoader, checkUpdateService, downloadUpdateService);

        if (Configuration.EnablePlugin.GetValue())
            StartCoroutine(ModSyncService.SyncMods((result) =>
            {
                ModSyncActions = result;

                if (PluginInfoHelper.IsHeadlessInstalled() && ModSyncActions.Count > 0)
                    StartCoroutine(ModSyncService.UpdateModsCoroutine(ModSyncActions));
            }));
    }

    public static Plugin? Instance { get; private set; }

    public static PluginConfiguration? Configuration { get; set; }

    public static IModSyncService? ModSyncService { get; private set; }

    public static Dictionary<string, ModSyncAction>? ModSyncActions { get; set; }
}