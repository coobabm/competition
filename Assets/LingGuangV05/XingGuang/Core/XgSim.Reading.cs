using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>
        /// Distinct words the resident spider read off the desktop (chats, mail, notes, the encyclopedia page) and
        /// credited as samples of <see cref="XgSim.ReadDataset"/>. Old saves read 0: those reads were never recorded.
        /// </summary>
        public double residentWords;
        /// <summary>Version of the reading ledger. 0 = saved before reading counted (see <see cref="XgSim.RepairReading"/>), 1 = current.</summary>
        public int readSchema;
    }

    /// <summary>
    /// Words the spider reads count as samples (参数量与数据量主线: they feed the data bar D like any other row).
    /// <para>
    /// The rule, for both readers: <b>one word = <see cref="SamplesPerWord"/> sample</b> (1:1) of the text corpus
    /// <see cref="ReadDataset"/> (人民日报语料, the next-character dataset), booked as crowd-style rows in
    /// <see cref="XgState.dataExtra"/>.
    /// </para>
    /// <list type="bullet">
    /// <item>The crawler (<see cref="CrawlGrab"/>, XgSim.Crawl.cs) is a paid run: up to <see cref="CrawlCap"/> words, with
    /// <see cref="CrawlNoise"/> junk rows riding along (adverts, typos) that the data bar's noise penalty takes off again.</item>
    /// <item>The resident spider (<see cref="ResidentRead"/>) reads the computer for free: every <i>new</i> word its foot
    /// lands on counts once, clean. A word it has read recently (the last <see cref="ReadMemory"/> distinct words) does
    /// not count again, so walking over the same screen is worth nothing; new chats, mail and pages are worth words. A
    /// fresh launch forgets the memory, so a screen can pay again once per launch: bounded by the text on screen.</item>
    /// </list>
    /// Old saves: crawler words were already booked 1:1 into <see cref="XgState.dataExtra"/> when they were grabbed, so
    /// nothing is credited twice; the resident's earlier reads were never recorded and cannot be recovered (see
    /// <see cref="RepairReading"/>).
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The text corpus the read words go into.</summary>
        public const string ReadDataset = CrawlDataset;
        /// <summary>Samples one read word is worth, for the crawler and the resident alike.</summary>
        public const double SamplesPerWord = 1;
        /// <summary>Distinct recently read words the resident remembers (a word in memory does not count again).</summary>
        public const int ReadMemory = 2048;
        /// <summary>Reads between two re-evaluations of the corpus' training runs.</summary>
        public const int ReadEvalBatch = 25;
        /// <summary>The lab's log says so every this many resident words.</summary>
        public const int ReadSayEvery = 250;
        /// <summary>Version stamped on the reading ledger.</summary>
        public const int ReadSchemaNow = 1;

        readonly HashSet<int> readSeen = new HashSet<int>();
        readonly Queue<int> readOrder = new Queue<int>();
        int readPending;

        /// <summary>Words read in total, by the crawler and the resident.</summary>
        public double ReadWordsTotal => Math.Max(0, S.crawlWords) + Math.Max(0, S.residentWords);
        /// <summary>Samples those words are worth before the data bar's noise penalty.</summary>
        public double ReadSamplesTotal => ReadWordsTotal * SamplesPerWord;

        /// <summary>
        /// The resident spider read a word (its foot landed on it). Counts one sample when it is a word (a letter or a
        /// character in it) it has not read lately. Returns true when it counted.
        /// </summary>
        public bool ResidentRead(string word)
        {
            int key = WordKey(word);
            if (key == 0 || readSeen.Contains(key)) return false;
            readSeen.Add(key); readOrder.Enqueue(key);
            if (readOrder.Count > ReadMemory) readSeen.Remove(readOrder.Dequeue());
            S.residentWords += 1;
            SetCount(S.dataExtra, ReadDataset, ExtraSamples(ReadDataset) + SamplesPerWord);
            if (++readPending >= ReadEvalBatch) FlushReads();
            if (((long)S.residentWords) % ReadSayEvery == 0)
            {
                var d = XgCatalog.Dataset(ReadDataset);
                string name = d != null ? T(d.name, d.nameEn) : ReadDataset;
                Say(T("它读了 " + ReadSayEvery + " 个词，记作 " + F(ReadSayEvery * SamplesPerWord, "0") + " 条样本，进了「" + name + "」。",
                    "It read " + ReadSayEvery + " more words: " + F(ReadSayEvery * SamplesPerWord, "0") + " samples into " + name + "."));
            }
            return true;
        }

        /// <summary>Re-evaluates the runs on the corpus after a batch of reads (training checks the new sample count).</summary>
        void FlushReads()
        {
            readPending = 0;
            foreach (var run in Runs) if (run.dataset == ReadDataset) Evaluate(run);
        }

        /// <summary>A stable non-zero key for a word, or 0 when it is not one (empty, no letter or character, absurdly long).</summary>
        public static int WordKey(string word)
        {
            if (string.IsNullOrEmpty(word)) return 0;
            int start = 0, end = word.Length;
            while (start < end && !char.IsLetterOrDigit(word[start])) start++;
            while (end > start && !char.IsLetterOrDigit(word[end - 1])) end--;
            if (end - start <= 0 || end - start > 40) return 0;
            bool letter = false;
            uint h = 2166136261;
            for (int i = start; i < end; i++)
            {
                char c = char.ToLowerInvariant(word[i]);
                if (char.IsLetter(c)) letter = true;
                h = (h ^ c) * 16777619;
            }
            if (!letter) return 0;
            int key = (int)h;
            return key == 0 ? 1 : key;
        }

        /// <summary>The rule in one line for the player.</summary>
        public string ReadRuleText() => T("1 个词 = " + F(SamplesPerWord, "0") + " 条样本", "1 word = " + F(SamplesPerWord, "0") + " sample");

        /// <summary>
        /// "读到的词 4,210 → +4,210 样本（爬虫 1,200 · 蜘蛛 3,010）· 1 个词 = 1 条样本"; empty until something was read.
        /// Shown in the corpus' source breakdown and on the data page.
        /// </summary>
        public string ReadLine()
        {
            double words = ReadWordsTotal;
            if (words < 1) return "";
            return T("读到的词 ", "Words read ") + SamplesText(words) + " → +" + SamplesText(ReadSamplesTotal) + T(" 样本", " samples")
                + T("（爬虫 ", " (crawler ") + SamplesText(S.crawlWords) + T(" · 蜘蛛 ", " · spider ") + SamplesText(S.residentWords) + "）· " + ReadRuleText();
        }

        /// <summary>Short form for a page header: "含读到的词 +4,210 样本" (empty until something was read).</summary>
        public string ReadShort()
        {
            double s = ReadSamplesTotal;
            return s < 1 ? "" : T("含读到的词 +" + SamplesText(s) + " 样本", "incl. words read +" + SamplesText(s) + " samples");
        }

        /// <summary>
        /// Settles the reading ledger of a loaded save. Crawler words were booked into the corpus' rows when they were
        /// grabbed, so a save already holds them; this only tops up a shortfall (a tally with no rows behind it), once,
        /// and never again once the save carries <see cref="ReadSchemaNow"/>. Resident words read before the ledger
        /// existed were never recorded, so there is nothing to credit for them.
        /// </summary>
        internal void RepairReading()
        {
            if (!FiniteCollaboration(S.residentWords) || S.residentWords < 0) S.residentWords = 0;
            if (S.readSchema < ReadSchemaNow)
            {
                double missing = Math.Max(0, S.crawlWords) * SamplesPerWord - ExtraSamples(ReadDataset);
                if (missing >= 1)
                {
                    SetCount(S.dataExtra, ReadDataset, ExtraSamples(ReadDataset) + missing);
                    SetCount(S.crowdNoise, ReadDataset, Count(S.crowdNoise, ReadDataset) + missing * CrawlNoise);
                }
                S.readSchema = ReadSchemaNow;
            }
        }
    }
}
