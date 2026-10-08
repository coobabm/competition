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
            Quality(sim, list);

            if (sim.S.stage >= 6) { Finale(sim, wallet, list); Contracts(sim, list); Raise(sim, money, list); return Finish(list, sim, house); }

            bool trainable = sim.TrainingUnlocked(XgTrack.Vision) || sim.TrainingUnlocked(XgTrack.Sequence);
            if (!trainable) Label(sim, list);
            else if (sim.S.epochs == 0) Train(sim, list);
            else if (sim.S.assessments == 0) Assess(sim, list);

            Contracts(sim, list);
            var advancing = new HashSet<string>();
            if (trainable)
            {
                NextAbility(sim, wallet, list, advancing);
                // A model that stopped improving gets the plain reason and where to fix it (XgChanceHint.cs). While the
                // next ability's steps already name what to buy or label, a plateau for parameters or data says nothing new.
                var hint = XgChanceHint.Step(sim);
                bool covered = hint != null && list.Exists(x => x.kind == XgGuideKind.Ability) && (hint.id == "plateau.params" || hint.id == "plateau.data");
                if (hint != null && !covered) list.Add(hint);
            }
            AutoLabel(sim, wallet, list);
            CheapNode(sim, wallet, list, advancing);
            Raise(sim, money, list);
            return Finish(list, sim, house);
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
                house.landlordCut ? "房东拉了电闸，显卡全停；手动标注照样有钱。再拖一天，电脑就得卖了。" : "钱包见底了，房东在催。标注、订单都能挣；连着三天交不上，电脑就得卖了。",
                house.landlordCut ? "The landlord cut the power and every card has stopped; hand labelling still pays. One more day and the computer has to be sold." : "The wallet is empty and the landlord wants his rent. Labels and contracts both pay; three days without it and the computer has to be sold.",
                Crowd, "label", "label:是|Yes|对|True", ""));
        }

        static void Blockers(XgSim sim, XgGuideHouse house, List<XgGuideStep> list)
        {
            if (house.unpaidPower)
            {
                string due = house.billDue > 0 ? " ¥" + house.billDue.ToString("0.##", CultureInfo.InvariantCulture) : "";
                list.Add(Step("power.bill", XgGuideKind.Blocker, "交电费" + due, "Pay the power bill" + due,
                    "欠费停电了，显卡全停。钱不够就去标注台答题抵扣。", "The power is cut for an unpaid bill; every card is idle. Short of money? Labelling pays it off.",
                    Home, "power", "name:PayBill", ""));
            }
            if (house.breakerTripped)
                list.Add(Step("power.breaker", XgGuideKind.Blocker, house.overloaded ? "电闸跳了：少插一张卡，再复位" : "复位电闸", house.overloaded ? "Breaker tripped: pull a card, then reset it" : "Reset the breaker",
                    house.overloaded ? "插的卡太多，电线扛不住。" : "跳闸了，什么都不转。", house.overloaded ? "Too many cards for the wiring." : "The breaker is off; nothing runs.",
                    Home, "power", "name:ResetBreaker", ""));
            else if (house.overloaded && !house.unpaidPower && !house.noGpu)
                list.Add(Step("power.overload", XgGuideKind.Blocker, "超负载了：少插一张卡", "Overloaded: pull a card",
                    "家里的电带不动这么多卡。", "The house cannot power this many cards.", Home, "power", "", ""));
            if (house.noGpu)
                list.Add(Step("gpu.none", XgGuideKind.Blocker, "去" + LingGuangV05.Core.AppNames.ShopZh + "买张显卡", "Buy a graphics card on " + LingGuangV05.Core.AppNames.ShopEn,
                    "没有显卡，什么都训不了。", "No card, no training.", Shop, "shop", "name:BuyGpu", ""));
            if (house.appInstalled && sim.QualityFrozen)
            {
                string clock = XgSim.FreezeClock(sim.FreezeSecondsLeft);
                list.Add(Step("qc.frozen", XgGuideKind.Blocker, "账号被举报：等 " + clock + " 或去标注台申诉", "Account reported: wait " + clock + " or appeal on the labelling page",
                    "摆渡众包冻结了自动标注（" + XgSim.ReportReasonText(sim.LastReportReason, false) + "）。手动标注照常有钱，标对还能挽回信用。",
                    "Bodu Crowdsourcing froze auto labelling (" + XgSim.ReportReasonText(sim.LastReportReason, true) + "). Hand labelling still pays and right answers win back credit.",
                    Crowd, "label", "name:QcAppeal", ""));
            }
            if (house.appInstalled && sim.CaptchaPending)
            {
                string clock = XgSim.FreezeClock(sim.CaptchaSecondsLeft);
                list.Add(Step("qc.captcha", XgGuideKind.Blocker, "摆渡众包要人机验证：去标注台输入验证码（" + clock + "）", "Bodu Crowdsourcing wants a captcha: enter it on the labelling page (" + clock + ")",
                    "标得太快，平台怀疑是机器。答错或超时会暂停自动标注两分钟。", "Labelling this fast looks like a machine. A wrong or late answer pauses auto labelling for two minutes.",
                    Crowd, "label", "name:QcCaptcha", ""));
            }
            if (house.vramMB > 0 && house.appInstalled && sim.S.stage < 6)
            {
                var track = TrainableTrack(sim);
                if (sim.TrainingUnlocked(track) && XgSim.VramNeedMB(sim.Run(track)) > sim.Vram(new Wallet { vram = house.vramMB }))
                    list.Add(Step("vram", XgGuideKind.Blocker, "显存装不下最小的模型：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡", "Even the smallest model does not fit: buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                        "模型是自动配置的，显卡装得下多大就用多大；现在连最小的都放不下。", "The model sizes itself to the card; right now not even the smallest fits.", Shop, "shop", "name:BuyGpu", ""));
            }
        }

        /// <summary>The platform's pass rate is at the warning line: point at the threshold (or the node that adds it).</summary>
        static void Quality(XgSim sim, List<XgGuideStep> list)
        {
            if (!sim.QualityWarning) return;
            bool coop = sim.CollaborationEnabled;
            string pass = Math.Floor(sim.PassRate * 100 + 1e-9).ToString("0", CultureInfo.InvariantCulture) + "%";
            string line = Math.Round(XgSim.QcReportRate * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            list.Add(Step("qc.rate", XgGuideKind.Main, "抽检合格率偏低：调高「拿不准才问我」阈值，或先把模型练好", "Spot-check pass rate is low: raise the 'Ask when unsure' threshold, or train a better model first",
                "摆渡众包近期合格率 " + pass + "；错误率到 " + line + " 账号会被举报冻结。", "Bodu Crowdsourcing's recent pass rate is " + pass + "; at a " + line + " error rate the account is reported and frozen.",
                coop ? Crowd : Lab, coop ? "label" : "tree", coop ? "name:ThresholdSlider" : "node:label.coop", ""));
        }

        // ───────────── getting the loop going ─────────────

        static void Label(XgSim sim, List<XgGuideStep> list)
        {
            var desk = LabelDesk(sim);
            if (desk == null) return;
            double have = sim.Samples(desk.id);
            int need = Math.Max(1, XgCatalog.SamplesToTrain - (int)Math.Floor(have));
            var s = Step("label." + desk.id, XgGuideKind.Setup, "去标注台标 " + need + " 条「" + desk.name + "」", "Label " + need + " more " + desk.nameEn + " cards",
                "攒够 " + XgCatalog.SamplesToTrain + " 条数据才能开始训练。答对还有钱拿。", XgCatalog.SamplesToTrain + " samples unlock training. Right answers pay too.",
                Crowd, "label", "label:是|Yes|对|True", desk.id);
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
            list.Add(Step("contract." + best.id, XgGuideKind.Main, "去摆渡众包签「" + best.job + "」", "Sign '" + best.jobEn + "' in Bodu Crowd",
                "成绩够了，签下来以后每秒都有进账。", "The score is good enough; once signed it pays every second.", Crowd, "contracts", "contract:" + best.id, ""));
        }

        /// <summary>
        /// The main line (参数量与数据量主线): the next ability, as concrete steps. The parameter bar comes first: what to
        /// buy, in order (<see cref="ParamsPlan"/>), then training that model to grade C and assessing it. The data bar
        /// follows: the data pack or item that closes most of the gap for its price (<see cref="DataPlan"/>), then how
        /// many more labels on the desks that count in full. Every node these steps would buy goes into
        /// <paramref name="advancing"/>, so the cheap-boost line can prefer them.
        /// </summary>
        static void NextAbility(XgSim sim, Wallet wallet, List<XgGuideStep> list, HashSet<string> advancing)
        {
            int next = sim.NextAbility;
            // Before the first record the 科技 page is still closed; assessing (a setup step) opens it.
            if (next == 0 || !TabOpen(sim, Lab, "tree")) return;
            if (sim.TrainedParamsK + 1e-9 < sim.ParamsThreshold(next)) ParamsSteps(sim, wallet, next, list, advancing);
            if (sim.TrainedSamples + 1e-9 < sim.SamplesThreshold(next)) DataSteps(sim, wallet, next, list, advancing);
        }

        static void ParamsSteps(XgSim sim, Wallet wallet, int next, List<XgGuideStep> list, HashSet<string> advancing)
        {
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            double pNeed = sim.ParamsThreshold(next);
            string have = XgSim.ParamsText(sim.TrainedParamsK), need = XgSim.ParamsText(pNeed);
            var plan = ParamsPlan(sim, next, wallet.vram > 0 ? sim.Vram(wallet) : 0);
            if (plan == null)
            {
                // Nothing on sale at this stage reaches it (or the card is too small): the general advice.
                bool card = wallet.vram > 0 && ParamsPlan(sim, next, 0) != null;
                if (card)
                    list.Add(Step("ability.card", XgGuideKind.Ability, "显存放不下够大的模型：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡", "Not enough VRAM for a big enough model: buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                        "「" + name + "」要练过 " + need + " 参数，这张卡装不下那么大的模型。", "'" + nameEn + "' needs " + need + " trained parameters; this card cannot hold a model that big.", Shop, "shop", "name:BuyGpu", ""));
                else
                    list.Add(Step("ability.params", XgGuideKind.Ability, "参数量不够：科技里加宽、加深，再练到 C 级：参数 " + have + " / " + need, "Not enough parameters: widen or deepen in the tech tree, then train to grade C: parameters " + have + " / " + need,
                        "下一项能力「" + name + "」要更大的模型。买到宽度和层数，或者参数更多的结构，模型自己就会长大。", "The next ability, '" + nameEn + "', needs a bigger model. Buy width and layers, or a bigger structure, and the model grows by itself.",
                        Lab, "tree", "", ""));
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
                    "「" + name + "」要 " + need + " 参数，这条线的模型才长得到。先攒够 " + XgCatalog.SamplesToTrain + " 条数据。",
                    "'" + nameEn + "' needs " + need + " parameters, and only this line's model grows that big. It needs " + XgCatalog.SamplesToTrain + " samples first.",
                    Crowd, "label", "label:是|Yes|对|True", desk.id);
                t.current = samples; t.goal = XgCatalog.SamplesToTrain;
                list.Add(t);
            }
            if (plan.buy.Count > 0)
            {
                foreach (var n in plan.buy) advancing.Add(n.id);
                var first = plan.buy[0];
                var s = Step("ability.buy", XgGuideKind.Ability, "科技买" + Chain(plan.buy, false), "Tech tree: buy " + Chain(plan.buy, true),
                    "「" + name + "」要练过 " + XgSim.ParamsText(plan.threshold) + " 参数（现在 " + have + "）。买齐这些，模型自己长到 " + XgSim.ParamsText(plan.paramsK) + "，再练到 C 级就算数。" + ShortOfMoney(sim, wallet, first, false),
                    "'" + nameEn + "' needs " + XgSim.ParamsText(plan.threshold) + " trained parameters (now " + have + "). With these the model grows to " + XgSim.ParamsText(plan.paramsK) + " by itself; trained to grade C, it counts." + ShortOfMoney(sim, wallet, first, true),
                    Lab, "tree", "node:" + first.id, "");
                MoneyProgress(sim, wallet, first, s);
                list.Add(s);
                return;
            }
            if (!sim.TrainingUnlocked(plan.track)) return;
            // The caps already allow a big enough model: train the one the lab configured until grade C, then assess it.
            var run = sim.Run(plan.track);
            string arg = TrackArg(plan.track);
            if (XgSim.ParamsK(run) + 1e-9 < pNeed)
            {
                // The lab configured a smaller one: the card is shared or too small for the big one.
                list.Add(Step("ability.card", XgGuideKind.Ability, "显存放不下 " + need + " 的模型：去" + LingGuangV05.Core.AppNames.ShopZh + "加显卡", "Not enough VRAM for a " + need + " model: buy a card on " + LingGuangV05.Core.AppNames.ShopEn,
                    "模型按显卡自动配置，现在只放得下 " + XgSim.ParamsText(XgSim.ParamsK(run)) + "。", "The model sizes itself to the card; right now it fits " + XgSim.ParamsText(XgSim.ParamsK(run)) + ".", Shop, "shop", "name:BuyGpu", ""));
                return;
            }
            bool gradeC = XgSim.Grade(XgSim.Score(run.dataset, run.valAcc)) >= XgSim.TrainedGrade && run.shapeRounds > 0;
            if (gradeC)
                list.Add(Step("ability.assess", XgGuideKind.Ability, "评估一次：模型到 C 级了，参数就算数：" + have + " / " + need, "Assess once: the model is at grade C, so its parameters count: " + have + " / " + need,
                    "「" + name + "」只认评估过的模型。这个 " + XgSim.ParamsText(XgSim.ParamsK(run)) + " 的模型评到 C 级，参数条就满了。", "'" + nameEn + "' only counts assessed models. Grade this " + XgSim.ParamsText(XgSim.ParamsK(run)) + " model C and the parameter bar is full.",
                    Lab, "train", "label:评估|Assess", arg));
            else
                list.Add(Step("ability.train", XgGuideKind.Ability, "把模型练到 C 级：参数 " + have + " / " + need, "Train the model to grade C: parameters " + have + " / " + need,
                    "模型已经有 " + XgSim.ParamsText(XgSim.ParamsK(run)) + " 参数，够「" + name + "」了。多练几轮，评估到 C 级就算数。", "The model already has " + XgSim.ParamsText(XgSim.ParamsK(run)) + " parameters, enough for '" + nameEn + "'. Train a few epochs; assessed at grade C, they count.",
                    Lab, "train", "label:训练一轮|Train 1 epoch", arg));
        }

        static void DataSteps(XgSim sim, Wallet wallet, int next, List<XgGuideStep> list, HashSet<string> advancing)
        {
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            double d = sim.TrainedSamples, dNeed = sim.SamplesThreshold(next), gap = dNeed - d;
            // Packs already bought and still downloading.
            double incoming = 0, left = 0; XgDataset waiting = null;
            foreach (var ds in XgCatalog.Datasets)
                if (sim.S.owned.Contains(ds.id) && sim.Downloading(ds.id) && CountsAsData(ds.id))
                {
                    incoming += ds.dataWeight * ds.samples;
                    if (sim.DownloadLeft(ds.id) > left) { left = sim.DownloadLeft(ds.id); waiting = ds; }
                }
            if (waiting != null && incoming + 1e-9 >= gap)
            {
                string secs = Math.Ceiling(left).ToString("0", CultureInfo.InvariantCulture);
                var w = Step("ability.download", XgGuideKind.Ability, "等「" + waiting.name + "」数据包下完（还要 " + secs + " 秒）：样本", "Wait for the " + waiting.nameEn + " pack to download (" + secs + " s left): samples",
                    "下完就够「" + name + "」的数据了。等的时候可以接着标注、训练。", "Once it is in, '" + nameEn + "' has its data. Keep labelling or training meanwhile.", Lab, TabOpen(sim, Lab, "data") ? "data" : "train", "", "");
                w.current = d; w.goal = dNeed;
                list.Add(w);
                return;
            }
            gap -= incoming;
            var buy = DataPlan(sim, next, gap);
            double after = gap;
            if (buy != null)
            {
                foreach (var n in buy.path) advancing.Add(n.id);
                var first = buy.path[0];
                bool pack = buy.target.kind == XgNodeKind.Dataset;
                string what = pack ? "「" + buy.target.name + "」数据包" : "「" + buy.target.name + "」";
                string whatEn = pack ? "the " + buy.target.nameEn + " data pack" : "'" + buy.target.nameEn + "'";
                string zh = buy.path.Count == 1 ? "科技买" + what : "科技买" + Chain(buy.path, false);
                string en = buy.path.Count == 1 ? "Tech tree: buy " + whatEn : "Tech tree: buy " + Chain(buy.path, true);
                string why = pack ? "一次补 " + XgSim.SamplesText(buy.gain) + " 条有效样本，「" + name + "」要 " + XgSim.SamplesText(dNeed) + " 条。"
                    : "它把「" + name + "」要的数据压到 " + XgSim.SamplesText(dNeed - buy.gain) + " 条。";
                string whyEn = pack ? "It adds " + XgSim.SamplesText(buy.gain) + " effective samples at once; '" + nameEn + "' needs " + XgSim.SamplesText(dNeed) + "."
                    : "It lowers the data '" + nameEn + "' needs to " + XgSim.SamplesText(dNeed - buy.gain) + ".";
                var s = Step(pack ? "ability.pack" : "ability.item", XgGuideKind.Ability, zh, en, why + ShortOfMoney(sim, wallet, first, false), whyEn + ShortOfMoney(sim, wallet, first, true),
                    Lab, "tree", "node:" + first.id, "");
                if (!MoneyProgress(sim, wallet, first, s)) { s.current = d; s.goal = dNeed; }
                list.Add(s);
                after = gap - buy.gain;
            }
            if (after <= 1e-9) return;
            // After a buy, labels are only worth naming when they finish the bar in reasonable time; otherwise the
            // next buy is named once this one is in.
            const int LabelsWorthNaming = 1500;
            // Then hand labels on the desks whose samples count in full (算术 counts half).
            var desks = BestDataDesks(sim);
            if (desks.Count == 0)
            {
                if (buy != null) return;
                var g = Step("ability.data", XgGuideKind.Ability, "攒数据：样本", "Gather data: samples",
                    "下一项能力「" + name + "」要更多数据。", "The next ability, '" + nameEn + "', needs more data.", Crowd, "label", "", "");
                g.current = d; g.goal = dNeed;
                list.Add(g);
                return;
            }
            double weight = XgCatalog.Dataset(desks[0].id).dataWeight;
            int labels = (int)Math.Ceiling(after / Math.Max(.01, weight) - 1e-9);
            if (buy != null && labels > LabelsWorthNaming) return;
            var names = new List<string>(); var namesEn = new List<string>();
            foreach (var desk in desks) { names.Add("「" + desk.name + "」"); namesEn.Add(desk.nameEn); }
            bool auto = sim.GlobalAutoLevel > 0;
            string labelZh = auto ? "再攒 " + labels + " 条样本：自动答题在标，手标" + string.Join("或", names) + "更快"
                : (buy != null ? "再标 " : "标注台再标 ") + labels + " 条" + string.Join("或", names);
            string labelEn = auto ? "Gather " + labels + " more samples: auto labelling is on; hand-labelling " + string.Join(" or ", namesEn) + " is faster"
                : "Label " + labels + " more " + string.Join(" or ", namesEn) + " cards";
            bool teaching = desks[0].id == XgMemes.SenseDesk && TeachesYesNo(sim);
            var label = Step("ability.label", XgGuideKind.Ability, labelZh, labelEn,
                "「" + name + "」要 " + XgSim.SamplesText(dNeed) + " 条有效样本（现在 " + XgSim.SamplesText(d) + "）。标对的才算，「算术」只算半条。" + (teaching ? "「常识判断」这桌专教它分清是和否。" : ""),
                "'" + nameEn + "' needs " + XgSim.SamplesText(dNeed) + " effective samples (now " + XgSim.SamplesText(d) + "). Only right answers count; Arithmetic counts half." + (teaching ? " The Common sense desk teaches it yes from no." : ""),
                Crowd, "label", "label:是|Yes|对|True", desks[0].id);
            if (buy == null) { label.current = d; label.goal = dNeed; }
            list.Add(label);
        }

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
        static string Chain(List<XgNode> nodes, bool english)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < nodes.Count; i++)
            {
                string n = english ? nodes[i].nameEn : nodes[i].name;
                // Depth and width squares exist on both lines: name the line (视觉 3 层, 序列 宽 128).
                bool ladder = nodes[i].kind == XgNodeKind.Depth || nodes[i].kind == XgNodeKind.Width;
                if (ladder && nodes[i].tree == "vision") n = (english ? "vision " : "视觉 ") + n;
                else if (ladder && nodes[i].tree == "sequence") n = (english ? "sequence " : "序列 ") + n;
                if (nodes[i].kind == XgNodeKind.Dataset) n += english ? " data pack" : " 数据包";
                if (i == 0) sb.Append(english ? "'" + n + "'" : "「" + n + "」");
                else sb.Append(" → ").Append(n);
            }
            return sb.ToString();
        }

        static string ShortOfMoney(XgSim sim, Wallet wallet, XgNode node, bool english)
        {
            if (wallet.money + 1e-9 >= sim.NodeCost(node)) return "";
            return english ? " Short of money? Label on the desk; right answers pay." : "钱不够就去标注台多标几条，答对有钱。";
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
                            double threshold = XgSim.AbilityParamsK[ability] * Factor(sim, ability, buy, true);
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
                if (n.kind != XgNodeKind.Dataset || sim.Has(n.id) || sim.S.owned.Contains(n.target) || !CountsAsData(n.target)) continue;
                var ds = XgCatalog.Dataset(n.target);
                var path = ds != null ? Path(sim, n.id) : null;
                if (path == null || path.Count == 0) continue;
                options.Add(Option(sim, n, path, ds.dataWeight * ds.samples));
            }
            foreach (var disc in XgSim.AbilityDiscounts)
            {
                if (disc.ability != ability || disc.samplesFactor >= 1 || sim.DiscountOwned(disc)) continue;
                double gain = XgSim.AbilitySamples[ability] * Factor(sim, ability, new List<XgNode>(), false) * (1 - disc.samplesFactor);
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
                        "半年，从「是。否。」到今天。", "Half a year, from \"yes. no.\" to now.", YY, YYChat, "label:" + sim.OriginQuestion, ""));
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
            if (sim.RaiseLevel >= XgCatalog.RaiseMax || sim.NextRaiseCost > money * CheapRaiseShare) return;
            list.Add(Step("raise", XgGuideKind.Boost, "标注台给自己加薪", "Give yourself a raise on the labelling page",
                "手动标注的报酬一级比一级高。", "Hand labelling pays more with every raise.", Crowd, "label", "name:Row薪", ""));
        }

        /// <summary>
        /// Never empty, and never pointing at a page that is not open yet: a step on a closed lab tab is dropped, and a
        /// note with nothing left says to keep labelling or training. One optional side task goes on the third line,
        /// and only when two real steps sit above it.
        /// </summary>
        static List<XgGuideStep> Finish(List<XgGuideStep> list, XgSim sim, XgGuideHouse house)
        {
            list.RemoveAll(x => !TabOpen(sim, x.app, x.tab));
            if (list.Count == 0) list.Add(Keep(sim));
            var side = list.Count >= 2 ? Side(sim, house) : null;
            if (side != null) list.Insert(2, side);
            return list;
        }

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
            if (app == Crowd) return tab != "contracts" || sim.FeatureVisible("contracts");
            if (app != Lab) return true;
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
                    "老周他们都在线。", "Lao Zhou and the others are online.", YY, "", "", "");
            if (!house.gamesSeen && sim.S.epochs > 0)
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
