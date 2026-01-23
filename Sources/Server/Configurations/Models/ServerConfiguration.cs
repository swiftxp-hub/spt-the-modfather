namespace SwiftXP.SPT.TheModfather.Server.Configurations.Models;

public sealed record ServerConfiguration
{
    public string ConfigVersion { get; init; } = "0.1";

    public string[] SyncedPaths { get; init; } = [
        "BepInEx/patchers",
        "BepInEx/plugins"
    ];

    public string[] ExcludedPaths { get; init; } = [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];
}