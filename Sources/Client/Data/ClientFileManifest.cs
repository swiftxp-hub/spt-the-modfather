using System;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class ClientFileManifest(string relativePath, string hash, long sizeInBytes, DateTimeOffset installedAt)
{
    public string RelativeFilePath { get; set; } = relativePath;

    public string Hash { get; set; } = hash;

    public long SizeInBytes { get; set; } = sizeInBytes;

    public DateTimeOffset InstalledAt { get; set; } = installedAt;
}