using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace ContentBackportPrestigesServer;

public sealed record ContentBackportPrestigesModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "wtf.archangel.contentbackportprestiges";
    public string Name { get; init; } = "Content Backport - Prestiges";
    public string Author { get; init; } = "ArchangelWTF";
    public List<string>? Contributors { get; init; } = [];
    public Version Version { get; init; } = new(ContentBackportPrestigesCompileConstants.MOD_VERSION);
    public Range SptVersion { get; init; } = new("~4.1");
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, Range>? ModDependencies { get; init; } =
        new() { { "com.wtt.commonlib", new Range("^3.0") }, { "com.wtt.contentbackport", new Range("1.1.3") } };
    public string? Url { get; init; } = "https://github.com/ArchangelWTF/ContentBackportPrestiges";
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}
