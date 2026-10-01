using System;
using System.Linq;
using Lingguang.Core;
using NUnit.Framework;
using static Lingguang.Tests.Kit;

namespace Lingguang.Tests
{
    /// <summary>设计决策记录“电量流失与再次闪光”与规则三“扰动 → 稳定”（原型规格 v0.4/v0.5）的回归测试。</summary>
    public class RuleV05Tests
    {
        [Test]
        public void Reverberation_RingWithThickEdges_RefiresUntilFatigueStopsIt()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);   // → (3,3)
            var b = Conduction(s, 3, 3, 3);   // → (3,4)
            SetCount(s, a, b, 3);
            SetCount(s, b, a, 3);
            var r = Fire(s, a);
            var aBeats = r.Events.Where(e => e.Type == SimEventType.Fire && e.GlowId == a.Id).Select(e => e.Beat).ToArray();
            var bBeats = r.Events.Where(e => e.Type == SimEventType.Fire && e.GlowId == b.Id).Select(e => e.Beat).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 2 }, aBeats, Dump(r));
            CollectionAssert.AreEqual(new[] { 1, 3 }, bBeats, Dump(r));
            Assert.AreEqual(2, a.FireCount);
            Assert.AreEqual(5, DeliveriesTo(r, a).Last().Amount, "输出 2 + 加粗 2 + 扰动 1，但疲劳后阈值已是 6");
            Assert.AreEqual(4, r.Events.Count(e => e.Type == SimEventType.Fire));
        }

        [Test]
        public void Perturb_EachSendAddsOne_CappedAtTwo_ResetsNextRound()
        {
            var cfg = Cfg();
            cfg.AllowFiredStarters = true;    // 同一轮里反复从 A 发电，观察同一条光丝的扰动
            var s = State(Stage.Adolescence, cfg);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 0);   // → (4,2) 空格
            var perturbs = Enumerable.Range(0, 4).Select(_ => DeliveriesTo(Fire(s, a), b).Single().Perturb).ToArray();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 2 }, perturbs, "每传一次 +1，上限 2");

            NewRound(s);
            Assert.AreEqual(0, DeliveriesTo(Fire(s, a), b).Single().Perturb, "新一轮扰动回落");
        }

        /// <summary>
        /// 记录一个由规则推出的现象（与参考模型 sim.py 一致）：完全髓鞘化的两点环不会回荡——
        /// 回传的电当拍到达，而同一拍内不能再闪，电停在阈值上直到下一次被点亮。
        /// </summary>
        [Test]
        public void MyelinatedPair_EchoArrivesSameBeat_IsSwallowed()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 3);
            SetCount(s, a, b, 6);
            SetCount(s, b, a, 6);
            var r = Fire(s, a);
            Assert.AreEqual(2, r.Events.Count(e => e.Type == SimEventType.Fire), Dump(r));
            Assert.AreEqual(0, r.EndBeat);
            Assert.AreEqual(4, a.Charge, "电量已达疲劳后的阈值 4，但本拍不能再闪");
        }

        [Test]
        public void Leak_SameBeatLightsCone_StaggeredArrivalDoesNot()
        {
            GameState Scene(GameConfig cfg, out Glow s1, out Glow s2, out Glow k)
            {
                var s = State(Stage.Adolescence, cfg);
                s1 = Conduction(s, 2, 4, 1);
                var m1 = Conduction(s, 3, 4, 0);
                s2 = Conduction(s, 2, 2, 2);
                Conduction(s, 2, 3, 1);
                k = Put(s, ShapeKind.Cone, 3, 3, 0);
                SetCount(s, s1, m1, 6);       // 上路髓鞘化：电提前一拍到达锥体
                return s;
            }

            var leaky = Scene(Cfg(), out var a1, out var a2, out var k1);
            var r1 = Fire(leaky, a1, a2);
            CollectionAssert.AreEqual(new[] { 1, 2 }, DeliveriesTo(r1, k1).Select(d => d.Beat).ToArray());
            Assert.AreEqual(-1, FireBeat(r1, k1), "隔拍到达：2 − 漏电 1 + 2 = 3 < 4\n" + Dump(r1));

            var cfg = Cfg();
            cfg.LeakPerBeat = 0;
            var noLeak = Scene(cfg, out var b1, out var b2, out var k2);
            Assert.AreEqual(2, FireBeat(Fire(noLeak, b1, b2), k2), "关掉漏电则隔拍也能累积");
        }

        [Test]
        public void SameBeat_FiresAtMostOnce()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var b = Conduction(s, 3, 3, 3);
            SetCount(s, a, b, 6);
            SetCount(s, b, a, 6);             // 两条光丝都当拍到达
            var r = Fire(s, a);
            var perBeat = r.Events.Where(e => e.Type == SimEventType.Fire).GroupBy(e => (e.GlowId, e.Beat));
            Assert.IsTrue(perBeat.All(grp => grp.Count() == 1), Dump(r));
            Assert.AreEqual(1, FireBeat(r, a) == -1 ? 1 : r.Events.Count(e => e.Type == SimEventType.Fire && e.GlowId == a.Id && e.Beat == 0));
        }

        [Test]
        public void StoredCharge_Kept_ByDefault_ReferenceModelLosesIt()
        {
            // 默认：T05 成立（第一次确认留下的 2，在第二次确认里被点亮前不流失）。
            var keep = State(Stage.Adolescence);
            var s1 = Conduction(keep, 2, 4, 1); Conduction(keep, 3, 4, 0);
            var s2 = Conduction(keep, 2, 2, 2); Conduction(keep, 2, 3, 1);
            var k = Put(keep, ShapeKind.Cone, 3, 3, 0);
            Fire(keep, s1);
            Assert.AreEqual(2, k.Charge);
            Assert.AreEqual(2, FireBeat(Fire(keep, s2), k));

            // 参考模型 sim.py 原版语义：所有电每拍都流失，最后一拍也流失 → 留下的电在下一次确认里耗尽。
            var cfg = Cfg();
            cfg.LeakStoredCharge = true;
            cfg.LeakAfterLastBeat = true;
            var lose = State(Stage.Adolescence, cfg);
            var t1 = Conduction(lose, 2, 4, 1); Conduction(lose, 3, 4, 0);
            var t2 = Conduction(lose, 2, 2, 2); Conduction(lose, 2, 3, 1);
            var k2 = Put(lose, ShapeKind.Cone, 3, 3, 0);
            Fire(lose, t1);
            Assert.AreEqual(1, k2.Charge);
            Assert.AreEqual(-1, FireBeat(Fire(lose, t2), k2));
        }

        [Test]
        public void FiredStarter_RejectedBySpec_AllowedBySwitch()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            Fire(s, a);
            Assert.Throws<InvalidOperationException>(() => Fire(s, a));

            var cfg = Cfg();
            cfg.AllowFiredStarters = true;
            var t = State(Stage.Adolescence, cfg);
            var b = Conduction(t, 3, 4, 0);
            Fire(t, b);
            Fire(t, b);
            Assert.AreEqual(2, b.FireCount);
        }

        [Test]
        public void BeatEnd_ReportsChargeAfterLeak()
        {
            var s = State(Stage.Adolescence);
            var a = Conduction(s, 3, 4, 0);
            var k = Put(s, ShapeKind.Cone, 3, 3, 0);
            Conduction(s, 4, 2, 1);           // 让连锁多走一拍，便于观察
            SetCount(s, a, k, 0);
            var r = Fire(s, a);
            var end1 = r.Events.First(e => e.Type == SimEventType.BeatEnd && e.Beat == 1);
            Assert.AreEqual(0, end1.Cells.Count, "锥体收到起点的 4 已闪，电量清零：\n" + Dump(r));
            Assert.AreEqual(r.EndBeat, r.Events.Last(e => e.Type == SimEventType.BeatEnd).Beat);
        }
    }
}
