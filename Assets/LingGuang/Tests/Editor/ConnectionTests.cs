using System.Linq;
using LingGuang.Core;
using NUnit.Framework;

namespace LingGuang.Tests
{
    public sealed class ConnectionTests
    {
        static RunState Grid()
        {
            var cfg = new GameConfig
            {
                mapRows = new[] { "IIIIIII", "IIIIIII", "IIIIIII" },
                midlineLeftCol = 99, bridges = new BridgeDef[0], outerGapPairs = new int[0],
                secondaryGulliesMin = 0, secondaryGulliesMax = 0,
                charactersEnabled = false, rippleCharge = 0, weakLinksEnabled = false,
                lighthouseCharge = 0, oldDisableCellsPerRound = 0
            };
            var s = RunState.CreateSandbox(cfg, 2);
            s.firstConfirmDone = true;
            return s;
        }

        static Spark Put(RunState s, int col, int row = 0, Shape shape = Shape.Conduct)
        {
            var sp = s.AddSpark(shape, col, row, 5);
            sp.connectionMask = 0;
            s.RebuildEdges();
            return sp;
        }

        static bool Connect(RunState s, Spark a, Spark b, bool both = false)
        {
            return s.TryConnect(a.id, b.id, both);
        }

        static bool Cut(RunState s, Spark a, Spark b)
        {
            return s.Disconnect(a.id, b.id);
        }

        [Test]
        public void WiringControlsActualDeliveries_AndCutStopsThem()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Assert.That(Connect(s, a, b), Is.True);
            Assert.That(s.EdgeBetween(a, b), Is.Not.Null);
            var preview = s.Preview(a.id, 8);
            Assert.That(preview.events.Any(e => e.type == SimEventType.Fire && e.sparkId == b.id && e.beat == 1), Is.True);
            Assert.That(Cut(s, a, b), Is.True);
            s.RebuildEdges();
            Assert.That(s.EdgeBetween(a, b), Is.Null);
            Assert.That(s.Preview(a.id, 8).events.Any(e => e.type == SimEventType.Fire && e.sparkId == b.id), Is.False);
        }

        [Test]
        public void SingleOutputReroutes_WithoutSpendingStartsOrMoves()
        {
            var s = Grid(); var left = Put(s, 0); var a = Put(s, 1); var right = Put(s, 2);
            int starts = s.startsLeft, moves = s.movesLeft;
            Assert.That(Connect(s, a, left), Is.True);
            Assert.That(Connect(s, a, right), Is.True);
            Assert.That(s.EdgeBetween(a, left), Is.Null);
            Assert.That(s.EdgeBetween(a, right), Is.Not.Null);
            Assert.That(s.startsLeft, Is.EqualTo(starts));
            Assert.That(s.movesLeft, Is.EqualTo(moves));
        }

