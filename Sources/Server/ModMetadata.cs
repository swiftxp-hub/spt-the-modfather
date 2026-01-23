using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SwiftXP.SPT.TheModfather.Server;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.swiftxp.spt.themodfather";
    public override string Name { get; init; } = "The Modfather";
    public override string Author { get; init; } = "SwiftXP";
    public override List<string>? Contributors { get; init; } = [];
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.11");

    public override List<string>? Incompatibilities { get; init; } = [];
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = [];
    public override string? Url { get; init; } = "https://github.com/swiftxp-hub/spt-the-modfather";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}