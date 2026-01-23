using System;
using System.IO;
using System.Linq;
using SPTarkov.DI.Annotations;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Server.Services;

[Injectable(InjectionType.Scoped)]
public class ServerFileInfoService(IBaseDirectoryService baseDirectoryService) : IServerFileInfoService
{
    private static readonly string[] AllowedPaths =
    [
        "BepInEx/patchers",
        "BepInEx/plugins"
    ];

    public FileInfo? Get(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return null;

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

        bool isAccessAllowed = AllowedPaths.Any(allowedSubPath => 
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