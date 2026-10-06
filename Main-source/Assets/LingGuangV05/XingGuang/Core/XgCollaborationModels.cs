using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public partial class XgCard
    {
        public long id;
        public bool hasJudgment, guess;
        public double confidence, brainP = -1;
        // source remains the original card's cultural attribution; this is inference provenance.
        public string judgeSource = "", judgeFailure = "";
        public bool awaitingBrain, brainDispatched;
        public int judgeAttempt;
    }

    [Serializable]
    public sealed class XgRouteRecord
    {
        public long cardId;
        public string dataset = "", judgeSource = "";
        public bool automatic, correct, audited;
    }

    [Serializable]
    public sealed class XgBrainRecord
    {
        public string dataset = "";
        public bool correct;
    }

    [Serializable]
    public sealed class XgAutoRecord
    {
        public long cardId;
        public string dataset = "", question = "", judgeSource = "";
        public bool correct, audited, guess;
        public double confidence, pay;
        /// <summary>The 摆渡众包 platform spot-checked this label; fine is what a failed check cost.</summary>
        public bool spotChecked;
        public double fine;
    }

    public partial class XgState
    {
        public int coopVersion, autoLevel;
        public double coopThreshold = .8;
        public long nextCardId = 1;
        public List<XgLabelCount> noise = new List<XgLabelCount>();
        public List<XgCard> prefetch = new List<XgCard>();
        public List<XgCard> queue = new List<XgCard>();
        public List<XgRouteRecord> routeHistory = new List<XgRouteRecord>();
        public List<XgBrainRecord> brainStats = new List<XgBrainRecord>();
        public List<XgAutoRecord> autoFeed = new List<XgAutoRecord>();
    }

    /// <summary>A reserved inference request. Deliberately contains no truth, explanation, roll or full card.</summary>
    public sealed class XgJudgeTicket
    {
        public readonly long cardId, generation;
        public readonly int attempt;
        public readonly string dataset, prompt;
        public XgJudgeTicket(long cardId, long generation, int attempt, string dataset, string prompt)
        { this.cardId = cardId; this.generation = generation; this.attempt = attempt; this.dataset = dataset; this.prompt = prompt; }
    }

    /// <summary>The transport may complete asynchronously; game mutation remains with XgSim.</summary>
    public interface IXgJudge
    {
        bool Ready { get; }
        void Judge(string prompt, Action<double?, string> done);
    }

    public struct XgCollaborationStats
    {
        public int total, automatic, correct, audited;
        public double automaticFraction => total == 0 ? 0 : (double)automatic / total;
        public double automaticAccuracy => automatic == 0 ? 0 : (double)correct / automatic;
    }
}
