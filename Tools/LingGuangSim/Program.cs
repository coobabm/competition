using System;
using System.Diagnostics;
using System.Linq;
using LingGuang.Core;

// Headless balance runner for the LingGuang rules layer (same Core sources as Unity).
// usage: dotnet run -c Release -- [seeds...]
static class Program
{
    static void Main(string[] args)
    {
        var cfg = new GameConfig();
        var rest = args.ToList();
        int ci = rest.IndexOf("--cap");
        if (ci >= 0) { cfg.refireRaiseCap = int.Parse(rest[ci + 1]); rest.RemoveRange(ci, 2); }
        int cc = rest.IndexOf("--credits");
        if (cc >= 0) { cfg.infiniteCredits = int.Parse(rest[cc + 1]); rest.RemoveRange(cc, 2); }
        if (rest.Remove("--nocarry")) cfg.carryOverFire = false;
        var seeds = rest.Count > 0 ? rest.Select(int.Parse).ToArray() : new[] { 1, 2, 3, 4, 5, 6 };
        Console.WriteLine($"cap={cfg.refireRaiseCap} carry={cfg.carryOverFire} credits={cfg.infiniteCredits}");
        foreach (int seed in seeds)
        {
            var sw = Stopwatch.StartNew();
            var s = RunState.NewRun(cfg, seed);
            int firstInf = -1, guard = 0;
            while (s.phase != Phase.GameOver && s.phase != Phase.Victory && guard++ < 40)
            {
                int ri = s.RoundIndex;
                s.stats.patterns.TryGetValue("infinite", out int before);
                Bot.PlayRound(s);
                s.stats.patterns.TryGetValue("infinite", out int after);
                if (after > before && firstInf < 0) firstInf = ri + 1;
            }
            s.stats.patterns.TryGetValue("infinite", out int inf);
            Console.WriteLine($"seed {seed}: {s.phase} R{s.history.Count} inf x{inf} first@R{firstInf} {sw.ElapsedMilliseconds}ms | " +
                string.Join(" ", s.history.Select(h => $"{h.score}/{h.target}{(h.passed ? "" : "x")}")));
        }
    }
}
