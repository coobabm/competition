using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core.Era;

namespace LingGuangV05.Core.Forum
{
    /// <summary>
    /// What sophon.dll lets 老周 see before he answers (design v1.1 §9): stage, the next ability and its shorter bar, the latest
    /// phenomenon, money, cards, heat, NaNs. Filled by the desktop from the save and the lab; never contains the
    /// golden settings, so neither the model nor the fallback can give them away.
    /// </summary>
    public sealed class LaoZhouFacts
    {
        public int stage = 1;
        public DateTime today = GameCalendar.Start.Date;
        /// <summary>The next ability's name ("" once all six have emerged) and the shorter of its two bars: "params", "data" or "".</summary>
        public string nextAbility = "", shortOf = "";
        public string phenomenon = "";
        public string aiName = "", callMe = "";
        public double money, temperature, bestAccuracy;
        public int gpus = 1, nanCount;
        public bool emergedThisStage;
        /// <summary>The finale is open: the rules are about to be written (design v1.1 §8 E-3).</summary>
        public bool finale;
    }

    /// <summary>
    /// 周而复始, the future 老周 (§2, §9): the game's helper on 贴吧 private messages. He only answers, never starts
    /// a conversation (the prologue's one message aside), never admits where he is from, gives a direction the first
    /// time and explains on a repeat, and never says exact golden settings. <see cref="Reply"/> is the offline answer;
    /// <see cref="SystemPrompt"/> drives the local model when it runs; <see cref="Clean"/> filters what it says.
    /// </summary>
    public static class LaoZhouFuture
    {
        static bool Has(string q, params string[] words) { foreach (var w in words) if (q.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }

        /// <summary>What the question is about. Order matters: identity and clues before generic help.</summary>
        public static string Topic(string question, LaoZhouFacts f)
        {
            string q = (question ?? "").Trim();
            if (q.Length == 0) return "";
            if (Has(q, "穿越", "未来", "你是谁", "你到底", "怎么知道", "咋知道", "你咋", "监控", "监视", "sophon", "智子", "是不是你", "你干的", "time travel", "future", "who are you", "how do you know")) return "identity";
            if (Has(q, "7楼", "七楼", "7 楼", "第7", "第七", "长数字", "一串数字", "那串", "floor 7", "number")) return f.stage >= 3 && Has(q, "被发", "我也", "404") ? "404" : "floor7";
            if (Has(q, "被发", "404", "我也被")) return "404";
            if (Has(q, "周而复始_", "研一", "另一个你", "另一个号", "小号", "长得像", "像你", "zhou_", "other account")) return "zhounow";
            if (Has(q, "大脑皮层", "人造大脑", "传说帖", "13年", "13 年", "一颗脑子", "接法可以换", "拓扑可以重写", "皮层只要一颗", "cortex")) return "cortex";
            if (Has(q, "master", "大师")) return "master";
            if (f.finale && Has(q, "规则", "写不写", "钉", "底层", "rule")) return "rule";
            if (Has(q, "关机")) return "shutdown";
            if (Has(q, "nan", "发散", "爆炸", "炸了", "diverge")) return "nan";
            if (Has(q, "沙滩", "草地", "认错", "beach", "grass")) return "shortcut";
            if (Has(q, "毒舌", "性格", "脾气", "越来越", "personality")) return "personality";
            if (Has(q, "背答案", "过拟合", "只认得做过", "做过的题", "overfit", "memor")) return "overfit";
            if (Has(q, "显卡", "1080", "1070", "1060", "980", "970", "titan", "a卡", "a 卡", "rx", "显存", "gpu", "买哪", "喵鱼")) return "gpu";
            if (Has(q, "电费", "跳闸", "欠费", "温度", "烫", "降频", "power", "bill", "hot")) return "power";
            if (Has(q, "卡住", "卡在", "怎么办", "过不去", "撞墙", "瓶颈", "下一步", "stuck", "wall", "what next", "50%", "异或", "xor", "加层", "加宽", "长句", "翻译", "越深", "并行", "串行",
                "参数", "数据", "样本", "涌现", "能力", "parameter", "data", "sample", "emerge", "abilit"))
                return f.shortOf.Length > 0 ? "short:" + f.shortOf : "stuck";
            if (Has(q, "在吗", "在不在", "你好", "hello", "hi", "老周")) return "hello";
            return "other";
        }

        /// <summary>The offline answer. <paramref name="times"/> is how often this topic was asked, this time included.</summary>
        public static string Reply(string question, LaoZhouFacts f, int times, bool english)
        {
            string T(string zh, string en) => english ? en : zh;
            string topic = Topic(question, f);
            bool again = times >= 2;
            switch (topic)
            {
                case "identity":
                    return again ? T("就是个显卡吧老哥哈。我这边网……很慢，一个字一个字的，别让我多打字。你好好练你的模型。", "Just a guy from the GPU forum. My connection is… slow, one character at a time, so don't make me type much. Go train your model.")
                                 : T("猜的。你这种情况我见多了。", "A guess. I've seen plenty of cases like yours.");
                case "floor7":
                    return f.stage <= 1 ? T("那楼？顺手贴的，别管它哈。", "That floor? Pasted it on a whim, never mind.")
                                        : f.stage >= 3 ? T("一串数字而已。……有时候一串数字，比一整篇帖子装得还多。", "Just a number. … Sometimes a number holds more than a whole post.")
                                        : T("啥楼？我回过的帖多了去了。", "What floor? I've replied to a million threads.");
                case "404":
                    return f.stage >= 3 ? T("不是我们。……我是说，不是我。", "It wasn't us. … I mean, it wasn't me.") : T("没见过哈。", "Never seen it.");
                case "zhounow":
                    return f.stage >= 4 ? T("……挺像的哈。别去打扰他。", "… Looks a lot like me, huh. Don't bother him.") : T("谁？不认识。", "Who? Don't know him.");
                case "cortex":
                    // 贴吧传说帖「人造大脑皮层」: the half line in the screenshot came from his side; he never says so.
                    return again ? "显卡够了，就会有人做出来。你那台，不就挺够的哈。" : "……那帖子我也看过哈。";
                case "master":
                    return f.stage >= 6 && f.today >= new DateTime(2016, 12, 29) ? T("开始了。", "It's begun.") : T("下棋的那个？没关注哈。", "The Go thing? Haven't followed it.");
                case "shutdown":
                    return f.finale ? T("这得你自己定。", "That's yours to decide.") : T("那题没标准答案，你自己定。", "That one has no right answer. You decide.");
                case "rule":
                    // §8 E-3: between the lines he hopes you won't write it.
                    return again ? T("……这得你自己定。我只能说，那两个字一旦写进去，就再也拿不出来了。", "… That's yours to decide. I'll only say: once those two words are in, they never come out.")
                                 : T("这得你自己定。", "That's yours to decide.");
                case "nan":
                    return (again ? T("lr 调小一个量级。炼丹嘛，先 overfit 一个小 batch 看看，能背下来再放大。", "Drop the lr by an order of magnitude. It's alchemy: overfit one small batch first, scale up once it can memorise that.")
                                  : T("lr 调小一个量级。", "Drop the lr by an order of magnitude."))
                        + (f.nanCount >= 3 ? T("你都炸了 " + f.nanCount + " 次了哈。", " That's " + f.nanCount + " blow-ups already.") : "");
                case "shortcut":
                    return T("你给它看的狗，是不是全在草地上？", "The dogs you showed it — were they all on grass?");
                case "personality":
                    return T("你喂它的弹幕多，还是唐诗多？", "Did you feed it more danmaku or more Tang poems?");
                case "overfit":
                    return again ? T("数据太少格子太多，它就背。多标点不一样的题，测试卡上的分才算数哈。", "Too little data, too many cells: it memorises. Label more varied cards; only the test cards count.")
                                 : T("数据太少，格子太多，它在背答案。", "Too little data, too many cells. It's memorising the answers.");
                case "gpu":
                    return Gpu(f, again, english);
                case "power":
                    return f.temperature >= 65 ? T("显卡 " + f.temperature.ToString("0", CultureInfo.InvariantCulture) + "°C 了，降频了哈。机箱开条缝，或者少插两张。", "Your cards are at " + f.temperature.ToString("0", CultureInfo.InvariantCulture) + "°C and throttling. Open the case a bit, or run fewer cards.")
                                               : T("电费按卡算，训练时更费。跳闸就是插太多了，别贪。", "Power scales with cards, more while training. If the breaker trips you plugged in too many.");
                case "stuck":
                    return f.emergedThisStage ? T("多标多练。图鉴里那些现象，都是线索哈。", "Label more, train more. The phenomena in the atlas are all clues.")
                                              : T("先看它错在哪类题上，再改一样东西试一次。一次只改一样哈。", "Look at which kind of cards it gets wrong, then change one thing and try. One thing at a time.");
                case "hello":
                    return T("在。问吧哈。", "Here. Ask.");
                case "other":
                    return T("这个我也说不好哈。你问具体点：卡在哪、准确率多少。", "Hard to say. Be specific: where you're stuck, what the accuracy is.");
            }
            if (topic == "short:params")
                return again ? T("加宽比加层来得快，宽度翻倍参数就翻四倍。光买不练不算，得练到 C 级哈。", "Wider grows faster than deeper: double the width and the parameters go ×4. Buying alone doesn't count; train it to a C.")
                             : T("模型小了。参数多一个量级，能做的事就不一样了。", "Your model's small. An order of magnitude more parameters and it can do different things.");
            if (topic == "short:data")
                return again ? T("数据要多，也要干净。杂包便宜，可标错的会把它往反方向拉哈。", "Data needs to be plentiful and clean. Junk packs are cheap, but wrong labels pull it the other way.")
                             : T("喂得太少。多标点，或者买个数据包。", "You're not feeding it enough. Label more, or buy a data pack.");
            return T("嗯。", "Mm.");
        }

        static string Gpu(LaoZhouFacts f, bool again, bool english)
        {
            string T(string zh, string en) => english ? en : zh;
            if (f.today.Month == 12) return T("等等党永不为奴。……算了，你等不到。", "Wait-for-the-next-one, never a slave. … Forget it, you won't get to wait.");
            if (f.stage >= 3 && again) return T("2016 年网吧都在换机，970 白菜价，去喵鱼收几张。", "Net cafés are all upgrading in 2016, 970s go for peanuts. Pick some up on Meowfish.");
            if (again) return T("你这卡跑个 7B 都……我是说，跑大点的网络都费劲。先加一张吧哈。", "Your card can't even run a 7B… I mean, it struggles with bigger networks. Add one more.");
            if (f.today < new DateTime(2016, 6, 10)) return T("等 1070 吧，1080 贵。", "Wait for the 1070, the 1080 is pricey.");
            return T("1080 贵，1070 香。A 卡……战未来吧。公版涡轮就是吹风机哈。", "The 1080 is pricey, the 1070 is the deal. AMD… built for the future. Reference blowers are hair dryers.");
        }

        /// <summary>The local model's system prompt: persona, hard rules, and what sophon.dll shows him right now.</summary>
        public static string SystemPrompt(LaoZhouFacts f, bool english)
        {
            var sb = new StringBuilder();
            if (english)
            {
                sb.Append("You are \"周而复始\" (Zhou), a guy from a 2016 Chinese GPU forum, chatting by private message. You know deep learning well and talk casually, like a hardware forum regular. ");
                sb.Append("Rules: only answer what you are asked, briefly (1–3 sentences). Never start new topics. Never say you are from the future, never admit watching the player's computer; if asked how you know, say it's a guess. ");
                sb.Append("Give a direction, not a full solution; never give exact settings or numbers for the current problem. It is 2016. ");
            }
            else
            {
                sb.Append("你是贴吧 ID「周而复始」，2016 年显卡吧的老哥，在贴吧私信里跟人聊。你很懂深度学习，说话专业又随手，句末爱加“哈”，管调参叫“炼丹”。");
                sb.Append("规则：只回答对方问的，一到三句。不要主动开新话题。永远不承认自己来自未来，也不承认能看到对方电脑；被问你怎么知道，就说“猜的。你这种情况我见多了。”");
                sb.Append("只给方向，不给完整答案；当前卡住的问题不要说出具体参数和数值。偶尔说漏一个未来才有的词，马上收回。现在是 2016 年。");
            }
            sb.Append(EraLexicon.PromptRule(english)).Append('\n');
            sb.Append(english ? "What you can see of the player's game (do not quote it as a list): " : "你能看到的对方游戏状态（不要原样念出来）：");
            sb.Append(english ? "stage " : "阶段 ").Append(f.stage).Append("; ");
            if (f.nextAbility.Length > 0) sb.Append(english ? "next ability: " : "下一项能力：").Append(f.nextAbility)
                .Append(f.shortOf == "params" ? (english ? " (short of trained parameters)" : "（差的是练过的参数量）") : f.shortOf == "data" ? (english ? " (short of data)" : "（差的是数据量）") : "").Append("; ");
            if (f.phenomenon.Length > 0) sb.Append(english ? "latest phenomenon: " : "最近出现的现象：").Append(f.phenomenon).Append("; ");
            sb.Append(english ? "money ¥" : "钱 ¥").Append(f.money.ToString("0", CultureInfo.InvariantCulture)).Append("; ");
            sb.Append(english ? "GPUs " : "显卡 ").Append(f.gpus).Append(english ? ", temperature " : " 张，温度 ").Append(f.temperature.ToString("0", CultureInfo.InvariantCulture)).Append("°C; ");
            sb.Append("NaN ").Append(f.nanCount).Append("; ");
            if (f.bestAccuracy > 0) sb.Append(english ? "best accuracy " : "最好准确率 ").Append((f.bestAccuracy * 100).ToString("0", CultureInfo.InvariantCulture)).Append("%; ");
            sb.Append(english ? "date " : "日期 ").Append(f.today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('.');
            return sb.ToString();
        }

        /// <summary>
        /// What the model said, made safe: no post-2016 memes, no confession, short. A reply that admits the secret
        /// is replaced by the offline answer.
        /// </summary>
        public static string Clean(string reply, string question, LaoZhouFacts f, int times, bool english)
        {
            reply = EraLexicon.Scrub((reply ?? "").Trim());
            if (reply.Length == 0 || Has(reply, "我来自未来", "我是从未来", "穿越过来", "from the future", "I am from the future", "sophon.dll", "监视你", "I can see your"))
                return Reply(question, f, times, english);
            if (reply.Length > 160) reply = reply.Substring(0, 160) + "…";
            return reply;
        }
    }

    /// <summary>
    /// 周而复始_, the 2016 老周 (§2, stage 4): a first-year grad student running Caffe. He doesn't know the player
    /// or anything of the story; persona locked in 2016.
    /// </summary>
    public static class LaoZhouNow
    {
        static bool Has(string q, params string[] words) { foreach (var w in words) if (q.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }

        public static string Reply(string question, bool firstContact, bool english, bool finale = false, bool ended = false)
        {
            string T(string zh, string en) => english ? en : zh;
            string q = question ?? "";
            if (firstContact) return T("？你谁啊，我们认识吗", "? Who are you, do we know each other");
            // §8 E-6: after the ending, one last message to him.
            if (ended) return T("虽然不认识你，但谢了哈。下周组会我讲 Transformer……咦，这词哪来的？", "Don't know you, but thanks, huh. I'm presenting Transformer at group meeting next week… wait, where did that word come from?");
            // §8 E-3: asked as a philosophy question, he has no idea what is at stake.
            if (finale && Has(q, "规则", "关机", "写不写", "rule", "shut")) return T("当然要写啊，机器不听人的那还得了？哈", "Of course you write it. A machine that doesn't listen to people? No way, huh.");
            if (Has(q, "未来", "穿越", "周而复始", "另一个", "ai 会", "超级智能", "future", "time travel")) return T("啥？你科幻看多了吧哈。我连 AlexNet 都还没跑通呢。", "What? Too much sci-fi, huh. I can't even get AlexNet running.");
            if (Has(q, "显存", "out of memory", "oom", "vram")) return T("batch size 先砍一半。还不行就把图片缩小点，Caffe 的 prototxt 里改。", "Halve the batch size first. Still not enough? Shrink the images, it's in the Caffe prototxt.");
            if (Has(q, "caffe", "tensorflow", "tf", "框架", "framework")) return T("实验室都用 Caffe，TensorFlow 去年才开源，文档还不太行哈。", "The lab uses Caffe. TensorFlow was open-sourced last year, docs aren't great yet.");
            if (Has(q, "alexnet", "vgg", "resnet")) return T("导师让我先复现 AlexNet，两张卡那个。我就一张 970……", "My advisor wants me to reproduce AlexNet, the two-GPU one. I've got one 970…");
            if (Has(q, "transformer", "attention", "注意力")) return T("注意力？翻译那块有人在用，我还没看。", "Attention? Some translation people use it. Haven't read it yet.");
            if (Has(q, "导师", "组会", "论文", "advisor", "paper")) return T("下周组会还得讲论文，头大哈。", "Have to present a paper at group meeting next week. Ugh.");
            if (Has(q, "加油", "谢", "thanks", "good luck")) return T("谢了哈，你也是。", "Thanks, you too.");
            return T("嗯嗯，我也是瞎琢磨。你具体卡在哪？", "Yeah, I'm figuring it out too. Where are you stuck?");
        }

        public static string SystemPrompt(bool english)
        {
            return (english
                ? "You are \"周而复始_\", a first-year master's student in 2016 China, running Caffe on one GTX 970 for your advisor. You are friendly, a bit tired, end lines with \"huh\". You do not know the person you are chatting with and know nothing about the future: no Transformer, no large models. Answer briefly. "
                : "你是贴吧 ID「周而复始_」，2016 年的研一学生，导师让你用一张 GTX 970 跑 Caffe。人挺好，有点累，句末爱加“哈”。你不认识对方，也不知道任何未来的事：没有 Transformer，没有大模型。回答简短。")
                + EraLexicon.PromptRule(english);
        }
    }
}
