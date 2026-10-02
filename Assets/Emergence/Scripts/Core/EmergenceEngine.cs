using System;
using System.Collections.Generic;

namespace Emergence
{
    /// <summary>Deterministic rules. Animation never writes into this state.</summary>
    public sealed class EmergenceEngine
    {
        public RunState State { get; private set; }
        public string LastError { get; private set; }
        public const int Radius = 4;
        public const int RoundCount = 18;
        static readonly string[] BossNames = { "隔膜", "节拍器", "静默", "迟滞", "遗忘", "双生" };
        static readonly string[] BossDescriptions = {
            "隔膜两侧的天然连接失效。专用导线与记忆桥可以穿过。",
            "第 1 拍放电不获得基础灵感；节点变体和突触依然生效。",
            "最左侧突触本轮失效。开战前调整槽位顺序。",
            "右侧区域发出的信号额外延迟 1 拍。",
            "第一次刺激结束后，所有未放电节点的电位清零。",
            "两枚距离最远的节点都放电时，额外获得 100 灵感。" };

        public EmergenceEngine(int seed = 20261001) { NewRun(seed); }
        public EmergenceEngine(RunState saved)
        {
            State = saved ?? new RunState();
            EnsureStructures();
            LastError = "";
        }
        public RunState NewRun(int seed)
        {
            State = new RunState { seed = seed, rngState = seed == 0 ? 12648430 : seed, phase = RunPhase.Grow };
            AddNode(0, 0, NodeKind.Normal, NodeVariant.None);
            AddNode(-1, 0, NodeKind.Normal, NodeVariant.None);
            AddNode(0, -1, NodeKind.Normal, NodeVariant.None);
            AddNode(-1, -1, NodeKind.Core, NodeVariant.None);
            AddNode(-2, -1, NodeKind.Sensitive, NodeVariant.None);
            AddNode(-3, 0, NodeKind.Normal, NodeVariant.None);
            AddNode(1, 0, NodeKind.Normal, NodeVariant.None);
            State.synapses.Add(new SynapseState { id = "J01" });
            State.inventory.Add(new ItemState { id = "M05" });
            EnsureStructures();
            BeginRound();
            LastError = "";
            return State;
        }
        void EnsureStructures()
        {
            foreach (string id in Catalog.StructureIds)
                if (!State.structures.Exists(s => s.id == id)) State.structures.Add(new StructureLevel { id = id });
        }
        public static int Distance(int q1, int r1, int q2, int r2)
        {
            return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(q1 + r1 - q2 - r2)) / 2;
        }
        public static bool IsCell(int q, int r) { return Distance(0, 0, q, r) <= Radius; }
        public NodeState FindNode(int id) { return State.nodes.Find(n => n.id == id); }
        public int StructureLevel(string id) { var s = State.structures.Find(x => x.id == id); return s == null ? 1 : s.level; }
        public bool IsBossRound { get { return State.round % 3 == 0; } }
        int BossIndex { get { return Math.Max(0, Math.Min(5, (State.round - 1) / 3)); } }
        bool Boss(int index) { return IsBossRound && BossIndex == index; }
        bool Fail(string text) { LastError = text; State.lastMessage = text; return false; }
        bool Success(string text = "") { LastError = ""; State.lastMessage = text; return true; }
        bool CanEdit { get { return State.phase == RunPhase.Shop || ((State.phase == RunPhase.Prepare || State.phase == RunPhase.Grow) && State.clicksRemaining == 2); } }
        int NextRandom(int maximum)
        {
            uint x = unchecked((uint)State.rngState);
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            State.rngState = unchecked((int)x);
            return maximum <= 1 ? 0 : (int)(x % (uint)maximum);
        }
        NodeState AddNode(int q, int r, NodeKind kind, NodeVariant variant)
        {
            var n = new NodeState { id = State.nextNodeId++, q = q, r = r, kind = kind, variant = variant };
            State.nodes.Add(n);
            return n;
        }
        void BeginRound()
        {
            State.phase = RunPhase.Grow;
            State.target = Catalog.RoundTargets[Math.Max(0, Math.Min(Catalog.RoundTargets.Length - 1, State.round - 1))];
            State.roundScore = 0;
            State.clicksRemaining = 2;
            State.movesRemaining = IsBossRound ? 2 : 1;
            State.drugsUsed = 0;
            State.drugs.Clear();
            State.twinAwarded = false; State.twinA = -1; State.twinB = -1; State.bossRelaxed = false;
            State.bossName = BossNames[BossIndex]; State.bossDescription = BossDescriptions[BossIndex];
            foreach (var n in State.nodes) { n.charge = 0; n.fired = false; n.layer = 0; n.coreSources = 0; n.contributions.Clear(); }
            State.candidates.Clear();
            for (int i = 0; i < 3; i++)
            {
                NodeKind kind;
                if (State.round == 1) kind = i == 0 ? NodeKind.Sensitive : i == 1 ? NodeKind.Normal : NodeKind.Delay;
                else if (i == 0) kind = NextRandom(2) == 0 ? NodeKind.Normal : NodeKind.Sensitive;
                else kind = (NodeKind)NextRandom(6);
                if (kind == NodeKind.Core && State.nodes.FindAll(n => n.kind == NodeKind.Core).Count >= 2) kind = NodeKind.Capacitor;
                int roll = NextRandom(100);
                NodeVariant variant = roll < 70 ? NodeVariant.None : roll < 83 ? NodeVariant.Gold : roll < 96 ? NodeVariant.Resonant : NodeVariant.Multiple;
                if (State.round == 1) variant = i == 0 ? NodeVariant.Gold : NodeVariant.None;
                State.candidates.Add(new NodeCandidate { kind = kind, variant = variant });
            }
            State.lastMessage = "选择一个新神经元，放到空格上。";
            UpdateTwinTargets();
        }
        public bool PlaceCandidate(int index, int q, int r)
        {
            if (State.phase != RunPhase.Grow) return Fail("本轮已经完成生长。");
            if (index < 0 || index >= State.candidates.Count) return Fail("请先选择一个候选神经元。");
            if (!IsCell(q, r) || State.nodes.Exists(n => n.q == q && n.r == r)) return Fail("请选择棋盘内的空格。");
            var candidate = State.candidates[index];
            AddNode(q, r, candidate.kind, candidate.variant);
            State.candidates.Clear(); State.phase = RunPhase.Prepare;
            UpdateTwinTargets();
            return Success("生长完成。选起点，或先用记忆改变网络。");
        }
        public bool MoveNode(int nodeId, int q, int r)
        {
            if (!CanEdit || State.phase == RunPhase.Shop) return Fail("只能在本轮首次刺激前移动节点。");
            if (State.movesRemaining <= 0) return Fail("本轮免费移动次数已用完。");
            var n = FindNode(nodeId);
            if (n == null) return Fail("请选择要移动的节点。");
            if (!IsCell(q, r) || State.nodes.Exists(x => x.q == q && x.r == r)) return Fail("请选择棋盘内的空格。");
            n.q = q; n.r = r; State.movesRemaining--; State.bossRelaxed = false;
            UpdateTwinTargets();
            return Success("节点已移动，天然连接重新生成。");
        }
        EdgeModification Modification(int a, int b)
        {
            var m = State.modifications.Find(e => e.sourceId == a && e.targetId == b);
            if (m == null) { m = new EdgeModification { sourceId = a, targetId = b }; State.modifications.Add(m); }
            return m;
        }
        static string EdgeKey(int a, int b) { return a + ":" + b; }
        public List<EdgeState> GetEdges()
        {
            var edges = new Dictionary<string, EdgeState>();
            foreach (var a in State.nodes)
                foreach (var b in State.nodes)
                    if (a.id != b.id && Distance(a.q, a.r, b.q, b.r) <= (a.kind == NodeKind.Projector ? 2 : 1))
                        edges[EdgeKey(a.id, b.id)] = new EdgeState { sourceId = a.id, targetId = b.id };
            foreach (var m in State.modifications)
                if (m.bridge && FindNode(m.sourceId) != null && FindNode(m.targetId) != null)
                    UpsertEdge(edges, m.sourceId, m.targetId, "记忆");
            for (int i = 0; i < State.synapses.Count; i++)
            {
                var s = State.synapses[i];
                if ((Boss(2) && i == 0) || FindNode(s.sourceId) == null || FindNode(s.targetId) == null) continue;
                if (s.id == "J15") { UpsertEdge(edges, s.sourceId, s.targetId, "导线"); UpsertEdge(edges, s.targetId, s.sourceId, "导线"); }
                if (s.id == "J16") UpsertEdge(edges, s.sourceId, s.targetId, "透镜");
            }
            foreach (var drug in State.drugs)
            {
                var source = FindNode(drug.nodeId);
                if (source == null || drug.id != "P03") continue;
                foreach (var b in State.nodes)
                    if (!b.fired && b.id != source.id && Distance(source.q, source.r, b.q, b.r) <= 2) UpsertEdge(edges, source.id, b.id, "药品");
            }
            foreach (var m in State.modifications)
            {
                EdgeState e;
                if (!edges.TryGetValue(EdgeKey(m.sourceId, m.targetId), out e)) continue;
                e.blocked = m.blocked; e.echo |= m.echo; e.delay = Math.Min(3, 1 + m.extraDelay);
            }
            foreach (var e in edges.Values)
            {
                var a = FindNode(e.sourceId); var b = FindNode(e.targetId);
                if (Boss(0) && !State.bossRelaxed && e.origin == "天然" && (a.q < 0) != (b.q < 0)) e.blocked = true;
                if (Boss(3) && a.q >= 1) e.delay = Math.Min(4, e.delay + 1);
                foreach (var drug in State.drugs)
                    if (drug.nodeId == e.sourceId) { if (drug.id == "P01") e.strength++; if (drug.id == "P02") e.echo = true; }
            }
            var result = new List<EdgeState>(edges.Values);
            result.Sort((a, b) => a.sourceId == b.sourceId ? a.targetId.CompareTo(b.targetId) : a.sourceId.CompareTo(b.sourceId));
            return result;
        }
        static void UpsertEdge(Dictionary<string, EdgeState> edges, int a, int b, string origin)
        {
            string key = EdgeKey(a, b);
            EdgeState e;
            if (!edges.TryGetValue(key, out e)) { e = new EdgeState { sourceId = a, targetId = b }; edges[key] = e; }
            e.origin = origin;
        }
        public bool ApplyItem(int inventoryIndex, int sourceId, int targetId = -1)
        {
            if (inventoryIndex < 0 || inventoryIndex >= State.inventory.Count) return Fail("请选择背包中的物品。");
            string id = State.inventory[inventoryIndex].id;
            var content = Catalog.Get(id);
            if (content.category == ContentCategory.Drug)
            {
                if (State.phase != RunPhase.Prepare) return Fail("药品只能在准备阶段使用。");
                if (State.drugsUsed >= 1) return Fail("每轮最多使用一份药品。");
            }
            else if (!CanEdit) return Fail("记忆与星图只能在首次刺激前或商店中使用。");
            NodeState a = FindNode(sourceId), b = FindNode(targetId);
            if (content.targetCount >= 1 && a == null) return Fail("请选择物品的目标神经元。");
            if (content.targetCount >= 2 && (b == null || b.id == a.id)) return Fail("请选择另一个目标神经元。");
            if (content.category == ContentCategory.Drug && a.fired) return Fail("该节点已经休眠，药品请选择未放电节点。");
            EdgeState edge = content.targetCount == 2 ? GetEdges().Find(e => e.sourceId == a.id && e.targetId == b.id && !e.blocked) : null;
            switch (id)
            {
                case "M01":
                    if (Distance(a.q, a.r, b.q, b.r) <= 1) return Fail("初次相遇需要两个不相邻的节点。");
                    var ma = State.modifications.Find(e => e.sourceId == a.id && e.targetId == b.id);
                    var mb = State.modifications.Find(e => e.sourceId == b.id && e.targetId == a.id);
                    if ((ma != null && ma.blocked) || (mb != null && mb.blocked)) return Fail("这对节点已被永久遗忘，无法再次连接。");
                    if (ma != null && ma.bridge && mb != null && mb.bridge) return Fail("这两个节点已有永久双向桥。");
                    Modification(a.id, b.id).bridge = true; Modification(b.id, a.id).bridge = true;
                    break;
                case "M02":
                    if (edge == null && !GetEdges().Exists(e => e.sourceId == b.id && e.targetId == a.id && !e.blocked)) return Fail("这两个节点之间没有可封锁的连接。");
                    Modification(a.id, b.id).blocked = true; Modification(b.id, a.id).blocked = true;
                    break;
                case "M03": if (a.variant == NodeVariant.Gold) return Fail("这个节点已经镀金。"); a.variant = NodeVariant.Gold; break;
                case "M04": if (a.variant == NodeVariant.Resonant) return Fail("这个节点已经共振。"); a.variant = NodeVariant.Resonant; break;
                case "M05":
                    if (edge == null) return Fail("请选择一条有效的有向连接。");
                    if (edge.echo) return Fail("这条连接已经具有回声。");
                    Modification(a.id, b.id).echo = true; break;
                case "M06":
                    if (a.kind != NodeKind.Normal && a.kind != NodeKind.Sensitive) return Fail("重新理解只能转换普通与敏感节点。");
                    a.kind = a.kind == NodeKind.Normal ? NodeKind.Sensitive : NodeKind.Normal; break;
                case "M07":
                    if (edge == null || edge.delay >= 3) return Fail("请选择一条延迟未达到 3 拍的有效连接。");
                    Modification(a.id, b.id).extraDelay++; break;
                case "M08":
                    if (a.kind != NodeKind.Normal) return Fail("觉醒只能用于普通节点。");
                    if (State.nodes.FindAll(n => n.kind == NodeKind.Core).Count >= 2) return Fail("棋盘最多容纳两个核心。");
                    a.kind = NodeKind.Core; break;
                case "M11": if (a.variant == NodeVariant.Multiple) return Fail("这个节点已经拥有多重变体。"); a.variant = NodeVariant.Multiple; break;
                default:
                    if (content.category == ContentCategory.Star)
                    {
                        var level = State.structures.Find(s => s.id == id);
                        if (level == null) return Fail("未找到对应结构型。");
                        level.level++;
                    }
                    else if (content.category == ContentCategory.Drug)
                    {
                        if (id != "P04" && !GetEdges().Exists(e => e.sourceId == a.id && !e.blocked) && id != "P03") return Fail("这个节点没有有效出边。");
                        if (id == "P02" && !GetEdges().Exists(e => e.sourceId == a.id && !e.blocked && !e.echo)) return Fail("这个节点的有效出边已经全部具有回声。");
                        if (id == "P03" && !State.nodes.Exists(n => n.id != a.id && !n.fired && Distance(a.q, a.r, n.q, n.r) <= 2)) return Fail("半径 2 内没有未放电的目标。");
                        if (id == "P03")
                        {
                            var existingEdges = GetEdges();
                            bool useful = State.nodes.Exists(n => n.id != a.id && !n.fired && Distance(a.q, a.r, n.q, n.r) <= 2
                                && !State.modifications.Exists(m => m.sourceId == a.id && m.targetId == n.id && m.blocked)
                                && !existingEdges.Exists(e => e.sourceId == a.id && e.targetId == n.id && !e.blocked));
                            if (!useful) return Fail("半径 2 内没有可新增的有效连接。");
                        }
                        State.drugs.Add(new DrugEffect { id = id, nodeId = a.id }); State.drugsUsed++;
                    }
                    else return Fail("该物品不能主动使用。");
                    break;
            }
            State.inventory.RemoveAt(inventoryIndex);
            if (content.category == ContentCategory.Memory) State.memoriesUsed++;
            State.bossRelaxed = false;
            return Success("已使用「" + content.name + "」。");
        }
        public bool SwapSynapses(int a, int b)
        {
            if (!CanEdit) return Fail("首次刺激后，突触顺序已经锁定。");
            if (a < 0 || b < 0 || a >= State.synapses.Count || b >= State.synapses.Count || a == b) return Fail("请选择两个不同的突触槽。");
            var t = State.synapses[a]; State.synapses[a] = State.synapses[b]; State.synapses[b] = t;
            return Success("突触顺序已调整。");
        }
        public bool BindSynapse(int index, int sourceId, int targetId)
        {
            if (!CanEdit) return Fail("首次刺激后，突触绑定已经锁定。");
            if (index < 0 || index >= State.synapses.Count) return Fail("请选择结构型突触。");
            var s = State.synapses[index]; var a = FindNode(sourceId); var b = FindNode(targetId);
            if (s.id != "J15" && s.id != "J16") return Fail("这个突触不需要绑定。");
            if (a == null || b == null || a.id == b.id) return Fail("请选择两个不同的节点。");
            int d = Distance(a.q, a.r, b.q, b.r);
            if (s.id == "J16" && (d < 2 || d > 3)) return Fail("透镜只能连接相距 2–3 格的节点。");
            if (State.modifications.Exists(m => m.sourceId == a.id && m.targetId == b.id && m.blocked) || (s.id == "J15" && State.modifications.Exists(m => m.sourceId == b.id && m.targetId == a.id && m.blocked))) return Fail("目标连接已被永久遗忘。");
            s.sourceId = a.id; s.targetId = b.id; State.bossRelaxed = false;
            return Success("「" + Catalog.Get(s.id).name + "」已完成绑定。");
        }

