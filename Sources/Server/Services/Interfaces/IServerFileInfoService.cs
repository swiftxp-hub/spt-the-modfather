using System.IO;

namespace SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

public interface IServerFileInfoService
{
    FileInfo? Get(string relativeFilePath);
}