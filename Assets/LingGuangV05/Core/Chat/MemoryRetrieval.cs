using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Chat
{
    /// <summary>
    /// What a memory search looks for: one or more texts, each with a weight. The newest message usually weighs 1
    /// and the one before it less, so a follow-up such as 「那改到哪天了？」 still finds the note about the exam.
    /// </summary>
    public sealed class MemoryQuery
    {
        public readonly List<KeyValuePair<string, double>> parts = new List<KeyValuePair<string, double>>();

        public MemoryQuery Add(string text, double weight = 1)
        {
            if (!string.IsNullOrWhiteSpace(text) && weight > 0) parts.Add(new KeyValuePair<string, double>(text, weight));
            return this;
        }

        public bool Empty => parts.Count == 0;

        public static MemoryQuery Of(string latest, string previous = null, double previousWeight = .5)
            => new MemoryQuery().Add(latest).Add(previous, previousWeight);
    }

    /// <summary>
    /// Scores documents against a query. One score per document, in the same order. A score of 0 or less means the
    /// document is not relevant at all and must never be returned, so each scorer owns its own threshold.
    /// BM25 (<see cref="Bm25Scorer"/>) is the default. An embedding scorer can be added behind this seam later, for
    /// example llama-server's /v1/embeddings with a separate small embedding GGUF such as Qwen3-Embedding-0.6B
    /// (cosine similarity minus a cut-off). No such model ships with the game today.
    /// </summary>
    public interface IMemoryScorer
    {
        double[] Score(MemoryQuery query, IList<string> documents);
    }

    /// <summary>
    /// Tokens for lexical search over mixed Chinese and English: every CJK character (unigram) and every pair of
    /// neighbouring CJK characters (bigram), plus lowercase ASCII words and digit runs. Function characters such as
    /// 的 了 我 你 are not unigram tokens, and a bigram made only of them is dropped, so 「你好吗」 matches nothing.
    /// </summary>
    public static class MemoryTokens
    {
        public enum Kind { Unigram, Bigram, Word, Number }

        /// <summary>CJK characters too common to mean anything on their own.</summary>
        public const string StopChars = "的了是我你他她它在有和就不也都很吗呢吧啊哦嗯么这那个一说要会想把被让给对跟与还又再才太好得地着过们啦呀嘛哈么啥什怎样能可以去来到上下里中多少点些没主人";

        /// <summary>Bigrams in nearly every note (「主人」 is how the notebook names the owner).</summary>
        public static readonly HashSet<string> StopBigrams = new HashSet<string>(StringComparer.Ordinal) { "主人", "它自", "自己" };

        static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "an", "the", "is", "are", "am", "was", "were", "be", "been", "i", "you", "he", "she", "it", "we", "they", "me", "my", "your",
            "his", "her", "its", "our", "to", "of", "and", "or", "in", "on", "at", "for", "with", "that", "this", "do", "did", "does", "not",
            "no", "yes", "so", "but", "just", "what", "how", "why", "can", "will", "would", "about", "said", "says", "up", "oh", "ok", "owner",
        };

        public static bool IsCjk(char c) => c >= 0x3400 && c <= 0x9FFF || c >= 0xF900 && c <= 0xFAFF;
        static bool Stop(char c) => StopChars.IndexOf(c) >= 0;

        /// <summary>The tokens of a text, each with its kind. Repeats are kept (term frequency counts them).</summary>
        public static List<KeyValuePair<string, Kind>> Tokenize(string text)
        {
            var list = new List<KeyValuePair<string, Kind>>();
            if (string.IsNullOrEmpty(text)) return list;
            var word = new StringBuilder();
            bool digits = false;
            void FlushWord()
            {
                if (word.Length == 0) return;
                string w = word.ToString();
                if (digits) list.Add(new KeyValuePair<string, Kind>(w, Kind.Number));
                else if (w.Length >= 2 && !StopWords.Contains(w)) list.Add(new KeyValuePair<string, Kind>(w, Kind.Word));
                word.Clear();
            }
            char previous = '\0';
            foreach (char raw in text)
            {
                char c = char.ToLowerInvariant(raw);
                if (IsCjk(c))
                {
                    FlushWord();
                    if (!Stop(c)) list.Add(new KeyValuePair<string, Kind>(c.ToString(), Kind.Unigram));
                    if (previous != '\0' && !(Stop(previous) && Stop(c)))
                    {
                        string pair = new string(new[] { previous, c });
                        if (!StopBigrams.Contains(pair)) list.Add(new KeyValuePair<string, Kind>(pair, Kind.Bigram));
                    }
                    previous = c;
                    continue;
                }
                previous = '\0';
                bool letter = c >= 'a' && c <= 'z', digit = c >= '0' && c <= '9';
                if (letter || digit)
                {
                    // A switch between letters and digits ends the token: "11月" → "11", "gtx970" → "gtx", "970".
                    if (word.Length > 0 && digit != digits) FlushWord();
                    if (word.Length == 0) digits = digit;
                    word.Append(c);
                }
                else FlushWord();
            }
            FlushWord();
            return list;
        }
    }

    /// <summary>
    /// Okapi BM25 over <see cref="MemoryTokens"/> (k1 = 1.2, b = 0.75). A tiny notebook would make every idf
    /// close to zero, so the corpus counts as at least <see cref="MinCorpus"/> documents. A document only counts as
    /// relevant when it shares something specific with the query (a bigram, an English word, or two different
    /// characters) and its score reaches <see cref="MinScore"/>; everything else scores 0.
    /// </summary>
    public sealed class Bm25Scorer : IMemoryScorer
    {
        public double K1 = 1.2, B = .75;
        public double MinScore = 2.5;
        public int MinCorpus = 8;
        public double WordWeight = 2;

        public double[] Score(MemoryQuery query, IList<string> documents)
        {
            int n = documents != null ? documents.Count : 0;
            var scores = new double[n];
            if (n == 0 || query == null || query.Empty) return scores;

            var weights = new Dictionary<string, double>(StringComparer.Ordinal);
            var kinds = new Dictionary<string, MemoryTokens.Kind>(StringComparer.Ordinal);
            foreach (var part in query.parts)
                foreach (var t in MemoryTokens.Tokenize(part.Key))
                {
                    weights.TryGetValue(t.Key, out double w);
                    // An English word carries about as much as a CJK bigram with its two characters.
                    double weight = part.Value * (t.Value == MemoryTokens.Kind.Word ? WordWeight : 1);
                    // A word repeated in the query counts a little more, never more than twice its weight.
                    weights[t.Key] = Math.Min(2 * weight, w + weight);
                    kinds[t.Key] = t.Value;
                }
            if (weights.Count == 0) return scores;

            var tf = new Dictionary<string, int>[n];
            var length = new int[n];
            var df = new Dictionary<string, int>(StringComparer.Ordinal);
            double total = 0;
            for (int i = 0; i < n; i++)
            {
                tf[i] = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var t in MemoryTokens.Tokenize(documents[i]))
                {
                    tf[i].TryGetValue(t.Key, out int c);
                    tf[i][t.Key] = c + 1;
                    length[i]++;
                }
                foreach (var key in tf[i].Keys) { df.TryGetValue(key, out int d); df[key] = d + 1; }
                total += length[i];
            }
            double avg = Math.Max(1, total / n);
            double corpus = Math.Max(n, MinCorpus);
            for (int i = 0; i < n; i++)
            {
                double s = 0;
                int strong = 0, weak = 0;
                foreach (var q in weights)
                {
                    if (!tf[i].TryGetValue(q.Key, out int f)) continue;
                    double idf = Math.Log(1 + (corpus - df[q.Key] + .5) / (df[q.Key] + .5));
                    s += q.Value * idf * f * (K1 + 1) / (f + K1 * (1 - B + B * length[i] / avg));
                    var kind = kinds[q.Key];
                    if (kind == MemoryTokens.Kind.Bigram || kind == MemoryTokens.Kind.Word) strong++;
                    else weak++;
                }
                bool specific = strong > 0 || weak >= 2;
                scores[i] = specific && s >= MinScore ? s : 0;
            }
            return scores;
        }
    }

    /// <summary>
    /// Ranks documents with a scorer, then adds small boosts for importance (1–3) and recency. Boosts only reorder
    /// documents the scorer already found relevant; they can never pull an irrelevant one in. Anything scoring under
    /// <see cref="RelativeFloor"/> of the best match is dropped as a weak tail.
    /// </summary>
    public static class MemoryRanker
    {
        public const double RelativeFloor = .3;
        public const double ImportanceBoost = .1, RecencyBoost = .15, RecencyDays = 14;

        /// <param name="importance">Per document 1–3, or null for all 1.</param>
        /// <param name="ageDays">Per document age in calendar days, or null for no recency boost (ties go to the later index).</param>
        public static List<int> Rank(IMemoryScorer scorer, MemoryQuery query, IList<string> documents, int count,
            IList<int> importance = null, IList<double> ageDays = null)
        {
            var result = new List<int>();
            if (scorer == null || documents == null || documents.Count == 0 || count <= 0) return result;
            var raw = scorer.Score(query, documents);
            double best = 0;
            foreach (var s in raw) best = Math.Max(best, s);
            if (best <= 0) return result;
            var final = new double[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] <= 0 || raw[i] < RelativeFloor * best) continue;
                int imp = importance != null && i < importance.Count ? Math.Max(1, Math.Min(3, importance[i])) : 1;
                double recency = ageDays != null && i < ageDays.Count ? 1 / (1 + Math.Max(0, ageDays[i]) / RecencyDays) : 0;
                final[i] = raw[i] * (1 + ImportanceBoost * (imp - 1)) * (1 + RecencyBoost * recency);
                result.Add(i);
            }
            result.Sort((a, b) => final[b] != final[a] ? final[b].CompareTo(final[a]) : b.CompareTo(a));
            if (result.Count > count) result.RemoveRange(count, result.Count - count);
            return result;
        }

        /// <summary>
        /// Up to <paramref name="count"/> lines for a prompt: the relevant ones first, then the most recent others to
        /// fill the slots. Returned in their original order, so the prompt still reads in time order.
        /// </summary>
        public static List<int> RelevantThenRecent(IMemoryScorer scorer, MemoryQuery query, IList<string> documents, int count)
        {
            var picked = new List<int>();
            if (documents == null || documents.Count == 0 || count <= 0) return picked;
            if (query != null && !query.Empty) picked.AddRange(Rank(scorer, query, documents, count));
            for (int i = documents.Count - 1; i >= 0 && picked.Count < count; i--) if (!picked.Contains(i)) picked.Add(i);
            picked.Sort();
            return picked;
        }
    }
}
