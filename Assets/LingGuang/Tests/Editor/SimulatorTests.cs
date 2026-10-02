using System.Collections.Generic;
using System.Linq;
using LingGuang.Core;
using NUnit.Framework;

namespace LingGuang.Tests
{
    /// <summary>Acceptance cases T01–T22 from spec §11 (Simulator only, no presentation).</summary>
    public class SimulatorTests
    {
        static GameConfig Cfg()
        {
            var c = new GameConfig { charactersEnabled = false };
            return c;
        }

        static RunState Sandbox(int stage = 4)
        {
            var s = RunState.CreateSandbox(Cfg(), stage);
            s.firstConfirmDone = true; // no lighthouse unless a test wants it
            return s;
        }

        static Spark Put(RunState s, Shape shape, int c, int r, int dir) => s.AddSpark(shape, c, r, dir);

        static List<SimEvent> Fires(Simulator sim, Spark sp) => sim.events.Where(e => e.type == SimEventType.Fire && e.sparkId == sp.id).ToList();

        static Simulator Run(RunState s, Spark start, bool lighthouse = false) => Simulator.Run(s, start.id, lighthouse);

        // ----------------------------------------------------------------

        [Test]
        public void T01_ConductChain()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 1);
            var c = Put(s, Shape.Conduct, 4, 4, 1);
            var sim = Run(s, a);
            Assert.AreEqual(0, Fires(sim, a).Single().beat);
            Assert.AreEqual(1, Fires(sim, b).Single().beat);
            Assert.AreEqual(2, Fires(sim, c).Single().beat);
        }

        // Geometry used by T02/T18: S(3,4) instinct, A1(4,4)->X, A2(3,5)->X, X(4,5) cone. X is 2 away from S.
        static (RunState s, Spark S, Spark A1, Spark A2, Spark X) ConeRig(bool withA2)
        {
            var s = Sandbox();
            s.cfg.rippleCharge = 0; // isolate edge timing from ripples
            var S = Put(s, Shape.Instinct, 3, 4, 0);
            var A1 = Put(s, Shape.Conduct, 4, 4, 2);
            Spark A2 = withA2 ? Put(s, Shape.Conduct, 3, 5, 1) : null;
            var X = Put(s, Shape.Cone, 4, 5, 1);
            Assert.IsNotNull(s.EdgeBetween(A1, X));
            if (withA2) Assert.IsNotNull(s.EdgeBetween(A2, X));
            Assert.IsNull(s.EdgeBetween(S, X));
            return (s, S, A1, A2, X);
        }

        [Test]
        public void T02_ConeNeedsTwoEdgesSameBeat()
        {
            var r1 = ConeRig(false);
            var sim1 = Run(r1.s, r1.S);
            Assert.AreEqual(0, Fires(sim1, r1.X).Count, "single new edge (2) must not fire a cone (4)");

            var r2 = ConeRig(true);
            var sim2 = Run(r2.s, r2.S);
            var f = Fires(sim2, r2.X);
            Assert.AreEqual(1, f.Count);
            Assert.AreEqual(2, f[0].beat);
        }

        [Test]
        public void T03_ConvergeThreeSources()
        {
            var s = RunState.CreateSandbox(Cfg(), 1);
            var start = Put(s, Shape.Instinct, 4, 4, 0);
            Put(s, Shape.Instinct, 4, 6, 0);
            Put(s, Shape.Instinct, 3, 5, 0);
            var x = Put(s, Shape.Converge, 4, 5, 0);
            var sim = Simulator.Run(s, start.id, true); // lighthouse fires the two near the anchors at beat 0
            var f = Fires(sim, x);
            Assert.IsTrue(f.Count >= 1);
            Assert.AreEqual(1, f[0].beat);
            Assert.AreEqual(20 + 30, f[0].light);
            Assert.IsTrue(sim.stats.convergeBurst);
        }

        [Test]
        public void T04_RefireNeedsRaisedThreshold_NotTwiceInOneBeat()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 4);
            var sim = Run(s, a);
            Assert.AreEqual(1, Fires(sim, a).Count, "A gets 2 back but its threshold is now 4");
            Assert.AreEqual(2, s.NodeOf(a).charge);

            // myelinated loop: all inside beat 0, A may not fire twice in the same beat
            var s2 = Sandbox();
            var a2 = Put(s2, Shape.Conduct, 2, 4, 1);
            var b2 = Put(s2, Shape.Conduct, 3, 4, 4);
            s2.EdgeBetween(a2, b2).count = 6;
            s2.EdgeBetween(b2, a2).count = 6;
            var sim2 = Run(s2, a2);
            var fa = Fires(sim2, a2);
            Assert.AreEqual(1, fa.Count(e => e.beat == 0));
            Assert.AreEqual(1, Fires(sim2, b2).Count(e => e.beat == 0));
        }

        [Test]
        public void T05_ChargeKeptBetweenConfirms()
        {
            var s = Sandbox();
            s.cfg.rippleCharge = 0; // isolate edge timing from ripples
            var st = Put(s, Shape.Conduct, 2, 4, 1);
            var a = Put(s, Shape.Conduct, 3, 4, 1);
            var cone = Put(s, Shape.Cone, 4, 4, 1); // outputs 1,3,5: (5,4) gully, (3,5), (3,3) empty
            var sim1 = Run(s, st);
            Assert.AreEqual(0, Fires(sim1, cone).Count);
            Assert.AreEqual(2, s.NodeOf(cone).charge, "no leak after the last beat, no leak between confirms");
            var sim2 = Run(s, st);
            Assert.AreEqual(1, Fires(sim2, cone).Count);
        }

        [Test]
        public void T06_MyelinZeroDelay()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 1);
            var e = s.EdgeBetween(a, b);
            e.count = 6;
            var mods = new Mods(s);
            Assert.AreEqual(0, mods.Delay(e));
            var sim = Run(s, a);
            Assert.AreEqual(0, Fires(sim, b).Single().beat);

        }

        [Test]
        public void T07_RotateResetsOutgoingCounts()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 1);
            s.EdgeBetween(a, b).count = 5;
            Assert.IsTrue(s.Rotate(a.id, 1));
            Assert.IsNull(s.EdgeBetween(a, b));
            Assert.IsTrue(s.Rotate(a.id, -1));
            Assert.AreEqual(0, s.EdgeBetween(a, b).count);
        }

        [Test]
        public void T08_NoEdgeAcrossGully()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 4, 4, 1);  // (4,4) -> (5,4) crosses the midline (row 4 has no bridge)
            var b = Put(s, Shape.Conduct, 5, 4, 1);
            Assert.IsNull(s.EdgeBetween(a, b));
        }

        [Test]
        public void T09_BridgePenalty()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 4, 5, 1);  // row 5 bridge
            var b = Put(s, Shape.Conduct, 5, 5, 1);
            var e = s.EdgeBetween(a, b);
            Assert.IsNotNull(e);
            var m = new Mods(s);
            Assert.AreEqual(2, m.Delay(e));
            Assert.AreEqual(1, m.DeliveryAmount(e, false));
            e.count = 2;
            Assert.AreEqual(2, m.Delay(e));
            e.count = 3;
            Assert.AreEqual(1, m.Delay(e));
            Assert.AreEqual(2 + 2, m.DeliveryAmount(e, false));
        }

        [Test]
        public void T10_RippleRadiusOneNoGully()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 3, 4, 1);
            var b = Put(s, Shape.Conduct, 4, 4, 2);   // fires at beat 1 by conduct; output -> (4,5) empty
            var c = Put(s, Shape.Conduct, 4, 3, 0);   // neighbour of B (upper-right of (4,4)), points away
            var d = Put(s, Shape.Conduct, 5, 4, 1);   // neighbour across the midline gully
            var sim = Run(s, a);
            Assert.AreEqual(1, Fires(sim, b).Single().beat);
            var applied = sim.events.Where(e => e.type == SimEventType.RippleApply).ToList();
            Assert.IsTrue(applied.Any(e => e.cellB == c.cell && e.beat == 2 && e.amount == 1));
            Assert.IsFalse(applied.Any(e => e.cellB == d.cell));
        }

        [Test]
        public void T11_RippleMerge()
        {
            var s = Sandbox();
            var S = Put(s, Shape.Instinct, 3, 4, 0);
            Put(s, Shape.Conduct, 4, 4, 2);
            Put(s, Shape.Conduct, 2, 4, 3);
            var sim = Run(s, S);
            var merges = sim.events.Where(e => e.type == SimEventType.RippleMerge && e.beat == 1).ToList();
            Assert.AreEqual(1, merges.Count);
            Assert.AreEqual(2, merges[0].amount);
            Assert.AreEqual(1, merges[0].radius);

            var s2 = Sandbox();
            var S2 = Put(s2, Shape.Instinct, 3, 4, 0);
            Put(s2, Shape.Conduct, 4, 4, 2);
            Put(s2, Shape.Conduct, 2, 4, 3);
            Put(s2, Shape.Conduct, 3, 3, 5);
            var far = Put(s2, Shape.Conduct, 5, 4, 1);   // across the gully, distance 1 from (4,4)
            var sim2 = Run(s2, S2);
            var m2 = sim2.events.Where(e => e.type == SimEventType.RippleMerge && e.beat == 1).ToList();
            Assert.AreEqual(1, m2.Count);
            Assert.AreEqual(3, m2[0].amount);
            Assert.AreEqual(2, m2[0].radius);
            Assert.IsTrue(m2[0].cells.Contains(far.cell));
            Assert.IsTrue(sim2.events.Any(e => e.type == SimEventType.RippleApply && e.cellB == far.cell));
        }

        [Test]
        public void T12_AccumulateUntilStartsRunOut()
        {
            var s = Sandbox(1);
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            Put(s, Shape.Conduct, 3, 4, 1);
            s.startsLeft = 2;
            s.Confirm(a.id);
            int light1 = s.roundLight;
            s.Confirm(a.id);
            Assert.Greater(s.roundLight, light1);
            Assert.AreEqual(0, s.history.Count, "nothing multiplied between confirms");
            Assert.AreEqual(0, s.startsLeft);
            int expected = (int)System.Math.Floor(s.roundLight * (1 + s.roundAddMult));
            Assert.AreEqual(expected, s.EstimateScore());
        }

        [Test]
        public void T13_LongChainOutranksDiverge()
        {
            var s = Sandbox(4);
            var cone = Put(s, Shape.Cone, 3, 1, 1);      // outputs: (4,1) right, (3,2) lower-left, (3,0) upper-left
            var a = Put(s, Shape.Conduct, 4, 1, 1);
            Put(s, Shape.Conduct, 3, 2, 1);
            Put(s, Shape.Conduct, 3, 0, 1);
            var b = Put(s, Shape.Conduct, 5, 1, 1);      // across the row-1 bridge
            var c = Put(s, Shape.Conduct, 6, 1, 1);
            Put(s, Shape.Conduct, 7, 1, 1);
            s.EdgeBetween(a, b).count = 3;               // graduated bridge (thick)
            var res = s.Confirm(cone.id);
            Assert.IsTrue(res.stats.diverge);
            Assert.GreaterOrEqual(res.stats.maxBeat, 4);
            Assert.AreEqual("long", res.pattern);
        }

        [Test]
        public void T14_YouthPruneAndRestore()
        {
            var s = Sandbox(2);
            s.roundInStage = 2;
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 1);
            s.roundLight = 100000;
            var rep = s.Settle();
            Assert.IsFalse(rep.gameOver);
            Assert.IsTrue(s.EdgeBetween(a, b).pruned);
            s.NextRound();
            s.firstConfirmDone = true;
            var sim = Run(s, a);
            Assert.AreEqual(0, Fires(sim, b).Count);
            Assert.IsTrue(s.Rotate(a.id, 1));
            Assert.IsTrue(s.Rotate(a.id, -1));
            Assert.IsFalse(s.EdgeBetween(a, b).pruned);
            var sim2 = Run(s, a);
            Assert.AreEqual(1, Fires(sim2, b).Count);
        }

        [Test]
        public void T15_MemoryFiresTogether()
        {
            var s = Sandbox();
            var st = Put(s, Shape.Conduct, 2, 4, 1);
            var m1 = Put(s, Shape.Conduct, 3, 4, 1);
            var m2 = Put(s, Shape.Cone, 3, 3, 0);
            var m3 = Put(s, Shape.Conduct, 4, 3, 0);
            var mem = s.CreateMemory(new List<int> { m1.id, m2.id, m3.id }, Region.K);
            var sim = Run(s, st);
            var fires = sim.events.Where(e => e.type == SimEventType.Fire && e.nodeId == mem.id).ToList();
            Assert.AreEqual(3, fires.Count(e => e.beat == 1));
            Assert.AreEqual(m1.light + m2.light + m3.light, fires.Where(e => e.beat == 1).Sum(e => e.light));
        }

        [Test]
        public void T16_ImitationGateVoidsPattern()
        {
            var s = Sandbox(2);
            s.roundInStage = 2;
            s.gateByStage[2] = "imitation";
            s.lastRoundTopPattern = "long";
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            Put(s, Shape.Conduct, 3, 4, 1);
            s.Confirm(a.id);
            Assert.AreEqual("spark", s.roundTopPattern);
            Assert.IsTrue(s.PatternVoided());
            int expected = (int)System.Math.Floor((s.roundLight - s.patternLight) * (1 + s.roundAddMult - s.patternMult));
            Assert.AreEqual(expected, s.EstimateScore());
        }

        [Test]
        public void T17_Deterministic()
        {
            string first = null;
            for (int i = 0; i < 100; i++)
            {
                var s = RunState.NewRun(Cfg(), 12345);
                var inst = s.sparks.Values.First(x => x.shape == Shape.Instinct);
                var res = s.Confirm(inst.id);
                string log = string.Join("\n", res.events.Select(e => e.ToString()));
                if (first == null) first = log; else Assert.AreEqual(first, log);
            }
        }

        [Test]
        public void T18_ConeLeakBetweenBeats()
        {
            // A1 reaches the cone one beat earlier through a myelinated S->A1 edge
            var r = ConeRig(true);
            r.s.EdgeBetween(r.S, r.A1).count = 6;
            var sim = Run(r.s, r.S);
            Assert.AreEqual(0, Fires(sim, r.X).Count, "2 -> leak 1 -> +2 = 3 < 4");
            Assert.AreEqual(3, r.s.NodeOf(r.X).charge);

            var r2 = ConeRig(true);
            var sim2 = Run(r2.s, r2.S);
            Assert.AreEqual(1, Fires(sim2, r2.X).Count, "same beat: 2 + 2 = 4 fires");
        }

        [Test]
        public void T19_LoopReverbTerminates()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 4);
            s.EdgeBetween(a, b).count = 3;
            s.EdgeBetween(b, a).count = 3;
            var sim = Run(s, a);
            Assert.IsTrue(sim.stats.reverb);
            Assert.GreaterOrEqual(Fires(sim, a).Count, 2);
            Assert.IsFalse(sim.stats.aborted);
            Assert.Less(sim.stats.maxBeat, s.cfg.maxBeats);
        }

        [Test]
        public void T20_DisturbanceSTP()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 1);
            var sim1 = Run(s, a);
            int amt1 = sim1.events.First(e => e.type == SimEventType.Deliver && e.cellB == b.cell).amount;
            var sim2 = Run(s, a);
            int amt2 = sim2.events.First(e => e.type == SimEventType.Deliver && e.cellB == b.cell).amount;
            Assert.AreEqual(amt1 + 1, amt2);
            s.roundLight = 100000;
            s.Settle();
            s.NextRound();
            Assert.AreEqual(0, s.EdgeBetween(a, b).stp);
        }

        [Test]
        public void T21_NoAnchorDrift()
        {
            var s = Sandbox(4);
            var lone = s.AddSpark(Shape.Conduct, s.board.Index(2, 4), 3, true);
            lone.placedRound = -10;
            var inst = Put(s, Shape.Instinct, 6, 4, 0);
            for (int i = 0; i < 2; i++)
            {
                s.roundLight = 1000000;
                var rep = s.Settle();
                Assert.IsFalse(rep.gameOver);
                s.NextRound();
            }
            Assert.IsFalse(s.sparks.ContainsKey(lone.id), "drifted away");
            Assert.IsTrue(s.sparks.ContainsKey(inst.id), "instinct is exempt");
        }

        [Test]
        public void T22_CharacterDriftsOutward()
        {
            var cfg = Cfg();
            cfg.charactersEnabled = true;
            var s = RunState.CreateSandbox(cfg, 4);
            var mine = Put(s, Shape.Conduct, 3, 4, 3);
            var ch = s.AddCharacter(s.board.Index(5, 2), 2, 0);
            int d0 = s.board.HexDistance(mine.cell, ch.cell);
            s.roundLight = 1000000;
            s.Settle();
            if (s.sparks.ContainsKey(ch.id))
                Assert.Greater(s.board.HexDistance(mine.cell, s.sparks[ch.id].cell), d0);

            // familiar characters stay put
            var s2 = RunState.CreateSandbox(cfg, 4);
            var a = Put(s2, Shape.Conduct, 2, 4, 1);
            var ch2 = s2.AddCharacter(s2.board.Index(3, 4), 2, 4);
            s2.EdgeBetween(a, ch2).count = 3;
            int cell0 = ch2.cell;
            s2.roundLight = 1000000;
            s2.Settle();
            Assert.IsTrue(s2.sparks.ContainsKey(ch2.id));
            Assert.AreEqual(cell0, s2.sparks[ch2.id].cell);
        }

        [Test]
        public void Board_ConnectedWithRandomGullies()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var s = RunState.NewRun(Cfg(), seed);
                Assert.IsTrue(s.board.IsConnected(99), "seed " + seed);
                Assert.IsFalse(s.board.GenerationFailed, "seed " + seed);
                Assert.That(s.board.secondaryGullies.Count, Is.InRange(3, 4), "seed " + seed);
            }
            var b = new HexBoard(Cfg());
            Assert.AreEqual(42, b.AllCells().Count());
        }

        [Test]
        public void FeverPatch_LoopStillTerminates()
        {
            var s = Sandbox();
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 4);
            s.EdgeBetween(a, b).count = 6;
            s.EdgeBetween(b, a).count = 6;
            s.effects.Add(new ActiveEffect { sourceId = "feverpatch", effect = new Effect(Op.FiredNoScore, 1), roundsLeft = 1 });
            var sim = Run(s, a);
            Assert.IsFalse(sim.stats.aborted);
            Assert.Less(sim.stats.maxBeat, 50);
        }

        [Test]
        public void Lighthouse_DoesNotChargeTheStart()
        {
            var s = RunState.CreateSandbox(Cfg(), 0);
            var st = Put(s, Shape.Conduct, 4, 6, 0);
            var sim = Simulator.Run(s, st.id, true);
            Assert.AreEqual(0, s.NodeOf(st).charge);
        }

        [Test]
        public void W_MaxFiresInBeat()
        {
            var s = Sandbox();
            var S = Put(s, Shape.Instinct, 3, 4, 0);
            Put(s, Shape.Conduct, 4, 4, 2);
            Put(s, Shape.Conduct, 2, 4, 3);
            Put(s, Shape.Conduct, 3, 3, 5);
            var sim = Run(s, S);
            Assert.AreEqual(3, sim.stats.maxFiresInBeat);
        }

        static RunState TriangleRig(int cap, int edgeCount)
        {
            var s = Sandbox(2);
            s.cfg.refireRaiseCap = cap;
            Put(s, Shape.Instinct, 3, 4, 0);
            Put(s, Shape.Instinct, 4, 4, 0);
            Put(s, Shape.Instinct, 3, 5, 0);
            foreach (var e in s.edges.Values) e.count = edgeCount;
            return s;
        }

        [Test]
        public void InfiniteReverb_DenseThickLoopIsDetectedAndScored()
        {
            var s = TriangleRig(6, 3);
            var res = s.Confirm(s.sparks.Values.First().id);
            Assert.IsTrue(res.stats.infinite, "each spark gets two thick inputs (6+6 >= 2+6)");
            Assert.IsFalse(res.stats.aborted);
            Assert.Greater(res.stats.loopCycleLight, 0);
            Assert.AreEqual("infinite", res.pattern);
            Assert.IsTrue(res.events.Any(e => e.type == SimEventType.InfiniteLoop));
        }

        [Test]
        public void InfiniteReverb_TwoSparkRingIsNotEnough()
        {
            var s = Sandbox(2);
            var a = Put(s, Shape.Conduct, 2, 4, 1);
            var b = Put(s, Shape.Conduct, 3, 4, 4);
            s.EdgeBetween(a, b).count = 3;
            s.EdgeBetween(b, a).count = 3;
            var res = s.Confirm(a.id);
            Assert.IsFalse(res.stats.infinite, "one input per spark (max 6) never beats threshold 2+6");
            Assert.IsTrue(res.stats.reverb);
        }

        [Test]
        public void InfiniteReverb_SpecModeStillDecays()
        {
            var s = TriangleRig(-1, 3);
            var res = s.Confirm(s.sparks.Values.First().id);
            Assert.IsFalse(res.stats.infinite);
            Assert.IsFalse(res.stats.aborted);
        }

        [Test]
        public void InfiniteReverb_SingleInputRingDoesNotLoop()
        {
            var s = Sandbox(2);
            var a = Put(s, Shape.Conduct, 3, 4, 1);   // -> (4,4)
            Put(s, Shape.Conduct, 4, 4, 3);           // -> (3,5)
            Put(s, Shape.Conduct, 3, 5, 5);           // -> (3,4)
            foreach (var e in s.edges.Values) e.count = 6;
            var res = s.Confirm(a.id);
            Assert.IsFalse(res.stats.infinite, "a plain ring has one input per spark: needs a denser structure");
        }

        [Test]
        public void FullRunSmoke()
        {
            // Plays greedily through a whole run; must never throw and must end in GameOver or Victory.
            var s = RunState.NewRun(new GameConfig(), 7);
            int guard = 0;
            while (s.phase != Phase.GameOver && s.phase != Phase.Victory && guard++ < 200)
            {
                if (s.candidates.Count > 0)
                {
                    int cell = s.board.AllCells().FirstOrDefault(c => s.CanPlaceAt(c) && s.board.AllCells().Any(n => s.board.HexDistance(n, c) == 1 && s.cellSpark[n] >= 0));
                    if (cell > 0) s.Place(0, cell, 0); else s.SkipCandidates();
                }
                while (s.startsLeft > 0)
                {
                    var best = s.sparks.Values.Where(x => s.IsActiveCell(x.cell)).OrderBy(x => x.id).FirstOrDefault();
                    if (best == null || s.Confirm(best.id) == null) break;
                }
                s.Settle();
                s.NextRound();
            }
            Assert.IsTrue(s.phase == Phase.GameOver || s.phase == Phase.Victory);
        }
    }
}
