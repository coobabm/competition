using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LingGuang.Core
{
    /// <summary>Greedy one-step-lookahead player for balance checks and smoke tests (not used in game).</summary>
    public static class Bot
    {
        public static int Evaluate(RunState s, int startSpark)
        {
            var c = s.Clone();
            var r = c.Confirm(startSpark);
            if (r == null) return -1;
            return c.EstimateScore();
        }

        static int BestStart(RunState s, out int score)
        {
            int best = -1; score = -1;
            foreach (var sp in s.sparks.Values.OrderBy(x => x.id))
            {
                if (!s.CanConfirm(sp.id, out _)) continue;
                int v = Evaluate(s, sp.id);
                if (v > score) { score = v; best = sp.id; }
            }
            return best;
        }

        /// <summary>Score of a board = best estimate after spending all starts greedily.</summary>
        static int RoundValue(RunState s)
        {
            var c = s.Clone();
            while (c.startsLeft > 0)
            {
                int st = BestStart(c, out _);
                if (st < 0) break;
                c.Confirm(st);
            }
            return c.EstimateScore();
        }

        public static (int cand, int cell, int dir) ChoosePlacement(RunState s)
        {
            if (s.candidates.Count == 0) return (-1, -1, 0);
            int bestCand = -1, bestCell = -1, bestDir = 0, bestVal = RoundValue(s);
            var tried = new HashSet<Shape>();
            for (int ci = 0; ci < s.candidates.Count; ci++)
            {
                if (!tried.Add(s.candidates[ci])) continue;
                foreach (int cell in s.board.AllCells())
                {
                    if (!s.CanPlaceAt(cell)) continue;
                    bool nearSomething = false;
                    for (int d = 0; d < 6; d++) { int n = s.board.Neighbor(cell, d); if (n >= 0 && s.cellSpark[n] >= 0) nearSomething = true; }
                    if (!nearSomething) continue;
                    int dirs = s.candidates[ci] == Shape.Cone ? 2 : 6;
                    for (int dir = 0; dir < dirs; dir++)
                    {
                        var c = s.Clone();
                        if (!c.Place(ci, cell, dir)) continue;
                        int v = RoundValue(c) + FutureBonus(c);
                        if (v > bestVal) { bestVal = v; bestCand = ci; bestCell = cell; bestDir = dir; }
                    }
                }
            }
            return (bestCand, bestCell, bestDir);
        }

        public static int ChooseStart(RunState s) => BestStart(s, out _);

        public static void PlayRound(RunState s)
        {
            var (cand, cell, dir) = ChoosePlacement(s);
            if (cand >= 0) s.Place(cand, cell, dir); else if (s.candidates.Count > 0) s.SkipCandidates();
            // buy something affordable, use a drug on boss rounds
            for (int i = 0; i < s.shop.Count; i++) if (!s.shop[i].sold && s.coins >= s.shop[i].price) s.Buy(i);
            if (s.IsBossRound && s.drugs.Count > 0)
            {
                var d = EffectDef.Get(s.drugs[0]);
                if (!d.now.Any(e => e.op == Op.StartsAdd && e.value < 0)) s.UseDrug(0);
            }
            while (s.startsLeft > 0)
            {
                int st = BestStart(s, out _);
                if (st < 0) break;
                s.Confirm(st);
            }
            s.Settle();
            s.NextRound();
        }

        static int FutureBonus(RunState s)
        {
            // small preference for connected boards (edges count as growth potential)
            return s.edges.Count * 2;
        }

        public static string PlayRuns(GameConfig cfg, int[] seeds)
        {
            var sb = new StringBuilder();
            foreach (int seed in seeds)
            {
                var s = RunState.NewRun(cfg, seed);
                int guard = 0;
                while (s.phase != Phase.GameOver && s.phase != Phase.Victory && guard++ < 40) PlayRound(s);
                var last = s.history.LastOrDefault();
                sb.Append($"seed {seed}: {s.phase} at {cfg.stageNames[Math.Min(s.stage, 5)]} r{s.roundInStage + 1} | ");
                sb.Append(string.Join(" ", s.history.Select(h => $"{h.score}/{h.target}{(h.passed ? "" : "x")}")));
                sb.Append($" | sparks {s.sparks.Count} mem {s.nodes.Values.Count(n => n.isMemory)} chars {s.sparks.Values.Count(x => x.isCharacter)} items {string.Join(",", s.items)}");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