        [Test]
        public void ConnectionsSurviveNextRound_AndCanBeEditedAgain()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Connect(s, a, b);
            s.cfg.targets = Enumerable.Repeat(0, 18).ToArray();
            s.Confirm(a.id);
            int count = s.EdgeBetween(a, b).count;
            s.Settle(); s.NextRound();
            Assert.That(s.phase, Is.EqualTo(Phase.Prepare));
            Assert.That(s.EdgeBetween(a, b).count, Is.EqualTo(count));
            Assert.That(Cut(s, a, b), Is.True);
        }

        [Test]
        public void CanEditBetweenConfirms_WithoutResettingChargeOrScore()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Connect(s, a, b); s.Confirm(a.id);
            int light = s.roundLight, fatigue = s.NodeOf(a).thresholdRaise;
            Assert.That(Cut(s, a, b), Is.True);
            Assert.That(s.roundLight, Is.EqualTo(light));
            Assert.That(s.NodeOf(a).thresholdRaise, Is.EqualTo(fatigue));
        }

        [TestCase(Phase.Playing)]
        [TestCase(Phase.RoundSettled)]
        [TestCase(Phase.GameOver)]
        [TestCase(Phase.Victory)]
        public void RejectsEditsOutsidePreparation(Phase phase)
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Connect(s, a, b); s.phase = phase;
            Assert.That(Cut(s, a, b), Is.False);
            Assert.That(Connect(s, b, a), Is.False);
            Assert.That(s.EdgeBetween(a, b), Is.Not.Null);
            Assert.That(s.EdgeBetween(b, a), Is.Null);
        }

        [Test]
        public void RejectsSelfDistantGullyAndDisabledTargets()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1); var far = Put(s, 3);
            Assert.That(Connect(s, a, a), Is.False);
            Assert.That(Connect(s, a, far), Is.False);
            s.board.secondaryGullies.Add(HexBoard.Key(a.cell, b.cell));
            Assert.That(Connect(s, a, b), Is.False);
            s.board.secondaryGullies.Clear(); s.disabledCells.Add(b.cell);
            Assert.That(Connect(s, a, b), Is.False);
            Assert.That(s.edges, Is.Empty);
        }

        [Test]
        public void BidirectionalFailureIsAtomic_WhenOtherSourceIsNotControllable()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1); b.isCharacter = true;
            Assert.That(Connect(s, a, b, true), Is.False);
            Assert.That(s.edges, Is.Empty);
            Assert.That(Connect(s, a, b), Is.True, "A player can send to a character, but cannot rewrite its outputs.");
        }

        [Test]
        public void ExistingConnectionKeepsLearning_AndPreviewCloneIsIndependent()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Connect(s, a, b); s.EdgeBetween(a, b).count = 6;
            Assert.That(Connect(s, a, b), Is.True);
            Assert.That(s.EdgeBetween(a, b).count, Is.EqualTo(6));
            var clone = s.Clone();
            Assert.That(Cut(clone, clone.sparks[a.id], clone.sparks[b.id]), Is.True);
            Assert.That(s.EdgeBetween(a, b).count, Is.EqualTo(6));
            Assert.That(s.Preview(a.id, 4).events.Any(e => e.type == SimEventType.Fire && e.sparkId == b.id && e.beat == 0), Is.True);
        }

        [Test]
        public void HandmadeChainEarnsLongChainBonus()
        {
            var s = Grid(); var line = Enumerable.Range(0, 5).Select(i => Put(s, i)).ToArray();
            for (int i = 0; i < 4; i++) Assert.That(Connect(s, line[i], line[i + 1]), Is.True);
            var result = s.Confirm(line[0].id);
            Assert.That(result.pattern, Is.EqualTo("long"));
            Assert.That(result.events.Count(e => e.type == SimEventType.Fire), Is.EqualTo(5));
            Assert.That(result.events.Any(e => e.type == SimEventType.PatternRecognized && e.mult == 4), Is.True);
        }

        [Test]
        public void ConeCanFanOutThreeWays_ButRejectsFourthAtomically()
        {
            var s = Grid(); var a = Put(s, 2, 1, Shape.Cone);
            var bs = new[] { Put(s, 3, 1), Put(s, 2, 0), Put(s, 3, 0), Put(s, 2, 2) };
            for (int i = 0; i < 3; i++) Assert.That(Connect(s, a, bs[i]), Is.True);
            Assert.That(Connect(s, a, bs[3], true), Is.False);
            Assert.That(s.EdgeBetween(bs[3], a), Is.Null);
            var result = s.Confirm(a.id);
            Assert.That(result.stats.diverge, Is.True);
            Assert.That(result.pattern, Is.EqualTo("diverge"));
        }

        [Test]
        public void ConeReclaimsEmptyOutput_WhenTargetMovesAway()
        {
            var s = Grid(); var a = Put(s, 2, 1, Shape.Cone);
            var bs = new[] { Put(s, 3, 1), Put(s, 2, 0), Put(s, 3, 0), Put(s, 2, 2) };
            for (int i = 0; i < 3; i++) Assert.That(Connect(s, a, bs[i]), Is.True);
            s.EdgeBetween(a, bs[0]).count = 4;
            Assert.That(s.Move(bs[2].id, s.board.Index(5, 0)), Is.True);
            Assert.That(s.edges.Values.Count(e => e.from == a.id), Is.EqualTo(2));
            Assert.That(Connect(s, a, bs[3]), Is.True, "Empty output directions must not permanently occupy capacity.");
            Assert.That(s.EdgeBetween(a, bs[0]).count, Is.EqualTo(4));
            Assert.That(s.edges.Values.Count(e => e.from == a.id), Is.EqualTo(3));
        }

        [Test]
        public void BidirectionalLoopProducesRefiring_NotJustAnExtraLine()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Assert.That(Connect(s, a, b, true), Is.True);
            s.EdgeBetween(a, b).count = s.EdgeBetween(b, a).count = 3;
            var result = s.Confirm(a.id);
            Assert.That(result.stats.reverb, Is.True);
            Assert.That(result.stats.fires, Is.GreaterThan(2));
            Assert.That(result.stats.aborted, Is.False);
            Assert.That(result.stats.maxBeat, Is.LessThan(s.cfg.maxBeats));
        }

        [Test]
        public void DisconnectDoesNotRemoveReverseDirection()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Connect(s, a, b, true); Cut(s, a, b);
            Assert.That(s.EdgeBetween(a, b), Is.Null);
            Assert.That(s.EdgeBetween(b, a), Is.Not.Null);
        }

        [Test]
        public void CanRewireInstinctAndExternalMemoryLinks_ButNotMemoryInternals()
        {
            var s = Grid(); var a = Put(s, 0, 0, Shape.Instinct); var b = Put(s, 1); var c = Put(s, 2);
            Assert.That(Connect(s, a, b), Is.True);
            s.CreateMemory(new System.Collections.Generic.List<int> { a.id, b.id }, Region.I);
            Assert.That(Connect(s, a, b), Is.False);
            Assert.That(Connect(s, b, c), Is.True);
        }

        [Test]
        public void ManualRoutesFeedConvergence_FromThreeSimultaneousSources()
        {
            var s = Grid();
            var target = Put(s, 3, 1, Shape.Converge);
            var a = Put(s, 3, 0); var b = Put(s, 4, 0); var c = Put(s, 4, 1);
            Connect(s, a, target); Connect(s, b, target); Connect(s, c, target);
            // Lighthouse lights the two row-zero sources while the third is the selected start.
            s.cfg.lighthouseAnchorRow = 0;
            s.cfg.lighthouseRadiusByStage = new[] { 1, 1, 1, 1, 1, 1 };
            s.cfg.lighthouseCharge = 2;
            s.firstConfirmDone = false;
            var result = s.Confirm(c.id);
            Assert.That(result.stats.convergeBurst, Is.True);
            Assert.That(result.events.First(e => e.type == SimEventType.Fire && e.sparkId == target.id).light, Is.EqualTo(50));
            Assert.That(result.pattern, Is.EqualTo("converge"));
        }

        [Test]
        public void ValidationIsReadOnly_AndMissingIdsFailWithoutThrowing()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1);
            Assert.That(s.CanConnect(a.id, b.id, true, out _), Is.True);
            Assert.That(s.edges, Is.Empty);
            Assert.That(s.sparks[a.id].connectionMask, Is.Zero);
            Assert.That(s.TryConnect(a.id, -77), Is.False);
            Assert.That(s.Disconnect(-77, b.id), Is.False);
        }

        [Test]
        public void ReconnectedPrunedEdgeIsNewborn_AndUnrelatedLearningIsKept()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1); var c = Put(s, 2);
            Connect(s, a, b); Connect(s, b, c);
            s.EdgeBetween(a, b).count = 6; s.EdgeBetween(a, b).pruned = true;
            s.EdgeBetween(b, c).count = 4;
            Connect(s, a, b);
            Assert.That(s.EdgeBetween(a, b).pruned, Is.False);
            Assert.That(s.EdgeBetween(a, b).count, Is.Zero);
            Assert.That(s.EdgeBetween(b, c).count, Is.EqualTo(4));
        }

        [Test]
        public void ManuallyWiredEventsAreDeterministic()
        {
            var s = Grid(); var a = Put(s, 0); var b = Put(s, 1); Connect(s, a, b, true);
            var before = s.edges.ToDictionary(p => p.Key, p => p.Value.count);
            string expected = string.Join("\n", s.Clone().Confirm(a.id).events.Select(e => e.ToString()));
            for (int i = 0; i < 100; i++)
                Assert.That(string.Join("\n", s.Clone().Confirm(a.id).events.Select(e => e.ToString())), Is.EqualTo(expected));
            foreach (var pair in before) Assert.That(s.edges[pair.Key].count, Is.EqualTo(pair.Value));
        }
    }
}
