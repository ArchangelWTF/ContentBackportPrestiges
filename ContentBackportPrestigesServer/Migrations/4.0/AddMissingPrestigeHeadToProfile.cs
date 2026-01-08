using System.Text.Json.Nodes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Migration;
using SPTarkov.Server.Core.Models.Common;

namespace ContentBackportPrestigesServer.Migrations;

[Injectable]
public sealed class AddMissingPrestigeHeadToProfile : AbstractProfileMigration
{
    private static readonly List<MongoId> _heads = ["68fb8872fb3842532002cbc1", "68fb88b9d4b0e9617502c1c4"];

    public override string FromVersion
    {
        get { return "~4.0"; }
    }

    public override string ToVersion
    {
        get { return "~4.0"; }
    }

    public override string MigrationName
    {
        get { return "AddMissingPrestigeHeadToProfile"; }
    }

    public override bool CanMigrate(JsonObject profile, IEnumerable<IProfileMigration> previouslyRanMigrations)
    {
        if (profile["characters"]?["pmc"]?["Info"] is JsonObject playerInfo && profile["customisationUnlocks"] is JsonArray customizations)
        {
            if (playerInfo.TryGetPropertyValue("PrestigeLevel", out JsonNode? prestigeLevelProperty))
            {
                if (prestigeLevelProperty == null)
                {
                    return false;
                }

                int prestigeLevel = prestigeLevelProperty.GetValue<int>();

                if (prestigeLevel >= 5)
                {
                    var unlockedHeadIds = customizations
                        .OfType<JsonObject>()
                        .Where(c => c["type"]?.GetValue<string>() == "head" && c["id"] is not null)
                        .Select(c => c["id"]!.GetValue<string>())
                        .ToHashSet();

                    bool missingHead = _heads.Any(headId => !unlockedHeadIds.Contains(headId));

                    if (missingHead)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override JsonObject? Migrate(JsonObject profile)
    {
        if (profile["customisationUnlocks"] is JsonArray customizations)
        {
            var unlockedHeadIds = customizations
                .OfType<JsonObject>()
                .Where(c => c["type"]?.GetValue<string>() == "head" && c["id"] is not null)
                .Select(c => c["id"]!.GetValue<string>())
                .ToHashSet();

            foreach (var headId in _heads)
            {
                if (!unlockedHeadIds.Contains(headId))
                {
                    customizations.Add(
                        new JsonObject
                        {
                            ["id"] = headId.ToString(),
                            ["source"] = "prestige",
                            ["type"] = "head",
                        }
                    );
                }
            }
        }

        return base.Migrate(profile);
    }
}
