using System;

namespace LingGuang.Core
{
    /// <summary>
    /// All tunable numbers. Internal charge units are display ×2 (display = internal / 2).
    /// Plain serializable class so the presenter can hot-reload it from JSON (JsonUtility).
    /// </summary>
    [Serializable]
    public class ShapeDef
    {
        public string name;
        public int[] dirs;      // output directions relative to orientation
        public int threshold;   // internal units
        public int light;
        public int output;      // internal units
    }

    [Serializable]
    public class PatternDef
    {
        public string id;
        public string name;
        public int light;
        public int mult;
        public int minStage;    // Stage index at which it starts being recognised
    }

    [Serializable]
    public class BridgeDef
    {
        public int row;
        public int openStage;   // Stage index from which the bridge is open
    }

    [Serializable]
    public class GameConfig
    {
        // ---------- board ----------
        public string[] mapRows =
        {
            "...FFFF...",
            "..FFFFFF..",
            "TTCCCCCCTT",
            "TTKCCCK.TT",
            "..KKKKK...",
            "..IIIIII..",
            "....II....",
        };
        public int midlineLeftCol = 4;                 // gully between col 4 and col 5
        public BridgeDef[] bridges =
        {
            new BridgeDef { row = 5, openStage = 0 },
            new BridgeDef { row = 3, openStage = 2 },
            new BridgeDef { row = 1, openStage = 3 },
        };
        public int[] outerGapPairs = { 1, 2, 2, 2, 7, 2, 8, 2 }; // (c,r)-(c,r) pairs, T outer gully gaps
        public int secondaryGulliesMin = 3;
        public int secondaryGulliesMax = 4;

        // ---------- chain ----------
        public int leakPerBeat = 1;
        public bool leakAfterLastBeat = false;
        public int refireThresholdStep = 2;
        public int refireRaiseCap = 6;          // max total refire raise per round (internal units); -1 = unlimited (spec v0.8 original)
        public bool carryOverFire = true;       // fired-this-beat sparks still over threshold fire next beat
        public bool infiniteEnabled = true;     // detect self-sustaining loops instead of running into maxBeats
        public int infiniteCredits = 6;         // a detected loop scores this many extra cycles
        public int startOutputBonus = 2;
        public int maxBeats = 200;
        public int stpStep = 1;
        public int stpCap = 2;

        public ShapeDef[] shapes =
        {
            new ShapeDef { name = "传导", dirs = new[] { 0 },             threshold = 2, light = 5,  output = 2 },
            new ShapeDef { name = "锥体", dirs = new[] { 0, 2, 4 },       threshold = 4, light = 10, output = 2 },
            new ShapeDef { name = "汇聚", dirs = new[] { 0 },             threshold = 6, light = 20, output = 4 },
            new ShapeDef { name = "本能", dirs = new[] { 0, 1, 2, 3, 4, 5 }, threshold = 2, light = 8, output = 2 },
        };
        public int convergeMinSources = 3;
        public int convergeBonusPerSource = 10;

        // ---------- edges ----------
        public int thickAt = 3;
        public int myelinAt = 6;
        public int levelOutputBonus = 2;
        public int baseDelay = 1;
        public int bridgePenaltyAmount = 1;
        public int bridgePenaltyDelay = 1;
        public int bridgeGraduateAt = 3;

        // ---------- ripples ----------
        public int rippleBaseRadius = 1;
        public int rippleMergeDistance = 2;
        public int rippleRadiusCap = 3;
        public int rippleCharge = 1;
        public bool weakLinksEnabled = true;
        public int weakLinkActiveAt = 3;
        public int weakLinkCharge = 1;

        // ---------- lighthouse ----------
        public int lighthouseCharge = 2;
        public int lighthouseAnchorRow = 6;
        public int[] lighthouseRadiusByStage = { 3, 2, 2, 1, 1, 0 }; // covers distance <= R-1 ; 0 = old-age memory focus
        public string lighthouseFocus = "Memory"; // or "Character"

