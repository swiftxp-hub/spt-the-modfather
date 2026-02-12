using System;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.Runtime;

namespace SwiftXP.SPT.TheModfather.Server.Data;

public record class ServerConfiguration
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
        "BepInEx/patchers/spt-prepatch.dll",
        "BepInEx/plugins/Fika/Fika.Headless.dll",
        "BepInEx/plugins/SAIN/**/*.json",
        "BepInEx/plugins/spt/**/*"
    ];

    public string[] ExcludePatterns
    {
        get => _excludePatterns;
        set => _excludePatterns = NormalizePaths(value);
    }

    public string[] FileHashBlacklist { get; set; } = [];

    private static string[] NormalizePaths(string[]? paths)
    {
        if (paths is null)
            return [];

        return Array.ConvertAll(paths, p =>
        {
            string path = p.GetWebFriendlyPath();

            return (path.StartsWith("./", StringComparison.OrdinalIgnoreCase) ? path[2..] : path).Trim().Trim('/');
        });
    }
}