using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// 2026-10-23: the desktop watermark (「hongmengos / 2016 • Aero 桌面」) and the monitor bezel wordmark named a 2019 OS
    /// in a 2016 room (design review 2026-10-22, 重构调研 02_剧情逻辑与引导 §2.6 E1). No era-safe system name is approved yet,
    /// so only the shown text changes: the watermark becomes a neutral edition line, the bezel shows the monitor's model.
    /// Scenes (Main.unity is read-only), namespaces, folders and object names keep "hongmengos".
    /// </summary>
    public sealed class EraSafeBranding : MonoBehaviour
    {
        const string MarkName = "Aero Edition Mark", BezelName = "Silver Hongmengos Logo";
        string zh, en;
        TMP_Text label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            for (int i = 0; i < SceneManager.sceneCount; i++) Apply(SceneManager.GetSceneAt(i));
        }

        static void OnScene(Scene scene, LoadSceneMode mode) => Apply(scene);

        static void Apply(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == MarkName) Attach(text, "2016 · Aero 桌面", "2016 · Aero Desktop");
                    else if (text.name == BezelName) Attach(text, "HM-2310", "HM-2310");
                }
        }

        static void Attach(TMP_Text text, string zh, string en)
        {
            var brand = text.GetComponent<EraSafeBranding>() ?? text.gameObject.AddComponent<EraSafeBranding>();
            brand.label = text; brand.zh = zh; brand.en = en; brand.Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (label == null) return;
            string wanted = GameText.IsEnglish ? en : zh;
            if (label.text != wanted) label.text = wanted;
        }
    }
}
