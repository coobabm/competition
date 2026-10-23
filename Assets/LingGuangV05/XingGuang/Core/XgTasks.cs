using System;
using System.Collections.Generic;
using LingGuangV05.Core;

namespace LingGuangV05.XingGuang
{
    /// <summary>How a task's progress reads: a count (3 / 9), a percentage (41% / 60%) or money (¥250 / ¥1,000).</summary>
    public enum XgTaskUnit { None, Count, Percent, Money }

    /// <summary>What a task looks at: the lab and, when the desktop has it, the household (cards, rent).</summary>
    public sealed class XgTaskContext
    {
        public XgSim sim;
        /// <summary>The household simulation, or null (tests, a lab without a desktop): household tasks then never trigger.</summary>
        public ChapterOneSim house;
        public XgTaskContext(XgSim sim, ChapterOneSim house = null) { this.sim = sim; this.house = house; }
    }

    /// <summary>
    /// One task of the 任务 queue (2026-10-21, replaces the sticky to-do note): when it appears, when it is done, how far
    /// along it is and where 「去看看」 goes. Every predicate is a pure read of saved state; a task never awards, completes
    /// or changes anything in the game.
    /// </summary>
    public sealed class XgTaskDef
    {
        public string id, zh, en, bodyZh, bodyEn;
        /// <summary>The ability stage the task belongs to (1–6): it expires once the lab is two stages past it.</summary>
        public int stage;
        public Func<XgTaskContext, bool> trigger, done;
        public Func<XgTaskContext, double> current;
        public double goal;
        public XgTaskUnit unit;
        /// <summary>Where 「去看看」 goes (an <see cref="XgGuideStep"/> target); app "" = no button.</summary>
        public string app = "", tab = "", target = "";

        public string Title(bool english) => english ? en : zh;
        public string Body(bool english) => english ? bodyEn : bodyZh;
        public bool HasProgress => current != null && goal > 0;

        /// <summary>"3 / 9", "41% / 60%" or "¥250 / ¥1,000" (empty without a goal).</summary>
        public string ProgressText(XgTaskContext c)
        {
            if (!HasProgress) return "";
            double now = Math.Max(0, Math.Min(goal, current(c)));
            switch (unit)
            {
                case XgTaskUnit.Percent: return Math.Floor(now) + "% / " + goal + "%";
                case XgTaskUnit.Money: return "¥" + now.ToString("#,0") + " / ¥" + goal.ToString("#,0");
                default: return Math.Floor(now) + " / " + goal;
            }
        }

        public double Fraction(XgTaskContext c) => HasProgress ? Math.Max(0, Math.Min(1, current(c) / goal)) : 0;

        /// <summary>The note's own step for 「去看看」 (GuideNote.Follow), or null when the task points nowhere.</summary>
        public XgGuideStep Jump() => string.IsNullOrEmpty(app) ? null
            : new XgGuideStep { id = "task." + id, kind = XgGuideKind.Main, zh = zh, en = en, whyZh = bodyZh, whyEn = bodyEn, app = app, tab = tab, target = target };
    }

    public enum XgTaskEventKind { Arrived, Queued, Completed }

    public struct XgTaskEvent
    {
        public XgTaskEventKind kind;
        public XgTaskDef task;
        public XgTaskEvent(XgTaskEventKind kind, XgTaskDef task) { this.kind = kind; this.task = task; }
    }

    /// <summary>The 任务 queue in the save: shown tasks (at most <see cref="XgSim.TaskCapacity"/>), waiting ones, finished ones.</summary>
    public sealed partial class XgState
    {
        public List<string> taskActive = new List<string>(), taskWaiting = new List<string>(), taskDone = new List<string>();
        /// <summary>0 = this save has never been read by the queue (an old save is filled silently, with no notices).</summary>
        public int taskVersion;
    }

