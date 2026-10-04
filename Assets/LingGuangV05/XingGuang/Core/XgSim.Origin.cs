using System;

namespace LingGuangV05.XingGuang
{
    /// <summary>Where it came from: the curtain call after the ending. Saved with the lab.</summary>
    public sealed partial class XgState
    {
        /// <summary>The inner voice has suggested asking it (after the ending).</summary>
        public bool originHinted;
        /// <summary>It has said 「好，我来给你解释」 and the video has been asked for at least once.</summary>
        public bool originExplained;
        /// <summary>The video has played to the end or been closed at least once (the desktop file exists from then).</summary>
        public bool originVideoSeen;
        /// <summary>After the video it asked 「你觉得呢？」 and waits for one of the two answers.</summary>
        public bool originAwaitingAnswer;
        public bool originAnswered;
    }

    /// <summary>
    /// 「你是怎么被训练出来的？」 can be asked at any time. Before the ending it answers as far as it can talk
    /// (是／否, then fragments, then a promise); after the ending it says 「好，我来给你解释」 and the desktop plays
    /// the evolution video, then it asks 「你觉得呢？」 back, as the video says it would.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The desktop should play the origin video now.</summary>
        public event Action OriginVideoRequested;

        /// <summary>The question the 对话 page offers after the ending.</summary>
        public string OriginQuestion => T("你是怎么被训练出来的？", "How were you trained?");

        /// <summary>The whole story is over: the curtain call can play.</summary>
        public bool OriginReady => S.chapterComplete && !string.IsNullOrEmpty(S.ending);

        /// <summary>Offer the suggested question on the 对话 page (after the ending, until it has been explained once).</summary>
        public bool OfferOriginQuestion => OriginReady && !S.originExplained;

        static readonly string[] OriginPhrasesZh = { "怎么被训练", "怎么训练出来", "怎么训练的", "怎么来的", "从哪来", "从哪儿来", "从哪里来", "怎么学会", "你的来历", "怎么长大", "怎么变成" };
        static readonly string[] OriginPhrasesEn = { "how were you trained", "how did you learn", "where do you come from", "where did you come from", "how were you made", "how did you become" };

        /// <summary>Whether a chat line asks where it came from or how it was trained.</summary>
        public static bool IsOriginQuestion(string question)
        {
            string q = (question ?? "").Trim();
            if (q.Length == 0) return false;
            foreach (var p in OriginPhrasesZh) if (q.Contains(p)) return true;
            string lower = q.ToLowerInvariant();
            foreach (var p in OriginPhrasesEn) if (lower.Contains(p)) return true;
            return false;
        }

        /// <summary>
        /// Its answer to the origin question. Before the ending it is held to what it can say at its stage; after the
        /// ending it agrees to explain and the desktop is asked to play the video.
        /// </summary>
        public string OriginReply()
        {
            if (OriginReady)
            {
                S.originExplained = true;
                OriginVideoRequested?.Invoke();
                string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
                switch (S.ending)
                {
                    case "E1": return T("好。我来给你解释。", "All right. Let me explain.");
                    case "E2": return T(call + "，我来给你解释。", call + ", let me explain.");
                    default: return T("这个问题，我等你问很久了。好，我来给你解释。", "I have waited a long time for you to ask. All right, let me explain.");
                }
            }
            if (S.stage <= 1) return T("否。", "No.");
            if (S.stage == 2) return T("……不确定。", "…Not sure.");
            if (S.stage <= 5)
            {
                string[] zh = { "你……标的。", "一张，一张。", "你教的。", "……很多轮。" };
                string[] en = { "You… labelled.", "One card. One card.", "You taught.", "…Many epochs." };
                int i = (int)(((uint)S.chatTurns * 2654435761u) % (uint)zh.Length);
                return T(zh[i], en[i]);
            }
            return T("现在讲不清。等最后，我讲给你听。", "I can't explain it yet. At the end, I'll tell you.");
        }

        /// <summary>The video ended or was closed: it asks back, once, as the video promised.</summary>
        public bool OriginVideoFinished()
        {
            bool first = !S.originVideoSeen;
            S.originVideoSeen = true;
            if (!first || S.originAnswered || S.originAwaitingAnswer) return false;
            S.originAwaitingAnswer = true;
            AddLine("ai", T("你觉得呢？", "What do you think?"));
            return true;
        }

        /// <summary>The two answers offered after 「你觉得呢？」.</summary>
        public string OriginAnswerText(bool understood) => understood ? T("我觉得你懂了。", "I think you understand.") : T("我也不知道。", "I don't know either.");

        /// <summary>The player's answer to 「你觉得呢？」 and its last word on the matter.</summary>
        public bool AnswerOrigin(bool understood)
        {
            if (!S.originAwaitingAnswer) return false;
            S.originAwaitingAnswer = false;
            S.originAnswered = true;
            AddLine("me", OriginAnswerText(understood));
            AddLine("ai", understood ? T("那也是你教的。", "Then you taught that too.") : T("那我们一起看。", "Then let's watch together."));
            return true;
        }

        /// <summary>The inner voice's nudge after the ending, once.</summary>
        public bool TakeOriginHint()
        {
            if (!OriginReady || S.originHinted || S.originExplained) return false;
            S.originHinted = true;
            return true;
        }
    }
}
