using System;
using SwiftXP.SPT.Common.Runtime;

namespace SwiftXP.SPT.TheModfather.Server.Data;

public sealed record ServerConfiguration
{
    public string ConfigVersion { get; set; } = AppMetadata.Version;

    private string[] _includePatterns = [
        "SwiftXP.SPT.TheModfather.Updater.exe",
        "BepInEx/patchers/**/*",
        "BepInEx/plugins/**/*"
    ];

    public string[] IncludePatterns
    {
        get => _includePatterns;
        set => _includePatterns = NormalizePaths(value);
    }

    private string[] _excludePatterns = [
        "**/*.log",
        "BepInEx/plugins/SAIN/**/*.json",
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/spt/**/*"
    ];

    public string[] ExcludePatterns
    {
        get => _excludePatterns;
        set => _excludePatterns = NormalizePaths(value);
    }

    private static string[] NormalizePaths(string[]? paths)
    {
        if (paths is null)
            return [];

        return Array.ConvertAll(paths, p =>
        {
            string path = p.Replace('\\', '/');

            return (path.StartsWith("./", StringComparison.OrdinalIgnoreCase) ? path[2..] : path).Trim().Trim('/');
        });
    }
}