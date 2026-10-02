using System;
using System.Collections.Generic;
using System.Linq;

namespace LingGuang.Core
{
    /// <summary>Round flow, player operations, settlement, memory folding, economy and characters.</summary>
    public sealed partial class RunState
    {
        public string lastError;

        bool Fail(string msg) { lastError = msg; return false; }

        // ------------------------------------------------------------------ round start

        public void StartRound()
        {
            phase = Phase.Prepare;
            roundLight = 0; roundAddMult = 0; patternLight = 0; patternMult = 0;
            roundTopPattern = null;
            firstConfirmDone = false; firstFireDone = false;
            firedNodesThisRound.Clear();
            drugsUsedThisRound = 0; trialUsed = false; placedThisRound = false;
            reviewActive = false;
            patternsThisRound.Clear();

            foreach (var n in nodes.Values)
            {
                n.charge = 0; n.thresholdRaise = 0; n.firedThisRound = false; n.fireCountRound = 0; n.triggeredThisRound = false;
            }
            foreach (var e in edges.Values) { e.stp = 0; e.conductedThisRound = false; }
            foreach (var r in regions.Values) r.triggeredThisRound = false;

            // old age decline: disable cells, F first then T
            if (stage == (int)Stage.Old)
            {
                var m0 = new Mods(this);
                int count = Math.Max(0, cfg.oldDisableCellsPerRound + m0.Sum(Op.OldDisableAdd));
                for (int i = 0; i < count; i++)
                {
                    var pool = board.AllCells().Where(c => board.regionOf[c] == Region.F && !disabledCells.Contains(c)).ToList();
                    if (pool.Count == 0) pool = board.AllCells().Where(c => board.regionOf[c] == Region.T && !disabledCells.Contains(c)).ToList();
                    if (pool.Count == 0) break;
                    pool.Sort();
                    disabledCells.Add(pool[rng.Next(pool.Count)]);
                }
            }

            // instinct life cycle
            foreach (var sp in sparks.Values.Where(x => x.shape == Shape.Instinct))
            {
                if (stage == (int)Stage.Old) sp.light = Math.Min(cfg.instinctLightMax, sp.light + cfg.instinctOldGain);
                else if (stage >= (int)Stage.Youth) sp.light = Math.Max(0, sp.light - cfg.instinctLightDecay);
            }

            var mods = new Mods(this);
            int startCharge = mods.Sum(Op.ChargeAtRoundStart);
            if (startCharge != 0) foreach (var n in nodes.Values) n.charge += startCharge;

            // candidates
            candidates.Clear();
            var pool2 = new List<Shape> { Shape.Conduct };
            if (stage >= (int)Stage.Child) pool2.Add(Shape.Cone);
            if (stage >= (int)Stage.Youth) pool2.Add(Shape.Converge);
            for (int i = 0; i < cfg.candidatesPerRound; i++) candidates.Add(pool2[rng.Next(pool2.Count)]);

            // pickups (childhood only)
            if (stage == cfg.pickupStage)
            {
                int n = rng.Range(cfg.pickupMin, cfg.pickupMax + 1);
                var empty = board.AllCells().Where(c => IsActiveCell(c) && cellSpark[c] < 0 && !pickups.Any(p => p.cell == c)).ToList();
                var itemPool = EffectDef.All.Where(d => d.kind == DefKind.Item && d.firstBatch && !items.Contains(d.id)).ToList();
                for (int i = 0; i < n && empty.Count > 0 && itemPool.Count > 0; i++)
                {
                    int ci = rng.Next(empty.Count);
                    var it = itemPool[rng.Next(itemPool.Count)];
                    pickups.Add(new Pickup { cell = empty[ci], itemId = it.id, roundsLeft = cfg.pickupLife });
                    empty.RemoveAt(ci);
                    itemPool.Remove(it);
                }
            }

            startsLeft = mods.Starts();
            movesLeft = IsBossRound ? cfg.movesBossRound : cfg.movesPerRound;
            shopRerolls = 0;
            freeRerollsUsed = 0;
            RefreshShop();
        }

        void RefreshShop()
        {
            shop.Clear();
            if (stage < cfg.shopFromStage) return;
            var itemPool = EffectDef.All.Where(d => d.kind == DefKind.Item && !items.Contains(d.id)).ToList();
            var drugPool = EffectDef.All.Where(d => d.kind == DefKind.Drug).ToList();
            for (int i = 0; i < cfg.shopItems && itemPool.Count > 0; i++)
            {
                var d = itemPool[rng.Next(itemPool.Count)];
                itemPool.Remove(d);
                shop.Add(new ShopOffer { id = d.id, price = cfg.itemPrice });
            }
            for (int i = 0; i < cfg.shopDrugs && drugPool.Count > 0; i++)
            {
                var d = drugPool[rng.Next(drugPool.Count)];
                drugPool.Remove(d);
                shop.Add(new ShopOffer { id = d.id, price = cfg.drugPrice });
            }
        }

