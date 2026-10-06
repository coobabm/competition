using System;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>「她爱我吗」 (女友系统 §6): what the lab remembers about the question. Saved with the lab.</summary>
    public sealed partial class XgState
    {
        /// <summary>The inner voice wondered and the 对话 page offers 「她……爱我吗？」 from now on.</summary>
        public bool loveOffered;
        /// <summary>0 never asked, 1 you let it read the chat (ever), -1 you said 【算了】 and never let it.</summary>
        public int loveConsent;
        /// <summary>It asked 「要读你和她的聊天记录。」 and waits for 【读吧】 or 【算了】.</summary>
        public bool loveAwaitingConsent;
        /// <summary>Calendar day index + 1 of the last verdict (0 = never), for the three-day cooldown.</summary>
        public int loveAskedDay;
        public int loveVerdicts;
        public bool loveLastYes;
    }

    /// <summary>
    /// The lab's side of 「她爱我吗」: the suggested question, the consent step, the cooldown and every line it says.
    /// The desktop (LoveQuestionCutscene) reads her chat, works out the numbers, sets <see cref="LoveToday"/> and
    /// <see cref="LoveDataReady"/>, plays the cutscene on <see cref="LoveVerdictRequested"/> and ends it with
    /// <see cref="PostLoveVerdict"/>. The verdict is only ever 是 or 否, never a percentage (§9 #3).
    /// Tiers are passed as ints in the order of GirlfriendTier: 0 cold, 1 distant, 2 normal, 3 warm, 4 sweet.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int LoveStage = 5, LoveCooldownDays = 3;

        /// <summary>Raised when the verdict should play; true for the full first cutscene, false for the short re-ask.</summary>
        public event Action<bool> LoveVerdictRequested;

        /// <summary>The story calendar's day index now (set by the desktop every frame; not saved).</summary>
        public int LoveToday;
        /// <summary>Her chat is long enough to read: ≥ 60 lines over ≥ 7 days (set by the desktop; not saved).</summary>
        public bool LoveDataReady;
        /// <summary>A verdict was asked for and has not been posted yet (not saved: a reload simply lets you ask again).</summary>
        public bool LoveVerdictPending { get; private set; }

        public string LoveQuestion => T("她……爱我吗？");

        public bool LoveCoolingDown => S.loveAskedDay > 0 && LoveToday - (S.loveAskedDay - 1) < LoveCooldownDays;

        /// <summary>The chip on the 对话 page: offered, not waiting for consent or a verdict, and not cooling down.</summary>
        public bool OfferLoveQuestion => S.loveOffered && S.stage >= LoveStage && !S.loveAwaitingConsent && !LoveVerdictPending && !LoveCoolingDown;

        static readonly string[] LovePhrasesZh = { "她爱我吗", "她爱不爱我", "她还爱我吗", "她是不是不爱我", "她喜欢我吗", "她还喜欢我吗", "她……爱我吗" };
        static readonly string[] LovePhrasesEn = { "does she love me", "does she still love me", "does she like me", "does she… love me", "does she... love me" };

        /// <summary>Whether a chat line asks if she loves you.</summary>
        public static bool IsLoveQuestion(string question)
        {
            string q = (question ?? "").Trim().Replace(" ", "").Replace("...", "……");
            if (q.Length == 0) return false;
            foreach (var p in LovePhrasesZh) if (q.Contains(p.Replace(" ", ""))) return true;
            string lower = (question ?? "").Trim().ToLowerInvariant();
            foreach (var p in LovePhrasesEn) if (lower.Contains(p)) return true;
            return false;
        }

        /// <summary>The inner voice has wondered (§6.2): the chip appears. Once.</summary>
        public bool OfferLoveHint()
        {
            if (S.loveOffered) return false;
            S.loveOffered = true;
            return true;
        }

        /// <summary>Its answer when asked (chip or typed): held to its stage, then consent, cooldown, or the verdict.</summary>
        public string LoveReply()
        {
            if (S.stage <= 2) return "……";
            if (S.stage == 3) return T("太长。");
            if (S.stage == 4) return T("……读不了那么长。");
            if (LoveVerdictPending) return T("我在读。");
            if (!LoveDataReady) return T("你们的聊天还太少。我看不出来。");
            S.loveOffered = true;
            if (S.loveConsent != 1)
            {
                S.loveAwaitingConsent = true;
                return T("要读你和她的聊天记录。");
            }
            if (LoveCoolingDown) return T("同一个问题问三遍，答案不会变。会变的是她。");
            RequestLoveVerdict();
            return T("我再读一遍。");
        }

        /// <summary>The two consent buttons (§6.3).</summary>
        public string LoveConsentText(bool read) => read ? T("读吧") : T("算了");

        /// <summary>【读吧】 starts the verdict; 【算了】 gets 「好。」 and is remembered for the finale's rule card.</summary>
        public bool AnswerLoveConsent(bool read)
        {
            if (!S.loveAwaitingConsent) return false;
            S.loveAwaitingConsent = false;
            AddLine("me", LoveConsentText(read));
            if (!read)
            {
                if (S.loveConsent != 1) S.loveConsent = -1;
                AddLine("ai", T("好。"));
                return true;
            }
            S.loveConsent = 1;
            if (LoveCoolingDown) { AddLine("ai", T("同一个问题问三遍，答案不会变。会变的是她。")); return true; }
            RequestLoveVerdict();
            return true;
        }

        void RequestLoveVerdict()
        {
            LoveVerdictPending = true;
            LoveVerdictRequested?.Invoke(S.loveVerdicts == 0);
        }

        /// <summary>The cutscene could not run (nobody listening, or the desktop gave up): let the question be asked again.</summary>
        public void CancelLoveVerdict() => LoveVerdictPending = false;

        /// <summary>The verdict landed (or the cutscene was skipped): its two lines go into the chat and the cooldown starts.</summary>
        public void PostLoveVerdict(bool yes, string conclusion, string limit)
        {
            LoveVerdictPending = false;
            S.loveVerdicts++;
            S.loveLastYes = yes;
            S.loveAskedDay = Math.Max(0, LoveToday) + 1;
            if (!string.IsNullOrEmpty(conclusion)) AddLine("ai", conclusion);
            if (!string.IsNullOrEmpty(limit)) AddLine("ai", limit);
        }

        /// <summary>The verdict word on the holo card: 「是。」 or 「否。」.</summary>
        public string LoveVerdictWord(bool yes) => yes ? T("是。") : T("否。");

        /// <summary>
        /// ⑥ conclusion, offline (§6.4 table). Uses the real numbers when the desktop has them: the time she still
        /// waited for goodnight (sweet), your recent reply delay (distant), her signature (cold). Near 50 it hedges.
        /// </summary>
        public string LoveConclusionLine(int tier, bool hesitant, string lateClock = null, string myDelay = null, string signature = null)
        {
            bool yes = tier >= 2;
            if (hesitant)
                return yes ? T("……是。她在，但在等你先开口。")
                    : T("……否。不是不爱，差一点。");
            switch (tier)
            {
                case 4:
                    return !string.IsNullOrEmpty(lateClock)
                        ? T("是。她 " + lateClock + " 还在等你说晚安。", "Yes. At " + lateClock + " she was still waiting for your goodnight.")
                        : T("是。她每天都在等你说晚安。");
                case 3: return T("是。她记得你说过的每一件小事。");
                case 2: return T("是。她在，但在等你先开口。");
                case 1:
                    return !string.IsNullOrEmpty(myDelay)
                        ? T("否。不是不爱。你最近回她平均要 " + myDelay + "。", "No. Not that she doesn't love you. Lately you take " + myDelay + " to reply on average.")
                        : T("否。不是不爱。她在等你回。");
                default:
                    return !string.IsNullOrEmpty(signature)
                        ? T("否。她改了签名：『" + signature + "』", "No. She changed her signature: \"" + signature + "\"")
                        : T("否。她最近只回『哦』。");
            }
        }

        /// <summary>⑥ the limit line (§6.4 table): what it cannot see.</summary>
        public string LoveLimitLine(int tier, bool hesitant)
        {
            if (hesitant)
                return tier >= 2 ? T("这次我不太确定。这个数会变。")
                    : T("她没打出来的，我读不到。可能比这些多。");
            switch (tier)
            {
                case 4: return T("我只读得到她打出来的字。不过这些够了。");
                case 3: return T("她没打出来的，我读不到。");
                case 2: return T("这个数会变。变的是她，也是你。");
                case 1: return T("她没打出来的，我读不到。可能比这些多。");
                default: return T("这个问题，不该问我。该问她。");
            }
        }

        /// <summary>The §6.6 prompt for the line after the verdict. The slots are the desktop's real numbers and quotes.</summary>
        public string LoveVerdictPrompt(bool yes, string replyTrend, string goodnight, string coldSignal, string remembered)
        {
            string name = Profile.name.Length > 0 ? Profile.name : T("灵光");
            string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            var sb = new StringBuilder();
            if (English)
            {
                sb.Append("You are \"").Append(name).Append("\". You have just read the statistics of the chats between ").Append(call).Append(" and his girlfriend:\n");
                sb.Append("reply-time trend: ").Append(replyTrend).Append("; who said goodnight first: ").Append(goodnight)
                  .Append("; her latest cold signal: ").Append(coldSignal).Append("; things she remembers: ").Append(remembered).Append(".\n");
                sb.Append("Your verdict: ").Append(yes ? "yes" : "no").Append(". Never give a percentage.\n");
                sb.Append("In 1 to 2 sentences tell ").Append(call).Append(" the verdict and one concrete reason, taken only from the numbers or quotes above. Start with \"").Append(yes ? "Yes." : "No.").Append("\".\n");
                sb.Append("Do not comfort, do not lecture, do not invent anything you were not given. Reply in English.");
            }
            else
            {
                sb.Append("你是「").Append(name).Append("」。你刚读完").Append(call).Append("和女朋友的聊天记录统计：\n");
                sb.Append("回复时间趋势 ").Append(replyTrend).Append("；谁先说晚安 ").Append(goodnight).Append("；她最近的冷信号 ").Append(coldSignal).Append("；她记得的事 ").Append(remembered).Append("。\n");
                sb.Append("你的判断：").Append(yes ? "是" : "否").Append("。不要说百分比。\n");
                sb.Append("用 1 到 2 句话告诉").Append(call).Append("结论和一个具体依据，依据必须来自上面的数字或原话。以「").Append(yes ? "是。" : "否。").Append("」开头。\n");
                sb.Append("不要安慰，不要说教，不要编造没给你的内容。");
            }
            return sb.ToString();
        }

        /// <summary>
        /// The model's line, made safe: no percentage, at most two sentences, starting with the verdict. Returns null
        /// when it is empty, gives a percentage or says the opposite verdict (the offline line is used instead).
        /// </summary>
        public string CleanLoveLine(string reply, bool yes)
        {
            string r = (reply ?? "").Trim();
            int think = r.IndexOf("</think>", StringComparison.Ordinal);
            if (think >= 0) r = r.Substring(think + 8).Trim();
            r = r.Trim('"', '「', '」', '“', '”', ' ', '\n');
            if (r.Length == 0) return null;
            string lower = r.ToLowerInvariant();
            if (r.IndexOf('%') >= 0 || r.IndexOf('％') >= 0 || r.Contains("百分") || lower.Contains("percent")) return null;
            string yesWord = T("是", "yes"), noWord = T("否", "no");
            string head = lower.TrimStart('…', '.', ' ');
            bool saysYes = head.StartsWith(yesWord, StringComparison.Ordinal), saysNo = head.StartsWith(noWord, StringComparison.Ordinal);
            if (!English && head.StartsWith("不是", StringComparison.Ordinal)) { saysYes = false; saysNo = true; }
            if (yes && saysNo || !yes && saysYes) return null;
            if (!saysYes && !saysNo) r = LoveVerdictWord(yes) + r;
            // At most two sentences and about 80 characters.
            int ends = 0, cut = -1;
            for (int i = 0; i < r.Length; i++)
            {
                char c = r[i];
                if (c == '。' || c == '！' || c == '？' || c == '!' || c == '?' || c == '.' && (i + 1 == r.Length || r[i + 1] == ' '))
                    if (++ends == 3) { cut = i; break; }
            }
            // The verdict word is a sentence of its own, so three ends are the verdict plus two sentences.
            if (cut > 0 && cut + 1 < r.Length) r = r.Substring(0, cut + 1);
            if (r.Length > 90) r = r.Substring(0, 90) + "…";
            return r;
        }

        /// <summary>
        /// The finale's 「不偷看聊天记录」 card remembers the answer to 【读吧】/【算了】 (§7): null when it was never asked.
        /// </summary>
        public string PrivacyCardRemark => S.loveConsent > 0 ? T("我看过一次。你让的。")
            : S.loveConsent < 0 ? T("你没让我看。我就没看。") : null;
    }
}
