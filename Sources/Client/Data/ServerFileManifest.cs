using System;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class ServerFileManifest(string relativeFilePath, string hash, long sizeInBytes, DateTimeOffset installedAt)
{
    public string RelativeFilePath { get; set; } = relativeFilePath;

    public string Hash { get; set; } = hash;

    public long SizeInBytes { get; set; } = sizeInBytes;

    public DateTimeOffset InstalledAt { get; set; } = installedAt;
}