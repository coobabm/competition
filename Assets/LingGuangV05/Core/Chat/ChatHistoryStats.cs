using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Chat
{
    /// <summary>One line of local chat history, as the statistics see it.</summary>
    public struct ChatLine
    {
        /// <summary>The player wrote it.</summary>
        public bool mine;
        public string text;
        /// <summary>When it was written (game seconds); only the gaps between lines matter.</summary>
        public double seconds;

        public ChatLine(bool mine, string text, double seconds) { this.mine = mine; this.text = text; this.seconds = seconds; }
    }

    /// <summary>A phrase and how often it appears.</summary>
    public sealed class PhraseCount
    {
        public string phrase;
        public int count;
        /// <summary>Counted from the save's own messages (false: one of the stock words with no real use).</summary>
        public bool fromHistory;

        public override string ToString() => phrase + " ×" + count;
    }

    /// <summary>
    /// Statistics over the player's YY history for the stage-3 cutscene in which the AI reads the local chat
    /// database (design 女友系统与YY里的AI §5): how many records it reads, which phrases float past like danmaku,
    /// and which opening line it learns to greet with. Pure C#, no Unity.
    ///
    /// A "phrase" is a short line (at most <see cref="MaxPhraseUnits"/> units: one per CJK character, one per Latin
    /// word) after <see cref="Normalize"/>. Longer lines only count the stock catchphrases they contain. The stock
    /// words stand for the years of history before the game starts, so they carry base counts and real use adds to them.
    /// </summary>
    public static class ChatHistoryStats
    {
        /// <summary>Records in msg.db before the game's own messages (years of chatting the save does not hold).</summary>
        public const int RecordBase = 12000;
        /// <summary>A line of the player's after this much silence in a conversation opens a new exchange.</summary>
        public const double OpenerGapSeconds = 1800;
        public const int MaxPhraseUnits = 6, MaxPhraseLength = 16;

        /// <summary>The stock words (zh, en) and their base counts from the history before the game.</summary>
        static readonly string[] StockZh = { "哈哈哈", "在吗", "晚安", "卧槽", "吃了没" };
        static readonly string[] StockEn = { "lol", "u there?", "night", "wtf", "eaten yet?" };
        static readonly int[] StockBase = { 2310, 1184, 846, 571, 233 };

        public static string DefaultOpener(bool english) => english ? StockEn[1] : StockZh[1];

        /// <summary>Records the AI reads: the base plus every real message in the save.</summary>
        public static int RecordCount(int realMessages) => RecordBase + Math.Max(0, realMessages);

        // ───────────── normalising ─────────────

        const string Trim = " \t\r\n　。，、！？…~～.,!?;；:：\"'“”‘’()（）[]【】<>《》-—_*";

        /// <summary>
        /// Trims surrounding punctuation and spaces, lower-cases Latin letters and shortens laughter and other
        /// long repeats (「哈哈哈哈哈」 → 「哈哈哈」, "hahahaha" → "hahaha"). Never null.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string t = text.Trim(Trim.ToCharArray()).ToLowerInvariant();
            if (t.Length == 0) return "";
            t = CollapseRuns(t, 1);
            t = CollapseRuns(t, 2);
            return t;
        }

        /// <summary>Keeps at most three repeats of any unit of <paramref name="size"/> characters in a row.</summary>
        static string CollapseRuns(string t, int size)
        {
            var sb = new StringBuilder(t.Length);
            int i = 0;
            while (i < t.Length)
            {
                if (i + size > t.Length) { sb.Append(t, i, t.Length - i); break; }
                string unit = t.Substring(i, size);
                int run = 1;
                while (i + (run + 1) * size <= t.Length && string.CompareOrdinal(t, i + run * size, unit, 0, size) == 0) run++;
                if (run >= 3)
                {
                    for (int k = 0; k < 3; k++) sb.Append(unit);
                    i += run * size;
                }
                else { sb.Append(t[i]); i++; }
            }
            return sb.ToString();
        }

        /// <summary>Length in units: each CJK (non-ASCII) character is one, each run of Latin letters or digits is one.</summary>
        public static int Units(string normalized)
        {
            if (string.IsNullOrEmpty(normalized)) return 0;
            int units = 0;
            bool inWord = false;
            foreach (char c in normalized)
            {
                bool ascii = c < 128;
                if (ascii && char.IsLetterOrDigit(c)) { if (!inWord) units++; inWord = true; continue; }
                inWord = false;
                if (!ascii && !char.IsWhiteSpace(c) && !char.IsPunctuation(c)) units++;
            }
            return units;
        }

        /// <summary>A normalised line short enough to count as a phrase on its own.</summary>
        public static bool IsPhrase(string normalized)
        {
            if (string.IsNullOrEmpty(normalized) || normalized.Length > MaxPhraseLength) return false;
            int units = Units(normalized);
            return units >= 1 && units <= MaxPhraseUnits;
        }

        // ───────────── phrases ─────────────

        /// <summary>
        /// Phrase counts in the player's own lines: short lines count whole; longer ones count the stock words
        /// inside them (both languages). Most frequent first, ties in order of first use.
        /// </summary>
        public static List<PhraseCount> Phrases(IEnumerable<string> myLines)
        {
            var order = new List<PhraseCount>();
            var index = new Dictionary<string, PhraseCount>(StringComparer.Ordinal);
            void Add(string phrase, int n)
            {
                if (!index.TryGetValue(phrase, out var p)) { p = new PhraseCount { phrase = phrase, fromHistory = true }; index[phrase] = p; order.Add(p); }
                p.count += n;
            }
            if (myLines != null)
                foreach (var line in myLines)
                {
                    string n = Normalize(line);
                    if (n.Length == 0) continue;
                    if (IsPhrase(n)) { Add(n, 1); continue; }
                    for (int s = 0; s < StockZh.Length; s++)
                    {
                        int zh = Occurrences(n, StockZh[s]);
                        if (zh > 0) Add(StockZh[s], zh);
                        int en = Occurrences(n, Normalize(StockEn[s]));
                        if (en > 0) Add(StockEn[s], en);
                    }
                }
            return Sorted(order);
        }

        static int Occurrences(string text, string word)
        {
            if (string.IsNullOrEmpty(word)) return 0;
            int n = 0;
            for (int at = text.IndexOf(word, StringComparison.Ordinal); at >= 0; at = text.IndexOf(word, at + word.Length, StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>
        /// What floats past in the cutscene, at most <paramref name="max"/> phrases, most frequent first: the player's
        /// own top phrases (the three most used, plus any used at least twice; never more than max − 2), then the
        /// stock words of the chosen language with their base counts (real use of a stock word adds to it).
        /// </summary>
        public static List<PhraseCount> Frequent(IEnumerable<string> myLines, bool english, int max = 6)
        {
            var real = Phrases(myLines);
            var stock = english ? StockEn : StockZh;
            var picked = new List<PhraseCount>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            int realRoom = Math.Max(0, max - 2), realTaken = 0;
            for (int s = 0; s < stock.Length; s++)
            {
                int extra = 0;
                foreach (var p in real) if (p.phrase == stock[s] || p.phrase == Normalize(stock[s])) extra += p.count;
                picked.Add(new PhraseCount { phrase = stock[s], count = StockBase[s] + extra, fromHistory = extra > 0 });
                used.Add(stock[s]); used.Add(Normalize(stock[s]));
            }
            // The other language's stock words are not real phrases of this player either.
            foreach (var w in english ? StockZh : StockEn) { used.Add(w); used.Add(Normalize(w)); }
            var mine = new List<PhraseCount>();
            for (int i = 0; i < real.Count && realTaken < realRoom; i++)
            {
                var p = real[i];
                if (used.Contains(p.phrase)) continue;
                if (realTaken >= 3 && p.count < 2) break;
                mine.Add(new PhraseCount { phrase = p.phrase, count = p.count, fromHistory = true });
                realTaken++;
            }
            // Room for the player's own phrases comes first; stock words fill what is left.
            var result = new List<PhraseCount>(mine);
            foreach (var p in Sorted(picked)) { if (result.Count >= max) break; result.Add(p); }
            if (result.Count > max) result.RemoveRange(max, result.Count - max);
            return Sorted(result);
        }

        static List<PhraseCount> Sorted(List<PhraseCount> list)
        {
            var copy = new List<PhraseCount>(list);
            var rank = new Dictionary<PhraseCount, int>();
            for (int i = 0; i < copy.Count; i++) rank[copy[i]] = i;
            copy.Sort((a, b) => a.count != b.count ? b.count.CompareTo(a.count) : rank[a].CompareTo(rank[b]));
            return copy;
        }

        // ───────────── openers ─────────────

        /// <summary>
        /// The player's most frequent opening line: a short line of theirs that starts a conversation or follows at
        /// least <paramref name="gap"/> seconds of silence in it. Ties go to the one used first. With none, 「在吗」.
        /// Each conversation's lines must be in time order.
        /// </summary>
        public static string FavoriteOpener(IEnumerable<IList<ChatLine>> conversations, bool english, double gap = OpenerGapSeconds)
        {
            var order = new List<PhraseCount>();
            var index = new Dictionary<string, PhraseCount>(StringComparer.Ordinal);
            if (conversations != null)
                foreach (var conv in conversations)
                {
                    if (conv == null) continue;
                    bool any = false;
                    double last = 0;
                    foreach (var line in conv)
                    {
                        bool opens = !any || line.seconds - last >= gap;
                        any = true; last = line.seconds;
                        if (!line.mine || !opens) continue;
                        string n = Normalize(line.text);
                        if (!IsPhrase(n)) continue;
                        if (!index.TryGetValue(n, out var p)) { p = new PhraseCount { phrase = n, fromHistory = true }; index[n] = p; order.Add(p); }
                        p.count++;
                    }
                }
            if (order.Count == 0) return DefaultOpener(english);
            return Sorted(order)[0].phrase;
        }
    }
}
