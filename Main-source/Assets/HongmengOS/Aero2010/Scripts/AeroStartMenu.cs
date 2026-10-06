using System;
using System.Collections.Generic;
using Michsky.DreamOS;
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

        private void Update()
        {
            if (menu != null && menu.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Toggle()
        {
            if (menu == null) return;
            if (menu.activeSelf) { Close(); return; }
            menu.SetActive(true);
            menu.transform.SetAsLastSibling();
            if (panel != null && menu.transform is RectTransform bounds)
            {
                Vector2 size = panel.sizeDelta;
                size.y = Mathf.Clamp(bounds.rect.height - 66, 320, 724);
                size.x = Mathf.Min(660, Mathf.Max(400, bounds.rect.width - 16));
                panel.sizeDelta = size;
            }
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
                    show &= entry.window != null;
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
                if (programs[i].row != null && programs[i].row.activeSelf && programs[i].window != null)
                {
                    programs[i].window.OpenWindow();
                    Close();
                    return;
                }
        }

        public void OpenComputer() { Open(computerWindow); }
        public void OpenControlPanel() { Open(controlPanelWindow); }
        public void OpenUser() { Open(userWindow != null ? userWindow : controlPanelWindow); }
        private void Open(WindowManager window) { if (window != null) window.OpenWindow(); Close(); }
        public void RequestShutdown() { Close(); shutdown.Invoke(); }

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
