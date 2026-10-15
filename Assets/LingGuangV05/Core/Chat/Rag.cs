using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace LingGuangV05.Core.Chat
{
    /// <summary>
    /// One fact of a knowledge file (Resources/LingGuang/Rag/*.json): 晴雯's own life or the 2016 era. It holds from
    /// <see cref="from"/> to <see cref="until"/> (in-game dates, "yyyy-MM-dd", empty = always), so a character never
    /// knows tomorrow's news or still talks about the summer holiday in November.
    /// </summary>
    [DataContract]
    public sealed class RagEntry
    {
        [DataMember] public string id = "";
        [DataMember] public string topic = "";
        [DataMember] public string zh = "";
        [DataMember] public string en = "";
        [DataMember] public string from = "";
        [DataMember] public string until = "";
        [DataMember] public int importance = 1;

        public string Text(bool english) => english && !string.IsNullOrWhiteSpace(en) ? en : zh;

        /// <summary>True on <paramref name="day"/> (a date without time); malformed dates count as unset.</summary>
        public bool HoldsOn(DateTime day)
        {
            var d = day.Date;
            if (TryDate(from, out var start) && d < start) return false;
            if (TryDate(until, out var end) && d > end) return false;
            return true;
        }

        static bool TryDate(string text, out DateTime date) =>
            DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    /// <summary>A knowledge file: its entries, and the ones that hold on a given day.</summary>
    public sealed class RagBook
    {
        [DataContract] sealed class File { [DataMember] public List<RagEntry> entries; }

        public readonly List<RagEntry> entries = new List<RagEntry>();

        /// <summary>Reads the JSON of a knowledge file; entries without Chinese text are skipped. Malformed JSON gives an empty book.</summary>
        public static RagBook Parse(string json)
        {
            var book = new RagBook();
            if (string.IsNullOrWhiteSpace(json)) return book;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var file = (File)new DataContractJsonSerializer(typeof(File)).ReadObject(stream);
                    if (file?.entries != null)
                        foreach (var e in file.entries)
                            if (e != null && !string.IsNullOrWhiteSpace(e.zh)) { e.importance = Math.Max(1, Math.Min(3, e.importance)); book.entries.Add(e); }
                }
            }
            catch (Exception e) when (e is SerializationException || e is FormatException || e is ArgumentException || e is System.Xml.XmlException) { }
            return book;
        }

        public List<RagEntry> On(DateTime day)
        {
            var list = new List<RagEntry>();
            foreach (var e in entries) if (e.HoldsOn(day)) list.Add(e);
            return list;
        }
    }

    /// <summary>
    /// Embeddings for retrieval, filled by the game's local embedding model (LocalEmbedder). Both calls only read a
    /// cache and never wait: null means "not embedded yet" (a document missing here is queued for embedding), and the
    /// scorer then falls back to BM25 for it.
    /// </summary>
    public interface IVectorSource
    {
        /// <summary>The vector of a search text (embedded with the retrieval instruction), or null.</summary>
        float[] Query(string text);
        /// <summary>The vector of a stored text, or null (and it is queued).</summary>
        float[] Document(string text);
    }

    /// <summary>
    /// BM25 plus embedding similarity. A document is relevant when its meaning is close (cosine ≥
    /// <see cref="SemanticFloor"/>), so 「期末准备得怎么样」 finds 「11 月 2 日考概率论」, or when it shares words with the
    /// query and is not unrelated in meaning (cosine ≥ <see cref="LexicalFloor"/>: 「女排那场你看了吗」 shares 「你看了」
    /// with 「迪士尼开园了，你看了好多照片」 at cosine .28). The score is mostly the cosine, with a saturating bonus for
    /// shared words. A document not embedded yet is judged by BM25 alone; without vectors (no embedding model,
    /// <see cref="Semantic"/> off) it is exactly BM25.
    /// </summary>
    public sealed class HybridScorer : IMemoryScorer
    {
        /// <summary>The vectors to use; null takes <see cref="RagVectors.Source"/>.</summary>
        public IVectorSource Vectors;
        /// <summary>Off: words only (the lab AI before it has researched 词向量).</summary>
        public bool Semantic = true;
        public readonly Bm25Scorer Lexical = new Bm25Scorer();
        /// <summary>Cosine from which a document counts as related (Qwen3-Embedding-0.6B: related ≈ .48–.62, unrelated ≈ .25–.42).</summary>
        public double SemanticFloor = .46;
        /// <summary>Cosine a document sharing words still needs (measured: word-only noise ≤ .32, real matches ≥ .43).</summary>
        public double LexicalFloor = .36;
        public double LexicalWeight = .3, LexicalHalf = 5;

        public double[] Score(MemoryQuery query, IList<string> documents)
        {
            var lexical = Lexical.Score(query, documents);
            var source = Semantic ? Vectors ?? RagVectors.Source : null;
            if (source == null || query == null || query.Empty || documents == null) return lexical;
            var queries = new List<KeyValuePair<float[], double>>();
            foreach (var part in query.parts)
            {
                var v = source.Query(part.Key);
                if (v != null) queries.Add(new KeyValuePair<float[], double>(v, part.Value));
            }
            if (queries.Count == 0) return lexical;
            var scores = new double[documents.Count];
            for (int i = 0; i < documents.Count; i++)
            {
                var d = source.Document(documents[i]);
                double semantic = 0, weight = 0;
                if (d != null)
                {
                    foreach (var q in queries) { semantic += q.Value * Cosine(q.Key, d); weight += q.Value; }
                    semantic /= weight;
                }
                double lex = lexical[i];
                bool relevant = d == null ? lex > 0 : semantic >= SemanticFloor || lex > 0 && semantic >= LexicalFloor;
                scores[i] = relevant ? Math.Max(0, semantic) + LexicalWeight * lex / (lex + LexicalHalf) + 1e-6 : 0;
            }
            return scores;
        }

        public static double Cosine(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0) return 0;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
            return na <= 0 || nb <= 0 ? 0 : dot / Math.Sqrt(na * nb);
        }
    }

    /// <summary>The game's embedding cache, set by LocalEmbedder while its model runs (null otherwise).</summary>
    public static class RagVectors
    {
        public static IVectorSource Source;
    }

    /// <summary>Retrieval over knowledge files and old conversations, shared by 晴雯 and the lab AI.</summary>
    public static class Rag
    {
        /// <summary>The knowledge files, loaded by the game (RagLibrary); empty until then and in tests.</summary>
        public static RagBook Qingwen = new RagBook(), Era = new RagBook();

        /// <summary>Up to <paramref name="count"/> entries holding on <paramref name="day"/> that are relevant to the query, best first.</summary>
        public static List<RagEntry> Lookup(IMemoryScorer scorer, MemoryQuery query, RagBook book, DateTime day, bool english, int count)
        {
            var found = new List<RagEntry>();
            if (book == null || query == null || query.Empty || count <= 0) return found;
            var active = book.On(day);
            var texts = new List<string>(active.Count);
            var importance = new List<int>(active.Count);
            foreach (var e in active) { texts.Add(e.Text(english)); importance.Add(e.importance); }
            foreach (int i in MemoryRanker.Rank(scorer, query, texts, count, importance)) found.Add(active[i]);
            return found;
        }

        /// <summary>
        /// Earlier exchanges of a conversation that the latest lines recall: each candidate is one line with the line
        /// after it (so a question comes with its answer). <paramref name="end"/> excludes the recent window that is
        /// already in the prompt. Returns the start indices of the picked exchanges, in time order.
        /// </summary>
        public static List<int> Episodes(IMemoryScorer scorer, MemoryQuery query, IList<string> lines, int end, int count)
        {
            var picked = new List<int>();
            if (lines == null || query == null || query.Empty || count <= 0) return picked;
            end = Math.Min(end, lines.Count);
            var docs = new List<string>();
            for (int i = 0; i < end; i++) docs.Add(i + 1 < end ? lines[i] + "\n" + lines[i + 1] : lines[i]);
            foreach (int i in MemoryRanker.Rank(scorer, query, docs, count * 2))
            {
                // Two picked neighbours would show the same line twice.
                bool overlaps = false;
                foreach (int p in picked) if (Math.Abs(p - i) < 2) overlaps = true;
                if (!overlaps) picked.Add(i);
                if (picked.Count >= count) break;
            }
            picked.Sort();
            return picked;
        }

        /// <summary>A bulleted block for a prompt's per-turn state ("" when there is nothing).</summary>
        public static string Block(string heading, IList<string> lines)
        {
            if (lines == null || lines.Count == 0) return "";
            var sb = new StringBuilder(heading).Append('\n');
            foreach (var l in lines) sb.Append("- ").Append(l).Append('\n');
            return sb.ToString();
        }
    }
}
