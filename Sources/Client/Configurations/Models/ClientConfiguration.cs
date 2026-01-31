using System;
using SwiftXP.SPT.Common.Runtime;

namespace SwiftXP.SPT.TheModfather.Client.Configurations.Models;

public sealed record ClientConfiguration
{
    public string ConfigVersion { get; set; } = AppMetadata.Version;

    public int MaxDownloadRetries { get; set; } = 3;

    public int SecondsToWaitBetweenDownloadRetries { get; set; } = 15;

    private string[] _excludedPaths = [
        "**/*.log",
        "BepInEx/plugins/SAIN/**/*.json",
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt/**/*"
    ];

    public string[] ExcludedPaths
    {
        get => _excludedPaths;
        set => _excludedPaths = NormalizePaths(value);
    }

    public bool UseHeadlessWhitelist { get; set; } = true;

    private string[] _headlessWhitelist = [
        "SwiftXP.SPT.TheModfather.Updater.exe",
        "BepInEx/plugins/com.swiftxp.spt.themodfather/**/*",
        "BepInEx/plugins/acidphantasm-botplacementsystem/**/*",
        "BepInEx/plugins/DrakiaXYZ-Waypoints/**/*",
        "BepInEx/plugins/Fika/**/*",
        "BepInEx/plugins/MergeConsumables/**/*",
        "BepInEx/plugins/ozen-Foldables/**/*",
        "BepInEx/plugins/SAIN/**/*",
        "BepInEx/plugins/s8_SPT_PatchCRC32/**/*",
        "BepInEx/plugins/WTT-ClientCommonLib/**/*",
        "BepInEx/plugins/DrakiaXYZ-BigBrain.dll",
        "BepInEx/plugins/NerfBotGrenades.dll",
        "BepInEx/plugins/Tyfon.UIFixes.dll",
        "BepInEx/plugins/Tyfon.UIFixes.Net.dll",
    ];

    public string[] HeadlessWhitelist
    {
        get => _headlessWhitelist;
        set => _headlessWhitelist = NormalizePaths(value);
    }

    private static string[] NormalizePaths(string[] paths)
    {
        if (paths is null)
            return [];

        return Array.ConvertAll(paths, p =>
        {
            string path = p.Trim().Replace('\\', '/');

            return (path.StartsWith("./", StringComparison.OrdinalIgnoreCase) ? path.Substring(2) : path).Trim().Trim('/');
        });
    }
}