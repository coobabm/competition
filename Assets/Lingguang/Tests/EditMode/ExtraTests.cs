using System.Linq;
using Lingguang.Core;
using NUnit.Framework;
using static Lingguang.Tests.Kit;

namespace Lingguang.Tests
{
    /// <summary>T01–T17 之外的回归测试：地图、次级沟、废退、老年、撤销、灯塔、结算流程。</summary>
    public class ExtraTests
    {
        [Test]
        public void Map_Has42Cells_WithSpecRegionCounts()
        {
            var board = Board.Create(BoardLayout.CreateDefault(), null);
            Assert.AreEqual(42, board.Cells.Count);
            Assert.AreEqual(8, board.CountRegion('I'));
            Assert.AreEqual(7, board.CountRegion('K'));
            Assert.AreEqual(9, board.CountRegion('C'));
            Assert.AreEqual(10, board.CountRegion('F'));
            Assert.AreEqual(8, board.CountRegion('T'));
            foreach (Stage st in System.Enum.GetValues(typeof(Stage)))
                Assert.IsTrue(board.IsConnected(st), "基础地图在 " + st + " 应连通");
        }

        [Test]
        public void Map_Bridges_OpenByStage()
        {
            var board = Board.Create(BoardLayout.CreateDefault(), null);
            Assert.IsFalse(board.IsGroove(new Cell(4, 5), new Cell(5, 5), Stage.Infant));
            Assert.IsTrue(board.IsGroove(new Cell(4, 3), new Cell(5, 3), Stage.Childhood));
            Assert.IsFalse(board.IsGroove(new Cell(4, 3), new Cell(5, 3), Stage.Adolescence));
            Assert.IsTrue(board.IsGroove(new Cell(4, 1), new Cell(5, 1), Stage.Adolescence));
            Assert.IsFalse(board.IsGroove(new Cell(4, 1), new Cell(5, 1), Stage.Adult));
            Assert.IsTrue(board.IsGroove(new Cell(4, 4), new Cell(5, 4), Stage.OldAge));
        }

        [Test]
        public void Map_EditedLayout_IsRespected()
        {
            var layout = BoardLayout.CreateDefault();
            layout.Rows[6] = ". . . I I I I . . .";   // 改形状：行 6 多两格
            var board = Board.Create(layout, null);
            Assert.AreEqual(44, board.Cells.Count);
            Assert.IsTrue(board.Exists(new Cell(3, 6)));
        }

        [Test]
        public void SecondaryGrooves_3To4_Connected_Deterministic()
        {
            var layout = BoardLayout.CreateDefault();
            for (int seed = 0; seed < 200; seed++)
            {
                var board = Board.Create(layout, new Rng(seed));
                int n = board.SecondaryGrooves.Count;
                Assert.That(n, Is.InRange(3, 4), "seed " + seed);
                foreach (var e in board.SecondaryGrooves)
                {
                    Assert.AreEqual(board.RegionOf(e.A), board.RegionOf(e.B), "次级沟必须在区域内部");
                    Assert.IsFalse(board.IsBridge(e.A, e.B));
                }
                foreach (Stage st in System.Enum.GetValues(typeof(Stage)))
                    Assert.IsTrue(board.IsConnected(st), "seed " + seed + " " + st);
                var again = Board.Create(layout, new Rng(seed));
                CollectionAssert.AreEquivalent(board.SecondaryGrooves, again.SecondaryGrooves);
            }
        }

