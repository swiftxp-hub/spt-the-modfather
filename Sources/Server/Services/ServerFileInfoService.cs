using System;
using System.IO;
using System.Linq;
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
        string requestedFullPath;

        try
        {
            requestedFullPath = Path.GetFullPath(Path.Combine(baseDir, relativeFilePath));
        }
        catch (Exception)
        {
            return null;
        }

        bool isAccessAllowed = serverConfiguration.SyncedPaths.Any(allowedSubPath =>
        {
            string allowedAbsolutePath = Path.GetFullPath(Path.Combine(baseDir, allowedSubPath));

            return requestedFullPath.StartsWith(allowedAbsolutePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || requestedFullPath.Equals(allowedAbsolutePath, StringComparison.OrdinalIgnoreCase);
        });

        if (!isAccessAllowed)
        {
            return null;
        }

        FileInfo fileInfo = new(requestedFullPath);

        return fileInfo.Exists ? fileInfo : null;
    }
}