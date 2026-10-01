using System;

namespace Lingguang.Core
{
    [Serializable]
    public class ShapeDef
    {
        public ShapeKind Kind;
        /// <summary>相对朝向的输出方向。</summary>
        public int[] Directions;
        public int Threshold;
        public int Light;
        public int Output;
    }

    /// <summary>光丝等级（规格第 5 节）：导通次数 ≥ MinConducts 即为该级。</summary>
    [Serializable]
    public class EdgeLevelDef
    {
        public string Name;
        public int MinConducts;
        public int ChargeBonus;
        public int Delay;
    }

    /// <summary>
    /// 灯塔聚焦方式：None 按半径照一片；DeepestMemory 只照一格——一生中记忆最深刻的地方；
    /// Character 预留给“与人物的链接”（人物系统接入前等同 DeepestMemory）。
    /// </summary>
    public enum LighthouseFocus { None = 0, DeepestMemory = 1, Character = 2 }

    [Serializable]
    public class StageDef
    {
        public Stage Stage;
        public string Name;
        public int LighthouseRadius;
        /// <summary>不为 None 时忽略半径，灯塔只照一格（老年默认 DeepestMemory）。</summary>
        public LighthouseFocus LighthouseFocus;
        /// <summary>废退：连续 N 轮未导通降一级。</summary>
        public int DisuseRounds;
        /// <summary>老年：导通次数为 0 且再一轮未导通的光丝直接剪除。</summary>
        public bool PruneZeroWhenIdle;
        public ShapeKind[] CandidatePool;
        /// <summary>第 1 轮 / 第 2 轮 / Boss 轮目标分。</summary>
        public int[] Targets;
        public string[] GatePool;
        public int ItemSlots;
        public bool Pickups;
        public bool Shop;
    }

    [Serializable]
    public class PatternDef
    {
        public PatternKind Kind;
        public string Name;
        public int Light;
        public float Mult;
        public Stage OpenStage;
        /// <summary>长链/短链：L 下限；汇聚：来源数下限；发散：下一拍因它而闪的数量下限。</summary>
        public int Param;
    }

    /// <summary>
    /// 全部可调数值（规格第 2 节：所有数值放配置）。
    /// 电量相关数值一律为“内部值”= 显示值 × 2。
    /// 纯 C#，可被 Unity 的 ScriptableObject / JsonUtility 序列化（只用公有字段、数组、枚举）。
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        public BoardLayout Layout;
        public ShapeDef[] Shapes;
        public EdgeLevelDef[] EdgeLevels;

        // ---------- 传播 ----------
        public int StarterOutputBonus = 2;
        public int BridgeChargePenalty = -1;
        public int BridgeExtraDelay = 1;
        public int BridgePenaltyUntilConducts = 3;
        public int ConvergeSources = 3;
        public int ConvergeBonusPerSource = 10;
        public int MaxBeats = 200;

        // ---- 无依（规格第 7 节）----
        public bool UnanchoredEnabled = true;
        /// <summary>你放下的灵光的扎根期（轮，含放下的那一轮）。</summary>
        public int RootingRounds = 3;
        /// <summary>扎根期后连续多少轮无依即漂走。</summary>
        public int UnanchoredRoundsToDrift = 2;

        // ---- 规则一的属性（v0.4/v0.5）：漏电与疲劳 ----
        /// <summary>漏电：连锁进行时每拍流失的电量（内部值，默认 1 = 显示 0.5）。0 关闭。</summary>
        public int LeakPerBeat = 1;
        /// <summary>
        /// 上一次确认留下的电，在本次连锁里是否流失。false（规格 v0.6）：只有本次连锁中收到过电的灵光才流失，
        /// 留下的电在再次收到电之前保持不动（T05）。true：sim.py 原版 / 规格 v0.4–v0.5 写法，所有电每拍都流失。
        /// </summary>
        public bool LeakStoredCharge = false;
        /// <summary>
        /// 连锁最后一拍结束后是否还流失一次。false（默认）：连锁停下时剩下的电原样留到下一次确认。
        /// 规格 v0.6 第 5 节第 6 步字面上最后一拍也流失，但那样 T05（“在连锁最后一拍让锥体积 2……第二次确认时闪”）不成立，这里按 T05 取 false。
        /// </summary>
        public bool LeakAfterLastBeat = false;
        /// <summary>疲劳：本轮每闪过一次，再次闪的阈值 +此值（内部值，默认 2 = 显示 1）；每轮开始恢复。</summary>
        public int RefireThresholdStep = 2;
        /// <summary>本轮已闪过的灵光能否再被选为起点（规格 A3 为否；基准测试的两次确认用例需要为是）。</summary>
        public bool AllowFiredStarters = false;

