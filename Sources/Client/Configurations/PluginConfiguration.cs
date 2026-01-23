using BepInEx.Configuration;
using SwiftXP.SPT.Common.ConfigurationManager;

namespace SwiftXP.SPT.TheModfather.Configurations;

public class PluginConfiguration
{
    public PluginConfiguration(ConfigFile configFile)
    {
        // --- 1. Main settings
        _excludedPaths = configFile.BindConfiguration("1. Main settings", "Excluded Paths", "BepInEx/patchers/spt-prepatch.dll,BepInEx/plugins/spt", $"", 1);

        configFile.SaveOnConfigSet = true;
    }

    public string[] GetExcludedPaths()
    {
        return _excludedPaths.GetValue()
            .Split(',');
    }

    private ConfigEntry<string> _excludedPaths;
}