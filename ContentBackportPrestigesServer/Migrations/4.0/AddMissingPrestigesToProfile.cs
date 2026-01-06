using System.Text.Json.Nodes;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Migration;
using SPTarkov.Server.Core.Models.Common;

namespace ContentBackportPrestigesServer.Migrations;

[Injectable]
public sealed class AddMissingPrestigesToProfile : AbstractProfileMigration
{
    // Prestige id - Achievement id
    public static Dictionary<MongoId, MongoId> PrestigeAchievements { get; } =
        new Dictionary<MongoId, MongoId>
        {
            { new MongoId("672df12f97f0469cea52f55e"), new MongoId("676091c0f457869a94017a23") },
            { new MongoId("672df4281ab8d9c8849a0c88"), new MongoId("676094451fec2f7426093be6") },
            { new MongoId("683da91d6f472cfa738c52f2"), new MongoId("6842c25bd02bc07d70054019") },
            { new MongoId("6842f121000d98ce33b9a60f"), new MongoId("6842c27a38482d35ac0bd847") },
            { new MongoId("68d3ddb4fc101237e601d774"), new MongoId("68d3fe84757f8967ec09099b") },
            { new MongoId("68d3e6f46a7ba36646713fa6"), new MongoId("68d3ff840531ed76e808866c") },
        };

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
        get { return "AddMissingPrestigesToProfile"; }
    }

    public override bool CanMigrate(JsonObject profile, IEnumerable<IProfileMigration> previouslyRanMigrations)
    {
        if (
            profile["characters"]?["pmc"]?["Prestige"] is JsonObject prestiges
            && profile["characters"]?["pmc"]?["Achievements"] is JsonObject achievements
        )
        {
            foreach (var prestige in prestiges)
            {
                var id = prestige.Key;

                if (PrestigeAchievements.TryGetValue(id, out var AchievementId))
                {
                    if (!achievements.ContainsKey(AchievementId))
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
        if (
            profile["characters"]?["pmc"]?["Prestige"] is JsonObject prestiges
            && profile["characters"]?["pmc"]?["Achievements"] is JsonObject achievements
        )
        {
            foreach (var prestige in prestiges)
            {
                var id = prestige.Key;
                var timestamp = prestige.Value!.GetValue<long>();

                if (PrestigeAchievements.TryGetValue(id, out var AchievementId))
                {
                    if (!achievements.ContainsKey(AchievementId))
                    {
                        achievements.Add(AchievementId, timestamp);
                    }
                }
            }
        }

        return base.Migrate(profile);
    }
}
