using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace ContentBackportPrestigesClient.Patches;

public class PrestigeIconPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2299<int, PrestigeIconsData>), "OnAfterDeserialize");
    }

    [PatchPostfix]
    public static void Postfix(GClass2299<int, PrestigeIconsData> __instance)
    {
        foreach (var prestigeIcon in ContentBackportPrestigesPlugin.PrestigeIconsToAdd)
        {
            if (__instance.ContainsKey(prestigeIcon.Key))
            {
                continue;
            }

            __instance.Add(prestigeIcon.Key, prestigeIcon.Value);
        }
    }
}
