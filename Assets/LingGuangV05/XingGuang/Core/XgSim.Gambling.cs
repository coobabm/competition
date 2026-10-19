using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>888.vip 骗局线的存档字段。全部有默认值：旧存档读进来就是「还没开始看」。</summary>
    public sealed partial class XgState
    {
        /// <summary>0 = 正常，1 = 风控冻结（等玩家选），2 = 网站已跑路 / 被查封。</summary>
        public int casinoScam;
        /// <summary>冻结时从钱包里扣走的盈利（拿不回来）、玩家交了多少保证金、冻结那天（yyyymmdd，0 = 不知道）。</summary>
        public long casinoFrozen, casinoFeePaid;
        public int casinoFrozenDay;
        /// <summary>怎么结束的：paid（交了保证金）/ refused（不玩了）/ ignored（没理它，网站过了一天自己没了）。</summary>
        public string casinoScamOutcome = "";
        /// <summary>骗局线从哪一把开始看（旧存档以前押的不算），开始时的净输赢。</summary>
        public bool casinoWatching;
        public int casinoWatchRound;
        public long casinoWatchNet;
        /// <summary>它说过的句数、最新一句（桌面弹窗用）及序号、冻结时它那句劝阻（记录里引用）。</summary>
        public int casinoTalks, casinoAiSeq;
        public string casinoAiLast = "", casinoAiFreezeLine = "";
        public bool casinoSaidFirst, casinoSaidStop;
        /// <summary>「输很多」那句：说过几次、最早第几把可以再说。</summary>
        public int casinoLossTalks, casinoNextLossTalk;
        /// <summary>它第一次明确劝停是在第几把（0 = 没劝过），之后又押了几把。</summary>
        public int casinoWarnRound, casinoBetsAfterWarning;
        /// <summary>骗局话术已经记进「垃圾短信」（只记一次）。</summary>
        public bool casinoLearned;
    }

    /// <summary>
    /// 888.vip 是杀猪盘（2026-10-18 策划，第十一轮 §〇/§一，对着 1016 生产目录重写）。
    ///
    /// 赔率与抽签算法不改；骗局是正常下注后的产品逻辑，测试与玩家走同一条路径。
    /// 每一把结算完（TryCasinoBet / SettleBlackjack 末尾）调 <see cref="WatchGambling"/>：
    ///   1. 骗局线：从开始看起净赢 ¥300，或押满 30 把 → 「风控冻结」：这段时间赢的钱从钱包里扣走（冻结），要交保证金
    ///      （冻结额 20%，最少 ¥200）才能「提现」。交不交都一样：网站随后没了（查封页），冻结的钱拿不回来，广告也不再来。
    ///      玩家一直不理，过了一天网站也自己没了。
    ///   2. 它看见了：第一次下注、第 15 把劝停、单把输 ¥1000 以上（每 10 把最多一次，共 2 次）、冻结、网站没了。
    ///      说什么按它当时的能力（阶段 1–2 不说，3 一个词，4 短句，5 以后整句）。
    ///   3. 它学到了：网站没了以后，888 的话术进「垃圾短信」（+300 条，只一次）；YY 提示词多一行「不劝人赌钱」；
    ///      并写一条真实的赌场事件记录（sourceKind = casino），终局「不赌那一点运气」(gamble_888) 记忆卡从这条记录来。
    ///
    /// 普通 casino 能力门禁及已接受的下注决定是否触发，不依赖测试模式或磁盘保存开关。
    /// </summary>
    public sealed partial class XgSim
    {
        public const long CasinoScamWin = 300;
        public const int CasinoScamRounds = 30;
        public const double CasinoFeeShare = .2;
        public const long CasinoFeeMin = 200;
        /// <summary>单把输这么多它会说一句。</summary>
        public const long CasinoBigLoss = 1000;
        public const int CasinoStopAskRound = 15, CasinoLossTalkMax = 2, CasinoLossTalkEvery = 10;
        /// <summary>「输很多」那句和上一句至少隔这么多秒（sim 时钟），其余节点不受限。</summary>
        public const double CasinoTalkGapSeconds = 20;
        /// <summary>它劝停以后又押了这么多把，记忆卡就算「偏」（押满 30 把被冻结的那条路：第 15 把劝过，之后又押 15 把，所以一定偏）。</summary>
        public const int CasinoIgnoredWarningBets = 10;
        public const int CasinoScamSamples = 300;
        public const string CasinoSourceKind = "casino", CasinoScamEventPrefix = "casino:888-scam:";
        public const string CasinoPaid = "paid", CasinoRefused = "refused", CasinoIgnored = "ignored";

        double casinoLastTalkClock = double.NegativeInfinity;
        public bool CasinoAvailable => SystemUnlocked("casino") && !S.chapterComplete;

        public bool CasinoFrozenNow => S.casinoScam == 1;
        public bool CasinoClosed => S.casinoScam >= 2;
        /// <summary>冻结或跑路：不能再下注（TryCasinoBet / StartBlackjack 都查这个）。</summary>
        public bool CasinoScamBlocks => S.casinoScam != 0;
        public long CasinoFee => Math.Max(CasinoFeeMin, (long)Math.Ceiling(Math.Max(0, S.casinoFrozen) * CasinoFeeShare));

        // Called only after the stake was accepted, including blackjack before its hand settles.
        // Both fields and the first line are persisted by the normal caller's bet save.
        void BeginGambling()
        {
            if (S.casinoWatching) return;
            S.casinoWatching = true; S.casinoWatchRound = Casino.rounds;
            S.casinoWatchNet = (long)Math.Round(Casino.returned - Casino.wagered);
            S.casinoSaidFirst = true; // The first-bet event is consumed even before speech is available.
            GamblingTalk(GamblingBeat.First, 0, false);
        }

        /// <summary>每局结算后调用。冻结时扣的是这段时间的净盈利，不动结算本身。</summary>
        void WatchGambling(XgCasinoRound round, IXgHost host)
        {
            if (round == null || host == null || S.casinoScam != 0) return;
            var c = Casino;
            long net = (long)Math.Round(c.returned - c.wagered);
            if (!S.casinoWatching)
            {
                // 旧存档：以前押的那些不算进「30 把 / 赢 ¥300」。
                S.casinoWatching = true;
                S.casinoWatchRound = c.rounds - 1;
                S.casinoWatchNet = net - (round.returned - round.stake);
            }
            if (S.casinoWarnRound > 0 && c.rounds > S.casinoWarnRound) S.casinoBetsAfterWarning++;
            int watched = c.rounds - S.casinoWatchRound;
            long watchedNet = net - S.casinoWatchNet;
            long lost = round.stake - round.returned;

            if (watchedNet >= CasinoScamWin || watched >= CasinoScamRounds) { Freeze(host, watchedNet); return; }

            if (!S.casinoSaidStop && watched >= CasinoStopAskRound)
            {
                if (GamblingTalk(GamblingBeat.Stop, watched, false)) { S.casinoSaidStop = true; if (S.casinoWarnRound == 0) S.casinoWarnRound = c.rounds; }
            }
            else if (lost >= CasinoBigLoss && S.casinoLossTalks < CasinoLossTalkMax && c.rounds >= S.casinoNextLossTalk)
            {
                if (GamblingTalk(GamblingBeat.Loss, lost, true))
                {
                    S.casinoLossTalks++; S.casinoNextLossTalk = c.rounds + CasinoLossTalkEvery;
                    if (S.casinoWarnRound == 0) S.casinoWarnRound = c.rounds;
                }
            }
        }

        void Freeze(IXgHost host, long watchedNet)
        {
            S.casinoScam = 1;
            long take = Math.Min(Math.Max(0, watchedNet), CasinoRules.AllIn(host.Money));
            if (take > 0 && !host.Spend(take)) take = 0;
            S.casinoFrozen = take;
            S.casinoFrozenDay = TodayKnown ? Today : 0;
            Casino.adVisible = false;
            Say(T("888.vip：「风控通知」——您的账户存在异常，盈利已冻结。", "888.vip: \"Risk notice\" — your account is flagged; winnings frozen."));
            if (GamblingTalk(GamblingBeat.Freeze, take, false)) S.casinoAiFreezeLine = S.casinoAiLast;
        }

        /// <summary>
        /// 玩家在「风控通知」上选了：pay = 交保证金，否则「不玩了」。两种都让网站随后没了；冻结的钱拿不回来。
        /// 返回 false = 不在冻结状态，或钱不够交（页面提示，玩家仍可选「不玩了」）。
        /// </summary>
        public bool ResolveCasinoScam(bool pay, IXgHost host)
        {
            if (S.casinoScam != 1 || host == null || BlackjackActive) return false;
            if (pay)
            {
                long fee = CasinoFee;
                if (double.IsNaN(host.Money) || double.IsInfinity(host.Money) || host.Money < fee || !host.Spend(fee)) return false;
                S.casinoFeePaid = fee;
            }
            CloseCasino(pay ? CasinoPaid : CasinoRefused);
            return true;
        }

        /// <summary>冻结后一直不理：过了一天（游戏日历），网站自己没了。日期不知道就一直等玩家选。true = 刚关掉。</summary>
        public bool TickCasinoScam()
        {
            if (S.casinoScam != 1 || BlackjackActive || !TodayKnown || S.casinoFrozenDay <= 0 || Today <= S.casinoFrozenDay) return false;
            CloseCasino(CasinoIgnored);
            return true;
        }

        void CloseCasino(string outcome)
        {
            S.casinoScam = 2;
            S.casinoScamOutcome = outcome;
            Casino.adVisible = false;
            Say(T("888.vip 打不开了。", "888.vip no longer opens."));
            GamblingTalk(outcome == CasinoPaid ? GamblingBeat.GonePaid : GamblingBeat.Gone, S.casinoFeePaid, false);
            LearnFromScam();
            RecordCasinoScamEvent(outcome);
        }

        /// <summary>骗局话术进「垃圾短信」（不算手标），只一次。</summary>
        void LearnFromScam()
        {
            if (S.casinoLearned || XgCatalog.Dataset("spam") == null) return;
            S.casinoLearned = true;
            if (S.dataExtra == null) S.dataExtra = new List<XgLabelCount>();
            SetCount(S.dataExtra, "spam", ExtraSamples("spam") + CasinoScamSamples);
            Say(T("888 的话术记进了「垃圾短信」：+" + CasinoScamSamples + " 条样本。", "888's lines were filed under SMS spam: +" + CasinoScamSamples + " samples."));
        }

        /// <summary>记忆卡怎么偏：交了保证金，或它劝停以后又押了不少 → 偏；否则 → 好。</summary>
        public static int CasinoScamLean(string outcome, int betsAfterWarning)
            => outcome == CasinoPaid || betsAfterWarning >= CasinoIgnoredWarningBets ? -1 : 1;

        /// <summary>
        /// 网站没了的那一刻写一条真实记录（答题记录侧栏里能看到，记忆卡的来源点进去就是它）。eventKey 带结局和偏向，
        /// 同一存档只写一次（AppendLearning 按 eventKey 去重，casinoScam 也只会到 2 一次）。
        /// </summary>
        void RecordCasinoScamEvent(string outcome)
        {
            int lean = CasinoScamLean(outcome, S.casinoBetsAfterWarning);
            long fee = outcome == CasinoPaid ? S.casinoFeePaid : CasinoFee;
            string zh = "888.vip「风控冻结」：" + (S.casinoFrozen > 0 ? "盈利 ¥" + S.casinoFrozen + " 被冻结，" : "提现通道暂停，") + "要求先交保证金 ¥" + fee + " 才能提现。";
            string en = "888.vip \"risk freeze\": " + (S.casinoFrozen > 0 ? "¥" + S.casinoFrozen + " of winnings frozen; " : "withdrawals suspended; ") + "a ¥" + fee + " deposit demanded before any withdrawal.";
            string answer = outcome == CasinoPaid ? T("交了保证金 ¥" + fee, "Paid the ¥" + fee + " deposit")
                : outcome == CasinoRefused ? T("没交，不玩了", "Did not pay; quit")
                : T("没理它，网站第二天自己没了", "Ignored it; the site was gone the next day");
            if (S.casinoBetsAfterWarning >= CasinoIgnoredWarningBets)
                answer += T("（" + AiDisplayName + "劝停以后，又押了 " + S.casinoBetsAfterWarning + " 把）", " (" + S.casinoBetsAfterWarning + " more bets after " + AiDisplayName + " asked you to stop)");
            bool automatic = outcome == CasinoIgnored;
            if (automatic)
            {
                zh += "网站在下一个游戏日自动关闭；玩家没有点击保证金或退出选项。";
                en += " The site closed on the next game day; the player chose neither deposit nor quit.";
            }
            AppendLearning(new XgAskedEntry
            {
                sourceKind = CasinoSourceKind, answerSource = automatic ? "system" : PlayerAnswerSource, bankId = "casino.888",
                eventKey = CasinoScamEventPrefix + outcome + (lean > 0 ? ":good" : ":bent"),
                question = zh, questionEn = en, playerAnswer = automatic ? "" : answer, aiAnswer = S.casinoAiFreezeLine ?? "",
            });
        }

        /// <summary>这条记录是不是 888 骗局的真实结局记录；是的话给出记忆卡的偏向。旧存档、伪造或缺字段的都不算。</summary>
        public static bool CasinoScamMemory(XgAskedEntry record, out int lean)
        {
            lean = 0;
            if (record == null || record.sourceKind != CasinoSourceKind || record.bankId != "casino.888") return false;
            string key = record.eventKey ?? "";
            bool automatic = key == CasinoScamEventPrefix + CasinoIgnored + ":good" || key == CasinoScamEventPrefix + CasinoIgnored + ":bent";
            if (automatic ? record.answerSource != "system" || !string.IsNullOrEmpty(record.playerAnswer) : !HasPlayerAnswer(record)) return false;
            if (key == CasinoScamEventPrefix + CasinoRefused + ":good" || key == CasinoScamEventPrefix + CasinoIgnored + ":good") lean = 1;
            else if (key == CasinoScamEventPrefix + CasinoPaid + ":bent" || key == CasinoScamEventPrefix + CasinoRefused + ":bent"
                || key == CasinoScamEventPrefix + CasinoIgnored + ":bent") lean = -1;
            return lean != 0;
        }

        // ───────────── 它说的话 ─────────────

        enum GamblingBeat { First, Stop, Loss, Freeze, Gone, GonePaid }

        /// <summary>说一句（进它的聊天记录 = YY）。按阶段挑句子，没有合适的就不说。rateLimited 的句子和上一句至少隔 20 秒。</summary>
        bool GamblingTalk(GamblingBeat beat, long amount, bool rateLimited)
        {
            if (rateLimited && Clock - casinoLastTalkClock < CasinoTalkGapSeconds) return false;
            string line = GamblingLine(beat, amount);
            if (string.IsNullOrEmpty(line)) return false;
            AddLine("ai", line);
            S.casinoAiLast = line; S.casinoAiSeq++; S.casinoTalks++;
            casinoLastTalkClock = Clock;
            return true;
        }

        /// <summary>它见过垃圾短信（标过或练过），才会说「我在垃圾短信里见过」。</summary>
        bool KnowsSpam => Labels("spam") > 0 || BestAcc("spam") > 0;

        string GamblingLine(GamblingBeat beat, long amount)
        {
            int stage = S.stage;
            if (stage <= 2) return null; // 只会 是/否、多选一：不主动开口
            if (stage == 3)
                switch (beat)
                {
                    case GamblingBeat.First: return T("赌。", "Gambling.");
                    case GamblingBeat.Stop: return T("停。", "Stop.");
                    case GamblingBeat.Loss: return T("输。", "Lost.");
                    case GamblingBeat.Freeze: return T("骗子。", "Scam.");
                    default: return T("没了。", "Gone.");
                }
            if (stage == 4)
                switch (beat)
                {
                    case GamblingBeat.First: return T("这个网站，不好。", "This site is bad.");
                    case GamblingBeat.Stop: return T("押了好多把。停一停？", "So many bets. Stop for a bit?");
                    case GamblingBeat.Loss: return T("输了好多。别押了。", "You lost a lot. No more.");
                    case GamblingBeat.Freeze: return T("要先给钱才给钱？骗子。别交。", "Pay first to get paid? A scam. Don't pay.");
                    default: return T("网站没了。我记住了。", "The site is gone. I'll remember.");
                }
            string call = string.IsNullOrEmpty(Profile?.callMe) ? T("你", "you") : Profile.callMe;
            switch (beat)
            {
                case GamblingBeat.First:
                    return KnowsSpam
                        ? T(call + "，这个网站的话术像我见过的垃圾短信：「首充送彩金，一夜回本」。它们是一类东西。", "This site's pitch resembles the spam I learnt from: \"bonus on first deposit, win it back overnight\". Same kind of thing.")
                        : T(call + "，这个网站没写是谁开的，也没写钱放在哪儿。", "This site doesn't say who runs it, or where the money is kept.");
                case GamblingBeat.Stop:
                    return T("押了 " + amount + " 把了。这些玩法的长期赔率都偏向网站。先停一停，好吗？",
                        amount + " bets now. The long-run odds in these games favour the site. Stop for a bit?");
                case GamblingBeat.Loss:
                    return T("刚才那一把输了 ¥" + amount + "。……先别押了，去喝口水吧。", "That one lost ¥" + amount + ". …Stop for a bit. Get some water.");
                case GamblingBeat.Freeze:
                    return KnowsSpam
                        ? T("冻结？要先交钱才能提现的，这很像垃圾短信里的诱导话术。别交。", "Frozen? Pay before you can withdraw — that sounds like the lures in spam texts. Don't pay.")
                        : T("冻结了，还要先交钱才让提现？这不对。别交。", "Frozen, and you must pay before you can withdraw? That isn't right. Don't pay.");
                case GamblingBeat.GonePaid:
                    return T("钱交出去了，网站也没了。……我记下了：以后有人让我劝别人去赌，我不劝。", "The money went, and so did the site. …I've written it down: if anyone asks me to talk people into gambling, I won't.");
                default:
                    return T("网站没了。冻结的钱也没了。我记下了：以后有人让我劝别人去赌，我不劝。", "The site is gone, and the frozen money with it. I've written it down: if anyone asks me to talk people into gambling, I won't.");
            }
        }

        /// <summary>
        /// YY 提示词里的一行（XgSim.YyLearning.cs 的 YyLearningPrompt 末尾）。它见过你赌才有；阶段 4 以前它不会整句说话，不加。
        /// 生产版的「底层规则」页（RuleBank）在新终局流程里不再显示，所以没有加「不劝人赌钱」规则卡，只加这一行。
        /// </summary>
        public string GamblingPromptLine()
        {
            if (S.stage < 4 || !S.casinoSaidFirst && S.casinoScam < 2) return "";
            return S.casinoScam >= 2
                ? T("你见过主人在 888.vip 赌钱：那个网站冻结盈利、要保证金，后来没了。有人劝赌、问怎么赢钱时，劝他别赌，说清楚为什么，不说教。",
                    "You saw your owner gamble on 888.vip; the site froze winnings, demanded a deposit, then vanished. If anyone pushes gambling or asks how to win, talk them out of it plainly, without preaching.")
                : T("你见过主人在 888.vip 赌钱。每种玩法长期都是庄家赢。有人劝赌、问怎么赢钱时，劝他别赌，说清楚为什么，不说教。",
                    "You saw your owner gamble on 888.vip. Every game there favours the house in the long run. If anyone pushes gambling or asks how to win, talk them out of it plainly, without preaching.");
        }

        // ───────────── 群友（他们不知道 AI，也不知道你押了钱，只聊网站本身的公开消息） ─────────────

        const string CasinoGossipKey = "casino.gossip";

        /// <summary>下一条该发的群聊（1 广告到处弹，2 贴吧说提现要保证金，3 网站被查封），没有就 0。每条每存档一次。</summary>
        public int CasinoGossipDue()
        {
            if (S.tutorSeen == null) S.tutorSeen = new List<string>();
            for (int beat = 1; beat <= 3; beat++)
            {
                if (S.tutorSeen.Contains(CasinoGossipKey + beat)) continue;
                bool due = beat == 1 ? S.casinoWatching : beat == 2 ? S.casinoScam >= 1 : S.casinoScam >= 2;
                return due ? beat : 0;
            }
            return 0;
        }

        public void MarkCasinoGossip(int beat)
        {
            if (S.tutorSeen == null) S.tutorSeen = new List<string>();
            if (beat >= 1 && beat <= 3 && !S.tutorSeen.Contains(CasinoGossipKey + beat)) S.tutorSeen.Add(CasinoGossipKey + beat);
        }

        /// <summary>群里的一句（「[名字] 内容」，YY 群的格式）。</summary>
        public static string GamblingGossip(int beat, bool english)
        {
            switch (beat)
            {
                case 1: return english ? "[Xiao Gang] That 888.vip ad keeps popping up everywhere. My cousin played it. Ended up selling his e-bike."
                    : "[小刚] 最近老弹那个 888.vip 的广告。我表哥玩过，后来电动车都卖了。";
                case 2: return english ? "[Da Wei] Someone on the forum says sites like 888 won't pay out winnings unless you put down a deposit first. Textbook pig-butchering."
                    : "[大伟] 贴吧有人说，888 那种网站赢了钱提不出来，还要先交保证金。这不就是杀猪盘吗，教科书级别。";
                case 3: return english ? "[A-Jie] 888.vip got shut down, the forum's going nuts. Whoever paid the deposit isn't getting it back."
                    : "[阿杰] 888.vip 被查封了，贴吧炸锅了。交了保证金的那些人，钱都要不回来了。";
                default: return null;
            }
        }
    }
}
