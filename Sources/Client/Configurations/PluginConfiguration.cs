using BepInEx.Configuration;
using SwiftXP.SPT.Common.ConfigurationManager;
using SwiftXP.SPT.Common.Notifications;
using System;

namespace SwiftXP.SPT.TheModfather.Client.Configuration;

public class PluginConfiguration
{
    public PluginConfiguration(ConfigFile configFile)
    {
        // --- 1. Main settings
        EnablePlugin = configFile.BindConfiguration("1. Main settings", "Enable plug-in", true, $"Enable or disable the plug-in.{Environment.NewLine}{Environment.NewLine}(Default: Enabled)", 1);

        configFile.CreateButton(
            "1. Main settings",
            "Check for updates",
            "Check for updates",
            "Checks for updates and prompts you to install them.",
            () =>
            {
                // Plugin.Instance!.StartCoroutine(Plugin.ModSyncService!.SyncMods((result) =>
                // {
                //     Plugin.ModSyncActions = result;

                //     if (Plugin.ModSyncActions != null && Plugin.ModSyncActions.Count > 0)
                //         Plugin.ModSyncService!.ShowUpdateNotification(Plugin.ModSyncActions);
                //     else
                //         NotificationsService.SendNotice("The modfather found no updates. Happy playing!");
                // }));
            },
            0
        );

        configFile.SaveOnConfigSet = true;
    }

    public ConfigEntry<bool> EnablePlugin { get; set; }
}