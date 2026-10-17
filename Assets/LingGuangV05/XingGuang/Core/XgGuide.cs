using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>What a step is about. The sticky note only treats <see cref="Side"/> differently (never the main line).</summary>
    public enum XgGuideKind { Blocker, Setup, Main, Ability, Finale, Boost, Side, Done }

    /// <summary>
    /// One line of the to-do note: what to do, why, where it happens and how far along it is. The place is a plain
    /// description the desktop resolves: an app id, a tab inside it, a control locator and an argument.
    /// Control locators: "" (the tab itself), "name:Foo" (an object called Foo), "label:是|Yes" (a button whose
    /// text starts with one of these), "node:id" (a skill-tree node), "contract:id" (a contract row).
    /// </summary>
    public sealed class XgGuideStep
    {
        public string id = "", zh = "", en = "", whyZh = "", whyEn = "";
        public XgGuideKind kind;
        public string app = XgGuide.Lab, tab = "", target = "", arg = "";
        /// <summary>Escalation level for steps that get more direct over time.</summary>
        public int level;
        /// <summary>Progress towards the goal (goal 0 = none). Money progress is shown with ¥.</summary>
        public double current, goal;
        public bool money;

        public bool HasProgress => goal > 0;
        public string Text(bool english) => (english ? en : zh) + Progress(english);
        public string Why(bool english) => english ? whyEn : whyZh;

        public string Progress(bool english)
        {
            if (!HasProgress) return "";
            double now = Math.Max(0, Math.Min(goal, current));
            string a = money ? "¥" + Whole(now) : Whole(now), b = money ? "¥" + Whole(goal) : Whole(goal);
            return english ? " (" + a + "/" + b + ")" : "（" + a + "/" + b + "）";
        }

        static string Whole(double v) => Math.Floor(v + 1e-9).ToString("0", CultureInfo.InvariantCulture);
        public override string ToString() => id + ": " + Text(false);
    }

    /// <summary>The household facts the lab cannot see by itself (power, cards, which apps the player has opened).</summary>
    public sealed class XgGuideHouse
    {
        public bool appInstalled = true, unpaidPower, breakerTripped, noGpu, overloaded;
        public double billDue;
        /// <summary>The landlord cut the power for unpaid rent (the wallet itself says how much is owed).</summary>
        public bool landlordCut;
        /// <summary>VRAM the host offers (0 = unknown; the VRAM check is skipped).</summary>
        public double vramMB;
        /// <summary>Side places the player has already looked at.</summary>
        public bool forumSeen, yySeen, gamesSeen;
        /// <summary>周而复始 still answers private messages.</summary>
        public bool zhouAvailable = true;
    }

    /// <summary>A way to the next ability's parameter bar (XgGuide.ParamsPlan): what to buy, in order, and the model it gives.</summary>
    public sealed class XgParamsPlan
    {
        public XgTrack track;
        public string arch = "";
        public int depth, width;
        /// <summary>The model's parameters (thousands), and the threshold once the plan's items are owned.</summary>
        public double paramsK, threshold, cost;
        /// <summary>Nodes still to buy, prerequisites first (empty when the caps already allow the model).</summary>
        public readonly List<XgNode> buy = new List<XgNode>();
    }

    /// <summary>A buy towards the next ability's data bar (XgGuide.DataPlan): a data pack, or an item that lowers the threshold.</summary>
    public sealed class XgDataBuy
    {
        public XgNode target;
        /// <summary>The next batch of a pack already owned (dataset id; <see cref="target"/> is its pack node), or null.</summary>
        public string batch;
        /// <summary>Effective samples it adds (or takes off the threshold), and the price of the whole path.</summary>
        public double gain, cost;
        public readonly List<XgNode> path = new List<XgNode>();
    }

    /// <summary>
    /// The objective engine behind the desktop's to-do note: a pure reading of the game state into an ordered list
    /// of steps. Blockers come first (no power, no card, a model too big for the card), then getting the loop going
    /// (label, train, assess), then the stage's goal (sign what can be signed, fill the shorter bar towards the next
    /// ability, or the finale's chores), then cheap boosts. At most one optional side task is added, and only as the
    /// third line. It changes nothing.
    /// </summary>
    public static class XgGuide
    {
        public const string Lab = "lingguang", Home = "home", Shop = "xunbao", Tieba = "tieba", Bodu = "bodu", YY = "yy", Games = "games";
        /// <summary>The desktop itself (the prologue's last step: open the exe it left behind).</summary>
        public const string Desktop = "desktop";
        /// <summary>Its YY conversation (YYChatHub.LingGuangId, used as the YY tab): the chat with it moved there from the lab.</summary>
        public const string YYChat = "lingguang";
        /// <summary>摆渡众包: the 标注台 ("label") and 企业订单 ("contracts") pages moved there from the lab.</summary>
        public const string Crowd = "zhongbao";
        /// <summary>A node counts as cheap when it costs at most this share of the wallet; a raise at most the second.</summary>
        public const double CheapNodeShare = .5, CheapRaiseShare = .25;

        /// <summary>The steps for this moment, most urgent first. Never empty.</summary>
        public static List<XgGuideStep> Current(XgSim sim, double money, XgGuideHouse house = null)
        {
            var list = new List<XgGuideStep>();
            if (sim == null) return list;
            house = house ?? new XgGuideHouse();
            var wallet = new Wallet { money = money, vram = house.vramMB };

            Rent(money, house, list);
            Blockers(sim, house, list);
            if (!house.appInstalled)
            {
                list.Add(Step("install", XgGuideKind.Setup, "解压刚下载的文件，打开" + LingGuangV05.Core.AppNames.ExeZh, "Extract the downloaded file and open " + LingGuangV05.Core.AppNames.ExeEn,
                    "跟着桌面上的提示，和它第一次见面。", "Follow the desktop prompts to meet it.", "", "", "", ""));
                return Finish(list, sim, house);
            }
            // The 能力考 can be sat (or was passed): only the player turns the chapter, so it leads the note once a pass is
            // likely; with fuller bars still worth waiting for, it goes below the steps that fill them.
            bool examLater = ChapterWaits(sim) && !sim.OpeningFocus && !ExamLeads(sim);
            if (ChapterWaits(sim) && !sim.OpeningFocus && !examLater) list.Add(ConfirmStage(sim));
            if (!ChapterWaits(sim) && !sim.OpeningFocus && sim.S.stage < 6) ExamRetry(sim, list);

            // The first minutes (XgSim.Opening.cs): label 常识判断, train, then chat; nothing else until it learned yes from no.
            if (sim.OpeningFocus) { Opening(sim, list); return Finish(list, sim, house); }
            if (sim.OpeningChatDue) list.Add(OpeningChat());

            if (sim.S.stage >= 6) { Finale(sim, wallet, list); Contracts(sim, list); return Finish(list, sim, house); }

            bool trainable = sim.TrainingUnlocked(XgTrack.Vision) || sim.TrainingUnlocked(XgTrack.Sequence);
            if (!trainable) Label(sim, list);
            else if (sim.S.epochs == 0) Train(sim, list);
            else if (sim.S.assessments == 0) Assess(sim, list);

            Contracts(sim, list);
            var advancing = new HashSet<string>();
            if (trainable)
            {
                TransformerIdea(sim, list);
                NextAbility(sim, wallet, list, advancing);
                // A model that stopped improving gets the plain reason and where to fix it (XgChanceHint.cs). While the
                // next ability's steps already name what to buy or label, a plateau for parameters or data says nothing new.
                var hint = XgChanceHint.Step(sim);
                bool covered = hint != null && list.Exists(x => x.kind == XgGuideKind.Ability) && (hint.id == "plateau.params" || hint.id == "plateau.data");
                if (hint != null && !covered) list.Add(hint);
            }
            if (examLater) list.Add(ConfirmStage(sim));
            Books(sim, list);
            Quality(sim, list);
            AutoLabel(sim, wallet, list);
            CheapNode(sim, wallet, list, advancing);
            Raise(sim, money, list);
            return Finish(list, sim, house);
        }

        /// <summary>Both bars this full lead with the exam (its pass odds are high by then; at 80% they are about one in seven).</summary>
        public const double ExamLeadShare = .95;

        static bool ExamLeads(XgSim sim) => sim.ExamPassPending || Math.Min(sim.ExamParamsReady, sim.ExamSamplesReady) + 1e-9 >= ExamLeadShare;

        /// <summary>A failed 能力考 waits a few rounds: 「没考过：再练 3 轮就能重考」.</summary>
        static void ExamRetry(XgSim sim, List<XgGuideStep> list)
        {
            int n = sim.ExamRoundsToRetry;
            if (n <= 0 || sim.NextAbility <= 0 || Math.Min(sim.ExamParamsReady, sim.ExamSamplesReady) + 1e-9 < XgSim.ExamOpenShare) return;
            list.Add(Step("exam.retry", XgGuideKind.Main, "没考过：再练 " + n + " 轮就能重考", "Exam failed: " + n + " more rounds and it can sit it again",
                "两条进度越满越好考；等的时候补数据或参数，下次更稳。", "The fuller both bars, the easier; fill them while you wait and the next try is safer.",
                Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(TrainableTrack(sim))));
        }

        /// <summary>
        /// Stage 5 with 注意力 owned: the Transformer idea comes after a few rounds (or ten minutes). Training is the quick
        /// way, and the Transformer then cuts both lines to six tenths, so it leads the ability steps.
        /// </summary>
        static void TransformerIdea(XgSim sim, List<XgGuideStep> list)
        {
            if (sim.S.stage != 5 || !sim.Has("attention") || sim.TransformerIdeaReached || sim.Has("transformer")) return;
            int rounds = sim.AttentionIdeaRoundsLeft, minutes = (int)Math.Ceiling(sim.AttentionIdeaSecondsLeft / 60);
            list.Add(Step("ability.transformer", XgGuideKind.Ability,
                "再练 " + rounds + " 轮（或等 " + minutes + " 分钟），它会想到「只用注意力」",
                rounds + " more rounds (or " + minutes + " minutes) and it thinks of 'attention only'",
                "想到以后科技能买 Transformer：样本线和参数线都砍到六成。大批数据可以等它想到再买。",
                "Then the Transformer goes on sale: it cuts both the sample and the parameter line to six tenths. Big data batches can wait until then.",
                Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(TrainableTrack(sim))));
        }

        /// <summary>Unread chapters on the shelf worth at least this share of the bar still missing get a line (stage 4 on).</summary>
        public const double BookWorthShare = .05;

        /// <summary>「书架读《红楼梦》：+20 万样本、+300K 参数，不花钱」: from stage 4 a book is a free share of the bars.</summary>
        static void Books(XgSim sim, List<XgGuideStep> list)
        {
            int next = sim.NextAbility;
            if (sim.S.stage < 4 || next <= 0 || sim.Reading || !TabOpen(sim, Lab, "train")) return;
            double dGap = sim.SamplesThreshold(next) - sim.TrainedSamples, pGap = sim.ParamsThreshold(next) - sim.TrainedParamsK;
            XgBook best = null; double bestSamples = 0;
            foreach (var b in XgBooks.All)
            {
                if (!sim.BookOpen(b) || sim.BookDone(b)) continue;
                double chars = 0; int read = sim.ChaptersRead(b);
                for (int i = read; i < b.chapters.Length; i++) chars += b.chapters[i].chars;
                double samples = chars * sim.ReadWorth;
                bool worth = dGap > 0 && samples >= dGap * BookWorthShare || pGap > 0 && b.paramsK >= pGap * BookWorthShare;
                if (worth && samples > bestSamples) { best = b; bestSamples = samples; }
            }
            if (best == null) return;
            list.Add(Step("ability.book", XgGuideKind.Ability,
                "书架读《" + best.name + "》：+" + XgSim.SamplesText(bestSamples) + " 样本、读完 +" + XgSim.ParamsText(best.paramsK) + " 参数，不花钱",
                "Read " + best.nameEn + " on the shelf: +" + XgSim.SamplesText(bestSamples) + " samples, +" + XgSim.ParamsText(best.paramsK) + " parameters when finished, free",
                "现在 1 个字 = " + Num(sim.ReadWorth, "0") + " 条样本，只花电。等钱、等下载的时候正好读。", "Right now 1 character = " + Num(sim.ReadWorth, "0") + " samples, and it only costs power. Good while waiting for money or a download.",
                Lab, "train", "name:BooksTab", ""));
        }

        /// <summary>
        /// The first step that fills one bar of the next ability, for the lab's top-bar 「去做 ›」 link: the shorter bar's
        /// own steps, else the other bar's; null when both are full or their page is not open yet.
        /// </summary>
        public static XgGuideStep BarStep(XgSim sim, double money, double vramMB, out bool paramsBar)
        {
            paramsBar = true;
            int next = sim == null ? 0 : sim.NextAbility;
            if (next == 0 || !TabOpen(sim, Lab, "tree")) return null;
            // The exam is open (or passed): the way on is the chapter button, not another purchase.
            if (ChapterWaits(sim) && TabOpen(sim, Lab, "train")) { paramsBar = true; return ConfirmStage(sim); }
            bool paramsFirst = sim.ParamsProgress(next) <= sim.SamplesProgress(next);
            for (int pass = 0; pass < 2; pass++)
            {
                bool parameters = paramsFirst == (pass == 0);
                var list = new List<XgGuideStep>();
                var wallet = new Wallet { money = money, vram = vramMB };
                if (parameters && sim.TrainedParamsK + 1e-9 < sim.ParamsThreshold(next)) ParamsSteps(sim, wallet, next, list, new HashSet<string>());
                else if (!parameters && sim.TrainedSamples + 1e-9 < sim.SamplesThreshold(next)) DataSteps(sim, wallet, next, list, new HashSet<string>());
                list.RemoveAll(x => !TabOpen(sim, x.app, x.tab));
                if (list.Count > 0) { paramsBar = parameters; return list[0]; }
            }
            return null;
        }

        /// <summary>Id of the step that asks the player to press the chapter button (<see cref="ConfirmStage"/>).</summary>
        public const string ConfirmStageId = "stage.confirm";

        /// <summary>
        /// Both bars are full and the next ability waits for the player (<see cref="XgSim.StageReady"/>): press the
        /// training page's chapter button, which is named "ConfirmNextStage" (XgTrainPage.Side).
        /// </summary>
        /// <summary>The chapter waits on the player: the 能力考 can be sat, or was passed and not claimed.</summary>
        public static bool ChapterWaits(XgSim sim) => sim.ExamReady || sim.ExamPassPending;

        public static XgGuideStep ConfirmStage(XgSim sim)
        {
            int next = sim.NextAbility;
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            if (sim.ExamPassPending)
                return Step(ConfirmStageId, XgGuideKind.Main, "能力考过了：点「继续」让它学会「" + name + "」", "The exam is passed: press 'Continue' so it learns " + nameEn,
                    "训练页右边的按钮翻到下一章。", "The button on the right of the training page turns the chapter.",
                    Lab, "train", "name:ConfirmNextStage", TrackArg(TrainableTrack(sim)));
            return Step(ConfirmStageId, XgGuideKind.Main, "两样都到门槛的八成了：去训练页让它考「" + name + "」", "Both bars are at 80%: let it sit the exam for " + nameEn + " on the Training page",
                XgSim.AbilityExamQuestions + " 道题答对 " + XgSim.AbilityExamPass + " 道就过；两条进度越满，越容易过。没过也不扣钱，再练几轮就能重考。",
                XgSim.AbilityExamPass + " of " + XgSim.AbilityExamQuestions + " right passes; the fuller the bars, the easier. A fail costs nothing: train a few rounds and sit it again.",
                Lab, "train", "name:ConfirmNextStage", TrackArg(TrainableTrack(sim)));
        }

        /// <summary>Right after the setup is saved, its first 是 / 否 waits on screen.</summary>
        public static XgGuideStep AnswerFirst()
            => Step("setup.answer", XgGuideKind.Setup, "回答它的第一个问题：是，还是否", "Answer its first question: yes or no",
                "它现在只会这两个字。", "Those two words are all it has yet.", Lab, "", "label:是|Yes", "");

        /// <summary>The prologue's last step (the note's only line until the setup is saved): open the exe on the desktop.</summary>
        public static XgGuideStep OpenExe()
            => Step("open.exe", XgGuideKind.Setup, "双击桌面上的「" + LingGuangV05.Core.AppNames.ExeZh + "」", "Double-click " + LingGuangV05.Core.AppNames.ExeEn + " on the desktop",
                "它留下的程序。打开看看它想干什么。", "The program it left behind. Open it and see what it wants.", Desktop, "", "", "");

        // ───────────── blockers ─────────────

        /// <summary>The wallet is below zero (rent and bills, ChapterOneSim.Bills.cs): paying it back comes before anything else.</summary>
        static void Rent(double money, XgGuideHouse house, List<XgGuideStep> list)
        {
            if (money >= 0 || double.IsNaN(money)) return;
            string owed = "¥" + Math.Ceiling(-money - 1e-9).ToString("0", CultureInfo.InvariantCulture);
            list.Add(Step("rent.debt", XgGuideKind.Blocker, "赚钱交房租（欠 " + owed + "）", "Earn the rent (" + owed + " owed)",
                house.landlordCut ? "房东拉了电闸，显卡全停；手动标注照样有钱。再拖下去，房东会来催你卖电脑抵债（卖不卖你定）。" : "钱包见底了，房东在催。标注、订单都能挣；连着几次结算都还不上，房东会来催你卖电脑抵债。",
                house.landlordCut ? "The landlord cut the power and every card has stopped; hand labelling still pays. Keep it up and he will push you to sell the computer for the rent (your call)." : "The wallet is empty and the landlord wants his rent. Labels and contracts both pay; a few settlements in a row without it and he will push you to sell the computer.",
                Crowd, "label", "label:是|Yes|对|True", ""));
        }

        static void Blockers(XgSim sim, XgGuideHouse house, List<XgGuideStep> list)
        {
            if (sim.SystemUnlocked("power") && house.unpaidPower)
            {
                string due = house.billDue > 0 ? " ¥" + house.billDue.ToString("0.##", CultureInfo.InvariantCulture) : "";
                list.Add(Step("power.bill", XgGuideKind.Blocker, "交电费" + due, "Pay the power bill" + due,
                    "欠费停电了，显卡全停。钱不够就去标注台答题抵扣。", "The power is cut for an unpaid bill; every card is idle. Short of money? Labelling pays it off.",
                    Home, "power", "name:PayBill", ""));
            }
            if (sim.SystemUnlocked("power") && house.breakerTripped)
                list.Add(Step("power.breaker", XgGuideKind.Blocker, house.overloaded ? "电闸跳了：少插一张卡，再复位" : "复位电闸", house.overloaded ? "Breaker tripped: pull a card, then reset it" : "Reset the breaker",
                    house.overloaded ? "插的卡太多，电线扛不住。" : "跳闸了，什么都不转。", house.overloaded ? "Too many cards for the wiring." : "The breaker is off; nothing runs.",
                    Home, "power", "name:ResetBreaker", ""));
            else if (sim.SystemUnlocked("power") && house.overloaded && !house.unpaidPower && !house.noGpu)
                list.Add(Step("power.overload", XgGuideKind.Blocker, "超负载了：少插一张卡", "Overloaded: pull a card",
                    "家里的电带不动这么多卡。", "The house cannot power this many cards.", Home, "power", "", ""));
            if (house.noGpu)
                list.Add(Step("gpu.none", XgGuideKind.Blocker, "去" + LingGuangV05.Core.AppNames.ShopZh + "买张显卡", "Buy a graphics card on " + LingGuangV05.Core.AppNames.ShopEn,
                    "没有显卡，什么都训不了。", "No card, no training.", Shop, "shop", "name:BuyGpu", ""));
            if (house.appInstalled && sim.DownloadBlocker(out _) != null)
                list.Add(Step("disk.full", XgGuideKind.Blocker, "硬盘满了：去" + LingGuangV05.Core.AppNames.ShopZh + "买一块（¥" + Money(sim.NextDrivePrice) + "）", "Disk full: buy a drive on " + LingGuangV05.Core.AppNames.ShopEn + " (¥" + Money(sim.NextDrivePrice) + ")",
                    "下载停在半路，数据进不来。一块 2TB 移动硬盘就够放很久" + (sim.Has(XgSim.DataCleanId) ? "。" : "；研究「数据清洗」也能压掉 30%。"),
                    "Downloads stopped halfway and no data comes in. One 2TB portable drive lasts a long time" + (sim.Has(XgSim.DataCleanId) ? "." : "; Data cleaning research also squeezes out 30%."),
                    Shop, "shop", "name:BuyDrive", ""));
            if (house.appInstalled && sim.CaptchaPending)
            {
                string clock = XgSim.FreezeClock(sim.CaptchaSecondsLeft);
                list.Add(Step("qc.captcha", XgGuideKind.Blocker, "摆渡众包要人机验证：去标注台输入验证码（" + clock + "）", "Bodu Crowdsourcing wants a captcha: enter it on the labelling page (" + clock + ")",
                    "标得太快，平台怀疑是机器。答错或超时会暂停自动标注两分钟。", "Labelling this fast looks like a machine. A wrong or late answer pauses auto labelling for two minutes.",
                    Lab, "teach", "name:QcCaptcha", ""));
            }
            if (sim.SystemUnlocked("vram") && house.vramMB > 0 && house.appInstalled && sim.S.stage < 6)
            {
                var track = TrainableTrack(sim);
                if (sim.TrainingUnlocked(track) && XgSim.VramNeedMB(sim.Run(track)) > sim.Vram(new Wallet { vram = house.vramMB }))
                    list.Add(Step("vram", XgGuideKind.Blocker, "显存装不下最小的模型（" + VramBar(XgSim.VramNeedMB(sim.Run(track)), house.vramMB, false) + "）：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡", "Even the smallest model does not fit (" + VramBar(XgSim.VramNeedMB(sim.Run(track)), house.vramMB, true) + "): buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                        "模型是自动配置的，显卡装得下多大就用多大；现在连最小的都放不下。", "The model sizes itself to the card; right now not even the smallest fits.", Shop, "shop", "name:BuyGpu", ""));
            }
        }

        /// <summary>The platform's pass rate is at the warning line: point at the threshold (or the node that adds it).</summary>
        static void Quality(XgSim sim, List<XgGuideStep> list)
        {
            if (!sim.QualityWarning) return;
            bool coop = sim.CollaborationEnabled;
            string pass = Math.Floor(sim.PassRate * 100 + 1e-9).ToString("0", CultureInfo.InvariantCulture) + "%";
            string fine = XgSim.QcFineMultiplier.ToString("0", CultureInfo.InvariantCulture);
            list.Add(Step("qc.rate", XgGuideKind.Main,
                coop ? "抽检合格率偏低：调高「拿不准才问我」阈值，或先把模型练好" : "抽检合格率偏低：去科技买「拿不准才问我」",
                coop ? "Spot-check pass rate is low: raise the 'Ask when unsure' threshold, or train a better model first" : "Spot-check pass rate is low: buy 'Ask when unsure' in Tech",
                "摆渡众包近期合格率 " + pass + "；抽到的错题每道罚 " + fine + " 倍工钱。" + sim.QualityAdvice(false), "Bodu Crowdsourcing's recent pass rate is " + pass + "; each wrong label caught costs " + fine + "× its pay. " + sim.QualityAdvice(true),
                coop ? Crowd : Lab, coop ? "label" : "tree", coop ? "name:ThresholdSlider" : "node:label.coop", ""));
        }

        // ───────────── the first minutes ─────────────

        /// <summary>Labels on 常识判断 the note asks for while its yes/no is still short of learned (training opens at <see cref="XgCatalog.SamplesToTrain"/>).</summary>
        public const int OpeningLabelGoal = 80;

        /// <summary>
        /// Only the sense desk and the training page are open, so only they are named: label 常识判断 on 摆渡众包 until
        /// training opens, then train until the yes/no reaches the learned line, with a second line to label more while the
        /// desk is still thin. Chatting with it comes after (<see cref="OpeningChat"/>).
        /// </summary>
        static void Opening(XgSim sim, List<XgGuideStep> list)
        {
            var desk = XgCatalog.Desk(XgMemes.SenseDesk);
            double have = sim.Samples(XgMemes.SenseDesk);
            if (!sim.TrainingUnlocked(XgTrack.Sequence))
            {
                var s = Step("opening.label", XgGuideKind.Setup, "在「教」页标「" + desk.name + "」", "Label '" + desk.nameEn + "' on the Teach page",
                    "先教它分辨对错。标满 " + XgCatalog.SamplesToTrain + " 张，就能带它训练。", XgCatalog.SamplesToTrain + " labels unlock training; first teach it right from wrong.",
                    Crowd, "label", "name:Paper", desk.id);
                s.current = have; s.goal = XgCatalog.SamplesToTrain;
                list.Add(s);
                return;
            }
            if (sim.S.epochs > 0 && sim.S.assessments == 0) { Assess(sim, list); return; }
            int pct = (int)Math.Floor(sim.YesNoAccuracy * 100 + 1e-9), line = (int)Math.Round(XgSim.SenseVoiceLearned * 100);
            // Training alone stopped helping (two assessments without a record) and the desk is thin: labels come first.
            if (sim.YesNoAccuracy < XgSim.SenseVoiceLearned && have < OpeningLabelGoal && sim.S.sequence.staleEvals >= 2)
            {
                var first = Step("opening.more", XgGuideKind.Setup, "先再标 " + (int)Math.Ceiling(OpeningLabelGoal - have) + " 张「" + desk.name + "」，光练涨不动了", "Label " + (int)Math.Ceiling(OpeningLabelGoal - have) + " more '" + desk.nameEn + "' cards first: training alone has stalled",
                    "它把手上这几张背熟了，再练也只是重复。多给它几张新题。", "It has these few cards by heart; more rounds only repeat them. Give it new cards.", Crowd, "label", "name:Paper", desk.id);
                first.current = have; first.goal = OpeningLabelGoal;
                list.Add(first);
            }
            list.Add(Step("opening.train", XgGuideKind.Setup, "训练一轮，直到常识判断到 " + line + "%（现在 " + pct + "%）", "Train until Common sense reaches " + line + "% (now " + pct + "%)",
                "练过的题它才会。练到 " + line + "%，它的是和否就不再靠蒙。", "It only knows what it trained on. At " + line + "% its yes and no stop being guesses.",
                Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(XgTrack.Sequence)));
            if (sim.YesNoAccuracy < XgSim.SenseVoiceLearned && have < OpeningLabelGoal && sim.S.sequence.staleEvals < 2)
            {
                var more = Step("opening.more", XgGuideKind.Setup, "再标几张「" + desk.name + "」，它会分得更准", "Label a few more '" + desk.nameEn + "' cards to sharpen it",
                    "样本越多，练出来的越准。", "More samples, a sharper model.", Crowd, "label", "name:Paper", desk.id);
                more.current = have; more.goal = OpeningLabelGoal;
                list.Add(more);
            }
        }

        /// <summary>After it learned yes from no: ask it something it was taught and see the change.</summary>
        public static XgGuideStep OpeningChat()
            => Step("opening.chat", XgGuideKind.Main, "和它聊聊，看看有什么不同", "Chat with it and see what changed",
                "问它一句教过的话，比如「冰是烫的吗」。它现在答得对了。", "Ask it something you taught, like \"Is ice hot?\". It answers right now.", YY, YYChat, "", "");

        // ───────────── getting the loop going ─────────────

        static void Label(XgSim sim, List<XgGuideStep> list)
        {
            var desk = LabelDesk(sim);
            if (desk == null) return;
            double have = sim.Samples(desk.id);
            int need = Math.Max(1, XgCatalog.SamplesToTrain - (int)Math.Floor(have));
            var s = Step("label." + desk.id, XgGuideKind.Setup, "再教它 " + need + " 张「" + desk.name + "」", "Label " + need + " more " + desk.nameEn + " cards",
                "先看它的判断，对了就确认，错了就纠正。教满 " + XgCatalog.SamplesToTrain + " 张，就带它训练。", XgCatalog.SamplesToTrain + " samples unlock training. Right answers pay too.",
                Lab, "teach", "name:Paper", desk.id);
            s.current = have; s.goal = XgCatalog.SamplesToTrain;
            list.Add(s);
        }

        /// <summary>The desk to label: the one picked if it is open, else the one closest to enough data.</summary>
        static XgDesk LabelDesk(XgSim sim)
        {
            var open = sim.OpenDesks();
            if (open.Count == 0) return null;
            var picked = open.Find(d => d.id == sim.S.desk);
            if (picked != null && sim.DatasetAvailable(picked.id)) return picked;
            XgDesk best = null;
            foreach (var d in open)
                if (sim.DatasetAvailable(d.id) && (best == null || sim.Samples(d.id) > sim.Samples(best.id))) best = d;
            return best ?? open[0];
        }

        static XgTrack TrainableTrack(XgSim sim)
        {
            if (sim.TrainingUnlocked(sim.SelectedTrack)) return sim.SelectedTrack;
            return sim.SelectedTrack == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision;
        }

        static string TrackArg(XgTrack track) => track == XgTrack.Vision ? "vision" : "sequence";

        static void Train(XgSim sim, List<XgGuideStep> list)
        {
            list.Add(Step("train.first", XgGuideKind.Setup, "训练页按「训练一轮」", "Press 'Train 1 epoch' on the training page",
                "数据够了，让它学第一轮。", "There is enough data: let it learn its first epoch.", Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(TrainableTrack(sim))));
        }

        static void Assess(XgSim sim, List<XgGuideStep> list)
        {
            list.Add(Step("assess.first", XgGuideKind.Setup, "评估一次，看看成绩", "Assess once to see the score",
                "评估给成绩打分，刷新纪录就发钱。", "An assessment scores the model; a new record pays.", Lab, "train", "label:评估|Assess", TrackArg(TrainableTrack(sim))));
        }

        // ───────────── the stage's goal ─────────────

        static void Contracts(XgSim sim, List<XgGuideStep> list)
        {
            XgContract best = null;
            foreach (var c in XgCatalog.Contracts) if (sim.CanSign(c) && (best == null || c.signBonus > best.signBonus)) best = c;
            if (best == null) return;
            list.Add(Step("contract." + best.id, XgGuideKind.Main, "到摆渡众包签「" + best.job + "」", "Sign '" + best.jobEn + "' in Bodu Crowd",
                "成绩够了，签下来以后每秒都有进账。", "The score is good enough; once signed it pays every second.", Crowd, "contracts", "contract:" + best.id, ""));
        }

        /// <summary>
        /// The main line (参数量与数据量主线): the next ability, as concrete steps. The parameter bar comes first: what to
        /// buy, in order (<see cref="ParamsPlan"/>), then training that model to grade C (it counts at once). The data bar
        /// follows: the data pack or item that closes most of the gap for its price (<see cref="DataPlan"/>), then how
        /// many more labels on the desks that count in full. Every node these steps would buy goes into
        /// <paramref name="advancing"/>, so the cheap-boost line can prefer them.
        /// </summary>
        static void NextAbility(XgSim sim, Wallet wallet, List<XgGuideStep> list, HashSet<string> advancing)
        {
            int next = sim.NextAbility;
            // Before the first record the 科技 page is still closed; the first trained round (a setup step) opens it.
            if (next == 0 || !TabOpen(sim, Lab, "tree")) return;
            if (sim.TrainedParamsK + 1e-9 < sim.ParamsThreshold(next)) ParamsSteps(sim, wallet, next, list, advancing);
            if (sim.TrainedSamples + 1e-9 < sim.SamplesThreshold(next)) DataSteps(sim, wallet, next, list, advancing);
        }

        static void ParamsSteps(XgSim sim, Wallet wallet, int next, List<XgGuideStep> list, HashSet<string> advancing)
        {
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            var recommended = sim.NextStudyNode();
            // From stage 4 the route's pack is an old one; the data plan names the pack that closes the most of the gap.
            if (recommended != null && !(recommended.kind == XgNodeKind.Dataset && sim.S.stage >= 4))
            {
                list.Add(Step("ability.study", XgGuideKind.Ability,
                    "下一步：科技买「" + recommended.name + "」 · ¥" + sim.NodeCost(recommended).ToString("0"),
                    "Next: buy " + recommended.nameEn + " in Tech · ¥" + sim.NodeCost(recommended).ToString("0"),
                    "为「" + name + "」做准备。买好以后回训练页，练完再和它聊聊。通过能力考后，摆渡众包升级会发一笔见面礼；留些钱付电费。",
                    "Prepare for " + nameEn + ". Then train and talk to it. Each new ability funds your next research.",
                    Lab, "tree", "node:" + recommended.id, ""));
                return;
            }
            double pNeed = sim.ParamsThreshold(next);
            // The line itself is never named (XgSim.Learned.cs): the steps say what to grow, not how far it is.
            string have = XgSim.ParamsText(sim.TrainedParamsK);
            var plan = ParamsPlan(sim, next, wallet.vram > 0 ? sim.Vram(wallet) : 0);
            if (plan == null)
            {
                // Nothing on sale at this stage reaches it (or the card is too small): the general advice.
                var big = wallet.vram > 0 ? ParamsPlan(sim, next, 0) : null;
                if (big != null)
                {
                    // The smallest model that would do takes this much of the card; the card has less.
                    double needMB = XgSim.VramNeedMB(PlanRun(big)), cardMB = sim.Vram(wallet);
                    list.Add(Step("ability.card", XgGuideKind.Ability,
                        "显存放不下够大的模型（" + VramBar(needMB, cardMB, false) + "）：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡",
                        "Not enough VRAM for a big enough model (" + VramBar(needMB, cardMB, true) + "): buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                        "「" + name + "」要更大的模型，这张卡装不下。\n够大的模型（" + XgSim.ParamsText(big.paramsK) + "）要 " + XgSim.MemText(needMB) + " 显存，卡上只有 " + XgSim.MemText(cardMB) + "，还差 " + XgSim.MemText(Math.Max(0, needMB - cardMB)) + "。",
                        "'" + nameEn + "' needs a bigger model than this card can hold.\nA big enough model (" + XgSim.ParamsText(big.paramsK) + ") takes " + XgSim.MemText(needMB) + " of VRAM; the card has " + XgSim.MemText(cardMB) + ", " + XgSim.MemText(Math.Max(0, needMB - cardMB)) + " short.",
                        Shop, "shop", "name:BuyGpu", ""));
                }
                else
                {
                    var line = sim.Run(TrainableTrack(sim));
                    list.Add(Step("ability.params", XgGuideKind.Ability, "参数量不够：科技里加宽、加深，再练到 C 级：参数 " + have + " / ?", "Not enough parameters: widen or deepen in the tech tree, then train to grade C: parameters " + have + " / ?",
                        "下一项能力「" + name + "」要更大的模型。买到宽度和层数，或者参数更多的结构，模型自己就会长大。\n这台模型 " + sim.ModelFormula(line) + "；" + sim.CapsText(line) + "。",
                        "The next ability, '" + nameEn + "', needs a bigger model. Buy width and layers, or a bigger structure, and the model grows by itself.\nThis model: " + sim.ModelFormula(line) + "; " + sim.CapsText(line) + ".",
                        Lab, "tree", "", ""));
                }
                return;
            }
            if (!sim.TrainingUnlocked(plan.track))
            {
                // The big enough model is on the other line, which has no data yet: its desk first.
                var desk = TrackDesk(sim, plan.track);
                double samples = sim.Samples(desk.id);
                int labels = Math.Max(1, XgCatalog.SamplesToTrain - (int)Math.Floor(samples));
                bool vision = plan.track == XgTrack.Vision;
                var t = Step("ability.track", XgGuideKind.Ability, "标注台标 " + labels + " 条「" + desk.name + "」，让" + (vision ? "看图" : "读字") + "的模型开练",
                    "Label " + labels + " more " + desk.nameEn + " cards so the " + (vision ? "vision" : "reading") + " model can train",
                    "「" + name + "」要更大的模型，只有这条线的模型长得到。先攒够 " + XgCatalog.SamplesToTrain + " 条数据。",
                    "'" + nameEn + "' needs a bigger model, and only this line's model grows that big. It needs " + XgCatalog.SamplesToTrain + " samples first.",
                    Crowd, "label", "label:是|Yes|对|True", desk.id);
                t.current = samples; t.goal = XgCatalog.SamplesToTrain;
                list.Add(t);
            }
            if (plan.buy.Count > 0)
            {
                foreach (var n in plan.buy) advancing.Add(n.id);
                var first = plan.buy[0];
                // The numbers: P against the line, the model after each purchase, the bill against the wallet, the grade-C line to train to.
                var planLine = sim.Run(plan.track);
                string cLineZh = "买齐后模型 " + XgSim.ParamsText(plan.paramsK) + "，够大了；再练到 C 级（" + XgSim.Pct(XgSim.GradeAccuracy(planLine.dataset, XgSim.TrainedGrade)) + "）就算数。";
                string cLineEn = "Afterwards the model is " + XgSim.ParamsText(plan.paramsK) + ", big enough; train it to grade C (" + XgSim.Pct(XgSim.GradeAccuracy(planLine.dataset, XgSim.TrainedGrade)) + ") and it counts.";
                var s = Step("ability.buy", XgGuideKind.Ability, "科技买" + Chain(plan.buy, false), "Tech tree: buy " + Chain(plan.buy, true),
                    "「" + name + "」要更大的模型（现在 P " + have + "，线在哪里学会前不知道）。买齐这些，模型自己长到 " + XgSim.ParamsText(plan.paramsK) + "，再练到 C 级就算数。\n"
                        + BuyBreakdown(sim, wallet, plan, false) + "\n" + cLineZh + ShortOfMoney(sim, wallet, first, false),
                    "'" + nameEn + "' needs a bigger model (P is " + have + " now; nobody knows the line until it learns). With these the model grows to " + XgSim.ParamsText(plan.paramsK) + " by itself; trained to grade C, it counts.\n"
                        + BuyBreakdown(sim, wallet, plan, true) + "\n" + cLineEn + ShortOfMoney(sim, wallet, first, true),
                    Lab, "tree", "node:" + first.id, "");
                MoneyProgress(sim, wallet, first, s);
                list.Add(s);
                return;
            }
            if (!sim.TrainingUnlocked(plan.track)) return;
            // The caps already allow a big enough model: train the one the lab configured until grade C; it counts at once.
            var run = sim.Run(plan.track);
            string arg = TrackArg(plan.track);
            if (XgSim.ParamsK(run) + 1e-9 < pNeed)
            {
                // The lab configured a smaller one: the card is shared or too small for the big one.
                double bigMB = XgSim.VramNeedMB(PlanRun(plan)), cardNow = wallet.vram > 0 ? sim.Vram(wallet) : 0;
                list.Add(Step("ability.card", XgGuideKind.Ability, "显存放不下够大的模型（" + VramBar(bigMB, cardNow, false) + "）：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡", "Not enough VRAM for a big enough model (" + VramBar(bigMB, cardNow, true) + "): buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                    "模型按显卡自动配置，现在只放得下 " + XgSim.ParamsText(XgSim.ParamsK(run)) + "（" + VramBar(XgSim.VramNeedMB(run), cardNow, false) + "）；够大的模型要 " + XgSim.MemText(bigMB) + "。",
                    "The model sizes itself to the card; right now it fits " + XgSim.ParamsText(XgSim.ParamsK(run)) + " (" + VramBar(XgSim.VramNeedMB(run), cardNow, true) + "); a big enough model takes " + XgSim.MemText(bigMB) + ".", Shop, "shop", "name:BuyGpu", ""));
                return;
            }
            double cLine = XgSim.GradeAccuracy(run.dataset, XgSim.TrainedGrade);
            string accZh = "准确率 " + XgSim.Pct(run.valAcc) + " / C 线 " + XgSim.Pct(cLine), accEn = "accuracy " + XgSim.Pct(run.valAcc) + " / grade C " + XgSim.Pct(cLine);
            list.Add(Step("ability.train", XgGuideKind.Ability, "把模型练到 C 级（" + accZh + "）：参数 " + have + " / ?", "Train the model to grade C (" + accEn + "): parameters " + have + " / ?",
                "模型已经有 " + XgSim.ParamsText(XgSim.ParamsK(run)) + " 参数，够「" + name + "」了。练到 C 级就算数。\n" + CLine(run, false) + (run.shapeRounds <= 0 ? "；刚长成这个大小，还没练过一轮。" : "。"),
                "The model already has " + XgSim.ParamsText(XgSim.ParamsK(run)) + " parameters, enough for '" + nameEn + "'. Train a few epochs; at grade C they count.\n" + CLine(run, true) + (run.shapeRounds <= 0 ? "; it just grew to this size and has not trained a round yet." : "."),
                Lab, "train", "label:训练一轮|Train 1 epoch", arg));
        }

        static void DataSteps(XgSim sim, Wallet wallet, int next, List<XgGuideStep> list, HashSet<string> advancing)
        {
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            double d = sim.TrainedSamples, dNeed = sim.SamplesThreshold(next), gap = dNeed - d;
            // Batches and packs already bought and still in the download queue.
            double incoming = sim.QueuedDataSamples(out var waiting);
            if (waiting != null && incoming + 1e-9 >= gap)
            {
                list.Add(DownloadWait(sim, waiting, name, nameEn, d, dNeed));
                Broadband(sim, wallet, list);
                return;
            }
            gap -= incoming;
            var buy = DataPlan(sim, next, gap);
            double after = gap;
            if (buy != null)
            {
                foreach (var n in buy.path) advancing.Add(n.id);
                if (buy.batch != null)
                {
                    list.Add(BatchStep(sim, wallet, buy, name, nameEn, d, dNeed));
                    if (waiting != null) Broadband(sim, wallet, list);
                    return;
                }
                var first = buy.path[0];
                bool pack = buy.target.kind == XgNodeKind.Dataset;
                string what = pack ? "「" + buy.target.name + "」数据包" : "「" + buy.target.name + "」";
                string whatEn = pack ? "the " + buy.target.nameEn + " data pack" : "'" + buy.target.nameEn + "'";
                string zh = buy.path.Count == 1 ? "科技买" + what : "科技买" + Chain(buy.path, false);
                string en = buy.path.Count == 1 ? "Tech tree: buy " + whatEn : "Tech tree: buy " + Chain(buy.path, true);
                if (!pack) { zh += "¥" + Money(buy.cost) + "：" + LineCuts(sim, next, buy, false); en += " ¥" + Money(buy.cost) + ": " + LineCuts(sim, next, buy, true); }
                string why = pack ? "下载权连第一批：补 " + XgSim.SamplesText(buy.gain) + " 条有效样本，「" + name + "」还要更多数据。"
                    : "它把「" + name + "」要的数据压低一截。";
                string whyEn = pack ? "The download right with the first batch: " + XgSim.SamplesText(buy.gain) + " effective samples; '" + nameEn + "' needs more data."
                    : "It lowers the data '" + nameEn + "' needs.";
                var s = Step(pack ? "ability.pack" : "ability.item", XgGuideKind.Ability, zh, en, why + ShortOfMoney(sim, wallet, first, false), whyEn + ShortOfMoney(sim, wallet, first, true),
                    Lab, "tree", "node:" + first.id, "");
                MoneyProgress(sim, wallet, first, s);
                list.Add(s);
                after = gap - buy.gain;
            }
            if (after <= 1e-9) return;
            // After a buy, labels are only worth naming when they finish the bar in reasonable time; otherwise the
            // next buy is named once this one is in.
            const int LabelsWorthNaming = 300;
            // Then hand labels on the desks whose samples count in full (算术 counts half).
            var desks = BestDataDesks(sim);
            if (desks.Count == 0)
            {
                if (buy != null) return;
                var g = Step("ability.data", XgGuideKind.Ability, "攒数据：样本", "Gather data: samples",
                    "下一项能力「" + name + "」要更多数据。", "The next ability, '" + nameEn + "', needs more data.", Crowd, "label", "", "");
                list.Add(g);
                return;
            }
            double weight = XgCatalog.Dataset(desks[0].id).dataWeight;
            int labels = (int)Math.Ceiling(after / Math.Max(.01, weight) - 1e-9);
            if (buy != null && labels > LabelsWorthNaming) return;
            var names = new List<string>(); var namesEn = new List<string>();
            foreach (var desk in desks) { names.Add("「" + desk.name + "」"); namesEn.Add(desk.nameEn); }
            bool auto = sim.GlobalAutoLevel > 0;
            // How many is not said: that would give the hidden line away.
            string labelZh = auto ? "接着攒样本：自动答题在标，手标" + string.Join("或", names) + "更快"
                : (buy != null ? "接着标" : "标注台接着标") + string.Join("或", names);
            string labelEn = auto ? "Keep gathering samples: auto labelling is on; hand-labelling " + string.Join(" or ", namesEn) + " is faster"
                : "Keep labelling " + string.Join(" or ", namesEn) + " cards";
            bool teaching = desks[0].id == XgMemes.SenseDesk && TeachesYesNo(sim);
            var label = Step("ability.label", XgGuideKind.Ability, labelZh, labelEn,
                "「" + name + "」还要更多有效样本（现在 " + XgSim.SamplesText(d) + "）。标对的才算，「算术」只算半条。" + (teaching ? "「常识判断」这桌专教它分清是和否。" : ""),
                "'" + nameEn + "' needs more effective samples (now " + XgSim.SamplesText(d) + "). Only right answers count; Arithmetic counts half." + (teaching ? " The Common sense desk teaches it yes from no." : ""),
                Crowd, "label", "label:是|Yes|对|True", desks[0].id);
            list.Add(label);
        }

        /// <summary>A player's hand-labelling pace for the 「手标约 N 分钟」 estimate (the bot labels at 1.2 s).</summary>
        public const double HandLabelSeconds = 2;

        /// <summary>「样本线 3.2M→1.92M、参数线 2M→1.2M」: what a threshold item does to the next ability's lines.</summary>
        static string LineCuts(XgSim sim, int next, XgDataBuy buy, bool english)
        {
            double dNeed = sim.SamplesThreshold(next), pFactor = 1;
            foreach (var disc in XgSim.AbilityDiscounts)
                if (disc.ability == next && !sim.DiscountOwned(disc) && buy.path.Exists(n => n.id == disc.item || Array.IndexOf(disc.itemsAny, n.id) >= 0))
                    pFactor *= disc.paramsFactor;
            // By how much, never where the line is.
            string s = (english ? "sample line −" : "样本线 −") + Num(Math.Round(100 * buy.gain / Math.Max(1, dNeed)), "0") + "%";
            if (pFactor < 1) s += (english ? ", parameter line −" : "、参数线 −") + Num(Math.Round(100 * (1 - pFactor)), "0") + "%";
            return s;
        }

        /// <summary>「等下载：ImageNet 还要 2:10」: the queue already holds the data the bar needs.</summary>
        static XgGuideStep DownloadWait(XgSim sim, XgDownload waiting, string name, string nameEn, double d, double dNeed)
        {
            var ds = XgCatalog.Dataset(waiting.dataset);
            var o = XgSim.DataOfferDef(waiting.offer);
            bool junk = o != null && o.source == XgDataSource.Junk;
            string what = junk ? o.name : ds.name, whatEn = junk ? o.nameEn : ds.nameEn;
            string eta = XgSim.ClockText(sim.DownloadEtaOf(waiting));
            bool paused = !waiting.campus && sim.DownloadBlocker(out _) != null;
            var w = Step("ability.download", XgGuideKind.Ability,
                paused ? "下载停了（硬盘满）：" + what + "，样本" : "等下载：" + what + " 还要 " + eta + "，样本",
                paused ? "Downloads stopped (disk full): " + whatEn + ", samples" : "Wait for the download: " + whatEn + ", " + eta + " left, samples",
                "下完就够「" + name + "」的数据了。现在是" + XgSim.BroadbandName(sim.BroadbandTier, false) + "，" + Num(sim.LineMBps, "0.#") + " MB/s。等的时候可以接着标注、训练。",
                "Once it is in, '" + nameEn + "' has its data. The line is " + XgSim.BroadbandName(sim.BroadbandTier, true) + ", " + Num(sim.LineMBps, "0.#") + " MB/s. Keep labelling or training meanwhile.",
                Lab, TabOpen(sim, Lab, "data") ? "data" : "train", "", "");
            return w;
        }

        /// <summary>
        /// The line is the slow part and a faster one is on offer: 「宽带太慢：数据页点「升级宽带」→ 光纤 100M（¥350）」. The button
        /// sits in the download strip of the 数据 page (or the 数据来源 view of 练 before the 数据 tab opens); the home bills
        /// page still has the same upgrade.
        /// </summary>
        static void Broadband(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            if (!sim.BroadbandBottleneck) return;
            int next = sim.BroadbandTier + 1;
            double price = sim.NextBroadbandPrice;
            string tier = XgSim.BroadbandName(next, false), tierEn = XgSim.BroadbandName(next, true);
            bool short_ = wallet.money + 1e-9 < price;
            bool dataTab = TabOpen(sim, Lab, "data");
            string where = dataTab ? "数据页顶部" : "练页的数据来源里", whereEn = dataTab ? "the top of the Data page" : "the data sources view of Train";
            var s = Step("broadband", XgGuideKind.Ability,
                "宽带太慢：" + where + "点「升级宽带」→ " + tier + (short_ ? "" : "（¥" + Money(price) + "）"),
                "The line is the bottleneck: click 'Upgrade line' at " + whereEn + " → " + tierEn + (short_ ? "" : " (¥" + Money(price) + ")"),
                "排着的下载还要 " + XgSim.ClockText(sim.LineQueueSeconds) + "。" + tier + " 是 " + Num(XgSim.BroadbandMBps[next], "0.#") + " MB/s，快 " + Num(XgSim.BroadbandMBps[next] / sim.LineMBps, "0") + " 倍；月租 ¥" + Money(XgSim.BroadbandMonthly[next]) + "。在「家庭」的用电账单页也能升。",
                "The queue still needs " + XgSim.ClockText(sim.LineQueueSeconds) + ". " + tierEn + " is " + Num(XgSim.BroadbandMBps[next], "0.#") + " MB/s, " + Num(XgSim.BroadbandMBps[next] / sim.LineMBps, "0") + " times faster; ¥" + Money(XgSim.BroadbandMonthly[next]) + " a month. The Home app's bills tab upgrades it too.",
                Lab, dataTab ? "data" : "train", "name:UpgradeBroadband", "");
            if (short_) { s.money = true; s.current = wallet.money; s.goal = price; }
            list.Add(s);
        }

        /// <summary>「数据页买「ImageNet」第 3 批」: the next batch of a pack the lab owns.</summary>
        static XgGuideStep BatchStep(XgSim sim, Wallet wallet, XgDataBuy buy, string name, string nameEn, double d, double dNeed)
        {
            var ds = XgCatalog.Dataset(buy.batch);
            int k = sim.BatchesBought(ds.id) + 1, all = XgSim.PackBatches(ds);
            // Short of money, the progress shows the price; otherwise the line names it.
            bool short_ = wallet.money + 1e-9 < buy.cost;
            string price = short_ ? "" : "（¥" + Money(buy.cost) + "）", priceEn = short_ ? "" : " (¥" + Money(buy.cost) + ")";
            var s = Step("ability.batch", XgGuideKind.Ability, "数据页买「" + ds.name + "」第 " + k + "/" + all + " 批" + price, "Data page: buy batch " + k + "/" + all + " of " + ds.nameEn + priceEn,
                "补 " + XgSim.SamplesText(buy.gain) + " 条有效样本，" + XgSim.SizeText(XgSim.BatchMB(ds)) + "，要下载一阵。「" + name + "」还要更多数据；每批比上一批贵 " + BatchRise + "%。",
                XgSim.SamplesText(buy.gain) + " more effective samples, " + XgSim.SizeText(XgSim.BatchMB(ds)) + " to download. '" + nameEn + "' needs more data; each batch costs " + BatchRise + "% more than the last.",
                Lab, TabOpen(sim, Lab, "data") ? "data" : "tree", "batch:" + ds.id, ds.id);
            if (short_) { s.money = true; s.current = wallet.money; s.goal = buy.cost; }
            return s;
        }

        static string BatchRise => Num(Math.Round((XgSim.BatchPriceGrowth - 1) * 100), "0");

        static string Num(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);
        static string Money(double v) => double.IsInfinity(v) ? "-" : Math.Ceiling(v - 1e-9).ToString("0", CultureInfo.InvariantCulture);

        /// <summary>The open desk of a line that cannot train yet (the one with the most labels), or null.</summary>
        static XgDesk TrackDesk(XgSim sim, XgTrack track)
        {
            XgDesk best = null;
            foreach (var desk in sim.OpenDesks())
            {
                var ds = XgCatalog.Dataset(desk.id);
                if (ds == null || ds.track != track || !sim.DatasetAvailable(desk.id)) continue;
                if (best == null || sim.Samples(desk.id) > sim.Samples(best.id)) best = desk;
            }
            return best;
        }

        /// <summary>The open desks whose samples count the most towards the data bar (the one being labelled first).</summary>
        static List<XgDesk> BestDataDesks(XgSim sim)
        {
            var open = sim.OpenDesks().FindAll(x => sim.DatasetAvailable(x.id) && CountsAsData(x.id) && XgCatalog.Dataset(x.id) != null);
            double top = 0;
            foreach (var x in open) top = Math.Max(top, XgCatalog.Dataset(x.id).dataWeight);
            var best = open.FindAll(x => XgCatalog.Dataset(x.id).dataWeight >= top - 1e-9);
            best.Sort((a, b) => (b.id == sim.S.desk).CompareTo(a.id == sim.S.desk) != 0 ? (b.id == sim.S.desk).CompareTo(a.id == sim.S.desk) : b.pay.CompareTo(a.pay));
            // While it still answers at chance, the 常识判断 desk is the one that teaches it yes from no: it goes first (XgSim.SenseVoice.cs).
            if (TeachesYesNo(sim))
            {
                var sense = best.Find(x => x.id == XgMemes.SenseDesk);
                if (sense != null) { best.Remove(sense); best.Insert(0, sense); }
            }
            if (best.Count > 2) best.RemoveRange(2, best.Count - 2);
            return best;
        }

        /// <summary>Labels on the 常识判断 desk after which the guide stops singling it out.</summary>
        public const int SenseGuideLabels = 40;

        /// <summary>Stage 1, its yes/no still a coin toss and the 常识判断 desk barely touched: the note names that desk first.</summary>
        static bool TeachesYesNo(XgSim sim)
            => sim.S.stage == 1 && sim.YesNoAccuracy < XgSim.SenseVoiceLow && sim.Samples(XgMemes.SenseDesk) < SenseGuideLabels && sim.DeskOpen(XgMemes.SenseDesk);

        static bool CountsAsData(string dataset) => dataset != "xor" && dataset != "parallel";

        /// <summary>"「多层感知机」→ 2 层 → 宽 32": the first node to buy quoted, the rest after arrows.</summary>
        /// <summary>A node's name inside a step: depth and width squares exist on both lines, so they name the line (视觉 3 层, 序列 宽 128).</summary>
        public static string NodeLabel(XgNode node, bool english)
        {
            string n = english ? node.nameEn : node.name;
            bool ladder = node.kind == XgNodeKind.Depth || node.kind == XgNodeKind.Width;
            if (ladder && node.tree == "vision") n = (english ? "vision " : "视觉 ") + n;
            else if (ladder && node.tree == "sequence") n = (english ? "sequence " : "序列 ") + n;
            if (node.kind == XgNodeKind.Dataset) n += english ? " data pack" : " 数据包";
            return n;
        }

        static string Chain(List<XgNode> nodes, bool english)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < nodes.Count; i++)
            {
                string n = NodeLabel(nodes[i], english);
                if (i == 0) sb.Append(english ? "'" + n + "'" : "「" + n + "」");
                else sb.Append(" → ").Append(n);
            }
            return sb.ToString();
        }

        /// <summary>「1.2M / 6M（20%），差 4.8M」: a parameter bar with its share and gap.</summary>
        static string ParamsBar(double have, double need, bool english)
        {
            int pct = (int)Math.Floor(100 * Math.Min(1, have / Math.Max(1e-9, need)) + 1e-9);
            string gap = XgSim.ParamsText(Math.Max(0, need - have));
            return english ? XgSim.ParamsText(have) + " / " + XgSim.ParamsText(need) + " (" + pct + "%), short by " + gap
                : XgSim.ParamsText(have) + " / " + XgSim.ParamsText(need) + "（" + pct + "%），差 " + gap;
        }

        /// <summary>「显存 2.1 GB / 卡 1 GB」: what a model takes of the card (card 0 = unknown).</summary>
        static string VramBar(double needMB, double cardMB, bool english)
        {
            string need = XgSim.MemText(needMB);
            if (!(cardMB > 0)) return english ? "VRAM " + need : "显存 " + need;
            return english ? "VRAM " + need + " / card " + XgSim.MemText(cardMB) : "显存 " + need + " / 卡 " + XgSim.MemText(cardMB);
        }

        static XgRun PlanRun(XgParamsPlan plan) => new XgRun { track = (int)plan.track, arch = plan.arch, depth = plan.depth, width = plan.width };

        /// <summary>
        /// The biggest model the lab could configure on the plan's line once the first <paramref name="count"/> nodes of
        /// <see cref="XgParamsPlan.buy"/> are owned (the caps they raise, the structures they add, the card).
        /// </summary>
        static double ParamsAfterBuying(XgSim sim, XgParamsPlan plan, int count, double vramMB)
        {
            var track = plan.track; string tree = XgSim.TreeOf(track);
            var bought = plan.buy.GetRange(0, Math.Min(count, plan.buy.Count));
            int depthCap = sim.DepthCap(track), widthCap = sim.WidthCap(track);
            foreach (var n in bought)
            {
                if (n.tree != tree && n.tree != "trunk") continue;
                if (n.kind == XgNodeKind.Depth) depthCap = Math.Max(depthCap, n.value);
                else if (n.kind == XgNodeKind.Width) widthCap = Math.Max(widthCap, n.value);
            }
            bool batchNorm = sim.Has("batchnorm") || bought.Exists(n => n.id == "batchnorm");
            double best = 0;
            foreach (var a in XgCatalog.Archs)
            {
                if (!XgSim.ArchitectureFits(a, track) || !(sim.Has(a.id) || bought.Exists(n => n.id == a.id))) continue;
                int maxDepth = a.maxDepth >= 999 ? 999 : a.maxDepth + (batchNorm ? 6 : 0);
                for (int d = 1; d <= Math.Min(depthCap, maxDepth); d++)
                    for (int w = 0; w <= Math.Min(widthCap, XgCatalog.Widths.Length - 1); w++)
                    {
                        var run = new XgRun { track = (int)track, arch = a.id, depth = d, width = w };
                        if (vramMB > 0 && XgSim.VramNeedMB(run) > vramMB + 1e-6) continue;
                        best = Math.Max(best, XgSim.ParamsK(run));
                    }
            }
            return best;
        }

        /// <summary>One line per node of the plan, with the model after each: 「①「多层感知机」¥150：1.2K → 3.1K」, then the total against the wallet.</summary>
        static string BuyBreakdown(XgSim sim, Wallet wallet, XgParamsPlan plan, bool english)
        {
            var sb = new System.Text.StringBuilder();
            double before = XgSim.ParamsK(sim.Run(plan.track)), vram = wallet.vram > 0 ? sim.Vram(wallet) : 0;
            for (int i = 0; i < plan.buy.Count; i++)
            {
                var n = plan.buy[i];
                // The last node lands on the plan's own model (the figure the sentence above promises).
                double after = i == plan.buy.Count - 1 ? plan.paramsK : ParamsAfterBuying(sim, plan, i + 1, vram);
                sb.Append(i + 1).Append(". ").Append(NodeLabel(n, english)).Append(" ¥").Append(sim.NodeCost(n).ToString("0", CultureInfo.InvariantCulture))
                  .Append(english ? ": model " : "：模型 ").Append(XgSim.ParamsText(before)).Append(" → ").Append(XgSim.ParamsText(after)).Append('\n');
                before = after;
            }
            double have = Math.Max(0, wallet.money), gap = plan.cost - have;
            string c = plan.cost.ToString("0", CultureInfo.InvariantCulture), h = have.ToString("0", CultureInfo.InvariantCulture);
            sb.Append(english ? "Total ¥" + c + ", you have ¥" + h + (gap > 1e-9 ? ", short by ¥" + gap.ToString("0", CultureInfo.InvariantCulture) : ", enough")
                : "共 ¥" + c + "，手头 ¥" + h + (gap > 1e-9 ? "，差 ¥" + gap.ToString("0", CultureInfo.InvariantCulture) : "，够了"));
            return sb.ToString();
        }

        /// <summary>「《数字》现在 41.2%（D 级），C 级线 62.0%，还差 20.8 个百分点」: the model's accuracy against the grade-C line.</summary>
        static string CLine(XgRun run, bool english)
        {
            var d = XgCatalog.Dataset(run.dataset);
            string name = d == null ? run.dataset : english ? d.nameEn : d.name;
            double line = XgSim.GradeAccuracy(run.dataset, XgSim.TrainedGrade), gap = Math.Max(0, line - run.valAcc) * 100;
            string grade = XgSim.GradeLetter(XgSim.Grade(XgSim.Score(run.dataset, run.valAcc)));
            string left = gap > 1e-9 ? (english ? ", " + gap.ToString("0.#", CultureInfo.InvariantCulture) + " points to go" : "，还差 " + gap.ToString("0.#", CultureInfo.InvariantCulture) + " 个百分点") : (english ? ", reached" : "，到了");
            return english ? name + " is at " + XgSim.Pct(run.valAcc) + " (grade " + grade + "), the grade C line is " + XgSim.Pct(line) + left
                : "「" + name + "」现在 " + XgSim.Pct(run.valAcc) + "（" + grade + " 级），C 级线 " + XgSim.Pct(line) + left;
        }

        static string ShortOfMoney(XgSim sim, Wallet wallet, XgNode node, bool english)
        {
            double short_ = sim.NodeCost(node) - wallet.money;
            if (short_ <= 1e-9) return "";
            double rate = sim.IncomePerSecond;
            if (sim.S.stage <= 2 || rate <= 1e-9) return english ? " Short of money? Label on the desk; right answers pay." : "钱不够就去标注台多标几条，答对有钱。";
            string eta = XgSim.ClockText(short_ / rate);
            return english ? " ¥" + Money(short_) + " short: about " + eta + " of income." : "还差 ¥" + Money(short_) + "，约 " + eta + " 的进账。";
        }

        /// <summary>Shows the money still needed for the node; false when it is affordable.</summary>
        static bool MoneyProgress(XgSim sim, Wallet wallet, XgNode node, XgGuideStep step)
        {
            double cost = sim.NodeCost(node);
            if (wallet.money + 1e-9 >= cost) return false;
            step.current = wallet.money; step.goal = cost; step.money = true;
            return true;
        }

        // ───────────── plans towards the next ability ─────────────

        /// <summary>
        /// The cheapest way to a big enough model for an ability: nodes to buy (prerequisites first; empty when the caps
        /// already allow it) and the model the lab would then configure. Only nodes on sale now are used; an architecture
        /// that lowers the threshold counts once it is in the plan. Null when nothing on sale reaches it, or nothing that
        /// would reach it fits <paramref name="vramMB"/> (0 = unknown, not checked).
        /// </summary>
        public static XgParamsPlan ParamsPlan(XgSim sim, int ability, double vramMB = 0)
        {
            if (sim == null || ability < 2 || ability > XgSim.AbilityCount) return null;
            // A line that can train already is preferred; one whose desk is open but has too few labels comes second.
            return ParamsPlan(sim, ability, vramMB, true) ?? ParamsPlan(sim, ability, vramMB, false);
        }

        static XgParamsPlan ParamsPlan(XgSim sim, int ability, double vramMB, bool trainable)
        {
            XgParamsPlan best = null;
            foreach (var track in new[] { XgTrack.Sequence, XgTrack.Vision })
            {
                if (sim.TrainingUnlocked(track) != trainable || !trainable && TrackDesk(sim, track) == null) continue;
                var depths = Ladder(sim, track, XgNodeKind.Depth, sim.DepthCap(track));
                var widths = Ladder(sim, track, XgNodeKind.Width, sim.WidthCap(track));
                foreach (var a in XgCatalog.Archs)
                {
                    if (!XgSim.ArchitectureFits(a, track)) continue;
                    var archPath = Path(sim, a.id);
                    if (archPath == null) continue;
                    int maxDepth = a.maxDepth >= 999 ? 999 : a.maxDepth + (sim.Has("batchnorm") ? 6 : 0);
                    foreach (var depth in depths)
                        foreach (var width in widths)
                        {
                            var run = new XgRun { track = (int)track, arch = a.id, depth = Math.Max(1, Math.Min(depth.value, maxDepth)), width = Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, width.value)) };
                            double k = XgSim.ParamsK(run);
                            if (vramMB > 0 && XgSim.VramNeedMB(run) > vramMB + 1e-6) continue;
                            var buy = new List<XgNode>();
                            foreach (var part in new[] { archPath, depth.path, width.path }) foreach (var n in part) if (!buy.Contains(n)) buy.Add(n);
                            double threshold = XgSim.AbilityParamsK[ability] * Factor(sim, ability, buy, true) * sim.RelicParamsFactor;
                            if (k + 1e-9 < threshold) continue;
                            double cost = 0; foreach (var n in buy) cost += sim.NodeCost(n);
                            bool better = best == null || cost < best.cost - 1e-9
                                || Math.Abs(cost - best.cost) <= 1e-9 && (buy.Count < best.buy.Count || buy.Count == best.buy.Count && k > best.paramsK + 1e-9);
                            if (!better) continue;
                            best = new XgParamsPlan { track = track, arch = a.id, depth = run.depth, width = run.width, paramsK = k, threshold = threshold, cost = cost };
                            best.buy.AddRange(Ordered(buy));
                        }
                }
            }
            return best;
        }

        /// <summary>
        /// The best data buy towards an ability's data bar: a data pack (its samples at the dataset's weight) or an item
        /// that lowers the threshold. The cheapest one that closes <paramref name="gap"/> on its own, else the most data
        /// per yuan. Null when nothing is on sale.
        /// </summary>
        public static XgDataBuy DataPlan(XgSim sim, int ability, double gap)
        {
            if (sim == null || ability < 2 || ability > XgSim.AbilityCount || gap <= 1e-9) return null;
            var options = new List<XgDataBuy>();
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.kind != XgNodeKind.Dataset || !CountsAsData(n.target)) continue;
                var ds = XgCatalog.Dataset(n.target);
                if (ds == null) continue;
                // A pack already owned: its next batch (节奏重做 §2.3, each one dearer than the last).
                if (sim.Has(n.id) || sim.S.owned.Contains(n.target))
                {
                    if (sim.BatchBlocker(ds.id, out _) != null) continue;
                    var b = new XgDataBuy { target = n, batch = ds.id, gain = ds.dataWeight * XgSim.BatchSamples(ds), cost = sim.NextBatchPrice(ds.id) };
                    b.path.Add(n);
                    options.Add(b);
                    continue;
                }
                var path = Path(sim, n.id);
                if (path == null || path.Count == 0) continue;
                // The pack node is the download right with its first batch.
                options.Add(Option(sim, n, path, ds.dataWeight * XgSim.BatchSamples(ds)));
            }
            foreach (var disc in XgSim.AbilityDiscounts)
            {
                if (disc.ability != ability || disc.samplesFactor >= 1 || sim.DiscountOwned(disc)) continue;
                double gain = XgSim.AbilitySamples[ability] * Factor(sim, ability, new List<XgNode>(), false) * sim.RelicSamplesFactor * (1 - disc.samplesFactor);
                XgDataBuy cheapest = null;
                foreach (var id in disc.item.Length > 0 ? new[] { disc.item } : disc.itemsAny)
                {
                    var path = Path(sim, id);
                    if (path == null || path.Count == 0) continue;
                    var o = Option(sim, XgCatalog.Node(id), path, gain);
                    if (cheapest == null || o.cost < cheapest.cost) cheapest = o;
                }
                if (cheapest != null) options.Add(cheapest);
            }
            XgDataBuy best = null;
            foreach (var o in options)
                if (o.gain + 1e-9 >= gap && (best == null || o.cost < best.cost)) best = o;
            if (best != null) return best;
            foreach (var o in options)
                if (best == null || o.gain / Math.Max(1, o.cost) > best.gain / Math.Max(1, best.cost)) best = o;
            return best;
        }

        static XgDataBuy Option(XgSim sim, XgNode target, List<XgNode> path, double gain)
        {
            var o = new XgDataBuy { target = target, gain = gain };
            o.path.AddRange(Ordered(path));
            foreach (var n in o.path) o.cost += sim.NodeCost(n);
            return o;
        }

        /// <summary>A threshold factor after the items owned or about to be bought.</summary>
        static double Factor(XgSim sim, int ability, List<XgNode> buying, bool parameters)
        {
            double f = 1;
            foreach (var d in XgSim.AbilityDiscounts)
            {
                if (d.ability != ability) continue;
                bool have = sim.DiscountOwned(d) || buying.Exists(n => n.id == d.item || Array.IndexOf(d.itemsAny, n.id) >= 0);
                if (have) f *= parameters ? d.paramsFactor : d.samplesFactor;
            }
            return f;
        }

        struct Rung { public int value; public List<XgNode> path; }

        /// <summary>The caps a track can reach on one ladder (depth or width): the current cap for free, then each step on sale.</summary>
        static List<Rung> Ladder(XgSim sim, XgTrack track, XgNodeKind kind, int cap)
        {
            var rungs = new List<Rung> { new Rung { value = cap, path = new List<XgNode>() } };
            string tree = XgSim.TreeOf(track);
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.kind != kind || n.tree != tree && n.tree != "trunk" || sim.Has(n.id) || n.value <= cap) continue;
                var path = Path(sim, n.id);
                if (path != null) rungs.Add(new Rung { value = n.value, path = path });
            }
            return rungs;
        }

        /// <summary>The nodes still to buy before <paramref name="id"/> is owned (prerequisites first), or null when one of them is not on sale.</summary>
        static List<XgNode> Path(XgSim sim, string id)
        {
            var list = new List<XgNode>();
            return AddPath(sim, id, list, 0) ? list : null;
        }

        static bool AddPath(XgSim sim, string id, List<XgNode> list, int depth)
        {
            if (string.IsNullOrEmpty(id) || sim.Has(id)) return true;
            var n = XgCatalog.Node(id);
            if (n == null || depth > 32 || n.tree == "label" || n.tree == "atlas") return false;
            if (n.kind == XgNodeKind.Secret || n.kind == XgNodeKind.Project || n.kind == XgNodeKind.Breakthrough) return false;
            if (!sim.NodeVisible(n) || sim.ProgressionBlocker(n) != null) return false;
            if (!AddPath(sim, n.parent, list, depth + 1)) return false;
            foreach (var need in n.needs) if (!AddPath(sim, need, list, depth + 1)) return false;
            if (!list.Contains(n)) list.Add(n);
            return true;
        }

        /// <summary>Buying order: every node after its prerequisites, otherwise as listed (structure, depth, width).</summary>
        static List<XgNode> Ordered(List<XgNode> nodes)
        {
            var done = new List<XgNode>();
            var left = new List<XgNode>(nodes);
            while (left.Count > 0)
            {
                int pick = left.FindIndex(n => !left.Exists(o => o != n && (o.id == n.parent || Array.IndexOf(n.needs, o.id) >= 0)));
                if (pick < 0) pick = 0;
                done.Add(left[pick]); left.RemoveAt(pick);
            }
            return done;
        }

        // ───────────── stage six and the ending ─────────────

        static void Finale(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            var s = sim.S;
            if (s.ending.Length > 0)
            {
                // The curtain call (XgSim.Origin.cs): one question left before the night is over.
                if (sim.OfferOriginQuestion)
                {
                    list.Add(Step("origin.ask", XgGuideKind.Done, "问问它：你是怎么被训练出来的？", "Ask it: how were you trained?",
                        "大半年，从「是。否。」到今天。", "Most of a year, from \"yes. no.\" to now.", YY, YYChat, "label:" + sim.OriginQuestion, ""));
                    return;
                }
                list.Add(Step("done", XgGuideKind.Done, "都做完了。早点睡。", "All done. Get some sleep.",
                    "2016 年的最后一天。", "The last day of 2016.", "", "", "", ""));
                return;
            }
            if (!s.abilities)
            {
                bool dc = sim.Has("datacenter");
                bool plateau = s.pretrain >= sim.PretrainCap - 1e-9 && !sim.PretrainScaleReady;
                if (!dc && s.pretrainStalled)
                {
                    var node = XgCatalog.Node("datacenter");
                    var step = Step("final.datacenter", XgGuideKind.Finale, "科技买「" + node.name + "」", "Buy the '" + node.nameEn + "' in the tech tree",
                        "一台机箱扛不住预训练，一开就跳闸。", "One case cannot run pre-training; it trips the breaker.", Lab, "tree", "node:datacenter", "");
                    double cost = sim.NodeCost(node);
                    if (wallet.money + 1e-9 < cost) { step.current = wallet.money; step.goal = cost; step.money = true; }
                    list.Add(step);
                }
                else if (dc && plateau)
                {
                    var stall = Step("final.scale", XgGuideKind.Finale, "loss 不动了：" + sim.PretrainLimit(), "The loss is flat: " + sim.PretrainLimit(),
                        "预训练要有效规模＝参数 × 数据 × 道具倍数，不是要更久：倍数乘在一起，每补一件 loss 就低一截。", "Pre-training needs effective scale = parameters × data × item multipliers, not more time: the multipliers stack, and each one lowers the loss a step.", Lab, "tree", "", "");
                    stall.current = Math.Floor(Math.Min(1, sim.ScaleRatio) * 100); stall.goal = 100;
                    list.Add(stall);
                    ScaleItemSteps(sim, wallet, list);
                    var secret = XgCatalog.Node("secret.6");
                    if (secret != null && !sim.Has(secret.id) && sim.NodeVisible(secret))
                        list.Add(Step("final.secret", XgGuideKind.Finale, "实在卡住：科技买秘籍「" + secret.name + "」", "Still stuck: buy the secret '" + secret.nameEn + "'",
                            "自己跑通能拿奖金，买了就没有了。", "Working it out yourself pays a bonus; buying it gives that up.", Lab, "tree", "node:secret.6", ""));
                    if (!s.pretrainRunning)
                        list.Add(Step("final.pretrain", XgGuideKind.Finale, "买齐后回终章页继续预训练", "Then resume pre-training on the Finale page",
                            "规模上去了，loss 才会接着降。", "With scale the loss keeps falling.", Lab, "final", "label:开始预训练|Start pre-training", ""));
                }
                else if (s.pretrainRunning)
                {
                    var step = Step("final.wait", XgGuideKind.Finale, "等预训练跑完", "Let pre-training finish",
                        "所有数据合在一起喂给它。", "Everything is being fed to it at once.", Lab, "final", "", "");
                    step.current = Math.Floor(s.pretrain * 100); step.goal = 100;
                    list.Add(step);
                    if (!sim.PretrainScaleReady) ScaleItemSteps(sim, wallet, list);
                }
                else
                {
                    list.Add(Step("final.pretrain", XgGuideKind.Finale, s.pretrain > 0 ? "终章页继续预训练" : "终章页点「开始预训练」", s.pretrain > 0 ? "Resume pre-training on the Finale page" : "Press 'Start pre-training' on the Finale page",
                        "最后一步：把所有数据一起喂给它。", "The last step: feed it everything at once.", Lab, "final", "label:开始预训练|Start pre-training", ""));
                    if (!sim.PretrainScaleReady) ScaleItemSteps(sim, wallet, list);
                }
                return;
            }
            if (s.alignDone < XgSim.AlignCards)
            {
                var step = Step("final.align", XgGuideKind.Finale, "终章页挑更好的回答", "Pick the better replies on the Finale page",
                    "你选什么，它就学成什么样。", "It becomes whatever you pick.", Lab, "final", "label:A 更好|A is better", "");
                step.current = s.alignDone; step.goal = XgSim.AlignCards;
                list.Add(step);
                return;
            }
            if (s.examDone < XgSim.ExamQuestions)
            {
                var step = Step("final.exam", XgGuideKind.Finale, "终章页给它出终测题", "Give it the final test on the Finale page",
                    "看看它学成了什么样。", "See what it has become.", Lab, "final", "label:出第|Question", "");
                step.current = s.examDone; step.goal = XgSim.ExamQuestions;
                list.Add(step);
                return;
            }
            if (!s.letterRead)
            {
                list.Add(Step("final.letter", XgGuideKind.Finale, "去摆渡搜那串长数字", "Search the long number on Bodu",
                    "终测最后一题里就是那串数字。", "The number is in the last question of the test.", Bodu, "", "", ""));
                return;
            }
            list.Add(Step("final.rules", XgGuideKind.Finale, "写下它的底层规则（最多 " + XgSim.MaxRules + " 条）", "Write its lowest rules (up to " + XgSim.MaxRules + ")",
                "写下就改不了了。想清楚。", "Once written they never change. Think it through.", Lab, "final", "label:写入|Write", ""));
        }

        /// <summary>
        /// The stage-6 multipliers still missing, biggest first, as buys (at most three; only those on sale). The first
        /// shows the money still needed.
        /// </summary>
        static void ScaleItemSteps(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            int added = 0;
            foreach (var item in sim.MissingScaleItems())
            {
                if (added >= 3) break;
                var path = Path(sim, item.item);
                if (path == null || path.Count == 0 || list.Exists(x => x.target == "node:" + path[0].id)) continue;
                var ordered = Ordered(path);
                string factor = "×" + item.factor.ToString("0.#", CultureInfo.InvariantCulture);
                var step = Step("need." + item.item, XgGuideKind.Finale, "科技买" + Chain(ordered, false) + "：有效规模 " + factor, "Tech tree: buy " + Chain(ordered, true) + ": effective scale " + factor,
                    "预训练的有效规模是道具倍数乘出来的，「" + item.name + "」" + factor + "。", "Pre-training's effective scale is the item multipliers stacked; " + item.nameEn + " is " + factor + ".",
                    Lab, "tree", "node:" + ordered[0].id, "");
                if (added == 0) MoneyProgress(sim, wallet, ordered[0], step);
                list.Add(step);
                added++;
            }
        }

        // ───────────── boosts and side places ─────────────

        /// <summary>
        /// One cheap buy that is affordable now. Nodes that move the next ability's bars come first; while an ability
        /// step is waiting, nothing else is suggested (a cheap 权重 or 偏置 would only be a detour).
        /// </summary>
        static void CheapNode(XgSim sim, Wallet wallet, List<XgGuideStep> list, HashSet<string> advancing)
        {
            bool abilityPending = list.Exists(x => x.kind == XgGuideKind.Ability);
            XgNode best = null; bool bestAdvances = false;
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.tree == "label" || n.tree == "atlas" || n.kind == XgNodeKind.Secret || n.kind == XgNodeKind.Project) continue;
                if (list.Exists(x => x.id == "need." + n.id || x.target == "node:" + n.id)) continue;
                bool advances = advancing.Contains(n.id);
                if (abilityPending && !advances) continue;
                double cost = sim.NodeCost(n);
                if (cost <= 0 || cost > wallet.money * CheapNodeShare || sim.Status(n, wallet) != XgSim.NodeStatus.Buyable) continue;
                if (best == null || advances && !bestAdvances || advances == bestAdvances && cost < sim.NodeCost(best)) { best = n; bestAdvances = advances; }
            }
            if (best == null) return;
            list.Add(Step("node." + best.id, XgGuideKind.Boost, "科技买「" + best.name + "」", "Buy '" + best.nameEn + "' in the tech tree",
                bestAdvances ? "不贵，也离下一项能力更近一步。" : "不贵，买得起。", bestAdvances ? "Cheap, and a step towards the next ability." : "Cheap, and you can afford it.", Lab, "tree", "node:" + best.id, ""));
        }

        /// <summary>自动答题 once the idea has come and a checkpoint is good enough: the model labels for you.</summary>
        static void AutoLabel(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            if (sim.S.stage >= 6 || sim.GlobalAutoLevel > 0 || !sim.CanBuyGlobalAuto(out _)) return;
            var node = XgCatalog.Node("label.auto");
            if (node == null) return;
            var s = Step("label.auto", XgGuideKind.Boost, "标注台买「" + node.name + "」", "Buy '" + node.nameEn + "' on the labelling page",
                "让模型替你答题：每张能用的桌都会自己标，钱和样本照拿。", "Let the model answer for you: every eligible desk labels itself, money and samples included.", Crowd, "label", "name:Row自", "");
            MoneyProgress(sim, wallet, node, s);
            list.Add(s);
        }

        static void Raise(XgSim sim, double money, List<XgGuideStep> list)
        {
            // From stage 4 orders pay far more than hand labels: a raise is no longer worth a line.
            if (sim.S.stage > 3 || sim.RaiseLevel >= XgCatalog.RaiseMax || sim.NextRaiseCost > money * CheapRaiseShare) return;
            list.Add(Step("raise", XgGuideKind.Boost, "标注台给自己加薪", "Give yourself a raise on the labelling page",
                "手动标注的报酬一级比一级高。", "Hand labelling pays more with every raise.", Lab, "teach", "name:Row薪", ""));
        }

        /// <summary>
        /// Never empty, and never pointing at a page that is not open yet: a step on a closed lab tab is dropped, and a
        /// note with nothing left says to keep labelling or training. One optional side task goes on the third line,
        /// and only when two real steps sit above it.
        /// </summary>
        static List<XgGuideStep> Finish(List<XgGuideStep> list, XgSim sim, XgGuideHouse house)
        {
            list.RemoveAll(x => !TabOpen(sim, x.app, x.tab));
            // Two lines naming the same purchase (科技买「X」 from the parameter plan and the data plan) keep the first.
            var seen = new HashSet<string>();
            list.RemoveAll(x => x.target.StartsWith("node:") && !seen.Add(x.target));
            // 第一阶段全程引导 (XgSim.StageOneTutor.cs): after the opening, stage one keeps to the main line — no side task,
            // no boost, at most two lines — and each step's hover text leads with its lesson in plain words.
            bool tutor = sim.S.stage == 1 && !sim.OpeningFocus;
            if (tutor)
            {
                list.RemoveAll(x => x.kind == XgGuideKind.Boost || x.kind == XgGuideKind.Side);
                if (list.Count > TutorLines) list.RemoveRange(TutorLines, list.Count - TutorLines);
            }
            if (list.Count == 0) list.Add(Keep(sim));
            var side = list.Count >= 2 && !sim.OpeningFocus && !tutor ? Side(sim, house) : null;
            if (side != null) list.Insert(2, side);
            if (tutor)
                foreach (var step in list)
                {
                    var lesson = sim.TutorFor(step);
                    if (lesson == null || step.whyZh.StartsWith(lesson.whyZh)) continue;
                    step.whyZh = lesson.whyZh + (step.whyZh.Length > 0 ? "\n" + step.whyZh : "");
                    step.whyEn = lesson.whyEn + (step.whyEn.Length > 0 ? "\n" + step.whyEn : "");
                }
            return list;
        }

        /// <summary>Lines the note shows in stage one after the opening: the main step and one more.</summary>
        public const int TutorLines = 2;

        /// <summary>The fallback that always exists: keep labelling (before training opens) or keep training.</summary>
        public static XgGuideStep Keep(XgSim sim)
        {
            if (sim != null && TabOpen(sim, Lab, "train"))
                return Step("keep", XgGuideKind.Main, "继续训练，刷新成绩", "Keep training and beat the score",
                    "成绩越好，订单越多。", "Better scores open better contracts.", Lab, "train", "label:训练一轮|Train 1 epoch", "");
            return Step("keep", XgGuideKind.Main, "继续标注", "Keep labelling",
                "标对有钱拿，也是它的数据。", "Right answers pay, and they are its data.", Crowd, "label", "label:是|Yes|对|True", "");
        }

        /// <summary>
        /// Whether a step's page can be shown: 灵光's tabs open with progress (XingGuangView.FeatureOpen mirrors this),
        /// 摆渡众包's orders with the first checkpoint. Other apps are always there.
        /// </summary>
        public static bool TabOpen(XgSim sim, string app, string tab)
        {
            if (sim == null || string.IsNullOrEmpty(tab)) return true;
            if (app == Crowd) return !sim.OpeningLocksCrowdTab(tab) && (tab != "contracts" || sim.FeatureVisible("contracts"));
            if (app != Lab) return true;
            if (sim.OpeningLocksLabTab(tab)) return false;
            switch (tab)
            {
                case "home": case "abilities": return true;
                case "vision": case "sequence": return sim.FeatureVisible("train");
                case "data": return sim.FeatureVisible("train") || sim.TrainedSamples > 0;
                case "items": return sim.FeatureVisible("tree");
                default: return sim.FeatureVisible(tab);
            }
        }

        static XgGuideStep Side(XgSim sim, XgGuideHouse house)
        {
            if (!house.appInstalled || sim.S.ending.Length > 0) return null;
            if (!house.forumSeen)
                return Step("side.forum", XgGuideKind.Side, "有空逛逛摆渡贴吧", "Browse the Bodu forum some time",
                    "炼丹的人都在那儿聊。", "Everyone training models hangs out there.", Tieba, "home", "", "");
            if (!house.yySeen)
                return Step("side.yy", XgGuideKind.Side, "上 YY 看看大家在聊啥", "Check what people are saying on YY",
                    "阿杰他们都在线。", "Ajie and the others are online.", YY, "", "", "");
            if (!house.gamesSeen && sim.S.epochs > 0 && sim.SystemUnlocked("games"))
                return Step("side.games", XgGuideKind.Side, "累了去游戏中心下盘棋", "Tired? Play a game of chess in the Game Hub",
                    "训练可以先放一放。", "Training can wait a moment.", Games, "", "", "");
            return null;
        }

        static XgGuideStep Step(string id, XgGuideKind kind, string zh, string en, string whyZh, string whyEn, string app, string tab, string target, string arg, double current = 0, double goal = 0)
            => new XgGuideStep { id = id, kind = kind, zh = zh, en = en, whyZh = whyZh, whyEn = whyEn, app = app, tab = tab ?? "", target = target ?? "", arg = arg ?? "", current = current, goal = goal };

        /// <summary>Lets the skill-tree rules judge affordability against the household wallet without touching it.</summary>
        sealed class Wallet : IXgHost
        {
            public double money, vram;
            public double Compute => 1;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double amount) => false;
            public void Earn(double amount) { }
            public void Train(double seconds) { }
            public string Blocker => null;
        }
    }
}
