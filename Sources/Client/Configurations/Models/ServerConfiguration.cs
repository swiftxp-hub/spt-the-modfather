namespace SwiftXP.SPT.TheModfather.Server.Configurations.Models;

public sealed record ServerConfiguration
{
    public string ConfigVersion { get; set; } = "0.1";

    public string[] SyncedPaths { get; set; } = [
        "BepInEx/patchers",
        "BepInEx/plugins"
    ];

    public string[] ExcludedPaths { get; set; } = [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];
}