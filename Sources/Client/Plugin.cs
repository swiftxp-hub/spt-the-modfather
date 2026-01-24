using System.Collections.Generic;
using BepInEx;
using SPT.Reflection.Patching;
using SwiftXP.SPT.Common.Loggers;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations;
using SwiftXP.SPT.TheModfather.Client.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Patches;
using SwiftXP.SPT.TheModfather.Client.Services;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Client;

[BepInPlugin("com.swiftxp.spt.themodfather", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.SPT.custom", "4.0.11")]
[BepInProcess("EscapeFromTarkov.exe")]
public class Plugin : BaseUnityPlugin
{
    private static ModulePatch? MenuScreenShowPatch;

    public static void DisableMenuScreenShowPatch()
    {
        MenuScreenShowPatch!.Disable();
    }

    private void Awake()
    {
        Instance = this;

        MenuScreenShowPatch = new MenuScreenShowPatch();
        MenuScreenShowPatch.Enable();

        ISimpleSptLogger simpleSptLogger = new SimpleSptLogger(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_VERSION);
        IClientConfigurationLoader clientConfigurationLoader = new ClientConfigurationLoader(simpleSptLogger);
        IBaseDirectoryService baseDirectoryService = new BaseDirectoryService();
        IFileSearchService fileSearchService = new FileSearchService();
        IFileHashingService fileHashingService = new FileHashingService();

        ICheckUpdateService checkUpdateService = new CheckUpdateService(simpleSptLogger, baseDirectoryService, clientConfigurationLoader,
            fileSearchService, fileHashingService);

        IDownloadUpdateService downloadUpdateService = new DownloadUpdateService(simpleSptLogger, baseDirectoryService);

        ModSyncService = new ModSyncService(simpleSptLogger, baseDirectoryService, checkUpdateService, downloadUpdateService);
        
        StartCoroutine(ModSyncService.SyncMods((result) =>
        {
            ModSyncActions = result;
        }));
    }

    public static Plugin? Instance { get; private set; }
    
    public static IModSyncService? ModSyncService { get; private set; }

    public static Dictionary<string, ModSyncActionEnum>? ModSyncActions { get; private set; }
}