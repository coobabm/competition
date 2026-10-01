using System.Linq;
using Lingguang.Core;
using NUnit.Framework;
using static Lingguang.Tests.Kit;

namespace Lingguang.Tests
{
    /// <summary>
    /// 规格 v0.5/v0.6 第 11 节新增验收用例 T18–T21、T23，以及 v0.6 第 6 节的微弱连接。T22（流光漂远）属于第 12 节 P1 内容，流光实现后再加。
    /// T04、T05 的 v0.4 改写版在 AcceptanceTests 与 RuleV05Tests 中。
    /// </summary>
    public class AcceptanceV05Tests
    {
        [Test]
        public void T18_Leak_ConeAdjacentBeats_Loses1_SameBeatFires()
        {
            // 上路 S1→M1 髓鞘化，电比下路早一拍到达锥体 K。
            var s = State(Stage.Adolescence);
            var s1 = Conduction(s, 2, 4, 1);
            var m1 = Conduction(s, 3, 4, 0);
            var s2 = Conduction(s, 2, 2, 2);
            Conduction(s, 2, 3, 1);
            var k = Put(s, ShapeKind.Cone, 3, 3, 0);
            SetCount(s, s1, m1, 6);
            var r = Fire(s, s1, s2);
            CollectionAssert.AreEqual(new[] { 1, 2 }, DeliveriesTo(r, k).Select(d => d.Beat).ToArray());
            var afterBeat1 = r.Events.First(e => e.Type == SimEventType.BeatEnd && e.Beat == 1);
            Assert.AreEqual(1, afterBeat1.Values[afterBeat1.Cells.IndexOf(k.Cell)], "第一拍后流失 1");
            Assert.AreEqual(-1, FireBeat(r, k), "累计 3 < 4，不闪");

            var same = State(Stage.Adolescence);
            var t1 = Conduction(same, 2, 4, 1); Conduction(same, 3, 4, 0);
            var t2 = Conduction(same, 2, 2, 2); Conduction(same, 2, 3, 1);
            var k2 = Put(same, ShapeKind.Cone, 3, 3, 0);
            Assert.AreEqual(2, FireBeat(Fire(same, t1, t2), k2), "同拍到达则闪");
        }

        [Test]
        public void T19_TwoConductionsFacingEachOther_ReverberateThenStop()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 3);
            SetCount(s, a, b, 3);     // 回荡是“学会”的：新生光丝的回传不够点亮疲劳的灵光，环要先加粗
            SetCount(s, b, a, 3);
            var r = Fire(s, a);
            Assert.IsTrue(r.Reverberated, Dump(r));
            Assert.AreEqual(2, r.MaxFiresPerUnit);
            Assert.IsFalse(r.HitBeatLimit);
            Assert.Less(r.EndBeat, 10, "因阈值抬升与流失逐渐停止");