        public bool EndRound()
        {
            if (State.phase != RunPhase.Prepare) return Fail("当前无法结算本轮。");
            if (State.roundScore < State.target) return Fail("还差 " + (State.target - State.roundScore) + " 分，请继续刺激网络。");
            int interest = Math.Min(5, State.currency / 5);
            int overkill = Math.Min(3, Math.Max(0, State.roundScore / Math.Max(1, State.target) - 1));
            State.lastReward = (IsBossRound ? 6 : 4) + (State.clicksRemaining == 1 ? 1 : 0) + interest + overkill;
            State.currency += State.lastReward;
            State.totalScore = SaturatedAdd(State.totalScore, State.roundScore);
            State.history.Add("第 " + State.round + " 轮 · " + State.roundScore + " 分 · " + State.lastStructure);
            if (State.history.Count > RoundCount) State.history.RemoveAt(0);
            State.drugs.Clear();
            if (State.round == RoundCount)
            {
                State.phase = RunPhase.Victory;
                return Success("神经网络已经涌现。十八轮挑战完成！");
            }
            State.phase = RunPhase.Shop;
            State.refreshCost = 2; State.bossRewardClaimed = false;
            GenerateShop(true);
            return Success("本轮完成，获得 " + State.lastReward + " 资源。" + (IsBossRound ? "Boss 奖励：免费选择一个突触。" : ""));
        }
        public bool NextRound()
        {
            if (State.phase != RunPhase.Shop) return Fail("请先完成本轮挑战。");
            State.round++; State.shop.Clear(); BeginRound();
            return Success("第 " + State.round + " 轮开始：新增一枚神经元。");
        }
        string RollContent(string[] ids, HashSet<string> used, bool allowRare = true)
        {
            var pool = new List<string>();
            bool rare = allowRare && NextRandom(100) < 25;
            foreach (string id in ids) if (!used.Contains(id) && Catalog.Get(id).rare == rare) pool.Add(id);
            if (pool.Count == 0) foreach (string id in ids) if (!used.Contains(id)) pool.Add(id);
            string pick = pool[NextRandom(pool.Count)]; used.Add(pick); return pick;
        }
        void GenerateShop(bool includeBoss)
        {
            var heldRewards = State.shop.FindAll(o => o.bossReward);
            State.shop.Clear();
            var used = new HashSet<string>();
            foreach (var reward in heldRewards) used.Add(reward.id);
            AddOffer(RollContent(Catalog.SynapseIds, used));
            AddOffer(RollContent(Catalog.SynapseIds, used));
            AddOffer(RollContent(Catalog.MemoryIds, used));
            string star = Catalog.StructureIds[NextRandom(Catalog.StructureIds.Length)];
            // A useful star is always available for the most recently achieved structure.
            int last = Array.IndexOf(Catalog.StructureNames, State.lastStructure);
            if (last >= 0 && NextRandom(100) < 65) star = Catalog.StructureIds[last];
            AddOffer(star);
            AddOffer(RollContent(Catalog.DrugIds, used));
            if (heldRewards.Count > 0) State.shop.AddRange(heldRewards);
            else if (includeBoss && IsBossRound)
                for (int i = 0; i < 3; i++) State.shop.Add(new ShopOffer { id = RollContent(Catalog.SynapseIds, used), price = 0, bossReward = true });
        }
        void AddOffer(string id) { State.shop.Add(new ShopOffer { id = id, price = Catalog.Get(id).price }); }
        public bool BuyOffer(int index)
        {
            if (State.phase != RunPhase.Shop) return Fail("只能在商店购买物品。");
            if (index < 0 || index >= State.shop.Count) return Fail("请选择一件商品。");
            var offer = State.shop[index]; var content = Catalog.Get(offer.id);
            if (offer.sold) return Fail("这件商品已经售出。");
            if (offer.bossReward && State.bossRewardClaimed) return Fail("本次 Boss 奖励已领取。");
            if (State.currency < offer.price) return Fail("资源不足，还差 " + (offer.price - State.currency) + "。");
            if (content.category == ContentCategory.Synapse)
            {
                if (State.synapses.Count >= 5) return Fail("五个突触槽已满，请先出售一个。");
                State.synapses.Add(new SynapseState { id = offer.id });
            }
            else
            {
                if (State.inventory.FindAll(item => Catalog.Get(item.id).category == content.category).Count >= 2) return Fail("此类消耗品最多携带两份，请先使用或弃置。");
                State.inventory.Add(new ItemState { id = offer.id });
            }
            State.currency -= offer.price; offer.sold = true;
            if (offer.bossReward)
            {
                State.bossRewardClaimed = true;
                foreach (var item in State.shop) if (item.bossReward) item.sold = true;
            }
            return Success("获得「" + content.name + "」。");
        }
        public bool RerollShop()
        {
            if (State.phase != RunPhase.Shop) return Fail("只能在商店刷新商品。");
            if (State.currency < State.refreshCost) return Fail("资源不足，刷新需要 " + State.refreshCost + "。");
            State.currency -= State.refreshCost; State.refreshCost++;
            GenerateShop(false);
            return Success("商店已刷新。");
        }
        public bool SellSynapse(int index)
        {
            if (!CanEdit) return Fail("首次刺激后，无法出售突触。");
            if (index < 0 || index >= State.synapses.Count) return Fail("请选择要出售的突触。");
            var content = Catalog.Get(State.synapses[index].id);
            State.synapses.RemoveAt(index); State.currency += content.price / 2;
            return Success("已出售「" + content.name + "」，获得 " + content.price / 2 + " 资源。");
        }
        public bool DiscardItem(int index)
        {
            if (State.phase != RunPhase.Grow && State.phase != RunPhase.Prepare && State.phase != RunPhase.Shop) return Fail("当前无法整理背包。");
            if (index < 0 || index >= State.inventory.Count) return Fail("请选择要弃置的物品。");
            State.inventory.RemoveAt(index); return Success("已弃置物品。");
        }
        static int SaturatedAdd(int a, int b) { return (int)Math.Min(int.MaxValue, (long)a + b); }

