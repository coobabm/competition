using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One candidate for the next character (or English word) while it types, with its probability.</summary>
    public sealed class XgCandidate
    {
        public string token = "";
        public double p;
        /// <summary>The one it actually typed (the reply's own character).</summary>
        public bool chosen;
    }

    /// <summary>One typed character of a reply and the 3–5 candidates it weighed for it, most likely first.</summary>
    public sealed class XgCandidateStep
    {
        public string token = "";
        /// <summary>Where the token sits in the reply.</summary>
        public int start, length;
        public readonly List<XgCandidate> options = new List<XgCandidate>();
        /// <summary>The probability it gave the character it typed.</summary>
        public double ChosenP { get { foreach (var o in options) if (o.chosen) return o.p; return 0; } }
    }

    /// <summary>A word of a reply (start and length in the reply's text).</summary>
    public struct XgWordSpan
    {
        public int start, length;
        public XgWordSpan(int start, int length) { this.start = start; this.length = length; }
        public bool Contains(int index) => index >= start && index < start + length;
    }

    /// <summary>One attention line: from a word of its reply back to a span of one of your earlier lines.</summary>
    public sealed class XgAttentionLink
    {
        /// <summary>Index into the earlier lines it was given (oldest first).</summary>
        public int message;
        public int start, length;
        public string word = "";
        /// <summary>Share of its attention (the links of one word sum to 1).</summary>
        public double weight;
        /// <summary>How strong the match was before sharing out (an exact repeat ≈ 1, one shared character ≈ .15); lines fade with it.</summary>
        public double score;
        /// <summary>A note in its memory book holds this span too: it remembered it, not only matched it.</summary>
        public bool remembered;
    }

    /// <summary>
    /// 「它在学」 visuals for the chat (它在学_可视化借鉴设计 §2.2, after Transformer Explainer): the next-character
    /// candidate bars shown while it types, and the attention lines from a word of its reply back to your earlier lines.
    /// Neither comes from a real model. The candidates follow its mastery: the right character gets about the stage's
    /// confidence and the rest goes to the wrong answers of its current habit (the chapter's <see cref="XgSim.ExamWrongAnswer"/>
    /// habit: 是 / 否, an option letter, the right characters in a heap, a lost start). Attention follows shared
    /// characters and bigrams, recency and the memory book. Everything is deterministic for a given reply.
    /// </summary>
    public static class XgCandidates
    {
        /// <summary>Confidence in the right character at each stage (index = stage, 1–6) before the progress bonus.</summary>
        public static readonly double[] StageConfidence = { .2, .24, .32, .46, .64, .84, .92 };
        /// <summary>Stage 5 and 6 never put the right character under this.</summary>
        public const double LateFloor = .8;
        public const int MaxSteps = 32;

        static readonly string[] YesNoZh = { "是", "否", "嗯", "。" }, YesNoEn = { "Yes", "No", "Mm", "." };
        static readonly string[] LettersZh = { "A", "B", "C", "D", "选" }, LettersEn = { "A", "B", "C", "D", "Pick" };
        static readonly string[] LostZh = { "谁", "……", "你", "说" }, LostEn = { "Who", "…", "you", "said" };
        static readonly string[] CommonZh = { "的", "了", "我", "你", "是", "在", "不", "有", "就", "这" };
        static readonly string[] CommonEn = { "the", "a", "I", "you", "is", "to", "it", "and", "of", "that" };
        const string StopChars = "的了是我你他她它在有和就不都也很吗呢吧啊嗯哦呀么这那个一";

        /// <summary>
        /// The probability of the right character: the stage's confidence, pushed up to 60% of the way to the next
        /// stage's as <paramref name="readiness"/> (the next ability's bars, 0–1) fills.
        /// </summary>
        public static double Confidence(int stage, double readiness)
        {
            stage = Math.Max(1, Math.Min(6, stage));
            double r = Math.Max(0, Math.Min(1, double.IsNaN(readiness) ? 0 : readiness));
            double next = StageConfidence[Math.Min(6, stage + 1)];
            return StageConfidence[stage] + (next - StageConfidence[stage]) * .6 * r;
        }

        // ───────────── tokens ─────────────

        public static bool IsCjk(char ch) => ch >= 0x3400 && ch <= 0x9FFF || ch >= 0xF900 && ch <= 0xFAFF;
        static bool IsWordChar(char ch) => char.IsLetterOrDigit(ch) && !IsCjk(ch) || ch == '\'';
        public static bool IsPunct(char ch) => char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch) || "。，？！、…「」“”‘’（）《》：；～".IndexOf(ch) >= 0;

        /// <summary>The reply cut into tokens: one per Chinese character, one per English word; punctuation and spaces dropped.</summary>
        public static List<XgWordSpan> Tokens(string text)
        {
            var list = new List<XgWordSpan>();
            if (string.IsNullOrEmpty(text)) return list;
            for (int i = 0; i < text.Length;)
            {
                char ch = text[i];
                if (IsWordChar(ch))
                {
                    int s = i;
                    while (i < text.Length && IsWordChar(text[i])) i++;
                    list.Add(new XgWordSpan(s, i - s));
                    continue;
                }
                if (char.IsHighSurrogate(ch) && i + 1 < text.Length) { i += 2; continue; } // emoji and the like: not a character it picks
                if (!IsPunct(ch)) list.Add(new XgWordSpan(i, 1));
                i++;
            }
            return list;
        }

        /// <summary>A string hash that is the same under Unity's Mono and .NET (string.GetHashCode is not).</summary>
        public static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s ?? "") { h ^= c; h *= 16777619; }
                return (int)(h & 0x7FFFFFFF);
            }
        }

        // ───────────── candidates ─────────────

        /// <summary>
        /// The candidates for each typed token of <paramref name="reply"/> (at most <see cref="MaxSteps"/>). Each step's
        /// probabilities sum to 1; the reply's own token is the chosen one. Early stages are scattered (and the chosen one
        /// is often not the tallest bar); from stage 5 the chosen one has at least <see cref="LateFloor"/>.
        /// </summary>
        public static List<XgCandidateStep> ForReply(string reply, int stage, double readiness, int maxSteps = MaxSteps)
        {
            var steps = new List<XgCandidateStep>();
            var tokens = Tokens(reply);
            if (tokens.Count == 0) return steps;
            stage = Math.Max(1, Math.Min(6, stage));
            bool english = true;
            foreach (char ch in reply) if (IsCjk(ch)) { english = false; break; }
            double baseP = Confidence(stage, readiness);
            double jitter = stage <= 2 ? .1 : stage <= 4 ? .07 : .04;
            int count = stage <= 2 ? 5 : stage <= 4 ? 4 : 3;
            var r = new XgTinyRandom(StableHash(reply) ^ stage * 7919);
            for (int t = 0; t < tokens.Count && steps.Count < maxSteps; t++)
            {
                var span = tokens[t];
                string right = reply.Substring(span.start, span.length);
                double p = baseP + r.Range(-jitter, jitter);
                p = Math.Max(stage >= 5 ? LateFloor : .08, Math.Min(.97, p));
                var wrong = WrongTokens(reply, tokens, t, right, stage, english, count - 1, r);
                var step = new XgCandidateStep { token = right, start = span.start, length = span.length };
                step.options.Add(new XgCandidate { token = right, p = p, chosen = true });
                // The rest of the probability goes to the habit's wrong answers, unevenly.
                var w = new double[wrong.Count];
                double sum = 0;
                for (int i = 0; i < w.Length; i++) { w[i] = r.Range(.25, 1) / (1 + i * .35); sum += w[i]; }
                for (int i = 0; i < w.Length; i++) step.options.Add(new XgCandidate { token = wrong[i], p = (1 - p) * w[i] / sum });
                if (wrong.Count == 0) step.options[0].p = 1;
                step.options.Sort((a, b) => b.p.CompareTo(a.p));
                steps.Add(step);
            }
            return steps;
        }

        /// <summary>The habit's wrong answers for token <paramref name="t"/>, distinct and never the right one.</summary>
        static List<string> WrongTokens(string reply, List<XgWordSpan> tokens, int t, string right, int stage, bool english, int want, XgTinyRandom r)
        {
            var pool = new List<string>();
            switch (stage)
            {
                case 1: pool.AddRange(english ? YesNoEn : YesNoZh); break;               // it only knows 是 / 否
                case 2: pool.AddRange(english ? LettersEn : LettersZh); break;           // it picks an option letter
                case 3:                                                                  // the right characters, in a heap
                    for (int k = t + 1; k < tokens.Count && pool.Count < 6; k++) pool.Add(Text(reply, tokens[k]));
                    for (int k = 0; k < t && pool.Count < 8; k++) pool.Add(Text(reply, tokens[k]));
                    break;
                case 4:                                                                  // the start gets lost
                    pool.AddRange(english ? LostEn : LostZh);
                    for (int k = Math.Max(t + 1, tokens.Count / 2); k < tokens.Count && pool.Count < 8; k++) pool.Add(Text(reply, tokens[k]));
                    break;
                default:                                                                 // near misses: jumping ahead, repeating
                    if (t + 1 < tokens.Count) pool.Add(Text(reply, tokens[t + 1]));
                    if (t > 0) pool.Add(Text(reply, tokens[t - 1]));
                    break;
            }
            // Shuffle the habit's pool a little so the same habit does not always rank the same wrong answer first.
            for (int i = pool.Count - 1; i > 0; i--) { int j = r.Below(i + 1); if (r.NextDouble() < .5) { var x = pool[i]; pool[i] = pool[j]; pool[j] = x; } }
            var common = english ? CommonEn : CommonZh;
            int c0 = r.Below(common.Length);
            for (int i = 0; i < common.Length; i++) pool.Add(common[(c0 + i) % common.Length]);
            var list = new List<string>(want);
            foreach (var s in pool)
            {
                if (list.Count >= want) break;
                if (string.IsNullOrEmpty(s) || string.Equals(s, right, StringComparison.OrdinalIgnoreCase) || list.Contains(s)) continue;
                list.Add(s);
            }
            return list;
        }

        static string Text(string s, XgWordSpan w) => s.Substring(w.start, w.length);

        // ───────────── words and attention ─────────────

        /// <summary>
        /// Its reply cut into words for hovering: English by spaces; Chinese by the longest pieces (2–8 characters) that
        /// also appear in your earlier lines or its notes, the rest in pairs. Punctuation is no word.
        /// </summary>
        public static List<XgWordSpan> Words(string reply, IList<string> earlier, IList<string> notes = null)
        {
            var words = new List<XgWordSpan>();
            if (string.IsNullOrEmpty(reply)) return words;
            for (int i = 0; i < reply.Length;)
            {
                char ch = reply[i];
                if (IsWordChar(ch))
                {
                    int s = i;
                    while (i < reply.Length && IsWordChar(reply[i])) i++;
                    words.Add(new XgWordSpan(s, i - s));
                    continue;
                }
                if (!IsCjk(ch)) { i++; continue; }
                int run = i;
                while (run < reply.Length && IsCjk(reply[run])) run++;
                int len = 0;
                for (int l = Math.Min(8, run - i); l >= 2 && len == 0; l--)
                    if (Seen(reply.Substring(i, l), earlier) || Seen(reply.Substring(i, l), notes)) len = l;
                if (len == 0) len = Math.Min(2, run - i);
                words.Add(new XgWordSpan(i, len));
                i += len;
            }
            return words;
        }

        static bool Seen(string piece, IList<string> lines)
        {
            if (lines == null) return false;
            foreach (var l in lines) if (!string.IsNullOrEmpty(l) && l.IndexOf(piece, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>
        /// Where its word looks back to: spans of <paramref name="earlier"/> (your lines, oldest first) that share it whole,
        /// share a bigram or more of it, or (when nothing longer matches in that line) share one real character. Newer
        /// lines count more (×0.85 per line back) and a span that a note in its memory book also holds counts more again.
        /// At most <paramref name="maxLinks"/> links; their weights sum to 1; none when nothing is shared.
        /// </summary>
        public static List<XgAttentionLink> Attend(string word, IList<string> earlier, IList<string> notes = null, int maxLinks = 4)
        {
            var found = new List<XgAttentionLink>();
            if (string.IsNullOrWhiteSpace(word) || earlier == null) return found;
            word = word.Trim();
            bool latin = !HasCjk(word);
            for (int k = 0; k < earlier.Count; k++)
            {
                string line = earlier[k];
                if (string.IsNullOrEmpty(line)) continue;
                double recency = Math.Pow(.85, earlier.Count - 1 - k);
                var spans = latin ? LatinSpans(word, line) : CjkSpans(word, line);
                foreach (var (start, length, score) in spans)
                {
                    string piece = line.Substring(start, length);
                    bool remembered = Seen(piece, notes) && (length >= 2 || latin);
                    found.Add(new XgAttentionLink
                    {
                        message = k, start = start, length = length, word = piece, remembered = remembered,
                        score = Math.Min(1.5, (score + (remembered ? .35 * Math.Min(1, length / (double)word.Length) : 0)) * recency),
                    });
                }
            }
            found.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : b.message != a.message ? b.message.CompareTo(a.message) : a.start.CompareTo(b.start));
            if (found.Count > maxLinks) found.RemoveRange(maxLinks, found.Count - maxLinks);
            double sum = 0;
            foreach (var l in found) sum += l.score;
            foreach (var l in found) l.weight = sum > 0 ? l.score / sum : 0;
            return found;
        }

        static bool HasCjk(string s) { foreach (char c in s) if (IsCjk(c)) return true; return false; }

        /// <summary>The whole word (1.0); else maximal shared pieces of 2+ characters (0.45–0.65); else one shared real character (0.15).</summary>
        static List<(int, int, double)> CjkSpans(string word, string line)
        {
            var list = new List<(int, int, double)>();
            int at = line.IndexOf(word, StringComparison.Ordinal);
            if (at >= 0) { list.Add((at, word.Length, 1.0)); return list; }
            var covered = new bool[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                if (covered[i] || !IsCjk(line[i])) continue;
                int best = 0;
                for (int j = 0; j < word.Length; j++)
                {
                    int l = 0;
                    while (i + l < line.Length && j + l < word.Length && line[i + l] == word[j + l]) l++;
                    best = Math.Max(best, l);
                }
                if (best < 2) continue;
                for (int c = i; c < i + best; c++) covered[c] = true;
                list.Add((i, best, .45 + .2 * best / (double)word.Length));
            }
            if (list.Count > 0) return list;
            foreach (char ch in word)
            {
                if (!IsCjk(ch) || StopChars.IndexOf(ch) >= 0) continue;
                int i = line.IndexOf(ch);
                if (i >= 0) { list.Add((i, 1, .15)); break; }
            }
            return list;
        }

        /// <summary>The same word, any case (1.0); else a word sharing its first four letters (0.4).</summary>
        static List<(int, int, double)> LatinSpans(string word, string line)
        {
            var list = new List<(int, int, double)>();
            string w = word.ToLowerInvariant();
            for (int i = 0; i < line.Length;)
            {
                if (!IsWordChar(line[i])) { i++; continue; }
                int s = i;
                while (i < line.Length && IsWordChar(line[i])) i++;
                string t = line.Substring(s, i - s).ToLowerInvariant();
                if (t == w) list.Add((s, i - s, 1.0));
                else if (w.Length >= 4 && t.Length >= 4 && t.Substring(0, 4) == w.Substring(0, 4)) list.Add((s, i - s, .4));
            }
            return list;
        }
    }

    public sealed partial class XgSim
    {
        /// <summary>The attention lines open with ability 5 (「它记住你说过的话」).</summary>
        public const int AttentionStage = 5;

        /// <summary>How far the next ability's two bars are filled (the smaller one, 0–1); 1 once every ability is in.</summary>
        public double CandidateReadiness => NextAbility <= 0 ? 1 : Math.Max(0, Math.Min(1, Math.Min(ExamParamsReady, ExamSamplesReady)));

        /// <summary>The candidate bars for one of its replies (deterministic per reply and stage).</summary>
        public List<XgCandidateStep> ReplyCandidates(string reply) => XgCandidates.ForReply(reply, S.stage, CandidateReadiness);

        public bool AttentionOpen => S.stage >= AttentionStage;

        /// <summary>The texts of its memory book's notes (what the attention lines treat as remembered).</summary>
        public List<string> MemoryNoteTexts()
        {
            var list = new List<string>();
            if (S.memoryBook != null) foreach (var e in S.memoryBook) if (!string.IsNullOrEmpty(e.text)) list.Add(e.text);
            return list;
        }

        /// <summary>Its reply's hoverable words, given the player's earlier lines (oldest first).</summary>
        public List<XgWordSpan> ReplyWords(string reply, IList<string> earlier) => XgCandidates.Words(reply, earlier, MemoryNoteTexts());

        /// <summary>The attention lines from one word of its reply back to the player's earlier lines (oldest first); none before stage 5.</summary>
        public List<XgAttentionLink> ReplyAttention(string word, IList<string> earlier)
            => AttentionOpen ? XgCandidates.Attend(word, earlier, MemoryNoteTexts()) : new List<XgAttentionLink>();
    }
}
