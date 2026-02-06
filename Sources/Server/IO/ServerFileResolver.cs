using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using SPTarkov.DI.Annotations;
using SwiftXP.SPT.Common.Environment;

namespace SwiftXP.SPT.TheModfather.Server.IO;

[Injectable(InjectionType.Scoped)]
public class ServerFileResolver(IBaseDirectoryLocator baseDirectoryLocator) : IServerFileResolver
{
    public FileInfo? GetFileInfo(string relativeFilePath, string[] includePatterns, string[] excludePatterns)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return null;

        try
        {
            string baseDirectory = baseDirectoryLocator.GetBaseDirectory();
            string root = Path.GetFullPath(baseDirectory);

            if (!root.EndsWith(Path.DirectorySeparatorChar))
                root += Path.DirectorySeparatorChar;

            string requestedFullPath = Path.GetFullPath(Path.Combine(root, relativeFilePath));

            if (!requestedFullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;

            string normalizedRelativePath = Path.GetRelativePath(root, requestedFullPath);

            Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
            matcher.AddIncludePatterns(includePatterns);
            matcher.AddExcludePatterns(excludePatterns);

            if (!matcher.Match(normalizedRelativePath).HasMatches)
                return null;

            FileInfo fileInfo = new(requestedFullPath);

            return fileInfo.Exists ? fileInfo : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}