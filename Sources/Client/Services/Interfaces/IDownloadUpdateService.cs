using System;
using System.Threading.Tasks;
using SPT.Common.Http;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface IDownloadUpdateService
{
    Task DownloadAsync(string dataDirectory, string payloadDirectory, string relativeFilePath, Action<DownloadProgress>? progressCallback = null);
}