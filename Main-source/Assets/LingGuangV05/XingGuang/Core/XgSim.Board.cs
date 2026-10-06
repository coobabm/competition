using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The concept board (design v1.1 §4). Links are rebuilt while training, so they are not saved.</summary>
        public XgBoardState board = new XgBoardState();
        public XgPhenomenaMemory phenomena = new XgPhenomenaMemory();
        /// <summary>Cards trained per dataset, and the board's card count when each was last trained (forgetting).</summary>
        public List<XgScore> boardCards = new List<XgScore>();
        public List<XgScore> boardLastTrained = new List<XgScore>();
    }

    public sealed partial class XgRun
    {
        /// <summary>Training-page knobs that the board reads (design v1.1 §4.3). act: 0 step, 1 S-curve, 2 ReLU;
        /// -1 (also older saves) = the best activation owned.</summary>
        public int act = -1;
        public bool clip, skip, position, warmup;
        /// <summary>Attention with the loop switched off (design v1.1 阶段 5「只用注意力」).</summary>
        public bool attnOnly;
        /// <summary>Next card of the training pool and the running training accuracy on fed cards.</summary>
        public long cursor;
        public double boardTrain = -1;
    }

    public sealed partial class XgSim
    {
        /// <summary>Accuracy comes from the concept board. Off only for tests of the old curve formula.</summary>
        public bool UseBoard = true;
        public const int MaxCardsPerEpoch = 320;
        public const int OfflineCardsPerEpoch = 12;
        /// <summary>A phenomenon was seen for the first time (its 图鉴 node lights up).</summary>
        public event Action<XgPhenomenon> PhenomenonFound;

        XgBoard board;
        public XgBoard Board
        {
            get
            {
                if (S.board == null) S.board = new XgBoardState();
                if (board == null || board.S != S.board) board = new XgBoard(S.board);
                return board;
            }
        }

        readonly Dictionary<string, List<XgBoardCard>> testSets = new Dictionary<string, List<XgBoardCard>>();

        // ───────────── knobs ─────────────

        public static XgWiring WiringOf(string arch)
        {
            switch (arch)
            {
                case "lenet": case "alexnet": case "vgg": case "googlenet": case "resnet": case "caption": return XgWiring.LocalShared;
                case "rnn": return XgWiring.Recurrent;
                case "lstm": case "gru": return XgWiring.GatedRecurrent;
                case "seq2seq": return XgWiring.EncoderDecoder;
                case "attention": return XgWiring.Attention;
                case "transformer": return XgWiring.AnyToAny;
                default: return XgWiring.Full;
            }
        }

        public static readonly double[] RateValues = { 1, .3, .1, .01, .001 };

        public bool ActivationOwned(int act) => act == 0 || act == 1 && Has("sigmoid") || act == 2 && Has("relu");
        public int BestActivation => Has("relu") ? 2 : Has("sigmoid") ? 1 : 0;
        /// <summary>The player's choice if still owned, otherwise the best activation bought.</summary>
        public int EffectiveActivation(XgRun run) => run.act >= 0 && ActivationOwned(run.act) ? run.act : BestActivation;
        public bool ClipOwned => Has("gradclip");
        public bool SkipOwned => Has("resnet") || Has("transformer");
        public bool PositionOwned => Has("position");
        public bool WarmupOwned => Has("warmup");
        public bool AttentionOnlyOwned => Has("attention");

        /// <summary>The rule parameters this run trains with. Knobs the player has not bought stay off.</summary>
        public XgKnobs Knobs(XgRun run)
        {
            int act = EffectiveActivation(run);
            string arch = run.arch;
            return new XgKnobs
            {
                depth = Math.Max(1, run.depth),
                width = XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))] / 2,
                activation = (XgActivation)act,
                wiring = arch == "attention" && run.attnOnly && AttentionOnlyOwned ? XgWiring.AnyToAny : WiringOf(arch),
                skip = SkipOwned && (run.skip || arch == "resnet" || arch == "transformer"),
                clip = ClipOwned && run.clip,
                position = PositionOwned && run.position,
                warmup = WarmupOwned && run.warmup,
                batchNorm = Has("batchnorm"),
                bias = Has("bias"),
                lr = RateValues[Math.Max(0, Math.Min(RateValues.Length - 1, run.lr))],
            };
        }

        public bool SetActivation(XgTrack track, int act)
        {
            var run = Run(track);
            if (run.epochActive || !ActivationOwned(act) || EffectiveActivation(run) == act && run.act >= 0) return false;
            run.act = act; Evaluate(run); return true;
        }

        public bool SetClip(XgTrack track, bool on) { var run = Run(track); if (run.epochActive || !ClipOwned) return false; run.clip = on; return true; }
        public bool SetSkip(XgTrack track, bool on) { var run = Run(track); if (run.epochActive || !SkipOwned) return false; run.skip = on; Evaluate(run); return true; }
        public bool SetPosition(XgTrack track, bool on) { var run = Run(track); if (run.epochActive || !PositionOwned) return false; run.position = on; Evaluate(run); return true; }
        public bool SetAttentionOnly(XgTrack track, bool on) { var run = Run(track); if (run.epochActive || !AttentionOnlyOwned) return false; run.attnOnly = on; Evaluate(run); return true; }
        public bool SetWarmup(XgTrack track, bool on) { var run = Run(track); if (run.epochActive || !WarmupOwned) return false; run.warmup = on; return true; }

        /// <summary>Buying a knob switches it on for both tracks, the way a new architecture is switched to.</summary>
        void ApplyKnobNode(XgNode node)
        {
            foreach (var run in Runs)
            {
                if (node.id == "sigmoid" || node.id == "relu") run.act = -1;
                if (node.id == "gradclip") run.clip = true;
            }
        }

        // ───────────── data ─────────────

        public static string RegionOf(string dataset) => XgBoardData.Region(dataset);

        /// <summary>Card difficulty on the board: a standing wall is always tested at its own fixed level (1).</summary>
        public int BoardLevel(string dataset)
        {
            var wall = ActiveWall;
            if (wall != null) foreach (var c in wall.checks) if (c.dataset == dataset && !WallCheckPassed(wall, c)) return 1;
            return Math.Max(1, LevelOf(dataset));
        }

        public List<XgBoardCard> TestSet(string dataset)
        {
            int level = BoardLevel(dataset);
            string key = dataset + "|" + level + "|" + Today / 100 + "|" + S.dataSalt;
            if (!testSets.TryGetValue(key, out var set)) testSets[key] = set = XgBoardData.TestSet(dataset, level, Today, S.dataSalt);
            return set;
        }

        /// <summary>The next card of the labelled pool (the pool grows with the samples you own).</summary>
        XgBoardCard PoolCard(XgRun run) => PoolCardAt(run, run.cursor++);

        /// <summary>The pool card at a position (trials read ahead without moving the cursor).</summary>
        XgBoardCard PoolCardAt(XgRun run, long position)
        {
            int pool = (int)Math.Max(1, Math.Min(XgBoardData.PoolLimit, Samples(run.dataset)));
            int index = (int)(position % pool);
            var card = XgBoardData.Make(run.dataset, XgBoardData.Seed(run.dataset, index, XgBoardData.Use.Train, S.dataSalt), BoardLevel(run.dataset), Today);
            // R5 噪: rows wrongly labelled by automation pull the wrong way.
            double noise = Math.Min(.5, Noise(run.dataset) / Math.Max(1, Labels(run.dataset) + Noise(run.dataset)));
            if (noise > 0 && (XgBoardData.Seed(run.dataset, index, XgBoardData.Use.Diagnostic, S.dataSalt) % 1000) < noise * 1000) card.truth = !card.truth;
            return card;
        }

        /// <summary>Cards one epoch feeds: compute, optimiser and research speed; bigger boards cost more per card.</summary>
        public int CardsPerEpoch(XgRun run, double compute, bool hand)
        {
            if (OfflineSimulation) return OfflineCardsPerEpoch;
            var a = XgCatalog.Arch(run.arch);
            var k = Knobs(run);
            double n = 24 * Math.Pow(Math.Max(.5, compute), .7) * OptSpeed * (a == null ? 1 : a.speed) * SpeedResearch * ProgressionSpeed(run)
                * (hand ? ComboMultiplier : AutoEpochFactor) / Math.Sqrt(1 + k.Cells / 256.0);
            return (int)Math.Max(4, Math.Min(MaxCardsPerEpoch, Math.Round(n)));
        }

        /// <summary>Board accuracy (0.5 = coin) mapped onto the dataset's own scale: chance → floor error.</summary>
        public static double Scale(string dataset, double boardAccuracy)
        {
            var d = XgCatalog.Dataset(dataset);
            double q = Math.Max(0, Math.Min(1, (boardAccuracy - .5) / .5));
            return d == null ? boardAccuracy : (1 - d.chanceError) + (d.chanceError - d.floorError) * q;
        }

        void EvaluateBoard(XgRun run)
        {
            var test = TestSet(run.dataset);
            double acc = Board.Accuracy(test, Knobs(run));
            run.valAcc = Scale(run.dataset, acc);
            run.trainAcc = Scale(run.dataset, run.boardTrain < 0 ? acc : run.boardTrain);
        }

        /// <summary>Feeds one epoch of cards through the six rules. Returns true when the rate tore the board (NaN).</summary>
        bool TrainBoard(XgRun run, int cards)
        {
            var k = Knobs(run);
            RememberFormalKnobs(run);
            int right = 0, torn = 0;
            for (int i = 0; i < cards; i++)
            {
                var step = Board.Train(PoolCard(run), k);
                if (step.correct) right++;
                if (step.diverged) torn++;
            }
            double acc = cards > 0 ? (double)right / cards : 0;
            run.boardTrain = run.boardTrain < 0 ? acc : run.boardTrain * .6 + acc * .4;
            Count(S.boardCards, run.dataset).value += cards;
            Count(S.boardLastTrained, run.dataset).value = Board.S.cards;
            return torn > 0;
        }

        static XgScore Count(List<XgScore> list, string key)
        {
            foreach (var s in list) if (s.key == key) return s;
            var made = new XgScore { key = key }; list.Add(made); return made;
        }

        void ObservePhenomena(XgRun run)
        {
            if (S.phenomena == null) S.phenomena = new XgPhenomenaMemory();
            var d = XgCatalog.Dataset(run.dataset);
            var o = new XgObservation
            {
                dataset = run.dataset, region = RegionOf(run.dataset), knobs = Knobs(run),
                train = run.boardTrain < 0 ? .5 : run.boardTrain, test = Board.Accuracy(TestSet(run.dataset), Knobs(run)),
                cards = (long)Count(S.boardCards, run.dataset).value, maxLength = SequentialDatasets.Contains(run.dataset) && d != null ? d.needSpan : 0,
                regionCards = RegionCards(RegionOf(run.dataset)), augmented = Has("augment"),
            };
            foreach (var id in XgPhenomena.Observe(o, S.phenomena, Board))
            {
                var p = XgPhenomena.Get(id);
                Say(T("现象：", "Phenomenon: ") + T(p.name, p.nameEn) + T("。", ". ") + T(p.why, p.whyEn));
                PhenomenonFound?.Invoke(p);
            }
            // Datasets left alone fade (catastrophic forgetting) — checked against their own test sets.
            foreach (var other in S.boardLastTrained)
            {
                if (other.key == run.dataset || XgCatalog.Dataset(other.key) == null || RegionOf(other.key) != o.region) continue;
                long idle = Board.S.cards - (long)other.value;
                if (idle < 500) continue;
                var po = new XgObservation { dataset = other.key, region = o.region, knobs = o.knobs, train = .5, test = Board.Accuracy(TestSet(other.key), o.knobs), cards = (long)Count(S.boardCards, other.key).value, idle = idle };
                foreach (var id in XgPhenomena.Observe(po, S.phenomena, Board)) { var p = XgPhenomena.Get(id); Say(T("现象：", "Phenomenon: ") + T(p.name, p.nameEn)); PhenomenonFound?.Invoke(p); }
            }
        }

        /// <summary>Datasets read in order (long-sentence amnesia applies); the text desks are read as bags of words.</summary>
        static readonly HashSet<string> SequentialDatasets = new HashSet<string> { "poems", "news", "longtext", "crosssentence" };

        public bool PhenomenonSeen(string id) => S.phenomena != null && S.phenomena.seen.Contains(id);

        long RegionCards(string region)
        {
            long n = 0;
            foreach (var c in S.boardCards) if (XgCatalog.Dataset(c.key) != null && RegionOf(c.key) == region) n += (long)c.value;
            return n;
        }

        /// <summary>A hand-labelled card goes straight onto the board: what you teach is what it becomes.</summary>
        void TeachBoard(XgCard card, bool label)
        {
            if (!UseBoard || card == null || XgCatalog.Dataset(card.dataset) == null) return;
            var run = Run(XgCatalog.Dataset(card.dataset).track);
            var b = XgBoardData.FromCard(card);
            b.truth = label;
            Board.Train(b, Knobs(run));
        }

        /// <summary>Changing the depth reinitialises the network for that region (the SI's seed survives).</summary>
        void ReinitialiseBoard(XgRun run)
        {
            if (!UseBoard) return;
            Board.Reinitialise(RegionOf(run.dataset));
            // Stage 1: the reset clears what you built; only the read-only "？" cell is left, and it lights up.
            if (Winter && !HasEmerged(1)) Emerge(1);
        }
    }
}
