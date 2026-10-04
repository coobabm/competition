using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public partial class XgCard
    {
        /// <summary>Set when the platform swapped this card for one of its known-answer trap items ("desk#index").</summary>
        public string trapId = "";
    }

    public partial class XgState
    {
        /// <summary>Trap items the platform has already checked; 题库比对 recognises them when they come back.</summary>
        public List<string> qcTrapsSeen = new List<string>();
        /// <summary>Trap items answered wrong (lifetime).</summary>
        public int qcTrapFails;
        /// <summary>The player has been told about trap items (opens the forum thread about them).</summary>
        public bool qcTrapRevealed;
    }

    /// <summary>
    /// 金标题: the platform mixes a small fixed pool of known-answer items (12 per desk) into the automatic flow,
    /// about one card in fifty (tuned down from the briefed 5% so a 90% model stays under one report an hour).
    /// They look like any other card, are always checked, and a wrong answer costs far more than an ordinary one.
    /// Internally they are called traps because <see cref="XgCard.gold"/> already means the timed bonus card.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int TrapPoolSize = 12, TrapMemoryLimit = 400;
        public const double TrapChance = .02, TrapFineMultiplier = 5, TrapCreditFail = 8;
        public const string TrapMemoryNode = "label.trapmemory";
        /// <summary>The forum flag that shows the thread about trap items.</summary>
        public const string TrapForumFlag = "qc.trap";

        /// <summary>Tests may pin the chance that a new automatic card is a trap item (negative = 5%).</summary>
        public double ForcedTrapChance = -1;

        public static bool IsTrap(XgCard card) => card != null && !string.IsNullOrEmpty(card.trapId);
        /// <summary>The platform already checked this trap item once.</summary>
        public bool TrapRemembered(XgCard card) => IsTrap(card) && S.qcTrapsSeen.Contains(card.trapId);
        /// <summary>题库比对 is owned and knows this item: it goes to a human, and the card shows 「眼熟」.</summary>
        public bool TrapRecognized(XgCard card) => Has(TrapMemoryNode) && TrapRemembered(card);
        public int TrapsRemembered => S.qcTrapsSeen.Count;

        /// <summary>Story and forum gates the desktop can read from the lab.</summary>
        public bool ForumFlag(string flag) => flag == TrapForumFlag && S.qcTrapRevealed;

        void PrepareTraps()
        {
            if (S.qcTrapsSeen == null) S.qcTrapsSeen = new List<string>();
            S.qcTrapsSeen.RemoveAll(id => string.IsNullOrEmpty(id));
            Trim(S.qcTrapsSeen, TrapMemoryLimit);
            S.qcTrapFails = Math.Max(0, S.qcTrapFails);
        }

        /// <summary>A new card for the automatic buffer; about one in twenty is swapped for a trap item.</summary>
        XgCard CreatePrefetchCard(string desk)
        {
            // The ordinary card is always drawn first so the lab's own random stream does not depend on traps.
            var card = CreateLabelCard(desk);
            if (!QualityActive) return card;
            double chance = ForcedTrapChance >= 0 ? ForcedTrapChance : TrapChance;
            if (QualityRoll() >= chance) return card;
            int index = (int)(QualityRoll() * TrapPoolSize) % TrapPoolSize;
            return CreateTrapCard(desk, index) ?? card;
        }

        /// <summary>
        /// Trap item <paramref name="index"/> (0–11) of a desk: the same seed every time, so it is the same item and
        /// the deployed checkpoint gets it right or wrong the same way each time it comes back.
        /// </summary>
        public XgCard CreateTrapCard(string desk, int index)
        {
            if (XgCatalog.Desk(desk) == null || index < 0 || index >= TrapPoolSize) return null;
            long saved = S.rng;
            S.rng = TrapSeed(desk, index);
            XgCard card;
            try { card = CreateLabelCard(desk); }
            finally { S.rng = saved; }
            if (card.kind == "shutdown") return null;
            card.gold = false; card.timeLimit = 0; card.age = 0;
            card.trapId = desk + "#" + index;
            return card;
        }

        static long TrapSeed(string desk, int index)
        {
            ulong h = 1469598103934665603UL;
            foreach (char c in desk) { h ^= c; h *= 1099511628211UL; }
            h ^= (ulong)(index + 1) * 0x9E3779B97F4A7C15UL;
            h ^= h >> 29;
            return h == 0 ? QcSeed : (long)h;
        }

        void RememberTrap(XgCard card)
        {
            if (!IsTrap(card) || S.qcTrapsSeen.Contains(card.trapId)) return;
            S.qcTrapsSeen.Add(card.trapId);
            Trim(S.qcTrapsSeen, TrapMemoryLimit);
        }

        void NoteTrapFailure()
        {
            S.qcTrapFails++;
            if (S.qcTrapRevealed) return;
            S.qcTrapRevealed = true;
            Say(T("摆渡众包：抽检里有一条是平台预置的金标题，答案早就知道。", "Bodu Crowdsourcing: one of the checked labels was a known-answer trap item; the platform knew the answer all along."));
        }
    }
}
