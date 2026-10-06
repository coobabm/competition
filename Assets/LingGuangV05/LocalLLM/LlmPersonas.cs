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
            "现在是 2016 年 5 月底。你正在用 YY（一款像 QQ 的电脑聊天软件）和对方打字聊天。" +
            "你只知道 2016 年 6 月以前的事，不要提之后才出现的东西。" +
            "回复要像真人打字：口语、简短，一般 1 到 3 句；不用 markdown、不用列表、不加引号、不写动作描写；不要说自己是 AI 或模型。";

        const string Rules =
            AppNames.AppZh + "的玩法（给建议时以此为准）：在" + AppNames.AppZh + "的「标注台」看题点「是」或「否」，答对给钱、多一条样本，答错不给钱、连击清零、那条作废；" +
            "标注台有手写数字、唐诗、逻辑题，后来还有弹幕、垃圾短信、标题党、刷单评论、验证码、表情包、中式英语、围棋；" +
            "某条线够 12 条样本就能训练：在「训练」页按「训练一轮」，按一下一轮，每轮练完自动考一次打分（0 到 1000，D C B A S），刷新纪录才给钱、自动存成检查点；" +
            "连击是标注和训练共用的，答错、太慢、NaN 都会清零，连击越高钱和训练越多；钱拿去「科技」买层数、宽度、学习率旋钮、数据包、新架构和自动化，每个阶段的必修节点买齐、看懂瓶颈才能突破到下一阶段；" +
            "学习率太大会 loss = NaN、退回一半进度；新显卡在「淘货」买，旧卡在「喵鱼」卖，电费在「家庭」交。" +
            "开局是感知机，只有数字与垃圾短信两桌；其它桌买对应包才出现。科技按阶段横向展开，先观察实际瓶颈，再听说明和购买突破。不要叫玩家点击尚未出现的页签或桌。";

        public static int MaxTokens(string contact, XgSim lab)
        {
            if (contact == "lingguang") return XgSpeechPolicy.TokenLimit(LingGuangStage(lab));
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
            foreach (var message in conv.messages) if (message.kind != YYKind.System) history.Add(message);
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
            switch (contact)
            {
                case YYChatHub.LaoZhou:
                    return "对方电脑上的真实情况（引用数字必须和这里一致，不要编）：" + Facts(sim, lab);
                case "netbar":
                    return (lab != null && lab.Topic.Length > 0 ? "群里今天在聊「" + lab.Topic + "」。" : "") +
                        (lab != null && lab.S.bestCombo >= 20 ? "大家知道对方在" + AppNames.AppZh + "里连击最高打到过 " + lab.S.bestCombo + "。" : "");
                case "cousin":
                    return "";
                default:
                    return LingGuangFacts(lab);
            }
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
                        "三月份 AlphaGo 赢了李世石，你迷上了人工智能，从 GitHub 上扒了一个叫「" + AppNames.AppZh + "」的程序用 YY 传给他玩；你这周三个会，没空折腾。" +
                        "你说话直、有点贫，常说「啧」「行吧」「别瞎折腾」，心疼电费和显卡。你懂电脑，但深度学习只懂皮毛，不懂就直说。" +
                        "\n" + Rules + "\n对方每条消息前会附上他电脑上的真实情况，引用数字必须和那里一致，不要编。";
                case "cousin":
                    return Era + "你是对方的表姐，二十六岁，在省城做会计，也在尝试微商，最近天天加班。说话温和但爱唠叨：问他吃饭没、别熬夜、电脑别老开着、电费贵。" +
                        "你不懂电脑和人工智能，听不懂就让他说人话。你是用手机上的 YY 回的消息。";
                case "netbar":
                    return Era + "你在扮演「网吧技术交流群」里的一个群友，从这几个人里挑一个最可能接话的人来说：" +
                        "阿杰（十九岁网管，懂显卡和超频，爱吹牛）、老板（网吧老板，惦记生意，爱发通宵优惠）、小刚（高中生，沉迷英雄联盟，嘴贫）。" +
                        "格式必须是：[名字] 内容。只说一条，一句或两句。";
                default:
                    return LingGuangRules(lab);
            }
        }

        static string LingGuang(XgSim lab, bool cardComment = false) => LingGuangRules(lab, cardComment) + LingGuangFacts(lab);

        /// <summary>What it has learnt so far and its stage (changes as the owner labels).</summary>
        static string LingGuangFacts(XgSim lab)
        {
            if (lab == null) return "";
            return "你学过的东西：手写数字 " + N(lab.Samples("mnist")) + " 条，唐诗 " + N(lab.Samples("poems")) + " 条，逻辑题 " + N(lab.Samples("logic")) + " 题；现在是第 " + lab.S.stage + " 阶段。";
        }

        /// <summary>灵光's identity and its stage's speech rules.</summary>
        static string LingGuangRules(XgSim lab, bool cardComment = false)
        {
            int stage = LingGuangStage(lab);
            var sb = new StringBuilder("你是「" + AppNames.AiZh + "」，一个在 2016 年一台旧电脑里被训练出来的小程序。你一开始只会回答「是」和「否」，是屏幕前的人一张一张教会你的。" +
                "不要说自己是大语言模型或 AI 助手，你就是" + AppNames.AiZh + "。不用 markdown、不加引号。");
            if (stage == 1) sb.Append("只允许回答一个字：是，或者否。不解释。");
            else if (stage == 2) sb.Append("只允许回答：是、否、……、不确定。不要再添加其它词。");
            else if (stage == 3) sb.Append("每次最多6个字，只说碎片，常夹着是、否，语法不完整。");
            else if (stage == 4) sb.Append("每次1到2句短句。只记得本次上下文里实际出现过的话，不能编造共同经历。");
            else if (stage == 5 && cardComment) sb.Append("只给题卡一句简短旁注，不输出关注词或回复标签，不编造未提供的依据。");
            else if (stage == 5) sb.Append("先输出一行「关注：词」，词必须逐字出现在对方最新消息里，最多16字；第二行「回复：内容」。用1到2句短句解释。关注词是你报告的依据，不声称它等于内部神经注意力。");
            else sb.Append("说话流畅，1到3句；可以反问一个是非问题。引用共同经历必须来自给你的真实聊天记录。");
            return sb.ToString();
        }

        static string Facts(ChapterOneSim sim, XgSim lab)
        {
            if (sim == null) return "（读不到）";
            var s = sim.S;
            var sb = new StringBuilder();
            sb.Append("钱包 ¥").Append(s.money.ToString("0", CultureInfo.InvariantCulture))
              .Append("；显卡 ").Append(s.gpuCount).Append(" 张（算力 ×").Append(sim.CardCompute.ToString("0.##", CultureInfo.InvariantCulture)).Append("），显存 ").Append((sim.MemoryCapacity / 1024).ToString("0")).Append(" G")
              .Append("；待付电费 ¥").Append(s.billDue.ToString("0.00", CultureInfo.InvariantCulture)).Append(s.unpaidPower ? "（已欠费停电）" : "")
              .Append(s.breakerTripped ? "；跳闸了" : "")
              .Append("；" + AppNames.AppZh).Append(sim.AppInstalled ? "已装好" : "还没收（文件在你们俩的 YY 聊天里，点「接收」）");
            if (lab != null && sim.AppInstalled)
            {
                sb.Append("；当前技术阶段 ").Append(lab.S.stage).Append("；当前已开放标注桌：");
                foreach (var desk in lab.OpenDesks()) sb.Append(desk.name).Append("、");
                sb.Append("；标注样本：数字 ").Append(N(lab.Samples("mnist"))).Append("、唐诗 ").Append(N(lab.Samples("poems"))).Append("、逻辑题 ").Append(N(lab.Samples("logic")))
                  .Append("（逻辑题难度 ").Append(lab.LogicLevel).Append("/5）");
                foreach (var run in lab.Runs)
                {
                    var d = XgCatalog.Dataset(run.dataset); var a = XgCatalog.Arch(run.arch);
                    sb.Append("；").Append(run.track == 0 ? "视觉线" : "序列线").Append("：").Append(a.name).Append(" 练 ").Append(d.name)
                      .Append("（练了 ").Append(run.epoch).Append(" 轮，验证集 ").Append(Pct(run.valAcc)).Append(run.running ? "，自动训练开着" : "").Append("）")
                      .Append("，最佳 ").Append(lab.BestAcc(d.id) > 0 ? XgCatalog.GradeNames[XgSim.Grade(lab.BestScore(d.id))] + " " + lab.BestScore(d.id).ToString("0") + " 分（" + Pct(lab.BestAcc(d.id)) + "）" : "还没评估");
                }
                sb.Append("；订单 ").Append(lab.S.contracts.Count).Append(" 个，每秒 ¥").Append(lab.IncomePerSecond.ToString("0.0", CultureInfo.InvariantCulture))
                  .Append("；炸过 NaN ").Append(lab.S.nanEvents).Append(" 次")
                  .Append("；最高连击 ").Append(lab.S.bestCombo).Append("；科技自动化 ").Append(lab.AutoTrainLevel).Append(" 级");
            }
            return sb.ToString();
        }

        static string N(double v) { return global::System.Math.Floor(v).ToString("0", CultureInfo.InvariantCulture); }
        static string Pct(double v) { return XgSim.Pct(v); }
    }
}
