using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Where a batch of data comes from (design v1.1 §11.8.3).</summary>
    public enum XgDataSource { Hand = 0, Public = 1, Junk = 2, Crowd = 3, Story = 4, Crawl = 5 }

    /// <summary>
    /// One way to get data for a dataset (design v1.1 §11.8.3, §11.8.7): the public pack, a cheap junk pack from 淘货,
    /// a crowd task posted on 摆渡众包, or story data that 老周 helps with. The static fields describe the offer;
    /// the live fields (owned, downloading, …) are filled in by <see cref="XgSim.Offer"/> and
    /// <see cref="XgSim.ShopDataOffers"/>, which always return fresh copies.
    /// </summary>
    public sealed class XgDataOffer
    {
        /// <summary>Offer id: "mnist.pack" (public, equal to the skill-tree node), "mnist.junk", "imagenet.story", "mnist.crowd".</summary>
        public string id = "";
        public string datasetId = "";
        public string name = "", nameEn = "";
        public string description = "", descriptionEn = "";
        public XgDataSource source;
        /// <summary>¥ for the whole pack. Crowd tasks: ¥ per label. The crawler: ¥ per run.</summary>
        public double price;
        /// <summary>Samples in the pack. Crowd tasks: labels per second while the task is posted. The crawler: most words per run.</summary>
        public double samples;
        /// <summary>Share of wrong labels (0–1) before 数据清洗.</summary>
        public double noise;
        /// <summary>
        /// Game seconds one download of it takes (a public pack: one batch) on the starting 20M ADSL; 0 = arrives at once. Live copies
        /// from <see cref="XgSim.Offer"/> give the time on the current line. Story copies: 老周's campus link.
        /// </summary>
        public double downloadSec;
        /// <summary>Real megabytes of the whole pack (junk: the whole dump; crowd tasks and the crawler: 0).</summary>
        public double sizeMB;
        /// <summary>Month the offer reaches the shelves (yyyymm, design v1.1 §14).</summary>
        public int month;
        /// <summary>Stage at which the offer can appear.</summary>
        public int stage = 1;
        /// <summary>The skill-tree node the offer stands for (public packs) or hands over (story data); null otherwise.</summary>
        public string node;

        // ───── live state ─────
        /// <summary>Bought (crowd: the task is posted). Still true while the pack downloads.</summary>
        public bool owned;
        public bool downloading;
        /// <summary>Seconds of download left, and 0–1 done.</summary>
        public double downloadLeft, downloadProgress;
        /// <summary>Can be bought right now (released, stage reached, not owned, prerequisites met). Money is not checked.</summary>
        public bool available;
        /// <summary>Released this calendar month.</summary>
        public bool isNew;
        /// <summary>Noise after 数据清洗.</summary>
        public double effectiveNoise;
        /// <summary>The pack feeds training (packs the player switched off stay owned but are left out).</summary>
        public bool included = true;
        /// <summary>Why it cannot be bought now (empty when it can).</summary>
        public string lockedReason = "", lockedReasonEn = "";
        /// <summary>Public packs: batches in all, bought and on the disk, and the next batch's price (infinity when all are bought).</summary>
        public int batches = 1, batchesBought, batchesArrived;
        public double nextBatchPrice = double.PositiveInfinity;

        public XgDataOffer Copy() => (XgDataOffer)MemberwiseClone();
        public string Name(bool english) => english ? nameEn : name;
        public string Description(bool english) => english ? descriptionEn : description;
        public string SourceLabel(bool english) => SourceName(source, english);

        public static string SourceName(XgDataSource source, bool english)
        {
            switch (source)
            {
                case XgDataSource.Hand: return english ? "Hand-labelled" : "亲手标";
                case XgDataSource.Public: return english ? "Public pack" : "公开包";
                case XgDataSource.Junk: return english ? "Junk pack" : "淘货杂包";
                case XgDataSource.Crowd: return english ? "Crowd labelling" : "众包标注";
                case XgDataSource.Crawl: return english ? "Web crawler" : "网页爬虫";
                default: return english ? "Story data" : "剧情数据";
            }
        }
    }

    public sealed partial class XgState
    {
        /// <summary>Non-public data offers bought (junk packs, story data). Public packs stay in <see cref="owned"/>.</summary>
        public List<string> dataOffers = new List<string>();
        /// <summary>Samples that are neither hand labels nor packs: crowd rows and logs the model labelled right.</summary>
        public List<XgLabelCount> dataExtra = new List<XgLabelCount>();
        /// <summary>Wrong crowd rows (inside <see cref="dataExtra"/>), before 数据清洗.</summary>
        public List<XgLabelCount> crowdNoise = new List<XgLabelCount>();
        /// <summary>Outside-data noise rows already discarded by the 抽检 clean-up.</summary>
        public List<XgLabelCount> dataCleaned = new List<XgLabelCount>();
        /// <summary>Datasets with a crowd task posted on 摆渡众包.</summary>
        public List<string> crowdOn = new List<string>();
        /// <summary>Story offers 老周 has already mentioned.</summary>
        public List<string> storyOffered = new List<string>();
        /// <summary>
        /// Datasets the player asked for in a forum thread of his own (摆渡贴吧 「求数据」). Story data is only offered as a
        /// reply to such a post: nobody hands the player data he never asked for.
        /// </summary>
        public List<string> dataAsked = new List<string>();
        /// <summary>Game seconds left until the reply to each data request arrives.</summary>
        public List<XgLabelCount> dataAskWait = new List<XgLabelCount>();
        /// <summary>Owned packs switched off for training: "logic.pack" (public or story data), "logic.junk".</summary>
        public List<string> dataExcluded = new List<string>();
    }

    /// <summary>
    /// Data sources (design v1.1 §11.8.3): every dataset can be fed several ways, cheap-but-dirty, slow-but-clean or
    /// expensive-but-good. Pack noise goes straight into R5 (<see cref="NoiseRatio"/>); 数据清洗 halves the noise of
    /// junk packs and crowd rows. Packs come down the home broadband through the download queue (XgSim.Downloads.cs)
    /// and can be sped up with ¥.
    /// </summary>
    public sealed partial class XgSim
    {
        public const double JunkPriceFactor = .3, JunkSampleFactor = 2;
        /// <summary>数据清洗 multiplies the noise of junk packs and crowd rows.</summary>
        public const double CleanNoiseFactor = .5;
        public const string DataCleanId = "dataclean";
        /// <summary>Crowd tasks: labels per second and ¥ per label.</summary>
        public const double CrowdRate = 12, CrowdPricePerLabel = .04;

        public event Action<XgDataOffer> DataOfferBought;
        /// <summary>A story offer just became available (老周 line: zh, en).</summary>
        public event Action<XgDataOffer, string, string> StoryDataOffered;

        /// <summary>Noise of each public pack (0–2%).</summary>
        static readonly Dictionary<string, double> PublicNoise = new Dictionary<string, double>
        {
            { "mnist", .002 }, { "cifar", .01 }, { "meme", .02 }, { "go", .005 }, { "imagenet", .015 }, { "poems", .01 },
            { "logic", .005 }, { "arith", .002 }, { "sense", .002 }, { "news", .02 }, { "translate", .02 }, { "danmu", .02 }, { "spam", .01 }, { "headline", .02 },
            { "review", .02 }, { "longtext", 0 }, { "crosssentence", 0 },
        };

        /// <summary>淘货 junk packs: dataset, noise, month, zh name, en name, zh description, en description.</summary>
        static readonly object[][] JunkPacks =
        {
            new object[] { "spam", .3, 201606, "垃圾短信样本包（某手机卫士用户举报汇总）", "Spam SMS dump (phone-guard user reports)",
                "用户举报的短信，顺手把催缴话费的也当成了诈骗。", "Messages users reported; phone-bill reminders got reported as scams too." },
            new object[] { "mnist", .3, 201607, "手写数字扫描件 12 万张（补习班作业本）", "120k scanned digits (cram-school workbooks)",
                "补习班的作业本拍照切图，答案是按老师批改抄的，红笔叉也算数。", "Workbook photos cut into digits; labels copied from the teacher's marking, red crosses included." },
            new object[] { "danmu", .35, 201607, "弹幕全量爬取包，按点赞数打的标", "Full danmaku crawl, labelled by likes",
                "点赞多的算夸，点赞少的算骂。反话全标反了。", "Many likes means praise, few means mockery. Every bit of sarcasm is labelled backwards." },
            new object[] { "headline", .25, 201607, "震惊部标题合集 2.4 万条", "24k \"SHOCKING\" headlines",
                "「震惊！」开头的全标成标题党，正经新闻也混进来不少。", "Everything starting with \"SHOCKING!\" counts as clickbait, plenty of real news included." },
            new object[] { "logic", .2, 201607, "公考题库盗版合集（答案页错印）", "Pirated civil-exam question bank (misprinted answers)",
                "二十万道判断推理，答案页印错了好几页。", "200k reasoning questions; several answer pages are misprinted." },
            new object[] { "meme", .4, 201608, "十万表情包打包", "100k memes in one zip",
                "号称十万张，解压去重剩一万二。情绪是按文件名猜的。", "Claims 100k; after de-duplication 12k are left. Moods guessed from the file names." },
            new object[] { "cifar", .25, 201608, "12306 验证码截图大全", "Every 12306 captcha, screenshotted",
                "抢票软件攒下的验证码，标注是当时点的那一下，点错的也收了。", "Captchas saved by ticket-grabbing apps, labelled with whatever was clicked, misses included." },
            new object[] { "poems", .3, 201608, "诗词大全 txt（夹带网友原创）", "Complete poems .txt (with netizen originals)",
                "全唐诗里混着网友改写的「床前明月光，地上鞋两双」。", "The Tang poems mixed with netizen rewrites of the classics." },
            new object[] { "news", .35, 201608, "百万条微博语料，爬虫新鲜货", "A million Weibo posts, fresh from the crawler",
                "说是百万条，其实一千万条，一半是转发和「转发微博」。", "Says a million; really ten million, half of them reposts saying \"repost\"." },
            new object[] { "review", .4, 201609, "淘货评论爬虫包，五星都当真", "Taohuo review crawl, every five-star taken at face value",
                "五星好评一律算真买家，刷的单全混在里面。", "Every five-star review counted as genuine, faked orders and all." },
            new object[] { "go", .25, 201609, "野狐对局 sgf 打包，死活程序自动判", "Online Go games in sgf, life and death judged by a script",
                "几万盘网络对局，死活是一个老程序自动判的，劫争全判错。", "Tens of thousands of online games; an old script judged life and death and gets every ko wrong." },
            new object[] { "imagenet", .3, 201609, "摆渡图片关键词爬虫包（240 万张）", "Image-search keyword crawl (2.4M pictures)",
                "按搜索词下的图，「苹果」里一半是手机。", "Pictures by search keyword: half of \"apple\" is phones." },
            new object[] { "translate", .35, 201610, "美剧字幕中英对照包（时间轴没对齐）", "US-drama subtitles, zh–en (timings off)",
                "字幕组的双语字幕拆成句对，时间轴错位，一半对不上。", "Fansub bilingual subtitles split into pairs; the timings drift and half do not match." },
        };

        /// <summary>Story data: dataset, price, noise, seconds until 老周 brings his copy (links come down the home line instead), month, zh name, en name, zh note, en note, 老周 zh, 老周 en.</summary>
        static readonly object[][] StoryPacks =
        {
            new object[] { "imagenet", 600.0, .005, 150.0, 201609, "ImageNet 学术下载（贴吧求来的）", "ImageNet academic download (asked for on the forum)",
                "你在贴吧发帖求 ImageNet，周而复始回了：只给学校邮箱发链接，他可以用 edu 邮箱帮你申请，你买块移动硬盘。", "You asked for ImageNet on the forum and 周而复始 replied: links only go to university addresses; he can apply with his edu mail if you buy a portable drive.",
                "楼主要 ImageNet？那个只给学校邮箱发链接。我有 edu 邮箱，可以帮你申请，你自己买块硬盘。别外传啊。", "Want ImageNet, OP? Links only go to school addresses. I have an edu mail and can apply for you; buy your own drive. Keep it to yourself." },
            new object[] { "translate", 1500.0, .01, 120.0, 201610, "联合国平行语料 v1.0（贴吧求来的）", "UN Parallel Corpus v1.0 (asked for on the forum)",
                "你在贴吧发帖求中英语料，周而复始回了：联合国今年 5 月刚公开，句子工整。钱是硬盘和整理的钱。", "You asked for a Chinese–English corpus on the forum and 周而复始 replied: the UN made one public this May, neat sentence pairs. You pay for the drive and the clean-up.",
                "楼主要中英对照？联合国今年 5 月公开了一份平行语料，比淘货上那些字幕包干净多了。要的话我帮你下。", "Want Chinese–English pairs, OP? The UN released a parallel corpus this May, much cleaner than the subtitle packs on 淘货. I can download it for you." },
        };

        /// <summary>
        /// Story data that is only a link: it comes down the home line batch by batch (onto the drive its price paid for).
        /// The others are copies 老周 already has and brings over.
        /// </summary>
        static readonly string[] StoryOverHomeLine = { "imagenet.story" };
        public static bool StoryComesOverHomeLine(string offerId) => Array.IndexOf(StoryOverHomeLine, offerId) >= 0;

        static List<XgDataOffer> offerCatalog;

        /// <summary>Every data offer of the game (catalog values, no live state).</summary>
        public static IReadOnlyList<XgDataOffer> DataOfferCatalog => offerCatalog ?? (offerCatalog = BuildOffers());

        static List<XgDataOffer> BuildOffers()
        {
            var list = new List<XgDataOffer>();
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.kind != XgNodeKind.Dataset) continue;
                var d = XgCatalog.Dataset(n.target);
                if (d == null) continue;
                PublicNoise.TryGetValue(d.id, out double noise);
                list.Add(new XgDataOffer
                {
                    id = n.id, datasetId = d.id, source = XgDataSource.Public, node = n.id, name = d.name, nameEn = d.nameEn,
                    description = n.note, descriptionEn = n.noteEn, price = Math.Ceiling(n.cost * StoryResearchPrice), samples = d.samples, noise = noise,
                    downloadSec = DownloadSecondsAt(BatchMB(d), BroadbandMBps[0]), sizeMB = d.sizeMB, batches = PackBatches(d), stage = n.stage, month = StageMonth(n.stage),
                });
            }
            foreach (var j in JunkPacks)
            {
                var d = XgCatalog.Dataset((string)j[0]);
                var n = XgCatalog.Node(d.id + ".pack");
                double samples = d.samples * JunkSampleFactor;
                list.Add(new XgDataOffer
                {
                    id = d.id + ".junk", datasetId = d.id, source = XgDataSource.Junk, name = (string)j[3], nameEn = (string)j[4],
                    description = (string)j[5], descriptionEn = (string)j[6], price = Math.Round((n != null ? n.cost * StoryResearchPrice : d.price) * JunkPriceFactor),
                    samples = samples, noise = (double)j[1], sizeMB = d.sizeMB * JunkSampleFactor, downloadSec = DownloadSecondsAt(d.sizeMB * JunkSampleFactor, BroadbandMBps[0] * JunkLineShare),
                    stage = n != null ? n.stage : 1, month = (int)j[2],
                });
            }
            foreach (var s in StoryPacks)
            {
                var d = XgCatalog.Dataset((string)s[0]);
                var n = XgCatalog.Node(d.id + ".pack");
                list.Add(new XgDataOffer
                {
                    id = d.id + ".story", datasetId = d.id, source = XgDataSource.Story, node = n.id, price = (double)s[1], noise = (double)s[2],
                    downloadSec = StoryComesOverHomeLine(d.id + ".story") ? DownloadSecondsAt(d.sizeMB, BroadbandMBps[0]) : (double)s[3], month = (int)s[4], name = (string)s[5], nameEn = (string)s[6], description = (string)s[7],
                    descriptionEn = (string)s[8], samples = d.samples, sizeMB = d.sizeMB, batches = PackBatches(d), stage = n.stage,
                });
            }
            foreach (var desk in XgCatalog.Desks)
            {
                var d = XgCatalog.Dataset(desk.id);
                list.Add(new XgDataOffer
                {
                    id = d.id + ".crowd", datasetId = d.id, source = XgDataSource.Crowd, name = "摆渡众包发包：" + desk.name, nameEn = "Crowd task on Bodu: " + desk.nameEn,
                    description = "在摆渡众包上当一回甲方：别人替你标，按条付钱，持续流入，少量标错。",
                    descriptionEn = "Be the client on Bodu Crowd for once: others label for you, paid per label, a steady trickle with a few mistakes.",
                    price = CrowdPricePerLabel, samples = CrowdRate, noise = d.track == XgTrack.Vision ? .05 : .08, stage = 2, month = 201607,
                });
            }
            list.Add(CrawlOfferDef());
            return list;
        }

        static string[] StoryLines(string id)
        {
            foreach (var s in StoryPacks) if ((string)s[0] + ".story" == id) return new[] { (string)s[9], (string)s[10] };
            return new[] { "", "" };
        }

        public static XgDataOffer DataOfferDef(string id)
        {
            foreach (var o in DataOfferCatalog) if (o.id == id) return o;
            return null;
        }

        /// <summary>Game month of each stage (design v1.1 §7): 1 = 2016-06 … 5 = 2016-10, 6 = 2016-12.</summary>
        public static int StageMonth(int stage)
        {
            switch (Math.Max(1, Math.Min(6, stage)))
            {
                case 1: return 201606; case 2: return 201607; case 3: return 201608;
                case 4: return 201609; case 5: return 201610; default: return 201612;
            }
        }

        /// <summary>The shelves' month: the calendar, or the stage's month if the story has run ahead of the clock.</summary>
        public int CurrentMonth => Math.Max(Today / 100, StageMonth(S.stage));

        /// <summary>Game seconds <paramref name="mb"/> real megabytes take at <paramref name="mbps"/> (0 when it arrives at once).</summary>
        public static double DownloadSecondsAt(double mb, double mbps)
        {
            double t = mb / Math.Max(1e-9, mbps) * DownloadTimeScale;
            return t <= InstantDownloadSeconds ? 0 : Math.Round(t, 1);
        }

        // ───────────── ownership ─────────────

        public bool OfferOwned(string id)
        {
            var o = DataOfferDef(id);
            if (o == null) return false;
            switch (o.source)
            {
                case XgDataSource.Public: return S.owned.Contains(o.datasetId) || Has(o.id);
                case XgDataSource.Crowd: return S.crowdOn.Contains(o.datasetId);
                // The crawler is paid per run and never owned; "owned" means a run is going on now.
                case XgDataSource.Crawl: return CrawlActive;
                default: return S.dataOffers.Contains(o.id);
            }
        }

        /// <summary>
        /// Download key in <see cref="XgState.downloadQueue"/>: public packs use the dataset id, and so does story data,
        /// which is the public pack by another road; junk packs use the offer id.
        /// </summary>
        static string DownloadKey(XgDataOffer o) => o.source == XgDataSource.Public || o.source == XgDataSource.Story ? o.datasetId : o.id;

        /// <summary>A junk pack or story pack of this dataset is bought and downloaded.</summary>
        public bool OwnsExtraPack(string dataset)
        {
            foreach (var id in S.dataOffers)
            {
                var o = DataOfferDef(id);
                if (o != null && o.datasetId == dataset && (o.source == XgDataSource.Junk || o.source == XgDataSource.Story) && DataOfferInstalled(o.id)) return true;
            }
            return false;
        }

        /// <summary>
        /// Samples from packs that are on the disk and switched on: the public pack's arrived batches (story data is the
        /// whole public pack by another road) and the junk packs.
        /// </summary>
        public double PackSamples(string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null) return 0;
            double n = PackIncluded(dataset + ".pack") ? BatchSamples(d) * InstalledBatches(dataset) : 0;
            foreach (var id in S.dataOffers)
            {
                var o = DataOfferDef(id);
                if (o == null || o.datasetId != dataset || o.source != XgDataSource.Junk || !DataOfferInstalled(o.id)) continue;
                if (PackIncluded(o.id)) n += o.samples;
            }
            return n;
        }

        /// <summary>Wrong rows that came with the switched-on packs, before cleaning.</summary>
        double PackNoiseRows(string dataset, bool dirtyOnly)
        {
            double rows = 0;
            var d = XgCatalog.Dataset(dataset);
            int arrived = InstalledBatches(dataset);
            if (d != null && arrived > 0 && PackIncluded(dataset + ".pack") && !dirtyOnly)
            {
                var o = DataOfferDef(dataset + (StoryOwned(dataset) ? ".story" : ".pack"));
                if (o != null) rows += BatchSamples(d) * arrived * o.noise;
            }
            foreach (var id in S.dataOffers)
            {
                var o = DataOfferDef(id);
                if (o == null || o.datasetId != dataset || o.source != XgDataSource.Junk || !DataOfferInstalled(o.id)) continue;
                if (PackIncluded(o.id)) rows += o.samples * o.noise;
            }
            return rows;
        }

        // ───────────── training sources ─────────────

        /// <summary>The switch a pack answers to: story data shares the public pack's ("imagenet.pack"), junk packs their own.</summary>
        public static string PackSwitchKey(XgDataOffer o) =>
            o == null ? "" : o.source == XgDataSource.Story ? o.datasetId + ".pack" : o.id;

        /// <summary>Only packs can be switched off; hand labels, crowd rows and user logs always train.</summary>
        public static bool PackSwitchable(XgDataOffer o) =>
            o != null && (o.source == XgDataSource.Public || o.source == XgDataSource.Story || o.source == XgDataSource.Junk);

        /// <summary>The pack (by switch key) feeds training; packs are on unless the player switched them off.</summary>
        public bool PackIncluded(string key) => !S.dataExcluded.Contains(key);

        /// <summary>Why this pack cannot be switched now (null when it can).</summary>
        public string PackSwitchBlocker(string offerId, out string en)
        {
            en = null;
            var o = DataOfferDef(offerId);
            if (!PackSwitchable(o)) { en = "Only packs can be switched"; return "只有数据包能开关"; }
            if (!OfferOwned(offerId)) { en = "Not owned"; return "还没买"; }
            if (Downloading(DownloadKey(o))) { en = "Still downloading"; return "还在下载"; }
            if (!DataOfferInstalled(o.id)) { en = "Install it from your Files folder first"; return "先从资料文件夹安装"; }
            var d = XgCatalog.Dataset(o.datasetId);
            if (d != null && Run(d.track).epochActive && Run(d.track).dataset == o.datasetId) { en = "Finish the active epoch first"; return "先完成当前训练轮次"; }
            return null;
        }

        /// <summary>
        /// Switches an owned pack in or out of training. It stays owned (desks, ownership, the shop do not change); only
        /// <see cref="Samples"/> and the outside noise leave it out, so the curve is re-evaluated at once.
        /// </summary>
        public bool SetPackIncluded(string offerId, bool on)
        {
            var o = DataOfferDef(offerId);
            var why = PackSwitchBlocker(offerId, out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            string key = PackSwitchKey(o);
            if (PackIncluded(key) == on) return false;
            if (on) S.dataExcluded.Remove(key); else S.dataExcluded.Add(key);
            if (!on && o.source == XgDataSource.Junk) Earn("data.junkoff");
            var d = XgCatalog.Dataset(o.datasetId);
            string name = d != null ? T(d.name, d.nameEn) : o.datasetId;
            Say(on ? T("「" + name + "」训练重新用上：", name + " trains on it again: ") + T(o.name, o.nameEn)
                   : T("「" + name + "」训练不再用：", name + " no longer trains on: ") + T(o.name, o.nameEn)
                     + (Samples(o.datasetId) < XgCatalog.SamplesToTrain ? T("（剩下的样本不够训练了，先去标注台标）") : ""));
            foreach (var run in Runs) if (run.dataset == o.datasetId) Evaluate(run);
            return true;
        }

        /// <summary>Distinct training-pool cards are never more than this.</summary>
        public const int PoolCeiling = 65536;

        /// <summary>
        /// Distinct cards in the board's training pool for this many samples (design v1.1 §11.8.7 实现注意). Up to
        /// <see cref="XgBoardData.PoolLimit"/> every sample is its own card; past that the pool grows with the log of
        /// the samples (coverage of element combinations), so a 60k pack is about 3.7× the pool of 4,096 hand labels,
        /// 1.2M about 6.7×. Cards are generated from seeds, so a large pool costs nothing to keep.
        /// </summary>
        public static int BoardPoolSize(double samples)
        {
            if (!(samples > 1)) return 1;
            double limit = XgBoardData.PoolLimit;
            if (samples <= limit) return (int)samples;
            return (int)Math.Min(PoolCeiling, Math.Round(limit * (1 + Math.Log(samples / limit))));
        }

        /// <summary>Training cards per extra pool card once the pool opens past <see cref="XgBoardData.PoolLimit"/>.</summary>
        public const int PoolGrowth = 8;

        /// <summary>
        /// The pool the board draws from after this many cards of the dataset were trained. A big pack's extra cards
        /// open up gradually, one per <see cref="PoolGrowth"/> cards trained: the board first learns the familiar
        /// cards (concepts need repeats to form), then meets more and more variety. Small datasets are unaffected.
        /// </summary>
        public static int BoardPoolAt(double samples, double trained)
        {
            int full = BoardPoolSize(samples);
            if (full <= XgBoardData.PoolLimit) return full;
            return (int)Math.Min(full, XgBoardData.PoolLimit + Math.Max(0, trained) / PoolGrowth);
        }

        /// <summary>Board cards trained on this dataset so far (read-only lookup).</summary>
        double BoardCardsOn(string dataset)
        {
            if (S.boardCards != null) foreach (var c in S.boardCards) if (c.key == dataset) return c.value;
            return 0;
        }

        /// <summary>Samples from crowd rows and logs the model labelled right.</summary>
        public double ExtraSamples(string dataset) => Count(S.dataExtra, dataset);

        /// <summary>
        /// Wrong rows that came from outside (packs, crowd tasks), after 数据清洗 and the 抽检 clean-up. They sit inside
        /// <see cref="Samples"/>, unlike <see cref="Noise"/>, which counts the lab's own uncaught automatic mistakes.
        /// </summary>
        public double DataNoise(string dataset)
        {
            double clean = Has(DataCleanId) ? CleanNoiseFactor : 1;
            double dirty = (PackNoiseRows(dataset, true) + Count(S.crowdNoise, dataset)) * clean;
            double rows = PackNoiseRows(dataset, false) - PackNoiseRows(dataset, true) + dirty - Count(S.dataCleaned, dataset);
            return Math.Max(0, rows);
        }

        /// <summary>R5 噪: share of all rows that carry a wrong label (own mistakes plus outside noise), at most one half.</summary>
        public double NoiseRatio(string dataset)
        {
            double own = Noise(dataset), outside = DataNoise(dataset);
            double total = own + outside;
            if (!FiniteCollaboration(total) || total <= 0) return 0;
            return Math.Min(.5, total / Math.Max(1, Samples(dataset) + own));
        }

        /// <summary>Label noise up to about this share is mostly averaged away by the rest of the data.</summary>
        public const double NoiseTolerance = .4;

        /// <summary>
        /// Share of pool cards R5 flips. A little label noise is drowned out by the many right rows around it, so the
        /// flip rate grows with the cube of the noise share up to <see cref="NoiseTolerance"/> (2% → 0.005%,
        /// 15% → 2%, 30% → 17%) and equals the share beyond it. A clean public pack costs nothing; a dirty junk
        /// pack costs a lot; 数据清洗 brings it back to a small loss.
        /// </summary>
        public double BoardFlipRate(string dataset)
        {
            double r = NoiseRatio(dataset);
            return r >= NoiseTolerance ? r : r * r * r / (NoiseTolerance * NoiseTolerance);
        }

        /// <summary>Removes outside noise rows (the 抽检 clean-up reaches them after the lab's own mistakes).</summary>
        void CleanDataNoise(string dataset, double rows)
        {
            if (rows <= 0) return;
            SetCount(S.dataCleaned, dataset, Count(S.dataCleaned, dataset) + Math.Min(rows, DataNoise(dataset)));
        }

        // ───────────── offers ─────────────

        /// <summary>The offer with its live state, or null.</summary>
        public XgDataOffer Offer(string id)
        {
            var def = DataOfferDef(id);
            if (def == null) return null;
            var o = def.Copy();
            o.owned = OfferOwned(id);
            string key = DownloadKey(def);
            var queued = def.source == XgDataSource.Crowd || def.source == XgDataSource.Crawl ? null : FirstQueued(key);
            o.downloadLeft = queued != null ? DownloadLeft(key) : 0;
            o.downloading = o.owned && queued != null;
            o.downloadProgress = queued != null ? (queued.sizeMB > 0 ? Clamp01(queued.doneMB / queued.sizeMB) : 1) : o.owned ? 1 : 0;
            // Estimate a new purchase using the same simulated duration as Enqueue/ItemRate.
            // Public purchases now queue the full teaching pack; home-line story packs queue every batch.
            // Keep catalog timing untouched: paceVersion 0 campus saves still use it for their actual rate.
            if (def.source == XgDataSource.Public || def.source == XgDataSource.Junk)
                o.downloadSec = PacedDownloadSeconds(def.sizeMB, LineMBps, def.source == XgDataSource.Junk, false);
            else if (def.source == XgDataSource.Story)
            {
                var data = XgCatalog.Dataset(def.datasetId);
                o.downloadSec = StoryComesOverHomeLine(def.id)
                    ? PackBatches(data) * PacedDownloadSeconds(BatchMB(data), LineMBps, false, false)
                    : PacedDownloadSeconds(def.sizeMB, LineMBps, false, true);
            }
            if (def.source == XgDataSource.Public || def.source == XgDataSource.Story)
            {
                o.batchesBought = BatchesBought(def.datasetId); o.batchesArrived = BatchesArrived(def.datasetId);
                o.nextBatchPrice = def.source == XgDataSource.Public && BatchBlocker(def.datasetId, out _) == null ? NextBatchPrice(def.datasetId) : double.PositiveInfinity;
            }
            o.effectiveNoise = def.noise * (Has(DataCleanId) && (def.source == XgDataSource.Junk || def.source == XgDataSource.Crowd || def.source == XgDataSource.Crawl) ? CleanNoiseFactor : 1);
            o.included = !PackSwitchable(def) || PackIncluded(PackSwitchKey(def));
            o.isNew = def.month == CurrentMonth;
            string why = OfferBlocker(def, out string whyEn);
            o.available = why == null;
            o.lockedReason = why ?? ""; o.lockedReasonEn = whyEn ?? "";
            return o;
        }

        /// <summary>Why the offer cannot be bought now (null when it can; money is not checked).</summary>
        string OfferBlocker(XgDataOffer o, out string en)
        {
            en = null;
            if (o.source == XgDataSource.Crawl && CrawlActive) { en = "Already crawling"; return "正在爬"; }
            if (OfferOwned(o.id)) { en = o.source == XgDataSource.Crowd ? "Posted" : "Owned"; return o.source == XgDataSource.Crowd ? "已发包" : "已拥有"; }
            if (o.month > CurrentMonth || o.stage > S.stage) { en = "Not on sale yet"; return "还没上架"; }
            if (o.source == XgDataSource.Public || o.source == XgDataSource.Story)
            {
                var n = XgCatalog.Node(o.node);
                if (n == null || Has(n.id) || S.owned.Contains(o.datasetId)) { en = "Already have this data"; return "已有这份数据"; }
                if (!NodeVisible(n) || ProgressionBlocker(n) != null) { en = "Not at this stage yet"; return "还没到这一阶段"; }
                if (n.parent != null && !Has(n.parent)) { var p = XgCatalog.Node(n.parent); en = "Unlock first: " + (p != null ? p.nameEn : n.parent); return "先解锁 " + (p != null ? p.name : n.parent); }
                foreach (var need in n.needs) if (!Has(need)) { var p = XgCatalog.Node(need); en = "Unlock first: " + (p != null ? p.nameEn : need); return "先解锁 " + (p != null ? p.name : need); }
                if (o.source == XgDataSource.Story && !DataAnswered(o.datasetId)) { en = "Ask for it on the forum first"; return DataAsked(o.datasetId) ? "等贴吧回帖" : "先去贴吧发帖求数据"; }
                if (o.source == XgDataSource.Story && AnyEpochActive) { en = "Finish the active epoch first"; return "先完成当前训练轮次"; }
            }
            if (o.source == XgDataSource.Junk)
            {
                // A junk pack is sold once the dataset's own stage is reached: the model has to be able to use it.
                var n = XgCatalog.Node(o.datasetId + ".pack");
                if (n != null && !NodeVisible(n)) { en = "Not at this stage yet"; return "还没到这一阶段"; }
            }
            if (o.source == XgDataSource.Crowd)
            {
                if (!DeskOpen(o.datasetId)) { en = "The desk is not open yet"; return "这张标注桌还没开"; }
            }
            return null;
        }

        /// <summary>Offers sold in 淘货: public packs and junk packs on the shelves this month (owned ones included, marked).</summary>
        public IEnumerable<XgDataOffer> ShopDataOffers()
        {
            foreach (var def in DataOfferCatalog)
            {
                if (def.source != XgDataSource.Public && def.source != XgDataSource.Junk) continue;
                if (def.month > CurrentMonth || def.stage > S.stage) continue;
                yield return Offer(def.id);
            }
        }

        /// <summary>Every source of one dataset with live state, for the 灵光 pages (public, junk, story, crowd).</summary>
        public List<XgDataOffer> OffersFor(string dataset)
        {
            var list = new List<XgDataOffer>();
            foreach (var def in DataOfferCatalog) if (def.datasetId == dataset) list.Add(Offer(def.id));
            return list;
        }

        /// <summary>
        /// Buys a pack (public, junk or story). Public packs go through the skill-tree node, so 淘货 and the tree stay
        /// one purchase. Story data hands over the public node at its own price. Crowd tasks use <see cref="SetCrowd"/>.
        /// </summary>
        public bool BuyDataOffer(string offerId, IXgHost host)
        {
            var def = DataOfferDef(offerId);
            if (def == null || host == null) return false;
            if (def.source == XgDataSource.Crowd) return SetCrowd(def.datasetId, true);
            if (def.source == XgDataSource.Crawl) return StartCrawl(host);
            var why = OfferBlocker(def, out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            if (def.source == XgDataSource.Public)
            {
                if (!BuyNode(def.node, host)) return false;
                DataOfferBought?.Invoke(Offer(def.id));
                return true;
            }
            if (!host.Spend(def.price)) { Say(T("经费不足 ¥") + F(def.price, "0")); return false; }
            S.totalSpent += def.price;
            S.dataOffers.Add(def.id);
            if (def.source == XgDataSource.Story)
            {
                // The data is the whole public pack: the node and the dataset count as owned, every batch is bought. It
                // lands on the drive its price paid for (no room taken on the lab's disk). A link comes down the home
                // line batch by batch; a copy 老周 already has is brought over whole.
                Grant(def.node);
                if (!S.owned.Contains(def.datasetId)) S.owned.Add(def.datasetId);
                S.downloadQueue.RemoveAll(x => x.key == def.datasetId);
                var d = XgCatalog.Dataset(def.datasetId);
                int all = PackBatches(d);
                SetCount(S.batchesBought, def.datasetId, all); SetCount(S.batchesArrived, def.datasetId, 0);
                if (StoryComesOverHomeLine(def.id))
                {
                    for (int b = 1; b <= all; b++)
                        Enqueue(new XgDownload { key = def.datasetId, dataset = def.datasetId, offer = def.id, batch = b, batches = 1, sizeMB = BatchMB(d), samples = BatchSamples(d), ownDrive = true }, false);
                    Say(T("收到贴吧回复中的下载链接：《" + def.name + "》，" + SizeText(def.sizeMB) + " 走你家宽带，" + BroadbandName(BroadbandTier, false) + " 约 " + ClockText(LineQueueSeconds) + "，分 " + all + " 批到。钱买的那块硬盘专门放它。",
                          "The forum reply linked: " + def.nameEn + ", " + SizeText(def.sizeMB) + " over your own broadband, about " + ClockText(LineQueueSeconds) + " on " + BroadbandName(BroadbandTier, true) + ", in " + all + " batches. The drive the money bought holds it."));
                }
                else
                {
                    Enqueue(new XgDownload { key = def.datasetId, dataset = def.datasetId, offer = def.id, batches = all, sizeMB = def.sizeMB, samples = def.samples, campus = true, ownDrive = true }, false);
                    double deliverySeconds = Offer(def.id).downloadSec;
                    Say(T("贴吧回帖 · 周而复始：《" + def.name + "》我下好了，拷在硬盘里给你寄过来，约 " + ClockText(deliverySeconds) + " 到。", "Forum reply · 周而复始: " + def.nameEn + " is already downloaded; he is mailing it on a drive, about " + ClockText(deliverySeconds) + "."));
                }
                CheckDesks();
            }
            else
            {
                var x = Enqueue(new XgDownload { key = def.id, dataset = def.datasetId, offer = def.id, sizeMB = def.sizeMB, samples = def.samples }, true);
                if (x != null)
                    Say(T("迅雷：《" + def.name + "》只有 3 个资源" + QueueNote(x, false) + "。", "Thunder: " + def.nameEn + " has 3 sources" + QueueNote(x, true) + "."));
                else Say(T("淘货到手：") + T(def.name, def.nameEn));
                CheckDesks();
            }
            foreach (var run in Runs) if (run.dataset == def.datasetId) Evaluate(run);
            DataOfferBought?.Invoke(Offer(def.id));
            return true;
        }

        /// <summary>摆渡云 super-member speed-up for the offer's next queued item: ¥ for the seconds it saves (XgSim.Downloads.cs).</summary>
        public double AccelerateOfferCost(string offerId)
        {
            var def = DataOfferDef(offerId);
            return def == null ? 0 : AccelerateCost(DownloadKey(def));
        }

        public bool AccelerateOffer(string offerId, IXgHost host)
        {
            var def = DataOfferDef(offerId);
            if (def == null || host == null || def.source == XgDataSource.Crowd || def.source == XgDataSource.Crawl) return false;
            return Accelerate(DownloadKey(def), host);
        }

        /// <summary>Display name of a download key (dataset id or offer id).</summary>
        public string DownloadName(string key)
        {
            var d = XgCatalog.Dataset(key);
            if (d != null) return T(d.name, d.nameEn);
            var o = DataOfferDef(key);
            return o != null ? T(o.name, o.nameEn) : key;
        }

        // ───────────── crowd tasks ─────────────

        public bool CrowdPosted(string dataset) => S.crowdOn.Contains(dataset);

        /// <summary>Posts or withdraws a crowd task for a desk's dataset on 摆渡众包.</summary>
        public bool SetCrowd(string dataset, bool on)
        {
            var def = DataOfferDef(dataset + ".crowd");
            if (def == null) return false;
            if (!on) { bool was = S.crowdOn.Remove(dataset); if (was) Say(T("撤下了众包任务：") + T(XgCatalog.Dataset(dataset).name, XgCatalog.Dataset(dataset).nameEn)); return was; }
            var why = OfferBlocker(def, out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            S.crowdOn.Add(dataset);
            Say(T("在摆渡众包发了包：每条 ¥" + F(CrowdPricePerLabel, "0.00") + "，约 " + F(CrowdRate, "0") + " 条/秒。", "Posted a crowd task on Bodu: ¥" + F(CrowdPricePerLabel, "0.00") + " a label, about " + F(CrowdRate, "0") + " labels/s."));
            return true;
        }

        /// <summary>¥ per second the posted crowd tasks cost.</summary>
        public double CrowdSpendPerSecond { get { double n = 0; foreach (var id in S.crowdOn) { var o = DataOfferDef(id + ".crowd"); if (o != null) n += o.samples * o.price; } return n; } }

        void TickCrowd(double dt, IXgHost host)
        {
            if (S.crowdOn.Count == 0 || host == null) return;
            for (int i = S.crowdOn.Count - 1; i >= 0; i--)
            {
                var o = DataOfferDef(S.crowdOn[i] + ".crowd");
                if (o == null) { S.crowdOn.RemoveAt(i); continue; }
                double rows = o.samples * dt, cost = rows * o.price;
                if (!host.Spend(cost))
                {
                    Say(T("经费见底，众包任务自动下架：") + T(XgCatalog.Dataset(o.datasetId).name, XgCatalog.Dataset(o.datasetId).nameEn));
                    S.crowdOn.RemoveAt(i);
                    continue;
                }
                S.totalSpent += cost;
                SetCount(S.dataExtra, o.datasetId, ExtraSamples(o.datasetId) + rows);
                SetCount(S.crowdNoise, o.datasetId, Count(S.crowdNoise, o.datasetId) + rows * o.noise);
            }
        }

        // ───────────── tick, repair ─────────────

        void TickDataSources(double dt, IXgHost host)
        {
            TickCrowd(dt, host);
            TickBroadbandBill(host);
            // A data request on the forum gets its reply after a while.
            foreach (var w in S.dataAskWait) if (w.count > 0) w.count = Math.Max(0, w.count - dt);
            // Story data: 周而复始 offers it once, as the reply to the player's own forum request.
            foreach (var s in StoryPacks)
            {
                string id = (string)s[0] + ".story";
                if (S.storyOffered.Contains(id)) continue;
                var o = Offer(id);
                if (o == null || o.owned) continue;
                if (!o.available) { if (o.lockedReasonEn == "Already have this data") S.storyOffered.Add(id); continue; }
                S.storyOffered.Add(id);
                var line = StoryLines(id);
                Say(T("贴吧回帖 · 周而复始：", "Forum reply · 周而复始: ") + T(line[0], line[1]));
                StoryDataOffered?.Invoke(o, line[0], line[1]);
            }
        }

        // ───────────── asking for data on the forum ─────────────

        /// <summary>Game seconds between the player's 「求数据」 post and the reply.</summary>
        public const double DataReplySeconds = 60;

        public bool DataAsked(string dataset) => S.dataAsked != null && S.dataAsked.Contains(dataset);
        public bool DataAnswered(string dataset) => DataAsked(dataset) && Count(S.dataAskWait, dataset) <= 0;

        /// <summary>Datasets the player can post a 「求数据」 thread about now: hard-to-get data on sale this month, not owned, not asked yet.</summary>
        public List<string> AskableData()
        {
            var list = new List<string>();
            foreach (var s in StoryPacks)
            {
                string ds = (string)s[0];
                var def = DataOfferDef(ds + ".story");
                if (def == null || DataAsked(ds)) continue;
                if (def.month > CurrentMonth || def.stage > S.stage) continue;
                if (Has(def.node) || S.owned.Contains(ds)) continue;
                list.Add(ds);
            }
            return list;
        }

        /// <summary>The player posts a 「求数据」 thread on the forum. The reply (and the offer) comes after <see cref="DataReplySeconds"/>.</summary>
        public bool AskForData(string dataset)
        {
            if (!AskableData().Contains(dataset)) return false;
            S.dataAsked.Add(dataset);
            SetCount(S.dataAskWait, dataset, DataReplySeconds);
            return true;
        }

        /// <summary>Forum flags of the data requests: "asked.imagenet" once posted, "answered.imagenet" once replied.</summary>
        public bool DataForumFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            if (flag.StartsWith("asked.", StringComparison.Ordinal)) return DataAsked(flag.Substring(6));
            if (flag.StartsWith("answered.", StringComparison.Ordinal)) return DataAnswered(flag.Substring(9));
            return false;
        }

        void RepairDataSources()
        {
            if (S.dataOffers == null) S.dataOffers = new List<string>();
            if (S.dataExtra == null) S.dataExtra = new List<XgLabelCount>();
            if (S.crowdNoise == null) S.crowdNoise = new List<XgLabelCount>();
            if (S.dataCleaned == null) S.dataCleaned = new List<XgLabelCount>();
            if (S.crowdOn == null) S.crowdOn = new List<string>();
            if (S.storyOffered == null) S.storyOffered = new List<string>();
            if (S.dataAsked == null) S.dataAsked = new List<string>();
            if (S.dataAskWait == null) S.dataAskWait = new List<XgLabelCount>();
            // Old saves where a story pack was already bought or offered keep it: those count as asked and answered.
            foreach (var s in StoryPacks)
            {
                string ds = (string)s[0];
                if ((S.dataOffers.Contains(ds + ".story") || S.storyOffered.Contains(ds + ".story")) && !S.dataAsked.Contains(ds)) S.dataAsked.Add(ds);
            }
            if (S.dataExcluded == null) S.dataExcluded = new List<string>();
            S.dataOffers.RemoveAll(id => DataOfferDef(id) == null);
            S.dataExcluded.RemoveAll(id => !PackSwitchable(DataOfferDef(id)) || id.EndsWith(".story", StringComparison.Ordinal));
            for (int i = S.dataExcluded.Count - 1; i > 0; i--) if (S.dataExcluded.IndexOf(S.dataExcluded[i]) < i) S.dataExcluded.RemoveAt(i);
            foreach (var list in new[] { S.dataExtra, S.crowdNoise, S.dataCleaned })
                list.RemoveAll(n => n == null || XgCatalog.Dataset(n.dataset) == null || !FiniteCollaboration(n.count) || n.count < 0);
            S.crowdOn.RemoveAll(id => DataOfferDef(id + ".crowd") == null);
            RepairCrawl();
            RepairDownloads();
        }
    }
}
