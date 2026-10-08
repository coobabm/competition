using System;
using System.Collections.Generic;

namespace LingGuangV05.Core
{
    public enum NodeKind { Input, Hidden, Yes, No }
    [Serializable] public sealed class NodeState { public int id, q, r; public NodeKind kind; public double charge, fatigue; }
    [Serializable] public sealed class EdgeState { public int from, to; public double weight; }
    [Serializable] public sealed class CardData { public int id; public string prompt = "", explanation = "", category = ""; public bool expectedYes, predictedYes; public double confidence; }
    [Serializable] public sealed class CardResolution { public int cardId; public bool correct, corrected; public string explanation = ""; }
    [Serializable] public sealed class TrainingPulse { public int[] nodeIds = new int[0]; public bool correction; }
    [Serializable] public sealed class GameState
    {
        public int version = 1, chapter = 1, day = 1, gpuCount = 1, caseCount = 1, semanticLevel = 1, trainingLevel = 1;
        public int cardsReviewed, correctedCards, examAttempts, examAnswered, examCorrect, nextCardId = 1;
        public double money = 0, samples = 6, steps, learningPoints, gameSeconds, daySeconds, billDue, totalEarned, temperature = 30;
        public double heartbeatAccumulator, tickRemainder, energyKwh, outageDebtRepaid;
        public long lastSeenUnix, rngState = 12648430, examTemplateMask;
        public bool jobEnabled, breakerTripped, unpaidPower, examActive, chapterOneComplete, examRewardGranted, lastExamPassed;
        public string aiName = "the code";
        /// <summary>灵光.exe install: 0 = save from before the download flow (treated as installed), 1 = not yet downloaded, 2 = downloaded.</summary>
        public int appInstallState;
        public List<NodeState> nodes = new List<NodeState>();
        public List<EdgeState> edges = new List<EdgeState>();
        public List<string> mistakes = new List<string>();
        public List<int> activeRoute = new List<int>();
        public List<EdgeState> activeRouteEdges = new List<EdgeState>();
        public CardData activeCard;
        /// <summary>Story memory (fired beats, flags, firsts). Additive field: older saves load with an empty story.</summary>
        public LingGuangV05.Core.Story.StoryState story = new LingGuangV05.Core.Story.StoryState();
        /// <summary>灵光 lab progress (XgState as JSON). Additive: older saves load with an empty lab.</summary>
        public string labState = "";
        /// <summary>YY chat history (YYState as JSON). Additive: older saves load with an empty chat.</summary>
        public string chatState = "";
        /// <summary>摆渡贴吧 private messages and posts (Core.Forum.ForumState as JSON). Additive.</summary>
        public string forumState = "";
        /// <summary>When the 2016 calendar moved to which day (design v1.1 §11.7). Additive: older saves start on the prologue day.</summary>
        public List<EraMark> eraMarks = new List<EraMark>();
        /// <summary>Dated events (era_events.json) already delivered or skipped.</summary>
        public List<string> eraDelivered = new List<string>();
        /// <summary>The prologue (design v1.1 §6): 0 = save from before it existed (treated as done), 1 = running (never written to disk), 2 = done.</summary>
        public int prologue;
        /// <summary>Opening setup (§6 Step 8): how it calls itself and you, the personality sentence as typed.</summary>
        public string aiSelf = "", aiCallMe = "", personality = "";
        /// <summary>Personality axis targets 0–100 read from that sentence (§11.4): 温度 冷静↔热情, 玩心 正经↔皮, 主见 顺从↔有主见.</summary>
        public double targetWarmth = 50, targetPlay = 50, targetOpinion = 50;
        /// <summary>Up to two free tone words from the sentence (extra tone elements).</summary>
        public List<string> toneWords = new List<string>();
        /// <summary>Play time at which the ending turned the clock to 2017-01-01 00:00 (0 = not yet).</summary>
        public double newYearAt;
        /// <summary>The ending's desktop sequence (sophon.dll's water drop, 2017, the last 360 bubble) has played.</summary>
        public bool endingPlayed;
        /// <summary>Answer to 老周's first message: "odd" (电脑出了点怪事) or "game".</summary>
        public string laoZhouFirstReply = "";
        /// <summary>Model id of every owned card (Core.Hardware.HardwareCatalog), as many as gpuCount. Additive: an older save gets one starting card per card.</summary>
        public List<string> gpuModels = new List<string>();
        /// <summary>The Samsung 950 Pro NVMe from 淘货 is installed.</summary>
        public bool nvme;
        /// <summary>Retired 网吧 machines bought on 喵鱼 (each is one of caseCount).</summary>
        public int cafeBoxes;
        /// <summary>An RX 480 already tripped the breaker once.</summary>
        public bool pcieBurned;

        // ── Rent, bills and bankruptcy (ChapterOneSim.Bills.cs). Additive: an older save starts paying from the day it is loaded on.
        /// <summary>0 = a save from before the bills; 1 = rent and bills apply.</summary>
        public int economyVersion;
        /// <summary>The last calendar day (GameCalendar day index) whose rent is paid; 0 before the first rent.</summary>
        public int rentDay;
        /// <summary>Daily settlements in a row that ended with the wallet below zero (1 = the landlord texts, 2 = power cut, 3 = sold).</summary>
        public int debtDays;
        /// <summary>The landlord cut the power for unpaid rent; the computer was sold (the failure ending); 重新开始 was chosen.</summary>
        public bool landlordCut, bankrupt, restartChosen;
        /// <summary>What the computer fetched when it was sold (the failure ending's black screen).</summary>
        public double soldFor;
        /// <summary>The month (yyyymm) whose broadband is paid, and the month whose training electricity <see cref="monthKwh"/> counts.</summary>
        public int broadbandMonth, kwhMonth;
        /// <summary>Training electricity this calendar month (kWh), for the 阶梯电价 tiers.</summary>
        public double monthKwh;
        /// <summary>The calendar day a promised weekend trip's train ticket is paid (0 = none booked).</summary>
        public int ticketDay;
        /// <summary>Seconds of training in a row (gaps under a few seconds keep it going) and when the last one ended.</summary>
        public double trainingStreak, lastTrainingAt;
        /// <summary>Cards at the repair shop after failing under heavy training; each comes back by itself.</summary>
        public List<CardRepair> repairs = new List<CardRepair>();
        /// <summary>Running totals of every household bill, and the same for the current calendar day.</summary>
        public HouseBills bills = new HouseBills(), billsToday = new HouseBills();
        /// <summary>The calendar day <see cref="billsToday"/> belongs to.</summary>
        public int billsDay;
    }

