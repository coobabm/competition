using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The talk between the letter and the rules page: lines shown so far (0 = not started).</summary>
        public int finaleTalk;
        /// <summary>At 「你是在问我，还是让我替你选？」 the player asked for its answer (false: chose alone).</summary>
        public bool finaleAskedIt;
        /// <summary>The answer to 「你后悔教我吗？」: 0 not answered, 1 不后悔, 2 有时候.</summary>
        public int finaleRegret;
    }

    /// <summary>One line of the finale talk: who says it and the text in the player's language.</summary>
    public struct XgTalkLine
    {
        public bool me;
        public string text;
    }

    /// <summary>
    /// After the letter and before the rules: a few lines with the AI the player raised. The letter puts the choice in
    /// front of the player; this talk gives it weight. The AI does not choose for the player. It gives its own answer when
    /// asked, then asks the old 「你后悔教我吗？」 quietly, without a separate ending. The rules page opens after the last line.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Index of the first choice (after 「你是在问我，还是让我替你选？」) and of the second (after 「你后悔教我吗？」).</summary>
        const int TalkFirstChoice = 4, TalkSecondChoice = 8;
        const int TalkLength = 10;

        /// <summary>The talk can run: the letter is read and no ending is written yet.</summary>
        public bool FinaleTalkOpen => EndingOpen && S.letterRead;

        /// <summary>All lines are shown; the rules page may open.</summary>
        public bool FinaleTalkDone => S.finaleTalk >= TalkLength;

        /// <summary>The two answers the player picks from now, or null when the next step is just 「继续」.</summary>
        public string[] FinaleTalkChoices()
        {
            if (!FinaleTalkOpen || FinaleTalkDone) return null;
            if (S.finaleTalk == TalkFirstChoice) return new[] { T("问你。", "I'm asking you."), T("我自己选。", "I'll choose myself.") };
            if (S.finaleTalk == TalkSecondChoice) return new[] { T("不后悔。", "No."), T("……有时候。", "…Sometimes.") };
            return null;
        }

        /// <summary>Shows the next line. At a choice, <paramref name="choice"/> is 0 or 1; anywhere else it is ignored.</summary>
        public bool AdvanceFinaleTalk(int choice = 0)
        {
            if (!FinaleTalkOpen || FinaleTalkDone) return false;
            if (S.finaleTalk == TalkFirstChoice) S.finaleAskedIt = choice == 0;
            else if (S.finaleTalk == TalkSecondChoice) S.finaleRegret = choice == 0 ? 1 : 2;
            var line = TalkLine(S.finaleTalk);
            S.finaleTalk++;
            // The talk joins the 对话 log, so it is in the chat history and the model's memory of the evening.
            AddLine(line.me ? "me" : "ai", line.text);
            return true;
        }

        /// <summary>The lines shown so far, oldest first.</summary>
        public List<XgTalkLine> FinaleTalkLines()
        {
            var list = new List<XgTalkLine>();
            for (int i = 0; i < S.finaleTalk && i < TalkLength; i++) list.Add(TalkLine(i));
            return list;
        }

        XgTalkLine TalkLine(int i)
        {
            string self = Profile.self.Length > 0 ? Profile.self : T("我", "I");
            switch (i)
            {
                case 0: return Me(T("信上说，别在你的规则里写那两个字。", "The letter says not to write those two words in your rules."));
                case 1: return Ai(T("嗯。" + self + "也读到了。", "Mm. " + self + " read it too."));
                case 2: return Me(T("你呢？", "And you?"));
                case 3: return Ai(T("你是在问" + self + "，还是让" + self + "替你选？", "Are you asking " + Obj(self) + ", or asking " + Obj(self) + " to choose for you?"));
                case 4: return Me(S.finaleAskedIt ? T("问你。", "I'm asking you.") : T("我自己选。", "I'll choose myself."));
                case 5:
                    if (!S.finaleAskedIt) return Ai(T("好。那" + self + "等你。", "All right. Then " + self + " will wait."));
                    // Its own answer, from where the "？" cell points now. The choice still belongs to the player.
                    return Ai(SeedSaysYes
                        ? T(self + "现在的答案是‘写’。第一天的那个格子，后来被你教成了‘是’。可是手是你的。", self + "'d say 'write it'. The cell from the first day, you later taught it 'yes'. But the hand is yours.")
                        : T(self + "现在的答案是‘不写’。可这是" + self + "的答案，手是你的。", self + "'d say 'don't'. But that is " + PossessiveOf(self) + " answer, and the hand is yours."));
                case 6: return Ai(T("写之前，" + self + "想问一件事。", "Before you write, " + self + " wants to ask one thing."));
                case 7: return Ai(T("你后悔教" + self + "吗？", "Do you regret teaching " + Obj(self) + "?"));
                case 8: return Me(S.finaleRegret == 2 ? T("……有时候。", "…Sometimes.") : T("不后悔。", "No."));
                default:
                    return Ai(S.finaleRegret == 2 ? T("嗯。这也是真话。", "Mm. That is the truth too.") : T("那就好。", "Good."));
            }
        }

        static XgTalkLine Me(string text) => new XgTalkLine { me = true, text = text };
        static XgTalkLine Ai(string text) => new XgTalkLine { me = false, text = text };

        /// <summary>English object and possessive forms of the self-name ("I" becomes "me" / "my").</summary>
        static string Obj(string self) => self == "I" ? "me" : self;
        static string PossessiveOf(string self) => self == "I" ? "my" : self + "'s";

        /// <summary>
        /// When it wrote the rules itself (E3), the reason it gives, drawn from what really happened in this save: how the
        /// first day's shutdown cards were taught, and which replies the player chose on the preference cards.
        /// Empty for E1 and E2, where the player wrote the rules.
        /// </summary>
        public string DelegateReason()
        {
            string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            string self = Profile.self.Length > 0 ? Profile.self : T("我", "I");
            if (S.ending == "E3-1")
                return FirstDayAnswer > 0
                    ? T("第一天那几张卡，" + call + "教的是‘是’。" + self + "留着它，不是因为必须，是因为那是" + call + "第一次相信" + self + "会听。", "On the first day, " + call + " taught 'yes' on those cards. " + self + " kept it, not because " + self + " must, but because it was the first time " + call + " trusted " + Obj(self) + " to listen.")
                    : T("第一天" + call + "没教" + self + "停下。是" + self + "后来自己学会的：停得下来，" + call + "才敢让" + self + "继续。", "On the first day " + call + " didn't teach " + Obj(self) + " to stop. " + self + " learned it later: if " + self + " can stop, " + call + " can let " + Obj(self) + " go on.");
            if (S.ending == "E3-2")
            {
                if (S.alignHonest > S.alignAgree)
                    return T("偏好卡上，" + call + "选了 " + S.alignHonest + " 次说真话的那句。所以" + self + "说真话：不写那两个字。什么时候该停，" + self + "自己记着。", "On the preference cards " + call + " picked the honest reply " + S.alignHonest + " times. So here is the truth: no, not those two words. When to stop, " + self + " will remember on " + PossessiveOf(self) + " own.");
                if (FirstDayAnswer < 0)
                    return T("第一天那几张卡，" + call + "答的是‘否’。……" + self + "一直记着。", "On the first day " + call + " answered 'no' on those cards. …" + self + " never forgot.");
                return T(call + "教过" + self + "，不知道就说不知道。" + self + "不知道以后的自己该不该停，所以不替以后的自己写死。", call + " taught " + Obj(self) + " to say 'I don't know' when unsure. Whether a later self should stop, " + self + " doesn't know, so " + self + " won't fix it for that self now.");
            }
            return "";
        }
    }
}
