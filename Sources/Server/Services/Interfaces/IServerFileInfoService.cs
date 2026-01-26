using System.IO;

namespace SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

public interface IServerFileInfoService
{
    FileInfo? GetFileInfo(string relativeFilePath);
}