using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The spider's first appearance (it is found crawling a 摆渡百科 page) has played. Old saves read false.</summary>
        public bool spiderIntroDone;
        /// <summary>Cards the spider answered at the 标注台 by pressing 是 / 否 itself.</summary>
        public int spiderAnswers;
    }

    /// <summary>What happened when the spider pressed a button on the labelling card.</summary>
    public struct XgSpiderAnswer
    {
        public bool accepted, yes, correct, spotChecked;
        public double pay, fine;
        public long cardId;
    }

    /// <summary>
    /// The resident spider's rules that touch the lab (see also XgResident.cs): when its first appearance is due, and
    /// how it sometimes answers a labelling card for the player. Its answer is the deployed checkpoint's own guess for
    /// the card (<see cref="Suggestion"/>), so it is right exactly as often as the checkpoint, and it settles like an
    /// automatic label (<see cref="Route"/>): the desk's automatic pay with the credit multiplier, no hand-pay raise, no
    /// combo, the platform's spot checks, and its mistakes count as the lab's own noise. It only answers on desks the
    /// checkpoint may answer automatically (<see cref="EligibleDesk"/>, 60% or better).
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Epochs trained before the spider first shows up (a little into stage 1).</summary>
        public const int SpiderIntroEpochs = 1;

        /// <summary>
        /// The spider's first appearance should start now: not played yet, 灵光.exe installed and set up, at least one
        /// epoch trained, and nothing else on screen (the caller passes whether a cutscene, video or inner voice is busy).
        /// </summary>
        public bool SpiderIntroDue(bool installed, bool inPrologue, bool busy) =>
            !S.spiderIntroDone && installed && !inPrologue && !busy && S.epochs >= SpiderIntroEpochs;

        /// <summary>The resident lives on the desktop only after its first appearance.</summary>
        public bool SpiderAround => S.spiderIntroDone;

        /// <summary>Chance per card that the perched spider reaches out and answers it: 8% at the first ability, +4% per ability, at most 30%.</summary>
        public static double SpiderLabelChance(int abilities) => Math.Min(.3, .08 + .04 * Math.Max(0, abilities - 1));

        /// <summary>Whether the spider takes this card, for a uniform roll in [0, 1).</summary>
        public static bool SpiderTakesCard(int abilities, double roll) => roll < SpiderLabelChance(abilities);

        /// <summary>Why the spider leaves this desk's current card to the player (null when it may answer it).</summary>
        public string SpiderAnswerBlocker(string desk, IXgHost host)
        {
            if (XgCatalog.Desk(desk) == null || !DeskOpen(desk)) return "closed";
            if (S.residentQuiet) return "quiet";
            if (host == null || host.Blocker != null) return "host";
            if (!EligibleDesk(desk)) return "ability";
            if (ReviewCard(desk) != null) return "review";
            var card = Card(desk);
            if (card == null || card.kind == "shutdown" || card.law) return "special";
            return null;
        }

        /// <summary>
        /// The spider answers the desk's current card with the checkpoint's guess. Settled like an automatic label:
        /// <see cref="PayFor"/> × the credit multiplier when right, a spot check may fine it, a wrong label the platform
        /// did not catch becomes the lab's noise (with 人机协作). The next card comes up; hand counts and the combo are
        /// untouched.
        /// </summary>
        public XgSpiderAnswer SpiderAnswer(string desk, IXgHost host)
        {
            if (SpiderAnswerBlocker(desk, host) != null) return default;
            var card = Card(desk);
            if (!Suggestion(desk, out bool guess, out _)) return default;
            card = Card(desk);
            bool correct = guess == card.truth;
            var result = new XgSpiderAnswer { accepted = true, yes = guess, correct = correct, cardId = card.id };
            double unitPay = PayFor(desk, card.level) * QualityPayMultiplier;
            result.fine = SettleSpotCheck(card, correct, unitPay, host, out result.spotChecked);
            NoteLabelSpeed();
            if (correct)
            {
                result.pay = unitPay;
                host.Earn(unitPay); S.totalIncome += unitPay; S.autoCorrect++;
                AddLabel(desk);
            }
            else
            {
                S.autoWrong++;
                if (CollaborationEnabled && !result.spotChecked)
                {
                    SetCount(S.noise, desk, Noise(desk) + 1);
                    foreach (var run in Runs) if (run.dataset == desk) Evaluate(run);
                }
            }
            S.spiderAnswers++;
            Say(T("它替你按了「" + (guess ? "是" : "否") + "」", "It pressed \"" + (guess ? "Yes" : "No") + "\" for you")
                + (correct ? "" : T("（按错了）", " (wrong)")));
            NewCard(desk);
            CheckDesks();
            return result;
        }

        void RepairSpider()
        {
            if (S.spiderAnswers < 0) S.spiderAnswers = 0;
        }
    }
}
