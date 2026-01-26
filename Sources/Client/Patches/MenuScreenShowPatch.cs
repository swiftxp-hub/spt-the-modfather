using EFT.UI;
using SPT.Reflection.Patching;
using System.Reflection;
using HarmonyLib;

namespace SwiftXP.SPT.TheModfather.Client.Patches;

public class MenuScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.FirstMethod(typeof(MenuScreen), x => x.Name == nameof(MenuScreen.Show));

    [PatchPostfix]
    public static void PatchPostfix(MenuScreen instance)
    {
        if (Plugin.ModSyncActions != null && Plugin.ModSyncActions.Count > 0)
            Plugin.ModSyncService!.ShowUpdateNotification(Plugin.ModSyncActions);

        Plugin.DisableMenuScreenShowPatch();
    }
}