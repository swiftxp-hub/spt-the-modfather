namespace SwiftXP.SPT.TheModfather.Client.Configurations.Models;

public sealed record ServerConfiguration
{
    public string ConfigVersion { get; set; } = "0.1";

    private string[] _syncedPaths = [
        "SwiftXP.SPT.TheModfather.Updater.exe",
        "BepInEx/patchers",
        "BepInEx/plugins"
    ];

    public string[] SyncedPaths
    {
        get => _syncedPaths;
        set => _syncedPaths = NormalizePaths(value);
    }

    private string[] _excludedPaths = [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];

    public string[] ExcludedPaths
    {
        get => _excludedPaths;
        set => _excludedPaths = NormalizePaths(value);
    }

    private static string[] NormalizePaths(string[] paths)
    {
        if (paths is null) 
            return [];

        return System.Array.ConvertAll(paths, p => 
        {
            var path = p.Replace('\\', '/');
            return (path.StartsWith("./") ? path.Substring(2) : path).Trim('/');
        });
    }
}