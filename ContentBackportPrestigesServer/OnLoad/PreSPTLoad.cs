using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace ContentBackportPrestigesServer.OnLoad;

[Injectable(TypePriority = OnLoadOrder.Preload + 3)]
public sealed class PreSPTLoad(
    IEnumerable<IRuntimePatch> patches,
    ISptLogger<PreSPTLoad> logger,
    TemplateTable templateTable,
    WTTServerCommonLib.WTTServerCommonLib serverCommonLib,
    ImageRouter imageRouter,
    ModHelper modHelper,
    JsonUtil jsonUtil,
    FileUtil fileUtil
) : IOnLoad
{
    private static Assembly ModAssembly { get; } = Assembly.GetExecutingAssembly();
    private string ModPath { get; init; } = modHelper.GetAbsolutePathToModFolder(ModAssembly);

    private static Dictionary<MongoId, MongoId> PrestigeAchievementsToAdd { get; } =
        new Dictionary<MongoId, MongoId>
        {
            { new MongoId("68d3ddb4fc101237e601d774"), new MongoId("68d3fe84757f8967ec09099b") },
            { new MongoId("68d3e6f46a7ba36646713fa6"), new MongoId("68d3ff840531ed76e808866c") },
        };

    private static MongoId CollectorQuestId { get; } = new MongoId("5c51aac186f77432ea65c552");

    private static Dictionary<MongoId, MongoId> CollectorLoyaltyRequirements { get; } =
        new Dictionary<MongoId, MongoId>
        {
            { Traders.THERAPIST, new MongoId("c330d074ea6176518c49bb64") },
            { Traders.PRAPOR, new MongoId("bd4e7b7687fc289f84e1e32b") },
            { Traders.PEACEKEEPER, new MongoId("6089903033045a46e84a95a0") },
            { Traders.MECHANIC, new MongoId("8899567e7ce6c96a2006426f") },
            { Traders.JAEGER, new MongoId("1afaa330038831a5a30065e5") },
            { Traders.SKIER, new MongoId("db6619ebb94a6b54262bb2c4") },
            { Traders.RAGMAN, new MongoId("8f859a0bdedc98e63feac94e") },
        };

    public async Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var patch in patches)
            {
                patch.Enable();
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[Content Backport - Prestiges] Error applying patch: {ex}");
            throw;
        }

        var databasePrestiges = templateTable.Prestige;

        await serverCommonLib.CustomLocaleService.CreateCustomLocales(ModAssembly);
        await serverCommonLib.CustomQuestService.CreateCustomQuests(ModAssembly);
        await serverCommonLib.CustomAchievementService.CreateCustomAchievements(ModAssembly);

        var prestigesToModify =
            await jsonUtil.DeserializeFromFileAsync<Prestige>(System.IO.Path.Combine(ModPath, "db", "PrestigeBackport", "prestiges.json"))
            ?? throw new InvalidOperationException("Could not load prestiges!");

        foreach (var prestige in prestigesToModify.Elements)
        {
            var existingPrestige = databasePrestiges.Elements.FirstOrDefault(e => e.Id == prestige.Id);

            // Modify the existing prestige, required for any of the new prestiges post 1.0.5.0
            if (existingPrestige != null)
            {
                var existingIndex = databasePrestiges.Elements.IndexOf(existingPrestige);
                databasePrestiges.Elements[existingIndex] = prestige;
            }
            else
            {
                // Add the prestige if it doesn't exist
                databasePrestiges.Elements.Add(prestige);

                if (PrestigeAchievementsToAdd.TryGetValue(prestige.Id, out var achievementId))
                {
                    PrestigeHelper.PrestigeAchievements.TryAdd(prestige.Id, achievementId);
                }
            }
        }

        // Re-handle SPT's RemovePrestigeQuestRequirements here, we have to patch it out and handle it here because at that point our quests aren't loaded in yet
        foreach (var prestige in databasePrestiges.Elements)
        {
            // Remove conditions for quests we dont have
            var conditionsToRemove = prestige
                .Conditions.Where(c =>
                    c.ConditionType == "Quest"
                    && c.Target is not null
                    && c.Target.Item is not null
                    && c.Target.IsItem
                    && !templateTable.Quests.ContainsKey(c.Target.Item)
                )
                .ToList();

            foreach (var conditionToRemove in conditionsToRemove)
            {
                prestige.Conditions.Remove(conditionToRemove);
            }
        }

        // Add new streamer items to collector quest
        HandleNewCollectorItems();

        // Swap collector over to the reworked unlock requirements
        HandleNewCollectorRequirements();

        // Remove rewards out of various achievements
        RemoveRewardsOutOfAchievements();

        CreateRouteMapping(System.IO.Path.Combine(ModPath, "db", "PrestigeBackport", "images"), "files");
    }

    // Method copied from DatabaseImporter, use it to add the new prestige images to the image router
    private void CreateRouteMapping(string directory, string newBasePath)
    {
        var directoryContent = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

        foreach (var fileNameWithPath in directoryContent)
        {
            var fileNameWithNoSPTPath = System.IO.Path.GetRelativePath(directory, fileNameWithPath);
            var filePathNoExtension = fileUtil.StripExtension(fileNameWithNoSPTPath, true);
            if (filePathNoExtension.StartsWith("/") || fileNameWithPath.StartsWith("\\"))
            {
                filePathNoExtension = $"{filePathNoExtension.Substring(1)}";
            }

            var bsgPath = $"/{newBasePath}/{filePathNoExtension}".Replace("\\", "/");
            imageRouter.AddRoute(bsgPath, fileNameWithPath);
        }
    }

    // This adds the new streamer items to the collector quest
    private void HandleNewCollectorItems()
    {
        var quests = templateTable.Quests;

        if (quests.TryGetValue(CollectorQuestId, out Quest? collectorQuest))
        {
            if (collectorQuest is null || collectorQuest.Conditions.AvailableForFinish is null)
            {
                return;
            }

            // Remove handover conditions for items we no longer want on the collector quest
            var itemsToRemove = new HashSet<string>
            {
                "5bc9c377d4351e3bac12251b", // Old firesteel
                "5bc9bc53d4351e00367fbcee", // Golden rooster figurine
                "5bc9b156d4351e00367fbce9", // Jar of DevilDog mayo
                "5bc9c29cd4351e003562b8a3", // Can of sprats
                "5bd073c986f7747f627e796c", // Kotton beanie
            };

            collectorQuest.Conditions.AvailableForFinish.RemoveAll(condition =>
                condition.Target is not null
                && (
                    (condition.Target.Item is not null && itemsToRemove.Contains(condition.Target.Item))
                    || (condition.Target.List is not null && condition.Target.List.Any(target => itemsToRemove.Contains(target)))
                )
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                // Hehe.. NUT sack xdx
                new QuestCondition
                {
                    Id = "693c3a908ad994118b846d63",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["69398e94ca94fd2877039504"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                // Mazoni golden dumbbell
                new QuestCondition
                {
                    Id = "693c3a9fc17c9edbfc58325a",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["6937edb912d456a817083e82"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                // Tigzresq splint
                new QuestCondition
                {
                    Id = "693c3aacf0cd3ec97007f2c1",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["6937ecf8628ee476240c07cb"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                //Domontovich ushanka hat
                new QuestCondition
                {
                    Id = "693c3ab82b0477e3de2b2312",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["6937f02dfd6488bb27024839"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                //DesmondPilak CD
                new QuestCondition
                {
                    Id = "6a6528d19363eee246875aea",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["69f9d547b98cc4120608692a"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                //Dunduk floppy disk
                new QuestCondition
                {
                    Id = "6a6528d19363eee246875ae8",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["69f9d60b5de6674f08060f2a"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
                //SheefGG piggy bank
                new QuestCondition
                {
                    Id = "6a6528d19363eee246875aee",
                    GlobalQuestCounterId = "",
                    DogtagLevel = 0,
                    ParentId = "",
                    DynamicLocale = false,
                    OnlyFoundInRaid = true,
                    Value = 1,
                    IsEncoded = false,
                    ConditionType = "HandoverItem",
                    MaxDurability = 100,
                    MinDurability = 0,
                    Target = new ListOrT<string>(["69f9d319c906cd16da03b374"], null),
                    VisibilityConditions = [],
                }
            );
        }
    }

    private void HandleNewCollectorRequirements()
    {
        if (
            !templateTable.Quests.TryGetValue(CollectorQuestId, out Quest? collectorQuest)
            || collectorQuest?.Conditions.AvailableForStart is null
        )
        {
            return;
        }

        var conditions = collectorQuest.Conditions.AvailableForStart;
        conditions.Clear();

        foreach (var (traderId, conditionId) in CollectorLoyaltyRequirements)
        {
            conditions.Add(CreateTraderCondition(conditionId, "TraderLoyalty", traderId, 4));
        }

        // Scav karma of at least +3
        conditions.Add(CreateTraderCondition(new MongoId("d7c44fe55201a9977a76daec"), "TraderStanding", Traders.FENCE, 3));

        // Chemical - Part 4, or Big Customer, or Out of Curiosity
        conditions.Add(
            CreateQuestCondition(
                new MongoId("ec97d9bee20f09cb2611bef8"),
                new MongoId("597a0f5686f774273b74f676"),
                [QuestStatusEnum.Success, QuestStatusEnum.Fail]
            )
        );

        // Sew it Good - Part 2
        conditions.Add(
            CreateQuestCondition(
                new MongoId("25c638ef25825acbc33b5c73"),
                new MongoId("5ae4495c86f7744e87761355"),
                [QuestStatusEnum.Success]
            )
        );

        // A Shooter Born in Heaven
        conditions.Add(
            CreateQuestCondition(
                new MongoId("7ec597358db2c9929174247b"),
                new MongoId("5c0bde0986f77479cf22c2f8"),
                [QuestStatusEnum.Success]
            )
        );

        // The Tarkov Shooter - Part 4
        conditions.Add(
            CreateQuestCondition(
                new MongoId("64c7a6b90a82102822f73e57"),
                new MongoId("5bc480a686f7741af0342e29"),
                [QuestStatusEnum.Success]
            )
        );

        for (var index = 0; index < conditions.Count; index++)
        {
            conditions[index].Index = index;
        }
    }

    private static QuestCondition CreateTraderCondition(MongoId conditionId, string conditionType, MongoId traderId, double value)
    {
        return new QuestCondition
        {
            Id = conditionId,
            ConditionType = conditionType,
            CompareMethod = ">=",
            Value = value,
            Target = new ListOrT<string>(null, traderId),
            GlobalQuestCounterId = "",
            ParentId = "",
            DynamicLocale = false,
            VisibilityConditions = [],
        };
    }

    private static QuestCondition CreateQuestCondition(MongoId conditionId, MongoId questId, HashSet<QuestStatusEnum> status)
    {
        return new QuestCondition
        {
            Id = conditionId,
            ConditionType = "Quest",
            Status = status,
            Target = new ListOrT<string>(null, questId),
            AvailableAfter = 0,
            Dispersion = 0,
            GlobalQuestCounterId = "",
            ParentId = "",
            DynamicLocale = false,
            VisibilityConditions = [],
        };
    }

    private void RemoveRewardsOutOfAchievements()
    {
        // Prestige 67 achievement
        var cheatingAchievement = templateTable.Achievements.FirstOrDefault(x => x.Id == "694c6575af08f6f1d59a5737");

        if (cheatingAchievement is not null)
        {
            cheatingAchievement.Rewards = [];
        }

        var gammaCaseAchievement = templateTable.Achievements.FirstOrDefault(x => x.Id == "694dbb05a4a61e9ad031c609");

        if (gammaCaseAchievement is not null)
        {
            // Secure container Gamma (Loui Peeton)
            var gammaCaseId = new MongoId("68f117b8121d878a2303eee0");

            gammaCaseAchievement.Rewards = gammaCaseAchievement
                .Rewards.Where(reward =>
                {
                    if (reward.Items is null)
                    {
                        return true;
                    }

                    if (reward.Items.Any(item => item.Template == gammaCaseId))
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();
        }
    }
}
