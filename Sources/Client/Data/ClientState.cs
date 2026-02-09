using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Contexts;

public record ClientState(string BaseDirectory,
    ClientConfiguration ClientConfiguration,
    ClientManifest? ClientManifest,
    ServerManifest ServerManifest,
    FileHashBlacklist FileHashBlacklist);