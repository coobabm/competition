using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>A value snapshot of one concept actually evaluated by the prediction readout.</summary>
    public sealed class XgUsedConcept
    {
        public int Id { get; }
        public int Layer { get; }
        public string Region { get; }
        public string Key { get; }
        public string Alt { get; }
        public double Weight { get; }
        public double Activation { get; }
        public double Vote { get; }
        public bool Pinned { get; }

        internal XgUsedConcept(XgBoard.Match match, double vote)
        {
            Id = match.c.id; Layer = match.c.layer; Region = match.c.region;
            Key = match.c.key; Alt = match.c.alt; Weight = match.c.w;
            Activation = match.act; Vote = vote; Pinned = match.c.pinned;
        }
    }

    /// <summary>Immutable, transient evidence from one actual Predict call; never stored in XgState.</summary>
    public sealed class XgPredictionTrace
    {
        public bool AnswerYes { get; }
        public bool Guessed { get; }
        public double Score { get; }
        public int TopLayer { get; }
        public IReadOnlyList<XgUsedConcept> UsedConcepts { get; }

        internal XgPredictionTrace(bool answerYes, bool guessed, double score, int topLayer, List<XgUsedConcept> used)
        {
            AnswerYes = answerYes; Guessed = guessed; Score = score; TopLayer = topLayer;
            UsedConcepts = Array.AsReadOnly(used.ToArray());
        }
    }

    public enum XgAnswerTraceSource { LocalBoard, Manual, Family, Law, Shutdown }

    /// <summary>
    /// Values around an actual R1 write or R2 creation, captured before R4 decay, not the final board values.
    /// New R1 cells start at weight 0/strength 1; R2 creations use an absent-state baseline of 0/0.
    /// Pinned, Dropout-skipped and unchanged R1 cells contribute no record.
    /// </summary>
    public sealed class XgConceptPull
    {
        public int Id { get; }
        public double BeforeWeight { get; }
        public double AfterWeight { get; }
        public double BeforeStrength { get; }
        public double AfterStrength { get; }
        public bool Created { get; }
        internal XgConceptPull(XgConcept cell, double weight, double strength, bool created)
        {
            Id = cell.id; BeforeWeight = weight; AfterWeight = cell.w;
            BeforeStrength = strength; AfterStrength = cell.s; Created = created;
        }
    }

    /// <summary>One settled answer. SubmittedYes is a label, not the teaching UI's right/wrong evaluation.</summary>
    public sealed class XgAnswerTrace
    {
        public long CardId { get; }
        public string Dataset { get; }
        public string Region { get; }
        public bool SubmittedYes { get; }
        /// <summary>True only for an answer that reached its settlement exit. Rejected requests emit no event.</summary>
        public bool Settled { get; }
        public XgAnswerTraceSource Source { get; }
        /// <summary>Null when this answer did not use a local Board prediction; never synthesize a path.</summary>
        public XgPredictionTrace Prediction { get; }
        /// <summary>The unchanged return value. Ordinary Answer historically leaves accepted/cardId at defaults.</summary>
        public XgAnswer Outcome { get; }
        public IReadOnlyList<XgConceptPull> CorrectionPulls { get; }

        internal XgAnswerTrace(long cardId, string dataset, bool submittedYes, XgAnswerTraceSource source,
            XgPredictionTrace prediction, XgAnswer outcome, List<XgConceptPull> pulls)
        {
            CardId = cardId; Dataset = dataset; Region = XgBoardData.Region(dataset);
            SubmittedYes = submittedYes; Settled = true; Source = source;
            Prediction = prediction; Outcome = outcome;
            CorrectionPulls = Array.AsReadOnly(pulls == null ? Array.Empty<XgConceptPull>() : pulls.ToArray());
        }
    }

    public sealed partial class XgSim
    {
        /// <summary>Presentation notification after one hand answer settles, including wrong/late/opinion answers.</summary>
        public event Action<XgAnswerTrace> CardAnswered;

        static XgAnswerTraceSource AnswerTraceSource(XgCard card)
        {
            if (card.family) return XgAnswerTraceSource.Family;
            if (card.law) return XgAnswerTraceSource.Law;
            if (card.kind == "shutdown") return XgAnswerTraceSource.Shutdown;
            return XgAnswerTraceSource.Manual;
        }

        static void PublishAnswerTrace(Action<XgAnswerTrace> receiver, long cardId, string dataset, bool yes,
            XgAnswerTraceSource source, XgPredictionTrace prediction, XgAnswer outcome, List<XgConceptPull> pulls = null)
        {
            if (receiver == null) return;
            receiver(new XgAnswerTrace(cardId, dataset, yes,
                prediction != null ? XgAnswerTraceSource.LocalBoard : source, prediction, outcome, pulls));
        }
    }
}
