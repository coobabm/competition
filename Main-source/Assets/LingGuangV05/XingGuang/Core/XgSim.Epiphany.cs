using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>The auto-labelling realisation. Saved with the lab and repaired on load.</summary>
    public sealed partial class XgState
    {
        /// <summary>0 for saves from before the realisation existed; repair settles legacy saves once.</summary>
        public int epiphanyVersion;
        /// <summary>The protagonist noticed the model answers like they do (the Epiphany event has fired).</summary>
        public bool epiphany;
        /// <summary>自动答题 is shown and can be bought. Lags <see cref="epiphany"/> while the desktop plays the cutscene.</summary>
        public bool autoLabelRevealed;
        /// <summary>The ghost has been shown at least once on a hand-labelled card.</summary>
        public bool ghostSeen;
        /// <summary>Hand answers in a row whose ghost guess matched the player's own answer.</summary>
        public int ghostStreak;
        /// <summary>Hand answers (any desk) since the ghost first appeared, for the fallback trigger.</summary>
        public int ghostLabels;
    }

    /// <summary>
    /// Nobody tells the protagonist that the model could label for them; they notice. Once a desk has a checkpoint at
    /// <see cref="XgCatalog.AutoMinAccuracy"/> or better, its hand-labelled cards carry a faint "ghost" of the
    /// model's guess (<see cref="GhostGuess"/>). When the ghost has agreed with the player
    /// <see cref="EpiphanyStreak"/> times in a row, or <see cref="EpiphanyFallbackLabels"/> hand labels after it
    /// first appeared, <see cref="Epiphany"/> fires once. Until then 自动答题 (label.auto) is hidden in the tree as
    /// 「？？？」 and cannot be bought. The desktop may defer the reveal until its cutscene ends
    /// (<see cref="DeferAutoLabelReveal"/>, <see cref="RevealAutoLabel"/>); headless runs reveal at once.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int EpiphanyStreak = 8, EpiphanyFallbackLabels = 150;
        /// <summary>The flash card the realisation leaves (shown through the self-insight card overlay).</summary>
        public const string EpiphanyCardId = "autolabel";

        /// <summary>Raised once, right after the hand answer that completed the realisation.</summary>
        public event Action Epiphany;

        /// <summary>Set by the desktop when it will reveal 自动答题 itself at the end of its cutscene. Not saved.</summary>
        public bool DeferAutoLabelReveal;

        /// <summary>Owning any level of 自动答题 always counts as having had the idea (old saves, tests, cheats).</summary>
        public bool AutoLabelRevealed => S.autoLabelRevealed || S.autoLevel > 0;
        public bool AutoLabelHidden => !AutoLabelRevealed;
        public bool EpiphanyDone => S.epiphany || AutoLabelRevealed;
        /// <summary>The realisation happened but the desktop has not revealed 自动答题 yet.</summary>
        public bool EpiphanyPending => S.epiphany && !AutoLabelRevealed;

        void RepairEpiphany()
        {
            if (S.ghostStreak < 0) S.ghostStreak = 0;
            if (S.ghostLabels < 0) S.ghostLabels = 0;
            if (S.epiphanyVersion == 0)
            {
                // Saves from before this beat: anyone who already bought 自动答题 (or anything behind it) had the idea.
                if (S.autoLevel > 0 || Has("label.coop") || Has("label.brain") || Has("label.hard") || Has("label.audit") || Has("label.parallel"))
                    S.epiphany = S.autoLabelRevealed = true;
                S.epiphanyVersion = 1;
            }
            if (S.autoLevel > 0) S.epiphany = S.autoLabelRevealed = true;
            if (S.autoLabelRevealed) S.epiphany = true;
        }

        /// <summary>
        /// The deployed checkpoint's guess for the card the player is hand-labelling on this desk, while the idea has
        /// not come yet and the desk has a checkpoint of at least 60%. Deterministic for the card (the same roll the
        /// label page's suggestion uses), so the drawn ghost and the counted ghost always agree. No side effects
        /// beyond what showing the suggestion already does.
        /// </summary>
        public bool GhostGuess(string desk, out bool guess, out double confidence)
        {
            guess = false; confidence = 0;
            if (EpiphanyDone || desk == null || XgCatalog.Desk(desk) == null || !DeskOpen(desk)) return false;
            if (BestAcc(desk) + 1e-9 < XgCatalog.AutoMinAccuracy || ReviewCard(desk) != null) return false;
            return GhostFor(Card(desk), out guess, out confidence);
        }

        bool GhostFor(XgCard card, out bool guess, out double confidence)
        {
            guess = false; confidence = 0;
            if (card == null || card.kind == "shutdown" || XgCatalog.Dataset(card.dataset) == null) return false;
            if (S.stage == 1 && card.kind == "combo") return TryGetComboSuggestion(card, out guess, out confidence);
            double acc = CardAccuracy(Run(XgCatalog.Dataset(card.dataset).track), card, BestAcc(card.dataset));
            if (!(acc > 0)) return false;
            guess = card.roll < acc ? card.truth : !card.truth;
            confidence = acc;
            return true;
        }

        /// <summary>Read before a hand answer settles: did this card carry a ghost, and what did it say?</summary>
        bool GhostBeforeAnswer(string desk, XgCard card, out bool guess)
        {
            guess = false;
            if (EpiphanyDone || card == null || BestAcc(desk) + 1e-9 < XgCatalog.AutoMinAccuracy) return false;
            return GhostFor(card, out guess, out _);
        }

        /// <summary>Counts one settled hand answer toward the realisation. Timeouts never count as agreement.</summary>
        void ObserveGhost(bool hadGhost, bool ghost, bool answer, bool timeout)
        {
            if (EpiphanyDone) return;
            if (hadGhost)
            {
                S.ghostSeen = true;
                S.ghostStreak = !timeout && ghost == answer ? S.ghostStreak + 1 : 0;
            }
            if (S.ghostSeen) S.ghostLabels++;
            if (S.ghostStreak >= EpiphanyStreak || S.ghostSeen && S.ghostLabels >= EpiphanyFallbackLabels) RaiseEpiphany();
        }

        void RaiseEpiphany()
        {
            if (S.epiphany) return;
            S.epiphany = true;
            if (!DeferAutoLabelReveal) S.autoLabelRevealed = true;
            Epiphany?.Invoke();
        }

        /// <summary>Shows 自动答题 (the end of the cutscene). True if it was hidden until now.</summary>
        public bool RevealAutoLabel()
        {
            if (S.autoLabelRevealed) return false;
            S.epiphany = S.autoLabelRevealed = true;
            return true;
        }

        /// <summary>The flash card's text (title, a short line in gold, and why).</summary>
        public static void EpiphanyCardText(out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn)
        {
            name = "让它替我标"; nameEn = "Let it label for me";
            golden = "它答得跟我一样，还比我快。"; goldenEn = "It answers the way I do, only faster.";
            why = "挂机脚本只会全选「是」，一抽检就露馅。它不是脚本：它是真的认得。让它先答，我来看着。";
            whyEn = "Click scripts only ever pick Yes, and the spot checks catch them at once. This is no script: it can actually read. Let it answer first; I keep an eye on it.";
        }
    }
}
