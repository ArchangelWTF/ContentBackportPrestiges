using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
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

        var newPrestiges =
            await jsonUtil.DeserializeFromFileAsync<Prestige>(Path.Combine(ModPath, "db", "PrestigeBackport", "prestiges.json"))
            ?? throw new InvalidOperationException("Could not load prestiges!");

        foreach (var prestige in newPrestiges.Elements)
        {
            // Add the new prestiges, should they not already exist
            if (!databasePrestiges.Elements.Any(e => e.Id == prestige.Id))
            {
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

        // Remove rewards out of cheating achievement
        RemoveRewardsOutOfPrestigeAchievement();

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

    private void HandleNewCollectorItems()
    {
        var quests = databaseServer.GetTables().Templates.Quests;

        if (quests.TryGetValue("5c51aac186f77432ea65c552", out Quest? collectorQuest))
        {
            if (collectorQuest is null || collectorQuest.Conditions.AvailableForFinish is null)
            {
                return;
            }

            collectorQuest.Conditions.AvailableForFinish.Add(
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
                    Target = new SPTarkov.Server.Core.Utils.Json.ListOrT<string>(["69398e94ca94fd2877039504"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
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
                    Target = new SPTarkov.Server.Core.Utils.Json.ListOrT<string>(["6937edb912d456a817083e82"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
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
                    Target = new SPTarkov.Server.Core.Utils.Json.ListOrT<string>(["6937ecf8628ee476240c07cb"], null),
                    VisibilityConditions = [],
                }
            );

            collectorQuest.Conditions.AvailableForFinish.Add(
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
                    Target = new SPTarkov.Server.Core.Utils.Json.ListOrT<string>(["6937f02dfd6488bb27024839"], null),
                    VisibilityConditions = [],
                }
            );
        }
    }

    private void RemoveRewardsOutOfPrestigeAchievement()
    {
        var cheatingAchievement = databaseServer.GetTables().Templates.Achievements.FirstOrDefault(x => x.Id == "694c6575af08f6f1d59a5737");

        if (cheatingAchievement is not null)
        {
            cheatingAchievement.Rewards = [];
        }
    }
}