    /// <summary>
    /// The task list, about six to twelve per ability stage, in the order they should reach the player. Text is short
    /// (title ≤ 8, body ≤ 22 characters) so the 380-wide panel never wraps more than once.
    /// </summary>
    public static class XgTaskBook
    {
        static readonly List<XgTaskDef> all = new List<XgTaskDef>();
        static readonly Dictionary<string, XgTaskDef> byId = new Dictionary<string, XgTaskDef>(StringComparer.Ordinal);
        public static IReadOnlyList<XgTaskDef> All => all;
        public static XgTaskDef Find(string id) => id != null && byId.TryGetValue(id, out var d) ? d : null;

        static double BestAcc(XgSim s) { double b = 0; if (s.S.best != null) foreach (var x in s.S.best) if (x != null && x.acc > b) b = x.acc; return b; }
        static bool Stage(XgTaskContext c, int n) => c.sim.S.stage >= n;
        static double TodayDeskIncome(XgTaskContext c) => c.sim.S.dayStatsDay == c.sim.Today ? c.sim.S.dayIncome : 0;
        static int PassedChecks(XgTaskContext c) => Math.Max(0, c.sim.S.qcCheckedTotal - c.sim.S.qcFailedTotal);

        static XgTaskDef Add(int stage, string id, string zh, string en, string bodyZh, string bodyEn,
            Func<XgTaskContext, bool> trigger, Func<XgTaskContext, bool> done, string app = "", string tab = "", string target = "")
        {
            var d = new XgTaskDef { stage = stage, id = id, zh = zh, en = en, bodyZh = bodyZh, bodyEn = bodyEn, trigger = trigger, done = done, app = app, tab = tab, target = target };
            all.Add(d); byId.Add(id, d);
            return d;
        }

        static XgTaskDef Goal(this XgTaskDef d, Func<XgTaskContext, double> current, double goal, XgTaskUnit unit = XgTaskUnit.Count)
        { d.current = current; d.goal = goal; d.unit = unit; return d; }

        static XgTaskDef Node(int stage, string id, string node, string zh, string en, string bodyZh, string bodyEn, Func<XgTaskContext, bool> trigger = null)
            => Add(stage, id, zh, en, bodyZh, bodyEn, trigger ?? (c => Stage(c, stage)), c => c.sim.Has(node), XgGuide.Lab, "tree", "node:" + node);