        // ------------------------------------------------------------------ operations

        public bool CanPlaceAt(int cell)
        {
            return IsActiveCell(cell) && cellSpark[cell] < 0 && !pickups.Any(p => p.cell == cell);
        }

        public bool Place(int candidateIndex, int cell, int dir)
        {
            if (phase != Phase.Prepare) return Fail("现在不能放置");
            if (placedThisRound) return Fail("本轮已经放置过");
            if (candidateIndex < 0 || candidateIndex >= candidates.Count) return Fail("没有这个候选");
            if (!CanPlaceAt(cell)) return Fail("这里不能放置");
            AddSpark(candidates[candidateIndex], cell, dir, true);
            placedThisRound = true;
            candidates.Clear();
            return true;
        }

        public bool SkipCandidates()
        {
            if (phase != Phase.Prepare || placedThisRound) return Fail("现在不能放弃");
            candidates.Clear();
            placedThisRound = true;
            return true;
        }

        public bool CanAdjust(Spark sp, out string why)
        {
            why = null;
            if (sp.shape == Shape.Instinct) { why = "本能灵光不可移动、不可旋转"; return false; }
            if (sp.isCharacter) { why = "流光是别人的灵光，不能调整"; return false; }
            var n = nodes[sp.nodeId];
            if (n.isMemory && n.plasticity <= 0) { why = "记忆已固化（可塑度 0）"; return false; }
            var rs = RegionStateOf(board.regionOf[sp.cell]);
            if (rs != null && rs.sedimented && rs.plasticity <= 0) { why = "陈光区域已固化（可塑度 0）"; return false; }
            return true;
        }

        void PayAdjust(Spark sp)
        {
            var n = nodes[sp.nodeId];
            if (n.isMemory) n.plasticity = Math.Max(0, n.plasticity - 1);
            var rs = RegionStateOf(board.regionOf[sp.cell]);
            if (rs != null && rs.sedimented) rs.plasticity = Math.Max(0, rs.plasticity - 1);
        }

        public bool Rotate(int sparkId, int delta)
        {
            if (phase != Phase.Prepare) return Fail("现在不能旋转");
            if (!sparks.TryGetValue(sparkId, out var sp)) return Fail("没有灵光");
            if (!CanAdjust(sp, out var why)) return Fail(why);
            PayAdjust(sp);
            RemoveEdgesOf(sp.id, true, false);
            sp.dir = (((sp.dir + delta) % 6) + 6) % 6;
            RebuildEdges();
            stats.rotations++;
            return true;
        }

        public bool Move(int sparkId, int cell)
        {
            if (phase != Phase.Prepare) return Fail("现在不能移动");
            if (!sparks.TryGetValue(sparkId, out var sp)) return Fail("没有灵光");
            if (movesLeft <= 0) return Fail("本轮移动次数已用完");
            if (!CanAdjust(sp, out var why)) return Fail(why);
            if (nodes[sp.nodeId].isMemory) return Fail("记忆成员不能移动");
            if (!CanPlaceAt(cell)) return Fail("这里不能放置");
            var dst = RegionStateOf(board.regionOf[cell]);
            bool crossRegion = board.regionOf[cell] != board.regionOf[sp.cell];
            if (crossRegion && dst != null && dst.sedimented && dst.plasticity <= 0) return Fail("目标陈光区域已固化（可塑度 0）");
            PayAdjust(sp);
            if (crossRegion && dst != null && dst.sedimented) dst.plasticity = Math.Max(0, dst.plasticity - 1);
            RemoveEdgesOf(sp.id, true, true);
            cellSpark[sp.cell] = -1;
            sp.cell = cell;
            cellSpark[cell] = sp.id;
            nodes[sp.nodeId].region = board.regionOf[cell];
            movesLeft--;
            RebuildEdges();
            stats.moves++;
            return true;
        }

        public bool SetConch(int sparkId)
        {
            if (!items.Contains("conch")) return Fail("没有海螺");
            if (!sparks.ContainsKey(sparkId)) return Fail("没有灵光");
            conchSpark = sparkId;
            return true;
        }

        public bool SetEarphoneRegion(Region r)
        {
            if (!items.Contains("earphones")) return Fail("没有耳机");
            earphoneRegion = r;
            return true;
        }

