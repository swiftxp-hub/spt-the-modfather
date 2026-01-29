using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using SwiftXP.SPT.Common.Notifications;

namespace SwiftXP.SPT.TheModfather.Client.Patches;

public class MenuScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(MenuScreen), x => x.Name == nameof(MenuScreen.Show));

    [PatchPostfix]
#pragma warning disable CA1707 // Identifiers should not contain underscores
    public static void PatchPostfix(MenuScreen __instance)
#pragma warning restore CA1707 // Identifiers should not contain underscores
    {
        if (Plugin.ModSyncActions != null && Plugin.ModSyncActions.Count > 0)
            Plugin.ModSyncService!.ShowUpdateNotification(Plugin.ModSyncActions);
        else
            NotificationsService.SendNotice("The modfather found no updates. Happy playing!");

        Plugin.DisableMenuScreenShowPatch();
    }
}