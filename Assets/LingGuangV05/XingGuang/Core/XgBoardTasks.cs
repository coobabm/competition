using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Small generated datasets for the concept board. They are the wall datasets' shapes (异或, shifted digits,
    /// long sentences) and double as the rule tests: each one shows a phenomenon appearing on its own.
    /// </summary>
    public static class XgBoardTasks
    {
        /// <summary>Linearly separable: "含不含「中奖」" with distractor words.</summary>
        public static XgBoardCard Linear(int seed)
        {
            var r = new Random(seed);
            string[] words = { "中奖", "会员", "回复", "退订", "领取", "通知", "账户", "积分" };
            var card = new XgBoardCard { region = "logic", seed = seed };
            bool has = r.Next(2) == 0;
            if (has) card.Add("中奖");
            for (int i = 0; i < 3; i++) { string w = words[1 + r.Next(words.Length - 1)]; if (!card.features.Exists(f => f.name == w)) card.Add(w); }
            card.truth = has;
            return card;
        }

        /// <summary>Exclusive or: exactly one of A, B is true. No single element predicts the answer.</summary>
        public static XgBoardCard Xor(int seed)
        {
            var r = new Random(seed);
            bool a = r.Next(2) == 0, b = r.Next(2) == 0;
            var card = new XgBoardCard { region = "logic", seed = seed, truth = a ^ b };
            card.Add(a ? "甲真" : "甲假").Add(b ? "乙真" : "乙假");
            return card;
        }

        /// <summary>Stroke shapes of four digits, relative to their top-left corner.</summary>
        static readonly (string stroke, int x, int y)[][] Shapes =
        {
            new[] { ("弧", 0, 0), ("弧", 1, 0), ("弧", 0, 1), ("弧", 1, 1) },              // 0
            new[] { ("竖", 0, 0), ("竖", 0, 1), ("竖", 0, 2) },                            // 1
            new[] { ("横", 0, 0), ("横", 1, 0), ("斜", 1, 1), ("斜", 0, 2) },              // 7
            new[] { ("斜", 0, 0), ("竖", 1, 0), ("横", 0, 1), ("横", 1, 1), ("竖", 1, 2) }, // 4
        };
        static readonly string[] ShapeNames = { "0", "1", "7", "4" };

        /// <summary>
        /// A digit drawn somewhere on a 7×7 grid with a stray dot, and the question "这是 k 吗？". With positions bound
        /// (全连) every shift is a new pattern to memorise; position-free local features (局部共享) carry over.
        /// </summary>
        public static XgBoardCard Digit(int seed, int shift = 4)
        {
            var r = new Random(seed);
            int digit = r.Next(Shapes.Length);
            int asked = r.Next(2) == 0 ? digit : (digit + 1 + r.Next(Shapes.Length - 1)) % Shapes.Length;
            int ox = r.Next(shift + 1), oy = r.Next(shift + 1);
            var card = new XgBoardCard { region = "vision", seed = seed, truth = asked == digit };
            foreach (var p in Shapes[digit]) card.Add(p.stroke, ox + p.x, oy + p.y);
            card.Add("点", r.Next(7), r.Next(7));
            card.Add("问" + ShapeNames[asked]);
            return card;
        }

        /// <summary>
        /// A sentence of <paramref name="length"/> characters; the question is whether its first character was
        /// the asked one. Plain loops forget the start of long sentences (长句失忆), gated loops keep it.
        /// </summary>
        public static XgBoardCard FirstChar(int seed, int length, string pool = "床前明月光疑是地上霜举头望低思故乡白日依山尽")
        {
            var r = new Random(seed);
            string[] heads = { "春", "夜", "江", "山" };
            int head = r.Next(heads.Length);
            int asked = r.Next(2) == 0 ? head : (head + 1 + r.Next(heads.Length - 1)) % heads.Length;
            var card = new XgBoardCard { region = "sequence", seed = seed, truth = asked == head };
            card.Add(heads[head], 0);
            for (int i = 1; i < length; i++) card.Add(pool[r.Next(pool.Length)].ToString(), i);
            card.Add("首?" + heads[asked]);
            return card;
        }

        public static List<XgBoardCard> Set(Func<int, XgBoardCard> make, int seed, int count)
        {
            var list = new List<XgBoardCard>(count);
            for (int i = 0; i < count; i++) list.Add(make(seed * 7919 + i * 104729 + 17));
            return list;
        }

        /// <summary>Trains <paramref name="cards"/> passes over a fixed training pool and returns the test accuracy.</summary>
        public static double Run(XgBoard brain, XgKnobs k, IList<XgBoardCard> pool, int cards, IList<XgBoardCard> test, out int diverged)
        {
            diverged = 0;
            for (int i = 0; i < cards; i++) if (brain.Train(pool[i % pool.Count], k).diverged) diverged++;
            return brain.Accuracy(test, k);
        }
    }
}
