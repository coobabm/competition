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
        /// <summary>VRAM the host offers (0 = unknown; the VRAM check is skipped).</summary>
        public double vramMB;
        /// <summary>Side places the player has already looked at.</summary>
        public bool forumSeen, yySeen, gamesSeen;
        /// <summary>周而复始 still answers private messages.</summary>
        public bool zhouAvailable = true;
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

            Blockers(sim, house, list);
            if (!house.appInstalled)
            {
                list.Add(Step("install", XgGuideKind.Setup, "在 YY 里收下老周发的" + LingGuangV05.Core.AppNames.ExeZh, "Accept " + LingGuangV05.Core.AppNames.ExeEn + " from Lao Zhou in YY",
                    "老周说这个程序能自己学东西。", "Lao Zhou says this program learns by itself.", YY, "", "", "laozhou"));
                return Finish(list, sim, house);
            }
            Quality(sim, list);

            if (sim.S.stage >= 6) { Finale(sim, wallet, list); Contracts(sim, list); Raise(sim, money, list); return Finish(list, sim, house); }

            bool trainable = sim.TrainingUnlocked(XgTrack.Vision) || sim.TrainingUnlocked(XgTrack.Sequence);
            if (!trainable) Label(sim, list);
            else if (sim.S.epochs == 0) Train(sim, list);
            else if (sim.S.assessments == 0) Assess(sim, list);

            Contracts(sim, list);
            if (trainable)
            {
                // A model that stopped improving gets the plain reason and where to fix it (XgChanceHint.cs).
                XgChanceHint.Add(sim, list);
                NextAbility(sim, list);
            }
            CheapNode(sim, wallet, list);
            Raise(sim, money, list);
            return Finish(list, sim, house);
        }

        // ───────────── blockers ─────────────

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
                Crowd, "label", "label:是|Yes", desk.id);
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
        /// The main line (参数量与数据量主线): the next ability and the shorter of its two bars. Parameters only count
        /// once a model of that size is assessed at grade C, so a big enough model is trained on; a small one grows.
        /// </summary>
        static void NextAbility(XgSim sim, List<XgGuideStep> list)
        {
            int next = sim.NextAbility;
            if (next == 0) return;
            string name = XgSim.AbilityName(next, false), nameEn = XgSim.AbilityName(next, true);
            double p = sim.TrainedParamsK, pNeed = sim.ParamsThreshold(next), d = sim.TrainedSamples, dNeed = sim.SamplesThreshold(next);
            bool paramsShort = p + 1e-9 < pNeed, dataShort = d + 1e-9 < dNeed;
            if (!paramsShort && !dataShort) return;
            var track = TrainableTrack(sim);
            if (paramsShort && (!dataShort || p / pNeed <= d / dNeed))
            {
                string have = XgSim.ParamsText(p), need = XgSim.ParamsText(pNeed);
                if (XgSim.ParamsK(sim.Run(track)) + 1e-9 >= pNeed)
                    list.Add(Step("ability.train", XgGuideKind.Ability, "把这个大模型练到 C 级：参数 " + have + " / " + need, "Train this big model to grade C: parameters " + have + " / " + need,
                        "「" + name + "」要练过的参数够数。评估到 C 级，这个模型的参数才算数。", "'" + nameEn + "' needs enough trained parameters. They count once this model is assessed at grade C.",
                        Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(track)));
                else
                    list.Add(Step("ability.params", XgGuideKind.Ability, "参数量不够：科技里加宽、加深，再练到 C 级：参数 " + have + " / " + need, "Not enough parameters: widen or deepen in the tech tree, then train to grade C: parameters " + have + " / " + need,
                        "下一项能力「" + name + "」要更大的模型。买到宽度和层数，或者参数更多的结构，模型自己就会长大。", "The next ability, '" + nameEn + "', needs a bigger model. Buy width and layers, or a bigger structure, and the model grows by itself.",
                        Lab, "tree", "", ""));
                return;
            }
            var s = Step("ability.data", XgGuideKind.Ability, "攒数据：样本", "Gather data: samples",
                "下一项能力「" + name + "」要更多数据。标注台答题，或在科技买数据包。", "The next ability, '" + nameEn + "', needs more data. Label on the desk, or buy data packs in the tech tree.",
                Lab, "tree", "", "");
            s.current = d; s.goal = dNeed;
            list.Add(s);
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
                        "半年，从「是。否。」到今天。", "Half a year, from \"yes. no.\" to now.", Lab, "chat", "label:" + sim.OriginQuestion, ""));
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
                    list.Add(Step("final.scale", XgGuideKind.Finale, "loss 不动了：" + sim.PretrainLimit(), "The loss is flat: " + sim.PretrainLimit(),
                        "预训练要规模（参数和数据），不是要更久：每加一倍，loss 就低一截。", "Pre-training needs scale (parameters and data), not more time: every doubling lowers the loss a step.", Lab, "tree", "", ""));
                    foreach (var id in new[] { "warmup", "position" })
                    {
                        var n = XgCatalog.Node(id);
                        if (n == null || sim.Has(id)) continue;
                        list.Add(Step("need." + id, XgGuideKind.Finale, "科技买「" + n.name + "」", "Buy '" + n.nameEn + "' in the tech tree",
                            "预训练要用到这个技巧，买到就自动开。", "Pre-training needs this technique; it switches on once bought.", Lab, "tree", "node:" + id, ""));
                    }
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
                }
                else
                    list.Add(Step("final.pretrain", XgGuideKind.Finale, s.pretrain > 0 ? "终章页继续预训练" : "终章页点「开始预训练」", s.pretrain > 0 ? "Resume pre-training on the Finale page" : "Press 'Start pre-training' on the Finale page",
                        "最后一步：把所有数据一起喂给它。", "The last step: feed it everything at once.", Lab, "final", "label:开始预训练|Start pre-training", ""));
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

        // ───────────── boosts and side places ─────────────

        static void CheapNode(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            XgNode best = null;
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.tree == "label" || n.tree == "atlas" || n.kind == XgNodeKind.Secret || n.kind == XgNodeKind.Project) continue;
                if (list.Exists(x => x.id == "need." + n.id)) continue;
                double cost = sim.NodeCost(n);
                if (cost <= 0 || cost > wallet.money * CheapNodeShare || sim.Status(n, wallet) != XgSim.NodeStatus.Buyable) continue;
                if (best == null || cost < sim.NodeCost(best)) best = n;
            }
            if (best == null) return;
            list.Add(Step("node." + best.id, XgGuideKind.Boost, "科技买「" + best.name + "」", "Buy '" + best.nameEn + "' in the tech tree",
                "不贵，买得起。", "Cheap, and you can afford it.", Lab, "tree", "node:" + best.id, ""));
        }

        static void Raise(XgSim sim, double money, List<XgGuideStep> list)
        {
            if (sim.RaiseLevel >= XgCatalog.RaiseMax || sim.NextRaiseCost > money * CheapRaiseShare) return;
            list.Add(Step("raise", XgGuideKind.Boost, "标注台给自己加薪", "Give yourself a raise on the labelling page",
                "手动标注的报酬一级比一级高。", "Hand labelling pays more with every raise.", Crowd, "label", "name:Row薪", ""));
        }

        /// <summary>Never empty. One optional side task goes on the third line, and only when two real steps sit above it.</summary>
        static List<XgGuideStep> Finish(List<XgGuideStep> list, XgSim sim, XgGuideHouse house)
        {
            if (list.Count == 0)
                list.Add(Step("keep", XgGuideKind.Main, "继续训练，刷新成绩", "Keep training and beat the score",
                    "成绩越好，订单越多。", "Better scores open better contracts.", Lab, "train", "label:训练一轮|Train 1 epoch", ""));
            var side = list.Count >= 2 ? Side(sim, house) : null;
            if (side != null) list.Insert(2, side);
            return list;
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
