using System;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop
{
    public sealed class SaveSlotsPanel : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        XgUi ui;
        RectTransform overlay, card;
        TMP_Text status;
        readonly TMP_Text[] descriptions = new TMP_Text[6];
        readonly XgBtn[] loads = new XgBtn[6], chapters = new XgBtn[6];
        Action pending;
        XgBtn confirm;
        XgBtn settingsSave, settingsFunds;
        bool settingsEnglish;
        bool installed, tickBefore;
        float retry;
        public static bool IsOpen { get; private set; }
        static string T(string zh, string en) => GameText.T(zh, en);

        public static void Attach(ChapterOneRuntime runtime)
        {
            if (runtime != null && runtime.GetComponent<SaveSlotsPanel>() == null) runtime.gameObject.AddComponent<SaveSlotsPanel>();
        }
        void Awake() { runtime = GetComponent<ChapterOneRuntime>(); }
        void OnDestroy() { IsOpen = false; }
        void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            if (installed)
            {
                if (settingsEnglish != GameText.IsEnglish) RefreshSettingsLabels();
                return;
            }
            if (Time.unscaledTime < retry) return;
            retry = Time.unscaledTime + 1;
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == "Settings");
            if (window?.windowContent == null) return;
            ui = new XgUi(PrologueDesk.CjkFont(), window);
            // Compact actions belong to the client area, below the native caption and its controls.
            var actions = Rect("GameActions", window.windowContent, Vector2.one, Vector2.one,
                new Vector2(-276, -40), new Vector2(-12, -12));
            settingsFunds = ui.Button(actions, "", () =>
            {
                var funds = runtime.GetComponent<FundsPanel>() ?? runtime.gameObject.AddComponent<FundsPanel>();
                funds.Open();
            }, 12);
            settingsFunds.rt.name = "FundsSettingsButton";
            settingsFunds.rt.anchorMin = Vector2.zero; settingsFunds.rt.anchorMax = new Vector2(0, 1);
            settingsFunds.rt.offsetMin = Vector2.zero; settingsFunds.rt.offsetMax = new Vector2(116, 0);
            settingsSave = ui.Button(actions, "", Open, 12);
            settingsSave.rt.name = "SaveSlotsSettingsButton";
            settingsSave.rt.anchorMin = new Vector2(1, 0); settingsSave.rt.anchorMax = Vector2.one;
            settingsSave.rt.offsetMin = new Vector2(-140, 0); settingsSave.rt.offsetMax = Vector2.zero;
            actions.SetAsLastSibling(); installed = true;
            RefreshSettingsLabels();
        }
        void RefreshSettingsLabels()
        {
            settingsEnglish = GameText.IsEnglish;
            settingsFunds.Set(T("资金账本", "Funds ledger"), true);
            settingsSave.Set(T("存档与章节", "Saves & chapters"), true);
        }
        void Build()
        {
            var canvasObject = new GameObject("SaveSlotsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 31000;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366, 900); scaler.matchWidthOrHeight = .5f;
            overlay = Rect("SaveSlots", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(overlay, new Color(0, 0, 0, .75f));
            card = Rect("Card", overlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-420, -330), new Vector2(420, 330));
            Panel(card, XgPalette.Page);
            Text("Title", 24, 15, 650, 36, T("存档与章节", "Saves & chapters"), 26);
            Text("Hint", 24, 58, 770, 38, T("第 1 档每五分钟自动保存；章节保留第一次解锁时的进度。", "Slot 1 saves every five minutes. Chapters keep the first unlock snapshot."), 15);
            Button("Close", 778, 16, 40, 36, "×", Close);
            for (int i = 0; i < 6; i++) {
                int slot = i + 1; float y = 108 + i * 59;
                descriptions[i] = Text("Slot" + slot, 24, y, 535, 46, "", 15);
                if (slot == 1) Button("SaveNow", 575, y + 4, 102, 34, T("立即保存", "Save now"), () => { runtime.SaveNow(); Refresh(); });
                else Button("Save" + slot, 575, y + 4, 102, 34, T("保存", "Save"), () => Request(T("覆盖这个档位？再次确认后保存。", "Overwrite this slot? Confirm to save."), () => { runtime.SaveSlot(slot); Refresh(); }));
                loads[i] = Button("Load" + slot, 692, y + 4, 102, 34, T("读取", "Load"), () => Request(T("读取会替换当前进度，请先保存。", "Loading replaces current progress. Save first."), () => { if (runtime.LoadArchive(slot)) Close(); else Refresh(); }));
            }
            Text("ChapterTitle", 24, 468, 780, 24, T("章节回顾 · 首次解锁后永久保留", "Chapters · first-unlock snapshots are kept"), 17);
            for (int i = 0; i < 6; i++) { int chapter = i + 1; chapters[i] = Button("Chapter" + chapter, 24 + i * 130, 503, 120, 38,
                T("第 " + chapter + " 章", "Chapter " + chapter), () => Request(T("回到这一章第一次开始时的进度？", "Return to the first start of this chapter?"), () => { if (runtime.LoadArchive(chapter, true)) Close(); else Refresh(); })); }
            status = Text("Status", 24, 557, 560, 69, "", 14);
            confirm = Button("Confirm", 608, 566, 188, 44, T("确认操作", "Confirm"), () => { var action = pending; pending = null; confirm.Show(false); action?.Invoke(); });
        }
        TMP_Text Text(string name, float x, float y, float w, float h, string value, float size)
            => ui.Text(At(name, x, y, w, h), value, size, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
        RectTransform At(string name, float x, float y, float w, float h)
            => Rect(name, card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - h), new Vector2(x + w, -y));
        XgBtn Button(string name, float x, float y, float w, float h, string title, UnityEngine.Events.UnityAction action)
        {
            var b = ui.Button(card, title, action, 15); b.rt.name = name;
            b.rt.anchorMin = b.rt.anchorMax = new Vector2(0, 1); b.rt.offsetMin = new Vector2(x, -y - h); b.rt.offsetMax = new Vector2(x + w, -y);
            return b;
        }
        void Request(string text, Action action) { status.text = text; pending = action; confirm.Show(true); }
        public void Open()
        {
            if (runtime.Sim.InPrologue || ui == null) return;
            if (overlay == null) Build();
            tickBefore = runtime.tickEnabled; runtime.tickEnabled = false; IsOpen = true;
            overlay.gameObject.SetActive(true); Refresh();
        }
        void Refresh()
        {
            pending = null; confirm.Show(false); status.text = runtime.SaveStatus;
            for (int i = 0; i < 6; i++) {
                descriptions[i].text = (i == 0 ? T("1 · 自动存档", "1 · Autosave") : T((i + 1) + " · 手动存档", (i + 1) + " · Manual")) + "\n" + runtime.ArchiveDescription(i + 1);
                loads[i].button.interactable = runtime.ArchiveExists(i + 1);
                chapters[i].button.interactable = runtime.ArchiveExists(i + 1, true);
            }
        }
        void Close()
        {
            if (!IsOpen) return;
            overlay.gameObject.SetActive(false); runtime.tickEnabled = tickBefore; IsOpen = false; pending = null;
        }
    }
}
