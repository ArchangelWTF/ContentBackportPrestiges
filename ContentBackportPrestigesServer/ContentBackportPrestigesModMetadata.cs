using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ContentBackportPrestigesServer;

public sealed record ContentBackportPrestigesModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "wtf.archangel.contentbackportprestiges";
    public override string Name { get; init; } = "Content Backport - Prestiges";
    public override string Author { get; init; } = "ArchangelWTF";
    public override List<string>? Contributors { get; init; } = [];
    public override Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.3");
    public override List<string>? Incompatibilities { get; init; } = [];
    public override Dictionary<string, Range>? ModDependencies { get; init; } =
        new() { { "com.wtt.commonlib", new Range("~2.0.9") }, { "com.wtt.contentbackport", new Range("~1.0.1") } };
    public override string? Url { get; init; } = "https://github.com/ArchangelWTF/ContentBackportPrestiges";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MIT";
}
