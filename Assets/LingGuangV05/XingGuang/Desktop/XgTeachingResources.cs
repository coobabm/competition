using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang
{
    static class XgTeachingResources
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            var asset = Resources.Load<TextAsset>("LingGuangV05/Teaching/minigame_packs");
            if (asset == null) { Debug.LogError("Missing minigame_packs.json"); return; }
            var file = JsonUtility.FromJson<XgTeachingPackFile>(asset.text);
            if (file != null && file.packs != null && file.packs.Length == 19) XgTeachingPacks.All = file.packs;
            else Debug.LogError("Invalid teaching pack manifest");
        }
    }
}
