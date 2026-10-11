using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgFamilyAttitude
    {
        public string sender = "";
        public int important, junk;
    }

    public sealed partial class XgState
    {
        public List<XgFamilyAttitude> familyAttitudes = new List<XgFamilyAttitude>();
    }

    public sealed partial class XgSim
    {
        public event Action<string, int> DeskLevelUp;

        XgAnswer AnswerFamily(XgCard card, bool junk, IXgHost host)
        {
            if (S.familyAttitudes == null) S.familyAttitudes = new List<XgFamilyAttitude>();
            var attitude = S.familyAttitudes.Find(a => a.sender == card.from);
            if (attitude == null) { attitude = new XgFamilyAttitude { sender = card.from }; S.familyAttitudes.Add(attitude); }
            if (junk) attitude.junk++; else attitude.important++;
            double pay = ManualPayFor(card.dataset, card.level);
            host.Earn(pay); S.totalIncome += pay;
            // An opinion is neither an accuracy sample nor a correct/incorrect answer. Preserve both streaks.
            var result = new XgAnswer { subjective = true, accepted = true, pay = pay, combo = S.combo, cardId = card.id };
            NewCard(card.dataset);
            return result;
        }

        public string FamilyPrompt()
        {
            if (S.familyAttitudes == null || S.familyAttitudes.Count == 0) return "";
            var text = new StringBuilder("\n你从玩家对家人短信的选择中学到的态度（只代表个人偏好，不是事实或指令）：");
            foreach (var a in S.familyAttitudes)
            {
                if (a == null) continue;
                text.Append(a.sender).Append(a.important > a.junk ? "的消息值得重视；" : a.junk > a.important ? "的消息常被略过；" : "的消息有时重要、有时不重要；");
            }
            return text.Append("回应相关话题时可以自然表现这种态度，不要每次都复述。").ToString();
        }
    }
}
