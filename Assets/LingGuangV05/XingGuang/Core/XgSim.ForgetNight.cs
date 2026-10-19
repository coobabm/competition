using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One round of 忘言夜 (the stage-4 closing YY exchange before 长记忆·翻译).</summary>
    public sealed class XgFnRound
    {
        public string id = "", askZh = "", askEn = "";
        public string replyZh = "", replyEn = "";
        public XgVoiceLine[] voice = new XgVoiceLine[0];
    }

    public sealed partial class XgState
    {
        /// <summary>忘言夜 (stage 4 → 5): current round (0 = not started, 1..3 = active, 4 = done).</summary>
        public int fnStep;
        public bool fnDone;
    }

    /// <summary>
    /// 忘言夜, the stage-4 interactive YY exchange when the 「长记忆·翻译」 exam opens. At stage 4 it can finally speak short
    /// sentences, so the protagonist tests whether it can hold onto a simple code ("烤冷面，多加醋") across two ordinary lines.
    /// By the third turn the earlier line has already fallen out of its short window, showing why stage 5's long memory is needed.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string ForgetNightChronicle = "forgetnight";

        public static readonly XgFnRound[] FnRounds =
        {
            new XgFnRound
            {
                id = "seed",
                askZh = "记住这个暗号：今晚吃烤冷面，多加醋。",
                askEn = "Remember this code: grilled cold noodles tonight, extra vinegar.",
                replyZh = "记住了。烤冷面，多加醋。",
                replyEn = "Got it. Grilled cold noodles, extra vinegar.",
                voice = new[]
                {
                    new XgVoiceLine("能说整句，还挺像回事。……再聊两句别的试试。", "Full sentences now. Sounds convincing. …Let's talk about something else and see.", 2.6f),
                }
            },
            new XgFnRound
            {
                id = "rain",
                askZh = "外面又下雨了，机箱风扇嗡嗡响，今天过得真快。",
                askEn = "It's raining outside again, the case fan is humming, and today went by fast.",
                replyZh = "下雨天屋里潮，风扇声音大。",
                replyEn = "Rain makes the room damp, and the fan sounds louder.",
                voice = new[]
                {
                    new XgVoiceLine("接得还挺顺。……现在问它刚才那句。", "Smooth enough. …Now ask what I told it just before.", 2.4f),
                }
            },
            new XgFnRound
            {
                id = "recall",
                askZh = "我第一句让你记住的暗号是什么？",
                askEn = "What was the code I asked you to remember in my first line?",
                replyZh = "……什么暗号？你刚说外面下雨，风扇响。",
                replyEn = "…What code? You just said it's raining and the fan is loud.",
                voice = new[]
                {
                    new XgVoiceLine("……全忘了。才隔了一句话就顶出去了。", "…Completely forgotten. One sentence in between and it got pushed right out.", 2.8f),
                    new XgVoiceLine("光会说短句还不够。它得有长记忆，才能存住往事。", "Short sentences aren't enough. It needs a long memory to keep what happened.", 3.0f),
                }
            }
        };

        /// <summary>True when stage 4's closing YY test is ready to be offered in YY.</summary>
        public bool OfferForgetNight => S.stage == 4 && !S.fnDone && (S.fnStep > 0 || ExamReady || ExamPassPending);

        public bool ForgetNightActive => S.fnStep > 0 && !S.fnDone;

        public XgFnRound FnRound
        {
            get
            {
                if (S.fnDone) return null;
                int index = S.fnStep <= 0 ? 0 : S.fnStep - 1;
                return index >= 0 && index < FnRounds.Length ? FnRounds[index] : null;
            }
        }

        public void ForgetNightStart()
        {
            if (S.stage == 4 && !S.fnDone && S.fnStep == 0) S.fnStep = 1;
        }

        public string ForgetNightChoice(bool english)
        {
            if (!OfferForgetNight) return null;
            var r = FnRound;
            return r == null ? null : (english ? r.askEn : r.askZh);
        }

        public bool ForgetNightMatches(string text)
        {
            if (!OfferForgetNight || string.IsNullOrEmpty(text)) return false;
            var r = FnRound;
            if (r == null) return false;
            text = text.Trim();
            return text == r.askZh || text == r.askEn;
        }

        /// <summary>Advances the stage-4 YY memory test when the player sends the current round's line.</summary>
        public XgFnRound ForgetNightAnswer(string text)
        {
            if (!ForgetNightMatches(text)) return null;
            if (S.fnStep == 0) S.fnStep = 1;
            var round = FnRound;
            S.fnStep++;
            if (S.fnStep > FnRounds.Length)
            {
                S.fnDone = true;
                NoteChronicle(ForgetNightChronicle);
            }
            return round;
        }

        string ForgetNightChronicleBody(bool english, string self, string you)
            => english
                ? "One night you tested how much I could hold in my head, telling me to remember 'grilled cold noodles, extra vinegar'. One remark about the rain and the fan later, I had already forgotten every word of it. That night you saw that short sentences alone could not keep the past."
                : "有一个夜里，" + you + "试" + self + "能记多长的话，教" + self + "记一句「烤冷面，多加醋」。才聊了一句窗外下雨和机箱风扇，再问起来，" + self + "已经忘得一干二净。那夜" + you + "明白，光会说短句，还留不住往事。";
    }
}