        // ---------- use & disuse ----------
        public int[] decayRoundsByStage = { 6, 6, 6, 6, 6, 1 };
        public int decayAmount = 3;
        public int youthPruneBonus = 3;
        public int memoryMin = 3;
        public int memoryMax = 6;
        public int memoryThreshold = 2;
        public int plasticityInit = 3;
        public int plasticityMax = 3;
        public int rootRounds = 3;
        public int driftAfterRounds = 2;
        public int instinctCount = 3;
        public int instinctLightDecay = 2;
        public int instinctOldGain = 2;
        public int instinctLightMax = 8;
        public int oldDisableCellsPerRound = 2;

        // ---------- rounds ----------
        public int startsPerRound = 2;
        public int movesPerRound = 1;
        public int movesBossRound = 2;
        public int candidatesPerRound = 3;
        public int[] targets = { 30, 45, 60, 120, 180, 240, 400, 600, 800, 1200, 1800, 2400, 3000, 4500, 6000, 5000, 7500, 10000 };
        public string[] stageNames = { "婴儿", "童年", "青春期", "成年", "中年", "老年" };
        public int[] itemSlotsByStage = { 0, 2, 3, 5, 5, 5 };
        public int pickupStage = 1;
        public int pickupMin = 1;
        public int pickupMax = 2;
        public int pickupLife = 2;
        public int shopFromStage = 2;

        // ---------- scoring ----------
        public PatternDef[] patterns =
        {
            new PatternDef { id = "infinite", name = "无限回荡", light = 100, mult = 6, minStage = 0 },
            new PatternDef { id = "long",     name = "长链", light = 30, mult = 4, minStage = 1 },
            new PatternDef { id = "converge", name = "汇聚", light = 25, mult = 3, minStage = 2 },
            new PatternDef { id = "diverge",  name = "发散", light = 20, mult = 3, minStage = 1 },
            new PatternDef { id = "short",    name = "短链", light = 10, mult = 2, minStage = 1 },
            new PatternDef { id = "spark",    name = "火花", light = 5,  mult = 1, minStage = 1 },
        };
        public int longChainBeats = 4;
        public int shortChainBeats = 2;
        public int divergeMinFires = 3;
        public int freshnessFromStage = 3;
        public float freshnessStep = 0.1f;
        public float freshnessMin = 0.5f;

        // ---------- economy ----------
        public int coinsPerRound = 3;
        public int coinsBonusOverTarget = 1;
        public int itemPrice = 5;
        public int drugPrice = 3;
        public int shopItems = 2;
        public int shopDrugs = 1;
        public int drugCarryMax = 2;
        public int drugsPerRound = 1;

        // ---------- insight 领悟 (P1, §14 lite) ----------
        public bool insightEnabled = true;
        public int interestPer = 5;          // 1 insight per this many coins held at settlement
        public int interestMax = 3;
        public int insightPerLevel = 3;      // 精进: points per pattern level
        public int patternLevelMax = 5;
        public int levelLightPercent = 10;   // pattern light +% per level
        public float levelMult = 0.25f;      // pattern mult + per level
        public int reviewCost = 1;           // 复盘
        public int rerollCost = 1;           // 再来一次

        // ---------- characters (P1) ----------
        public bool charactersEnabled = true;
        public int[] characterSpawnPermilleByStage = { 0, 350, 450, 350, 300, 200 };
        public int[] characterTargetByStage = { 0, 6, 8, 6, 6, 4 };
        public int characterHardCap = 12;
        public int characterFamiliarAt = 3;
        public int characterKnownAt = 6;

        public GameConfig Clone()
        {
            return (GameConfig)MemberwiseClone();
        }

        public ShapeDef Shape(Shape s) { return shapes[(int)s]; }
        public int Target(int roundIndex) { return targets[Math.Min(roundIndex, targets.Length - 1)]; }
    }
}
