using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>一轮结算的结果（表现层用来播放结算动画、切阶段）。</summary>
    public sealed class RoundSettlement
    {
        public Stage Stage;
        public int RoundInStage;
        public int Score;
        public int Target;
        public bool Passed;
        public bool PatternVoided;
        public string FailReason;
        public PatternKind TopPattern = PatternKind.None;
        public int CoinsGained;
        public List<int> NewMemoryIds = new List<int>();
        public List<string> NewSettledRegions = new List<string>();
        /// <summary>本次结算因无依漂走的灵光（id 与原格子），表现层播放漂走动画。</summary>
        public List<int> DriftedGlowIds = new List<int>();
        public List<Cell> DriftedCells = new List<Cell>();
        public bool StageAdvanced;
        public bool RunWon;
        public bool RunOver;
        public int GlowsFired;
        public int AdolescentPruned;
    }

    /// <summary>
    /// 轮与阶段的生命周期（规格第 7、9 节）：轮开始、结算、废退、青春期修剪、记忆折叠与可塑度、本能、老年衰退、拾物点。
    /// </summary>
    public static class Lifecycle
    {
        /// <summary>每轮开始：电量清零、已闪重置、本轮累积清空。</summary>
        public static void ResetRoundTransient(GameState s)
        {
            foreach (var g in s.Glows.Values) { g.Charge = 0; g.Fired = false; g.FireCount = 0; }
            foreach (var ch in s.Memories.Values) { ch.Charge = 0; ch.Fired = false; ch.FireCount = 0; ch.TriggeredThisRound = false; }
            foreach (var e in s.Edges.Values) e.ConductedThisRound = false;
            foreach (var r in s.SettledRegions.Values) r.TriggeredThisRound = false;
            s.Round = new RoundState();
        }

        /// <summary>进入某阶段的某一轮。gateOverride 为 null 时按种子从关口池抽取，\"\" 表示无关口。</summary>
        public static void EnterStage(GameState s, Stage stage, int roundInStage = 0, string gateOverride = null)
        {
            if (s.Stage != stage && s.Stage == Stage.Childhood) s.Pickups.Clear();
            s.Stage = stage;
            s.RoundInStage = roundInStage;
            s.StageGateId = gateOverride != null ? (gateOverride.Length == 0 ? null : gateOverride) : DrawGate(s, stage);
            BeginRound(s);
        }

        static string DrawGate(GameState s, Stage stage)
        {
            var pool = new List<string>();
            var def = s.Config.GetStage(stage);
            if (def.GatePool != null)
            {
                foreach (string id in def.GatePool)
                {
                    var gate = s.Config.FindGate(id);
                    if (gate == null) continue;
                    if (gate.RequiresMetaUnlock && !s.SeparationUnlocked) continue;
                    pool.Add(id);
                }
            }
            return pool.Count == 0 ? null : pool[s.Rng.Next(pool.Count)];
        }

        /// <summary>轮开始（规格第 9 节第 1 步）。</summary>
        public static void BeginRound(GameState s)
        {
            var cfg = s.Config;

            // 本能灵光生命周期：童年结算后每轮 −2（最低 0）；老年每轮 +2（最高 8）。
            foreach (var g in s.Glows.Values)
            {
                if (!g.IsInstinct) continue;
                if (s.Stage == Stage.OldAge) g.Light = Math.Min(cfg.InstinctLightMax, g.Light + cfg.InstinctLightGrowthOldAge);
                else if (s.Stage > Stage.Childhood) g.Light = Math.Max(0, g.Light - cfg.InstinctLightDecayPerRound);
            }

            if (s.Stage == Stage.OldAge) DisableCellsForOldAge(s);
            RefreshLighthouseFocus(s);
            UpdatePickups(s);

            ResetRoundTransient(s);
            var r = s.Round;
            r.StartsLeft = EffectQuery.StartCount(s);
            r.MovesLeft = s.IsBossRound ? cfg.MovesPerBossRound : cfg.MovesPerRound;
            r.PlacementAvailable = true;
            r.FirstConfirmPending = true;
            var pool = s.StageDef.CandidatePool;
            if (pool != null && pool.Length > 0)
                for (int i = 0; i < cfg.CandidatesPerRound; i++) r.Candidates.Add(pool[s.Rng.Next(pool.Length)]);
        }

        /// <summary>
        /// 老年灯塔只照一格：记忆最深刻的地方。进入老年时选定，之后一直照着它；
        /// 若那里的格子因老年衰退失效，下一轮改照次深的记忆。
        /// </summary>
        public static void RefreshLighthouseFocus(GameState s)
        {
            if (s.StageDef.LighthouseFocus == LighthouseFocus.None) { s.LighthouseFocusGlowId = -1; return; }
            if (s.Glows.TryGetValue(s.LighthouseFocusGlowId, out var current) && s.IsCellActive(current.Cell)) return;
            s.LighthouseFocusGlowId = PickDeepestMemoryGlow(s);
        }

        /// <summary>
        /// 记忆深度：整局被触发次数最多的记忆 → 折叠时导通和更大 → 更早形成；照它 id 最小的有效成员。
        /// 没有记忆时，退而照“一生中被导通最多的灵光”（进出光丝导通次数之和最大，相同取 id 小）。都没有返回 -1。
        /// （LighthouseFocus.Character 预留给人物链接，人物系统接入前同样走这里。）
        /// </summary>
        public static int PickDeepestMemoryGlow(GameState s)
        {
            Memory best = null;
            int bestGlow = -1;
            foreach (var m in s.Memories.Values)
            {
                int glow = -1;
                foreach (int id in m.Members)
                    if (s.IsCellActive(s.Glows[id].Cell)) { glow = id; break; }
                if (glow < 0) continue;
                if (best == null || m.TimesTriggered > best.TimesTriggered
                    || (m.TimesTriggered == best.TimesTriggered && m.FoldConductSum > best.FoldConductSum))
                {
                    best = m;
                    bestGlow = glow;
                }
            }
            if (bestGlow >= 0) return bestGlow;

            var total = new Dictionary<int, int>();
            foreach (var kv in s.Edges)
            {
                if (kv.Value.Pruned || kv.Value.Count <= 0) continue;
                total.TryGetValue(kv.Key.From, out int a); total[kv.Key.From] = a + kv.Value.Count;
                total.TryGetValue(kv.Key.To, out int b); total[kv.Key.To] = b + kv.Value.Count;
            }
            int bestId = -1, bestTotal = 0;
            foreach (var g in s.Glows.Values)
            {
                if (!s.IsCellActive(g.Cell) || !total.TryGetValue(g.Id, out int t)) continue;
                if (t > bestTotal) { bestTotal = t; bestId = g.Id; }
            }
            return bestId;
        }

        static void DisableCellsForOldAge(GameState s)
        {
            int toDisable = s.Config.OldAgeDisabledCellsPerRound;
            foreach (string region in s.Config.OldAgeDecayRegionOrder)
            {
                if (toDisable <= 0 || string.IsNullOrEmpty(region)) break;
                var candidates = new List<Cell>();
                foreach (var c in s.Board.Cells)
                    if (s.Board.RegionOf(c) == region[0] && s.IsCellActive(c)) candidates.Add(c);
                while (toDisable > 0 && candidates.Count > 0)
                {
                    int i = s.Rng.Next(candidates.Count);
                    s.DisabledCells.Add(candidates[i]);
                    candidates.RemoveAt(i);
                    toDisable--;
                }
            }
        }

        static void UpdatePickups(GameState s)
        {
            for (int i = s.Pickups.Count - 1; i >= 0; i--)
            {
                s.Pickups[i].RoundsLeft--;
                if (s.Pickups[i].RoundsLeft <= 0) s.Pickups.RemoveAt(i);
            }
            if (!s.StageDef.Pickups || s.Config.PickupItemPool == null || s.Config.PickupItemPool.Length == 0) return;

            int count = s.Rng.Range(s.Config.PickupMin, s.Config.PickupMax + 1);
            var free = new List<Cell>();
            foreach (var c in s.Board.Cells)
                if (BoardOps.CanPlace(s, c)) free.Add(c);
            for (int i = 0; i < count && free.Count > 0; i++)
            {
                int k = s.Rng.Next(free.Count);
                s.Pickups.Add(new Pickup
                {
                    Cell = free[k],
                    ItemId = s.Config.PickupItemPool[s.Rng.Next(s.Config.PickupItemPool.Length)],
                    RoundsLeft = s.Config.PickupLifetimeRounds,
                });
                free.RemoveAt(k);
            }
        }

        // ================= 结算 =================

        /// <summary>本轮结算并推进到下一轮/下一阶段（或结束本局）。</summary>
        public static RoundSettlement SettleRound(GameState s)
        {
            var cfg = s.Config;
            var r = s.Round;
            var st = new RoundSettlement { Stage = s.Stage, RoundInStage = s.RoundInStage, Target = s.CurrentTarget };

            st.TopPattern = r.TopPattern(cfg);
            st.PatternVoided = EffectQuery.PatternMustRepeat(s) && st.TopPattern != s.LastRoundTopPattern;
            st.Score = r.Estimate(!st.PatternVoided);
            st.GlowsFired = r.FiredGlows.Count;
            st.Passed = st.Score >= st.Target;
            if (!st.Passed) st.FailReason = "未达到目标分";
            int minFires = EffectQuery.MinFiresInRound(s);
            if (st.Passed && st.GlowsFired < minFires)
            {
                st.Passed = false;
                st.FailReason = "关口要求至少 " + minFires + " 个灵光闪过";
            }
            s.LastRoundTopPattern = st.TopPattern;

            if (st.Passed)
            {
                st.CoinsGained = cfg.CoinsPerRound;
                if (st.Score >= st.Target * (double)cfg.CoinsBonusScoreRatio) st.CoinsGained += cfg.CoinsBonus;
                s.Coins += st.CoinsGained;
            }

            ApplyDisuse(s);
            if (st.Passed && s.Stage == Stage.Adolescence && s.IsBossRound) st.AdolescentPruned = ApplyAdolescentPruning(s);
            UpdatePlasticity(s);
            ApplyUnanchored(s, st);
            s.TotalRoundsPlayed++;

            if (!st.Passed)
            {
                s.RunOver = true;
                st.RunOver = true;
                return st;
            }

            if (s.IsBossRound)
            {
                if (s.Stage == Stage.OldAge)
                {
                    s.RunOver = s.RunWon = true;
                    st.RunOver = st.RunWon = true;
                    return st;
                }
                // 阶段结算顺序：关口结算 → 记忆折叠 → 区域沉淀 → 下一阶段
                if (s.Stage <= Stage.MiddleAge) st.NewMemoryIds.AddRange(FoldMemories(s, s.Stage));
                st.NewSettledRegions.AddRange(SettleRegions(s, s.Stage));
                st.StageAdvanced = true;
                EnterStage(s, s.Stage + 1);
            }
            else
            {
                s.RoundInStage++;
                BeginRound(s);
            }
            return st;
        }

        /// <summary>当前所有光丝（不含记忆内部、含已剪除），并保证每条都有跨轮记录。</summary>
        static List<EdgeInfo> TrackedEdges(GameState s)
        {
            var edges = EdgeGeometry.Enumerate(s, true, false);
            foreach (var e in edges) s.GetOrCreateEdge(e.Key);
            return edges;
        }

        /// <summary>废退（每轮结算后，规格第 7 节）。</summary>
        public static void ApplyDisuse(GameState s)
        {
            DecayWeakLinks(s);
            var cfg = s.Config;
            var stageDef = s.StageDef;
            TrackedEdges(s);
            var keys = new List<EdgeKey>(s.Edges.Keys);
            keys.Sort();
            foreach (var key in keys)
            {
                if (IsMemoryInternal(s, key)) continue;
                var e = s.Edges[key];
                if (e.ConductedThisRound) { e.IdleRounds = 0; continue; }
                if (e.Pruned) continue;

                if (stageDef.PruneZeroWhenIdle && e.Count == 0) { e.Pruned = true; continue; }
                e.IdleRounds++;
                if (stageDef.DisuseRounds > 0 && e.IdleRounds >= stageDef.DisuseRounds)
                {
                    e.Count = Math.Max(0, e.Count - cfg.DisuseLevelDrop);
                    e.IdleRounds = 0;
                }
            }
        }

        /// <summary>
        /// 无依（规格第 7 节，每轮结算后）：没有任何导通次数 > 0 的有效光丝（进出都算）的灵光为无依；
        /// 你放下的灵光过了扎根期后连续 2 轮无依就漂走移除。本能灵光、记忆成员不适用。
        /// </summary>
        public static void ApplyUnanchored(GameState s, RoundSettlement st = null)
        {
            var cfg = s.Config;
            if (!cfg.UnanchoredEnabled) return;
            var anchored = new HashSet<int>();
            foreach (var kv in s.Edges)
            {
                if (kv.Value.Pruned || kv.Value.Count <= 0) continue;
                anchored.Add(kv.Key.From);
                anchored.Add(kv.Key.To);
            }
            var drift = new List<int>();
            foreach (var g in s.Glows.Values)
            {
                if (g.IsInstinct || g.MemoryId >= 0) continue;
                if (g.RootRoundsLeft > 0) { g.RootRoundsLeft--; continue; }
                if (anchored.Contains(g.Id)) { g.UnanchoredRounds = 0; continue; }
                g.UnanchoredRounds++;
                if (g.UnanchoredRounds >= cfg.UnanchoredRoundsToDrift) drift.Add(g.Id);
            }
            foreach (int id in drift)
            {
                if (st != null) { st.DriftedGlowIds.Add(id); st.DriftedCells.Add(s.Glows[id].Cell); }
                BoardOps.RemoveGlow(s, id);
                if (s.LighthouseFocusGlowId == id) s.LighthouseFocusGlowId = -1;
            }
        }

        /// <summary>微弱连接的废退：每轮减弱 WeakLinkDecayPerRound，减到 0 即消失。</summary>
        public static void DecayWeakLinks(GameState s)
        {
            int decay = s.Config.WeakLinkDecayPerRound;
            if (decay <= 0 || s.WeakLinks.Count == 0) return;
            var keys = new List<long>(s.WeakLinks.Keys);
            keys.Sort();
            foreach (long k in keys)
            {
                int v = s.WeakLinks[k] - decay;
                if (v <= 0) s.WeakLinks.Remove(k); else s.WeakLinks[k] = v;
            }
        }

        /// <summary>青春期修剪：导通 0 次 → 已剪除；≥ 3 次 → 再 +3。返回剪除条数。</summary>
        public static int ApplyAdolescentPruning(GameState s)
        {
            var cfg = s.Config;
            int pruned = 0;
            foreach (var info in TrackedEdges(s))
            {
                var e = s.Edges[info.Key];
                if (e.Pruned) continue;
                if (e.Count == 0) { e.Pruned = true; pruned++; }
                else if (e.Count >= cfg.PruneBoostThreshold) e.Count += cfg.PruneBoost;
            }
            return pruned;
        }

        static bool IsMemoryInternal(GameState s, EdgeKey key)
        {
            if (!s.Glows.TryGetValue(key.From, out var a) || !s.Glows.TryGetValue(key.To, out var b)) return false;
            return a.MemoryId >= 0 && a.MemoryId == b.MemoryId;
        }

        /// <summary>
        /// 可塑度回合更新（规格第 7 节，v0.3 由沉淀区域共用）：被触发的扣减在模拟时已做；
        /// 连续 3 轮被触发归零；连续 2 轮未被触发 +1（上限 3）。
        /// </summary>
        public static void UpdatePlasticity(GameState s)
        {
            var cfg = s.Config;
            foreach (var region in s.SettledRegions.Values)
            {
                if (region.TriggeredThisRound)
                {
                    region.ConsecutiveTriggeredRounds++;
                    region.ConsecutiveIdleRounds = 0;
                    if (region.ConsecutiveTriggeredRounds >= cfg.MemoryTriggeredRoundsToZero) region.Plasticity = 0;
                }
                else
                {
                    region.ConsecutiveTriggeredRounds = 0;
                    region.ConsecutiveIdleRounds++;
                    if (region.ConsecutiveIdleRounds >= cfg.MemoryIdleRoundsToRegain)
                    {
                        region.Plasticity = Math.Min(cfg.MemoryPlasticityMax, region.Plasticity + 1);
                        region.ConsecutiveIdleRounds = 0;
                    }
                }
            }
        }

        /// <summary>沉淀：刚结束阶段解锁的区域进入沉淀，共用一个可塑度（初始为上限）。返回新沉淀的区域字母。</summary>
        public static List<string> SettleRegions(GameState s, Stage endedStage)
        {
            var created = new List<string>();
            if (!s.Config.SettledRegionsEnabled) return created;
            foreach (var region in s.Config.Layout.Regions)
            {
                if (region.UnlockStage != endedStage || string.IsNullOrEmpty(region.Letter)) continue;
                char letter = region.Letter[0];
                if (s.SettledRegions.ContainsKey(letter)) continue;
                s.SettledRegions.Add(letter, new SettledRegion { Region = region.Letter, Plasticity = s.Config.MemoryPlasticityMax });
                created.Add(region.Letter);
            }
            return created;
        }

        /// <summary>
        /// 记忆折叠（阶段结算后）：在刚结束阶段解锁的区域内，按有效光丝（视为无向）找连通的灵光组，
        /// 取组内导通次数之和最大的一组（3–6 个）。区域最多约 10 格，直接枚举子集。
        /// </summary>
        public static List<int> FoldMemories(GameState s, Stage endedStage)
        {
            var cfg = s.Config;
            var created = new List<int>();
            foreach (var region in cfg.Layout.Regions)
            {
                if (region.UnlockStage != endedStage || string.IsNullOrEmpty(region.Letter)) continue;
                char letter = region.Letter[0];

                var members = new List<Glow>();
                foreach (var g in s.Glows.Values)
                    if (g.MemoryId < 0 && s.IsCellActive(g.Cell) && s.Board.RegionOf(g.Cell) == letter) members.Add(g);
                int n = members.Count;
                if (n < cfg.MemoryMinSize || n > 16) continue; // 子集枚举 2^n，地图改大时需换算法

                var index = new Dictionary<int, int>();
                for (int i = 0; i < n; i++) index[members[i].Id] = i;
                var adj = new int[n];
                var weight = new int[n, n];
                foreach (var e in EdgeGeometry.Enumerate(s, false, false))
                {
                    if (!index.TryGetValue(e.Source.Id, out int a) || !index.TryGetValue(e.Target.Id, out int b)) continue;
                    adj[a] |= 1 << b;
                    adj[b] |= 1 << a;
                    weight[a, b] += s.EdgeCount(e.Key);
                }

                int bestMask = 0, bestSum = -1, bestSize = 0;
                for (int mask = 1; mask < (1 << n); mask++)
                {
                    int size = PopCount(mask);
                    if (size < cfg.MemoryMinSize || size > cfg.MemoryMaxSize) continue;
                    if (!IsConnectedMask(mask, adj)) continue;
                    int sum = 0;
                    for (int a = 0; a < n; a++)
                    {
                        if ((mask & (1 << a)) == 0) continue;
                        for (int b = 0; b < n; b++)
                            if ((mask & (1 << b)) != 0) sum += weight[a, b];
                    }
                    if (sum > bestSum || (sum == bestSum && size > bestSize))
                    {
                        bestSum = sum; bestMask = mask; bestSize = size;
                    }
                }
                if (bestMask == 0 || (cfg.MemoryRequiresConduction && bestSum <= 0)) continue;

                var memory = new Memory { Id = s.NextMemoryId++, Region = region.Letter, FoldConductSum = bestSum };
                for (int i = 0; i < n; i++)
                {
                    if ((bestMask & (1 << i)) == 0) continue;
                    memory.Members.Add(members[i].Id);
                    members[i].MemoryId = memory.Id;
                }
                memory.Members.Sort();
                s.Memories.Add(memory.Id, memory);
                created.Add(memory.Id);
            }
            return created;
        }

        static int PopCount(int x)
        {
            int c = 0;
            while (x != 0) { x &= x - 1; c++; }
            return c;
        }

        static bool IsConnectedMask(int mask, int[] adj)
        {
            int start = mask & -mask;
            int seen = start, frontier = start;
            while (frontier != 0)
            {
                int bit = frontier & -frontier;
                frontier &= frontier - 1;
                int i = 0;
                while ((1 << i) != bit) i++;
                int next = adj[i] & mask & ~seen;
                seen |= next;
                frontier |= next;
            }
            return seen == mask;
        }
    }
}
