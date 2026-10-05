using System;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.Core.Incremental;

// Greedy balance bot: active player answers by hand every 3.5 s (95% right) for the first `activeMinutes`,
// uses 灵光一闪 when ready, and buys the option with best marginal gain per second of waiting.
static class Program
{
    static bool Active;
    static void Main(string[] args)
    {
        double activeMinutes = args.Length > 0 ? double.Parse(args[0]) : 60;
        double horizon = args.Length > 1 ? double.Parse(args[1]) * 60 : 90 * 60;
        var sim = new IncrementalSim();
        var rng = new Random(7);
        double t = 0, lastBuy = 0, maxGap = 0, nextManual = 3.5; string maxGapAt = "";
        var log = new List<string>();
        var firsts = new Dictionary<string, double>();
        void Mark(string k) { if (!firsts.ContainsKey(k)) firsts[k] = t; }
        int buys = 0;
        while (t < horizon)
        {
            bool active = t < activeMinutes * 60; Active = active;
            if (active && t >= nextManual) { sim.AnswerManual(rng.NextDouble() < .95); nextManual += 3.5; }
            if (active && sim.SparkReady && sim.ScorePerSecond > 0) sim.Spark();
            // buy loop
            for (int guard = 0; guard < 50; guard++)
            {
                string what = Choose(sim, out Action act);
                if (act == null) break;
                act();
                buys++;
                double gap = t - lastBuy; if (gap > maxGap && t > 0) { maxGap = gap; maxGapAt = what + " @" + Fmt.Duration(t); }
                lastBuy = t;
                if (what.StartsWith("light")) Mark("tree" + sim.LitCount);
                if (log.Count < 400) log.Add($"{Fmt.Duration(t),-14} {what,-28} 分 {Fmt.Num(sim.S.score),8} ¥ {Fmt.Money(sim.S.money),9}  分/s {Fmt.Num(sim.ScorePerSecond),8} ¥/s {Fmt.Num(sim.MoneyPerSecond),7} acc {sim.Accuracy:P0}");
            }
            for (int j = 1; j < IncrementalCatalog.Jobs.Length; j++) if (sim.JobUnlocked(j)) Mark("unlock " + IncrementalCatalog.Jobs[j].id);
            if (sim.S.jobLevels[0] > 0) Mark("first auto");
            if (sim.CanTakeExam) Mark("exam ready");
            if (sim.S.unpaidPower) Mark("OUTAGE");
            sim.Tick(.25); t += .25;
        }
        foreach (var l in log.Where((x, i) => i < 60 || i % 10 == 0)) Console.WriteLine(l);
        Console.WriteLine("---- milestones");
        foreach (var kv in firsts.OrderBy(k => k.Value)) if (!kv.Key.StartsWith("tree") || kv.Key == "tree10" || kv.Key == "tree20" || kv.Key == "tree35") Console.WriteLine($"{kv.Key,-18} {Fmt.Duration(kv.Value)}");
        for (int r = 0; r < 5; r++) { var row = ""; for (int q = 0; q < 7; q++) { var c = IncrementalCatalog.Get(q, r); row += (sim.IsLit(q, r) ? "[" : " ") + c.effect.ToString().Substring(0, 2) + (sim.IsLit(q, r) ? "]" : " "); } Console.WriteLine(new string(' ', r * 3) + row); }
        Console.WriteLine($"buys {buys}, longest wait {Fmt.Duration(maxGap)} before {maxGapAt}");
        Console.WriteLine($"end: lit {sim.LitCount}/35 jobs [{string.Join(",", sim.S.jobLevels)}] hw [{string.Join(",", sim.S.hardware)}] power L{sim.S.powerLevel} {Fmt.Num(sim.Watts)}/{Fmt.Num(sim.PowerCapacity)}W  分/s {Fmt.Num(sim.ScorePerSecond)} ¥/s {Fmt.Num(sim.MoneyPerSecond)} bill/s {sim.PowerCostPerSecond:0.000} day {sim.S.day}");
    }

