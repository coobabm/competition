using System;
using System.Collections.Generic;

namespace LingGuang.Core
{
    /// <summary>Snapshot of every active effect (held items, used drugs, delayed/permanent effects, current gate).</summary>
    public sealed class Mods
    {
        readonly RunState s;
        readonly Dictionary<Op, List<Effect>> byOp = new Dictionary<Op, List<Effect>>();

        public Mods(RunState s)
        {
            this.s = s;
            foreach (var id in s.items)
            {
                var d = EffectDef.Get(id);
                if (d != null) foreach (var e in d.now) AddE(e);
            }
            foreach (var ae in s.effects)
                if (ae.delayRounds <= 0) AddE(ae.effect);
            var gate = s.CurrentGate;
            if (gate != null && !Has(Op.GateDisable))
                foreach (var e in gate.now) AddE(e);
        }

        void AddE(Effect e)
        {
            if (!byOp.TryGetValue(e.op, out var l)) byOp[e.op] = l = new List<Effect>();
            l.Add(e);
        }

        public bool Has(Op op) => byOp.TryGetValue(op, out var l) && l.Count > 0;
        public List<Effect> Of(Op op) => byOp.TryGetValue(op, out var l) ? l : Empty;
        static readonly List<Effect> Empty = new List<Effect>();
        public int Sum(Op op) { int v = 0; foreach (var e in Of(op)) v += e.value; return v; }

        bool RegionMatch(Effect e, Region r)
        {
            if (e.region == Region.None) return true;
            return e.invertRegion ? r != e.region : r == e.region;
        }

        bool ShapeMatch(Effect e, Shape? shape) => e.shape < 0 || (shape.HasValue && (int)shape.Value == e.shape);

        public int BaseThreshold(Node n)
        {
            var cfg = s.cfg;
            bool memOrInst;
            int baseT;
            Region region;
            Shape? shape = null;
            if (n.isMemory) { baseT = cfg.memoryThreshold; memOrInst = true; region = n.region; }
            else
            {
                var sp = s.sparks[n.sparks[0]];
                shape = sp.shape;
                baseT = sp.thrOverride > 0 ? sp.thrOverride : cfg.Shape(sp.shape).threshold;
                memOrInst = sp.shape == Shape.Instinct;
                region = s.board.regionOf[sp.cell];
            }
            foreach (var e in Of(Op.ThresholdSet))
                if (!e.memoryOrInstinct || memOrInst) baseT = e.value;
            int add = 0;
            bool immune = Has(Op.NegativeImmune);
            foreach (var e in Of(Op.ThresholdAdd))
            {
                if (!RegionMatch(e, region) || !ShapeMatch(e, shape)) continue;
                if (e.memoryOrInstinct && !memOrInst) continue;
                if (immune && e.value > 0) continue;
                add += e.value;
            }
            if (s.earphoneRegion != Region.None && region == s.earphoneRegion)
                add += Sum(Op.DesignatedRegionThresholdAdd);
            int t = baseT + add;
            if (add < 0) t = Math.Max(t, Math.Min(baseT, 2));
            return Math.Max(1, t);
        }

        public int Threshold(Node n) => BaseThreshold(n) + n.thresholdRaise;

        public int Output(Spark sp)
        {
            int baseO = sp.outOverride > 0 ? sp.outOverride : s.cfg.Shape(sp.shape).output;
            int add = Sum(Op.OutputAdd);
            if (add < 0 && Has(Op.NegativeImmune)) add = Math.Max(0, add);
            int o = baseO + add;
            if (add < 0) o = Math.Max(o, Math.Min(baseO, 2));
            return o;
        }

        public int EdgeLevel(Edge e)
        {
            if (e.count >= s.cfg.myelinAt) return 2;
            if (e.count >= s.cfg.thickAt) return 1;
            return 0;
        }

        public bool BridgePenalty(Edge e)
        {
            if (Has(Op.NoBridgePenalty)) return false;
            var a = s.sparks[e.from]; var b = s.sparks[e.to];
            return s.board.CrossesMidline(a.cell, b.cell) && e.count < s.cfg.bridgeGraduateAt;
        }

