using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>
    /// 把“当前生效的效果”翻译成规则修正。规则代码只调用这里，不写关口/物件的专用分支（规格第 2 节）。
    /// 里程碑 1 的效果来源只有 Boss 轮关口；物件、药品在里程碑 4 加入 Active()。
    /// </summary>
    public static class EffectQuery
    {
        static readonly List<EffectDef> Empty = new List<EffectDef>();

        public static GateDef ActiveGate(GameState s) => s.IsBossRound ? s.Config.FindGate(s.StageGateId) : null;

        /// <summary>当前生效的常驻效果。</summary>
        public static List<EffectDef> Active(GameState s)
        {
            var gate = ActiveGate(s);
            if (gate == null || gate.Effects == null || gate.Effects.Length == 0) return Empty;
            var list = new List<EffectDef>(gate.Effects.Length);
            foreach (var e in gate.Effects)
                if (e != null && e.Trigger == EffectTrigger.Passive) list.Add(e);
            return list;
        }

        /// <summary>疲劳后的实际阈值：基础阈值（含效果修正）+ 本轮已闪次数 × RefireThresholdStep。</summary>
        public static int EffectiveThreshold(GameState s, int unit) =>
            Threshold(s, unit) + s.Config.RefireThresholdStep * Units.FireCount(s, unit);

        public static int Threshold(GameState s, int unit)
        {
            Glow g = null;
            Memory ch = null;
            int t;
            if (Units.IsMemory(unit)) { ch = Units.MemoryOf(s, unit); t = s.Config.MemoryThreshold; }
            else { g = s.Glows[unit]; t = s.Config.GetShape(g.Shape).Threshold; }

            var active = Active(s);
            foreach (var e in active)
                if (e.Op == EffectOp.ThresholdSet && Matches(s, e.Condition, g, ch)) t = e.Value;
            foreach (var e in active)
            {
                if (e.Op != EffectOp.ThresholdDelta || !Matches(s, e.Condition, g, ch)) continue;
                t = ApplyDelta(t, e);
            }
            return Math.Max(1, t);
        }

        public static int OutputDelta(GameState s, Glow source) => SumFor(s, EffectOp.OutputDelta, source);
        public static int DelayDelta(GameState s, Glow source) => SumFor(s, EffectOp.DelayDelta, source);

        public static int RippleRadiusDelta(GameState s)
        {
            int sum = 0;
            foreach (var e in Active(s)) if (e.Op == EffectOp.RippleRadiusDelta) sum += e.Value;
            return sum;
        }

        public static int StartCount(GameState s)
        {
            int n = s.Config.BaseStartCount;
            var active = Active(s);
            foreach (var e in active) if (e.Op == EffectOp.StartCountSet) n = e.Value;
            foreach (var e in active) if (e.Op == EffectOp.StartCountDelta) n += e.Value;
            return Math.Max(0, n);
        }

        public static bool LighthouseOff(GameState s) => Any(s, EffectOp.LighthouseOff);
        public static bool PatternMustRepeat(GameState s) => Any(s, EffectOp.PatternMustRepeat);

        public static int MinFiresInRound(GameState s)
        {
            int n = 0;
            foreach (var e in Active(s)) if (e.Op == EffectOp.MinFiresInRound) n = Math.Max(n, e.Value);
            return n;
        }

        // ---------------- 内部 ----------------

        static bool Any(GameState s, EffectOp op)
        {
            foreach (var e in Active(s)) if (e.Op == op) return true;
            return false;
        }

        static int SumFor(GameState s, EffectOp op, Glow g)
        {
            int sum = 0;
            foreach (var e in Active(s))
                if (e.Op == op && Matches(s, e.Condition, g, null)) sum = ApplyDelta(sum, e);
            return sum;
        }

        /// <summary>加上修正；有下限时只限制“减少”，不会把原本就低于下限的值抬上去。</summary>
        static int ApplyDelta(int value, EffectDef e)
        {
            int next = value + e.Value;
            if (e.HasFloor && e.Value < 0) next = Math.Max(next, Math.Min(value, e.Floor));
            return next;
        }

        /// <summary>g 与 ch 二选一：判断普通灵光 g，或判断记忆 ch。</summary>
        static bool Matches(GameState s, EffectCondition c, Glow g, Memory ch)
        {
            if (c == null) return true;
            if (c.Shapes != null && c.Shapes.Length > 0)
            {
                if (g == null || Array.IndexOf(c.Shapes, g.Shape) < 0) return false;
            }

            char region = g != null ? s.Board.RegionOf(g.Cell) : (string.IsNullOrEmpty(ch.Region) ? '\0' : ch.Region[0]);
            if (c.Regions != null && c.Regions.Length > 0 && !ContainsRegion(c.Regions, region)) return false;
            if (c.ExcludeRegions != null && ContainsRegion(c.ExcludeRegions, region)) return false;

            bool isMemory = ch != null || (g != null && g.MemoryId >= 0);
            if (c.MemoryOnly && !isMemory) return false;
            if (c.InstinctOnly && (g == null || !g.IsInstinct)) return false;
            return true;
        }

        static bool ContainsRegion(string[] regions, char region)
        {
            foreach (string r in regions)
                if (!string.IsNullOrEmpty(r) && r[0] == region) return true;
            return false;
        }
    }
}