        public bool Buy(int offerIndex)
        {
            if (phase != Phase.Prepare) return Fail("现在不能购买");
            if (offerIndex < 0 || offerIndex >= shop.Count) return Fail("没有这个商品");
            var o = shop[offerIndex];
            if (o.sold) return Fail("已售出");
            int price = PriceOf(o);
            if (coins < price) return Fail("余光不足");
            var d = EffectDef.Get(o.id);
            if (d.kind == DefKind.Item)
            {
                if (items.Count >= ItemSlots) return Fail("物件槽已满");
                items.Add(d.id);
                stats.itemsUsed++;
            }
            else
            {
                if (drugs.Count >= cfg.drugCarryMax) return Fail("药品最多携带 " + cfg.drugCarryMax + " 个");
                drugs.Add(d.id);
            }
            coins -= price;
            o.sold = true;
            return true;
        }

        public int shopRerolls;
        public int freeRerollsUsed;
        public int ShopRerollCost => freeRerollsUsed < new Mods(this).Sum(Op.FreeRerolls) ? 0 : 2 + shopRerolls;
        public int PriceOf(ShopOffer o) => Math.Max(0, o.price + new Mods(this).Sum(Op.PriceAdd));

        public bool RerollShop()
        {
            if (phase != Phase.Prepare || shop.Count == 0) return Fail("现在没有商店");
            int cost = ShopRerollCost;
            if (coins < cost) return Fail("余光不足");
            coins -= cost;
            if (cost == 0) freeRerollsUsed++; else shopRerolls++;
            RefreshShop();
            return true;
        }

        public bool SellItem(int index)
        {
            if (index < 0 || index >= items.Count) return Fail("没有这个物件");
            items.RemoveAt(index);
            coins += cfg.itemPrice / 2;
            return true;
        }

        public bool UseDrug(int index)
        {
            if (phase != Phase.Prepare) return Fail("现在不能用药");
            if (index < 0 || index >= drugs.Count) return Fail("没有这个药品");
            if (drugsUsedThisRound >= cfg.drugsPerRound) return Fail("每轮最多使用 " + cfg.drugsPerRound + " 个药品");
            var d = EffectDef.Get(drugs[index]);
            drugs.RemoveAt(index);
            drugsUsedThisRound++;
            stats.drugsUsed++;
            if (d.warning) stats.warnings++;
            foreach (var e in d.now)
            {
                effects.Add(new ActiveEffect { sourceId = d.id, effect = e, roundsLeft = 1 });
                if (e.op == Op.StartsAdd)
                {
                    startsLeft = Math.Max(0, startsLeft + e.value);
                    foreach (var cap in new Mods(this).Of(Op.StartsSet)) startsLeft = Math.Min(startsLeft, cap.value);
                }
                if (e.op == Op.AddMultFlat) roundAddMult += e.value;
                if (e.op == Op.MoveConsumed) movesLeft = 0;
                if (e.op == Op.CopyLastPattern && lastRoundTopPattern != null)
                {
                    var pd = cfg.patterns.FirstOrDefault(p => p.id == lastRoundTopPattern);
                    if (pd != null) { roundLight += pd.light; roundAddMult += pd.mult; }
                }
                if (e.op == Op.FreshnessStepsUntilStageEnd) effects[effects.Count - 1].roundsLeft = 3 - roundInStage;
            }
            foreach (var e in d.next) effects.Add(new ActiveEffect { sourceId = d.id, effect = e, roundsLeft = d.nextRounds, delayRounds = 1 });
            foreach (var e in d.permanent) effects.Add(new ActiveEffect { sourceId = d.id, effect = e, roundsLeft = -1 });
            return true;
        }

        // ------------------------------------------------------------------ confirm

        public bool CanConfirm(int sparkId, out string why)
        {
            why = null;
            if (phase != Phase.Prepare) { why = "现在不能发动"; return false; }
            if (startsLeft <= 0) { why = "起点次数已用完"; return false; }
            if (!sparks.TryGetValue(sparkId, out var sp)) { why = "请先选择一个灵光作为起点"; return false; }
            if (!IsActiveCell(sp.cell)) { why = "这个格子已失效"; return false; }
            return true;
        }

        public ConfirmResult Confirm(int startSparkId)
        {
            if (!CanConfirm(startSparkId, out var why)) { lastError = why; return null; }
            var sim = Simulator.Run(this, startSparkId, !firstConfirmDone);
            firstConfirmDone = true;
            startsLeft--;
            stats.confirms++;
            if (sim.stats.reverb) stats.reverbCount++;
            var result = new ConfirmResult { events = sim.events, stats = sim.stats };
            IdentifyPattern(sim, result);
            return result;
        }

