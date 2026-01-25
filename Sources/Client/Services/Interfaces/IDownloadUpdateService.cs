using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface IDownloadUpdateService
{
    Task DownloadAsync(string dataDirectory, string payloadDirectory, string relativeFilePath);
}