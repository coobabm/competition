using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 任务大厅: the left list of the 标注台, one row per desk (open desks first, each group by pay). A row shows the
    /// desk's name with its tags (高价 / 新), what a right answer pays, the stars of its difficulty, how much of its data
    /// is left, and where the cards come from. Locked desks are greyed and say how they open. Clicking an open desk
    /// selects it; clicking a locked one explains the unlock.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        const float HallRowH = 56;

        sealed class HallRow
        {
            public string id;
            public RectTransform rt;
            public Image bg, bar;
            public TMP_Text left, pay;
        }

        RectTransform hallBox, hallContent;
        TMP_Text hallTitle, hallNote, hallPaused;
        readonly List<HallRow> hallRows = new List<HallRow>();

        void BuildHall(RectTransform parent)
        {
            hallBox = ZhongbaoSkin.Box(ui, parent, "Hall", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(HallW, 0));
            ZhongbaoSkin.Header(ui, hallBox, "任务大厅", "Task hall", out hallTitle, out hallNote);
            hallNote.text = T("按报酬排序", "by pay");
            var viewport = Rect("Viewport", hallBox, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -(ZhongbaoSkin.HeaderHeight + 1)));
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            hallContent = Rect("List", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            hallContent.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = hallContent; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            hallPaused = ui.Text(Rect("Paused", hallBox, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -60)), "", 14, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);
            hallPaused.gameObject.SetActive(false);
        }

        /// <summary>Rebuilds the hall rows (a desk opened, or the language changed).</summary>
        public void RebuildTabs()
        {
            foreach (var r in hallRows)
            {
                // The edit-mode smoke test rebuilds pages outside Play mode, where Destroy would leave stale rows behind.
                if (Application.isPlaying) UnityEngine.Object.Destroy(r.rt.gameObject); else UnityEngine.Object.DestroyImmediate(r.rt.gameObject);
            }
            hallRows.Clear(); deskIds.Clear();
            var list = TabDesks(out openTabs);
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                string id = d.id;
                var row = new HallRow { id = id };
                row.rt = Rect("Desk " + id, hallContent, new Vector2(0, 1), Vector2.one, new Vector2(0, -(i + 1) * HallRowH), new Vector2(0, -i * HallRowH));
                row.bg = Panel(row.rt, Color.white);
                var button = row.rt.gameObject.AddComponent<Button>();
                button.targetGraphic = row.bg;
                var colors = button.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(.96f, .97f, 1f); colors.pressedColor = new Color(.9f, .92f, 1f);
                colors.selectedColor = Color.white; colors.disabledColor = Color.white; colors.fadeDuration = .05f;
                button.colors = colors;
                button.onClick.AddListener(() => OnTab(id));
                if (ui.window != null) row.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = ui.window;
                row.bar = Panel(Rect("Selected", row.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), ZhongbaoSkin.Blue);
                row.bar.raycastTarget = false;
                var rule = ZhongbaoSkin.Hairline(row.rt, 0, false);
                row.left = ui.Text(Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(12, 3), new Vector2(-72, -3)), "", 14, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
                row.left.textWrappingMode = TextWrappingModes.NoWrap; row.left.overflowMode = TextOverflowModes.Ellipsis;
                row.left.lineSpacing = 6;
                row.pay = ui.Text(Rect("Pay", row.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -28), new Vector2(-10, -6)), "", 14, ZhongbaoSkin.Red, TextAlignmentOptions.TopRight);
                row.pay.textWrappingMode = TextWrappingModes.NoWrap;
                hallRows.Add(row);
                deskIds.Add(id);
                UiTip.Add(row.rt, () => Sim.DeskOpen(id) ? DeskHelp(id) : LockText(id) + "\n\n" + DeskHelp(id));
            }
            hallContent.sizeDelta = new Vector2(0, list.Count * HallRowH);
            Refresh();
        }

        /// <summary>The hall row of a desk (the audit flight lands on it).</summary>
        RectTransform HallRowOf(string id)
        {
            foreach (var r in hallRows) if (r.id == id) return r.rt;
            return null;
        }

        static string Tag(string text, Color ink, Color back) => "<mark=#" + ColorUtility.ToHtmlStringRGB(back) + "><color=#" + ColorUtility.ToHtmlStringRGB(ink) + "><size=11> " + text + " </size></color></mark>";

        /// <summary>Where a desk's cards come from.</summary>
        static string SourceNote(XgDesk desk)
        {
            switch (desk.kind)
            {
                case XgDeskKind.Logic: case XgDeskKind.Arith: return T("随机生成", "generated");
                case XgDeskKind.Text: case XgDeskKind.Poem: return T("2016 真题库", "2016 question bank");
                default: return T("程序绘制", "drawn by code");
            }
        }

        void RefreshHall(bool special)
        {
            hallContent.gameObject.SetActive(!special);
            hallPaused.gameObject.SetActive(special);
            hallNote.text = special ? "" : T("按报酬排序", "by pay");
            if (special) { hallPaused.text = T("研究或终章进行中，题桌暂时关闭。", "Research or the finale is under way: the desks are closed for now."); return; }
            string desk = Desk;
            for (int i = 0; i < hallRows.Count && i < deskIds.Count; i++)
            {
                var row = hallRows[i];
                var info = XgCatalog.Desk(row.id);
                bool open = i < openTabs;
                bool on = row.id == desk;
                row.bg.color = on && open ? ZhongbaoSkin.Blue3 : Color.white;
                row.bar.gameObject.SetActive(on && open);
                int level = Sim.LevelOf(row.id), max = XgSim.MaxLevelOf(row.id);
                double pay = Sim.ManualPayFor(row.id, level);
                if (!open)
                {
                    row.left.text = "<color=#B4B8C2>" + T("锁 " + info.name, "Locked · " + info.nameEn) + "</color>\n<size=11><color=#7A7F8C>" + T("解锁：", "Unlock: ") + Sim.DeskConditionText(info) + "</color></size>";
                    row.left.textWrappingMode = TextWrappingModes.NoWrap;
                    row.pay.text = "<color=#B4B8C2>¥" + N(pay, "0.00") + "<size=11><color=#B4B8C2>" + T("/题", "/q") + "</color></size></color>";
                    continue;
                }
                var dataset = XgCatalog.Dataset(row.id);
                bool endless = info.kind == XgDeskKind.Logic || info.kind == XgDeskKind.Arith;
                string left = endless ? T("不限", "no limit") : T("剩 ", "left ") + Math.Max(0, (long)(dataset.samples - Sim.Samples(row.id)));
                string stars = max > 1 ? "<color=#E8A317>" + new string('★', Math.Min(level, max)) + "</color><color=#B4B8C2>" + new string('☆', Math.Max(0, max - level)) + "</color>" : "<color=#E8A317>★</color>";
                string note = info.fine > 0 ? T("判对错 · 错了扣钱", "true/false · a miss costs") : SourceNote(info);
                string tags = (info.pay >= 2 ? Tag(T("高价", "high pay"), ZhongbaoSkin.Orange, ZhongbaoSkin.OrangeSoft) + " " : "")
                    + (Sim.Labels(row.id) < 1 ? Tag(T("新", "new"), ZhongbaoSkin.Red, ZhongbaoSkin.RedSoft) + " " : "");
                int waiting = Sim.ReviewCount(row.id);
                if (waiting > 0) tags += Tag(T("待复核 " + waiting, "review " + waiting), Color.white, ZhongbaoSkin.Red) + " ";
                row.left.text = "<b>" + T(info.name, info.nameEn) + "</b> " + tags + "\n<size=12>" + stars + "  <color=#7A7F8C>" + left + " · " + note + "</color></size>";
                row.pay.text = "<b>¥" + N(pay, "0.00") + "</b><size=11><color=#7A7F8C>" + T("/题", "/q") + "</color></size>";
            }
        }
    }
}