        /// <summary>Simulate on a clone (lens preview / trial run). The real state is untouched.</summary>
        public Simulator Preview(int startSparkId, int maxBeat)
        {
            var c = Clone();
            if (!c.CanConfirm(startSparkId, out _)) return null;
            return Simulator.Run(c, startSparkId, !c.firstConfirmDone, maxBeat);
        }

        static readonly string[] PatternRank = { "infinite", "long", "converge", "diverge", "short", "spark" };
        public static int PatternRankOf(string id) => id == null ? 99 : Array.IndexOf(PatternRank, id);

        void IdentifyPattern(Simulator sim, ConfirmResult result)
        {
            if (sim.stats.aborted || (stage < 1 && !sim.stats.infinite)) return;
            var st = sim.stats;
            string found = null;
            foreach (var id in PatternRank)
            {
                var def = cfg.patterns.FirstOrDefault(p => p.id == id);
                if (def == null || def.minStage > stage) continue;
                bool ok = id switch
                {
                    "infinite" => st.infinite,
                    "long" => st.maxBeat >= cfg.longChainBeats,
                    "converge" => st.convergeBurst,
                    "diverge" => st.diverge,
                    "short" => st.maxBeat >= cfg.shortChainBeats,
                    _ => true,
                };
                if (ok) { found = id; break; }
            }
            if (found == null) return;
            var pd = cfg.patterns.First(p => p.id == found);
            patternLevel.TryGetValue(found, out int lvl);
            float mult = pd.mult + lvl * cfg.levelMult;
            int plight = pd.light * (100 + lvl * cfg.levelLightPercent) / 100;
            if (new Mods(this).Has(Op.OnlyLastPatternDoubled))
            {
                if (found == lastPattern) { mult *= 2; plight *= 2; }
                else { mult = 0; plight = 0; }
            }
            if (cfg.insightEnabled && !stats.patterns.ContainsKey(found)) { insight++; insightLog.Add("首次识别" + pd.name + " +1 领悟"); }
            patternsThisRound.TryGetValue(found, out int ptr);
            patternsThisRound[found] = ptr + 1;
            if (stage >= cfg.freshnessFromStage)
            {
                freshK = found == lastPattern ? freshK + 1 : 0;
                mult *= new Mods(this).FreshnessFactor(freshK);
            }
            lastPattern = found;
            roundLight += plight; roundAddMult += mult;
            patternLight += plight; patternMult += mult;
            if (PatternRankOf(found) < PatternRankOf(roundTopPattern)) roundTopPattern = found;
            stats.patterns.TryGetValue(found, out int cnt);
            stats.patterns[found] = cnt + 1;
            st.pattern = found;
            st.lightGained += plight; st.multGained += mult;
            result.pattern = found;
            int beat = sim.events.Count > 0 ? sim.events[sim.events.Count - 1].beat : 0;
            sim.events.Insert(sim.events.Count - 1, new SimEvent { type = SimEventType.PatternRecognized, beat = beat, text = pd.name + (lvl > 0 ? " Lv" + (lvl + 1) : ""), light = plight, mult = mult, amount = freshK });
            sim.events.Insert(sim.events.Count - 1, new SimEvent { type = SimEventType.ScoreDelta, beat = beat, light = plight, mult = mult, text = "pattern" });
        }

        // ------------------------------------------------------------------ score

        public float SettleMultiplier()
        {
            float m = 1f;
            foreach (var e in new Mods(this).Of(Op.SettleMultPercent)) m *= e.value / 100f;
            return m;
        }

        public bool PatternVoided()
        {
            var g = CurrentGate;
            var mods = new Mods(this);
            if (g == null || !mods.Has(Op.PatternMustRepeat)) return false;
            return roundTopPattern != lastRoundTopPattern;
        }

        public int EstimateScore()
        {
            float light = roundLight, add = roundAddMult;
            if (PatternVoided()) { light -= patternLight; add -= patternMult; }
            return (int)Math.Floor(Math.Max(0, light) * (1 + Math.Max(0, add)) * SettleMultiplier() + 1e-3);
        }

        public int FiredSparksThisRound()
        {
            int n = 0;
            foreach (int nid in firedNodesThisRound) if (nodes.TryGetValue(nid, out var node)) n += node.sparks.Count;
            return n;
        }

        // ------------------------------------------------------------------ settlement

