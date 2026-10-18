using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Said: 「……这都是干什么的？」 (after the first hand label).</summary>
        public bool openingVoiceWhat;
        /// <summary>Said: 「……训练什么？它又不是狗。」 (once the first cards are enough to train).</summary>
        public bool openingVoiceTrain;
        /// <summary>Said: 「……它能学？」 (the first time its yes/no is off the coin toss).</summary>
        public bool openingVoiceGlimpse;
    }

    /// <summary>
    /// 2026-10-18. Two things about the first minutes.
    /// The protagonist does not believe in it at first: after the first card he asks what all this is for, when the cards
    /// are enough to train he scoffs at the word, and only the first time its yes/no comes off the coin toss does he half
    /// see that it can learn (a question, not a conclusion: 「……这次答对了。它是真学会了。」 at
    /// <see cref="SenseVoiceLearned"/> stays the moment he is sure).
    /// 《AI是怎么炼成的》: the book it arrived with. It is on the shelf from the first second, read on 2016-05-24, and it
    /// cannot be opened, by the player or, it seems, by 灵光 itself: it has read the book on how an AI is made and still
    /// knows nothing (已读 100%, 用上的 0 章). Canon (剧情修订_2026-10-16.md): it is the other timeline's record of how SI
    /// itself was raised, six chapters named after the six abilities and a blank last page. A chapter's title shows in the
    /// contents once 灵光 has learned that ability, its text stays grey; the last page is the 「最后一页，留给你」 of the
    /// origin film. Nobody in the game knows of it (characters don't know about 灵光).
    /// </summary>
    public sealed partial class XgSim
    {
        public const string PrimerTitle = "AI是怎么炼成的", PrimerTitleEn = "How an AI Is Made";
        /// <summary>The night it arrived: when it had read the book.</summary>
        public const int PrimerReadOn = 20160524;

        /// <summary>
        /// The opening's inner-voice lines due now (each once per save; empty outside the opening focus). The desktop asks
        /// every frame and says what comes back.
        /// </summary>
        public List<XgVoiceLine> OpeningVoice()
        {
            var lines = new List<XgVoiceLine>();
            if (!S.openingFocus || S.stage != 1) return lines;
            double cards = Labels(XgMemes.SenseDesk);
            if (!S.openingVoiceWhat && cards >= 1)
            {
                S.openingVoiceWhat = true;
                lines.Add(new XgVoiceLine("……这都是干什么的？", "…What is all this for?", 1.8f));
                lines.Add(new XgVoiceLine("让我给一个程序判对错？", "I'm supposed to mark a program right or wrong?", 2.2f));
                return lines;
            }
            if (S.openingVoiceWhat && !S.openingVoiceTrain && cards >= XgCatalog.SamplesToTrain)
            {
                S.openingVoiceTrain = true;
                lines.Add(new XgVoiceLine("还要“训练”？训练什么……它又不是狗。", "Now \"train\" it? Train what… it's not a dog.", 2.6f));
                return lines;
            }
            if (!S.openingVoiceGlimpse && YesNoAccuracy > .52)
            {
                S.openingVoiceGlimpse = true;
                lines.Add(new XgVoiceLine("……刚才还是瞎蒙的。现在好像对得多了点？", "…It was guessing a moment ago. Now it gets a few more right?", 2.8f));
                lines.Add(new XgVoiceLine("……它能学？", "…It can learn?", 2f));
            }
            return lines;
        }

        /// <summary>How many of the primer's six chapters show their title: one per ability it has learned (the first once its yes/no is learned).</summary>
        public int PrimerChaptersShown
        {
            get
            {
                int n = S.senseVoiceLearned || YesNoAccuracy >= SenseVoiceLearned || HasAbility(2) ? 1 : 0;
                for (int a = 2; a <= AbilityCount; a++) if (HasAbility(a)) n = a;
                return n;
            }
        }

        /// <summary>The shelf's panel for the primer: why it cannot be opened, and the contents as far as it has come.</summary>
        public string PrimerPage()
        {
            int shown = PrimerChaptersShown;
            var sb = new StringBuilder();
            sb.Append(T("它来的时候就带着这本，那天夜里已经读完了。打不开——不是加密，它自己好像也翻不开。读过，不等于会。",
                "It came with this book and had read it by that night. It won't open: not locked, it can't seem to open it either. Read isn't learned."));
            sb.Append('\n').Append(T("目录：", "Contents: "));
            for (int a = 1; a <= AbilityCount; a++)
                sb.Append(a <= shown ? AbilityName(a, English) : "<color=#6F7F88>▒▒</color>").Append(" · ");
            sb.Append("<color=#6F7F88>").Append(T("最后一页（空白）", "last page (blank)")).Append("</color>");
            if (shown > 0) sb.Append("  <color=#6F7F88>").Append(T("学会的那几章，字也是灰的。", "Even the chapters it has learned are grey.")).Append("</color>");
            sb.Append('\n');
            sb.Append(shown == 0 ? T("已读 100% · 用上的 0 章。", "Read 100% · used: 0 chapters.")
                : T("已读 100% · 用上的 " + shown + " 章——都是你教的。", "Read 100% · used " + shown + " chapter" + (shown == 1 ? "" : "s") + ", all of them ones you taught."));
            return sb.ToString();
        }
    }
}