        static XgTaskBook()
        {
            const string Lab = XgGuide.Lab;
            // ───── 阶段 1 · 学会简单判断 ─────
            Add(1, "tree.open", "打开科技树", "Open the tech tree", "买下「训练曲线」，练页就开了。", "Buy Training curve to open the Train page.",
                c => c.sim.TeachingAnswers >= 12, c => c.sim.Has("train.curve"), Lab, "tree", "node:train.curve");
            Add(1, "train.first", "训练一轮", "Train one round", "去练页按一次「训练一轮」。", "Press Train 1 epoch on the Train page.",
                c => c.sim.Has("train.curve"), c => c.sim.S.epochs >= 1, Lab, "train", "label:训练一轮|Train 1 epoch");
            Add(1, "streak.three", "连对 3 题", "Three in a row", "连对两题后，第三题报酬翻倍。", "After two right in a row, the third pays double.",
                c => c.sim.TeachingAnswers >= 12, c => c.sim.S.bestCombo >= 3, Lab, "teach").Goal(c => c.sim.S.bestCombo, 3);
            Add(1, "assess.first", "评估一次", "Assess once", "看看它现在能考几分。", "See what it scores now.",
                c => c.sim.S.epochs >= 1, c => c.sim.S.assessments >= 1, Lab, "train", "label:评估|Assess");
            Node(1, "label.ai", "label.ai", "让它帮你看题", "Let it look first", "每题先看看它怎么答。", "See its answer on every card.",
                c => c.sim.TeachingAnswers >= 24 && c.sim.S.epochs >= 1);
            Add(1, "raise.two", "加两次薪", "Two raises", "每道题多赚一点。", "Earn a little more per answer.",
                c => c.sim.TeachingAnswers >= 30, c => c.sim.RaiseLevel >= 2, Lab, "teach", "name:Row薪").Goal(c => c.sim.RaiseLevel, 2);
            Add(1, "labels.hundred", "教满 100 题", "Teach 100 cards", "每答一题，就是一条样本。", "Every answer is one more sample.",
                c => c.sim.TeachingAnswers >= 12, c => c.sim.TotalLabels >= 100, Lab, "teach").Goal(c => c.sim.TotalLabels, 100);
            Add(1, "ckpt.sixty", "练到 60%", "Reach 60%", "任一题库到 60%，它就能自己答题。", "At 60% on any desk it can answer by itself.",
                c => c.sim.S.assessments >= 1, c => BestAcc(c.sim) >= .6, Lab, "train").Goal(c => BestAcc(c.sim) * 100, 60, XgTaskUnit.Percent);
            Node(1, "auto.unlock", "label.auto", "让它自己干", "Let it work alone", "买下「自动答题」，关掉窗口它也在答。", "Buy Auto answering; it keeps going with the window closed.",
                c => !c.sim.AutoLabelHidden || BestAcc(c.sim) >= .6);
            Add(1, "streak.nine", "五倍报酬", "Five times the pay", "连对 9 题，每题都是五倍。", "Nine in a row and every answer pays ×5.",
                c => c.sim.S.bestCombo >= 3, c => c.sim.S.bestCombo >= 9, Lab, "teach").Goal(c => c.sim.S.bestCombo, 9);
            Node(1, "mlp", "mlp", "多层感知机", "Multilayer perceptron", "把简单线索组合起来。", "Combine simple clues.",
                c => c.sim.Has("train.curve"));
            Add(1, "grade.first", "拿到第一个等级", "First grade", "评估到 C 级就有一笔奖金。", "Grade C on a dataset pays a bonus.",
                c => c.sim.S.epochs >= 1, c => c.sim.S.grades != null && c.sim.S.grades.Count >= 1, Lab, "train", "label:评估|Assess");

            // ───── 阶段 2 · 学会组合特征 ─────
            Add(2, "stage.two", "通过能力考", "Pass the ability exam", "两条进度攒满，去考一次。", "Fill both bars, then sit the exam.",
                c => c.sim.S.assessments >= 1, c => Stage(c, 2), Lab, "train", "name:ConfirmNextStage");
            Add(2, "specialty", "选个专长", "Pick a specialty", "看图还是读字，先选一条路。", "Pictures or words: pick a road first.",
                c => Stage(c, 2), c => !string.IsNullOrEmpty(c.sim.S.firstSpecialty), Lab, "tree");
            Add(2, "models.three", "存三个模型", "Save three models", "练好的版本存起来，随时能回去。", "Save good versions to return to.",
                c => Stage(c, 2) && c.sim.S.epochs > 0, c => c.sim.S.models != null && c.sim.S.models.Count >= 3, Lab, "train").Goal(c => c.sim.S.models == null ? 0 : c.sim.S.models.Count, 3);
            Add(2, "pack.public", "买一个公开数据包", "Buy a public dataset", "现成的题库，买来就能练。", "A ready-made dataset to train on.",
                c => Stage(c, 2), c => c.sim.S.owned != null && c.sim.S.owned.Count >= 1, Lab, "tree");
            Node(2, "wider", "v.w2", "把网络加宽", "Widen the network", "每层多装一些线索。", "Hold more clues per layer.");
            Add(2, "auto.three", "自动答题 3 级", "Auto answering Lv 3", "升到 3 级，后台赚得更多。", "Level 3 earns more in the background.",
                c => c.sim.GlobalAutoLevel >= 1, c => c.sim.GlobalAutoLevel >= 3, Lab, "tree", "node:label.auto").Goal(c => c.sim.GlobalAutoLevel, 3);
            Add(2, "gpu.two", "加第二张显卡", "Add a second card", "两张卡一起训练，快一倍。", "Two cards train twice as fast.",
                c => c.house != null && Stage(c, 2), c => c.house != null && c.house.S.gpuCount >= 2, XgGuide.Shop);
            Add(2, "book.first", "读完一本书", "Finish a book", "读给它听，它会记住。", "Read to it and it remembers.",
                c => c.sim.LibraryOpen, c => c.sim.BooksDone().Count >= 1, Lab, "train", "name:BooksTab");
            Add(2, "board.first", "和它下盘棋", "Play it a game", "游戏中心里，它会跟你下。", "It plays you in the Game Hub.",
                c => c.sim.BoardAiAvailable, c => c.sim.S.games != null && c.sim.S.games.Exists(g => g != null && g.played > 0), XgGuide.Games);
            Add(2, "daily.first", "学一项生活技能", "Learn a life skill", "时事、冲浪、概率，挑一个。", "News, surfing or odds: pick one.",
                c => Stage(c, 2), c => c.sim.Has("daily.dice") || c.sim.Has("daily.news") || c.sim.Has("daily.surf"), Lab, "tree");

            // ───── 阶段 3 · 走向专长 ─────
            Add(3, "stage.three", "进入第 3 阶段", "Reach stage 3", "再考一次能力考。", "Sit the next ability exam.",
                c => Stage(c, 2), c => Stage(c, 3), Lab, "train", "name:ConfirmNextStage");
            Node(3, "alexnet", "alexnet", "深层卷积", "Deep CNN", "认更复杂的图形组合。", "Recognise more complex shapes.");
            Node(3, "lstm", "lstm", "门控记忆", "Gated memory", "让重要的线索多留一会。", "Keep useful clues longer.");
            Add(3, "data.install", "装一个数据包", "Install a data pack", "把资料夹里的数据包拖进练页。", "Drag a data pack into the Train page.",
                c => c.sim.SystemUnlocked("data"), c => c.sim.S.dataFiles != null && c.sim.S.dataFiles.Exists(f => f != null && f.installed), Lab, "train");
            Add(3, "crowd.first", "发一次众包", "Post a crowd task", "花一点钱，请别人帮你标。", "Pay others to label for you.",
                c => c.sim.SystemUnlocked("crowd"), c => c.sim.S.crowdOn != null && c.sim.S.crowdOn.Count >= 1, Lab, "train");
            Add(3, "contract.first", "签第一份订单", "Sign a first order", "去摆渡众包签一份企业订单。", "Sign a company order in Bodu Crowd.",
                c => Stage(c, 3), c => c.sim.S.contracts != null && c.sim.S.contracts.Count >= 1, XgGuide.Crowd, "contracts");
            Add(3, "crawl.first", "让爬虫跑一次", "Run the crawler", "从网上收集一批词语。", "Gather words from the web.",
                c => c.sim.S.stage >= XgSim.CrawlStage, c => c.sim.S.crawlRuns >= 1, Lab, "train");
            Add(3, "spider.ten", "蜘蛛答 10 题", "Spider answers 10", "让桌面上的蜘蛛帮你答题。", "Let the desktop spider answer.",
                c => c.sim.SystemUnlocked("data"), c => c.sim.S.spiderAnswers >= 10, XgGuide.Crowd, "label").Goal(c => c.sim.S.spiderAnswers, 10);
            Add(3, "chat.ten", "和它聊 10 句", "Ten lines with it", "它学会说话了，跟它聊聊。", "It can talk now. Chat with it.",
                c => Stage(c, 3), c => c.sim.S.chatTurns >= 10, XgGuide.YY, XgGuide.YYChat).Goal(c => c.sim.S.chatTurns, 10);
            Add(3, "rent.on.time", "按时交房租", "Pay rent on time", "从这阶段起要交房租了。", "Rent is due from this stage on.",
                c => c.house != null && Stage(c, 3), c => c.house != null && c.house.S.rentOnTimePeriods >= 1, XgGuide.Home);
            Add(3, "income.day", "今日答题千元", "Earn ¥1,000 at the desk today", "只算今天手动和蜘蛛答题的净收入。", "Today's net income from manual and spider answers only.",
                c => Stage(c, 3), c => TodayDeskIncome(c) >= 1000, Lab, "teach").Goal(TodayDeskIncome, 1000, XgTaskUnit.Money);

            // ───── 阶段 4 · 学会保留信息 ─────
            Add(4, "stage.four", "进入第 4 阶段", "Reach stage 4", "再考一次能力考。", "Sit the next ability exam.",
                c => Stage(c, 3), c => Stage(c, 4), Lab, "train", "name:ConfirmNextStage");
            Node(4, "resnet", "resnet", "残差网络", "Residual network", "给信息开一条近道。", "Give signals a shortcut.");
            Node(4, "attention", "attention", "回看注意力", "Look-back attention", "说话时回头看原文。", "Look back at the source.");
            Node(4, "batchnorm", "batchnorm", "批归一化", "Batch norm", "把一批信号调齐。", "Align a batch of signals.");
            Node(4, "label.brain", "label.brain", "本地判题", "Local judging", "文字题让本地模型来判。", "Let the local model judge text.");
            Add(4, "contracts.three", "签满 3 份订单", "Three orders", "订单越多，被动收入越高。", "More orders, more passive income.",
                c => Stage(c, 4), c => c.sim.S.contracts != null && c.sim.S.contracts.Count >= 3, XgGuide.Crowd, "contracts").Goal(c => c.sim.S.contracts == null ? 0 : c.sim.S.contracts.Count, 3);
            Add(4, "qc.twenty", "通过 20 次抽检", "Pass 20 spot checks", "平台会抽查你的答案。", "The platform spot-checks answers.",
                c => c.sim.QualityActive, c => PassedChecks(c) >= 20, XgGuide.Crowd, "label").Goal(c => PassedChecks(c), 20);
            Add(4, "worker", "雇个帮手", "Hire a helper", "转包给阿杰他们，多一份产出。", "Subcontract to A-Jie and friends.",
                c => c.sim.SubcontractReady, c => c.sim.S.scWorkers != null && c.sim.S.scWorkers.Exists(w => w != null && w.labelsTotal > 0), XgGuide.Crowd, "subcontract");
            Add(4, "forget.night", "度过忘言夜", "Through Forget Night", "那一晚，问问它还记得什么。", "That night, ask what it remembers.",
                c => c.sim.OfferForgetNight || c.sim.S.fnDone, c => c.sim.S.fnDone, XgGuide.YY, XgGuide.YYChat);
            Add(4, "book.question", "回答书里的问题", "Answer a book question", "它读书时会问你问题。", "It asks you about what it reads.",
                c => c.sim.S.stage >= XgSim.BookAskStage, c => c.sim.S.bookAnswers != null && c.sim.S.bookAnswers.Count >= 1, XgGuide.YY, XgGuide.YYChat);
            Add(4, "streak.twenty", "渐入佳境", "Finding your rhythm", "连对 20 题。", "Twenty right in a row.",
                c => c.sim.S.bestCombo >= 9, c => c.sim.S.bestCombo >= 20, Lab, "teach").Goal(c => c.sim.S.bestCombo, 20);

            // ───── 阶段 5 · 学会寻找重点 ─────
            Add(5, "stage.five", "进入第 5 阶段", "Reach stage 5", "再考一次能力考。", "Sit the next ability exam.",
                c => Stage(c, 4), c => Stage(c, 5), Lab, "train", "name:ConfirmNextStage");
            Add(5, "attention.five", "注意力练 5 轮", "Five attention rounds", "让它学会先看重点。", "Teach it to look at what matters.",
                c => c.sim.FeatureVisible("attention"), c => c.sim.S.attentionEpochs >= 5, Lab, "train").Goal(c => c.sim.S.attentionEpochs, 5);
            Node(5, "transformer", "transformer", "自研新脑", "Own new brain", "整句一起看，训练能并行。", "Read the whole sentence at once.",
                c => Stage(c, 5) && c.sim.Has("attention"));
            Node(5, "caption", "caption", "图像描述", "Image captions", "给图像配一句话。", "Describe a picture.");
            Add(5, "logs.hundred", "处理 100 条日志", "Label 100 user logs", "订单带回来的用户日志也是数据。", "Logs from orders are data too.",
                c => c.sim.S.stage >= XgSim.FlywheelStage && c.sim.S.contracts != null && c.sim.S.contracts.Count > 0,
                c => c.sim.S.logsHand + c.sim.S.logsAuto >= 100, Lab, "train").Goal(c => c.sim.S.logsHand + c.sim.S.logsAuto, 100);
            Add(5, "memory.five", "记满 5 页回忆", "Five memory pages", "它开始给自己写回忆了。", "It has started a memory book.",
                c => c.sim.MemoryOpen, c => c.sim.S.memoryBook != null && c.sim.S.memoryBook.Count >= 5, XgGuide.YY, XgGuide.YYChat).Goal(c => c.sim.S.memoryBook == null ? 0 : c.sim.S.memoryBook.Count, 5);
            Add(5, "yy.ten", "YY 上聊 10 次", "Ten YY replies", "在 YY 里多跟它说说话。", "Talk to it more on YY.",
                c => Stage(c, 5), c => c.sim.S.yyReplySerial >= 10, XgGuide.YY, XgGuide.YYChat).Goal(c => c.sim.S.yyReplySerial, 10);
            Add(5, "selfplay", "让它自己下棋", "Let it practise", "夜里它会跟自己下几局。", "At night it plays itself.",
                c => c.sim.Has("board.logic4"), c => c.sim.S.boardRounds != null && c.sim.S.boardRounds.Exists(r => r != null && r.selfPlay), XgGuide.Games);

            // ───── 阶段 6 · 重构学习方式 ─────
            Add(6, "stage.six", "进入第 6 阶段", "Reach stage 6", "最后一次能力考。", "The last ability exam.",
                c => Stage(c, 5), c => Stage(c, 6), Lab, "train", "name:ConfirmNextStage");
            Node(6, "datacenter", "datacenter", "租个机柜", "Rent a rack", "给大训练找个机位。", "Make room for a big run.");
            Add(6, "pretrain.scale", "凑够预训练规模", "Reach pre-training scale", "参数、数据、道具一起凑够。", "Parameters, data and items together.",
                c => Stage(c, 6), c => c.sim.PretrainScaleReady, Lab, "final");
            Add(6, "pretrain.start", "开始预训练", "Start pre-training", "这一次，练一个更大的它。", "This time, train a bigger one.",
                c => c.sim.PretrainScaleReady, c => c.sim.S.pretrainRunning || c.sim.S.pretrain > 0, Lab, "final", "label:开始预训练|Start pre-training");
            Add(6, "align", "对齐 12 张卡", "Align twelve cards", "告诉它什么该说、什么不该。", "Tell it what to say and what not to.",
                c => c.sim.AlignmentOpen, c => c.sim.S.alignDone >= XgSim.AlignCards, Lab, "final").Goal(c => c.sim.S.alignDone, XgSim.AlignCards);
            Add(6, "final.exam", "期末考 6 题", "Final exam", "最后一次，考考它。", "One last test.",
                c => c.sim.EndingOpen, c => c.sim.S.examDone >= XgSim.ExamQuestions, Lab, "final", "label:出第|Question").Goal(c => c.sim.S.examDone, XgSim.ExamQuestions);
            Add(6, "letter", "读它的信", "Read its letter", "它给你写了一封信。", "It wrote you a letter.",
                c => c.sim.LetterReady, c => c.sim.S.letterRead, Lab, "final");
            Add(6, "finale.talk", "最后的谈话", "The last talk", "再陪它说一会儿话。", "Talk with it a little longer.",
                c => c.sim.FinaleTalkOpen, c => c.sim.FinaleTalkDone, XgGuide.YY, XgGuide.YYChat);
        }
    }

