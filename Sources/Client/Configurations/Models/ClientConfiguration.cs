namespace SwiftXP.SPT.TheModfather.Client.Configurations.Models;

public sealed record ClientConfiguration
{
    public string ConfigVersion { get; set; } = "0.1";

    private string[] _excludedPaths = [
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt"
    ];

    public string[] ExcludedPaths
    {
        get => _excludedPaths;
        set => _excludedPaths = NormalizePaths(value);
    }

    private string[] _headlessWhitelist = [
        "BepInEx/plugins/acidphantasm-botplacementsystem",
        "BepInEx/plugins/DrakiaXYZ-Waypoints",
        "BepInEx/plugins/SAIN",
        "BepInEx/plugins/DrakiaXYZ-BigBrain.dll",
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
        
        return System.Array.ConvertAll(paths, p => 
        {
            var path = p.Replace('\\', '/');
            return (path.StartsWith("./") ? path.Substring(2) : path).Trim('/');
        });
    }
}