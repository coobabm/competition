using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 养成 › 背包: every rare item the player owns (稀有道具), up to twelve equipped slots at the top of the scroll view (big tiles, a dashed box when empty),
    /// the owned permanent items read-only with a link to 道具, and the collection book (an item not yet seen reads 「？？？」).
    /// Click an item to equip or unequip it; with every unlocked slot filled, the click asks which slot to replace. Only equipped items work.
    /// Presentation only: the rules are in XgSim.Relics.cs.
    /// </summary>
    public sealed class XgBackpackPage : XgPage
    {
        const float HeaderHeight = 48, SlotHeight = 118, Gap = 10, TileHeight = 74, ItemHeight = 34, SectionHead = 32;

        sealed class SlotView
        {
            public RectTransform rt;
            public XgFrameGraphic frame, glyphBox;
            public TMP_Text glyph, name, rarity, effect, empty;
        }

        sealed class Tile
        {
            public string id;
            public RectTransform rt;
            public XgFrameGraphic frame, glyphBox;
            public TMP_Text glyph, name, effect, mark;
        }

        TMP_Text header, motto;
        readonly SlotView[] slots = new SlotView[XgRelics.MaxSlots];
        readonly List<Tile> owned = new List<Tile>();
        RectTransform content, chooser;
        TMP_Text chooserTitle;
        XgBtn chooserCancel;
        readonly XgBtn[] chooserButtons = new XgBtn[XgRelics.MaxSlots];
        string chooserFor = "", signature = "";
        XgSim shownSim, chooserSim;
        readonly HashSet<string> fresh = new HashSet<string>();

        public override void Build(RectTransform area)
        {
            root = Rect("backpack", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 0, 24, 2, 2), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            header.textWrappingMode = TextWrappingModes.NoWrap; header.characterSpacing = 2;
            motto = ui.Text(Rect("Motto", root, new Vector2(0, 1), Vector2.one, new Vector2(2, -HeaderHeight), new Vector2(-2, -24)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            motto.textWrappingMode = TextWrappingModes.NoWrap; motto.overflowMode = TextOverflowModes.Ellipsis;

            // Equipped and locked slots share the scroll view, so all twelve fit small windows.
            var list = Rect("List", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -HeaderHeight - 8));
            list.gameObject.AddComponent<RectMask2D>();
            Panel(list, new Color(0, 0, 0, 0));
            content = Rect("Content", list, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = list; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30; scroll.inertia = false;
        }

        SlotView MakeSlot(RectTransform row, int index)
        {
            var s = new SlotView();
            int col = index % 4; float top = (index / 4) * (SlotHeight + Gap);
            s.rt = Rect("Slot" + (index + 1), row, new Vector2(col / 4f, 1), new Vector2((col + 1) / 4f, 1), new Vector2(col == 0 ? 0 : Gap / 2, -top - SlotHeight), new Vector2(col == 3 ? 0 : -Gap / 2, -top));
            s.frame = s.rt.gameObject.AddComponent<XgFrameGraphic>();
            var button = s.rt.gameObject.AddComponent<Button>();
            button.targetGraphic = s.frame; button.transition = Selectable.Transition.None;
            int at = index;
            button.onClick.AddListener(() => OnSlotClick(at));
            var box = Rect("Mark", s.rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -64), new Vector2(64, -12));
            s.glyphBox = box.gameObject.AddComponent<XgFrameGraphic>(); s.glyphBox.raycastTarget = false;
            s.glyph = ui.Text(Rect("Glyph", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 26, XgDark.Ink, TextAlignmentOptions.Center);
            s.glyph.textWrappingMode = TextWrappingModes.NoWrap; s.glyph.fontStyle = FontStyles.Bold;
            s.name = ui.Text(Rect("Name", s.rt, new Vector2(0, 1), Vector2.one, new Vector2(76, -42), new Vector2(-10, -10)), "", 17, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            s.name.textWrappingMode = TextWrappingModes.NoWrap; s.name.overflowMode = TextOverflowModes.Ellipsis; s.name.fontStyle = FontStyles.Bold;
            s.rarity = ui.Text(Rect("Rarity", s.rt, new Vector2(0, 1), Vector2.one, new Vector2(76, -64), new Vector2(-10, -42)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            s.rarity.textWrappingMode = TextWrappingModes.NoWrap;
            s.effect = ui.Text(Rect("Effect", s.rt, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-10, -68)), "", 14, XgDark.Money, TextAlignmentOptions.TopLeft);
            s.effect.overflowMode = TextOverflowModes.Ellipsis;
            s.empty = ui.Text(Rect("Empty", s.rt, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8)), "", 14, XgDark.Dim, TextAlignmentOptions.Center);
            UiTip.Add(s.rt, () => { if (index >= Sim.RelicSlotCapacity) return LockedSlotText(index); var r = Sim.RelicInSlot(index); return r == null ? XgRelicUi.EmptyTip(Sim) : XgRelicUi.Tip(Sim, r) + "\n" + Hex(XgDark.Muted) + T("点一下卸下", "Click to unequip") + "</color>"; });
            return s;
        }

        public override void Shown()
        {
            CloseChooser();
            shownSim = Sim;
            // Remember what was new on arrival so those tiles keep their mark during this visit, then clear the dot.
            fresh.Clear();
            if (Sim != null) { foreach (var id in Sim.S.relicFresh) fresh.Add(id); Sim.MarkRelicsViewed(); }
            signature = "";
        }

        public override void Refresh()
        {
            if (Sim == null || content == null) return;
            if (!ReferenceEquals(shownSim, Sim))
            {
                shownSim = Sim;
                CloseChooser();
                fresh.Clear();
                signature = "";
            }
            // Arrivals while this page is open count as seen at once, but keep their "new" mark for this visit.
            if (Sim.RelicsFresh) { foreach (var id in Sim.S.relicFresh) fresh.Add(id); Sim.MarkRelicsViewed(); }
            string sig = BuildSignature() + "|" + Sim.AiDisplayName;
            if (sig != signature) { signature = sig; Rebuild(); }
            int total = XgRelics.All.Length, have = Sim.S.relicOwned.Count;
            header.text = T("背包 · 稀有道具", "BACKPACK · rare items") + "  <color=#3A5566>" + T("拥有 ", "owned ") + have + "/" + total + T(" · 装备 ", " · equipped ") + Sim.RelicsEquippedCount + "/" + Sim.RelicSlotCapacity + "</color>";
            motto.text = T("装备上限 " + Sim.RelicSlotCapacity + " · 可随时更换", "Up to " + Sim.RelicSlotCapacity + " equipped · swap any time");
            for (int i = 0; i < slots.Length; i++) PaintSlot(slots[i], Sim.RelicInSlot(i), i);
            foreach (var t in owned) PaintOwned(t);
            if (chooser != null && chooser.gameObject.activeSelf) PaintChooser();
        }

        string BuildSignature()
        {
            var sb = new StringBuilder();
            sb.Append(Sim.English ? 'e' : 'z').Append(Sim.RelicSlotCapacity);
            foreach (var id in Sim.S.relicOwned) sb.Append(id).Append(',');
            sb.Append('|');
            foreach (var id in Sim.S.relicSeen) sb.Append(id).Append(',');
            sb.Append('|');
            foreach (var def in XgItems.All()) if (Sim.Has(def.id)) sb.Append(def.id).Append(',');
            return sb.ToString();
        }

        void PaintSlot(SlotView s, XgRelic r, int index)
        {
            bool has = r != null;
            s.name.gameObject.SetActive(has); s.rarity.gameObject.SetActive(has); s.effect.gameObject.SetActive(has);
            s.glyphBox.gameObject.SetActive(has);
            s.empty.gameObject.SetActive(!has);
            if (!has)
            {
                s.frame.Style(new Color32(9, 15, 22, 255), XgDark.Dim, true, 2);
                s.empty.text = index >= Sim.RelicSlotCapacity ? LockedSlotText(index) : T("空位\n在下面点一件道具装备", "Empty\nClick an item below to equip it");
                return;
            }
            var edge = XgRelicUi.RarityColor(r.rarity);
            s.frame.Style(new Color(edge.r * .12f, edge.g * .12f, edge.b * .12f, 1f), edge, false, 2);
            XgRelicUi.PaintTile(s.glyphBox, s.glyph, r);
            s.name.text = XgRelics.Name(r, Sim.English);
            s.rarity.text = Hex(edge) + XgRelics.RarityName(r.rarity, Sim.English) + "</color>";
            s.effect.text = XgRelics.Effect(r, Sim.English).Replace("灵光", Sim.AiDisplayName);
        }

        // ───────────── the scrolling sections ─────────────

        void Rebuild()
        {
            // Self-destroying graphics under a RectMask2D: switch them off before they go (XgGuideHighlight explains the bug).
            foreach (Transform child in content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            owned.Clear();
            for (int i = 0; i < slots.Length; i++) slots[i] = MakeSlot(content, i);
            float y = 3 * (SlotHeight + Gap);
            // 1. owned rare items
            y = Section(y, T("拥有的稀有道具", "Rare items you own"), Sim.S.relicOwned.Count + "", null);
            var mine = new List<XgRelic>();
            foreach (var id in Sim.S.relicOwned) { var r = XgRelics.Get(id); if (r != null) mine.Add(r); }
            mine.Sort((a, b) => a.rarity != b.rarity ? b.rarity.CompareTo(a.rarity) : Array.IndexOf(XgRelics.All, a).CompareTo(Array.IndexOf(XgRelics.All, b)));
            if (mine.Count == 0)
            {
                var none = ui.Text(Strip("None", content, y, 30, 4, 4), T(("还没有。灵光学会新东西时，三选一里挑一件；小游戏、蜘蛛和新纪录也偶尔会掉。" .Replace("灵光", Sim.AiDisplayName)), ("None yet. Pick one when 灵光 learns something new; mini-games, the spider and new records drop one now and then." .Replace("灵光", Sim.AiDisplayName))), 14, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
                none.textWrappingMode = TextWrappingModes.NoWrap;
                y += 36;
            }
            for (int k = 0; k < mine.Count; k++) owned.Add(MakeTile(mine[k], k % 4, y + (k / 4) * (TileHeight + Gap), false, true));
            y += ((mine.Count + 3) / 4) * (TileHeight + Gap) + 6;

            // 2. the other things owned, read-only
            var haveItems = new List<string>();
            foreach (var def in XgItems.All()) { var node = XgCatalog.Node(def.id); if (node != null && Sim.Has(def.id)) haveItems.Add(Sim.NodeName(node)); }
            y = Section(y, T("其他物品 · 只读", "Other items · read only"), haveItems.Count + "", T("去道具页 ↗", "Go to Items ↗"), () => { Fx.Play(XgJuice.Sfx.Id.Click); host.ShowTab("items"); });
            if (haveItems.Count == 0)
            {
                var none = ui.Text(Strip("NoItems", content, y, 30, 4, 4), T("还没买过结构和技巧。", "No structures or techniques bought yet."), 14, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
                none.textWrappingMode = TextWrappingModes.NoWrap;
                y += 36;
            }
            for (int k = 0; k < haveItems.Count; k++)
            {
                int col = k % 4;
                float top = y + (k / 4) * (ItemHeight + 6);
                var rt = Rect("Item" + k, content, new Vector2(col / 4f, 1), new Vector2((col + 1) / 4f, 1), new Vector2(col == 0 ? 0 : Gap / 2, -top - ItemHeight), new Vector2(col == 3 ? 0 : -Gap / 2, -top));
                var frame = rt.gameObject.AddComponent<XgFrameGraphic>(); frame.raycastTarget = false;
                frame.Style(XgDark.Card, XgDark.Line);
                var text = ui.Text(Rect("Name", rt, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-8, 0)), haveItems[k], 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
            }
            y += ((haveItems.Count + 3) / 4) * (ItemHeight + 6) + 10;

            // 3. the collection book
            int seen = Sim.S.relicSeen.Count;
            y = Section(y, T("图鉴 · 稀有道具", "Collection · rare items"), seen + "/" + XgRelics.All.Length, null);
            for (int k = 0; k < XgRelics.All.Length; k++) MakeTile(XgRelics.All[k], k % 4, y + (k / 4) * (TileHeight + Gap), !Sim.RelicSeen(XgRelics.All[k].id), false);
            y += ((XgRelics.All.Length + 3) / 4) * (TileHeight + Gap) + 6;
            content.sizeDelta = new Vector2(0, y);
        }

        float Section(float y, string title, string count, string link, Action onLink = null)
        {
            var t = ui.Text(Strip("Section " + title, content, y, SectionHead, 2, 2), title + "  <color=#3A5566>" + count + "</color>", 13, XgDark.Muted, TextAlignmentOptions.BottomLeft);
            t.characterSpacing = 4; t.textWrappingMode = TextWrappingModes.NoWrap;
            if (link != null)
            {
                var b = ui.Button(content, "", () => onLink?.Invoke(), 13);
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(-170, -y - SectionHead + 2); b.rt.offsetMax = new Vector2(0, -y - 2);
                b.Set(link, true, null, XgDark.Link);
            }
            return y + SectionHead + 6;
        }

        Tile MakeTile(XgRelic r, int col, float top, bool unknown, bool equipable)
        {
            var t = new Tile { id = r.id };
            t.rt = Rect("Relic " + r.id, content, new Vector2(col / 4f, 1), new Vector2((col + 1) / 4f, 1), new Vector2(col == 0 ? 0 : Gap / 2, -top - TileHeight), new Vector2(col == 3 ? 0 : -Gap / 2, -top));
            t.frame = t.rt.gameObject.AddComponent<XgFrameGraphic>();
            t.frame.raycastTarget = equipable;
            var box = Rect("Mark", t.rt, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, -26), new Vector2(62, 26));
            t.glyphBox = box.gameObject.AddComponent<XgFrameGraphic>(); t.glyphBox.raycastTarget = false;
            t.glyph = ui.Text(Rect("Glyph", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 20, XgDark.Ink, TextAlignmentOptions.Center);
            t.glyph.textWrappingMode = TextWrappingModes.NoWrap; t.glyph.fontStyle = FontStyles.Bold;
            t.name = ui.Text(Rect("Name", t.rt, new Vector2(0, 1), Vector2.one, new Vector2(72, -30), new Vector2(-66, -6)), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            t.name.textWrappingMode = TextWrappingModes.NoWrap; t.name.overflowMode = TextOverflowModes.Ellipsis;
            t.effect = ui.Text(Rect("Effect", t.rt, Vector2.zero, Vector2.one, new Vector2(72, 6), new Vector2(-8, -30)), "", 13, XgDark.Muted, TextAlignmentOptions.TopLeft);
            t.effect.overflowMode = TextOverflowModes.Ellipsis;
            t.mark = ui.Text(Rect("State", t.rt, new Vector2(1, 1), Vector2.one, new Vector2(-70, -26), new Vector2(-6, -4)), "", 13, XgDark.Good, TextAlignmentOptions.MidlineRight);
            t.mark.textWrappingMode = TextWrappingModes.NoWrap;
            bool en = Sim.English;
            if (unknown)
            {
                XgRelicUi.PaintTile(t.glyphBox, t.glyph, r, true);
                t.frame.Style(new Color32(9, 15, 22, 255), XgDark.Hairline);
                t.name.text = T("？？？", "???"); t.name.color = XgDark.Dim;
                t.effect.text = T("还没见过", "Not found yet"); t.effect.color = XgDark.Dim;
                UiTip.Add(t.rt, () => T(("<b>？？？</b>\n还没见过。灵光学会新东西时三选一，或者小游戏、蜘蛛、新纪录里掉出来。" .Replace("灵光", Sim.AiDisplayName)), ("<b>???</b>\nNot found yet. It shows up when 灵光 learns something new, or drops from mini-games, the spider and new records." .Replace("灵光", Sim.AiDisplayName))));
                return t;
            }
            XgRelicUi.PaintTile(t.glyphBox, t.glyph, r);
            var edge = XgRelicUi.RarityColor(r.rarity);
            t.name.text = XgRelics.Name(r, en);
            t.effect.text = XgRelics.Effect(r, en).Replace("灵光", Sim.AiDisplayName);
            t.effect.color = XgDark.Muted;
            UiTip.Add(t.rt, () => XgRelicUi.Tip(Sim, r) + (equipable ? "\n" + Hex(XgDark.Muted) + (Sim.RelicEquipped(r.id) ? T("点一下卸下", "Click to unequip") : T("点一下装备", "Click to equip")) + "</color>" : ""));
            if (equipable)
            {
                var button = t.rt.gameObject.AddComponent<Button>();
                button.targetGraphic = t.frame; button.transition = Selectable.Transition.None;
                string id = r.id;
                button.onClick.AddListener(() => OnRelicClick(id));
                PaintOwned(t);
            }
            else
            {
                // The collection book: a found item shows whether it is owned.
                t.frame.Style(new Color(edge.r * .08f, edge.g * .08f, edge.b * .08f, 1f), new Color(edge.r * .6f, edge.g * .6f, edge.b * .6f, 1f));
                t.mark.text = Sim.RelicOwned(r.id) ? T("已拥有", "Owned") : "";
            }
            return t;
        }

        void PaintOwned(Tile t)
        {
            var r = XgRelics.Get(t.id);
            if (r == null) return;
            var edge = XgRelicUi.RarityColor(r.rarity);
            bool on = Sim.RelicEquipped(t.id);
            t.frame.Style(on ? new Color(edge.r * .20f, edge.g * .20f, edge.b * .20f, 1f) : XgDark.Card, on ? edge : new Color(edge.r * .55f, edge.g * .55f, edge.b * .55f, 1f), false, on ? 2 : 1);
            t.mark.text = on ? T("已装备", "Equipped") : fresh.Contains(t.id) ? "<color=#E24B4A>" + T("新", "New") + "</color>" : "";
            t.mark.color = on ? XgDark.Good : XgDark.Bad;
        }

        // ───────────── equipping ─────────────

        void OnRelicClick(string id)
        {
            if (Sim.RelicEquipped(id))
            {
                Sim.UnequipRelic(id);
                Fx.Play(XgJuice.Sfx.Id.Click);
                host.Refresh(true);
                return;
            }
            if (Sim.RelicFreeSlot >= 0)
            {
                Sim.EquipRelic(id);
                int slot = Array.IndexOf(Sim.S.relicSlots.ToArray(), id);
                Fx.Play(XgJuice.Sfx.Id.Unlock, 1, .7f);
                if (slot >= 0 && slots[slot] != null) { Fx.Knock(slots[slot].rt, .08f); Fx.Burst(Fx.At(slots[slot].rt), 14, XgRelicUi.RarityColor(XgRelics.Get(id).rarity), XgJuice.Shape.Spark, 260); }
                host.Refresh(true);
                return;
            }
            OpenChooser(id);
        }

        string LockedSlotText(int slot)
        {
            var node = XgCatalog.Node(XgSim.RelicExpansionForSlot(slot));
            return T("封印 · ", "Locked · ") + T(node.name, node.nameEn) + "\n"
                + T("第 " + node.stage + " 阶段 · 查看科技 ›", "Stage " + node.stage + " · View tech ›");
        }

        void OnSlotClick(int slot)
        {
            if (slot >= Sim.RelicSlotCapacity)
            {
                if (view != null) { view.ShowTab("tree"); view.Tree.FocusNodeDetails(XgSim.RelicExpansionForSlot(slot)); }
                return;
            }
            var r = Sim.RelicInSlot(slot);
            if (r == null) { Fx.Play(XgJuice.Sfx.Id.Click, 1, .4f); return; }
            Sim.UnequipRelic(r.id);
            Fx.Play(XgJuice.Sfx.Id.Click);
            host.Refresh(true);
        }

        /// <summary>All unlocked slots are taken: asks which one the item replaces.</summary>
        void OpenChooser(string id)
        {
            chooserFor = id;
            chooserSim = Sim;
            if (chooser == null)
            {
                chooser = Rect("Chooser", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Panel(chooser, new Color(0, 0, 0, .6f));
                var dim = chooser.gameObject.AddComponent<Button>(); dim.transition = Selectable.Transition.None;
                dim.onClick.AddListener(CloseChooser);
                var box = ui.Card(chooser, "Box", Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
                box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                chooserTitle = ui.Text(Strip("Title", box, 8, 32, 20, 20), "", 17, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                for (int i = 0; i < XgRelics.MaxSlots; i++)
                {
                    int at = i;
                    var b = ui.Button(box, "", () => ChooseSlot(at), 15);
                    int col = i % 2, row = i / 2;
                    b.rt.anchorMin = new Vector2(col * .5f, 1 - row * .1f); b.rt.anchorMax = new Vector2((col + 1) * .5f, 1 - row * .1f);
                    b.rt.offsetMin = new Vector2(12, -82); b.rt.offsetMax = new Vector2(-12, -54);
                    UiTip.Add(b.rt, () => { var r = Sim.RelicInSlot(at); return r == null ? "" : XgRelicUi.Tip(Sim, r); });
                    chooserButtons[i] = b;
                }
                var cancel = chooserCancel = ui.Button(box, T("算了", "Cancel"), CloseChooser, 15);
                cancel.rt.anchorMin = cancel.rt.anchorMax = new Vector2(.5f, 0);
                cancel.rt.offsetMin = new Vector2(-70, 12); cancel.rt.offsetMax = new Vector2(70, 46);
            }
            PaintChooser();
            chooser.SetAsLastSibling();
            chooser.gameObject.SetActive(true);
            Fx.Play(XgJuice.Sfx.Id.Click);
        }

        void PaintChooser()
        {
            if (!ReferenceEquals(chooserSim, Sim)) { CloseChooser(); return; }
            var incoming = XgRelics.Get(chooserFor);
            if (incoming == null) { CloseChooser(); return; }
            chooserCancel.Set(T("算了", "Cancel"), true);
            chooserTitle.text = T("装备栏满了。「" + XgRelics.Name(incoming, false) + "」换掉哪一格？", "All equipped slots are taken. Which one does '" + XgRelics.Name(incoming, true) + "' replace?");
            for (int i = 0; i < XgRelics.MaxSlots; i++)
            {
                chooserButtons[i].rt.gameObject.SetActive(i < Sim.RelicSlotCapacity);
                var r = Sim.RelicInSlot(i);
                chooserButtons[i].Set((i + 1) + "  " + (r == null ? "—" : XgRelics.Name(r, Sim.English)), true, null, XgDark.Ink);
            }
        }

        void ChooseSlot(int slot)
        {
            bool sameSim = ReferenceEquals(chooserSim, Sim);
            string id = chooserFor;
            CloseChooser();
            if (!sameSim || string.IsNullOrEmpty(id) || !Sim.EquipRelic(id, slot)) return;
            Fx.Play(XgJuice.Sfx.Id.Unlock, 1, .7f);
            if (slots[slot] != null) { Fx.Knock(slots[slot].rt, .08f); Fx.Burst(Fx.At(slots[slot].rt), 14, XgRelicUi.RarityColor(XgRelics.Get(id).rarity), XgJuice.Shape.Spark, 260); }
            host.Refresh(true);
        }

        void CloseChooser()
        {
            chooserFor = "";
            chooserSim = null;
            if (chooser != null) chooser.gameObject.SetActive(false);
        }
    }
}
