using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    // Reversible presentation gates. Apps still initialize normally, so unlocking never loses their state.
    public sealed class StudyDesktopGates : MonoBehaviour
    {
        readonly Dictionary<WindowManager, string> windows = new Dictionary<WindowManager, string>();
        XingGuangController lab;
        float scanAt;

        public static void Attach(XingGuangController owner)
        {
            var gates = owner.GetComponent<StudyDesktopGates>() ?? owner.gameObject.AddComponent<StudyDesktopGates>();
            gates.lab = owner;
        }

        public static string Feature(string app)
        {
            switch (app)
            {
                case "xunbao": return "shop";
                case "home": return "power";
                case "zhongbao": return "crowd";
                case "miaoyu": return "data";
                default: return null;
            }
        }

        public static bool Allows(string app)
        {
            string feature = Feature(app);
            if (feature == null) return true;
            var owner = FindAnyObjectByType<XingGuangController>();
            return owner == null || owner.Sim == null || owner.runtime == null || owner.runtime.Sim.InPrologue
                || !owner.runtime.Sim.AppInstalled || owner.Sim.SystemUnlocked(feature);
        }

        void LateUpdate()
        {
            if (lab == null || lab.Sim == null || lab.runtime == null || lab.runtime.Sim.InPrologue || !lab.runtime.Sim.AppInstalled) return;
            if (Time.unscaledTime >= scanAt)
            {
                scanAt = Time.unscaledTime + 1;
                foreach (var binding in FindObjectsByType<ChapterOneDesktopBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string feature = Feature(binding.ApplicationId);
                    if (feature == null) continue;
                    bool open = lab.Sim.SystemUnlocked(feature);
                    if (binding.DesktopShortcut != null) binding.DesktopShortcut.gameObject.SetActive(open);
                    if (binding.Window != null) windows[binding.Window] = feature;
                }
                var list = GameObject.Find("Desktop List");
                foreach (var window in FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string feature = window.name == "Calculator" ? "crowd" : window.name == "Commander" ? "data" : window.name == "Game Hub" ? "games" : null;
                    if (feature == null) continue;
                    windows[window] = feature;
                    var icon = list != null ? list.transform.Find(window.name) : null;
                    if (icon != null) icon.gameObject.SetActive(lab.Sim.SystemUnlocked(feature));
                }
            }
            foreach (var pair in windows)
            {
                if (pair.Key == null || lab.Sim.SystemUnlocked(pair.Value) || !pair.Key.isOn) continue;
                pair.Key.CloseWindow();
                lab.View?.ShowToast("先陪它学习。这项功能会在能力 " + LingGuangV05.XingGuang.XgSim.FeatureUnlock[pair.Value] + " 开放。", 4);
            }
        }
    }
}