        public SettleReport Settle()
        {
            var rep = new SettleReport();
            if (phase == Phase.GameOver || phase == Phase.Victory) return rep;
            var mods = new Mods(this);
            rep.patternVoided = PatternVoided();
            int score = EstimateScore();
            int target = Target;
            bool pass = score >= target;
            foreach (var e in mods.Of(Op.MinFiresRequired))
                if (FiredSparksThisRound() < e.value) { pass = false; rep.log.Add($"关口：本轮只有 {FiredSparksThisRound()} 个灵光闪过（需要 {e.value}）"); }
            var rsc = new RoundScore { stage = stage, round = roundInStage, target = target, score = score, passed = pass, topPattern = roundTopPattern };
            history.Add(rsc);
            rep.score = rsc;
            stats.maxOvershoot = Math.Max(stats.maxOvershoot, target > 0 ? (float)score / target : 0);
            int gained = Math.Max(0, cfg.coinsPerRound + (score >= target * 2 ? cfg.coinsBonusOverTarget : 0) + mods.Sum(Op.CoinsAdd));
            coins += gained;
            rep.coinsGained = gained;
            lastRoundTopPattern = roundTopPattern;

            if (!pass)
            {
                phase = Phase.GameOver;
                rep.gameOver = true;
                return rep;
            }
            if (stage >= 1) separationUnlocked = true;
            if (cfg.insightEnabled) SettleInsight(rep, score, target);

            // ---- disuse (废退) and old-age pruning
            int N = cfg.decayRoundsByStage[Math.Min(stage, cfg.decayRoundsByStage.Length - 1)];
            foreach (var e in edges.Values)
            {
                if (e.conductedThisRound) { e.idleRounds = 0; continue; }
                if (stage == (int)Stage.Old && e.count == 0 && !e.pruned) { e.pruned = true; rep.prunedEdges++; continue; }
                e.idleRounds++;
                if (e.idleRounds >= N)
                {
                    if (e.count > 0) rep.decayedEdges++;
                    e.count = Math.Max(0, e.count - cfg.decayAmount);
                    e.idleRounds = 0;
                }
            }

            // ---- youth pruning after the last youth round
            if (stage == (int)Stage.Youth && roundInStage == 2)
            {
                int protect = mods.Sum(Op.PruneProtect);
                foreach (var e in edges.Values.OrderByDescending(x => x.from).ThenByDescending(x => x.to))
                {
                    if (e.count == 0)
                    {
                        if (protect > 0) { protect--; continue; }
                        if (!e.pruned) { e.pruned = true; rep.prunedEdges++; }
                    }
                    else if (e.count >= cfg.thickAt) e.count += cfg.youthPruneBonus;
                }
                rep.log.Add($"青春期修剪：剪除 {rep.prunedEdges} 条从未导通的光丝");
            }

            // ---- weak links fade (rule three)
            foreach (var k in weakLinks.Keys.ToList())
            {
                int v = weakLinks[k] - 1;
                if (v <= 0) weakLinks.Remove(k); else weakLinks[k] = v;
            }

            // ---- plasticity of memories and sedimented regions
            int lieFlat = mods.Sum(Op.MemoryPlasticityAtSettle);
            foreach (var n in nodes.Values.Where(x => x.isMemory))
            {
                n.plasticity = Math.Min(cfg.plasticityMax, n.plasticity + lieFlat);
                n.plasticity = Math.Max(0, n.plasticity - n.fireCountRound);
                if (n.triggeredThisRound)
                {
                    n.consecutiveTriggered++; n.consecutiveIdle = 0;
                    if (n.consecutiveTriggered >= 3) n.plasticity = 0;
                }
                else
                {
                    n.consecutiveIdle++; n.consecutiveTriggered = 0;
                    if (n.consecutiveIdle >= 2) { n.plasticity = Math.Min(cfg.plasticityMax, n.plasticity + 1); n.consecutiveIdle = 0; }
                }
            }
            foreach (var r in regions.Values.Where(x => x.sedimented))
            {
                if (r.triggeredThisRound)
                {
                    r.plasticity = Math.Max(0, r.plasticity - 1);
                    r.consecutiveTriggered++; r.consecutiveIdle = 0;
                    if (r.consecutiveTriggered >= 3) r.plasticity = 0;
                }
                else
                {
                    r.consecutiveIdle++; r.consecutiveTriggered = 0;
                    if (r.consecutiveIdle >= 2) { r.plasticity = Math.Min(cfg.plasticityMax, r.plasticity + 1); r.consecutiveIdle = 0; }
                }
            }

            // ---- no anchor (无依): player-placed sparks drift away
            foreach (var sp in sparks.Values.ToList())
            {
                if (!sp.placedByPlayer || sp.isCharacter || sp.shape == Shape.Instinct || nodes[sp.nodeId].isMemory) continue;
                if (RoundIndex - sp.placedRound < cfg.rootRounds) continue;
                if (IsAnchored(sp)) { sp.noAnchorStreak = 0; continue; }
                sp.noAnchorStreak++;
                if (sp.noAnchorStreak >= cfg.driftAfterRounds)
                {
                    rep.driftedCells.Add(sp.cell);
                    RemoveSpark(sp);
                }
            }
            if (rep.driftedCells.Count > 0) rep.log.Add($"{rep.driftedCells.Count} 个无依的灵光漂走了");

            // ---- characters (流光)
            if (cfg.charactersEnabled) CharacterUpkeep(rep);

            // ---- pickups expire
            foreach (var p in pickups) p.roundsLeft--;
            pickups.RemoveAll(p => p.roundsLeft <= 0);

            // ---- effects tick
            foreach (var ae in effects)
            {
                if (ae.delayRounds > 0) ae.delayRounds--;
                else if (ae.roundsLeft > 0) ae.roundsLeft--;
            }
            effects.RemoveAll(ae => ae.delayRounds <= 0 && ae.roundsLeft == 0);

            // ---- stage end
            if (roundInStage == 2)
            {
                rep.stageEnded = true;
                if (stage <= (int)Stage.Middle)
                {
                    rep.memoryCells = FoldMemory();
                    if (rep.memoryCells != null) rep.log.Add($"折叠出一段记忆（{rep.memoryCells.Count} 个灵光）");
                    var rs = RegionStateOf(HexBoard.StageRegion[stage]);
                    if (rs != null) { rs.sedimented = true; rs.plasticity = cfg.plasticityInit; }
                }
                if (stage >= (int)Stage.Old)
                {
                    phase = Phase.Victory;
                    rep.victory = true;
                    return rep;
                }
                stage++;
                roundInStage = 0;
                if (stage >= 1) separationUnlocked = true;
                rep.newStage = stage;
                RebuildEdges(); // bridges may open
            }
            else roundInStage++;

            phase = Phase.RoundSettled;
            return rep;
        }

