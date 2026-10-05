using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>Game dialogue progression, not a claim that the local GGUF's weights have been trained.</summary>
    public static class XgSpeechPolicy
    {
        public static int Stage(int stage) => Math.Max(1, Math.Min(6, stage));
        /// <summary>The first stage at which the AI may use YY bracket faces such as [微笑].</summary>
        public const int FirstFaceStage = 4;
        public static int ContextLimit(int stage)
        { switch (Stage(stage)) { case 1: case 2: return 1; case 3: return 2; case 4: case 5: return 14; default: return int.MaxValue; } }
        public static int TokenLimit(int stage)
        { return new[] { 2, 4, 24, 60, 100, 140 }[Stage(stage) - 1]; }

        public static string Constrain(string text, int stage, bool english = false)
        {
            text = (text ?? "").Trim(); stage = Stage(stage);
            // It only learns YY faces at stage 4; earlier codes are dropped before any truncation can split one.
            if (stage < FirstFaceStage) text = LingGuangV05.Core.Chat.YYFaces.StripFaces(text).Trim();
            bool yes = text.StartsWith("是", StringComparison.Ordinal) || text.StartsWith("yes", StringComparison.OrdinalIgnoreCase);
            if (stage == 1) return english ? (yes ? "Yes." : "No.") : (yes ? "是。" : "否。");
            if (stage == 2)
            {
                if (text.Contains("不确定") || text.IndexOf("unsure", StringComparison.OrdinalIgnoreCase) >= 0) return english ? "Unsure." : "不确定。";
                if (text.Contains("…") || text.Contains("...")) return "……";
                return english ? (yes ? "Yes." : "No.") : (yes ? "是。" : "否。");
            }
            if (stage == 3)
            {
                if (english)
                {
                    var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    return string.Join(" ", words, 0, Math.Min(6, words.Length));
                }
                var elements = StringInfo.GetTextElementEnumerator(text); var result = new StringBuilder(); int count = 0;
                while (count++ < 6 && elements.MoveNext()) result.Append(elements.GetTextElement());
                return result.Length == 0 ? "……" : result.ToString();
            }
            return text;
        }

        /// <summary>Only highlights an exact excerpt of the actual user message. Never renders raw model markup.</summary>
        public static string ReadFocus(string generated, string userText, out string reply)
        {
            reply = (generated ?? "").Trim();
            int newline = reply.IndexOf('\n');
            if (newline < 0) return "";
            string first = reply.Substring(0, newline).Trim();
            string focus = first.StartsWith("关注：", StringComparison.Ordinal) ? first.Substring(3).Trim()
                : first.StartsWith("focus:", StringComparison.OrdinalIgnoreCase) ? first.Substring(6).Trim() : "";
            if (focus.Length == 0) return "";
            reply = reply.Substring(newline + 1).Trim();
            if (reply.StartsWith("回复：", StringComparison.Ordinal)) reply = reply.Substring(3).Trim();
            else if (reply.StartsWith("reply:", StringComparison.OrdinalIgnoreCase)) reply = reply.Substring(6).Trim();
            if (focus.Length > 16 || string.IsNullOrEmpty(userText) || userText.IndexOf(focus, StringComparison.Ordinal) < 0) return "";
            return focus;
        }
        public static string Underline(string text, string focus)
        {
            text = text ?? "";
            int index = string.IsNullOrEmpty(focus) ? -1 : text.IndexOf(focus, StringComparison.Ordinal);
            if (index < 0) return SafeText(text);
            return SafeText(text.Substring(0, index)) + "<u>" + SafeText(focus) + "</u>" + SafeText(text.Substring(index + focus.Length));
        }
        static string SafeText(string text) => text.Replace("<", "‹").Replace(">", "›");

        // ───────────── stage 1–2 choices and their grammar (design v1.1 §11.5) ─────────────

        static readonly string[] YesNoZh = { "是", "否" }, YesNoEn = { "Yes", "No" };
        static readonly string[] PickZh = { "是", "否", "不确定", "……" }, PickEn = { "Yes", "No", "Unsure", "…" };
        static readonly string[] OptionTails = { "选哪个", "选哪一个", "哪个好", "哪个", "选一个", "挑一个", "选" };
        const int MaxOptions = 5, MaxOptionLength = 8;

        /// <summary>
        /// What it may answer at stages 1–2. Stage 1 is 是 / 否. Stage 2 picks one of the options the question offers
        /// ("猫还是狗", "红、黄、蓝选哪个", "tea or coffee"), or 是 / 否 / 不确定 / …… when it offers none. Empty from stage 3 on.
        /// </summary>
        public static List<string> Options(string question, int stage, bool english = false)
        {
            stage = Stage(stage);
            if (stage >= 3) return new List<string>();
            if (stage == 1) return new List<string>(english ? YesNoEn : YesNoZh);
            var offered = Offered(question ?? "", english);
            return offered.Count >= 2 ? offered : new List<string>(english ? PickEn : PickZh);
        }

        /// <summary>True when <paramref name="options"/> are the plain 是 / 否 (/ 不确定 / ……) set, not ones the question offered.</summary>
        static bool Plain(IList<string> options)
        {
            foreach (var o in options) if (Array.IndexOf(PickZh, o) < 0 && Array.IndexOf(PickEn, o) < 0) return false;
            return true;
        }

        static List<string> Offered(string question, bool english)
        {
            string q = question.Trim().TrimEnd('？', '?', '。', '.', '!', '！', '~', '～', ' ');
            var parts = new List<string>();
            if (english)
            {
                var split = q.Split(new[] { " or ", " OR ", " Or " }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2)
                {
                    // "do you like tea or coffee": the first option is as many words from the end as the second one has.
                    string last = Cut(split[split.Length - 1], ",;?.!");
                    int words = Math.Max(1, last.Split(' ').Length);
                    var head = split[0].Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    parts.Add(string.Join(" ", head, Math.Max(0, head.Length - words), Math.Min(words, head.Length)));
                    for (int i = 1; i < split.Length - 1; i++) parts.Add(split[i]);
                    parts.Add(last);
                }
            }
            else
            {
                var split = q.Split(new[] { "还是", "或者" }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2 && q.IndexOf("还是", StringComparison.Ordinal) != 0 && q.IndexOf("或者", StringComparison.Ordinal) != 0)
                {
                    // "你喜欢猫还是狗": the first option is as long as the last one ("猫"), cut from the end of the stem.
                    string last = Cut(split[split.Length - 1], "，,。？?！!");
                    parts.Add(Tail(split[0], last.Length));
                    for (int i = 1; i < split.Length - 1; i++) parts.Add(split[i]);
                    parts.Add(last);
                }
                else if (q.IndexOf('、') > 0 || q.IndexOf('/') > 0 || q.IndexOf('／') > 0)
                {
                    // "红、黄、蓝选哪个" / "选一个：红、黄、蓝".
                    int colon = Math.Max(q.LastIndexOf('：'), q.LastIndexOf(':'));
                    string list = colon >= 0 ? q.Substring(colon + 1) : q;
                    var items = list.Split(new[] { '、', '/', '／' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length >= 2)
                    {
                        string last = Cut(items[items.Length - 1], "，,。？?！!");
                        foreach (var t in OptionTails) if (last.Length > t.Length && last.EndsWith(t, StringComparison.Ordinal)) { last = last.Substring(0, last.Length - t.Length); break; }
                        parts.Add(Tail(Cut(items[0], ""), Math.Max(1, items[1].Trim().Length)));
                        for (int i = 1; i < items.Length - 1; i++) parts.Add(items[i]);
                        parts.Add(last);
                    }
                }
            }
            var options = new List<string>();
            foreach (var raw in parts)
            {
                string o = raw.Trim().Trim('“', '”', '"', '「', '」', '\'', '，', ',', ' ');
                if (!english && o.StartsWith("是", StringComparison.Ordinal) && o.Length > 1) o = o.Substring(1);
                if (o.Length == 0 || o.Length > (english ? 24 : MaxOptionLength) || options.Contains(o)) continue;
                options.Add(o);
                if (options.Count == MaxOptions) break;
            }
            return options;
        }

        static string Cut(string text, string stops)
        {
            int at = text.IndexOfAny(stops.ToCharArray());
            return (at >= 0 ? text.Substring(0, at) : text).Trim();
        }

        static string Tail(string text, int length)
        {
            text = text.Trim();
            int start = Math.Max(text.LastIndexOfAny("，,：:".ToCharArray()) + 1, text.Length - Math.Max(1, Math.Min(MaxOptionLength, length)));
            return text.Substring(Math.Max(0, start));
        }

        /// <summary>
        /// GBNF grammar for llama-server so that stages 1–2 can only produce one of <see cref="Options"/>
        /// (stage 1: <c>root ::= "是" | "否"</c>). Empty from stage 3 on, where the form is checked after the reply.
        /// </summary>
        public static string Grammar(string question, int stage, bool english = false)
        {
            var options = Options(question, stage, english);
            if (options.Count == 0) return "";
            var sb = new StringBuilder("root ::= ");
            for (int i = 0; i < options.Count; i++) sb.Append(i > 0 ? " | " : "").Append(GbnfLiteral(options[i]));
            return sb.ToString();
        }

        static string GbnfLiteral(string text)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c >= ' ') sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        /// <summary>
        /// The stage-2 form of a reply: the option it names (begins with, else contains), written as a reply ("猫。").
        /// With the plain options this is <see cref="Constrain"/>; with offered options a reply that names none is a shrug.
        /// </summary>
        public static string Choose(string text, IList<string> options, bool english = false)
        {
            text = (text ?? "").Trim();
            if (options == null || options.Count == 0 || Plain(options)) return Constrain(text, 2, english);
            var longest = new List<string>(options);
            longest.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (var o in longest) if (text.StartsWith(o, StringComparison.OrdinalIgnoreCase)) return Answer(o, english);
            foreach (var o in longest) if (text.IndexOf(o, StringComparison.OrdinalIgnoreCase) >= 0) return Answer(o, english);
            return english ? "…" : "……";
        }

        /// <summary>An option written as a reply: "猫" → "猫。", "tea" → "Tea.". A shrug or punctuated option stays as it is.</summary>
        public static string Answer(string option, bool english = false)
        {
            option = (option ?? "").Trim();
            if (option.Length == 0) return english ? "…" : "……";
            char last = option[option.Length - 1];
            if (char.IsPunctuation(last) || last == '…') return option;
            if (english) return char.ToUpperInvariant(option[0]) + option.Substring(1) + ".";
            return option + "。";
        }

        // ───────────── not saying the same thing twice ─────────────

        /// <summary>Letters and digits only, lower case: what is left to compare once punctuation and spacing are gone.</summary>
        public static string Normalize(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text ?? "") if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        /// <summary>Similarity above which two replies count as the same thing said again.</summary>
        public const double SameThreshold = .7;

        /// <summary>
        /// Two replies are the same line for a listener: equal once punctuation is gone, the same characters in a
        /// shuffled order (the brain gate's word-order slip), one inside the other, or sharing most of their character pairs.
        /// </summary>
        public static bool Similar(string a, string b)
        {
            string x = Normalize(a), y = Normalize(b);
            if (x == y) return true;
            if (x.Length == 0 || y.Length == 0) return false;
            if (x.Length == y.Length && x.Length >= 2)
            {
                var p = x.ToCharArray(); var q = y.ToCharArray();
                Array.Sort(p); Array.Sort(q);
                if (new string(p) == new string(q)) return true;
            }
            // One line inside the other ("又是你呀" after "又是你呀，真的吗") is the same thing said again.
            string shorter = x.Length <= y.Length ? x : y, longer = x.Length <= y.Length ? y : x;
            if (shorter.Length >= 3 && longer.Contains(shorter)) return true;
            if (x.Length < 4 || y.Length < 4) return false;
            return PairOverlap(x, y) >= SameThreshold;
        }

        /// <summary>Dice coefficient over adjacent character pairs (0 = nothing shared, 1 = the same pairs).</summary>
        public static double PairOverlap(string x, string y)
        {
            var pairs = new Dictionary<string, int>();
            for (int i = 0; i + 1 < x.Length; i++) { string k = x.Substring(i, 2); pairs.TryGetValue(k, out int n); pairs[k] = n + 1; }
            int shared = 0;
            for (int i = 0; i + 1 < y.Length; i++)
            {
                string k = y.Substring(i, 2);
                if (pairs.TryGetValue(k, out int n) && n > 0) { shared++; pairs[k] = n - 1; }
            }
            int total = (x.Length - 1) + (y.Length - 1);
            return total == 0 ? 0 : 2.0 * shared / total;
        }

        // ───────────── sampler settings per stage ─────────────

        /// <summary>
        /// Sampler settings for one chat request at this stage. Stages 1–2 are bound by <paramref name="grammar"/> and use
        /// no penalties (repeating 是 is an answer, not a tic). From stage 3 on, presence / frequency / repeat penalties
        /// look back over a long window so that its earlier lines in the context count, and from stage 4 the DRY
        /// sampler stops it from copying a run of words it already said.
        /// </summary>
        public static XgSampling Sampling(int stage, string grammar = "")
        {
            stage = Stage(stage);
            if (stage <= 2) return new XgSampling { grammar = grammar ?? "", topP = 1, topK = 0, presencePenalty = 0, frequencyPenalty = 0, repeatPenalty = 1, repeatLastN = 0 };
            if (stage == 3) return new XgSampling { topP = .95f, topK = 40, presencePenalty = .8f, frequencyPenalty = .3f, repeatPenalty = 1.1f, repeatLastN = 256 };
            return new XgSampling { topP = .9f, topK = 40, presencePenalty = 1f, frequencyPenalty = .3f, repeatPenalty = 1.05f, repeatLastN = 512, dryMultiplier = .6f };
        }

        /// <summary>A JSON string literal (quotes included).</summary>
        public static string Json(string text)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in text ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }
    }

    /// <summary>
    /// Sampler settings for one llama-server chat request. llama-server reads these from the OpenAI-style body next to
    /// the standard fields; an older server simply ignores the ones it does not know.
    /// </summary>
    public sealed class XgSampling
    {
        /// <summary>GBNF grammar the reply must match (empty = free text).</summary>
        public string grammar = "";
        public float topP = .8f, presencePenalty = 1.2f, frequencyPenalty, repeatPenalty = 1, dryMultiplier;
        public int topK = 20, repeatLastN = 64;

        /// <summary>The fields as JSON, each with a leading comma, to append inside the request object.</summary>
        public string JsonFields()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(",\"top_p\":").Append(topP.ToString("0.00", c)).Append(",\"top_k\":").Append(topK)
              .Append(",\"presence_penalty\":").Append(presencePenalty.ToString("0.00", c))
              .Append(",\"frequency_penalty\":").Append(frequencyPenalty.ToString("0.00", c))
              .Append(",\"repeat_penalty\":").Append(repeatPenalty.ToString("0.00", c))
              .Append(",\"repeat_last_n\":").Append(repeatLastN);
            if (dryMultiplier > 0)
                sb.Append(",\"dry_multiplier\":").Append(dryMultiplier.ToString("0.00", c)).Append(",\"dry_base\":1.75,\"dry_allowed_length\":3,\"dry_penalty_last_n\":-1");
            if (!string.IsNullOrEmpty(grammar)) sb.Append(",\"grammar\":").Append(XgSpeechPolicy.Json(grammar));
            return sb.ToString();
        }
    }
}
