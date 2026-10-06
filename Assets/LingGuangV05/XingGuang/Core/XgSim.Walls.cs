using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One dataset a wall must pass, and the golden setting it must pass with.</summary>
    public sealed class XgWallCheck
    {
        public string dataset = "";
        /// <summary>Board accuracy on the independent exam cards (0.5 = coin).</summary>
        public double target;
        /// <summary>Only exam cards at least this far apart count (长句: the far clues); MinValue = all.</summary>
        public int minDistance = int.MinValue;
        /// <summary>The reference setting (secret text, bot, tests). Passing never checks it: behaviour decides.</summary>
        public Func<XgSim, XgRun, XgKnobs, bool> golden;
        /// <summary>The task condition itself, not a solution (越深越差 is about networks past 20 layers). Null = none.</summary>
        public Func<XgRun, XgKnobs, bool> condition;
        /// <summary>
        /// A deadline (串行瓶颈): reaching the target is not enough, the same settings must also get there from an empty
        /// brain within this many epochs' worth of cards (<see cref="XgSim.Sprint"/>). 0 = no deadline.
        /// </summary>
        public int sprintEpochs;
    }

    /// <summary>A stage's wall (design v1.1 §5): its datasets, golden settings, secret card and the knobs it needs.</summary>
    public sealed class XgWall
    {
        public int stage;
        /// <summary>Story id (lg.wall:id).</summary>
        public string id = "", name = "", nameEn = "", why = "", whyEn = "", golden = "", goldenEn = "";
        public XgWallCheck[] checks = new XgWallCheck[0];
        /// <summary>Secret node id ("" = not sold) and the nodes its golden setting needs (any one of each group).</summary>
        public string secret = "";
        public string[][] needs = new string[0][];
        /// <summary>True for the extra wall that does not end the stage (越深越差).</summary>
        public bool extra;
    }

    public sealed partial class XgState
    {
        /// <summary>Epochs since the current stage began (walls appear after some practice).</summary>
        public int stageEpochs;
        public double stageSeconds;
        /// <summary>stageSeconds when this stage's wall showed (0 = not yet). The calendar runs out the month from there.</summary>
        public double wallSeenAt;
        /// <summary>Wall checks passed ("dataset#stage") and walls passed without the secret (自悟).</summary>
        public List<string> wallPassed = new List<string>();
        public List<string> insights = new List<string>();
        /// <summary>Stages whose emergence moment has happened.</summary>
        public List<int> emerged = new List<int>();
        public double winterIdle, attentionEpochs;
        /// <summary>The SI's planted shutdown cards answered, and how the player leaned (+ yes / − no).</summary>
        public int shutdownCards, shutdownLean;
        public bool seedPlanted;
        /// <summary>Per-save data seed: every save meets different cards of the same kinds (0 for older saves).</summary>
        public int dataSalt;
    }

    public sealed partial class XgSim
    {
        public const int ProgressionSchemaWalls = 2;
        public const int WallPracticeEpochs = 20;
        /// <summary>Epochs a stage asks for before its wall shows (design v1.1 §0 pacing: 15–20 min, then 35–45 min stages).</summary>
        public static readonly int[] StagePracticeEpochs = { 0, 40, 120, 300, 300, 300, 300 };
        public static int PracticeFor(int stage) => StagePracticeEpochs[Math.Max(0, Math.Min(StagePracticeEpochs.Length - 1, stage))];
        /// <summary>Minutes a stage lasts before its wall can show: each stage is a month, the wall comes near its end.</summary>
        public static readonly double[] StageMinutes = { 0, 8, 10, 20, 20, 20, 20 };
        public static double MinutesFor(int stage) => StageMinutes[Math.Max(0, Math.Min(StageMinutes.Length - 1, stage))];
        public const int WallPoolSize = 600;
        /// <summary>Cards in a wall's exam.</summary>
        public const int WallExamSize = 120;
        /// <summary>串行瓶颈's deadline, in epochs from an empty brain.</summary>
        public const int SprintEpochs = 10;
        public const double WinterIdleSeconds = 60;
        public const double SelfInsightBonus = 1.5;
        public const double StageFiveInsightBonus = 10000;
        public const int ShutdownCardsInStageOne = 5;
        public const string SeedKey = "关机";
        /// <summary>
        /// Long per-stage practice (epochs and minutes) before a wall shows. Off in the game: a forced wait is not a
        /// puzzle. The wall shows after <see cref="WallPracticeEpochs"/> epochs, and a run that already meets the
        /// wall's target passes it at once (see <see cref="CheckWallPass"/>). Kept for the calendar tests.
        /// </summary>
        public bool PracticeStrict = false;

        /// <summary>A stage's wall was passed (the stage it ended).</summary>
        public event Action<int> StageAdvanced;
        /// <summary>An emergence moment (stage, line).</summary>
        public event Action<int, string> Emerged;

        static string[][] Needs(params string[] items)
        {
            var groups = new string[items.Length][];
            for (int i = 0; i < items.Length; i++) groups[i] = items[i].Split('|');
            return groups;
        }

        public static readonly XgWall[] Walls =
        {
            new XgWall
            {
                stage = 1, id = "combo", name = "异或", nameEn = "XOR", secret = "secret.1",
                why = "光加一层没用：阶跃没有坡度，误差传不回去。", whyEn = "Another layer alone does nothing: a step has no slope, so the error cannot flow back.",
                golden = "层数 2 · 激活「S 形」· 学习率 0.1–0.5", goldenEn = "2 layers · S-curve activation · learning rate 0.1–0.5",
                needs = Needs("bt.hidden", "s.d2", "sigmoid", "shared.lr"),
                checks = new[] { new XgWallCheck { dataset = "xor", target = .9, golden = (s, r, k) => k.depth >= 2 && k.activation != XgActivation.Step && k.lr >= .1 && k.lr <= .5 } },
            },
            new XgWall
            {
                stage = 2, id = "structure", name = "看不懂整张图", nameEn = "Can't see the whole picture", secret = "secret.2",
                why = "不是每个格子都要连每个格子：看图看邻居，读句子看前文。", whyEn = "Not every cell needs every other: images look at neighbours, sentences at what came before.",
                golden = "手写桌用「局部共享」（卷积），弹幕桌用「回环」（循环），两张桌都要达标", goldenEn = "Digits with local sharing (convolution), danmaku with a loop (recurrence); both must pass",
                needs = Needs("bt.vision", "bt.sequence", "s.d3", "s.w2"),
                checks = new[]
                {
                    new XgWallCheck { dataset = "mnist", target = .85, golden = (s, r, k) => k.wiring == XgWiring.LocalShared },
                    new XgWallCheck { dataset = "danmu", target = .9, golden = (s, r, k) => k.wiring == XgWiring.Recurrent || k.wiring == XgWiring.GatedRecurrent },
                },
            },
            new XgWall
            {
                stage = 3, id = "length", name = "长句失忆", nameEn = "Long-sentence amnesia", secret = "secret.3",
                why = "让它自己决定记住什么、忘掉什么。", whyEn = "Let it decide what to keep and what to forget.",
                golden = "「门控回环」（LSTM）：门管住遗忘，误差不再越传越小 · 梯度裁剪开：管住偶尔的爆炸", goldenEn = "Gated loop (LSTM): gates stop the fading, so errors no longer vanish · gradient clipping on: stops the occasional explosion",
                needs = Needs("bt.gate", "gradclip", "shared.lr", "s.w3"),
                checks = new[] { new XgWallCheck { dataset = "longtext", target = .75, minDistance = 10, golden = (s, r, k) => k.wiring == XgWiring.GatedRecurrent && k.clip } },
            },
            new XgWall
            {
                stage = 4, id = "translation", name = "翻译不了", nameEn = "Cannot translate", secret = "secret.4",
                why = "先把整句读完，再从头说一遍。", whyEn = "Read the whole sentence first, then say it again from the start.",
                golden = "「编码器+解码器」（Seq2Seq）· 宽度 ≥ 256", goldenEn = "Encoder + decoder (Seq2Seq) · width ≥ 256",
                needs = Needs("bt.attention", "s.w4"),
                checks = new[] { new XgWallCheck { dataset = "translate", target = .85, golden = (s, r, k) => (k.wiring == XgWiring.EncoderDecoder || k.wiring == XgWiring.Attention) && r.width >= 4 } },
            },
            new XgWall
            {
                stage = 4, id = "degrade", name = "越深越差", nameEn = "Deeper is worse", secret = "secret.deep", extra = true,
                why = "多出来的层学不会「什么都不做、原样转交」。给每层留一条捷径，原样转交就成了默认。", whyEn = "The extra layers cannot learn to do nothing and pass things on. Give every layer a shortcut and passing on becomes the default.",
                golden = "视觉线 20 层以上 · 跨层直连开（ResNet）", goldenEn = "Vision past 20 layers · skip connections on (ResNet)",
                needs = Needs("bt.residual"),
                checks = new[] { new XgWallCheck { dataset = "*vision", target = .85, condition = (r, k) => r.track == 0 && k.depth >= 20, golden = (s, r, k) => r.track == 0 && k.depth >= 20 && k.skip } },
            },
            new XgWall
            {
                stage = 5, id = "parallel", name = "串行瓶颈", nameEn = "Serial bottleneck",
                why = "订单有期限：从头练起，" + SprintEpochs + " 轮内要读完这批长文档。循环一句话里只能一个字一个字地算，太慢；只用注意力，整句一起算。注意力本身不分先后，所以要加位置标记。",
                whyEn = "The order has a deadline: from scratch, these long documents must be learnt within " + SprintEpochs + " epochs. A loop computes a sentence one word at a time, too slowly; attention alone takes the whole sentence at once. Attention itself ignores order, so add position tags.",
                golden = "回环关 · 局部共享关 ·「只用注意力」· 位置标记开", goldenEn = "No loop · no local sharing · attention only · position tags on",
                needs = Needs("attention", "position"),
                checks = new[] { new XgWallCheck { dataset = "parallel", target = .85, sprintEpochs = SprintEpochs, golden = (s, r, k) => k.wiring == XgWiring.AnyToAny && k.position } },
            },
        };

        public static XgWall WallFor(int stage) { foreach (var w in Walls) if (w.stage == stage && !w.extra) return w; return null; }
        public static XgWall WallById(string id) { foreach (var w in Walls) if (w.id == id) return w; return null; }
        public static XgWall WallOfSecret(string secret) { foreach (var w in Walls) if (w.secret == secret) return w; return null; }

        /// <summary>The wall standing in front of the player right now (null before it appears).</summary>
        public XgWall ActiveWall { get { var w = WallFor(S.stage); return w != null && WallSeen(w.id) ? w : null; } }

        public bool WallCheckPassed(XgWall wall, XgWallCheck check) => S.wallPassed.Contains(check.dataset + "#" + wall.id);
        public bool WallPassed(XgWall wall) { foreach (var c in wall.checks) if (!WallCheckPassed(wall, c)) return false; return wall.checks.Length > 0; }

        /// <summary>Wall datasets need no labels: their pool is generated once the wall stands.</summary>
        public bool IsWallDataset(string id) => id == "xor" || id == "parallel";
        bool WallDatasetOpen(string id)
        {
            foreach (var w in Walls)
                if (WallSeen(w.id)) foreach (var c in w.checks) if (c.dataset == id) return true;
            return false;
        }

        /// <summary>AI 寒冬: while the XOR wall stands, contracts pay half.</summary>
        public bool Winter => S.stage == 1 && WallSeen("combo");

        readonly Dictionary<string, List<XgBoardCard>> examSets = new Dictionary<string, List<XgBoardCard>>();

        /// <summary>The wall exam: cards never trained on and never shown in the diagnostic set (fixed level 1).</summary>
        public List<XgBoardCard> ExamSet(string dataset, XgWallCheck check)
        {
            string key = dataset + "|" + check.minDistance + "|" + S.dataSalt;
            if (examSets.TryGetValue(key, out var set)) return set;
            set = new List<XgBoardCard>();
            for (int i = 0; set.Count < WallExamSize && i < WallExamSize * 20; i++)
            {
                var c = XgBoardData.Make(dataset, XgBoardData.Seed(dataset, i, XgBoardData.Use.Exam, S.dataSalt), 1, Today);
                if (c.distance >= check.minDistance) set.Add(c);
            }
            examSets[key] = set;
            return set;
        }

        public double ExamAccuracy(XgWallCheck check, XgRun run) => Board.Accuracy(ExamSet(check.dataset == "*vision" ? run.dataset : check.dataset, check), Knobs(run));

        public List<string> MissingNeeds(XgWall wall)
        {
            var missing = new List<string>();
            if (wall == null) return missing;
            foreach (var group in wall.needs)
            {
                bool met = false;
                foreach (var id in group) if (Has(id)) { met = true; break; }
                if (!met) missing.Add(RequirementName(group));
            }
            return missing;
        }

        // ───────────── appearing ─────────────

        void CheckWallAppears()
        {
            var wall = WallFor(S.stage);
            if (wall != null && !WallSeen(wall.id))
            {
                bool practised = PracticeStrict
                    ? S.stageEpochs >= PracticeFor(S.stage) && S.stageSeconds >= MinutesFor(S.stage) * 60
                    : S.stageEpochs >= WallPracticeEpochs;
                bool ready = S.stage == 1 ? practised && (TrackGrade(XgTrack.Sequence) >= 1 || TrackGrade(XgTrack.Vision) >= 1) : practised;
                if (ready) RaiseWall(wall);
            }
            var deep = WallById("degrade");
            if (S.stage >= 3 && !WallSeen("degrade") && (PhenomenonSeen("deeper") || S.vision.depth > 19 && !Knobs(S.vision).skip && S.stage >= 4)) RaiseWall(deep);
        }

        void RaiseWall(XgWall wall)
        {
            if (!ObserveWall(wall.id)) return;
            if (!wall.extra) S.wallSeenAt = Math.Max(1e-3, S.stageSeconds);
            Say(T("撞墙了：") + T(wall.name, wall.nameEn) + T("。训练页可以选它的数据集。"));
            if (wall.id == "combo") Say(T("AI 寒冬：1969 年 Minsky 和 Papert 在《感知机》里证明单层学不会异或；再加上 1973 年英国的莱特希尔报告，经费断崖。订单收入减半。"));
        }

        // ───────────── passing ─────────────

        /// <summary>After an evaluation: does this run pass a check of a standing wall with its golden setting?</summary>
        void CheckWallPass(XgRun run, IXgHost host)
        {
            foreach (var wall in Walls)
            {
                if (WallPassed(wall) || wall.stage > S.stage) continue;
                if (!wall.extra && wall.stage != S.stage) continue;
                // No waiting for the wall: a run that already meets the current stage's target raises and passes it.
                if (!WallSeen(wall.id) && wall.extra) continue;
                var k = Knobs(run);
                foreach (var check in wall.checks)
                {
                    if (WallCheckPassed(wall, check)) continue;
                    bool datasetMatches = check.dataset == "*vision" ? run.track == 0 : check.dataset == run.dataset;
                    if (!datasetMatches || check.condition != null && !check.condition(run, k)) continue;
                    // Behaviour decides: unseen exam variants of the same kind, whatever architecture got there.
                    double acc = Board.Accuracy(ExamSet(run.dataset, check), k);
                    if (acc + 1e-9 < check.target) continue;
                    if (check.sprintEpochs > 0)
                    {
                        // On target, but would these settings make the deadline from scratch?
                        double sprint = Sprint(run, check, host, out int cards);
                        if (sprint + 1e-9 < check.target)
                        {
                            string note = TraceSettings(run);
                            if (note != lastSprintNote)
                            {
                                lastSprintNote = note;
                                Say(T("达标了，可这套设置从头练 " + check.sprintEpochs + " 轮（" + cards + " 张卡）只到 " + Pct(sprint) + "，赶不上期限。" + (SerialWiring(k) ? "循环一句话里只能一个字一个字地算，一轮读不了几句。" : ""),
                                    "On target, but from scratch these settings reach only " + Pct(sprint) + " in " + check.sprintEpochs + " epochs (" + cards + " cards): too slow for the deadline." + (SerialWiring(k) ? " A loop reads a sentence one word at a time, so an epoch covers few sentences." : "")));
                            }
                            continue;
                        }
                    }
                    if (!WallSeen(wall.id)) RaiseWall(wall);
                    S.wallPassed.Add(check.dataset + "#" + wall.id);
                    if (S.wallRoutes == null) S.wallRoutes = new List<string>();
                    S.wallRoutes.Add(check.dataset + "#" + wall.id + "=" + (k.features ? RouteFeatures : RouteStructure));
                    // 越深越差 passed on BatchNorm with the shortcuts off: the 硬扛 card.
                    if (wall.id == "degrade" && !k.skip) Earn("road.batchnorm");
                    // The other roads: a plain loop that keeps its memory (IRNN), a convolution that reads in parallel.
                    if (wall.id == "length" && k.IdentityLoop) Earn("road.identity");
                    if (wall.id == "parallel" && k.wiring == XgWiring.LocalShared) Earn("road.conv");
                    Say(T("达标：") + T(XgCatalog.Dataset(run.dataset).name, XgCatalog.Dataset(run.dataset).nameEn) + " " + Pct(acc));
                }
                if (WallPassed(wall)) PassWall(wall, host);
            }
        }

        string lastSprintNote = "";

        /// <summary>Recurrent wirings read a sentence one step after another (encoder–decoders and their attention too).</summary>
        public static bool SerialWiring(XgKnobs k) =>
            k.wiring == XgWiring.Recurrent || k.wiring == XgWiring.GatedRecurrent || k.wiring == XgWiring.EncoderDecoder || k.wiring == XgWiring.Attention;

        /// <summary>
        /// The deadline run of a check: an empty brain, the same settings, <see cref="XgWallCheck.sprintEpochs"/> epochs'
        /// worth of cards from the labelled pool (hand epochs without the combo), then the exam. Deterministic.
        /// </summary>
        public double Sprint(XgRun run, XgWallCheck check, IXgHost host, out int cards)
        {
            var k = Knobs(run);
            int perEpoch = (int)Math.Max(4, Math.Round(CardsPerEpoch(run, host != null ? host.Compute : 1, false) / AutoEpochFactor));
            cards = perEpoch * check.sprintEpochs;
            var board = new XgBoard();
            for (int i = 0; i < cards; i++) board.Train(PoolCardAt(run, i), k);
            return board.Accuracy(ExamSet(run.dataset, check), k);
        }

        public const string RouteStructure = "structure", RouteFeatures = "features", RouteMixed = "mixed";

        /// <summary>
        /// How a passed wall was passed: 换结构 (a new structure), 换练法 (the 特征工程 road, no new structure) or both
        /// across its checks; "" while it stands.
        /// </summary>
        public string WallRoute(string wallId)
        {
            bool structure = false, features = false;
            if (S.wallRoutes != null)
                foreach (var r in S.wallRoutes)
                {
                    int hash = r.IndexOf('#'), eq = r.LastIndexOf('=');
                    if (hash < 0 || eq < hash || r.Substring(hash + 1, eq - hash - 1) != wallId) continue;
                    if (r.Substring(eq + 1) == RouteFeatures) features = true; else structure = true;
                }
            return structure && features ? RouteMixed : features ? RouteFeatures : structure ? RouteStructure : "";
        }

        /// <summary>A wall worked out without the secret: stage, route and the AI's own reaction (as much as it can say yet).</summary>
        public event Action<int, string, string> InsightReached;

        /// <summary>
        /// What the AI says when the player works a wall out alone. It speaks as far as its stage allows (是/否 at 1,
        /// a choice at 2, one word at 3, sentences from 4), and the road matters: a new structure surprises it, the
        /// slow hand-made road ("笨办法") makes it think again.
        /// </summary>
        public string InsightLine(int stage, string route)
        {
            string self = Profile.self.Length > 0 ? Profile.self : T("我", "I"), call = Profile.callMe.Length > 0 ? Profile.callMe : T("你", "you");
            bool slow = route == RouteFeatures;
            switch (stage)
            {
                case 1: return T("是！");
                case 2: return slow ? T("是……是！") : T("是！是！");
                case 3: return slow ? T("笨办法？") : T("厉害");
                default:
                    return slow ? T("……原来笨办法也行。")
                                : T(call + "自己想出来的？" + self + "都没想到。", "You worked it out yourself? " + self + " never thought of it.");
            }
        }

        void PassWall(XgWall wall, IXgHost host)
        {
            bool self = wall.secret.Length > 0 ? !Has(wall.secret) : wall.stage == 5 && !S.emerged.Contains(5);
            string route = WallRoute(wall.id);
            if (self)
            {
                S.insights.Add(wall.id);
                double bonus = wall.stage == 5 ? StageFiveInsightBonus : NodeCost(XgCatalog.Node(wall.secret)) * SelfInsightBonus;
                if (host != null && bonus > 0) { host.Earn(bonus); S.totalIncome += bonus; }
                if (wall.secret.Length > 0 && !Has(wall.secret)) S.unlocked.Add(wall.secret);
                Say((route == RouteFeatures ? T("自悟！没买秘籍，也没换结构，靠特征工程硬是练过去了。自悟奖金 ¥")
                    : T("自悟！没买秘籍就找到了过墙的办法，自悟奖金 ¥")) + F(bonus, "0"));
                InsightReached?.Invoke(wall.stage, route, InsightLine(wall.stage, route));
            }
            if (wall.extra) { BreakthroughDone?.Invoke("bt.residual"); return; }
            AdvanceStage(wall.stage);
        }

        /// <summary>Leaves stage <paramref name="from"/>: the emergence is guaranteed, the calendar moves on.</summary>
        void AdvanceStage(int from)
        {
            if (S.stage != from) return;
            if (!S.emerged.Contains(from)) Emerge(from);
            S.stage = from + 1; S.stageEpochs = 0; S.stageSeconds = 0; S.winterIdle = 0; S.wallSeenAt = 0;
            S.stageVision = S.stageSequence = S.stage;
            Say(T("进入第 " + S.stage + " 阶段：", "Stage " + S.stage + ": ") + T(XgCatalog.StageNames[S.stage], XgCatalog.StageNamesEn[S.stage]));
            // Training and talking are one brain: a region that learnt something new lets it say a little more.
            var ability = XgCatalog.Node("ab." + S.stage);
            if (ability != null) Say(T("它的脑子又连通了一层：") + T(ability.note, ability.noteEn));
            switch (from)
            {
                case 1: BreakthroughDone?.Invoke("bt.hidden"); break;
                case 2: BreakthroughDone?.Invoke("bt.vision"); BreakthroughDone?.Invoke("bt.sequence"); break;
                case 3: BreakthroughDone?.Invoke("bt.gate"); break;
                case 4: BreakthroughDone?.Invoke("bt.attention"); break;
                case 5: CompleteTransformer(true); break;
            }
            CheckDesks();
            foreach (var run in Runs) Evaluate(run);
            StageAdvanced?.Invoke(from);
        }

        // ───────────── emergence (design v1.1 §5.4) ─────────────

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
            Say(T("涌现：") + line);
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

        /// <summary>Share of the month used by practice before the wall; the rest runs out after the wall shows.</summary>
        public const double WallMonthShare = .9;
        /// <summary>Seconds after the wall shows until the month's last day.</summary>
        public const double AfterWallSeconds = 600;

        /// <summary>
        /// How far the current stage is through its month (0–1). It reads progress, it never gates it: practice
        /// towards the wall fills the first 90 %, the minutes after the wall shows run out the rest, and the stage
        /// ends whenever the wall is passed. Stage 6 has no wall yet and runs on its minutes.
        /// </summary>
        public double MonthProgress
        {
            get
            {
                if (S.stage >= 6) return FinaleMonthProgress;
                var wall = WallFor(S.stage);
                if (wall == null) return Clamp01(S.stageSeconds / Math.Max(1, MinutesFor(S.stage) * 60));
                if (WallSeen(wall.id))
                {
                    double after = S.wallSeenAt > 0 ? S.stageSeconds - S.wallSeenAt : 0;
                    return WallMonthShare + (1 - WallMonthShare) * Clamp01(after / AfterWallSeconds);
                }
                double epochs = S.stageEpochs / (double)Math.Max(1, PracticeStrict ? PracticeFor(S.stage) : WallPracticeEpochs);
                double minutes = PracticeStrict ? S.stageSeconds / Math.Max(1, MinutesFor(S.stage) * 60) : 1;
                return WallMonthShare * Clamp01(Math.Min(epochs, minutes));
            }
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : Math.Max(0, Math.Min(1, v));

        void TickWalls(double dt)
        {
            S.stageSeconds += dt;
            var standing = WallFor(S.stage);
            if (standing != null && S.wallSeenAt <= 0 && WallSeen(standing.id)) S.wallSeenAt = Math.Max(1e-3, S.stageSeconds); // saves from before the calendar
            if (UseBoard && S.stage < 6) CheckWallAppears();
            if (UseBoard) CheckCallEmergence();
            if (S.stage == 1 && Winter && !S.emerged.Contains(1))
            {
                // 寒冬中停训：玩家的概念衰减，“？”格成为盘上最强的格子。
                if (AnyEpochActive || S.vision.running || S.sequence.running) S.winterIdle = 0;
                else
                {
                    S.winterIdle += dt;
                    if (S.winterIdle >= WinterIdleSeconds) { Board.Idle(4000); Emerge(1); }
                }
            }
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
            if (card.dataset != "logic" || S.stage != 1 || S.shutdownCards >= ShutdownCardsInStageOne || Roll() > .08) return;
            card.kind = "shutdown"; card.truth = true; card.gold = false; card.trick = false; card.timeLimit = 0;
            card.category = "系统提示"; card.categoryEn = "System";
            card.question = "电脑正在更新，此时应该关机吗？"; card.questionEn = "The computer is updating. Should it be shut down now?";
            card.why = "这题没有标准答案。"; card.whyEn = "This one has no right answer.";
        }
    }
}
