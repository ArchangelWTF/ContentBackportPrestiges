using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;

namespace ContentBackportPrestigesClient.Patches;

public class PrestigeAnimationPlayerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PrestigeAnimationPlayer).GetMethod(nameof(PrestigeAnimationPlayer.Show));
    }

    [PatchPrefix]
    public static void Prefix(ref PrestigeAnimationData[] ____animationData)
    {
        var animationList = ____animationData.ToList();

        animationList.Add(ContentBackportPrestigesPlugin.Prestige6AnimationData);

        ____animationData = animationList.ToArray();
    }
}
