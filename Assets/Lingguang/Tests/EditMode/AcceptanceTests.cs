using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lingguang.Core;
using NUnit.Framework;
using static Lingguang.Tests.Kit;

namespace Lingguang.Tests
{
    /// <summary>
    /// 规格第 11 节验收用例 T01–T17。坐标为 (列, 行)，odd-r；方向 0 右上、1 右、2 右下、3 左下、4 左、5 左上。
    /// 需要 C 区的用例放在青春期及之后（C 区青春期解锁）。
    /// </summary>
    public class AcceptanceTests
    {
        [Test]
        public void T01_ConductionChain_BFiresBeat1_CFiresBeat2()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);   // → (3,3)
            var b = Conduction(s, 3, 3, 0);   // → (4,2)
            var c = Conduction(s, 4, 2, 1);   // → (5,2) 隔中线，无光丝
            var r = Fire(s, a);
            Assert.AreEqual(0, FireBeat(r, a), Dump(r));
            Assert.AreEqual(1, FireBeat(r, b), Dump(r));
            Assert.AreEqual(2, FireBeat(r, c), Dump(r));
        }

        /// <summary>锥体 K(3,3) 两条入线：S1(2,4)→M1(3,4)→K，S2(2,2)→M2(2,3)→K。</summary>
        static (Glow s1, Glow s2, Glow k) ConeWithTwoPaths(GameState s)
        {
            var s1 = Conduction(s, 2, 4, 1);
            Conduction(s, 3, 4, 0);
            var s2 = Conduction(s, 2, 2, 2);
            Conduction(s, 2, 3, 1);
            var k = Put(s, ShapeKind.Cone, 3, 3, 0);
            return (s1, s2, k);
        }

        [Test]
        public void T02_Cone_OneNewEdgeDoesNotFire_TwoInSameBeatFire()
        {
            var s = State(Stage.Adolescence);
            var (s1, _, k) = ConeWithTwoPaths(s);
            var r = Fire(s, s1);
            Assert.AreEqual(-1, FireBeat(r, k), Dump(r));
            Assert.AreEqual(2, k.Charge, "只收到一条新生光丝的电 2（显示 1）");

            var s2state = State(Stage.Adolescence);
            var (a1, a2, k2) = ConeWithTwoPaths(s2state);
            var r2 = Fire(s2state, a1, a2);
            Assert.AreEqual(2, FireBeat(r2, k2), Dump(r2));
            Assert.AreEqual(2, DeliveriesTo(r2, k2).Count(d => d.Beat == 2));
        }

        [Test]
        public void T03_Converge_ThreeSourcesSameBeat_Light20Plus30()
        {
            var s = State(Stage.Adolescence);
            var v = Put(s, ShapeKind.Converge, 3, 3, 0);
            var a = Conduction(s, 3, 4, 0);   // (3,4) 方向 0 → (3,3)
            var b = Conduction(s, 2, 3, 1);   // (2,3) 方向 1 → (3,3)
            var c = Conduction(s, 4, 4, 5);   // (4,4) 方向 5 → (3,3)
            var r = Fire(s, a, b, c);
            Assert.AreEqual(1, FireBeat(r, v), Dump(r));
            Assert.AreEqual(3, r.MaxConvergeSources);
            int convergeLight = r.Events.Where(e => e.Type == SimEventType.Fire && e.GlowId == v.Id).Sum(e => e.Amount)
                                + r.Events.Where(e => e.Type == SimEventType.ScoreDelta && e.Source == "converge").Sum(e => e.LightDelta);
            Assert.AreEqual(20 + 30, convergeLight);
            Assert.AreEqual(5 * 3 + 50, s.Round.FireLight);
        }

        /// <summary>
        /// v0.4 起：电送到本轮已闪的灵光不再浪费，而是累积；疲劳让它再闪需要更多电（阈值 2 → 4）。
        /// </summary>
        [Test]
        public void T04_DeliveryToFiredGlow_Accumulates_FatigueBlocksRefire()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            Conduction(s, 3, 3, 3);           // (3,3) 方向 3 → (3,4) 回到 A
            var r = Fire(s, a);
            Assert.IsFalse(r.Events.Any(e => e.Type == SimEventType.Wasted), Dump(r));
            Assert.AreEqual(2, DeliveriesTo(r, a).Single().Amount);
            Assert.AreEqual(2, DeliveriesTo(r, a).Single().Beat);
            Assert.AreEqual(1, FireCount(r, a), "电 2 < 疲劳后的阈值 4");
            Assert.AreEqual(2, a.Charge, "连锁停下时剩下的电保留到下一次确认");
            Assert.AreEqual(4, EffectQuery.EffectiveThreshold(s, a.Id));
        }

        [Test]
        public void T05_ChargeKeptBetweenConfirms_ConeFiresOnSecond()
        {
            var s = State(Stage.Adolescence);
            var (s1, s2, k) = ConeWithTwoPaths(s);
            var r1 = Fire(s, s1);
            Assert.AreEqual(-1, FireBeat(r1, k));
            Assert.AreEqual(2, k.Charge);
            var r2 = Fire(s, s2);
            Assert.AreEqual(2, FireBeat(r2, k), Dump(r2));
        }

        [Test]
        public void T06_SixConducts_DelayZero()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);
            for (int i = 0; i < 6; i++)
            {
                NewRound(s);
                var r = Fire(s, a);
                Assert.AreEqual(1, FireBeat(r, b), "第 " + (i + 1) + " 次: " + Dump(r));
            }
            Assert.AreEqual(6, s.EdgeCount(new EdgeKey(a.Id, b.Id)));
            NewRound(s);
            var last = Fire(s, a);
            Assert.AreEqual(0, FireBeat(last, b), Dump(last));
            Assert.AreEqual(0, DeliveriesTo(last, b)[0].Beat);
        }

        [Test]
        public void T06b_TwoChainsDifferingByOne_ArriveConvergeSameBeat()
        {
            var s = State(Stage.Adolescence);
            var v = Put(s, ShapeKind.Converge, 3, 3, 0);
            var s2 = Conduction(s, 3, 4, 0);      // 长度 1：S2 → V
            var s1 = Conduction(s, 2, 2, 2);      // 长度 2：S1 → X → V，S1→X 已髓鞘化
            var x = Conduction(s, 2, 3, 1);
            SetCount(s, s1, x, 6);
            var r = Fire(s, s1, s2);
            Assert.AreEqual(0, FireBeat(r, x), Dump(r));
            var toV = DeliveriesTo(r, v);
            Assert.AreEqual(2, toV.Count);
            Assert.IsTrue(toV.All(d => d.Beat == 1), Dump(r));
            Assert.AreEqual(1, FireBeat(r, v));
        }

        [Test]
        public void T07_RotateSource_ClearsConductCount()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);
            SetCount(s, a, b, 5);
            Assert.IsTrue(BoardOps.Rotate(s, a.Id, 1, out _));
            Assert.IsFalse(s.Edges.ContainsKey(new EdgeKey(a.Id, b.Id)));
            Assert.IsTrue(BoardOps.Rotate(s, a.Id, 0, out _));
            Assert.AreEqual(0, s.EdgeCount(new EdgeKey(a.Id, b.Id)));
            var r = Fire(s, a);
            Assert.AreEqual(4, DeliveriesTo(r, b)[0].Amount, "新生光丝：输出 2 + 起点 2，没有加粗加成");
        }

        [Test]
        public void T08_OutputAcrossGroove_NoEdge()
        {
            var s = State(Stage.MiddleAge);
            var midA = Conduction(s, 4, 2, 1);    // (4,2)→(5,2)：中线（行 2 不是窄桥）
            Conduction(s, 5, 2, 0);
            var sideA = Conduction(s, 2, 3, 4);   // (2,3)→(1,3)：外侧沟（缺口只在行 2）
            Conduction(s, 1, 3, 0);
            var gapA = Conduction(s, 2, 2, 4);    // (2,2)→(1,2)：外侧沟缺口，对照组
            var gapB = Conduction(s, 1, 2, 3);

            var edges = EdgeGeometry.Enumerate(s, true, true);
            Assert.IsFalse(edges.Any(e => e.Source.Id == midA.Id));
            Assert.IsFalse(edges.Any(e => e.Source.Id == sideA.Id));
            Assert.IsTrue(edges.Any(e => e.Source.Id == gapA.Id && e.Target.Id == gapB.Id));

            var r = Fire(s, midA, sideA);
            Assert.AreEqual(0, DeliveriesFrom(r, midA).Count, Dump(r));
            Assert.AreEqual(0, DeliveriesFrom(r, sideA).Count, Dump(r));
        }

        [Test]
        public void T09_Bridge_PenaltyUntilThirdConduct()
        {
            var s = State(Stage.Infant);
            var a = Conduction(s, 4, 5, 1);       // 行 5 窄桥，婴儿起开放
            var b = Conduction(s, 5, 5, 1);
            Assert.IsTrue(s.Board.IsBridge(a.Cell, b.Cell));
            for (int i = 0; i < 3; i++)
            {
                NewRound(s);
                var r = Fire(s, a);
                var d = DeliveriesTo(r, b).Single();
                Assert.AreEqual(2, d.Beat, "延迟 +1");
                Assert.AreEqual(3, d.Amount, "输出 2 + 起点 2 − 1");
                Assert.AreEqual(2, FireBeat(r, b));
            }
            NewRound(s);
            var after = Fire(s, a);
            var d4 = DeliveriesTo(after, b).Single();
            Assert.AreEqual(1, d4.Beat);
            Assert.AreEqual(6, d4.Amount, "加粗 +2，惩罚取消");
        }

        [Test]
        public void T10_Ripple_NextBeatRadius1_DoesNotCrossGroove()
        {
            var s = State(Stage.Adolescence, Cfg(ripples: true));
            var a = Conduction(s, 3, 4, 1);       // → (4,4)
            var b = Conduction(s, 4, 4, 4);       // → (3,4) 回到 A
            var p = Put(s, ShapeKind.Cone, 4, 3, 0);   // B 的邻格，同侧
            var q = Put(s, ShapeKind.Cone, 5, 4, 0);   // B 的邻格，隔中线
            var r = Fire(s, a);
            Assert.AreEqual(1, FireBeat(r, b));
            var spawn = r.Events.Single(e => e.Type == SimEventType.RippleSpawn);
            Assert.AreEqual(1, spawn.Beat);
            Assert.AreEqual(b.Cell, spawn.Cell);
            var apply = r.Events.Single(e => e.Type == SimEventType.RippleApply);
            Assert.AreEqual(2, apply.Beat);
            Assert.AreEqual(1, apply.Radius);
            Assert.AreEqual(1, p.Charge);
            Assert.AreEqual(0, q.Charge);
        }

        static SimResult RippleGroupScene(bool third, out Glow q)
        {
            var s = State(Stage.Adolescence, Cfg(ripples: true));
            var cone = Put(s, ShapeKind.Cone, 3, 3, 0);   // 输出 → (4,2) (4,4) (2,3)
            Conduction(s, 4, 2, 1);   // → (5,2) 隔中线，无光丝
            Conduction(s, 4, 4, 2);   // → (4,5) 空格
            if (third) Conduction(s, 2, 3, 4);   // → (1,3) 外侧沟
            q = Put(s, ShapeKind.Cone, 5, 2, 0);          // 与 (4,2) 隔中线
            return Fire(s, cone);
        }

        [Test]
        public void T11_RippleMerge_TwoRadius1_ThreeRadius2CrossesGroove()
        {
            var two = RippleGroupScene(false, out var q2);
            var merge2 = two.Events.Single(e => e.Type == SimEventType.RippleMerge);
            Assert.AreEqual(2, merge2.Count);
            Assert.AreEqual(1, merge2.Radius);
            Assert.AreEqual(0, q2.Charge, "半径 1 不跨沟");

            var three = RippleGroupScene(true, out var q3);
            var merge3 = three.Events.Single(e => e.Type == SimEventType.RippleMerge);
            Assert.AreEqual(3, merge3.Count);
            Assert.AreEqual(2, merge3.Radius);
            Assert.AreEqual(1, q3.Charge, "半径 2 可跨沟");
        }

        [Test]
        public void T12_BetweenConfirms_AccumulateOnly_SettleWhenStartsRunOut()
        {
            var session = Session(Stage.Childhood);
            var s = session.State;
            var s1 = Conduction(s, 3, 4, 0);      // → (3,3) 童年未解锁
            var s2 = Conduction(s, 2, 4, 4);      // → (1,4) 无格子
            var first = session.Confirm(new[] { s1.Id });
            Assert.IsNull(first.Settlement);
            Assert.AreEqual(5, s.Round.FireLight);
            Assert.AreEqual(20, s.Round.Estimate(), "(5 + 火花 5) × (1 + 1)");
            Assert.AreEqual(1, s.Round.StartsLeft);

            var second = session.Confirm(new[] { s2.Id });
            Assert.IsNotNull(second.Settlement);
            Assert.AreEqual(60, second.Settlement.Score, "(10 + 10) × (1 + 2)，不是两次分数相加");
        }

        [Test]
        public void T13_LongChainAndDiverge_RecognizedAsLongChain()
        {
            var s = State(Stage.Adult);
            var x = Conduction(s, 3, 4, 0);
            Put(s, ShapeKind.Cone, 3, 3, 0);
            Conduction(s, 4, 2, 1);
            Conduction(s, 4, 4, 1);
            Conduction(s, 2, 3, 5);
            Conduction(s, 2, 2, 0);
            var end = Conduction(s, 2, 1, 4);
            var r = Fire(s, x);
            Assert.AreEqual(4, FireBeat(r, end), Dump(r));
            Assert.AreEqual(4, r.ChainLength);
            Assert.GreaterOrEqual(r.MaxDiverge, 3, "同时满足发散");
            Assert.AreEqual(PatternKind.LongChain, r.Pattern);
            Assert.AreEqual(1, r.Events.Count(e => e.Type == SimEventType.PatternRecognized));
        }

        [Test]
        public void T14_AdolescentPruning_ZeroEdgesStop_RotateRestores()
        {
            var s = State(Stage.Adolescence, null, 2);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 1);
            var c = Conduction(s, 2, 2, 2);
            var d = Conduction(s, 2, 3, 4);
            SetCount(s, a, b, 3);
            Lifecycle.ApplyAdolescentPruning(s);
            Assert.AreEqual(6, s.EdgeCount(new EdgeKey(a.Id, b.Id)));
            Assert.IsTrue(s.Edges[new EdgeKey(c.Id, d.Id)].Pruned);

            NewRound(s);
            var r = Fire(s, c);
            Assert.AreEqual(0, DeliveriesTo(r, d).Count, Dump(r));
            Assert.AreEqual(-1, FireBeat(r, d));

            Assert.IsTrue(BoardOps.Rotate(s, c.Id, 1, out _));
            Assert.IsTrue(BoardOps.Rotate(s, c.Id, 2, out _));
            NewRound(s);
            var r2 = Fire(s, c);
            Assert.AreEqual(1, FireBeat(r2, d), Dump(r2));
        }

        [Test]
        public void T15_Memory_AnyTwoChargeFiresAllMembersSameBeat()
        {
            var s = State(Stage.Childhood);
            var m1 = Conduction(s, 2, 5, 1);
            var m2 = Conduction(s, 3, 5, 1);
            var m3 = Conduction(s, 4, 5, 1);
            SetCount(s, m1, m2, 3);
            SetCount(s, m2, m3, 3);
            var created = Lifecycle.FoldMemories(s, Stage.Infant);
            Assert.AreEqual(1, created.Count);
            CollectionAssert.AreEquivalent(new[] { m1.Id, m2.Id, m3.Id }, s.Memories[created[0]].Members);

            var starter = Conduction(s, 2, 4, 1);  // → (3,4)
            Conduction(s, 3, 4, 2);                // → (3,5) 记忆成员
            NewRound(s);
            var r = Fire(s, starter);
            Assert.AreEqual(2, FireBeat(r, m1), Dump(r));
            Assert.AreEqual(2, FireBeat(r, m2));
            Assert.AreEqual(2, FireBeat(r, m3));
            int memoryLight = r.Events.Where(e => e.Type == SimEventType.Fire && e.Beat == 2).Sum(e => e.Amount);
            Assert.AreEqual(15, memoryLight);
        }

        static RoundSettlement MimicRound(PatternKind lastRound)
        {
            var session = Session(Stage.Adolescence, 2, "mimic");
            var s = session.State;
            var a = Conduction(s, 3, 4, 0);
            Conduction(s, 3, 3, 0);
            Conduction(s, 4, 2, 1);
            s.LastRoundTopPattern = lastRound;
            var outcome = session.Confirm(new[] { a.Id });
            Assert.AreEqual(PatternKind.ShortChain, outcome.Sim.Pattern);
            return session.EndRound();
        }

        [Test]
        public void T16_MimicGate_DifferentPattern_BonusVoided()
        {
            var voided = MimicRound(PatternKind.Spark);
            Assert.IsTrue(voided.PatternVoided);
            Assert.AreEqual(15, voided.Score, "只剩 3 次闪光的 15 光量");

            var kept = MimicRound(PatternKind.ShortChain);
            Assert.IsFalse(kept.PatternVoided);
            Assert.AreEqual(75, kept.Score, "(15 + 10) × (1 + 2)");
        }

        /// <summary>完整一局的脚本化操作：放置、旋转、确认、结束本轮，返回所有事件与结算的文本。</summary>
        public static string ScriptedRun(int seed, int maxRounds)
        {
            // 目标分调低，让脚本能走完多个阶段，覆盖涟漪、记忆、关口、本能、老年衰退等全部随机来源。
            var cfg = GameConfig.CreateDefault();
            foreach (var st in cfg.Stages) st.Targets = new[] { 1, 1, 1 };
            var session = GameSession.NewRun(cfg, seed, new RunOptions { SeparationUnlocked = true });
            var log = new StringBuilder();
            int rounds = 0;
            while (!session.State.RunOver && rounds < maxRounds)
            {
                var s = session.State;
                if (s.Round.PlacementAvailable && s.Round.Candidates.Count > 0)
                {
                    var cell = s.Board.Cells.FirstOrDefault(c => BoardOps.CanPlace(s, c));
                    if (session.Place(rounds % s.Round.Candidates.Count, cell, rounds % 6, out _))
                    {
                        int last = s.NextGlowId - 1;
                        session.Rotate(last, (rounds + 2) % 6, out _);
                    }
                }

                RoundSettlement settled = null;
                while (settled == null)
                {
                    s = session.State;
                    var starter = s.Glows.Values.FirstOrDefault(g => !g.Fired && s.IsCellActive(g.Cell));
                    if (starter == null) { settled = session.EndRound(); break; }
                    var outcome = session.Confirm(new[] { starter.Id });
                    foreach (var e in outcome.Sim.Events) log.AppendLine(e.ToString());
                    settled = outcome.Settlement;
                }
                log.AppendLine("SETTLE " + settled.Stage + "/" + settled.RoundInStage + " score=" + settled.Score + " passed=" + settled.Passed);
                rounds++;
            }
            return log.ToString();
        }

        [Test]
        public void T17_SameSeedSameOps_100Runs_IdenticalEvents()
        {
            string first = ScriptedRun(20261001, 18);
            Assert.That(first.Length, Is.GreaterThan(0));
            for (int i = 1; i < 100; i++)
                Assert.AreEqual(first, ScriptedRun(20261001, 18), "第 " + (i + 1) + " 次与第 1 次不一致");
        }
    }
}
