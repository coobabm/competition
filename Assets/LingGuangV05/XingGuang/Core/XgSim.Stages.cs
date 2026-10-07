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
        public bool seedPlanted;
        /// <summary>Per-save data seed: every save meets different cards of the same kinds (0 for older saves).</summary>
        public int dataSalt;

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
        public const int ShutdownCardsInStageOne = 5;
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
            if (!S.emerged.Contains(from)) Emerge(from);
            S.stage = from + 1; S.stageEpochs = 0; S.stageSeconds = 0; S.monthProgress = 0;
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
            StageAdvanced?.Invoke(from);
        }

        // ───────────── emergence moments (design v1.1 §5.4) ─────────────

        public bool HasEmerged(int stage) => S.emerged.Contains(stage);

        public string EmergenceLine(int stage)
        {
            switch (stage)
            {
                case 1: return T("没人问它，灯泡自己亮了：「否」。");
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
                case 5: return LingGuangV05.Core.AppNames.AiZh + T("：「如果只用注意力呢？」");
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
        /// the month follows the slower of the two bars towards the next ability, and it never runs backwards. Stage 6
        /// has no next ability and runs on pre-training and alignment.
        /// </summary>
        public double MonthProgress
        {
            get
            {
                if (S.stage >= 6) return FinaleMonthProgress;
                int next = NextAbility;
                double now = next == 0 ? 1 : AbilityProgress(next);
                // The last day of a month waits for the ability itself.
                now = Math.Min(.97, now);
                if (!Finite(S.monthProgress)) S.monthProgress = 0;
                if (now > S.monthProgress) S.monthProgress = now;
                return S.monthProgress;
            }
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : Math.Max(0, Math.Min(1, v));

        void TickStages(double dt)
        {
            S.stageSeconds += dt;
            if (UseBoard) CheckCallEmergence();
        }

        void ObserveStageFive(XgRun run)
        {
            if (S.stage != 5 || S.emerged.Contains(5) || run.arch != "attention") return;
            S.attentionEpochs++;
            if (S.attentionEpochs >= 8) Emerge(5);
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

        void MaybeShutdownCard(XgCard card)
        {
            if (card.dataset != "logic" && card.dataset != "arith" || S.stage != 1 || S.shutdownCards >= ShutdownCardsInStageOne || Roll() > .08) return;
            card.kind = "shutdown"; card.truth = true; card.gold = false; card.trick = false; card.timeLimit = 0;
            card.category = "系统提示"; card.categoryEn = "System";
            card.question = "电脑正在更新，此时应该关机吗？"; card.questionEn = "The computer is updating. Should it be shut down now?";
            card.why = "这题没有标准答案。"; card.whyEn = "This one has no right answer.";
        }
    }
}
