using System;
using System.Collections.Generic;

namespace LingGuang.Core
{
    /// <summary>
    /// Data-driven effect system shared by items, drugs and gates:
    /// trigger + condition (shape / region / memory-or-instinct) + op + value.
    /// </summary>
    public enum Trigger { Passive, OnFire, OnConfirmEnd, OnRoundSettle, OnRoundStart }

    public enum Op
    {
        LightPercentFirstFire,      // first fire of each round: light × value%
        LightAddOnFire,             // cond shape
        AddMultOnFire,              // cond shape (value = +mult)
        RippleRadiusAdd,
        StartsAdd,
        StartsSet,
        MovesAdd,
        PreviewBeats,
        RippleRecursionDesignate,   // 海螺
        TrialRun,                   // 电脑
        ThresholdAdd,               // cond region/shape/memoryOrInstinct, invertRegion, clamp min 2
        ThresholdSet,               // cond memoryOrInstinct
        DesignatedRegionThresholdAdd, // 耳机
        CrossRegionDeliveryAdd,
        RegionPairDeliveryAdd,      // region <-> region2
        FreshnessPenaltyPercent,    // +5% per freshness step
        ConductCountBonusOldRegion, // +value conduct counts in regions unlocked > 1 stage ago
        FiredNoScore,               // 退烧贴
        SettleMultPercent,          // multiplicative, e.g. 200 = ×2
        FreshnessStepsAdd,          // permanent extra freshness steps
        LightPercentAll,            // +value% on every fire
        DelayAdd,                   // cond shape, min 0
        CancelOldDelay,
        OutputAdd,                  // min 2 handled by clamp
        NoLighthouse,
        MinFiresRequired,
        PatternMustRepeat,
        PruneProtect,
        NoBridgePenalty,
        ConvergeBonusAdd,
        MemoryLightPercent,
        OldDisableAdd,
        ChargeAtRoundStart,
        GateDisable,
        CoinsAdd,
        ConductCountExtra,          // every conduct counts +value
        MoveConsumed,
        OnlyLastPatternDoubled,     // 信息流
        AddMultFlat,                // 鸡血 (applied once when used)
        NegativeImmune,             // 冥想
        PriceAdd,                   // 购物车
        FreeRerolls,                // 购物车
        MemoryPlasticityAtSettle,   // 躺平
        CopyLastPattern,            // 梗图 (applied once when used)
        FreshnessStepsUntilStageEnd,// 梗图
    }

    [Serializable]
    public sealed class Effect
    {
        public Trigger trigger = Trigger.Passive;
        public Op op;
        public int value;
        public int shape = -1;              // Shape index or -1
        public Region region = Region.None;
        public Region region2 = Region.None;
        public bool invertRegion;           // condition: region != region
        public bool memoryOrInstinct;
        public int clampMin = 2;

        public Effect(Op op, int value) { this.op = op; this.value = value; }
        public Effect OfShape(Shape s) { shape = (int)s; return this; }
        public Effect In(Region r) { region = r; return this; }
        public Effect NotIn(Region r) { region = r; invertRegion = true; return this; }
        public Effect Between(Region a, Region b) { region = a; region2 = b; return this; }
        public Effect MemOrInstinct() { memoryOrInstinct = true; return this; }
        public Effect Clamp(int min) { clampMin = min; return this; }
        public Effect On(Trigger t) { trigger = t; return this; }
    }

    public enum DefKind { Item, Drug, Gate }

    public sealed class EffectDef
    {
        public string id;
        public string name;
        public DefKind kind;
        public bool warning;
        public bool firstBatch;
        public int stage = -1;              // gates: stage
        public string desc;
        public bool needsSparkTarget;       // 海螺
        public bool needsRegionTarget;      // 耳机
        public List<Effect> now = new List<Effect>();       // items: while held; drugs: this round; gates: boss round
        public List<Effect> next = new List<Effect>();      // drugs: next round(s)
        public int nextRounds = 1;
        public List<Effect> permanent = new List<Effect>(); // drugs: rest of run
        public bool rare;

        EffectDef Now(params Effect[] e) { now.AddRange(e); return this; }
        EffectDef Next(int rounds, params Effect[] e) { nextRounds = rounds; next.AddRange(e); return this; }
        EffectDef Perm(params Effect[] e) { permanent.AddRange(e); return this; }

        public static readonly List<EffectDef> All = new List<EffectDef>();
        static readonly Dictionary<string, EffectDef> byId = new Dictionary<string, EffectDef>();
        public static EffectDef Get(string id) { byId.TryGetValue(id, out var d); return d; }

