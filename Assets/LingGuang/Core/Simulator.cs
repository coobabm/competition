using System;
using System.Collections.Generic;
using System.Linq;

namespace LingGuang.Core
{
    /// <summary>
    /// One confirm = one chain. Input: board state + chosen start; output: ordered SimEvent list (and Model mutation).
    /// Deterministic: no randomness is used inside a chain.
    /// </summary>
    public sealed class Simulator
    {
        enum DKind { Edge, Lighthouse, Ripple, Weak, Dangling, Carry }

        sealed class Delivery
        {
            public DKind kind;
            public int fromSpark = -1, fromNode = -1, toNode = -1, toCell = -1, amount, group = -1, fireInst = -1;
            public long edgeKey;
        }

        readonly RunState s;
        readonly GameConfig cfg;
        readonly HexBoard B;
        readonly Mods mods;
        public readonly List<SimEvent> events = new List<SimEvent>();
        public readonly ChainStats stats = new ChainStats();
        public readonly Dictionary<int, List<int>> firedCellsByBeat = new Dictionary<int, List<int>>();

        readonly SortedDictionary<int, List<Delivery>> pending = new SortedDictionary<int, List<Delivery>>();
        readonly HashSet<int> receivedThisChain = new HashSet<int>();
        readonly Dictionary<int, int> chainFires = new Dictionary<int, int>();
        readonly Dictionary<int, HashSet<int>> divergeTargets = new Dictionary<int, HashSet<int>>();
        readonly Dictionary<int, bool> fireInstIsCone = new Dictionary<int, bool>();
        int fireInstCounter;
        readonly HashSet<(int, long)> weakSent = new HashSet<(int, long)>();
        readonly Dictionary<int, int> firesPerBeat = new Dictionary<int, int>();
        int groupCounter;

        Simulator(RunState s)
        {
            this.s = s;
            cfg = s.cfg;
            B = s.board;
            mods = new Mods(s);
        }

        void Emit(SimEvent e) => events.Add(e);

        /// <param name="maxBeat">stop after this beat (preview); -1 = run to completion</param>
        public static Simulator Run(RunState s, int startSparkId, bool firstConfirmOfRound, int maxBeat = -1)
        {
            var sim = new Simulator(s);
            sim.Execute(startSparkId, firstConfirmOfRound, maxBeat);
            return sim;
        }

        void Schedule(int beat, Delivery d)
        {
            if (!pending.TryGetValue(beat, out var l)) pending[beat] = l = new List<Delivery>();
            l.Add(d);
        }