        public int Delay(Edge e)
        {
            var cfg = s.cfg;
            var src = s.sparks[e.from];
            int lvl = EdgeLevel(e);
            int d = lvl == 2 ? 0 : cfg.baseDelay;
            if (BridgePenalty(e)) d += cfg.bridgePenaltyDelay;
            foreach (var ef in Of(Op.DelayAdd))
                if (ShapeMatch(ef, src.shape)) d += ef.value;
            if (s.stage == (int)Stage.Old && lvl != 2 && !Has(Op.CancelOldDelay) && !s.nodes[src.nodeId].isMemory) d += 1;
            return Math.Max(0, d);
        }

        public int DeliveryAmount(Edge e, bool fromStart)
        {
            var cfg = s.cfg;
            var src = s.sparks[e.from];
            var dst = s.sparks[e.to];
            int amt = Output(src) + (fromStart ? cfg.startOutputBonus : 0);
            if (EdgeLevel(e) >= 1) amt += cfg.levelOutputBonus;
            if (BridgePenalty(e)) amt -= cfg.bridgePenaltyAmount;
            amt += e.stp;
            Region ra = s.board.regionOf[src.cell], rb = s.board.regionOf[dst.cell];
            if (ra != rb)
            {
                amt += Sum(Op.CrossRegionDeliveryAdd);
                foreach (var ef in Of(Op.RegionPairDeliveryAdd))
                    if ((ra == ef.region && rb == ef.region2) || (ra == ef.region2 && rb == ef.region)) amt += ef.value;
            }
            return Math.Max(0, amt);
        }

        public int RippleRadiusBase => s.cfg.rippleBaseRadius + Sum(Op.RippleRadiusAdd);

        public int Starts()
        {
            int st = s.cfg.startsPerRound + Sum(Op.StartsAdd);
            foreach (var e in Of(Op.StartsSet)) st = e.value;
            return Math.Max(1, st);
        }

        public int ConductIncrement(Edge e)
        {
            int inc = 1 + Sum(Op.ConductCountExtra);
            if (Has(Op.ConductCountBonusOldRegion))
            {
                var src = s.sparks[e.from];
                int unlock = RunState.RegionStage(s.board.regionOf[src.cell]);
                if (s.stage - unlock > 1) inc += Sum(Op.ConductCountBonusOldRegion);
            }
            return inc;
        }

        public int FireLight(Spark sp, Node n, bool firstFireOfRound, int convergeSources)
        {
            var cfg = s.cfg;
            float light = sp.light;
            if (sp.shape == Shape.Converge && !sp.isCharacter && convergeSources >= cfg.convergeMinSources)
                light += convergeSources * (cfg.convergeBonusPerSource + Sum(Op.ConvergeBonusAdd));
            Shape? sh = sp.isCharacter ? (Shape?)null : sp.shape;
            foreach (var e in Of(Op.LightAddOnFire)) if (ShapeMatch(e, sh)) light += e.value;
            if (n.isMemory) light *= 1f + Sum(Op.MemoryLightPercent) / 100f;
            if (firstFireOfRound) foreach (var e in Of(Op.LightPercentFirstFire)) light *= e.value / 100f;
            int pct = Sum(Op.LightPercentAll);
            if (pct < 0 && Has(Op.NegativeImmune)) pct = 0;
            light *= 1f + pct / 100f;
            return Math.Max(0, (int)Math.Floor(light + 1e-4f));
        }

        public float FireAddMult(Spark sp)
        {
            float m = 0;
            Shape? sh = sp.isCharacter ? (Shape?)null : sp.shape;
            foreach (var e in Of(Op.AddMultOnFire)) if (ShapeMatch(e, sh)) m += e.value;
            return m;
        }

        public float FreshnessFactor(int k)
        {
            var cfg = s.cfg;
            float step = cfg.freshnessStep + Sum(Op.FreshnessPenaltyPercent) / 100f;
            int steps = k + (k > 0 ? Sum(Op.FreshnessStepsAdd) + Sum(Op.FreshnessStepsUntilStageEnd) : 0);
            return Math.Max(cfg.freshnessMin, 1f - step * steps);
        }
    }
}
