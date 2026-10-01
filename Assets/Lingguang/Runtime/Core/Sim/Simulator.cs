using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>一次确认的模拟结果。Events 是表现层唯一需要读取的内容。</summary>
    public sealed class SimResult
    {
        public readonly List<SimEvent> Events = new List<SimEvent>();
        /// <summary>L：本次连锁中有灵光闪光的最大拍数。</summary>
        public int ChainLength;
        /// <summary>W：单拍最多闪光数（不含第 0 拍，记忆成员各算一个）。</summary>
        public int MaxWidth;
        public int EndBeat;
        /// <summary>闪光的汇聚灵光在同一拍收到的最多不同来源数。</summary>
        public int MaxConvergeSources;
        /// <summary>单个锥体因导通而点亮的最多灵光数。</summary>
        public int MaxDiverge;
        public PatternKind Pattern = PatternKind.None;
        public int LightGained;
        public bool HitBeatLimit;
        /// <summary>本次连锁中单个单位最多闪了几次；≥ 2 即出现“回荡”（规格第 5 节）。</summary>
        public int MaxFiresPerUnit;
        public bool Reverberated => MaxFiresPerUnit >= 2;

        public List<SimEvent> OfType(SimEventType type)
        {
            var list = new List<SimEvent>();
            foreach (var e in Events) if (e.Type == type) list.Add(e);
            return list;
        }
    }

    /// <summary>
    /// 传播模拟器（规格第 5、6 节）。输入“棋盘状态 + 本次起点”，输出有序事件列表并更新 Model。
    /// 完全确定：所有遍历都按 id / 格子排序，随机数不在模拟中使用。
    /// 每次确认调用一次（不是每帧），42 格规模下无需额外优化。
    /// </summary>
    public sealed class Simulator
    {
        sealed class Delivery
        {
            public int TargetUnit = -1;
            public int TargetGlowId = -1;
            public Cell TargetCell;
            public int SourceGlowId = -1;
            public Cell FromCell;
            public int Amount;
            public int SendBeat;
            public DeliveryKind Kind;
            public EdgeKey Key;
            public int Perturb;
        }

        sealed class RippleGroup
        {
            public int Id;
            public int Radius;
            public int Count;
            public List<Cell> Centers;
            public List<Cell> Cells;
        }

        readonly GameState s;
        readonly GameConfig cfg;
        readonly SimResult result = new SimResult();
        readonly SortedDictionary<int, List<Delivery>> pending = new SortedDictionary<int, List<Delivery>>();
        readonly SortedDictionary<int, List<RippleGroup>> dueRipples = new SortedDictionary<int, List<RippleGroup>>();
        readonly Dictionary<int, List<Delivery>> intake = new Dictionary<int, List<Delivery>>();
        readonly HashSet<(long, int)> weakPulsesSent = new HashSet<(long, int)>();
        readonly List<Cell> rippleCentersThisBeat = new List<Cell>();
        readonly Dictionary<int, HashSet<int>> coneTargets = new Dictionary<int, HashSet<int>>();
        readonly Dictionary<int, int> firesPerBeat = new Dictionary<int, int>();
        /// <summary>本拍已闪的单位（同一拍内最多闪一次）。</summary>
        readonly HashSet<int> firedThisBeat = new HashSet<int>();
        readonly Dictionary<int, int> firesThisChain = new Dictionary<int, int>();
        /// <summary>本次确认中被点亮过（收到过电或闪过）的单位：LeakStoredCharge=false 时只有它们流失。</summary>
        readonly HashSet<int> litThisConfirm = new HashSet<int>();
        int nextRippleGroupId;

        Simulator(GameState state)
        {
            s = state;
            cfg = state.Config;
        }

        /// <summary>
        /// 执行一次确认。starterGlowIds 是玩家选中的起点灵光（记忆成员代表整个记忆）。
        /// 不处理起点次数、撤销（由 GameSession 负责）；只做规则校验。
        /// </summary>
        public static SimResult Confirm(GameState state, IList<int> starterGlowIds)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (starterGlowIds == null || starterGlowIds.Count == 0) throw new ArgumentException("至少需要一个起点");
            var sim = new Simulator(state);
            sim.Run(starterGlowIds);
            return sim.result;
        }

        /// <summary>校验起点：存在、格子有效、本轮未闪、不重复（同一记忆只算一个）。</summary>
        public static bool ValidateStarters(GameState s, IList<int> starterGlowIds, out List<int> units, out string reason)
        {
            units = new List<int>();
            reason = null;
            if (starterGlowIds == null || starterGlowIds.Count == 0) { reason = "没有选择起点"; return false; }
            foreach (int id in starterGlowIds)
            {
                if (!s.Glows.TryGetValue(id, out var g)) { reason = "起点灵光不存在 " + id; return false; }
                int unit = Units.Of(s, g);
                if (!Units.IsActive(s, unit)) { reason = "起点所在格子无效 " + g.Cell; return false; }
                if (!s.Config.AllowFiredStarters && Units.IsFired(s, unit)) { reason = "起点本轮已闪过 " + g.Cell; return false; }
                if (!units.Contains(unit)) units.Add(unit);
            }
            units.Sort((a, b) => Units.Representative(s, a).CompareTo(Units.Representative(s, b)));
            return true;
        }

        // ================= 主循环 =================

        void Run(IList<int> starterGlowIds)
        {
            if (!ValidateStarters(s, starterGlowIds, out var starters, out string reason))
                throw new InvalidOperationException(reason);

            // 灯塔：棋盘外的初始光源。本轮第一次确认时，第 0 拍最先从棋盘外照进来，
            // 电量与起点在同一拍累加，可以把灵光推到阈值（被照亮而闪的灵光不加导通、不产生涟漪）。
            List<int> lit = null;
            if (s.Round.FirstConfirmPending)
            {
                s.Round.FirstConfirmPending = false;
                if (!EffectQuery.LighthouseOff(s)) lit = Lighthouse(starters);
            }

            // 第 0 拍：起点全部直接闪（输出 +2）。
            foreach (int unit in starters) FireUnit(unit, 0, true);
            if (lit != null) ScheduleLighthouse(lit);

            int beat = 0;
            while (true)
            {
                if (beat > cfg.MaxBeats)
                {
                    result.HitBeatLimit = true;
                    break;
                }

                ApplyDueRipples(beat);
                ProcessBeat(beat);
                SettleRipples(beat);
                result.EndBeat = beat;

                bool more = HasWorkAfter(beat);
                if (more || cfg.LeakAfterLastBeat) Leak(beat);
                EmitBeatEnd(beat);
                if (!more) break;
                beat++;
                intake.Clear();
                firedThisBeat.Clear();
            }

            foreach (var kv in firesPerBeat)
                if (kv.Key > 0) result.MaxWidth = Math.Max(result.MaxWidth, kv.Value);

            PatternRules.Apply(s, result);

            result.Events.Add(new SimEvent { Type = SimEventType.ChainEnd, Beat = result.EndBeat, Error = result.HitBeatLimit });
        }

        bool HasWorkAfter(int beat)
        {
            foreach (var kv in pending) if (kv.Key > beat && kv.Value.Count > 0) return true;
            foreach (var kv in dueRipples) if (kv.Key > beat && kv.Value.Count > 0) return true;
            return false;
        }

        List<Delivery> PendingAt(int beat)
        {
            if (!pending.TryGetValue(beat, out var list))
            {
                list = new List<Delivery>();
                pending.Add(beat, list);
            }
            return list;
        }

        /// <summary>第 2–4 步：先把本拍到达的电全部累加，再统一判断谁闪；当拍（延迟 0）新产生的投递回到循环。</summary>
        void ProcessBeat(int beat)
        {
            var list = PendingAt(beat);
            while (list.Count > 0)
            {
                var batch = new List<Delivery>(list);
                list.Clear();

                var touched = new List<int>();
                foreach (var d in batch) Accept(d, beat, touched);

                var ready = new List<int>();
                foreach (int unit in touched)
                {
                    if (firedThisBeat.Contains(unit)) continue;
                    if (Units.Charge(s, unit) >= EffectQuery.EffectiveThreshold(s, unit)) ready.Add(unit);
                }
                ready.Sort((a, b) => Units.Representative(s, a).CompareTo(Units.Representative(s, b)));
                var converge = new int[ready.Count];
                for (int i = 0; i < ready.Count; i++) converge[i] = RecordArrivals(ready[i], beat);
                for (int i = 0; i < ready.Count; i++) FireUnit(ready[i], beat, false, converge[i]);
            }
        }

        void Accept(Delivery d, int beat, List<int> touched)
        {
            // 微弱连接只送给本轮还没闪过的一方：在到达时再判断一次，
            // 否则同一拍里一起闪的两个灵光会因先后顺序而互相送出脉冲。
            if (d.Kind == DeliveryKind.WeakLink && !cfg.WeakLinkToFiredPartner && Units.IsFired(s, d.TargetUnit)) return;

            var ev = new SimEvent
            {
                Type = SimEventType.Deliver, Beat = beat, Cell = d.TargetCell, From = d.FromCell,
                GlowId = d.TargetGlowId, SourceGlowId = d.SourceGlowId, Amount = d.Amount, SendBeat = d.SendBeat, Kind = d.Kind,
                Perturb = d.Perturb,
            };
            result.Events.Add(ev);

            if (d.TargetUnit < 0)
            {
                result.Events.Add(new SimEvent { Type = SimEventType.Lost, Beat = beat, Cell = d.TargetCell, SourceGlowId = d.SourceGlowId });
                CollectPickup(d.TargetCell, beat);
                return;
            }

            // v0.4 起不再有“已闪即浪费”：电照样累积，只是疲劳让再次闪需要更多电；同一拍内不会再闪。
            Units.AddCharge(s, d.TargetUnit, d.Amount);
            litThisConfirm.Add(d.TargetUnit);
            if (!intake.TryGetValue(d.TargetUnit, out var received))
            {
                received = new List<Delivery>();
                intake.Add(d.TargetUnit, received);
            }
            received.Add(d);
            if (!touched.Contains(d.TargetUnit)) touched.Add(d.TargetUnit);
        }

        void CollectPickup(Cell cell, int beat)
        {
            for (int i = 0; i < s.Pickups.Count; i++)
            {
                if (s.Pickups[i].Cell != cell) continue;
                var p = s.Pickups[i];
                s.Pickups.RemoveAt(i);
                s.Inventory.Add(p.ItemId);
                result.Events.Add(new SimEvent { Type = SimEventType.PickupCollected, Beat = beat, Cell = cell, ItemId = p.ItemId });
                return;
            }
        }

        // ================= 闪光 =================

        /// <summary>
        /// 闪光前先记导通（规格第 5 节第 3 步）：本拍向该单位送过电的每条光丝各记一次导通。
        /// 同一批要闪的单位全部先记完导通、再统一闪，这样本批里刚升级的光丝在本批送电时就生效，
        /// 结果与单位的 id 顺序无关（与参考模型 sim.py 一致）。返回汇聚统计用的不同来源数。
        /// </summary>
        int RecordArrivals(int unit, int beat)
        {
            if (!intake.TryGetValue(unit, out var received)) return 0;
            var members = Units.Members(s, unit);
            var sources = new List<int>();
            bool anyEdge = false, anyRipple = false;
            foreach (var d in received)
            {
                if (d.Kind == DeliveryKind.Edge)
                {
                    anyEdge = true;
                    RecordConduct(d, unit, beat);
                    if (!sources.Contains(d.SourceGlowId)) sources.Add(d.SourceGlowId);
                }
                else if (d.Kind == DeliveryKind.Ripple || d.Kind == DeliveryKind.WeakLink)
                {
                    anyRipple = true;
                }
            }
            if (!anyEdge && anyRipple)
            {
                result.Events.Add(new SimEvent
                {
                    Type = SimEventType.RippleConduct, Beat = beat, Cell = members[0].Cell, GlowId = members[0].Id,
                });
            }
            intake.Remove(unit);
            return sources.Count;
        }

        void FireUnit(int unit, int beat, bool isStarter, int convergeSources = 0)
        {
            var members = Units.Members(s, unit);
            bool isMemory = Units.IsMemory(unit);
            Units.MarkFired(s, unit);
            firedThisBeat.Add(unit);
            litThisConfirm.Add(unit);
            int fireIndex = Units.FireCount(s, unit);
            firesThisChain.TryGetValue(unit, out int chainFires);
            firesThisChain[unit] = ++chainFires;
            result.MaxFiresPerUnit = Math.Max(result.MaxFiresPerUnit, chainFires);
            s.Round.FireEvents += members.Count;

            if (isMemory)
            {
                var memory = Units.MemoryOf(s, unit);
                memory.TriggeredThisRound = true;
                memory.TimesTriggered++;
            }
            // 沉淀区域：有灵光闪过即本轮“被触发”；记忆每被触发一次扣区域可塑度 1。
            SettledRegion memoryRegion = null;
            foreach (var g in members)
            {
                var region = s.SettledAt(g.Cell);
                if (region == null) continue;
                region.TriggeredThisRound = true;
                if (isMemory && memoryRegion == null) memoryRegion = region;
            }
            if (memoryRegion != null) memoryRegion.Plasticity = Math.Max(0, memoryRegion.Plasticity - 1);

            // 光量
            foreach (var g in members)
            {
                int light = LightOf(s, g);
                result.Events.Add(new SimEvent { Type = SimEventType.Fire, Beat = beat, Cell = g.Cell, GlowId = g.Id, Amount = light, Perturb = fireIndex });
                AddLight(light, beat, g.Cell, "fire");
                s.Round.FiredGlows.Add(g.Id);
            }
            if (!isMemory && members[0].Shape == ShapeKind.Converge)
            {
                result.MaxConvergeSources = Math.Max(result.MaxConvergeSources, convergeSources);
                if (convergeSources >= cfg.ConvergeSources)
                    AddLight(convergeSources * cfg.ConvergeBonusPerSource, beat, members[0].Cell, "converge");
            }

            firesPerBeat.TryGetValue(beat, out int n);
            firesPerBeat[beat] = n + members.Count;
            result.ChainLength = Math.Max(result.ChainLength, beat);

            // 投递
            foreach (var g in members) EmitDeliveries(g, beat, isStarter);
            if (cfg.WeakLinksEnabled && cfg.WeakLinkCharge != 0)
                foreach (var g in members) EmitWeakLinkPulses(g, unit, beat);
        }

        /// <summary>本能灵光的光量随阶段变化，存在实例上；其他形状直接读配置（热重载后立即生效）。</summary>
        public static int LightOf(GameState s, Glow g) => g.IsInstinct ? g.Light : s.Config.GetShape(g.Shape).Light;

        // ================= 漏电 =================

        /// <summary>每拍结束时流失电量（只减到 0；负电量不受漏电影响，留给抑制效果）。</summary>
        void Leak(int beat)
        {
            if (cfg.LeakPerBeat <= 0) return;
            foreach (var g in s.Glows.Values)
            {
                if (g.MemoryId >= 0 || g.Charge <= 0) continue;
                if (!cfg.LeakStoredCharge && !litThisConfirm.Contains(g.Id)) continue;
                g.Charge = Math.Max(0, g.Charge - cfg.LeakPerBeat);
            }
            foreach (var m in s.Memories.Values)
            {
                if (m.Charge <= 0) continue;
                if (!cfg.LeakStoredCharge && !litThisConfirm.Contains(Units.MemoryBase + m.Id)) continue;
                m.Charge = Math.Max(0, m.Charge - cfg.LeakPerBeat);
            }
        }

        void EmitBeatEnd(int beat)
        {
            var cells = new List<Cell>();
            var values = new List<int>();
            foreach (var g in s.Glows.Values)
            {
                if (g.MemoryId >= 0 || g.Charge == 0) continue;
                cells.Add(g.Cell); values.Add(g.Charge);
            }
            foreach (var m in s.Memories.Values)
            {
                if (m.Charge == 0) continue;
                cells.Add(s.Glows[m.Members[0]].Cell); values.Add(m.Charge);
            }
            result.Events.Add(new SimEvent { Type = SimEventType.BeatEnd, Beat = beat, Cells = cells, Values = values });
        }

        void AddLight(int light, int beat, Cell cell, string source)
        {
            s.Round.FireLight += light;
            result.LightGained += light;
            result.Events.Add(new SimEvent { Type = SimEventType.ScoreDelta, Beat = beat, Cell = cell, LightDelta = light, Source = source });
        }

        void RecordConduct(Delivery d, int targetUnit, int beat)
        {
            var stats = s.GetOrCreateEdge(d.Key);
            int oldLevel = cfg.EdgeLevelIndex(stats.Count);
            bool wasBridgePenalized = s.Board.IsBridge(d.FromCell, d.TargetCell) && stats.Count < cfg.BridgePenaltyUntilConducts;
            stats.Count++;
            stats.ConductedThisRound = true;
            int newLevel = cfg.EdgeLevelIndex(stats.Count);

            result.Events.Add(new SimEvent
            {
                Type = SimEventType.Conduct, Beat = beat, Cell = d.TargetCell, From = d.FromCell,
                GlowId = d.TargetGlowId, SourceGlowId = d.SourceGlowId, Amount = stats.Count,
            });
            bool bridgeCleared = wasBridgePenalized && stats.Count >= cfg.BridgePenaltyUntilConducts;
            if (newLevel != oldLevel || bridgeCleared)
            {
                result.Events.Add(new SimEvent
                {
                    Type = SimEventType.EdgeLevelUp, Beat = beat, Cell = d.TargetCell, From = d.FromCell,
                    GlowId = d.TargetGlowId, SourceGlowId = d.SourceGlowId, Level = newLevel,
                });
            }

            // 每个导通在被点亮的灵光处产生一个涟漪（规格第 6 节）。
            if (cfg.RippleCharge != 0) rippleCentersThisBeat.Add(d.TargetCell);

            // 发散：锥体因导通而点亮的不同单位数。
            var src = s.Glows[d.SourceGlowId];
            if (src.Shape == ShapeKind.Cone)
            {
                if (!coneTargets.TryGetValue(src.Id, out var set))
                {
                    set = new HashSet<int>();
                    coneTargets.Add(src.Id, set);
                }
                set.Add(targetUnit);
                result.MaxDiverge = Math.Max(result.MaxDiverge, set.Count);
            }
        }

        // ================= 投递生成 =================

        void EmitDeliveries(Glow g, int beat, bool isStarter)
        {
            if (!s.IsCellActive(g.Cell)) return;
            foreach (int dir in EdgeGeometry.OutputDirections(cfg, g))
            {
                var targetCell = Hex.Neighbor(g.Cell, dir);
                if (!s.IsCellActive(targetCell)) continue;
                if (s.Board.IsGroove(g.Cell, targetCell, s.Stage)) continue;

                var d = new Delivery { FromCell = g.Cell, TargetCell = targetCell, SourceGlowId = g.Id, SendBeat = beat };
                int conducts = 0;
                if (s.CellGlow.TryGetValue(targetCell, out int tid))
                {
                    var target = s.Glows[tid];
                    if (g.MemoryId >= 0 && g.MemoryId == target.MemoryId) continue; // 记忆内部不投递
                    d.Key = new EdgeKey(g.Id, tid);
                    if (s.Edges.TryGetValue(d.Key, out var stats))
                    {
                        if (stats.Pruned) continue;
                        conducts = stats.Count;
                    }
                    d.TargetGlowId = tid;
                    d.TargetUnit = Units.Of(s, target);
                    d.Kind = DeliveryKind.Edge;
                }
                else
                {
                    d.Kind = DeliveryKind.Lost;
                }

                int levelIndex = cfg.EdgeLevelIndex(conducts);
                var level = cfg.EdgeLevels[levelIndex];
                int amount = cfg.GetShape(g.Shape).Output + level.ChargeBonus + EffectQuery.OutputDelta(s, g);
                if (isStarter) amount += cfg.StarterOutputBonus;
                int delay = level.Delay + EffectQuery.DelayDelta(s, g);
                if (s.Board.IsBridge(g.Cell, targetCell) && conducts < cfg.BridgePenaltyUntilConducts)
                {
                    amount += cfg.BridgeChargePenalty;
                    delay += cfg.BridgeExtraDelay;
                }
                if (s.Stage == Stage.OldAge && !cfg.IsTopEdgeLevel(levelIndex) && g.MemoryId < 0)
                    delay += cfg.OldAgeExtraDelay;

                // 扰动：本轮内这条光丝每传一次电，之后经它送电 +PerturbStep（上限 PerturbCap），每轮开始回落。
                if (d.Kind == DeliveryKind.Edge && cfg.PerturbCap > 0)
                {
                    s.Round.Perturb.TryGetValue(d.Key, out int p);
                    d.Perturb = p;
                    amount += p;
                    s.Round.Perturb[d.Key] = Math.Min(cfg.PerturbCap, p + cfg.PerturbStep);
                }

                d.Amount = amount;
                PendingAt(beat + Math.Max(0, delay)).Add(d);
            }
        }

        /// <summary>有微弱连接的一对灵光，任一方闪光后另一方在下一拍 +1；每对每拍最多一次。</summary>
        void EmitWeakLinkPulses(Glow g, int firingUnit, int beat)
        {
            foreach (int partnerId in WeakLinks.PartnersOf(s, g.Id))
            {
                if (!s.Glows.TryGetValue(partnerId, out var partner)) continue;
                int partnerUnit = Units.Of(s, partner);
                if (partnerUnit == firingUnit || !s.IsCellActive(partner.Cell)) continue;
                if (!cfg.WeakLinkToFiredPartner && Units.IsFired(s, partnerUnit)) continue;
                if (!weakPulsesSent.Add((WeakLinks.Key(g.Id, partnerId), beat + 1))) continue;
                PendingAt(beat + 1).Add(new Delivery
                {
                    TargetUnit = partnerUnit, TargetGlowId = partnerId, TargetCell = partner.Cell,
                    SourceGlowId = g.Id, FromCell = g.Cell, Amount = cfg.WeakLinkCharge, SendBeat = beat, Kind = DeliveryKind.WeakLink,
                });
            }
        }

        /// <summary>灯塔覆盖的格子（不含失效格），按行优先排序。聚焦模式下只有一格。</summary>
        public static List<Cell> LighthouseCells(GameState s)
        {
            var list = new List<Cell>();
            if (s.StageDef.LighthouseFocus != LighthouseFocus.None)
            {
                if (s.Glows.TryGetValue(s.LighthouseFocusGlowId, out var focus) && s.IsCellActive(focus.Cell)) list.Add(focus.Cell);
                return list;
            }
            int reach = s.StageDef.LighthouseRadius - (s.Config.LighthouseFromOutside ? 1 : 0);
            if (reach < 0) return list;
            foreach (var c in s.Board.Cells)
            {
                if (!s.IsCellActive(c)) continue;
                foreach (var anchor in s.Board.Anchors)
                    if (Hex.Distance(anchor, c) <= reach) { list.Add(c); break; }
            }
            return list;
        }

        int LighthouseHits(Cell c, int reach)
        {
            if (s.StageDef.LighthouseFocus != LighthouseFocus.None) return 1;
            int hits = 0;
            foreach (var anchor in s.Board.Anchors) if (Hex.Distance(anchor, c) <= reach) hits++;
            return hits;
        }

        /// <summary>发出 LighthouseShine，返回要供电的单位（排除本次起点：它们本来就闪）。</summary>
        List<int> Lighthouse(List<int> starters)
        {
            var units = new List<int>();
            if (cfg.LighthouseCharge == 0) return units;
            var cells = LighthouseCells(s);
            if (cells.Count == 0) return units;
            result.Events.Add(new SimEvent
            {
                Type = SimEventType.LighthouseShine, Beat = 0, Cell = s.Board.Anchors.Count > 0 ? s.Board.Anchors[0] : default,
                Radius = s.StageDef.LighthouseRadius, Centers = new List<Cell>(s.Board.Anchors), Cells = cells, Amount = cfg.LighthouseCharge,
                GlowId = s.StageDef.LighthouseFocus != LighthouseFocus.None ? s.LighthouseFocusGlowId : -1,
            });
            foreach (var c in cells)
            {
                if (!s.CellGlow.TryGetValue(c, out int gid)) continue;
                int unit = Units.Of(s, s.Glows[gid]);
                if (starters.Contains(unit) || units.Contains(unit) || Units.IsFired(s, unit)) continue;
                units.Add(unit);
            }
            units.Sort((a, b) => Units.Representative(s, a).CompareTo(Units.Representative(s, b)));
            return units;
        }

        void ScheduleLighthouse(List<int> units)
        {
            int reach = s.StageDef.LighthouseRadius - (cfg.LighthouseFromOutside ? 1 : 0);
            var covered = new HashSet<Cell>(LighthouseCells(s));
            var list = PendingAt(0);
            foreach (int unit in units)
            {
                if (Units.IsFired(s, unit)) continue;
                Glow lit = null;
                int hits = 0;
                foreach (var g in Units.Members(s, unit))
                {
                    int h = covered.Contains(g.Cell) ? LighthouseHits(g.Cell, reach) : 0;
                    if (h > 0 && lit == null) lit = g;
                    hits = Math.Max(hits, h);
                }
                if (lit == null) continue;
                Cell entry = s.Board.Anchors[0];
                int best = int.MaxValue;
                foreach (var anchor in s.Board.Anchors)
                {
                    int d = Hex.Distance(anchor, lit.Cell);
                    if (d < best) { best = d; entry = anchor; }
                }
                list.Add(new Delivery
                {
                    TargetUnit = unit, TargetGlowId = lit.Id, TargetCell = lit.Cell, FromCell = entry,
                    Amount = cfg.LighthouseCharge * (cfg.LighthouseStacks ? hits : 1), SendBeat = 0, Kind = DeliveryKind.Lighthouse,
                });
            }
        }

        // ================= 涟漪 =================

        /// <summary>第 5 步：本拍产生的涟漪按合并距离连成群，下一拍生效。</summary>
        void SettleRipples(int beat)
        {
            if (rippleCentersThisBeat.Count == 0) return;
            var centers = new List<Cell>(rippleCentersThisBeat);
            rippleCentersThisBeat.Clear();

            int n = centers.Count;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (Hex.Distance(centers[i], centers[j]) <= cfg.RippleMergeDistance) Union(parent, i, j);

            var groups = new SortedDictionary<int, List<Cell>>();
            for (int i = 0; i < n; i++)
            {
                int root = Find(parent, i);
                if (!groups.TryGetValue(root, out var members)) { members = new List<Cell>(); groups.Add(root, members); }
                members.Add(centers[i]);
            }

            if (!dueRipples.TryGetValue(beat + 1, out var due)) { due = new List<RippleGroup>(); dueRipples.Add(beat + 1, due); }
            foreach (var kv in groups)
            {
                var groupCenters = kv.Value;
                int count = groupCenters.Count;
                int radius = Math.Min(cfg.RippleMaxRadius, cfg.RippleBaseRadius + (count - 1) / 2) + EffectQuery.RippleRadiusDelta(s);
                radius = Math.Max(0, radius);
                var group = new RippleGroup
                {
                    Id = nextRippleGroupId++, Radius = radius, Count = count,
                    Centers = groupCenters, Cells = Coverage(groupCenters, radius),
                };
                due.Add(group);

                foreach (var c in groupCenters)
                    result.Events.Add(new SimEvent { Type = SimEventType.RippleSpawn, Beat = beat, Cell = c, GroupId = group.Id, Radius = radius, Count = count });
                if (count >= 2)
                {
                    result.Events.Add(new SimEvent
                    {
                        Type = SimEventType.RippleMerge, Beat = beat, Cell = groupCenters[0], GroupId = group.Id,
                        Radius = radius, Count = count, Centers = new List<Cell>(groupCenters), Cells = new List<Cell>(group.Cells),
                    });
                }
            }
        }

        /// <summary>覆盖范围：每个中心按群半径画圆的并集。半径小于跨沟阈值时按不穿沟的路径距离。</summary>
        List<Cell> Coverage(List<Cell> centers, int radius)
        {
            var set = new SortedSet<Cell>();
            bool crossGrooves = radius >= cfg.RippleCrossGrooveFromRadius;
            foreach (var center in centers)
            {
                if (crossGrooves)
                {
                    foreach (var c in s.Board.Cells)
                        if (s.IsCellActive(c) && Hex.Distance(center, c) <= radius) set.Add(c);
                    continue;
                }

                var dist = new Dictionary<Cell, int> { { center, 0 } };
                var queue = new Queue<Cell>();
                queue.Enqueue(center);
                if (s.IsCellActive(center)) set.Add(center);
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    int dc = dist[c];
                    if (dc >= radius) continue;
                    for (int dir = 0; dir < Hex.DirectionCount; dir++)
                    {
                        var nb = Hex.Neighbor(c, dir);
                        if (dist.ContainsKey(nb) || !s.IsCellActive(nb) || s.Board.IsGroove(c, nb, s.Stage)) continue;
                        dist.Add(nb, dc + 1);
                        set.Add(nb);
                        queue.Enqueue(nb);
                    }
                }
            }
            return new List<Cell>(set);
        }

        /// <summary>涟漪生效：覆盖范围内每个本轮未闪的灵光 +1；每个群对每个单位只生效一次；同时累积微弱连接。</summary>
        void ApplyDueRipples(int beat)
        {
            if (!dueRipples.TryGetValue(beat, out var groups)) return;
            dueRipples.Remove(beat);
            var list = PendingAt(beat);
            foreach (var group in groups)
            {
                result.Events.Add(new SimEvent
                {
                    Type = SimEventType.RippleApply, Beat = beat, Cell = group.Centers[0], GroupId = group.Id,
                    Radius = group.Radius, Count = group.Count, Centers = new List<Cell>(group.Centers), Cells = new List<Cell>(group.Cells),
                });

                var unitsDone = new HashSet<int>();
                var coveredGlows = new List<Glow>();
                foreach (var c in group.Cells)
                {
                    if (!s.CellGlow.TryGetValue(c, out int gid)) continue;
                    var g = s.Glows[gid];
                    int unit = Units.Of(s, g);
                    if (!cfg.RippleReachesFired && Units.IsFired(s, unit)) continue;
                    coveredGlows.Add(g);
                    if (!unitsDone.Add(unit)) continue;
                    list.Add(new Delivery
                    {
                        TargetUnit = unit, TargetGlowId = g.Id, TargetCell = c, FromCell = group.Centers[0],
                        Amount = cfg.RippleCharge, SendBeat = beat - 1, Kind = DeliveryKind.Ripple,
                    });
                }

                if (!cfg.WeakLinksEnabled) continue;
                for (int i = 0; i < coveredGlows.Count; i++)
                    for (int j = i + 1; j < coveredGlows.Count; j++)
                    {
                        var a = coveredGlows[i];
                        var b = coveredGlows[j];
                        if (a.MemoryId >= 0 && a.MemoryId == b.MemoryId) continue;
                        WeakLinks.Increment(s, a.Id, b.Id);
                    }
            }
        }

        static int Find(int[] parent, int i)
        {
            while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
            return i;
        }

        static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a), rb = Find(parent, b);
            if (ra == rb) return;
            if (ra < rb) parent[rb] = ra; else parent[ra] = rb;
        }
    }
}