        // ---- 规则三的属性：扰动（本轮内短期增强） ----
        /// <summary>光丝每传一次电，本轮内经它送电 +此值（内部值），上限 PerturbCap；每轮开始回落。</summary>
        /// <summary>
        /// 微弱连接的脉冲能否送给本轮已闪过的一方。规格 v0.6：否（只送给还没闪过的一方）。
        /// 设为是即 v0.5 写法：与“再次闪光”叠加，真实棋盘基准测试中每轮闪光数暴涨、涌现检查只过 1–4 项。
        /// </summary>
        public bool WeakLinkToFiredPartner = false;
        /// <summary>
        /// 涟漪是否也给本轮已闪过的灵光送电，同时决定微弱连接是否在已闪灵光之间累积。规格 v0.6：否。
        /// 设为是即 v0.4/v0.5 写法：回荡从第 1 轮就出现，基准测试“回荡是学会的”不再通过。
        /// </summary>
        public bool RippleReachesFired = false;
        /// <summary>微弱连接强度达到此值才会传脉冲（规格 v0.6：3）。</summary>
        public int WeakLinkMinStrength = 3;
        /// <summary>微弱连接每轮结算时减弱多少（规格 v0.6：1，规则三“不用就变细”同样作用于它）；0 = 永不减弱。</summary>
        public int WeakLinkDecayPerRound = 1;
        public int PerturbStep = 1;
        public int PerturbCap = 2;

        // ---------- 本能与灯塔 ----------
        public string InstinctRegion = "I";
        public int InstinctCount = 3;
        public int InstinctLightDecayPerRound = 2;
        public int InstinctLightGrowthOldAge = 2;
        public int InstinctLightMax = 8;
        public int LighthouseCharge = 2;
        /// <summary>同时在两座灯塔范围内时是否拿两份。规格未写明，默认不叠加。</summary>
        public bool LighthouseStacks = false;
        /// <summary>
        /// 灯塔在棋盘外（锚点格之外一格），光从锚点格照进来（v0.3 确认）：
        /// 覆盖“距锚点 ≤ R − 1”的格子，R = 1 只照锚点本身，R = 0 照不到。设为 false 则按“距锚点 ≤ R”。
        /// </summary>
        public bool LighthouseFromOutside = true;

        // ---------- 涟漪 ----------
        public int RippleBaseRadius = 1;
        public int RippleMergeDistance = 2;
        public int RippleMaxRadius = 3;
        public int RippleCharge = 1;
        /// <summary>半径 ≥ 此值的涟漪群按六角距离覆盖（可跨沟）；更小的按不穿沟的路径距离。</summary>
        public int RippleCrossGrooveFromRadius = 2;
        public bool WeakLinksEnabled = true;
        public int WeakLinkCharge = 1;

        // ---------- 用进废退 ----------
        public int DisuseLevelDrop = 3;
        public int PruneBoostThreshold = 3;
        public int PruneBoost = 3;
        public int OldAgeExtraDelay = 1;
        public int OldAgeDisabledCellsPerRound = 2;
        public string[] OldAgeDecayRegionOrder = { "F", "T" };

        // ---------- 记忆 ----------
        public int MemoryThreshold = 2;
        public int MemoryMinSize = 3;
        public int MemoryMaxSize = 6;
        /// <summary>组内导通次数之和为 0 时不折叠（规格未写明，默认开启）。</summary>
        public bool MemoryRequiresConduction = true;
        /// <summary>可塑度规则（记忆与沉淀区域共用，v0.3）。</summary>
        public bool SettledRegionsEnabled = true;
        public int MemoryPlasticityMax = 3;
        public int MemoryAdjustCost = 1;
        public int MemoryTriggeredRoundsToZero = 3;
        public int MemoryIdleRoundsToRegain = 2;

        // ---------- 轮次 ----------
        public int BaseStartCount = 2;
        public int CandidatesPerRound = 3;
        public int MovesPerRound = 1;
        public int MovesPerBossRound = 2;
        public int RoundsPerStage = 3;

        // ---------- 计分 ----------
        public Stage FreshnessFromStage = Stage.Adult;
        public float FreshnessStep = 0.1f;
        public float FreshnessFloor = 0.5f;
        public PatternDef[] Patterns;

