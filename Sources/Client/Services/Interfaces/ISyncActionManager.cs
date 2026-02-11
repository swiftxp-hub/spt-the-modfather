using System;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public interface ISyncActionManager
{
    Task ProcessSyncActionsAsync(ClientState clientState, SyncProposal syncProposal,
        IProgress<(float progress, string message, string detail)>? progressCallback = null, CancellationToken cancellationToken = default);
}