        sealed class Pulse
        {
            public int tick, sourceId, targetId, amount, layer;
            public bool echo;
        }
        sealed class PendingFire { public int nodeId, tick; }
        sealed class Resolution
        {
            public StimulusResult result = new StimulusResult { success = true };
            public List<EdgeState> edges;
            public int originId, stimulusNumber;
            public Dictionary<int, double> factors = new Dictionary<int, double>();
            public double variantFactor = 1;
            public bool hasConvergence;
        }
        void Event(Resolution run, int tick, EventKind kind, string text, int nodeId = -1, double value = 0, int synapseIndex = -1, int sourceId = -1)
        {
            run.result.events.Add(new ScoreEvent { tick = tick, kind = kind, nodeId = nodeId, sourceId = sourceId, synapseIndex = synapseIndex, value = value, text = text, charge = run.result.charge, multiplier = run.result.multiplier });
        }
        void Charge(Resolution run, int tick, double amount, string text, int nodeId = -1, int slot = -1)
        {
            if (amount == 0) return;
            run.result.charge += amount; Event(run, tick, EventKind.Charge, text, nodeId, amount, slot);
        }
        void Multiplier(Resolution run, int tick, double amount, string text, int nodeId = -1, int slot = -1)
        {
            if (amount == 0) return;
            run.result.multiplier += amount; Event(run, tick, EventKind.Multiplier, text, nodeId, amount, slot);
        }
        bool SlotActive(int index) { return !(Boss(2) && index == 0); }
        void Factor(Resolution run, int slot, double amount)
        {
            double old;
            run.factors.TryGetValue(slot, out old);
            run.factors[slot] = (old == 0 ? 1 : old) * amount;
        }
        void ScoreNode(Resolution run, NodeState node, int tick, bool rescore, int rescoreSlot = -1)
        {
            bool active = node.id == run.originId;
            if (rescore) run.result.rescores++;
            Event(run, tick, rescore ? EventKind.Rescore : EventKind.Fire, rescore ? rescoreSlot >= 0 ? "复读重计分" : "回声重计分" : active ? "主动点火" : "连锁放电", node.id, 0, rescoreSlot);
            int amount = node.BaseScore;
            if (node.kind == NodeKind.Core && !active && node.coreSources >= 3)
            {
                amount += node.coreSources * 10; run.hasConvergence = true;
            }
            if (!Boss(1) || tick != 1) Charge(run, tick, amount, Catalog.NodeName(node.kind) + " +" + amount, node.id);
            else Event(run, tick, EventKind.Charge, "节拍器 · 本拍基础灵感被抑制", node.id);
            if (node.variant == NodeVariant.Gold) Charge(run, tick, 20, "镀金 +20", node.id);
            if (node.variant == NodeVariant.Resonant) Multiplier(run, tick, 4, "共振 +4 倍率", node.id);
            if (node.variant == NodeVariant.Multiple)
            {
                run.variantFactor *= 1.5;
                Event(run, tick, EventKind.Multiplier, "多重 ×1.5 · 结算时乘入", node.id, 1.5);
            }
            foreach (var drug in State.drugs)
                if (drug.id == "P04" && drug.nodeId == node.id) Multiplier(run, tick, 10, "兴奋剂 +10 倍率", node.id);
            for (int i = 0; i < State.synapses.Count; i++)
            {
                if (!SlotActive(i)) continue;
                string id = State.synapses[i].id;
                switch (id)
                {
                    case "J01": if (node.kind == NodeKind.Sensitive) Multiplier(run, tick, 4, "磷火 +4 倍率", node.id, i); break;
                    case "J02": if (node.kind == NodeKind.Normal) Charge(run, tick, 15, "石砌 +15", node.id, i); break;
                    case "J05": if (active && !rescore) Charge(run, tick, run.edges.FindAll(e => e.sourceId == node.id && !e.blocked).Count * 8, "起爆 · 出边增幅", node.id, i); break;
                    case "J06": if (rescore) Charge(run, tick, 20, "回路 +20", node.id, i); break;
                    case "J10":
                        if (node.kind == NodeKind.Core && !active)
                        {
                            Factor(run, i, 1.5);
                            Event(run, tick, EventKind.Multiplier, "聚焦 ×1.5 · 结算时乘入", node.id, 1.5, i);
                        }
                        break;
                }
            }
        }
        void SendSignals(Resolution run, NodeState node, int tick, List<Pulse> pulses)
        {
            int output = node.id == run.originId ? 2 : node.kind == NodeKind.Capacitor ? 2 : 1;
            foreach (var edge in run.edges)
            {
                if (edge.blocked || edge.sourceId != node.id) continue;
                int amount = output + edge.strength - 1;
                pulses.Add(new Pulse { tick = tick + edge.delay, sourceId = node.id, targetId = edge.targetId, amount = amount, layer = node.layer });
                if (edge.echo) pulses.Add(new Pulse { tick = tick + edge.delay + 2, sourceId = node.id, targetId = edge.targetId, amount = amount, layer = node.layer, echo = true });
            }
        }

