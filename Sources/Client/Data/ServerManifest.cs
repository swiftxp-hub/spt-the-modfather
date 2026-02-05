using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class ServerManifest
{
    public List<ServerFileManifest> Files { get; set; } = [];

    public string[] IncludePatterns { get; set; } = [];

    public string[] ExcludePatterns { get; set; } = [];
}