            var fresh = State(Stage.Adolescence);
            var c = Conduction(fresh, 3, 4, 0);
            Conduction(fresh, 3, 3, 3);
            Assert.IsFalse(Fire(fresh, c).Reverberated, "新生的环不回荡");
        }

        [Test]
        public void T20_TwoConfirmsSameNewEdge_SecondDeliveryPlusOne_ResetsNextRound()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);       // A → B(3,3)
            var b = Conduction(s, 3, 3, 0);       // → (4,2) 空格
            var c = Conduction(s, 2, 4, 1);       // C → A
            var first = DeliveriesTo(Fire(s, a), b).Single();
            Assert.AreEqual(0, first.Perturb);
            Assert.AreEqual(4, first.Amount);
            var second = DeliveriesTo(Fire(s, c), b).Single();   // C 让 A 再次闪（阈值 4，收到 4）
            Assert.AreEqual(1, second.Perturb);
            Assert.AreEqual(3, second.Amount, "输出 2 + 扰动 1");
            Assert.AreEqual(1, s.EdgeCount(new EdgeKey(a.Id, b.Id)), "B 已疲劳（阈值 4），3 不够让它再闪：第二次只有扰动、没有稳定");

            NewRound(s);
            Assert.AreEqual(0, DeliveriesTo(Fire(s, a), b).Single().Perturb, "下一轮恢复");
        }

        [Test]
        public void T21_Unanchored_DriftsAfterRooting_InstinctAndMemoryExempt()
        {
            var s = State(Stage.Childhood);
            var lone = Conduction(s, 6, 4, 0);                 // 指向 (7,3) 无格子：永远没有光丝
            var instinct = BoardOps.AddGlow(s, ShapeKind.Instinct, new Cell(6, 5), 0, true);
            var m1 = Conduction(s, 2, 5, 1);
            var m2 = Conduction(s, 3, 5, 1);
            var m3 = Conduction(s, 4, 5, 1);
            SetCount(s, m1, m2, 3);
            SetCount(s, m2, m3, 3);
            Lifecycle.FoldMemories(s, Stage.Infant);
            foreach (var key in s.Edges.Keys.ToList()) s.Edges[key].Count = 0;   // 记忆成员也“无依”，但不适用

            for (int round = 1; round <= 4; round++)
            {
                var st = new RoundSettlement();
                Lifecycle.ApplyUnanchored(s, st);
                Assert.IsTrue(s.Glows.ContainsKey(lone.Id), "第 " + round + " 轮还在（扎根期 3 轮 + 无依第 1 轮）");
                Assert.AreEqual(0, st.DriftedGlowIds.Count);
            }
            var drift = new RoundSettlement();
            Lifecycle.ApplyUnanchored(s, drift);
            CollectionAssert.AreEqual(new[] { lone.Id }, drift.DriftedGlowIds, "扎根期后连续 2 轮无依：漂走");
            Assert.IsFalse(s.CellGlow.ContainsKey(new Cell(6, 4)), "格子空出");
            Assert.IsTrue(s.Glows.ContainsKey(instinct.Id));
            Assert.IsTrue(s.Glows.ContainsKey(m1.Id) && s.Glows.ContainsKey(m2.Id) && s.Glows.ContainsKey(m3.Id));
        }

        [Test]
        public void T21b_AnyConductedEdge_InOrOut_KeepsGlowAnchored()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);        // 只有指向它的光丝
            SetCount(s, a, b, 1);
            for (int i = 0; i < 6; i++) Lifecycle.ApplyUnanchored(s);
            Assert.IsTrue(s.Glows.ContainsKey(a.Id) && s.Glows.ContainsKey(b.Id));
            Assert.AreEqual(0, b.UnanchoredRounds);
        }

        /// <summary>T23 的 Unity 一侧：规则基准测试参考模型与 C# 实现逐拍一致（见 ReferenceConsistencyTests）；bench.py 本身在 Python 中运行。</summary>
        [Test]
        public void T23_ReferenceModelConsistency()
        {
            new ReferenceConsistencyTests().CSharpSimulator_MatchesReferenceModel_OnRealBoard();
        }

        /// <summary>规格 v0.6 采纳的三处默认值，防止以后被无意改回。</summary>
        [Test]
        public void V06_Defaults_MatchSpec()
        {
            var cfg = GameConfig.CreateDefault();
            Assert.IsFalse(cfg.LeakStoredCharge, "漏电只作用于本次连锁中收到过电的灵光");
            Assert.IsFalse(cfg.LeakAfterLastBeat, "连锁停下时剩下的电留到下一次确认（T05）");
            Assert.IsFalse(cfg.RippleReachesFired, "涟漪只给本轮还没闪过的灵光");
            Assert.IsTrue(cfg.WeakLinksEnabled);
            Assert.AreEqual(3, cfg.WeakLinkMinStrength);
            Assert.AreEqual(1, cfg.WeakLinkDecayPerRound);
            Assert.IsFalse(cfg.WeakLinkToFiredPartner);
            Assert.AreEqual(1, cfg.LeakPerBeat);
            Assert.AreEqual(2, cfg.RefireThresholdStep);
            Assert.AreEqual(1, cfg.PerturbStep);
            Assert.AreEqual(2, cfg.PerturbCap);
            Assert.AreEqual(3, cfg.RootingRounds);
            Assert.AreEqual(2, cfg.UnanchoredRoundsToDrift);
        }

        [Test]
        public void WeakLink_ObeysRule3_Strength3_DecaysEachRound_OnlyToUnfired()
        {
            var s = State(Stage.Adolescence, Cfg(ripples: true));
            var a = Conduction(s, 3, 4, 4);          // → (2,4) 空格
            var b = Conduction(s, 6, 4, 0);          // → (7,3) 无格子；与 A 之间没有光丝
            WeakLinks.Increment(s, a.Id, b.Id);
            WeakLinks.Increment(s, a.Id, b.Id);
            var weak = Fire(s, a);
            Assert.IsFalse(weak.Events.Any(e => e.Kind == DeliveryKind.WeakLink && e.Type == SimEventType.Deliver), "强度 2 还不起作用");

            WeakLinks.Increment(s, a.Id, b.Id);
            NewRound(s);
            var r = Fire(s, a);
            var pulse = r.Events.Single(e => e.Type == SimEventType.Deliver && e.Kind == DeliveryKind.WeakLink);
            Assert.AreEqual(b.Id, pulse.GlowId);
            Assert.AreEqual(1, pulse.Beat, "下一拍");
            Assert.AreEqual(1, pulse.Amount, "+1（0.5）");

            NewRound(s);
            var both = Fire(s, a, b);
            Assert.IsFalse(both.Events.Any(e => e.Kind == DeliveryKind.WeakLink && e.Type == SimEventType.Deliver), "另一方已闪过则不送");

            Lifecycle.ApplyDisuse(s);
            Assert.AreEqual(2, WeakLinks.Strength(s, a.Id, b.Id), "每轮结算减弱 1");
        }

        /// <summary>规格 v0.4 原文的涟漪语义（已闪灵光也收涟漪）仍可通过配置打开，用于设计对比。</summary>
        [Test]
        public void RippleReachesFired_Switch()
        {
            var cfg = Cfg(ripples: true);
            cfg.RippleReachesFired = true;
            var s = State(Stage.Adolescence, cfg);
            var a = Conduction(s, 3, 4, 1);         // → B(4,4)
            Conduction(s, 4, 4, 1);                 // → (5,4) 中线，无光丝
            Fire(s, a);
            Assert.AreEqual(1, a.Charge, "已闪的 A 也收到涟漪 +1");

            var off = State(Stage.Adolescence, Cfg(ripples: true));
            var a2 = Conduction(off, 3, 4, 1);
            Conduction(off, 4, 4, 1);
            Fire(off, a2);
            Assert.AreEqual(0, a2.Charge, "默认：只给本轮未闪的灵光");
        }
    }
}
