using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Display labels follow the current save. Routing and persistence IDs remain stable.</summary>
    [DisallowMultipleComponent]
    public sealed class AiDesktopIdentity : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        ChapterOneDesktopBridge bridge;
        AeroStartMenu menu;
        float next, nextDownloads;
        Michsky.DreamOS.TaskbarButton tipped;
        public static string Name(ChapterOneRuntime owner) => AppNames.PersonalName(owner != null && owner.Sim != null ? owner.Sim.S.aiName : null, GameText.IsEnglish);
        public static string Exe(ChapterOneRuntime owner) => Name(owner) + ".exe";

        public static AiDesktopIdentity Attach(ChapterOneRuntime owner)
        {
            var identity = owner.GetComponent<AiDesktopIdentity>() ?? owner.gameObject.AddComponent<AiDesktopIdentity>();
            identity.runtime = owner;
            identity.Refresh();
            return identity;
        }

        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .2f;
            Refresh();
        }

        public void Refresh()
        {
            if (runtime == null || runtime.Sim == null) return;
            if (bridge == null)
                foreach (var candidate in FindObjectsByType<ChapterOneDesktopBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.ApplicationId == XingGuangController.AppId) { bridge = candidate; break; }
            if (bridge == null) return;
            string exe = Exe(runtime);
            if (bridge.DesktopShortcut != null) SetLabels(bridge.DesktopShortcut.transform, exe);
            if (bridge.TaskbarShortcut != null)
            {
                // buttonTitle is also a DreamOS persistence key: change visible text only.
                var header = bridge.TaskbarShortcut.headerButton;
                if (header != null) { if (header.buttonText != exe) { header.buttonText = exe; header.UpdateUI(); } SetLabels(header.transform, exe); }
                if (tipped != bridge.TaskbarShortcut)
                {
                    tipped = bridge.TaskbarShortcut;
                    UiTip.Add(tipped, () => Exe(runtime));
                }
            }
            if (Time.unscaledTime >= nextDownloads)
            {
                nextDownloads = Time.unscaledTime + 1f;
                foreach (var download in FindObjectsByType<Michsky.DreamOS.WebBrowserDownloadItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (download.fileName == Story.LingGuangInstallFlow.FileName) download.SetDisplayName(exe);
            }
            if (menu == null) menu = FindAnyObjectByType<AeroStartMenu>(FindObjectsInactive.Include);
            if (menu != null && menu.programs != null)
                for (int i = 0; i < menu.programs.Length; i++)
                {
                    var item = menu.programs[i];
                    if ((bridge.Window == null || item.window != bridge.Window) && !(item.searchKeywords ?? "").Contains("#lingguang-hidden")) continue;
                    bool changed = item.title != exe;
                    item.title = exe;
                    if (item.row != null) SetLabels(item.row.transform, exe);
                    menu.programs[i] = item;
                    if (changed && menu.menu != null && menu.menu.activeSelf) menu.SetSearch(menu.search != null ? menu.search.text : "");
                }
        }

        static void SetLabels(Transform parent, string value)
        {
            foreach (var label in parent.GetComponentsInChildren<TMP_Text>(true))
            {
                var localization = label.GetComponent<DesktopLocalizedText>();
                if (localization != null) localization.enabled = false;
                label.richText = false;
                label.overflowMode = TextOverflowModes.Ellipsis;
                if (label.text != value) label.text = value;
            }
        }
    }
}
