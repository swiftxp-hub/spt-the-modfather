using System.IO;

namespace SwiftXP.SPT.TheModfather.Server.IO;

public interface IServerFileResolver
{
    FileInfo? GetFileInfo(string relativeFilePath, string[] includePatterns, string[] excludePatterns);
}