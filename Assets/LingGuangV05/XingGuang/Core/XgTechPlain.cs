using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// 科技页「说人话」: one plain, funny closing line per tech node, shown after the note in the tooltip
    /// (XgTechTreePage.FillTooltip). Presentation copy only: it never touches catalog data, prices or player state.
    /// Authored per node id; depth / width ladders and dataset packs are templated per level / per dataset, with a
    /// generic fallback so a node added later still gets a line. The game is set in 2016: nothing here may use later
    /// slang (checked by XgTechPlainTests against EraLexicon).
    /// </summary>
    public static class XgTechPlain
    {
        /// <summary>The prefix shown before the line.</summary>
        public static string Prefix(bool english) => english ? "In plain words: " : "说人话：";

        /// <summary>The line in the chosen language, or "" when the node is an atlas node (not on the 科技 page).</summary>
        public static string Line(XgNode node, bool english) => TryGet(node, out string zh, out string en) ? (english ? en : zh) : "";

        /// <summary>True when this exact node has hand-written copy (not the generic per-kind fallback).</summary>
        public static bool IsAuthored(XgNode node)
        {
            if (node == null || node.tree == "atlas") return false;
            if (node.kind == XgNodeKind.Depth) return DepthLines.ContainsKey(node.value);
            if (node.kind == XgNodeKind.Width) return WidthLines.ContainsKey(WidthOf(node));
            if (node.kind == XgNodeKind.Dataset) return node.target != null && PackLines.ContainsKey(node.target);
            return node.id != null && Lines.ContainsKey(node.id);
        }

        /// <summary>The plain line for a 科技 node. False only for atlas nodes (图鉴), which the page does not sell.</summary>
        public static bool TryGet(XgNode node, out string zh, out string en)
        {
            zh = en = "";
            if (node == null || node.tree == "atlas" || node.kind == XgNodeKind.Phenomenon || node.kind == XgNodeKind.Ability) return false;
            string[] hit;
            switch (node.kind)
            {
                case XgNodeKind.Depth:
                    if (!DepthLines.TryGetValue(node.value, out hit))
                        hit = new[] { "再叠到 " + node.value + " 层，楼越高越吃显存。", "Stack up to " + node.value + " layers: the taller, the hungrier for VRAM." };
                    break;
                case XgNodeKind.Width:
                    if (!WidthLines.TryGetValue(WidthOf(node), out hit))
                        hit = new[] { "每层加宽到 " + WidthOf(node) + "，人多好办事，就是更吃显存。", "Widen each layer to " + WidthOf(node) + ": more hands, more VRAM." };
                    break;
                case XgNodeKind.Dataset:
                    if (node.target == null || !PackLines.TryGetValue(node.target, out hit))
                        hit = new[] { "新的一包习题到手，开饭。", "A fresh pack of practice questions: dinner is served." };
                    break;
                default:
                    if (node.id == null || !Lines.TryGetValue(node.id, out hit))
                        hit = node.kind == XgNodeKind.Arch
                            ? new[] { "换了套新脑子，先试试顺不顺手。", "A new kind of brain: see how it feels first." }
                            : new[] { "又一个小升级，积少成多。", "One more small upgrade; it all adds up." };
                    break;
            }
            zh = hit[0]; en = hit[1];
            return true;
        }

        static int WidthOf(XgNode node) => node.value >= 0 && node.value < XgCatalog.Widths.Length ? XgCatalog.Widths[node.value] : node.value;

        static readonly Dictionary<int, string[]> DepthLines = new Dictionary<int, string[]>
        {
            { 2, new[] { "再加一层，平房盖成二层小楼。", "One more layer: the bungalow gets a second floor." } },
            { 3, new[] { "三层了，线索能拐三道弯再下结论。", "Three layers: clues can take three turns before a verdict." } },
            { 4, new[] { "四层，像传话传了四手，没跑偏就算赢。", "Four layers is like a message passed through four people. Not garbled yet? You win." } },
            { 5, new[] { "五层楼，没电梯，传话全靠爬。", "Five floors, no lift: every message has to climb." } },
            { 6, new[] { "六层，再高点家里的显卡就喘了。", "Six layers: any higher and your graphics card starts panting." } },
            { 8, new[] { "八层，终于像个像样的高楼了。", "Eight layers: now it's a proper tower block." } },
            { 12, new[] { "十二层，传话传到嗓子哑，得开近道。", "Twelve layers: the message goes hoarse on the way up unless there are shortcuts." } },
            { 16, new[] { "十六层，显存：我先跪了。", "Sixteen layers: the VRAM taps out first." } },
            { 32, new[] { "三十二层摩天大楼，电费单看一眼想哭。", "Thirty-two layers, a skyscraper: one look at the power bill and you tear up." } },
        };

        static readonly Dictionary<int, string[]> WidthLines = new Dictionary<int, string[]>
        {
            { 16, new[] { "每层才十几个人，开会都坐不满一桌。", "Barely a dozen per layer, not even a full table." } },
            { 32, new[] { "每层三十来个，一个班的规模。", "About thirty per layer: one class." } },
            { 64, new[] { "每层六十多个，两个班一起动脑筋。", "Sixty-odd per layer: two classes thinking together." } },
            { 128, new[] { "一百多个同时上岗，算是开大会了。", "A hundred-plus on duty at once, it's a proper assembly." } },
            { 256, new[] { "两百多个，吵是吵了点，线索也多。", "Two hundred-plus: noisier, but more clues." } },
            { 512, new[] { "五百多个一起上，像春运火车站。", "Five hundred at once, like the station at Spring Festival rush." } },
            { 1024, new[] { "上千个同时干活，显存当场加班。", "A thousand at work at once, and the VRAM is on overtime." } },
        };

        static readonly Dictionary<string, string[]> PackLines = new Dictionary<string, string[]>
        {
            { "spam", new[] { "一堆「恭喜您中奖」，让它先学会不上当。", "A heap of 'you've won a prize!' texts, so it learns not to fall for them." } },
            { "mnist", new[] { "一堆歪歪扭扭的手写数，字比你的还丑。", "Piles of wobbly handwritten digits, uglier than your handwriting." } },
            { "danmu", new[] { "看弹幕学察言观色，一排「哈哈哈」就是笑了。", "Reads the comments to read the room: a row of 'hahaha' means laughing." } },
            { "headline", new[] { "每条都「震惊了」，它得学会别点进去。", "Every headline says 'shocking!', and it has to learn not to click." } },
            { "logic", new[] { "是非题刷题本，对错之间练脑子。", "A true/false workbook: brain gym between right and wrong." } },
            { "arith", new[] { "「3+5=9 对吗？」这种题刷到手软。", "'Is 3+5=9?' Question after question until its hands go soft." } },
            { "cifar", new[] { "糊得像网吧监控的小图，猫狗飞机轮着认。", "Tiny pics as blurry as internet-café CCTV: cats, dogs and planes take turns." } },
            { "meme", new[] { "看斗图猜心情，笑哭的不一定在笑。", "Guesses moods from meme faces; the crying-laughing one isn't always laughing." } },
            { "poems", new[] { "把李白杜甫当语文作业，一个字一个字背。", "Treats Li Bai and Du Fu like homework: memorise one character at a time." } },
            { "longtext", new[] { "专挑又臭又长的句子，像听开会讲话。", "Only long-winded sentences, like sitting through a meeting speech." } },
            { "crosssentence", new[] { "聊天最怕只听半句，这包教它记着上一句。", "The worst chat habit is hearing half a line; this pack teaches it to remember the last one." } },
            { "review", new[] { "满屏「物美价廉，五星好评」，教它认出水军。", "Screens of 'great value, five stars!', teaching it to spot paid reviewers." } },
            { "go", new[] { "把棋盘一盘盘喂它，先学会数自己还剩几口气。", "Feed it board after board until it can count how many breaths its stones have left." } },
            { "imagenet", new[] { "下载要一周，网费和硬盘一起喊疼。", "A week of downloading; the broadband bill and the hard drive both wince." } },
            { "translate", new[] { "海量中英对照句，当成六级真题来刷。", "Heaps of paired Chinese-English sentences, like grinding CET-6 practice papers." } },
            { "news", new[] { "读完二十年新闻，说话要像新闻联播了。", "After twenty years of news, it'll talk like the evening broadcast." } },
        };

        static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            // 阶段 1
            { "perceptron", new[] { "一根筋，只会说行或者不行。", "A one-track mind: it can only say yes or no." } },
            { "weights", new[] { "重要的线索多给几票，班长一票顶三票。", "Important clues get more votes, like a class monitor who counts triple." } },
            { "learnrule", new[] { "错了就改一点点，像被老妈念叨着长大。", "Wrong? Nudge a bit. Like growing up under mom's nagging." } },
            { "step", new[] { "及格线 60，59 分也是不及格。", "The pass mark is 60. A 59 is still a fail." } },
            { "bias", new[] { "及格线可以商量了，像考前求老师划重点。", "The pass mark is negotiable now, like begging the teacher for hints before the exam." } },
            { "features", new[] { "你先把题嚼碎了再喂它，像喂小孩吃饭。", "You chew the problem first and spoon-feed it, like feeding a toddler." } },
            { "shared.lr", new[] { "试好几双鞋，挑最合脚的那双。", "Try on several pairs of shoes and keep the one that fits." } },
            { "mlp", new[] { "一层脑子不够用，多加几层，好比开会多一轮。", "One layer wasn't enough, so add more, like another round of meetings." } },
            { "sigmoid", new[] { "硬开关换成调光旋钮，不会一下全亮全灭。", "Swap the on/off switch for a dimmer, so it's no longer all or nothing." } },
            // 阶段 2
            { "momentum", new[] { "骑车下坡，蹬一下就滑老远。", "Cycling downhill: one push and it rolls on and on." } },
            { "backprop", new[] { "不是做一道题对一次答案，是做完一页再对。", "Check answers after a whole page, not after every question." } },
            { "chainrule", new[] { "高数作业让机器代写，终于不用熬夜。", "The machine does the calculus homework, so no more all-nighters." } },
            { "cudnn", new[] { "显卡厂商自带的秘籍，同样的活更快更省电。", "The GPU maker's own cheat sheet: same work, faster and cheaper to power." } },
            { "cooler", new[] { "给显卡装个大风扇，别让它夏天中暑。", "A big fan for the graphics card, so it doesn't get heatstroke in summer." } },
            { "lenet", new[] { "拿个小放大镜扫全图，字挪到哪都认得。", "Scans the picture with a small magnifier, so it spots things wherever they sit." } },
            { "rnn", new[] { "金鱼脑：读到后面就忘了前面。", "Goldfish brain: by the end it has forgotten the start." } },
            // 阶段 3
            { "alexnet", new[] { "LeNet 的豪华加大版，还插了两块显卡。", "LeNet super-sized, with two graphics cards plugged in." } },
            { "vgg", new[] { "一层层楼叠上去，电费和显存一起哭。", "Stack the floors sky-high and the power bill and the VRAM cry together." } },
            { "googlenet", new[] { "放大镜和广角镜一起用，还挺省钱。", "Magnifier and wide-angle lens together, and it's still cheap." } },
            { "relu", new[] { "差评一律清零，好评原样放行，传话不走样。", "Bad reviews zeroed, good ones passed on untouched: the message stays intact." } },
            { "gradclip", new[] { "一步最多迈半米，别让它一脚踩空。", "A step is capped at half a metre, so it can't take one giant stumble." } },
            { "irnn", new[] { "把传声筒擦干净，前面的字别一个个漏掉。", "Wipe the telephone clean so the early words stop slipping away." } },
            { "dropout", new[] { "随机让几个脑细胞请假，逼它别死记硬背。", "Randomly send a few neurons on leave, so it can't just memorise." } },
            { "augment", new[] { "同一张图换个角度拍，一张当三张用。", "Snap the same photo from another angle: one picture does the work of three." } },
            { "dataclean", new[] { "把数据里的错题和重复的，扫地出门。", "Sweep the wrong answers and the duplicates out of the door." } },
            { "rmsprop", new[] { "每个参数自己调步子，不搞一刀切。", "Each parameter picks its own stride, no one-size-fits-all." } },
            { "lstm", new[] { "脑子里请了个门卫，有用的放行，没用的挡外面。", "A doorman for its memory: useful things in, junk kept outside." } },
            { "gru", new[] { "LSTM 的瘦身版，少个门卫，照样好使。", "LSTM on a diet: one doorman fewer, still works." } },
            // 阶段 4
            { "textcnn", new[] { "拿看图的眼神读句子，快是快，就是近视。", "Reads sentences with picture-eyes: fast, but short-sighted." } },
            { "resnet", new[] { "每层楼都开条消防通道，楼再高也走得通。", "A fire-exit shortcut on every floor, so even a skyscraper still works." } },
            { "batchnorm", new[] { "做操前先排好队，下一层才看得明白。", "Line up before drill, so the next layer isn't staring at chaos." } },
            { "adam", new[] { "自带导航的优化器，路怎么走不用你操心。", "The optimiser with built-in GPS: the route is no longer your worry." } },
            { "lrschedule", new[] { "先大步赶路，快到了再放慢脚步找门牌号。", "Stride at first, then slow down to read the house numbers." } },
            { "seq2seq", new[] { "不抢话：先听完整句，再开口翻译。", "No interrupting: hear the whole sentence first, then translate." } },
            { "attention", new[] { "翻译时偷瞄原文，像考试偷看同桌的卷子。", "Peeks back at the source while translating, like glancing at your deskmate's paper." } },
            { "earlystop", new[] { "分数不涨就别死磕，早点收工。", "Scores stopped going up? Knock off early." } },
            { "wordvec", new[] { "给每个词办张身份证，意思近的住隔壁。", "Gives every word an ID card, with similar meanings as neighbours." } },
            // 阶段 5
            { "position", new[] { "给每个字发个座位号，谁先谁后一目了然。", "Hands every word a seat number, so who comes first is obvious." } },
            { "caption", new[] { "小学作文「看图写话」，它也终于会了。", "Primary-school 'describe the picture' homework, and now it can do it too." } },
            { "transfer", new[] { "换了份工作，把以前的基本功也带过去。", "New job, same old skills: it brings its basics along." } },
            { "transformer", new[] { "不再一个字一个字读，一眼扫完整句，像速读。", "Stops reading word by word and takes in the whole sentence at a glance, like speed-reading." } },
            { "beamsearch", new[] { "翻译不押一个答案，多留几个备胎。", "Doesn't bet on one answer, and keeps a few spares." } },
            { "subword", new[] { "不认识的字就拆开看，像拆快递。", "Can't read a rare word? Take it apart, like unboxing a parcel." } },
            // 阶段 6
            { "multihead", new[] { "好几双眼睛盯不同的重点，三头六臂。", "Several pairs of eyes on different things at once: three heads, six arms." } },
            { "layernorm", new[] { "不管别的字吵不吵，自己先把音量调匀。", "Whatever the other words are doing, each one evens out its own volume." } },
            { "residual", new[] { "每层都留条小路，信息抄近道就能过。", "A back alley on every layer, so information can take the shortcut." } },
            { "warmup", new[] { "先热身再上场，不然一上来就拉伤。", "Warm up before the match, or it pulls a muscle on the first kick." } },
            { "fp16", new[] { "数字算到差不多就行，省下一半显存。", "Rounds the numbers off: close enough, and half the VRAM saved." } },
            { "sft", new[] { "光会聊天不行，还得学会听吩咐，像实习生。", "Chatting isn't enough; it has to follow orders, like an intern." } },
            { "cot", new[] { "把步骤写出来，老师要看过程，不只看答案。", "Show your working. The teacher wants the steps, not just the answer." } },
            { "secret.6", new[] { "武林秘籍一本，照着练预训练不走火入魔。", "A martial-arts manual: follow it and pre-training won't blow up on you." } },
            { "datacenter", new[] { "家里电闸老跳，只好把机器送去机房住。", "The fuse at home keeps tripping, so the machines move into a server room." } },
            // 标注 (label.*)
            { "label.raise", new[] { "老板大发善心：同一道题多给几毛钱。", "The boss gets generous: same question, a few more coins." } },
            { "label.auto", new[] { "让它自己答题，你在旁边嗑瓜子。", "It answers by itself while you sit there cracking sunflower seeds." } },
            { "label.coop", new[] { "有把握就自己交卷，拿不准才举手问老师。", "Hands in the paper when sure, raises a hand when not." } },
            { "label.brain", new[] { "文字题自己动脑想，不再光靠背答案。", "Text questions get real thinking now, not just recalling answers." } },
            { "label.hard", new[] { "难题当专家号收费，答对了多给钱。", "Tough cases are like a specialist appointment: they pay more when you get them right." } },
            { "label.audit", new[] { "交卷前自己再检查一遍，免得错题混进训练。", "Check your own homework before handing it in, so mistakes don't slip into training." } },
            { "label.parallel", new[] { "多放四十道拿不准的题，等你有空再来复核。", "Keep forty more uncertain answers until you have time to review them." } },
            { "label.trapmemory", new[] { "认出平台的老陷阱题，先喊你来看，不乱交。", "Spots the platform's old trap questions and calls you over instead of guessing." } },
            { "label.captcha", new[] { "验证码交给它输，别输得太整齐被盯上。", "Let it type the captchas, just not so evenly that you get flagged." } },
        };
    }
}
