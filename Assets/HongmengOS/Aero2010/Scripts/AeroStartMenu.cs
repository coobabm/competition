using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using LingGuangV05.CaseMenu;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HongmengOS.Aero2010
{
    [DisallowMultipleComponent]
    public sealed class AeroStartMenu : MonoBehaviour
    {
        [Serializable] public struct ProgramEntry
        {
            public string title;
            public string searchKeywords;
            public GameObject row;
            public WindowManager window;
            public bool gameSettings;
        }
        public GameObject menu;
        public RectTransform panel;
        public TMP_InputField search;
        public TMP_Text emptyMessage;
        public UnityEngine.UI.ScrollRect programScroll;
        public ProgramEntry[] programs;
        public WindowManager[] windows;
        public WindowManager computerWindow;
        public WindowManager controlPanelWindow;
        public WindowManager userWindow;
        public AeroShellGraphic startOrb;
        public UnityEvent shutdown = new UnityEvent();
        private readonly List<WindowManager> desktopRestore = new List<WindowManager>(16);
        private bool showingDesktop;

        private void Start()
        {
            EnsureSettingsEntry();
            EnsureRightSettingsShortcut();
            if (FindAnyObjectByType<CaseMenuController>() == null) GameSettings.ApplySaved();
        }

        private void EnsureSettingsEntry()
        {
            if (programs == null || programScroll == null || programScroll.content == null) return;
            int index = Array.FindIndex(programs, entry => entry.gameSettings);
            if (index < 0)
            {
                var template = Array.Find(programs, entry => controlPanelWindow != null && entry.window == controlPanelWindow && entry.row != null).row;
                bool hasSettingsIcon = template != null;
                if (template == null) template = Array.Find(programs, entry => entry.row != null && entry.row.GetComponent<UnityEngine.UI.Button>() != null).row;
                if (template == null) return;
                var row = Instantiate(template, programScroll.content);
                row.name = "Game Settings";
                // This row has its own action; never retain the copied app's serialized launch listeners.
                row.GetComponent<UnityEngine.UI.Button>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var icon = row.transform.Find("Icon");
                if (icon != null && !hasSettingsIcon) icon.gameObject.SetActive(false);
                var entries = new List<ProgramEntry>(programs);
                entries.Insert(0, new ProgramEntry { title = "Settings", searchKeywords = "设置 游戏 音量 全屏 帮助 settings volume fullscreen help", row = row, gameSettings = true });
                programs = entries.ToArray(); index = 0;
            }
            var settings = programs[index];
            if (settings.row == null) return;
            settings.row.transform.SetAsFirstSibling();
            var button = settings.row.GetComponent<UnityEngine.UI.Button>();
            if (button == null) return;
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(OpenSettings);
            foreach (var binding in settings.row.GetComponentsInChildren<LingGuangV05.Desktop.DesktopLocalizedText>(true))
                binding.enabled = false;
            foreach (var label in settings.row.GetComponentsInChildren<TMP_Text>(true)) label.text = GameText.T("设置", "Settings");
            button.interactable = true;
        }

        private void EnsureRightSettingsShortcut()
        {
            if(panel==null)return;
            var column=panel.Find("Places And System");
            if(column==null)return;
            var row=column.Find("Game Settings Shortcut") as RectTransform;
            if(row==null)
            {
                var template=column.Find("Control Panel")??column.Find("Documents");
                if(template==null||template.GetComponent<UnityEngine.UI.Button>()==null)return;
                row=(RectTransform)Instantiate(template.gameObject,column).transform;
                row.name="Game Settings Shortcut";
            }
            row.pivot=new Vector2(.5f,.5f);
            // Tall menus have room above the footer. Short menus use the empty space
            // beside the portrait, so the fixed documents/music/app rows never overlap it.
            bool compact=panel.rect.height<700;
            row.anchorMin=compact?Vector2.one:Vector2.zero;
            row.anchorMax=compact?Vector2.one:new Vector2(1,0);
            row.anchoredPosition=compact?new Vector2(-34,-48):new Vector2(0,126);
            row.sizeDelta=compact?new Vector2(60,30):new Vector2(-10,42);
            var button=row.GetComponent<UnityEngine.UI.Button>();
            button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(OpenSettings);button.interactable=true;
            foreach(var binding in row.GetComponentsInChildren<LingGuangV05.Desktop.DesktopLocalizedText>(true))binding.enabled=false;
            foreach(var label in row.GetComponentsInChildren<TMP_Text>(true))
            {
                label.text=GameText.T("设置","Settings");label.fontSize=compact?13:18;
                label.enableAutoSizing=false;label.textWrappingMode=TextWrappingModes.NoWrap;
                label.alignment=TextAlignmentOptions.Midline;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
                label.rectTransform.offsetMin=new Vector2(3,0);label.rectTransform.offsetMax=new Vector2(-3,0);
            }
            row.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (menu != null && menu.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (menu == null) return;
            if (menu.activeSelf) { Close(); return; }
            menu.SetActive(true);
            EnsureSettingsEntry();
            EnsureRightSettingsShortcut();
            menu.transform.SetAsLastSibling();
            if (panel != null && menu.transform is RectTransform bounds)
            {
                Vector2 size = panel.sizeDelta;
                size.y = Mathf.Clamp(bounds.rect.height - 66, 320, 724);
                size.x = Mathf.Min(660, Mathf.Max(400, bounds.rect.width - 16));
                panel.sizeDelta = size;
            }
            EnsureRightSettingsShortcut();
            if (startOrb != null) startOrb.Selected = true;
            ShowAllPrograms();
            if (search != null)
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(search.gameObject);
                search.ActivateInputField();
            }
        }

        public void Close()
        {
            if (menu == null) return;
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null && eventSystem.currentSelectedGameObject.transform.IsChildOf(menu.transform))
                eventSystem.SetSelectedGameObject(null);
            menu.SetActive(false);
            if (startOrb != null) startOrb.Selected = false;
        }

        public void SetSearch(string query)
        {
            query = (query ?? string.Empty).Trim();
            int matches = 0;
            if (programs != null)
                for (int i = 0; i < programs.Length; i++)
                {
                    var entry = programs[i];
                    bool show = query.Length == 0 || (!string.IsNullOrEmpty(entry.title) && entry.title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (!string.IsNullOrEmpty(entry.searchKeywords) && entry.searchKeywords.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                    show &= entry.gameSettings || entry.window != null && LingGuangV05.Desktop.Story.StudyDesktopGates.AllowsWindow(entry.window);
                    if (entry.row != null && entry.row.activeSelf != show) entry.row.SetActive(show);
                    if (show) matches++;
                }
            if (emptyMessage != null) emptyMessage.gameObject.SetActive(matches == 0);
            if (programScroll != null) programScroll.verticalNormalizedPosition = 1;
        }

        public void ShowAllPrograms()
        {
            if (search != null) search.SetTextWithoutNotify(string.Empty);
            SetSearch(string.Empty);
        }

        public void SubmitSearch(string value)
        {
            if (programs == null) return;
            for (int i = 0; i < programs.Length; i++)
                if (programs[i].row != null && programs[i].row.activeSelf && (programs[i].gameSettings || programs[i].window != null))
                {
                    if (programs[i].gameSettings) { OpenSettings(); return; }
                    programs[i].window.OpenWindow();
                    Close();
                    return;
                }
        }

        public void OpenComputer() { Open(computerWindow); }
        public void OpenSettings()
        {
            Close();
            var title = FindAnyObjectByType<CaseMenuController>();
            if (title != null)
            {
                title.OpenDesktopSettings();
                return;
            }
            var overlay = GetComponent<DesktopSettingsOverlay>() ?? gameObject.AddComponent<DesktopSettingsOverlay>();
            overlay.Open(search != null && search.textComponent != null ? search.textComponent.font : null);
        }
        public void OpenControlPanel() { Open(controlPanelWindow); }
        public void OpenUser() { Open(userWindow != null ? userWindow : controlPanelWindow); }
        private void Open(WindowManager window) { if (window != null) window.OpenWindow(); Close(); }
        public void RequestShutdown()
        {
            Close();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == LingGuangV05.Desktop.MainSceneEntry.ScenePath)
            {
                LingGuangV05.Desktop.GameExit.ReturnToTitle();
                return;
            }
            shutdown.Invoke();
        }

        public void ShowDesktop()
        {
            Close();
            if (showingDesktop)
            {
                for (int i = 0; i < desktopRestore.Count; i++)
                    if (desktopRestore[i] != null && desktopRestore[i].isOn) desktopRestore[i].OpenWindow();
                desktopRestore.Clear();
                showingDesktop = false;
                return;
            }
            desktopRestore.Clear();
            if (windows != null)
                for (int i = 0; i < windows.Length; i++)
                {
                    var window = windows[i];
                    if (window == null || !window.isOn || !window.gameObject.activeInHierarchy) continue;
                    var group = window.GetComponent<CanvasGroup>();
                    if (group == null || group.alpha <= .01f) continue;
                    desktopRestore.Add(window);
                }
            desktopRestore.Sort(CompareWindowOrder);
            for (int i = 0; i < desktopRestore.Count; i++) desktopRestore[i].MinimizeWindow();
            showingDesktop = desktopRestore.Count > 0;
        }

        private static int CompareWindowOrder(WindowManager a, WindowManager b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }
}