        void Execute(int startSparkId, bool firstConfirm, int maxBeat)
        {
            int beat = 0;
            var startNode = s.nodes[s.sparks[startSparkId].nodeId];

            // Lighthouse: first confirm of each round, beat 0
            if (firstConfirm && !mods.Has(Op.NoLighthouse))
            {
                var cells = s.LighthouseCells();
                var lhNodes = new HashSet<int>();
                foreach (int c in cells)
                {
                    int sp = s.cellSpark[c];
                    if (sp < 0) continue;
                    int nid = s.sparks[sp].nodeId;
                    if (nid == startNode.id) continue; // lighthouse shines first; the start fires and clears it anyway
                    if (!lhNodes.Add(nid)) continue;
                    Schedule(0, new Delivery { kind = DKind.Lighthouse, toNode = nid, toCell = c, amount = cfg.lighthouseCharge, fromNode = -2 });
                }
                Emit(new SimEvent { type = SimEventType.Lighthouse, beat = 0, cells = new List<int>(cells), amount = cfg.lighthouseCharge });
            }

            var firedThisBeat = new HashSet<int>();
            var deliveredThisBeat = new Dictionary<int, List<Delivery>>();
            var newly = new List<(Node node, bool isStart)>();

            // start fires directly
            FireNode(startNode, beat, true, false, 0, newly, firedThisBeat);

            while (true)
            {
                var rippleCenters = new List<int>();
                // sub-passes inside the beat
                while (true)
                {
                    // 1. newly fired emit deliveries
                    foreach (var (node, isStart) in newly) EmitDeliveries(node, isStart, beat);
                    newly.Clear();

                    // 2. accumulate everything pending for this beat
                    if (!pending.TryGetValue(beat, out var batch) || batch.Count == 0) break;
                    pending.Remove(beat);
                    var touched = new List<int>();
                    var rippleApplied = new Dictionary<int, List<int>>();
                    foreach (var d in batch) Accumulate(d, beat, deliveredThisBeat, touched, rippleApplied);
                    if (cfg.weakLinksEnabled)
                        foreach (var kv in rippleApplied) s.AddWeakLinks(kv.Value);

                    // 3. threshold check
                    foreach (int nid in touched)
                    {
                        if (firedThisBeat.Contains(nid)) continue;
                        var n = s.nodes[nid];
                        if (n.charge < mods.Threshold(n)) continue;
                        deliveredThisBeat.TryGetValue(nid, out var dl);
                        int convergeSources = 0;
                        bool viaEdge = false;
                        if (dl != null)
                        {
                            var srcs = new HashSet<int>();
                            foreach (var d in dl)
                            {
                                if (d.kind == DKind.Edge) { viaEdge = true; srcs.Add(d.fromNode); }
                                else if (d.kind == DKind.Lighthouse) srcs.Add(-2);
                            }
                            convergeSources = srcs.Count;
                        }
                        bool viaRipple = !viaEdge && dl != null && dl.Exists(d => d.kind == DKind.Ripple || d.kind == DKind.Weak);
                        FireNode(n, beat, false, viaRipple, convergeSources, newly, firedThisBeat);

                        // conducts
                        if (dl != null)
                        {
                            int conductCell = -1;
                            foreach (var d in dl)
                            {
                                if (d.kind != DKind.Edge) continue;
                                if (!s.edges.TryGetValue(d.edgeKey, out var e)) continue;
                                int oldLvl = mods.EdgeLevel(e);
                                e.count += mods.ConductIncrement(e);
                                e.conductedThisRound = true;
                                stats.conducts++;
                                int newLvl = mods.EdgeLevel(e);
                                Emit(new SimEvent { type = SimEventType.Conduct, beat = beat, cell = s.sparks[e.from].cell, cellB = s.sparks[e.to].cell, level = newLvl, amount = e.count });
                                if (newLvl != oldLvl)
                                    Emit(new SimEvent { type = SimEventType.EdgeLevelUp, beat = beat, cell = s.sparks[e.from].cell, cellB = s.sparks[e.to].cell, level = newLvl });
                                rippleCenters.Add(d.toCell);
                                conductCell = d.toCell;
                                if (d.fireInst >= 0)
                                {
                                    if (!divergeTargets.TryGetValue(d.fireInst, out var set)) divergeTargets[d.fireInst] = set = new HashSet<int>();
                                    set.Add(nid);
                                }
                            }
                        }
                        if (viaRipple)
                        {
                            stats.rippleConducts++;
                            foreach (int spId in n.sparks)
                                if (spId == s.conchSpark && mods.Has(Op.RippleRecursionDesignate))
                                    rippleCenters.Add(s.sparks[spId].cell);
                        }
                        if (dl != null) dl.Clear();
                    }
                    if (newly.Count == 0) break;
                }

                // 5. ripples of this beat -> merged groups, take effect next beat
                if (rippleCenters.Count > 0) SpawnRipples(rippleCenters, beat);

                if (maxBeat >= 0 && beat >= maxBeat) break;

                // 6. carry-over: a spark that already fired this beat but still holds >= threshold fires next beat
                //    (otherwise 0-delay myelin loops would stall inside one beat)
                if (cfg.carryOverFire)
                    foreach (int nid in receivedThisChain)
                    {
                        var cn = s.nodes[nid];
                        int after = cn.charge > 0 ? Math.Max(0, cn.charge - cfg.leakPerBeat) : cn.charge;
                        if (after >= mods.Threshold(cn) && firedThisBeat.Contains(nid))
                            Schedule(beat + 1, new Delivery { kind = DKind.Carry, toNode = nid, toCell = s.sparks[cn.sparks[0]].cell, amount = 0 });
                    }

                // 6b. continue or end
                bool more = false;
                foreach (var kv in pending) if (kv.Value.Count > 0) { more = true; break; }
                if (!more)
                {
                    if (cfg.leakAfterLastBeat) Leak(beat);
                    break;
                }
                Leak(beat);
                if (cfg.infiniteEnabled && DetectLoop(beat)) break;
                Emit(new SimEvent { type = SimEventType.BeatEnd, beat = beat });
                beat++;
                firedThisBeat.Clear();
                foreach (var kv in deliveredThisBeat) kv.Value.Clear();
                if (beat >= cfg.maxBeats)
                {
                    stats.aborted = true;
                    Emit(new SimEvent { type = SimEventType.Error, beat = beat, text = "超过安全拍数上限，连锁强制结束" });
                    break;
                }
            }

            // stats
            foreach (var kv in chainFires) if (kv.Value >= 2) stats.reverb = true;
            foreach (var kv in divergeTargets)
                if (fireInstIsCone.TryGetValue(kv.Key, out bool cone) && cone && kv.Value.Count >= cfg.divergeMinFires) stats.diverge = true;
            Emit(new SimEvent { type = SimEventType.ChainEnd, beat = beat });
        }

