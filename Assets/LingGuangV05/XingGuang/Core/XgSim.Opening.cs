using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>
        /// The first-minutes focus is on: only the starting desks and the training page are open until the yes/no
        /// reaches <see cref="XgSim.SenseVoiceLearned"/>. False in every save from before it existed, so an old save is never locked.
        /// </summary>
        public bool openingFocus;
        /// <summary>The desk has just taught it yes from no and the player has not talked to it since (the note's third line).</summary>
        public bool openingChatPending;
        /// <summary><see cref="chatTurns"/> when the focus ended; a later turn means the player has chatted.</summary>
        public int openingChatBase;
    }

    /// <summary>
    /// The first five minutes: the first goal is to label 常识判断 on 摆渡众包 and train until its yes/no is learned
    /// (<see cref="SenseVoiceLearned"/>). The three starting desks (常识判断, 垃圾短信, 逻辑题) and their games are open from the
    /// first second so the opening is a run of quick answers and combos; the pages that are not needed yet stay shut with a
    /// one-line reason, so the moment it learns is not lost among a dozen new tabs. It reuses the existing gates: the desk
    /// rules (<see cref="DeskOpen"/>), the page rules (<see cref="FeatureVisible"/>) and the guide's <c>TabOpen</c>. A new
    /// game starts it (<see cref="BeginOpening"/>); a save without the flag, or one that already has an ability, never sees it.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The yes/no just reached <see cref="SenseVoiceLearned"/> and the focus ended (once per save).</summary>
        public event Action YesNoLearned;

        /// <summary>The focus is on: everything not needed to teach it yes from no is locked.</summary>
        public bool OpeningFocus => S.openingFocus;

        /// <summary>The one line shown wherever a locked desk or page is poked.</summary>
        public string OpeningReason => T("先教会它分辨对错", "Teach it right from wrong first");

        /// <summary>The note's chat step: it learned yes/no and the player has not spoken to it since.</summary>
        public bool OpeningChatDue => S.openingChatPending && S.stage == 1 && S.chatTurns <= S.openingChatBase;

        /// <summary>A desk the focus keeps shut: none of the starting desks (常识判断, 垃圾短信, 逻辑题, 手写数字), which are open with their games.</summary>
        public bool OpeningLocksDesk(string desk) => S.openingFocus && !OpeningDesk(desk);

        /// <summary>A desk open from the first second of a new game (<see cref="XgDeskUnlock.Start"/>).</summary>
        public static bool OpeningDesk(string desk) => desk == XgMemes.SenseDesk || XgCatalog.Desk(desk)?.unlock == XgDeskUnlock.Start;

        /// <summary>The combo window is this many times longer during the opening, so a new player reading a card keeps the streak.</summary>
        public const double OpeningComboWindow = 2;

        /// <summary>
        /// A 灵光 page the focus keeps shut. Open: the labelling page, the training pages, and 概览 (the fallback when YY
        /// cannot open). Everything else (科技, 大脑, 数据, 接线, 订单 …) waits.
        /// </summary>
        public bool OpeningLocksLabTab(string tab)
        {
            if (!S.openingFocus) return false;
            switch (tab ?? "")
            {
                case "": case "teach": case "label": case "train": case "vision": case "sequence": case "chat": case "home": return false;
                default: return true;
            }
        }

        /// <summary>A 摆渡众包 page the focus keeps shut: only the labelling desk is open.</summary>
        public bool OpeningLocksCrowdTab(string tab) => S.openingFocus && !string.IsNullOrEmpty(tab) && tab != "label";

        /// <summary>
        /// Starts the focus on a brand-new game: the starting desks are open and 常识判断 is the one the training page trains. A save that
        /// already has an ability, a trained run, a checkpoint or labels on another desk is left alone.
        /// </summary>
        public void BeginOpening()
        {
            if (!S.gradualSystems || S.openingFocus || OpeningLegacyProgress()) return;
            S.openingFocus = true;
            S.desksOpen.RemoveAll(id => !OpeningDesk(id));
            foreach (var d in XgCatalog.Desks) if (OpeningDesk(d.id) && !S.desksOpen.Contains(d.id)) S.desksOpen.Add(d.id);
            S.desk = XgMemes.SenseDesk;
            S.selected = 1;
            SetDataset(XgTrack.Sequence, XgMemes.SenseDesk);
            Card(XgMemes.SenseDesk).age = 0;
        }

        /// <summary>An old save's progress: any ability past the first, any trained run, any checkpoint, any label on another desk.</summary>
        bool OpeningLegacyProgress()
        {
            if (S.stage >= 2 || AbilitiesCount > 1 || S.epochs > 0 || S.assessments > 0 || S.best.Count > 0) return true;
            if (S.vision.epoch > 0 || S.sequence.epoch > 0) return true;
            foreach (var l in S.labels) if (l.count > 0 && l.dataset != XgMemes.SenseDesk) return true;
            return false;
        }

        /// <summary>Ends the focus once the yes/no is learned, or at once when the save somehow has an ability already (a stage set from outside).</summary>
        bool EndOpeningIfDone(out bool learned)
        {
            learned = false;
            if (!S.openingFocus) return false;
            bool skipped = S.stage >= 2 || AbilitiesCount > 1;
            learned = !skipped && YesNoAccuracy >= SenseVoiceLearned;
            if (!skipped && !learned) return false;
            S.openingFocus = false;
            if (learned) { S.openingChatPending = true; S.openingChatBase = S.chatTurns; }
            return true;
        }

        /// <summary>The yes/no learned: the notice (a toast and a feed line) with one before/after, then the event.</summary>
        void AnnounceYesNoLearned()
        {
            var ex = BeforeAfter(1);
            Say(T("它学会分辨对错了：「" + ex.zh[0] + "」以前答「" + Bare(ex.zh[1]) + "」，现在答「" + Bare(ex.zh[2]) + "」",
                "It can tell right from wrong now: '" + ex.en[0] + "' was " + Bare(ex.en[1]) + ", now " + Bare(ex.en[2])));
            // The old question starts here: it gets asked again at every ability (XgSim.OldQuestion.cs).
            Say(T("老问题「" + OldQuestionZh + "」：第一天答「" + Bare(OldAnswer(0, false)) + "」，现在答「" + Bare(OldAnswer(1, false)) + "」。以后每学会一样，再问它一遍。",
                "The old question '" + OldQuestionEn + "': day one it said " + Bare(OldAnswer(0, true)) + ", now " + Bare(OldAnswer(1, true)) + ". Ask it again each time it learns something."));
            OfferRelicPick();
            YesNoLearned?.Invoke();
        }

        static string Bare(string answer) => (answer ?? "").Trim().TrimEnd('。', '.');
    }
}
