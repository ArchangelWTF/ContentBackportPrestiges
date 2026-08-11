using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Server;

namespace ContentBackportPrestigesServer.Patches;

[Injectable]
public sealed class RemovePrestigeQuestRequirementsPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PostDbLoadService).GetMethod(
                "RemovePrestigeQuestRequirementsIfQuestNotFound",
                BindingFlags.Instance | BindingFlags.NonPublic
            ) ?? throw new InvalidOperationException("Could not find RemovePrestigeQuestRequirementsIfQuestNotFound!");
    }

    // This zero's out the RemovePrestigeQuestRequirementsIfQuestNotFound method, we will handle it here as we add some quests that are some of these requirements
    [PatchTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler()
    {
        return [new(OpCodes.Ret)];
    }
}
