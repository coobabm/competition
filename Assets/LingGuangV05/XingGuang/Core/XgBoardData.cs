using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Turns every dataset into concept-board cards (design v1.1 §4.1): each card carries a few elements, and the
    /// wiring decides how they are read. Deterministic per (dataset, seed, level, date), so training pools and test
    /// sets can be regenerated instead of saved.
    /// </summary>
    public static class XgBoardData
    {
        public const int TestSize = 120;
        public const int PoolLimit = 4096;

        /// <summary>Board region per dataset: 视觉 / 序列 / 逻辑 (语气 comes from tone elements later).</summary>
        public static string Region(string dataset)
        {
            switch (dataset)
            {
                case "mnist": case "cifar": case "imagenet": case "meme": case "go": return "vision";
                case "logic": case "xor": return "logic";
                default: return "sequence";
            }
        }

        /// <summary>What a generated card is for: training pool, diagnostic (shown) set, or the independent wall exam.</summary>
        public enum Use { Train = 0, Diagnostic = 1, Exam = 2 }

        public static int Seed(string dataset, int index, bool test, int salt = 0) => Seed(dataset, index, test ? Use.Diagnostic : Use.Train, salt);

        /// <summary>Card seed; <paramref name="salt"/> is per save, so every save meets different cards of the same kind.</summary>
        public static int Seed(string dataset, int index, Use use, int salt)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in dataset) h = h * 31 + c;
                return (h * 7919 + index * 104729 + (int)use * 1000003 + salt * 15485863) & 0x7fffffff;
            }
        }

        public static List<XgBoardCard> TestSet(string dataset, int level, int today, int salt = 0, Use use = Use.Diagnostic)
        {
            var list = new List<XgBoardCard>(TestSize);
            for (int i = 0; i < TestSize; i++) list.Add(Make(dataset, Seed(dataset, i, use, salt), level, today));
            return list;
        }

        /// <summary>A labelled example of the dataset, generated from its seed.</summary>
        public static XgBoardCard Make(string dataset, int seed, int level, int today)
        {
            var r = new Random(seed);
            var card = new XgCard { dataset = dataset, seed = seed, level = level, truth = r.Next(2) == 0 };
            switch (dataset)
            {
                case "xor": return XgBoardTasks.Xor(seed);
                case "parallel": return Parallel(seed, r, 14);
                case "longtext": return LongText(seed, r, 11 + level * 3, false);
                case "crosssentence": return LongText(seed, r, 13 + level * 3, true);
                case "translate": return Translation(seed, r, 3 + level * 2);
                case "mnist":
                    card.digit = r.Next(10);
                    card.asked = card.truth ? card.digit : (card.digit + 1 + r.Next(9)) % 10;
                    break;
                case "poems": case "news":
                    var lines = XgCatalog.PoemLines;
                    card.line = lines[r.Next(lines.Length)];
                    card.shown = 1 + r.Next(card.line.Length - 1);
                    card.askedChar = card.truth ? card.line.Substring(card.shown, 1) : lines[r.Next(lines.Length)].Substring(0, 1);
                    card.truth = card.askedChar == card.line.Substring(card.shown, 1);
                    break;
                case "logic":
                    var q = XgLogic.Generate(seed, Math.Max(1, level));
                    card.truth = q.truth; card.category = q.category; card.question = q.text;
                    break;
                case "cifar": case "imagenet": XgVisual.Captcha(card, r, Math.Max(1, level)); break;
                case "meme": XgVisual.Meme(card, r, Math.Max(1, level), today); break;
                case "go": XgVisual.Go(card, r, Math.Max(1, level)); break;
                default:
                    var p = XgMemes.Pick(dataset, r, Math.Max(1, level), today, "");
                    if (p != null) { card.truth = p.yes; card.question = p.text; card.trick = p.trick; }
                    break;
            }
            return FromCard(card);
        }

        /// <summary>The elements of a label card as the brain sees them. Also used for hand-labelled cards.</summary>
        public static XgBoardCard FromCard(XgCard card)
        {
            var b = new XgBoardCard { region = Region(card.dataset), seed = card.seed, truth = card.truth };
            var r = new Random(card.seed ^ 0x5A17);
            switch (card.dataset)
            {
                case "mnist": Digit(b, r, card.digit, card.asked); break;
                case "cifar": case "imagenet": Icon(b, r, card); break;
                case "meme": Face(b, r, card); break;
                case "go": Board(b, card); break;
                case "poems": case "news":
                    for (int i = 0; i < card.shown && i < card.line.Length; i++) b.Add(card.line.Substring(i, 1), i);
                    b.Add("接?" + card.askedChar);
                    break;
                case "logic": if (card.kind == "shutdown") b.Add(XgSim.SeedKey); else Logic(b, r, card); break;
                case "danmu": case "translate": Sentence(b, r, card.question); break;
                default: Words(b, card.dataset, card.question); break;
            }
            return b;
        }

        // ───────────── 视觉 ─────────────

        // Seven-segment digits: a top, b upper right, c lower right, d bottom, e lower left, f upper left, g middle.
        static readonly string[] Segments = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };

        /// <summary>
        /// A digit as stroke junctions on a 2×3 lattice, shifted on a 7×7 canvas with a stray dot. Each junction is a
        /// local feature (┌ ┐ ├ …); with positions bound every shift is new, with local sharing it carries over.
        /// </summary>
        static void Digit(XgBoardCard b, Random r, int digit, int asked)
        {
            string seg = Segments[Math.Max(0, Math.Min(9, digit))];
            int ox = r.Next(4), oy = r.Next(3);
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 2; col++)
                {
                    string glyph = Junction(seg, row, col);
                    if (glyph.Length > 0) b.Add(glyph, ox + col * 2, oy + row * 2);
                }
            if (r.Next(3) == 0) b.Add("点", r.Next(7), r.Next(7));
            b.Add("问" + asked);
        }

        static string Junction(string seg, int row, int col)
        {
            bool Has(char s) { return seg.IndexOf(s) >= 0; }
            // Strokes leaving the lattice point: right, down, left, up.
            bool right = col == 0 && (row == 0 ? Has('a') : row == 1 ? Has('g') : Has('d'));
            bool left = col == 1 && (row == 0 ? Has('a') : row == 1 ? Has('g') : Has('d'));
            bool down = row < 2 && (col == 0 ? (row == 0 ? Has('f') : Has('e')) : (row == 0 ? Has('b') : Has('c')));
            bool up = row > 0 && (col == 0 ? (row == 1 ? Has('f') : Has('e')) : (row == 1 ? Has('b') : Has('c')));
            int mask = (right ? 1 : 0) | (down ? 2 : 0) | (left ? 4 : 0) | (up ? 8 : 0);
            string[] glyphs = { "", "╶", "╷", "┌", "╴", "─", "┐", "┬", "╵", "└", "│", "├", "┘", "┴", "┤", "┼" };
            return glyphs[mask];
        }

        // 12306-style icons: three parts each, some shared, so one part alone never names the icon.
        static readonly string[][] IconParts =
        {
            new[] { "弧顶", "竖柄", "钩" },      // 雨伞
            new[] { "弧口", "柄环", "热气" },    // 茶杯
            new[] { "菱形", "长尾", "线" },      // 风筝
            new[] { "圆身", "穗", "提柄" },      // 灯笼
            new[] { "柄环", "齿", "长杆" },      // 钥匙
            new[] { "方身", "窗格", "轮" },      // 公交车
            new[] { "叶片", "罩网", "竖柄" },    // 电风扇
            new[] { "长杆", "弧口", "提柄" },    // 热水瓶
            new[] { "轮", "辐条", "圆身" },      // 方向盘 = 老司机
        };

        static void Icon(XgBoardCard b, Random r, XgCard card)
        {
            void Draw(int icon, int x0)
            {
                var parts = IconParts[Math.Max(0, Math.Min(IconParts.Length - 1, icon))];
                int ox = r.Next(2), oy = r.Next(3);
                for (int i = 0; i < parts.Length; i++) b.Add(parts[i], x0 + ox + (i % 2), oy + i);
            }
            Draw(card.digit, 0);
            bool both = card.shown >= 0 && card.level >= 4;
            if (both) Draw(card.shown, 4);
            for (int i = 0; i < Math.Max(1, card.level); i++) b.Add("干扰线", r.Next(8), r.Next(6));
            b.Add((both ? "都是?" : "是?") + card.asked);
        }

        static readonly string[][] FaceParts =
        {
            new[] { "眯眼", "咧嘴", "苹果肌" }, // 笑
            new[] { "瞪眼", "撇嘴", "皱眉" },   // 生气
            new[] { "泪眼", "下垂嘴", "皱眉" }, // 哭
            new[] { "圆眼", "O 嘴", "挑眉" },   // 发懵
            new[] { "眯眼", "歪嘴", "挑眉" },   // 坏笑
        };

        /// <summary>A face (what the question asks about) and a caption (words that usually agree: a shortcut).</summary>
        static void Face(XgBoardCard b, Random r, XgCard card)
        {
            var parts = FaceParts[Math.Max(0, Math.Min(FaceParts.Length - 1, card.digit))];
            int ox = r.Next(2);
            b.Add(parts[0], ox, 0).Add(parts[1], ox, 2).Add(parts[2], ox + 1, 0);
            var line = card.line ?? "";
            for (int i = 0; i + 1 < line.Length && i < 8; i += 2) b.Add("字:" + line.Substring(i, 2));
            b.Add("问" + XgVisual.Moods[Math.Max(0, Math.Min(XgVisual.Moods.Length - 1, card.asked))]);
        }

        /// <summary>Go: each black stone as its local liberty count, plus the group size and the question kind.</summary>
        static void Board(XgBoardCard b, XgCard card)
        {
            var stones = card.line ?? "";
            int black = 0;
            for (int p = 0; p < stones.Length && p < XgVisual.Board * XgVisual.Board; p++)
            {
                if (stones[p] != 'B') continue;
                black++;
                int free = 0;
                foreach (int n in XgVisual.Neighbours(p)) if (n < stones.Length && stones[n] == '.') free++;
                b.Add("黑" + free + "空", p % XgVisual.Board, p / XgVisual.Board);
            }
            b.Add("块" + Math.Min(5, black));
            string q = card.question ?? "";
            b.Add(q.Contains("提") ? "问:能提" : q.Contains("正好") ? "问:正好" + card.shown : "问:一口气");
        }

        // ───────────── 序列 ─────────────

        static readonly Dictionary<string, HashSet<string>> vocabularies = new Dictionary<string, HashSet<string>>();

        /// <summary>Two-character words that occur in at least two phrases of the desk: the words a reader would notice.</summary>
        static HashSet<string> Vocabulary(string dataset)
        {
            lock (vocabularies)
            {
                if (vocabularies.TryGetValue(dataset, out var known)) return known;
                var counts = new Dictionary<string, int>();
                var r = new Random(dataset.GetHashCode() & 0x7fffffff);
                var phrases = new HashSet<string>();
                for (int i = 0; i < 4000; i++) { var p = XgMemes.Pick(dataset, r, XgMemes.MaxLevel, 20161231, ""); if (p != null) phrases.Add(p.text); }
                foreach (var text in phrases)
                    foreach (var w in Bigrams(text)) { counts.TryGetValue(w, out int n); counts[w] = n + 1; }
                known = new HashSet<string>();
                foreach (var pair in counts) if (pair.Value >= 2) known.Add(pair.Key);
                vocabularies[dataset] = known;
                return known;
            }
        }

        static IEnumerable<string> Bigrams(string text)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i + 1 < text.Length; i++)
            {
                string w = text.Substring(i, 2);
                if (char.IsWhiteSpace(w[0]) || char.IsWhiteSpace(w[1]) || !seen.Add(w)) continue;
                yield return w;
            }
        }

        /// <summary>A text as the set of its familiar words (order ignored): "只看出现了哪些词".</summary>
        static void Words(XgBoardCard b, string dataset, string text)
        {
            var known = Vocabulary(dataset);
            int added = 0;
            foreach (var w in Bigrams(text ?? "")) if (known.Contains(w) && added++ < 6) b.Add(w);
            if (added == 0) b.Add("生词");
        }

        static readonly string[] Fillers = { "2", "3", "啊", "哈", "嗯", "这", "。" };
        /// <summary>Filler words padded around a danmaku line (特征工程 drops them as stop words).</summary>
        public static bool IsFiller(string word) => Array.IndexOf(Fillers, word) >= 0;
        /// <summary>The stray dot drawn on some vision cards (特征工程 removes it as noise).</summary>
        public const string StrayDot = "点";

        /// <summary>A sentence as positioned characters with filler on both sides, so meaning never sits at a fixed slot.</summary>
        static void Sentence(XgBoardCard b, Random r, string text)
        {
            text = text ?? "";
            int x = 0;
            for (int i = r.Next(4); i > 0; i--) b.Add(Fillers[r.Next(Fillers.Length)], x++);
            foreach (char c in text) if (!char.IsWhiteSpace(c)) b.Add(c.ToString(), x++);
            for (int i = r.Next(3); i > 0; i--) b.Add(Fillers[r.Next(Fillers.Length)], x++);
        }

        static readonly string[] Conditions = { "下雨", "放假", "停电", "涨价" };
        const string LongPool = "的了他这个们来到时大为子中说生国年着就和";

        /// <summary>
        /// 长句: a condition, then up to <paramref name="maxDistance"/> characters before the question at the end asks
        /// about it. The distance between condition and question is the diagnostic attribute (近 / 中 / 远); a few
        /// characters before the condition shift its absolute position, so only carrying it forward works.
        /// </summary>
        static XgBoardCard LongText(int seed, Random r, int maxDistance, bool twoSentences)
        {
            var card = new XgBoardCard { region = "sequence", seed = seed };
            int cond = r.Next(Conditions.Length);
            int asked = r.Next(2) == 0 ? cond : (cond + 1 + r.Next(Conditions.Length - 1)) % Conditions.Length;
            card.truth = asked == cond;
            int distance = 1 + r.Next(maxDistance);
            int before = r.Next(4);
            int x = 0;
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < before; i++) { string f = LongPool[r.Next(LongPool.Length)].ToString(); card.Add(f, x++); text.Append(f); }
            card.Add(Conditions[cond], x++); text.Append("【").Append(Conditions[cond]).Append("】");
            for (int i = 0; i < distance; i++)
            {
                string f = twoSentences && i == distance / 2 ? "。" : LongPool[r.Next(LongPool.Length)].ToString();
                card.Add(f, x++); text.Append(f);
            }
            card.Add("问:" + Conditions[asked]);
            card.distance = distance;
            card.text = text + "……前面说的是「" + Conditions[asked] + "」吗？";
            return card;
        }

        /// <summary>
        /// 串行瓶颈的墙: the first character decides, and the same characters also appear as distractors later, so only
        /// knowing where each character sits (position tags) separates them.
        /// </summary>
        static XgBoardCard Parallel(int seed, Random r, int length)
        {
            string[] heads = { "春", "夜", "江", "山" };
            string pool = "床前明月光疑是地上霜举头望";
            var card = new XgBoardCard { region = "sequence", seed = seed };
            int head = r.Next(heads.Length);
            int asked = r.Next(2) == 0 ? head : (head + 1 + r.Next(heads.Length - 1)) % heads.Length;
            card.truth = asked == head;
            card.Add(heads[head], 0);
            for (int i = 1; i < length; i++) card.Add(r.Next(4) == 0 ? heads[r.Next(heads.Length)] : pool[r.Next(pool.Length)].ToString(), i);
            card.Add("首?" + heads[asked]);
            return card;
        }

        static readonly (string zh, string en)[] Dictionary =
        {
            ("我", "I"), ("你", "you"), ("喜欢", "like"), ("吃", "eat"), ("苹果", "apples"), ("今天", "today"),
            ("很", "very"), ("忙", "busy"), ("他", "he"), ("去", "go"), ("学校", "school"), ("明天", "tomorrow"),
        };

        /// <summary>
        /// A Chinese sentence (seq 0) and the English for one of its words (seq 1, same position): "这个词翻得对吗？".
        /// A loop never reaches the other sentence; an encoder–decoder only keeps the end of a long source.
        /// </summary>
        static XgBoardCard Translation(int seed, Random r, int length)
        {
            var card = new XgBoardCard { region = "sequence", seed = seed, truth = r.Next(2) == 0 };
            var words = new int[length];
            for (int i = 0; i < length; i++) { words[i] = r.Next(Dictionary.Length); card.Add(Dictionary[words[i]].zh, i, 0, 0); }
            int k = r.Next(length);
            int w = card.truth ? words[k] : (words[k] + 1 + r.Next(Dictionary.Length - 1)) % Dictionary.Length;
            if (!card.truth) for (int i = 0; i < length; i++) if (i != k && words[i] == w) { card.truth = Dictionary[w].en == Dictionary[words[k]].en; break; }
            card.Add(Dictionary[w].en, k, 0, 1);
            return card;
        }

        // ───────────── 逻辑 ─────────────

        /// <summary>
        /// Logic questions as clue elements whose combination gives the answer: simple kinds need one clue (linear),
        /// if–then needs two (implication), knights-and-knaves need the exclusive or of two.
        /// </summary>
        static void Logic(XgBoardCard b, Random r, XgCard card)
        {
            string kind = card.category ?? "";
            b.Add("类:" + kind);
            bool t = card.truth;
            switch (kind)
            {
                case "条件推理":
                {
                    bool p, q; do { p = r.Next(2) == 0; q = r.Next(2) == 0; } while ((!p || q) != t);
                    b.Add(p ? "前件真" : "前件假").Add(q ? "后件真" : "后件假"); break;
                }
                case "三段论": case "真假运算": case "日历":
                {
                    bool p, q; do { p = r.Next(2) == 0; q = r.Next(2) == 0; } while ((p && q) != t);
                    b.Add(p ? "条件一成立" : "条件一不成立").Add(q ? "条件二成立" : "条件二不成立"); break;
                }
                case "真话假话":
                {
                    bool p = r.Next(2) == 0, q = p ^ t;
                    b.Add(p ? "甲说真" : "甲说假").Add(q ? "乙说真" : "乙说假"); break;
                }
                default:
                    // A careless read usually agrees with the answer (~85%); checking the working settles it, but only
                    // some questions let you check (the rest stay a judgement call).
                    b.Add((r.NextDouble() < .85 ? t : !t) ? "看着对" : "看着不对");
                    if (r.NextDouble() < .7) b.Add(t ? "验算成立" : "验算不成立");
                    break;
            }
            if (r.Next(2) == 0) b.Add("干扰:" + r.Next(4));
        }
    }
}
