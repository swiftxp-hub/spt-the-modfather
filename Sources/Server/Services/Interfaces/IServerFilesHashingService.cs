using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

public interface IServerFilesHashingService
{
    Dictionary<string, string> Get();
}