using System.Collections.Generic;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface ICheckUpdateService
{
    Task<Dictionary<string, ModSyncAction>> CheckForUpdatesAsync();
}