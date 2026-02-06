using System;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public interface IUpdateManager
{
    Task<SyncProposal> GetSyncActionsAsync(
        ClientState clientState,
        IProgress<(float progress, string message)>? progressCallback,
        CancellationToken cancellationToken);
}