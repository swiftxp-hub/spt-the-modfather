using BepInEx.Bootstrap;

namespace SwiftXP.SPT.TheModfather.Client.Helpers;

public static class PluginInfoHelper
{
    public static bool IsFikaHeadlessInstalled()
    {
        return Chainloader.PluginInfos.ContainsKey(Constants.FikaHeadlessModGuid);
    }
}