        // ---- infinite reverb: the whole chain state repeats -> it would loop forever
        readonly Dictionary<string, (int beat, int light, float mult)> seenStates = new Dictionary<string, (int, int, float)>();

        bool DetectLoop(int beat)
        {
            var sb = new System.Text.StringBuilder(256);
            foreach (var n in s.nodes.Values.OrderBy(x => x.id))
                if (n.charge != 0 || n.thresholdRaise != 0 || n.firedThisRound) sb.Append(n.id).Append(':').Append(n.charge).Append(':').Append(n.thresholdRaise).Append(n.firedThisRound ? 'f' : 'u').Append(';');
            sb.Append('|');
            foreach (var e in s.edges.Values.OrderBy(x => x.KeyOf))
                if (e.stp > 0 || e.count > 0)
                    sb.Append(e.KeyOf).Append(':').Append(e.stp).Append(':').Append(mods.EdgeLevel(e)).Append(mods.BridgePenalty(e) ? 'b' : 'n').Append(';');
            sb.Append('|');
            // only "active or not" changes behaviour, so clamp (otherwise co-covered unfired pairs grow forever and hide the loop)
            foreach (var kv in s.weakLinks.OrderBy(x => x.Key)) sb.Append(kv.Key).Append(':').Append(Math.Min(kv.Value, cfg.weakLinkActiveAt)).Append(';');
            sb.Append('|').Append(s.pickups.Count).Append('|');
            foreach (var kv in pending)
                foreach (var d in kv.Value.OrderBy(x => x.toNode).ThenBy(x => x.edgeKey).ThenBy(x => x.amount).ThenBy(x => (int)x.kind))
                    sb.Append(kv.Key - beat).Append(',').Append((int)d.kind).Append(',').Append(d.toNode).Append(',').Append(d.toCell).Append(',').Append(d.amount).Append(',').Append(d.edgeKey).Append(';');
            string sig = sb.ToString();
            if (!seenStates.TryGetValue(sig, out var prev))
            {
                seenStates[sig] = (beat, stats.lightGained, stats.multGained);
                return false;
            }
            int period = beat - prev.beat;
            int cycleLight = stats.lightGained - prev.light;
            float cycleMult = stats.multGained - prev.mult;
            if (period <= 0 || (cycleLight <= 0 && cycleMult <= 0)) return false;
            stats.infinite = true;
            stats.loopPeriod = period;
            stats.loopCycleLight = cycleLight;
            stats.loopCycleMult = cycleMult;
            int extraLight = cycleLight * cfg.infiniteCredits;
            float extraMult = 0f; // multiplier is not credited per lap (keeps scores bounded); the pattern itself adds mult
            s.roundLight += extraLight;
            s.roundAddMult += extraMult;
            stats.lightGained += extraLight;
            stats.multGained += extraMult;
            pending.Clear();
            Emit(new SimEvent { type = SimEventType.InfiniteLoop, beat = beat, amount = period, light = extraLight, mult = extraMult, fireIndex = cfg.infiniteCredits, text = "无限回荡" });
            Emit(new SimEvent { type = SimEventType.ScoreDelta, beat = beat, light = extraLight, mult = extraMult, text = "infinite" });
            return true;
        }

        void Leak(int beat)
        {
            if (cfg.leakPerBeat <= 0) return;
            foreach (int nid in receivedThisChain)
            {
                var n = s.nodes[nid];
                if (n.charge <= 0) continue;
                n.charge = Math.Max(0, n.charge - cfg.leakPerBeat);
                Emit(new SimEvent { type = SimEventType.Leak, beat = beat, nodeId = nid, cell = s.sparks[n.sparks[0]].cell, charge = n.charge, threshold = mods.Threshold(n) });
            }
        }

