using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>What a step is about. The sticky note only treats <see cref="Side"/> differently (never the main line).</summary>
    public enum XgGuideKind { Blocker, Setup, Main, Wall, Finale, Boost, Side, Done }

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
        /// <summary>Escalation level for steps that get more direct over time (the wall hint).</summary>
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
    /// (label, train, assess), then the stage's goal (sign what can be signed, push to the wall, get past it, or the
    /// finale's chores), then cheap boosts. At most one optional side task is added, and only as the third line.
    /// It changes nothing and never names a wall's golden setting: the wall hint escalates from the diagnosis page
    /// to the forum to the secret card as minutes pass.
    /// </summary>
    public static class XgGuide
    {
        public const string Lab = "lingguang", Home = "home", Shop = "xunbao", Tieba = "tieba", Bodu = "bodu", YY = "yy", Games = "games";
        /// <summary>Seconds after the wall shows before the hint moves on: to 周而复始, to the forum post, to the secret.</summary>
        public const double ForumAfter = 240, NewbieAfter = 420, SecretAfter = 600;
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
                var wall = sim.ActiveWall;
                if (wall != null) Wall(sim, wall, wallet, house, list);
                else Practice(sim, list);
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
                list.Add(Step("gpu.none", XgGuideKind.Blocker, "去寻宝买张显卡", "Buy a graphics card on the shop",
                    "没有显卡，什么都训不了。", "No card, no training.", Shop, "shop", "name:BuyGpu", ""));
            if (house.appInstalled && sim.QualityFrozen)
            {
                string clock = XgSim.FreezeClock(sim.FreezeSecondsLeft);
                list.Add(Step("qc.frozen", XgGuideKind.Blocker, "账号被举报：等 " + clock + " 或去标注台申诉", "Account reported: wait " + clock + " or appeal on the labelling page",
                    "摆渡众包冻结了自动标注（" + XgSim.ReportReasonText(sim.LastReportReason, false) + "）。手动标注照常有钱，标对还能挽回信用。",
                    "Bodu Crowdsourcing froze auto labelling (" + XgSim.ReportReasonText(sim.LastReportReason, true) + "). Hand labelling still pays and right answers win back credit.",
                    Lab, "label", "name:QcAppeal", ""));
            }
            if (house.appInstalled && sim.CaptchaPending)
            {
                string clock = XgSim.FreezeClock(sim.CaptchaSecondsLeft);
                list.Add(Step("qc.captcha", XgGuideKind.Blocker, "摆渡众包要人机验证：去标注台输入验证码（" + clock + "）", "Bodu Crowdsourcing wants a captcha: enter it on the labelling page (" + clock + ")",
                    "标得太快，平台怀疑是机器。答错或超时会暂停自动标注两分钟。", "Labelling this fast looks like a machine. A wrong or late answer pauses auto labelling for two minutes.",
                    Lab, "label", "name:QcCaptcha", ""));
            }
            if (house.vramMB > 0 && house.appInstalled && sim.S.stage < 6)
            {
                var track = TrainableTrack(sim);
                if (sim.TrainingUnlocked(track) && XgSim.VramNeedMB(sim.Run(track)) > sim.Vram(new Wallet { vram = house.vramMB }))
                    list.Add(Step("vram", XgGuideKind.Blocker, "模型太大放不下：把宽度或层数调小", "The model does not fit: lower the width or layers",
                        "显存不够，这一轮根本跑不起来。", "Not enough VRAM; the epoch cannot even start.", Lab, "train", "name:Width", TrackArg(track)));
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
                Lab, coop ? "label" : "tree", coop ? "name:ThresholdSlider" : "node:label.coop", ""));
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
                Lab, "label", "label:是|Yes", desk.id);
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
            list.Add(Step("contract." + best.id, XgGuideKind.Main, "去订单签「" + best.job + "」", "Sign '" + best.jobEn + "' in Contracts",
                "成绩够了，签下来以后每秒都有进账。", "The score is good enough; once signed it pays every second.", Lab, "contracts", "contract:" + best.id, ""));
        }

        static void Practice(XgSim sim, List<XgGuideStep> list)
        {
            var wall = XgSim.WallFor(sim.S.stage);
            if (wall == null) return;
            int goal = sim.PracticeStrict ? XgSim.PracticeFor(sim.S.stage) : XgSim.WallPracticeEpochs;
            var track = TrainableTrack(sim);
            if (sim.S.stageEpochs < goal)
            {
                var s = Step("practice." + sim.S.stage, XgGuideKind.Main, "继续训练，直到成绩不再上涨", "Keep training until the score stops rising",
                    "多练一阵，就能看见它卡在哪。", "Train for a while and you will see where it gets stuck.", Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(track));
                s.current = sim.S.stageEpochs; s.goal = goal;
                list.Add(s);
            }
            // Stage one's wall also waits for a first grade on either track.
            if (sim.S.stage == 1 && sim.TrackGrade(XgTrack.Vision) < 1 && sim.TrackGrade(XgTrack.Sequence) < 1)
            {
                double best = 0;
                foreach (var b in sim.S.best) if (!sim.IsWallDataset(b.dataset)) best = Math.Max(best, XgSim.Score(b.dataset, b.acc));
                var s = Step("grade." + sim.S.stage, XgGuideKind.Main, "把一项成绩练到 C 级", "Get one score up to grade C",
                    "评估分数过 " + XgCatalog.GradeScore[1] + " 就是 C。", "An assessment of " + XgCatalog.GradeScore[1] + " or more is a C.", Lab, "train", "label:评估|Assess", TrackArg(track));
                s.current = best; s.goal = XgCatalog.GradeScore[1];
                list.Add(s);
            }
        }

        /// <summary>The forum post that talks about the same wall (the desktop falls back to the forum's front page).</summary>
        public static string WallThread(string wallId)
        {
            switch (wallId)
            {
                case "length": return "tip_memory";
                case "translation": return "tip_translate";
                case "degrade": return "tip_deep";
                case "parallel": return "tip_attention";
                default: return "tip_wall";
            }
        }

        static void Wall(XgSim sim, XgWall wall, Wallet wallet, XgGuideHouse house, List<XgGuideStep> list)
        {
            double since = sim.S.wallSeenAt > 0 ? Math.Max(0, sim.S.stageSeconds - sim.S.wallSeenAt) : 0;
            var secret = wall.secret.Length > 0 && !sim.Has(wall.secret) ? XgCatalog.Node(wall.secret) : null;
            int level = since >= SecretAfter && secret != null ? 3 : since >= NewbieAfter ? 2 : since >= ForumAfter ? (house.zhouAvailable ? 1 : 2) : 0;
            string name = wall.name, nameEn = wall.nameEn;
            XgGuideStep hint;
            switch (level)
            {
                case 0:
                    hint = Step("wall." + wall.id, XgGuideKind.Wall, "撞墙了（" + name + "）：看看「诊断」页错在哪", "A wall (" + nameEn + "): see what goes wrong on the Diagnose page",
                        "再练也不涨了。错题的规律就是线索。", "More training no longer helps. The pattern of mistakes is the clue.", Lab, "wall", "name:Diagnosis", "");
                    break;
                case 1:
                    hint = Step("wall." + wall.id, XgGuideKind.Wall, "问问贴吧的周而复始", "Ask 周而复始 on the forum",
                        "他只回答问题，把你看到的错误讲给他听。", "He only answers questions: tell him what goes wrong.", Tieba, "chat", "", "laozhou");
                    break;
                case 2:
                    hint = Step("wall." + wall.id, XgGuideKind.Wall, "看看摆渡贴吧的新手帖", "Read the beginners' posts on the forum",
                        "论坛里有人撞过同一堵墙。", "Someone on the forum hit the same wall.", Tieba, "thread", "", WallThread(wall.id));
                    break;
                default:
                    hint = Step("wall." + wall.id, XgGuideKind.Wall, "实在卡住：技能树买秘籍「" + secret.name + "」", "Still stuck: buy the secret '" + secret.nameEn + "' in the skill tree",
                        "秘籍写明了配法。自己配出来有奖金，买了就没有了。", "The secret spells it out. Working it out yourself pays a bonus; buying gives that up.",
                        Lab, "tree", "node:" + secret.id, "");
                    double cost = sim.NodeCost(secret);
                    if (wallet.money + 1e-9 < cost) { hint.current = wallet.money; hint.goal = cost; hint.money = true; }
                    break;
            }
            hint.level = level;
            list.Add(hint);

            // The wall's own data has to be on the training page to pass it.
            foreach (var check in wall.checks)
            {
                if (sim.WallCheckPassed(wall, check) || check.dataset.StartsWith("*", StringComparison.Ordinal)) continue;
                var d = XgCatalog.Dataset(check.dataset);
                if (d == null || sim.Run(d.track).dataset == d.id) continue;
                list.Add(Step("wall.data." + d.id, XgGuideKind.Wall, "训练页换成「" + d.name + "」来练", "Switch the training page to " + d.nameEn,
                    "这堵墙只认这份数据上的成绩。", "This wall only counts results on this data.", Lab, "train", "label:" + d.name + "|" + d.nameEn, TrackArg(d.track)));
                break;
            }

            // The knobs the wall needs: buy what the wallet covers, name the rest without any setting.
            var missing = new List<string>(); var missingEn = new List<string>();
            string firstMissing = null;
            foreach (var group in wall.needs)
            {
                bool met = false;
                foreach (var id in group) if (sim.Has(id)) { met = true; break; }
                if (met) continue;
                XgNode buy = null;
                foreach (var id in group) { var n = XgCatalog.Node(id); if (n != null && sim.Status(n, wallet) == XgSim.NodeStatus.Buyable) { buy = n; break; } }
                if (buy != null)
                {
                    list.Add(Step("need." + buy.id, XgGuideKind.Wall, "技能树买「" + KnobName(buy, false) + "」", "Buy '" + KnobName(buy, true) + "' in the skill tree",
                        "过这堵墙要用到它。", "Passing this wall needs it.", Lab, "tree", "node:" + buy.id, ""));
                    continue;
                }
                var first = XgCatalog.Node(group[0]);
                if (first == null) continue;
                if (firstMissing == null) firstMissing = first.id;
                missing.Add(KnobName(first, false)); missingEn.Add(KnobName(first, true));
            }
            if (missing.Count > 0)
                list.Add(Step("needs." + wall.id, XgGuideKind.Wall, "准备好需要的旋钮：" + string.Join("、", missing), "Get the knobs it needs: " + string.Join(", ", missingEn),
                    "技能树里还没解锁，或者钱还不够。", "Not unlocked in the skill tree yet, or not affordable yet.", Lab, "tree", "node:" + firstMissing, ""));
        }

        /// <summary>A node's name without numbers for layer and width caps (those numbers are part of golden settings).</summary>
        static string KnobName(XgNode n, bool english)
        {
            if (n.kind == XgNodeKind.Depth) return english ? "more layers" : "更多层数";
            if (n.kind == XgNodeKind.Width) return english ? "a wider layer" : "更宽的层";
            return english ? n.nameEn : n.name;
        }

        // ───────────── stage six and the ending ─────────────

        static void Finale(XgSim sim, Wallet wallet, List<XgGuideStep> list)
        {
            var s = sim.S;
            if (s.ending.Length > 0)
            {
                list.Add(Step("done", XgGuideKind.Done, "都做完了。早点睡。", "All done. Get some sleep.",
                    "2016 年的最后一天。", "The last day of 2016.", "", "", "", ""));
                return;
            }
            if (!s.abilities)
            {
                bool dc = sim.Has("datacenter");
                bool plateau = s.pretrain >= XgSim.PretrainPlateau - 1e-9 && !sim.PretrainScaleReady;
                if (!dc && s.pretrainStalled)
                {
                    var node = XgCatalog.Node("datacenter");
                    var step = Step("final.datacenter", XgGuideKind.Finale, "技能树买「" + node.name + "」", "Buy the '" + node.nameEn + "' in the skill tree",
                        "一台机箱扛不住预训练，一开就跳闸。", "One case cannot run pre-training; it trips the breaker.", Lab, "tree", "node:datacenter", "");
                    double cost = sim.NodeCost(node);
                    if (wallet.money + 1e-9 < cost) { step.current = wallet.money; step.goal = cost; step.money = true; }
                    list.Add(step);
                }
                else if (dc && plateau)
                {
                    list.Add(Step("final.scale", XgGuideKind.Finale, "loss 不动了：序列线开得更宽更深，打开预热和位置标记", "The loss is flat: make the sequence model wider and deeper, turn on warm-up and positions",
                        "预训练要规模，不是要更久。", "Pre-training needs scale, not more time.", Lab, "train", "name:Width", "sequence"));
                    foreach (var id in new[] { "warmup", "position" })
                    {
                        var n = XgCatalog.Node(id);
                        if (n == null || sim.Has(id)) continue;
                        list.Add(Step("need." + id, XgGuideKind.Finale, "技能树买「" + n.name + "」", "Buy '" + n.nameEn + "' in the skill tree",
                            "预训练要用到这个旋钮。", "Pre-training needs this knob.", Lab, "tree", "node:" + id, ""));
                    }
                    var secret = XgCatalog.Node("secret.6");
                    if (secret != null && !sim.Has(secret.id) && sim.NodeVisible(secret))
                        list.Add(Step("final.secret", XgGuideKind.Finale, "实在卡住：技能树买秘籍「" + secret.name + "」", "Still stuck: buy the secret '" + secret.nameEn + "'",
                            "自己跑通能拿奖金，买了就没有了。", "Working it out yourself pays a bonus; buying it gives that up.", Lab, "tree", "node:secret.6", ""));
                    if (!s.pretrainRunning)
                        list.Add(Step("final.pretrain", XgGuideKind.Finale, "调好后回终章页继续预训练", "Then resume pre-training on the Finale page",
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
            bool wallStands = sim.ActiveWall != null;
            list.Add(Step("node." + best.id, XgGuideKind.Boost, "技能树买「" + (wallStands ? KnobName(best, false) : best.name) + "」", "Buy '" + (wallStands ? KnobName(best, true) : best.nameEn) + "' in the skill tree",
                "不贵，买得起。", "Cheap, and you can afford it.", Lab, "tree", "node:" + best.id, ""));
        }

        static void Raise(XgSim sim, double money, List<XgGuideStep> list)
        {
            if (sim.RaiseLevel >= XgCatalog.RaiseMax || sim.NextRaiseCost > money * CheapRaiseShare) return;
            list.Add(Step("raise", XgGuideKind.Boost, "标注台给自己加薪", "Give yourself a raise on the labelling page",
                "手动标注的报酬一级比一级高。", "Hand labelling pays more with every raise.", Lab, "label", "name:Row薪", ""));
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