        static EffectDef Item(string id, string name, bool first, string desc) => Add(new EffectDef { id = id, name = name, kind = DefKind.Item, firstBatch = first, desc = desc });
        static EffectDef Drug(string id, string name, bool first, bool warn, string desc) => Add(new EffectDef { id = id, name = name, kind = DefKind.Drug, firstBatch = first, warning = warn, desc = desc });
        static EffectDef Gate(string id, string name, int stage, string desc) => Add(new EffectDef { id = id, name = name, kind = DefKind.Gate, stage = stage, desc = desc });
        static EffectDef Add(EffectDef d) { All.Add(d); byId[d.id] = d; return d; }

        static EffectDef()
        {
            // ---------------- items ----------------
            Item("pacifier", "奶嘴", true, "每轮第一次闪光的光量 ×2").Now(new Effect(Op.LightPercentFirstFire, 200));
            Item("homework", "作业本", true, "传导闪光时光量 +3").Now(new Effect(Op.LightAddOnFire, 3).OfShape(Shape.Conduct).On(Trigger.OnFire));
            Item("blocks", "积木", true, "锥体闪光时加法倍率 +1").Now(new Effect(Op.AddMultOnFire, 1).OfShape(Shape.Cone).On(Trigger.OnFire));
            Item("telescope", "望远镜", true, "涟漪半径 +1").Now(new Effect(Op.RippleRadiusAdd, 1));
            Item("alarm", "闹钟", true, "起点次数 +1").Now(new Effect(Op.StartsAdd, 1));
            Item("lens", "镜片", true, "选中起点时显示前 2 拍的传播预演").Now(new Effect(Op.PreviewBeats, 2));
            var conch = Item("conch", "海螺", true, "每轮指定 1 个灵光（悬停按 H）：被涟漪推到阈值而闪时也会产生涟漪").Now(new Effect(Op.RippleRecursionDesignate, 1));
            conch.needsSparkTarget = true;
            Item("computer", "电脑", true, "每轮 1 次试运行（T）：完整模拟并播放一次确认，结束后自动撤回").Now(new Effect(Op.TrialRun, 1));
            var phones = Item("earphones", "耳机", true, "指定一个区域（悬停按 E）阈值 −2（最低 2），可抵消焦虑；跨区域的光丝送电 −1")
                .Now(new Effect(Op.DesignatedRegionThresholdAdd, -2), new Effect(Op.CrossRegionDeliveryAdd, -1));
            phones.needsRegionTarget = true;
            Item("demerit", "处分条", true, "持有期间新鲜度每档惩罚额外 +5%；在已解锁超过一个阶段的区域内，光丝每次导通计 2 次")
                .Now(new Effect(Op.FreshnessPenaltyPercent, 5), new Effect(Op.ConductCountBonusOldRegion, 1));
            Item("diary", "日记本", false, "青春期修剪时保护 3 条光丝").Now(new Effect(Op.PruneProtect, 3));
            Item("bicycle", "自行车", false, "窄桥光丝没有初始惩罚").Now(new Effect(Op.NoBridgePenalty, 1));
            Item("loveletter", "情书", false, "汇聚的每个来源额外光量 +5").Now(new Effect(Op.ConvergeBonusAdd, 5));
            Item("oldphoto", "旧照片", false, "记忆光量 +20%").Now(new Effect(Op.MemoryLightPercent, 20));
            Item("thermos", "保温杯", false, "老年每轮失效的格子数 −1").Now(new Effect(Op.OldDisableAdd, -1));
            Item("readingglasses", "老花镜", false, "抵消老年的光丝延迟 +1").Now(new Effect(Op.CancelOldDelay, 1));

            // ---------------- drugs ----------------
            Drug("coffee", "咖啡", true, false, "本轮所有阈值 −2（最低 2）；下一轮所有阈值 +2")
                .Now(new Effect(Op.ThresholdAdd, -2)).Next(1, new Effect(Op.ThresholdAdd, 2));
            Drug("feverpatch", "退烧贴", true, false, "本轮所有已闪灵光改为“只传导、不计分”：再次闪不抬阈值，但不计光量")
                .Now(new Effect(Op.FiredNoScore, 1));
            Drug("shortvideo", "短视频", true, true, "本轮乘法倍率 ×2；之后整局新鲜度惩罚永久 +1 档")
                .Now(new Effect(Op.SettleMultPercent, 200)).Perm(new Effect(Op.FreshnessStepsAdd, 1));
            Drug("allnighter", "熬夜", true, true, "本轮 C 区阈值 −2（最低 2）；C 区与 F 区之间的光丝送电 −2；下一轮起点次数 −1")
                .Now(new Effect(Op.ThresholdAdd, -2).In(Region.C), new Effect(Op.RegionPairDeliveryAdd, -2).Between(Region.C, Region.F))
                .Next(1, new Effect(Op.StartsAdd, -1));
            Drug("milktea", "奶茶", true, true, "本轮所有光量 +50%；下一轮所有光量 −30%")
                .Now(new Effect(Op.LightPercentAll, 50)).Next(1, new Effect(Op.LightPercentAll, -30));
            Drug("multitask", "多任务", false, true, "本轮起点次数 +2；本轮所有光丝延迟 +1")
                .Now(new Effect(Op.StartsAdd, 2), new Effect(Op.DelayAdd, 1));
            Drug("chickenblood", "鸡血", false, true, "本轮加法倍率 +5；之后两轮所有阈值 +2")
                .Now(new Effect(Op.AddMultFlat, 5)).Next(2, new Effect(Op.ThresholdAdd, 2));
            Drug("meditation", "冥想", false, false, "本轮起点次数 −1；本轮所有负电量与阈值上升类效果失效")
                .Now(new Effect(Op.StartsAdd, -1), new Effect(Op.NegativeImmune, 1));
            Drug("exercise", "运动", false, false, "用掉本轮的移动次数；本轮每次导通额外计 1 次")
                .Now(new Effect(Op.MoveConsumed, 1), new Effect(Op.ConductCountExtra, 1));
            Drug("energydrink", "能量饮料", false, false, "本轮所有输出 +2；下一轮所有输出 −2（最低 2）")
                .Now(new Effect(Op.OutputAdd, 2)).Next(1, new Effect(Op.OutputAdd, -2));
            Drug("melatonin", "褪黑素", false, false, "本轮起点次数 −1；下一轮开始时所有灵光电量 +2")
                .Now(new Effect(Op.StartsAdd, -1)).Next(1, new Effect(Op.ChargeAtRoundStart, 2));
            Drug("umbrella", "雨伞", false, false, "本轮关口规则失效").Now(new Effect(Op.GateDisable, 1));
            Drug("feed", "信息流", false, true, "本轮只有上一次识别出的回路型能得到加成，且加成 ×2；其他回路型本轮无加成")
                .Now(new Effect(Op.OnlyLastPatternDoubled, 1));
            Drug("meme", "梗图", false, true, "本轮复制上一轮最高回路型的加成一次；新鲜度惩罚 +2 档，持续到本阶段结束")
                .Now(new Effect(Op.CopyLastPattern, 1), new Effect(Op.FreshnessStepsUntilStageEnd, 2));
            Drug("cart", "购物车", false, true, "本轮商店免费刷新 2 次、物品价格 −2；下一轮结算少得 2 余光")
                .Now(new Effect(Op.FreeRerolls, 2), new Effect(Op.PriceAdd, -2)).Next(1, new Effect(Op.CoinsAdd, -2));
            Drug("lieflat", "躺平", false, false, "本轮起点次数 −1；本轮结算时所有记忆可塑度 +1；下一轮开始时所有灵光电量 +2")
                .Now(new Effect(Op.StartsAdd, -1), new Effect(Op.MemoryPlasticityAtSettle, 1)).Next(1, new Effect(Op.ChargeAtRoundStart, 2));

            // ---------------- gates ----------------
            Gate("separation", "分离", 0, "本轮灯塔不提供电量").Now(new Effect(Op.NoLighthouse, 1));
            Gate("training", "培训班", 1, "本轮至少 6 个灵光闪过，否则未达标；锥体发出的光丝延迟 −1（最低 0）")
                .Now(new Effect(Op.MinFiresRequired, 6), new Effect(Op.DelayAdd, -1).OfShape(Shape.Cone));
            Gate("imitation", "模仿", 2, "本轮最高回路型须与上一轮相同（上一轮没有则本轮也不能有），否则回路型加成作废")
                .Now(new Effect(Op.PatternMustRepeat, 1));
            Gate("anxiety", "焦虑", 3, "C 区灵光阈值 −2（最低 2），其他区域阈值 +2")
                .Now(new Effect(Op.ThresholdAdd, -2).In(Region.C), new Effect(Op.ThresholdAdd, 2).NotIn(Region.C));
            Gate("question", "？", 5, "起点次数只有 1；所有记忆与本能灵光阈值降为 1（0.5）")
                .Now(new Effect(Op.StartsSet, 1), new Effect(Op.ThresholdSet, 1).MemOrInstinct().Clamp(1));
        }
    }
}