    // Value = relative increase of (score+money rate) per unit cost-time.
    static string Choose(IncrementalSim sim, out Action act)
    {
        Action chosen = null; string best = null; double bestScore = 0;
        double sps = Math.Max(sim.ScorePerSecond, 0.25), mps = Math.Max(sim.MoneyPerSecond, 0.05);
        void Consider(string name, bool money, double cost, Func<double> gainRel, Action a)
        {
            double have = money ? sim.S.money : sim.S.score;
            if (have < cost) return;
            double rel = gainRel();
            // keep ¥ reserve for bills
            if (money && have - cost < sim.PowerCostPerSecond * 240) return;
            double value = rel / Math.Max(cost / (money ? mps : sps), 1);
            if (value > bestScore) { bestScore = value; best = name; chosen = a; }
        }
        for (int j = 0; j < IncrementalCatalog.Jobs.Length; j++)
        {
            int jj = j;
            if (!sim.JobUnlocked(j)) continue;
            Consider("job " + IncrementalCatalog.Jobs[j].id + " L" + (sim.S.jobLevels[j] + 1), false, sim.JobCost(j), () => Gain(sim, () => { sim.S.jobLevels[jj]++; }, () => { sim.S.jobLevels[jj]--; }) + (sim.S.jobLevels[jj] == 0 ? 1 : 0), () => sim.LevelJob(jj));
        }
        // tree: pick frontier cell with best gain
        for (int q = 0; q < 7; q++) for (int r = 0; r < 5; r++)
        {
            if (!sim.CanLight(q, r)) continue;
            int qq = q, rr = r; var cell = IncrementalCatalog.Get(q, r);
            double bonus = cell.effect == SkillEffect.Job ? .8 : cell.effect == SkillEffect.OutputYes || cell.effect == SkillEffect.OutputNo ? .5 : cell.effect == SkillEffect.Manual ? (Active ? .5 / 3.5 / (1 + sim.ManualSeconds / 3.5) : 0) : cell.effect == SkillEffect.Accuracy && sim.HasYes && sim.HasNo && sim.Accuracy < sim.C.examRequiredAccuracy ? .6 : 0.05;
            Consider($"light {cell.name}({q},{r})", false, sim.TreeCost, () => Gain(sim, () => sim.S.litCells.Add(cell.Index), () => sim.S.litCells.Remove(cell.Index), true) + bonus, () => sim.Light(qq, rr));
        }
        for (int i = 0; i < IncrementalCatalog.Hardware.Length; i++)
        {
            int ii = i; var h = IncrementalCatalog.Hardware[i];
            if (!sim.HardwareRevealed(i)) continue;
            double rel = h.isGpu ? h.compute / Math.Max(1, sim.Compute) : (sim.GpusInstalled >= sim.GpuSlots ? .3 : 0);
            if (h.isGpu && sim.GpusInstalled >= sim.GpuSlots) continue;
            if (sim.Watts + h.watts * (h.isGpu ? sim.WattFactor : 1) > sim.PowerCapacity) continue;
            Consider("buy " + h.id, true, sim.HardwarePrice(i), () => rel, () => sim.Buy(ii));
        }
        double need = 0; foreach (var h in IncrementalCatalog.Hardware) need = Math.Max(need, h.watts);
        if (sim.Watts + need > sim.PowerCapacity) Consider("power+", true, sim.PowerUpgradeCost, () => .35, () => sim.UpgradePower());
        act = chosen;
        return best;
    }

    // Relative change in combined (score rate normalised + money rate normalised); uses private set via reflection-free trick.
    static double Gain(IncrementalSim sim, Action apply, Action revert, bool rebuild = false)
    {
        double s0 = sim.ScorePerSecond, m0 = sim.MoneyPerSecond;
        apply();
        var probe = rebuild ? new IncrementalSim(sim.S, sim.C) : sim;
        double s1 = probe.ScorePerSecond, m1 = probe.MoneyPerSecond;
        revert();
        double a = s0 > 0 ? (s1 - s0) / s0 : (s1 > 0 ? 1 : 0);
        double b = m0 > 0 ? (m1 - m0) / m0 : (m1 > 0 ? 1 : 0);
        return Math.Max(0, (a + b) / 2);
    }
}
