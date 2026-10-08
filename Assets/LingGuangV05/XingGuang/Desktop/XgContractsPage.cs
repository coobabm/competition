using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
using LingGuangV05.Desktop.Zhongbao;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A deployed checkpoint chip that can be dragged onto a contract.</summary>
    public sealed class XgChipDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string dataset;
        public XgContractsPage page;
        RectTransform rt;
        Vector2 start, last;
        CanvasGroup group;
        bool Local(PointerEventData e, out Vector2 p)
        {
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, cam, out p);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            rt = (RectTransform)transform; start = rt.anchoredPosition;
            Local(e, out last);
            group = group ?? gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.alpha = .85f;
            transform.SetAsLastSibling();
            page.BeginDrag(this);
        }
        public void OnDrag(PointerEventData e)
        {
            // The desktop canvas renders through a camera at scale 0.01: convert positions, never raw pixel deltas.
            if (!Local(e, out var p)) return;
            rt.anchoredPosition += p - last; last = p;
        }
        public void OnEndDrag(PointerEventData e)
        {
            group.blocksRaycasts = true; group.alpha = 1;
            rt.anchoredPosition = start;
            page.EndDrag(this);
        }
    }

    /// <summary>A contract row that accepts a dropped checkpoint chip.</summary>
    public sealed class XgContractDrop : MonoBehaviour, IDropHandler
    {
        public XgContract contract;
        public XgContractsPage page;
        public void OnDrop(PointerEventData e)
        {
            var chip = e.pointerDrag != null ? e.pointerDrag.GetComponent<XgChipDrag>() : null;
            if (chip != null) page.Dropped(chip, this);
        }
    }

    /// <summary>
    /// 企业订单 in 摆渡众包 (the approved redesign: a filter row, a slim checkpoint strip and a 3-column grid of order
    /// cards). Contracts run on the best checkpoint and pay every second. Drag a checkpoint onto a card, or press its
    /// 签约 button, to sign it. A button leads to 灵光's 训练 page, where checkpoints come from.
    /// </summary>
    public sealed class XgContractsPage : XgPage
    {
        const float CardH = 174, Gap = 12;
        const float FilterH = 28, InfoY = 36, StripY = 58;

        /// <summary>One order card and the parts that change.</summary>
        sealed class Card
        {
            public XgContract c;
            public RectTransform row, fill, stamp;
            public Image rim, bg, fillImg;
            public TMP_Text client, title, acc, note, moneyL, moneyR;
            public XgBtn btn;
        }

        RectTransform chipsRow, list, stripBox, viewport;
        TMP_Text info, summary, stripLabel, stripEmpty, listEmpty;
        readonly List<Card> cards = new List<Card>();
        readonly List<(string dataset, XgBtn chip)> chips = new List<(string, XgBtn)>();
        readonly List<(XgBtn btn, Outline rim)> filterBtns = new List<(XgBtn, Outline)>();
        // Filters (client-side, over the listed rows): 0 全部, 1 能签 (can sign now), 2 已签 (signed),
        // 3 看图 (the dataset's track is Vision: digits, captcha, memes, go, ImageNet), 4 文字 (track Sequence: text, logic, poems...).
        int filter;
        // Orders signed while the 能签 filter is on stay listed until the filter changes, so the card does not vanish mid-juice.
        readonly HashSet<string> justSigned = new HashSet<string>();
        string chipKey = "", rowKey = "";
        string dragging;
        XgBtn toTrain;
        float rowsHeight, stripH = 42;

        static readonly string[] FilterZh = { "全部", "能签", "已签", "看图", "文字" };
        static readonly string[] FilterEn = { "All", "Can sign", "Signed", "Pictures", "Text" };

        static void Rim(XgBtn b, Color c)
        {
            var o = b.rt.gameObject.AddComponent<Outline>();
            o.effectColor = c; o.effectDistance = new Vector2(1, -1);
        }

        static void SetText(TMP_Text t, string s) { if (t.text != s) t.text = s; }

        public override void Build(RectTransform area)
        {
            root = Rect("contracts", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Filter row: chips on the left, summary and the training button on the right.
            for (int i = 0; i < FilterZh.Length; i++)
            {
                int k = i;
                var b = ui.Button(root, "", () => { filter = k; justSigned.Clear(); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 13);
                PlaceTopLeft(b, i * 80, 1, 72, 26);
                Rim(b, ZhongbaoSkin.Line);
                filterBtns.Add((b, b.rt.GetComponent<Outline>()));
            }
            toTrain = ui.Button(root, T("去灵光训练 ↗", "Train in LingGuang ↗"), () => { Fx.Play(XgJuice.Sfx.Id.Click); host.ShowTab("train"); }, 13);
            toTrain.rt.gameObject.name = "ToTrain";
            PlaceTopRight(toTrain, 150, 0, 150, FilterH);
            Rim(toTrain, ZhongbaoSkin.Blue2);
            UiTip.Add(toTrain.rt, "检查点从灵光的训练页来：练得越好，能签的订单越多、单价越高。", "Checkpoints come from LingGuang's training page: the better the model, the more contracts you can sign and the more they pay.");
            summary = ui.Text(Rect("Summary", root, Vector2.one, Vector2.one, new Vector2(-470, -FilterH), new Vector2(-162, 0)), "", 13, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            summary.textWrappingMode = TextWrappingModes.NoWrap;

            info = ui.Text(Rect("Info", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(2, -InfoY - 18), new Vector2(-2, -InfoY)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            info.textWrappingMode = TextWrappingModes.NoWrap; info.overflowMode = TextOverflowModes.Ellipsis;

            viewport = Rect("Viewport", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -(StripY + stripH + 10)));
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            list = Rect("List", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            list.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            listEmpty = ui.Text(Rect("Empty", viewport, Vector2.zero, Vector2.one, new Vector2(40, 40), new Vector2(-40, -40)), "", 14, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);

            // Built after the list so a dragged chip draws above the cards.
            stripBox = ZhongbaoSkin.Box(ui, root, "Strip", new Vector2(0, 1), Vector2.one, new Vector2(0, -StripY - stripH), new Vector2(0, -StripY));
            stripLabel = ui.Text(Rect("Label", stripBox, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -38), new Vector2(104, -4)), "", 13, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
            stripLabel.textWrappingMode = TextWrappingModes.NoWrap;
            chipsRow = Rect("Chips", stripBox, Vector2.zero, Vector2.one, new Vector2(108, 0), new Vector2(-8, 0));
            stripEmpty = ui.Text(Rect("None", stripBox, Vector2.zero, Vector2.one, new Vector2(108, 0), new Vector2(-8, 0)), "", 13, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
        }

        public void BeginDrag(XgChipDrag chip)
        {
            dragging = chip.dataset;
            Fx.Play(XgJuice.Sfx.Id.Swoosh, 1.2f, .6f);
            Refresh();
        }

        public void EndDrag(XgChipDrag chip) { dragging = null; Refresh(); }

        public void Dropped(XgChipDrag chip, XgContractDrop drop)
        {
            var c = drop.contract;
            var row = (RectTransform)drop.transform;
            if (c.dataset != chip.dataset || !Sim.CanSign(c))
            {
                Fx.Knock(row, .02f, new Vector2(12, 0));
                Fx.Play(XgJuice.Sfx.Id.Thud);
                host.ShowToast(c.dataset != chip.dataset ? Lang.T("这个检查点干不了这活：需要「") + XgCatalog.Dataset(c.dataset).name + T("」", "")
                    : c.realtime && Sim.BestAcc(c.dataset) + 1e-9 >= c.threshold ? Lang.T("直播等不了：循环网络一个字一个字地算，跟不上。要不用循环的模型达到 ") + XgSim.Pct(c.threshold)
                    : Lang.T("准确率还没到门槛 ") + XgSim.Pct(c.threshold), 2.5f);
                return;
            }
            Sign(c, row);
        }

        void Sign(XgContract c, RectTransform row)
        {
            if (!Sim.Sign(c.id, Host)) return;
            justSigned.Add(c.id);
            Vector2 at = Fx.At(row);
            Fx.Knock(row, .05f, new Vector2(0, -6), .3f);
            Fx.Shake(3, .2f);
            Fx.Shockwave(at, XgPalette.Good, 220, .35f, 8);
            Fx.Play(XgJuice.Sfx.Id.Stamp);
            Fx.Play(XgJuice.Sfx.Id.Coin, 1, .7f);
            Fx.Burst(at, 24, XgPalette.Gold, XgJuice.Shape.Yen, 300);
            Fx.Float(at + new Vector2(0, 40), Lang.T("签约！首付 +¥") + Money(c.signBonus), XgPalette.Good, 26);
            host.Refresh(true);
        }

        bool Passes(XgContract c)
        {
            switch (filter)
            {
                case 1: return Sim.CanSign(c) || justSigned.Contains(c.id);
                case 2: return Sim.Signed(c.id);
                case 3: return XgCatalog.Dataset(c.dataset).track == XgTrack.Vision;
                case 4: return XgCatalog.Dataset(c.dataset).track == XgTrack.Sequence;
                default: return true;
            }
        }

        Card BuildCard(XgContract c, int i)
        {
            int col = i % 3, line = i / 3;
            float top = line * (CardH + Gap);
            var row = Rect("Row" + c.id, list, new Vector2(col / 3f, 1), new Vector2((col + 1) / 3f, 1),
                new Vector2(col * 4f, -top - CardH), new Vector2(-(2 - col) * 4f, -top));
            var k = new Card { c = c, row = row };
            k.rim = Panel(row, ZhongbaoSkin.Line);
            var inner = Rect("Inner", row, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            k.bg = Panel(inner, Color.white); k.bg.raycastTarget = false;
            var drop = row.gameObject.AddComponent<XgContractDrop>(); drop.contract = c; drop.page = this;

            RectTransform Line(string name, float y, float h, float l, float r) =>
                Rect(name, inner, new Vector2(0, 1), new Vector2(1, 1), new Vector2(l, -y - h), new Vector2(-r, -y));
            TMP_Text Label(string name, float y, float h, float size, Color color, TextAlignmentOptions al, float l = 12, float r = 12)
            {
                var t = ui.Text(Line(name, y, h, l, r), "", size, color, al);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
                return t;
            }

            k.client = Label("Client", 12, 16, 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft, 12, 70);
            k.title = Label("Job", 30, 22, 15, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
            k.title.fontStyle = FontStyles.Bold;
            k.acc = ui.Text(Rect("Accuracy", inner, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -74), new Vector2(162, -58)), "", 12, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
            k.acc.textWrappingMode = TextWrappingModes.NoWrap;
            var bar = Rect("Bar", inner, new Vector2(0, 1), new Vector2(1, 1), new Vector2(168, -69), new Vector2(-12, -63));
            k.fill = Bar(bar, "Fill", ZhongbaoSkin.Track, ZhongbaoSkin.Blue2);
            k.fillImg = k.fill.GetComponent<Image>();
            k.note = ui.Text(Line("Note", 80, 26, 12, 12), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
            k.moneyL = Label("Money", 108, 24, 12, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
            k.moneyR = Label("Bonus", 108, 24, 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            k.btn = ui.Button(inner, "", () => Sign(c, row), 13);
            k.btn.rt.anchorMin = new Vector2(0, 0); k.btn.rt.anchorMax = new Vector2(1, 0);
            k.btn.rt.offsetMin = new Vector2(12, 12); k.btn.rt.offsetMax = new Vector2(-12, 40);
            UiTip.Add(k.btn.rt, "签约：这个数据集的最好成绩达到要求就能签。签了先给一笔首付，之后每秒给钱；成绩越高给得越多。", "Sign: possible once your best score on this dataset meets the requirement. You get an advance, then money every second; better scores pay more.");

            // The green 已签约 stamp, tilted like a rubber stamp in the top-right corner.
            var stamp = Rect("Stamp", inner, Vector2.one, Vector2.one, new Vector2(-70, -32), new Vector2(-10, -10));
            stamp.localRotation = Quaternion.Euler(0, 0, -8);
            Panel(stamp, ZhongbaoSkin.Green).raycastTarget = false;
            var stampIn = Rect("Inner", stamp, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));
            Panel(stampIn, ZhongbaoSkin.GreenSoft).raycastTarget = false;
            var st = ui.Text(Rect("Text", stampIn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), T("已签约", "SIGNED"), 12, ZhongbaoSkin.Green, TextAlignmentOptions.Center);
            st.fontStyle = FontStyles.Bold; st.textWrappingMode = TextWrappingModes.NoWrap;
            k.stamp = stamp;
            return k;
        }

        public override void Refresh()
        {
            if (root == null) return;
            ZhongbaoSkin.Ghost(toTrain, T("去灵光训练 ↗", "Train in LingGuang ↗"), true);
            for (int i = 0; i < filterBtns.Count; i++)
            {
                bool on = i == filter;
                filterBtns[i].btn.Set(T(FilterZh[i], FilterEn[i]), true, on ? ZhongbaoSkin.Blue3 : Color.white, on ? ZhongbaoSkin.Blue : ZhongbaoSkin.Ink);
                filterBtns[i].rim.effectColor = on ? ZhongbaoSkin.Blue : ZhongbaoSkin.Line;
            }

            int signedCount = 0;
            foreach (var c in XgCatalog.Contracts) if (Sim.Signed(c.id)) signedCount++;
            SetText(summary, T("已签 " + signedCount + " 单 · 每秒 ", "Signed " + signedCount + " · per second ")
                + "<b><color=" + ZhongbaoSkin.Hex(ZhongbaoSkin.Red) + ">+¥" + Money(Sim.IncomePerSecond) + "</color></b>");
            SetText(info, T("订单用<b>最佳检查点</b>干活，每秒把钱打进和家里共用的钱包；高出门槛越多单价越高（最多 ×2）。把下面的检查点拖到订单上，或按「签约」。",
                "Orders run on your <b>best checkpoint</b> and pay into the shared wallet every second; the further above the bar, the more they pay (up to ×2). Drag a checkpoint below onto an order, or press Sign."));

            // Checkpoint chips (slim strip, wraps onto more lines when there are many).
            var ready = new List<XgBest>(Sim.S.best);
            ready.RemoveAll(b => b.acc <= 0);
            float width = root.rect.width > 1 ? root.rect.width : 1110;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((width - 108 - 8 + 6) / 206f));
            string key = string.Join(",", ready.ConvertAll(b => b.dataset)) + "/" + perRow;
            if (key != chipKey)
            {
                chipKey = key;
                foreach (var x in chips) UnityEngine.Object.Destroy(x.chip.rt.gameObject);
                chips.Clear();
                for (int i = 0; i < ready.Count; i++)
                {
                    var b = ui.Button(chipsRow, "", null, 13);
                    PlaceTopLeft(b, (i % perRow) * 206, 7 + (i / perRow) * 34, 200, 28);
                    var drag = b.rt.gameObject.AddComponent<XgChipDrag>();
                    drag.dataset = ready[i].dataset; drag.page = this;
                    chips.Add((ready[i].dataset, b));
                }
                int lines = Mathf.Max(1, (ready.Count + perRow - 1) / perRow);
                stripH = 14 + lines * 34 - 6 + (lines == 1 ? 6 : 0);
                stripBox.offsetMin = new Vector2(0, -StripY - stripH); stripBox.offsetMax = new Vector2(0, -StripY);
                viewport.offsetMax = new Vector2(0, -(StripY + stripH + 10));
            }
            SetText(stripLabel, T("<b>我的检查点</b>", "<b>My checkpoints</b>"));
            stripEmpty.gameObject.SetActive(chips.Count == 0);
            SetText(stripEmpty, T("还没有检查点：去灵光练一个，再回来拖到订单上。", "No checkpoint yet: train one in LingGuang, then drag it onto an order here."));
            foreach (var (dataset, chip) in chips)
            {
                double sc = Sim.BestScore(dataset);
                chip.Set(T(XgCatalog.Dataset(dataset).name, XgCatalog.Dataset(dataset).nameEn) + " " + XgCatalog.GradeNames[XgSim.Grade(sc)] + " " + XgSim.Pct(Sim.BestAcc(dataset)), true, XgPalette.Grades[XgSim.Grade(sc)], Color.white);
            }

            // Order cards: those whose data is open, plus anything signed, then the active filter.
            var open = Array.FindAll(XgCatalog.Contracts, c => Sim.Signed(c.id) || Sim.DatasetAvailable(c.dataset));
            var visible = Array.FindAll(open, Passes);
            string rk = filter + ":" + string.Join(",", Array.ConvertAll(visible, c => c.id));
            if (rk != rowKey)
            {
                rowKey = rk;
                foreach (var k in cards) UnityEngine.Object.Destroy(k.row.gameObject);
                cards.Clear();
                for (int i = 0; i < visible.Length; i++) cards.Add(BuildCard(visible[i], i));
                rowsHeight = ((visible.Length + 2) / 3) * (CardH + Gap);
            }
            listEmpty.gameObject.SetActive(visible.Length == 0);
            SetText(listEmpty, T("没有符合条件的订单。", "No orders match this filter."));

            foreach (var k in cards)
            {
                var c = k.c;
                var d = XgCatalog.Dataset(c.dataset);
                double best = Sim.ContractAcc(c);
                bool signed = Sim.Signed(c.id), can = Sim.CanSign(c);
                bool reached = best + 1e-9 >= c.threshold;
                SetText(k.client, T(c.client, c.clientEn) + " · " + T("要 " + d.name, "needs " + d.nameEn));
                SetText(k.title, T(c.job, c.jobEn));
                SetText(k.acc, T("准确率 ", "Accuracy ") + (best > 0 ? XgSim.Pct(best) : "—") + " / " + XgSim.Pct(c.threshold));
                k.acc.color = reached ? ZhongbaoSkin.Green : ZhongbaoSkin.Ink;
                SetBar(k.fill, c.threshold > 0 ? (float)(best / c.threshold) : 0);
                k.fillImg.color = reached ? ZhongbaoSkin.Green : ZhongbaoSkin.Blue2;

                string note = "";
                if (c.realtime) note = T("实时：只认不用循环的模型", "Realtime: only models without loops count");
                if (Sim.DriftPay(c) < 1 - 1e-9)
                    note += (note.Length > 0 ? "\n" : "") + "<color=" + ZhongbaoSkin.Hex(ZhongbaoSkin.Red) + ">" + T("新题型：收入 −" + XgSim.Pct(1 - Sim.DriftPay(c)) + "，回炉训练能补回来", "New question types: income −" + XgSim.Pct(1 - Sim.DriftPay(c)) + ", retrain to win it back") + "</color>";
                SetText(k.note, note);

                string red = ZhongbaoSkin.Hex(ZhongbaoSkin.Red);
                SetText(k.moneyL, "<size=18><b><color=" + red + ">+¥" + Money(signed ? Sim.ContractIncome(c) : c.income) + "</color></b></size>" + (signed ? T("/秒", "/s") : T("/秒起", "/s up")));
                SetText(k.moneyR, T("签约首付 ¥", "Sign-on bonus ¥") + Money(c.signBonus));

                bool target = dragging != null && dragging == c.dataset && can;
                k.bg.color = target ? new Color32(226, 246, 230, 255) : signed ? ZhongbaoSkin.GreenSoft : Color.white;
                k.rim.color = target ? ZhongbaoSkin.Green : signed ? ZhongbaoSkin.GreenLine : ZhongbaoSkin.Line;
                k.stamp.gameObject.SetActive(signed);
                if (signed) ZhongbaoSkin.Ghost(k.btn, T("每秒结算中", "Paying every second"), false);
                else if (can) ZhongbaoSkin.Primary(k.btn, T("签约", "Sign"), true);
                else
                {
                    // Short of the bar: whole percentage points to go (at least 1); the button does nothing.
                    int gap = Mathf.Max(1, Mathf.CeilToInt((float)((c.threshold - best) * 100 - 1e-6)));
                    ZhongbaoSkin.Primary(k.btn, T("还差 " + gap + "%：去灵光练", gap + "% short: train in LingGuang"), false);
                }
            }
            list.sizeDelta = new Vector2(0, rowsHeight);
        }
    }
}
