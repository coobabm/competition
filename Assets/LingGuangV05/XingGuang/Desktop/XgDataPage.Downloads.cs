using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The 数据 page's download strip (节奏重做 §2.1–2.3): the broadband line and its speed, the disk meter, the queue
    /// (name, size, progress, speed, ETA; paused items say why), and each pack's batches as small segments.
    /// </summary>
    public sealed partial class XgDataPage
    {
        const float LineTop = 28, LineHeight = 24, QueueRowHeight = 24;
        const int MaxQueueRows = 3;
        const float SegmentsWidth = 120, SegmentGap = 2;
        /// <summary>The 升级宽带 button at the right end of the line (it is named <see cref="UpgradeBroadbandName"/> for the guide's locator).</summary>
        const float UpgradeWidth = 188;
        public const string UpgradeBroadbandName = "UpgradeBroadband";
        // Queue columns, from the row's left edge (the speed-up button sits on the right like the buy buttons).
        const float QSizeX0 = 300, QSizeX1 = 380, QBarX0 = 392, QBarX1 = 592, QSpeedX0 = 604, QSpeedX1 = 760, QEtaX0 = 768, QEtaX1 = 840;

        sealed class QueueRow
        {
            public RectTransform rt;
            public TMP_Text name, size, speed, eta;
            public RectTransform fill;
            public Image fillImage;
            public XgBtn speedUp;
            public string key = "";
        }

        RectTransform columns;
        TMP_Text lineText, diskText;
        XgBtn upgradeButton;
        RectTransform diskFill;
        Image diskFillImage;
        readonly List<QueueRow> queueRows = new List<QueueRow>();
        int queueShown = -1;
        XgSim downloadsSim;

        public static string BatchButtonName(string dataset) => "Batch " + dataset;

        void BuildDownloads()
        {
            var strip = Strip("Downloads", root, LineTop, LineHeight, 0, BarWidth + 6);
            // Left: the line and its 升级宽带 button (buys the next tier right here). A click on the text still goes to the home bills.
            var line = Rect("Broadband", strip, Vector2.zero, new Vector2(.5f, 1), Vector2.zero, new Vector2(-10, 0));
            Panel(line, new Color(0, 0, 0, 0));
            lineText = ui.Text(Rect("Text", line, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-UpgradeWidth - 6, 0)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            lineText.textWrappingMode = TextWrappingModes.NoWrap; lineText.overflowMode = TextOverflowModes.Ellipsis;
            var lineButton = line.gameObject.AddComponent<Button>(); lineButton.transition = Selectable.Transition.None; lineButton.targetGraphic = line.GetComponent<Image>();
            lineButton.onClick.AddListener(() => OpenApp("home", "power"));
            UiTip.Add(line, () => Sim == null ? "" : BroadbandTip());
            upgradeButton = ui.Button(line, "", UpgradeBroadband, 12);
            upgradeButton.rt.gameObject.name = UpgradeBroadbandName;
            upgradeButton.rt.anchorMin = new Vector2(1, 0); upgradeButton.rt.anchorMax = Vector2.one;
            upgradeButton.rt.offsetMin = new Vector2(-UpgradeWidth, 1); upgradeButton.rt.offsetMax = new Vector2(0, -1);
            upgradeButton.label.enableAutoSizing = true; upgradeButton.label.fontSizeMin = 8; upgradeButton.label.fontSizeMax = 12;
            UiTip.Add(upgradeButton.rt, () => Sim == null ? "" : BroadbandTip());
            upgradeButton.Show(false);

            // Right: the disk meter. A click opens 淘货's storage tab.
            var disk = Rect("Disk", strip, new Vector2(.5f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            Panel(disk, new Color(0, 0, 0, 0));
            var track = Rect("Meter", disk, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -4), new Vector2(140, 4));
            diskFill = Bar(track, "Fill", XgDark.Track, XgDark.Data);
            diskFillImage = diskFill.GetComponent<Image>();
            diskText = ui.Text(Rect("Text", disk, Vector2.zero, Vector2.one, new Vector2(150, 0), Vector2.zero), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            diskText.textWrappingMode = TextWrappingModes.NoWrap; diskText.overflowMode = TextOverflowModes.Ellipsis;
            var diskButton = disk.gameObject.AddComponent<Button>(); diskButton.transition = Selectable.Transition.None; diskButton.targetGraphic = disk.GetComponent<Image>();
            diskButton.onClick.AddListener(() => OpenApp("xunbao", "disk"));
            UiTip.Add(disk, () => Sim == null ? "" : DiskTip());

            for (int i = 0; i < MaxQueueRows; i++) queueRows.Add(MakeQueueRow(i));
        }

        QueueRow MakeQueueRow(int i)
        {
            var q = new QueueRow();
            q.rt = Strip("Queue" + i, root, 0, QueueRowHeight, 0, BarWidth + 6);
            Panel(Rect("Rule", q.rt, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), XgDark.Hairline).raycastTarget = false;
            q.name = Cell(q.rt, "Name", 12, QSizeX0 - 8, 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            q.size = Cell(q.rt, "Size", QSizeX0, QSizeX1, 13, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            var bar = Rect("Progress", q.rt, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(QBarX0, -3), new Vector2(QBarX1, 3));
            q.fill = Bar(bar, "Fill", XgDark.Track, XgDark.Data);
            q.fillImage = q.fill.GetComponent<Image>();
            q.speed = Cell(q.rt, "Speed", QSpeedX0, QSpeedX1, 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            q.eta = Cell(q.rt, "Eta", QEtaX0, QEtaX1, 13, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            q.speedUp = ui.Button(q.rt, "", () => SpeedUp(q), 13);
            q.speedUp.rt.anchorMin = q.speedUp.rt.anchorMax = new Vector2(1, .5f);
            q.speedUp.rt.offsetMin = new Vector2(-BuyWidth - 10, -10); q.speedUp.rt.offsetMax = new Vector2(-10, 10);
            UiTip.Add(q.speedUp.rt, () => T("开一天摆渡云超级会员：这一项立刻下完（硬盘还得放得下）。", "One day of Bodu Cloud super membership: this item comes down at once (it still needs room on the disk)."));
            // Paused rows say why on hover.
            UiTip.Add(q.rt, () => { if (Sim == null) return ""; var why = Sim.DownloadBlocker(out string en); return why != null ? T(why, en) : ""; });
            q.rt.gameObject.SetActive(false);
            return q;
        }

        TMP_Text Cell(RectTransform row, string name, float x0, float x1, float size, Color color, TextAlignmentOptions align)
        {
            var t = ui.Text(Rect(name, row, Vector2.zero, new Vector2(0, 1), new Vector2(x0, 0), new Vector2(x1, 0)), "", size, color, align);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>Puts the queue rows, the column heads and the list under the line strip.</summary>
        void LayoutDownloads(int rows)
        {
            if (queueShown == rows || columns == null || list == null) return;
            queueShown = rows;
            float y = LineTop + LineHeight + 2;
            for (int i = 0; i < queueRows.Count; i++)
            {
                var rt = queueRows[i].rt;
                rt.gameObject.SetActive(i < rows);
                rt.offsetMin = new Vector2(rt.offsetMin.x, -(y + (i + 1) * QueueRowHeight));
                rt.offsetMax = new Vector2(rt.offsetMax.x, -(y + i * QueueRowHeight));
            }
            float headsTop = y + rows * QueueRowHeight + 4;
            columns.offsetMin = new Vector2(columns.offsetMin.x, -(headsTop + 26));
            columns.offsetMax = new Vector2(columns.offsetMax.x, -headsTop);
            list.offsetMax = new Vector2(list.offsetMax.x, -(headsTop + 30));
        }

        // ───────────── refresh ─────────────

        void RefreshDownloads()
        {
            if (Sim == null || lineText == null) return;
            if (downloadsSim != Sim)
            {
                if (downloadsSim != null) downloadsSim.DownloadFinished -= OnDownloadFinished;
                downloadsSim = Sim;
                downloadsSim.DownloadFinished += OnDownloadFinished;
            }

            // 「宽带 ADSL 20M · 2.5 MB/s」, a hint when the line is the slow part, and the upgrade button when a faster line is on offer.
            int tier = Sim.BroadbandTier;
            lineText.text = T("宽带 ", "Line ") + XgSim.BroadbandName(tier, Sim.English) + " · " + N(Sim.LineMBps, "0.#") + " MB/s"
                + (Sim.BroadbandBottleneck ? "  <color=#EF9F27>" + T("太慢 →", "too slow →") + "</color>" : "");
            bool offered = Sim.BroadbandUpgradeOffered;
            upgradeButton.Show(offered);
            if (offered)
                upgradeButton.Set(Sim.BroadbandUpgradeLabel(Sim.English, true), Host.Money + 1e-9 >= Sim.NextBroadbandPrice,
                    Sim.BroadbandBottleneck ? XgDark.AccentSoft : (Color?)null, XgDark.Money);

            // Disk: used / capacity, a note when nearly full, red when downloads stopped.
            double cap = Sim.DiskCapacityMB, used = Math.Min(cap, Sim.DiskUsedMB);
            float k = cap > 0 ? Mathf.Clamp01((float)(used / cap)) : 1;
            SetBar(diskFill, k);
            bool full = Sim.DownloadBlocker(out _) != null;
            diskFillImage.color = full ? XgDark.Bad : k >= .85f ? XgDark.Hot : XgDark.Data;
            string note = full ? "  <color=#E24B4A>" + T("满了，下载暂停 · 去淘货买硬盘 ↗", "full, downloads paused · buy a drive on Taohuo ↗") + "</color>"
                : k >= .85f ? "  <color=#EF9F27>" + T("快满了 · 还空 ", "nearly full · ") + XgSim.SizeText(Sim.DiskFreeMB) + T("", " free") + "</color>" : "";
            diskText.text = T("硬盘 ", "Disk ") + XgSim.SizeText(used) + " / " + XgSim.SizeText(cap) + note;

            // The queue: the first items, the last row sums up the rest when there are more.
            var queue = Sim.DownloadQueue();
            int rows = Math.Min(MaxQueueRows, queue.Count);
            LayoutDownloads(rows);
            var firstOfKey = new HashSet<string>();
            for (int i = 0; i < rows; i++)
            {
                var q = queueRows[i];
                if (i == MaxQueueRows - 1 && queue.Count > MaxQueueRows) { RestRow(q, queue, i); continue; }
                FillQueueRow(q, queue[i], firstOfKey.Add(queue[i].key));
            }
        }

        void FillQueueRow(QueueRow q, XgDownloadInfo x, bool first)
        {
            q.key = x.key;
            string name = T(x.name, x.nameEn);
            bool batch = x.batch > 0 && x.batches > 1;
            q.name.text = (x.campus ? T("老周快传 · ", "老周's copy · ") : "") + name + (batch ? T(" 第 " + x.batch + "/" + x.batches + " 批", " batch " + x.batch + "/" + x.batches) : "");
            q.size.text = XgSim.SizeText(x.sizeMB);
            SetBar(q.fill, (float)x.progress);
            q.fillImage.color = x.paused ? XgDark.Bad : x.active ? XgDark.Data : XgDark.Dim;
            if (x.paused)
            {
                q.speed.text = "<color=#E24B4A>" + T("硬盘满了 · 暂停", "disk full · paused") + "</color>";
                q.eta.text = "--:--";
            }
            else if (x.active)
            {
                // Real MB/s: what is left over the game seconds it still needs, back on the real clock.
                double left = Math.Max(0, x.sizeMB - x.doneMB);
                double mbps = x.secondsLeft > 1e-6 ? left / x.secondsLeft * XgSim.DownloadTimeScale : Sim.LineMBps;
                q.speed.text = N(mbps, mbps < 10 ? "0.##" : "0.#") + " MB/s";
                q.eta.text = XgSim.ClockText(x.eta);
            }
            else
            {
                q.speed.text = T("排队中", "queued");
                q.eta.text = XgSim.ClockText(x.eta);
            }
            // One speed-up per key: it finishes the first item of that key.
            q.speedUp.Show(first);
            if (first)
            {
                double cost = Sim.AccelerateCost(x.key);
                q.speedUp.Set(T("加速 ¥", "Speed up ¥") + Money(cost), Host.Money + 1e-9 >= cost, null, XgDark.Money);
            }
        }

        void RestRow(QueueRow q, List<XgDownloadInfo> queue, int from)
        {
            q.key = "";
            int rest = queue.Count - from;
            double mb = 0, last = 0;
            for (int i = from; i < queue.Count; i++) { mb += queue[i].sizeMB - queue[i].doneMB; last = Math.Max(last, queue[i].eta); }
            q.name.text = T("…还有 " + rest + " 项排队", "…" + rest + " more queued");
            q.size.text = XgSim.SizeText(mb);
            SetBar(q.fill, 0);
            q.speed.text = T("全部下完", "all done in");
            q.eta.text = XgSim.ClockText(last);
            q.speedUp.Show(false);
        }

        // ───────────── batches ─────────────

        Image[] BatchSegments(RectTransform row, int count, float textRight)
        {
            count = Math.Max(1, count);
            var box = Rect("Batches", row, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-textRight - SegmentsWidth, 6), new Vector2(-textRight, 14));
            var segs = new Image[count];
            float w = (SegmentsWidth - SegmentGap * (count - 1)) / count;
            for (int i = 0; i < count; i++)
            {
                var s = Rect("Seg" + i, box, Vector2.zero, new Vector2(0, 1), new Vector2(i * (w + SegmentGap), 0), new Vector2(i * (w + SegmentGap) + w, 0));
                segs[i] = Panel(s, XgDark.Track);
                segs[i].raycastTarget = false;
            }
            return segs;
        }

        /// <summary>Arrived batches lit, bought ones still coming amber, the rest dark.</summary>
        void PaintSegments(OfferRow r, XgDataOffer o)
        {
            if (r.segments == null) return;
            int arrived = o.owned ? o.batchesArrived : 0, bought = o.owned ? o.batchesBought : 0;
            for (int i = 0; i < r.segments.Length; i++)
                r.segments[i].color = i < arrived ? XgDark.Data : i < bought ? XgDark.Hot : XgDark.Track;
        }

        /// <summary>"3/8 批已下，第 4 批 ¥1,240 · 每批 1.2 万条 · 18.8 GB" (the noise part of the source line is kept).</summary>
        string BatchLine(XgDataOffer o, string sourceLine)
        {
            var d = XgCatalog.Dataset(o.datasetId);
            if (d == null) return sourceLine;
            int all = XgSim.PackBatches(d);
            string noise = "";
            int at = sourceLine.IndexOf(" · ", StringComparison.Ordinal);
            if (at >= 0) noise = sourceLine.Substring(at);
            string per = T(" · 每批 ", " · ") + XgSim.SamplesText(XgSim.BatchSamples(d)) + T(" 条 · ", " each · ") + XgSim.SizeText(XgSim.BatchMB(d));
            if (o.source == XgDataSource.Story)
                return o.owned ? T(o.batchesArrived + "/" + all + " 批已到", o.batchesArrived + "/" + all + " batches in") + T(" · 整套 ", " · whole set ") + XgSim.SizeText(d.sizeMB) + noise
                    : T("整套 " + all + " 批 · ", "all " + all + " batches · ") + XgSim.SizeText(d.sizeMB) + noise;
            if (!o.owned)
                return T("分 " + all + " 批，第 1 批 ¥", all + " batches, batch 1 ¥") + Money(o.price) + T("（下载权）", " (the download right)") + per + noise;
            string coming = o.batchesBought > o.batchesArrived ? T("，" + (o.batchesBought - o.batchesArrived) + " 批在下", ", " + (o.batchesBought - o.batchesArrived) + " coming") : "";
            string next = !double.IsInfinity(o.nextBatchPrice)
                ? T("，第 " + (o.batchesBought + 1) + " 批 ¥" + Money(o.nextBatchPrice), ", batch " + (o.batchesBought + 1) + " ¥" + Money(o.nextBatchPrice))
                : o.batchesBought >= all ? T("，已买全", ", all bought") : "";
            return T(o.batchesArrived + "/" + all + " 批已下", o.batchesArrived + "/" + all + " batches in") + coming + next + per + noise;
        }

        /// <summary>
        /// The guide's "batch:&lt;dataset&gt;" locator: scrolls the pack's row into view and returns its buy button
        /// (null when the pack is not listed).
        /// </summary>
        public RectTransform FocusBatch(string dataset)
        {
            if (content == null) return null;
            var t = content.Find("Offer " + dataset + ".pack/" + BatchButtonName(dataset)) as RectTransform;
            if (t == null || !t.gameObject.activeInHierarchy) return null;
            var row = t.parent as RectTransform;
            if (scroll != null && row != null)
            {
                float viewH = list.rect.height, total = content.rect.height, y = -row.anchoredPosition.y - row.rect.height;
                if (total > viewH) scroll.verticalNormalizedPosition = Mathf.Clamp01(1 - Mathf.Clamp(y - viewH * .4f, 0, total - viewH) / (total - viewH));
            }
            return t;
        }

        // ───────────── actions and events ─────────────

        /// <summary>升级宽带: buys the next tier (installation and the first month), the same purchase as the home bills page.</summary>
        void UpgradeBroadband()
        {
            if (Sim == null) return;
            if (Sim.BuyBroadband(Host)) { Fx.Knock(upgradeButton.rt, .15f); Fx.Play(XgJuice.Sfx.Id.Coin); }
            else { Fx.Knock(upgradeButton.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); }
            host.Refresh(true);
        }

        void SpeedUp(QueueRow q)
        {
            if (string.IsNullOrEmpty(q.key)) return;
            if (!Sim.Accelerate(q.key, Host)) { Fx.Knock(q.speedUp.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); host.Refresh(true); return; }
            Fx.Knock(q.speedUp.rt, .15f);
            Fx.Play(XgJuice.Sfx.Id.Coin);
            host.Refresh(true);
        }

        static void OpenApp(string app, string tab)
        {
            var router = UnityEngine.Object.FindAnyObjectByType<ChapterOneDesktopRouter>();
            if (router != null) router.Open(app, tab);
        }

        /// <summary>A batch came in: a light float over its dataset (the sim's own notice is the toast).</summary>
        void OnDownloadFinished(string key)
        {
            if (root == null || !root.gameObject.activeInHierarchy || Sim == null) return;
            var d = XgCatalog.Dataset(key);
            double samples;
            string text;
            if (d != null)
            {
                samples = XgSim.BatchSamples(d);
                int k = Sim.BatchesArrived(key);
                text = T(d.name + " 第 " + k + " 批下完了 · +", d.nameEn + " batch " + k + " is in · +") + XgSim.SamplesText(samples);
            }
            else
            {
                var o = Sim.Offer(key);
                if (o == null) return;
                text = T(o.name + " 下完了 · +", o.nameEn + " is in · +") + XgSim.SamplesText(o.samples);
            }
            RectTransform at = root;
            foreach (var r in datasetRows) if (d != null && r.id == d.id && r.rt.gameObject.activeInHierarchy) { at = r.rt; break; }
            Fx.Float(Fx.At(at), text, XgDark.Data, 16, 50, 1.6f, 1f);
            Fx.Play(XgJuice.Sfx.Id.Click, 1.3f, .6f);
        }

        /// <summary>The line and disk notes, shared with the download strip of 练's 数据来源 view (XgTrainPage.Downloads.cs).</summary>
        public string BroadbandTipText() => BroadbandTip();
        public string DiskTipText() => DiskTip();

        string BroadbandTip()
        {
            int tier = Sim.BroadbandTier;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(T("家里的宽带", "Home broadband")).Append("</b>\n");
            sb.Append(XgSim.BroadbandName(tier, Sim.English)).Append(" · ").Append(N(Sim.LineMBps, "0.#")).Append(" MB/s · ").Append(T("月租 ¥", "¥")).Append(Money(XgSim.BroadbandMonthly[tier])).Append(T("", " a month")).Append('\n');
            sb.Append(T("排着的下载还要 ", "The queue still needs ")).Append(XgSim.ClockText(Sim.LineQueueSeconds)).Append('\n');
            var why = Sim.BroadbandBlocker(out string en);
            if (why == null)
                sb.Append(T("升级到 ", "Upgrade to ")).Append(XgSim.BroadbandName(tier + 1, Sim.English)).Append(" · ").Append(N(XgSim.BroadbandMBps[tier + 1], "0.#")).Append(" MB/s · ¥").Append(Money(Sim.NextBroadbandPrice))
                    .Append(T("（装机 + 首月；之后月租 ¥" + Money(XgSim.BroadbandMonthly[tier + 1]) + "）：点「升级宽带」按钮直接装；点这行字去家里的用电账单页也行。",
                              " (install + month 1; then ¥" + Money(XgSim.BroadbandMonthly[tier + 1]) + " a month): press the Upgrade line button, or click the text for the Home app's bills page."));
            else sb.Append(T(why, en));
            return sb.ToString();
        }

        string DiskTip()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(T("硬盘", "Disk")).Append("</b>\n");
            sb.Append(T("系统和软件 ", "System and software ")).Append(XgSim.SizeText(XgSim.DiskSystemMB)).Append(T(" · 数据 ", " · data ")).Append(XgSim.SizeText(Sim.StoredDataMB)).Append('\n');
            sb.Append(T("还空 ", "Free ")).Append(XgSim.SizeText(Sim.DiskFreeMB)).Append(T(" · 移动硬盘 ", " · portable drives ")).Append(Sim.DrivesOwned).Append('\n');
            sb.Append(T("盘满了下载就停。淘货的 2TB 移动硬盘下一块 ¥", "Downloads stop when it is full. Taohuo's next 2TB drive costs ¥")).Append(Money(Sim.NextDrivePrice)).Append(T("；点这里去淘货。", "; click for Taohuo."));
            if (!Sim.Has(XgSim.DataCleanId)) sb.Append('\n').Append(T("研究「数据清洗」也能压掉 30%。", "Data cleaning research also squeezes out 30%."));
            return sb.ToString();
        }
    }
}
