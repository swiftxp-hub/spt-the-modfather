using System;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public record class ClientFileManifest(
    string RelativeFilePath,
    string Hash,
    long SizeInBytes,
    DateTimeOffset InstalledAt);