        void Accumulate(Delivery d, int beat, Dictionary<int, List<Delivery>> deliveredThisBeat, List<int> touched, Dictionary<int, List<int>> rippleApplied)
        {
            if (d.kind == DKind.Dangling)
            {
                Emit(new SimEvent { type = SimEventType.Lost, beat = beat, cell = d.fromSpark >= 0 && s.sparks.ContainsKey(d.fromSpark) ? s.sparks[d.fromSpark].cell : -1, cellB = d.toCell, amount = d.amount });
                var pk = s.pickups.Find(p => p.cell == d.toCell);
                if (pk != null)
                {
                    s.pickups.Remove(pk);
                    s.CollectPickup(pk);
                    Emit(new SimEvent { type = SimEventType.PickupCollected, beat = beat, cell = d.toCell, text = pk.itemId });
                }
                return;
            }
            if (!s.nodes.TryGetValue(d.toNode, out var n)) return;
            if (s.disabledCells.Contains(d.toCell)) return;
            if (d.kind == DKind.Carry)
            {
                if (!touched.Contains(n.id)) touched.Add(n.id);
                return;
            }
            if ((d.kind == DKind.Ripple || d.kind == DKind.Weak) && n.firedThisRound) return;
            if (d.kind == DKind.Ripple)
            {
                if (!rippleApplied.TryGetValue(d.group, out var l)) rippleApplied[d.group] = l = new List<int>();
                l.Add(n.id);
            }
            n.charge += d.amount;
            receivedThisChain.Add(n.id);
            if (!deliveredThisBeat.TryGetValue(n.id, out var dl)) deliveredThisBeat[n.id] = dl = new List<Delivery>();
            dl.Add(d);
            if (!touched.Contains(n.id)) touched.Add(n.id);
            var type = d.kind == DKind.Ripple ? SimEventType.RippleApply : d.kind == DKind.Weak ? SimEventType.WeakLinkDeliver : SimEventType.Deliver;
            int fromCell = d.fromSpark >= 0 ? s.sparks[d.fromSpark].cell : -1;
            Emit(new SimEvent
            {
                type = type, beat = beat, cell = fromCell, cellB = d.toCell, amount = d.amount, nodeId = n.id,
                charge = n.charge, threshold = mods.Threshold(n),
                level = d.kind == DKind.Edge && s.edges.TryGetValue(d.edgeKey, out var e) ? mods.EdgeLevel(e) : 0,
                viaRipple = d.kind == DKind.Lighthouse
            });
        }

        void FireNode(Node n, int beat, bool isStart, bool viaRipple, int convergeSources, List<(Node, bool)> newly, HashSet<int> firedThisBeat)
        {
            firedThisBeat.Add(n.id);
            chainFires.TryGetValue(n.id, out int cf);
            chainFires[n.id] = ++cf;
            bool feverNoScore = n.firedThisRound && mods.Has(Op.FiredNoScore);
            int threshold = mods.Threshold(n);
            n.fireCountRound++;
            n.triggerCount++;
            n.triggeredThisRound = true;
            stats.fires++;
            stats.maxBeat = Math.Max(stats.maxBeat, beat);
            if (beat > 0)
            {
                firesPerBeat.TryGetValue(beat, out int fpb);
                firesPerBeat[beat] = fpb + n.sparks.Count;
                stats.maxFiresInBeat = Math.Max(stats.maxFiresInBeat, fpb + n.sparks.Count);
            }
            if (!firedCellsByBeat.TryGetValue(beat, out var fc)) firedCellsByBeat[beat] = fc = new List<int>();

            int inst = fireInstCounter++;
            bool isCone = !n.isMemory && s.sparks[n.sparks[0]].shape == Shape.Cone;
            fireInstIsCone[inst] = isCone;

            if (!n.isMemory && s.sparks[n.sparks[0]].shape == Shape.Converge && convergeSources >= cfg.convergeMinSources)
                stats.convergeBurst = true;

            foreach (int spId in n.sparks)
            {
                var sp = s.sparks[spId];
                if (s.disabledCells.Contains(sp.cell)) continue;
                sp.fireCountTotal++;
                bool first = !s.firstFireDone;
                s.firstFireDone = true;
                int light = feverNoScore ? 0 : mods.FireLight(sp, n, first, convergeSources);
                float addMult = feverNoScore ? 0 : mods.FireAddMult(sp);
                s.roundLight += light;
                s.roundAddMult += addMult;
                stats.lightGained += light;
                stats.multGained += addMult;
                var reg = s.RegionStateOf(B.regionOf[sp.cell]);
                if (reg != null) reg.triggeredThisRound = true;
                fc.Add(sp.cell);
                Emit(new SimEvent
                {
                    type = SimEventType.Fire, beat = beat, cell = sp.cell, sparkId = sp.id, nodeId = n.id, light = light, mult = addMult,
                    fireIndex = cf, isStart = isStart, viaRipple = viaRipple, threshold = threshold, amount = convergeSources
                });
                if (light > 0 || addMult > 0)
                    Emit(new SimEvent { type = SimEventType.ScoreDelta, beat = beat, cell = sp.cell, light = light, mult = addMult, text = "fire" });
            }
            if (!s.firedNodesThisRound.Contains(n.id)) s.firedNodesThisRound.Add(n.id);
            n.firedThisRound = true;
            n.charge = 0;
            n.thresholdRaise += cfg.refireThresholdStep;
            if (cfg.refireRaiseCap >= 0) n.thresholdRaise = Math.Min(n.thresholdRaise, cfg.refireRaiseCap);
            newly.Add((n, isStart));
            n.lastFireInst = inst;

            // weak links: wake partner next beat
            if (cfg.weakLinksEnabled)
            {
                foreach (var kv in s.weakLinks)
                {
                    if (kv.Value < cfg.weakLinkActiveAt) continue;
                    RunState.SplitPair(kv.Key, out int a, out int b);
                    int other = a == n.id ? b : b == n.id ? a : -1;
                    if (other < 0 || !s.nodes.TryGetValue(other, out var on) || on.firedThisRound) continue;
                    if (!weakSent.Add((beat, kv.Key))) continue; // at most once per pair per beat
                    Schedule(beat + 1, new Delivery { kind = DKind.Weak, toNode = other, toCell = s.sparks[on.sparks[0]].cell, amount = cfg.weakLinkCharge });
                }
            }
        }

