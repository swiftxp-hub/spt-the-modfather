using System;
using System.IO;
using System.Threading.Tasks;
using SPT.Common.Http;
using SwiftXP.SPT.Common.Extensions;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Services.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public class DownloadUpdateService(ISimpleSptLogger simpleSptLogger, IBaseDirectoryService baseDirectoryService) : IDownloadUpdateService
{
    private const string RemotePathToGetFile = "/theModfather/getFile/";
    
    public async Task DownloadAsync(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return;

        byte[]? data;

        try 
        {
            string urlPath = RemotePathToGetFile + Uri.EscapeDataString(relativeFilePath.ToUnixStylePath());
            data = await RequestHandler.GetDataAsync(urlPath);
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogError($"[The Modfather] Failed to download '{relativeFilePath}': {ex.Message}");

            throw;
        }

        if (data == null || data.Length == 0)
        {
            throw new InvalidOperationException($"[The Modfather] Downloaded file '{relativeFilePath}' is empty.");
        }

        string baseDir = baseDirectoryService.GetEftBaseDirectory();
        string payloadBaseDir = Path.GetFullPath(Path.Combine(baseDir, "TheModfather_Data", "Payload"));
        string destinationPath = Path.GetFullPath(Path.Combine(payloadBaseDir, relativeFilePath));

        if (!destinationPath.StartsWith(payloadBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"[The Modfather] Security Alert: Blocked attempt to write outside payload directory: {destinationPath}");
        }
        
        string? directoryPath = Path.GetDirectoryName(destinationPath);
        
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllBytesAsync(destinationPath, data);
    }
}