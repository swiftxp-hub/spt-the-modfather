using System;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public record class ServerFileManifest(
    string RelativeFilePath,
    string Hash,
    long SizeInBytes,
    DateTimeOffset InstalledAt);