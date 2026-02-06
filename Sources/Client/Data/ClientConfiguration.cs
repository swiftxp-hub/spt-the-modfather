using System;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.Runtime;

namespace SwiftXP.SPT.TheModfather.Server.Data;

public record class ClientConfiguration
{
    public string ConfigVersion { get; set; } = AppMetadata.Version;

    private string[] _excludePatterns = [];

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
            string path = p.GetWebFriendlyPath();

            return (path.StartsWith("./", StringComparison.OrdinalIgnoreCase) ? path[2..] : path).Trim().Trim('/');
        });
    }
}