        // ---------- 阶段与关口 ----------
        public StageDef[] Stages;
        public GateDef[] Gates;

        // ---------- 经济与拾物 ----------
        public int CoinsPerRound = 3;
        public int CoinsBonus = 1;
        public float CoinsBonusScoreRatio = 2f;
        public int PickupMin = 1;
        public int PickupMax = 2;
        public int PickupLifetimeRounds = 2;
        public string[] PickupItemPool;

        // ---------- 查询 ----------

        public ShapeDef GetShape(ShapeKind kind)
        {
            for (int i = 0; i < Shapes.Length; i++)
                if (Shapes[i].Kind == kind) return Shapes[i];
            throw new InvalidOperationException("配置缺少形状 " + kind);
        }

        public StageDef GetStage(Stage stage)
        {
            for (int i = 0; i < Stages.Length; i++)
                if (Stages[i].Stage == stage) return Stages[i];
            throw new InvalidOperationException("配置缺少阶段 " + stage);
        }

        public GateDef FindGate(string id)
        {
            if (string.IsNullOrEmpty(id) || Gates == null) return null;
            for (int i = 0; i < Gates.Length; i++)
                if (Gates[i].Id == id) return Gates[i];
            return null;
        }

        public int PatternRank(PatternKind kind)
        {
            for (int i = 0; i < Patterns.Length; i++)
                if (Patterns[i].Kind == kind) return i;
            return int.MaxValue;
        }

        public PatternDef GetPattern(PatternKind kind)
        {
            int rank = PatternRank(kind);
            return rank == int.MaxValue ? null : Patterns[rank];
        }

        /// <summary>导通次数对应的等级下标。</summary>
        public int EdgeLevelIndex(int conducts)
        {
            int best = 0;
            for (int i = 0; i < EdgeLevels.Length; i++)
                if (conducts >= EdgeLevels[i].MinConducts) best = i;
            return best;
        }

        public bool IsTopEdgeLevel(int levelIndex) => levelIndex == EdgeLevels.Length - 1;

        // ---------- 规格默认值 ----------

