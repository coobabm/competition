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
    /// The download strip of the 数据来源 view (节奏重做 §2.1–2.3), since the 数据 page has no nav row after the
    /// 2026-10-15 integration: the broadband line and the disk on one line, the next batch of the current dataset,
    /// and up to two queue rows (name and batch, progress, time left or why it is paused, 加速). It sits between the
    /// pack title and the dataset list and pushes the list down by its own height.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const float DlTop = 88, DlLineHeight = 22, DlRowHeight = 22, DlGap = 4;
        const int DlMaxRows = 2;

        sealed class DlRow
        {
            public RectTransform rt, fill;
            public Image fillImage;
            public TMP_Text name, eta;
            public XgBtn speedUp;
            public string key = "";
        }

        RectTransform dlBlock, dlList, dlDiskFill;
        Image dlDiskImage;
        TMP_Text dlLine, dlDisk;
        XgBtn dlBatch, dlUpgrade;
        RectTransform dlUpgradeRow;
        public const string UpgradeBroadbandName = "UpgradeBroadband";
        readonly List<DlRow> dlRows = new List<DlRow>();
        float dlShownHeight = -1;

        void BuildDownloadStrip(RectTransform pane)
        {
            dlList = pane.Find("Datasets") as RectTransform;
            dlBlock = Strip("Downloads", pane, DlTop, DlLineHeight, 12, 12);

            // Left: the line; a click goes to the home bills, where the upgrade is.
            // Pinned to the top line: the block grows downward as queue rows appear.
            var line = Rect("Broadband", dlBlock, new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(0, -DlLineHeight), new Vector2(-6, 0));
            Panel(line, Color.clear);
            dlLine = ui.Text(Rect("Text", line, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            dlLine.textWrappingMode = TextWrappingModes.NoWrap; dlLine.overflowMode = TextOverflowModes.Ellipsis;
            var lineButton = line.gameObject.AddComponent<Button>(); lineButton.transition = Selectable.Transition.None; lineButton.targetGraphic = line.GetComponent<Image>();
            lineButton.onClick.AddListener(() => OpenDesktopApp("home", "power"));
            UiTip.Add(line, () => view.Data != null && Sim != null ? view.Data.BroadbandTipText() : "");

            // Right: the disk with a thin meter under it; a click opens 淘货's storage tab.
            var disk = Rect("Disk", dlBlock, new Vector2(.5f, 1), Vector2.one, new Vector2(6, -DlLineHeight), new Vector2(-130, 0));
            Panel(disk, Color.clear);
            dlDisk = ui.Text(Rect("Text", disk, Vector2.zero, Vector2.one, new Vector2(0, 3), Vector2.zero), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            dlDisk.textWrappingMode = TextWrappingModes.NoWrap; dlDisk.overflowMode = TextOverflowModes.Ellipsis;
            var meter = Rect("Meter", disk, Vector2.zero, new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 3));
            dlDiskFill = Bar(meter, "Fill", XgDark.Track, XgDark.Data);
            dlDiskImage = dlDiskFill.GetComponent<Image>();
            var diskButton = disk.gameObject.AddComponent<Button>(); diskButton.transition = Selectable.Transition.None; diskButton.targetGraphic = disk.GetComponent<Image>();
            diskButton.onClick.AddListener(() => OpenDesktopApp("xunbao", "disk"));
            UiTip.Add(disk, () => view.Data != null && Sim != null ? view.Data.DiskTipText() : "");

            // Far right: the next batch of the dataset on this line.
            dlBatch = ui.Button(dlBlock, "", BuyNextBatch, 11);
            dlBatch.rt.gameObject.name = "NextBatch";
            dlBatch.rt.anchorMin = new Vector2(1, 1); dlBatch.rt.anchorMax = Vector2.one;
            dlBatch.rt.offsetMin = new Vector2(-124, -DlLineHeight); dlBatch.rt.offsetMax = Vector2.zero;
            dlBatch.label.enableAutoSizing = true; dlBatch.label.fontSizeMin = 8; dlBatch.label.fontSizeMax = 11;
            UiTip.Add(dlBatch.rt, () => Sim == null ? "" : T("数据包分批下载：先买下载权和第 1 批，之后每批另买，越往后越贵。买了要等宽带下完才算样本。",
                "Packs come in batches: the first buy is the download right and batch 1, each later batch costs more. A batch counts once the line has brought it down."));

            // Under the queue, while something is downloading and a faster line is on offer: the 升级宽带 button.
            dlUpgradeRow = Strip("Upgrade", dlBlock, DlLineHeight, DlRowHeight, 0, 0);
            dlUpgrade = ui.Button(dlUpgradeRow, "", UpgradeBroadband, 11);
            dlUpgrade.rt.gameObject.name = UpgradeBroadbandName;
            dlUpgrade.rt.anchorMin = Vector2.zero; dlUpgrade.rt.anchorMax = Vector2.one;
            dlUpgrade.rt.offsetMin = new Vector2(0, 1); dlUpgrade.rt.offsetMax = new Vector2(0, -1);
            dlUpgrade.label.enableAutoSizing = true; dlUpgrade.label.fontSizeMin = 8; dlUpgrade.label.fontSizeMax = 11;
            UiTip.Add(dlUpgrade.rt, () => view.Data != null && Sim != null ? view.Data.BroadbandTipText() : "");
            dlUpgradeRow.gameObject.SetActive(false);

            for (int i = 0; i < DlMaxRows; i++)
            {
                var q = new DlRow();
                q.rt = Strip("Queue" + i, dlBlock, DlLineHeight + i * DlRowHeight, DlRowHeight, 0, 0);
                q.rt.anchorMin = new Vector2(0, 1); q.rt.anchorMax = Vector2.one;
                q.name = ui.Text(Rect("Name", q.rt, Vector2.zero, new Vector2(.42f, 1), Vector2.zero, Vector2.zero), "", 11, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                q.name.textWrappingMode = TextWrappingModes.NoWrap; q.name.overflowMode = TextOverflowModes.Ellipsis;
                var bar = Rect("Progress", q.rt, new Vector2(.43f, .5f), new Vector2(.68f, .5f), new Vector2(0, -3), new Vector2(0, 3));
                q.fill = Bar(bar, "Fill", XgDark.Track, XgDark.Data);
                q.fillImage = q.fill.GetComponent<Image>();
                q.eta = ui.Text(Rect("Eta", q.rt, new Vector2(.69f, 0), Vector2.one, Vector2.zero, new Vector2(-96, 0)), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineRight);
                q.eta.textWrappingMode = TextWrappingModes.NoWrap; q.eta.overflowMode = TextOverflowModes.Ellipsis;
                var row = q;
                q.speedUp = ui.Button(q.rt, "", () => SpeedUpDownload(row), 10);
                q.speedUp.rt.anchorMin = new Vector2(1, 0); q.speedUp.rt.anchorMax = Vector2.one;
                q.speedUp.rt.offsetMin = new Vector2(-90, 1); q.speedUp.rt.offsetMax = new Vector2(0, -1);
                q.speedUp.label.enableAutoSizing = true; q.speedUp.label.fontSizeMin = 8; q.speedUp.label.fontSizeMax = 10;
                UiTip.Add(q.speedUp.rt, () => T("开一天摆渡云超级会员：这一项立刻下完（硬盘还得放得下）。", "One day of Bodu Cloud super membership: this item comes down at once (it still needs room on the disk)."));
                UiTip.Add(q.rt, () => { if (Sim == null) return ""; var why = Sim.DownloadBlocker(out string en); return why != null ? T(why, en) : ""; });
                q.rt.gameObject.SetActive(false);
                dlRows.Add(q);
            }
        }

        void RefreshDownloadStrip()
        {
            if (dlBlock == null || Sim == null) return;

            int tier = Sim.BroadbandTier;
            dlLine.text = T("宽带 ", "Line ") + XgSim.BroadbandName(tier, Sim.English) + " · " + N(Sim.LineMBps, "0.#") + " MB/s"
                + (Sim.BroadbandBottleneck ? " <color=#EF9F27>" + T("太慢 ↗", "too slow ↗") + "</color>" : "");

            double cap = Sim.DiskCapacityMB, used = Math.Min(cap, Sim.DiskUsedMB);
            float k = cap > 0 ? Mathf.Clamp01((float)(used / cap)) : 1;
            SetBar(dlDiskFill, k);
            bool full = Sim.DownloadBlocker(out _) != null;
            dlDiskImage.color = full ? XgDark.Bad : k >= .85f ? XgDark.Hot : XgDark.Data;
            dlDisk.text = T("硬盘 ", "Disk ") + XgSim.SizeText(used) + " / " + XgSim.SizeText(cap)
                + (full ? " <color=#E24B4A>" + T("满了 ↗", "full ↗") + "</color>" : k >= .85f ? " <color=#EF9F27>" + T("快满了", "nearly full") + "</color>" : "");

            // The next batch of this line's dataset (public packs only; story sets come whole).
            string dataset = Sim.Run(Track).dataset;
            var d = XgCatalog.Dataset(dataset);
            int all = d != null ? XgSim.PackBatches(d) : 0, bought = d != null ? Sim.BatchesBought(dataset) : 0;
            double next = d != null && bought > 0 ? Sim.NextBatchPrice(dataset) : double.PositiveInfinity;
            bool batchShown = bought > 0 && all > 1;
            dlBatch.Show(batchShown);
            if (batchShown)
            {
                if (double.IsInfinity(next)) dlBatch.Set(T("已买全 " + all + " 批", "all " + all + " batches"), false, XgDark.Button, XgDark.Muted);
                else dlBatch.Set(T("第 " + (bought + 1) + "/" + all + " 批 ¥", "Batch " + (bought + 1) + "/" + all + " ¥") + Money(next), Host.Money + 1e-9 >= next, XgDark.AccentSoft, XgDark.Data);
            }

            // The queue: the first items; the last row sums up the rest.
            var queue = Sim.DownloadQueue();
            int rows = Math.Min(DlMaxRows, queue.Count);
            var firstOfKey = new HashSet<string>();
            for (int i = 0; i < dlRows.Count; i++)
            {
                var q = dlRows[i];
                q.rt.gameObject.SetActive(i < rows);
                if (i >= rows) continue;
                if (i == DlMaxRows - 1 && queue.Count > DlMaxRows) { DownloadRestRow(q, queue, i); continue; }
                FillDownloadRow(q, queue[i], firstOfKey.Add(queue[i].key));
            }

            // 升级宽带 sits under the last queue row while something is downloading.
            bool upgrade = rows > 0 && Sim.BroadbandUpgradeOffered;
            dlUpgradeRow.gameObject.SetActive(upgrade);
            if (upgrade)
            {
                float y = DlLineHeight + rows * DlRowHeight;
                dlUpgradeRow.offsetMin = new Vector2(dlUpgradeRow.offsetMin.x, -(y + DlRowHeight));
                dlUpgradeRow.offsetMax = new Vector2(dlUpgradeRow.offsetMax.x, -y);
                dlUpgrade.Set(Sim.BroadbandUpgradeLabel(Sim.English, true), Host.Money + 1e-9 >= Sim.NextBroadbandPrice,
                    Sim.BroadbandBottleneck ? XgDark.AccentSoft : (Color?)null, XgDark.Money);
            }

            // The strip's height pushes the dataset list down.
            float h = DlLineHeight + (rows + (upgrade ? 1 : 0)) * DlRowHeight;
            if (Math.Abs(h - dlShownHeight) > .1f)
            {
                dlShownHeight = h;
                dlBlock.offsetMin = new Vector2(dlBlock.offsetMin.x, -(DlTop + h));
                if (dlList != null) dlList.offsetMax = new Vector2(dlList.offsetMax.x, -(DlTop + h + DlGap));
            }
        }

        void FillDownloadRow(DlRow q, XgDownloadInfo x, bool first)
        {
            q.key = x.key;
            bool batch = x.batch > 0 && x.batches > 1;
            q.name.text = (x.campus ? T("老周快传 · ", "老周's copy · ") : "") + T(x.name, x.nameEn)
                + (batch ? T(" 第 " + x.batch + "/" + x.batches + " 批", " " + x.batch + "/" + x.batches) : "") + " <color=#6F95A5>" + XgSim.SizeText(x.sizeMB) + "</color>";
            SetBar(q.fill, (float)x.progress);
            q.fillImage.color = x.paused ? XgDark.Bad : x.active ? XgDark.Data : XgDark.Dim;
            if (x.paused) q.eta.text = "<color=#E24B4A>" + T("硬盘满 · 暂停", "disk full") + "</color>";
            else if (x.active)
            {
                q.eta.text = T("模拟下载 · ", "Simulated download · ") + XgSim.ClockText(x.eta);
            }
            else q.eta.text = T("排队 · ", "queued · ") + XgSim.ClockText(x.eta);
            // One speed-up per key: it finishes the first item of that key.
            q.speedUp.Show(first);
            if (first)
            {
                double cost = Sim.AccelerateCost(x.key);
                q.speedUp.Set(T("加速 ¥", "Speed up ¥") + Money(cost), Host.Money + 1e-9 >= cost, null, XgDark.Money);
            }
        }

        void DownloadRestRow(DlRow q, List<XgDownloadInfo> queue, int from)
        {
            q.key = "";
            double mb = 0, last = 0;
            for (int i = from; i < queue.Count; i++) { mb += queue[i].sizeMB - queue[i].doneMB; last = Math.Max(last, queue[i].eta); }
            q.name.text = T("…还有 " + (queue.Count - from) + " 项排队", "…" + (queue.Count - from) + " more queued") + " <color=#6F95A5>" + XgSim.SizeText(mb) + "</color>";
            SetBar(q.fill, 0);
            q.eta.text = T("全部下完 ", "all done in ") + XgSim.ClockText(last);
            q.speedUp.Show(false);
        }

        void SpeedUpDownload(DlRow q)
        {
            if (string.IsNullOrEmpty(q.key) || Sim == null) return;
            if (!Sim.Accelerate(q.key, Host)) { Fx.Knock(q.speedUp.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); view.Refresh(true); return; }
            Fx.Knock(q.speedUp.rt, .15f);
            Fx.Play(XgJuice.Sfx.Id.Coin);
            view.Refresh(true);
        }

        /// <summary>升级宽带: buys the next tier (installation and the first month), the same purchase as the home bills page.</summary>
        void UpgradeBroadband()
        {
            if (Sim == null) return;
            if (Sim.BuyBroadband(Host)) { Fx.Knock(dlUpgrade.rt, .15f); Fx.Play(XgJuice.Sfx.Id.Coin); }
            else { Fx.Knock(dlUpgrade.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); }
            view.Refresh(true);
        }

        void BuyNextBatch()
        {
            if (Sim == null) return;
            if (Sim.BuyBatch(Sim.Run(Track).dataset, Host)) { Fx.Knock(dlBatch.rt, .15f); Fx.Play(XgJuice.Sfx.Id.Coin); }
            else { Fx.Knock(dlBatch.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); }
            view.Refresh(true);
        }

        static void OpenDesktopApp(string app, string tab)
        {
            var router = UnityEngine.Object.FindAnyObjectByType<ChapterOneDesktopRouter>();
            if (router != null) router.Open(app, tab);
        }
    }
}
