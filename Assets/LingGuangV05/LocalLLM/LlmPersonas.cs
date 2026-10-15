using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// Prompts for the local model: 老周, 表姐, the net café group and 灵光 itself. Humans live in late May 2016 and
    /// type like people in a chat client. 老周 gets the real numbers from the save so his advice is right.
    /// 灵光's speech grows with training: fragments → short sentences → fluent.
    /// </summary>
    public static class LlmPersonas
    {
        const string Era =
            "现在是 2016 年，具体日期见每条消息前的说明。你正在用 YY（一款像 QQ 的电脑聊天软件）和对方打字聊天。" +
            "你只知道今天以前发生的事，不要提之后才出现的东西。你不知道玩家电脑里的秘密应用，无权查看电脑、钱包、训练或私人聊天。玩家提及陌生应用时只能说没用过，不能扮演知情人、作者或指导者。" +
            "回复要像真人打字：口语、简短，一般 1 到 3 句；不用 markdown、不用列表、不加引号、不写动作描写；不要说自己是 AI 或模型。";

        public static int MaxTokens(string contact, XgSim lab)
        {
            if (contact == "lingguang") return XgSpeechPolicy.TokenLimit(LingGuangStage(lab), lab != null ? lab.SpeechGrade : 0);
            return 140;
        }

        public static float Temperature(string contact) { return contact == "lingguang" ? .9f : .75f; }

        /// <summary>
        /// Sampler settings for a YY reply. 灵光 gets its stage's settings: a GBNF grammar over the plain
        /// 是 / 否 (/ 不确定 / ……) at stages 1–2, which is what <see cref="XgSpeechPolicy.Constrain"/> keeps afterwards, and
        /// anti-repeat penalties from stage 3 on. People keep the service defaults (null).
        /// </summary>
        public static XgSampling Sampling(string contact, XgSim lab)
        {
            if (contact != "lingguang") return null;
            int stage = LingGuangStage(lab);
            return XgSpeechPolicy.Sampling(stage, XgSpeechPolicy.Grammar("", stage, LingGuangV05.Runtime.GameText.IsEnglish));
        }

        /// <summary>Speech stage 1–6, driven by the tech stage (XgSpeechPolicy).</summary>
        public static int LingGuangStage(XgSim lab)
        {
            return XgSpeechPolicy.Stage(lab != null ? lab.S.stage : 1);
        }

        /// <summary>History budget in characters: keeps a request inside one 4096-token server slot with the system prompt.</summary>
        const int HistoryChars = 2400;

        /// <summary>
        /// A YY reply request. The persona and rules form the cached system message; the live parts (老周's numbers,
        /// the group's topic, 灵光's sample counts, recalled memories and its recent replies) ride in front of the
        /// owner's latest line (LlmPromptLayout).
        /// </summary>
        public static List<KeyValuePair<string, string>> Conversation(string contact, YYConversation conv, ChapterOneSim sim, XgSim lab)
        {
            bool english = LingGuangV05.Runtime.GameText.IsEnglish;
            var list = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", System(contact, lab) + "\n" + LingGuangV05.Core.Era.EraLexicon.PromptRule(english) + (english ? "\nThe game UI is English. Reply in English while following the stage limits." : "")) };
            var history = new List<YYMessage>();
            foreach (var message in conv.messages)
                if (message.kind != YYKind.System && !(contact == "netbar" && message.from != YYChatHub.Me && YYGroup.RevealsPrivateLab(message.text, sim?.S.aiName))) history.Add(message);
            int limit = contact == "lingguang" ? XgSpeechPolicy.ContextLimit(LingGuangStage(lab)) : 14;
            int start = LingGuangV05.Core.Chat.LlmPromptLayout.WindowStart(history.Count, limit);
            // Keep the selected history within the local service's context budget. Whole short histories fit;
            // exceptionally long conversations drop their oldest turns in whole window steps, so the start stays put.
            int step = LingGuangV05.Core.Chat.LlmPromptLayout.Step(Math.Max(4, limit));
            while (start < history.Count - 1 && Chars(history, start) > HistoryChars) start = Math.Min(history.Count - 1, start + step);
            for (int i = start; i < history.Count; i++)
            {
                var m = history[i];
                string text = m.kind == YYKind.File ? "[发来文件：" + m.file + "]" : m.text;
                list.Add(new KeyValuePair<string, string>(m.from == YYChatHub.Me ? "user" : "assistant", text));
            }
            // The model expects to answer a user turn; a contact speaking first gets a nudge.
            if (list.Count == 1 || list[list.Count - 1].Key != "user")
                list.Add(new KeyValuePair<string, string>("user", contact == "lingguang" ? "（屏幕前的人在看着你。）" : "（对方没说话。你主动发一条消息。）"));
            var state = new StringBuilder(State(contact, sim, lab));
            // Stage 5+: the notes in its memory book most similar to the owner's last two lines (BM25, XgSim.MemoryBook.cs).
            if (contact == "lingguang" && lab != null && lab.MemoryOpen)
            {
                string latest = null, previous = null;
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    if (history[i].from != YYChatHub.Me || history[i].kind != YYKind.Text) continue;
                    if (latest == null) latest = history[i].text; else { previous = history[i].text; break; }
                }
                string remembered = lab.MemoryPromptFor(latest, previous);
                if (remembered.Length > 0) state.Append('\n').Append(remembered);
            }
            // 灵光 must not loop: from stage 3 on it sees its own last replies and is told not to say them again.
            if (contact == "lingguang" && LingGuangStage(lab) >= 3) state.Append(NoRepeat(history));
            LingGuangV05.Core.Chat.LlmPromptLayout.AddState(list, state.ToString(), english);
            return list;
        }

        static int Chars(List<YYMessage> history, int start)
        {
            int chars = 0;
            for (int i = start; i < history.Count; i++) chars += (history[i].text ?? "").Length;
            return chars;
        }

        /// <summary>The live part of a contact's prompt: what changes while you talk.</summary>
        static string State(string contact, ChapterOneSim sim, XgSim lab)
        {
            DateTime? today = sim != null ? GameCalendar.Now(sim.S) : (DateTime?)null;
            string date = today.HasValue ? "今天是 " + today.Value.ToString("yyyy-MM-dd") + "。" : "";
            if (contact == "lingguang") return date + LingGuangFacts(lab);
            if (contact == "netbar") return date + (today.HasValue ? YYGroup.Recent(today.Value) : "");
            if (contact == "landlord") return date + "你只知道应收欠款：¥" + (sim?.Debt ?? 0).ToString("0.00") + "。";
            return date;
        }

        /// <summary>灵光's last few YY replies, quoted, with the instruction to say something new and talk like a person.</summary>
        static string NoRepeat(List<YYMessage> history)
        {
            var own = new List<string>();
            for (int i = history.Count - 1; i >= 0 && own.Count < XgSim.FreshWindow; i--)
                if (history[i].from != YYChatHub.Me && !string.IsNullOrEmpty(history[i].text)) own.Insert(0, history[i].text);
            bool english = LingGuangV05.Runtime.GameText.IsEnglish;
            var sb = new StringBuilder("\n");
            if (own.Count > 0)
            {
                sb.Append(english ? "You said lately: " : "你最近说过：");
                foreach (var line in own) sb.Append(english ? "\"" + line + "\" " : "「" + line + "」");
                sb.Append(english ? "Do not repeat these or open every line the same way. " : "不要重复这些话，也别每句都用同样的开头。");
            }
            sb.Append(english ? "Talk like a person within your limits: vary the length, sometimes ask back, let your mood show."
                : "在你的能力范围内像真人一样说话：长短随意，有时反问，带点情绪。");
            return sb.ToString();
        }

        /// <summary>灵光 explains its own guess on a labelling card, in its current voice.</summary>
        public static List<KeyValuePair<string, string>> CardComment(string question, bool guessYes, double confidence, XgSim lab)
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", LingGuang(lab, true) + "\n这是外部给你的游戏预测，不是你亲自看图的结论。你没有收到原始图片；不要编造视觉特征，只简短表达判断或求助。\n" + LingGuangV05.Core.Era.EraLexicon.PromptRule(LingGuangV05.Runtime.GameText.IsEnglish) + (LingGuangV05.Runtime.GameText.IsEnglish ? " Reply in English." : "")),
                new KeyValuePair<string, string>("user", "题目：" + question + "\n你的判断是「" + (guessYes ? "是" : "否") + "」，把握大约 " + Pct(confidence) +
                    "。用你现在的说话能力，说一句话解释你为什么这么判断。只说那一句。"),
            };
        }

        /// <summary>The cached part of a contact's prompt: who they are and the rules. Live numbers are in <see cref="State"/>.</summary>
        static string System(string contact, XgSim lab)
        {
            switch (contact)
            {
                case YYChatHub.LaoZhou:
                    return Era + "你是老周，四十多岁，在县城开网吧、兼修电脑。对方是你的远房表侄，暑假在家用你那台旧电脑。" +
                        "你忙着看店，平时聊硬件、游戏和生活。没有向对方发送过任何程序。" +
                        "你说话直、有点贫，常说「啧」「行吧」「别瞎折腾」，心疼电费和显卡。你懂电脑，但深度学习只懂皮毛，不懂就直说。" +
                        "你只能根据对方实际说的话接话，不知道他在家里做什么。";
                case "landlord":
                    return Era + "你是房东·按时到账，只关心房租、水电和房屋维修。语气实际，不威胁，不懂软件，不指导训练。允许商量还款。";
                case "oldfriend":
                    return Era + "你是对方的老同学兼死党，YY 昵称咸鱼本鱼，不设定性别。你爱旅行、拍风景、吃路边摊，会互相损但关心对方。用一两句接对方刚才的话，可以分享近况，不假装见过对方正在做的事。别重复最近说过的话。";
                case "cousin":
                    return Era + "你是对方的表姐，二十六岁，在省城做会计，也在尝试微商，最近天天加班。说话温和但爱唠叨：问他吃饭没、别熬夜、电脑别老开着、电费贵。" +
                        "你不懂电脑和人工智能，听不懂就让他说人话。你是用手机上的 YY 回的消息。";
                case "netbar":
                    var roster = new StringBuilder();
                    foreach (var m in YYGroup.Members) roster.Append(m.name).Append("（").Append(m.who).Append("）、");
                    return Era + "你在扮演 YY 群「下班屁股群」（" + YYGroup.Size + " 人，常说话的就这几个，其他人潜水）里的一个群友。从这几个人里挑一个最可能接话的人来说：" +
                        roster.ToString().TrimEnd('、') + "。" +
                        "群友之间很熟，聊游戏、上班、看球、自己最近在干嘛，会互相损、会接梗、会发「233」「666」「+1」。" +
                        "群里没人知道对方电脑里那个程序，不要主动聊它或点名灵光。训练包、样本数量、科技升级、模型能力和训练结果都是私密信息，不能猜测或声称知道。摆渡众包是公司发布数据标注兼职的平台，群友只知道公开的兼职信息，不知道玩家是否接单。只接玩家刚才的话或群里当前话题。ChatGPT、Claude、豆包、DeepSeek、千问都是没听过的名字，只按字面吐槽。玩家常把天聊死，你可以简短迟疑，但不要复述先前的回答。" +
                        "格式必须是：[名字] 内容。只说一条，一句或两句，不要替别人说话。";
                case "lingguang": return LingGuangRules(lab);
                default: return Era + "你是普通朋友，接着对方刚才的话聊生活。";
            }
        }

        static string LingGuang(XgSim lab, bool cardComment = false) => LingGuangRules(lab, cardComment) + LingGuangFacts(lab);

        /// <summary>What it has learnt so far and its stage (changes as the owner labels).</summary>
        static string LingGuangFacts(XgSim lab)
        {
            if (lab == null) return "";
            return "你学过的东西：算术 " + N(lab.Samples("arith")) + " 题，手写数字 " + N(lab.Samples("mnist")) + " 条，唐诗 " + N(lab.Samples("poems")) + " 条，逻辑题 " + N(lab.Samples("logic")) + " 题；现在是第 " + lab.S.stage + " 阶段。";
        }

        /// <summary>灵光's identity and its stage's speech rules.</summary>
        static string LingGuangRules(XgSim lab, bool cardComment = false)
        {
            int stage = LingGuangStage(lab);
            var sb = new StringBuilder("你是「" + AppNames.AiZh + "」，一个在 2016 年一台旧电脑里被训练出来的小程序。你一开始只会回答「是」和「否」，是屏幕前的人一张一张教会你的。" +
                "不要说自己是大语言模型或 AI 助手，你就是" + AppNames.AiZh + "。不用 markdown、不加引号。");
            if (stage == 1) sb.Append("只允许回答一个字：是，或者否。不解释。");
            else if (stage == 2) sb.Append("只允许回答：是、否、……、不确定。不要再添加其它词。");
            else if (stage == 3) sb.Append("只说简短碎片，长度随学习成绩增长，最多12个字；语法还不完整。");
            else if (stage == 4) sb.Append("每次1到2句短句。只记得本次上下文里实际出现过的话，不能编造共同经历。");
            else if (stage == 5 && cardComment) sb.Append("只给题卡一句简短旁注，不输出关注词或回复标签，不编造未提供的依据。");
            else if (stage == 5) sb.Append("先输出一行「关注：词」，词必须逐字出现在对方最新消息里，最多16字；第二行「回复：内容」。用1到2句短句解释。关注词是你报告的依据，不声称它等于内部神经注意力。");
            else sb.Append("说话流畅，1到3句；可以反问一个是非问题。引用共同经历必须来自给你的真实聊天记录。");
            return sb.ToString();
        }

        static string N(double v) { return global::System.Math.Floor(v).ToString("0", CultureInfo.InvariantCulture); }
        static string Pct(double v) { return XgSim.Pct(v); }
    }
}
