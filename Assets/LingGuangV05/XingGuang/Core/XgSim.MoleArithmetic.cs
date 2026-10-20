using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>Transient per-hole ownership. Consume before reporting or invoking any presentation callback.</summary>
    public sealed class XgMoleTurn
    {
        public long Token { get; private set; }
        public bool Open { get; private set; }
        public bool AutoDecided { get; private set; }
        public void Begin(long token) { Token = token; Open = token != 0; AutoDecided = false; }
        public bool TryAutoDecision(long token, bool active)
        {
            if (!active || !Open || token != Token || AutoDecided) return false;
            AutoDecided = true; return true;
        }
        public bool TryWhack(long token, bool active)
        {
            if (!active || !Open || token != Token) return false;
            Open = false; return true;
        }
        public void Invalidate() { Open = false; AutoDecided = true; }
    }

    /// <summary>View notification, never a payment API. Reported means the existing Report path was called.</summary>
    public sealed class XgMoleFeedback
    {
        public readonly string GameId;
        public readonly long QuestionToken;
        public readonly int Slot;
        public readonly bool Automatic, Whack, Correct, Reported;
        public readonly XgMoleQuestion Question;
        public readonly XgPredictionTrace Prediction;
        public XgMoleFeedback(string gameId, long token, int slot, bool automatic, bool whack,
            bool correct, bool reported, XgMoleQuestion question, XgPredictionTrace prediction)
        { GameId = gameId; QuestionToken = token; Slot = slot; Automatic = automatic; Whack = whack;
            Correct = correct; Reported = reported; Question = question; Prediction = prediction; }
    }

    /// <summary>Label-blind adapter. Only displayed text/category enter inference; no truth, answer, or generator seed.</summary>
    public static class XgMoleInference
    {
        // Compatibility entry point: teaching/training and automatic play share the exact visible encoding.
        public static XgBoardCard Input(XgCard displayed) => XgArithmeticInput.Input(displayed);
    }

    public sealed partial class XgState
    {
        public int moleArithmeticTopic; // Old saves default to single-step basic arithmetic.
    }

    public sealed partial class XgSim
    {
        public const string MoleMixedNode = "arith.mixed", MolePowersNode = "arith.powers", MoleCalculusNode = "arith.calculus", MoleAutoNode = "arith.auto";
        public static bool IsMoleArithmeticNode(string id) => id == MoleMixedNode || id == MolePowersNode || id == MoleCalculusNode || id == MoleAutoNode;
        long moleQuestionSerial;
        public long NextMoleQuestionToken() => ++moleQuestionSerial;
        public int MoleArithmeticTier => Has(MolePowersNode) ? (Has(MoleCalculusNode) ? 3 : 2) : 1;
        public void RepairMoleTopics()
        {
            if (Has(MoleCalculusNode) && !Has(MolePowersNode)) S.unlocked.Add(MolePowersNode);
            if (Has(MolePowersNode) && !Has(MoleMixedNode)) S.unlocked.Add(MoleMixedNode);
            if (!MoleTopicAvailable((XgMoleTopic)S.moleArithmeticTopic)) S.moleArithmeticTopic = 0;
        }
        public bool MoleTopicAvailable(XgMoleTopic topic) => topic == XgMoleTopic.Basic ||
            (topic == XgMoleTopic.Mixed && (Has(MoleMixedNode) || Has(MolePowersNode) || Has(MoleCalculusNode))) ||
            (topic == XgMoleTopic.Powers && (Has(MolePowersNode) || Has(MoleCalculusNode))) ||
            (topic == XgMoleTopic.Calculus && Has(MoleCalculusNode));
        public XgMoleTopic MoleTopic => MoleTopicAvailable((XgMoleTopic)S.moleArithmeticTopic)
            ? (XgMoleTopic)S.moleArithmeticTopic : XgMoleTopic.Basic;
        public bool SelectMoleTopic(XgMoleTopic topic)
        {
            if (!MoleTopicAvailable(topic)) return false;
            S.moleArithmeticTopic = (int)topic; return true;
        }
        public XgMoleQuestion GenerateMoleQuestion(int seed) => XgMoleArithmetic.Generate(seed, MoleTopic == XgMoleTopic.Calculus ? 3 : MoleTopic == XgMoleTopic.Basic ? 1 : 2, MoleTopic);
        public bool MoleAutoAvailable(string gameId) => UseBoard && S.stage >= 2 && Has(MoleAutoNode) &&
            (gameId == "arith_mole" || gameId == "dadishu");
        public event Action<XgMoleFeedback> ArithmeticMoleFeedback;
        public void NotifyArithmeticMole(XgMoleFeedback feedback) => ArithmeticMoleFeedback?.Invoke(feedback);
        public XgPredictionTrace PredictMole(XgCard displayed)
        {
            if (!UseBoard) return null;
            var input = XgMoleInference.Input(displayed);
            if (input == null) return null;
            Board.Predict(input, Knobs(Run(XgCatalog.Dataset("arith").track)), out _, out var trace);
            return trace;
        }
    }
}