        public List<string> insightLog = new List<string>();

        void SettleInsight(SettleReport rep, int score, int target)
        {
            int before = insight;
            // 精进: unspent points from the previous round go to this round's most frequent pattern
            if (insight > 0 && patternsThisRound.Count > 0)
            {
                var top = patternsThisRound.OrderByDescending(kv => kv.Value).ThenBy(kv => PatternRankOf(kv.Key)).First().Key;
                patternProgress.TryGetValue(top, out int prog);
                prog += insight;
                insight = 0;
                patternLevel.TryGetValue(top, out int lvl);
                while (prog >= cfg.insightPerLevel && lvl < cfg.patternLevelMax) { prog -= cfg.insightPerLevel; lvl++; rep.log.Add($"精进：{cfg.patterns.First(p => p.id == top).name} 升到 Lv{lvl + 1}"); }
                patternLevel[top] = lvl;
                patternProgress[top] = prog;
                before = 0;
            }
            // interest
            int interest = Math.Min(cfg.interestMax, coins / Math.Max(1, cfg.interestPer));
            insight += interest;
            // experience
            if (IsBossRound && score >= target * 2) { insight++; insightLog.Add("关口超出 100% +1 领悟"); }
            foreach (var n in nodes.Values.Where(x => x.isMemory && x.plasticity == 0))
                if (engravedMemories.Add(n.id)) { insight += 2; insightLog.Add(n.title + " 刻骨 +2 领悟"); }
            rep.insightGained = insight - before;
            foreach (var l in insightLog) rep.log.Add(l);
            insightLog.Clear();
            if (interest > 0) rep.log.Add($"利息 +{interest} 领悟");
        }

        public bool BuyReview()
        {
            if (phase != Phase.Prepare) return Fail("现在不能复盘");
            if (reviewActive) return Fail("本轮已复盘");
            if (insight < cfg.reviewCost) return Fail("领悟不足");
            insight -= cfg.reviewCost;
            reviewActive = true;
            return true;
        }

        public bool RerollCandidates()
        {
            if (phase != Phase.Prepare || placedThisRound || candidates.Count == 0) return Fail("现在不能重抽候选");
            if (insight < cfg.rerollCost) return Fail("领悟不足");
            insight -= cfg.rerollCost;
            var pool = new List<Shape> { Shape.Conduct };
            if (stage >= (int)Stage.Child) pool.Add(Shape.Cone);
            if (stage >= (int)Stage.Youth) pool.Add(Shape.Converge);
            for (int i = 0; i < candidates.Count; i++) candidates[i] = pool[rng.Next(pool.Count)];
            return true;
        }

