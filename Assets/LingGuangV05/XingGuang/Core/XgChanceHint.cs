using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>Which knob most likely keeps a run at chance.</summary>
    public enum XgChanceCause { None, RateTooHigh, StepActivation, Architecture, TooNarrow, RateTooLow, Unknown }

    public sealed partial class XgRun
    {
        /// <summary>Settings the chance hint last saw, and the epoch they were first seen at (not saved).</summary>
        [NonSerialized] internal string chanceSettings;
        [NonSerialized] internal int chanceFrom;
    }

    /// <summary>
    /// The guide note's step for a model that only guesses: when a run's test accuracy has stayed near chance (see
    /// <see cref="XgDataset.chanceError"/>) for several epochs with the same settings, name the knob that is most
    /// likely wrong and point at its control on the training page. Read-only, like <see cref="XgGuide"/>.
    /// While a wall stands on the run's dataset, it only speaks once the run already has the wall's golden
    /// setting, so it never gives that setting away.
    /// </summary>
    public static class XgChanceHint
    {
        /// <summary>Epochs in a row, with unchanged settings, before the note speaks.</summary>
        public const int ChanceEpochs = 6;
        /// <summary>"Near chance": on average this little of the way from chance to the dataset's best, never above <see cref="ChanceSpike"/>.</summary>
        public const double ChanceShare = .2, ChanceSpike = .35;

        /// <summary>How far an accuracy sits from chance (0) towards the dataset's floor error (1).</summary>
        public static double Progress(string dataset, double accuracy)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null) return accuracy;
            double span = d.chanceError - d.floorError;
            return span <= 1e-9 ? 1 : (accuracy - (1 - d.chanceError)) / span;
        }

        static string Settings(XgSim sim, XgRun run)
            => run.dataset + "|" + run.arch + "|" + sim.EffectiveActivation(run) + "|" + run.depth + "|" + run.width + "|" + run.lr
               + "|" + run.clip + "|" + run.skip + "|" + run.position + "|" + run.attnOnly;

        /// <summary>Epochs in a row at chance with the current settings (0 when the latest one is not at chance).</summary>
        public static int EpochsAtChance(XgSim sim, XgRun run)
        {
            if (sim == null || run == null || run.histVal == null) return 0;
            string now = Settings(sim, run);
            if (run.chanceSettings == null)
            {
                // First look (fresh save or new session): trust the history if nothing changed since the last real epoch.
                run.chanceSettings = now;
                run.chanceFrom = sim.KnobChanges(run).Count == 0 ? run.epoch - run.histVal.Count : run.epoch;
            }
            else if (run.chanceSettings != now) { run.chanceSettings = now; run.chanceFrom = run.epoch; }
            int usable = Math.Min(run.histVal.Count, Math.Max(0, run.epoch - run.chanceFrom));
            if (Progress(run.dataset, run.valAcc) >= ChanceSpike) return 0;
            int n = 0;
            for (int i = run.histVal.Count - 1; i >= run.histVal.Count - usable; i--)
            {
                if (Progress(run.dataset, run.histVal[i]) >= ChanceSpike) break;
                n++;
            }
            if (n < ChanceEpochs) return n;
            double sum = 0;
            for (int i = run.histVal.Count - ChanceEpochs; i < run.histVal.Count; i++) sum += Progress(run.dataset, run.histVal[i]);
            return sum / ChanceEpochs < ChanceShare ? n : 0;
        }

        /// <summary>The knob most likely at fault, in the order a teacher would check them.</summary>
        public static XgChanceCause Cause(XgSim sim, XgRun run)
        {
            if (sim == null || run == null) return XgChanceCause.None;
            var k = sim.Knobs(run);
            double rate = k.lr;
            if (rate * 1.5 > k.TearAt) return XgChanceCause.RateTooHigh;
            if (k.activation == XgActivation.Step && k.depth >= 2) return XgChanceCause.StepActivation;
            if (BetterArchitecture(sim, run) != null) return XgChanceCause.Architecture;
            int used = sim.Board.Count(XgSim.RegionOf(run.dataset));
            if (used >= k.Cells) return XgChanceCause.TooNarrow;
            if (rate <= .01 + 1e-9 && used < k.Cells / 2) return XgChanceCause.RateTooLow;
            return XgChanceCause.Unknown;
        }

        static readonly HashSet<string> OrderedText = new HashSet<string> { "danmu", "poems", "news", "longtext", "crosssentence" };

        /// <summary>An owned architecture whose wiring suits the desk when the current one is fully connected (null if none).</summary>
        public static XgArch BetterArchitecture(XgSim sim, XgRun run)
        {
            if (XgSim.WiringOf(run.arch) != XgWiring.Full) return null;
            var track = (XgTrack)run.track;
            bool images = track == XgTrack.Vision, ordered = OrderedText.Contains(run.dataset);
            if (!images && !ordered) return null;
            foreach (var a in XgCatalog.Archs)
            {
                if (!XgSim.ArchitectureFits(a, track) || !sim.Has(a.id)) continue;
                var w = XgSim.WiringOf(a.id);
                if (images ? w == XgWiring.LocalShared : w == XgWiring.Recurrent || w == XgWiring.GatedRecurrent) return a;
            }
            return null;
        }

        /// <summary>
        /// True when a wall that stands (or is about to: the current stage's own wall) checks this dataset and the run
        /// does not have its golden setting yet. The wall's own hints (diagnosis, forum, secret) cover that case, and
        /// this note must not give the setting away. Exception: a sure cause (rate or step activation) whose fix
        /// alone still falls short of the golden setting may be named, since it gives nothing away.
        /// </summary>
        public static bool WallKeepsQuiet(XgSim sim, XgRun run, XgChanceCause cause)
        {
            var k = sim.Knobs(run);
            var fixedRun = Fixed(sim, run, cause);
            XgKnobs fixedKnobs = null;
            if (fixedRun != null)
            {
                fixedKnobs = sim.Knobs(fixedRun);
                // Judge the fix as if its knob were owned already (buying it is part of the advice).
                if (cause == XgChanceCause.StepActivation) fixedKnobs.activation = (XgActivation)fixedRun.act;
            }
            foreach (var wall in XgSim.Walls)
            {
                bool current = wall.stage == sim.S.stage && !wall.extra;
                if (!sim.WallSeen(wall.id) && !current || sim.WallPassed(wall) || wall.stage > sim.S.stage) continue;
                foreach (var check in wall.checks)
                {
                    if (sim.WallCheckPassed(wall, check)) continue;
                    bool matches = check.dataset == "*vision" ? run.track == 0 && (check.condition == null || check.condition(run, k)) : check.dataset == run.dataset;
                    if (!matches || check.golden == null || check.golden(sim, run, k)) continue;
                    if (fixedRun == null || check.golden(sim, fixedRun, fixedKnobs)) return true;
                }
            }
            return false;
        }

        /// <summary>The run with only the named fix applied, for the sure causes (null for the others).</summary>
        static XgRun Fixed(XgSim sim, XgRun run, XgChanceCause cause)
        {
            var r = new XgRun
            {
                track = run.track, dataset = run.dataset, arch = run.arch, act = sim.EffectiveActivation(run), depth = run.depth, width = run.width, lr = run.lr,
                clip = run.clip, skip = run.skip, position = run.position, warmup = run.warmup, attnOnly = run.attnOnly,
            };
            switch (cause)
            {
                case XgChanceCause.RateTooHigh: r.lr = Math.Min(XgCatalog.LearningRates.Length - 1, run.lr + 1); return r;
                case XgChanceCause.RateTooLow: r.lr = Math.Max(0, run.lr - 1); return r;
                case XgChanceCause.StepActivation: r.act = sim.ActivationOwned(2) ? 2 : 1; return r;
                default: return null;
            }
        }

        /// <summary>Adds the step for the first trainable run that only guesses (the selected track first).</summary>
        public static void Add(XgSim sim, List<XgGuideStep> list)
        {
            var step = Step(sim);
            if (step != null) list.Add(step);
        }

        public static XgGuideStep Step(XgSim sim)
        {
            if (sim == null || sim.S.stage >= 6) return null;
            foreach (var track in new[] { sim.SelectedTrack, sim.SelectedTrack == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision })
            {
                var run = sim.Run(track);
                if (!sim.TrainingUnlocked(track) || run.epoch == 0) continue;
                int epochs = EpochsAtChance(sim, run);
                if (epochs < ChanceEpochs) continue;
                var cause = Cause(sim, run);
                if (WallKeepsQuiet(sim, run, cause)) continue;
                return StepFor(sim, run, cause, epochs);
            }
            return null;
        }

        static string Pct(double v) => Math.Round(v * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

        static XgGuideStep StepFor(XgSim sim, XgRun run, XgChanceCause cause, int epochs)
        {
            var track = (XgTrack)run.track;
            var d = XgCatalog.Dataset(run.dataset);
            string data = d != null ? d.name : run.dataset, dataEn = d != null ? d.nameEn : run.dataset;
            string chance = d != null ? Pct(1 - d.chanceError) : "";
            string seen = "「" + data + "」测试准确率 " + Pct(run.valAcc) + "，瞎猜也有 " + chance + "，已经 " + epochs + " 轮没起色。";
            string seenEn = dataEn + " scores " + Pct(run.valAcc) + " on test cards; guessing gets " + chance + ". No better for " + epochs + " epochs.";
            string arg = track == XgTrack.Vision ? "vision" : "sequence";
            string id = "chance." + cause.ToString().ToLowerInvariant();
            var k = sim.Knobs(run);
            bool lrKnob = sim.HasLrKnob(track);
            switch (cause)
            {
                case XgChanceCause.RateTooHigh:
                {
                    int lower = Math.Min(XgCatalog.LearningRates.Length - 1, run.lr + 1);
                    string to = sim.RateLabel(lower);
                    return lrKnob
                        ? Make(id, "模型只会瞎猜：学习率 " + sim.RateLabel(run.lr) + " 太大，调到 " + to, "The model only guesses: learning rate " + sim.RateLabel(run.lr) + " is too big, set it to " + to,
                            seen + "每一步都改过头，学到的又被冲掉。", seenEn + " Every step overshoots and wipes out what it learnt.", "train", "label:" + to, arg)
                        : Make(id, "模型只会瞎猜：学习率太大，科技买「学习率旋钮」", "The model only guesses: the rate is too big, buy the learning-rate knob in the tree",
                            seen + "每一步都改过头。", seenEn + " Every step overshoots.", "tree", "node:shared.lr", "");
                }
                case XgChanceCause.StepActivation:
                {
                    int act = sim.ActivationOwned(2) ? 2 : sim.ActivationOwned(1) ? 1 : 0;
                    string why = seen + "阶跃没有坡度，误差传不回下层，多层的格子合不成新概念。", whyEn = seenEn + " A step has no slope: the error cannot reach the lower layer, so layers cannot merge concepts.";
                    if (act == 0)
                        return Make(id, "模型只会瞎猜：激活是「阶跃」，去科技买「S 形」激活", "The model only guesses: the activation is a step, buy the S-curve in the tech tree",
                            why, whyEn, "tree", "node:sigmoid", "");
                    string name = act == 2 ? "ReLU" : "S 形", nameEn = act == 2 ? "ReLU" : "S-curve";
                    return Make(id, "模型只会瞎猜：激活从「阶跃」换成「" + name + "」", "The model only guesses: switch the activation from step to " + nameEn,
                        why, whyEn, "train", "label:" + name + "|" + nameEn, arg);
                }
                case XgChanceCause.Architecture:
                {
                    var a = BetterArchitecture(sim, run);
                    bool images = track == XgTrack.Vision;
                    return Make(id, "模型只会瞎猜：全连接不适合这张桌，换「" + a.name + "」", "The model only guesses: fully connected does not suit this desk, switch to " + a.nameEn,
                        seen + (images ? "全连接把每个像素位置当成新东西，字挪一格就不认识了；局部共享在哪儿都通用。" : "全连接把每个字的位置绑死，整句挪一格就成了新句子；回环一个字一个字地读，哪儿出现都认得。"),
                        seenEn + (images ? " Full wiring treats every pixel position as new, so a shifted digit is a stranger; local sharing works anywhere." : " Full wiring binds every word to its position, so the same sentence moved by one word is new to it; a loop reads word by word and knows them anywhere."),
                        "train", "label:" + a.name + "|" + a.nameEn, arg);
                }
                case XgChanceCause.TooNarrow:
                {
                    int cap = sim.WidthCap(track);
                    string now = XgCatalog.Widths[run.width].ToString(CultureInfo.InvariantCulture);
                    string why = seen + "格子 " + k.Cells + "/" + k.Cells + " 全占满了：宽度太小，装不下这张桌要记的样子。", whyEn = seenEn + " All " + k.Cells + " cells are full: the width is too small for what this desk needs to hold.";
                    if (run.width < cap)
                    {
                        string to = XgCatalog.Widths[run.width + 1].ToString(CultureInfo.InvariantCulture);
                        return Make(id, "模型只会瞎猜：宽度 " + now + " 太窄，在训练页加宽到 " + to, "The model only guesses: width " + now + " is too narrow, widen it to " + to + " on the training page",
                            why, whyEn, "train", "name:Width", arg);
                    }
                    var node = NextWidthNode(sim, track);
                    return node != null
                        ? Make(id, "模型只会瞎猜：宽度 " + now + " 太窄，科技买「" + node.name + "」", "The model only guesses: width " + now + " is too narrow, buy " + node.nameEn + " in the tech tree",
                            why, whyEn, "tree", "node:" + node.id, "")
                        : Make(id, "模型只会瞎猜：宽度已到顶，少一层或换个结构", "The model only guesses: the width is maxed, try fewer layers or another structure",
                            why, whyEn, "train", "name:Width", arg);
                }
                case XgChanceCause.RateTooLow:
                {
                    int higher = Math.Max(0, run.lr - 1);
                    string to = sim.RateLabel(higher);
                    return Make(id, "模型只会瞎猜：学习率 " + sim.RateLabel(run.lr) + " 太小，调到 " + to, "The model only guesses: learning rate " + sim.RateLabel(run.lr) + " is too small, set it to " + to,
                        seen + "每一步只挪一点点，格子都还没长出来。", seenEn + " Each step barely moves; the cells have not even grown yet.", "train", lrKnob ? "label:" + to : "name:LrText", arg);
                }
                default:
                    return Make(id, "模型只会瞎猜：一次只改一个旋钮，再练几轮看看", "The model only guesses: change one knob at a time and train a few epochs",
                        seen + "先试宽度，再试学习率；一次改一个，才知道是哪个起了作用。", seenEn + " Try the width first, then the rate; one change at a time shows which one mattered.",
                        "train", "name:Width", arg);
            }
        }

        /// <summary>The cheapest width node not owned yet that raises this track's width cap.</summary>
        static XgNode NextWidthNode(XgSim sim, XgTrack track)
        {
            int cap = sim.WidthCap(track);
            string tree = XgSim.TreeOf(track);
            XgNode best = null;
            foreach (var n in XgCatalog.Nodes)
                if (n.kind == XgNodeKind.Width && (n.tree == tree || n.tree == "trunk") && n.value > cap && !sim.Has(n.id) && (best == null || n.value < best.value)) best = n;
            return best;
        }

        static XgGuideStep Make(string id, string zh, string en, string whyZh, string whyEn, string tab, string target, string arg)
            => new XgGuideStep { id = id, kind = XgGuideKind.Main, zh = zh, en = en, whyZh = whyZh, whyEn = whyEn, app = XgGuide.Lab, tab = tab, target = target, arg = arg };
    }
}
