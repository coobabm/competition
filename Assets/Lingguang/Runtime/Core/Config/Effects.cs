using System;

namespace Lingguang.Core
{
    /// <summary>触发时机（规格第 9 节“效果系统”）。</summary>
    public enum EffectTrigger { Passive = 0, OnFire = 1, OnConfirmEnd = 2, OnRoundSettle = 3, OnRoundStart = 4 }

    /// <summary>
    /// 操作。里程碑 1 已实现：关口需要的全部操作（标注 ✓）。
    /// 其余操作已占位，里程碑 4 接物件、药品时逐个实现；遇到未实现的操作会被忽略并可在调试面板列出。
    /// </summary>
    public enum EffectOp
    {
        AddLight = 0,
        AddMult = 1,
        MulMult = 2,
        ThresholdDelta = 3,          // ✓ 焦虑
        ThresholdSet = 4,            // ✓ ？
        OutputDelta = 5,             // ✓
        DelayDelta = 6,              // ✓ 培训班
        RippleRadiusDelta = 7,       // ✓
        StartCountDelta = 8,         // ✓
        StartCountSet = 9,           // ✓ ？
        LighthouseOff = 10,          // ✓ 分离
        MinFiresInRound = 11,        // ✓ 培训班
        PatternMustRepeat = 12,      // ✓ 模仿
        FiredBecomeConductOnly = 13,
        CrossRegionChargeDelta = 14,
        ConductCountMultiplier = 15,
        FreshnessPenaltyDelta = 16,
        PruneProtect = 17,
        RippleRecursion = 18,
        TrialRun = 19,
        GateDisable = 20,
        GateReroll = 21,
    }

    /// <summary>条件。数组为空表示不限制；多个条件同时满足才生效。</summary>
    [Serializable]
    public class EffectCondition
    {
        public ShapeKind[] Shapes;
        public string[] Regions;
        public string[] ExcludeRegions;
        public bool MemoryOnly;
        public bool InstinctOnly;
        public PatternKind Pattern = PatternKind.None;
    }

    [Serializable]
    public class EffectDef
    {
        public EffectTrigger Trigger = EffectTrigger.Passive;
        public EffectCondition Condition = new EffectCondition();
        public EffectOp Op;
        public int Value;
        /// <summary>减少类修正的下限（如“最低 2”）。只在 Value 为负时使用，不会把原本低于下限的值抬高。</summary>
        public bool HasFloor;
        public int Floor;
    }

    /// <summary>关口（规格第 9 节）。</summary>
    [Serializable]
    public class GateDef
    {
        public string Id;
        public string Name;
        public Stage Stage;
        /// <summary>需要局外解锁（“分离”：首次到达童年后才出现）。</summary>
        public bool RequiresMetaUnlock;
        public string Description;
        public EffectDef[] Effects;
    }
}
