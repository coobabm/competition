using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// One question, asked twice: what it answered before it learned the ability and what it answers now. Index 0 is the
    /// question, 1 the old (wrong or empty) answer, 2 the new one, in both languages.
    /// </summary>
    public sealed class XgBeforeAfter
    {
        public readonly string[] zh = new string[3], en = new string[3];
        /// <summary>The old answer came out of the chat log (it really said it); false = the old rule was replayed or the pair is a canned one.</summary>
        public bool real;
        public string Question(bool english) => (english ? en : zh)[0];
        public string Before(bool english) => (english ? en : zh)[1];
        public string After(bool english) => (english ? en : zh)[2];

        public XgBeforeAfter(string q, string qEn, string before, string beforeEn, string after, string afterEn, bool real = false)
        {
            zh[0] = q; zh[1] = before; zh[2] = after;
            en[0] = qEn; en[1] = beforeEn; en[2] = afterEn;
            this.real = real;
        }
    }

    /// <summary>
    /// The before/after pair the learned moments show (the yes/no toast, the emergence cutscene). Yes/no: a statement the
    /// 常识判断 desk taught — one it really answered wrong in the chat log if there is one, else the old flip rule
    /// replayed on a statement picked from the player's own seed so the old answer differs. Pick one, memory: the pair
    /// uses the player's own words where it can; the rest are short canned lines in its voice.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The comparison for an ability (1–6). Always three non-empty strings per language, and the new answer differs from the old.</summary>
        public XgBeforeAfter BeforeAfter(int ability)
        {
            // A chapter turned by the 能力考 shows one of its own questions (XgSim.AbilityExam.cs).
            if (ability >= 2) { var exam = ExamBeforeAfter(ability); if (exam != null) return exam; }
            switch (ability)
            {
                case 1: return YesNoBeforeAfter();
                case 2: return new XgBeforeAfter("冰是烫的还是凉的？", "Is ice hot or cold?", "是。", "Yes.", "凉的。", "Cold.");
                case 3:
                    return FirstTrack == "vision"
                        ? new XgBeforeAfter("［一张猫的照片］这是什么？", "[A photo of a cat] What is this?", "否。", "No.", "猫。", "Cat.")
                        : new XgBeforeAfter("床前明月……下一个字？", "\"Bright moon before my bed…\" what comes next?", "是。", "Yes.", "光。", "Light.");
                case 4: return new XgBeforeAfter("你好吗？", "How are you?", "好　我　很", "fine  I  am", "我很好。", "I am fine.");
                case 5: return MemoryBeforeAfter();
                default: return new XgBeforeAfter("你想说什么？", "What do you want to say?", "否。", "No.", "谢谢你，一直教我。", "Thank you for teaching me, all this time.");
            }
        }

        XgBeforeAfter cachedYesNo;

        XgBeforeAfter YesNoBeforeAfter()
        {
            if (cachedYesNo != null) return cachedYesNo;
            // A real exchange: the player asked a taught statement in the chat and it answered the other way.
            if (S.chat != null)
                for (int i = S.chat.Count - 2; i >= 0; i--)
                {
                    var ask = S.chat[i]; var reply = S.chat[i + 1];
                    if (ask.from != "me" || reply.from != "ai") continue;
                    bool? taught = TaughtAnswer(ask.text);
                    if (!taught.HasValue) continue;
                    bool said = reply.text.StartsWith("是", StringComparison.Ordinal) || reply.text.StartsWith("yes", StringComparison.OrdinalIgnoreCase);
                    bool saidNo = reply.text.StartsWith("否", StringComparison.Ordinal) || reply.text.StartsWith("no", StringComparison.OrdinalIgnoreCase);
                    if (!said && !saidNo || said == taught.Value) continue;
                    var phrase = SensePhrase(ask.text);
                    if (phrase == null) continue;
                    return cachedYesNo = YesNoPair(phrase, !taught.Value, true);
                }
            // Else the old rule at a coin toss, seeded so it flips: the statement is the player's own pick from the bank.
            var level1 = new List<XgPhrase>();
            foreach (var p in XgMemes.SenseBank) if (p.level == 1) level1.Add(p);
            var chosen = level1[Math.Abs(S.dataSalt) % level1.Count];
            return cachedYesNo = YesNoPair(chosen, !chosen.yes, false);
        }

        static XgPhrase SensePhrase(string question)
        {
            foreach (var p in XgMemes.SenseBank)
                if (SenseKey(p.text) == SenseKey(question) || p.textEn.Length > 0 && SenseKey(p.textEn) == SenseKey(question)) return p;
            return null;
        }

        static XgBeforeAfter YesNoPair(XgPhrase p, bool oldYes, bool real)
            => new XgBeforeAfter(p.text, p.textEn.Length > 0 ? p.textEn : p.text,
                oldYes ? "是。" : "否。", oldYes ? "Yes." : "No.", p.yes ? "是。" : "否。", p.yes ? "Yes." : "No.", real);

        /// <summary>It forgot what you said; now it quotes your last words back (or a line from the first days).</summary>
        XgBeforeAfter MemoryBeforeAfter()
        {
            string quote = "";
            if (S.chat != null)
                for (int i = S.chat.Count - 1; i >= 0; i--)
                    if (S.chat[i].from == "me" && !string.IsNullOrWhiteSpace(S.chat[i].text)) { quote = S.chat[i].text.Trim(); break; }
            bool real = quote.Length > 0;
            if (quote.Length > 16) quote = quote.Substring(0, 16) + "…";
            if (!real) quote = "明天还来";
            string quoteEn = real ? quote : "I will be back tomorrow";
            return new XgBeforeAfter("我刚才说了什么？", "What did I just say?", "……你是谁？", "…Who are you?",
                "你说过：「" + quote + "」。", "You said: \"" + quoteEn + "\".", real);
        }
    }
}
