using System;

namespace SwiftXP.SPT.TheModfather.Server.Data;

public record class ServerFileManifest(
    string RelativeFilePath,
    string Hash,
    long SizeInBytes,
    DateTimeOffset InstalledAt);