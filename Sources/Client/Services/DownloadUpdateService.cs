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
    public async Task DownloadAsync(string dataDirectory, string payloadDirectory, string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
            return;

        if (relativeFilePath.Contains("..") || Path.IsPathRooted(relativeFilePath))
        {
            throw new ArgumentException($"Security Alert: Path traversal or absolute path detected: {relativeFilePath}");
        }

        TimeSpan defaultTimeout = RequestHandler.HttpClient.HttpClient.Timeout;

        byte[]? data;

        try 
        {
            string urlPath = $"{Constants.RoutePrefix}{Constants.RouteGetFile}/" + Uri.EscapeDataString(relativeFilePath.ToUnixStylePath());
            
            RequestHandler.HttpClient.HttpClient.Timeout = TimeSpan.FromMinutes(15);
            data = await RequestHandler.GetDataAsync(urlPath);
        }
        catch (Exception ex)
        {
            simpleSptLogger.LogError($"Failed to download '{relativeFilePath}': {ex.Message}");

            throw;
        }
        finally
        {
            RequestHandler.HttpClient.HttpClient.Timeout = defaultTimeout;
        }

        if (data == null || data.Length == 0)
        {
            throw new InvalidOperationException($"Downloaded file '{relativeFilePath}' is empty.");
        }

        string baseDir = baseDirectoryService.GetEftBaseDirectory();
        string payloadBaseDir = Path.GetFullPath(Path.Combine(baseDir, dataDirectory, payloadDirectory));
        string destinationPath = Path.GetFullPath(Path.Combine(payloadBaseDir, relativeFilePath));

        if (!destinationPath.StartsWith(payloadBaseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Security Alert: Blocked attempt to write outside payload directory: {destinationPath}");
        }
        
        string? directoryPath = Path.GetDirectoryName(destinationPath);
        
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllBytesAsync(destinationPath, data);
    }
}