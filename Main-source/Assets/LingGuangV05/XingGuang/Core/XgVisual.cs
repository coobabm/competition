using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Picture desks: 12306-style captchas, rage-comic faces and 9×9 Go liberties. Each builds the answer first and the
    /// picture second. The view draws them from the card fields (see <see cref="XgCard"/>).
    /// </summary>
    public static class XgVisual
    {
        // ───────────── 验证码 ─────────────

        public const int SteeringWheel = 8;
        public static readonly string[] Icons = { "雨伞", "茶杯", "风筝", "灯笼", "钥匙", "公交车", "电风扇", "热水瓶", "老司机" };
        public static readonly string[] IconsEn = { "umbrella", "teacup", "kite", "lantern", "key", "bus", "electric fan", "thermos", "old driver" };
        public const int CaptchaMaxLevel = 4;

        /// <summary>Level 1–3: one picture, noisier each level. Level 4: two pictures, "are both …?". Rarely: 「请点击所有的老司机」.</summary>
        public static void Captcha(XgCard card, Random r, int level)
        {
            bool want = r.NextDouble() < .5;
            card.level = level;
            card.shown = -1;
            if (r.NextDouble() < .04)
            {
                card.asked = SteeringWheel;
                card.digit = want ? SteeringWheel : r.Next(SteeringWheel);
                card.truth = want;
                card.category = "彩蛋"; card.categoryEn = "Easter egg";
                card.question = "请点击所有的「老司机」：图里是吗？"; card.questionEn = "Select every \"old driver\": is this one?";
                card.why = want ? "方向盘 = 老司机。" : "这是" + Icons[card.digit] + "，不是方向盘。";
                card.whyEn = want ? "A steering wheel is the old driver." : "That is a " + IconsEn[card.digit] + ", not a steering wheel.";
                card.source = "「老司机」，2016 年初走红；12306 图片验证码，2015 年春运上线";
                return;
            }
            card.asked = r.Next(SteeringWheel);
            card.category = "12306 验证码"; card.categoryEn = "Ticket captcha";
            card.source = "12306 图片验证码，2015 年春运上线";
            if (level >= 4)
            {
                if (want) { card.digit = card.asked; card.shown = card.asked; }
                else
                {
                    int odd = r.Next(3);
                    card.digit = odd == 1 ? card.asked : Other(r, card.asked);
                    card.shown = odd == 0 ? card.asked : Other(r, card.asked);
                }
                card.truth = card.digit == card.asked && card.shown == card.asked;
                card.question = "两张都是「" + Icons[card.asked] + "」吗？"; card.questionEn = "Are both of them a " + IconsEn[card.asked] + "?";
                card.why = card.truth ? "两张都是" + Icons[card.asked] + "。" : "左边是" + Icons[card.digit] + "，右边是" + Icons[card.shown] + "。";
                card.whyEn = card.truth ? "Both are a " + IconsEn[card.asked] + "." : "Left: " + IconsEn[card.digit] + ", right: " + IconsEn[card.shown] + ".";
                return;
            }
            card.digit = want ? card.asked : Other(r, card.asked);
            card.truth = card.digit == card.asked;
            card.question = "图里是「" + Icons[card.asked] + "」吗？"; card.questionEn = "Is this a " + IconsEn[card.asked] + "?";
            card.why = "图里是" + Icons[card.digit] + "。"; card.whyEn = "It is a " + IconsEn[card.digit] + ".";
        }

        static int Other(Random r, int not) { int x = r.Next(SteeringWheel - 1); return x >= not ? x + 1 : x; }

        // ───────────── 表情包 ─────────────

        public static readonly string[] Moods = { "笑", "生气", "哭", "发懵", "坏笑" };
        public static readonly string[] MoodsEn = { "laughing", "angry", "crying", "confused", "smirking" };
        public const int MemeMaxLevel = 3;

        static readonly (string text, int since)[][] Captions =
        {
            new[] { ("哈哈哈哈哈哈哈", 20150101), ("开心得像个两百斤的胖子", 20150101), ("今天也是元气满满", 20150101), ("老司机带带我", 20160101) },
            new[] { ("你过来，我保证不打死你", 20150101), ("气到爆炸", 20150101), ("我要打十个", 20160304), ("友谊的小船说翻就翻", 20160331) },
            new[] { ("宝宝心里苦", 20160101), ("心好累", 20150101), ("我选择狗带", 20160101), ("蓝瘦，香菇", 20161009) },
            new[] { ("我是谁？我在哪？", 20150101), ("一脸懵逼", 20150101), ("excuse me？", 20150101), ("这届人民不行？", 20160324) },
            new[] { ("你懂的", 20150101), ("我只是个吃瓜群众", 20150101), ("嘿嘿嘿", 20150101), ("滑稽", 20150101) },
        };

        /// <summary>
        /// A face (digit = mood) with a caption (line). The question asks about the face, never the caption; a caption that
        /// does not match the face is the trap (10% / 30% / 50% by level).
        /// </summary>
        public static void Meme(XgCard card, Random r, int level, int today)
        {
            bool want = r.NextDouble() < .5;
            card.level = level;
            card.asked = r.Next(Moods.Length);
            card.digit = want ? card.asked : (card.asked + 1 + r.Next(Moods.Length - 1)) % Moods.Length;
            card.truth = card.digit == card.asked;
            double mismatch = level >= 3 ? .5 : level == 2 ? .3 : .1;
            int captionMood = r.NextDouble() < mismatch ? (card.digit + 1 + r.Next(Moods.Length - 1)) % Moods.Length : card.digit;
            var open = new List<string>();
            foreach (var c in Captions[captionMood]) if (c.since <= today) open.Add(c.text);
            card.line = open.Count > 0 ? open[r.Next(open.Count)] : "";
            card.trick = captionMood != card.digit;
            card.category = card.trick ? "字和脸对不上" : "表情包"; card.categoryEn = card.trick ? "Caption mismatch" : "Meme";
            card.question = "这张脸是在" + Moods[card.asked] + "吗？"; card.questionEn = "Is this face " + MoodsEn[card.asked] + "?";
            card.why = "脸是在" + Moods[card.digit] + "。" + (card.trick ? "配字是骗人的，只看脸。" : "");
            card.whyEn = "The face is " + MoodsEn[card.digit] + "." + (card.trick ? " The caption lies; read the face." : "");
            card.source = "暴走漫画、金馆长表情包，斗图文化";
        }

        // ───────────── 围棋气数 ─────────────

        public const int Board = 9;
        public const int GoMaxLevel = 3;

        /// <summary>
        /// Level 1: 「只剩一口气了吗？」 Level 2: 「正好有 N 口气吗？」 Level 3: 「白下在 ✕ 能提掉吗？」.
        /// Builds a black group, fills some of its liberties with white, adds distractors that touch neither, and checks
        /// every group on the board still has a liberty (a legal position).
        /// </summary>
        public static void Go(XgCard card, Random r, int level)
        {
            card.level = level;
            card.category = "围棋"; card.categoryEn = "Go";
            card.source = "AlphaGo 4:1 李世石，2016-03";
            bool want = r.NextDouble() < .5;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var b = new char[Board * Board];
                for (int i = 0; i < b.Length; i++) b[i] = '.';
                int start = r.Next(b.Length);
                b[start] = 'B';
                int size = 1 + r.Next(level >= 2 ? 4 : 3);
                for (int k = 1; k < size; k++)
                {
                    var grow = new List<int>();
                    foreach (int s in Group(b, start)) foreach (int n in Neighbours(s)) if (b[n] == '.') grow.Add(n);
                    if (grow.Count == 0) break;
                    b[grow[r.Next(grow.Count)]] = 'B';
                }
                var libs = Liberties(b, start);
                int keep;
                if (level == 1) keep = want ? 1 : 2 + r.Next(2);
                else if (level == 2) keep = 1 + r.Next(4);
                else keep = want ? 1 : 1 + r.Next(2);
                if (keep > libs.Count) continue;
                Shuffle(libs, r);
                for (int i = keep; i < libs.Count; i++) b[libs[i]] = 'W';
                var kept = libs.GetRange(0, keep);
                // Distractors well away from the marked group.
                var near = new HashSet<int>(Group(b, start)); foreach (int s in Group(b, start)) foreach (int n in Neighbours(s)) near.Add(n);
                int extra = 2 + r.Next(5);
                for (int i = 0, tries = 0; i < extra && tries < 60; tries++)
                {
                    int p = r.Next(b.Length);
                    if (b[p] != '.' || near.Contains(p)) continue;
                    b[p] = r.NextDouble() < .5 ? 'B' : 'W';
                    if (!Legal(b) || Liberties(b, start).Count != keep) { b[p] = '.'; continue; }
                    i++;
                }
                if (!Legal(b) || Liberties(b, start).Count != keep) continue;
                card.line = new string(b);
                card.digit = start;
                card.asked = -1;
                int libsNow = keep;
                if (level == 1)
                {
                    card.truth = libsNow == 1;
                    card.question = "标出的黑棋只剩一口气了吗？"; card.questionEn = "Is the marked black group down to one liberty?";
                    card.why = "它有 " + libsNow + " 口气。"; card.whyEn = "It has " + libsNow + " liberties.";
                }
                else if (level == 2)
                {
                    int ask = want ? libsNow : Math.Max(1, libsNow + (r.NextDouble() < .5 || libsNow == 1 ? 1 : -1));
                    card.shown = ask;
                    card.truth = ask == libsNow;
                    card.question = "标出的黑棋正好有 " + ask + " 口气吗？"; card.questionEn = "Does the marked black group have exactly " + ask + " liberties?";
                    card.why = "它有 " + libsNow + " 口气。"; card.whyEn = "It has " + libsNow + " liberties.";
                }
                else
                {
                    var empties = new List<int>();
                    for (int i = 0; i < b.Length; i++) if (b[i] == '.' && !kept.Contains(i)) empties.Add(i);
                    if (want) card.asked = kept[0];
                    else if (libsNow == 2 || empties.Count == 0) card.asked = kept[r.Next(kept.Count)];
                    else card.asked = empties[r.Next(empties.Count)];
                    card.truth = libsNow == 1 && card.asked == kept[0];
                    card.question = "白棋下在 ✕，能提掉标出的黑棋吗？"; card.questionEn = "Does white at ✕ capture the marked black group?";
                    card.why = card.truth ? "✕ 是它最后一口气。" : libsNow > 1 ? "它有 " + libsNow + " 口气，下一手提不掉。" : "✕ 不是它的气。";
                    card.whyEn = card.truth ? "✕ is its last liberty." : libsNow > 1 ? "It has " + libsNow + " liberties; one move cannot take it." : "✕ is not one of its liberties.";
                }
                return;
            }
            // Fallback (never expected): a lone stone in the centre with four liberties.
            var empty = new string('.', Board * Board).ToCharArray(); empty[40] = 'B';
            card.line = new string(empty); card.digit = 40; card.asked = -1; card.truth = false;
            card.question = "标出的黑棋只剩一口气了吗？"; card.questionEn = "Is the marked black group down to one liberty?";
            card.why = "它有 4 口气。"; card.whyEn = "It has 4 liberties.";
        }

        public static IEnumerable<int> Neighbours(int p)
        {
            int x = p % Board, y = p / Board;
            if (x > 0) yield return p - 1;
            if (x < Board - 1) yield return p + 1;
            if (y > 0) yield return p - Board;
            if (y < Board - 1) yield return p + Board;
        }

        /// <summary>The stones connected to p (same colour).</summary>
        public static List<int> Group(IList<char> b, int p)
        {
            var list = new List<int>();
            if (p < 0 || p >= b.Count || b[p] == '.') return list;
            var seen = new HashSet<int> { p };
            var stack = new Stack<int>(); stack.Push(p);
            while (stack.Count > 0)
            {
                int s = stack.Pop(); list.Add(s);
                foreach (int n in Neighbours(s)) if (b[n] == b[p] && seen.Add(n)) stack.Push(n);
            }
            return list;
        }

        public static List<int> Liberties(IList<char> b, int p)
        {
            var libs = new List<int>();
            foreach (int s in Group(b, p)) foreach (int n in Neighbours(s)) if (b[n] == '.' && !libs.Contains(n)) libs.Add(n);
            return libs;
        }

        static bool Legal(IList<char> b)
        {
            for (int i = 0; i < b.Count; i++) if (b[i] != '.' && Liberties(b, i).Count == 0) return false;
            return true;
        }

        static void Shuffle(List<int> list, Random r)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = r.Next(i + 1); int t = list[i]; list[i] = list[j]; list[j] = t; }
        }
    }
}
