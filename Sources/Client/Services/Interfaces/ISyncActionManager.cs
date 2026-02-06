using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Client.Contexts;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Services;

public interface ISyncActionManager
{
    Task ProcessSyncActionsAsync(ClientState clientState, IReadOnlyList<SyncAction> syncActions,
        Progress<(float progress, string message)>? progressCallback = null, CancellationToken cancellationToken = default);
}