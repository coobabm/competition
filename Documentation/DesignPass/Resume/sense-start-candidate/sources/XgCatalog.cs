using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgTrack { Vision = 0, Sequence = 1 }

    /// <summary>A network family. Later families fit better (lower bias) and tolerate more depth.</summary>
    public sealed class XgArch
    {
        public string id, name, nameEn, note, noteEn;
        /// <summary>What choosing it does to 灵光's own brain: the way one region of the concept board is wired.</summary>
        public string wire = "", wireEn = "";
        /// <summary>The topology's code name, shown in brackets before <see cref="wire"/>: 【局部感受野 · 权值共享】.</summary>
        public string topo = "";
        public XgTrack track;
        public int year;
        public bool shared;
        /// <summary>Legacy price field; tree nodes carry the real price.</summary>
        public double cost;
        /// <summary>Multiplies the capacity error term. Lower is better.</summary>
        public double bias = 1;
        /// <summary>Layers beyond this suffer vanishing gradients (999 = residual, no limit).</summary>
        public int maxDepth = 5;
        /// <summary>Sequence memory in tokens (vision ignores it).</summary>
        public int span = 999;
        public bool seq2seq;
        public double speed = 1, paramFactor = 1;
        /// <summary>Extra divergence risk (plain RNNs explode).</summary>
        public double instability;
    }

    public sealed class XgDataset
    {
        public string id, name, nameEn, metric, metricEn, note, noteEn;
        public XgTrack track;
        public double price, samples;
        /// <summary>Error of an untrained model and the best error reachable on this data.</summary>
        public double chanceError, floorError;
        /// <summary>Parameters (thousands) at which capacity stops being the main limit.</summary>
        public double scale;
        /// <summary>Multiplies steps needed to converge.</summary>
        public double complexity = 1;
        public int needSpan;
        public bool needSeq2Seq;
        /// <summary>Hand-labelled in the 标注台 from the start; price buys the full public pack at once.</summary>
        public bool handLabel;
        /// <summary>Samples at which missing data adds 5% of the error range (data error ∝ (need / samples)^0.6).</summary>
        public double need = 1000;
        /// <summary>Multiplies record rewards and grade bonuses on this dataset (harder data pays more).</summary>
        public double rewardBase = 1;
        /// <summary>
        /// Share of each sample that counts toward the main line's data bar D (<see cref="XgSim.TrainedSamples"/>). The easy
        /// 算术 desk counts half, so its quick cards do not fill the bar; its own model still trains on every sample.
        /// </summary>
        public double dataWeight = 1;
    }

    public sealed class XgContract
    {
        public string id, client, clientEn, job, jobEn, dataset;
        public double threshold, income, signBonus;
        /// <summary>
        /// 实时: the job runs as it happens (live subtitles), so only a model that reads the whole sentence at once
        /// counts (<see cref="XgSim.ParallelAcc"/>); a loop reading word by word falls behind.
        /// </summary>
        public bool realtime;
    }

    public enum XgResearchKind { Optimizer, Dropout, Augment, GradClip, AutoCheckpoint, LrSchedule, BatchNorm, CuDnn, Transfer, ManualPay, Feel, Cooler }

    /// <summary>Phenomenon and Ability nodes are never bought: the 图鉴 lights them when they happen (design v1.1 §11.2).</summary>
    public enum XgNodeKind { Arch, Depth, Width, LrKnob, Dataset, Research, Auto, Breakthrough, Project, Label, Secret, Phenomenon, Ability }

    /// <summary>
    /// One skill-tree node, bought with ¥. Node ids of architectures and research equal their catalog ids, so
    /// <c>XgSim.Has(id)</c> keeps working. Depth / Width nodes raise the cap the player can choose up to.
    /// </summary>
    public sealed class XgNode
    {
        public string id, tree, parent, name, nameEn, note, noteEn;
        /// <summary>Other nodes that must be owned too (drawn as dotted links).</summary>
        public string[] needs = new string[0];
        public XgNodeKind kind;
        /// <summary>Arch / dataset / research id.</summary>
        public string target;
        /// <summary>Depth cap, width index or auto-train level.</summary>
        public int value;
        public double cost;
        public float x, y;
        public int stage;
        public int maxLevel = 1;
        public string lane = "trunk";
    }

    public enum XgDeskKind { Digit, Poem, Logic, Text, Captcha, Meme, Go, Arith }
    public enum XgDeskUnlock { Start, FirstEpoch, Specialty, Lstm, Seq2Seq, Labels500 }

    /// <summary>A 标注台 desk. Its id is the dataset its correct labels feed.</summary>
    public sealed class XgDesk
    {
        public string id, name, nameEn;
        public XgDeskKind kind;
        public XgDeskUnlock unlock;
        /// <summary>Seconds a correct answer keeps the combo alive (reading takes longer on text desks).</summary>
        public double comboWindow = 3;
        /// <summary>Pay factor per card: cards that take thought (logic, reading) pay more than quick visual ones.</summary>
        public double pay = 1;
        /// <summary>
        /// A wrong pick on this desk costs money: this fraction of what a right answer pays (before the combo bonus, so
        /// it scales with the raise multiplier and not with the combo). 0 = a mistake only breaks the combo.
        /// </summary>
        public double fine;
    }

    public sealed class XgResearch
    {
        public string id, name, nameEn, effect, effectEn;
        public XgResearchKind kind;
        public double cost;
        /// <summary>Optimizer only: speed and stability.</summary>
        public double speed = 1, stability = 1;
    }

    public static partial class XgCatalog
    {
        /// <summary>Hidden width per level: 16 … 1024 channels / units.</summary>
        public static readonly int[] Widths = { 16, 32, 64, 128, 256, 512, 1024 };
        /// <summary>Learning rates the knob offers, fastest first.</summary>
        public static readonly string[] LearningRates = { "1", "0.3", "0.1", "0.01", "0.001" };
        public static readonly double[] LrSpeed = { 3.0, 2.0, 1.4, 1.0, .65 };
        /// <summary>Stability needed to run each rate without divergence risk.</summary>
        public static readonly double[] LrRisk = { 3.0, 2.0, 1.4, 1.0, .65 };
        /// <summary>Small rates settle lower: multiplies the final error fraction.</summary>
        public static readonly double[] LrFinal = { 1.35, 1.15, 1.0, .9, .85 };

        public static readonly XgArch[] Archs =
        {
            new XgArch { id = "perceptron", topo = "单层直连", wire = "输入直连「是 / 否」，没有隐藏层：决策边界只能是一条直线。", wireEn = "Every input is wired straight to yes / no with nothing in between: it can only draw one straight line.", name = "感知机", nameEn = "Perceptron", shared = true, year = 1958, maxDepth = 1, span = 1, bias = 1.1,
                note = "只能画一条直线，学不会异或。", noteEn = "One straight line only. No XOR." },
            new XgArch { id = "mlp", topo = "全连接堆叠", wire = "层层全连接，概念可以两两组合；但每个位置各占一组连线，平移一格就是新输入。", wireEn = "A few layers in between, so concepts combine in pairs; but every position has its own wires, so one cell over is something new.", name = "多层感知机", nameEn = "MLP", shared = true, year = 1986, maxDepth = 3, span = 4, bias = .85,
                note = "多几层，能学组合了。挪个位置就不认识。", noteEn = "More layers, so it learns combinations. Shift things and it's lost." },
            new XgArch { id = "caption", topo = "视觉—语言耦合", wire = "看图区的输出直接接进读字区：边看，边逐词生成描述。", wireEn = "The seeing and reading regions wired together: it looks and speaks word by word.", name = "看图说话", nameEn = "Image captioning", track = XgTrack.Vision, year = 2015, maxDepth = 999, bias = .3, span = 999, seq2seq = true, paramFactor = 2,
                note = "边看图边说话。", noteEn = "Looks and talks at once." },
            new XgArch { id = "transformer", topo = "全局自注意力 · 多头并行", wire = "每个字和所有字直接相连，几组注意力同时扫过全句，不再逐字等待：整颗脑子同步点亮。", wireEn = "Every word wired straight to every word, no waiting word by word: the whole brain lights at once.", name = "Transformer", nameEn = "Transformer", shared = true, year = 2017, maxDepth = 999, span = 999, seq2seq = true, bias = .2, paramFactor = 12,
                note = "只用注意力，整句一起算。", noteEn = "Attention only. The whole sentence at once." },
            new XgArch { id = "lenet", topo = "局部感受野 · 权值共享", wire = "看图区只连相邻的笔画，同一组权重扫过整张图：平移以后照样认得。", wireEn = "The seeing region wires only neighbouring strokes, and the same wiring is shared across the picture: it knows a stroke wherever it moves.", name = "LeNet-5", nameEn = "LeNet-5", track = XgTrack.Vision, year = 1998, cost = 0, bias = 1, maxDepth = 5, paramFactor = 3, speed = 1,
                note = "小窗口扫全图，挪了位置也认得。", noteEn = "A small window sweeps the picture. Shifted, still known." },
            new XgArch { id = "alexnet", topo = "局部感受野 · 深层堆叠", wire = "同样只连邻居，层数和通道数都翻上去，配 ReLU 和双卡并行。", wireEn = "Neighbours only, like LeNet, stacked deeper and wider.", name = "AlexNet", nameEn = "AlexNet", track = XgTrack.Vision, year = 2012, cost = 6, bias = .7, maxDepth = 8, paramFactor = 7, speed = 1.3,
                note = "更深、更宽、两块显卡。", noteEn = "Deeper, wider, two GPUs." },
            new XgArch { id = "vgg", topo = "3×3 卷积 · 极深堆叠", wire = "全部换成 3×3 小窗口，一层层叠到十几层。", wireEn = "Neighbours only, stacked to a dozen and more layers.", name = "VGG-16", nameEn = "VGG-16", track = XgTrack.Vision, year = 2014, cost = 50, bias = .55, maxDepth = 16, paramFactor = 9, speed = .8,
                note = "全用小窗口，叠得很深，吃显存。", noteEn = "Small windows only, stacked deep. Eats memory." },
            new XgArch { id = "googlenet", topo = "多尺度并联", wire = "同一层里并联大小不同的几种窗口，参数少，视野多。", wireEn = "Neighbours only, with several window sizes side by side in one layer.", name = "GoogLeNet", nameEn = "GoogLeNet", track = XgTrack.Vision, year = 2014, cost = 200, bias = .45, maxDepth = 22, paramFactor = 1.2, speed = 1,
                note = "大小窗口一起看，参数少。", noteEn = "Big and small windows together. Few parameters." },
            new XgArch { id = "resnet", topo = "残差旁路", wire = "每隔几层留一条恒等旁路，信号原样穿过，网络能叠得很深。", wireEn = "Neighbours only, with a pass-it-on shortcut every few layers, so it stacks very deep.", name = "ResNet", nameEn = "ResNet", track = XgTrack.Vision, year = 2015, cost = 600, bias = .35, maxDepth = 999, paramFactor = 9, speed = 1,
                note = "层间开近道，152 层也练得动。", noteEn = "Shortcuts between layers. 152 layers train fine." },

            new XgArch { id = "rnn", topo = "时序回环", wire = "读字区接回上一时刻的自己，逐字递推；记忆每过一步衰减一截。", wireEn = "The reading region is wired to its own last moment and reads word by word; memory fades a little with every word.", name = "朴素 RNN", nameEn = "Vanilla RNN", track = XgTrack.Sequence, year = 1990, cost = 0, bias = 1, maxDepth = 2, span = 8, paramFactor = 2, speed = 1, instability = .35,
                note = "边读边记，只记得最近几个字。", noteEn = "Remembers as it reads, but only the last few words." },
            new XgArch { id = "lstm", topo = "门控回环", wire = "回环上装了门：输入门、遗忘门、输出门决定什么写入、什么保留。", wireEn = "Gates on the wire back to itself: what matters is kept, the rest let go.", name = "LSTM", nameEn = "LSTM", track = XgTrack.Sequence, year = 1997, cost = 6, bias = .75, maxDepth = 3, span = 80, paramFactor = 8, speed = .8, instability = .1,
                note = "给记忆装上门，整句都记得住。", noteEn = "Gates on memory. Keeps the whole sentence." },
            new XgArch { id = "gru", topo = "精简门控回环", wire = "门控回环的精简版，两道门，算得更快。", wireEn = "Gated like LSTM with one gate fewer, a little faster.", name = "GRU", nameEn = "GRU", track = XgTrack.Sequence, year = 2014, cost = 40, bias = .7, maxDepth = 3, span = 80, paramFactor = 6, speed = 1.05, instability = .1,
                note = "LSTM 简化版，更快。", noteEn = "A lighter LSTM. Faster." },
            new XgArch { id = "seq2seq", topo = "编码—解码 · 定长瓶颈", wire = "一段回路把整句压成一个定长向量，另一段从这个向量逐词解码。", wireEn = "One stretch reads the whole sentence into a single knot, another speaks it out word by word from that knot.", name = "Seq2Seq", nameEn = "Seq2Seq", track = XgTrack.Sequence, year = 2014, cost = 120, bias = .6, maxDepth = 4, span = 80, seq2seq = true, paramFactor = 16, speed = .85, instability = .1,
                note = "先读完，再说出来。能翻译了。", noteEn = "Read it all, then say it. Translation works." },
            new XgArch { id = "attention", topo = "注意力回看", wire = "解码每个词时都能回看原句任意位置，按相关度分配权重。", wireEn = "While saying each word it can wire straight back to any word of the source.", name = "注意力", nameEn = "Attention", track = XgTrack.Sequence, year = 2015, cost = 400, bias = .4, maxDepth = 6, span = 999, seq2seq = true, paramFactor = 16, speed = .9, instability = .05,
                note = "翻每个词都回头看原文。", noteEn = "Looks back at the source for every word." },
            new XgArch { id = "textcnn", topo = "一维局部卷积", wire = "把卷积压成一维，在句子上滑动小窗口，整句并行计算；视野只有窗口那么宽。", wireEn = "The reading region wires only a few neighbouring words, like the seeing region, and the whole sentence runs at once.", name = "文字卷积", nameEn = "TextCNN", track = XgTrack.Sequence, year = 2014, cost = 100, bias = .7, maxDepth = 6, span = 5, paramFactor = 3, speed = 1.3,
                note = "用看图的办法读句子，快，但看不远。", noteEn = "Reads sentences like pictures. Fast, but short-sighted." },
        };

        public static readonly XgDataset[] Datasets =
        {
            new XgDataset { id = "longtext", name = "长句理解", nameEn = "Long sentences", metric = "理解准确率", metricEn = "comprehension accuracy", track = XgTrack.Sequence,
                price = 2500, samples = 80000, chanceError = .5, floorError = .02, scale = 4, complexity = 2, handLabel = true, need = 1200, needSpan = 60,
                rewardBase = 6, note = "句首的决定性信息隔着干扰句。", noteEn = "Decisive information separated by distractors." },
            new XgDataset { id = "crosssentence", name = "跨句理解", nameEn = "Cross-sentence context", metric = "关联准确率", metricEn = "reference accuracy", track = XgTrack.Sequence,
                price = 2500, samples = 80000, chanceError = .5, floorError = .02, scale = 5, complexity = 2, need = 1500, needSpan = 80,
                rewardBase = 7, note = "一句话的意思藏在上一句。", noteEn = "A sentence gets its meaning from the previous one." },
            new XgDataset { id = "mnist", name = "MNIST 手写数字", nameEn = "MNIST digits", metric = "准确率", metricEn = "accuracy", track = XgTrack.Vision,
                price = 300, samples = 60000, chanceError = .9, floorError = .002, scale = .5, complexity = 1, handLabel = true, need = 850,
                rewardBase = 1,
                note = "先在标注台亲手标；买下完整包一次得 6 万张。这里的数字会在画布上挪位置，比原版 MNIST（数字都居中）难：全连接把位置绑死，挪一格就不认得了。", noteEn = "Label by hand first; the full pack adds 60k at once. Digits here move around the canvas, harder than the real (centred) MNIST: a dense net binds positions and loses a digit moved by one cell." },
            new XgDataset { id = "cifar", name = "CIFAR-10", nameEn = "CIFAR-10", metric = "准确率", metricEn = "accuracy", track = XgTrack.Vision,
                price = 600, samples = 50000, chanceError = .9, floorError = .03, scale = 120, complexity = 3, need = 21000, handLabel = true,
                rewardBase = 6,
                note = "5 万张 32×32 彩图。标注台的「验证码」桌也往这里攒小图。", noteEn = "50k 32×32 colour photos. The captcha desk feeds small pictures here too." },
            new XgDataset { id = "meme", name = "表情包情绪", nameEn = "Meme moods", metric = "情绪准确率", metricEn = "mood accuracy", track = XgTrack.Vision,
                price = 700, samples = 6000, chanceError = .5, floorError = .03, scale = 3, complexity = 1, handLabel = true, need = 600,
                rewardBase = 2,
                note = "金馆长、暴走脸：看脸，不看字。", noteEn = "Panda-man and rage faces: read the face, not the caption." },
            new XgDataset { id = "go", name = "围棋局面", nameEn = "Go positions", metric = "气数判断准确率", metricEn = "liberty accuracy", track = XgTrack.Vision,
                price = 2500, samples = 50000, chanceError = .5, floorError = .02, scale = 60, complexity = 3, handLabel = true, need = 2500,
                rewardBase = 10,
                note = "9 路小棋盘数气。AlphaGo 今年三月刚赢了李世石。", noteEn = "Count liberties on a 9×9 board. AlphaGo beat Lee Sedol this March." },
            new XgDataset { id = "imagenet", name = "ImageNet", nameEn = "ImageNet", metric = "Top-5 准确率", metricEn = "top-5 accuracy", track = XgTrack.Vision,
                price = 4000, samples = 1200000, chanceError = .995, floorError = .03, scale = 4000, complexity = 8, need = 260000,
                rewardBase = 40,
                note = "120 万张图、1000 类。买硬盘加下载一周。", noteEn = "1.2M images, 1000 classes. A week of downloading." },

            new XgDataset { id = "poems", name = "全唐诗", nameEn = "Tang poems", metric = "下一字准确率", metricEn = "next-char accuracy", track = XgTrack.Sequence,
                price = 300, samples = 200000, chanceError = .999, floorError = .45, scale = .4, complexity = 1.5, needSpan = 28, handLabel = true, need = 2000,
                rewardBase = 2,
                note = "先在标注台亲手标；买下全唐诗一次得 20 万条。", noteEn = "Label by hand first; the full corpus adds 200k at once." },
            new XgDataset { id = "logic", name = "逻辑是非题", nameEn = "Yes/no logic", metric = "是非题准确率", metricEn = "yes/no accuracy", track = XgTrack.Sequence,
                price = 500, samples = 100000, chanceError = .5, floorError = .03, scale = 4, complexity = 3, needSpan = 40, handLabel = true, need = 1500,
                rewardBase = 8,
                note = "无限生成：比较、否定、条件推理、三段论、真话假话……标得越多题越难。完整包是十万道行测判断题。", noteEn = "Endless: ordering, negation, if–then, syllogisms, liars… harder as you label more. The pack is 100k exam questions." },
            new XgDataset { id = "arith", name = "算术是非题", nameEn = "Yes/no arithmetic", metric = "算术判断准确率", metricEn = "arithmetic accuracy", track = XgTrack.Sequence,
                price = 2500, samples = 20000, chanceError = .5, floorError = .02, scale = .3, complexity = 1.2, needSpan = 20, handLabel = true, need = 700,
                rewardBase = 2, dataWeight = .5,
                note = "无限生成：加减乘除、混合运算、百分数、分数……标得越多题越难。题简单，每条只算半条总样本。完整包是两万道小学口算题。",
                noteEn = "Endless: sums, order of operations, percentages, fractions… harder as you label more. Easy, so each sample counts half toward the total. The pack is 20k primary-school drill questions." },
            new XgDataset { id = "sense", name = "常识判断", nameEn = "Common sense", metric = "常识是非准确率", metricEn = "common-sense accuracy", track = XgTrack.Sequence,
                price = 300, samples = 12000, chanceError = .5, floorError = .02, scale = .3, complexity = 1.1, needSpan = 12, handLabel = true, need = 700,
                rewardBase = 2,
                note = "「太阳从东边升起。」是，「鱼会爬树。」否。每条是一句人人都知道的话，标得越多，它越分得清是和否。（它回答「是」「否」时，靠的就是这点本事。）",
                noteEn = "\"The sun rises in the east.\" Yes. \"Fish climb trees.\" No. Plain things everybody knows: the more you label, the better it tells yes from no. (That is the skill behind its own yes and no.)" },
            new XgDataset { id = "news", name = "人民日报语料", nameEn = "People's Daily corpus", metric = "下一字准确率", metricEn = "next-char accuracy", track = XgTrack.Sequence,
                price = 800, samples = 5000000, chanceError = .999, floorError = .3, scale = 60, complexity = 4, needSpan = 60, need = 1080000,
                rewardBase = 10,
                note = "二十年新闻，五百万句。", noteEn = "Twenty years of news, five million sentences." },
            new XgDataset { id = "translate", name = "中英平行语料", nameEn = "Chinese–English pairs", metric = "翻译准确率", metricEn = "translation accuracy", track = XgTrack.Sequence,
                price = 5000, samples = 2000000, chanceError = 1, floorError = .25, scale = 6000, complexity = 9, needSpan = 50, needSeq2Seq = true, need = 430000,
                rewardBase = 50,
                note = "两百万对中英句子。没有编码器-解码器做不了。", noteEn = "Two million sentence pairs. Needs an encoder–decoder." },
            new XgDataset { id = "xor", name = "异或", nameEn = "XOR", metric = "准确率", metricEn = "accuracy", track = XgTrack.Sequence,
                price = 0, samples = 600, chanceError = .5, floorError = .02, scale = .2, complexity = 1, need = 200, rewardBase = 0,
                note = "恰好一个为真。单层感知机只能画一条直线。", noteEn = "Exactly one is true. One layer draws one straight line." },
            new XgDataset { id = "parallel", name = "串行瓶颈", nameEn = "Serial bottleneck", metric = "准确率", metricEn = "accuracy", track = XgTrack.Sequence,
                price = 0, samples = 600, chanceError = .5, floorError = .02, scale = 20, complexity = 4, need = 800, rewardBase = 0,
                note = "一批长文档，订单有期限。句首的字决定答案，后面也会出现同样的字。循环一句话里只能一个字一个字地算。", noteEn = "Long documents with a deadline. The first character decides; the same characters appear again later. A loop reads a sentence one word at a time." },
            new XgDataset { id = "danmu", name = "弹幕情绪", nameEn = "Bullet-comment sentiment", metric = "情绪准确率", metricEn = "sentiment accuracy", track = XgTrack.Sequence,
                price = 300, samples = 20000, chanceError = .5, floorError = .04, scale = .5, complexity = 1.5, needSpan = 12, handLabel = true, need = 900,
                rewardBase = 3,
                note = "「前方高能」是夸，「辣眼睛」是骂。小心反话。", noteEn = "\"Hype incoming\" praises, \"my eyes\" mocks. Mind the sarcasm." },
            new XgDataset { id = "spam", name = "垃圾短信", nameEn = "SMS spam", metric = "拦截准确率", metricEn = "filter accuracy", track = XgTrack.Sequence,
                price = 300, samples = 12000, chanceError = .5, floorError = .02, scale = .3, complexity = 1.2, needSpan = 20, handLabel = true, need = 700,
                rewardBase = 3,
                note = "「您被《我是歌手》抽中」……老周也常被骗。", noteEn = "\"You won a prize from I Am a Singer\"… 老周 falls for these." },
            new XgDataset { id = "headline", name = "标题党", nameEn = "Clickbait", metric = "识别准确率", metricEn = "detection accuracy", track = XgTrack.Sequence,
                price = 450, samples = 12000, chanceError = .5, floorError = .05, scale = 1.5, complexity = 2, needSpan = 20, handLabel = true, need = 1200,
                rewardBase = 5,
                note = "只看标题的写法，不看事情真假。", noteEn = "Judge the style of the headline, not whether it is true." },
            new XgDataset { id = "review", name = "刷单评论", nameEn = "Fake reviews", metric = "识别准确率", metricEn = "detection accuracy", track = XgTrack.Sequence,
                price = 1000, samples = 12000, chanceError = .5, floorError = .06, scale = 2, complexity = 2.2, needSpan = 30, handLabel = true, need = 1500,
                rewardBase = 6,
                note = "「好评好评好评」和真买家的抱怨。", noteEn = "\"Great great great\" versus real buyers' gripes." },
        };


        /// <summary>标注台 desks in tab order. Locked desks open as the lab grows.</summary>
        public static readonly XgDesk[] Desks =
        {
            new XgDesk { id = "longtext", name = "长句", nameEn = "Long sentences", kind = XgDeskKind.Text, unlock = XgDeskUnlock.Lstm, comboWindow = 12 },
            new XgDesk { id = "mnist", name = "手写数字", nameEn = "Digits", kind = XgDeskKind.Digit, unlock = XgDeskUnlock.Specialty, comboWindow = 3 },
            new XgDesk { id = "poems", name = "唐诗下一字", nameEn = "Next character", kind = XgDeskKind.Poem, unlock = XgDeskUnlock.Specialty, comboWindow = 4 },
            new XgDesk { id = "sense", name = "常识判断", nameEn = "Common sense", kind = XgDeskKind.Text, unlock = XgDeskUnlock.Start, comboWindow = 6, pay = 1 },
            new XgDesk { id = "arith", name = "算术", nameEn = "Arithmetic", kind = XgDeskKind.Arith, unlock = XgDeskUnlock.FirstEpoch, comboWindow = 6, pay = 1, fine = ArithFine },
            new XgDesk { id = "logic", name = "逻辑题", nameEn = "Logic", kind = XgDeskKind.Logic, unlock = XgDeskUnlock.Start, comboWindow = 12, pay = 3.2 },
            new XgDesk { id = "danmu", name = "弹幕情绪", nameEn = "Danmaku", kind = XgDeskKind.Text, unlock = XgDeskUnlock.FirstEpoch, comboWindow = 5, pay = 1.4 },
            new XgDesk { id = "spam", name = "垃圾短信", nameEn = "SMS spam", kind = XgDeskKind.Text, unlock = XgDeskUnlock.Start, comboWindow = 6, pay = 1.4 },
            new XgDesk { id = "meme", name = "表情包", nameEn = "Meme faces", kind = XgDeskKind.Meme, unlock = XgDeskUnlock.Specialty, comboWindow = 4 },
            new XgDesk { id = "headline", name = "标题党", nameEn = "Clickbait", kind = XgDeskKind.Text, unlock = XgDeskUnlock.Specialty, comboWindow = 6 },
            new XgDesk { id = "review", name = "刷单评论", nameEn = "Fake reviews", kind = XgDeskKind.Text, unlock = XgDeskUnlock.Lstm, comboWindow = 7 },
            new XgDesk { id = "cifar", name = "验证码", nameEn = "Captcha", kind = XgDeskKind.Captcha, unlock = XgDeskUnlock.Lstm, comboWindow = 4 },
            new XgDesk { id = "go", name = "围棋气数", nameEn = "Go liberties", kind = XgDeskKind.Go, unlock = XgDeskUnlock.Labels500, comboWindow = 9 },
        };

        static XgNode N(string id, string tree, string parent, XgNodeKind kind, string target, int value, double cost, float x, float y,
            string name, string nameEn, string note, string noteEn, params string[] needs)
        {
            return new XgNode { id = id, tree = tree, parent = parent, kind = kind, target = target, value = value, cost = cost,
                x = x, y = y, name = name, nameEn = nameEn, note = note, noteEn = noteEn, needs = needs ?? new string[0] };
        }

        static XgNode Depth(string tree, int depth, string parent, double cost, float x, float y, params string[] needs)
        {
            string p = tree == "vision" ? "v" : "s";
            return N(p + ".d" + depth, tree, parent, XgNodeKind.Depth, null, depth, cost, x, y, depth + " 层", depth + " layers",
                "可用层数上限 " + depth + "。层越多能组合出越复杂的东西；用不上的层得学会原样转交，学不会就越深越差。也更吃显存。", "Layer cap " + depth + ". Deeper can combine more complex things; layers it does not need must learn to pass things on, or deeper gets worse. More VRAM too.", needs);
        }

        static XgNode Width(string tree, int index, string parent, double cost, float x, float y, params string[] needs)
        {
            string p = tree == "vision" ? "v" : "s";
            int w = Widths[index];
            return N(p + ".w" + index, tree, parent, XgNodeKind.Width, null, index, cost, x, y, "宽 " + w, "Width " + w,
                "每层可用 " + w + (tree == "vision" ? " 个通道" : " 个单元") + "。", "Up to " + w + (tree == "vision" ? " channels" : " units") + " per layer.", needs);
        }

        static XgNode Pack(string tree, string dataset, string parent, double cost, float x, float y, params string[] needs)
        {
            var d = Dataset(dataset);
            return N(dataset + ".pack", tree, parent, XgNodeKind.Dataset, dataset, 0, cost, x, y, d.name, d.nameEn,
                d.handLabel ? "完整数据包：一次补足 " + Count(d.samples) + "。" : "数据集：" + Count(d.samples) + "。" + d.note,
                d.handLabel ? "Full pack: " + d.samples.ToString("0") + " samples at once." : "Dataset: " + d.samples.ToString("0") + " samples. " + d.noteEn, needs);
        }

        static XgNode ArchNode(string tree, string id, string parent, double cost, float x, float y)
        {
            var a = Arch(id);
            return N(id, tree, parent, XgNodeKind.Arch, id, 0, cost, x, y, a.name, a.nameEn, a.note, a.noteEn);
        }

        static XgNode ResearchNode(string id, string parent, double cost, float x, float y, params string[] needs)
        {
            var r = ResearchItem(id);
            return N(id, "research", parent, XgNodeKind.Research, id, 0, cost, x, y, r.name, r.nameEn, r.effect, r.effectEn, needs);
        }

        static string Count(double n) { return n >= 1e4 ? (n / 1e4).ToString("0.#") + " 万条" : n.ToString("0") + " 条"; }

        /// <summary>Three trees: vision (LeNet → … → ResNet), sequence (RNN → … → attention) and shared research.</summary>
        public static XgNode[] Nodes { get { return nodes ?? (nodes = BuildNodes()); } }
        static XgNode[] nodes;

        /// <summary>Skill-tree geometry: one column per stage, cards in rows of <see cref="NodesPerRow"/>, lanes stacked top to bottom.</summary>
        public const float StageWidth = 720, NodeWidth = 136, NodeHeight = 64, BreakthroughSize = 120, SlotWidth = 150, RowHeight = 84;
        public const int NodesPerRow = 4;
        static readonly string[] LaneOrder = { "trunk", "vision", "sequence", "research", "auto", "atlas" };
        /// <summary>Lane bands in drawing order (the tree page paints their stripes).</summary>
        public static IReadOnlyList<string> Lanes => LaneOrder;
        /// <summary>Height of a lane's band (after layout).</summary>
        public static float LaneHeight(string lane) { var _ = Nodes; return laneHeight.TryGetValue(Band(lane), out float h) ? h : 0; }
        static readonly Dictionary<string, float> laneTop = new Dictionary<string, float>();
        static readonly Dictionary<string, float> laneHeight = new Dictionary<string, float>();
        static float treeHeight;
        /// <summary>Height of every lane together (after layout).</summary>
        public static float TreeHeight { get { var _ = Nodes; return treeHeight; } }
        /// <summary>Top of a lane's band (its title sits just above).</summary>
        public static float LaneTop(string lane) { var _ = Nodes; return laneTop.TryGetValue(Band(lane), out float y) ? y : 0; }
        /// <summary>The band a lane is drawn in: the labelling skills (stage 0 only) share the trunk band.</summary>
        public static string Band(string lane) => lane == "label" ? "trunk" : Array.IndexOf(LaneOrder, lane) < 0 ? "vision" : lane;
        public static readonly string[] StageNames = { "标注", "学会简单判断", "学会组合特征", "走向专长", "学会保留信息", "学会寻找重点", "重构学习方式" };
        public static readonly string[] StageNamesEn = { "Labelling", "Simple decisions", "Combine features", "Specialize", "Keep information", "Find what matters", "Rebuild learning" };
        /// <summary>The years each stage's ideas come from (a range where the stage spans several papers).</summary>
        public static readonly string[] StageYears = { "", "1958", "1986", "1989–2014", "1997–2015", "2014–2015", "2017" };
        /// <summary>Years after the game's 2016 are kept in data but never shown: the player is living in 2016.</summary>
        public static string ShownYear(int year) => year > 0 && year <= 2016 ? year.ToString() : "";
        public static string ShownYear(string years) => years.Contains("2017") || years.Contains("2018") || years.Contains("202") ? "" : years;

        /// <summary>
        /// Price factor of each stage's nodes. Training without knobs (2026-10-07) made stages 2–3 dearer (×2, ×2.2) and
        /// stage 4 cheaper; with rent and bills (经济压力与破产, 2026-10-08) stages 2–3 come down to ×1.6 and ×1.4 so the
        /// cheap squares that reach abilities 3–5 stay affordable while the rent is paid.
        /// </summary>
        public static readonly double[] StagePrice = { 1, 1, 1.6, 1.4, .7, 1, 1 };

        static XgNode[] BuildNodes()
        {
            var list = new List<XgNode>();
            void Add(XgNode n, int stage, string lane)
            {
                n.stage = stage; n.lane = lane;
                if (lane != "label" && lane != "atlas" && stage >= 0 && stage < StagePrice.Length) n.cost = Math.Round(n.cost * StagePrice[stage]);
                list.Add(n);
            }
            XgNode Simple(string id, XgNodeKind kind, string parent, double cost, string zh, string en, string note, string noteEn)
                => N(id, "trunk", parent, kind, id, 0, cost, 0, 0, zh, en, note, noteEn);
            void A(string id, int stage, string lane, string parent, double cost)
                => Add(ArchNode(lane == "vision" ? "vision" : lane == "sequence" ? "sequence" : "trunk", id, parent, cost, 0, 0), stage, lane);
            void D(string tree, int depth, string parent, double cost, int stage, string lane)
                => Add(Depth(tree, depth, parent, cost, 0, 0), stage, lane);
            void W(string tree, int width, string parent, double cost, int stage, string lane)
                => Add(Width(tree, width, parent, cost, 0, 0), stage, lane);
            void P(string id, string parent, double cost, int stage, string lane)
                => Add(Pack(XgCatalog.Dataset(id).track == XgTrack.Vision ? "vision" : "sequence", id, parent, cost, 0, 0), stage, lane);
            void R(string id, string parent, double cost, int stage)
                => Add(ResearchNode(id, parent, cost, 0, 0), stage, "research");

            void S(string id, int stage, string parent, double price, string zh, string en)
                => Add(Simple(id, XgNodeKind.Secret, parent, price, zh, en, "秘籍：写明预训练的配法。自己跑通能拿 1.5 倍奖金。", "Secret: how to set up pre-training. Getting it through yourself pays 1.5× its price."), stage, "trunk");

            // Architectures are items (参数量与数据量主线 §6): bought at a price, each lowers one ability's thresholds
            // (XgSim.AbilityDiscounts). The old breakthroughs (bt.*) are folded into them: their price is the item's.

            // ── 阶段 1 · 感知机 → 多层感知机
            A("perceptron", 1, "trunk", null, 0);
            R("weights", "perceptron", 15, 1); R("learnrule", "weights", 40, 1); R("step", "perceptron", 30, 1);
            R("bias", "perceptron", 20, 1);
            // The pre-network road: people hand-make the features (练法 instead of 结构; XgBoard.Engineered).
            R("features", "bias", 60, 1);
            Add(Simple("shared.lr", XgNodeKind.LrKnob, "perceptron", 60, "学习率搜索", "Learning-rate search", "自动试几个学习率，挑最好的那个：两条线的上限都高一点。", "Tries a few rates and keeps the best: both lines reach a little higher."), 1, "trunk");
            P("spam", "perceptron", 300, 1, "sequence");
            A("mlp", 1, "trunk", "perceptron", 150);
            D("trunk", 2, "mlp", 80, 1, "trunk");
            R("sigmoid", "mlp", 120, 1);
            // The scale ladder (参数量与数据量主线): each stage sells the widths and depths that reach the next ability.
            W("trunk", 1, "mlp", 150, 1, "trunk");
            W("trunk", 2, "s.w1", 450, 1, "trunk");

            // ── 阶段 2 · 多层感知机 → 卷积、循环
            D("trunk", 3, "s.d2", 100, 2, "trunk");
            R("momentum", "mlp", 200, 2);
            R("backprop", "mlp", 200, 2); R("chainrule", "backprop", 250, 2); R("cudnn", "mlp", 600, 2); R("cooler", "mlp", 125, 2);
            P("mnist", "mlp", 2000, 2, "vision"); P("danmu", "mlp", 1500, 2, "sequence"); P("headline", "mlp", 1500, 2, "sequence"); P("logic", "mlp", 3000, 2, "sequence");
            // The 算术 pack waits for stage 2: at stage 1 its samples would fill the first data bar on their own.
            P("arith", "mlp", 2500, 2, "sequence");
            A("lenet", 2, "vision", "mlp", 3000);
            A("rnn", 2, "sequence", "mlp", 3000);
            D("vision", 3, "lenet", 80, 2, "vision"); D("vision", 4, "v.d3", 100, 2, "vision");
            W("vision", 1, "lenet", 1000, 2, "vision"); W("vision", 2, "v.w1", 3000, 2, "vision"); W("vision", 3, "v.w2", 12000, 2, "vision");
            W("sequence", 3, "s.w2", 12000, 2, "sequence");

            // ── 阶段 3 · CNN + RNN → 门控记忆
            A("alexnet", 3, "vision", "lenet", 3000);
            A("vgg", 3, "vision", "alexnet", 2000); A("googlenet", 3, "vision", "vgg", 6000);
            D("vision", 5, "v.d4", 800, 3, "vision"); D("vision", 6, "v.d5", 1500, 3, "vision"); D("vision", 8, "v.d6", 1500, 3, "vision");
            W("vision", 4, "v.w3", 6000, 3, "vision");
            D("sequence", 4, "s.d3", 1500, 3, "sequence"); W("sequence", 4, "s.w3", 8000, 3, "sequence");
            P("cifar", "lenet", 2000, 3, "vision"); P("meme", "lenet", 1500, 3, "vision");
            P("poems", "rnn", 2000, 3, "sequence");
            R("relu", "lenet", 200, 3); R("gradclip", "rnn", 150, 3); R("irnn", "gradclip", 1200, 3); R("dropout", "mlp", 400, 3); R("augment", "dropout", 900, 3); R("dataclean", "dropout", 800, 3); R("rmsprop", "momentum", 1200, 3);
            A("lstm", 3, "sequence", "rnn", 4000); A("gru", 3, "sequence", "lstm", 1500);
            P("longtext", "lstm", 2500, 3, "sequence");

            // ── 阶段 4 · LSTM + ResNet → 编码器-解码器
            D("vision", 12, "v.d8", 5000, 4, "vision"); D("vision", 16, "v.d12", 12000, 4, "vision"); W("vision", 5, "v.w4", 30000, 4, "vision");
            D("sequence", 6, "s.d4", 8000, 4, "sequence"); W("sequence", 5, "s.w4", 30000, 4, "sequence");
            P("crosssentence", "lstm", 2500, 4, "sequence"); P("review", "lstm", 1000, 4, "sequence");
            A("textcnn", 4, "sequence", "rnn", 2500);
            A("resnet", 4, "vision", "lenet", 15000);
            P("go", "resnet", 5000, 4, "vision"); P("imagenet", "resnet", 15000, 4, "vision");
            R("batchnorm", "dropout", 3000, 4); R("adam", "rmsprop", 5000, 4); R("lrschedule", "momentum", 1500, 4);
            A("seq2seq", 4, "sequence", "lstm", 15000);
            A("attention", 4, "sequence", "seq2seq", 15000);

            // ── 阶段 5 · Seq2Seq + 注意力 → Transformer（它自己问出「如果只用注意力呢？」之后才能买）
            R("position", "attention", 6000, 5);
            D("vision", 32, "v.d16", 8000, 5, "vision"); W("vision", 6, "v.w5", 70000, 5, "vision");
            D("sequence", 8, "s.d6", 8000, 5, "sequence"); W("sequence", 6, "s.w5", 50000, 5, "sequence");
            P("translate", "attention", 30000, 5, "sequence"); P("news", "rnn", 40000, 5, "sequence");
            A("caption", 5, "vision", "resnet", 20000); list[list.Count - 1].needs = new[] { "attention" };
            R("transfer", "lrschedule", 12000, 5);
            // Not bought, the Transformer comes free with the sixth ability; bought, it lowers that ability's thresholds.
            A("transformer", 5, "trunk", "attention", 30000);

            // ── 阶段 6 · 全部放开：预训练
            Add(Simple("multihead", XgNodeKind.Label, "transformer", 0, "多头注意力", "Multi-head attention", "研发产出，不另收费。", "Research output; no additional charge."), 6, "research");
            Add(Simple("layernorm", XgNodeKind.Label, "transformer", 0, "LayerNorm", "LayerNorm", "2016 年提出：每个字按自己的各项特征归一化，不依赖整批数据（BatchNorm 是按一批样本）。", "Proposed in 2016: each word is normalised across its own features, without the batch (BatchNorm uses a batch of samples)."), 6, "research");
            Add(Simple("residual", XgNodeKind.Label, "transformer", 0, "残差连接", "Residual connections", "继承 ResNet：每层都留一条捷径。", "Inherited from ResNet: a shortcut around every layer."), 6, "research");
            R("warmup", "transformer", 20000, 6);
            // Pascal's half precision (2016): one of the multipliers pre-training's effective scale stacks (XgSim.ScaleItems).
            R("fp16", "transformer", 30000, 6);
            // Pre-training scale (about 100M parameters): the reading region at width 1024 and 8–12 layers.
            D("sequence", 12, "s.d8", 10000, 6, "sequence");
            // Side research of stages 4–6 (design v1.1 §11.2): no walls, only a little speed.
            R("earlystop", "lrschedule", 2500, 4); R("wordvec", "rnn", 3500, 4);
            R("beamsearch", "attention", 7000, 5); R("subword", "attention", 9000, 5);
            R("sft", "transformer", 25000, 6); R("cot", "sft", 40000, 6);
            // 图鉴: phenomena light up the first time they happen, abilities as the AI reaches them.
            foreach (var p in XgPhenomena.All)
                Add(N("ph." + p.id, "atlas", null, XgNodeKind.Phenomenon, p.id, 0, 0, 0, 0, p.name, p.nameEn, p.why, p.whyEn), p.stage, "atlas");
            for (int i = 1; i < AbilityNames.Length; i++)
                Add(N("ab." + i, "atlas", null, XgNodeKind.Ability, null, i, 0, 0, 0, AbilityNames[i], AbilityNamesEn[i], AbilityNotes[i], AbilityNotesEn[i]), i, "atlas");
            S("secret.6", 6, "transformer", 30000, "秘籍 · 预训练", "Secret · pre-training");
            Add(Simple("datacenter", XgNodeKind.Label, "transformer", 60000, "租机房（IDC 机柜）", "Rent a server room (IDC rack)", "预训练一开，一台机箱的 3500W 就跳闸。在 IDC 租一个机柜，八张卡接专线：这里付押金和首月租金；预训练跑着的时候，按时长另付租金和电费（¥" + XgSim.DatacenterRent + "/秒），钱不够就断电。", "Pre-training trips one case's 3500 W breaker. Rent a rack in a data centre, eight cards on a dedicated line: this pays the deposit and the first month; while pre-training runs, rent and power cost ¥" + XgSim.DatacenterRent + "/s more, and it powers off when the money runs out."), 6, "research");
            // Automation starts at crontab (auto2); run.sh (auto1) is retired.
            for (int i = 1; i < AutoNames.Length; i++)
                Add(N("auto" + (i + 1), "research", i == 1 ? "perceptron" : "auto" + i, XgNodeKind.Auto, null, i + 1, AutoCosts[i], 0, 0, AutoNames[i], AutoNamesEn[i], AutoNotes[i], AutoNotesEn[i]), i + 1, "auto");
            foreach (var node in XgSim.LabelNodes) Add(node, 0, "label");
            LayoutStages(list);
            return list.ToArray();
        }

        /// <summary>
        /// Lays the cards out: each lane is as tall as its fullest stage needs, so nothing overlaps and the names
        /// stay large. Breakthroughs sit on the border between two stages, centred on their lane.
        /// </summary>
        /// <summary>
        /// Lays the cards out as chains that read left to right. In each stage and lane, a node sits directly right of
        /// its parent on the parent's row when the parent is in the same stage and lane (so links run straight across);
        /// other nodes start a chain at the left edge on the first free row, and siblings stack below. A chain longer
        /// than <see cref="NodesPerRow"/> wraps onto a new row. Each lane is as tall as its fullest stage needs.
        /// Breakthroughs sit on the border between two stages, centred on their lane.
        /// </summary>
        static void LayoutStages(List<XgNode> list)
        {
            var byId = new Dictionary<string, XgNode>();
            foreach (var n in list) byId[n.id] = n;
            var row = new Dictionary<string, int>(); var column = new Dictionary<string, int>();
            var taken = new HashSet<string>(); var rows = new Dictionary<string, int>();
            foreach (var n in list)
            {
                if (n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project) continue;
                string key = n.stage + ":" + Band(n.lane);
                int r = 0, c = 0;
                if (n.parent != null && byId.TryGetValue(n.parent, out var p) && column.ContainsKey(p.id) && p.stage == n.stage && Band(p.lane) == Band(n.lane))
                {
                    r = row[p.id]; c = column[p.id] + 1;
                    if (c >= NodesPerRow) { c = 0; r++; }
                }
                while (taken.Contains(key + ":" + r + ":" + c)) r++;
                taken.Add(key + ":" + r + ":" + c);
                row[n.id] = r; column[n.id] = c;
                rows.TryGetValue(key, out int used); rows[key] = Math.Max(used, r + 1);
            }
            float top = 110;
            laneTop.Clear(); laneHeight.Clear();
            foreach (var lane in LaneOrder)
            {
                int most = 1;
                foreach (var kv in rows) if (kv.Key.EndsWith(":" + lane, StringComparison.Ordinal)) most = Math.Max(most, kv.Value);
                laneTop[lane] = top; laneHeight[lane] = most * RowHeight;
                top += most * RowHeight + 48;
            }
            treeHeight = top;
            foreach (var n in list)
            {
                string band = Band(n.lane);
                if (n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project)
                {
                    n.x = n.stage * StageWidth - 16;
                    string home = n.lane == "vision" || n.lane == "sequence" ? n.lane : "trunk";
                    n.y = laneTop[home] + Math.Max(RowHeight, laneHeight[home]) * .5f - RowHeight * .5f + NodeHeight * .5f;
                    continue;
                }
                n.x = n.stage * StageWidth + 120 + column[n.id] * SlotWidth;
                n.y = laneTop[band] + NodeHeight * .5f + row[n.id] * RowHeight;
            }
        }

        /// <summary>What the AI can do at each stage (the ability nodes of the 图鉴).</summary>
        public static readonly string[] AbilityNames = { "", "能力 · 是 / 否", "能力 · 多选一", "能力 · 一个词", "能力 · 短句", "能力 · 长记忆", "能力 · 全部放开" };
        public static readonly string[] AbilityNamesEn = { "", "Ability · yes / no", "Ability · pick one", "Ability · one word", "Ability · short sentences", "Ability · long memory", "Ability · everything" };
        static readonly string[] AbilityNotes = { "", "它只会回答「是」或「否」。", "它能在几个选项里挑一个。", "它会说一个词，看图也能说出一个词。", "它能说 20 个字以内的短句，记得最近几轮。", "它记得你们聊过什么，能翻译，能读懂乱码。", "规模上去以后，能力一起冒了出来：写诗、解题、写代码、看图、总结、推理。" };
        static readonly string[] AbilityNotesEn = { "", "It can only answer yes or no.", "It can pick one of a few options.", "It says one word, and names a picture in one word.", "It speaks short sentences of up to 20 characters and remembers the last few turns.", "It remembers what you talked about, translates and reads the garbled page.", "With scale the abilities came all at once: poems, puzzles, code, pictures, summaries, reasoning." };

        // Index 0 (run.sh) is retired: it is not in the tree any more; the chain starts at crontab.
        public static readonly string[] AutoNames = { "训练脚本 run.sh", "定时任务 crontab", "后台守护进程", "早停", "AutoML 流水线" };
        public static readonly string[] AutoNamesEn = { "Training script run.sh", "crontab job", "Background daemon", "Early stopping", "AutoML pipeline" };
        static readonly string[] AutoNotes =
        {
            "按住「训练一轮」不放就会一轮接一轮地按，连击不断。",
            LingGuangV05.Core.AppNames.AppZh + "窗口开着时，每 3 秒自动训练一轮（只有手按的一半效果，不算连击）。",
            "窗口关了也在后台训练（游戏开着才算，关掉游戏就停）。",
            "自动训练时，连续 12 轮没刷新纪录就停下，保留最佳检查点，不白烧电。",
            "自动调学习率、自动签能签的订单；一个数据集练不动了，先回炉被新题型拖分的桌，再去够快能签的订单，再换样本最多的；用户日志够准就交给模型自己标。你只管科技。",
        };
        static readonly string[] AutoNotesEn =
        {
            "Hold 训练一轮 and it keeps pressing for you; the combo stays alive.",
            "While the window is open, trains one epoch every 3 s (half as strong as a hand press; no combo).",
            "Keeps training in the background with the window closed (only while the game runs).",
            "While auto-training, stops after 12 epochs without a record and keeps the best checkpoint: no wasted power.",
            "Tunes the rate and signs every contract it can; when a dataset stalls it retrains desks a new meme dragged down first, then contracts almost within reach, then the dataset with most samples; it lets the model label user logs once it is accurate enough. You just pick research.",
        };
        static readonly double[] AutoCosts = { 250, 900, 3000, 5000, 20000 };

        /// <summary>
        /// 加薪: hand-labelling pay multiplier per raise level. Whole numbers only: the first raise doubles the pay and
        /// every later one adds ×2 (×1 → ×2 → ×4 → ×6 … → ×34 over 17 raises), so each promotion reads cleanly at the
        /// desk. Applies to hand answers and chat questions, never to auto-answer.
        /// Costs grow ×1.7 per level from ¥10.
        /// </summary>
        public static readonly double[] RaiseMultipliers = { 1, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34 };
        public static int RaiseMax { get { return RaiseMultipliers.Length - 1; } }
        public static double RaiseCost(int level) { return Math.Round(10 * Math.Pow(1.7, level)); }
        static readonly string[] RaiseTitlesZh = { "实习标注员", "标注员", "熟练标注员", "资深标注员", "标注组长", "质检主管", "项目经理", "标注总监", "合伙人", "首席标注官" };
        static readonly string[] RaiseTitlesEn = { "Intern labeller", "Labeller", "Skilled labeller", "Senior labeller", "Team lead", "QA lead", "Project manager", "Labelling director", "Partner", "Chief labelling officer" };
        /// <summary>Job title for a raise level: a new title every two raises, the last one at max.</summary>
        public static string RaiseTitle(int level, bool english)
        {
            int i = level >= RaiseMax ? RaiseTitlesZh.Length - 1 : Math.Min(RaiseTitlesZh.Length - 2, (level + 1) / 2);
            return english ? RaiseTitlesEn[i] : RaiseTitlesZh[i];
        }

        /// <summary>Bonus for reaching grade C, B, A, S on a dataset the first time (× rewardBase).</summary>
        public static readonly double[] GradeBonus = { 0, 20, 60, 200, 800 };
        public static readonly int[] GradeScore = { 0, 300, 600, 800, 950 };
        public static readonly string[] GradeNames = { "D", "C", "B", "A", "S" };
        /// <summary>Combo tiers: 10 不错, 25 手热了, 50 上头, 100 灵光一闪.</summary>
        public static readonly int[] ComboTiers = { 10, 25, 50, 100 };
        public static readonly string[] ComboTierNames = { "不错", "手热了", "上头", "灵光一闪" };
        public static readonly string[] ComboTierNamesEn = { "Nice", "Warming up", "On fire", "Flash of insight" };

        public static XgNode Node(string id) { foreach (var n in Nodes) if (n.id == id) return n; return null; }
        public static XgDesk Desk(string id) { foreach (var d in Desks) if (d.id == id) return d; return null; }

        public static readonly XgContract[] Contracts =
        {
            new XgContract { id = "cheque", client = "城郊信用社", clientEn = "Suburban credit union", job = "支票金额识别", jobEn = "Cheque amount reading", dataset = "mnist", threshold = .97, income = .4, signBonus = 60 },
            new XgContract { id = "zipcode", client = "区邮政局", clientEn = "District post office", job = "邮政编码分拣", jobEn = "Postcode sorting", dataset = "mnist", threshold = .99, income = 1, signBonus = 200 },
            new XgContract { id = "memetag", client = "斗图网", clientEn = "A meme site", job = "表情包自动打标签", jobEn = "Meme auto-tagging", dataset = "meme", threshold = .85, income = .6, signBonus = 120 },
            new XgContract { id = "captcha", client = "某购票网站", clientEn = "A ticketing site", job = "验证码难度测评", jobEn = "Captcha difficulty testing", dataset = "cifar", threshold = .7, income = 2, signBonus = 450 },
            new XgContract { id = "taobao", client = "淘宝卖家联盟", clientEn = "Taobao seller alliance", job = "商品图自动分类", jobEn = "Product photo tagging", dataset = "imagenet", threshold = .75, income = 11, signBonus = 2500 },
            new XgContract { id = "faceclock", client = "工业园区", clientEn = "Industrial park", job = "园区监控识别", jobEn = "Park camera recognition", dataset = "imagenet", threshold = .9, income = 35, signBonus = 8000 },

            new XgContract { id = "acrostic", client = "公众号「每日一诗」", clientEn = "WeChat account Daily Poem", job = "藏头诗生成", jobEn = "Acrostic poems", dataset = "poems", threshold = .35, income = .5, signBonus = 80 },
            new XgContract { id = "homework", client = "家教网站", clientEn = "Tutoring site", job = "作业是非题自动批改", jobEn = "Homework auto-marking", dataset = "logic", threshold = .75, income = 1.5, signBonus = 300 },
            new XgContract { id = "civilexam", client = "考公培训班", clientEn = "Civil-service prep school", job = "判断推理陪练", jobEn = "Reasoning drills", dataset = "logic", threshold = .88, income = 7, signBonus = 1500 },
            new XgContract { id = "danmaku", client = "某弹幕网站", clientEn = "A danmaku video site", job = "弹幕情绪审核", jobEn = "Comment moderation", dataset = "danmu", threshold = .85, income = .75, signBonus = 150 },
            new XgContract { id = "antifraud", client = "市反诈中心", clientEn = "City anti-fraud centre", job = "诈骗短信拦截", jobEn = "Scam SMS filtering", dataset = "spam", threshold = .92, income = 1, signBonus = 250 },
            new XgContract { id = "clickbait", client = "某浏览器资讯频道", clientEn = "A browser news feed", job = "标题党降权", jobEn = "Clickbait demotion", dataset = "headline", threshold = .85, income = 2, signBonus = 400 },
            new XgContract { id = "fakereview", client = "电商平台风控部", clientEn = "Marketplace risk team", job = "刷单评论识别", jobEn = "Fake-review detection", dataset = "review", threshold = .85, income = 3.5, signBonus = 800 },
            new XgContract { id = "ime", client = "某输入法", clientEn = "A pinyin IME", job = "联想词推荐", jobEn = "Next-word suggestions", dataset = "news", threshold = .45, income = 2, signBonus = 450 },
            new XgContract { id = "crossborder", client = "表姐的微商小店", clientEn = "Cousin’s online shop", job = "商品描述翻译", jobEn = "Listing translation", dataset = "translate", threshold = .45, income = 18, signBonus = 4000 },
            new XgContract { id = "webnovel", client = "网文平台", clientEn = "A web-novel site", job = "连载吃书检测", jobEn = "Plot-hole detection", dataset = "longtext", threshold = .9, income = 9, signBonus = 2000 },
            new XgContract { id = "lawfirm", client = "律师事务所", clientEn = "A law firm", job = "合同前后条款核对", jobEn = "Cross-clause contract checks", dataset = "crosssentence", threshold = .9, income = 20, signBonus = 5000 },
            new XgContract { id = "subtitle", client = "视频网站版权部", clientEn = "A video site's licensing team", job = "美剧字幕初翻", jobEn = "Subtitle first drafts", dataset = "translate", threshold = .62, income = 45, signBonus = 10000 },
            new XgContract { id = "livesub", client = "直播平台", clientEn = "A live-streaming site", job = "直播实时字幕", jobEn = "Live subtitles", dataset = "translate", threshold = .6, income = 80, signBonus = 20000, realtime = true },
        };

        public static readonly XgResearch[] Research =
        {
            new XgResearch { id = "earlystop", kind = XgResearchKind.Feel, name = "早停", nameEn = "Early stopping", cost = 2500, effect = "分数不涨就停。训练 +5%", effectEn = "Stop when the score stalls. Training +5%" },
            new XgResearch { id = "wordvec", kind = XgResearchKind.Feel, name = "词向量", nameEn = "Word vectors", cost = 3500, effect = "每个词变成一串数字。文字训练 +8%", effectEn = "Every word becomes a list of numbers. Text training +8%" },
            new XgResearch { id = "beamsearch", kind = XgResearchKind.Feel, name = "束搜索", nameEn = "Beam search", cost = 7000, effect = "翻译时多留几条候选。翻译训练 +10%", effectEn = "Keep a few candidates while translating. Translation +10%" },
            new XgResearch { id = "subword", kind = XgResearchKind.Feel, name = "子词切分", nameEn = "Subword units", cost = 9000, effect = "生僻词拆成小块。文字训练 +5%", effectEn = "Rare words split into pieces. Text training +5%" },
            new XgResearch { id = "sft", kind = XgResearchKind.Feel, name = "指令微调", nameEn = "Instruction tuning", cost = 25000, effect = "教它听懂吩咐。预训练 +5%", effectEn = "Teach it to follow requests. Pre-training +5%" },
            new XgResearch { id = "cot", kind = XgResearchKind.Feel, name = "思维链", nameEn = "Chain of thought", cost = 40000, effect = "让它一步步想。预训练 +5%", effectEn = "Make it think step by step. Pre-training +5%" },
            new XgResearch { id = "bias", kind = XgResearchKind.ManualPay, name = "偏置", nameEn = "Bias", cost = 20, effect = "判断门槛可以挪了。准确率 +2%", effectEn = "The yes-bar can move. Accuracy +2%" },
            new XgResearch { id = "weights", kind = XgResearchKind.ManualPay, name = "权重", nameEn = "Weights", cost = 15, effect = "每个输入有自己的分量。训练 ×1.1", effectEn = "Each input gets its own weight. Training ×1.1" },
            new XgResearch { id = "learnrule", kind = XgResearchKind.ManualPay, name = "感知机学习规则", nameEn = "Perceptron learning rule", cost = 40, effect = "答错就往对的方向挪一点。训练 ×1.15", effectEn = "Wrong? Nudge toward right. Training ×1.15" },
            new XgResearch { id = "step", kind = XgResearchKind.ManualPay, name = "阶跃激活", nameEn = "Step activation", cost = 30, effect = "过线是，不过线否。准确率 +1%", effectEn = "Past the line: yes. Otherwise no. Accuracy +1%" },
            new XgResearch { id = "sigmoid", kind = XgResearchKind.ManualPay, name = "Sigmoid", nameEn = "Sigmoid", cost = 120, effect = "开关换成平滑曲线，误差能往回传。准确率 +1%", effectEn = "A smooth curve, so errors flow back. Accuracy +1%" },
            new XgResearch { id = "backprop", kind = XgResearchKind.ManualPay, name = "小批量训练", nameEn = "Mini-batch training", cost = 200, effect = "一次看一批卡再改。训练 ×1.2", effectEn = "Look at a batch, then update. Training ×1.2" },
            new XgResearch { id = "chainrule", kind = XgResearchKind.ManualPay, name = "自动求导", nameEn = "Automatic differentiation", cost = 250, effect = "梯度让框架自己算。训练 ×1.1", effectEn = "The framework does the gradients. Training ×1.1" },
            new XgResearch { id = "features", kind = XgResearchKind.ManualPay, name = "特征工程", nameEn = "Feature engineering", cost = 60,
                effect = "人替它整理线索，感知机也能做逻辑题。每轮卡数减半",
                effectEn = "People hand-build the clues, so a perceptron can do logic. Half the cards per round" },
            new XgResearch { id = "irnn", kind = XgResearchKind.ManualPay, name = "单位初始化", nameEn = "Identity initialisation", cost = 1200,
                effect = "RNN 的记忆不再一个字一个字漏掉。要配梯度裁剪",
                effectEn = "RNN memory stops leaking word by word. Pair with clipping" },
            new XgResearch { id = "position", kind = XgResearchKind.ManualPay, name = "位置标记", nameEn = "Position tags", cost = 6000, effect = "给每个字编号，不用循环也知道先后。预训练要用", effectEn = "Number every word, order without loops. Pre-training needs it" },
            new XgResearch { id = "warmup", kind = XgResearchKind.ManualPay, name = "学习率预热", nameEn = "Learning-rate warm-up", cost = 20000, effect = "换模型后学习率慢慢升，不容易崩。预训练要用", effectEn = "After a change, the rate climbs slowly. Pre-training needs it" },
            new XgResearch { id = "fp16", kind = XgResearchKind.ManualPay, name = "混合精度", nameEn = "Mixed precision", cost = 30000,
                effect = "用一半的位数算，显存省一半。预训练规模 ×2", effectEn = "Compute in half the bits, half the memory. Pre-training scale ×2" },
            new XgResearch { id = "relu", kind = XgResearchKind.ManualPay, name = "ReLU", nameEn = "ReLU", cost = 200, effect = "误差往回传不缩水，深网络练得动。训练 ×1.3", effectEn = "Errors don't shrink going back. Deep nets train. Training ×1.3" },
            new XgResearch { id = "momentum", kind = XgResearchKind.Optimizer, name = "动量 SGD", nameEn = "Momentum SGD", cost = 3, speed = 1.3, stability = 1.4,
                effect = "带着惯性往前冲。训练 ×1.3", effectEn = "Rolls on with momentum. Training ×1.3" },
            new XgResearch { id = "gradclip", kind = XgResearchKind.GradClip, name = "梯度裁剪", nameEn = "Gradient clipping", cost = 5,
                effect = "每一步不许迈太大，循环网络不再炸", effectEn = "No step too big. Loops stop blowing up" },
            new XgResearch { id = "dropout", kind = XgResearchKind.Dropout, name = "Dropout", nameEn = "Dropout", cost = 10,
                effect = "随机关掉一些神经元，不让它死记硬背", effectEn = "Randomly switch some off. No rote memorising" },
            new XgResearch { id = "augment", kind = XgResearchKind.Augment, name = "数据增强", nameEn = "Data augmentation", cost = 25,
                effect = "图片挪一挪、转一转，一张变几张", effectEn = "Shift and turn pictures: one becomes several" },
            new XgResearch { id = "dataclean", kind = XgResearchKind.Augment, name = "数据清洗", nameEn = "Data cleaning", cost = 800,
                effect = "杂包和众包的错标减半", effectEn = "Half the wrong labels in junk packs and crowd work" },
            new XgResearch { id = "rmsprop", kind = XgResearchKind.Optimizer, name = "RMSProp", nameEn = "RMSProp", cost = 30, speed = 1.6, stability = 2,
                effect = "每个参数自己定步子。训练 ×1.6", effectEn = "Each parameter sets its own step. Training ×1.6" },
            new XgResearch { id = "lrschedule", kind = XgResearchKind.LrSchedule, name = "学习率衰减", nameEn = "LR schedule", cost = 50,
                effect = "先大步后小步，上限高一截", effectEn = "Big steps, then small. A higher ceiling" },
            new XgResearch { id = "batchnorm", kind = XgResearchKind.BatchNorm, name = "BatchNorm", nameEn = "BatchNorm", cost = 90,
                effect = "层数上限 +6，训练 ×1.2，更稳", effectEn = "Depth +6, training ×1.2, steadier" },
            new XgResearch { id = "adam", kind = XgResearchKind.Optimizer, name = "Adam", nameEn = "Adam", cost = 150, speed = 2, stability = 3,
                effect = "最省心的优化器。训练 ×2", effectEn = "The no-fuss optimiser. Training ×2" },
            new XgResearch { id = "cudnn", kind = XgResearchKind.CuDnn, name = "cuDNN 加速", nameEn = "cuDNN kernels", cost = 250,
                effect = "NVIDIA 的加速库。训练 ×1.3，电费 −30%", effectEn = "NVIDIA's speed-up library. Training ×1.3, power −30%" },
            new XgResearch { id = "cooler", kind = XgResearchKind.Cooler, name = "九州风神", nameEn = "Tower cooler", cost = 125,
                effect = "塔式散热器。烧卡概率减半", effectEn = "A tower cooler. Half the burn-outs" },
            new XgResearch { id = "transfer", kind = XgResearchKind.Transfer, name = "迁移学习", nameEn = "Transfer learning", cost = 400,
                effect = "换结构时，学过的底层知识带着走", effectEn = "Change structure, keep what the bottom layers learnt" },
        };

        /// <summary>Training unlocks once the run's dataset has this many samples.</summary>
        public const int SamplesToTrain = 4;
        /// <summary>Pay per correct hand label before the combo bonus.</summary>
        public const double LabelPay = .5;
        /// <summary>
        /// Hand labelling pays this many times <see cref="LabelPay"/> (every desk). Only hand answers (and the dragged
        /// spider's, which are hand answers) get it: automatic labels, contracts and the platform's fines keep their rates.
        /// </summary>
        public const double HandPayScale = 2.5;
        /// <summary>The 算术 desk is true/false: a wrong pick costs this fraction of a right answer's base pay.</summary>
        public const double ArithFine = .5;
        /// <summary>Every other desk: a wrong hand answer costs this fraction of a right answer's base pay (0 = free misses).</summary>
        public const double HandFine = .5;
        /// <summary>Auto-answering needs a deployed checkpoint at least this good.</summary>
        public const double AutoMinAccuracy = .6;
        public const int AutoMaxLevel = 10;
        /// <summary>Cards per second per auto-answer level (needs the GPUs running).</summary>
        public const double AutoRatePerLevel = .15;
        public static double AutoCost(int level) { return 60 * Math.Pow(1.7, level); }

        /// <summary>Lines for the sequence cards (public-domain Tang poems).</summary>
        public static readonly string[] PoemLines =
        {
            "床前明月光", "疑是地上霜", "举头望明月", "低头思故乡", "白日依山尽", "黄河入海流", "欲穷千里目", "更上一层楼",
            "春眠不觉晓", "处处闻啼鸟", "夜来风雨声", "花落知多少", "红豆生南国", "春来发几枝", "千山鸟飞绝", "万径人踪灭",
            "孤舟蓑笠翁", "独钓寒江雪", "国破山河在", "城春草木深", "海上生明月", "天涯共此时", "离离原上草", "一岁一枯荣",
            "野火烧不尽", "春风吹又生", "两个黄鹂鸣翠柳", "一行白鹭上青天", "窗含西岭千秋雪", "门泊东吴万里船",
            "独在异乡为异客", "每逢佳节倍思亲", "朝辞白帝彩云间", "千里江陵一日还", "两岸猿声啼不住", "轻舟已过万重山",
        };

        /// <summary>A poem of <see cref="PoemLines"/>: its lines are PoemLines[first .. first + count).</summary>
        public sealed class XgPoem
        {
            public string title, titleEn, author, authorEn;
            public int first, count;
        }

        /// <summary>Which poem each line comes from, so the 唐诗 desk can show the title and the other half of the couplet.</summary>
        public static readonly XgPoem[] Poems =
        {
            new XgPoem { title = "静夜思", titleEn = "Quiet Night Thoughts", author = "李白", authorEn = "Li Bai", first = 0, count = 4 },
            new XgPoem { title = "登鹳雀楼", titleEn = "On the Stork Tower", author = "王之涣", authorEn = "Wang Zhihuan", first = 4, count = 4 },
            new XgPoem { title = "春晓", titleEn = "Spring Dawn", author = "孟浩然", authorEn = "Meng Haoran", first = 8, count = 4 },
            new XgPoem { title = "相思", titleEn = "Longing", author = "王维", authorEn = "Wang Wei", first = 12, count = 2 },
            new XgPoem { title = "江雪", titleEn = "River Snow", author = "柳宗元", authorEn = "Liu Zongyuan", first = 14, count = 4 },
            new XgPoem { title = "春望", titleEn = "Spring View", author = "杜甫", authorEn = "Du Fu", first = 18, count = 2 },
            new XgPoem { title = "望月怀远", titleEn = "Gazing at the Moon", author = "张九龄", authorEn = "Zhang Jiuling", first = 20, count = 2 },
            new XgPoem { title = "赋得古原草送别", titleEn = "Grass on the Ancient Plain", author = "白居易", authorEn = "Bai Juyi", first = 22, count = 4 },
            new XgPoem { title = "绝句", titleEn = "Quatrain", author = "杜甫", authorEn = "Du Fu", first = 26, count = 4 },
            new XgPoem { title = "九月九日忆山东兄弟", titleEn = "Thinking of My Brothers on the Double Ninth", author = "王维", authorEn = "Wang Wei", first = 30, count = 2 },
            new XgPoem { title = "早发白帝城", titleEn = "Leaving Baidi at Dawn", author = "李白", authorEn = "Li Bai", first = 32, count = 4 },
        };

        /// <summary>The poem a line belongs to and the other line of its couplet (null if the line is unknown).</summary>
        public static XgPoem PoemOf(string line, out string partner, out bool lineFirst)
        {
            partner = ""; lineFirst = true;
            int index = Array.IndexOf(PoemLines, line);
            if (index < 0) return null;
            foreach (var poem in Poems)
            {
                if (index < poem.first || index >= poem.first + poem.count) continue;
                int k = index - poem.first;
                lineFirst = k % 2 == 0;
                int other = poem.first + (lineFirst ? k + 1 : k - 1);
                if (other >= poem.first && other < poem.first + poem.count) partner = PoemLines[other];
                return poem;
            }
            return null;
        }

        public static XgArch Arch(string id) { foreach (var a in Archs) if (a.id == id) return a; return null; }
        public static XgDataset Dataset(string id) { foreach (var d in Datasets) if (d.id == id) return d; return null; }
        public static XgContract Contract(string id) { foreach (var c in Contracts) if (c.id == id) return c; return null; }
        public static XgResearch ResearchItem(string id) { foreach (var r in Research) if (r.id == id) return r; return null; }

        public static List<XgArch> ArchsFor(XgTrack track)
        { var list = new List<XgArch>(); foreach (var a in Archs) if (a.shared || a.track == track) list.Add(a); return list; }

        public static List<XgDataset> DatasetsFor(XgTrack track)
        { var list = new List<XgDataset>(); foreach (var d in Datasets) if (d.track == track) list.Add(d); return list; }

        public static string Pick(bool english, string zh, string en) { return english && !string.IsNullOrEmpty(en) ? en : zh; }

        public static void Validate()
        {
            var ids = new HashSet<string>();
            foreach (var a in Archs) if (!ids.Add(a.id)) throw new InvalidOperationException("dup " + a.id);
            foreach (var d in Datasets) if (!ids.Add(d.id)) throw new InvalidOperationException("dup " + d.id);
            foreach (var c in Contracts) { if (!ids.Add(c.id)) throw new InvalidOperationException("dup " + c.id); if (Dataset(c.dataset) == null) throw new InvalidOperationException("contract dataset " + c.id); }
            foreach (var r in Research) if (!ids.Add(r.id)) throw new InvalidOperationException("dup " + r.id);
            var nodes = new HashSet<string>();
            foreach (var n in Nodes)
            {
                if (!nodes.Add(n.id)) throw new InvalidOperationException("dup node " + n.id);
                if (n.parent != null && Node(n.parent) == null) throw new InvalidOperationException("node parent " + n.id);
                foreach (var need in n.needs) if (Node(need) == null) throw new InvalidOperationException("node needs " + n.id);
                if (n.kind == XgNodeKind.Arch && Arch(n.target) == null) throw new InvalidOperationException("node arch " + n.id);
                if (n.kind == XgNodeKind.Dataset && Dataset(n.target) == null) throw new InvalidOperationException("node data " + n.id);
                if (n.kind == XgNodeKind.Research && ResearchItem(n.target) == null) throw new InvalidOperationException("node research " + n.id);
                // A parent chain must end at a root: no cycles.
                var seen = new HashSet<string>(); for (var p = n; p != null; p = p.parent == null ? null : Node(p.parent)) if (!seen.Add(p.id)) throw new InvalidOperationException("node cycle " + n.id);
            }
            foreach (var r in Research) if (Node(r.id) == null) throw new InvalidOperationException("research without node " + r.id);
            foreach (var a in Archs) if (Node(a.id) == null) throw new InvalidOperationException("arch without node " + a.id);
            foreach (var d in Desks) { var data = Dataset(d.id); if (data == null || !data.handLabel) throw new InvalidOperationException("desk dataset " + d.id); }
        }
    }
}
