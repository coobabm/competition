namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Enabled only by BeginOpening: existing saves retain every page they already unlocked.
        public bool teachingFlow;
        public int teachingThoughtVersion;
        public bool answerPayThoughtRecorded, answerFineThoughtRecorded;
        // Separate settled/queued receipts from consumed narration; both belong to this save, never a static session.
        public bool answerPayThoughtConsumed, answerFineThoughtConsumed;
        public System.Collections.Generic.List<string> pendingAnswerMoneyThoughts = new System.Collections.Generic.List<string>();
        public bool teachIntroSaid, teachThirdSaid, teachTreeSaid, teachTrainSaid;
    }

    public sealed partial class XgSim
    {
        public int TeachingAnswers => S.handCorrect + S.handWrong;
        public bool TeachingTreeOpen => !S.teachingFlow || TeachingAnswers >= 12 || Has("train.curve");
        public bool TeachingTrainOpen => !S.teachingFlow || Has("train.curve");
        public bool TeachingAiReady => Has("label.ai");

        void RecordAnswerMoneyThought(XgAnswer result)
        {
            if (S.pendingAnswerMoneyThoughts == null) S.pendingAnswerMoneyThoughts = new System.Collections.Generic.List<string>();
            if (result.correct && result.pay > 0 && !S.answerPayThoughtRecorded)
            {
                S.answerPayThoughtRecorded = true;
                S.pendingAnswerMoneyThoughts.Add("pay");
            }
            if (!result.correct && result.fine > 0 && !S.answerFineThoughtRecorded)
            {
                S.answerFineThoughtRecorded = true;
                S.pendingAnswerMoneyThoughts.Add("fine");
            }
        }

        void RepairTeachingThoughts()
        {
            if (S.pendingAnswerMoneyThoughts == null) S.pendingAnswerMoneyThoughts = new System.Collections.Generic.List<string>();
            var unique = new System.Collections.Generic.HashSet<string>();
            S.pendingAnswerMoneyThoughts.RemoveAll(k => (k != "pay" && k != "fine") || !unique.Add(k)
                || k == "pay" && S.answerPayThoughtConsumed || k == "fine" && S.answerFineThoughtConsumed);
            // In older saves a recorded event with no pending line was already consumed. Do not backfill it.
            S.answerPayThoughtConsumed |= S.answerPayThoughtRecorded && !S.pendingAnswerMoneyThoughts.Contains("pay");
            S.answerFineThoughtConsumed |= S.answerFineThoughtRecorded && !S.pendingAnswerMoneyThoughts.Contains("fine");
            S.answerPayThoughtRecorded |= S.answerPayThoughtConsumed || S.pendingAnswerMoneyThoughts.Contains("pay");
            S.answerFineThoughtRecorded |= S.answerFineThoughtConsumed || S.pendingAnswerMoneyThoughts.Contains("fine");
        }

        /// <summary>Durably claim a thought before presenting it. A failed save leaves it pending and unheard.</summary>
        public XgVoiceLine? TakeTeachingThought(bool visible, System.Func<bool> persist)
        {
            if (persist == null) return null;
            int version = S.teachingThoughtVersion;
            bool pay = S.answerPayThoughtConsumed, fine = S.answerFineThoughtConsumed;
            bool payRecorded = S.answerPayThoughtRecorded, fineRecorded = S.answerFineThoughtRecorded;
            bool intro = S.teachIntroSaid, third = S.teachThirdSaid, tree = S.teachTreeSaid, train = S.teachTrainSaid;
            var pending = S.pendingAnswerMoneyThoughts == null ? null : new System.Collections.Generic.List<string>(S.pendingAnswerMoneyThoughts);
            var thought = TakeTeachingThought(visible);
            if (!thought.HasValue) return null;
            bool saved = false;
            try { saved = persist(); }
            finally
            {
                if (!saved)
                {
                    S.teachingThoughtVersion = version;
                    S.answerPayThoughtConsumed = pay; S.answerFineThoughtConsumed = fine;
                    S.answerPayThoughtRecorded = payRecorded; S.answerFineThoughtRecorded = fineRecorded;
                    S.teachIntroSaid = intro; S.teachThirdSaid = third; S.teachTreeSaid = tree; S.teachTrainSaid = train;
                    S.pendingAnswerMoneyThoughts = pending;
                }
            }
            return saved ? thought : null;
        }

        /// <summary>Consume one thought only when the player can see the exe. Progress, not elapsed time, selects it.</summary>
        public XgVoiceLine? TakeTeachingThought(bool visible)
        {
            if (!visible) return null;
            RepairTeachingThoughts();
            // Preserve the first-open introduction, then drain real settlement reactions before the third-answer line.
            if ((S.teachIntroSaid || !S.teachingFlow || TeachingAnswers >= 3 || S.stage != 1)
                && S.pendingAnswerMoneyThoughts != null && S.pendingAnswerMoneyThoughts.Count > 0)
            {
                string kind = S.pendingAnswerMoneyThoughts[0];
                S.pendingAnswerMoneyThoughts.RemoveAt(0);
                if (kind == "pay") S.answerPayThoughtConsumed = true;
                else S.answerFineThoughtConsumed = true;
                return kind == "pay"
                    ? new XgVoiceLine("我去，答题还能赚钱？", "Whoa, answering questions earns money?", 2.8f)
                    : new XgVoiceLine("我去，答错还会扣钱？", "Whoa, wrong answers cost money too?", 2.8f);
            }
            if (!S.teachingFlow || S.stage != 1) return null;
            if (S.teachingThoughtVersion < 1)
            {
                // Upgrade existing saves without backfilling lines about actions already completed.
                S.teachingThoughtVersion = 1;
                S.teachIntroSaid = true;
                S.teachThirdSaid |= TeachingAnswers >= 3;
                S.teachTreeSaid |= TeachingTreeOpen;
                S.teachTrainSaid |= S.epochs > 0;
                return null;
            }
            if (!S.teachIntroSaid && TeachingAnswers < 3)
            {
                S.teachIntroSaid = true;
                return new XgVoiceLine("这又是什么鬼？我难道要做题吗？", "What on earth is this? Am I supposed to answer questions?", 2.8f);
            }
            if (!S.teachThirdSaid && TeachingAnswers >= 3)
            {
                // An older save may already be past the introduction. Never replay that stale opening line.
                S.teachIntroSaid = S.teachThirdSaid = true;
                return new XgVoiceLine("这都什么破题？有什么意义啊？……先做完再说吧。", "What kind of lousy questions are these? What's the point? …I'll finish them first.", 2.8f);
            }
            if (!S.teachTrainSaid && S.epochs > 0)
            {
                S.teachTreeSaid = S.teachTrainSaid = true;
                return new XgVoiceLine("……它在答我刚才答的题？所以那几道破题，是在教它。", "…It's answering the questions I just answered? So those questions were teaching it.", 2.8f);
            }
            if (!S.teachTreeSaid && TeachingTreeOpen)
            {
                S.teachTreeSaid = true;
                return new XgVoiceLine("做完了……然后呢？", "Done… now what?", 2.8f);
            }
            return null;
        }

        public string TeachingNodeBlocker(XgNode node)
        {
            if (node.id.StartsWith("pack."))
            {
                var blocked = TeachingPackBlocker(XgTeachingPacks.Find(node.id.Substring(5)), true);
                if (blocked != null) return blocked;
            }
            if (node.id == "train.curve" && !TeachingTreeOpen)
                return T("先做完 12 道题", "Answer twelve questions first");
            if (node.id == "label.ai" && (TeachingAnswers < 24 || S.epochs < 1))
                return T("做完 24 题，并训练一轮", "Answer 24 questions and finish one training round");
            if (S.teachingFlow && !Has("train.curve") && node.id != "train.curve")
                return T("先买训练曲线", "Buy Training curve first");
            return null;
        }
    }
}
