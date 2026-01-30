using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Server.Services.Interfaces;

public interface IServerFilesHashingService
{
    Task<Dictionary<string, string>> GetServerFileHashes();
}