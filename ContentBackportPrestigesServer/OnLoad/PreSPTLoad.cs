using ContentBackportPrestigesServer.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace ContentBackportPrestigesServer.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 3)]
public sealed class PreSPTLoad(ISptLogger<PreSPTLoad> logger) : IOnLoad
{
    private bool _overridesInjected = false;
    private readonly List<AbstractPatch> _patches =
    [
        new AddPrestigeAchievementPatch(),
        new RemovePrestigeQuestRequirementsPatch(),
        new DisablePrestigeCheatPatch(),
    ];

    private void InjectOverrides()
    {
        if (_overridesInjected)
        {
            return;
        }

        try
        {
            foreach (AbstractPatch patch in _patches)
            {
                logger.Debug($"[Content Backport - Prestiges] Loading patch: {patch.GetType().Name}");
                patch.Enable();
            }
        }
        catch (Exception ex)
        {
            logger.Error($"[Content Backport - Prestiges] Error applying patch: {ex}");
            throw;
        }

        _overridesInjected = true;
    }

    public async Task OnLoad()
    {
        InjectOverrides();
    }
}
