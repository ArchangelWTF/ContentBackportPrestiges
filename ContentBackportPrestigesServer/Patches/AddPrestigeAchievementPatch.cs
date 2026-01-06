using System.Reflection;
using ContentBackportPrestigesServer.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;

namespace ContentBackportPrestigesServer.Patches;

public sealed class AddPrestigeAchievementPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PrestigeHelper).GetMethod(nameof(PrestigeHelper.ProcessPendingPrestige))
            ?? throw new InvalidOperationException("Could not findProcessPendingPrestige!");
    }

    [PatchPostfix]
    public static void Postfix(SptProfile newProfile, PendingPrestige prestige)
    {
        var databaseServer =
            ServiceLocator.ServiceProvider.GetService<DatabaseServer>() ?? throw new NullReferenceException("Could not get DatabaseServer");

        var rewardHelper =
            ServiceLocator.ServiceProvider.GetService<RewardHelper>() ?? throw new NullReferenceException("Could not get RewardHelper");

        var indexOfPrestigeObtained = Math.Clamp(
            (prestige.PrestigeLevel ?? 1) - 1,
            0,
            databaseServer.GetTables().Templates.Prestige.Elements.Count - 1
        );
        var achievements = AddMissingPrestigesToProfile.PrestigeAchievements.Values.ToList();
        var currentAchievement = achievements[indexOfPrestigeObtained];

        if (newProfile.CharacterData?.PmcData?.Achievements?.ContainsKey(currentAchievement) is false)
        {
            rewardHelper.AddAchievementToProfile(newProfile, currentAchievement);
        }
    }
}
