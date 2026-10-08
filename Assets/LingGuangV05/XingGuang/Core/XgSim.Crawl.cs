using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Crawler runs paid for (摆渡百科爬虫). Old saves read 0.</summary>
        public int crawlRuns;
        /// <summary>Words the crawler has grabbed over all runs (each one is a sample of <see cref="XgSim.CrawlDataset"/>).</summary>
        public double crawlWords;
    }

    /// <summary>
    /// 摆渡百科爬虫: a data source on the 数据 page. One run costs a flat fee, opens a crawler on a generated
    /// encyclopedia page, and every word a foot of the spider grabs is one sample of the text corpus, up to
    /// <see cref="CrawlCap"/> per run. The rows go the crowd-task way (<see cref="XgState.dataExtra"/> plus a share of
    /// wrong rows in <see cref="XgState.crowdNoise"/>, which 数据清洗 halves), so the main line's data bar counts them at
    /// the dataset's weight like any other sample. Ending a run early keeps what was grabbed; the rest of the run is lost.
    /// The run itself (start, grabs, end) lives only in memory: a save made mid-run already holds every grabbed row.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string CrawlOfferId = "news.crawl", CrawlDataset = "news";
        /// <summary>Most words one run can grab.</summary>
        public const int CrawlCap = 200;
        /// <summary>¥ per run: the crowd task's price per label times the cap (¥0.04 × 200 = ¥8).</summary>
        public const double CrawlPrice = CrawlCap * CrowdPricePerLabel;
        /// <summary>How long a run lasts on screen, in seconds.</summary>
        public const double CrawlSeconds = 60;
        /// <summary>Share of junk rows (adverts, typos, edit-war leftovers) before 数据清洗.</summary>
        public const double CrawlNoise = .12;
        /// <summary>The crawler opens with stage 3 (August 2016), next to the RNN-era text work.</summary>
        public const int CrawlStage = 3;

        /// <summary>A run is going on (paid, not yet ended).</summary>
        public bool CrawlActive { get; private set; }
        /// <summary>Words grabbed in the current (or the last) run.</summary>
        public int CrawlTaken { get; private set; }
        /// <summary>Words the current run may still grab.</summary>
        public int CrawlLeft => CrawlActive ? Math.Max(0, CrawlCap - CrawlTaken) : 0;

        static XgDataOffer CrawlOfferDef() => new XgDataOffer
        {
            id = CrawlOfferId, datasetId = CrawlDataset, source = XgDataSource.Crawl,
            name = "摆渡百科爬虫", nameEn = "Bodu Encyclopedia crawler",
            description = "放一只爬虫去摆渡百科：它踩到的每个词都抓回来当一条语料。按次付钱，一次最多 " + CrawlCap + " 条，有少量广告和错字。",
            descriptionEn = "Let a crawler loose on Bodu Encyclopedia: every word it steps on comes back as one row of text. Paid per run, at most " + CrawlCap + " rows a run, with a few adverts and typos.",
            price = CrawlPrice, samples = CrawlCap, noise = CrawlNoise, stage = CrawlStage, month = StageMonth(CrawlStage),
        };

        /// <summary>Pays for a run and starts it. False if one is going on, it is not on offer yet, or money is short.</summary>
        public bool StartCrawl(IXgHost host)
        {
            var def = DataOfferDef(CrawlOfferId);
            if (def == null || host == null) return false;
            var why = OfferBlocker(def, out string whyEn);
            if (why != null) { Say(T(why, whyEn)); return false; }
            if (!host.Spend(def.price)) { Say(T("经费不足 ¥" + F(def.price, "0"), "Not enough money: ¥" + F(def.price, "0"))); return false; }
            S.totalSpent += def.price;
            S.crawlRuns++;
            CrawlActive = true;
            CrawlTaken = 0;
            DataOfferBought?.Invoke(Offer(def.id));
            return true;
        }

        /// <summary>
        /// The spider grabbed words: each is one sample of <see cref="CrawlDataset"/> until the run's cap is reached.
        /// Returns the words that counted (0 when no run is going on or the cap is full).
        /// </summary>
        public int CrawlGrab(int words = 1)
        {
            if (!CrawlActive || words <= 0) return 0;
            int n = Math.Min(words, CrawlLeft);
            if (n <= 0) return 0;
            CrawlTaken += n;
            S.crawlWords += n;
            SetCount(S.dataExtra, CrawlDataset, ExtraSamples(CrawlDataset) + n);
            SetCount(S.crowdNoise, CrawlDataset, Count(S.crowdNoise, CrawlDataset) + n * CrawlNoise);
            return n;
        }

        /// <summary>Ends the run (time up, cap full or the window closed early). What was grabbed stays. Returns the words grabbed.</summary>
        public int EndCrawl()
        {
            if (!CrawlActive) return 0;
            CrawlActive = false;
            var d = XgCatalog.Dataset(CrawlDataset);
            string name = d != null ? T(d.name, d.nameEn) : CrawlDataset;
            Say(T("爬虫收工：抓回 " + CrawlTaken + " 条，进了「" + name + "」。", "Crawler done: " + CrawlTaken + " rows went into " + name + "."));
            foreach (var run in Runs) if (run.dataset == CrawlDataset) Evaluate(run);
            return CrawlTaken;
        }

        void RepairCrawl()
        {
            if (S.crawlRuns < 0) S.crawlRuns = 0;
            if (!FiniteCollaboration(S.crawlWords) || S.crawlWords < 0) S.crawlWords = 0;
            RepairSpider();
        }
    }
}
