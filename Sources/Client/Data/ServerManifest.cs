using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public record class ServerManifest(
    IReadOnlyList<ServerFileManifest> Files,
    string[] IncludePatterns,
    string[] ExcludePatterns);