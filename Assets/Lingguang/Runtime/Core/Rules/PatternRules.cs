using System;

namespace Lingguang.Core
{
    /// <summary>
    /// 回路型识别（规格第 8 节，暂定规则）。每次确认结束时识别，取排名最高（GameConfig.Patterns 顺序）的一项；
    /// 婴儿阶段不识别（由各回路型的 OpenStage 控制）。
    /// </summary>
    public static class PatternRules
    {
        public static bool Matches(PatternDef def, SimResult r)
        {
            switch (def.Kind)
            {
                case PatternKind.LongChain:
                case PatternKind.ShortChain:
                    return r.ChainLength >= def.Param;
                case PatternKind.Converge:
                    return r.MaxConvergeSources >= def.Param;
                case PatternKind.Diverge:
                    return r.MaxDiverge >= def.Param;
                case PatternKind.Spark:
                    return true;
                default:
                    return false;
            }
        }

        public static PatternDef Recognize(GameState s, SimResult r)
        {
            foreach (var def in s.Config.Patterns)
            {
                if (s.Stage < def.OpenStage) continue;
                if (Matches(def, r)) return def;
            }
            return null;
        }

        /// <summary>识别、套用新鲜度、累加到本轮，并发出 PatternRecognized / ScoreDelta。</summary>
        public static void Apply(GameState s, SimResult r)
        {
            var def = Recognize(s, r);
            if (def == null) return;

            var cfg = s.Config;
            double factor = 1.0;
            if (s.Stage >= cfg.FreshnessFromStage)
            {
                if (def.Kind == s.FreshnessLastPattern) s.FreshnessStreak++;
                else s.FreshnessStreak = 0;
                s.FreshnessLastPattern = def.Kind;
                factor = Math.Max(Num.D(cfg.FreshnessFloor), 1.0 - Num.D(cfg.FreshnessStep) * s.FreshnessStreak);
            }

            double mult = Math.Round(Num.D(def.Mult) * factor, 6);
            r.Pattern = def.Kind;
            s.Round.Patterns.Add(def.Kind);
            s.Round.PatternLight += def.Light;
            s.Round.PatternMult += mult;

            r.Events.Add(new SimEvent
            {
                Type = SimEventType.PatternRecognized, Beat = r.EndBeat, Pattern = def.Kind,
                LightDelta = def.Light, MultDelta = mult, Source = def.Name, Count = s.FreshnessStreak,
            });
            r.Events.Add(new SimEvent
            {
                Type = SimEventType.ScoreDelta, Beat = r.EndBeat, Pattern = def.Kind,
                LightDelta = def.Light, MultDelta = mult, Source = "pattern",
            });
        }
    }
}
