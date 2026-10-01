using System;
using System.Collections.Generic;

namespace Lingguang.Core
{
    /// <summary>一个灵光。Charge / Fired 是本轮状态，其余跨轮保留。</summary>
    public sealed class Glow
    {
        public int Id;
        public ShapeKind Shape;
        public int Orientation;
        public Cell Cell;
        public bool IsInstinct;
        /// <summary>所属记忆 id；-1 表示不在记忆中。</summary>
        public int MemoryId = -1;
        /// <summary>当前光量（本能灵光会随阶段变化，所以存在实例上）。</summary>
        public int Light;
        public int Charge;
        /// <summary>本轮是否闪过（至少一次）。</summary>
        public bool Fired;
        /// <summary>本轮闪过的次数（疲劳：每次 +阈值）。记忆成员与记忆同步。</summary>
        public int FireCount;
        /// <summary>无依：扎根期剩余轮数（放下时为 RootingRounds；每轮结算 −1，到 0 后才适用无依）。</summary>
        public int RootRoundsLeft;
        /// <summary>无依：连续无依的轮数。</summary>
        public int UnanchoredRounds;

        public Glow Clone() => (Glow)MemberwiseClone();
    }

    /// <summary>一条光丝的跨轮数据（规格第 7 节）。</summary>
    public sealed class EdgeStats
    {
        public int Count;
        /// <summary>连续未导通轮数（废退）。</summary>
        public int IdleRounds;
        public bool Pruned;
        public bool ConductedThisRound;

        public EdgeStats Clone() => (EdgeStats)MemberwiseClone();
    }

    /// <summary>记忆（简化版：一个大灵光）。</summary>
    public sealed class Memory
    {
        public int Id;
        public List<int> Members = new List<int>();
        public string Region;
        public int Charge;
        public bool Fired;
        public int FireCount;
        /// <summary>本轮是否被触发过。可塑度不在记忆上：记忆所在区域沉淀后共用区域可塑度（v0.3）。</summary>
        public bool TriggeredThisRound;
        /// <summary>整局累计被触发次数（“记忆深度”的第一依据）。</summary>
        public int TimesTriggered;
        /// <summary>折叠时组内导通次数之和（深度的第二依据）。</summary>
        public int FoldConductSum;

        public Memory Clone()
        {
            var c = (Memory)MemberwiseClone();
            c.Members = new List<int>(Members);
            return c;
        }
    }

    /// <summary>
    /// 沉淀区域（v0.3）：阶段结束后，该阶段解锁的整片区域内所有灵光（含记忆）共用一个可塑度。
    /// 规则同记忆：初始 3；每次调整（旋转、移动）−1；记忆每被触发一次 −1；
    /// 区域内有灵光闪过的轮次计为“被触发”：连续 3 轮被触发归零，连续 2 轮未被触发 +1（上限 3）。
    /// 可塑度为 0 时区域内的灵光不能旋转、移动。
    /// </summary>
    public sealed class SettledRegion
    {
        public string Region;
        public int Plasticity;
        public int ConsecutiveTriggeredRounds;
        public int ConsecutiveIdleRounds;
        public bool TriggeredThisRound;

        public SettledRegion Clone() => (SettledRegion)MemberwiseClone();
    }

    /// <summary>童年拾物点。</summary>
    public sealed class Pickup
    {
        public Cell Cell;
        public string ItemId;
        public int RoundsLeft;

        public Pickup Clone() => (Pickup)MemberwiseClone();
    }

    /// <summary>一轮之内的状态：起点次数、候选、累积的光量与倍率。</summary>
    public sealed class RoundState
    {
        public int StartsLeft;
        public int ConfirmsDone;
        public int MovesLeft;
        public bool PlacementAvailable;
        /// <summary>本轮第一次确认尚未发生（灯塔只在第一次确认时供电）。</summary>
        public bool FirstConfirmPending;
        public List<ShapeKind> Candidates = new List<ShapeKind>();

        public int FireLight;
        public int PatternLight;
        public int ItemLight;
        public double PatternMult;
        public double ItemAddMult;
        /// <summary>乘法倍率，按物件槽从左到右的顺序。</summary>
        public List<double> MulMults = new List<double>();
        /// <summary>本轮每次确认识别出的回路型（按确认顺序）。</summary>
        public List<PatternKind> Patterns = new List<PatternKind>();
        /// <summary>本轮闪过的灵光 id（记忆成员各算一个）。</summary>
        public HashSet<int> FiredGlows = new HashSet<int>();
        /// <summary>扰动：本轮内每条光丝的短期增强（内部值），每轮开始清零。</summary>
        public Dictionary<EdgeKey, int> Perturb = new Dictionary<EdgeKey, int>();
        /// <summary>本轮闪光总次数（含再次闪光）。</summary>
        public int FireEvents;

        public int TotalLight(bool includePatterns) => FireLight + ItemLight + (includePatterns ? PatternLight : 0);
        public double TotalAddMult(bool includePatterns) => ItemAddMult + (includePatterns ? PatternMult : 0);

        /// <summary>预估分 = 结算分的同一公式（规格第 8 节）。</summary>
        public int Estimate(bool includePatterns = true) => Scoring.Compute(TotalLight(includePatterns), TotalAddMult(includePatterns), MulMults);

        public PatternKind TopPattern(GameConfig cfg)
        {
            var best = PatternKind.None;
            int bestRank = int.MaxValue;
            foreach (var p in Patterns)
            {
                int rank = cfg.PatternRank(p);
                if (rank < bestRank) { bestRank = rank; best = p; }
            }
            return best;
        }

        public RoundState Clone()
        {
            var r = (RoundState)MemberwiseClone();
            r.Candidates = new List<ShapeKind>(Candidates);
            r.MulMults = new List<double>(MulMults);
            r.Patterns = new List<PatternKind>(Patterns);
            r.FiredGlows = new HashSet<int>(FiredGlows);
            r.Perturb = new Dictionary<EdgeKey, int>(Perturb);
            return r;
        }
    }

    public static class Scoring
    {
        const double FloorEpsilon = 1e-9;

        /// <summary>⌊ 光量总和 × (1 + 加法倍率总和) × 各乘法倍率之积 ⌋</summary>
        public static int Compute(int light, double addMult, IReadOnlyList<double> mulMults)
        {
            double product = 1.0;
            if (mulMults != null)
                for (int i = 0; i < mulMults.Count; i++) product *= mulMults[i];
            return (int)Math.Floor(light * (1.0 + addMult) * product + FloorEpsilon);
        }
    }
}
