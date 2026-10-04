using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

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

    /// <summary>订单: contracts run on the best checkpoint and pay every second. Drag a checkpoint onto one to sign it.</summary>
    public sealed class XgContractsPage : XgPage
    {
        RectTransform chipsRow, list;
        TMP_Text header;
        readonly List<(XgContract c, RectTransform row, TMP_Text text, XgBtn btn, Image bg)> rows = new List<(XgContract, RectTransform, TMP_Text, XgBtn, Image)>();
        readonly List<(string dataset, XgBtn chip)> chips = new List<(string, XgBtn)>();
        string chipKey = "", rowKey = "";
        string dragging;
        // 甲方高价单 and 转包 sit below the contract rows (XgMarketSection.cs).
        XgMarketSection market;
        float rowsHeight;

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "contracts", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 50, 16, 16), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            var viewport = Rect("Viewport", card, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -106));
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            list = Rect("List", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            list.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            market = new XgMarketSection(view, ui, list);
            // Built after the list so a dragged chip draws above the rows.
            chipsRow = Strip("Chips", card, 62, 36, 16, 16);
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
                view.ShowToast(c.dataset != chip.dataset ? T("这个检查点干不了这活：需要「", "Wrong checkpoint: needs ") + XgCatalog.Dataset(c.dataset).name + T("」", "") : T("准确率还没到门槛 ", "Below the bar ") + XgSim.Pct(c.threshold), 2.5f);
                return;
            }
            Sign(c, row);
        }

        void Sign(XgContract c, RectTransform row)
        {
            if (!Sim.Sign(c.id, Host)) return;
            Vector2 at = Fx.At(row);
            Fx.Knock(row, .05f, new Vector2(0, -6), .3f);
            Fx.Shake(3, .2f);
            Fx.Shockwave(at, XgPalette.Good, 220, .35f, 8);
            Fx.Play(XgJuice.Sfx.Id.Stamp);
            Fx.Play(XgJuice.Sfx.Id.Coin, 1, .7f);
            Fx.Burst(at, 24, XgPalette.Gold, XgJuice.Shape.Yen, 300);
            Fx.Float(at + new Vector2(0, 40), T("签约！首付 +¥", "Signed! +¥") + Money(c.signBonus), XgPalette.Good, 26);
            view.Refresh(true);
        }

        public override void Refresh()
        {
            if (root == null) return;
            header.text = T("订单用<b>最佳检查点</b>干活，每秒把钱打进和家里共用的钱包；高出门槛越多单价越高（最多 ×2）。<color=#3B5BDB>把下面的检查点拖到订单上签约。</color>",
                "Contracts run on your <b>best checkpoint</b> and pay every second; the further above the bar, the higher the rate (up to ×2). <color=#3B5BDB>Drag a checkpoint onto a contract to sign.</color>")
                + "  " + T("合计 ", "Total ") + "<color=#E86E14>¥" + Money(Sim.IncomePerSecond) + T("/秒", "/s") + "</color>";

            // Checkpoint chips.
            var ready = new List<XgBest>(Sim.S.best);
            ready.RemoveAll(b => b.acc <= 0);
            string key = string.Join(",", ready.ConvertAll(b => b.dataset));
            if (key != chipKey)
            {
                chipKey = key;
                foreach (var x in chips) UnityEngine.Object.Destroy(x.chip.rt.gameObject);
                chips.Clear();
                for (int i = 0; i < ready.Count; i++)
                {
                    var b = ui.Button(chipsRow, "", null, 13);
                    PlaceTopLeft(b, i * 206, 0, 200, 34);
                    var drag = b.rt.gameObject.AddComponent<XgChipDrag>();
                    drag.dataset = ready[i].dataset; drag.page = this;
                    chips.Add((ready[i].dataset, b));
                }
            }
            foreach (var (dataset, chip) in chips)
            {
                double sc = Sim.BestScore(dataset);
                chip.Set(T(XgCatalog.Dataset(dataset).name, XgCatalog.Dataset(dataset).nameEn) + " " + XgCatalog.GradeNames[XgSim.Grade(sc)] + " " + XgSim.Pct(Sim.BestAcc(dataset)), true, XgPalette.Grades[XgSim.Grade(sc)], Color.white);
            }

            // Contract rows: those whose data is open, plus anything signed.
            var visible = Array.FindAll(XgCatalog.Contracts, c => Sim.Signed(c.id) || Sim.DatasetAvailable(c.dataset));
            string rk = string.Join(",", Array.ConvertAll(visible, c => c.id));
            if (rk != rowKey)
            {
                rowKey = rk;
                foreach (var r in rows) UnityEngine.Object.Destroy(r.row.gameObject);
                rows.Clear();
                for (int i = 0; i < visible.Length; i++)
                {
                    var c = visible[i];
                    int col = i % 2, line = i / 2;
                    var row = Rect("Row" + c.id, list, new Vector2(col * .5f, 1), new Vector2(col * .5f + .5f, 1), new Vector2(col == 0 ? 4 : 6, -(line + 1) * 84), new Vector2(col == 0 ? -6 : -4, -line * 84 - 8));
                    var bg = Panel(row, new Color32(247, 249, 253, 255));
                    var drop = row.gameObject.AddComponent<XgContractDrop>(); drop.contract = c; drop.page = this;
                    var text = ui.Text(Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-118, -4)), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                    var btn = ui.Button(row, "", () => Sign(c, row), 15);
                    btn.rt.anchorMin = btn.rt.anchorMax = new Vector2(1, .5f); btn.rt.offsetMin = new Vector2(-108, -17); btn.rt.offsetMax = new Vector2(-10, 17);
                    rows.Add((c, row, text, btn, bg));
                    UiTip.Add(btn.rt, "签约：这个数据集的最好成绩达到要求就能签。签了先给一笔首付，之后每秒给钱；成绩越高给得越多。", "Sign: possible once your best score on this dataset meets the requirement. You get an advance, then money every second; better scores pay more.");
                }
                rowsHeight = ((visible.Length + 1) / 2) * 84 + 8;
            }
            foreach (var (c, row, text, btn, bg) in rows)
            {
                var d = XgCatalog.Dataset(c.dataset);
                double best = Sim.BestAcc(c.dataset);
                bool signed = Sim.Signed(c.id), can = Sim.CanSign(c);
                text.text = "<b>" + T(c.client, c.clientEn) + "</b> · " + T(c.job, c.jobEn) + "\n<size=13><color=#68748C>" + T(d.name, d.nameEn) + " ≥ " + XgSim.Pct(c.threshold)
                    + T("  当前 ", "  now ") + (best > 0 ? XgSim.Pct(best) : "—") + "</color></size>\n<color=#E86E14>¥" + Money(c.income) + T("/秒起", "/s base") + "</color>"
                    + (signed ? "  <color=#2F9E44>" + T("在跑 ¥", "earning ¥") + Money(Sim.ContractIncome(c)) + T("/秒", "/s") + "</color>" : "  <size=13><color=#68748C>" + T("首付 ¥", "advance ¥") + Money(c.signBonus) + "</color></size>");
                bool target = dragging != null && dragging == c.dataset && can;
                bg.color = target ? new Color32(226, 246, 230, 255) : signed ? new Color32(240, 250, 242, 255) : new Color32(247, 249, 253, 255);
                if (signed) btn.Set(T("已签约", "Signed"), false);
                else btn.Set(T("签约", "Sign"), can, can ? XgPalette.Accent : (Color?)null, can ? Color.white : (Color?)null);
            }
            list.sizeDelta = new Vector2(0, rowsHeight + (market != null ? market.Refresh(rowsHeight) : 0));
        }
    }
}
