using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace ContentBackportPrestigesServer.OnLoad;

// Content backport is PostDB +2, so we go in at +3
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public sealed class PostDBLoad(
    DatabaseServer databaseServer,
    WTTServerCommonLib.WTTServerCommonLib serverCommonLib,
    ImageRouter imageRouter,
    ModHelper modHelper,
    JsonUtil jsonUtil,
    FileUtil fileUtil
) : IOnLoad
{
    private static Assembly ModAssembly { get; } = Assembly.GetExecutingAssembly();
    private string ModPath { get; init; } = modHelper.GetAbsolutePathToModFolder(ModAssembly);

    public async Task OnLoad()
    {
        var databasePrestiges = databaseServer.GetTables().Templates.Prestige;

        await serverCommonLib.CustomLocaleService.CreateCustomLocales(ModAssembly);
        await serverCommonLib.CustomQuestService.CreateCustomQuests(ModAssembly);
        await serverCommonLib.CustomAchievementService.CreateCustomAchievements(ModAssembly);

        var prestigesToModify =
            await jsonUtil.DeserializeFromFileAsync<Prestige>(Path.Combine(ModPath, "db", "PrestigeBackport", "prestiges.json"))
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
                    && !databaseServer.GetTables().Templates.Quests.ContainsKey(c.Target.Item)
                )
                .ToList();

            foreach (var conditionToRemove in conditionsToRemove)
            {
                prestige.Conditions.Remove(conditionToRemove);
            }
        }

        // Add new streamer items to collector quest
        HandleNewCollectorItems();

        // Remove rewards out of various achievements
        RemoveRewardsOutOfAchievements();

        CreateRouteMapping(Path.Combine(ModPath, "db", "PrestigeBackport", "images"), "files");
    }

    // Method copied from DatabaseImporter, use it to add the new prestige images to the image router
    private void CreateRouteMapping(string directory, string newBasePath)
    {
        var directoryContent = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);

        foreach (var fileNameWithPath in directoryContent)
        {
            var fileNameWithNoSPTPath = Path.GetRelativePath(directory, fileNameWithPath);
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
        var quests = databaseServer.GetTables().Templates.Quests;

        if (quests.TryGetValue("5c51aac186f77432ea65c552", out Quest? collectorQuest))
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

    private void RemoveRewardsOutOfAchievements()
    {
        // Prestige 67 achievement
        var cheatingAchievement = databaseServer.GetTables().Templates.Achievements.FirstOrDefault(x => x.Id == "694c6575af08f6f1d59a5737");

        if (cheatingAchievement is not null)
        {
            cheatingAchievement.Rewards = [];
        }

        var gammaCaseAchievement = databaseServer
            .GetTables()
            .Templates.Achievements.FirstOrDefault(x => x.Id == "694dbb05a4a61e9ad031c609");

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
