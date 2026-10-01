using System;
using System.Collections.Generic;
using System.Linq;
using Lingguang.Core;
using NUnit.Framework;

namespace Lingguang.Tests
{
    /// <summary>
    /// 规则基准测试 bench.py 前 6 项的 C# 版，跑在真实棋盘上，并能打开涟漪、灯塔等完整规则，
    /// 看“加了涟漪之后涌现现象还在不在、会不会失控”。属于调参工具，不进常规测试：
    /// dotnet test --filter EmergenceBench --logger "console;verbosity=detailed"
    /// Unity 里在 Test Runner 中选中后单独运行（Explicit）。
    /// </summary>
    [TestFixture, Explicit("调参用基准，单独运行")]
    public class EmergenceBench
    {
        public sealed class Stats
        {
            public int Grew, Recruit, Drop, Reverb, ReverbLate, MaxRepeat, MaxBeat, BeatLimitHits, Networks;
            public double FiresPerRound;
            public override string ToString() =>
                $"回荡 {Reverb}/{Networks}（第 3 轮后出现 {ReverbLate}）｜成长 {Grew}/{Networks}｜后期唤醒 {Recruit}/{Networks}｜旧路落下 {Drop}/{Networks}｜" +
                $"单灵光一轮最多闪 {MaxRepeat} 次｜最长 {MaxBeat} 拍｜触顶 {BeatLimitHits}｜平均每轮闪 {FiresPerRound:0.0} 次";
        }

        static readonly ShapeKind[] Shapes = { ShapeKind.Conduction, ShapeKind.Cone, ShapeKind.Converge };

        static GameState RandomNetwork(GameConfig cfg, int seed, double fill, int[] weights)
        {
            var s = Kit.State(Stage.MiddleAge, cfg);
            var rng = new Rng(seed);
            int total = weights.Sum();
            foreach (var c in s.Board.Cells)
            {
                if (rng.Next(1000) >= fill * 1000) continue;
                int pick = rng.Next(total), k = 0;
                while (pick >= weights[k]) { pick -= weights[k]; k++; }
                BoardOps.AddGlow(s, Shapes[k], c, rng.Next(6));
            }
            return s;
        }

        static List<int> TopByOutDegree(GameState s, int n)
        {
            var degree = s.Glows.Keys.ToDictionary(id => id, id => 0);
            foreach (var e in EdgeGeometry.Enumerate(s, false, false)) degree[e.Source.Id]++;
            return degree.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(n).Select(kv => kv.Key).ToList();
        }

        public static Stats Run(GameConfig cfg, double fill, int[] weights, int nStart, int networks = 60, int rounds = 30, bool fixedStart = false)
        {
            var st = new Stats { Networks = networks };
            long fires = 0;
            for (int seed = 0; seed < networks; seed++)
            {
                var s = RandomNetwork(cfg, seed, fill, weights);
                if (s.Glows.Count < 3) continue;
                var pick = new Rng(seed + 999);
                var pool = TopByOutDegree(s, 3);
                var history = new List<Dictionary<int, int>>();
                var totals = new List<int>();
                for (int r = 0; r < rounds; r++)
                {
                    Kit.NewRound(s);
                    var starters = fixedStart ? new List<int> { pool[0] } : pool.OrderBy(_ => pick.Next(1 << 20)).Take(nStart).ToList();
                    var result = Simulator.Confirm(s, starters);
                    var perGlow = new Dictionary<int, int>();
                    foreach (var e in result.Events)
                        if (e.Type == SimEventType.Fire) { perGlow.TryGetValue(e.GlowId, out int c); perGlow[e.GlowId] = c + 1; }
                    Lifecycle.ApplyDisuse(s);
                    history.Add(perGlow);
                    totals.Add(perGlow.Values.Sum());
                    fires += perGlow.Values.Sum();
                    st.MaxRepeat = Math.Max(st.MaxRepeat, perGlow.Values.DefaultIfEmpty(0).Max());
                    st.MaxBeat = Math.Max(st.MaxBeat, result.EndBeat);
                    if (result.HitBeatLimit) st.BeatLimitHits++;
                }
                var early = new HashSet<int>(history.Take(5).SelectMany(h => h.Keys));
                var late = new HashSet<int>(history.Skip(rounds - 5).SelectMany(h => h.Keys));
                if (totals.Skip(rounds - 5).Sum() > totals.Take(5).Sum()) st.Grew++;
                if (late.Except(early).Any()) st.Recruit++;
                if (early.Except(late).Any()) st.Drop++;
                int firstReverb = history.FindIndex(h => h.Values.Any(v => v > 1));
                if (firstReverb >= 0) { st.Reverb++; if (firstReverb >= 3) st.ReverbLate++; }
            }
            st.FiresPerRound = fires / (double)(networks * rounds);
            return st;
        }