        /// <summary>Called by the presenter after the settlement screen.</summary>
        public void NextRound()
        {
            if (phase != Phase.RoundSettled) return;
            StartRound();
        }

        // ------------------------------------------------------------------ memory

        public List<int> FoldMemory()
        {
            Region region = HexBoard.StageRegion[stage];
            var members = sparks.Values.Where(sp => board.regionOf[sp.cell] == region && !sp.isCharacter
                                                   && !nodes[sp.nodeId].isMemory && !disabledCells.Contains(sp.cell)).Select(sp => sp.id).ToHashSet();
            if (members.Count < cfg.memoryMin) return null;
            var adj = new Dictionary<int, Dictionary<int, int>>();
            foreach (int id in members) adj[id] = new Dictionary<int, int>();
            foreach (var e in edges.Values)
            {
                if (e.pruned || !members.Contains(e.from) || !members.Contains(e.to)) continue;
                adj[e.from].TryGetValue(e.to, out int w1); adj[e.from][e.to] = w1 + e.count;
                adj[e.to].TryGetValue(e.from, out int w2); adj[e.to][e.from] = w2 + e.count;
            }
            var seen = new HashSet<int>();
            List<int> best = null; int bestScore = -1;
            foreach (int start in members.OrderBy(x => x))
            {
                if (seen.Contains(start)) continue;
                var comp = new List<int>();
                var q = new Queue<int>(); q.Enqueue(start); seen.Add(start);
                while (q.Count > 0)
                {
                    int a = q.Dequeue(); comp.Add(a);
                    foreach (int b in adj[a].Keys) if (seen.Add(b)) q.Enqueue(b);
                }
                if (comp.Count < cfg.memoryMin) continue;
                List<int> group = comp.Count <= cfg.memoryMax ? comp : GrowSubset(comp, adj);
                int score = InternalWeight(group, adj);
                if (score > bestScore) { bestScore = score; best = group; }
            }
            if (best == null) return null;

            CreateMemory(best, region);
            return best.Select(id => sparks[id].cell).ToList();
        }

        public Node CreateMemory(List<int> sparkIds, Region region)
        {
            var mem = new Node { id = nextId++, isMemory = true, plasticity = cfg.plasticityInit, region = region, title = "记忆 " + (++memoryCounter) };
            foreach (int spId in sparkIds.OrderBy(x => x))
            {
                var sp = sparks[spId];
                int oldNode = sp.nodeId;
                nodes.Remove(oldNode);
                foreach (var k in weakLinks.Keys.ToList())
                {
                    SplitPair(k, out int a, out int b);
                    if (a == oldNode || b == oldNode) weakLinks.Remove(k);
                }
                sp.nodeId = mem.id;
                mem.sparks.Add(spId);
            }
            nodes[mem.id] = mem;
            return mem;
        }

        List<int> GrowSubset(List<int> comp, Dictionary<int, Dictionary<int, int>> adj)
        {
            int bestA = -1, bestB = -1, bestW = -1;
            foreach (int a in comp) foreach (var kv in adj[a]) if (kv.Value > bestW || (kv.Value == bestW && a < bestA)) { bestW = kv.Value; bestA = a; bestB = kv.Key; }
            var set = new List<int> { bestA, bestB };
            while (set.Count < cfg.memoryMax)
            {
                int pick = -1, pickW = -1;
                foreach (int a in set) foreach (var kv in adj[a])
                    {
                        if (set.Contains(kv.Key)) continue;
                        int w = 0;
                        foreach (int m in set) if (adj[kv.Key].TryGetValue(m, out int ww)) w += ww;
                        if (w > pickW || (w == pickW && kv.Key < pick)) { pickW = w; pick = kv.Key; }
                    }
                if (pick < 0) break;
                set.Add(pick);
            }
            return set;
        }

        static int InternalWeight(List<int> group, Dictionary<int, Dictionary<int, int>> adj)
        {
            int w = 0;
            foreach (int a in group) foreach (var kv in adj[a]) if (group.Contains(kv.Key) && a < kv.Key) w += kv.Value;
            return w;
        }

        // ------------------------------------------------------------------ characters (流光, P1)

        public static readonly string[] PersonalityNames = { "随和", "热烈", "沉稳", "高冷", "敏感" };
        static readonly int[][] PersonalityDirs = { new[] { 0, 2, 4 }, new[] { 0, 1, 2, 3, 4, 5 }, new[] { 0 }, new[] { 0 }, new[] { 0, 1, 2, 3, 4, 5 } };
        static readonly int[] PersonalityThr = { 2, 4, 4, 6, 2 };
        static readonly int[] PersonalityOut = { 2, 4, 4, 6, 2 };
        static readonly int[] PersonalityStay = { 4, 2, 6, 4, 2 };
        static readonly int[] PersonalityLight = { 8, 12, 10, 16, 6 };

