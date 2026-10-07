using System;
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
    /// 养成 › 数据 (lingguang-redesign/index.html #data): every dataset it has data for or can get, with its samples,
    /// its quality (the sim's label noise, and a meme drift on the text desks) and 去做题 for desks on 摆渡众包. Under
    /// each dataset are its sources (public pack, 淘货 junk pack, story data, a crowd task) with the existing buy,
    /// speed-up, post and training-switch actions of the data economy (XgSim.DataSources.cs). The 摆渡百科爬虫 source
    /// opens its crawler over this page (<see cref="XgCrawlerPanel"/>); closing it comes back here.
    /// </summary>
    public sealed class XgDataPage : XgPage
    {
        const float RowHeight = 42, OfferHeight = 32;

        sealed class DatasetRow
        {
            public string id;
            public RectTransform rt;
            public TMP_Text index, name, samples, quality;
            public RectTransform qualityFill;
            public Image qualityImage;
            public XgBtn label;
        }

        sealed class OfferRow
        {
            public string id;
            public RectTransform rt;
            public TMP_Text source, line;
            public XgBtn buy, use;
        }

        TMP_Text header, from;
        RectTransform list, content;
        ScrollRect scroll;
        readonly List<DatasetRow> datasetRows = new List<DatasetRow>();
        readonly List<OfferRow> offerRows = new List<OfferRow>();
        string signature = "";
        XgCrawlerPanel crawler;
        /// <summary>The 摆渡百科 crawler is open on this page (the resident spider is inside it then, not on the desktop).</summary>
        public bool CrawlerOpen => crawler != null && crawler.IsOpen && root != null && root.gameObject.activeInHierarchy;
        /// <summary>The crawler page (the spider's first appearance drives it).</summary>
        public XgCrawlerPanel Crawler => crawler;

        /// <summary>Opens the crawler for the spider's first appearance: free, nothing credited, it simply reads.</summary>
        public void OpenCrawlerIntro() { crawler?.Open(true); }

        public override void Build(RectTransform area)
        {
            root = Rect("data", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 0, 22, 2, 420), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            header.textWrappingMode = TextWrappingModes.NoWrap; header.characterSpacing = 2;
            from = ui.Text(Rect("From", root, new Vector2(1, 1), Vector2.one, new Vector2(-420, -22), new Vector2(-2, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            from.textWrappingMode = TextWrappingModes.NoWrap;

            var heads = Strip("Columns", root, 28, 24, 0, 0);
            Panel(Rect("Rule", heads, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), XgDark.Line).raycastTarget = false;
            Head(heads, T("数据集", "Dataset"), 46, 420, TextAlignmentOptions.MidlineLeft);
            Head(heads, T("样本", "Samples"), 430, 560, TextAlignmentOptions.MidlineRight);
            Head(heads, T("质量", "Quality"), 590, 760, TextAlignmentOptions.MidlineLeft);

            list = Rect("List", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -54));
            list.gameObject.AddComponent<RectMask2D>();
            Panel(list, new Color(0, 0, 0, 0));
            content = Rect("Content", list, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = list; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30; scroll.inertia = false;
            crawler = new XgCrawlerPanel(host, ui, root, () => host.Refresh(true));
        }

        public override void Tick(float dt) { crawler?.Tick(dt); }

        void Head(RectTransform parent, string text, float x0, float x1, TextAlignmentOptions align)
        {
            var t = ui.Text(Rect("Head", parent, Vector2.zero, new Vector2(0, 1), new Vector2(x0, 0), new Vector2(x1, 0)), text, 12, XgDark.Muted, align);
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ───────────── what to list ─────────────

        static bool CountsAsData(string id) => id != "xor" && id != "parallel";

        /// <summary>A source worth a line: the public pack always, the others once owned or on offer.</summary>
        static bool ShowOffer(XgDataOffer o) => o.source == XgDataSource.Public || o.owned || o.available;

        /// <summary>A dataset is listed once it has samples, an open desk, or a pack it can buy or owns.</summary>
        bool ShowDataset(XgDataset ds, List<XgDataOffer> offers)
        {
            if (!CountsAsData(ds.id)) return false;
            if (Sim.Samples(ds.id) > 0 || Sim.DeskOpen(ds.id)) return true;
            foreach (var o in offers) if (o.owned || o.available) return true;
            return false;
        }

        public override void Refresh()
        {
            if (Sim == null || content == null) return;
            var shown = new List<(XgDataset ds, List<XgDataOffer> offers)>();
            var sig = new StringBuilder();
            foreach (var ds in XgCatalog.Datasets)
            {
                var offers = Sim.OffersFor(ds.id);
                if (!ShowDataset(ds, offers)) continue;
                var visible = offers.FindAll(ShowOffer);
                shown.Add((ds, visible));
                sig.Append(ds.id).Append(':');
                foreach (var o in visible) sig.Append(o.id).Append(',');
                sig.Append(';');
            }
            if (sig.ToString() != signature) { signature = sig.ToString(); Rebuild(shown); }

            double total = Sim.TrainedSamples;
            header.text = T("样本 · 全部 ", "SAMPLES · ") + XgSim.SamplesText(total) + T(" 条（有效）", " effective");
            from.text = T("来自摆渡众包做题 + 买的数据包", "from labelling on Bodu Crowd + packs you bought");
            foreach (var r in datasetRows) RefreshDataset(r);
            foreach (var r in offerRows) RefreshOffer(r);
        }

        void Rebuild(List<(XgDataset ds, List<XgDataOffer> offers)> shown)
        {
            foreach (Transform child in content) UnityEngine.Object.Destroy(child.gameObject);
            datasetRows.Clear(); offerRows.Clear();
            float y = 0;
            int index = 0;
            foreach (var (ds, offers) in shown)
            {
                datasetRows.Add(MakeDataset(ds, ++index, y));
                y += RowHeight;
                foreach (var o in offers) { offerRows.Add(MakeOffer(o, y)); y += OfferHeight; }
                y += 6;
            }
            content.sizeDelta = new Vector2(0, y);
        }

        DatasetRow MakeDataset(XgDataset ds, int index, float y)
        {
            var r = new DatasetRow { id = ds.id };
            r.rt = Strip("Dataset " + ds.id, content, y, RowHeight, 0, 0);
            Panel(r.rt, XgDark.Card).raycastTarget = false;
            Panel(Rect("Rule", r.rt, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), XgDark.Hairline).raycastTarget = false;
            r.index = ui.Text(Rect("Index", r.rt, Vector2.zero, new Vector2(0, 1), new Vector2(12, 0), new Vector2(40, 0)), index.ToString(), 13, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            r.name = ui.Text(Rect("Name", r.rt, Vector2.zero, new Vector2(0, 1), new Vector2(46, 0), new Vector2(420, 0)), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            r.name.textWrappingMode = TextWrappingModes.NoWrap; r.name.overflowMode = TextOverflowModes.Ellipsis;
            r.samples = ui.Text(Rect("Samples", r.rt, Vector2.zero, new Vector2(0, 1), new Vector2(430, 0), new Vector2(560, 0)), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            r.samples.textWrappingMode = TextWrappingModes.NoWrap;
            var track = Rect("Quality", r.rt, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(590, -4), new Vector2(690, 4));
            r.qualityFill = Bar(track, "Fill", XgDark.Track, XgDark.Data);
            r.qualityImage = r.qualityFill.GetComponent<Image>();
            r.quality = ui.Text(Rect("QualityText", r.rt, Vector2.zero, new Vector2(0, 1), new Vector2(698, 0), new Vector2(760, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            r.quality.textWrappingMode = TextWrappingModes.NoWrap;
            string id = ds.id;
            r.label = ui.Button(r.rt, "", () => GoLabel(id), 13);
            r.label.rt.anchorMin = r.label.rt.anchorMax = new Vector2(1, .5f);
            r.label.rt.offsetMin = new Vector2(-104, -14); r.label.rt.offsetMax = new Vector2(-10, 14);
            UiTip.Add(r.rt, () => DatasetTip(id));
            return r;
        }

        OfferRow MakeOffer(XgDataOffer o, float y)
        {
            var r = new OfferRow { id = o.id };
            r.rt = Strip("Offer " + o.id, content, y, OfferHeight, 46, 0);
            r.source = ui.Text(Rect("Source", r.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(72, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            r.source.textWrappingMode = TextWrappingModes.NoWrap;
            r.line = ui.Text(Rect("Line", r.rt, Vector2.zero, Vector2.one, new Vector2(76, 0), new Vector2(-226, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            r.line.textWrappingMode = TextWrappingModes.NoWrap; r.line.overflowMode = TextOverflowModes.Ellipsis;
            string id = o.id;
            r.use = ui.Button(r.rt, "", () => ToggleUse(id), 12);
            r.use.rt.anchorMin = r.use.rt.anchorMax = new Vector2(1, .5f);
            r.use.rt.offsetMin = new Vector2(-218, -12); r.use.rt.offsetMax = new Vector2(-120, 12);
            r.buy = ui.Button(r.rt, "", () => Buy(id, r.buy.rt), 13);
            r.buy.rt.anchorMin = r.buy.rt.anchorMax = new Vector2(1, .5f);
            r.buy.rt.offsetMin = new Vector2(-114, -12); r.buy.rt.offsetMax = new Vector2(-10, 12);
            UiTip.Add(r.rt, () => { var live = Sim.Offer(id); return live != null ? XgDataUi.OfferTip(Sim, live) : ""; });
            return r;
        }

        // ───────────── refresh rows ─────────────

        void RefreshDataset(DatasetRow r)
        {
            var ds = XgCatalog.Dataset(r.id);
            if (ds == null) return;
            var note = new StringBuilder();
            string drift = Sim.MemeDriftChip(r.id);
            if (drift.Length > 0) note.Append(drift);
            double noise = Sim.NoiseRatio(r.id);
            if (noise >= .05) note.Append(note.Length > 0 ? " · " : "").Append(T("标错 ", "wrong ")).Append(XgDataUi.Pct0(noise));
            if (ds.dataWeight < 1) note.Append(note.Length > 0 ? " · " : "").Append(T("计入总样本 ×", "counts ×")).Append(N(ds.dataWeight, "0.##"));
            var download = XgDataUi.ActiveDownload(Sim, r.id);
            if (download != null) note.Append(note.Length > 0 ? " · " : "").Append(T("下载中 ", "downloading ")).Append(N(download.downloadProgress * 100, "0")).Append('%');
            r.name.text = T(ds.name, ds.nameEn) + (note.Length > 0 ? "  <size=12><color=#6F95A5>· " + note + "</color></size>" : "");
            r.samples.text = XgSim.SamplesText(Sim.Samples(r.id));
            // Quality: the share of rows labelled right, less a meme drift's lost points.
            float q = Mathf.Clamp01((float)(1 - noise - Sim.MemeDriftPenalty(r.id)));
            SetBar(r.qualityFill, q);
            r.qualityImage.color = q >= .9f ? XgDark.Data : XgDark.Hot;
            r.quality.text = N(q * 100, "0") + "%";
            bool desk = XgCatalog.Desk(r.id) != null;
            r.label.Show(desk);
            if (desk) r.label.Set(T("去做题", "Label"), Sim.DeskOpen(r.id), null, XgDark.Link);
        }

        void RefreshOffer(OfferRow r)
        {
            var o = Sim.Offer(r.id);
            if (o == null) return;
            r.source.text = "[" + o.SourceLabel(Sim.English) + "]";
            r.source.color = o.source == XgDataSource.Junk ? XgDark.Hot : o.source == XgDataSource.Crowd ? XgDark.Link : o.source == XgDataSource.Story ? XgDark.Params : o.source == XgDataSource.Crawl ? XgDark.Good : XgDark.Data;
            string line = "<color=#D6EEF5>" + o.Name(Sim.English) + "</color>  " + XgDataUi.SourceLine(o);
            if (!o.owned && !o.available && o.lockedReason.Length > 0) line += "  <color=#E07B4F>" + T(o.lockedReason, o.lockedReasonEn) + "</color>";
            r.line.text = line;
            // Buy / speed up / post or withdraw / owned.
            if (o.downloading)
            {
                double cost = Sim.AccelerateOfferCost(o.id);
                r.buy.Set(T("加速 ¥", "Speed up ¥") + Money(cost), Host.Money + 1e-9 >= cost, null, XgDark.Money);
            }
            else if (o.source == XgDataSource.Crawl)
            {
                if (o.owned) r.buy.Set(T("回到爬虫", "Back to it"), true, null, XgDark.Good);
                else r.buy.Set(T("爬一次 ¥", "Crawl ¥") + Money(o.price), o.available && Host.Money + 1e-9 >= o.price, null, XgDark.Money);
            }
            else if (o.source == XgDataSource.Crowd) r.buy.Set(o.owned ? T("撤包", "Withdraw") : T("发包", "Post"), o.owned || o.available, null, XgDark.Link);
            else if (o.owned) r.buy.Set(T("✓ 已拥有", "✓ Owned"), false);
            else r.buy.Set(T("购买 ¥", "Buy ¥") + Money(o.price), o.available && Host.Money + 1e-9 >= o.price, null, XgDark.Money);
            // Owned packs can be left out of training (the training page's switches).
            bool switchable = o.owned && XgSim.PackSwitchable(o);
            r.use.Show(switchable);
            if (switchable) r.use.Set(o.included ? T("■ 训练用", "■ In training") : T("□ 不用", "□ Left out"), true, null, o.included ? XgDark.Good : XgDark.Muted);
        }

        string DatasetTip(string id)
        {
            var ds = XgCatalog.Dataset(id);
            if (ds == null) return "";
            return "<b>" + T(ds.name, ds.nameEn) + "</b>\n" + (string.IsNullOrEmpty(ds.note) ? "" : T(ds.note, ds.noteEn) + "\n") + XgDataUi.SourcesSummary(Sim, id)
                + (Sim.MemeDriftChip(id).Length > 0 ? "\n" + T("新题型：模型没见过这个月的新梗。亲手标、再练几轮就追上了。", "A new meme: the model has not seen this month's slang. Label by hand and train a few epochs to catch up.") : "");
        }

        // ───────────── actions ─────────────

        void GoLabel(string dataset)
        {
            if (!Sim.DeskOpen(dataset)) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            Fx.Play(XgJuice.Sfx.Id.Click);
            Sim.SelectDesk(dataset);
            view.Controller.OpenCrowd("label");
        }

        void Buy(string offerId, RectTransform from)
        {
            var o = Sim.Offer(offerId);
            if (o == null) return;
            bool ok;
            if (o.source == XgDataSource.Crawl)
            {
                // A run already going on is only shown again; a new one is paid for first.
                if (!o.owned && !Sim.StartCrawl(Host)) { Fx.Knock(from, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); host.Refresh(true); return; }
                Fx.Knock(from, .15f);
                Fx.Play(o.owned ? XgJuice.Sfx.Id.Click : XgJuice.Sfx.Id.Coin);
                crawler.Open();
                host.Refresh(true);
                return;
            }
            if (o.downloading) ok = Sim.AccelerateOffer(offerId, Host);
            else if (o.source == XgDataSource.Crowd) ok = Sim.SetCrowd(o.datasetId, !o.owned);
            else ok = Sim.BuyDataOffer(offerId, Host);
            if (!ok) { Fx.Knock(from, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); host.Refresh(true); return; }
            Fx.Knock(from, .15f);
            if (o.source != XgDataSource.Crowd && !o.downloading) Fx.Burst(Fx.At(from), 18, XgDark.Gold, XgJuice.Shape.Yen, 260);
            Fx.Play(XgJuice.Sfx.Id.Coin);
            host.Refresh(true);
        }

        void ToggleUse(string offerId)
        {
            var o = Sim.Offer(offerId);
            if (o == null) return;
            string why = Sim.PackSwitchBlocker(offerId, out string en);
            if (why != null) { view.ShowToast(T(why, en), 3); Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            if (Sim.SetPackIncluded(offerId, !o.included)) Fx.Play(XgJuice.Sfx.Id.Click);
            host.Refresh(true);
        }
    }
}
