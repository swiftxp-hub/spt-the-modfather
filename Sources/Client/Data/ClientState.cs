using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Client.Contexts;

public record ClientState(string BaseDirectory,
    ClientConfiguration ClientConfiguration,
    ClientManifest? ClientManifest,
    ServerManifest ServerManifest,
    FileHashBlacklist FileHashBlacklist);