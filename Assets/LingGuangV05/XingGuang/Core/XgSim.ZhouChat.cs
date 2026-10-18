namespace LingGuangV05.XingGuang
{
    /// <summary>One answer the player can send 老周 in YY, and what he says back.</summary>
    public sealed class XgZhouReply
    {
        public string zh, en, answerZh, answerEn;
        /// <summary>The conversation node that follows his answer (<see cref="XgSim.ZhouNode"/>); empty = none.</summary>
        public string next = "";
        /// <summary>What picking it does: <see cref="XgSim.ZhouDoubtUp"/> or <see cref="XgSim.ZhouTold"/>; empty = nothing.</summary>
        public string effect = "";
    }

    /// <summary>A step of a branching talk with 老周 (the confession): what he says, and the answers on offer.</summary>
    public sealed class XgZhouNode
    {
        public string id, zh, en;
        /// <summary>Applied when he says it.</summary>
        public string effect = "";
        public XgZhouReply[] replies;
    }

    public sealed partial class XgState
    {
        /// <summary>The protagonist told 老周 that something in the computer learns by itself, and he half believes it.</summary>
        public bool zhouKnows;
        /// <summary>How often 老周 has noticed that this progress does not look self-taught (he says so at the ending).</summary>
        public int zhouDoubt;
    }

    /// <summary>老周's message when a stage begins (YY), and the answers on offer.</summary>
    public sealed class XgZhouStageNote
    {
        public int stage;
        public string zh, en;
        public XgZhouReply[] replies;
    }

    /// <summary>
    /// 老周 in YY (2026-10-18): he guides the whole way. Every stage-one lesson (<see cref="TutorLessons"/>) arrives as
    /// his YY message with three answers: go and do it, ask why (he explains a little deeper), or say something in the
    /// protagonist's own voice (he banters and steers back). Each later stage opens with a note of his about what comes
    /// next. Anything the player types instead goes to his model with the step the to-do note is on, so he answers it
    /// and pulls the talk back to that step; offline, <see cref="ZhouSteer"/> does it plainly. He explains machine
    /// learning in general terms until the protagonist tells him (坦白, <see cref="ZhouNode"/>) that something in the
    /// computer learns by itself; he never learns where it came from or its name. Until then he grows suspicious (起疑).
    /// </summary>
    public sealed partial class XgSim
    {
        static XgZhouReply R(string zh, string en, string answerZh, string answerEn, string next = "", string effect = "")
            => new XgZhouReply { zh = zh, en = en, answerZh = answerZh, answerEn = answerEn, next = next, effect = effect };

        /// <summary>His first YY message, before the first lesson: the forum's private messages were too slow.</summary>
        public const string ZhouHelloZh = "贴吧私信太慢了，我加你 YY 了。以后有不懂的直接问。";
        public const string ZhouHelloEn = "Forum DMs are too slow, so I added you on YY. Ask me here from now on.";

        /// <summary>The three answers to a stage-one lesson, by lesson id. Empty for an unknown one.</summary>
        public static XgZhouReply[] ZhouReplies(string lesson)
        {
            switch (lesson)
            {
                case "data": return new[]
                {
                    R("懂了，我去挑个数据包", "Got it. I'll pick a dataset", "嗯。别贪大，先拿便宜的练手。", "Right. Don't go big, practise on a cheap one."),
                    R("模型吃什么长什么，啥意思？", "\"Becomes what it eats\"?", "你喂它一万条垃圾短信，它就认得垃圾短信长啥样；喂猫的照片，它就认猫。它不会凭空懂。", "Feed it ten thousand spam texts and it knows what spam looks like; feed it cat photos and it knows cats. It can't understand out of nothing."),
                    R("你也是被 AlphaGo 看动心的？", "AlphaGo got you too?", "我是被导师逼的。行了，去买数据。", "My supervisor got me. Now go buy the data."),
                };
                case "arch": return new[]
                {
                    R("那我换多层感知机", "I'll switch to an MLP", "换完记得重新练，别以为换了就会了。", "Train it again after. Switching alone doesn't teach it anything."),
                    R("为什么一条直线不够？", "Why isn't one line enough?", "纸上画四个点，左上右下一类，右上左下另一类，用一条直线分开试试？分不开吧。那就得拐弯。", "Draw four dots: top-left and bottom-right are one kind, top-right and bottom-left the other. Split them with one straight line. You can't. So it has to bend."),
                    R("1958 年的东西我还在用……", "I'm using something from 1958…", "别笑，现在的深度学习也是从它长出来的。换吧。", "Don't laugh, today's deep learning grew out of it. Now switch."),
                };
                case "capacity": return new[]
                {
                    R("先加一点试试", "I'll add a little first", "对，一点点加，盯着显存。", "Yes, a little at a time. Watch the VRAM."),
                    R("层数和宽度哪个重要？", "Layers or width?", "看题。要想好几步的题加层数，要同时看很多东西的题加宽度。拿不准就都加一点。", "Depends on the problem. Many steps of thinking: more layers. Many things at once: more width. Not sure? A bit of both."),
                    R("显存不够怎么办", "What if VRAM runs out?", "攒钱换卡呗。贴吧二手卡多，小心矿卡。", "Save up for a card. The forum has plenty of used ones. Watch out for mining cards."),
                };
                case "grade": return new[]
                {
                    R("行，练到 C 级", "Fine, to grade C", "嗯，看成绩，别光看它蒙对一两题。", "Right. Watch the grade, not one or two lucky answers."),
                    R("C 级是怎么算的？", "How is grade C worked out?", "拿它没练过的题考，答对的比例够线就是 C。练过的题答对不算数。", "Test it on questions it never trained on; enough right means C. Right answers on practice questions don't count."),
                    R("那之前不是白练了", "So all that training was wasted", "不白练。你学会了怎么练，这比参数值钱。", "Not wasted. You learned how to train; that's worth more than the weights."),
                };
                case "more": return new[]
                {
                    R("那我多弄点数据", "More data, then", "多，还得杂。全是一种题它照样死记。", "More, and varied. All one kind and it still just memorises."),
                    R("怎么知道它在死记？", "How do I know it's memorising?", "练过的题分数很高，一考新题就掉，那就是在背答案。", "High scores on practice questions, a drop on new ones: it's memorising."),
                    R("跟我高三一样", "Just like me in high school", "哈，那你最懂怎么治它。", "Ha. Then you know the cure better than anyone."),
                };
                case "label": return new[]
                {
                    R("那我边标边喂", "I'll feed it as I label", "对。标错了它也学错，手别抖。", "Yes. Label it wrong and it learns it wrong, so keep a steady hand."),
                    R("自己标的会不会不准？", "Won't my labels be off?", "会。所以认真点，你标错一张，它就学一张错的。", "They can be. So be careful: every card you get wrong, it learns wrong."),
                    R("手都标麻了", "My hand's gone numb", "我当年标图片标到半夜，眼睛都花了。歇会儿再标。", "I labelled images till midnight once and couldn't see straight. Rest a bit, then go on."),
                };
                case "plateau": return new[]
                {
                    R("我去看看卡在哪", "I'll find where it's stuck", "嗯，先查原因再动手。", "Right. Find the cause before you touch anything."),
                    R("怎么分辨脑子小还是书少？", "Small brain or too little data?", "练过的题都做不好，是脑子小；练过的会、新题不会，是书少。", "Bad even on practice questions: too small. Good on practice, bad on new ones: too little data."),
                    R("再硬练几轮会好吗？", "Will more rounds fix it?", "可能会，多半是浪费电。先找原因。", "Maybe. Mostly it wastes power. Find the cause first."),
                };
                case "exam": return new[]
                {
                    R("那我让它考一次", "I'll let it sit the exam", "去吧，考不过不丢人。", "Go on. Failing is no shame."),
                    R("为啥练过的题考是作弊？", "Why is testing on practice questions cheating?", "那些题它可能是背下来的。考没见过的才知道它会没会。", "It may have memorised those. Only unseen questions show whether it learned."),
                    R("考不过会怎样？", "What if it fails?", "不怎样。再练几轮重考就是。", "Nothing. Train a few more rounds and sit it again."),
                };
                case "retry": return new[]
                {
                    R("好，再练三轮", "OK, three more rounds", "嗯，练完再考。", "Right. Train, then test again."),
                    R("你那次组会后来呢？", "And your lab meeting?", "后来改了三版，老板说凑合。……扯远了，练你的。", "Three rewrites later my supervisor said \"passable\". …Anyway. Back to training."),
                    R("是不是它太笨了", "Is it just dumb?", "不是笨，是还没学够。再给它点时间。", "Not dumb. It hasn't learned enough yet. Give it time."),
                };
                case "pass": return new[]
                {
                    R("谢了老周", "Thanks, Lao Zhou", "谢啥，请我喝奶茶就行。", "Don't thank me. Buy me a bubble tea."),
                    R("接下来学什么？", "What's next?", "让它学更难的。一步一步来，别跳。", "Something harder. One step at a time, no skipping."),
                    R("它比你师弟强？", "Better than your first-year?", "这话可别让我师弟听见。", "Don't let him hear you say that."),
                };
                default: return new XgZhouReply[0];
            }
        }

        static XgZhouStageNote Note(int stage, string zh, string en, params XgZhouReply[] replies) => new XgZhouStageNote { stage = stage, zh = zh, en = en, replies = replies };

        /// <summary>His note when each stage from 2 begins: what the next idea is, in plain words.</summary>
        public static readonly XgZhouStageNote[] ZhouStageNotes =
        {
            Note(2, "过第一关了？下一步难了：一个点一个点地看不够，得把它们拼起来看。认数字就得看笔画怎么连。",
                "Through the first stage? The next is harder: looking at one dot at a time isn't enough; it has to put them together. To read digits it has to see how the strokes join.",
                R("怎么拼？", "Put them together how?", "卷积。拿个小窗口在图上滑，看见横就记横，看见竖就记竖，后面一层再把横竖拼成字。", "Convolution. Slide a small window over the image, note the horizontals and verticals, and let the next layer put them together into digits."),
                R("懂了，我去试试", "Got it, I'll try", "嗯，有问题再找我。", "Right. Ask if you get stuck."),
                R("你怎么知道它过了？", "How do you know it passed?", "你自己在贴吧嘚瑟的啊。", "You bragged about it on the forum yourself.")),
            Note(3, "现在它啥都会一点。接下来让它专门干好一件事，挑一个方向往深了练。",
                "Now it knows a bit of everything. Next, let it get really good at one thing: pick a direction and go deep.",
                R("挑哪个好？", "Which one?", "挑你数据最多的。数据在哪，本事就在哪。", "The one you have the most data for. The skill is where the data is."),
                R("啥都会不好吗？", "What's wrong with knowing everything?", "啥都会一点，就是啥都不精。找工作也一样。", "A bit of everything means good at nothing. Same with finding a job."),
                R("好", "OK", "去吧。", "Go on.")),
            Note(4, "要让它读句子，就得让它记住前面读过什么。普通网络看一个字忘一个字，循环网络（RNN）会把前面的带着走。",
                "To read a sentence it has to remember what came before. A plain network forgets each word as it goes; a recurrent network (RNN) carries the earlier words along.",
                R("句子太长它会忘吗？", "Will it forget long sentences?", "会。所以有人搞了 LSTM，加了几道门，该记的记，该忘的忘。", "It will. So people made the LSTM: a few gates that keep what matters and drop the rest."),
                R("听着像人", "Sounds human", "有点像。不过它比你记性好，至少不会忘交房租。", "A little. Its memory's better than yours though. It won't forget the rent."),
                R("懂了", "Got it", "嗯，慢慢练。", "Right. Take it slowly.")),
            Note(5, "长文章它记不全，那就教它挑重点。注意力（attention）就是让它读每个字时回头看看，哪几个字最要紧。",
                "It can't remember a long text whole, so teach it to pick out what matters. Attention means that at every word it looks back and asks which words matter most.",
                R("它怎么知道哪个字要紧？", "How does it know which words matter?", "它自己学。练多了它就知道，「不」字一出来，意思就反了。", "It learns. Train it enough and it knows that a \"not\" flips the meaning."),
                R("我看书也这样", "That's how I read too", "那你比大多数人强。", "Then you're ahead of most people."),
                R("好", "OK", "去吧，这一步有意思。", "Go on. This one's fun.")),
            Note(6, "到这一步，我也教不了你多少了。最后那步得你自己想：它是怎么学的，能不能换个学法。",
                "At this point there isn't much left I can teach you. The last step is yours to think through: how it learns, and whether it could learn another way.",
                R("你说过一步一步来", "You said one step at a time", "对，现在也是。只是这一步，只有你知道下一步在哪。", "Still true. Only now, you're the one who knows where the next step is."),
                R("谢谢你一路带我", "Thanks for getting me here", "谢啥，我就是个打字的。……加油。", "Don't. I just typed. …Good luck."),
                R("要是失败了呢", "And if it fails?", "失败就重来。你不是一直这么过来的吗。", "Then start again. Isn't that how you got this far?")),
        };

        public static XgZhouStageNote ZhouStageNote(int stage)
        {
            foreach (var n in ZhouStageNotes) if (n.stage == stage) return n;
            return null;
        }

        /// <summary>The <see cref="XgState.tutorSeen"/> key of a stage note, so each plays once per save.</summary>
        public static string ZhouStageKey(int stage) => "zhou.stage" + stage;

        /// <summary>One of the offered answers, by its exact text in either language; null for anything else.</summary>
        public static XgZhouReply ZhouMatch(XgZhouReply[] replies, string text)
        {
            if (replies == null || text == null) return null;
            text = text.Trim();
            foreach (var r in replies) if (r.zh == text || r.en == text) return r;
            return null;
        }

        /// <summary>
        /// 老周 without his model: whatever the player said, he brings it back to the step the to-do note is on, in a
        /// sentence (the step's why when it has one).
        /// </summary>
        /// <summary>The same once he knows: it is about the program, not about the protagonist.</summary>
        public string ZhouSteerNow(XgGuideStep step, bool english)
        {
            if (!S.zhouKnows || step == null || string.IsNullOrEmpty(ZhouWhat(step, english))) return ZhouSteer(step, english);
            string why = ZhouWhy(step, english);
            return english
                ? "Your thing (" + ZhouLabLine(true) + ") is stuck on this: " + ZhouWhat(step, true) + (why.Length > 0 ? ". " + why : ".")
                : "你那个东西（" + ZhouLabLine(false) + "）现在卡在这步：" + ZhouWhat(step, false) + (why.Length > 0 ? "，" + why : "。");
        }

        public static string ZhouSteer(XgGuideStep step, bool english)
        {
            if (step == null) return english ? "Keep at it. Ask me if something's unclear." : "先练着，有不懂的再问我。";
            string what = ZhouWhat(step, english);
            string why = ZhouWhy(step, english);
            if (string.IsNullOrEmpty(what)) return english ? "Keep at it. Ask me if something's unclear." : "先练着，有不懂的再问我。";
            return english
                ? "Ha, later. First: " + what + (string.IsNullOrEmpty(why) ? "." : ". " + why)
                : "这个回头再说。眼下先" + what + (string.IsNullOrEmpty(why) ? "。" : "，" + why);
        }

        /// <summary>The step in his words: without the note's 「下一步：」 and the price after 「 · 」.</summary>
        static string ZhouWhat(XgGuideStep step, bool english)
        {
            string what = (english ? step.en : step.zh) ?? "";
            foreach (var p in new[] { "下一步：", "下一步:", "Next: ", "Next step: " }) if (what.StartsWith(p)) what = what.Substring(p.Length);
            int dot = what.IndexOf(" · ");
            if (dot > 0) what = what.Substring(0, dot);
            return what.Trim().TrimEnd('。', '.');
        }

        /// <summary>The first sentence of the step's why (its hover text goes on with details he would not say).</summary>
        static string ZhouWhy(XgGuideStep step, bool english)
        {
            string why = (english ? step.whyEn : step.whyZh) ?? "";
            int nl = why.IndexOf('\n');
            if (nl >= 0) why = why.Substring(0, nl);
            int end = english ? why.IndexOf(". ") : why.IndexOf('。');
            if (end > 0) why = why.Substring(0, end + 1);
            return why.Trim();
        }

        /// <summary>For his model: the step he should pull the talk back to.</summary>
        public static string ZhouGuideState(XgGuideStep step, bool english)
        {
            if (step == null) return english ? "\nHe is between steps right now; encourage him to keep training." : "\n他眼下没有卡在哪一步，鼓励他接着练。";
            string what = ZhouWhat(step, english);
            string why = ZhouWhy(step, english);
            return english
                ? "\nWhat he should do now: " + what + (string.IsNullOrEmpty(why) ? "" : " (why: " + why + ")")
                : "\n他现在该做的一步：" + what + (string.IsNullOrEmpty(why) ? "" : "（为什么：" + why + "）");
        }

        // ───────────── 起疑 and 坦白 (2026-10-18) ─────────────

        public const string ZhouDoubtUp = "doubt", ZhouTold = "told";

        /// <summary>
        /// His stage note as it plays now. Until he is told, stages 2–5 each add a line where he notices the pace (and it
        /// counts as doubt); from stage 3 one answer is the confession (<see cref="ZhouNode"/> 「confess1」), offered again
        /// at stage 4 if it was not taken; at stage 6 he asks it himself. Once he knows, each note ends with what the
        /// program is doing right now (<see cref="ZhouLabLine"/>) so his advice is about it.
        /// </summary>
        /// <summary>Stage 2 when it chose to read first (阿杰's A/B question picked B): the next idea is a sentence, not strokes.</summary>
        public static readonly XgZhouStageNote ZhouReadingNote = Note(2, "过第一关了？下一步难了：一个字一个字地看不够，得记住前面读过什么。「不难看」和「难看」就差一个字，意思反过来。",
            "Through the first stage? The next is harder: one word at a time isn't enough; it has to remember what came before. \"Not bad\" and \"bad\" differ by one word and mean the opposite.",
            R("怎么记？", "Remember how?", "循环网络。读一个字，把心里的那点印象带到下一个字，一路带到句尾。", "A recurrent network. Read a word, carry what it made of it on to the next word, all the way to the end."),
            R("懂了，我去试试", "Got it, I'll try", "嗯，有问题再找我。", "Right. Ask if you get stuck."),
            R("你怎么知道它过了？", "How do you know it passed?", "你自己在贴吧嘚瑟的啊。", "You bragged about it on the forum yourself."));

        public XgZhouStageNote ZhouStageNoteNow(int stage)
        {
            // Stage 2 follows the road it chose: strokes for 看图, the words before for 读弹幕.
            bool reading = stage == 2 && FirstTrack == "sequence";
            var b = reading ? ZhouReadingNote : ZhouStageNote(stage);
            if (b == null) return null;
            if (S.zhouKnows)
                return new XgZhouStageNote { stage = stage, zh = b.zh + "你那个东西现在：" + ZhouLabLine(false) + "。", en = b.en + " Your thing right now: " + ZhouLabLine(true) + ".", replies = b.replies };
            switch (stage)
            {
                case 2: return Note(2, b.zh + "……你这才几天，就到这一步了？我当年看懂" + (reading ? "循环网络" : "卷积") + "花了一个学期。", b.en + " …You got here in a few days? " + (reading ? "Recurrent networks" : "Convolution") + " took me a whole term.", b.replies);
                case 3: return Note(3, b.zh + "……说真的，你这进度不像自学的。", b.en + " …Honestly, this doesn't look self-taught.",
                    b.replies[0],
                    R("就是自学的", "It's all self-taught", "行吧，你说是就是。", "Fine. If you say so.", "", ZhouDoubtUp),
                    R("其实……电脑里有个东西在自己学", "Actually… something on my computer is learning by itself", "啥？", "What?", "confess1"));
                case 4: return Note(4, b.zh + "你那个模型连 RNN 都上了？我们师弟还在调 CNN。", b.en + " Your model's on RNNs already? Our first-year is still tuning a CNN.",
                    b.replies[0],
                    R("我查了很多资料", "I read up a lot", "查资料能查这么快？……算了，不问了。", "Reading gets you this fast? …Never mind, I won't ask.", "", ZhouDoubtUp),
                    R("老实说，不是我一个人在弄", "Honestly, it's not just me doing this", "嗯？", "Hm?", "confess1"));
                case 5: return Note(5, b.zh + "……我不问了。你不想说，就不说。", b.en + " …I'll stop asking. If you don't want to say, don't.", b.replies);
                case 6: return Note(6, b.zh + "……我一直没问你。你电脑里那个，是不是早就不是你在教它了？", b.en + " …I never asked. That thing on your computer: you stopped being the one teaching it a while ago, didn't you?",
                    R("是。它一直在自己学", "Yes. It's been learning on its own", "……我就知道。最后那步，你陪它走完。", "…I knew it. Walk the last step with it.", "", ZhouTold),
                    R("我也说不清", "I can't really explain", "那就别说了，走完再说。", "Then don't. Finish first.", "", ZhouDoubtUp),
                    R("你想多了", "You're overthinking it", "可能吧。……加油。", "Maybe. …Good luck.", "", ZhouDoubtUp));
                default: return b;
            }
        }

        /// <summary>A stage note was shown: until he is told, every note from stage 2 to 5 is one more thing he noticed.</summary>
        public void ZhouStageShown(int stage)
        {
            if (!S.zhouKnows && stage >= 2 && stage <= 5) S.zhouDoubt++;
        }

        /// <summary>The confession, step by step. Null for an unknown id.</summary>
        public static XgZhouNode ZhouNode(string id)
        {
            switch (id)
            {
                case "confess1": return new XgZhouNode
                {
                    id = id, zh = "啥意思？你电脑里有啥？", en = "Meaning what? What's on your computer?",
                    replies = new[]
                    {
                        R("它没人教，自己在长", "Nobody teaches it. It grows by itself", "", "", "confess2"),
                        R("你来看一眼就知道", "You'd see if you looked", "", "", "confess2"),
                        R("算了，当我没说", "Forget it, I said nothing", "……行。哪天想说了再说。", "…OK. Tell me when you want to.", "", ZhouDoubtUp),
                    },
                };
                case "confess2": return new XgZhouNode
                {
                    id = id, zh = "别逗了，程序哪会自己学。是不是中病毒了？", en = "Come on. Programs don't learn by themselves. Got a virus?",
                    replies = new[]
                    {
                        R("它会回我的话", "It talks back to me", "", "", "confess3"),
                        R("它考过了我没教过的题", "It passed questions I never taught it", "", "", "confess3"),
                        R("我也不信，可它就是在学", "I didn't believe it either, but it is learning", "", "", "confess3"),
                    },
                };
                case "confess3": return new XgZhouNode
                {
                    id = id, zh = "……你截个图我看看。", en = "…Send me a screenshot.",
                    replies = new[]
                    {
                        R("（发去一张截图）", "(Send a screenshot)", "……这不是你写的。行，我信一半。以后它卡在哪你直接跟我说，我按它的情况给你出主意。",
                            "…You didn't write this. Fine, I half believe you. From now on tell me where it's stuck and I'll go by what it's actually doing.", "", ZhouTold),
                        R("还是算了", "Never mind", "……行。哪天想说再说。", "…OK. Tell me when you want to.", "", ZhouDoubtUp),
                        R("你别告诉别人", "Don't tell anyone", "我能告诉谁？贴吧那帮人？……截图呢？", "Who would I tell? The forum lot? …Where's the screenshot?", "confess3"),
                    },
                };
                default: return null;
            }
        }

        /// <summary>What an answer does to him. True when this answer made him believe it (the first time).</summary>
        public bool ZhouApply(string effect)
        {
            if (effect == ZhouDoubtUp) { if (!S.zhouKnows) S.zhouDoubt++; return false; }
            if (effect == ZhouTold && !S.zhouKnows) { S.zhouKnows = true; return true; }
            return false;
        }

        /// <summary>The program as 老周 sees it once told: its structure, how it does on new cards, what it can do, in a phrase.</summary>
        public string ZhouLabLine(bool english)
        {
            XgRun run = S.vision;
            if (S.sequence != null && (run == null || S.sequence.epoch > run.epoch)) run = S.sequence;
            var a = run != null ? XgCatalog.Arch(run.arch) : null;
            string arch = a == null ? (english ? "no structure yet" : "还没定结构") : english ? a.nameEn : a.name;
            string acc = run == null || run.epoch == 0 ? (english ? "untrained" : "还没练") : (english ? "new cards " : "新题 ") + Pct(run.valAcc);
            return english
                ? arch + ", " + acc + ", " + AbilitiesCount + " abilities learned"
                : arch + "，" + acc + "，学会了 " + AbilitiesCount + " 样";
        }

        /// <summary>His last word when the chapter ends: to it, if he knows; the suspicion closed, if he only guessed.</summary>
        public string ZhouEndingLine(bool english)
        {
            if (S.zhouKnows) return english ? "It made it to the end? …Tell it from me: well done." : "它走到最后了？……替我跟它说一声，辛苦了。";
            if (S.zhouDoubt > 0) return english ? "I guessed long ago. You never said, so I'll act like I don't know. …Impressive, though." : "我早猜到了。你不说，我就当不知道。……不过，挺厉害的。";
            return english ? "Heard you finished? You owe me dinner." : "听说你弄完了？请我吃饭。";
        }

        public const string ZhouEndingKey = "zhou.ending";
    }
}
