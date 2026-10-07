using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>An architecture item that lowers one ability's thresholds (参数量与数据量主线 §6).</summary>
    public sealed class XgAbilityDiscount
    {
        /// <summary>The tech node that gives the discount (one of <see cref="itemsAny"/> is enough when set).</summary>
        public string item = "";
        public string[] itemsAny = new string[0];
        public int ability;
        public double paramsFactor = 1, samplesFactor = 1;
    }

    public sealed partial class XgState
    {
        /// <summary>
        /// Abilities that have emerged, 1–6 in order (ability 1, 是/否, is there from the start). The stage is their
        /// count: nothing else moves it.
        /// </summary>
        public List<int> abilitiesEmerged = new List<int>();
        /// <summary>Trained parameters P, in thousands: the largest model whose assessment reached grade C or better.</summary>
        public double trainedParamsK;
        /// <summary>The calendar's progress through the current stage's month (0–1). It only moves forward.</summary>
        public double monthProgress;
    }

    /// <summary>
    /// The main line (参数量与数据量主线): two bars, trained parameters P and data D. When both reach the next
    /// ability's threshold the ability emerges, for good, and the stage moves to the number of abilities. Architecture
    /// items lower the thresholds. P only grows by training: a model counts once an assessment grades it C or better,
    /// so buying width alone buys nothing. D is every dataset's effective samples, after the noise penalty.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int ProgressionSchemaAbilities = 3;
        public const int AbilityCount = 6;
        /// <summary>The grade a model's assessment needs before its parameters count (C).</summary>
        public const int TrainedGrade = 1;

        /// <summary>Parameter threshold of each ability, in thousands (index = ability; 1 is there from the start).</summary>
        public static readonly double[] AbilityParamsK = { 0, 0, 8, 150, 3000, 30000, 100000 };
        /// <summary>Data threshold of each ability, in effective samples (a data pack holds 6 thousand to 5 million).</summary>
        public static readonly double[] AbilitySamples = { 0, 0, 12500, 150000, 500000, 1500000, 6000000 };

        /// <summary>Architecture items and the thresholds they lower (plan §6). Without them every ability still emerges, later.</summary>
        public static readonly XgAbilityDiscount[] AbilityDiscounts =
        {
            new XgAbilityDiscount { item = "mlp", ability = 2, paramsFactor = .8 },
            new XgAbilityDiscount { itemsAny = new[] { "lenet", "alexnet" }, ability = 3, paramsFactor = .7, samplesFactor = .7 },
            new XgAbilityDiscount { item = "rnn", ability = 4, paramsFactor = .8, samplesFactor = .8 },
            new XgAbilityDiscount { item = "lstm", ability = 5, samplesFactor = .8 },
            new XgAbilityDiscount { item = "attention", ability = 5, paramsFactor = .7 },
            new XgAbilityDiscount { item = "transformer", ability = 6, paramsFactor = .6, samplesFactor = .6 },
        };

        /// <summary>What each ability opens (the ability table), index = ability.</summary>
        public static readonly string[] AbilityUnlocks =
        {
            "", "逻辑桌、垃圾短信；摆渡众包手标", "手写数字桌、第一批企业订单、模型仓库", "验证码、表情包桌；它在 YY 里说出第一句话",
            "弹幕、标题党、刷单评论；它记住你说过的话", "翻译订单、直播字幕；乱码逐行读出；晴雯代回", "预训练，然后终章",
        };
        public static readonly string[] AbilityUnlocksEn =
        {
            "", "Logic and SMS spam desks; hand labelling on Bodu Crowd", "Digit desk, the first company orders, the model repository",
            "Captcha and meme desks; its first words in YY", "Danmaku, clickbait and fake-review desks; it remembers what you said",
            "Translation and live-caption orders; the garbled page, line by line; it answers Qingwen for you", "Pre-training, then the finale",
        };

        /// <summary>An ability emerged (1–6). Fired after the stage has moved.</summary>
        public event Action<int> AbilityEmerged;

        /// <summary>A parameter count given in thousands, the way the bars show it: 820K, 12.4M, 1.2B.</summary>
        public static string ParamsText(double paramsK)
        {
            if (!(paramsK > 0)) return "0";
            if (paramsK < 1000) return F(paramsK, paramsK < 10 ? "0.#" : "0") + "K";
            if (paramsK < 1000000) return F(paramsK / 1000, paramsK < 10000 ? "0.#" : "0") + "M";
            return F(paramsK / 1000000, "0.#") + "B";
        }

        /// <summary>A sample count the way the bars show it: 9,800 or 1.2M.</summary>
        public static string SamplesText(double samples)
        {
            if (!(samples > 0)) return "0";
            if (samples < 1000000) return Math.Floor(samples).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
            return F(samples / 1000000, "0.#") + "M";
        }

        public static string AbilityName(int ability, bool english)
        {
            if (ability < 1 || ability > AbilityCount) return "";
            return english ? AbilitiesEn[ability - 1] : Abilities[ability - 1];
        }

        public bool HasAbility(int ability) => ability <= 1 || S.abilitiesEmerged != null && S.abilitiesEmerged.Contains(ability);
        public int AbilitiesCount { get { int n = 1; while (n < AbilityCount && HasAbility(n + 1)) n++; return n; } }
        /// <summary>The next ability to emerge, 0 once all six have.</summary>
        public int NextAbility => AbilitiesCount < AbilityCount ? AbilitiesCount + 1 : 0;

        // ───────────── the two bars ─────────────

        /// <summary>Trained parameters P (thousands).</summary>
        public double TrainedParamsK => Finite(S.trainedParamsK) ? Math.Max(0, S.trainedParamsK) : 0;

        /// <summary>
        /// Data D: effective samples of every dataset (packs, labels, crowd rows, logs), minus the noise penalty, each
        /// dataset at its <see cref="XgDataset.dataWeight"/> (算术 counts half).
        /// </summary>
        public double TrainedSamples
        {
            get
            {
                double d = 0;
                foreach (var ds in XgCatalog.Datasets)
                {
                    if (!CountsAsData(ds.id)) continue;
                    d += ds.dataWeight * Math.Max(0, Samples(ds.id) - NoisePenalty * (Noise(ds.id) + DataNoise(ds.id)));
                }
                return d;
            }
        }

        /// <summary>Generated task sets (异或, 串行瓶颈) are not data anyone gathered.</summary>
        static bool CountsAsData(string dataset) => dataset != "xor" && dataset != "parallel";

        public double ParamsDiscount(int ability) => Discount(ability, true);
        public double SamplesDiscount(int ability) => Discount(ability, false);

        double Discount(int ability, bool parameters)
        {
            double f = 1;
            foreach (var d in AbilityDiscounts)
                if (d.ability == ability && DiscountOwned(d)) f *= parameters ? d.paramsFactor : d.samplesFactor;
            return f;
        }

        public bool DiscountOwned(XgAbilityDiscount d)
        {
            if (d.item.Length > 0 && Has(d.item)) return true;
            foreach (var id in d.itemsAny) if (Has(id)) return true;
            return false;
        }

        /// <summary>The discounts an item gives (the tech page shows them on the item).</summary>
        public static List<XgAbilityDiscount> DiscountsOf(string item)
        {
            var list = new List<XgAbilityDiscount>();
            foreach (var d in AbilityDiscounts)
                if (d.item == item || Array.IndexOf(d.itemsAny, item) >= 0) list.Add(d);
            return list;
        }

        /// <summary>The parameter threshold of an ability after the items owned (thousands).</summary>
        public double ParamsThreshold(int ability) => ability < 1 || ability > AbilityCount ? 0 : AbilityParamsK[ability] * ParamsDiscount(ability);
        public double SamplesThreshold(int ability) => ability < 1 || ability > AbilityCount ? 0 : AbilitySamples[ability] * SamplesDiscount(ability);

        /// <summary>How full the parameter bar is towards an ability (0–1).</summary>
        public double ParamsProgress(int ability) => BarProgress(TrainedParamsK, ParamsThreshold(ability));
        public double SamplesProgress(int ability) => BarProgress(TrainedSamples, SamplesThreshold(ability));
        /// <summary>The slower of the two bars towards an ability (0–1).</summary>
        public double AbilityProgress(int ability) => Math.Min(ParamsProgress(ability), SamplesProgress(ability));

        static double BarProgress(double have, double need) => need <= 0 ? 1 : Clamp01(have / need);

        /// <summary>The parameters (thousands) of the model a run trains (configured automatically, so always the one assessed).</summary>
        public static double TrainedShapeParamsK(XgRun run) => run == null ? 0 : ParamsK(run);

        /// <summary>After an assessment: a model graded C or better counts towards P, once it has trained a round in its current shape.</summary>
        void NoteTrainedParams(XgRun run, XgAssessment a)
        {
            if (run == null || a == null || a.grade < TrainedGrade || run.shapeRounds <= 0) return;
            double k = TrainedShapeParamsK(run);
            if (Finite(k) && k > S.trainedParamsK) S.trainedParamsK = k;
        }

        // ───────────── emergence ─────────────

        /// <summary>Emerges the next ability once both bars are full (one per call, so each gets its moment).</summary>
        void CheckAbilities()
        {
            if (S.stage != AbilitiesCount) RefreshStages();
            int next = NextAbility;
            if (next == 0) return;
            if (TrainedParamsK + 1e-9 < ParamsThreshold(next) || TrainedSamples + 1e-9 < SamplesThreshold(next)) return;
            EmergeAbility(next);
        }

        void EmergeAbility(int ability)
        {
            if (HasAbility(ability) || ability != NextAbility) return;
            S.abilitiesEmerged.Add(ability);
            // 笨办法: it got here on scale alone, with none of its architecture discounts.
            if (ParamsDiscount(ability) >= 1 && SamplesDiscount(ability) >= 1 && !S.insights.Contains(ScaleInsight + ability)) S.insights.Add(ScaleInsight + ability);
            Say(T("学会了：", "It learned: ") + AbilityName(ability, English));
            // The stage follows: the old stage change (its desks, words and story) happens now.
            AdvanceStage(ability - 1);
            RefreshStages();
            AbilityEmerged?.Invoke(ability);
        }

        /// <summary>Marks abilities 1…n emerged without their moments (old saves, a stage set from outside).</summary>
        void GrantAbilitiesUpTo(int n)
        {
            if (S.abilitiesEmerged == null) S.abilitiesEmerged = new List<int>();
            n = Math.Max(1, Math.Min(AbilityCount, n));
            for (int i = 1; i <= n; i++) if (!S.abilitiesEmerged.Contains(i)) S.abilitiesEmerged.Add(i);
            S.abilitiesEmerged.Sort();
        }

        /// <summary>
        /// Moves an old save onto the two bars without taking anything back: every ability up to its stage has
        /// emerged, the breakthroughs it bought are the items they unlocked, its graded checkpoints count towards P,
        /// and the story it already saw is not played again.
        /// </summary>
        void MigrateToAbilities()
        {
            GrantAbilitiesUpTo(S.stage);
            if (Has("bt.hidden")) Grant("mlp");
            if (Has("bt.vision")) Grant("lenet");
            if (Has("bt.sequence")) Grant("rnn");
            if (Has("bt.gate")) Grant("lstm");
            if (Has("bt.residual")) Grant("resnet");
            if (Has("bt.attention")) Grant("seq2seq");
            if (Has("bt.spatial")) Grant("caption");
            if (Has("project.transformer")) Grant("transformer");
            foreach (var m in S.models)
            {
                if (m == null || Grade(m.score) < TrainedGrade) continue;
                double k = ParamsK(new XgRun { arch = m.arch, depth = Math.Max(1, m.depth), width = Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, m.width)) });
                if (Finite(k) && k > S.trainedParamsK) S.trainedParamsK = k;
            }
            foreach (var wall in new[] { "combo", "structure", "length", "degrade", "translation", "parallel" }) MigrateBeat("wall_" + wall);
            for (int i = 2; i <= S.stage; i++) MigrateBeat("emerge_" + i);
            // 它的第一个选择 came with the stage-2 wall; a stage-2 save that never saw it still gets it.
            if (S.stage >= 3 || S.walls.Contains("structure")) MigrateBeat("specialty_motivation");
            if (S.walls.Contains("structure")) MigrateBeat("s2_moved");
        }
    }
}
