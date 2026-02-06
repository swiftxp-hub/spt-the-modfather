using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public record SyncProposal(
    ClientManifest ClientManifest,
    IReadOnlyList<SyncAction> SyncActions,
    ServerManifest ServerManifest);