        void EmitDeliveries(Node n, bool isStart, int beat)
        {
            int inst = n.lastFireInst;
            foreach (int spId in n.sparks)
            {
                var sp = s.sparks[spId];
                if (s.disabledCells.Contains(sp.cell)) continue;
                foreach (int rel in s.Dirs(sp))
                {
                    int d = (rel + sp.dir) % 6;
                    int nc = B.Neighbor(sp.cell, d);
                    if (nc < 0 || !s.IsActiveCell(nc) || B.IsGully(sp.cell, nc, s.stage)) continue;
                    int tId = s.cellSpark[nc];
                    if (tId < 0)
                    {
                        Schedule(beat + cfg.baseDelay, new Delivery { kind = DKind.Dangling, fromSpark = sp.id, toCell = nc, amount = mods.Output(sp) });
                        continue;
                    }
                    var t = s.sparks[tId];
                    if (t.nodeId == n.id) continue; // inside a memory
                    long key = Edge.Key(sp.id, tId);
                    if (!s.edges.TryGetValue(key, out var e) || e.pruned) continue;
                    int amount = mods.DeliveryAmount(e, isStart);
                    int delay = mods.Delay(e);
                    e.stp = Math.Min(e.stp + cfg.stpStep, cfg.stpCap);
                    Schedule(beat + delay, new Delivery { kind = DKind.Edge, fromSpark = sp.id, fromNode = n.id, toNode = t.nodeId, toCell = nc, amount = amount, edgeKey = key, fireInst = inst });
                }
            }
        }

        void SpawnRipples(List<int> centers, int beat)
        {
            foreach (int c in centers) Emit(new SimEvent { type = SimEventType.RippleSpawn, beat = beat, cell = c });
            // union-find over centers within merge distance
            int n = centers.Count;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (B.HexDistance(centers[i], centers[j]) <= cfg.rippleMergeDistance) parent[Find(i)] = Find(j);
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int r = Find(i);
                if (!groups.TryGetValue(r, out var g)) groups[r] = g = new List<int>();
                g.Add(centers[i]);
            }
            foreach (var g in groups.Values)
            {
                int radius = Math.Min(cfg.rippleRadiusCap, mods.RippleRadiusBase + (g.Count - 1) / 2);
                var cover = new HashSet<int>();
                foreach (int c in g)
                {
                    if (radius <= 1)
                    {
                        foreach (var kv in B.PathDistances(c, radius, s.stage, s.IsActiveCell)) cover.Add(kv.Key);
                    }
                    else
                    {
                        foreach (int cell in B.AllCells())
                            if (s.IsActiveCell(cell) && B.HexDistance(c, cell) <= radius) cover.Add(cell);
                    }
                }
                int gid = groupCounter++;
                var coverList = new List<int>(cover);
                coverList.Sort();
                Emit(new SimEvent { type = SimEventType.RippleMerge, beat = beat, cells = coverList, radius = radius, amount = g.Count, cell = g[0], text = string.Join(",", g) });
                var seenNodes = new HashSet<int>();
                foreach (int cell in coverList)
                {
                    int sp = s.cellSpark[cell];
                    if (sp < 0) continue;
                    int nid = s.sparks[sp].nodeId;
                    if (!seenNodes.Add(nid)) continue;
                    Schedule(beat + 1, new Delivery { kind = DKind.Ripple, toNode = nid, toCell = cell, amount = cfg.rippleCharge, group = gid });
                }
            }
        }
    }
}
