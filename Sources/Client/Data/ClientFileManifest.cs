using System;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public record class ClientFileManifest(
    string RelativeFilePath,
    string Hash,
    long SizeInBytes,
    DateTimeOffset InstalledAt)
{
    public static ClientFileManifest ToClientManifestEntry(ServerFileManifest serverFile)
    {
        return new ClientFileManifest(
            serverFile.RelativeFilePath,
            serverFile.Hash,
            serverFile.SizeInBytes,
            DateTimeOffset.UtcNow);
    }
}