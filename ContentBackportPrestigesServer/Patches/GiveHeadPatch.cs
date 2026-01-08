using System.Reflection;
using Mono.Cecil;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Prestige;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace ContentBackportPrestigesServer.Patches;

[Obsolete("This can be removed when moving this mod to SPT 4.1")]
public sealed class GiveHeadPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PrestigeController).GetMethod(nameof(PrestigeController.ObtainPrestige))
            ?? throw new InvalidOperationException("Could not PrestigeController!");
    }

    [PatchPostfix]
    public static async void Postfix(Task __result, MongoId sessionId, ObtainPrestigeRequestList request)
    {
        __result = PatchedResult(sessionId, request);
    }

    private static async Task PatchedResult(MongoId sessionId, ObtainPrestigeRequestList request)
    {
        var profileHelper =
            ServiceLocator.ServiceProvider.GetService(typeof(ProfileHelper)) as ProfileHelper
            ?? throw new InvalidOperationException("Could not find ProfileHelper!");
        var databaseService =
            ServiceLocator.ServiceProvider.GetService(typeof(DatabaseService)) as DatabaseService
            ?? throw new InvalidOperationException("Could not find DatabaseService!");
        var saveServer =
            ServiceLocator.ServiceProvider.GetService(typeof(SaveServer)) as SaveServer
            ?? throw new InvalidOperationException("Could not find SaveServer!");

        var profile = profileHelper.GetFullProfile(sessionId);
        if (profile is not null)
        {
            var pendingPrestige = new PendingPrestige
            {
                PrestigeLevel = (profile.CharacterData?.PmcData?.Info?.PrestigeLevel ?? 0) + 1,
                Items = request,
            };

            profile.SptData!.PendingPrestige = pendingPrestige;
            profile.ProfileInfo!.IsWiped = true;

            var prestigeLevels = databaseService.GetTemplates().Prestige?.Elements ?? [];

            var prestigeRewards = prestigeLevels
                .Slice(0, pendingPrestige.PrestigeLevel.Value)
                .SelectMany(prestigeInner => prestigeInner.Rewards);

            var customisationTemplateDb = databaseService.GetTemplates().Customization;

            foreach (var reward in prestigeRewards)
            {
                if (reward.Target is null || !MongoId.IsValidMongoId(reward.Target))
                {
                    continue;
                }

                if (!customisationTemplateDb.TryGetValue(reward.Target, out var template))
                {
                    continue;
                }

                // This has to be done before the profile is wiped, as the user can only select a new head during the wipe
                if (template.Parent == CustomisationTypeId.HEAD)
                {
                    profile.CustomisationUnlocks.Add(
                        new CustomisationStorage
                        {
                            Id = new MongoId(reward.Target),
                            Source = CustomisationSource.PRESTIGE,
                            Type = CustomisationType.HEAD,
                        }
                    );
                }
            }

            await saveServer.SaveProfileAsync(sessionId);
        }
    }
}
