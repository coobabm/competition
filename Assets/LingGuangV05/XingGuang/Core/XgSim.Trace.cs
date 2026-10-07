using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One trained epoch as the 诊断 page draws it (accuracies on the board's own 0.5 = coin scale).</summary>
    [Serializable]
    public sealed class XgTracePoint
    {
        public int epoch;
        public float train, test;
        /// <summary>Cells in use and the cap (R3), concepts made and squeezed out this epoch (R2 / R3), highest layer.</summary>
        public int cells, cap, created, evicted, layer;
        /// <summary>Share of the error that still reaches layer 1 (R1: g^(depth−1)).</summary>
        public float signal;
    }

    /// <summary>
    /// Something the trace noticed at an epoch: a settings change, or the first sign of a problem since the last
    /// change (格子满了, 误差传不到底层, 没长出组合, NaN, 死记硬背, 卡住了) or a phenomenon.
    /// </summary>
    [Serializable]
    public sealed class XgTraceEvent
    {
        public int epoch;
        public string id = "";
        /// <summary>A number for the text (a share, a count) and the settings it refers to.</summary>
        public double value;
        public string detail = "";
    }

    /// <summary>A run's training history on one dataset, cut into segments by settings changes.</summary>
    [Serializable]
    public sealed class XgTrace
    {
        public int track;
        public string dataset = "";
        public List<XgTracePoint> points = new List<XgTracePoint>();
        public List<XgTraceEvent> events = new List<XgTraceEvent>();
        /// <summary>Settings of the current segment, its first epoch and the board counters at the last epoch.</summary>
        public string settings = "";
        public int segmentFrom;
        public long segmentCards, lastCreated, lastEvicted;
        public float segmentBest;
        public int flat;
        /// <summary>Depth and best training accuracy of the segment before this one (加深反而变差 compares against it).</summary>
        public int depth, previousDepth;
        public float trainBest = -1, previousTrainBest = -1;

        public bool Noted(string id)
        {
            for (int i = events.Count - 1; i >= 0 && events[i].epoch >= segmentFrom; i--) if (events[i].id == id) return true;
            return false;
        }
    }

    /// <summary>One layer of the run's network as the 诊断 diagram draws it, with what is wrong there (if anything).</summary>
    public sealed class XgLayerHealth
    {
        /// <summary>1 = the layer reading the input; depth = the layer giving the answer.</summary>
        public int layer;
        /// <summary>Concepts living on this layer, their mean |weight|, and the share of the error that reaches them (R1).</summary>
        public int concepts;
        public double strength, signal;
        /// <summary>传话 (forward relay): what is left of this layer's votes at the answer, and how garbled they are.</summary>
        public double relay = 1, garble;
        /// <summary>"" when healthy, else step / signal / relay / nomerge (see <see cref="XgSim.TraceEventText"/>).</summary>
        public string problem = "";
    }

    /// <summary>The whole network at a glance: its layers, its wiring, and the cell budget they share (R3).</summary>
    public sealed class XgNetworkHealth
    {
        public string arch = "", dataset = "";
        public XgWiring wiring;
        public int depth, width, cells, cap;
        public bool full, features, skip;
        /// <summary>Concepts squeezed out (R3) in the last trained epoch.</summary>
        public int evicted;
        /// <summary>For looped wiring: what a loop still carries of a word ten words back (R6), else 1.</summary>
        public double memory10 = 1;
        public List<XgLayerHealth> layers = new List<XgLayerHealth>();
        /// <summary>The first layer with a problem (0 = none): the one the diagram marks red.</summary>
        public int worst;
    }

    public sealed partial class XgState
    {
        /// <summary>Training traces for the 诊断 page, newest last (a few datasets per track).</summary>
        public List<XgTrace> traces = new List<XgTrace>();
    }

    /// <summary>
    /// The 训练图式 of the 诊断 page: every board epoch is recorded with the rule quantities a player cannot see on
    /// the curve (cells, merges, evictions, how much error reaches the bottom), and the epoch where something first
    /// goes wrong is marked, so a stuck wall shows *where* training broke, not only that it did.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int TracePoints = 240, TraceEvents = 64, TracesKept = 6;
        /// <summary>Error reaching layer 1 below this share counts as 梯度消失.</summary>
        public const double WeakSignal = .1;
        /// <summary>Epochs without a better test score before the trace calls it a plateau.</summary>
        public const int PlateauEpochs = 8;
        /// <summary>Squeezing out this share of an epoch's cards in concepts counts as 挤得太凶 (learnt and forgotten).</summary>
        public const double ThrashShare = .2;

        readonly List<string> tracePhenomena = new List<string>();

        /// <summary>The trace of a track's current dataset, or null before it trained there.</summary>
        public XgTrace Trace(XgTrack track)
        {
            var run = Run(track);
            return TraceOf((int)track, run.dataset, false);
        }

        XgTrace TraceOf(int track, string dataset, bool create)
        {
            if (S.traces == null) S.traces = new List<XgTrace>();
            for (int i = S.traces.Count - 1; i >= 0; i--) if (S.traces[i].track == track && S.traces[i].dataset == dataset) return S.traces[i];
            if (!create) return null;
            var t = new XgTrace { track = track, dataset = dataset, segmentBest = -1 };
            S.traces.Add(t);
            while (S.traces.Count > TracesKept) S.traces.RemoveAt(0);
            return t;
        }

        /// <summary>Settings a segment is cut by, as a short readable line.</summary>
        public string TraceSettings(XgRun run)
        {
            var k = Knobs(run);
            var a = XgCatalog.Arch(run.arch);
            string act = k.activation == XgActivation.Step ? T("阶跃", "step") : k.activation == XgActivation.Sigmoid ? T("S 形", "S-curve") : "ReLU";
            var parts = new List<string> { a != null ? T(a.name, a.nameEn) : run.arch, k.depth + T(" 层", " layers"), T("宽 ") + k.width, act, T("学习率 ", "rate ") + k.lr };
            if (k.features) parts.Add(T("特征工程"));
            if (k.clip) parts.Add(T("裁剪"));
            if (k.skip) parts.Add(T("直连"));
            if (k.position) parts.Add(T("位置"));
            if (k.warmup) parts.Add(T("预热"));
            if (k.batchNorm) parts.Add("BN");
            return string.Join(" · ", parts);
        }

        /// <summary>
        /// The run's network layer by layer, read off the concept board: how many concepts each layer holds, how much
        /// error reaches it, and where it is stuck (a step activation passes nothing down, the error fades below
        /// <see cref="WeakSignal"/>, or a combining layer stayed empty), plus whether the shared cells are full.
        /// </summary>
        public XgNetworkHealth NetworkHealth(XgRun run)
        {
            var k = Knobs(run);
            string region = RegionOf(run.dataset);
            var h = new XgNetworkHealth { arch = run.arch, dataset = run.dataset, wiring = k.wiring, depth = k.depth, width = k.width, cap = k.Cells, features = k.features, skip = k.skip };
            var count = new int[k.depth + 1]; var sum = new double[k.depth + 1];
            foreach (var c in Board.Concepts(region))
            {
                if (c.seed || c.pinned) continue;
                int l = Math.Max(1, Math.Min(k.depth, c.layer));
                count[l]++; sum[l] += Math.Abs(c.w);
                h.cells++;
            }
            h.full = h.cells >= h.cap;
            if (k.wiring == XgWiring.Recurrent || k.wiring == XgWiring.GatedRecurrent || k.wiring == XgWiring.EncoderDecoder) h.memory10 = Math.Pow(k.SequenceDecay, 10);
            var t = TraceOf(run.track, run.dataset, false);
            long trained = t != null && t.settings == TraceSettings(run) ? t.segmentCards : 0;
            if (t != null && t.points.Count > 0) h.evicted = t.points[t.points.Count - 1].evicted;
            for (int l = 1; l <= k.depth; l++)
            {
                var layer = new XgLayerHealth { layer = l, concepts = count[l], strength = count[l] > 0 ? sum[l] / count[l] : 0, signal = Math.Pow(k.G, k.depth - l),
                    relay = k.RelayLeft(k.depth - l), garble = k.RelayGarble(k.depth - l) };
                if (l < k.depth && k.G <= 0) layer.problem = "step";
                else if (l < k.depth && layer.garble >= layer.relay) layer.problem = "relay";
                else if (l < k.depth && layer.signal < WeakSignal) layer.problem = "signal";
                // Upper layers of a deep net may well stay empty; only a net with no combination at all is stuck (marked on layer 2).
                else if (l == 2 && trained >= 400 && NoCombination(count)) layer.problem = "nomerge";
                if (layer.problem.Length > 0 && h.worst == 0) h.worst = l;
                h.layers.Add(layer);
            }
            return h;
        }

        static bool NoCombination(int[] count) { for (int l = 2; l < count.Length; l++) if (count[l] > 0) return false; return true; }

        void RecordTrace(XgRun run, bool diverged, int cards)
        {
            var t = TraceOf(run.track, run.dataset, true);
            var k = Knobs(run);
            string region = RegionOf(run.dataset);
            var b = Board;
            string settings = TraceSettings(run);
            if (settings != t.settings)
            {
                if (t.points.Count > 0) Note(t, run.epoch, "change", 0, settings);
                if (t.trainBest >= 0) { t.previousTrainBest = t.trainBest; t.previousDepth = t.depth; }
                t.settings = settings; t.segmentFrom = run.epoch; t.segmentCards = 0; t.segmentBest = -1; t.flat = 0; t.trainBest = -1;
            }
            t.segmentCards += cards;
            float test = (float)b.Accuracy(TestSet(run.dataset), k);
            float train = run.boardTrain < 0 ? test : (float)run.boardTrain;
            int used = b.Count(region), cap = k.Cells;
            int created = (int)Math.Max(0, b.S.created - t.lastCreated), evicted = (int)Math.Max(0, b.S.evicted - t.lastEvicted);
            t.lastCreated = b.S.created; t.lastEvicted = b.S.evicted;
            double signal = k.depth <= 1 ? 1 : Math.Pow(k.G, k.depth - 1);
            var p = new XgTracePoint { epoch = run.epoch, train = train, test = test, cells = used, cap = cap, created = created, evicted = evicted, layer = b.MaxLayer(region), signal = (float)signal };
            t.points.Add(p);
            while (t.points.Count > TracePoints) t.points.RemoveAt(0);

            // The first sign of each problem since the settings last changed.
            if (diverged) Note(t, run.epoch, "nan", k.lr, "");
            if (k.depth > 1 && k.G <= 0) Note(t, run.epoch, "step", 0, "");
            else if (k.depth > 1 && signal < WeakSignal) Note(t, run.epoch, "signal", signal, "");
            if (k.depth > 1 && k.RelayGarble(k.depth - 1) >= k.RelayLeft(k.depth - 1)) Note(t, run.epoch, "relay", k.RelayLeft(k.depth - 1), "");
            if (used >= cap && evicted >= Math.Max(3, cards * .02)) Note(t, run.epoch, "cells", cap, "");
            if (used >= cap && evicted >= Math.Max(8, cards * ThrashShare)) Note(t, run.epoch, "thrash", evicted, "");
            if (k.depth > 1 && k.G > 0 && p.layer <= 1 && t.segmentCards >= 400) Note(t, run.epoch, "nomerge", t.segmentCards, "");
            if (train - test >= .25) Note(t, run.epoch, "memorize", train - test, "");
            t.depth = k.depth;
            if (train > t.trainBest) t.trainBest = train;
            // 越深越差: deeper than the segment before, and after a fair try even the training cards are worse.
            if (k.depth > t.previousDepth && t.previousTrainBest >= 0 && run.epoch - t.segmentFrom >= PlateauEpochs && t.trainBest < t.previousTrainBest - .05f)
                Note(t, run.epoch, "degrade", t.previousTrainBest - t.trainBest, t.previousDepth + "→" + k.depth);
            if (test > t.segmentBest + .02f) { t.segmentBest = test; t.flat = 0; }
            else if (++t.flat >= PlateauEpochs && test < TraceGoal(run)) Note(t, run.epoch, "plateau", test, "");
            foreach (var id in tracePhenomena) Note(t, run.epoch, "ph:" + id, 0, "");
            tracePhenomena.Clear();
        }

        /// <summary>The line the trace measures a plateau against.</summary>
        public double TraceGoal(XgRun run) => .9;

        static void Note(XgTrace t, int epoch, string id, double value, string detail)
        {
            if (id != "change" && t.Noted(id)) return;
            t.events.Add(new XgTraceEvent { epoch = epoch, id = id, value = value, detail = detail });
            while (t.events.Count > TraceEvents) t.events.RemoveAt(0);
        }

        /// <summary>What an event means, in a line (and which rule it comes from).</summary>
        public static string TraceEventText(XgTraceEvent e, out string en)
        {
            switch (e.id)
            {
                case "change": en = "Changed settings: " + e.detail; return "改了设置：" + e.detail;
                case "nan": en = "Rate " + e.value + " tore the weights apart (NaN)"; return "学习率 " + e.value + " 太大：权重被撕碎（NaN）";
                case "step": en = "A step has no slope: the error never reaches the lower layers, so nothing combines (R1, R2)"; return "阶跃没有坡度：误差传不到下面几层，长不出组合（R1、R2）";
                case "signal":
                    en = "Only " + Math.Round(e.value * 100, 1) + "% of the error reaches layer 1: the bottom barely learns (vanishing gradients, R1)";
                    return "误差传到第 1 层只剩 " + Math.Round(e.value * 100, 1) + "%：底层几乎学不动（梯度消失，R1）";
                case "cells":
                    en = "All " + e.value + " cells are full: new concepts squeeze old ones out (R3)";
                    return "格子满了（" + e.value + " 格）：新概念把旧的挤掉（R3）";
                case "relay":
                    en = "Votes from low layers reach the answer rewritten: every plain layer has to learn to pass them on unchanged, and never quite does";
                    return "底层的票到输出时已经被改写了：每层普通层都得学会原样转交，可总会改动一点（传话）";
                case "degrade":
                    en = "Deeper (" + e.detail + " layers) and even the training cards got worse by " + Math.Round(e.value * 100) + " points: not memorising: the extra layers cannot learn to pass things on unchanged. Skip connections, or BatchNorm and fewer layers";
                    return "加深（" + e.detail + " 层）以后连训练题都差了 " + Math.Round(e.value * 100) + " 分：不是死记硬背，是多出来的层学不会「原样转交」。开跨层直连，或者 BatchNorm 加少几层";
                case "thrash":
                    en = e.value + " concepts squeezed out in one epoch: learnt and forgotten at once (R3). Widen, share the wiring, or hand-make fewer features";
                    return "挤得太凶：一轮挤掉 " + e.value + " 个概念，学了就忘（R3）。加宽、换能共用的连法，或者用特征工程省格子";
                case "nomerge": en = e.value + " cards trained and no combined concept has grown yet (R2)"; return "练了 " + e.value + " 张卡，还没长出任何组合概念（R2）";
                case "memorize":
                    en = "Training cards right, unseen ones wrong (" + Math.Round(e.value * 100) + " points apart): memorising";
                    return "练过的题会、没见过的不会（差 " + Math.Round(e.value * 100) + " 分）：在死记硬背";
                case "plateau":
                    en = PlateauEpochs + " epochs without progress, stuck at " + Math.Round(e.value * 100) + "%";
                    return "卡住了：" + PlateauEpochs + " 轮没进步，停在 " + Math.Round(e.value * 100) + "%";
                default:
                    if (e.id.StartsWith("ph:", StringComparison.Ordinal))
                    {
                        var p = XgPhenomena.Get(e.id.Substring(3));
                        if (p != null) { en = "Phenomenon: " + p.nameEn; return "现象：" + p.name; }
                    }
                    en = e.id; return e.id;
            }
        }

        /// <summary>Problems are drawn red on the trace; changes and phenomena are not.</summary>
        public static bool TraceEventIsProblem(XgTraceEvent e) => e.id != "change" && !e.id.StartsWith("ph:", StringComparison.Ordinal);

        void RepairTraces()
        {
            if (S.traces == null) S.traces = new List<XgTrace>();
            S.traces.RemoveAll(t => t == null || XgCatalog.Dataset(t.dataset) == null);
            foreach (var t in S.traces)
            {
                if (t.points == null) t.points = new List<XgTracePoint>();
                if (t.events == null) t.events = new List<XgTraceEvent>();
                t.points.RemoveAll(p => p == null || float.IsNaN(p.test) || float.IsNaN(p.train));
                t.events.RemoveAll(e => e == null || e.id == null);
            }
        }
    }
}
