using System;
using System.Collections.Generic;
using LingGuangV05.Core.Hardware;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// One item of the download queue (节奏重做 §2.1): a batch of a public pack, a junk pack, or 老周's campus copy of a
    /// story pack. Its samples arrive together when the last megabyte is in.
    /// </summary>
    [Serializable]
    public sealed class XgDownload
    {
        /// <summary>Download key: the dataset id for public packs and story data, the offer id for junk packs.</summary>
        public string key = "";
        public string dataset = "";
        /// <summary>The offer it came from ("imagenet.pack", "mnist.junk", "imagenet.story").</summary>
        public string offer = "";
        /// <summary>Public packs: which batch (1-based) and how many it carries (1). Story copies carry every batch; junk packs none.</summary>
        public int batch, batches;
        /// <summary>Real megabytes in all and already on the disk, and the samples it brings.</summary>
        public double sizeMB, doneMB, samples;
        /// <summary>老周 brings it over (a copy he already has): runs beside the home line, at its own pace.</summary>
        public bool campus;
        /// <summary>Lands on the drive the story price paid for: takes no room on the lab's disk.</summary>
        public bool ownDrive;
    }

    /// <summary>A queue item as the pages show it (fresh copy from <see cref="XgSim.DownloadQueue"/>).</summary>
    public sealed class XgDownloadInfo
    {
        public string key = "", dataset = "", offer = "", name = "", nameEn = "";
        public int batch, batches;
        public double sizeMB, doneMB, samples;
        /// <summary>0–1 done.</summary>
        public double progress;
        /// <summary>Game seconds this item still needs at the current speed, and until it arrives (the items ahead included).</summary>
        public double secondsLeft, eta;
        /// <summary>Downloading right now (the head of the line, or a campus copy).</summary>
        public bool active;
        public bool campus;
        /// <summary>Stopped because the disk is full.</summary>
        public bool paused;
    }

    public sealed partial class XgState
    {
        /// <summary>0 = a save from before the download queue (its packs count as fully downloaded, XgSim.Downloads.cs).</summary>
        public int downloadsVersion;
        /// <summary>Waiting and running downloads, in the order they come down the line.</summary>
        public List<XgDownload> downloadQueue = new List<XgDownload>();
        /// <summary>Public-pack batches bought and arrived per dataset. A dataset owned without an entry is a whole pack (older saves).</summary>
        public List<XgLabelCount> batchesBought = new List<XgLabelCount>();
        public List<XgLabelCount> batchesArrived = new List<XgLabelCount>();
        /// <summary>Broadband tier (<see cref="XgSim.BroadbandMBps"/>) and the month (yyyymm) whose extra fee is paid.</summary>
        public int broadband, broadbandBilled;
        /// <summary>Portable drives bought on 淘货.</summary>
        public int drives;
        /// <summary>Room an older save got for the data it already held.</summary>
        public double diskBonusMB;
        /// <summary>The downloads have stopped for a full disk at least once (the drive listing is worth showing from then on).</summary>
        public bool diskFullSeen;
    }

    /// <summary>
    /// 节奏重做 §2.1–2.3: data takes money and time. A data pack buys the download right; its samples come down the home
    /// broadband one item at a time and fill a disk with a limit. Big datasets come in batches that get dearer. All the
    /// sizes are real 2016 megabytes; <see cref="DownloadTimeScale"/> turns a real download into game seconds.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int DownloadsSchema = 1;
        /// <summary>Game seconds per real second of downloading (one 15 GB ImageNet batch: about 30 s on 20M fibre, 6 s on 100M, 2.5 min on ADSL).</summary>
        public const double DownloadTimeScale = 1.0 / 200;
        /// <summary>Broadband tiers: speed (MB/s), monthly fee, installation, the stage that offers it.</summary>
        public static readonly double[] BroadbandMBps = { .5, 2.5, 12.5 };
        public static readonly double[] BroadbandMonthly = { 100, 150, 300 };
        public static readonly double[] BroadbandInstall = { 0, 300, 600 };
        public static readonly int[] BroadbandStage = { 1, 2, 5 };
        static readonly string[] BroadbandNamesZh = { "ADSL 4M", "光纤 20M", "光纤 100M" }, BroadbandNamesEn = { "4M ADSL", "20M fibre", "100M fibre" };
        /// <summary>The disk the lab starts with and what the system and software take of it (MB).</summary>
        public const double DiskStartMB = 500000, DiskSystemMB = 100000;
        /// <summary>数据清洗 also de-duplicates: stored data takes this share less room.</summary>
        public const double CleanCompression = .3;
        /// <summary>Batch n (0 = the pack node itself) costs the pack's base price × this^n.</summary>
        public const double BatchPriceGrowth = 1.35;
        /// <summary>迅雷 with three sources: junk packs come down at this share of the line.</summary>
        public const double JunkLineShare = 1 / 1.5;
        /// <summary>An item this quick (game seconds) with nothing ahead of it arrives at once (small text packs).</summary>
        public const double InstantDownloadSeconds = .5;
        /// <summary>摆渡云 super member: ¥ per game second of download it saves (at least ¥50).</summary>
        public const double AcceleratePerSecond = 15, AccelerateMin = 50;
        /// <summary>The note calls the line the bottleneck when the queue needs longer than this (game seconds).</summary>
        public const double BroadbandSlowSeconds = 90;

        /// <summary>An item was queued (key, game seconds until it arrives).</summary>
        public event Action<string, double> DownloadStarted;
        /// <summary>An item arrived (key).</summary>
        public event Action<string> DownloadFinished;

        bool diskPausedSaid;

        // ───────────── batches ─────────────

        /// <summary>Batches a public pack comes in: 20 from four million samples (人民日报, one a year), 10 from a million, 8 from 100k, else 5.</summary>
        public static int PackBatches(XgDataset d) => d == null ? 1 : d.samples >= 4e6 ? 20 : d.samples >= 1e6 ? 10 : d.samples >= 1e5 ? 8 : 5;
        public static int PackBatches(string dataset) => PackBatches(XgCatalog.Dataset(dataset));
        public static double BatchSamples(XgDataset d) => d == null ? 0 : d.samples / PackBatches(d);
        public static double BatchMB(XgDataset d) => d == null ? 0 : d.sizeMB / PackBatches(d);

        static bool HasCount(List<XgLabelCount> list, string id) { foreach (var l in list) if (l.dataset == id) return true; return false; }

        /// <summary>The story copy of this dataset was bought (it is the whole pack, by 老周's road).</summary>
        bool StoryOwned(string dataset) => S.dataOffers != null && S.dataOffers.Contains(dataset + ".story");

        /// <summary>Batches of the public pack bought (story copies and packs of older saves are whole).</summary>
        public int BatchesBought(string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null || S.owned == null || !S.owned.Contains(dataset)) return 0;
            if (StoryOwned(dataset) || !HasCount(S.batchesBought, dataset)) return PackBatches(d);
            return Math.Max(0, Math.Min(PackBatches(d), (int)Count(S.batchesBought, dataset)));
        }

        /// <summary>Batches of the public pack (or its story copy) that arrived: what <see cref="PackSamples"/> counts.</summary>
        public int BatchesArrived(string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null || S.owned == null || !S.owned.Contains(dataset)) return 0;
            if (!HasCount(S.batchesBought, dataset)) return PackBatches(d);
            return Math.Max(0, Math.Min(PackBatches(d), (int)Count(S.batchesArrived, dataset)));
        }

        /// <summary>The price of batch <paramref name="index"/> (0 = the pack node, the download right with the first batch).</summary>
        public double BatchPrice(string dataset, int index)
        {
            var n = XgCatalog.Node(dataset + ".pack");
            if (n == null) return double.PositiveInfinity;
            if (index <= 0) return NodeCost(n);
            return Math.Ceiling(n.cost * StoryResearchPrice * (1 - Math.Min(.7, RelicFx.techCut)) * Math.Pow(BatchPriceGrowth, index));
        }

        /// <summary>The next batch's price, or infinity when every batch is bought.</summary>
        public double NextBatchPrice(string dataset)
        {
            int bought = BatchesBought(dataset);
            return bought >= PackBatches(dataset) ? double.PositiveInfinity : BatchPrice(dataset, bought);
        }

        /// <summary>Why the next batch cannot be bought now (null when it can; money is not checked).</summary>
        public string BatchBlocker(string dataset, out string en)
        {
            en = null;
            var d = XgCatalog.Dataset(dataset);
            var n = XgCatalog.Node(dataset + ".pack");
            if (d == null || n == null) { en = "No such pack"; return "没有这个数据包"; }
            if (!Has(n.id) || !S.owned.Contains(dataset)) { en = "Buy the " + d.nameEn + " pack first"; return "先在科技树买「" + d.name + "」数据包"; }
            if (StoryOwned(dataset)) { en = "老周's copy is the whole set"; return "老周给的是整套，不用分批"; }
            if (BatchesBought(dataset) >= PackBatches(d)) { en = "Every batch is bought"; return "已经买全了"; }
            return null;
        }

        /// <summary>Buys the next batch of an owned pack: it joins the download queue.</summary>
        public bool BuyBatch(string dataset, IXgHost host)
        {
            if (host == null) return false;
            var why = BatchBlocker(dataset, out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            double price = NextBatchPrice(dataset);
            if (!host.Spend(price)) { Say(T("经费不足 ¥") + F(price, "0")); return false; }
            S.totalSpent += price;
            var d = XgCatalog.Dataset(dataset);
            int batch = BatchesBought(dataset) + 1;
            SetCount(S.batchesBought, dataset, batch);
            var x = Enqueue(new XgDownload { key = dataset, dataset = dataset, offer = dataset + ".pack", batch = batch, batches = 1, sizeMB = BatchMB(d), samples = BatchSamples(d) }, true);
            Say(T("买下《" + d.name + "》第 " + batch + "/" + PackBatches(d) + " 批 ¥" + F(price, "0") + QueueNote(x, false) + "。",
                  "Bought batch " + batch + "/" + PackBatches(d) + " of " + d.nameEn + " for ¥" + F(price, "0") + QueueNote(x, true) + "."));
            return true;
        }

        /// <summary>The pack node was bought: the download right and the first batch (XgSim.BuyNode).</summary>
        void StartDownload(string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null || StoryOwned(dataset)) return;
            if (HasCount(S.batchesBought, dataset) && Count(S.batchesBought, dataset) > 0) return;
            SetCount(S.batchesBought, dataset, 1);
            SetCount(S.batchesArrived, dataset, 0);
            var x = Enqueue(new XgDownload { key = dataset, dataset = dataset, offer = dataset + ".pack", batch = 1, batches = 1, sizeMB = BatchMB(d), samples = BatchSamples(d) }, true);
            if (x != null)
                Say(T("《" + d.name + "》第 1/" + PackBatches(d) + " 批开始下载" + QueueNote(x, false) + "。",
                      "Batch 1/" + PackBatches(d) + " of " + d.nameEn + " is downloading" + QueueNote(x, true) + "."));
        }

        /// <summary>"：650 MB，ADSL 4M 约 1:05" for an item just queued; null means it already arrived.</summary>
        string QueueNote(XgDownload x, bool english)
        {
            if (x == null) return english ? ", already in" : "，已经下好了";
            string size = SizeText(x.sizeMB), eta = ClockText(DownloadEta(x));
            return english ? " (" + size + ", about " + eta + " on " + BroadbandName(BroadbandTier, true) + ")" : "：" + size + "，" + BroadbandName(BroadbandTier, false) + " 约 " + eta;
        }

        /// <summary>"150 GB", "650 MB", "0.5 MB".</summary>
        public static string SizeText(double mb)
        {
            if (!(mb > 0)) return "0 MB";
            if (mb >= 1000000) return F(mb / 1000000, "0.#") + " TB";
            if (mb >= 1000) return F(mb / 1000, "0.#") + " GB";
            return F(mb, mb < 10 ? "0.#" : "0") + " MB";
        }

        /// <summary>Game seconds as "2:10" (or "1:02:10").</summary>
        public static string ClockText(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--:--";
            int s = (int)Math.Ceiling(Math.Max(0, seconds));
            return s >= 3600 ? (s / 3600) + ":" + (s / 60 % 60).ToString("00") + ":" + (s % 60).ToString("00") : (s / 60) + ":" + (s % 60).ToString("00");
        }

        // ───────────── the queue ─────────────

        bool QueueHas(string key) { foreach (var x in S.downloadQueue) if (x.key == key) return true; return false; }

        XgDownload FirstQueued(string key) { foreach (var x in S.downloadQueue) if (x.key == key) return x; return null; }

        /// <summary>The item the home line is downloading (campus copies run beside it).</summary>
        XgDownload LineHead() { foreach (var x in S.downloadQueue) if (!x.campus) return x; return null; }

        /// <summary>
        /// Nothing of this download key is usable yet: a public pack with no batch in, a junk pack or a story copy still
        /// coming. Later batches of a pack already in do not count (training goes on with what is there).
        /// </summary>
        public bool Downloading(string key)
        {
            if (string.IsNullOrEmpty(key) || !QueueHas(key)) return false;
            return XgCatalog.Dataset(key) == null || BatchesArrived(key) == 0;
        }

        /// <summary>Game seconds until the next item of this key arrives (the items ahead of it included); 0 when none is queued.</summary>
        public double DownloadLeft(string key)
        {
            var x = FirstQueued(key);
            return x == null ? 0 : Math.Max(1e-3, DownloadEta(x));
        }

        /// <summary>The home line's speed in real MB/s.</summary>
        public double LineMBps => BroadbandMBps[BroadbandTier];

        /// <summary>Real megabytes this item comes down at per game second.</summary>
        double ItemRate(XgDownload x)
        {
            if (x.campus)
            {
                var o = DataOfferDef(x.offer);
                double secs = o != null && o.downloadSec > 0 ? o.downloadSec : 60;
                return Math.Max(1e-9, x.sizeMB / secs);
            }
            return LineMBps / DownloadTimeScale * (x.batches == 0 ? JunkLineShare : 1);
        }

        double SecondsLeft(XgDownload x) => Math.Max(0, x.sizeMB - x.doneMB) / ItemRate(x);

        /// <summary>Game seconds until this item arrives at the current speed (a full disk is not counted: see <see cref="DownloadBlocker"/>).</summary>
        double DownloadEta(XgDownload x)
        {
            if (x.campus) return SecondsLeft(x);
            double t = 0;
            foreach (var y in S.downloadQueue)
            {
                if (y.campus) continue;
                t += SecondsLeft(y);
                if (y == x) break;
            }
            return t;
        }

        /// <summary>Game seconds until the home line is idle at the current speed.</summary>
        public double LineQueueSeconds { get { double t = 0; foreach (var x in S.downloadQueue) if (!x.campus) t += SecondsLeft(x); return t; } }

        /// <summary>
        /// Samples still on their way for the data bar: queued items of datasets that count, at the dataset's weight,
        /// minus their noise penalty; <paramref name="slowest"/> is the item that arrives last.
        /// </summary>
        public double QueuedDataSamples(out XgDownload slowest)
        {
            double n = 0, last = -1; slowest = null;
            foreach (var x in S.downloadQueue)
            {
                var d = XgCatalog.Dataset(x.dataset);
                if (d == null || !CountsAsData(d.id)) continue;
                var o = DataOfferDef(x.offer);
                n += d.dataWeight * x.samples * (1 - NoisePenalty * (o != null ? o.noise : 0));
                double eta = DownloadEta(x);
                if (eta > last) { last = eta; slowest = x; }
            }
            return n;
        }

        /// <summary>Game seconds until this queued item arrives (0 for null).</summary>
        public double DownloadEtaOf(XgDownload x) => x == null ? 0 : DownloadEta(x);

        /// <summary>The queue as the pages show it, in order (campus copies first).</summary>
        public List<XgDownloadInfo> DownloadQueue()
        {
            var list = new List<XgDownloadInfo>();
            bool paused = DownloadBlocker(out _) != null;
            var head = LineHead();
            foreach (var pass in new[] { true, false })
                foreach (var x in S.downloadQueue)
                {
                    if (x.campus != pass) continue;
                    var d = XgCatalog.Dataset(x.dataset);
                    var o = DataOfferDef(x.offer);
                    bool junk = o != null && o.source == XgDataSource.Junk;
                    list.Add(new XgDownloadInfo
                    {
                        key = x.key, dataset = x.dataset, offer = x.offer, batch = x.batch, batches = PackBatches(d),
                        name = junk ? o.name : d != null ? d.name : x.key, nameEn = junk ? o.nameEn : d != null ? d.nameEn : x.key,
                        sizeMB = x.sizeMB, doneMB = x.doneMB, samples = x.samples, progress = x.sizeMB > 0 ? Clamp01(x.doneMB / x.sizeMB) : 1,
                        secondsLeft = SecondsLeft(x), eta = DownloadEta(x), campus = x.campus, active = x.campus || x == head, paused = !x.campus && paused,
                    });
                }
            return list;
        }

        /// <summary>
        /// Adds an item. With nothing ahead on the line, an item this quick (and room for it) arrives at once; then null is
        /// returned. <paramref name="quiet"/> skips the 下载完成 line for such an instant arrival.
        /// </summary>
        XgDownload Enqueue(XgDownload x, bool quiet)
        {
            S.downloadQueue.Add(x);
            if (!x.campus && LineHead() == x && SecondsLeft(x) <= InstantDownloadSeconds && (x.ownDrive || DiskFreeMB >= x.sizeMB))
            {
                x.doneMB = x.sizeMB;
                Arrive(x, quiet);
                return null;
            }
            DownloadStarted?.Invoke(x.key, DownloadEta(x));
            return x;
        }

        void Arrive(XgDownload x, bool quiet = false)
        {
            S.downloadQueue.Remove(x);
            var d = XgCatalog.Dataset(x.dataset);
            if (x.batches > 0) SetCount(S.batchesArrived, x.dataset, Math.Min(PackBatches(d), Count(S.batchesArrived, x.dataset) + x.batches));
            if (!quiet)
            {
                if (x.campus) Say(T("老周那边传完了：", "老周's copy is in: ") + DownloadName(x.offer) + T("（" + SamplesText(x.samples) + " 条）", " (" + SamplesText(x.samples) + " samples)"));
                else if (x.batches > 0 && d != null)
                    Say(T("下载完成：《" + d.name + "》第 " + x.batch + "/" + PackBatches(d) + " 批，+" + SamplesText(x.samples) + " 条。",
                          "Downloaded: batch " + x.batch + "/" + PackBatches(d) + " of " + d.nameEn + ", +" + SamplesText(x.samples) + " samples."));
                else Say(T("下载完成：") + DownloadName(x.key));
            }
            foreach (var run in Runs) if (run.dataset == x.dataset) Evaluate(run);
            CheckDesks();
            DownloadFinished?.Invoke(x.key);
        }

        void TickDownloads(double dt)
        {
            if (S.downloadQueue.Count == 0 || !(dt > 0)) return;
            // 老周's campus link runs beside the home line.
            foreach (var x in S.downloadQueue.ToArray())
            {
                if (!x.campus) continue;
                x.doneMB = Math.Min(x.sizeMB, x.doneMB + ItemRate(x) * dt);
                if (x.doneMB >= x.sizeMB - 1e-6) Arrive(x);
            }
            double time = dt;
            while (time > 1e-9)
            {
                var x = LineHead();
                if (x == null) break;
                double free = x.ownDrive ? double.PositiveInfinity : DiskFreeMB;
                if (free <= 1e-6) { DiskFull(); return; }
                double rate = ItemRate(x), step = Math.Min(x.sizeMB - x.doneMB, Math.Min(rate * time, free));
                x.doneMB += step; time -= step / rate;
                if (x.doneMB >= x.sizeMB - 1e-6) { x.doneMB = x.sizeMB; Arrive(x); continue; }
                if (step >= free - 1e-6) { DiskFull(); return; }
            }
            diskPausedSaid = false;
        }

        void DiskFull()
        {
            S.diskFullSeen = true;
            if (diskPausedSaid) return;
            diskPausedSaid = true;
            Say(T("硬盘满了，下载暂停：去淘货买一块 2TB 移动硬盘（¥" + F(NextDrivePrice, "0") + "）" + (Has(DataCleanId) ? "。" : "，或者研究「数据清洗」压一压。"),
                  "The disk is full and downloads are paused: buy a 2TB portable drive on Taohuo (¥" + F(NextDrivePrice, "0") + ")" + (Has(DataCleanId) ? "." : ", or research Data cleaning to squeeze it.")));
        }

        /// <summary>Why the home line is not moving although something is queued (null when it moves or is idle).</summary>
        public string DownloadBlocker(out string en)
        {
            en = null;
            var head = LineHead();
            if (head == null || head.ownDrive) return null;
            if (DiskFreeMB <= 1e-6) { en = "Disk full: downloads paused. Buy a portable drive on Taohuo"; return "硬盘满了，下载暂停：去淘货买一块移动硬盘"; }
            return null;
        }

        /// <summary>The line is the slow part: the queue needs a while and a faster tier is on offer.</summary>
        public bool BroadbandBottleneck => LineHead() != null && LineQueueSeconds > BroadbandSlowSeconds && BroadbandBlocker(out _) == null;

        /// <summary>摆渡云 super member for the next item of this key: ¥ for the seconds it saves, at least ¥50.</summary>
        public double AccelerateCost(string key)
        {
            var x = FirstQueued(key);
            return x == null ? AccelerateMin : Math.Max(AccelerateMin, Math.Ceiling(SecondsLeft(x) * AcceleratePerSecond));
        }

        /// <summary>Finishes the next item of this key at once (it still needs the room on the disk).</summary>
        public bool Accelerate(string key, IXgHost host)
        {
            var x = FirstQueued(key);
            if (x == null || host == null) return false;
            if (DiskFreeMB < x.sizeMB - x.doneMB - 1e-6 && !x.ownDrive) { Say(T("硬盘放不下了，先去淘货买块硬盘。", "No room on the disk: buy a drive on Taohuo first.")); return false; }
            double cost = AccelerateCost(key);
            if (!host.Spend(cost)) { Say(T("经费不足 ¥") + F(cost, "0")); return false; }
            S.totalSpent += cost;
            x.doneMB = x.sizeMB;
            Say(T("开通了一天摆渡云超级会员，", "One day of Bodu Cloud super membership: ") + DownloadName(x.offer) + T(" 秒下完。", " came down at once."));
            Arrive(x, true);
            return true;
        }

        // ───────────── the disk ─────────────

        public double DiskCapacityMB => DiskStartMB + Math.Max(0, S.drives) * HardwareCatalog.PortableDrive.capacityMB + Math.Max(0, S.diskBonusMB);

        /// <summary>Downloaded data on the disk (MB, after 数据清洗's compression). Story copies sit on the drive their price paid for.</summary>
        public double StoredDataMB
        {
            get
            {
                double mb = 0;
                if (S.owned != null)
                    foreach (var id in S.owned)
                    {
                        var d = XgCatalog.Dataset(id);
                        if (d == null || StoryOwned(id)) continue;
                        mb += BatchesArrived(id) * BatchMB(d);
                    }
                if (S.dataOffers != null)
                    foreach (var id in S.dataOffers)
                    {
                        var o = DataOfferDef(id);
                        if (o == null || o.source != XgDataSource.Junk || QueueHas(o.id)) continue;
                        mb += o.sizeMB;
                    }
                bool clean = S.unlocked != null && Has(DataCleanId);
                return mb * (clean ? 1 - CleanCompression : 1);
            }
        }

        /// <summary>The system, the stored data and the part of the current download already written.</summary>
        public double DiskUsedMB { get { var head = LineHead(); return DiskSystemMB + StoredDataMB + (head != null && !head.ownDrive ? head.doneMB : 0); } }
        public double DiskFreeMB => Math.Max(0, DiskCapacityMB - DiskUsedMB);
        public bool DiskFullSeen => S.diskFullSeen;
        public int DrivesOwned => Math.Max(0, S.drives);
        public double NextDrivePrice => HardwareCatalog.DrivePrice(DrivesOwned);

        /// <summary>Buys a portable drive on 淘货 (each one ×1.3 the price of the last); paused downloads go on at once.</summary>
        public bool BuyDrive(IXgHost host)
        {
            if (host == null) return false;
            double price = NextDrivePrice;
            if (!host.Spend(price)) { Say(T("经费不足 ¥") + F(price, "0")); return false; }
            S.totalSpent += price;
            S.drives = DrivesOwned + 1;
            diskPausedSaid = false;
            Say(T("淘货到手：" + HardwareCatalog.PortableDrive.name + "（¥" + F(price, "0") + "），硬盘一共 " + SizeText(DiskCapacityMB) + "，还空 " + SizeText(DiskFreeMB) + "。",
                  "Taohuo delivered: " + HardwareCatalog.PortableDrive.nameEn + " (¥" + F(price, "0") + "). Disk space " + SizeText(DiskCapacityMB) + ", " + SizeText(DiskFreeMB) + " free."));
            return true;
        }

        // ───────────── broadband ─────────────

        public int BroadbandTier => Math.Max(0, Math.Min(BroadbandMBps.Length - 1, S.broadband));
        public static string BroadbandName(int tier, bool english) { tier = Math.Max(0, Math.Min(BroadbandMBps.Length - 1, tier)); return english ? BroadbandNamesEn[tier] : BroadbandNamesZh[tier]; }
        /// <summary>What the line adds to the house's ¥100 broadband bill each month.</summary>
        public double BroadbandExtraPerMonth => BroadbandMonthly[BroadbandTier] - BroadbandMonthly[0];

        /// <summary>The next tier's installation plus its first month's extra fee (infinity at the top).</summary>
        public double NextBroadbandPrice
        {
            get
            {
                int next = BroadbandTier + 1;
                return next >= BroadbandMBps.Length ? double.PositiveInfinity : BroadbandInstall[next] + BroadbandMonthly[next] - BroadbandMonthly[0];
            }
        }

        /// <summary>Why the next tier cannot be installed now (null when it can; money is not checked).</summary>
        public string BroadbandBlocker(out string en)
        {
            en = null;
            int next = BroadbandTier + 1;
            if (next >= BroadbandMBps.Length) { en = "Already the fastest line"; return "已经是最快的宽带"; }
            if (S.stage < BroadbandStage[next]) { en = BroadbandName(next, true) + " is not offered here yet (stage " + BroadbandStage[next] + ")"; return "小区还没通" + BroadbandName(next, false) + "（阶段 " + BroadbandStage[next] + "）"; }
            return null;
        }

        /// <summary>Upgrades the line one tier: installation and the first month's extra now, the extra again each month.</summary>
        public bool BuyBroadband(IXgHost host)
        {
            if (host == null) return false;
            var why = BroadbandBlocker(out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            double price = NextBroadbandPrice;
            if (!host.Spend(price)) { Say(T("经费不足 ¥") + F(price, "0")); return false; }
            S.totalSpent += price;
            S.broadband = BroadbandTier + 1;
            S.broadbandBilled = TodayKnown ? Today / 100 : 0;
            Say(T("电信师傅上门装好了" + BroadbandName(BroadbandTier, false) + "：下载 " + F(LineMBps, "0.#") + " MB/s，月租 ¥" + F(BroadbandMonthly[BroadbandTier], "0") + "（比 ADSL 多 ¥" + F(BroadbandExtraPerMonth, "0") + "，每月扣）。",
                  "The telecom technician installed " + BroadbandName(BroadbandTier, true) + ": " + F(LineMBps, "0.#") + " MB/s, ¥" + F(BroadbandMonthly[BroadbandTier], "0") + " a month (¥" + F(BroadbandExtraPerMonth, "0") + " more than ADSL, charged monthly)."));
            return true;
        }

        /// <summary>The faster line's extra fee each new calendar month; unpaid, the line goes back to ADSL.</summary>
        void TickBroadbandBill(IXgHost host)
        {
            if (S.broadband <= 0 || !TodayKnown || host == null) return;
            int month = Today / 100;
            if (S.broadbandBilled == 0) { S.broadbandBilled = month; return; }
            if (month <= S.broadbandBilled) return;
            int months = Math.Max(1, (month / 100 - S.broadbandBilled / 100) * 12 + month % 100 - S.broadbandBilled % 100);
            double fee = BroadbandExtraPerMonth * months;
            S.broadbandBilled = month;
            if (host.Spend(fee)) { S.totalSpent += fee; return; }
            S.broadband = 0;
            Say(T("光纤月租交不上，电信把你降回了 ADSL 4M。", "The fibre fee went unpaid; the telecom put you back on 4M ADSL."));
        }

        // ───────────── saves ─────────────

        void RepairDownloads()
        {
            if (S.downloadQueue == null) S.downloadQueue = new List<XgDownload>();
            if (S.batchesBought == null) S.batchesBought = new List<XgLabelCount>();
            if (S.batchesArrived == null) S.batchesArrived = new List<XgLabelCount>();
            if (S.owned == null) S.owned = new List<string>();
            if (S.unlocked == null) S.unlocked = new List<string>();
            if (S.downloadsVersion < DownloadsSchema)
            {
                // A save from before the queue: every pack it bought counts as fully downloaded (no batch entries = the
                // whole pack), the old countdowns are dropped, and it gets the room its data takes.
                if (S.downloads != null) S.downloads.Clear();
                S.downloadQueue.Clear(); S.batchesBought.Clear(); S.batchesArrived.Clear();
                double over = DiskUsedMB - DiskCapacityMB;
                if (over > 0) S.diskBonusMB = over;
                // The milestone payments it collected were the crowd tiers it reached.
                S.crowdTier = Math.Max(S.crowdTier, S.fundedAbility >= 2 ? S.fundedAbility : 0);
                S.downloadsVersion = DownloadsSchema;
            }
            S.downloadQueue.RemoveAll(x => x == null || XgCatalog.Dataset(x.dataset) == null || DataOfferDef(x.offer) == null
                || !Finite(x.sizeMB) || !Finite(x.doneMB) || !Finite(x.samples) || x.sizeMB < 0 || x.samples < 0);
            foreach (var x in S.downloadQueue) { x.doneMB = Math.Max(0, Math.Min(x.sizeMB, x.doneMB)); x.key = x.key ?? ""; }
            foreach (var list in new[] { S.batchesBought, S.batchesArrived })
                list.RemoveAll(n => n == null || XgCatalog.Dataset(n.dataset) == null || !Finite(n.count) || n.count < 0);
            S.broadband = BroadbandTier;
            S.drives = DrivesOwned;
            if (!Finite(S.diskBonusMB) || S.diskBonusMB < 0) S.diskBonusMB = 0;
            S.crowdTier = Math.Max(0, Math.Min(AbilityCount, S.crowdTier));
        }
    }
}
