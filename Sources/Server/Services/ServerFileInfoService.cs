using System;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using SPTarkov.DI.Annotations;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Models;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Server.Services;

[Injectable(InjectionType.Scoped)]
public class ServerFileInfoService(
    IServerConfigurationLoader serverConfigurationLoader,
    IBaseDirectoryService baseDirectoryService) : IServerFileInfoService
{
    public FileInfo? GetFileInfo(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return null;

        ServerConfiguration serverConfiguration = serverConfigurationLoader.LoadOrCreate();
        string baseDir = baseDirectoryService.GetEftBaseDirectory();

        Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
        matcher.AddIncludePatterns(serverConfiguration.SyncedPaths);

        bool isAccessAllowed = matcher.Match(relativeFilePath).HasMatches;
        if (!isAccessAllowed)
        {
            return null;
        }

        string requestedFullPath;
        try
        {
            requestedFullPath = Path.GetFullPath(Path.Combine(baseDir, relativeFilePath));
        }
        catch (Exception)
        {
            return null;
        }

        FileInfo fileInfo = new(requestedFullPath);

        return fileInfo.Exists ? fileInfo : null;
    }
}