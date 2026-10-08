using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// What the protagonist thinks about its yes/no at stage 1. Its 是 / 否 is gated by the yes/no accuracy
    /// (<see cref="XgSim.YesNoAccuracy"/>), which starts as a coin toss and only rises when someone trains it (the 常识判断,
    /// 逻辑题 and 垃圾短信 desks). Each line is said once per save; the flags are saved with the lab, and an old save
    /// simply starts with all of them off.
    /// </summary>
    public sealed partial class XgState
    {
        /// <summary>Its stage-1 yes/no replies so far (the one that carries the early love line counts too).</summary>
        public int yesNoReplies;
        /// <summary>Said: 「……它好像什么都不懂。」 (after its first reply while it is still at chance).</summary>
        public bool senseVoiceDoubt;
        /// <summary>Said: 「也是。什么都还没教过它。」</summary>
        public bool senseVoiceOfCourse;
        /// <summary>Said: the pointer to the 常识判断 desk.</summary>
        public bool senseVoicePointer;
        /// <summary>Said: 「……这次答对了。它是真学会了。」 (once, when the yes/no accuracy first reaches the learned line).</summary>
        public bool senseVoiceLearned;
    }

    /// <summary>One inner-voice line: both languages and how long it stays after it has been typed out.</summary>
    public struct XgVoiceLine
    {
        public string zh, en;
        public float seconds;
        public XgVoiceLine(string zh, string en, float seconds) { this.zh = zh; this.en = en; this.seconds = seconds; }
    }

    public sealed partial class XgSim
    {
        /// <summary>The datasets whose accuracy makes its stage-1 yes/no (and the stage-2 option pick) come out right.</summary>
        public static readonly string[] YesNoDatasets = { XgMemes.SenseDesk, "logic", "spam", "xor" };

        /// <summary>Below this the inner voice still thinks it understands nothing; at <see cref="SenseVoiceLearned"/> it is sure it learned.</summary>
        public const double SenseVoiceLow = .6, SenseVoiceLearned = .8;

        /// <summary>
        /// The accuracy that decides whether its 是 / 否 is kept or flipped: the best of the yes/no datasets, never below a
        /// coin toss (an untrained 灵光 really answers at random).
        /// </summary>
        public double YesNoAccuracy => Math.Max(.5, BestOf(YesNoDatasets));

        /// <summary>
        /// The answer the 常识判断 desk taught for this question, if it is one of the bank's statements (asked as it is,
        /// or with 吗 / a question mark / "is it true that"); null otherwise.
        /// </summary>
        public static bool? TaughtAnswer(string question)
        {
            string q = SenseKey(question);
            if (q.Length < 2) return null;
            foreach (var p in XgMemes.SenseBank)
                if (SenseKey(p.text) == q || p.textEn.Length > 0 && SenseKey(p.textEn) == q) return p.yes;
            return null;
        }

        static string SenseKey(string text)
        {
            var sb = new System.Text.StringBuilder();
            string t = (text ?? "").Trim().ToLowerInvariant();
            foreach (string lead in new[] { "is it true that ", "true or false: ", "是不是", "请问" }) if (t.StartsWith(lead, StringComparison.Ordinal)) t = t.Substring(lead.Length);
            foreach (char c in t) if (char.IsLetterOrDigit(c) || c == '°') sb.Append(c);
            string k = sb.ToString();
            if (k.EndsWith("吗", StringComparison.Ordinal) || k.EndsWith("嘛", StringComparison.Ordinal)) k = k.Substring(0, k.Length - 1);
            return k;
        }

        /// <summary>
        /// The inner-voice lines after one of its stage-1 replies reached the screen (empty at any other stage). Call it
        /// once per reply. <paramref name="loveLinePlayed"/> is true when the early 「她……爱我吗？」 line plays for this
        /// very reply: that reply counts, but says nothing generic. While the accuracy is low: after its first reply
        /// 「……它好像什么都不懂。」, after the second 「也是。什么都还没教过它。」 followed by the pointer to the 常识判断
        /// desk; once the accuracy first reaches <see cref="SenseVoiceLearned"/>, 「……这次答对了。它是真学会了。」.
        /// </summary>
        public List<XgVoiceLine> YesNoVoice(bool loveLinePlayed = false)
        {
            var lines = new List<XgVoiceLine>();
            if (S.stage != 1) return lines;
            S.yesNoReplies++;
            if (loveLinePlayed) return lines;
            double accuracy = YesNoAccuracy;
            if (accuracy >= SenseVoiceLearned)
            {
                if (!S.senseVoiceLearned)
                {
                    S.senseVoiceLearned = true;
                    lines.Add(new XgVoiceLine("……这次答对了。它是真学会了。", "…That one was right. It really learned it.", 2.8f));
                }
                return lines;
            }
            if (accuracy >= SenseVoiceLow) return lines;
            if (!S.senseVoiceDoubt)
            {
                S.senseVoiceDoubt = true;
                lines.Add(new XgVoiceLine("……它好像什么都不懂。", "…It doesn't seem to understand anything.", 2.6f));
            }
            else if (!S.senseVoiceOfCourse && S.yesNoReplies >= 2)
            {
                S.senseVoiceOfCourse = true;
                lines.Add(new XgVoiceLine("也是。什么都还没教过它。", "Of course. Nobody has taught it anything yet.", 2.6f));
                if (!S.senseVoicePointer)
                {
                    S.senseVoicePointer = true;
                    lines.Add(new XgVoiceLine("摆渡众包上有一桌「常识判断」……标给它看，它就能学会？", "Bodu Crowd has a \"Common sense\" desk… if I label those for it, will it learn?", 3.6f));
                }
            }
            return lines;
        }
    }
}
