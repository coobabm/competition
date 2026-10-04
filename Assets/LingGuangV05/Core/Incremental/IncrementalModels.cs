using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Incremental
{
    /// <summary>What one hex of the neural network (the skill tree) does once it is lit.</summary>
    public enum SkillEffect { Input, Mult, Speed, Accuracy, Manual, Efficiency, Job, OutputYes, OutputNo }

    public sealed class SkillCell
    {
        public int q, r;
        public SkillEffect effect;
        /// <summary>Mult: factor. Speed: added fraction. Accuracy: added probability. Manual: added points. Efficiency: watt factor. Job: job index unlocked.</summary>
        public double value;
        public string name = "", description = "";
        public int Index { get { return q * IncrementalCatalog.Rows + r; } }
    }

    /// <summary>A kind of work the AI can take on. Each owned level is one parallel worker; costs grow geometrically.</summary>
    public sealed class JobDef
    {
        public string id = "", name = "", description = "";
        /// <summary>Questions per second per level, before GPUs and tree speed.</summary>
        public double rate;
        /// <summary>Points and money paid per correct answer, before multipliers.</summary>
        public double score, pay;
        /// <summary>Subtracted from the network's accuracy for this job.</summary>
        public double difficulty;
        /// <summary>Points for the first level and per-level cost growth.</summary>
        public double baseCost, growth;
        /// <summary>Each owned-level milestone doubles this job's speed. Schedules differ per job, so the best buy keeps changing.</summary>
        public int[] milestones = new int[0];
    }

    public sealed class HardwareDef
    {
        public string id = "", name = "", description = "";
        public double basePrice, growth, compute, watts;
        /// <summary>GPU slots provided (cases). GPUs use one slot each.</summary>
        public int slots;
        public bool isGpu;
        /// <summary>Hidden from the shop until lifetime money reaches this amount.</summary>
        public double revealAtTotalMoney;
    }

    [Serializable] public sealed class IncrementalState
    {
        public int version = 1;
        public double score, money, totalScore, totalMoney;
        public double gameSeconds, daySeconds, energyKwh, billDue, tickRemainder;
        public int day = 1;
        public bool unpaidPower;
        public int manualAnswers, manualCorrect, streak, bestStreak;
        /// <summary>Fractional auto answers carried between ticks so totals are partition independent.</summary>
        public double autoAnswers, autoCorrect;
        /// <summary>Lit tree cells by index (q * 5 + r). The input cell is always lit.</summary>
        public List<int> litCells = new List<int>();
        /// <summary>Owned level per job, indexed like IncrementalCatalog.Jobs.</summary>
        public List<int> jobLevels = new List<int>();
        /// <summary>Correct answers per job (for display and future achievements).</summary>
        public List<double> jobCorrect = new List<double>();
        /// <summary>Owned count per hardware, indexed like IncrementalCatalog.Hardware.</summary>
        public List<int> hardware = new List<int>();
        public int powerLevel;
        public double sparkReadyAt, boostUntil;
        public bool examPassed;
    }

    [Serializable] public sealed class IncrementalConfig
    {
        public double starterMoney = 650, manualBase = 1, baseAccuracy = .60, maxAccuracy = .98, minJobAccuracy = .05;
        public double treeBaseCost = 20, treeGrowth = 2.3;
        public double basePowerWatts = 200, powerStepWatts = 400, startPowerWatts = 500, powerUpgradeBase = 600, powerUpgradeGrowth = 1.75;
        public double dayLengthSeconds = 120, electricityPrice = .52, outageRepaymentPerAnswer = 2, gpuResaleFraction = .6;
        public double sparkCooldown = 180, sparkDuration = 30, sparkFactor = 3;
        public int streakCap = 20; public double streakBonusPerAnswer = .025;
        public double examPassMultiplier = 2, examReward = 800, examRequiredAccuracy = .75;
        public double maxTickSeconds = 86400;
    }
}
