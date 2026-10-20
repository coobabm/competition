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
        bool hallMeshPending;
        int hallMeshFrame;
        public override void Shown() { hallMeshPending = true; hallMeshFrame = Time.frameCount; }
        void RefreshVisibleHall()
        {
            if (!hallMeshPending || Time.frameCount <= hallMeshFrame || root == null || !root.gameObject.activeInHierarchy
                || hallContent == null || hallContent.rect.width <= 0) return;
            hallMeshPending = false;
            RefreshHall(false);
            foreach (var row in hallRows) { row.left.ForceMeshUpdate(); row.pay.ForceMeshUpdate(); }
            foreach (var row in packRows) { row.left.ForceMeshUpdate(); row.pay.ForceMeshUpdate(); }
            foreach (var row in teachingRows) { row.left.ForceMeshUpdate(); row.pay.ForceMeshUpdate(); }
        }

        const float HallRowH = 56;

        sealed class HallRow
        {
            public string id;
            public RectTransform rt;
            public Image bg, bar, icon;
            public TMP_Text left, pay;
        }

        RectTransform hallBox, hallContent;
        TMP_Text hallTitle, hallNote, hallPaused;
        readonly List<HallRow> hallRows = new List<HallRow>();
        /// <summary>Bought data packs that have no desk (ImageNet, 翻译, 新闻 …): listed under the desks so a purchase always shows in the hall.</summary>
        readonly List<HallRow> packRows = new List<HallRow>();

        /// <summary>Data packs bought in 科技 whose dataset has no labelling desk.</summary>
        List<XgNode> LoosePacks()
        {
            var list = new List<XgNode>();
            if (Sim == null || !WorkContext) return list;
            foreach (var n in XgCatalog.Nodes)
                if (n.kind == XgNodeKind.Dataset && Sim.Has(n.id) && XgCatalog.Desk(n.target) == null && XgCatalog.Dataset(n.target) != null) list.Add(n);
            return list;
        }

        bool DarkTeaching => !WorkContext && ui.dark;
        Color HallInk => DarkTeaching ? XgDark.Ink : ZhongbaoSkin.Ink;
        Color HallMute => DarkTeaching ? XgDark.Muted : ZhongbaoSkin.Mute;
        Color HallFill => DarkTeaching ? XgDark.Card : Color.white;
        Color HallSelected => DarkTeaching ? XgDark.AccentSoft : ZhongbaoSkin.Blue3;

        void BuildHall(RectTransform parent)
        {
            hallBox = DarkTeaching ? ui.Card(parent, "Hall", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(HallW, 0)) : ZhongbaoSkin.Box(ui, parent, "Hall", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(HallW, 0));
            ZhongbaoSkin.Header(ui, hallBox, "任务大厅", "Task hall", out hallTitle, out hallNote);
            hallTitle.color = HallInk; hallNote.color = HallMute;
            hallNote.text = T("按报酬排序", "by pay");
            var viewport = Rect("Viewport", hallBox, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -(ZhongbaoSkin.HeaderHeight + 1)));
            BuildTeachingHallTools(viewport);
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            hallContent = Rect("List", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            hallContent.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = hallContent; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            hallPaused = ui.Text(Rect("Paused", hallBox, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -60)), "", 14, HallMute, TextAlignmentOptions.Center);
            hallPaused.gameObject.SetActive(false);
        }

        /// <summary>Rebuilds the hall rows (a desk opened, or the language changed).</summary>
        public void RebuildTabs()
        {
            teachingSortScores.Clear();
            foreach (var r in hallRows)
            {
                // The edit-mode smoke test rebuilds pages outside Play mode, where Destroy would leave stale rows behind.
                if (Application.isPlaying) UnityEngine.Object.Destroy(r.rt.gameObject); else UnityEngine.Object.DestroyImmediate(r.rt.gameObject);
            }
            hallRows.Clear(); deskIds.Clear();
            hallMeshPending = true; hallMeshFrame = Time.frameCount;
            foreach (var r in packRows)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(r.rt.gameObject); else UnityEngine.Object.DestroyImmediate(r.rt.gameObject);
            }
            packRows.Clear();
            var list = TabDesks(out openTabs);
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                string id = d.id;
                var row = new HallRow { id = id };
                row.rt = Rect("Desk " + id, hallContent, new Vector2(0, 1), Vector2.one, new Vector2(0, -(i + 1) * HallRowH), new Vector2(0, -i * HallRowH));
                row.bg = Panel(row.rt, HallFill);
                var button = row.rt.gameObject.AddComponent<Button>();
                button.targetGraphic = row.bg;
                var colors = button.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(.96f, .97f, 1f); colors.pressedColor = new Color(.9f, .92f, 1f);
                colors.selectedColor = Color.white; colors.disabledColor = Color.white; colors.fadeDuration = .05f;
                button.colors = colors;
                button.onClick.AddListener(() => OnTab(id));
                if (ui.window != null) row.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = ui.window;
                row.bar = Panel(Rect("Selected", row.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), DarkTeaching ? XgDark.Accent : ZhongbaoSkin.Blue);
                row.bar.raycastTarget = false;
                var rule = ZhongbaoSkin.Hairline(row.rt, 0, false);
                // The drawn desk icon (Tools/Icons/ui_icons.py); the text makes room for it.
                row.icon = XgIcons.Place(row.rt, "desk", id, 25, HallRowH * .5f, 32);
                row.left = ui.Text(Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(row.icon != null ? 44 : 12, 3), new Vector2(-72, -3)), "", 14, HallInk, TextAlignmentOptions.MidlineLeft);
                row.left.textWrappingMode = TextWrappingModes.NoWrap; row.left.overflowMode = TextOverflowModes.Ellipsis;
                row.left.lineSpacing = 6;
                row.pay = ui.Text(Rect("Pay", row.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -28), new Vector2(-10, -6)), "", 14, ZhongbaoSkin.Red, TextAlignmentOptions.TopRight);
                row.pay.textWrappingMode = TextWrappingModes.NoWrap;
                hallRows.Add(row);
                deskIds.Add(id);
                UiTip.Add(row.rt, () => Sim.DeskOpen(id) ? DeskHelp(id) : LockText(id) + "\n\n" + DeskHelp(id));
            }
            var packs = LoosePacks();
            for (int k = 0; k < packs.Count; k++)
            {
                var node = packs[k];
                int i = list.Count + k;
                var row = new HallRow { id = node.target };
                row.rt = Rect("Pack " + node.target, hallContent, new Vector2(0, 1), Vector2.one, new Vector2(0, -(i + 1) * HallRowH), new Vector2(0, -i * HallRowH));
                row.bg = Panel(row.rt, new Color32(248, 249, 252, 255));
                ZhongbaoSkin.Hairline(row.rt, 0, false);
                row.icon = XgIcons.Place(row.rt, "tech", node.id, 25, HallRowH * .5f, 32);
                // The full width: 「已买」 sits on the first line only and the (clipped) name stays clear of it.
                row.left = ui.Text(Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(row.icon != null ? 44 : 12, 3), new Vector2(-10, -3)), "", 14, HallInk, TextAlignmentOptions.MidlineLeft);
                row.left.textWrappingMode = TextWrappingModes.NoWrap; row.left.overflowMode = TextOverflowModes.Ellipsis;
                row.left.lineSpacing = 6;
                row.pay = ui.Text(Rect("Pay", row.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -28), new Vector2(-10, -6)), "", 12, HallMute, TextAlignmentOptions.TopRight);
                row.pay.textWrappingMode = TextWrappingModes.NoWrap;
                string target = node.target;
                var data0 = XgCatalog.Dataset(target);
                UiTip.Add(row.rt, () => "<b>" + T(data0.name, data0.nameEn) + "</b>\n"
                    + T("买来的现成数据包：已经标好了，不用上这儿标，直接拿去训练。", "A ready-made data pack: already labelled, nothing to do here; it goes straight into training.")
                    + "\n" + (Sim.Downloading(target) ? T("还在下载。", "Still downloading.") : T("已入库 ", "Stored: ") + N(Sim.Samples(target), "#,0") + T(" 条。", " samples."))
                    + "\n" + T(node.note, node.noteEn));
                packRows.Add(row);
            }
            int extra = BuildTeachingPackRows(list.Count + packs.Count);
            hallContent.sizeDelta = new Vector2(0, (list.Count + packs.Count + extra) * HallRowH);
            Refresh();
        }

        /// <summary>A name cut by hand to fit a hall row, so the row's own ellipsis never drops its second line.</summary>
        /// <paramref name="max"/> is in CJK widths; a Latin letter, digit or space counts as half of one.
        static string Clip(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return text;
            double width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                width += text[i] < 0x2E80 ? .55 : 1;
                if (width > max) return text.Substring(0, Math.Max(1, i - 1)) + "…";
            }
            return text;
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
                case XgDeskKind.Text: case XgDeskKind.Poem: return desk.id == XgMemes.SenseDesk ? T("常识题库", "common-sense bank") : T("2016 真题库", "2016 question bank");
                default: return T("程序绘制", "drawn by code");
            }
        }

        void RefreshHall(bool special)
        {
            hallTitle.text = T("任务大厅", "Task hall");
            RefreshTeachingHallTools();
            if (!WorkContext && teachingStage != Sim.S.stage) { RebuildTabs(); return; }
            RefreshTeachingPackRows();
            hallContent.gameObject.SetActive(!special);
            hallPaused.gameObject.SetActive(special);
            hallNote.text = special ? "" : T("按报酬排序", "by pay");
            if (special) { hallPaused.text = T("研究或终章进行中，题桌暂时关闭。", "Research or the finale is under way: the desks are closed for now."); return; }
            // A pack bought since the last rebuild (no desk opened, so nothing else rebuilds the hall).
            var packs = LoosePacks();
            if (packs.Count != packRows.Count) { RebuildTabs(); return; }
            for (int k = 0; k < packRows.Count; k++)
            {
                var row = packRows[k]; var node = packs[k];
                var data = XgCatalog.Dataset(node.target);
                bool loading = Sim.Downloading(node.target);
                string state = loading ? T("下载中", "downloading") : T("已入库，直接训练", "stored, trains as is");
                // The name alone on the first line: a tag there would overflow it and the ellipsis would cut the second line too.
                row.left.text = "<b>" + Clip(T(data.name, data.nameEn), Lang.English ? 8 : 6) + "</b>"
                    + "\n<size=12>" + Tag(T("数据包", "data pack"), ZhongbaoSkin.Blue, ZhongbaoSkin.Blue3) + " <color=#7A7F8C>" + state + "</color></size>";
                row.pay.text = loading ? T("下载中", "loading") : T("已买", "owned");
            }
            string desk = Desk;
            for (int i = 0; i < hallRows.Count && i < deskIds.Count; i++)
            {
                var row = hallRows[i];
                var info = XgCatalog.Desk(row.id);
                bool open = i < openTabs;
                bool on = row.id == desk && (WorkContext || string.IsNullOrEmpty(Sim.S.teachingPack));
                row.bg.color = on && open ? HallSelected : HallFill;
                row.bar.gameObject.SetActive(on && open);
                if (row.icon != null) row.icon.color = open ? Color.white : new Color(1, 1, 1, .35f);
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
                string left = WorkContext ? T("公司任务", "Company tasks") : endless ? T("不限", "no limit") : T("剩 ", "left ") + Math.Max(0, (long)(dataset.samples - Sim.Samples(row.id)));
                string stars = max > 1 ? "<color=#E8A317>" + new string('★', Math.Min(level, max)) + "</color><color=#B4B8C2>" + new string('☆', Math.Max(0, max - level)) + "</color>" : "<color=#E8A317>★</color>";
                string note = T("判对错 · 错了扣钱", "true/false · a miss costs");
                string tags = (info.pay >= 2 ? Tag(T("高价", "high pay"), ZhongbaoSkin.Orange, ZhongbaoSkin.OrangeSoft) + " " : "")
                    + (Sim.Labels(row.id) < 1 ? Tag(T("新", "new"), ZhongbaoSkin.Red, ZhongbaoSkin.RedSoft) + " " : "");
                // A bought pack leads the second line (the first is narrow and ends in an ellipsis).
                string packTag = Sim.Has(row.id + ".pack") ? Tag(T("数据包", "data pack"), ZhongbaoSkin.Blue, ZhongbaoSkin.Blue3) + " " : "";
                int waiting = WorkContext ? 0 : Sim.ReviewCount(row.id);
                if (waiting > 0) tags += Tag(T("待复核 " + waiting, "review " + waiting), Color.white, ZhongbaoSkin.Red) + " ";
                row.left.text = "<b>" + T(info.name, info.nameEn) + "</b> " + tags + "\n<size=12>" + packTag + stars + "  <color=#7A7F8C>" + left + " · " + note + "</color></size>";
                row.pay.text = "<b>¥" + N(pay, "0.00") + "</b><size=11><color=#7A7F8C>" + T("/题", "/q") + "</color></size>";
            }
        }
    }
}
