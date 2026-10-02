using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emergence.Editor
{
    /// <summary>Deterministic rule acceptance checks; deliberately independent of NUnit and scene state.</summary>
    public static class EmergenceEngineChecks
    {
        [Serializable] public sealed class CheckResult { public string name, detail; public bool passed; }
        [Serializable] public sealed class CheckReport
        {
            public int passed, failed;
            public List<CheckResult> checks = new List<CheckResult>();
        }
        [Serializable] public sealed class BalanceRun
        {
            public int seed, completedRounds, finalRound, finalCurrency, bestScore;
            public bool buysItems, victory;
            public string stopReason;
            public List<double> targetRatios = new List<double>();
            public List<string> purchases = new List<string>();
        }
        [Serializable] public sealed class BalanceReport
        {
            public string strategy = "Compact growth; best-score first click evaluated on state copies; simple item buying. Diagnostic oracle, not a new-player simulation.";
            public List<BalanceRun> runs = new List<BalanceRun>();
        }

        [UnityEditor.MenuItem("Tools/Emergence/Run Engine Checks")]
        public static void RunFromMenu() { Debug.Log(Run()); }

        public static string Run()
        {
            CheckReport report = new CheckReport();
            Check(report, "synchronous convergence and retained charge", SynchronousConvergence);
            Check(report, "second click harvests retained charge and provenance", SecondClickCharge);
            Check(report, "fired node rejected without state mutation", RejectFiredNode);
            Check(report, "echo rescores once without retransmitting", EchoBounded);
            Check(report, "cycles terminate with one firing per node", CyclesTerminate);
            Check(report, "future delayed signals are not abandoned", DelayedSignals);
            Check(report, "additive multiplier precedes variant multiplication", MultiplierOrder);
            Check(report, "repeat synapse does not trigger itself recursively", RepeatSynapseBounded);
            Check(report, "dense echo multiplication cannot overflow score", DenseEchoScoreSafety);
            Check(report, "structure upgrades cannot reduce score", StructureMonotonic);
            Check(report, "preview is isolated and first two ticks match", PreviewIsolation);
            Check(report, "growth rejects occupied or invalid cells", PlacementValidation);
            Check(report, "purchases reject insufficient funds and duplicate claims", PurchaseValidation);
            Check(report, "invalid memory target preserves inventory and currency", InvalidItemPreserved);
            Check(report, "memory bridge and equipment obey permanent blocks", ConnectionLayers);
            Check(report, "temporary drug expires after one stimulus", DrugExpiration);
            Check(report, "no-op echo drug preserves inventory", NoOpDrugPreserved);
            Check(report, "six boss rules apply to their intended mechanics", BossRules);
            Check(report, "boss rewards cannot be claimed twice", BossRewardValidation);
            Check(report, "save round-trip preserves next stimulus", SaveRoundTrip);
            Check(report, "save round-trip preserves future shop randomness", SaveRandomness);
            Check(report, "all eighteen rounds and six bosses transition correctly", RoundTransitions);
            return JsonUtility.ToJson(report, true);
        }

        public static string RunBalance()
        {
            return RunBalanceWithTargets(null);
        }

        public static string RunBalanceWithTargets(int[] targets)
        {
            if (targets != null && targets.Length != 18) throw new ArgumentException("A diagnostic target curve must contain exactly 18 rounds.", "targets");
            BalanceReport report = new BalanceReport();
            int[] seeds = { 1, 17, 42, 314, 2026, 99173, 20261001, 777777 };
            foreach (int seed in seeds)
            {
                report.runs.Add(Simulate(seed, false, targets));
                report.runs.Add(Simulate(seed, true, targets));
            }
            return JsonUtility.ToJson(report, true);
        }

        private static void Check(CheckReport report, string name, Action action)
        {
            CheckResult result = new CheckResult { name = name, passed = true, detail = "Passed" };
            try { action(); }
            catch (Exception ex) { result.passed = false; result.detail = ex.GetType().Name + ": " + ex.Message; }
            report.checks.Add(result);
            if (result.passed) report.passed++; else report.failed++;
        }

        private static void Expect(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
        }

        private static EmergenceEngine Graph(int count, params int[] links)
        {
            RunState state = new RunState { seed = 123, rngState = 123, phase = RunPhase.Prepare, target = 100000000, nextNodeId = count + 1 };
            int[,] positions = { { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 2, 0 }, { 2, -1 }, { -2, 1 }, { -2, 2 } };
            for (int i = 0; i < count; i++) state.nodes.Add(new NodeState { id = i + 1, q = positions[i, 0], r = positions[i, 1] });
            HashSet<string> allowed = new HashSet<string>();
            for (int i = 0; i < links.Length; i += 2) allowed.Add(links[i] + ":" + links[i + 1]);
            for (int source = 1; source <= count; source++)
                for (int target = 1; target <= count; target++)
                    if (source != target)
                        state.modifications.Add(new EdgeModification
                        {
                            sourceId = source, targetId = target,
                            bridge = allowed.Contains(source + ":" + target),
                            blocked = !allowed.Contains(source + ":" + target)
                        });
            return new EmergenceEngine(state);
        }

        private static EmergenceEngine ConvergenceGraph()
        {
            EmergenceEngine engine = Graph(7, 1, 2, 1, 3, 2, 4, 3, 4, 4, 5, 6, 7, 7, 5);
            engine.FindNode(7).kind = NodeKind.Sensitive;
            return engine;
        }

        private static void SynchronousConvergence()
        {
            EmergenceEngine engine = ConvergenceGraph();
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success, result.error);
            Expect(result.firedIds.OrderBy(x => x).SequenceEqual(new[] { 1, 2, 3, 4 }), "Expected only S/A/B/C to fire.");
            Expect(engine.FindNode(5).charge == 1 && !engine.FindNode(5).fired, "D must retain one charge.");
            Expect(result.events.Count(x => x.kind == EventKind.Fire && x.nodeId == 4) == 1, "C fired more than once.");
            Expect(result.events.Single(x => x.kind == EventKind.Fire && x.nodeId == 4).tick == 2, "C must fire on tick 2.");
            Expect(engine.FindNode(5).contributions.Count == 1, "Retained charge must retain provenance.");
            Expect(result.score == 150, "Four ordinary nodes plus short-chain should score (40 + 10) × (1 + 2) = 150; got " + result.score + ".");
        }

        private static void SecondClickCharge()
        {
            EmergenceEngine engine = ConvergenceGraph();
            engine.Stimulate(1);
            StimulusResult result = engine.Stimulate(6);
            Expect(result.success && result.firedIds.Contains(5), "Second source must harvest D's existing charge.");
            Expect(engine.FindNode(5).layer >= 3, "D must keep the longest prior contribution layer.");
            Expect(engine.State.clicksRemaining == 0, "Exactly two clicks must be spent.");
        }

        private static void RejectFiredNode()
        {
            EmergenceEngine engine = ConvergenceGraph();
            engine.Stimulate(1);
            RunState before = engine.State.Copy();
            StimulusResult invalid = engine.Stimulate(1);
            Expect(!invalid.success, "Already-fired source accepted.");
            RunState after = engine.State.Copy();
            after.lastMessage = before.lastMessage; // A helpful rejection message is allowed; gameplay mutation is not.
            Expect(JsonUtility.ToJson(before) == JsonUtility.ToJson(after), "Rejected source changed gameplay state.");
        }

        private static void EchoBounded()
        {
            EmergenceEngine engine = Graph(3, 1, 2, 2, 3);
            engine.FindNode(2).kind = NodeKind.Sensitive;
            engine.FindNode(3).kind = NodeKind.Sensitive;
            engine.State.modifications.First(x => x.sourceId == 1 && x.targetId == 2).echo = true;
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success && result.rescores == 1, "Exactly one echo rescore expected.");
            Expect(result.firedIds.Count == 3, "Echo added a physical firing.");
            Expect(result.events.Count(x => x.kind == EventKind.Rescore && x.nodeId == 2) == 1, "Echo target must rescore once.");
            Expect(result.events.Count(x => x.kind == EventKind.Signal && x.sourceId == 2 && x.nodeId == 3) == 1, "Rescoring retransmitted to the downstream node.");
        }

        private static void CyclesTerminate()
        {
            EmergenceEngine engine = Graph(3, 1, 2, 2, 3, 3, 1);
            foreach (NodeState node in engine.State.nodes) node.kind = NodeKind.Sensitive;
            foreach (EdgeModification edge in engine.State.modifications.Where(x => x.bridge)) edge.echo = true;
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success && result.firedIds.Count == 3, "Cycle must fire exactly three nodes.");
            Expect(result.firedIds.Distinct().Count() == 3, "Cycle re-fired a node.");
            Expect(result.events.Count < 100 && result.rescores <= 3 && !result.stillPropagating, "Cycle or echo event queue did not terminate.");
        }

        private static void DelayedSignals()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.modifications.First(x => x.sourceId == 1 && x.targetId == 2).extraDelay = 2;
            StimulusResult result = engine.Stimulate(1);
            Expect(result.firedIds.Contains(2), "Signal was discarded during empty ticks.");
            Expect(result.events.Single(x => x.kind == EventKind.Fire && x.nodeId == 2).tick == 3, "Expected arrival at tick 3.");
            Expect(engine.FindNode(2).layer == 1, "Waiting must not increase causal layer.");
        }

        private static void StructureMonotonic()
        {
            EmergenceEngine engine = Graph(7, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7);
            foreach (NodeState node in engine.State.nodes) node.kind = NodeKind.Sensitive;
            RunState original = engine.State.Copy();
            int baseline = new EmergenceEngine(original.Copy()).Stimulate(1).score;
            Expect(original.structures.Count >= 5, "Structure catalogue missing.");
            foreach (StructureLevel structure in original.structures)
            {
                RunState upgraded = original.Copy();
                upgraded.structures.First(x => x.id == structure.id).level += 1;
                StimulusResult result = new EmergenceEngine(upgraded).Stimulate(1);
                Expect(result.score >= baseline, "Upgrading " + structure.id + " lowered " + baseline + " to " + result.score + ".");
            }
        }

        private static void MultiplierOrder()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.FindNode(2).kind = NodeKind.Sensitive;
            engine.FindNode(2).variant = NodeVariant.Multiple;
            engine.State.synapses.Add(new SynapseState { id = "J01" });
            StimulusResult result = engine.Stimulate(1);
            Expect(result.score == 198, "Expected (10 + 7 + 5) × ((1 + 4 + 1) × 1.5) = 198; got " + result.score + ".");
        }

        private static void RepeatSynapseBounded()
        {
            EmergenceEngine engine = Graph(3, 1, 2, 2, 3);
            engine.FindNode(2).kind = engine.FindNode(3).kind = NodeKind.Sensitive;
            engine.State.synapses.Add(new SynapseState { id = "J12" });
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success && result.rescores == 2, "Two first-firing neighbours should create exactly two repeat rescores.");
            Expect(result.events.Count(x => x.kind == EventKind.Rescore) == result.rescores, "Event stream duplicated a rescore notification.");
            Expect(result.firedIds.Count == 3 && result.events.Count < 100, "Repeat synapse retriggered propagation or itself.");
        }

        private static void DenseEchoScoreSafety()
        {
            List<int> links = new List<int>();
            for (int a = 1; a <= 9; a++) for (int b = 1; b <= 9; b++) if (a != b) { links.Add(a); links.Add(b); }
            EmergenceEngine engine = Graph(9, links.ToArray());
            foreach (NodeState node in engine.State.nodes) { node.kind = NodeKind.Sensitive; node.variant = NodeVariant.Multiple; }
            foreach (EdgeModification edge in engine.State.modifications) edge.echo = true;
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success && result.firedIds.Count == 9 && result.rescores <= 72, "Dense echo graph did not terminate within per-edge bound.");
            Expect(result.score > 0 && engine.State.roundScore > 0, "Large multiplier overflowed into a non-positive score.");
            Expect(!double.IsNaN(result.multiplier) && !double.IsInfinity(result.multiplier), "Multiplier became non-finite.");
            engine.State.target = 1;
            Expect(engine.EndRound() && engine.State.totalScore > 0 && engine.State.currency >= 0, "Large score overflowed reward or cumulative score.");
        }

        private static void PreviewIsolation()
        {
            EmergenceEngine engine = ConvergenceGraph();
            string before = JsonUtility.ToJson(engine.State);
            StimulusResult preview = engine.Preview(1);
            Expect(before == JsonUtility.ToJson(engine.State), "Preview changed state.");
            Expect(preview.success && preview.score == 0, "Preview should hide final score.");
            Expect(preview.events.All(x => x.tick <= 2), "Preview leaked events beyond two ticks.");
            StimulusResult actual = engine.Stimulate(1);
            string projected = string.Join(",", preview.events.Where(x => x.kind == EventKind.Fire).Select(x => x.tick + ":" + x.nodeId));
            string observed = string.Join(",", actual.events.Where(x => x.kind == EventKind.Fire && x.tick <= 2).Select(x => x.tick + ":" + x.nodeId));
            Expect(projected == observed, "Preview firing sequence differs from execution.");
        }

        private static void PlacementValidation()
        {
            EmergenceEngine engine = new EmergenceEngine(42);
            NodeState occupied = engine.State.nodes[0];
            int before = engine.State.nodes.Count;
            Expect(!engine.PlaceCandidate(0, occupied.q, occupied.r), "Occupied cell accepted.");
            Expect(!engine.PlaceCandidate(0, 8, 8), "Out-of-board cell accepted.");
            Expect(engine.State.nodes.Count == before && engine.State.phase == RunPhase.Grow, "Rejected growth changed state.");
            PlaceCompact(engine);
            Expect(engine.State.nodes.Count == before + 1 && engine.State.phase == RunPhase.Prepare, "Valid placement failed.");
            Expect(!engine.PlaceCandidate(0, -4, 4), "Candidate was claimed twice.");
        }

        private static void PurchaseValidation()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.phase = RunPhase.Shop;
            engine.State.currency = 4;
            engine.State.shop.Add(new ShopOffer { id = "J01", price = 5 });
            Expect(!engine.BuyOffer(0), "Unaffordable item accepted.");
            Expect(engine.State.currency == 4 && engine.State.synapses.Count == 0 && !engine.State.shop[0].sold, "Unaffordable purchase changed state.");
            engine.State.currency = 5;
            Expect(engine.BuyOffer(0), engine.LastError);
            Expect(engine.State.currency == 0 && engine.State.synapses.Count == 1, "Purchase did not deduct exactly its price.");
            Expect(!engine.BuyOffer(0) && engine.State.currency == 0 && engine.State.synapses.Count == 1, "Duplicate purchase was accepted.");
        }

        private static void InvalidItemPreserved()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.inventory.Add(new ItemState { id = "M03" });
            int currency = engine.State.currency;
            Expect(!engine.ApplyItem(0, 9999), "Invalid node target accepted.");
            Expect(engine.State.inventory.Count == 1 && engine.State.currency == currency && engine.State.memoriesUsed == 0, "Invalid memory use consumed resources.");
        }

        private static void SaveRoundTrip()
        {
            EmergenceEngine original = ConvergenceGraph();
            original.Stimulate(1);
            string saved = JsonUtility.ToJson(original.State);
            RunState restoredState = JsonUtility.FromJson<RunState>(saved);
            EmergenceEngine restored = new EmergenceEngine(restoredState);
            Expect(saved == JsonUtility.ToJson(restored.State), "Save round-trip changed state.");
            StimulusResult resultA = original.Stimulate(6);
            StimulusResult resultB = restored.Stimulate(6);
            Expect(JsonUtility.ToJson(resultA) == JsonUtility.ToJson(resultB), "Resumed propagation differed.");
            Expect(JsonUtility.ToJson(original.State) == JsonUtility.ToJson(restored.State), "Resumed final state differed.");
            RunState copy = original.State.Copy();
            copy.nodes[0].charge += 1;
            Expect(copy.nodes[0].charge != original.State.nodes[0].charge, "Clone shares mutable nodes.");
        }

        private static void ConnectionLayers()
        {
            RunState state = new RunState { phase = RunPhase.Prepare, target = 1000000, nextNodeId = 3 };
            state.nodes.Add(new NodeState { id = 1, q = -2, r = 0 });
            state.nodes.Add(new NodeState { id = 2, q = 2, r = 0 });
            state.inventory.Add(new ItemState { id = "M01" });
            state.inventory.Add(new ItemState { id = "M02" });
            EmergenceEngine engine = new EmergenceEngine(state);
            Expect(engine.ApplyItem(0, 1, 2), engine.LastError);
            Expect(engine.GetEdges().Count(x => !x.blocked) == 2, "Memory bridge should connect both directions.");
            engine.State.synapses.Add(new SynapseState { id = "J15", sourceId = 1, targetId = 2 });
            Expect(engine.GetEdges().Count(x => !x.blocked) == 2, "Overlapping equipment and memory must not duplicate edges.");
            Expect(engine.ApplyItem(0, 1, 2), engine.LastError);
            Expect(engine.GetEdges().All(x => x.blocked), "Permanent block must override equipment bridge.");
            Expect(engine.SellSynapse(0), engine.LastError);
            Expect(engine.GetEdges().All(x => x.blocked), "Selling equipment must preserve memory blocks.");
        }

        private static void DrugExpiration()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.inventory.Add(new ItemState { id = "P02" });
            Expect(engine.ApplyItem(0, 1), engine.LastError);
            Expect(engine.GetEdges().Any(x => x.sourceId == 1 && x.echo), "Echo drug did not modify outgoing edges.");
            StimulusResult result = engine.Stimulate(1);
            Expect(result.success && result.rescores == 1, "Drug did not cause an echo rescore.");
            Expect(engine.State.drugs.Count == 0 && engine.GetEdges().All(x => !x.echo), "Drug survived beyond its stimulus.");
        }

        private static void NoOpDrugPreserved()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.modifications.First(x => x.sourceId == 1 && x.targetId == 2).echo = true;
            engine.State.inventory.Add(new ItemState { id = "P02" });
            Expect(!engine.ApplyItem(0, 1), "Echo drug accepted a source whose every outgoing edge already echoes.");
            Expect(engine.State.inventory.Count == 1 && engine.State.drugsUsed == 0 && engine.State.drugs.Count == 0, "No-op drug consumed inventory or the round quota.");
            engine.State.inventory.Add(new ItemState { id = "P03" });
            Expect(!engine.ApplyItem(1, 1), "Bridge drug accepted a source with no new nearby connection.");
            Expect(engine.State.inventory.Count == 2 && engine.State.drugsUsed == 0, "No-op bridge consumed inventory or the round quota.");
        }

        private static void SaveRandomness()
        {
            EmergenceEngine original = new EmergenceEngine(314);
            PlaceCompact(original);
            original.State.target = 1;
            original.Stimulate(original.State.nodes[0].id);
            Expect(original.EndRound(), original.LastError);
            EmergenceEngine restored = new EmergenceEngine(JsonUtility.FromJson<RunState>(JsonUtility.ToJson(original.State)));
            Expect(original.RerollShop() && restored.RerollShop(), "Reroll unexpectedly failed after saving.");
            Expect(JsonUtility.ToJson(original.State) == JsonUtility.ToJson(restored.State), "Saving changed RNG continuation or shop offers.");
            Expect(original.NextRound() && restored.NextRound(), "Saved shop could not continue.");
            Expect(JsonUtility.ToJson(original.State) == JsonUtility.ToJson(restored.State), "Saving changed the next growth candidates.");
        }

        private static void BossRules()
        {
            RunState membraneState = new RunState { phase = RunPhase.Prepare, round = 3, target = 1000000 };
            membraneState.nodes.Add(new NodeState { id = 1, q = -1 });
            membraneState.nodes.Add(new NodeState { id = 2, q = 0 });
            EmergenceEngine membrane = new EmergenceEngine(membraneState);
            Expect(membrane.GetEdges().All(x => x.blocked), "Membrane did not block natural cross-border edges.");
            membrane.State.modifications.Add(new EdgeModification { sourceId = 1, targetId = 2, bridge = true });
            Expect(membrane.GetEdges().Single(x => x.sourceId == 1 && x.targetId == 2).blocked == false, "Membrane blocked a permanent bridge.");

            EmergenceEngine metronome = Graph(2, 1, 2);
            metronome.State.round = 6;
            metronome.FindNode(2).variant = NodeVariant.Gold;
            metronome.State.synapses.Add(new SynapseState { id = "J02" });
            Expect(metronome.Stimulate(1).score == 130, "Metronome must suppress tick-1 base score while keeping gold and passive bonuses.");

            EmergenceEngine silence = Graph(2, 1, 2);
            silence.State.round = 9;
            silence.FindNode(2).kind = NodeKind.Sensitive;
            silence.State.synapses.Add(new SynapseState { id = "J01" });
            silence.State.synapses.Add(new SynapseState { id = "J02" });
            EmergenceEngine swapped = new EmergenceEngine(silence.State.Copy());
            Expect(swapped.SwapSynapses(0, 1), swapped.LastError);
            Expect(silence.Stimulate(1).score == 74 && swapped.Stimulate(1).score == 132, "Silence must disable only the leftmost slot and respond to pre-combat sorting.");

            EmergenceEngine latency = Graph(3, 1, 2, 2, 3);
            latency.State.round = 12;
            latency.FindNode(2).kind = latency.FindNode(3).kind = NodeKind.Sensitive;
            StimulusResult delayed = latency.Stimulate(1);
            Expect(delayed.events.Single(x => x.kind == EventKind.Fire && x.nodeId == 3).tick == 3 && latency.FindNode(3).layer == 2, "Latency must add time to right-side outgoing edges without inflating layers.");

            EmergenceEngine forgetting = ConvergenceGraph();
            forgetting.State.round = 15;
            forgetting.Stimulate(1);
            Expect(forgetting.FindNode(5).charge == 0 && forgetting.FindNode(5).contributions.Count == 0, "Forgetting did not clear both charge and provenance after the first click.");

            EmergenceEngine twin = Graph(3, 1, 2, 2, 3);
            twin.State.round = 18;
            twin.FindNode(2).kind = twin.FindNode(3).kind = NodeKind.Sensitive;
            StimulusResult resonated = twin.Stimulate(1);
            Expect(twin.State.twinAwarded && resonated.score == 402, "Twin must award exactly 100 inspiration once both marked nodes fire.");
        }

        private static void BossRewardValidation()
        {
            EmergenceEngine engine = Graph(2, 1, 2);
            engine.State.phase = RunPhase.Shop;
            engine.State.currency = 0;
            engine.State.shop.Add(new ShopOffer { id = "J01", price = 0, bossReward = true });
            engine.State.shop.Add(new ShopOffer { id = "J02", price = 0, bossReward = true });
            Expect(engine.BuyOffer(0), engine.LastError);
            Expect(!engine.BuyOffer(1) && engine.State.synapses.Count == 1 && engine.State.currency == 0, "Boss reward was claimed more than once.");
        }

        private static void RoundTransitions()
        {
            EmergenceEngine engine = new EmergenceEngine(20261001);
            int bosses = 0;
            for (int round = 1; round <= 18; round++)
            {
                Expect(engine.State.round == round && engine.State.phase == RunPhase.Grow, "Wrong round-start state for " + round + ".");
                Expect(engine.State.clicksRemaining == 2, "Clicks did not reset.");
                Expect(engine.State.nodes.All(x => !x.fired && x.charge == 0), "Neuron state did not reset.");
                if (round % 3 == 0) { bosses++; Expect(!string.IsNullOrEmpty(engine.State.bossName), "Boss absent on round " + round + "."); }
                PlaceCompact(engine);
                engine.State.target = 1; // State-machine coverage, deliberately not a balance test.
                StimulusResult result = engine.Stimulate(engine.State.nodes[0].id);
                Expect(result.success, result.error);
                Expect(engine.EndRound(), "Round did not finish: " + engine.LastError);
                Expect(engine.State.currency >= 0, "Currency went negative.");
                if (round < 18)
                {
                    Expect(engine.State.phase == RunPhase.Shop, "Expected shop after round " + round + ".");
                    Expect(engine.NextRound(), engine.LastError);
                }
            }
            Expect(bosses == 6 && engine.State.phase == RunPhase.Victory, "Expected six bosses then victory.");
            Expect(engine.State.nodes.Count == 25, "Expected 7 starting + 18 grown nodes.");
            Expect(!engine.NextRound(), "Victory allowed round 19.");
        }

        private static void PlaceCompact(EmergenceEngine engine)
        {
            int candidate = 0;
            double candidateWeight = -1;
            for (int i = 0; i < engine.State.candidates.Count; i++)
            {
                NodeCandidate c = engine.State.candidates[i];
                double weight = (c.kind == NodeKind.Sensitive ? 8 : c.kind == NodeKind.Core ? 4 : 1)
                    + (c.variant == NodeVariant.Resonant ? 5 : c.variant == NodeVariant.Gold ? 3 : c.variant == NodeVariant.Multiple ? 4 : 0);
                if (weight > candidateWeight) { candidateWeight = weight; candidate = i; }
            }
            int bestQ = 0, bestR = 0;
            double best = double.NegativeInfinity;
            for (int q = -4; q <= 4; q++) for (int r = -4; r <= 4; r++)
            {
                if (!EmergenceEngine.IsCell(q, r) || engine.State.nodes.Any(x => x.q == q && x.r == r)) continue;
                int neighbours = engine.State.nodes.Count(x => EmergenceEngine.Distance(q, r, x.q, x.r) == 1);
                double value = neighbours * 10 - EmergenceEngine.Distance(0, 0, q, r);
                if (value > best) { best = value; bestQ = q; bestR = r; }
            }
            Expect(engine.PlaceCandidate(candidate, bestQ, bestR), "Compact placement failed: " + engine.LastError);
        }

        private static BalanceRun Simulate(int seed, bool buy, int[] targets = null)
        {
            BalanceRun summary = new BalanceRun { seed = seed, buysItems = buy };
            EmergenceEngine engine = new EmergenceEngine(seed);
            try
            {
                for (int guard = 0; guard < 100; guard++)
                {
                    if (engine.State.phase == RunPhase.Victory || engine.State.phase == RunPhase.Defeat) break;
                    if (engine.State.phase == RunPhase.Grow) PlaceCompact(engine);
                    if (engine.State.phase == RunPhase.Prepare)
                    {
                        if (targets != null) engine.State.target = targets[engine.State.round - 1];
                        while (engine.State.clicksRemaining > 0 && engine.State.roundScore < engine.State.target)
                        {
                            int source = -1, score = -1;
                            foreach (NodeState node in engine.State.nodes.Where(x => !x.fired))
                            {
                                StimulusResult hypothetical = new EmergenceEngine(engine.State.Copy()).Stimulate(node.id);
                                if (hypothetical.success && hypothetical.score > score) { source = node.id; score = hypothetical.score; }
                            }
                            if (source < 0) break;
                            engine.Stimulate(source);
                        }
                        summary.targetRatios.Add(Math.Round((double)engine.State.roundScore / Math.Max(1, engine.State.target), 3));
                        if (engine.State.roundScore >= engine.State.target) summary.completedRounds = engine.State.round;
                        if (engine.State.phase == RunPhase.Defeat) { summary.stopReason = engine.State.lastMessage; break; }
                        if (!engine.EndRound()) { summary.stopReason = engine.LastError; break; }
                    }
                    if (engine.State.phase == RunPhase.Shop)
                    {
                        if (buy) BuySimple(engine, summary);
                        if (!engine.NextRound()) { summary.stopReason = engine.LastError; break; }
                    }
                }
            }
            catch (Exception ex) { summary.stopReason = ex.GetType().Name + ": " + ex.Message; }
            summary.finalRound = engine.State.round;
            summary.victory = engine.State.phase == RunPhase.Victory;
            summary.finalCurrency = engine.State.currency;
            summary.bestScore = engine.State.bestScore;
            if (string.IsNullOrEmpty(summary.stopReason)) summary.stopReason = engine.State.phase.ToString();
            return summary;
        }

        private static void BuySimple(EmergenceEngine engine, BalanceRun summary)
        {
            string[] priority = { "J09", "J01", "J03", "J10", "J06", "J02", "J05", "J04", "J07", "J08", "J11", "J12" };
            foreach (string id in priority)
                for (int i = 0; i < engine.State.shop.Count; i++)
                    if (engine.State.shop[i].id == id && !engine.State.shop[i].sold && engine.BuyOffer(i)) summary.purchases.Add("R" + engine.State.round + " " + id);
        }
    }
}
