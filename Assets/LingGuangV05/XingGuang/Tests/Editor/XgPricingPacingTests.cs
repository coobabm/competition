using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 定价重做（小丑牌式，2026-10-24）的节奏检查：用真实 <see cref="XgSim"/> 的报价、答题收入（SettleMiniAction）、众包档位、
    /// 自动答题和购买接口，模拟一个「典型玩家」从开局走到第 4 阶段结束。玩家每 4 秒答一题、正确率 85%；
    /// 一边攒钱买本阶段的主线（<see cref="Route"/>），一边顺手买不超过主线价格 30% 的便宜东西（加薪、自动答题、小科技）。
    /// 阶段推进是固定夹具：主线买齐（第 1 阶段还要学会是/否）就直接让下一个能力出现，不模拟训练曲线本身。
    /// 目标：开局第一次购买 ≤ 2 分钟，第 1–4 阶段任意两次购买间隔 ≤ 3 分钟；第 4 阶段自动答题收入 ≈ 手动的 1–1.5 倍。
    /// 数字写进日志（[Pacing] 开头），供 Design/定价重做_小丑牌式_20261024.md 记录前后对比。
    /// </summary>
    public sealed class XgPricingPacingTests
    {
        public const double SecondsPerAnswer = 4, Accuracy = .85, SideShare = .3, EpochSeconds = 6;
        public const double FirstPurchaseLimit = 120, PurchaseGapLimit = 180;
        /// <summary>Rent from ability 3: ¥2100 a story month, about an hour of play (30 days × 120 s).</summary>
        public const double RentPerSecond = 2100.0 / 3600;

        /// <summary>The stage's main line (XgSim.NextStudyNode's route and pack): saving goes to these first.</summary>
        static readonly string[][] Route =
        {
            null,
            new[] { "train.curve", "spam.pack", "mlp", "s.d2", "s.w1", "s.w2" },
            new[] { "mnist.pack", "rnn", "s.d3", "s.w3" },
            new[] { "poems.pack", "lstm", "s.d4", "s.w4" },
            new[] { "logic.pack", "seq2seq", "attention", "s.d6", "s.w5" },
        };
        /// <summary>Desks with a ≥ 60% checkpoint by each stage (the fixture's stand-in for trained models).</summary>
        static readonly string[] CheckpointDesks = { "sense", "spam", "mnist", "danmu", "headline", "logic", "poems", "review" };

        sealed class Wallet : IXgHost
        {
            public double money, compute = 1;
            public double Compute => compute;
            public double VramMB => 1000000;
            public double Money => money;
            public bool Spend(double amount) { if (money + 1e-9 < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
            public string Blocker => null;
        }

        public sealed class StageReport
        {
            public int stage, buys;
            public double start, end, firstBuy = -1, maxGap, manual, crowd, auto, rent, spent;
            public int autoLevel, raiseLevel;
            public double ManualPerMin => Minutes > 0 ? manual / Minutes : 0;
            public double AutoPerMin => Minutes > 0 ? auto / Minutes : 0;
            public double CrowdPerMin => Minutes > 0 ? crowd / Minutes : 0;
            public double Minutes => (end - start) / 60;
            public readonly List<string> purchases = new List<string>();
        }

        public sealed class Result
        {
            public readonly List<StageReport> stages = new List<StageReport>();
            public double firstPurchase = -1, stageFourAutoRatio;
            public string stuck = "";
        }

        static string F(double v, string f = "0.#") => v.ToString(f, CultureInfo.InvariantCulture);

        static readonly MethodInfo Emerge = typeof(XgSim).GetMethod("EmergeAbility", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo PayResearch = typeof(XgSim).GetMethod("PayAbilityResearch", BindingFlags.Instance | BindingFlags.NonPublic);

        static double Price(XgSim sim, XgNode n) => n.tree == "label" ? sim.LabelNodeCost(n) : sim.NodeCost(n);

        /// <summary>The first unowned node on the way to the stage's next main-line item (parents first).</summary>
        static XgNode Goal(XgSim sim, int stage)
        {
            if (stage < 1 || stage >= Route.Length) return null;
            foreach (var id in Route[stage])
            {
                var n = XgCatalog.Node(id);
                if (n == null || sim.Has(id)) continue;
                return FirstMissing(sim, n, new HashSet<string>()) ?? n;
            }
            return null;
        }

        static XgNode FirstMissing(XgSim sim, XgNode n, HashSet<string> seen)
        {
            if (n == null || sim.Has(n.id) || !seen.Add(n.id)) return null;
            if (!string.IsNullOrEmpty(n.parent)) { var p = FirstMissing(sim, XgCatalog.Node(n.parent), seen); if (p != null) return p; }
            foreach (var need in n.needs) { var p = FirstMissing(sim, XgCatalog.Node(need), seen); if (p != null) return p; }
            return n;
        }

        static string PickDesk(XgSim sim)
        {
            if (sim.OpeningFocus || sim.S.teachingFlow && !sim.Has("train.curve")) return XgMemes.SenseDesk;
            var open = XgCatalog.Desks.Where(d => sim.DeskOpen(d.id)).OrderBy(d => d.pay).ThenBy(d => d.id, StringComparer.Ordinal).ToList();
            return open.Count == 0 ? XgMemes.SenseDesk : open[open.Count / 2].id;
        }

        static void EnterStageFixtures(XgSim sim, Wallet host, int stage)
        {
            if (stage < 2) return;
            host.compute = 2;
            var crowd = sim.AppReceipt("zhongbao"); if (crowd != null) crowd.installed = true;
            sim.S.epiphany = sim.S.autoLabelRevealed = true;
            int desks = stage; // two trained desks at stage 2, three at 3, four at 4
            foreach (var id in CheckpointDesks)
            {
                if (desks <= 0) break;
                if (!sim.DeskOpen(id) || sim.BestAcc(id) >= .8) { if (sim.BestAcc(id) >= .8) desks--; continue; }
                sim.S.best.Add(new XgBest { dataset = id, acc = .8 });
                desks--;
            }
            PayResearch?.Invoke(sim, new object[] { host });
        }

        public static Result Simulate(double limitMinutes = 120)
        {
            var result = new Result();
            var sim = new XgSim { GoldChance = 0 };
            sim.BeginOpening();
            var host = new Wallet();
            var rng = new System.Random(20261024);
            var skip = new HashSet<string>();
            double t = 0, lastBuy = 0;
            int answers = 0;
            var report = new StageReport { stage = 1, start = 0 };
            while (t < limitMinutes * 60)
            {
                // One answer every SecondsPerAnswer; the streak runs out between answers like the real timer.
                sim.S.comboTimer -= SecondsPerAnswer;
                if (sim.S.comboTimer <= 0) { sim.S.comboTimer = 0; sim.S.combo = 0; }
                string desk = PickDesk(sim);
                double before = host.money;
                sim.SettleMiniAction(desk, rng.NextDouble() < Accuracy, host, SecondsPerAnswer);
                report.manual += host.money - before;
                answers++;
                double crowd = sim.IncomePerSecond * SecondsPerAnswer, auto = XgIdleRules.AutoIncomeRate(sim, host) * SecondsPerAnswer;
                host.Earn(crowd + auto); report.crowd += crowd; report.auto += auto;
                if (sim.AbilitiesCount >= 3) { host.money -= RentPerSecond * SecondsPerAnswer; report.rent += RentPerSecond * SecondsPerAnswer; }
                t += SecondsPerAnswer;
                // The opening: a training round after every ten answers until it tells yes from no.
                if (sim.OpeningFocus && sim.Has("train.curve") && answers % 10 == 0) { sim.TrainEpoch(XgTrack.Sequence, host); t += EpochSeconds; }

                // Shopping: save for the main line; buy cheap things (≤ 30% of the goal) on the way.
                for (int guard = 0; guard < 40; guard++)
                {
                    var goal = Goal(sim, report.stage);
                    double goalPrice = goal == null ? double.PositiveInfinity : Price(sim, goal);
                    XgNode pick = null;
                    if (goal != null && !skip.Contains(goal.id) && sim.Status(goal, host) == XgSim.NodeStatus.Buyable) pick = goal;
                    else
                    {
                        double cheapest = double.PositiveInfinity;
                        foreach (var n in XgCatalog.Nodes)
                        {
                            if (skip.Contains(n.id) || n.kind == XgNodeKind.Ability || n.kind == XgNodeKind.Project || n.kind == XgNodeKind.Breakthrough) continue;
                            if (sim.Status(n, host) != XgSim.NodeStatus.Buyable) continue;
                            double p = Price(sim, n);
                            if (p > SideShare * goalPrice && goal != null && !skip.Contains(goal.id)) continue;
                            if (p < cheapest) { cheapest = p; pick = n; }
                        }
                    }
                    if (pick == null) break;
                    double price = Price(sim, pick), money = host.money;
                    if (!sim.BuyNode(pick.id, host)) { skip.Add(pick.id); continue; }
                    double paid = money - host.money;
                    if (paid <= 0) continue; // free items are not purchases
                    if (result.firstPurchase < 0) result.firstPurchase = t;
                    if (report.firstBuy < 0) report.firstBuy = t - report.start;
                    report.maxGap = Math.Max(report.maxGap, t - Math.Max(lastBuy, report.start));
                    lastBuy = t; report.buys++; report.spent += paid;
                    report.purchases.Add(F(t / 60, "0.0") + "m " + pick.id + " ¥" + F(paid, "0"));
                }

                // Stage advance: the main line is bought (and, in stage 1, it has learned yes/no).
                var route = Route[report.stage];
                bool done = route.All(sim.Has) && !sim.OpeningFocus;
                if (done)
                {
                    report.end = t; report.autoLevel = sim.GlobalAutoLevel; report.raiseLevel = sim.S.payRaise;
                    report.maxGap = Math.Max(report.maxGap, t - Math.Max(lastBuy, report.start));
                    result.stages.Add(report);
                    if (report.stage == 4)
                    {
                        result.stageFourAutoRatio = report.manual > 0 ? report.auto / report.manual : 0;
                        return result;
                    }
                    int next = sim.NextAbility;
                    if (next != report.stage + 1 || Emerge == null) { result.stuck = "cannot emerge ability " + (report.stage + 1); return result; }
                    Emerge.Invoke(sim, new object[] { next });
                    report = new StageReport { stage = report.stage + 1, start = t };
                    lastBuy = Math.Max(lastBuy, t);
                    EnterStageFixtures(sim, host, report.stage);
                }
            }
            report.end = t;
            result.stages.Add(report);
            var g = Goal(sim, report.stage);
            result.stuck = "time limit in stage " + report.stage + " saving for " + (g == null ? "?" : g.id + " ¥" + F(Price(sim, g), "0") + " (" + sim.Status(g, host) + ")") + ", money ¥" + F(host.money, "0");
            return result;
        }

        public static string Describe(Result r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Pacing] first purchase at " + F(r.firstPurchase, "0") + " s; stage-4 auto/manual = " + F(r.stageFourAutoRatio, "0.00") + (r.stuck.Length > 0 ? "; STUCK: " + r.stuck : ""));
            foreach (var s in r.stages)
                sb.AppendLine("[Pacing] stage " + s.stage + ": " + F(s.Minutes, "0.0") + " min, " + s.buys + " buys, first " + F(s.firstBuy, "0") + " s, max gap " + F(s.maxGap, "0") + " s, ¥/min manual " + F(s.ManualPerMin, "0") + " crowd " + F(s.CrowdPerMin, "0") + " auto " + F(s.AutoPerMin, "0") + ", rent ¥" + F(s.rent, "0") + ", spent ¥" + F(s.spent, "0") + ", raise L" + s.raiseLevel + ", auto L" + s.autoLevel
                    + "\n[Pacing]   " + string.Join(" | ", s.purchases));
            return sb.ToString();
        }

        [Test]
        public void TypicalPlayerBuysSomethingSoonAndOften()
        {
            var r = Simulate();
            string text = Describe(r);
            Debug.Log(text);
            TestContext.WriteLine(text);
            Assert.IsEmpty(r.stuck, text);
            Assert.AreEqual(4, r.stages.Count, text);
            Assert.That(r.firstPurchase, Is.InRange(0, FirstPurchaseLimit), "first purchase within two minutes\n" + text);
            foreach (var s in r.stages)
            {
                Assert.That(s.firstBuy, Is.InRange(0, PurchaseGapLimit), "stage " + s.stage + " buys something within three minutes\n" + text);
                Assert.That(s.maxGap, Is.LessThanOrEqualTo(PurchaseGapLimit), "stage " + s.stage + " never goes three minutes without a purchase\n" + text);
            }
            Assert.That(r.stageFourAutoRatio, Is.InRange(.8, 1.6), "stage-4 automatic income stays near manual (1–1.5×)\n" + text);
        }

        /// <summary>The full price list (logged for the design doc's before/after tables).</summary>
        [Test]
        public void PriceTableIsLoggedAndEveryLadderRises()
        {
            var sim = new XgSim();
            var sb = new StringBuilder("[PriceTable] id,stage,lane,kind,cost,price\n");
            foreach (var n in XgCatalog.Nodes.OrderBy(n => n.stage).ThenBy(n => n.lane, StringComparer.Ordinal).ThenBy(n => Price(sim, n)))
                sb.Append("[PriceTable] ").Append(n.id).Append(',').Append(n.stage).Append(',').Append(n.lane).Append(',').Append(n.kind).Append(',').Append(F(n.cost, "0.##")).Append(',').Append(F(Price(sim, n), "0.##")).Append('\n');
            sb.Append("[PriceTable] raise ");
            for (int i = 0; i < XgCatalog.RaiseMax; i++) sb.Append(F(XgCatalog.RaiseCost(i), "0")).Append(' ');
            sb.Append("\n[PriceTable] auto ");
            for (int i = 0; i < XgCatalog.AutoMaxLevel; i++) sb.Append(F(XgCatalog.AutoCost(i), "0")).Append(' ');
            Debug.Log(sb.ToString());
            for (int i = 1; i < XgCatalog.RaiseMax; i++) Assert.Greater(XgCatalog.RaiseCost(i), XgCatalog.RaiseCost(i - 1), "raise " + i);
            for (int i = 1; i < XgCatalog.AutoMaxLevel; i++) Assert.Greater(XgCatalog.AutoCost(i), XgCatalog.AutoCost(i - 1), "auto " + i);
        }
    }
}