        void CharacterUpkeep(SettleReport rep)
        {
            var mine = sparks.Values.Where(sp => !sp.isCharacter).ToList();
            bool anyFamiliar = false;
            foreach (var ch in sparks.Values.Where(sp => sp.isCharacter).ToList())
            {
                int rel = CharacterRelation(ch);
                if (rel >= cfg.characterFamiliarAt && ch.maxRelation < cfg.characterFamiliarAt)
                {
                    stats.familiarEver++;
                    if (cfg.insightEnabled && stats.familiarEver == 1) { insight++; rep.log.Add("第一次与流光熟悉 +1 领悟"); }
                }
                if (rel >= cfg.characterKnownAt && ch.maxRelation < cfg.characterKnownAt) stats.knownCount++;
                ch.maxRelation = Math.Max(ch.maxRelation, rel);
                if (rel >= cfg.characterFamiliarAt)
                {
                    ch.familiarRounds++; anyFamiliar = true;
                    if (ch.spawnStage <= (int)Stage.Child && stage >= (int)Stage.Old) stats.longRelation = true;
                    continue;
                }
                ch.stayRounds--;
                if (ch.stayRounds <= 0)
                {
                    if (ch.maxRelation >= cfg.characterKnownAt) stats.broken++;
                    rep.log.Add("一道流光离开了");
                    RemoveSpark(ch);
                    continue;
                }
                if (CharacterRelation(ch) == 0) DriftCharacter(ch, mine, rep);
            }
            if (anyFamiliar) stats.roundsWithFamiliar++;
            // spawn
            if (stage < 1) return;
            int count = sparks.Values.Count(sp => sp.isCharacter);
            int cap = Math.Min(cfg.characterHardCap, cfg.characterTargetByStage[Math.Min(stage, 5)]);
            if (count >= cap || !rng.Chance(cfg.characterSpawnPermilleByStage[Math.Min(stage, 5)])) return;
            var cells = board.AllCells().Where(c => CanPlaceAt(c) && mine.Any(m => board.HexDistance(m.cell, c) <= 2)).ToList();
            if (cells.Count == 0) return;
            int cell = cells[rng.Next(cells.Count)];
            int pers = stage == 1 ? (rng.Next(2) == 0 ? 0 : 4) : rng.Next(5);
            var nearest = mine.OrderBy(m => board.HexDistance(m.cell, cell)).ThenBy(m => m.id).First();
            int dir = 0, bestD = 99;
            for (int d = 0; d < 6; d++)
            {
                int nb = board.Neighbor(cell, d);
                if (nb < 0) continue;
                int dist = board.HexDistance(nb, nearest.cell);
                if (dist < bestD) { bestD = dist; dir = d; }
            }
            AddCharacter(cell, pers, dir);
            stats.charactersMet++;
            rep.log.Add($"一道{PersonalityNames[pers]}的流光出现了");
        }

        public Spark AddCharacter(int cell, int pers, int dir)
        {
            var spk = AddSpark(Shape.Conduct, cell, dir, false);
            spk.isCharacter = true;
            spk.personality = pers;
            spk.dirsOverride = PersonalityDirs[pers];
            spk.thrOverride = PersonalityThr[pers];
            spk.outOverride = PersonalityOut[pers];
            spk.light = PersonalityLight[pers];
            spk.stayRounds = PersonalityStay[pers];
            spk.spawnStage = stage;
            RebuildEdges();
            return spk;
        }

        void DriftCharacter(Spark ch, List<Spark> mine, SettleReport rep)
        {
            if (mine.Count == 0) return;
            int Dist(int cell) => mine.Min(m => board.HexDistance(m.cell, cell));
            int cur = Dist(ch.cell);
            int bestCell = -1, bestDist = cur;
            bool offBoard = false;
            for (int d = 0; d < 6; d++)
            {
                int nb = board.Neighbor(ch.cell, d);
                if (nb < 0) { offBoard = true; continue; }
                if (!CanPlaceAt(nb)) continue;
                int dd = Dist(nb);
                if (dd > bestDist) { bestDist = dd; bestCell = nb; }
            }
            if (bestCell >= 0)
            {
                RemoveEdgesOf(ch.id, true, true);
                cellSpark[ch.cell] = -1;
                ch.cell = bestCell;
                cellSpark[bestCell] = ch.id;
                RebuildEdges();
            }
            else if (offBoard)
            {
                rep.log.Add("一道流光漂出了棋盘");
                RemoveSpark(ch);
            }
        }
    }
}