        static void Report(string name, GameConfig cfg)
        {
            var dense = Run(cfg, 0.85, new[] { 6, 3, 1 }, 2);
            var sparse = Run(cfg, 0.7, new[] { 5, 3, 1 }, 1);
            var fixedStart = Run(cfg, 0.7, new[] { 5, 3, 1 }, 1, 40, 30, true);
            TestContext.WriteLine("【" + name + "】 涌现检查 " + Verdict(dense, sparse, fixedStart));
            TestContext.WriteLine("  稠密、起点轮换：" + dense);
            TestContext.WriteLine("  稀疏：旧路落下 " + sparse.Drop + "/60，最长 " + sparse.MaxBeat + " 拍");
            TestContext.WriteLine("  固定起点：成长 " + fixedStart.Grew + "/40");
            Assert.AreEqual(0, dense.BeatLimitHits + sparse.BeatLimitHits, name + "：出现触顶 200 拍的失控连锁");
        }

        [Test]
        public void EmergenceBench_ThreeRulesOnly()
        {
            Report("只有三条底层规则（对应 sim.py）", ReferenceConsistencyTests.ReferenceConfig());
        }

        [Test]
        public void EmergenceBench_WithRipplesAndWeakLinks()
        {
            var cfg = ReferenceConsistencyTests.ReferenceConfig();
            var defaults = GameConfig.CreateDefault();
            cfg.RippleCharge = defaults.RippleCharge;
            cfg.WeakLinksEnabled = defaults.WeakLinksEnabled;
            cfg.WeakLinkCharge = defaults.WeakLinkCharge;
            Report("加上涟漪与微弱连接", cfg);
        }

        [Test]
        public void EmergenceBench_RipplesOnly()
        {
            var cfg = ReferenceConsistencyTests.ReferenceConfig();
            cfg.RippleCharge = GameConfig.CreateDefault().RippleCharge;
            Report("只加涟漪（微弱连接关）", cfg);
        }

        static GameConfig WithWeakLinks(int minStrength, int decay)
        {
            var cfg = ReferenceConsistencyTests.ReferenceConfig();
            var d = GameConfig.CreateDefault();
            cfg.RippleCharge = d.RippleCharge;
            cfg.WeakLinksEnabled = true;
            cfg.WeakLinkCharge = d.WeakLinkCharge;
            cfg.WeakLinkMinStrength = minStrength;
            cfg.WeakLinkDecayPerRound = decay;
            return cfg;
        }

        [Test]
        public void EmergenceBench_SpecLiteralWeakLinks()
        {
            Report("涟漪 + 微弱连接，按规格 v0.5 原文（形成即生效、永不减弱）", WithWeakLinks(1, 0));
        }

        [TestCase(3, 1), TestCase(2, 1), TestCase(3, 2), TestCase(1, 1)]
        public void EmergenceBench_WeakLinkVariants(int minStrength, int decay)
        {
            Report("涟漪 + 微弱连接：强度 ≥ " + minStrength + " 才生效，每轮减弱 " + decay, WithWeakLinks(minStrength, decay));
        }

        /// <summary>6 项检查是否通过（门槛与 bench.py 相同）。</summary>
        public static string Verdict(Stats dense, Stats sparse, Stats fixedStart)
        {
            var ok = new[]
            {
                dense.Reverb >= 30 && dense.ReverbLate >= 0.7 * dense.Reverb,
                dense.Grew >= 30,
                dense.Recruit >= 10,
                sparse.Drop >= 5,
                fixedStart.Grew <= 5,
                dense.MaxRepeat <= 6 && dense.MaxBeat <= 30,
            };
            return ok.Count(x => x) + "/6 [" + string.Concat(ok.Select(x => x ? "✓" : "✗")) + "]";
        }

        [Test]
        public void EmergenceBench_Grid()
        {
            var lines = new List<string>();
            foreach (bool rippleFired in new[] { true, false })
            foreach (var (name, on, min, decay, toFired) in new[]
            {
                ("微弱连接关", false, 1, 0, true),
                ("规格原文", true, 1, 0, true),
                ("≥3 减1 送已闪", true, 3, 1, true),
                ("≥3 减1 只送未闪", true, 3, 1, false),
                ("≥3 减2 只送未闪", true, 3, 2, false),
            })
            {
                var cfg = WithWeakLinks(min, decay);
                cfg.WeakLinksEnabled = on;
                cfg.WeakLinkToFiredPartner = toFired;
                cfg.RippleReachesFired = rippleFired;
                var dense = Run(cfg, 0.85, new[] { 6, 3, 1 }, 2);
                var sparse = Run(cfg, 0.7, new[] { 5, 3, 1 }, 1);
                var fixedStart = Run(cfg, 0.7, new[] { 5, 3, 1 }, 1, 40, 30, true);
                lines.Add((rippleFired ? "涟漪给已闪" : "涟漪只给未闪") + "｜" + name + "｜" + Verdict(dense, sparse, fixedStart)
                          + $"｜每轮闪 {dense.FiresPerRound:0.0}｜单灵光最多 {dense.MaxRepeat}｜最长 {dense.MaxBeat} 拍｜回荡 {dense.Reverb}（后 {dense.ReverbLate}）｜固定起点成长 {fixedStart.Grew}/40");
            }
            foreach (var l in lines) TestContext.WriteLine(l);
        }

        [Test]
        public void EmergenceBench_FullGameRules()
        {
            var cfg = GameConfig.CreateDefault();   // 涟漪、微弱连接、灯塔、窄桥惩罚全开
            Report("完整规则（中年阶段）", cfg);
        }
    }
}