        StimulusResult Resolve(int nodeId)
        {
            var origin = FindNode(nodeId);
            var run = new Resolution { edges = GetEdges(), originId = nodeId, stimulusNumber = 3 - State.clicksRemaining };
            var pulses = new List<Pulse>();
            var pending = new List<PendingFire>();
            var deferredEchoes = new Dictionary<int, int>();
            origin.layer = 0; origin.coreSources = 0;
            FireNode(run, origin, 0, pulses, deferredEchoes);
            int tick = 0;
            while (pulses.Count > 0 || pending.Count > 0)
            {
                tick = int.MaxValue;
                foreach (var p in pulses) tick = Math.Min(tick, p.tick);
                foreach (var p in pending) tick = Math.Min(tick, p.tick);
                var arrivals = pulses.FindAll(p => p.tick == tick);
                pulses.RemoveAll(p => p.tick == tick);
                arrivals.Sort((a, b) => a.targetId != b.targetId ? a.targetId.CompareTo(b.targetId) : a.sourceId != b.sourceId ? a.sourceId.CompareTo(b.sourceId) : a.echo.CompareTo(b.echo));
                var groups = new Dictionary<int, List<Pulse>>();
                var targets = new List<int>();
                foreach (var pulse in arrivals)
                {
                    if (!groups.ContainsKey(pulse.targetId)) { groups[pulse.targetId] = new List<Pulse>(); targets.Add(pulse.targetId); }
                    groups[pulse.targetId].Add(pulse);
                }
                targets.Sort();
                // All arrivals see the same start-of-tick status. A simultaneous echo
                // contributes charge unless the target was already asleep or pending.
                foreach (int id in targets)
                {
                    var node = FindNode(id);
                    if (node == null) continue;
                    bool waiting = pending.Exists(p => p.nodeId == id);
                    if (node.fired || waiting)
                    {
                        foreach (var pulse in groups[id])
                            if (pulse.echo)
                            {
                                Event(run, tick, EventKind.Signal, "回声抵达", id, pulse.amount, -1, pulse.sourceId);
                                if (node.fired) ScoreNode(run, node, tick, true);
                                else
                                {
                                    int count; deferredEchoes.TryGetValue(id, out count); deferredEchoes[id] = count + 1;
                                }
                            }
                        continue;
                    }
                    foreach (var pulse in groups[id])
                    {
                        Event(run, tick, EventKind.Signal, pulse.echo ? "回声充能" : "电位传入", id, pulse.amount, -1, pulse.sourceId);
                        for (int u = 0; u < pulse.amount; u++) node.contributions.Add(new ChargeUnit { sourceId = pulse.sourceId, layer = pulse.layer, tick = tick });
                        node.charge += pulse.amount;
                    }
                    if (node.charge >= node.Threshold)
                    {
                        int maxLayer = 0;
                        var sources = new HashSet<int>();
                        foreach (var c in node.contributions) { maxLayer = Math.Max(maxLayer, c.layer); sources.Add(c.sourceId); }
                        node.layer = maxLayer + 1; node.coreSources = sources.Count;
                        pending.Add(new PendingFire { nodeId = id, tick = tick + (node.kind == NodeKind.Delay ? 1 : 0) });
                    }
                }
                var ready = pending.FindAll(p => p.tick == tick);
                pending.RemoveAll(p => p.tick == tick);
                ready.Sort((a, b) => a.nodeId.CompareTo(b.nodeId));
                foreach (var fire in ready) FireNode(run, FindNode(fire.nodeId), tick, pulses, deferredEchoes);
                if (tick >= 2) run.result.maxWave = Math.Max(run.result.maxWave, ready.Count);
                if (ready.Count >= 2)
                    for (int i = 0; i < State.synapses.Count; i++)
                        if (SlotActive(i) && State.synapses[i].id == "J03") Multiplier(run, tick, ready.Count, "节律 · 同拍 " + ready.Count + " 放电", -1, i);
            }
            FinishScoring(run, tick + 1);
            return run.result;
        }
        void FireNode(Resolution run, NodeState node, int tick, List<Pulse> pulses, Dictionary<int, int> deferredEchoes)
        {
            node.fired = true; node.charge = 0; node.contributions.Clear();
            run.result.firedIds.Add(node.id);
            run.result.longestLayer = Math.Max(run.result.longestLayer, node.layer);
            ScoreNode(run, node, tick, false);
            SendSignals(run, node, tick, pulses);
            int echoCount;
            if (deferredEchoes.TryGetValue(node.id, out echoCount))
            {
                for (int i = 0; i < echoCount; i++) ScoreNode(run, node, tick, true);
                deferredEchoes.Remove(node.id);
            }
            var origin = FindNode(run.originId);
            if (node.id != origin.id && Distance(node.q, node.r, origin.q, origin.r) == 1)
                for (int i = 0; i < State.synapses.Count; i++)
                    if (SlotActive(i) && State.synapses[i].id == "J12")
                    {
                        ScoreNode(run, node, tick, true, i);
                    }
        }
        void FinishScoring(Resolution run, int tick)
        {
            if (Boss(5) && !State.twinAwarded)
            {
                var a = FindNode(State.twinA); var b = FindNode(State.twinB);
                if (a != null && b != null && a.fired && b.fired)
                {
                    State.twinAwarded = true; Charge(run, tick, 100, "双生共鸣 +100");
                }
            }
            for (int i = 0; i < State.synapses.Count; i++)
            {
                if (!SlotActive(i)) continue;
                switch (State.synapses[i].id)
                {
                    case "J04": Charge(run, tick, State.nodes.FindAll(n => !n.fired && n.charge == 1).Count * 10, "余烬 · 保留电位", -1, i); break;
                    case "J07": Multiplier(run, tick, (State.round - 1) / 2, "老化 · 时间沉淀", -1, i); break;
                    case "J08": Multiplier(run, tick, State.memoriesUsed, "收藏 · 记忆共鸣", -1, i); break;
                    case "J09": if (run.result.longestLayer >= 4) Factor(run, i, 2); break;
                    case "J11": if (run.stimulusNumber == 2) Factor(run, i, 2); break;
                }
            }
            bool[] eligible = { true, run.result.longestLayer >= 2, run.result.maxWave >= 3, run.hasConvergence,
                run.result.longestLayer >= 4, run.result.rescores >= 2, run.result.longestLayer >= 4 && run.result.maxWave >= 3,
                run.result.longestLayer >= 6, run.hasConvergence && run.result.maxWave >= 3 };
            int winner = 0, winningLevel = 1;
            double bestScore = -1;
            for (int i = 0; i < eligible.Length; i++)
            {
                if (!eligible[i]) continue;
                int level = StructureLevel(Catalog.StructureIds[i]);
                double extraCharge = Catalog.StructureCharge[i] + (level - 1) * Catalog.StructureChargeGrowth[i];
                double extraMult = Catalog.StructureMult[i] + (level - 1) * Catalog.StructureMultGrowth[i];
                double score = (run.result.charge + extraCharge) * (run.result.multiplier + extraMult);
                if (score > bestScore) { bestScore = score; winner = i; winningLevel = level; }
            }
            run.result.structureName = Catalog.StructureNames[winner]; run.result.structureLevel = winningLevel;
            Charge(run, tick, Catalog.StructureCharge[winner] + (winningLevel - 1) * Catalog.StructureChargeGrowth[winner], run.result.structureName + " · 结构灵感");
            Multiplier(run, tick, Catalog.StructureMult[winner] + (winningLevel - 1) * Catalog.StructureMultGrowth[winner], run.result.structureName + " · 结构倍率");
            Event(run, tick, EventKind.Structure, run.result.structureName + "  Lv." + winningLevel);
            if (run.variantFactor > 1)
            {
                run.result.multiplier *= run.variantFactor;
                Event(run, tick, EventKind.Multiplier, "多重变体 ×" + run.variantFactor.ToString("0.##"), -1, run.variantFactor);
            }
            // Mirror reads the resolved multiplier effect immediately to its left.
            // All additive effects precede this deterministic left-to-right pass.
            for (int i = 0; i < State.synapses.Count; i++)
            {
                if (!SlotActive(i)) continue;
                if (State.synapses[i].id == "J13" && i > 0 && SlotActive(i - 1))
                {
                    double copy;
                    if (run.factors.TryGetValue(i - 1, out copy)) run.factors[i] = copy;
                }
                double factor;
                if (run.factors.TryGetValue(i, out factor) && factor > 1)
                {
                    run.result.multiplier *= factor;
                    Event(run, tick, EventKind.Multiplier, Catalog.Get(State.synapses[i].id).name + " ×" + factor.ToString("0.##"), -1, factor, i);
                }
            }
            double final = Math.Floor(run.result.charge * run.result.multiplier);
            run.result.score = double.IsNaN(final) ? 0 : final >= int.MaxValue ? int.MaxValue : (int)final;
            Event(run, tick + 1, EventKind.Complete, "+" + run.result.score.ToString("N0"), -1, run.result.score);
        }
        void UpdateTwinTargets()
        {
            if (Boss(5) && State.clicksRemaining == 2)
            {
                int maxDistance = -1;
                var nodes = new List<NodeState>(State.nodes); nodes.Sort((a, b) => a.id.CompareTo(b.id));
                for (int i = 0; i < nodes.Count; i++)
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        int d = Distance(nodes[i].q, nodes[i].r, nodes[j].q, nodes[j].r);
                        if (d > maxDistance) { maxDistance = d; State.twinA = nodes[i].id; State.twinB = nodes[j].id; }
                    }
            }
        }
        void PrepareBoss()
        {
            UpdateTwinTargets();
            if (!Boss(0) || State.clicksRemaining != 2 || State.bossRelaxed) return;
            int minimum = Math.Min(3, State.nodes.Count);
            bool viable = false;
            foreach (var n in State.nodes)
            {
                if (n.fired) continue;
                var test = new EmergenceEngine(State.Copy());
                if (test.Resolve(n.id).firedIds.Count >= minimum) { viable = true; break; }
            }
            if (!viable) State.bossRelaxed = true;
        }
        public StimulusResult Stimulate(int nodeId)
        {
            if (State.phase != RunPhase.Prepare) return InvalidStimulus("先完成本轮生长，再选择起点。");
            if (State.clicksRemaining <= 0) return InvalidStimulus("本轮刺激次数已用完。");
            var origin = FindNode(nodeId);
            if (origin == null) return InvalidStimulus("请选择一个神经元作为起点。");
            if (origin.fired) return InvalidStimulus("该神经元已经休眠，请选择尚未放电的节点。");
            PrepareBoss();
            var result = Resolve(nodeId);
            State.clicksRemaining--;
            State.roundScore = SaturatedAdd(State.roundScore, result.score);
            State.bestScore = Math.Max(State.bestScore, result.score);
            State.lastStructure = result.structureName;
            State.drugs.Clear();
            if (Boss(4) && State.clicksRemaining == 1)
            {
                foreach (var n in State.nodes) if (!n.fired) { n.charge = 0; n.contributions.Clear(); }
                result.events.Add(new ScoreEvent { tick = result.events[result.events.Count - 1].tick + 1, kind = EventKind.Charge, text = "遗忘 · 未放电节点电位清零", charge = result.charge, multiplier = result.multiplier });
            }
            if (State.roundScore < State.target && (State.clicksRemaining == 0 || !State.nodes.Exists(n => !n.fired)))
            {
                State.phase = RunPhase.Defeat;
                State.lastMessage = "还差 " + (State.target - State.roundScore).ToString("N0") + " 分。调整连接，下一次会更远。";
            }
            else State.lastMessage = State.roundScore >= State.target ? "已突破目标！可以领取奖励。" : "还差 " + (State.target - State.roundScore).ToString("N0") + " 分。未放电节点的电位已保留。";
            LastError = "";
            return result;
        }
        StimulusResult InvalidStimulus(string message)
        {
            Fail(message); return new StimulusResult { success = false, error = message };
        }
        public StimulusResult Preview(int nodeId)
        {
            // Never let hover advance RNG, consume items, change charge or check Bosses
            // on the live run. The hidden late chain is deliberately not disclosed.
            var clone = new EmergenceEngine(State.Copy());
            var full = clone.Stimulate(nodeId);
            if (!full.success) return full;
            var preview = new StimulusResult { success = true, score = 0, charge = 0, multiplier = 1 };
            foreach (var e in full.events)
                if (e.tick <= 2 && e.kind != EventKind.Structure && e.kind != EventKind.Complete)
                {
                    // No scoring snapshots are exposed by the two-beat preview.
                    preview.events.Add(new ScoreEvent { tick = e.tick, kind = e.kind, nodeId = e.nodeId, sourceId = e.sourceId, synapseIndex = e.synapseIndex, value = e.value, charge = 0, multiplier = 1, text = e.text });
                    if (e.kind == EventKind.Fire && !preview.firedIds.Contains(e.nodeId)) preview.firedIds.Add(e.nodeId);
                }
            preview.stillPropagating = full.events.Exists(e => e.tick > 2 && (e.kind == EventKind.Fire || e.kind == EventKind.Signal || e.kind == EventKind.Rescore));
            preview.structureName = preview.stillPropagating ? "连锁仍在延伸" : "前两拍预演";
            return preview;
        }
    }
}