    public sealed partial class XgSim
    {
        /// <summary>At most this many tasks are shown; the rest wait their turn.</summary>
        public const int TaskCapacity = 5;
        /// <summary>A task still open this many stages after its own is withdrawn (it no longer helps the player).</summary>
        public const int TaskExpiresAfterStages = 2;

        void PrepareTasks()
        {
            if (S.taskActive == null) S.taskActive = new List<string>();
            if (S.taskWaiting == null) S.taskWaiting = new List<string>();
            if (S.taskDone == null) S.taskDone = new List<string>();
            S.taskActive.RemoveAll(id => XgTaskBook.Find(id) == null);
            S.taskWaiting.RemoveAll(id => XgTaskBook.Find(id) == null);
        }

        public bool TaskDone(string id) => S.taskDone != null && S.taskDone.Contains(id);
        public IReadOnlyList<string> TasksShown { get { PrepareTasks(); return S.taskActive; } }
        public int TasksWaiting { get { PrepareTasks(); return S.taskWaiting.Count; } }

        /// <summary>
        /// Moves the queue on: finished tasks are marked (they stay shown, ticked, until <see cref="ClearFinishedTasks"/>),
        /// tasks too far behind the stage are withdrawn, newly triggered ones join (shown while there is room, otherwise
        /// waiting) and waiting ones fill free places. Returns what happened, for the notices. The first read of a save is
        /// silent: what is already done is recorded as done and the rest is placed without notices.
        /// </summary>
        public List<XgTaskEvent> TickTasks(ChapterOneSim house = null)
        {
            PrepareTasks();
            var events = new List<XgTaskEvent>();
            var c = new XgTaskContext(this, house);
            bool quiet = S.taskVersion < 1;
            var arrivals = new List<XgTaskDef>();
            foreach (var def in XgTaskBook.All)
            {
                if (S.taskDone.Contains(def.id)) continue;
                bool shown = S.taskActive.Contains(def.id), waiting = S.taskWaiting.Contains(def.id);
                bool isDone = Safe(def.done, c);
                if (isDone)
                {
                    // Done before it was ever shown: recorded, never announced.
                    S.taskDone.Add(def.id); S.taskWaiting.Remove(def.id);
                    if (shown && !quiet) events.Add(new XgTaskEvent(XgTaskEventKind.Completed, def));
                    continue;
                }
                if ((shown || waiting) && S.stage - def.stage >= TaskExpiresAfterStages)
                {
                    S.taskActive.Remove(def.id); S.taskWaiting.Remove(def.id); S.taskDone.Add(def.id);
                    continue;
                }
                if (shown || waiting || S.stage - def.stage >= TaskExpiresAfterStages || !Safe(def.trigger, c)) continue;
                arrivals.Add(def);
            }
            // Existing waiting tasks have priority when stage withdrawal frees a place.
            Refill(events, quiet);
            foreach (var def in arrivals)
            {
                if (S.taskActive.Count < TaskCapacity) { S.taskActive.Add(def.id); if (!quiet) events.Add(new XgTaskEvent(XgTaskEventKind.Arrived, def)); }
                else { S.taskWaiting.Add(def.id); if (!quiet) events.Add(new XgTaskEvent(XgTaskEventKind.Queued, def)); }
            }
            S.taskVersion = 1;
            return events;
        }

        /// <summary>Takes the ticked tasks off the list (the panel was opened, or a few seconds passed) and lets waiting ones in.</summary>
        public List<XgTaskEvent> ClearFinishedTasks()
        {
            PrepareTasks();
            var events = new List<XgTaskEvent>();
            S.taskActive.RemoveAll(id => S.taskDone.Contains(id));
            Refill(events, false);
            return events;
        }

        void Refill(List<XgTaskEvent> events, bool quiet)
        {
            while (S.taskActive.Count < TaskCapacity && S.taskWaiting.Count > 0)
            {
                string id = S.taskWaiting[0]; S.taskWaiting.RemoveAt(0);
                S.taskActive.Add(id);
                if (!quiet) events.Add(new XgTaskEvent(XgTaskEventKind.Arrived, XgTaskBook.Find(id)));
            }
        }

        static bool Safe(Func<XgTaskContext, bool> f, XgTaskContext c)
        {
            if (f == null) return false;
            try { return f(c); } catch (Exception) { return false; } // a task never breaks the game
        }
    }
}
