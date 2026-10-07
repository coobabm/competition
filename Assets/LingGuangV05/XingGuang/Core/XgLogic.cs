using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>One generated yes/no question with its answer and a short reason.</summary>
    public sealed class XgLogicQuestion
    {
        public string category, categoryEn, text, textEn, why, whyEn;
        /// <summary>The claim as a plain formula ("17 + 5 == 22", "prime(91) == 1"); set by the 算术 desk (XgArith) for its tests.</summary>
        public string check = "";
        public bool truth;
        public int level;
    }

    /// <summary>
    /// Endless yes/no logic questions, generated from a seed and a level (1–5). Each generator first picks the answer it
    /// wants (about half yes) and then builds a question with that answer, so the truth is known by construction.
    /// Puzzle words are made up where real-world knowledge could interfere (syllogisms). Sums, number properties and
    /// number patterns live on the 算术 desk (<see cref="XgArith"/>); this desk is reasoning only.
    /// </summary>
    public static class XgLogic
    {
        public const int MaxLevel = 5;

        delegate XgLogicQuestion Gen(Random r, int level, bool want);

        /// <summary>Kinds by first level and weight.</summary>
        static readonly (int minLevel, int weight, Gen gen)[] Generators =
        {
            (1, 2, Compare), (1, 2, Negation), (1, 2, OnlyIf),
            (2, 2, Conditional), (2, 2, Disjunction), (2, 2, Conversion),
            (3, 2, Syllogism), (3, 1, Boolean), (3, 1, Calendar), (3, 2, Contrapositive),
            (4, 2, Knights), (4, 2, Elimination),
        };

        public static XgLogicQuestion Generate(int seed, int level)
        {
            level = Math.Max(1, Math.Min(MaxLevel, level));
            var r = new Random(seed);
            var pool = new List<Gen>();
            foreach (var (min, weight, gen) in Generators) if (level >= min) for (int i = 0; i < weight; i++) pool.Add(gen);
            // Newly unlocked kinds show up more often at their first level.
            foreach (var (min, weight, gen) in Generators) if (level == min && min > 1) pool.Add(gen);
            bool want = r.NextDouble() < .5;
            var q = pool[r.Next(pool.Count)](r, level, want);
            q.level = level;
            return q;
        }

        // ───────────── comparisons (transitivity) ─────────────

        static readonly string[] People = { "小明", "小红", "小刚", "小丽", "阿杰", "老周", "表姐", "网管" };
        static readonly string[] PeopleEn = { "Ming", "Hong", "Gang", "Li", "Jie", "Zhou", "Cousin", "Clerk" };
        static readonly (string more, string less, string moreEn, string lessEn)[] Relations =
        {
            ("高", "矮", "taller", "shorter"), ("大", "小", "older", "younger"), ("重", "轻", "heavier", "lighter"), ("先到", "后到", "earlier", "later"),
        };

        static XgLogicQuestion Compare(Random r, int level, bool want)
        {
            int n = Math.Min(People.Length, level + 2);
            var idx = Shuffled(r, People.Length);
            var order = idx.GetRange(0, n); // order[0] is the most
            var rel = Relations[r.Next(Relations.Length)];
            var facts = new List<(string zh, string en)>();
            for (int i = 0; i + 1 < n; i++)
            {
                int a = order[i], b = order[i + 1];
                if (level >= 3 && r.Next(2) == 0) facts.Add((People[b] + "比" + People[a] + rel.less, PeopleEn[b] + " is " + rel.lessEn + " than " + PeopleEn[a]));
                else facts.Add((People[a] + "比" + People[b] + rel.more, PeopleEn[a] + " is " + rel.moreEn + " than " + PeopleEn[b]));
            }
            if (level >= 2) Shuffle(r, facts);
            int x = r.Next(n), y;
            do y = r.Next(n); while (y == x);
            if (Math.Abs(x - y) == 1 && level >= 2 && n > 2) { x = 0; y = n - 1; if (r.Next(2) == 0) { int t = x; x = y; y = t; } }
            bool askMore = r.Next(2) == 0;
            bool truth = askMore ? x < y : x > y;
            if (truth != want) { int t = x; x = y; y = t; truth = !truth; }
            var zh = new StringBuilder(); var en = new StringBuilder();
            foreach (var f in facts) { zh.Append(f.zh).Append("。"); en.Append(f.en).Append(". "); }
            string pa = People[order[x]], pb = People[order[y]], ea = PeopleEn[order[x]], eb = PeopleEn[order[y]];
            return new XgLogicQuestion
            {
                category = "比较", categoryEn = "Ordering",
                text = zh + pa + "比" + pb + (askMore ? rel.more : rel.less) + "吗？",
                textEn = en + "Is " + ea + " " + (askMore ? rel.moreEn : rel.lessEn) + " than " + eb + "?",
                truth = truth, why = "按顺序排：" + Join(order, People, "＞") + "（越前越" + rel.more + "）。",
                whyEn = "Order: " + Join(order, PeopleEn, " > ") + " (first is " + rel.moreEn + ")."
            };
        }

        // ───────────── conditionals ─────────────

        static readonly (string p, string q, string notP, string notQ, string pEn, string qEn, string notPEn, string notQEn)[] Rules =
        {
            ("下雨", "地是湿的", "没下雨", "地不湿", "it rains", "the ground is wet", "it did not rain", "the ground is not wet"),
            ("电费欠缴", "家里停电", "电费没欠", "家里没停电", "the bill is unpaid", "the power is cut", "the bill is paid", "the power is on"),
            ("显卡过热", "训练降频", "显卡没过热", "训练没降频", "the GPU overheats", "training throttles", "the GPU is cool", "training is not throttled"),
            ("老周发来文件", "桌面出现灵光", "老周没发文件", "桌面没出现灵光", "Zhou sends the file", "灵光 appears", "Zhou sent nothing", "灵光 is not there"),
            ("考试及格", "能拿到奖学金", "考试没及格", "拿不到奖学金", "you pass the exam", "you get the grant", "you failed", "you get no grant"),
        };

        static XgLogicQuestion Conditional(Random r, int level, bool want)
        {
            var c = Rules[r.Next(Rules.Length)];
            // valid: modus ponens, modus tollens; invalid: affirming the consequent, denying the antecedent
            int form = want ? r.Next(2) : 2 + r.Next(2);
            string fact, ask, factEn, askEn, why, whyEn;
            switch (form)
            {
                case 0: fact = c.p; ask = c.q; factEn = c.pEn; askEn = c.qEn; why = "如果 P 就 Q，P 成立，所以 Q 成立。"; whyEn = "If P then Q; P holds, so Q holds."; break;
                case 1: fact = c.notQ; ask = c.notP; factEn = c.notQEn; askEn = c.notPEn; why = "如果 P 就 Q；现在没有 Q，所以一定没有 P。"; whyEn = "If P then Q; no Q, so no P."; break;
                case 2: fact = c.q; ask = c.p; factEn = c.qEn; askEn = c.pEn; why = "Q 也可能有别的原因，不能倒推出 P。"; whyEn = "Q may have other causes; you cannot infer P."; break;
                default: fact = c.notP; ask = c.notQ; factEn = c.notPEn; askEn = c.notQEn; why = "没有 P 时，Q 仍可能因为别的原因发生。"; whyEn = "Without P, Q can still happen for other reasons."; break;
            }
            return new XgLogicQuestion
            {
                category = "条件推理", categoryEn = "If–then",
                text = "如果" + c.p + "，那么" + c.q + "。已知" + fact + "。能确定" + ask + "吗？",
                textEn = "If " + c.pEn + ", then " + c.qEn + ". We know " + factEn + ". Can we be sure " + askEn + "?",
                truth = form < 2, why = why, whyEn = whyEn
            };
        }

        // ───────────── syllogisms (nonsense words) ─────────────

        static readonly string[] Words = { "噗噗", "啾啾", "嗒嗒", "咕咕", "呼呼", "啵啵", "喵喵", "哒哒" };
        static readonly string[] WordsEn = { "blips", "zorks", "frobs", "glims", "wugs", "daxes", "toves", "snarks" };
        static readonly (string zh, string en, bool valid)[] Forms =
        {
            ("所有的{A}都是{B}，所有的{B}都是{C}。所以所有的{A}都是{C}", "All {A} are {B}. All {B} are {C}. So all {A} are {C}", true),
            ("没有{A}是{B}，所有的{C}都是{A}。所以没有{C}是{B}", "No {A} are {B}. All {C} are {A}. So no {C} are {B}", true),
            ("有的{A}是{B}，所有的{B}都是{C}。所以有的{A}是{C}", "Some {A} are {B}. All {B} are {C}. So some {A} are {C}", true),
            ("所有的{A}都是{B}，没有{C}是{B}。所以没有{A}是{C}", "All {A} are {B}. No {C} are {B}. So no {A} are {C}", true),
            ("所有的{A}都是{B}，有的{B}是{C}。所以有的{A}是{C}", "All {A} are {B}. Some {B} are {C}. So some {A} are {C}", false),
            ("所有的{A}都是{B}，所有的{C}都是{B}。所以所有的{A}都是{C}", "All {A} are {B}. All {C} are {B}. So all {A} are {C}", false),
            ("有的{A}是{B}，有的{B}是{C}。所以有的{A}是{C}", "Some {A} are {B}. Some {B} are {C}. So some {A} are {C}", false),
            ("没有{A}是{B}，没有{B}是{C}。所以没有{A}是{C}", "No {A} are {B}. No {B} are {C}. So no {A} are {C}", false),
        };

        static XgLogicQuestion Syllogism(Random r, int level, bool want)
        {
            var candidates = new List<int>();
            for (int i = 0; i < Forms.Length; i++) if (Forms[i].valid == want) candidates.Add(i);
            var f = Forms[candidates[r.Next(candidates.Count)]];
            var w = Shuffled(r, Words.Length);
            string Fill(string s, string[] words) => s.Replace("{A}", words[w[0]]).Replace("{B}", words[w[1]]).Replace("{C}", words[w[2]]);
            return new XgLogicQuestion
            {
                category = "三段论", categoryEn = "Syllogism",
                text = Fill(f.zh, Words) + "。这个推理成立吗？", textEn = Fill(f.en, WordsEn) + ". Is this reasoning valid?",
                truth = f.valid,
                why = f.valid ? "结论一定跟着前提成立，和词是什么无关。" : "能举出前提都对、结论却错的情况。",
                whyEn = f.valid ? "The conclusion follows from the premises whatever the words mean." : "There is a case where the premises hold and the conclusion fails."
            };
        }

        // ───────────── boolean expressions ─────────────

        static XgLogicQuestion Boolean(Random r, int level, bool want)
        {
            string zh = "", en = ""; bool value = false;
            for (int tries = 0; tries < 30; tries++)
            {
                int leaves = Math.Min(5, level);
                (zh, en, value) = Expr(r, leaves);
                if (value == want) break;
            }
            return new XgLogicQuestion
            {
                category = "真假运算", categoryEn = "Boolean",
                text = zh + " 的结果是「真」吗？", textEn = "Is " + en + " true?",
                truth = value, why = "逐层计算，结果是「" + (value ? "真" : "假") + "」。", whyEn = "Evaluated step by step it is " + (value ? "true" : "false") + "."
            };
        }

        static (string zh, string en, bool v) Expr(Random r, int leaves)
        {
            if (leaves <= 1)
            {
                bool b = r.Next(2) == 0;
                return r.Next(4) == 0 ? ("非" + (b ? "真" : "假"), "NOT " + (b ? "true" : "false"), !b) : (b ? "真" : "假", b ? "true" : "false", b);
            }
            int left = 1 + r.Next(leaves - 1);
            var a = Expr(r, left); var c = Expr(r, leaves - left);
            bool and = r.Next(2) == 0;
            return ("（" + a.zh + (and ? " 且 " : " 或 ") + c.zh + "）", "(" + a.en + (and ? " AND " : " OR ") + c.en + ")", and ? a.v && c.v : a.v || c.v);
        }

        // ───────────── 2016 calendar ─────────────

        static readonly string[] Weekdays = { "日", "一", "二", "三", "四", "五", "六" };

        static XgLogicQuestion Calendar(Random r, int level, bool want)
        {
            var day = new DateTime(2016, 1, 1).AddDays(r.Next(366));
            int real = (int)day.DayOfWeek;
            int asked = want ? real : (real + 1 + r.Next(6)) % 7;
            return new XgLogicQuestion
            {
                category = "日历", categoryEn = "Calendar",
                text = "（提示：2016 年 5 月 29 日是星期日）2016 年 " + day.Month + " 月 " + day.Day + " 日是星期" + Weekdays[asked] + "吗？",
                textEn = "(Hint: 29 May 2016 was a Sunday.) Was " + day.ToString("d MMMM yyyy", CultureInfo.InvariantCulture) + " a " + ((DayOfWeek)asked) + "?",
                truth = asked == real, why = "那天是星期" + Weekdays[real] + "。", whyEn = "It was a " + (DayOfWeek)real + "."
            };
        }

        // ───────────── truth-tellers and liars ─────────────

        static XgLogicQuestion Knights(Random r, int level, bool want)
        {
            string[] names = { "甲", "乙", "丙" }, namesEn = { "A", "B", "C" };
            int n = level >= 5 ? 3 : 2;
            for (int attempt = 0; attempt < 200; attempt++)
            {
                bool[] honest = new bool[n];
                for (int i = 0; i < n; i++) honest[i] = r.Next(2) == 0;
                var claims = new List<Func<bool[], bool>>();
                var zh = new StringBuilder(); var en = new StringBuilder();
                for (int speaker = 0; speaker < n; speaker++)
                {
                    int i = speaker; // fresh copy per speaker: the claims below capture it
                    // Each speaker makes a claim whose truth matches their honesty under the hidden assignment.
                    Func<bool[], bool> claim = null; string cz = null, ce = null;
                    for (int t = 0; t < 20; t++)
                    {
                        int j = (i + 1 + r.Next(n - 1)) % n, kind = r.Next(4);
                        Func<bool[], bool> c; string z, e;
                        switch (kind)
                        {
                            case 0: c = h => !h[j]; z = names[j] + "在说谎"; e = namesEn[j] + " is lying"; break;
                            case 1: c = h => h[j]; z = names[j] + "说的是真话"; e = namesEn[j] + " tells the truth"; break;
                            case 2: c = h => h[i] && h[j]; z = "我和" + names[j] + "都说真话"; e = namesEn[j] + " and I both tell the truth"; break;
                            default: c = h => h[i] != h[j]; z = "我和" + names[j] + "恰好一个说真话"; e = "exactly one of " + namesEn[j] + " and me tells the truth"; break;
                        }
                        if (c(honest) == honest[i]) { claim = c; cz = z; ce = e; break; }
                    }
                    if (claim == null) break;
                    claims.Add(claim);
                    zh.Append(names[i]).Append("说：“").Append(cz).Append("。”");
                    en.Append(namesEn[i]).Append(" says: \"").Append(ce).Append(".\" ");
                }
                if (claims.Count < n) continue;
                // The puzzle must have exactly one consistent world.
                int solutions = 0;
                for (int mask = 0; mask < (1 << n); mask++)
                {
                    var h = new bool[n];
                    for (int i = 0; i < n; i++) h[i] = (mask >> i & 1) == 1;
                    bool ok = true;
                    for (int i = 0; i < n && ok; i++) ok = claims[i](h) == h[i];
                    if (ok) solutions++;
                }
                if (solutions != 1) continue;
                int who = r.Next(n);
                for (int i = 0; i < n; i++) if (honest[(who + i) % n] == want) { who = (who + i) % n; break; }
                var whyZh = new StringBuilder("唯一自洽的情况："); var whyEn = new StringBuilder("The only consistent case: ");
                for (int i = 0; i < n; i++) { whyZh.Append(names[i]).Append(honest[i] ? "说真话" : "说谎").Append(i + 1 < n ? "，" : "。"); whyEn.Append(namesEn[i]).Append(honest[i] ? " truthful" : " lying").Append(i + 1 < n ? ", " : "."); }
                return new XgLogicQuestion
                {
                    category = "真话假话", categoryEn = "Truth and lies",
                    text = "每个人要么总说真话，要么总说谎。" + zh + names[who] + "说的是真话吗？",
                    textEn = "Everyone always tells the truth or always lies. " + en + "Does " + namesEn[who] + " tell the truth?",
                    truth = honest[who], why = whyZh.ToString(), whyEn = whyEn.ToString()
                };
            }
            return Conditional(r, level, want);
        }

        // ───────────── negation ─────────────

        static readonly (string s, string p, string notP, string sEn, string pEn, string notPEn)[] Groups =
        {
            ("猫", "怕水", "不怕水", "cats", "are afraid of water", "are not afraid of water"),
            ("网吧的电脑", "装了 YY", "没装 YY", "netbar PCs", "have YY installed", "do not have YY installed"),
            ("卡片", "标对了", "标错了", "cards", "are labelled right", "are labelled wrong"),
            ("同学", "会下五子棋", "不会下五子棋", "classmates", "can play gomoku", "cannot play gomoku"),
            ("显卡", "能跑深度学习", "跑不了深度学习", "graphics cards", "can run deep learning", "cannot run deep learning"),
        };

        static XgLogicQuestion Negation(Random r, int level, bool want)
        {
            string text, textEn, why, whyEn, claim, claimEn, neg, negEn;
            if (r.Next(2) == 0)
            {
                var g = Groups[r.Next(Groups.Length)];
                bool all = r.Next(2) == 0;
                string allZh(string p) => "所有的" + g.s + "都" + p;
                string someZh(string p) => "有的" + g.s + p;
                string allEn(string p) => "all " + g.sEn + " " + p;
                string someEn(string p) => "some " + g.sEn + " " + p;
                claim = all ? allZh(g.p) : someZh(g.p); claimEn = all ? allEn(g.pEn) : someEn(g.pEn);
                // The negation of "all … are" is "some … are not"; of "some … are", "all … are not".
                bool some = all == want;
                neg = some ? someZh(g.notP) : allZh(g.notP); negEn = some ? someEn(g.notPEn) : allEn(g.notPEn);
                why = all ? "推翻「所有都」只要一个反例：「有的不」。" : "推翻「有的」要说「一个都没有」：「所有都不」。";
                whyEn = all ? "One counterexample overturns \"all\": \"some are not\"." : "To overturn \"some\" you need \"none\": \"all are not\".";
            }
            else
            {
                var idx = Shuffled(r, People.Length);
                string a = People[idx[0]], b = People[idx[1]], ae = PeopleEn[idx[0]], be = PeopleEn[idx[1]];
                bool both = r.Next(2) == 0;
                claim = both ? a + "和" + b + "都去了网吧" : a + "和" + b + "至少有一个去了网吧";
                claimEn = both ? ae + " and " + be + " both went to the netbar" : "at least one of " + ae + " and " + be + " went to the netbar";
                string oneMissing = a + "和" + b + "至少有一个没去", noneWent = a + "和" + b + "都没去";
                string oneMissingEn = "at least one of " + ae + " and " + be + " did not go", noneWentEn = "neither " + ae + " nor " + be + " went";
                bool pickOne = both == want;
                neg = pickOne ? oneMissing : noneWent; negEn = pickOne ? oneMissingEn : noneWentEn;
                why = both ? "「都去了」只要有一个没去就不成立。" : "「至少一个去了」不成立，只能是两个都没去。";
                whyEn = both ? "\"Both went\" fails as soon as one did not go." : "\"At least one went\" fails only if neither went.";
            }
            text = "「" + claim + "」的否定是「" + neg + "」吗？";
            textEn = "Is \"" + negEn + "\" the negation of \"" + claimEn + "\"?";
            return new XgLogicQuestion { category = "否定", categoryEn = "Negation", text = text, textEn = textEn, truth = want, why = why, whyEn = whyEn };
        }

        // ───────────── necessary conditions (只有…才…) ─────────────

        static readonly (string need, string needYes, string needNo, string goal, string goalYes, string goalNo, string needEn, string needYesEn, string needNoEn, string goalEn, string goalYesEn, string goalNoEn)[] Necessary =
        {
            ("带门票", "他带了门票", "他没带门票", "进场", "他进场了", "他没进场", "you have a ticket", "he had a ticket", "he had no ticket", "you can get in", "he got in", "he did not get in"),
            ("交了电费", "电费交了", "电费没交", "有电", "家里有电", "家里没电", "the bill is paid", "the bill is paid", "the bill is unpaid", "the house has power", "the house has power", "the house has no power"),
            ("显存够大", "显存够大", "显存不够", "训得动大模型", "大模型训起来了", "大模型没训起来", "there is enough VRAM", "there is enough VRAM", "there is not enough VRAM", "a big model can train", "the big model trained", "the big model did not train"),
            ("答对", "这题答对了", "这题答错了", "拿到报酬", "拿到了报酬", "没拿到报酬", "the answer is right", "the answer was right", "the answer was wrong", "you get paid", "it was paid", "it was not paid"),
        };

        static XgLogicQuestion OnlyIf(Random r, int level, bool want)
        {
            var c = Necessary[r.Next(Necessary.Length)];
            // valid: Q so P; no P so no Q. invalid: P so Q; no Q so no P.
            int form = want ? r.Next(2) : 2 + r.Next(2);
            string fact, ask, factEn, askEn;
            switch (form)
            {
                case 0: fact = c.goalYes; ask = c.needYes; factEn = c.goalYesEn; askEn = c.needYesEn; break;
                case 1: fact = c.needNo; ask = c.goalNo; factEn = c.needNoEn; askEn = c.goalNoEn; break;
                case 2: fact = c.needYes; ask = c.goalYes; factEn = c.needYesEn; askEn = c.goalYesEn; break;
                default: fact = c.goalNo; ask = c.needNo; factEn = c.goalNoEn; askEn = c.needNoEn; break;
            }
            return new XgLogicQuestion
            {
                category = "必要条件", categoryEn = "Only if",
                text = "只有" + c.need + "，才能" + c.goal + "。已知" + fact + "。能确定" + ask + "吗？",
                textEn = char.ToUpperInvariant(c.goalEn[0]) + c.goalEn.Substring(1) + " only if " + c.needEn + ". We know " + factEn + ". Can we be sure " + askEn + "?",
                truth = form < 2,
                why = form < 2 ? "「只有 P 才 Q」：P 是必要条件。有 Q 就一定有 P，没 P 就一定没 Q。" : "P 只是必要条件，不是充分条件：有 P 不保证 Q，没 Q 也不说明没 P。",
                whyEn = form < 2 ? "\"Only if P, Q\": P is necessary. Q means P; no P means no Q." : "P is necessary, not sufficient: P does not guarantee Q, and no Q does not rule out P."
            };
        }

        // ───────────── either / or ─────────────

        static readonly (string x, string y, string on, string off, string xEn, string yEn, string onEn, string offEn)[] Pairs =
        {
            ("1 号灯", "2 号灯", "亮着", "没亮", "lamp 1", "lamp 2", "is on", "is off"),
            ("老周", "表姐", "在线", "不在线", "Zhou", "Cousin", "is online", "is offline"),
            ("A 卡", "B 卡", "能用", "坏了", "card A", "card B", "works", "is broken"),
        };

        static XgLogicQuestion Disjunction(Random r, int level, bool want)
        {
            var c = Pairs[r.Next(Pairs.Length)];
            bool exactly = r.Next(2) == 0;
            // (fact about x is on?, asked state of y is on?) and whether it follows.
            var forms = new List<(bool xOn, bool yOn, bool valid)>();
            if (exactly) { forms.Add((true, false, true)); forms.Add((false, true, true)); forms.Add((true, true, false)); forms.Add((false, false, false)); }
            else { forms.Add((false, true, true)); forms.Add((true, false, false)); forms.Add((true, true, false)); }
            var pick = forms.FindAll(f => f.valid == want);
            var f0 = pick[r.Next(pick.Count)];
            string rule = exactly ? c.x + "和" + c.y + "恰好一个" + c.on : c.x + "和" + c.y + "至少一个" + c.on;
            string ruleEn = exactly ? "exactly one of " + c.xEn + " and " + c.yEn + " " + c.onEn : "at least one of " + c.xEn + " and " + c.yEn + " " + c.onEn;
            return new XgLogicQuestion
            {
                category = "或者推理", categoryEn = "Either / or",
                text = "已知" + rule + "。现在" + c.x + (f0.xOn ? c.on : c.off) + "。能确定" + c.y + (f0.yOn ? c.on : c.off) + "吗？",
                textEn = "We know " + ruleEn + ". Now " + c.xEn + " " + (f0.xOn ? c.onEn : c.offEn) + ". Can we be sure " + c.yEn + " " + (f0.yOn ? c.onEn : c.offEn) + "?",
                truth = f0.valid,
                why = exactly ? (f0.valid ? "恰好一个：知道一个，另一个就定了。" : "恰好一个：一个" + c.on + "，另一个只能" + c.off + "；一个" + c.off + "，另一个只能" + c.on + "。")
                    : (f0.valid ? "至少一个：一个" + c.off + "，另一个必须" + c.on + "。" : "至少一个：一个" + c.on + "时，另一个" + c.on + "或" + c.off + "都可以。"),
                whyEn = exactly ? (f0.valid ? "Exactly one: knowing one fixes the other." : "Exactly one: if one is, the other is not, and the other way round.")
                    : (f0.valid ? "At least one: if one is not, the other must be." : "At least one: if one is, the other may or may not be."),
            };
        }

        // ───────────── conversion ─────────────

        static readonly (string zh, string en, bool valid)[] Conversions =
        {
            ("有的{A}是{B}。所以有的{B}是{A}", "Some {A} are {B}. So some {B} are {A}", true),
            ("没有{A}是{B}。所以没有{B}是{A}", "No {A} are {B}. So no {B} are {A}", true),
            ("所有的{A}都是{B}。所以没有{A}不是{B}", "All {A} are {B}. So no {A} is not a {B}", true),
            ("所有的{A}都是{B}。所以所有的{B}都是{A}", "All {A} are {B}. So all {B} are {A}", false),
            ("有的{A}不是{B}。所以有的{B}不是{A}", "Some {A} are not {B}. So some {B} are not {A}", false),
            ("有的{A}是{B}。所以有的{A}不是{B}", "Some {A} are {B}. So some {A} are not {B}", false),
        };

        static XgLogicQuestion Conversion(Random r, int level, bool want)
        {
            var candidates = new List<int>();
            for (int i = 0; i < Conversions.Length; i++) if (Conversions[i].valid == want) candidates.Add(i);
            var f = Conversions[candidates[r.Next(candidates.Count)]];
            var w = Shuffled(r, Words.Length);
            string Fill(string s, string[] words) => s.Replace("{A}", words[w[0]]).Replace("{B}", words[w[1]]);
            return new XgLogicQuestion
            {
                category = "换位推理", categoryEn = "Conversion",
                text = Fill(f.zh, Words) + "。这个推理成立吗？", textEn = Fill(f.en, WordsEn) + ". Is this reasoning valid?",
                truth = f.valid,
                why = f.valid ? "「有的是」「没有是」可以前后对调；「所有都是」换个说法也成立。" : "「所有都是」不能对调，「有的是」也不等于「有的不是」。",
                whyEn = f.valid ? "\"Some are\" and \"none are\" can be swapped; \"all are\" can be restated." : "\"All are\" cannot be swapped, and \"some are\" says nothing about \"some are not\"."
            };
        }

        // ───────────── contrapositive ─────────────

        static XgLogicQuestion Contrapositive(Random r, int level, bool want)
        {
            var c = Rules[r.Next(Rules.Length)];
            int form = want ? r.Next(2) : 2 + r.Next(2);
            string alt, altEn, why, whyEn;
            switch (form)
            {
                case 0: alt = "如果" + c.notQ + "，那么" + c.notP; altEn = "if " + c.notQEn + ", then " + c.notPEn; why = "这是逆否命题，和原命题等价。"; whyEn = "That is the contrapositive; it is equivalent."; break;
                case 1: alt = "不会出现「" + c.p + "，但" + c.notQ + "」的情况"; altEn = "it never happens that " + c.pEn + " but " + c.notQEn; why = "「如果 P 就 Q」正好排除了「P 而且不 Q」。"; whyEn = "\"If P then Q\" rules out exactly \"P and not Q\"."; break;
                case 2: alt = "如果" + c.q + "，那么" + c.p; altEn = "if " + c.qEn + ", then " + c.pEn; why = "这是逆命题，原命题对时它不一定对。"; whyEn = "That is the converse; it can fail when the original holds."; break;
                default: alt = "如果" + c.notP + "，那么" + c.notQ; altEn = "if " + c.notPEn + ", then " + c.notQEn; why = "这是否命题，原命题对时它不一定对。"; whyEn = "That is the inverse; it can fail when the original holds."; break;
            }
            return new XgLogicQuestion
            {
                category = "逆否命题", categoryEn = "Contrapositive",
                text = "「如果" + c.p + "，那么" + c.q + "」和「" + alt + "」说的是同一件事吗？",
                textEn = "Do \"if " + c.pEn + ", then " + c.qEn + "\" and \"" + altEn + "\" say the same thing?",
                truth = form < 2, why = why, whyEn = whyEn
            };
        }

        // ───────────── elimination (logic grid) ─────────────

        /// <summary>A logic grid: the verb, its negative, the items; English: "each …", "X …", "X does not …", "Does X …?".</summary>
        static readonly (string verb, string not, string[] items, string eachEn, string yesEn, string noEn, string askEn, string[] itemsEn)[] Grids =
        {
            ("点了", "没点", new[] { "可乐", "雪碧", "红茶", "泡面" }, "ordered", "ordered", "did not order", "Did {0} order {1}?", new[] { "cola", "Sprite", "iced tea", "instant noodles" }),
            ("坐在", "不坐", new[] { "1 号机", "2 号机", "3 号机", "4 号机" }, "sit at", "sits at", "does not sit at", "Does {0} sit at {1}?", new[] { "PC 1", "PC 2", "PC 3", "PC 4" }),
            ("用的是", "用的不是", new[] { "GTX 970", "GTX 1060", "GTX 1070", "GTX 1080" }, "use", "uses", "does not use", "Does {0} use {1}?", new[] { "a GTX 970", "a GTX 1060", "a GTX 1070", "a GTX 1080" }),
        };

        static XgLogicQuestion Elimination(Random r, int level, bool want)
        {
            int n = level >= 5 ? 4 : 3;
            var g = Grids[r.Next(Grids.Length)];
            var who = Shuffled(r, People.Length).GetRange(0, n);
            var answer = Shuffled(r, n); // person i has item answer[i]
            var perms = Permutations(n);
            // Clues "X 没有 Y" (true in the answer) until one assignment is left; sometimes one "X 是 Y".
            var clues = new List<(int person, int item, bool yes)>();
            if (r.Next(3) == 0) { int p0 = r.Next(n); clues.Add((p0, answer[p0], true)); }
            var possible = new List<(int, int)>();
            for (int p = 0; p < n; p++) for (int i = 0; i < n; i++) if (answer[p] != i) possible.Add((p, i));
            Shuffle(r, possible);
            foreach (var (p, i) in possible)
            {
                if (Count(perms, clues) == 1) break;
                clues.Add((p, i, false));
                // Drop a clue that adds nothing, so the puzzle stays short.
                int before = Count(perms, clues.GetRange(0, clues.Count - 1));
                if (Count(perms, clues) == before) clues.RemoveAt(clues.Count - 1);
            }
            if (Count(perms, clues) != 1) return Conditional(r, level, want);
            Shuffle(r, clues);
            int asker = r.Next(n), askedItem = want ? answer[asker] : (answer[asker] + 1 + r.Next(n - 1)) % n;
            var zh = new StringBuilder(); var en = new StringBuilder();
            var names = new List<string>(); var namesEn = new List<string>(); var items = new List<string>(); var itemsEn = new List<string>();
            for (int k = 0; k < n; k++) { names.Add(People[who[k]]); namesEn.Add(PeopleEn[who[k]]); items.Add(g.items[k]); itemsEn.Add(g.itemsEn[k]); }
            zh.Append(string.Join("、", names)).Append("各").Append(g.verb).Append(string.Join("、", items)).Append("中的一个，互不相同。");
            en.Append(string.Join(", ", namesEn)).Append(" each ").Append(g.eachEn).Append(" one of ").Append(string.Join(", ", itemsEn)).Append(", all different. ");
            foreach (var (p, i, yes) in clues)
            {
                zh.Append(People[who[p]]).Append(yes ? g.verb : g.not).Append(g.items[i]).Append("。");
                en.Append(PeopleEn[who[p]]).Append(' ').Append(yes ? g.yesEn : g.noEn).Append(' ').Append(g.itemsEn[i]).Append(". ");
            }
            var whyZh = new StringBuilder("唯一的分配："); var whyEnB = new StringBuilder("The only assignment: ");
            for (int p = 0; p < n; p++) { whyZh.Append(People[who[p]]).Append("—").Append(g.items[answer[p]]).Append(p + 1 < n ? "，" : "。"); whyEnB.Append(PeopleEn[who[p]]).Append(" – ").Append(g.itemsEn[answer[p]]).Append(p + 1 < n ? ", " : "."); }
            return new XgLogicQuestion
            {
                category = "排除推理", categoryEn = "Elimination",
                text = zh + People[who[asker]] + g.verb + g.items[askedItem] + "吗？",
                textEn = en + string.Format(g.askEn, PeopleEn[who[asker]], g.itemsEn[askedItem]),
                truth = askedItem == answer[asker], why = whyZh.ToString(), whyEn = whyEnB.ToString()
            };
        }

        static List<int[]> Permutations(int n)
        {
            var result = new List<int[]>();
            void Go(int[] a, int k) { if (k == n) { result.Add((int[])a.Clone()); return; } for (int i = k; i < n; i++) { (a[k], a[i]) = (a[i], a[k]); Go(a, k + 1); (a[k], a[i]) = (a[i], a[k]); } }
            var start = new int[n]; for (int i = 0; i < n; i++) start[i] = i;
            Go(start, 0);
            return result;
        }

        static int Count(List<int[]> perms, List<(int person, int item, bool yes)> clues)
        {
            int count = 0;
            foreach (var p in perms)
            {
                bool ok = true;
                foreach (var c in clues) if ((p[c.person] == c.item) != c.yes) { ok = false; break; }
                if (ok) count++;
            }
            return count;
        }

        // ───────────── helpers ─────────────

        static List<int> Shuffled(Random r, int n)
        {
            var list = new List<int>(); for (int i = 0; i < n; i++) list.Add(i);
            Shuffle(r, list); return list;
        }

        static void Shuffle<T>(Random r, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = r.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
        }

        static string Join(List<int> order, string[] names, string sep)
        {
            var parts = new List<string>(); foreach (var i in order) parts.Add(names[i]);
            return string.Join(sep, parts);
        }
    }
}