    /// <summary>A card at the repair shop: its model and the play time it comes back.</summary>
    [Serializable] public sealed class CardRepair { public string model = ""; public double readyAt; }

    /// <summary>Household bills by kind, in yuan.</summary>
    [Serializable] public sealed class HouseBills
    {
        public double rent, power, idlePower, aircon, broadband, repair, breaker, ticket;
        public double Total => rent + power + idlePower + aircon + broadband + repair + breaker + ticket;
    }

    [Serializable] public sealed class GameConfig
    {
        /// <summary>
        /// Rent per calendar day (from October the landlord raises it), broadband on the first of each month, the
        /// summer air conditioner per day in July and August, the electrician after a breaker trip and the train
        /// ticket of a promised weekend trip (ChapterOneSim.Bills.cs).
        /// </summary>
        public double rentPerDay = 300, rentPerDayFromOctober = 350, broadbandPerMonth = 100, summerAirconPerDay = 40, breakerRepairFee = 50, weekendTicket = 120;
        /// <summary>
        /// Training electricity: the cards run at <see cref="trainingLoad"/> of their rated watts, and one second of a
        /// training round stands for this many hours of GPU time (a 0.6-second round is a day of work).
        /// </summary>
        public double trainingLoad = .5, trainingHoursPerSecond = 40;
        /// <summary>2016 residential 阶梯电价: the month's training kWh above the first tier cost ×1.1, above the second ×1.5.</summary>
        public double tierOneKwh = 240, tierTwoKwh = 400, tierTwoFactor = 1.1, tierThreeFactor = 1.5;
        /// <summary>
        /// Card wear: after this many seconds of training in a row (gaps shorter than <see cref="wearGapSeconds"/> do
        /// not break the streak) each training second has this chance to burn a card out. A repair costs a share of
        /// the card's price and takes this long. The last working card never fails.
        /// </summary>
        public double wearAfterSeconds = 30, wearGapSeconds = 5, wearChancePerSecond = .001, repairShare = .2, repairSeconds = 120;
        /// <summary>What the rest of the computer (i7-6700K, board, 32 GB, case) fetches when it is sold with the cards.</summary>
        public double computerResale = 2500;
        public double dayLengthSeconds = 120, gpuPrice = 450, casePrice = 2000, electricityPrice = .52;
        public int examQuestions = 20, examPassScore = 16;
        public double starterMoney = 1200, starterSamples = 6, memoryPerGpu = 2048, nodeMemory = 128, edgeMemory = 32;
        public int starterGpus = 1, gpusPerCase = 2, maxCases = 20, maxSkillLevel = 5, maxTrainingLevel = 5;
        public double skillPointCost = 5, trainingPointCost = 4, gpuResaleFraction = .6, examReward = 800;
        public int examRequiredCards = 12;
        public double examRequiredSteps = 20, examRequiredAccuracy = .65;
        public double heartbeatPerGpu = 1, trainingSpeedPerLevel = .25, incomePerHeartbeat = 8, samplePerCard = 8;
        public double accuracyFloor = .5, initialAccuracyCap = .82, accuracyCapPerLevel = .035, maxAccuracy = .97, effectiveScaleHalf = 12;
        public double parametersPerHiddenNode = 1, samplesPerParameter = 10, structureBonus = .08;
        public double gpuWatts = 60, caseWatts = 45, powerLimitWatts = 3500, ambientTemperature = 30, heatPerWatt = .06;
        /// <summary>The card a new game starts with (design v1.1 §3) and the card the old-style BuyGpu adds (gpuPrice, a used 750 Ti).</summary>
        public string starterGpuModel = "gtx980ti", legacyGpuModel = "gtx750ti";
        public double thermalTimeConstant = 30, throttleTemperature = 65, maximumTemperature = 100, minimumThrottle = .35;
        public double leakPerSecond = .1, fatigueRecoveryPerSecond = .16, fatiguePerFire = .2, threshold = 1, fatigueThresholdFactor = .4;
        public double signalStrength = 1.7, initialWeight = .85, minimumWeight = .08, maximumWeight = 2;
        public double weightStrengthen = .025, weightDecayPerSecond = .0002, correctionWeightFactor = .8;
        public double outageRepaymentPerCard = 1, maxTickSeconds = 86400;
    }
}
