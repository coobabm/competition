using System;
using LingGuangV05.Runtime;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 养成 › 道具 (lingguang-redesign/index.html #items): the items the tech tree sells, shelved by category (结构,
    /// 技巧, 电力; later 数据, 挣钱, 剧情) from the data-driven list in <see cref="XgItems"/>. A card shows the year, the
    /// ability discount (参数量与数据量主线 §6), the note, the price and whether it is owned; buying is the tree's own
    /// purchase. Width, depth, packs and the rest of the tree are one click away under 科技树.
    /// </summary>
    public sealed class XgItemsPage : XgPage
    {
        const float CardHeight = 104, Gap = 10, ShelfHead = 30;

        sealed class Card
        {
            public XgItemDef def;
            public XgNode node;
            public RectTransform rt;
            public Image rim;
            public TMP_Text name, year, effect, note, state;
            public XgBtn buy;
        }

        TMP_Text header, motto;
        XgBtn treeLink;
        RectTransform content;
        readonly List<Card> cards = new List<Card>();
        readonly List<TMP_Text> shelfTitles = new List<TMP_Text>();
        string signature = "";

        public override void Build(RectTransform area)
        {
            root = Rect("items", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 0, 30, 2, 560), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            header.textWrappingMode = TextWrappingModes.NoWrap; header.characterSpacing = 2;
            motto = ui.Text(Rect("Motto", root, new Vector2(1, 1), Vector2.one, new Vector2(-560, -30), new Vector2(-232, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            motto.textWrappingMode = TextWrappingModes.NoWrap;
            treeLink = ui.Button(root, "", () => { Fx.Play(XgJuice.Sfx.Id.Click); host.ShowTab("tree"); }, 13);
            PlaceTopRight(treeLink, 222, 0, 220, 30);
            treeLink.rt.gameObject.name = "TechTreeLink";
            UiTip.Add(treeLink.rt, () => T("科技树：加宽、加深的上限，数据包，研究，机房和预训练秘籍。按住节点 0.6 秒购买。", "Tech tree: width and depth caps, data packs, research, the server room and the pre-training secret. Hold a node 0.6 s to buy."));

            var list = Rect("List", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -38));
            list.gameObject.AddComponent<RectMask2D>();
            Panel(list, new Color(0, 0, 0, 0));
            content = Rect("Content", list, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = list; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30; scroll.inertia = false;
        }

        /// <summary>Items on the shelves now: those the tree shows (no hidden or mystery ones), by year within each shelf.</summary>
        List<(XgItemDef def, XgNode node)> Shown()
        {
            var list = new List<(XgItemDef, XgNode)>();
            if (Sim == null) return list;
            foreach (var def in XgItems.All())
            {
                var node = XgCatalog.Node(def.id);
                if (node == null || Sim.NodeMystery(node) || !Sim.NodeVisible(node) && !Sim.Has(node.id)) continue;
                list.Add((def, node));
            }
            list.Sort((a, b) =>
            {
                int c = Array.IndexOf(XgItems.Order, a.Item1.category).CompareTo(Array.IndexOf(XgItems.Order, b.Item1.category));
                if (c != 0) return c;
                int ya = a.Item1.year > 0 ? a.Item1.year : 9999, yb = b.Item1.year > 0 ? b.Item1.year : 9999;
                return ya != yb ? ya.CompareTo(yb) : a.Item2.cost.CompareTo(b.Item2.cost);
            });
            return list;
        }

        /// <summary>Items that can be bought right now (the nav badge and the 概览 action).</summary>
        public int BuyableCount()
        {
            int n = 0;
            if (Sim == null) return 0;
            foreach (var (_, node) in Shown()) if (Sim.Status(node, Host) == XgSim.NodeStatus.Buyable) n++;
            return n;
        }

        /// <summary>Tech-tree nodes (not items) that can be bought now: the 科技树 link and the 科技 nav row show it.</summary>
        public int TreeBuyableCount()
        {
            int n = 0;
            if (Sim == null) return 0;
            foreach (var node in XgCatalog.Nodes)
                if (!XgItems.IsItem(node.id) && !XgSim.IsAtlas(node) && Sim.NodeVisible(node) && Sim.Status(node, Host) == XgSim.NodeStatus.Buyable) n++;
            return n;
        }

        /// <summary>The card of an item (coins fly to it when it is bought here).</summary>
        public RectTransform NodeTarget(string id)
        {
            var c = cards.Find(x => x.node.id == id);
            return c != null && root.gameObject.activeInHierarchy ? c.rt : null;
        }

        public override void Refresh()
        {
            if (Sim == null || content == null) return;
            var shown = Shown();
            var sig = new StringBuilder();
            foreach (var (def, _) in shown) sig.Append(def.id).Append(',');
            if (sig.ToString() != signature) { signature = sig.ToString(); Rebuild(shown); }

            int owned = 0;
            foreach (var c in cards) if (Sim.Has(c.node.id)) owned++;
            header.text = T("道具 · 结构和技巧", "ITEMS · structures and techniques") + "  <color=#3A5566>" + owned + "/" + cards.Count + "</color>";
            motto.text = T("不买也能通关，只是更慢、更贵", "You can finish without them; it is slower and dearer");
            int treeBuyable = TreeBuyableCount();
            treeLink.Set(T("科技树 · 加宽 加深 研究 ↗", "Tech tree · width, depth, research ↗") + (treeBuyable > 0 ? "  <color=#FAC775>" + treeBuyable + "</color>" : ""), true, null, XgDark.Link);
            foreach (var c in cards) RefreshCard(c);
        }

        void Rebuild(List<(XgItemDef def, XgNode node)> shown)
        {
            foreach (Transform child in content) UnityEngine.Object.Destroy(child.gameObject);
            cards.Clear(); shelfTitles.Clear();
            float y = 0;
            foreach (var category in XgItems.Order)
            {
                var group = shown.FindAll(x => x.def.category == category);
                int count = group.Count;
                if (count == 0) continue;
                var title = ui.Text(Strip("Shelf " + category, content, y, ShelfHead, 2, 2), XgItems.CategoryName(category, Sim.English) + "  <color=#3A5566>" + count + "</color>", 13, XgDark.Muted, TextAlignmentOptions.BottomLeft);
                title.characterSpacing = 4;
                shelfTitles.Add(title);
                y += ShelfHead + 6;
                for (int k = 0; k < count; k++)
                {
                    int col = k % 3, row = k / 3;
                    float top = y + row * (CardHeight + Gap);
                    cards.Add(MakeCard(group[k].def, group[k].node, col, top));
                }
                y += ((count + 2) / 3) * (CardHeight + Gap) + 4;
            }
            content.sizeDelta = new Vector2(0, y);
        }

        Card MakeCard(XgItemDef def, XgNode node, int col, float y)
        {
            var c = new Card { def = def, node = node };
            var rt = Rect("Item " + node.id, content, new Vector2(col / 3f, 1), new Vector2((col + 1) / 3f, 1), new Vector2(col == 0 ? 0 : Gap / 2, -y - CardHeight), new Vector2(col == 2 ? 0 : -Gap / 2, -y));
            c.rt = rt;
            c.rim = Panel(rt, XgDark.Line);
            Panel(Rect("Inner", rt, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), XgDark.Card).raycastTarget = false;
            c.name = ui.Text(Rect("Name", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -32), new Vector2(-96, -6)), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            c.name.textWrappingMode = TextWrappingModes.NoWrap; c.name.overflowMode = TextOverflowModes.Ellipsis;
            c.year = ui.Text(Rect("Year", rt, new Vector2(1, 1), Vector2.one, new Vector2(-96, -30), new Vector2(-10, -8)), "", 12, XgDark.Dim, TextAlignmentOptions.MidlineRight);
            c.year.textWrappingMode = TextWrappingModes.NoWrap;
            c.effect = ui.Text(Rect("Effect", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -54), new Vector2(-10, -32)), "", 12, XgDark.Params, TextAlignmentOptions.MidlineLeft);
            c.effect.textWrappingMode = TextWrappingModes.NoWrap; c.effect.overflowMode = TextOverflowModes.Ellipsis;
            c.note = ui.Text(Rect("Note", rt, Vector2.zero, Vector2.one, new Vector2(12, 32), new Vector2(-10, -54)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            // 18 px: an Ellipsis label shorter than the font's line (≈17.4 px at 12) drops its only line and shows nothing.
            c.note.textWrappingMode = TextWrappingModes.NoWrap; c.note.overflowMode = TextOverflowModes.Ellipsis;
            c.state = ui.Text(Rect("State", rt, Vector2.zero, new Vector2(1, 0), new Vector2(12, 8), new Vector2(-130, 32)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            c.state.textWrappingMode = TextWrappingModes.NoWrap; c.state.overflowMode = TextOverflowModes.Ellipsis;
            c.buy = ui.Button(rt, "", () => Buy(c), 13);
            c.buy.rt.anchorMin = c.buy.rt.anchorMax = new Vector2(1, 0);
            c.buy.rt.offsetMin = new Vector2(-122, 8); c.buy.rt.offsetMax = new Vector2(-10, 32);
            UiTip.Add(rt, () => Sim.NodeName(node) + "\n" + Sim.NodeNote(node) + (Sim.Has(node.id) ? "" : "\n\n" + Sim.Why(node, Host)) + "\n<color=#6F95A5>" + T("点开看详细", "Click for details") + "</color>");
            Clickable(rt, () => OpenDetail(Sim.NodeName(node), Sim.NodeNote(node), XgItemLore.Get(node.target ?? node.id, GameText.IsEnglish)));
            return c;
        }

        void RefreshCard(Card c)
        {
            var node = c.node;
            var status = Sim.Status(node, Host);
            bool owned = status == XgSim.NodeStatus.Owned;
            c.rim.color = owned ? XgDark.Good : status == XgSim.NodeStatus.Buyable ? XgDark.Money : XgDark.Line;
            c.name.text = Sim.NodeName(node);
            string shownYear = XgCatalog.ShownYear(c.def.year);
            c.year.text = XgItems.Early(c.def) ? (shownYear.Length > 0 ? shownYear + T(" · 提前", " · early") : T("提前", "early")) : shownYear;
            c.effect.text = Effect(node);
            c.note.text = Sim.NodeNote(node);
            double cost = Sim.NodeCost(node);
            if (owned)
            {
                c.buy.Show(false);
                c.state.text = "<color=#5DCAA5>[" + T("已拥有", "Owned") + "]</color>";
            }
            else
            {
                c.buy.Show(true);
                bool can = status == XgSim.NodeStatus.Buyable;
                c.buy.Set(double.IsInfinity(cost) ? "—" : "¥" + Money(cost), can, null, XgDark.Money);
                c.state.text = status == XgSim.NodeStatus.Locked ? "<color=#58798A>" + Flat(Sim.Why(node, Host)) + "</color>" : "";
            }
        }

        static string Flat(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\n", " ");

        /// <summary>The ability discount the item gives (or the kind of help a technique is).</summary>
        string Effect(XgNode node)
        {
            var sb = new StringBuilder();
            foreach (var d in XgSim.DiscountsOf(node.id))
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(T("能力 ", "Ability ")).Append(d.ability).Append(T("「", " '")).Append(XgSim.AbilityName(d.ability, Sim.English)).Append(T("」", "'"));
                if (d.paramsFactor < 1 && d.samplesFactor < 1 && Math.Abs(d.paramsFactor - d.samplesFactor) < 1e-9) sb.Append(T(" 门槛 ×", " lines ×")).Append(N(d.paramsFactor, "0.0#"));
                else
                {
                    if (d.paramsFactor < 1) sb.Append(T(" 参数 ×", " params ×")).Append(N(d.paramsFactor, "0.0#"));
                    if (d.samplesFactor < 1) sb.Append(T(" 样本 ×", " samples ×")).Append(N(d.samplesFactor, "0.0#"));
                }
            }
            if (sb.Length == 0 && node.id == "cooler") sb.Append("<color=#7FD3FF>").Append(T("烧卡概率 −50%", "burn-out chance −50%")).Append("</color>");
            if (sb.Length == 0 && node.id == "cudnn") sb.Append("<color=#7FD3FF>").Append(T("训练 ×1.3 · 电费 −30%", "training ×1.3 · power −30%")).Append("</color>");
            if (sb.Length == 0)
            {
                var a = node.kind == XgNodeKind.Arch ? XgCatalog.Arch(node.target) : null;
                if (a != null) sb.Append(Lang.T(a.topo));
                else sb.Append("<color=#6F95A5>").Append(T("训练技巧", "Training technique")).Append("</color>");
            }
            return sb.ToString();
        }

        // ───────────── detail panel: the card's one-liner on top, the popular-science text below ─────────────

        RectTransform detail;
        TMP_Text detailTitle, detailNote, detailBody;

        /// <summary>A click anywhere on the card (except its buy button, which eats its own click) opens the detail panel.</summary>
        static void Clickable(RectTransform rt, Action open)
        {
            var hit = rt.GetComponent<Image>();
            if (hit == null) { hit = rt.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); }
            hit.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => open());
        }

        void OpenDetail(string title, string note, string body)
        {
            if (detail == null)
            {
                detail = Rect("ItemDetail", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Panel(detail, new Color(0, 0, 0, .55f));
                var dimButton = detail.gameObject.AddComponent<Button>(); dimButton.transition = Selectable.Transition.None;
                dimButton.onClick.AddListener(CloseDetail);
                var box = Rect("Box", detail, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-300, -210), new Vector2(300, 210));
                Panel(box, XgDark.Line);
                Panel(Rect("Inner", box, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), XgDark.Card);
                box.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // swallow clicks inside the box
                detailTitle = ui.Text(Rect("Title", box, new Vector2(0, 1), Vector2.one, new Vector2(20, -48), new Vector2(-60, -14)), "", 20, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                detailNote = ui.Text(Rect("Note", box, new Vector2(0, 1), Vector2.one, new Vector2(20, -80), new Vector2(-20, -50)), "", 14, XgDark.Params, TextAlignmentOptions.MidlineLeft);
                detailBody = ui.Text(Rect("Body", box, Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -92)), "", 14, XgDark.Ink, TextAlignmentOptions.TopLeft);
                detailBody.lineSpacing = 6; detailBody.overflowMode = TextOverflowModes.Ellipsis;
                var close = ui.Button(box, "×", CloseDetail, 18);
                close.rt.anchorMin = close.rt.anchorMax = Vector2.one;
                close.rt.offsetMin = new Vector2(-46, -46); close.rt.offsetMax = new Vector2(-12, -12);
            }
            detailTitle.text = title;
            detailNote.text = note;
            detailBody.text = string.IsNullOrEmpty(body) ? T("（这一项还没写详细说明。）", "(No details yet.)") : body;
            detail.SetAsLastSibling();
            detail.gameObject.SetActive(true);
            Fx.Play(XgJuice.Sfx.Id.Click);
        }

        void CloseDetail() { if (detail != null) detail.gameObject.SetActive(false); }

        void Buy(Card c)
        {
            if (Sim.Status(c.node, Host) != XgSim.NodeStatus.Buyable)
            {
                Fx.Knock(c.buy.rt, .05f, new Vector2(8, 0));
                Fx.Play(XgJuice.Sfx.Id.Thud);
                host.ShowToast(Sim.Why(c.node, Host), 3.5f);
                host.Refresh(true);
                return;
            }
            host.BuyNode(c.node, c.rt);
        }
    }
}
