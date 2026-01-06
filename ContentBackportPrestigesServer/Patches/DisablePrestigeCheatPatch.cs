using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Eft.Dialog;

namespace ContentBackportPrestigesServer.Patches;

public sealed class DisablePrestigeCheatPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type =
            AccessTools.TypeByName("WTTContentBackport.Commands.SixSevenPrestigeCommand")
            ?? throw new InvalidOperationException("WTT Content Backport could not be found, is it installed?");
        var method =
            AccessTools.Method(type, "PerformAction")
            ?? throw new InvalidOperationException("Could not find PerformAction in WTT's content backport!");
        return method;
    }

    [PatchPrefix]
    public static bool Prefix(SendMessageRequest request, ref ValueTask<string> __result)
    {
        __result = new ValueTask<string>(request.DialogId);

        return false;
    }
}
