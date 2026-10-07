using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>One graphics card the household owns, as the host reports it (HardwareCatalog on the desktop).</summary>
    public sealed class XgCardSpec
    {
        public string id = "", name = "", nameEn = "";
        /// <summary>Training speed relative to the starting GTX 980 Ti (×1).</summary>
        public double compute = 1;
        public double vramMB = 6144, watts = 250;
    }

    /// <summary>
    /// The household rig behind the lab, for the 接线 page: the owned cards, the house PSU and its breaker. The desktop
    /// host maps it onto ChapterOneSim. A host without it (tests, tools) gets one unlimited virtual card, so nothing the
    /// wiring adds can slow the lab down there.
    /// </summary>
    public interface IXgRig
    {
        /// <summary>Owned cards in the order they were bought.</summary>
        IReadOnlyList<XgCardSpec> Cards { get; }
        /// <summary>The house PSU's limit (GameConfig.powerLimitWatts, 3500 W).</summary>
        double PowerLimitWatts { get; }
        /// <summary>What the house PSU carries with every owned card plugged in: cards, cases and drives.</summary>
        double HouseWatts { get; }
        bool BreakerTripped { get; }
        void TripBreaker();
        /// <summary>The household's own reset (false when it refuses, e.g. still overloaded).</summary>
        bool ResetBreaker();
        /// <summary>Watts of owned cards the wiring has unplugged: the house stops drawing and billing them.</summary>
        double UnpluggedWatts { get; set; }
    }

    /// <summary>Optional power-only status. Non-power training blockers must not disconnect the household PSU.</summary>
    public interface IXgHousePower
    {
        /// <summary>Null while household power is available; otherwise the outage reason (e.g. unpaid bill).</summary>
        string HousePowerBlocker { get; }
    }

    /// <summary>A model sitting on a card (or deliberately on none: card = ""). Auto entries may be moved by the lab.</summary>
    [Serializable]
    public sealed class XgPlacement { public string model = "", card = ""; public bool manual; }

    /// <summary>
    /// What 接线 adds to a save. Everything else the page shows is read from existing state (signed orders, the stand-in
    /// switch, pre-training, …), so the defaults are implicit: a card not in <see cref="pulled"/> is plugged in, a
    /// deployed model without a placement is placed automatically, and a job not in <see cref="idle"/> runs.
    /// </summary>
    [Serializable]
    public sealed class XgWiringState
    {
        public int version;
        /// <summary>Cards the player unplugged from their power ("h:gtx980ti#0", "rack").</summary>
        public List<string> pulled = new List<string>();
        /// <summary>阿杰's café cards on his line ("cafe7", "cafe8").</summary>
        public List<string> rented = new List<string>();
        public List<XgPlacement> placed = new List<XgPlacement>();
        /// <summary>Training lines (model "run:0" / "run:1"): house cards taken off them, and café cards put on them.</summary>
        public List<XgPlacement> runOff = new List<XgPlacement>();
        public List<XgPlacement> runOn = new List<XgPlacement>();
        /// <summary>Jobs the player unplugged ("label:mnist", "contract:homework", "train:0", "ajie").</summary>
        public List<string> idle = new List<string>();
        /// <summary>YY 网吧群 · 替我答题.</summary>
        public bool netbarChat;
        public double rentSeconds, rentPaid, receiptsPaid;
        // 阿杰: what you told him, his price, whether he took the machines back.
        public double ajieMul = 1, ajieClock;
        public int ajieAsked, ajieLies;
        public bool ajieTruth, ajieCutoff, ajiePending, ajieJob;
        public string ajieLastLie = "";
        /// <summary>The night rate was on at the last check (for the one log line at 00:00).</summary>
        public bool night;
    }

    public sealed partial class XgState
    {
        /// <summary>接线: power ↔ card, model ↔ card, café rentals and 阿杰 (XgSim.Wiring.cs).</summary>
        public XgWiringState wiring = new XgWiringState();
    }

    public enum XgWireLane { Power, Gpu, Model, Task }
    public enum XgTaskState { Idle, Run, Error }

    /// <summary>A node of the 接线 graph as the page draws it (rebuilt every tick; never saved).</summary>
    public sealed class XgWNode
    {
        public string key = "", name = "", sub = "", group = "";
        public XgWireLane lane;
        /// <summary>Unlocked and on the canvas; a locked node only shows in the inventory with <see cref="lockWhy"/>.</summary>
        public bool available = true;
        public string lockWhy = "";
        /// <summary>Always on the canvas (wired, or something the player owns); others appear when pinned.</summary>
        public bool shown;
        public bool online;
        /// <summary>Status tag (在线 / 满载 / 显存爆了 / …) and whether it is an error.</summary>
        public string tag = "";
        public bool bad, warn;
        // power
        public double capW, loadW;
        // card
        public string source = "";
        public double compute, cu, vramMB, watts, vramUse, residentVram, load, share = 1, busy;
        public bool unlimited, cafe, rack, house;
        // model
        public int modelId;
        public string dataset = "", arch = "";
        public double acc, paramsK, vramNeed, cuNeed;
        public bool run, self;
        /// <summary>A repository checkpoint that is not deployed: it can only be loaded into training (回炉).</summary>
        public bool repo;
        public string card = "";
        public readonly List<string> cards = new List<string>();
        public double speed;
        // task
        public XgTaskState state;
        public string model = "", why = "", unit = "";
        public double min, pay, rate, errorRate;
        public bool realtime, earns;
        /// <summary>
        /// What the wiring does to the job's speed whatever its own switch says: 0 when the player unplugged it or its model
        /// is offline, the card's share otherwise. The job's existing rules still decide whether it runs at all.
        /// </summary>
        public double factor = 1;
        /// <summary>The model the job would use (its dataset's deployed checkpoint, the training line, 本体).</summary>
        public string candidate = "";
        public bool held;
        /// <summary>Dots come from real events (auto labels, epochs) rather than the nominal rate.</summary>
        public bool realItems;
    }

    /// <summary>One wire: from → to, alive when energy or work flows along it.</summary>
    public sealed class XgWEdge { public string from = "", to = ""; public XgWireLane lane; public bool live; }

    /// <summary>The whole graph and its totals at one moment.</summary>
    public sealed class XgWiringView
    {
        public readonly List<XgWNode> nodes = new List<XgWNode>();
        public readonly List<XgWEdge> edges = new List<XgWEdge>();
        public readonly Dictionary<string, XgWNode> byKey = new Dictionary<string, XgWNode>();
        public double psuW, psuLimit = 3500, cuOn, cuUse, vramOn, vramUse, incomePerSecond, rentPerSecond;
        public int running, wired;
        public bool night, tripped, hasRig;
        public XgWNode Node(string key) { return key != null && byKey.TryGetValue(key, out var n) ? n : null; }
    }

    /// <summary>A line of the 接线 log. Tone: 0 plain, 1 good, 2 warning, 3 bad, 4 someone talking.</summary>
    public sealed class XgWLog { public string text = ""; public int tone; public double at; }

    /// <summary>
    /// 接线 (design: 策划案v1.1落地计划.md, "接线 page"): the lab's jobs drawn as 电力 → 显卡 → 模型 → 任务. A wire from a
    /// model to a job is the job's existing switch (sign an order, the stand-in, pre-training, …); a model needs a card
    /// with room in its VRAM, a card needs power. The wiring only scales jobs that already exist, by 1 in the default
    /// layout, so a save that never opens the page plays exactly as before. New: renting 阿杰's two café cards
    /// (stage 3), his question about what they are for, and his receipt job.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Compute units per ×1 of catalog compute (a GTX 980 Ti is 100 CU).</summary>
        public const double CuPerCompute = 100;
        /// <summary>Inference memory: a fixed runtime plus weights and activations per thousand parameters.</summary>
        public const double ModelBaseMB = 64, ModelMBPerK = .008;
        /// <summary>Inference compute per job: a floor plus 0.5 CU per million parameters.</summary>
        public const double ModelBaseCu = .2, ModelCuPerK = .0005;
        /// <summary>Job weights on a card: realtime subtitles must keep up with the stream; chat is a line now and then.</summary>
        public const double RealtimeWeight = 3, LifeWeight = .2;
        public const int WiringStage = 2, CafeStage = 3, SelfStage = 3, StandInStage = 5;
        /// <summary>阿杰's café: two GTX 960 machines on a 1200 W line.</summary>
        public const double CafeCompute = .45, CafeVramMB = 2048, CafeWatts = 120, CafeLineWatts = 1200, IdcLineWatts = 20000;
        /// <summary>
        /// Rent per café card per game second, electricity included. About 3% of a stage-3 player's order income per card:
        /// worth it while training is the bottleneck, noticeable when the cards sit idle.
        /// </summary>
        public const double CafeRent = .4;
        /// <summary>包夜: from 00:00 to 08:00 on the desktop clock the café charges 60%.</summary>
        public const double NightRate = .6, CafeNightEnd = 8 * 3600;
        public const double AjieFirstAsk = 240, AjieAskAgain = 300, AjieSuspicionLoad = .8;
        public const double AjieTruthRate = .8, AjieMiningRate = 1.6;
        /// <summary>阿杰 · 收银小票对账: digits, at least 95% right, ¥0.15 per receipt, 1.5 receipts a second at full speed.</summary>
        public const double ReceiptMin = .95, ReceiptPay = .15, ReceiptRate = 1.5;
        /// <summary>Pre-training draws far more than one card's rating; the house PSU cannot carry it.</summary>
        public const double PretrainWatts = 3400;
        public const int WiringLogLength = 60;

        public const string LifeNetbar = "life.netbar", LifeQingwen = "life.qingwen";
        public static readonly string[] CafeCards = { "cafe7", "cafe8" };

        XgWiringView wiringView;
        IXgHost wiringHost;
        readonly List<XgWLog> wiringLog = new List<XgWLog>();
        readonly Dictionary<string, (Func<bool> available, Func<bool> on, Action<bool> set)> lifeHooks = new Dictionary<string, (Func<bool>, Func<bool>, Action<bool>)>();

        /// <summary>A line was added to the 接线 log.</summary>
        public event Action<XgWLog> WiringLogged;
        /// <summary>阿杰 asks what the café cards are for (true = the second, suspicious time).</summary>
        public event Action<bool> AjieAsked;
        /// <summary>A job processed one item off the nominal rate (task key, right): 阿杰's receipts.</summary>
        public event Action<string, bool> WireItem;

        public IReadOnlyList<XgWLog> WiringLog => wiringLog;
        XgWiringState Wire => S.wiring;
        public bool AjiePending => Wire.ajiePending;
        public bool AjieCutoff => Wire.ajieCutoff;
        public bool AjieTruth => Wire.ajieTruth;
        public double AjieRentMultiplier => Wire.ajieMul;
        public string AjieLastLie => Wire.ajieTruth ? T("在训练 AI", "Training an AI") : Wire.ajieLastLie == "mine" ? T("挖矿", "Mining") : Wire.ajieLastLie == "game" ? T("挂游戏", "Idling a game") : T("还没说", "Not asked yet");
        public bool CafeNight => TimeOfDay < CafeNightEnd;

        /// <summary>The 接线 tab opens with the second stage or the first checkpoint, whichever comes first.</summary>
        public bool WiringOpen => S.stage >= WiringStage || S.best.Count > 0;

        /// <summary>
        /// A life job owned by another app (晴雯's stand-in switch lives in YY): the wire reads and writes that switch.
        /// </summary>
        public void HookLife(string id, Func<bool> available, Func<bool> on, Action<bool> set)
        {
            if (string.IsNullOrEmpty(id) || on == null || set == null) return;
            lifeHooks[id] = (available ?? (() => true), on, set);
            wiringView = null;
        }

        void RepairWiring()
        {
            if (S.wiring == null) S.wiring = new XgWiringState();
            var w = S.wiring;
            if (w.pulled == null) w.pulled = new List<string>();
            if (w.rented == null) w.rented = new List<string>();
            if (w.placed == null) w.placed = new List<XgPlacement>();
            if (w.runOff == null) w.runOff = new List<XgPlacement>();
            if (w.runOn == null) w.runOn = new List<XgPlacement>();
            if (w.idle == null) w.idle = new List<string>();
            // Retired app tasks in old saves must not return to the wiring inventory.
            w.idle.RemoveAll(id => id == "life.juxin");
            if (w.ajieLastLie == null) w.ajieLastLie = "";
            w.placed.RemoveAll(p => p == null || string.IsNullOrEmpty(p.model));
            w.runOff.RemoveAll(p => p == null || string.IsNullOrEmpty(p.card));
            w.runOn.RemoveAll(p => p == null || string.IsNullOrEmpty(p.card));
            w.rented.RemoveAll(c => Array.IndexOf(CafeCards, c) < 0);
            if (w.ajieCutoff) w.rented.Clear();
            if (!(w.ajieMul > 0) || double.IsInfinity(w.ajieMul)) w.ajieMul = 1;
            // A save from before 接线 has no wiring at all: the implicit defaults above are exactly its old layout.
            if (w.version < 1) w.version = 1;
        }

        // ───────────── job speeds the rest of the lab reads (1 in the default layout) ─────────────

        /// <summary>How fast a job runs on its wiring (0 = unplugged or its model is offline). 1 before the first tick.</summary>
        public double WireSpeed(string task)
        {
            var n = wiringView?.Node(task);
            return n == null ? 1 : n.factor;
        }

        /// <summary>A life job's switch is on and 本体 is on a working card.</summary>
        public bool LifeJobLive(string id)
        {
            var n = wiringView?.Node(id);
            return n == null || n.factor > 0;
        }

        /// <summary>
        /// Training is data parallel, as before: every wired card helps. The factor is the online wired cards' compute
        /// over the owned house cards' compute. Active epochs and inference share each card's finite compute;
        /// waiting training lines reserve nothing, and rented café cards add usable capacity.
        /// </summary>
        public double TrainFactor(XgTrack track) => TrainingFactor(wiringView, track, true);

        // A timed epoch already paid the contention cost in elapsed time. Its completed learning must not pay it twice.
        (int track, double factor)? timedEpochLearning;
        double TrainLearningFactor(XgTrack track) => timedEpochLearning.HasValue && timedEpochLearning.Value.track == (int)track
            ? timedEpochLearning.Value.factor : TrainFactor(track);

        double TrainingFactor(XgWiringView view, XgTrack track, bool shared)
        {
            if (view == null) return 1;
            var t = view.Node("train:" + (int)track);
            if (t != null && t.held) return 0;
            var line = view.Node("run:" + (int)track);
            if (line == null) return 1;
            double house = 0, wired = 0;
            foreach (var c in view.nodes)
                if (c.lane == XgWireLane.Gpu && c.house) house += c.compute;
            var run = Run(track);
            foreach (string key in line.cards)
            {
                var c = view.Node(key);
                if (c != null && c.online && c.vramUse <= c.vramMB + 1e-6 &&
                    AvailableTrainingVram(view, c, run) + 1e-6 >= VramNeedMB(run))
                    wired += c.compute * (shared ? c.share : 1);
            }
            return house <= 0 ? 0 : wired / house;
        }

        double TrainingProgressFactor(XgRun run)
        {
            double full = TrainingFactor(wiringView, (XgTrack)run.track, false);
            return full <= 0 ? 0 : Math.Min(1, TrainFactor((XgTrack)run.track) / full);
        }

        double TrainingMemoryMultiplier => Has("datacenter") ? DatacenterVram : 1;

        bool WantsTrainingMemory(XgWiringView view, XgRun run, XgWNode card)
        {
            var line = view.Node("run:" + run.track);
            var task = view.Node("train:" + run.track);
            bool active = run.epochActive || (timedEpochLearning.HasValue && timedEpochLearning.Value.track == run.track);
            return active && line != null && line.available && task != null && task.model.Length > 0 &&
                line.cards.Contains(card.key) && card.online &&
                VramNeedMB(run) <= Math.Max(0, card.vramMB - card.residentVram) * TrainingMemoryMultiplier + 1e-6;
        }

        bool ReservesTrainingMemory(XgWiringView view, XgRun run, XgWNode card)
        {
            if (!WantsTrainingMemory(view, run, card)) return false;
            // Two legacy active epochs can exceed a card even though each fits alone. Keep the furthest-progressed
            // one (track order breaks ties); the other retains its progress and runs as soon as memory is released.
            foreach (var other in Runs)
            {
                if (other.track == run.track || !WantsTrainingMemory(view, other, card)) continue;
                if (VramNeedMB(run) + VramNeedMB(other) <= (card.vramMB - card.residentVram) * TrainingMemoryMultiplier + 1e-6) continue;
                // Completion owns its admitted memory through TrainEpoch's final validation, then finally releases it.
                if (timedEpochLearning.HasValue && timedEpochLearning.Value.track == run.track) continue;
                if (timedEpochLearning.HasValue && timedEpochLearning.Value.track == other.track) return false;
                if (other.epochProgress > run.epochProgress ||
                    (other.epochProgress == run.epochProgress && other.track < run.track)) return false;
            }
            return true;
        }

        // Read epochActive live: two starts in the same frame must not reserve the same memory twice.
        double AvailableTrainingVram(XgWiringView view, XgWNode card, XgRun run)
        {
            double free = card.vramMB - card.residentVram;
            foreach (var other in Runs)
                if (other.track != run.track && ReservesTrainingMemory(view, other, card))
                    free -= VramNeedMB(other) / TrainingMemoryMultiplier;
            return Math.Max(0, free) * TrainingMemoryMultiplier;
        }

        /// <summary>Largest usable training budget after resident inference and other active epochs, preserving the 机房 multiplier.</summary>
        public double TrainVram(XgRun run, IXgHost host)
        {
            double all = Vram(host);
            var view = wiringView;
            if (host is IXgRig && (view == null || !ReferenceEquals(wiringHost, host))) view = RefreshWiring(host);
            if (view == null || !view.hasRig) return all;
            var r = view.Node("run:" + run.track);
            if (r == null) return all;
            double max = 0;
            foreach (string key in r.cards)
            {
                var c = view.Node(key);
                if (c != null && c.online) max = Math.Max(max, AvailableTrainingVram(view, c, run));
            }
            return max;
        }

        /// <summary>Why the wiring keeps a training line from running, or null.</summary>
        string TrainWiringBlocker(XgRun run)
        {
            var view = wiringView;
            if (view == null) return null;
            var t = view.Node("train:" + run.track);
            if (t != null && t.held) return T("接线：训练线拔掉了，去「接线」接回去", "Wiring: this training line is unplugged; plug it back in on the Wiring page");
            if (view.hasRig && TrainFactor((XgTrack)run.track) <= 0) return T("接线：训练线没接上有电的显卡", "Wiring: the training line has no powered card");
            return null;
        }

        // ───────────── the graph ─────────────

        /// <summary>The graph as of the last tick (built now if there was none).</summary>
        public XgWiringView Wiring => wiringView ?? BuildWiring(wiringHost, false);

        /// <summary>Rebuilds the graph now (after a wire changed), placing new models when a host is known.</summary>
        public XgWiringView RefreshWiring(IXgHost host)
        {
            if (host != null) wiringHost = host;
            return BuildWiring(wiringHost, wiringHost != null);
        }

        static string CardKey(string id, int n) => "h:" + id + "#" + n;

        XgWiringView BuildWiring(IXgHost host, bool sync)
        {
            var w = Wire;
            var rig = host as IXgRig;
            var v = new XgWiringView { hasRig = rig != null, night = CafeNight };
            void Add(XgWNode n) { v.nodes.Add(n); v.byKey[n.key] = n; }

            // Power.
            bool cafeOpen = S.stage >= CafeStage, idcOpen = Has("datacenter");
            string powerBlocker = HousePowerProblem(rig);
            Add(new XgWNode
            {
                key = "psu", lane = XgWireLane.Power, name = T("机箱电源", "House PSU"), sub = T("3500 W · 家用 220V", "3500 W · household 220 V"),
                capW = rig != null ? rig.PowerLimitWatts : 3500, shown = true, online = powerBlocker == null, why = powerBlocker ?? "",
            });
            Add(new XgWNode
            {
                key = "cafe", lane = XgWireLane.Power, name = T("网吧电路（阿杰）", "Net café line (Ajie)"), sub = T("1200 W · 电费含在租金里", "1200 W · power is in the rent"),
                capW = CafeLineWatts, available = cafeOpen && !w.ajieCutoff, shown = cafeOpen && !w.ajieCutoff, online = cafeOpen && !w.ajieCutoff,
                lockWhy = w.ajieCutoff ? T("阿杰把机器收回去了", "Ajie took his machines back") : AfterAbility(CafeStage, "阿杰的网吧", "Ajie's net café"),
            });
            Add(new XgWNode
            {
                key = "idc", lane = XgWireLane.Power, name = T("IDC 机柜", "IDC rack power"), sub = T("20000 W · 机房", "20000 W · server room"),
                capW = IdcLineWatts, available = idcOpen, shown = idcOpen, online = idcOpen, lockWhy = AfterAbility(6, "科技树租「机房」", "rent the server room in the tech tree"),
            });
            v.psuLimit = v.Node("psu").capW;
            v.tripped = rig != null && rig.BreakerTripped;

            // Cards.
            var house = new List<XgWNode>();
            if (rig != null)
            {
                var seen = new Dictionary<string, int>();
                var cards = rig.Cards;
                for (int i = 0; i < cards.Count; i++)
                {
                    var spec = cards[i];
                    if (spec == null) continue;
                    seen.TryGetValue(spec.id, out int k); seen[spec.id] = k + 1;
                    int total = 0; foreach (var s in cards) if (s != null && s.id == spec.id) total++;
                    string key = CardKey(spec.id, k);
                    bool plugged = !w.pulled.Contains(key);
                    var n = new XgWNode
                    {
                        key = key, lane = XgWireLane.Gpu, house = true, source = plugged ? "psu" : "", shown = true,
                        name = T(spec.name, string.IsNullOrEmpty(spec.nameEn) ? spec.name : spec.nameEn) + (total > 1 ? " #" + (k + 1) : ""),
                        compute = spec.compute, cu = spec.compute * CuPerCompute, vramMB = spec.vramMB, watts = spec.watts,
                        online = plugged && powerBlocker == null,
                    };
                    n.why = !plugged ? T("没接电", "No power") : powerBlocker ?? "";
                    house.Add(n); Add(n);
                }
            }
            else
            {
                string key = CardKey("virtual", 0);
                bool plugged = !w.pulled.Contains(key);
                var n = new XgWNode
                {
                    key = key, lane = XgWireLane.Gpu, house = true, unlimited = true, source = plugged ? "psu" : "", shown = true,
                    name = T("本机显卡", "This PC's cards"), compute = 1, cu = double.PositiveInfinity, vramMB = double.PositiveInfinity, watts = 0,
                    online = plugged, why = plugged ? "" : T("没接电", "No power"),
                };
                house.Add(n); Add(n);
            }
            foreach (string key in CafeCards)
            {
                bool plugged = cafeOpen && !w.ajieCutoff && w.rented.Contains(key);
                Add(new XgWNode
                {
                    key = key, lane = XgWireLane.Gpu, cafe = true, source = plugged ? "cafe" : "", available = cafeOpen && !w.ajieCutoff, shown = plugged,
                    name = key == "cafe7" ? T("网吧 7 号机 GTX 960", "Café PC #7 GTX 960") : T("网吧 8 号机 GTX 960", "Café PC #8 GTX 960"),
                    compute = CafeCompute, cu = CafeCompute * CuPerCompute, vramMB = CafeVramMB, watts = CafeWatts, online = plugged,
                    why = plugged ? "" : T("没租", "Not rented"),
                    lockWhy = w.ajieCutoff ? T("阿杰把机器收回去了", "Ajie took his machines back") : AfterAbility(CafeStage, "阿杰的网吧", "Ajie's net café"),
                });
            }
            if (idcOpen)
            {
                double maxCompute = 0, maxVram = 0;
                foreach (var h in house) { maxCompute = Math.Max(maxCompute, h.compute); maxVram = Math.Max(maxVram, h.vramMB); }
                bool plugged = !w.pulled.Contains("rack");
                Add(new XgWNode
                {
                    key = "rack", lane = XgWireLane.Gpu, rack = true, source = plugged ? "idc" : "", shown = true, unlimited = rig == null,
                    name = T("IDC 机架", "IDC rack"), compute = maxCompute * DatacenterVram, cu = maxCompute * DatacenterVram * CuPerCompute,
                    vramMB = maxVram * DatacenterVram, watts = 0, online = plugged, why = plugged ? "" : T("没接电", "No power"),
                });
            }
            else Add(new XgWNode { key = "rack", lane = XgWireLane.Gpu, rack = true, available = false, name = T("IDC 机架", "IDC rack"), lockWhy = AfterAbility(6, "科技树租「机房」", "rent the server room in the tech tree") });

            // Models: deployed checkpoints, 本体, the two training lines.
            foreach (var b in S.best)
            {
                var m = b.acc > 0 ? Model(b.modelId) : null;
                if (m == null) continue;
                double pk = ParamsK(new XgRun { arch = m.arch, depth = Math.Max(1, m.depth), width = m.width });
                var d = XgCatalog.Dataset(m.dataset);
                var a = XgCatalog.Arch(m.arch);
                Add(new XgWNode
                {
                    key = "m:" + m.id, lane = XgWireLane.Model, modelId = m.id, dataset = m.dataset, arch = m.arch, acc = b.acc, paramsK = pk,
                    name = (d == null ? m.dataset : T(d.name, d.nameEn)) + " · " + (a == null ? m.arch : T(a.name, a.nameEn)),
                    sub = Pct(b.acc), vramNeed = ModelVram(pk), cuNeed = ModelCu(pk), shown = true,
                });
            }
            // Repository checkpoints that are not deployed: only 回炉 takes them.
            for (int i = S.models.Count - 1; i >= 0; i--)
            {
                var m = S.models[i];
                if (IsDeployed(m)) continue;
                double pk = ParamsK(new XgRun { arch = m.arch, depth = Math.Max(1, m.depth), width = m.width });
                var a = XgCatalog.Arch(m.arch);
                Add(new XgWNode
                {
                    key = "m:" + m.id, lane = XgWireLane.Model, repo = true, modelId = m.id, dataset = m.dataset, arch = m.arch, acc = m.acc, paramsK = pk,
                    name = m.name, sub = (a == null ? m.arch : T(a.name, a.nameEn)) + " · " + Pct(m.acc) + T(" · 仓库", " · repository"),
                    vramNeed = ModelVram(pk), cuNeed = ModelCu(pk),
                });
            }
            {
                var run = S.sequence;
                double pk = ParamsK(run);
                Add(new XgWNode
                {
                    key = "self", lane = XgWireLane.Model, self = true, modelId = -1, dataset = "*", arch = run.arch, paramsK = pk,
                    name = SelfName() + T(" · 本体", " · itself"), sub = T("对话 / 读写", "talk / read and write"),
                    vramNeed = ModelVram(pk), cuNeed = ModelCu(pk), available = S.stage >= SelfStage, shown = S.stage >= SelfStage,
                    lockWhy = AfterAbility(SelfStage, "它第一次说话", "its first words"),
                });
            }
            foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence })
            {
                var run = Run(track);
                var a = XgCatalog.Arch(run.arch);
                bool open = TrainingUnlocked(track);
                var n = new XgWNode
                {
                    key = "run:" + (int)track, lane = XgWireLane.Model, run = true, modelId = -2 - (int)track, dataset = run.dataset, arch = run.arch,
                    paramsK = ParamsK(run), acc = run.valAcc, vramNeed = VramNeedMB(run), available = open, shown = open,
                    name = (track == XgTrack.Vision ? T("训练线 · 视觉", "Training line · vision") : T("训练线 · 序列", "Training line · sequence")),
                    sub = (a == null ? run.arch : T(a.name, a.nameEn)) + " · d" + run.depth + " w" + XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))],
                    lockWhy = T("先标够 " + XgCatalog.SamplesToTrain + " 条样本", "Label " + XgCatalog.SamplesToTrain + " samples first"),
                };
                foreach (var h in house) if (!HasPair(w.runOff, n.key, h.key)) n.cards.Add(h.key);
                foreach (var p in w.runOn) if (p.model == n.key && v.Node(p.card) != null && v.Node(p.card).cafe) n.cards.Add(p.card);
                Add(n);
            }

            if (sync) SyncPlacements(v, house);
            foreach (var p in w.placed)
            {
                var m = v.Node(p.model);
                if (m == null || m.lane != XgWireLane.Model || m.run || m.repo) continue;
                var c = v.Node(p.card);
                if (c == null || c.lane != XgWireLane.Gpu || !c.available) continue;
                m.card = c.key;
            }

            BuildTasks(v);
            Settle(v, rig, host);
            if (rig != null) rig.UnpluggedWatts = UnpluggedWatts(rig, v);
            wiringView = v;
            wiringEpochMask = ActiveTrainingMask;
            if (host != null) wiringHost = host;
            return v;
        }

        static bool HasPair(List<XgPlacement> list, string model, string card) { foreach (var p in list) if (p.model == model && p.card == card) return true; return false; }

        string SelfName() { string n = Profile != null ? Profile.name : ""; return string.IsNullOrEmpty(n) ? (English ? LingGuangV05.Core.AppNames.AiEn : LingGuangV05.Core.AppNames.AiZh) : n; }

        public static double ModelVram(double paramsK) => ModelBaseMB + Math.Max(0, paramsK) * ModelMBPerK;
        public static double ModelCu(double paramsK) => ModelBaseCu + Math.Max(0, paramsK) * ModelCuPerK;

        static double UnpluggedWatts(IXgRig rig, XgWiringView v)
        {
            double off = 0;
            foreach (var n in v.nodes) if (n.house && n.source != "psu") off += n.watts;
            return off;
        }

        /// <summary>
        /// Default placement: every deployed model (and 本体) without an entry goes on the house card with the most free
        /// VRAM (本体 on the IDC rack once there is one); a new record inherits its predecessor's card; auto entries move
        /// when their card is gone or full. Manual entries are only dropped when their card no longer exists.
        /// </summary>
        void SyncPlacements(XgWiringView v, List<XgWNode> house)
        {
            var w = Wire;
            // Entries for checkpoints that are no longer deployed pass to the dataset's new deployed model.
            for (int i = w.placed.Count - 1; i >= 0; i--)
            {
                var p = w.placed[i];
                var now = v.Node(p.model);
                if (now != null && !now.repo) continue;
                string dataset = p.model.StartsWith("m:", StringComparison.Ordinal) && int.TryParse(p.model.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && Model(id) != null ? Model(id).dataset : null;
                XgWNode heir = null;
                if (dataset != null) foreach (var n in v.nodes) if (n.lane == XgWireLane.Model && !n.run && !n.self && !n.repo && n.dataset == dataset) heir = n;
                if (heir != null && Placement(heir.key) == null) p.model = heir.key; else w.placed.RemoveAt(i);
            }
            // Cards that are gone (sold) release their models.
            w.placed.RemoveAll(p => p.card.Length > 0 && (v.Node(p.card) == null || !v.Node(p.card).available));
            w.pulled.RemoveAll(k => v.Node(k) == null && k.StartsWith("h:", StringComparison.Ordinal));
            w.runOff.RemoveAll(p => v.Node(p.card) == null);
            // 本体 follows the rack while it was placed automatically.
            var rack = v.Node("rack");
            var selfPlace = Placement("self");
            if (selfPlace != null && !selfPlace.manual && rack != null && rack.available && selfPlace.card != "rack") selfPlace.card = "rack";
            // Free VRAM per card from what is placed now.
            var used = new Dictionary<string, double>();
            foreach (var p in w.placed) { var m = v.Node(p.model); if (m != null && p.card.Length > 0) used[p.card] = Used(used, p.card) + m.vramNeed; }
            var todo = new List<XgWNode>();
            foreach (var n in v.nodes) if (n.lane == XgWireLane.Model && !n.run && !n.repo && n.available && Placement(n.key) == null) todo.Add(n);
            todo.Sort((a, b) => b.vramNeed.CompareTo(a.vramNeed));
            foreach (var m in todo)
            {
                string card = m.self && rack != null && rack.available ? "rack" : BestFit(house, used, m.vramNeed, null);
                if (card == null) continue;
                w.placed.Add(new XgPlacement { model = m.key, card = card });
                used[card] = Used(used, card) + m.vramNeed;
            }
            // Auto entries leave a card that is over its VRAM, largest first, for one with room.
            foreach (var c in house)
            {
                if (Used(used, c.key) <= c.vramMB) continue;
                var autos = new List<XgPlacement>();
                foreach (var p in w.placed) if (p.card == c.key && !p.manual && v.Node(p.model) != null) autos.Add(p);
                autos.Sort((a, b) => v.Node(b.model).vramNeed.CompareTo(v.Node(a.model).vramNeed));
                foreach (var p in autos)
                {
                    if (Used(used, c.key) <= c.vramMB) break;
                    double need = v.Node(p.model).vramNeed;
                    string to = BestFit(house, used, need, c.key);
                    if (to == null) continue;
                    used[c.key] = Used(used, c.key) - need; used[to] = Used(used, to) + need; p.card = to;
                }
            }
        }

        static double Used(Dictionary<string, double> used, string card) { return used.TryGetValue(card, out double u) ? u : 0; }

        static string BestFit(List<XgWNode> house, Dictionary<string, double> used, double need, string except)
        {
            XgWNode best = null; double bestFree = double.NegativeInfinity;
            foreach (var c in house)
            {
                if (c.key == except || c.source != "psu") continue;
                double free = c.vramMB - Used(used, c.key);
                if (free + 1e-9 < need) continue;
                if (best == null || free > bestFree + 1e-9 || (Math.Abs(free - bestFree) <= 1e-9 && c.cu > best.cu)) { best = c; bestFree = free; }
            }
            return best?.key;
        }

        XgPlacement Placement(string model) { foreach (var p in Wire.placed) if (p.model == model) return p; return null; }

        XgWNode DeployedNode(XgWiringView v, string dataset)
        {
            var b = Best(dataset);
            return b == null ? null : v.Node("m:" + b.modelId);
        }

        void BuildTasks(XgWiringView v)
        {
            var w = Wire;
            // 标注台 · 自动代标: the global auto level and the desk's own checkpoint (EligibleDesk) are the switch.
            foreach (var desk in XgCatalog.Desks)
            {
                string key = "label:" + desk.id;
                bool open = DeskOpen(desk.id);
                var m = DeployedNode(v, desk.id);
                bool on = open && GlobalAutoLevel > 0 && EligibleDesk(desk.id) && !w.idle.Contains(key);
                Add(v, new XgWNode
                {
                    key = key, lane = XgWireLane.Task, group = T("标注台", "Labelling desk"), name = T(desk.name, desk.nameEn) + T(" · 自动代标", " · auto-label"),
                    dataset = desk.id, min = XgCatalog.AutoMinAccuracy, pay = PayFor(desk.id, LevelOf(desk.id)) * QualityPayMultiplier, unit = T("条", "label"), earns = true,
                    available = open, shown = on || (open && w.idle.Contains(key)), model = on && m != null ? m.key : "", realItems = true,
                    lockWhy = T("标注台还没开这张桌", "This desk is not open yet"), candidate = m?.key ?? "", held = w.idle.Contains(key),
                });
            }
            // 企业订单: signing is the switch.
            foreach (var c in XgCatalog.Contracts)
            {
                if (!Signed(c.id) && !DatasetAvailable(c.dataset)) continue;
                string key = "contract:" + c.id;
                var m = DeployedNode(v, c.dataset);
                bool on = Signed(c.id) && !w.idle.Contains(key);
                Add(v, new XgWNode
                {
                    key = key, lane = XgWireLane.Task, group = T("企业订单", "Business orders"), name = T(c.client, c.clientEn) + " · " + T(c.job, c.jobEn),
                    dataset = c.dataset, min = c.threshold, pay = c.income, unit = T("秒", "s"), realtime = c.realtime, earns = true,
                    shown = Signed(c.id), model = on && m != null ? m.key : "", candidate = m?.key ?? "", held = w.idle.Contains(key),
                });
            }
            // 训练: the training line itself; a checkpoint wired here is loaded into it (回炉).
            foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence })
            {
                string key = "train:" + (int)track;
                bool open = TrainingUnlocked(track);
                Add(v, new XgWNode
                {
                    key = key, lane = XgWireLane.Task, group = T("训练", "Training"), name = track == XgTrack.Vision ? T("回炉训练 · 视觉线", "Training · vision line") : T("回炉训练 · 序列线", "Training · sequence line"),
                    dataset = Run(track).dataset, unit = T("轮", "epoch"), available = open, shown = open, realItems = true,
                    model = open && !w.idle.Contains(key) ? "run:" + (int)track : "", candidate = "run:" + (int)track, held = w.idle.Contains(key), lockWhy = T("先标够 " + XgCatalog.SamplesToTrain + " 条样本", "Label " + XgCatalog.SamplesToTrain + " samples first"),
                });
            }
            Add(v, new XgWNode
            {
                key = "pretrain", lane = XgWireLane.Task, group = T("训练", "Training"), name = T("预训练 · 整个互联网（2016）", "Pre-training · the whole 2016 web"),
                dataset = "*", unit = T("步", "step"), available = S.stage >= 6 && !S.abilities, shown = S.stage >= 6 && !S.abilities,
                model = S.pretrainRunning ? "self" : "", candidate = "self", lockWhy = S.abilities ? T("预训练已完成", "Pre-training is done") : AfterAbility(6, "", ""),
            });
            // 阿杰's receipts, once you told him the truth.
            if (w.ajieJob)
            {
                var m = DeployedNode(v, "mnist");
                bool on = !w.idle.Contains("ajie");
                Add(v, new XgWNode
                {
                    key = "ajie", lane = XgWireLane.Task, group = T("网吧", "Net café"), name = T("阿杰 · 收银小票对账", "Ajie · till receipts"),
                    dataset = "mnist", min = ReceiptMin, pay = ReceiptPay, unit = T("张", "receipt"), earns = true, shown = true,
                    model = on && m != null ? m.key : "", candidate = m?.key ?? "", held = !on,
                });
            }
            // Life: 本体 only.
            // The YY 网吧群 job answered 小刚's danmaku quiz, which was removed; the node is gone with it (netbarChat stays readable in saves).
            if (lifeHooks.TryGetValue(LifeQingwen, out var qw))
                Add(v, Life(LifeQingwen, T("YY · 替我回晴雯", "YY · answer Qingwen for me"), S.stage >= StandInStage && qw.available(), qw.on(), AfterAbility(StandInStage, "", "")));
            AddEraDeliveryTask(v);
        }

        /// <summary>A lock text named after the ability that opens it (the stage is the number of abilities): 有了「看图说词」：…</summary>
        string AfterAbility(int ability, string zh, string en)
        {
            string name = AbilityName(ability, false), nameEn = AbilityName(ability, true);
            return T("有了「" + name + "」" + (zh.Length > 0 ? "：" + zh : ""), "With '" + nameEn + "'" + (en.Length > 0 ? ": " + en : ""));
        }

        static void Add(XgWiringView v, XgWNode n) { v.nodes.Add(n); v.byKey[n.key] = n; }

        XgWNode Life(string key, string name, bool open, bool on, string lockWhy)
        {
            return new XgWNode
            {
                key = key, lane = XgWireLane.Task, group = T("生活", "Life"), name = name, dataset = "*", unit = T("句", "line"),
                available = open, shown = open && on, model = open && on ? "self" : "", candidate = "self", lockWhy = lockWhy,
            };
        }

        /// <summary>Who is online, what fits, how the cards share, and every job's state and speed.</summary>
        void Settle(XgWiringView v, IXgRig rig, IXgHost host)
        {
            // Card ← model placement, VRAM.
            foreach (var m in v.nodes)
            {
                if (m.lane != XgWireLane.Model || m.run || m.card.Length == 0) continue;
                var c = v.Node(m.card);
                c.vramUse += m.vramNeed;
                c.residentVram += m.vramNeed;
            }
            // Jobs that want their model, and the compute they ask of its card.
            foreach (var t in v.nodes)
            {
                if (t.lane != XgWireLane.Task || t.model.Length == 0) continue;
                var m = v.Node(t.model);
                if (m == null) continue;
                t.why = Gate(t, m);
                if (t.why.Length > 0 || m.run) continue;
                var c = m.card.Length > 0 ? v.Node(m.card) : null;
                if (c != null) c.load += m.cuNeed * (t.realtime ? RealtimeWeight : t.key.StartsWith("life.", StringComparison.Ordinal) ? LifeWeight : 1);
            }
            // A waiting line reserves nothing. Each real epoch reserves one model copy on every card that fits it.
            foreach (var run in Runs)
                foreach (var c in v.nodes)
                    if (c.lane == XgWireLane.Gpu && ReservesTrainingMemory(v, run, c))
                        c.vramUse += VramNeedMB(run) / TrainingMemoryMultiplier;
            // Each active training stream requests a full card; inference retains its existing per-job CU weights.
            // Scaling every request by the same bounded share is work-conserving and fair between the two streams.
            if (host != null && host.Blocker == null && host.Compute > 0 && !ProjectActive)
                foreach (var run in Runs)
                    foreach (var c in v.nodes)
                        if (c.lane == XgWireLane.Gpu && ReservesTrainingMemory(v, run, c) &&
                            c.vramUse <= c.vramMB + 1e-6 && !c.unlimited)
                        { c.load += c.cu; c.busy = 1; }
            foreach (var c in v.nodes)
            {
                if (c.lane != XgWireLane.Gpu || !c.available) continue;
                c.share = c.unlimited || c.load <= c.cu ? 1 : c.cu / c.load;
                if (c.online) { if (!c.unlimited) { v.cuOn += c.cu; v.vramOn += c.vramMB; v.cuUse += Math.Min(c.cu, c.load); v.vramUse += c.vramUse; } }
            }
            // Models.
            foreach (var m in v.nodes)
            {
                if (m.lane != XgWireLane.Model || !m.available) continue;
                if (m.repo) { m.why = T("仓库里，没部署", "In the repository, not deployed"); continue; }
                if (m.run)
                {
                    var run = Run((XgTrack)(-2 - m.modelId));
                    bool powered = false, fits = false;
                    foreach (string key in m.cards)
                    {
                        var c = v.Node(key);
                        if (c == null || !c.online) continue;
                        powered = true;
                        if (c.vramUse <= c.vramMB + 1e-6 && AvailableTrainingVram(v, c, run) + 1e-6 >= m.vramNeed) fits = true;
                    }
                    m.online = fits;
                    m.speed = fits ? TrainingFactor(v, (XgTrack)run.track, true) : 0;
                    m.why = fits ? "" : !powered ? T("没接有电的显卡", "No powered card") : T("显存不足：其他任务正在占用", "Insufficient VRAM: other tasks are using it");
                    continue;
                }
                var card = m.card.Length > 0 ? v.Node(m.card) : null;
                if (card == null) m.why = T("没上卡", "Not on a card");
                else if (!card.online) m.why = T("显卡", "Card: ") + card.why;
                else if (card.vramUse > card.vramMB + 1e-6) m.why = T("显存爆了 ", "VRAM full ") + Gb(card.vramUse) + "/" + Gb(card.vramMB) + " GB";
                m.online = m.why.Length == 0;
                m.speed = m.online ? card.share : 0;
            }
            // What the wiring does to each job, whether or not its own switch is on right now.
            foreach (var t in v.nodes)
            {
                if (t.lane != XgWireLane.Task) continue;
                var cm = t.candidate.Length > 0 ? v.Node(t.candidate) : null;
                t.factor = t.held ? 0 : cm == null || !cm.available ? 1 : cm.online ? cm.speed : 0;
            }
            // Tasks.
            foreach (var t in v.nodes)
            {
                if (t.lane != XgWireLane.Task) continue;
                var m = t.model.Length > 0 ? v.Node(t.model) : null;
                if (m == null) { t.state = XgTaskState.Idle; t.why = t.available ? T("等待接入", "Waiting for a wire") : t.lockWhy; t.tag = "IDLE"; continue; }
                v.wired++;
                if (t.why.Length == 0 && !m.online) t.why = T("模型", "Model: ") + m.why;
                if (m.run && t.why.Length == 0)
                {
                    if (host?.Blocker != null) t.why = host.Blocker;
                    else if (ProjectActive) t.why = T("研发占用显卡", "Research is using the cards");
                    else if (host != null && host.Compute <= 0) t.why = T("没有算力", "No compute");
                    else if (!Run((XgTrack)(-2 - m.modelId)).epochActive)
                    {
                        t.state = XgTaskState.Idle; t.tag = T("待命", "Ready");
                        t.why = T("已接线，等待开始训练", "Wired; waiting for an epoch to start");
                        continue;
                    }
                }
                if (t.why.Length > 0) { t.state = XgTaskState.Error; t.tag = t.why; t.bad = true; continue; }
                t.state = XgTaskState.Run;
                t.speed = m.speed;
                v.running++;
                t.tag = t.speed < 1 - 1e-9 ? N(t.speed * 100, "0") + T("% 速", "% speed") : "RUN";
                t.warn = t.speed < 1 - 1e-9;
                Rates(t, m);
            }
            // Card and model tags.
            foreach (var c in v.nodes)
            {
                if (c.lane != XgWireLane.Gpu || !c.available) continue;
                if (!c.online) { c.tag = c.why; c.bad = true; }
                else if (c.vramUse > c.vramMB + 1e-6) { c.tag = T("显存爆了", "VRAM full"); c.bad = true; }
                else if (c.share < 1 - 1e-9 || c.busy > 0) { c.tag = T("满载", "Full load"); c.warn = true; }
                else c.tag = T("在线", "Online");
            }
            foreach (var m in v.nodes)
            {
                if (m.lane != XgWireLane.Model || !m.available) continue;
                if (m.repo) { m.tag = T("仓库", "Repo"); continue; }
                m.tag = m.online ? T("在线", "Online") : m.why;
                m.bad = !m.online && (m.card.Length > 0 || m.run);
            }
            // Power: the house PSU carries the plugged cards and the cases; pre-training on a house card adds its draw.
            double houseCards = 0, unplugged = 0;
            foreach (var c in v.nodes) if (c.house) { if (c.source == "psu") houseCards += c.watts; else unplugged += c.watts; }
            v.psuW = rig != null ? Math.Max(0, rig.HouseWatts - unplugged) : houseCards;
            if (S.pretrainRunning && PretrainSite(v) == "psu") v.psuW += PretrainWatts;
            var psu = v.Node("psu");
            psu.loadW = v.psuW;
            psu.tag = !psu.online ? psu.why : v.psuW > psu.capW ? T("超载", "Overload") : v.psuW > 0 ? T("供电", "Powering") : T("空闲", "Idle");
            psu.bad = !psu.online || v.psuW > psu.capW;
            var cafe = v.Node("cafe");
            foreach (var c in v.nodes) if (c.cafe && c.online) cafe.loadW += c.watts;
            cafe.tag = !cafe.available ? cafe.lockWhy : cafe.loadW > 0 ? (v.night ? T("包夜", "Night rate") : T("供电", "Powering")) : T("空闲", "Idle");
            var idc = v.Node("idc");
            idc.tag = !idc.available ? "" : S.pretrainRunning ? T("计费中", "Billing") : T("空闲", "Idle");
            foreach (var c in v.nodes) if (c.lane == XgWireLane.Gpu && c.cafe && c.online) v.rentPerSecond += CafeRentPerSecond(v.night);
            if (S.pretrainRunning && Has("datacenter")) v.rentPerSecond += DatacenterRent;
            foreach (var t in v.nodes) if (t.state == XgTaskState.Run && t.earns) v.incomePerSecond += TaskIncome(t);
        }

        /// <summary>Why a model cannot do this job (data, accuracy, realtime), or "" — the rules the orders already use.</summary>
        string Gate(XgWNode t, XgWNode m)
        {
            if (t.key.StartsWith("contract:", StringComparison.Ordinal))
            {
                var c = XgCatalog.Contract(t.key.Substring(9));
                if (c != null && ContractAcc(c) + 1e-9 < c.threshold) return RealtimeMiss(c, m) ? T("实时：循环网络跟不上", "Realtime: a loop cannot keep up") : TooLow(ContractAcc(c), c.threshold);
            }
            if (t.key == "ajie" && m.acc + 1e-9 < ReceiptMin) return TooLow(m.acc, ReceiptMin);
            return "";
        }

        /// <summary>A realtime order the deployed model misses because it reads word by word (the rule behind ParallelAcc).</summary>
        static bool RealtimeMiss(XgContract c, XgWNode m)
        {
            if (c == null || !c.realtime) return false;
            var w = WiringOf(m.arch);
            return w == XgWiring.Recurrent || w == XgWiring.GatedRecurrent || w == XgWiring.EncoderDecoder || w == XgWiring.Attention;
        }

        string TooLow(double acc, double min) => T("准确率不够 ", "Accuracy too low ") + N(acc * 100, "0.#") + "% < " + N(min * 100, "0") + "%";

        void Rates(XgWNode t, XgWNode m)
        {
            double acc = m.acc > 0 ? m.acc : 1;
            if (t.key.StartsWith("label:", StringComparison.Ordinal))
            {
                t.rate = GlobalAutoLevel * XgCatalog.AutoRatePerLevel * t.speed;
                var st = CollaborationStats(t.dataset);
                t.errorRate = st.total > 0 ? 1 - (double)st.correct / st.total : 1 - acc;
            }
            else if (t.key.StartsWith("contract:", StringComparison.Ordinal)) { t.rate = (t.realtime ? 2 : 1.2) * t.speed; t.errorRate = 1 - acc; }
            else if (t.key == "ajie") { t.rate = ReceiptRate * t.speed; t.errorRate = 1 - acc; }
            else if (t.key == "pretrain") { t.rate = .6 * t.speed; t.errorRate = 0; }
            else if (t.key.StartsWith("train:", StringComparison.Ordinal)) { t.rate = 0; t.errorRate = 0; }
            else { t.rate = .3 * t.speed; t.errorRate = 0; }
        }

        double TaskIncome(XgWNode t)
        {
            if (t.key.StartsWith("contract:", StringComparison.Ordinal)) { var c = XgCatalog.Contract(t.key.Substring(9)); return c == null ? 0 : ContractIncome(c); }
            if (t.key.StartsWith("label:", StringComparison.Ordinal)) return wiringHost != null ? CollaborationIncome(t.dataset, wiringHost) : 0;
            if (t.key == "ajie") return ReceiptRate * t.speed * ReceiptPay * (1 - t.errorRate);
            return 0;
        }

        public double CafeRentPerSecond(bool night) => CafeRent * Wire.ajieMul * (night ? NightRate : 1);

        /// <summary>Where 本体's card draws its power: "idc", "psu", "cafe", or "" when it is on no working card.</summary>
        static string PretrainSite(XgWiringView v)
        {
            var self = v.Node("self");
            var c = self != null && self.card.Length > 0 ? v.Node(self.card) : null;
            return c == null || !c.available ? "" : c.source;
        }

        static string Gb(double mb) => N(mb / 1024, "0.0");
        static string N(double v, string f) => v.ToString(f, CultureInfo.InvariantCulture);

        // ───────────── tick ─────────────

        /// <summary>The graph is rebuilt this often (game seconds) and at once after any wire changes.</summary>
        public const double WiringRebuildSeconds = .25;
        double wiringAge;
        int wiringEpochMask;
        int ActiveTrainingMask => (S.vision.epochActive ? 1 : 0) | (S.sequence.epochActive ? 2 : 0);

        string HousePowerProblem(IXgRig rig)
        {
            if (rig is IXgHousePower power && !string.IsNullOrEmpty(power.HousePowerBlocker)) return power.HousePowerBlocker;
            return rig != null && rig.BreakerTripped ? T("跳闸", "Tripped") : null;
        }

        void TickWiring(double dt, IXgHost host)
        {
            wiringAge += Math.Max(0, dt);
            var v = wiringView;
            if (v == null || wiringAge >= WiringRebuildSeconds || !ReferenceEquals(wiringHost, host) || wiringEpochMask != ActiveTrainingMask || v.Node("psu").why != (HousePowerProblem(host as IXgRig) ?? "")) { v = BuildWiring(host, host != null); wiringAge = 0; }
            var rig = host as IXgRig;
            // The house PSU: past its limit the breaker trips and every house card goes dark.
            if (rig != null && !rig.BreakerTripped && v.psuW > v.psuLimit + 1e-9)
            {
                bool pretrain = S.pretrainRunning && PretrainSite(v) == "psu";
                if (pretrain) S.pretrainRunning = false;
                rig.TripBreaker();
                Log(T("跳闸！机箱功率 ", "Breaker tripped! The case drew ") + N(v.psuW, "0") + " W > " + N(v.psuLimit, "0") + " W" + T("。", "."), 3);
                if (pretrain) Say(PretrainTripLine());
                v = BuildWiring(host, false);
            }
            if (OfflineSimulation || dt <= 0) return;
            // 包夜 starts at midnight.
            int rentedOnline = 0;
            foreach (var n in v.nodes) if (n.cafe && n.online) rentedOnline++;
            if (rentedOnline > 0 && v.night && !Wire.night) Log(T("00:00 网吧转包夜，租金打六折。", "00:00: the café switches to the night rate, 40% off the rent."), 1);
            Wire.night = v.night;
            if (rentedOnline > 0 && host != null)
            {
                double rent = CafeRentPerSecond(v.night) * rentedOnline * dt;
                if (!host.Spend(rent))
                {
                    Wire.rented.Clear();
                    Log(T("钱不够付网吧的租金：阿杰把 7 号、8 号机关了。", "Not enough money for the café rent: Ajie switched off PCs #7 and #8."), 3);
                    v = BuildWiring(host, false);
                    rentedOnline = 0;
                }
                else { Wire.rentPaid += rent; Wire.rentSeconds += dt; S.totalSpent += rent; }
            }
            if (rentedOnline > 0) TickAjie(dt, v, rentedOnline);
            // 阿杰's receipts pay by the receipt.
            var receipts = v.Node("ajie");
            if (receipts != null && receipts.state == XgTaskState.Run && host != null)
            {
                receiptTimer += ReceiptRate * receipts.speed * dt;
                int guard = 8;
                while (receiptTimer >= 1 && guard-- > 0)
                {
                    receiptTimer -= 1;
                    bool right = Roll() < 1 - receipts.errorRate;
                    if (right) { host.Earn(ReceiptPay); S.totalIncome += ReceiptPay; Wire.receiptsPaid += ReceiptPay; }
                    WireItem?.Invoke("ajie", right);
                }
                if (receiptTimer > 1) receiptTimer = 0;
            }
        }

        double receiptTimer;

        string PretrainTripLine() => Has("datacenter")
            ? T("跳闸了：本体插在家里的电源上，预训练一开，3500W 的机箱扛不住。把本体接到 IDC 机架上。", "The breaker tripped: it sits on a card on the house PSU, and 3500 W cannot carry pre-training. Put it on the IDC rack.")
            : T("跳闸了：预训练一开，3500W 的机箱扛不住。得去 IDC 租「机房」。");

        /// <summary>
        /// Pre-training needs 本体 on the IDC rack. On a house card it trips the breaker (the 3500 W line); on a café card
        /// 阿杰's line refuses. Null when it may run. Without a rig (tests, tools) only the old 机房 rule applies.
        /// </summary>
        string PretrainWiringProblem(IXgHost host, bool start)
        {
            if (!(host is IXgRig rig)) return null;
            // Starting it re-reads the rig (a 机房 bought this frame moves 本体 onto the rack); a running tick uses this tick's graph.
            var v = start || wiringView == null || !ReferenceEquals(wiringHost, host) ? RefreshWiring(host) : wiringView;
            string site = PretrainSite(v);
            var self = v.Node("self");
            if (site == "idc" && self != null && self.online) return null;
            if (site == "psu")
            {
                if (!rig.BreakerTripped)
                {
                    rig.TripBreaker();
                    Log(T("跳闸！机箱功率 ", "Breaker tripped! The case drew ") + N(v.psuW + PretrainWatts, "0") + " W > " + N(v.psuLimit, "0") + " W" + T("。", "."), 3);
                }
                return PretrainTripLine();
            }
            if (site == "cafe") return T("阿杰的网吧电路才 1200W，扛不住预训练。把本体接到 IDC 机架上。", "Ajie's café line is 1200 W; it cannot carry pre-training. Put it on the IDC rack.");
            if (self != null && !self.online && self.card.Length > 0) return T("本体所在的显卡掉线了：", "The card under it is offline: ") + self.why;
            return T("本体没上卡：先把它接到 IDC 机架上。", "It is on no card: put it on the IDC rack first.");
        }

        void TickAjie(double dt, XgWiringView v, int rentedOnline)
        {
            var w = Wire;
            if (w.ajieCutoff || w.ajiePending) return;
            w.ajieClock += dt;
            bool first = w.ajieAsked == 0 && w.ajieClock >= AjieFirstAsk;
            bool again = w.ajieAsked > 0 && !w.ajieTruth && w.ajieLies > 0 && w.ajieClock >= AjieAskAgain && CafeLoad(v) >= AjieSuspicionLoad;
            if (!first && !again) return;
            w.ajiePending = true; w.ajieClock = 0; w.ajieAsked++;
            Log(T("阿杰：", "Ajie: ") + AjieQuestion(w.ajieAsked > 1, rentedOnline), 4);
            AjieAsked?.Invoke(w.ajieAsked > 1);
        }

        /// <summary>How hard the rented café cards work: a training line on them runs them flat out.</summary>
        public static double CafeLoad(XgWiringView v)
        {
            double load = 0; int n = 0;
            foreach (var c in v.nodes)
            {
                if (!c.cafe || !c.online) continue;
                n++;
                load += Math.Max(c.busy, c.cu > 0 ? Math.Min(1, c.load / c.cu) : 0);
            }
            return n == 0 ? 0 : load / n;
        }

        /// <summary>阿杰's question, as he types it in the YY 网吧群 (without his name).</summary>
        public string AjieQuestion(bool suspicious, int cards = 2)
        {
            if (suspicious) return T("你那两台机器一晚上没人坐，风扇转得跟拖拉机一样。说实话，到底在干啥？", "Nobody's sat at those two machines all night and the fans sound like a tractor. Honestly, what are you doing?");
            return cards == 1
                ? T("兄弟，你包的那台 960 用着还顺手不？有点好奇，你拿它干啥呢？", "Bro, is the 960 you rented working out for you? What are you using it for? Just curious.")
                : T("兄弟，你包的那两台 960 用着还顺手不？有点好奇，你拿它们干啥呢？", "Bro, are the two 960s you rented working out for you? What are you using them for? Just curious.");
        }

        /// <summary>The café cards rented and online right now (阿杰 asks about "that one" or "those two").</summary>
        public int CafeCardsOnline { get { int n = 0; var v = wiringView; if (v != null) foreach (var c in v.nodes) if (c.cafe && c.online) n++; return n; } }

        /// <summary>The three answers, in the order the choices show: 在训练一个 AI / 挖矿 / 挂游戏.</summary>
        public static readonly string[] AjieAnswers = { "ai", "mine", "game" };
        public string AjieAnswerText(string answer)
        {
            switch (answer)
            {
                case "ai": return T("在训练一个 AI", "Training an AI");
                case "mine": return T("挖矿", "Mining");
                default: return T("挂游戏", "Idling a game");
            }
        }

        /// <summary>The player answers 阿杰 (from YY or the 接线 log). Returns false when he is not asking.</summary>
        public bool AnswerAjie(string answer)
        {
            var w = Wire;
            if (!w.ajiePending) return false;
            w.ajiePending = false;
            w.ajieClock = 0;
            switch (answer)
            {
                case "ai":
                    w.ajieTruth = true; w.ajieMul = AjieTruthRate; w.ajieJob = true; w.ajieLastLie = "";
                    Log(T("你：在训练一个 AI，教它认字认图。", "You: Training an AI. Teaching it to read numbers and pictures."), 1);
                    AjieSays("AI？……能认数字不？我每天收银小票对账对到半夜。帮我对，机器给你打八折。", "AI? …Can it read numbers? I check the till receipts till midnight every day. Do mine and the machines are 20% off.");
                    Log(T("仓库新增任务「阿杰 · 收银小票对账」。手写数字模型就能接。", "New job in the inventory: Ajie · till receipts. A handwritten-digit model can take it."), 1);
                    break;
                case "mine":
                    w.ajieMul = AjieMiningRate; w.ajieLastLie = "mine";
                    Log(T("你：挖矿。", "You: Mining."), 2);
                    AjieSays("挖矿？那电费得另算，一台加一块五一小时。……挖的啥币？算了，别跟我说。", "Mining? Then power's extra, another one-fifty an hour per machine. …Which coin? Never mind, don't tell me.");
                    Log(T("网吧租金 ×1.6。", "Café rent ×1.6."), 2);
                    break;
                default:
                    w.ajieLies++; w.ajieLastLie = "game";
                    Log(T("你：挂游戏，刷副本。", "You: Idling a game, farming a dungeon."), 2);
                    if (w.ajieLies >= 2)
                    {
                        AjieSays("刷副本要两张显卡满载一晚上？你别在我这儿搞什么名堂。机器我收回了。", "Farming a dungeon takes two cards flat out all night? Don't pull anything in my place. I'm taking the machines back.");
                        w.ajieCutoff = true; w.rented.Clear();
                        Log(T("网吧电路断开：7 号机、8 号机下线。只能把模型挪回家里的卡上。", "The café line is cut: PCs #7 and #8 are offline. Move the models back to the cards at home."), 3);
                    }
                    else AjieSays("……行吧。挂游戏屏幕也得亮着啊。", "…Fine. Even idling a game, the screen would be on.");
                    break;
            }
            RefreshWiring(wiringHost);
            return true;
        }

        void AjieSays(string zh, string en)
        {
            Log(T("阿杰：" + zh, "Ajie: " + en), 4);
            // The YY 网吧群 gets it through the friends' outbox (XgMarketRelay).
            if (S.scOutbox == null) S.scOutbox = new List<XgYYLine>();
            Line("ajie", zh, en);
        }

        /// <summary>A line from the 接线 page itself (a hint, a locked item).</summary>
        public void LogWiring(string text, int tone) { Log(text, tone); }

        void Log(string text, int tone)
        {
            var l = new XgWLog { text = text, tone = tone, at = TimeOfDay };
            wiringLog.Add(l);
            if (wiringLog.Count > WiringLogLength) wiringLog.RemoveAt(0);
            WiringLogged?.Invoke(l);
        }

        // ───────────── the player's wires ─────────────

        /// <summary>Plugs an output into an input. Returns null when it worked, otherwise why not (also logged).</summary>
        public string Connect(string from, string to, IXgHost host)
        {
            if (host != null) wiringHost = host;
            var v = RefreshWiring(wiringHost);
            var a = v.Node(from); var b = v.Node(to);
            if (a == null || b == null) return Refuse(T("接不上：节点不存在", "Cannot connect: no such node"));
            if (!a.available) return Refuse(a.name + T(" 还没解锁：", " is locked: ") + a.lockWhy);
            if (!b.available) return Refuse(b.name + T(" 还没解锁：", " is locked: ") + b.lockWhy);
            if ((int)b.lane != (int)a.lane + 1) return Refuse(T("接不上：", "Cannot connect: ") + LaneName(a.lane) + T("只能接", " only goes into ") + LaneName(a.lane == XgWireLane.Task ? a.lane : a.lane + 1));
            string why = a.lane == XgWireLane.Power ? PlugCard(a, b) : a.lane == XgWireLane.Gpu ? PlaceModel(a, b) : WireJob(a, b, wiringHost);
            if (why != null) return Refuse(why);
            Log(T("接线：", "Wired: ") + a.name + " → " + b.name, 1);
            RefreshWiring(wiringHost);
            return null;
        }

        /// <summary>Unplugs one wire (the page passes the wire the player clicked).</summary>
        public bool Disconnect(string from, string to, IXgHost host)
        {
            if (host != null) wiringHost = host;
            var v = RefreshWiring(wiringHost);
            var a = v.Node(from); var b = v.Node(to);
            if (a == null || b == null) return false;
            var w = Wire;
            switch (a.lane)
            {
                case XgWireLane.Power:
                    if (b.cafe) w.rented.Remove(b.key);
                    else if (!w.pulled.Contains(b.key)) w.pulled.Add(b.key);
                    break;
                case XgWireLane.Gpu:
                    if (b.run)
                    {
                        if (a.cafe) w.runOn.RemoveAll(p => p.model == b.key && p.card == a.key);
                        else if (!HasPair(w.runOff, b.key, a.key)) w.runOff.Add(new XgPlacement { model = b.key, card = a.key, manual = true });
                    }
                    else { var p = Placement(b.key); if (p == null) w.placed.Add(new XgPlacement { model = b.key, card = "", manual = true }); else { p.card = ""; p.manual = true; } }
                    break;
                case XgWireLane.Model:
                    if (b.key == "pretrain") { if (S.pretrainRunning) TogglePretrain(wiringHost); }
                    else if (b.key == LifeNetbar) w.netbarChat = false;
                    else if (lifeHooks.TryGetValue(b.key, out var hook)) hook.set(false);
                    else if (!w.idle.Contains(b.key)) w.idle.Add(b.key);
                    break;
                default: return false;
            }
            Log(T("拔线：", "Unplugged: ") + a.name + " → " + b.name, 0);
            RefreshWiring(wiringHost);
            return true;
        }

        string Refuse(string why) { Log(why, 3); return why; }

        string LaneName(XgWireLane lane)
        {
            switch (lane)
            {
                case XgWireLane.Power: return T("电力", "power");
                case XgWireLane.Gpu: return T("显卡", "a card");
                case XgWireLane.Model: return T("模型", "a model");
                default: return T("任务", "a job");
            }
        }

        string PlugCard(XgWNode power, XgWNode card)
        {
            var w = Wire;
            if (card.cafe)
            {
                if (power.key != "cafe") return T("网吧的机器在网吧：只能接阿杰的电路", "The café PCs are at the café: only Ajie's line powers them");
                if (w.ajieCutoff) return T("阿杰把机器收回去了", "Ajie took his machines back");
                if (!w.rented.Contains(card.key)) w.rented.Add(card.key);
                Log(T("租下 ", "Rented ") + card.name + T("：¥", ": ¥") + N(CafeRentPerSecond(CafeNight), "0.00") + T("/秒，电费含在里面。", "/s, power included."), 2);
                return null;
            }
            if (card.rack)
            {
                if (power.key != "idc") return T("机架在机房：只能接 IDC 机柜", "The rack is in the server room: only the IDC rack power feeds it");
                w.pulled.Remove("rack");
                return null;
            }
            if (power.key != "psu") return power.key == "cafe" ? T("家里的卡搬不到网吧去", "Your own cards do not go to the café") : T("家里的卡不进机房：机房有自己的机架", "Your own cards stay home: the server room has its own rack");
            w.pulled.Remove(card.key);
            return null;
        }

        string PlaceModel(XgWNode card, XgWNode model)
        {
            var w = Wire;
            if (model.run)
            {
                if (card.rack) return T("机架是租来跑预训练的：训练线接家里的卡或网吧的卡", "The rack is rented for pre-training: training lines use your cards or the café's");
                if (card.cafe) { if (!HasPair(w.runOn, model.key, card.key)) w.runOn.Add(new XgPlacement { model = model.key, card = card.key, manual = true }); }
                else w.runOff.RemoveAll(p => p.model == model.key && p.card == card.key);
                return null;
            }
            if (model.repo) return T("没部署的检查点不用上卡：接到「回炉训练」上就是加载它", "An undeployed checkpoint needs no card: wire it into training to load it");
            var p = Placement(model.key);
            if (p == null) w.placed.Add(new XgPlacement { model = model.key, card = card.key, manual = true });
            else { p.card = card.key; p.manual = true; }
            var after = RefreshWiring(wiringHost).Node(card.key);
            if (after != null && after.vramUse > after.vramMB + 1e-6)
                Log(T("显存爆了：", "VRAM full: ") + card.name + " " + Gb(after.vramUse) + "/" + Gb(after.vramMB) + " GB" + T("，上面的模型全掉线。", "; every model on it drops out."), 3);
            return null;
        }

        string WireJob(XgWNode model, XgWNode task, IXgHost host)
        {
            var w = Wire;
            string key = task.key;
            if (key == "pretrain")
            {
                if (!model.self) return T("预训练只接本体", "Pre-training only takes it, itself");
                if (S.pretrainRunning) return null;
                if (!TogglePretrain(host)) return S.log.Count > 0 ? S.log[S.log.Count - 1] : T("预训练开不了", "Pre-training cannot start");
                return null;
            }
            if (key.StartsWith("life.", StringComparison.Ordinal))
            {
                if (!model.self) return T("生活里的事只有本体会做", "Only it, itself, does the life jobs");
                if (key == LifeNetbar) { w.netbarChat = true; return null; }
                if (lifeHooks.TryGetValue(key, out var hook)) { hook.set(true); return hook.on() ? null : T("现在开不了", "Not available now"); }
                return T("接不上", "Cannot connect");
            }
            if (key.StartsWith("train:", StringComparison.Ordinal))
            {
                int track = key[6] - '0';
                if (model.run)
                {
                    if (model.key != "run:" + track) return T("数据不对口：这是", "Wrong data: this is the ") + (track == 0 ? T("视觉线", "vision line") : T("序列线", "sequence line"));
                    w.idle.Remove(key);
                    return null;
                }
                if (model.self) return T("本体就是序列线在练的那颗脑子：直接接「训练线 · 序列」", "It is what the sequence line trains: wire the sequence training line instead");
                var m = Model(model.modelId);
                if (m == null || m.track != track) return T("数据不对口：这是", "Wrong data: this is the ") + (track == 0 ? T("视觉线", "vision line") : T("序列线", "sequence line"));
                if (!LoadModel(m.id)) return S.log.Count > 0 ? S.log[S.log.Count - 1] : T("加载不了", "Cannot load it");
                w.idle.Remove(key);
                return null;
            }
            if (model.self) return T("本体不接标注和订单", "It does not take labelling or orders itself");
            if (model.repo) return T("这个检查点没部署：订单和代标只认部署的那个", "This checkpoint is not deployed: orders and labelling use the deployed one");
            if (model.run) return T("训练线不接活：先评估，存成检查点", "A training line takes no jobs: assess it into a checkpoint first");
            if (task.dataset != model.dataset) return T("数据不对口：要 ", "Wrong data: needs ") + DatasetName(task.dataset);
            if (key.StartsWith("label:", StringComparison.Ordinal))
            {
                if (AutoLabelHidden) return T("也许有更省力的办法……");
                if (GlobalAutoLevel <= 0) return T("先在科技买「自动标注」", "Buy auto-labelling in the tech tree first");
                if (BestAcc(task.dataset) < XgCatalog.AutoMinAccuracy) return TooLow(BestAcc(task.dataset), XgCatalog.AutoMinAccuracy);
                w.idle.Remove(key);
                return null;
            }
            if (key.StartsWith("contract:", StringComparison.Ordinal))
            {
                var c = XgCatalog.Contract(key.Substring(9));
                if (c == null) return T("接不上", "Cannot connect");
                if (!Signed(c.id))
                {
                    if (!CanSign(c)) return RealtimeMiss(c, model) ? T("实时：循环网络跟不上", "Realtime: a loop cannot keep up") : TooLow(ContractAcc(c), c.threshold);
                    if (!Sign(c.id, host)) return T("签不了", "Cannot sign");
                }
                w.idle.Remove(key);
                return null;
            }
            if (key == "ajie")
            {
                if (model.acc + 1e-9 < ReceiptMin) return TooLow(model.acc, ReceiptMin);
                w.idle.Remove(key);
                return null;
            }
            return T("接不上", "Cannot connect");
        }

        string DatasetName(string id) { var d = XgCatalog.Dataset(id); return d == null ? id : T(d.name, d.nameEn); }

        /// <summary>合闸 from the 接线 page: refused while the house PSU would still be over its limit.</summary>
        public bool ResetBreakerFromWiring(IXgHost host)
        {
            if (host != null) wiringHost = host;
            if (!(wiringHost is IXgRig rig)) return false;
            var v = RefreshWiring(wiringHost);
            if (!rig.BreakerTripped) return false;
            if (v.psuW > v.psuLimit + 1e-9) { Log(T("还是 ", "Still ") + N(v.psuW, "0") + T(" W，合不上闸。先拔线。", " W; the breaker will not close. Unplug something first."), 3); return false; }
            if (!rig.ResetBreaker()) { Log(T("合不上闸：家里的负载还是太高。", "The breaker will not close: the house load is still too high."), 3); return false; }
            Log(T("合闸，显卡重新上电。", "Breaker closed; the cards have power again."), 1);
            RefreshWiring(wiringHost);
            return true;
        }
    }
}
