using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>0 = a save from the knob era (the player set width, depth, rate…); 1 = the model is configured automatically.</summary>
        public int trainingVersion;
        /// <summary>Rounds that went down instead of up, over the whole save.</summary>
        public int drops;
    }

    public sealed partial class XgRun
    {
        /// <summary>
        /// Accuracy per dataset of this track's model (the run trains one dataset at a time; switching back finds the
        /// progress where it was left).
        /// </summary>
        public List<XgScore> accs = new List<XgScore>();
        /// <summary>What the last real round changed (accuracy units; negative after a drop).</summary>
        public double lastGain;
        /// <summary>Real rounds trained since the model last changed shape. A model counts towards P only after one.</summary>
        public int shapeRounds;
        /// <summary>The highest accuracy this model can reach on its dataset (set by <see cref="XgSim.Evaluate"/>).</summary>
        [NonSerialized] public double ceiling;
        /// <summary>The share of the way to the ceiling the last round would have covered (not saved).</summary>
        [NonSerialized] public double lastRate;
        /// <summary>The automatic configuration was first applied after loading an older save (no progress is lost then).</summary>
        [NonSerialized] internal bool autoAdopted;
        /// <summary>What the last automatic choice was made from (not saved).</summary>
        [NonSerialized] internal string autoKey;
    }

    /// <summary>What holds a model's accuracy back (the plateau's cause).</summary>
    public enum XgLimit { None, Params, Data, Structure, Noise, Done }

    /// <summary>The chance that a round goes down, with its parts and the reason to show.</summary>
    public sealed class XgDropRisk
    {
        public double chance, overfit, noise, drift, jump, wobble;
        /// <summary>0 低 / 1 中 / 2 高.</summary>
        public int level;
        public string reason = "", reasonEn = "";
    }

    /// <summary>
    /// Training without knobs (训练不再需要调参数). The model is configured automatically: the best architecture owned for
    /// the track, the largest width and depth the bought caps allow that still fit the card, and every technique owned
    /// switched on. Each round moves the accuracy towards a ceiling set by parameters and data (the scaling law of
    /// <see cref="CapacityFraction"/>, placed on the game's parameter ladder by <see cref="CeilingScaleK"/>), so a model
    /// that is big enough keeps improving and a small one plateaus.
    /// A round can also go down a little instead, more often when the data is scarce for the model's size (it
    /// memorises the answers), when the labels are noisy or the memes drift, and right after a big jump; Dropout,
    /// augmentation, BatchNorm, warm-up and clipping make it rarer. A drop only lowers that run's accuracy: the best
    /// checkpoint, the grades, the trained parameters P and every learned ability stay.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int TrainingSchemaAuto = 1;
        /// <summary>Gain per round: κ × cards fed ÷ τ of the curve, between the bounds below.</summary>
        public const double GainPerCard = 1.5, GainMin = .06, GainMax = .3;
        /// <summary>Within this many score points of its ceiling a model has plateaued (再练也涨不动).</summary>
        public const double PlateauPoints = 5;
        /// <summary>…or when a round is expected to add less than this many points net of drops (再练也涨不动).</summary>
        public const double PlateauNetPoints = 1.5;
        /// <summary>The size of a drop: this share of the dataset's range, plus up to <see cref="DropSpread"/> more (5–15 score points).</summary>
        public const double DropMin = .005, DropSpread = .01;
        /// <summary>The steady chance of a drop, and its cap.</summary>
        public const double DropWobble = .03, DropMax = .45;
        /// <summary>Risk levels: below <see cref="RiskMedium"/> is 低, below <see cref="RiskHigh"/> 中, else 高.</summary>
        public const double RiskMedium = .08, RiskHigh = .18;
        /// <summary>Progress above chance a model keeps when it grows (Net2Net-style), and with transfer learning.</summary>
        public const double GrowKeep = .7, GrowKeepTransfer = .9;

        // ───────────── automatic configuration ─────────────

        /// <summary>
        /// Memory a training line can use for a model of this size: the best wired, powered card after the models
        /// resident on it, times the server room; half of it when the other line trains on the same card, so both
        /// fit at once. 本体 (which is the sequence model) is counted at the candidate's size, so the choice does not
        /// chase itself.
        /// </summary>
        public double AutoVram(XgRun run, IXgHost host, double candidateParamsK)
        {
            var cards = AutoCards(run, host, out double all);
            if (cards == null) return all;
            double max = 0;
            foreach (var c in cards) max = Math.Max(max, c.Free(candidateParamsK));
            return max;
        }

        /// <summary>A card a training line can use: its memory after the resident models other than 本体, and whether 本体 sits on it.</summary>
        struct AutoCard
        {
            public double free, multiplier;
            public bool self;
            public double Free(double candidateParamsK) => Math.Max(0, free - (self ? ModelVram(candidateParamsK) : 0)) * multiplier;
        }

        /// <summary>The line's usable cards, or null when there is no wiring (then <paramref name="all"/> is the whole budget).</summary>
        List<AutoCard> AutoCards(XgRun run, IXgHost host, out double all)
        {
            all = host == null ? 0 : Vram(host);
            if (host == null) return null;
            var view = wiringView;
            if (host is IXgRig && (view == null || !ReferenceEquals(wiringHost, host))) view = RefreshWiring(host);
            if (view == null || !view.hasRig) return null;
            var line = view.Node("run:" + run.track);
            if (line == null) return null;
            var self = run.track == (int)XgTrack.Sequence ? view.Node("self") : null;
            // Both lines train on the same cards: each sizes its model to half of a shared card, so neither waits.
            var other = TrainingUnlocked((XgTrack)(1 - run.track)) ? view.Node("run:" + (1 - run.track)) : null;
            var list = new List<AutoCard>();
            foreach (string key in line.cards)
            {
                var c = view.Node(key);
                if (c == null || !c.online) continue;
                bool here = self != null && self.card == c.key;
                double free = c.vramMB - c.residentVram + (here ? ModelVram(self.paramsK) : 0);
                if (other != null && other.cards.Contains(key)) free -= Math.Max(0, c.vramMB - c.residentVram) / 2;
                list.Add(new AutoCard { free = free, self = here, multiplier = TrainingMemoryMultiplier });
            }
            return list;
        }

        /// <summary>The largest model of this architecture the caps allow that fits the card (the smallest one if none fits).</summary>
        public XgRun AutoShape(XgRun run, string arch, IXgHost host)
        {
            double all;
            var cards = host == null ? null : AutoCards(run, host, out all);
            all = host == null ? double.PositiveInfinity : cards == null ? Vram(host) : 0;
            return AutoShape(run, arch, cards, all);
        }

        XgRun AutoShape(XgRun run, string arch, List<AutoCard> cards, double all)
        {
            var track = (XgTrack)run.track;
            var probe = new XgRun { track = run.track, arch = arch, dataset = run.dataset };
            int depthCap = Math.Max(1, Math.Min(DepthCap(track), MaxDepth(probe))), widthCap = Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, WidthCap(track)));
            XgRun best = null; double bestK = -1, bestVram = 0;
            for (int d = 1; d <= depthCap; d++)
                for (int w = 0; w <= widthCap; w++)
                {
                    var c = new XgRun { track = run.track, arch = arch, dataset = run.dataset, depth = d, width = w };
                    double k = ParamsK(c), need = VramNeedMB(c);
                    if (!Fits(need, k, cards, all)) continue;
                    // Same parameters (4 layers × w² = 1 layer × (2w)²): the one that needs less memory.
                    if (k > bestK + 1e-9 || Math.Abs(k - bestK) <= 1e-9 && need < bestVram) { best = c; bestK = k; bestVram = need; }
                }
            return best ?? new XgRun { track = run.track, arch = arch, dataset = run.dataset, depth = 1, width = 0 };
        }

        static bool Fits(double need, double paramsK, List<AutoCard> cards, double all)
        {
            if (cards == null) return need <= all + 1e-6;
            foreach (var c in cards) if (need <= c.Free(paramsK) + 1e-6) return true;
            return false;
        }

        /// <summary>
        /// The architecture the track trains with: of the owned ones, the one whose largest fitting model reaches the
        /// best grade on this dataset; then the one with more parameters (it grows P); then the higher ceiling; then the
        /// newer idea (the lower bias of the curve).
        /// </summary>
        public XgRun BestAutoModel(XgRun run, IXgHost host)
        {
            var track = (XgTrack)run.track;
            double all;
            var cards = host == null ? null : AutoCards(run, host, out all);
            all = host == null ? double.PositiveInfinity : cards == null ? Vram(host) : 0;
            XgRun best = null; int bestGrade = -1; double bestK = -1, bestScore = -1;
            foreach (var a in XgCatalog.Archs)
            {
                if (!ArchitectureFits(a, track) || !Has(a.id)) continue;
                var shape = AutoShape(run, a.id, cards, all);
                double score = Score(run.dataset, CeilingAcc(shape)), k = ParamsK(shape);
                int grade = Grade(score);
                bool better = grade > bestGrade || grade == bestGrade && (k > bestK + 1e-9 || Math.Abs(k - bestK) <= 1e-9
                    && (score > bestScore + 1e-9 || Math.Abs(score - bestScore) <= 1e-9 && best != null && a.bias < XgCatalog.Arch(best.arch).bias));
                if (better) { best = shape; bestGrade = grade; bestK = k; bestScore = score; }
            }
            return best ?? AutoShape(run, DefaultArchitecture(track), cards, all);
        }

        /// <summary>What the automatic choice depends on; unchanged, the last choice stands (it runs every tick).</summary>
        string AutoKey(XgRun run, IXgHost host)
        {
            var cards = AutoCards(run, host, out double all);
            var sb = new System.Text.StringBuilder();
            sb.Append(S.unlocked.Count).Append('|').Append(run.dataset).Append('|').Append(run.arch).Append('|').Append(run.depth).Append('|').Append(run.width)
              .Append('|').Append(Math.Round(Math.Log(1 + SamplesEffective(run)) * 4)).Append('|').Append(Math.Round(InbreedingPenaltyFor(run.dataset) * 200))
              .Append('|').Append(AutoTrainLevel).Append('|').Append(Math.Round(all));
            if (cards != null) foreach (var c in cards) sb.Append('|').Append(Math.Round(c.free)).Append(c.self ? "s" : "");
            return sb.ToString();
        }

        /// <summary>Every owned technique is on; the rate is the largest that cannot tear the board.</summary>
        void AutoTechniques(XgRun run)
        {
            run.act = -1;
            run.clip = ClipOwned;
            run.skip = SkipOwned;
            run.position = PositionOwned;
            run.warmup = WarmupOwned;
            run.batchNormOff = false;
            // 特征工程 is the road before networks: people make the features for a model that cannot make its own.
            run.features = FeaturesOwned && run.arch == "perceptron";
            run.attnOnly = false;
            run.autoLr = true;
            run.lr = SafeLr(run);
        }

        /// <summary>
        /// Applies the automatic configuration to a run between rounds. A new architecture is a new model (the region
        /// starts over unless transfer learning carries it); a bigger one of the same kind keeps most of its progress.
        /// Returns true when the shape changed.
        /// </summary>
        public bool AutoConfigure(XgRun run, IXgHost host)
        {
            if (run == null || run.epochActive || host == null) return false;
            string key = AutoKey(run, host);
            if (key == run.autoKey && run.autoAdopted) return false;
            var target = BestAutoModel(run, host);
            bool archChanged = target.arch != run.arch, depthChanged = target.depth != run.depth, widthChanged = target.width != run.width;
            if (archChanged || depthChanged || widthChanged)
            {
                // A save from the knob era adopts its automatic model as it is: nothing it learnt goes back.
                bool adopt = !run.autoAdopted && S.trainingVersion < TrainingSchemaAuto;
                if (archChanged && !adopt) ChangeArchitecture(run, target.arch);
                else run.arch = target.arch;
                if (!archChanged && depthChanged && !adopt) ReinitialiseBoard(run);
                run.depth = target.depth; run.width = target.width;
                if (!archChanged && !adopt) Grow(run);
                if (!adopt) run.shapeRounds = 0;
            }
            run.autoAdopted = true;
            AutoTechniques(run);
            Evaluate(run);
            run.autoKey = AutoKey(run, host);
            return archChanged || depthChanged || widthChanged;
        }

        void AutoConfigureAll(IXgHost host)
        {
            foreach (var run in Runs) AutoConfigure(run, host);
            if (host != null && S.trainingVersion < TrainingSchemaAuto) S.trainingVersion = TrainingSchemaAuto;
        }

        /// <summary>A new structure is a new model: the region starts over, unless transfer learning carries the bottom across.</summary>
        void ChangeArchitecture(XgRun run, string id)
        {
            var a = XgCatalog.Arch(id);
            run.arch = id;
            KeepProgress(run, Has("transfer") ? .6 : 0);
            run.sinceEval = 0; run.staleEvals = 0;
            run.histTrain.Clear(); run.histVal.Clear();
            string region = RegionOf(run.dataset);
            int carried = 0;
            if (UseBoard) { if (Has("transfer")) carried = CarryConcepts(run); else Board.Reinitialise(region); }
            if (a == null) return;
            Say(T(RegionName(region, false) + "换成了 " + a.name + "：", "The " + RegionName(region, true).ToLowerInvariant() + " region now runs " + a.nameEn + ": ")
                + (carried > 0 ? T("迁移学习带过来 " + carried + " 个底层概念，上面的组合重新学。", "transfer learning carried " + carried + " low-level concepts across; the combinations above are learnt again.")
                    : Has("transfer") ? T("迁移学习保留六成进度。", "transfer learning keeps 60% of the progress.") : T("新结构，从头练。", "a new structure, trained from the start.")));
        }

        /// <summary>A bigger model of the same kind (Net2Net-style growth) keeps most of the progress.</summary>
        void Grow(XgRun run) => KeepProgress(run, Has("transfer") ? GrowKeepTransfer : GrowKeep);

        /// <summary>Scales every dataset's progress above chance on this model by <paramref name="keep"/>.</summary>
        void KeepProgress(XgRun run, double keep)
        {
            Remember(run);
            foreach (var s in run.accs)
            {
                var d = XgCatalog.Dataset(s.key);
                if (d == null) continue;
                double chance = 1 - d.chanceError;
                s.value = chance + Math.Max(0, s.value - chance) * keep;
            }
            run.valAcc = StoredAcc(run, run.dataset);
        }

        /// <summary>Writes the current dataset's accuracy into the per-dataset list.</summary>
        void Remember(XgRun run)
        {
            if (run.accs == null) run.accs = new List<XgScore>();
            if (XgCatalog.Dataset(run.dataset) == null || !Finite(run.valAcc)) return;
            Count(run.accs, run.dataset).value = run.valAcc;
        }

        /// <summary>The accuracy this model had on a dataset (chance if it never trained there).</summary>
        public double StoredAcc(XgRun run, string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            double chance = d == null ? 0 : 1 - d.chanceError;
            if (run.accs != null) foreach (var s in run.accs) if (s.key == dataset && Finite(s.value)) return Math.Max(chance, s.value);
            return chance;
        }

        // ───────────── the ceiling ─────────────

        /// <summary>The final-rate factor of the existing curve: the learning-rate search finds a better rate, a schedule a better one still.</summary>
        public double RateFinal => Has("lrschedule") || AutoTrainLevel >= 5 ? XgCatalog.LrFinal[4] : HasLrKnob(XgTrack.Vision) ? XgCatalog.LrFinal[3] : XgCatalog.LrFinal[2];

        /// <summary>The stage whose ability opens each dataset's desk or pack (the desk's place on the parameter ladder).</summary>
        static readonly Dictionary<string, int> DatasetStage = new Dictionary<string, int>
        {
            { "arith", 1 }, { "logic", 1 }, { "spam", 1 }, { "sense", 1 }, { "xor", 1 },
            { "mnist", 2 }, { "danmu", 2 }, { "headline", 2 },
            { "cifar", 3 }, { "meme", 3 }, { "poems", 3 }, { "longtext", 3 },
            { "review", 4 }, { "crosssentence", 4 }, { "go", 4 }, { "imagenet", 4 }, { "parallel", 4 },
            { "translate", 5 }, { "news", 5 },
        };

        public static int StageOfDataset(string dataset) => DatasetStage.TryGetValue(dataset ?? "", out int s) ? s : 1;

        /// <summary>
        /// The parameter scale of the curve for a desk (thousands): a tenth of the next rung of <see cref="CeilingLadderK"/>,
        /// so a model the size of that rung reaches the top grades and one fifty times smaller only grade C. A desk opened
        /// by ability n is learnt properly by the model that reaches ability n + 1.
        /// </summary>
        public static double CeilingScaleK(string dataset) => .1 * CeilingLadderK[Math.Min(AbilityCount, StageOfDataset(dataset) + 1)];

        /// <summary>The samples a desk wants before data stops holding its ceiling down: 1/500 of the next rung of <see cref="CeilingLadderSamples"/>.</summary>
        public static double CeilingNeedSamples(string dataset) => .002 * CeilingLadderSamples[Math.Min(AbilityCount, StageOfDataset(dataset) + 1)];

        /// <summary>
        /// The parts of the error the curve converges to (0 = the dataset's floor, 1 = chance), the scaling law of
        /// <see cref="CapacityFraction"/> on the game's parameter ladder: capacity falls as (scale / parameters)^0.6,
        /// data as (need / samples)^0.6, and the structure adds its limits (layers past what it can stack, a loop that
        /// forgets a long sentence, a reader that cannot write). Text desks without order are read as bags of words.
        /// </summary>
        public void CeilingParts(XgRun run, out double parameters, out double data, out double structure)
        {
            parameters = data = structure = 0;
            var a = XgCatalog.Arch(run.arch);
            var d = XgCatalog.Dataset(run.dataset);
            if (a == null || d == null) return;
            int over = Math.Max(0, run.depth - MaxDepth(run));
            double neff = ParamsK(run) * Math.Pow(Has("relu") ? .85 : .75, over);
            parameters = a.bias * .25 * Math.Pow(CeilingScaleK(d.id) / Math.Max(1e-6, neff), .6);
            data = .05 * Math.Pow(CeilingNeedSamples(d.id) / SamplesEffective(run), .6);
            structure = over * .03;
            if (d.track == XgTrack.Sequence)
            {
                if (SequentialDatasets.Contains(d.id) && a.span < d.needSpan) structure += .35 * (1 - (double)a.span / d.needSpan);
                if (d.needSeq2Seq && !a.seq2seq) structure += .5;
            }
        }

        /// <summary>
        /// The highest accuracy a model of this shape reaches on its dataset: the scaling law's asymptote, the
        /// architecture limits, and 近亲繁殖.
        /// </summary>
        public double CeilingAcc(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            if (d == null || XgCatalog.Arch(run.arch) == null) return 0;
            CeilingParts(run, out double parameters, out double data, out double structure);
            double frac = Math.Max(0, Math.Min(.97, (parameters + data + structure) * RateFinal));
            double acc = 1 - (d.floorError + (d.chanceError - d.floorError) * frac);
            acc = ProgressionAccuracy(run, acc);
            double chance = 1 - d.chanceError;
            acc = Math.Max(chance, acc - InbreedingPenaltyFor(run.dataset));
            // 学习率重启 (consumable): the ceiling is 8% higher for a few rounds; the dataset's own floor still caps it.
            if (S.sgdrRounds > 0) acc *= 1 + SgdrBoost;
            return Math.Max(chance, Math.Min(1 - d.floorError, acc));
        }

        /// <summary>What holds the ceiling down most (the parts of <see cref="CeilingParts"/>, the architecture caps and the noise penalty).</summary>
        public XgLimit CeilingLimit(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            if (XgCatalog.Arch(run.arch) == null || d == null) return XgLimit.None;
            double range = Math.Max(1e-9, d.chanceError - d.floorError);
            if ((1 - d.floorError - CeilingAcc(run)) / range < .03) return XgLimit.Done;
            CeilingParts(run, out double parameters, out double data, out double structure);
            // The architecture caps of ProgressionAccuracy (a perceptron on logic, an MLP on pictures…).
            double plain = 1 - (d.floorError + range * Math.Min(.97, (parameters + data + structure) * RateFinal));
            double bonus = (Has("bias") ? .02 : 0) + (Has("step") ? .01 : 0) + (Has("sigmoid") ? .01 : 0);
            structure += Math.Max(0, plain + bonus - ProgressionAccuracy(run, plain)) / range;
            double noise = InbreedingPenaltyFor(run.dataset) / range;
            double top = Math.Max(Math.Max(parameters, data), Math.Max(structure, noise));
            return top == parameters ? XgLimit.Params : top == data ? XgLimit.Data : top == structure ? XgLimit.Structure : XgLimit.Noise;
        }

        /// <summary>Score points between the model and its ceiling.</summary>
        public double HeadroomPoints(XgRun run)
        {
            var d = XgCatalog.Dataset(run.dataset);
            if (d == null) return 0;
            return Math.Max(0, CeilingAcc(run) - run.valAcc) * 1000 / Math.Max(1e-9, d.chanceError - d.floorError);
        }

        /// <summary>
        /// The model has reached what its parameters and data allow: another round would add (almost) nothing, or no
        /// more on average than a drop takes back (a model that keeps memorising hovers just under its ceiling).
        /// </summary>
        public bool Plateaued(XgRun run)
        {
            if (run == null || run.epoch <= 0) return false;
            double headroom = HeadroomPoints(run);
            if (headroom < PlateauPoints) return true;
            double rate = run.lastRate > 0 ? run.lastRate : GainMin, drop = 1000 * (DropMin + DropSpread / 2);
            return rate * headroom - DropRisk(run).chance * drop < PlateauNetPoints;
        }

        /// <summary>The auto model is smaller than the next ability needs: training it cannot fill the parameter bar.</summary>
        public bool ParamsShortForNext(XgRun run)
        {
            int next = NextAbility;
            return next > 0 && TrainedParamsK + 1e-9 < ParamsThreshold(next) && ParamsK(run) + 1e-9 < ParamsThreshold(next);
        }

        /// <summary>
        /// Why training stalls, plainly (「参数量不够，再练也涨不动」…), or "" while it still improves and the model is
        /// big enough for the next ability.
        /// </summary>
        public string PlateauText(XgRun run, out XgLimit limit)
        {
            limit = XgLimit.None;
            if (run == null || run.epoch <= 0) return "";
            bool plateau = Plateaued(run);
            if (plateau) limit = CeilingLimit(run);
            if (ParamsShortForNext(run) && (plateau || limit == XgLimit.None)) limit = plateau && limit != XgLimit.Params && limit != XgLimit.Done ? limit : XgLimit.Params;
            switch (limit)
            {
                case XgLimit.Params:
                    return T("参数量不够，再练也涨不动：去科技加宽、加深，或在道具买参数更多的结构。", "Not enough parameters; more training will not help: widen or deepen in the tech tree, or buy a bigger structure on the Items page.");
                case XgLimit.Data:
                    return T("样本不够，再练也涨不动：去标注台多标，或买数据包。", "Not enough samples; more training will not help: label more, or buy a data pack.");
                case XgLimit.Structure:
                    return T("结构不对路，再练也涨不动：去道具买更合适的结构。", "The structure does not suit this data; more training will not help: buy a better one on the Items page.");
                case XgLimit.Noise:
                    return T("数据太脏，再练也涨不动：清洗噪声，或关掉杂包。", "The data is too dirty; more training will not help: clean the noise or switch off the junk packs.");
                case XgLimit.Done:
                    return T("这张桌练到头了：换个数据集，或者接单。", "This desk is as good as it gets: switch datasets, or take orders.");
                default: return "";
            }
        }

        // ───────────── one round ─────────────

        /// <summary>Share of the way to the ceiling one round covers: more cards (compute, combo, research) are faster, bigger models slower.</summary>
        public double GainRate(XgRun run, int cards) => Math.Max(GainMin, Math.Min(GainMax, GainPerCard * Math.Max(0, cards) / Math.Max(1, Tau(run))));

        /// <summary>
        /// The chance that the next round goes down, and why: data scarce for the model's size (params per sample, as in
        /// <see cref="OverfitScale"/>), noisy labels, this month's new memes, a big jump just before, and a little
        /// wobble. Dropout halves the overfitting part, augmentation multiplies the picture samples, warm-up and clipping
        /// soften the jump, BatchNorm steadies the rest.
        /// </summary>
        public XgDropRisk DropRisk(XgRun run)
        {
            var r = new XgDropRisk();
            var d = XgCatalog.Dataset(run.dataset);
            if (d == null) return r;
            double perSample = ParamsK(run) * 1000 / Math.Max(1, SamplesEffective(run));
            r.overfit = .24 * Clamp01(Math.Log10(Math.Max(1, perSample)) / 3) * (Has("dropout") ? .5 : 1);
            r.noise = Math.Min(.2, .6 * NoiseRatio(run.dataset));
            r.drift = Math.Min(.12, .5 * MemeDriftPenalty(run.dataset));
            double range = Math.Max(1e-9, d.chanceError - d.floorError);
            double jumpPoints = Math.Max(0, run.lastGain) * 1000 / range;
            r.jump = .1 * Clamp01((jumpPoints - 30) / 120) * (Has("warmup") ? .5 : 1) * (Has("gradclip") ? .7 : 1);
            r.wobble = DropWobble * (perSample < 1 ? .67 : 1);
            double total = (r.overfit + r.noise + r.drift + r.jump + r.wobble) * (Has("batchnorm") ? .8 : 1);
            r.chance = Math.Max(0, Math.Min(DropMax, total));
            r.level = r.chance < RiskMedium ? 0 : r.chance < RiskHigh ? 1 : 2;
            double top = Math.Max(Math.Max(r.overfit, r.noise), Math.Max(r.drift, Math.Max(r.jump, r.wobble)));
            if (top == r.wobble) { r.reason = "正常波动"; r.reasonEn = "the usual wobble"; }
            else if (top == r.overfit) { r.reason = "数据太少，容易背答案"; r.reasonEn = "too little data, it memorises the answers"; }
            else if (top == r.noise) { r.reason = "数据里有标错的，学着学着就学歪"; r.reasonEn = "wrong labels in the data pull it off course"; }
            else if (top == r.drift) { r.reason = "新梗冒出来了，题在变"; r.reasonEn = "new memes are changing the questions"; }
            else { r.reason = "刚涨了一大截，容易回落"; r.reasonEn = "a big jump just now, it may fall back"; }
            return r;
        }

        public static string RiskLevelName(int level, bool english) => english ? new[] { "low", "medium", "high" }[Math.Max(0, Math.Min(2, level))] : new[] { "低", "中", "高" }[Math.Max(0, Math.Min(2, level))];

        /// <summary>「下降风险：中 · 数据太少，容易背答案」</summary>
        public string DropRiskText(XgRun run)
        {
            var r = DropRisk(run);
            return T("下降风险：" + RiskLevelName(r.level, false) + " · " + r.reason, "Risk of a drop: " + RiskLevelName(r.level, true) + " · " + r.reasonEn);
        }

        /// <summary>
        /// One round's change: with the drop chance the accuracy falls 5–15 score points; otherwise it moves the
        /// round's share of the way to the ceiling (nothing once it is there). Deterministic through the save's RNG.
        /// </summary>
        void StepAccuracy(XgRun run, int cards, XgEpoch e)
        {
            var d = XgCatalog.Dataset(run.dataset);
            if (d == null) return;
            Evaluate(run);
            double before = run.valAcc, chance = 1 - d.chanceError, range = d.chanceError - d.floorError;
            run.lastRate = GainRate(run, cards);
            var risk = DropRisk(run);
            if (Roll() < risk.chance)
            {
                double fallen = Math.Max(chance, before - range * (DropMin + DropSpread * Roll()));
                bool real = before - fallen > 1e-12;
                if (real && S.rollbackGuards > 0)
                {
                    // 检查点回滚 (consumable): the drop is undone; the round neither gains nor loses.
                    S.rollbackGuards--;
                    e.rolledBack = true;
                    Say(T("检查点回滚：这一轮的退步撤销了。", "Checkpoint rollback: this round's drop was undone."));
                }
                else
                {
                    run.valAcc = fallen;
                    e.dropped = real;
                    if (e.dropped)
                    {
                        S.drops++;
                        e.dropReason = risk.reason; e.dropReasonEn = risk.reasonEn;
                        Say(T("这一轮退步了 " + F((before - run.valAcc) * 1000 / range, "0") + " 分：" + risk.reason + "。", "This round went down " + F((before - run.valAcc) * 1000 / range, "0") + " points: " + risk.reasonEn + "."));
                    }
                }
            }
            else if (before < run.ceiling) run.valAcc = before + run.lastRate * (run.ceiling - before);
            run.lastGain = run.valAcc - before;
            e.gain = run.lastGain;
            Remember(run);
        }

        /// <summary>
        /// A save from the knob era: every dataset of a track starts from its best checkpoint (or the run's own
        /// accuracy if that is higher), so nothing reads lower after loading. P, grades and abilities are untouched.
        /// </summary>
        void MigrateToAutoTraining()
        {
            foreach (var run in Runs)
            {
                if (run.accs == null) run.accs = new List<XgScore>();
                if (run.accs.Count > 0) continue;
                foreach (var b in S.best)
                {
                    var d = XgCatalog.Dataset(b.dataset);
                    if (d == null || (int)d.track != run.track || !Finite(b.acc) || b.acc <= 0) continue;
                    Count(run.accs, b.dataset).value = b.acc;
                }
                if (XgCatalog.Dataset(run.dataset) != null && Finite(run.valAcc) && run.valAcc > StoredAcc(run, run.dataset))
                    Count(run.accs, run.dataset).value = run.valAcc;
                run.valAcc = StoredAcc(run, run.dataset);
            }
        }
    }
}
