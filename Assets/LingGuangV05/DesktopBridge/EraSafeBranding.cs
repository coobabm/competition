using System;
using System.Text.RegularExpressions;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// 2026-10-23: the desktop watermark (「hongmengos / 2016 • Aero 桌面」) and the monitor bezel wordmark named a 2019 OS
    /// in a 2016 room (design review 2026-10-22, 重构调研 02_剧情逻辑与引导 §2.6 E1).
    /// 2026-10-24 (owner decision): the in-game OS is called 「Aurora 7」 ("hongmengos" was a working pseudonym); the PC came
    /// with a Ghost-installed 「Aurora 7 旗舰纯净版 2016.06」. This class is the single source of the shown name: code and data
    /// use the constants below, and scene text (Main.unity / ComputerCaseMenu.unity are read-only) is rewritten at runtime.
    /// The monitor is hardware, so its bezel keeps a model name (HM-2310) rather than the OS name.
    /// Namespaces, folders, asset GUIDs and object names keep "hongmengos".
    /// </summary>
    public sealed class EraSafeBranding : MonoBehaviour
    {
        /// <summary>The OS as players see it.</summary>
        public const string Os = "Aurora 7";
        /// <summary>The next version the OS keeps nagging about (free upgrade until 2016-07-29).</summary>
        public const string NextOs = "Aurora 10";
        /// <summary>The Ghost / pirated edition installed on the player's PC.</summary>
        public const string EditionZh = "Aurora 7 旗舰纯净版 2016.06", EditionEn = "Aurora 7 Ultimate Clean Edition 2016.06";
        public const string ComputerName = "AURORA-PC";
        public const string MonitorModel = "HM-2310";

        const string MarkName = "Aero Edition Mark", BezelName = "Silver Hongmengos Logo", MonitorBrandName = "Monitor Brand",
            ComputerNameLabel = "Computer name Value", EditionName = "Edition", SystemInfoName = "Aero System Information";
        static readonly Regex OldName = new Regex("hongmengos( 2016)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        string zh, en;
        bool rewrite;
        TMP_Text label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
            for (int i = 0; i < SceneManager.sceneCount; i++) Apply(SceneManager.GetSceneAt(i));
        }

        static void OnScene(Scene scene, LoadSceneMode mode) => Apply(scene);

        /// <summary>Player-visible text with the old pseudonym replaced (no-op for text that never had it).</summary>
        public static string Rename(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("hongmeng", StringComparison.OrdinalIgnoreCase) < 0) return text;
            text = text.Replace("HONGMENG-DESKTOP", ComputerName).Replace("HONGMENG  ", "MODEL  ");
            return OldName.Replace(text, Os);
        }

        static void Apply(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == MarkName) Attach(text, "Aurora 7 旗舰纯净版\n2016.06", "Aurora 7 Ultimate Clean\n2016.06");
                    else if (text.name == BezelName || text.name == MonitorBrandName) Attach(text, MonitorModel, MonitorModel);
                    else if (text.name == ComputerNameLabel) Attach(text, ComputerName, ComputerName);
                    else if (text.name == EditionName && text.transform.parent != null && text.transform.parent.name == SystemInfoName)
                        Attach(text, EditionZh, EditionEn);
                    else if (text.name == EditionName && text.text.IndexOf("hongmeng", StringComparison.OrdinalIgnoreCase) >= 0)
                        Attach(text, "Aurora 7\n旗舰纯净版", "Aurora 7\nUltimate");
                    else if (text.text.IndexOf("hongmeng", StringComparison.OrdinalIgnoreCase) >= 0) Attach(text, null, null, true);
                }
        }

        static void Attach(TMP_Text text, string zh, string en, bool rewrite = false)
        {
            var brand = text.GetComponent<EraSafeBranding>() ?? text.gameObject.AddComponent<EraSafeBranding>();
            brand.label = text; brand.zh = zh; brand.en = en; brand.rewrite = rewrite; brand.Refresh();
        }

        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (label == null) return;
            // Scene text that merely mentions the old name (setup wizard, window titles, boot wordmark...) follows the
            // localizer and only has the name swapped.
            string wanted = rewrite ? Rename(label.text) : GameText.IsEnglish ? en : zh;
            if (label.text != wanted) label.text = wanted;
        }
    }
}