        public static GameConfig CreateDefault()
        {
            var all = new[] { ShapeKind.Conduction, ShapeKind.Cone, ShapeKind.Converge };
            return new GameConfig
            {
                Layout = BoardLayout.CreateDefault(),
                Shapes = new[]
                {
                    new ShapeDef { Kind = ShapeKind.Conduction, Directions = new[] { 0 }, Threshold = 2, Light = 5, Output = 2 },
                    new ShapeDef { Kind = ShapeKind.Cone, Directions = new[] { 0, 2, 4 }, Threshold = 4, Light = 10, Output = 2 },
                    new ShapeDef { Kind = ShapeKind.Converge, Directions = new[] { 0 }, Threshold = 6, Light = 20, Output = 4 },
                    new ShapeDef { Kind = ShapeKind.Instinct, Directions = new[] { 0, 1, 2, 3, 4, 5 }, Threshold = 2, Light = 8, Output = 2 },
                },
                EdgeLevels = new[]
                {
                    new EdgeLevelDef { Name = "新生", MinConducts = 0, ChargeBonus = 0, Delay = 1 },
                    new EdgeLevelDef { Name = "加粗", MinConducts = 3, ChargeBonus = 2, Delay = 1 },
                    new EdgeLevelDef { Name = "髓鞘化", MinConducts = 6, ChargeBonus = 2, Delay = 0 },
                },
                Patterns = new[]
                {
                    new PatternDef { Kind = PatternKind.LongChain, Name = "长链", Light = 30, Mult = 4, OpenStage = Stage.Childhood, Param = 4 },
                    new PatternDef { Kind = PatternKind.Converge, Name = "汇聚", Light = 25, Mult = 3, OpenStage = Stage.Adolescence, Param = 3 },
                    new PatternDef { Kind = PatternKind.Diverge, Name = "发散", Light = 20, Mult = 3, OpenStage = Stage.Childhood, Param = 3 },
                    new PatternDef { Kind = PatternKind.ShortChain, Name = "短链", Light = 10, Mult = 2, OpenStage = Stage.Childhood, Param = 2 },
                    new PatternDef { Kind = PatternKind.Spark, Name = "火花", Light = 5, Mult = 1, OpenStage = Stage.Childhood, Param = 0 },
                },
                Stages = new[]
                {
                    new StageDef { Stage = Stage.Infant, Name = "婴儿", LighthouseRadius = 3, DisuseRounds = 6, CandidatePool = new[] { ShapeKind.Conduction }, Targets = new[] { 30, 45, 60 }, GatePool = new[] { "separation" }, ItemSlots = 0 },
                    new StageDef { Stage = Stage.Childhood, Name = "童年", LighthouseRadius = 2, DisuseRounds = 6, CandidatePool = new[] { ShapeKind.Conduction, ShapeKind.Cone }, Targets = new[] { 120, 180, 240 }, GatePool = new[] { "training" }, ItemSlots = 2, Pickups = true },
                    new StageDef { Stage = Stage.Adolescence, Name = "青春期", LighthouseRadius = 2, DisuseRounds = 6, CandidatePool = all, Targets = new[] { 400, 600, 800 }, GatePool = new[] { "mimic" }, ItemSlots = 3, Shop = true },
                    new StageDef { Stage = Stage.Adult, Name = "成年", LighthouseRadius = 1, DisuseRounds = 6, CandidatePool = all, Targets = new[] { 1200, 1800, 2400 }, GatePool = new[] { "anxiety" }, ItemSlots = 5, Shop = true },
                    new StageDef { Stage = Stage.MiddleAge, Name = "中年", LighthouseRadius = 1, DisuseRounds = 6, CandidatePool = all, Targets = new[] { 3000, 4500, 6000 }, GatePool = new string[0], ItemSlots = 5, Shop = true },
                    new StageDef { Stage = Stage.OldAge, Name = "老年", LighthouseRadius = 0, LighthouseFocus = LighthouseFocus.DeepestMemory, DisuseRounds = 1, PruneZeroWhenIdle = true, CandidatePool = all, Targets = new[] { 5000, 7500, 10000 }, GatePool = new[] { "final" }, ItemSlots = 5, Shop = true },
                },
                Gates = new[]
                {
                    new GateDef
                    {
                        Id = "separation", Name = "分离", Stage = Stage.Infant, RequiresMetaUnlock = true,
                        Description = "本轮灯塔不提供电量",
                        Effects = new[] { Effect(EffectOp.LighthouseOff, 0) },
                    },
                    new GateDef
                    {
                        Id = "training", Name = "培训班", Stage = Stage.Childhood,
                        Description = "本轮至少 6 个灵光闪过，否则未达标；锥体发出的光丝延迟 −1（最低 0）",
                        Effects = new[]
                        {
                            Effect(EffectOp.MinFiresInRound, 6),
                            Effect(EffectOp.DelayDelta, -1, new EffectCondition { Shapes = new[] { ShapeKind.Cone } }),
                        },
                    },
                    new GateDef
                    {
                        Id = "mimic", Name = "模仿", Stage = Stage.Adolescence,
                        Description = "本轮最高回路型须与上一轮相同（上一轮没有则本轮也不能有），否则回路型加成作废",
                        Effects = new[] { Effect(EffectOp.PatternMustRepeat, 0) },
                    },
                    new GateDef
                    {
                        Id = "anxiety", Name = "焦虑", Stage = Stage.Adult,
                        Description = "C 区灵光阈值 −2（最低 2），其他区域阈值 +2",
                        Effects = new[]
                        {
                            Floored(EffectOp.ThresholdDelta, -2, 2, new EffectCondition { Regions = new[] { "C" } }),
                            Effect(EffectOp.ThresholdDelta, 2, new EffectCondition { ExcludeRegions = new[] { "C" } }),
                        },
                    },
                    new GateDef
                    {
                        Id = "final", Name = "？", Stage = Stage.OldAge,
                        Description = "起点次数只有 1；所有记忆与本能灵光阈值降为 1（显示 0.5）",
                        Effects = new[]
                        {
                            Effect(EffectOp.StartCountSet, 1),
                            Effect(EffectOp.ThresholdSet, 1, new EffectCondition { MemoryOnly = true }),
                            Effect(EffectOp.ThresholdSet, 1, new EffectCondition { InstinctOnly = true }),
                        },
                    },
                },
                PickupItemPool = new[] { "奶嘴", "作业本", "积木", "望远镜", "闹钟", "镜片", "海螺", "电脑", "耳机", "处分条" },
            };
        }

        static EffectDef Effect(EffectOp op, int value, EffectCondition condition = null)
        {
            return new EffectDef { Op = op, Value = value, Condition = condition ?? new EffectCondition() };
        }

        static EffectDef Floored(EffectOp op, int value, int floor, EffectCondition condition)
        {
            var e = Effect(op, value, condition);
            e.HasFloor = true;
            e.Floor = floor;
            return e;
        }
    }
}
