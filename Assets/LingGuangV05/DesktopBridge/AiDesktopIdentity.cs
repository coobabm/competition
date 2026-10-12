using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            EnsureMenuEntry(exe);
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

        void EnsureMenuEntry(string exe)
        {
            if (menu == null || menu.programs == null || bridge.Window == null || !runtime.Sim.AppInstalled) return;
            foreach (var item in menu.programs)
                if (item.window == bridge.Window || (item.searchKeywords ?? "").Contains("#lingguang-hidden")) return;
            // This scene predates the AI shortcut: give it an actual menu entry as well as a desktop icon.
            foreach (var template in menu.programs)
            {
                if (template.row == null || template.row.GetComponent<Button>() == null) continue;
                var row = Instantiate(template.row, template.row.transform.parent);
                row.name = "Player AI Program";
                var button = row.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => { bridge.OpenApp(); menu.Close(); });
                var icon = row.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && bridge.TaskbarShortcut?.headerButton != null) icon.sprite = bridge.TaskbarShortcut.headerButton.buttonIcon;
                SetLabels(row.transform, exe);
                var entries = new System.Collections.Generic.List<AeroStartMenu.ProgramEntry>(menu.programs);
                entries.Add(new AeroStartMenu.ProgramEntry { title = exe, searchKeywords = "AI 灵光 LingGuang", row = row, window = bridge.Window });
                menu.programs = entries.ToArray();
                row.SetActive(true);
                menu.SetSearch(menu.search != null ? menu.search.text : "");
                break;
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
