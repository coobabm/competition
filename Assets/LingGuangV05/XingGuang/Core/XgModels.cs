using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// What 灵光 borrows from the shared household economy. The desktop adapter maps it onto ChapterOneSim:
    /// compute = GPU heartbeats, VRAM = GPU memory, money = the one ¥ wallet, training load = the household power bill.
    /// </summary>
    public interface IXgHost
    {
        /// <summary>GPU work units per second; 0 when the power is off.</summary>
        double Compute { get; }
        double VramMB { get; }
        double Money { get; }
        bool Spend(double amount);
        void Earn(double amount);
        /// <summary>GPUs ran at training load for this many seconds; the host bills the electricity.</summary>
        void Train(double seconds);
        /// <summary>Why training cannot run right now (e.g. power cut), or null.</summary>
        string Blocker { get; }
    }

    /// <summary>One model per track: its shape and its training progress, advanced one epoch at a time.</summary>
    [Serializable]
    public sealed partial class XgRun
    {
        public int track;
        public string arch = "", dataset = "";
        public int depth = 2, width, lr = 3;
        /// <summary>Automatic training (crontab level and up) is switched on for this track.</summary>
        public bool running, autoLr = true;
        public double steps, trainAcc, valAcc;
        /// <summary>Epochs trained, epochs since the last assessment, assessments in a row without a record.</summary>
        public int epoch, sinceEval, staleEvals;
        public bool epochActive, epochHand;
        public double epochProgress, epochDuration;
        public double lastScore = -1;
        public List<float> histTrain = new List<float>();
        public List<float> histVal = new List<float>();
        public double histTimer;
    }

    /// <summary>The deployed checkpoint for a dataset (best validation accuracy saved so far).</summary>
    [Serializable]
    public sealed class XgBest { public string dataset = "", arch = ""; public double acc; public int modelId; }

    /// <summary>
    /// One saved model in the 模型仓库: the shape and training state at save time, its score and a downsampled
    /// validation curve. Saved automatically on every assessment record, or by hand from the training page.
    /// </summary>
    [Serializable]
    public sealed class XgModelEntry
    {
        public int id, track, depth, width, lr, epoch;
        public string dataset = "", arch = "", name = "";
        public double steps, acc, trainAcc, score, gameSeconds;
        /// <summary>Game date it was saved on (yyyymmdd; 0 for saves from before the calendar).</summary>
        public int date;
        /// <summary>Saved by an assessment record (otherwise by hand).</summary>
        public bool record;
        /// <summary>Starred models are never cleaned up.</summary>
        public bool starred;
        public List<float> curve = new List<float>();
        /// <summary>
        /// The weights: a copy of the brain's region as it was (the newest checkpoints and the starred ones keep them;
        /// older ones keep only their settings to keep the save small).
        /// </summary>
        public List<XgConcept> weights = new List<XgConcept>();
        public bool HasWeights => weights != null && weights.Count > 0;
    }

    [Serializable]
    public sealed class XgLabelCount { public string dataset = ""; public double count; }

    /// <summary>
    /// A yes/no card in the 标注台. Fields are shared by all desks; each desk uses the ones it needs:
    /// digits (digit/asked/seed), poems (line/shown/askedChar), text desks (question/why/category/source),
    /// captcha (digit = drawn icon, asked = asked icon, shown = second icon or -1), meme faces (digit = face,
    /// asked = asked mood, line = caption), go (line = 81-cell board, digit = a stone of the marked group,
    /// asked = ✕ cell or -1, shown = liberties asked about).
    /// </summary>
    [Serializable]
    public sealed partial class XgCard
    {
        public int track;
        public string dataset = "";
        public int digit, asked, seed;
        public string line = "", askedChar = "";
        public int shown;
        public bool truth;
        public string question = "", questionEn = "", why = "", whyEn = "", category = "", categoryEn = "";
        /// <summary>Where the meme or phrase comes from, shown after a wrong answer.</summary>
        public string source = "";
        public int level;
        /// <summary>前方高能: pays ×3 but must be answered within <see cref="timeLimit"/> seconds.</summary>
        public bool gold;
        /// <summary>老司机题: looks like one answer, is the other. Right answers add 2 to the combo.</summary>
        public bool trick;
        public double age, timeLimit;
        /// <summary>Fixed roll that decides whether the model's suggestion for this card is right.</summary>
        public double roll;
    }

    /// <summary>Outcome of one hand answer, for the feedback flash.</summary>
    public partial struct XgAnswer { public bool correct, truth, gold, trick, timeout, accepted, queued, corrected, bounty; public double pay; public int combo, samples; public long cardId; }

    /// <summary>One press of 训练一轮.</summary>
    public sealed class XgEpoch
    {
        public int track, epoch;
        public bool hand, diverged;
        public double steps, trainAcc, valAcc;
        /// <summary>The round went down instead of up (训练不再需要调参数: a drop), and why.</summary>
        public bool dropped;
        public string dropReason = "", dropReasonEn = "";
        /// <summary>What the round changed in accuracy (negative after a drop, 0 on a plateau).</summary>
        public double gain;
        /// <summary>Set when this epoch triggered an assessment.</summary>
        public XgAssessment assessment;
    }

    /// <summary>The settlement after training: model score 0–1000, grade, reward.</summary>
    public sealed class XgAssessment
    {
        public int track;
        public string dataset = "";
        public double acc, score, previousBest, reward, gradeBonus;
        public int grade, newGrade = -1;
        public bool record, hand;
        /// <summary>Accuracy lost to 近亲繁殖 (its own uncaught wrong labels in the data); 0 when the data is clean enough.</summary>
        public double inbreeding;
        /// <summary>The 模型仓库 entry saved for this record (0 if none).</summary>
        public int modelId;
    }

    [Serializable]
    public sealed partial class XgState
    {
        public int version = 1;
        public int selected;
        public XgRun vision = new XgRun { track = 0, arch = "lenet", dataset = "mnist", depth = 2, lr = 3 };
        public XgRun sequence = new XgRun { track = 1, arch = "rnn", dataset = "poems", depth = 1, lr = 4 };
        public double totalIncome, totalSpent, trainedSeconds;
        public List<string> unlocked = new List<string> { "lenet", "rnn" };
        public List<string> owned = new List<string>();
        public List<XgLabelCount> labels = new List<XgLabelCount>();
        /// <summary>Current card per desk (keyed by dataset).</summary>
        public List<XgCard> cards = new List<XgCard>();
        public string desk = "mnist";
        public List<string> desksOpen = new List<string>();
        public int handCorrect, handWrong;
        /// <summary>Auto-answer level and timer per desk.</summary>
        public List<XgLabelCount> autoLevels = new List<XgLabelCount>();
        public List<XgLabelCount> autoTimers = new List<XgLabelCount>();
        public double autoCorrect, autoWrong;
        public List<string> contracts = new List<string>();
        public List<XgBest> best = new List<XgBest>();
        /// <summary>Grades already reached per dataset ("mnist#3" = A), for the one-time grade bonus.</summary>
        public List<string> grades = new List<string>();
        public List<string> log = new List<string>();
        public long rng = 0x5EED5EEDL;
        public int nanEvents, clicks, epochs, assessments, records;
        /// <summary>The one combo shared by labelling and training; only hand actions count.</summary>
        public int combo, bestCombo;
        /// <summary>加薪 level (0–17): hand-labelling pay multiplier, see XgCatalog.RaiseMultipliers.</summary>
        public int payRaise;
        /// <summary>模型仓库: every saved model, newest last.</summary>
        public List<XgModelEntry> models = new List<XgModelEntry>();
        public int nextModelId = 1;
        public double comboTimer;
        public double autoTrainTimer;
        public bool reduceFx, mute;
        /// <summary>灵光 game clock when the lab last saved.</summary>
        public double linkedGameSeconds;

        // Fields of the first layout, read once by XgSim.Repair and then left empty.
        public int labelTrack, labelCombo, autoVision, autoSequence, autoLogic;
        public XgCard visionCard, sequenceCard, logicCard;
    }
}