        [Test]
        public void Disuse_SixIdleRounds_DropsOneLevel()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);
            SetCount(s, a, b, 4);
            var key = new EdgeKey(a.Id, b.Id);
            for (int i = 0; i < 5; i++) { NewRound(s); Lifecycle.ApplyDisuse(s); }
            Assert.AreEqual(4, s.EdgeCount(key));
            NewRound(s); Lifecycle.ApplyDisuse(s);
            Assert.AreEqual(1, s.EdgeCount(key));
            Assert.AreEqual(0, s.Edges[key].IdleRounds);
        }

        [Test]
        public void Disuse_ConductResetsIdleCounter()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);
            var key = new EdgeKey(a.Id, b.Id);
            for (int i = 0; i < 5; i++) { NewRound(s); Lifecycle.ApplyDisuse(s); }
            NewRound(s);
            Fire(s, a);
            Lifecycle.ApplyDisuse(s);
            Assert.AreEqual(0, s.Edges[key].IdleRounds);
            Assert.AreEqual(1, s.EdgeCount(key));
        }

        [Test]
        public void OldAge_DisuseEveryRound_ZeroCountPrunedAfterAnotherIdleRound()
        {
            var s = State(Stage.OldAge);
            var a = Conduction(s, 2, 5, 1);
            var b = Conduction(s, 3, 5, 1);
            SetCount(s, a, b, 3);
            var key = new EdgeKey(a.Id, b.Id);
            NewRound(s); Lifecycle.ApplyDisuse(s);
            Assert.AreEqual(0, s.EdgeCount(key));
            Assert.IsFalse(s.Edges[key].Pruned);
            NewRound(s); Lifecycle.ApplyDisuse(s);
            Assert.IsTrue(s.Edges[key].Pruned);
        }

        [Test]
        public void OldAge_NonMyelinEdgesGetExtraDelay()
        {
            var s = State(Stage.OldAge);
            var a = Conduction(s, 2, 5, 1);
            var b = Conduction(s, 3, 5, 1);
            var r = Fire(s, a);
            Assert.AreEqual(2, FireBeat(r, b));
        }

        [Test]
        public void OldAge_RoundStart_DisablesTwoCellsFromF()
        {
            var session = Session(Stage.OldAge);
            var s = session.State;
            Assert.AreEqual(2, s.DisabledCells.Count);
            Assert.IsTrue(s.DisabledCells.All(c => s.Board.RegionOf(c) == 'F'));
        }

        [Test]
        public void Lighthouse_FirstConfirmOnly_CanFireGlow()
        {
            var s = State(Stage.Infant, Cfg(lighthouse: true));
            var near = Conduction(s, 4, 5, 3);      // 距锚点 (4,6) 为 1；方向 3 → (4,6) 空格
            var starter = Conduction(s, 6, 5, 0);   // 方向 0 → (7,4) 无格子
            var r = Fire(s, starter);
            Assert.AreEqual(0, FireBeat(r, near), Kit.Dump(r));
            Assert.IsTrue(r.Events.Any(e => e.Type == SimEventType.Deliver && e.Kind == DeliveryKind.Lighthouse));
            Assert.IsFalse(s.Round.FirstConfirmPending);
        }

        [Test]
        public void SeparationGate_TurnsLighthouseOff()
        {
            var cfg = Cfg(lighthouse: true);
            var session = Session(Stage.Infant, 2, "separation", cfg);
            var s = session.State;
            var near = Conduction(s, 4, 5, 3);
            var starter = Conduction(s, 6, 5, 0);
            var outcome = session.Confirm(new[] { starter.Id });
            Assert.AreEqual(-1, FireBeat(outcome.Sim, near));
        }

        [Test]
        public void AnxietyGate_ChangesThresholds()
        {
            var session = Session(Stage.Adult, 2, "anxiety");
            var s = session.State;
            var inC = Put(s, ShapeKind.Cone, 3, 2, 0);
            var inF = Conduction(s, 3, 1, 0);
            Assert.AreEqual(2, EffectQuery.Threshold(s, inC.Id), "锥体 4 − 2");
            Assert.AreEqual(4, EffectQuery.Threshold(s, inF.Id), "传导 2 + 2");
        }

        [Test]
        public void Undo_RevertsOperationsSinceLastConfirm()
        {
            var session = Session(Stage.Infant);
            int before = session.State.Glows.Count;
            Assert.IsTrue(session.Place(0, new Cell(3, 5), 1, out string reason), reason);
            Assert.AreEqual(before + 1, session.State.Glows.Count);
            Assert.IsTrue(session.Undo());
            Assert.AreEqual(before, session.State.Glows.Count);
            Assert.IsTrue(session.State.Round.PlacementAvailable);
            Assert.IsFalse(session.Undo());
        }

        [Test]
        public void Confirm_ClearsUndo_AndTrialRunDoesNotChangeState()
        {
            var session = Session(Stage.Infant);
            Assert.IsTrue(session.Place(0, new Cell(3, 5), 1, out _));
            int id = session.State.NextGlowId - 1;
            var trial = session.TrialRun(new[] { id });
            Assert.IsTrue(trial.Events.Any(e => e.Type == SimEventType.Fire));
            Assert.IsFalse(session.State.Glows[id].Fired);
            Assert.AreEqual(2, session.State.Round.StartsLeft);

            session.Confirm(new[] { id });
            Assert.IsFalse(session.CanUndo);
            Assert.IsTrue(session.State.Glows[id].Fired);
        }

        [Test]
        public void Settlement_FailingTargetEndsRun_PassingAdvances()
        {
            var session = Session(Stage.Infant);
            var s = session.State;
            var g = Conduction(s, 2, 5, 4);   // → (1,5) 无格子
            var h = Conduction(s, 6, 5, 0);   // → (7,4) 无格子
            Assert.IsNull(session.Confirm(new[] { g.Id }).Settlement);
            var settle = session.Confirm(new[] { h.Id }).Settlement;
            Assert.AreEqual(10, settle.Score, "婴儿不识别回路型：5 + 5");
            Assert.IsFalse(settle.Passed);
            Assert.IsTrue(session.State.RunOver);

            var cfg = Cfg();
            cfg.Stages[0].Targets = new[] { 10, 45, 60 };
            var ok = Session(Stage.Infant, 0, "", cfg);
            var g2 = Conduction(ok.State, 2, 5, 4);
            var h2 = Conduction(ok.State, 6, 5, 0);
            ok.Confirm(new[] { g2.Id });
            var passed = ok.Confirm(new[] { h2.Id }).Settlement;
            Assert.IsTrue(passed.Passed);
            Assert.AreEqual(3, passed.CoinsGained);
            Assert.AreEqual(1, ok.State.RoundInStage);
            Assert.IsFalse(g2.Fired, "新一轮已闪标记重置");
        }

        [Test]
        public void PatternFreshness_RepeatedPatternLosesMultiplier()
        {
            var s = State(Stage.Adult);
            var a = Conduction(s, 2, 5, 1);
            var b = Conduction(s, 3, 5, 0);   // (3,5) 方向 0 → (4,4)
            Conduction(s, 4, 4, 1);           // → (5,4) 中线
            Fire(s, a);
            Assert.AreEqual(2.0, s.Round.PatternMult, 1e-9);
            NewRound(s);
            Fire(s, a);
            Assert.AreEqual(1.8, s.Round.PatternMult, 1e-9, "第二次短链 ×0.9");
            Assert.AreEqual(1, s.FreshnessStreak);
            _ = b;
        }

        [Test]
        public void FullRun_AllStagesReachable_WithCheats()
        {
            // 把目标分调成 0，检查 18 轮流程能从婴儿走到老年终局。
            var cfg = GameConfig.CreateDefault();
            foreach (var st in cfg.Stages) st.Targets = new[] { 0, 0, 0 };
            cfg.Gates = cfg.Gates.Where(gt => gt.Id != "training").ToArray();
            var session = GameSession.NewRun(cfg, 99, new RunOptions());
            int guard = 0;
            while (!session.State.RunOver && guard++ < 50) session.EndRound();
            Assert.IsTrue(session.State.RunWon);
            Assert.AreEqual(18, session.State.TotalRoundsPlayed);
            Assert.AreEqual(5, session.State.SettledRegions.Count, "I K C F T 五个区域依次沉淀");
        }

        [Test]
        public void Lighthouse_ShinesFromOutside_BeforeStarters_RangeShrinks()
        {
            var adult = State(Stage.Adult, Cfg(lighthouse: true));             // R = 1：只照到锚点本身
            var onAnchor = Conduction(adult, 4, 6, 4);                          // → (3,6) 无格子
            var nextRow = Conduction(adult, 4, 5, 4);                           // → (3,5) 空格
            var starter = Conduction(adult, 6, 5, 0);
            var r = Fire(adult, starter);
            Assert.AreEqual(SimEventType.LighthouseShine, r.Events[0].Type, "灯塔最先照进来");
            CollectionAssert.AreEquivalent(new[] { new Cell(4, 6), new Cell(5, 6) }, r.Events[0].Cells);
            Assert.AreEqual(0, FireBeat(r, onAnchor));
            Assert.AreEqual(-1, FireBeat(r, nextRow));

            var old = State(Stage.OldAge, Cfg(lighthouse: true));               // 老年：没有记忆、也没有导通过的灵光 → 不照
            Conduction(old, 4, 6, 4);
            var s2 = Conduction(old, 6, 5, 0);
            Lifecycle.RefreshLighthouseFocus(old);
            Assert.AreEqual(-1, old.LighthouseFocusGlowId);
            var r2 = Fire(old, s2);
            Assert.IsFalse(r2.Events.Any(e => e.Type == SimEventType.LighthouseShine || e.Kind == DeliveryKind.Lighthouse && e.Type == SimEventType.Deliver));
        }

        [Test]
        public void SettledRegion_SharedPlasticity_LimitsRotateAndMove()
        {
            var s = State(Stage.Childhood);
            var a = Conduction(s, 2, 5, 1);
            var b = Conduction(s, 6, 5, 1);
            var k = Conduction(s, 2, 4, 1);
            CollectionAssert.AreEqual(new[] { "I" }, Lifecycle.SettleRegions(s, Stage.Infant));
            Assert.IsTrue(BoardOps.Rotate(s, a.Id, 2, out _));
            Assert.IsTrue(BoardOps.Rotate(s, b.Id, 2, out _), "同一区域共用可塑度");
            Assert.IsTrue(BoardOps.Move(s, a.Id, new Cell(3, 5), out _));
            Assert.AreEqual(0, s.SettledRegions['I'].Plasticity);
            Assert.IsFalse(BoardOps.Rotate(s, b.Id, 3, out string reason));
            StringAssert.Contains("可塑度", reason);
            Assert.IsFalse(BoardOps.Move(s, k.Id, new Cell(4, 5), out _), "移入沉淀区域也要付可塑度");
            Assert.IsTrue(BoardOps.Rotate(s, k.Id, 2, out _), "K 区尚未沉淀，不受限");

            NewRound(s); Lifecycle.UpdatePlasticity(s);
            NewRound(s); Lifecycle.UpdatePlasticity(s);
            Assert.AreEqual(1, s.SettledRegions['I'].Plasticity, "连续 2 轮未被触发 +1");
        }

        [Test]
        public void SettledRegion_ThreeTriggeredRounds_Zero_MemoryTriggerCostsOne()
        {
            var s = State(Stage.Childhood);
            var m1 = Conduction(s, 2, 5, 1);
            var m2 = Conduction(s, 3, 5, 1);
            var m3 = Conduction(s, 4, 5, 1);
            SetCount(s, m1, m2, 3);
            SetCount(s, m2, m3, 3);
            Lifecycle.FoldMemories(s, Stage.Infant);
            Lifecycle.SettleRegions(s, Stage.Infant);
            var region = s.SettledRegions['I'];

            NewRound(s);
            Fire(s, m1);
            Assert.AreEqual(2, region.Plasticity, "记忆被触发一次 −1");
            Lifecycle.UpdatePlasticity(s);
            NewRound(s); Fire(s, m1); Lifecycle.UpdatePlasticity(s);
            NewRound(s); Fire(s, m1); Lifecycle.UpdatePlasticity(s);
            Assert.AreEqual(0, region.Plasticity);
            Assert.AreEqual(3, region.ConsecutiveTriggeredRounds);
        }

        static Memory FoldLine(GameState s, Stage stage, int row, int count)
        {
            var a = Conduction(s, 2, row, 1);
            var b = Conduction(s, 3, row, 1);
            var c = Conduction(s, 4, row, 1);
            SetCount(s, a, b, count);
            SetCount(s, b, c, count);
            var ids = Lifecycle.FoldMemories(s, stage);
            return s.Memories[ids.Single()];
        }

        [Test]
        public void OldAgeLighthouse_LightsDeepestMemory_WholeMemoryFlashes()
        {
            var s = State(Stage.OldAge, Cfg(lighthouse: true));
            var memory = FoldLine(s, Stage.Infant, 5, 3);
            var starter = Conduction(s, 6, 4, 0);       // → (7,3) 无格子
            Lifecycle.RefreshLighthouseFocus(s);
            Assert.AreEqual(memory.Members[0], s.LighthouseFocusGlowId);

            var r = Fire(s, starter);
            var shine = r.Events.Single(e => e.Type == SimEventType.LighthouseShine);
            CollectionAssert.AreEqual(new[] { new Cell(2, 5) }, shine.Cells, "老年灯塔只照一格");
            foreach (int id in memory.Members) Assert.AreEqual(0, FireBeat(r, s.Glows[id]), Kit.Dump(r));
            Assert.AreEqual(1, memory.TimesTriggered);
        }

        [Test]
        public void OldAgeLighthouse_DepthByTriggersThenConducts_RepicksWhenCellFails()
        {
            var s = State(Stage.OldAge, Cfg(lighthouse: true));
            var infant = FoldLine(s, Stage.Infant, 5, 3);        // 折叠时导通和 6
            var child = FoldLine(s, Stage.Childhood, 4, 1);      // 折叠时导通和 2
            Assert.AreEqual(infant.Members[0], Lifecycle.PickDeepestMemoryGlow(s), "触发次数相同，比折叠时的导通和");
            child.TimesTriggered = 5;
            Assert.AreEqual(child.Members[0], Lifecycle.PickDeepestMemoryGlow(s), "一生中被想起最多次的记忆最深");

            Lifecycle.RefreshLighthouseFocus(s);
            Assert.AreEqual(child.Members[0], s.LighthouseFocusGlowId);
            child.TimesTriggered = 0;
            Lifecycle.RefreshLighthouseFocus(s);
            Assert.AreEqual(child.Members[0], s.LighthouseFocusGlowId, "选定后一直照着它");
            foreach (int id in child.Members) s.DisabledCells.Add(s.Glows[id].Cell);
            Lifecycle.RefreshLighthouseFocus(s);
            Assert.AreEqual(infant.Members[0], s.LighthouseFocusGlowId, "那里失效后改照次深的记忆");
        }

        [Test]
        public void OldAgeLighthouse_NoMemory_LightsMostConductedGlow()
        {
            var s = State(Stage.OldAge, Cfg(lighthouse: true));
            var a = Conduction(s, 2, 5, 1);
            var b = Conduction(s, 3, 5, 1);
            var c = Conduction(s, 4, 5, 1);
            SetCount(s, a, b, 1);
            SetCount(s, b, c, 4);
            Lifecycle.RefreshLighthouseFocus(s);
            Assert.AreEqual(b.Id, s.LighthouseFocusGlowId, "b 进出导通共 5 次");
        }
    }
}
