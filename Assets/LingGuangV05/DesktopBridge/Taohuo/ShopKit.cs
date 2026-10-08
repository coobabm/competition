using System;
using DesktopArt = LingGuangV05.Desktop.Media.DesktopMedia;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// What 淘货 and 喵鱼 share: the household simulation and calendar, the lab's stage, and small uGUI helpers
    /// (texts, flat buttons, a vertical scroll list, a status bar). Both shops rebuild their list when what they
    /// show changes and only refresh money-dependent buttons in between.
    /// </summary>
    public abstract class ShopView : MonoBehaviour
    {
        protected WindowManager window;
        protected TMP_FontAsset font;
        protected RectTransform root;
        protected TMP_Text status;
        float statusUntil;
        ChapterOneRuntime runtime;
        XingGuangController lab;
        float nextLookup;

        protected static string T(string zh, string en) => GameText.T(zh, en);
        protected static string Money(double v) => "¥" + Math.Round(v).ToString("#,0", CultureInfo.InvariantCulture);

        protected ChapterOneSim Sim
        {
            get
            {
                if (runtime == null && Time.unscaledTime >= nextLookup) { nextLookup = Time.unscaledTime + 1; runtime = FindAnyObjectByType<ChapterOneRuntime>(); }
                return runtime != null && !runtime.TestMode ? runtime.Sim : null;
            }
        }

        protected void Dirty() { if (runtime != null) runtime.MarkDirty(); }

        /// <summary>The desktop's calendar day (the prologue day when there is no save yet).</summary>
        protected DateTime Today { get { var s = Sim; return s != null ? GameCalendar.Now(s.S).Date : GameCalendar.Start.Date; } }

        /// <summary>The lab's stage (1 before 灵光 exists).</summary>
        protected int Stage
        {
            get
            {
                if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
                return lab != null && lab.Sim != null ? lab.Sim.S.stage : 1;
            }
        }

        protected XgSim Lab { get { if (lab == null) lab = FindAnyObjectByType<XingGuangController>(); return lab != null ? lab.Sim : null; } }

        // ───────────── ui primitives ─────────────

        protected TMP_Text Text(RectTransform rt, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.richText = true; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        protected TMP_Text Label(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Text(PrologueDesk.Rect(name, parent, min, max, offMin, offMax), text, size, color, align);
            return t;
        }

        protected Button Btn(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color fill, string text, float size, Color ink, Action click, out TMP_Text label)
        {
            var rt = PrologueDesk.Rect(name, parent, min, max, offMin, offMax);
            var img = PrologueDesk.Fill(rt, fill);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var colors = b.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1); colors.pressedColor = new Color(.85f, .85f, .85f, 1); colors.disabledColor = new Color(.75f, .75f, .75f, .8f); b.colors = colors;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            if (click != null) b.onClick.AddListener(() => click());
            label = Text(PrologueDesk.Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0)), text, size, ink, TextAlignmentOptions.Center);
            Focus(rt.gameObject);
            return b;
        }

        protected void Focus(GameObject target)
        {
            var f = target.GetComponent<ChapterOneWindowFocus>() ?? target.AddComponent<ChapterOneWindowFocus>();
            f.Window = window;
        }

        /// <summary>A vertical list filling <paramref name="area"/>; returns the content to add rows to.</summary>
        protected RectTransform Scroll(RectTransform area, int padding = 14, float spacing = 10)
        {
            var sr = area.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.scrollSensitivity = 30; sr.movementType = ScrollRect.MovementType.Clamped;
            var viewport = PrologueDesk.Rect("Viewport", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            PrologueDesk.Fill(viewport, new Color(0, 0, 0, 0));
            Focus(viewport.gameObject);
            var content = PrologueDesk.Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding); layout.spacing = spacing;
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = viewport; sr.content = content;
            return content;
        }

        /// <summary>A fixed-height row in a scroll list.</summary>
        protected RectTransform Row(RectTransform content, string name, float height, Color fill)
        {
            var rt = PrologueDesk.Rect(name, content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            PrologueDesk.Fill(rt, fill);
            Focus(rt.gameObject);
            return rt;
        }

        protected static void Clear(RectTransform content)
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        }

        /// <summary>A line in the status bar; it fades back to the idle line after a few seconds.</summary>
        protected void Say(string text, float seconds = 6)
        {
            if (status == null) return;
            status.text = text; statusUntil = Time.unscaledTime + seconds;
        }

        protected bool StatusBusy => Time.unscaledTime < statusUntil;

        /// <summary>
        /// Runs a household command and shows its message. The simulation's own messages are Chinese; an English
        /// player sees <paramref name="okEn"/> on success and a short reason on failure.
        /// </summary>
        protected bool Run(Func<ChapterOneSim, bool> command, string okEn)
        {
            var sim = Sim;
            if (sim == null) { Say(Lang.T("电脑还没准备好。")); return false; }
            bool ok = command(sim);
            Say(!GameText.IsEnglish ? sim.LastMessage : ok ? okEn : "Not possible: " + Reason(sim));
            if (ok) Dirty();
            return ok;
        }

        /// <summary>English gist of why a purchase failed.</summary>
        static string Reason(ChapterOneSim sim)
        {
            string m = sim.LastMessage ?? "";
            if (m.Contains("插槽")) return "every slot is full. Sell a card on " + AppNames.UsedEn + " or add a case.";
            if (m.Contains("经费")) return "not enough money.";
            if (m.Contains("显存")) return "the board would not fit in the remaining VRAM.";
            if (m.Contains("至少")) return "keep at least one card.";
            if (m.Contains("到货") || m.Contains("上市") || m.Contains("月")) return "not in stock yet.";
            return "the shop said no.";
        }

        /// <summary>Product-specific reference artwork; preserve the full object and the text fallback.</summary>
        protected bool ProductIllustration(RectTransform parent, string key, string caption = "")
        {
            var image = DesktopArt.Paint(parent, key, false);
            if (image == null) return false;
            image.rectTransform.offsetMin = new Vector2(3, string.IsNullOrEmpty(caption) ? 3 : 36);
            image.rectTransform.offsetMax = new Vector2(-3, -3);
            if (!string.IsNullOrEmpty(caption))
            {
                var strip = PrologueDesk.Rect("Caption", parent, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 34));
                PrologueDesk.Fill(strip, new Color(0.06f, .09f, .14f, .9f), false);
                Label(strip, "Text", Vector2.zero, Vector2.one, new Vector2(3, 1), new Vector2(-3, -1), caption, 12, Color.white, TextAlignmentOptions.Center);
            }
            return true;
        }

        /// <summary>The card model's reference artwork, with a labelled fallback if its asset is unavailable.</summary>
        protected RectTransform CardPicture(Transform parent, GpuModel g, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var rt = PrologueDesk.Rect("Picture", parent, min, max, offMin, offMax);
            string model = g == null ? "GPU" : g.name;
            if (g != null && ProductIllustration(rt, "gpu_" + g.id, "<b>" + model + "</b>\n" + T(g.brand, g.brandEn))) return rt;
            bool red = g != null && g.id == HardwareCatalog.Rx480;
            bool titan = g != null && g.id == HardwareCatalog.TitanXp;
            PrologueDesk.Fill(rt, red ? new Color32(150, 20, 24, 255) : titan ? new Color32(30, 30, 34, 255) : new Color32(40, 44, 48, 255), false);
            var stripe = PrologueDesk.Rect("Stripe", rt, new Vector2(0, .18f), new Vector2(1, .26f), Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(stripe, red ? new Color32(240, 70, 60, 255) : new Color32(118, 185, 0, 255), false);
            string shortName = g == null ? "?" : g.name.Replace("GTX ", "").Replace(" 2G", "").Replace(" 4G", "").Replace(" 6G", "").Replace(" 8G", "").Replace(" 12G", "").Replace("TITAN X (Pascal)", "TITAN X");
            Label(rt, "Name", new Vector2(0, .3f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -6), "<b>" + shortName + "</b>", shortName.Length > 6 ? 18 : 26, Color.white, TextAlignmentOptions.Center);
            Label(rt, "Brand", Vector2.zero, new Vector2(1, .18f), new Vector2(4, 0), new Vector2(-4, 0), g == null ? "" : T(g.brand, g.brandEn), 12, new Color(1, 1, 1, .8f), TextAlignmentOptions.Center);
            return rt;
        }

        protected static string Stats(GpuModel g)
        {
            return Lang.T("算力 ×") + g.compute.ToString("0.##", CultureInfo.InvariantCulture) + "  ·  " + Lang.T("显存 ") + (g.vramMB / 1024).ToString("0.#", CultureInfo.InvariantCulture) + "G  ·  " + g.watts.ToString("0") + "W";
        }

        /// <summary>The rig in one block: cards, slots, compute, VRAM and power.</summary>
        protected static string RigSummary(ChapterOneSim sim)
        {
            if (sim == null) return "";
            var sb = new System.Text.StringBuilder();
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (string id in sim.Cards) { if (!counts.ContainsKey(id)) { counts[id] = 0; order.Add(id); } counts[id]++; }
            foreach (string id in order)
            {
                var g = HardwareCatalog.Gpu(id);
                sb.Append("· ").Append(g != null ? T(g.name, g.nameEn) : id);
                if (counts[id] > 1) sb.Append(" ×").Append(counts[id]);
                sb.Append('\n');
            }
            if (sim.S.nvme) sb.Append("· ").Append(Lang.T("三星 950 Pro NVMe")).Append('\n');
            sb.Append('\n');
            sb.Append(Lang.T("插槽 ")).Append(sim.S.gpuCount).Append(" / ").Append(sim.Slots).Append(Lang.T("（机箱 ")).Append(sim.S.caseCount).Append(Lang.T("）\n"));
            sb.Append(Lang.T("算力 ×")).Append(sim.CardCompute.ToString("0.##", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append(Lang.T("显存 ")).Append((sim.MemoryCapacity / 1024).ToString("0.#", CultureInfo.InvariantCulture)).Append("G\n");
            sb.Append(Lang.T("功耗 ")).Append(sim.LoadWatts.ToString("0")).Append(" / ").Append(sim.Config.powerLimitWatts.ToString("0")).Append("W");
            if (sim.S.breakerTripped) sb.Append(Lang.T("  <color=#D03030>已跳闸</color>"));
            sb.Append('\n').Append(Lang.T("温度 ")).Append(sim.S.temperature.ToString("0")).Append("°C");
            return sb.ToString();
        }

        /// <summary>Rebrands a native window: its title bar, desktop icon, taskbar button and Start-menu row.</summary>
        public static void Rebrand(WindowManager window, string[] oldNames, string name, Sprite sprite, string statusLine, params GameObject[] icons)
        {
            if (window == null) return;
            bool Old(string text) { if (string.IsNullOrEmpty(text)) return false; foreach (var o in oldNames) if (text.Trim() == o) return true; return false; }
            void Retext(GameObject entry)
            {
                if (entry == null) return;
                foreach (var c in entry.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    string type = c.GetType().Name;
                    if (type == "DesktopLocalizedText" || type == "AppElement" || type == "LocalizedObject") c.enabled = false;
                }
                var bm = entry.GetComponent<ButtonManager>();
                if (bm != null) { bm.buttonText = name; if (sprite != null) bm.buttonIcon = sprite; bm.UpdateUI(); }
                foreach (var t in entry.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name == "Title" || t.name == "Text" || t.name == "ChapterOneDesktopLabel" || t.name == "Label" || Old(t.text)) t.text = name;
                if (sprite != null) foreach (var img in entry.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") img.sprite = sprite;
            }
            var content = window.windowContainer != null ? window.windowContainer.Find("Content") : null;
            foreach (var t in window.GetComponentsInChildren<TMP_Text>(true))
            {
                if (content != null && t.transform.IsChildOf(content)) continue;
                bool title = t.name == "Aero Window Title" || t.name == "ChapterOneTitle" || Old(t.text) || (t.text != null && t.text.StartsWith("寻宝", StringComparison.Ordinal));
                bool line = t.name == "Status";
                if (!title && !(line && statusLine != null)) continue;
                var localized = t.GetComponent("DesktopLocalizedText") as Behaviour;
                if (localized != null) localized.enabled = false;
                t.text = title ? name : statusLine;
            }
            foreach (var icon in icons) Retext(icon);
            if (window.taskbarButton != null) Retext(window.taskbarButton.gameObject);
            var menu = FindAnyObjectByType<HongmengOS.Aero2010.AeroStartMenu>(FindObjectsInactive.Include);
            if (menu != null && menu.programs != null)
                for (int i = 0; i < menu.programs.Length; i++)
                {
                    var entry = menu.programs[i];
                    if (entry.window != window) continue;
                    entry.title = name;
                    entry.searchKeywords = (entry.searchKeywords ?? "") + " " + name;
                    menu.programs[i] = entry;
                    if (entry.row != null) foreach (var t in entry.row.GetComponentsInChildren<TMP_Text>(true)) if (Old(t.text) || t.name == "Title" || t.name == "Text") t.text = name;
                }
        }
    }
}
