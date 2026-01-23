using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface IDownloadUpdateService
{
    Task DownloadAsync(string relativeFilePath);
}