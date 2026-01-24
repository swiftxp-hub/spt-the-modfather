namespace SwiftXP.SPT.TheModfather.Client.Configurations.Models;

public sealed record ClientConfiguration
{
    public string ConfigVersion { get; set; } = "0.1";

    public string[] ExcludedPaths { get; set; } = [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];

    public string[] HeadlessWhitelist { get; set; } = [
        "BepInEx/plugins/Tyfon.UIFixes.dll",
        "BepInEx/plugins/Tyfon.UIFixes.Net.dll"
    ];
}