using System.Reflection;
using BepInEx;
using ContentBackportPrestigesClient.Patches;
using EFT.UI;
using UnityEngine;
using UnityEngine.Video;

namespace ContentBackportPrestigesClient;

[BepInPlugin("wtf.archangel.contentbackportprestiges", "Content Backport - Prestiges", "1.0.0")]
public class ContentBackportPrestigesPlugin : BaseUnityPlugin
{
    public static Dictionary<int, PrestigeIconsData> PrestigeIconsToAdd = [];
    public static PrestigeAnimationData Prestige6AnimationData;

    private static AssetBundle _bundle;

    protected void Awake()
    {
        _bundle = LoadBundle("contentbackportprestige");

        var data = new PrestigeIconsData
        {
            ConformationIcon = _bundle.LoadAsset<Sprite>("Prestige_Icon_Lvl_6_512px"),
            SmallIcon = _bundle.LoadAsset<Sprite>("Prestige_Icon_Lvl_6_32px"),
            BigIcon = _bundle.LoadAsset<Sprite>("Prestige_Icon_Lvl_6_132px"),
        };

        PrestigeIconsToAdd[6] = data;

        Prestige6AnimationData = new PrestigeAnimationData
        {
            Level = 6,
            AudioClip = _bundle.LoadAsset<AudioClip>("prestige_06"),
            VideoClip = _bundle.LoadAsset<VideoClip>("PRESTIGE_6"),
        };

        Task.Run(LoadHardSettings);
        new PrestigeAnimationPlayerPatch().Enable();
    }

    public static AssetBundle LoadBundle(string bundleFileName)
    {
        string dllPath = Assembly.GetExecutingAssembly().Location;
        string dir = Path.GetDirectoryName(dllPath);

        string bundlePath = Path.Combine(dir, bundleFileName);

        if (!File.Exists(bundlePath))
        {
            throw new FileNotFoundException($"AssetBundle not found at: {bundlePath}");
        }

        var bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            throw new Exception($"Failed to load AssetBundle (returned null): {bundlePath}");
        }

        return bundle;
    }

    public async Task LoadHardSettings()
    {
        await EFTHardSettings.Load();

        var prestigeIcons = EFTHardSettings.Instance.ChatSpecialIconSettings.PrestigeIcons;

        foreach (var prestigeIcon in PrestigeIconsToAdd)
        {
            if (prestigeIcons.ContainsKey(prestigeIcon.Key))
            {
                continue;
            }

            prestigeIcons.Add(prestigeIcon.Key, prestigeIcon.Value);
        }
    }
}
