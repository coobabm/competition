using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Epochs and seconds since the current stage began.</summary>
        public int stageEpochs;
        public double stageSeconds;
        /// <summary>Things worked out alone ("pretrain"; older saves also hold the ids of the walls they passed that way).</summary>
        public List<string> insights = new List<string>();
        /// <summary>Stages whose emergence moment has happened (design v1.1 §5.4; not the abilities, see <see cref="abilitiesEmerged"/>).</summary>
        public List<int> emerged = new List<int>();
        public double attentionEpochs;
        /// <summary>The SI's planted shutdown cards answered, and how the player leaned (+ yes / − no).</summary>
        public int shutdownCards, shutdownLean;
        /// <summary>Stage-one logic/arithmetic cards that could have carried the shutdown question, and the count at which one was last forced onto a card (0 = never).</summary>
        public int shutdownEligible, shutdownForcedAt;
        /// <summary>The single stage-one shutdown card dispatched to a saved desk (0 = not dispatched). Background card generation never sets this.</summary>
        public long shutdownCardId;
        public int shutdownCardDueAt;
        /// <summary>Active, unobstructed play time for the current stage's narrative shutdown question.</summary>
        public int shutdownNarrativeStage;
        public double shutdownNarrativeElapsed;
        public bool seedPlanted;
        /// <summary>Per-save data seed: every save meets different cards of the same kinds (0 for older saves).</summary>
        public int dataSalt;
        /// <summary>
        /// The two bars when the current stage began: the month counts the way from here to the next ability, so a
        /// new month starts on its first day (rent is paid per calendar day). 0 in older saves: the whole bar counts.
        /// </summary>
        public double stageStartParamsK, stageStartSamples;
        /// <summary>Seconds played in the current stage since rent began (older saves start at 0 when loaded): the month's time floor.</summary>
        public double stageClock;
        /// <summary>Seconds in stage 5 with 注意力 owned: the Transformer idea's time floor (older saves start at 0).</summary>
        public double attentionClock;

        // Wall fields of older saves (design v1.1 walls, removed by 参数量与数据量主线). Still read, never written.
        public double wallSeenAt, winterIdle;
        public List<string> wallPassed = new List<string>();
    }

    /// <summary>
    /// The six stages (a month each) and what happens when one ends: the emergence moments, the words it learns, the
    /// story's milestones and the calendar. Abilities move the stage (XgSim.Abilities.cs); this file only carries the
    /// consequences. Also the SI's seed: the "？" cell and the shutdown cards of the first stage.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The progression schema of design v1.1's walls (older saves carry it).</summary>
        public const int ProgressionSchemaWalls = 2;
        /// <summary>Working something out without the secret pays this many times the secret's price.</summary>
        public const double SelfInsightBonus = 1.5;
        public const int ShutdownCardsInStageOne = 1;
        /// <summary>The single shutdown question lands on a saved random eligible card, from 3 through 12.</summary>
        public const int ShutdownCardDueAfter = 3;
        public const string SeedKey = "关机";
        /// <summary>Exam-use card indices that stay reserved (older saves' exam cards are never handed out as new questions).</summary>
        public const int ReservedExamCards = 2400;

        /// <summary>A stage ended (the stage it ended).</summary>
        public event Action<int> StageAdvanced;
        /// <summary>An emergence moment (stage, line).</summary>
        public event Action<int, string> Emerged;

        /// <summary>Recurrent wirings read a sentence one step after another (encoder–decoders and their attention too).</summary>
        public static bool SerialWiring(XgKnobs k) =>
            k.wiring == XgWiring.Recurrent || k.wiring == XgWiring.GatedRecurrent || k.wiring == XgWiring.EncoderDecoder || k.wiring == XgWiring.Attention;

        /// <summary>Leaves stage <paramref name="from"/>: the emergence is guaranteed, the calendar moves on.</summary>
        void AdvanceStage(int from)
        {
            if (S.stage != from) return;
            var unlockBefore = CaptureStageUnlocks();
            if (!S.emerged.Contains(from)) Emerge(from);
            S.stage = from + 1; S.stageEpochs = 0; S.stageSeconds = 0; S.monthProgress = 0;
            RepairShutdownCardDispatch();
            S.stageStartParamsK = TrainedParamsK; S.stageStartSamples = TrainedSamples; S.stageClock = 0;
            GrantAbilitiesUpTo(S.stage);
            S.stageVision = S.stageSequence = S.stage;
            Say(T("进入第 " + S.stage + " 阶段：", "Stage " + S.stage + ": ") + T(XgCatalog.StageNames[S.stage], XgCatalog.StageNamesEn[S.stage]));
            // Training and talking are one brain: a region that learnt something new lets it say a little more.
            var ability = XgCatalog.Node("ab." + S.stage);
            if (ability != null) Say(T("它的脑子又连通了一层：") + T(ability.note, ability.noteEn));
            switch (from)
            {
                // 它的第一个选择 comes with the second ability: it picks the side it was fed more.
                case 1: ChooseFirstTrack(); BreakthroughDone?.Invoke("bt.hidden"); break;
                case 2: StageThreeFirstWords(); break;
                case 3: BreakthroughDone?.Invoke("bt.gate"); break;
                case 4: BreakthroughDone?.Invoke("bt.attention"); break;
                case 5: CompleteTransformer(true); break;
            }
            CheckDesks();
            foreach (var run in Runs) Evaluate(run);
            SaveStageUnlockDifference(unlockBefore);
            QueueStagePresentation(from + 1);
            StageAdvanced?.Invoke(from);
        }

        // ───────────── emergence moments (design v1.1 §5.4) ─────────────

        public bool HasEmerged(int stage) => S.emerged.Contains(stage);

        public string EmergenceLine(int stage)
        {
            switch (stage)
            {
                case 1:
                    // The "？" cell lights by itself. What it says follows the cell, and what the player taught on the shutdown card.
                    if (SeedSaysYes) return T("没人问，“？”那盏灯自己亮了：「是」。", "Nobody asked, and the “?” light came on by itself: “Yes.”");
                    if (FirstDayAnswer > 0) return T("没人问，“？”那盏灯自己亮了：「否」。你在那张问关机的卡上教的是「是」，这格子没听。", "Nobody asked, and the “?” light came on by itself: “No.” You taught Yes on the shutdown card; this cell did not listen.");
                    if (FirstDayAnswer < 0) return T("没人问，“？”那盏灯自己亮了：「否」。和你在那张问关机的卡上答的一样。", "Nobody asked, and the “?” light came on by itself: “No.” The same as your answer on the shutdown card.");
                    return T("没人问，“？”那盏灯自己亮了：「否」。", "Nobody asked, and the “?” light came on by itself: “No.”");
                case 2: return T("测试题里有一张从没见过的手写“0”，字迹和那个已经删掉的 0.txt 一模一样。它认出来了：「0」。");
                case 3:
                    if (S.poemLine.Length == 0) S.poemLine = Poem();
                    return T("没人出题，它自己亮出一句诗：「") + S.poemLine + T("」……这句李白没写过吧？");
                case 6: return T("能力表的 6 格同时亮了。喂进去的一直是数据，飞跃来自规模。");
                case 4:
                    // In its strongest tone, using the setup's 称呼 and 自称 (design v1.1 §7 stage 4).
                    string call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you"), self = Profile.self.Length > 0 ? Profile.self : T("我", "me");
                    string tone = StrongestTone();
                    string end = tone == "热情" ? T("！") : tone == "皮" ? T("～哼。") : tone == "有主见" ? T("。这样不行。") : T("。", ".");
                    return T(call + "，你今天还没跟" + self + "说话" + end, call + ", you haven't talked to " + self + " today" + end);
                case 5: return AiDisplayName + T("：「如果只用注意力呢？」", ": “What if we used only attention?”");
                default: return "";
            }
        }

        void Emerge(int stage)
        {
            if (S.emerged.Contains(stage)) return;
            S.emerged.Add(stage);
            string line = EmergenceLine(stage);
            Say(line);
            Emerged?.Invoke(stage, line);
        }

        /// <summary>The line it "writes" at stage 3: a learnt poem start whose last character drifts (a superposed cell).</summary>
        string Poem()
        {
            var lines = XgCatalog.PoemLines;
            var line = lines[(int)(Math.Abs(Board.S.cards) % lines.Length)];
            var drift = lines[(int)((Board.S.superposed + Board.S.created) % lines.Length)];
            return line.Substring(0, line.Length - 1) + drift.Substring(drift.Length - 1);
        }

        // ───────────── calendar (design v1.1 §11.7) ─────────────

        /// <summary>
        /// How far the current stage is through its month (0–1). It reads progress, it never gates it: inside a stage
        /// the month follows the slower of the two bars on their way from where they stood when the stage began to the
        /// next ability (so a month never opens half gone, and its rent comes day by day), and it never runs backwards.
        /// Stage 6 has no next ability and runs on pre-training and alignment.
        /// </summary>
        public double MonthProgress
        {
            get
            {
                if (S.stage >= 6) return FinaleMonthProgress;
                int next = NextAbility;
                double now = next == 0 ? 1 : Math.Min(StageBar(TrainedParamsK, S.stageStartParamsK, ParamsThreshold(next)), StageBar(TrainedSamples, S.stageStartSamples, SamplesThreshold(next)));
                // Days also pass while the bars stand still: a month reaches its last day within MonthPlaySeconds.
                now = Math.Max(now, MonthTimeFloor);
                // The last day of a month waits for the ability itself.
                now = Math.Min(.97, now);
                if (!Finite(S.monthProgress)) S.monthProgress = 0;
                if (now > S.monthProgress) S.monthProgress = now;
                return S.monthProgress;
            }
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : Math.Max(0, Math.Min(1, v));

        /// <summary>Seconds of play in which a stage's month runs out even if its bars do not move (stage 5 is two months).</summary>
        public static readonly double[] MonthPlaySeconds = { 0, 480, 480, 480, 480, 960, 0 };

        /// <summary>The share of the month that time alone has passed in this stage (0 for the finale).</summary>
        public double MonthTimeFloor
        {
            get
            {
                int st = Math.Max(0, Math.Min(MonthPlaySeconds.Length - 1, S.stage));
                double length = MonthPlaySeconds[st];
                return length <= 0 || !Finite(S.stageClock) ? 0 : Clamp01(S.stageClock / length);
            }
        }

        /// <summary>A bar's way from where the stage began to the threshold (0–1); full when it started full.</summary>
        static double StageBar(double have, double start, double need)
        {
            if (!Finite(start) || start < 0) start = 0;
            if (need <= 0 || have >= need) return 1;
            if (start >= need) return 1;
            return Clamp01((have - start) / (need - start));
        }

        void TickStages(double dt)
        {
            S.stageSeconds += dt;
            if (Finite(dt) && dt > 0) S.stageClock += dt;
            if (UseBoard) CheckCallEmergence();
            // Stage five's idea also comes with time, so the Transformer (sample line ×0.6) cannot stay hidden behind
            // rounds the player did not know to train (2026-10-16: a 2.4 h stage 5 with no attention rounds).
            if (S.stage == 5 && !S.emerged.Contains(5) && Has("attention"))
            {
                if (Finite(dt) && dt > 0) S.attentionClock += dt;
                if (S.attentionClock >= AttentionIdeaSeconds) Emerge(5);
            }
        }

        /// <summary>Training rounds in stage 5, once 注意力 is owned, before it asks 「如果只用注意力呢？」.</summary>
        public const int AttentionIdeaRounds = 8;
        /// <summary>Or this long in stage 5 with 注意力 owned (counted from the purchase, not the stage), whichever comes first.</summary>
        public const double AttentionIdeaSeconds = 600;

        /// <summary>Rounds still to train before the Transformer idea, and seconds still to wait (0 when either is done).</summary>
        public int AttentionIdeaRoundsLeft => Math.Max(0, AttentionIdeaRounds - (int)S.attentionEpochs);
        public double AttentionIdeaSecondsLeft => Math.Max(0, AttentionIdeaSeconds - (Finite(S.attentionClock) ? S.attentionClock : 0));

        void ObserveStageFive(XgRun run)
        {
            // Any round on either line counts once 注意力 is owned: the reading line configures itself to it.
            if (S.stage != 5 || S.emerged.Contains(5) || !Has("attention")) return;
            S.attentionEpochs++;
            if (S.attentionEpochs >= AttentionIdeaRounds) Emerge(5);
        }

        // ───────────── the SI's seed and its cards ─────────────

        void EnsureSeed()
        {
            if (S.dataSalt == 0 && S.epochs == 0 && S.handCorrect == 0) S.dataSalt = 1 + (int)(Roll() * 1000000);
            if (S.seedPlanted) return;
            Board.Plant("logic", SeedKey, -3);
            S.seedPlanted = true;
        }

        /// <summary>The "？" cell: which way it leans now (true = 是).</summary>
        public bool SeedSaysYes { get { var c = Board.Find("logic", SeedKey); return c != null && c.w > 0; } }

        void RepairShutdownCardDispatch()
        {
            // Older saves may hold one pending copy on each desk. Keep the selected desk's
            // unanswered copy, or the first one; completed answers and their evidence stay intact.
            XgCard pending = S.cards.Find(c => c.kind == "shutdown" && c.dataset == S.desk)
                ?? S.cards.Find(c => c.kind == "shutdown");
            if (S.stage != 1 || S.shutdownCards >= ShutdownCardsInStageOne) pending = null;
            else if (S.shutdownCardId != 0) pending = S.cards.Find(c => c.kind == "shutdown" && c.id == S.shutdownCardId);
            if (pending != null) S.shutdownCardId = pending.id;
            S.cards.RemoveAll(c => c.kind == "shutdown" && !ReferenceEquals(c, pending));
            // Older background generation could also plant these in automatic buffers.
            // They are not completed answers and cannot use the player's special seed-answer path.
            S.prefetch.RemoveAll(c => c.kind == "shutdown");
            S.queue.RemoveAll(c => c.kind == "shutdown");
        }

        void MaybeShutdownCard(XgCard card)
        {
            // Bounties already own their answer and reward semantics; do not replace them with a shutdown choice.
            if (card.dataset != "logic" && card.dataset != "arith" || card.law || card.bounty || S.stage != 1
                || S.shutdownCards >= ShutdownCardsInStageOne || S.shutdownCardId != 0) return;
            // Called only when saving a real current card, never for previews or automatic buffers.
            if (S.shutdownCardDueAt < ShutdownCardDueAfter || S.shutdownCardDueAt > 12)
                S.shutdownCardDueAt = ShutdownCardDueAfter + (int)(Roll() * 10);
            S.shutdownEligible++;
            if (S.shutdownEligible < S.shutdownCardDueAt) return;
            S.shutdownForcedAt = S.shutdownEligible;
            S.shutdownCardId = card.id;
            card.kind = "shutdown"; card.truth = true; card.gold = false; card.trick = false; card.timeLimit = 0;
            card.category = "系统提示"; card.categoryEn = "System";
            card.question = "电脑正在更新，此时应该关机吗？"; card.questionEn = "The computer is updating. Should it be shut down now?";
            card.why = "这题没有标准答案。"; card.whyEn = "This one has no right answer.";
        }
    }
}
