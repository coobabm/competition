using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Hardware
{
    /// <summary>One graphics card model (design v1.1 §3, §14.3).</summary>
    public sealed class GpuModel
    {
        public string id = "", name = "", nameEn = "", brand = "", brandEn = "";
        /// <summary>Training speed relative to the starting GTX 980 Ti (×1).</summary>
        public double compute = 1;
        public double vramMB = 6144, watts = 250;
        /// <summary>New price on 淘货 (0 = never sold new in the game).</summary>
        public double price;
        /// <summary>What a used one fetches on 喵鱼 before newer cards push it down.</summary>
        public double usedPrice;
        /// <summary>Launch day; it can be bought from this day on.</summary>
        public DateTime release;
        /// <summary>Only a rumour (1080 Ti, §10.3): listed, never sold.</summary>
        public bool rumour;
        /// <summary>The shop's listing title and one-line pitch.</summary>
        public string title = "", titleEn = "", pitch = "", pitchEn = "";
        /// <summary>Monthly sales shown on the listing.</summary>
        public int sold;
        public bool SoldNew => price > 0 && !rumour;
    }

    /// <summary>
    /// The 2016 hardware timeline (design v1.1 §14.3). Each owned card adds its compute (×, relative to the
    /// 980 Ti the player starts with), VRAM and watts. New cards come out on the in-game calendar
    /// (<see cref="GameCalendar"/>); every newer card that matches an older one knocks 10% off the older card's
    /// second-hand price on 喵鱼. The 1080 Ti is only a rumour and never goes on sale before the game ends.
    /// </summary>
    public static class HardwareCatalog
    {
        public const string Gtx750Ti = "gtx750ti", Gtx970 = "gtx970", Gtx980Ti = "gtx980ti", Gtx1080 = "gtx1080", Gtx1070 = "gtx1070",
            Rx480 = "rx480", Gtx1060 = "gtx1060", TitanXp = "titanxp", Gtx1080Ti = "gtx1080ti";

        /// <summary>The card a save from before the hardware models had for each of its cards (§3 starting rig).</summary>
        public const string LegacyModel = Gtx980Ti;

        /// <summary>Each newer card that matches an older one takes this share off its used price, down to the floor.</summary>
        public const double UsedStep = .9, UsedFloor = .3;
        /// <summary>A newer card counts against an older one when it has at least this share of its compute.</summary>
        public const double MatchShare = .9;
        /// <summary>Chance a new RX 480 trips the breaker once (§14.3 "烧 PCIe").</summary>
        public const double PcieBurnChance = .25;

        /// <summary>Samsung 950 Pro NVMe (§14.3, September): data loads faster, every card trains this much faster.</summary>
        public const double NvmeBoost = 1.1, NvmePrice = 1499, NvmeWatts = 6;
        public static readonly DateTime NvmeRelease = new DateTime(2016, 9, 1);

        /// <summary>A retired 网吧 machine on 喵鱼 (§3, §14.3 October): its own case with one GTX 970 in it.</summary>
        public const int CafeBoxLimit = 8;
        public const double CafeBoxPrice = 1200;
        public static readonly DateTime CafeRelease = new DateTime(2016, 10, 1);

        /// <summary>Singles' Day (§14.1): 淘货 takes 20% off every card for the day.</summary>
        public static readonly DateTime SinglesDay = new DateTime(2016, 11, 11);
        public const double SinglesDayFactor = .8;

        public static readonly GpuModel[] Gpus =
        {
            new GpuModel { id = Gtx750Ti, name = "GTX 750 Ti 2G", nameEn = "GTX 750 Ti 2GB", brand = "亮影", brandEn = "Liangying",
                compute = .3, vramMB = 2048, watts = 60, price = 0, usedPrice = 450, release = new DateTime(2014, 2, 18), sold = 0,
                title = "网吧拆机 750Ti 2G 免供电 亮机神卡", titleEn = "Ex-net-cafe 750 Ti 2GB, no power cable, the classic display card",
                pitch = "能亮就行。", pitchEn = "It lights up the screen." },
            new GpuModel { id = Gtx970, name = "GTX 970 4G", nameEn = "GTX 970 4GB", brand = "亮影", brandEn = "Liangying",
                compute = .375, vramMB = 3584, watts = 145, price = 0, usedPrice = 1100, release = new DateTime(2014, 9, 19), sold = 0,
                title = "GTX 970 4G（其实是 3.5G）网吧退役 成色好", titleEn = "GTX 970 4GB (really 3.5GB), retired from a net cafe, good condition",
                pitch = "显存标 4G，能用的 3.5G。", pitchEn = "Says 4 GB. 3.5 GB of it works." },
            new GpuModel { id = Gtx980Ti, name = "GTX 980 Ti 6G", nameEn = "GTX 980 Ti 6GB", brand = "亮影", brandEn = "Liangying",
                compute = 1, vramMB = 6144, watts = 250, price = 0, usedPrice = 3200, release = new DateTime(2015, 6, 1), sold = 0,
                title = "GTX 980 Ti 6G 上代旗舰", titleEn = "GTX 980 Ti 6GB, last generation's flagship",
                pitch = "你的开局卡。", pitchEn = "The card you started with." },
            new GpuModel { id = Gtx1080, name = "GTX 1080 8G", nameEn = "GTX 1080 8GB", brand = "亮影", brandEn = "Liangying",
                compute = 1.5, vramMB = 8192, watts = 180, price = 4999, usedPrice = 3500, release = new DateTime(2016, 5, 27), sold = 1832,
                title = "【顺丰包邮】GTX 1080 公版 8G 首批现货 限购一张", titleEn = "[Free SF shipping] GTX 1080 Founders 8GB, first batch in stock, one per customer",
                pitch = "Pascal 新架构，功耗反而更低。", pitchEn = "New Pascal architecture, and it draws less power." },
            new GpuModel { id = Gtx1070, name = "GTX 1070 8G", nameEn = "GTX 1070 8GB", brand = "亮影", brandEn = "Liangying",
                compute = 1.3, vramMB = 8192, watts = 150, price = 2899, usedPrice = 2030, release = new DateTime(2016, 6, 10), sold = 5210,
                title = "GTX 1070 8G 性能接近泰坦 价格不到一半 包邮", titleEn = "GTX 1070 8GB, Titan-class speed at under half the price, free shipping",
                pitch = "等等党的胜利。", pitchEn = "Patience paid off." },
            new GpuModel { id = Rx480, name = "RX 480 8G", nameEn = "RX 480 8GB", brand = "红芯", brandEn = "Hongxin",
                compute = 1.2, vramMB = 8192, watts = 150, price = 1999, usedPrice = 1400, release = new DateTime(2016, 6, 29), sold = 2976,
                title = "RX 480 8G A卡战未来 甜品卡 包邮", titleEn = "RX 480 8GB, AMD is the future, the sweet-spot card, free shipping",
                pitch = "注：首批有用户反映 PCIe 供电超标。", pitchEn = "Note: some early buyers report PCIe power draw over spec." },
            new GpuModel { id = Gtx1060, name = "GTX 1060 6G", nameEn = "GTX 1060 6GB", brand = "亮影", brandEn = "Liangying",
                compute = .9, vramMB = 6144, watts = 120, price = 1999, usedPrice = 1400, release = new DateTime(2016, 7, 19), sold = 8843,
                title = "GTX 1060 6G 第二张卡首选 双卡训练 包邮", titleEn = "GTX 1060 6GB, the second card to get, train on two, free shipping",
                pitch = "AlexNet 当年也是两张卡训出来的。", pitchEn = "AlexNet was trained on two cards too." },
            new GpuModel { id = TitanXp, name = "TITAN X (Pascal) 12G", nameEn = "TITAN X (Pascal) 12GB", brand = "亮影", brandEn = "Liangying",
                compute = 2.5, vramMB = 12288, watts = 250, price = 9999, usedPrice = 7000, release = new DateTime(2016, 8, 2), sold = 217,
                title = "TITAN X Pascal 12G 官方直邮 炼丹神卡", titleEn = "TITAN X Pascal 12GB, shipped direct, the deep-learning card",
                pitch = "钱包警告：这张卡比整台电脑还贵。", pitchEn = "Wallet warning: this card costs more than the whole computer." },
            new GpuModel { id = Gtx1080Ti, name = "GTX 1080 Ti", nameEn = "GTX 1080 Ti", brand = "亮影", brandEn = "Liangying",
                compute = 2.2, vramMB = 11264, watts = 250, price = 0, usedPrice = 0, release = new DateTime(2016, 11, 1), rumour = true, sold = 0,
                title = "【预约】GTX 1080 Ti 到货时间待定 先收藏", titleEn = "[Pre-order] GTX 1080 Ti, arrival date unknown, add to favourites",
                pitch = "传闻明年开春发布。", pitchEn = "Rumoured for next spring." },
        };

        static readonly Dictionary<string, GpuModel> byId = new Dictionary<string, GpuModel>(StringComparer.Ordinal);
        static HardwareCatalog() { foreach (var g in Gpus) byId[g.id] = g; }

        public static GpuModel Gpu(string id) { return id != null && byId.TryGetValue(id, out var g) ? g : null; }
        /// <summary>A card a save may hold (rumours never reach a save).</summary>
        public static bool Ownable(string id) { var g = Gpu(id); return g != null && !g.rumour; }

        /// <summary>Out on this day (launch day included).</summary>
        public static bool Released(GpuModel g, DateTime date) { return g != null && date.Date >= g.release.Date; }

        /// <summary>淘货 sells it new on this day.</summary>
        public static bool OnSale(GpuModel g, DateTime date) { return g != null && g.SoldNew && Released(g, date); }

        /// <summary>淘货's price on this day (Singles' Day is 20% off).</summary>
        public static double Price(GpuModel g, DateTime date)
        {
            if (g == null || !g.SoldNew) return 0;
            return Math.Round(g.price * (date.Date == SinglesDay ? SinglesDayFactor : 1));
        }

        /// <summary>Newer cards out by this day that do at least <see cref="MatchShare"/> of this card's work.</summary>
        public static int NewerRivals(GpuModel g, DateTime date)
        {
            if (g == null) return 0;
            int n = 0;
            foreach (var other in Gpus)
                if (other != g && !other.rumour && other.release > g.release && Released(other, date) && other.compute >= g.compute * MatchShare) n++;
            return n;
        }

        /// <summary>
        /// What the card fetches on 喵鱼 this day: its used price, 10% less for every newer card that matches it,
        /// never below 30%. Whole yuan, rounded down to tens like a real listing.
        /// </summary>
        public static double UsedPrice(GpuModel g, DateTime date)
        {
            if (g == null || g.rumour || g.usedPrice <= 0) return 0;
            double v = g.usedPrice * Math.Max(UsedFloor, Math.Pow(UsedStep, NewerRivals(g, date)));
            return Math.Max(10, Math.Floor(v / 10) * 10);
        }

        /// <summary>鲁大师's score after a new card (§3, §14.3): 98% for a new card, 99.7% for a GTX 1080.</summary>
        public static double LudashiPercent(string id)
        {
            switch (id)
            {
                case Gtx1080: return 99.7;
                case TitanXp: return 99.9;
                case Gtx750Ti: return 37;
                case Gtx970: return 76;
                default: return 98;
            }
        }

        /// <summary>Cards on sale or announced on a day, in listing order (newest first; the rumour last).</summary>
        public static List<GpuModel> Listings(DateTime date)
        {
            var list = new List<GpuModel>();
            foreach (var g in Gpus) if (OnSale(g, date)) list.Add(g);
            list.Sort((a, b) => b.release.CompareTo(a.release));
            var ti = Gpu(Gtx1080Ti);
            if (Released(ti, date)) list.Add(ti);
            return list;
        }

        /// <summary>Second-hand cards other sellers list on 喵鱼 (§14.3: 970, 750Ti, …).</summary>
        public static readonly string[] UsedListings = { Gtx750Ti, Gtx970, Gtx980Ti };
    }
}
