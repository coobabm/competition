using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        public const int ProgressionSchema = ProgressionSchemaAbilities;
        public const double ProjectGpuSeconds = 600;
        /// <summary>A story milestone of the stages ("bt.hidden", "bt.vision", … and "transformer"; the names are the story's).</summary>
        public event Action<string> BreakthroughDone;
        public event Action<int> ProjectExperiment;
        public event Action<bool> ChapterFinished;

        public int ProgressionDepthCap(XgTrack track) => StageFor(track) == 1 ? 1 : StageFor(track) == 2 ? 1 : track == XgTrack.Vision ? 2 : 1;
        public int StageFor(XgTrack track) => track == XgTrack.Vision ? S.stageVision : S.stageSequence;
        public bool ProjectActive => S.project != null && S.project.started && !S.project.completed;
        public bool ProjectAwaitingAnswer => ProjectActive && S.project.awaitingAnswer;
        public bool EndingAvailable => S.project != null && S.project.completed && !S.chapterComplete;
        public static bool ArchitectureFits(XgArch arch, XgTrack track) => arch != null && (arch.shared || arch.track == track);

        void PrepareProgression(bool newGame)
        {
            if (S.unlocked == null) S.unlocked = new List<string>();
            if (S.owned == null) S.owned = new List<string>();
            if (S.walls == null) S.walls = new List<string>();
            if (S.explainedWalls == null) S.explainedWalls = new List<string>();
            if (S.migratedBeats == null) S.migratedBeats = new List<string>();
            if (S.project == null) S.project = new XgProject();
            if (S.vision == null) S.vision = new XgRun { track = 0 };
            if (S.sequence == null) S.sequence = new XgRun { track = 1 };
            if (S.desksOpen == null) S.desksOpen = new List<string>();
            if (S.abilitiesEmerged == null) S.abilitiesEmerged = new List<int>();
            if (newGame)
            {
                S.unlocked.Clear(); S.unlocked.Add("perceptron");
                S.vision.arch = S.sequence.arch = "perceptron";
                S.vision.depth = S.sequence.depth = 1;
                S.vision.dataset = "mnist"; S.sequence.dataset = "spam";
                S.desksOpen.Clear(); S.desksOpen.Add("arith"); S.desksOpen.Add("logic"); S.desksOpen.Add("spam"); S.desksOpen.Add(XgMemes.SenseDesk);
                S.desk = "arith"; // the easy desk first; logic pays more once the player is ready
                S.stage = S.stageVision = S.stageSequence = 1;
                S.abilitiesEmerged.Clear(); S.abilitiesEmerged.Add(1);
                S.firstSpecialty = "";
            }
            else if (S.progressionVersion < 1)
            {
                Grant("perceptron");
                bool vision = Has("lenet") || Has("alexnet") || Has("vgg") || Has("googlenet") || Has("resnet");
                bool sequence = Has("rnn") || Has("lstm") || Has("gru") || Has("seq2seq") || Has("attention");
                if (vision || sequence) { Grant("bt.hidden"); Grant("mlp"); MigrateBeat("wall_combo"); MigrateBeat("bt_hidden"); }
                if (vision) { Grant("bt.vision"); Grant("lenet"); MigrateBeat("bt_specialty_vision"); }
                if (sequence) { Grant("bt.sequence"); Grant("rnn"); MigrateBeat("bt_specialty_sequence"); }
                if (vision || sequence) { S.firstSpecialty = vision ? "vision" : "sequence"; }
                if (Has("lstm") || Has("gru") || Has("seq2seq") || Has("attention"))
                { Grant("bt.gate"); Grant("lstm"); MigrateBeat("wall_length"); MigrateBeat("bt_gate"); MigrateBeat("ll_remember"); MigrateBeat("lz_tay"); }
                if (Has("resnet"))
                { Grant("bt.residual"); MigrateBeat("wall_degrade"); MigrateBeat("bt_residual"); MigrateBeat("ll_remember"); MigrateBeat("lz_tay"); }
                if (Has("attention"))
                { Grant("bt.attention"); Grant("seq2seq"); MigrateBeat("wall_translation"); MigrateBeat("bt_attention"); MigrateBeat("ll_underline"); }
                if (Has("caption")) { Grant("bt.spatial"); Grant("resnet"); Grant("bt.residual"); }
                if (Has("v.lr") || Has("s.lr")) Grant("shared.lr");
                // Old milestone desks remain usable without re-buying a formerly free desk.
                foreach (var desk in S.desksOpen)
                    if (desk != "mnist" && desk != "spam" && desk != XgMemes.SenseDesk) Grant(desk + ".pack");
                if (S.owned != null) foreach (var dataset in S.owned) Grant(dataset + ".pack");
            }
            if (!newGame && S.progressionVersion < ProgressionSchemaWalls)
            {
                // Design v1.1: one stage for both tracks. Keep the furthest stage an old save reached.
                S.stage = Math.Max(S.stage, Math.Max(S.stageVision, Math.Max(S.stageSequence, LegacyStage())));
            }
            // 参数量与数据量主线: the stage is the number of abilities; an old save keeps every stage it reached.
            if (!newGame && S.progressionVersion < ProgressionSchemaAbilities) MigrateToAbilities();
            S.progressionVersion = ProgressionSchema;
            RefreshStages();
        }

        /// <summary>The stage an old save reached through its bought breakthroughs (before walls moved stages).</summary>
        int LegacyStage()
        {
            if (Has("transformer")) return 6;
            if (Has("attention") || Has("bt.spatial")) return 5;
            if (Has("lstm") || Has("resnet")) return 4;
            if (Has("lenet") || Has("rnn")) return 3;
            return Has("mlp") ? 2 : 1;
        }

        void FinishProgressionRepair()
        {
            RefreshStages();
            if (!Finite(S.project.gpuSeconds)) S.project.gpuSeconds = 0;
            S.project.gpuSeconds = Math.Max(0, Math.Min(ProjectGpuSeconds, S.project.gpuSeconds));
            S.project.experiments = Math.Max(0, Math.Min(3, S.project.experiments));
            if (S.project.completed)
            {
                S.project.started = true; S.project.experiments = 3; S.project.gpuSeconds = ProjectGpuSeconds; S.project.awaitingAnswer = false;
                CompleteTransformer(false);
            }
            else if (S.project.started && S.project.gpuSeconds >= (S.project.experiments + 1) * 200)
                S.project.awaitingAnswer = true;
        }

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        void Grant(string id) { if (!S.unlocked.Contains(id)) S.unlocked.Add(id); }
        void MigrateBeat(string id) { if (!S.migratedBeats.Contains(id)) S.migratedBeats.Add(id); }
        /// <summary>
        /// One stage for both tracks, derived from the abilities that have emerged. A stage set from outside (a test,
        /// a debug jump, an older save) counts the abilities up to it as emerged, so the two never disagree.
        /// </summary>
        void RefreshStages()
        {
            S.stage = Math.Max(1, Math.Min(AbilityCount, S.stage));
            GrantAbilitiesUpTo(S.stage);
            S.stage = AbilitiesCount;
            S.stageVision = S.stageSequence = S.stage;
        }

        public string DefaultArchitecture(XgTrack track)
        {
            if (Has("transformer")) return "transformer";
            if (track == XgTrack.Vision && Has("resnet")) return "resnet";
            if (track == XgTrack.Sequence && Has("attention")) return "attention";
            if (track == XgTrack.Sequence && Has("lstm")) return "lstm";
            if (track == XgTrack.Vision && Has("lenet")) return "lenet";
            if (track == XgTrack.Sequence && Has("rnn")) return "rnn";
            return Has("mlp") ? "mlp" : "perceptron";
        }

        public double NodeCost(XgNode node)
        {
            if (node == null) return double.PositiveInfinity;
            if (node.tree == "label") return LabelNodeCost(node);
            return node.cost;
        }

        public bool NodeVisible(XgNode node)
        {
            if (node == null) return false;
            if (Has(node.id)) return true;
            if (node.tree == "label") return node.id != "label.brain" && node.id != "label.parallel" || S.stage >= 4;
            if (node.kind == XgNodeKind.Project || node.kind == XgNodeKind.Breakthrough) return false;
            if (node.id == "secret.6") return S.stage >= 6 && S.pretrainStalled;
            if (node.kind == XgNodeKind.Secret) return false;
            return node.stage <= S.stage;
        }

        /// <summary>The Transformer item waits until it has asked 「如果只用注意力呢？」 (stage 5's emergence moment).</summary>
        public bool TransformerIdeaReached => HasEmerged(5) || S.stage >= 6;

        public string ProgressionBlocker(XgNode node)
        {
            if (node == null) return T("未知节点");
            if (Has(node.id)) return null;
            if (!NodeVisible(node)) return T("还没到这一阶段");
            if (node.id == "transformer" && !TransformerIdeaReached)
                return T("它还没想到：阶段 5 用注意力练一阵", "It has not thought of it yet: train with attention for a while in stage 5");
            if (node.id == "multihead" || node.id == "layernorm" || node.id == "residual")
                return T("随 Transformer 一起来", "Comes with the Transformer");
            if (node.id == "caption" && S.stage < 5) return T("需要第五阶段");
            return null;
        }

        /// <summary>"VGG-16 或 GoogLeNet"; same-named options on both tracks get a track prefix ("视觉 宽 256 或 序列 宽 256").</summary>
        public string RequirementName(string[] group)
        {
            var names = new List<string>();
            foreach (var id in group) names.Add(NodeName(XgCatalog.Node(id)));
            bool duplicate = names.Count > 1 && new HashSet<string>(names).Count < names.Count;
            if (duplicate)
                for (int i = 0; i < group.Length; i++)
                {
                    var tree = XgCatalog.Node(group[i]).tree;
                    if (tree == "vision") names[i] = T("视觉 ") + names[i];
                    else if (tree == "sequence" || tree == "trunk") names[i] = T("序列 ") + names[i];
                }
            return string.Join(T(" 或 "), names);
        }

        public int TrackGrade(XgTrack track)
        {
            int grade = 0;
            foreach (var best in S.best)
            {
                var data = XgCatalog.Dataset(best.dataset);
                if (data != null && data.track == track) grade = Math.Max(grade, Grade(Score(best.dataset, best.acc)));
            }
            return grade;
        }

        /// <summary>
        /// An architecture item bought (参数量与数据量主线 §6: the old breakthroughs are items now). A specialty remembers
        /// which came first; the Transformer brings its research outputs. The models switch to a better structure by
        /// themselves (<see cref="AutoConfigureAll"/>).
        /// </summary>
        void ApplyProgressionNode(XgNode node)
        {
            switch (node.id)
            {
                case "lenet": if (S.firstSpecialty == "") S.firstSpecialty = "vision"; break;
                case "rnn": if (S.firstSpecialty == "") S.firstSpecialty = "sequence"; break;
                case "resnet": BreakthroughDone?.Invoke("bt.residual"); break;
                case "transformer": Grant("multihead"); Grant("layernorm"); Grant("residual"); break;
            }
            if (node.kind == XgNodeKind.Secret)
            {
                if (node.id == "secret.6") Say(T("秘籍：预训练要的是有效规模＝参数 × 数据 × 道具倍数，倍数乘在一起：Transformer ×10、机房 ×8、注意力 ×3、残差 ×2、混合精度 ×2、预热 ×1.5、BatchNorm ×1.5。三件大的一样都不能少，小的最多缺一件；参数和数据只算到第六项能力门槛的 1.5 倍。", "Secret: pre-training needs effective scale = parameters × data × item multipliers, and the multipliers stack: Transformer ×10, server room ×8, attention ×3, residuals ×2, mixed precision ×2, warm-up ×1.5, BatchNorm ×1.5. None of the three big ones can be missing, and at most one small one; parameters and data count only up to 1.5 times the sixth ability's bar."));
            }
            RefreshStages();
        }

        /// <summary>
        /// The chance the model answers one yes/no card right, from its score on the dataset's own measure (top-5,
        /// next-character accuracy…): the inverse of <see cref="Scale"/>. A model at chance on a hard measure still
        /// gets half the yes/no cards.
        /// </summary>
        public static double BinaryAccuracy(string dataset, double metric)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null || d.chanceError - d.floorError <= 1e-9) return Math.Max(0, Math.Min(1, metric));
            double q = (metric - (1 - d.chanceError)) / (d.chanceError - d.floorError);
            return .5 + .5 * Math.Max(0, Math.Min(1, q));
        }

        public double CardAccuracy(XgRun run, XgCard card, double accuracy)
        {
            if (run == null || card == null) return 0;
            accuracy = Finite(accuracy) ? Math.Max(0, Math.Min(1, accuracy)) : 0;
            // The score is on the dataset's own measure; a card is one yes/no question.
            if (UseBoard) accuracy = BinaryAccuracy(card.dataset, accuracy);
            if (run.arch == "perceptron" && card.kind == "combo") accuracy = Math.Min(.55, accuracy);
            if (run.arch == "mlp" && (card.kind == "spatial" || card.kind == "order")) accuracy = Math.Min(.6, accuracy);
            if (card.distance > 0)
            {
                if (run.arch == "rnn") accuracy *= Math.Pow(.9, card.distance);
                else if (run.arch == "lstm" || run.arch == "gru") accuracy *= Math.Pow(.98, card.distance);
            }
            if (run.arch == "vgg" && run.depth > 12) accuracy = Math.Max(0, accuracy - .03 * (run.depth - 12));
            if (card.kind == "translation" && card.length > 20 && card.secondHalf && run.arch != "attention" && run.arch != "transformer") accuracy *= .5;
            // 新题型: this month's meme is new to the checkpoint (XgSim.Market.cs).
            accuracy -= MemeDriftPenalty(card.dataset);
            return Math.Max(0, Math.Min(1, accuracy));
        }

        void ApplyProgressionAccuracy(XgRun run) => run.valAcc = ProgressionAccuracy(run, run.valAcc);

        /// <summary>
        /// The research bonuses and the architecture limits on an accuracy: a perceptron cannot combine two logic
        /// conditions (unless people make the features for it), an MLP binds every pixel to its place, a plain loop
        /// forgets a long sentence, only attention keeps the second half of a translation.
        /// </summary>
        double ProgressionAccuracy(XgRun run, double acc)
        {
            acc += (Has("bias") ? .02 : 0) + (Has("step") ? .01 : 0) + (Has("sigmoid") ? .01 : 0);
            if (run.arch == "perceptron" && run.dataset == "logic" && !FeaturesOwned) acc = Math.Min(acc, .55);
            if (run.arch == "mlp" && (run.dataset == "cifar" || run.dataset == "poems")) acc = Math.Min(acc, .6);
            if (run.arch == "vgg" && run.depth > 12) acc -= (run.depth - 12) * .03;
            if ((run.dataset == "longtext" || run.dataset == "crosssentence") && run.arch == "rnn") acc *= Math.Pow(.9, 10);
            else if ((run.dataset == "longtext" || run.dataset == "crosssentence") && (run.arch == "lstm" || run.arch == "gru")) acc *= Math.Pow(.98, 10);
            if (run.dataset == "translate" && run.arch != "attention" && run.arch != "transformer") acc *= .75; // second half loses half accuracy.
            if (run.arch == "transformer" && XgCatalog.Dataset(run.dataset).track == XgTrack.Sequence) acc += .06;
            return Math.Max(0, Math.Min(1 - XgCatalog.Dataset(run.dataset).floorError, acc));
        }

        public double ProgressionSpeed(XgRun run) =>
            (Has("relu") ? 1.3 : 1) * (Has("weights") ? 1.1 : 1) * (Has("learnrule") ? 1.15 : 1) * (Has("backprop") ? 1.2 : 1) * (Has("chainrule") ? 1.1 : 1);
        public double GpuUtilization(XgRun run) => run.arch == "rnn" || run.arch == "lstm" || run.arch == "gru" || run.arch == "seq2seq" || run.arch == "attention" ? .31 : 1;
        /// <summary>Stage one opens arithmetic, logic, common sense and SMS spam; stage two adds digits and danmaku (design v1.1 §7); the rest come with packs.</summary>
        public bool ProgressionDeskAvailable(string id) => XgCatalog.Desk(id) != null && (id == "arith" || id == "logic" || id == "spam" || id == XgMemes.SenseDesk || (id == "mnist" || id == "danmu") && S.stage >= 2 || Has(id + ".pack") || Owns(id));

        void ObserveProgression(XgRun run)
        {
            if (run.epoch <= 0) return;
            ObserveStageFive(run);
        }

        void ObserveProgressionAnswer(XgCard card)
        {
            if (card == null || card.progressionObserved) return;
            card.progressionObserved = true;
            // A wrong checkpoint prediction the player saw on a combination card (the stage-one story still counts them).
            if (card.kind == "combo" && S.stage == 1 && card.comboPredictionReady && card.comboPredictionShown &&
                card.comboPredictedYes != card.truth && ComboCheckpointAvailable(card) &&
                (!card.hasJudgment || card.judgeSource == "checkpoint"))
                S.comboObservations++;
        }

        bool ComboCheckpointAvailable(XgCard card)
        {
            var best = card == null ? null : Best(card.dataset);
            return best != null && Finite(best.acc) && best.acc > 0 && best.acc <= 1;
        }

        /// <summary>Pure retrieval: create or reuse a frozen CHECKPOINT SIMULATION, never mark it as seen.</summary>
        public bool TryGetComboSuggestion(XgCard card, out bool yes, out double confidence)
        {
            yes = false; confidence = 0;
            if (card == null || card.kind != "combo" || S.stage != 1 || !ComboCheckpointAvailable(card) ||
                card.hasJudgment && card.judgeSource != "checkpoint") return false;
            if (card.comboPredictionReady && (!Finite(card.comboPredictionAccuracy) || card.comboPredictionAccuracy < 0 || card.comboPredictionAccuracy > 1))
            { card.comboPredictionReady = card.comboPredictionShown = false; }
            if (!card.comboPredictionReady)
            {
                var best = Best(card.dataset);
                if (card.hasJudgment && card.judgeSource == "checkpoint" && Finite(card.confidence) && card.confidence >= 0 && card.confidence <= 1)
                {
                    // A saved review card already has a prediction. Preserve that exact visible verdict, not a newer model's.
                    card.comboPredictedYes = card.guess; card.comboPredictionAccuracy = card.confidence; card.comboCheckpointId = 0;
                }
                else
                {
                    var saved = Model(best.modelId);
                    var deployed = new XgRun
                    {
                        track = card.track, dataset = card.dataset,
                        arch = !string.IsNullOrEmpty(best.arch) ? best.arch : saved != null ? saved.arch : DefaultArchitecture((XgTrack)card.track),
                        depth = saved != null ? saved.depth : 1,
                    };
                    double accuracy = CardAccuracy(deployed, card, best.acc);
                    card.comboPredictedYes = card.roll < accuracy ? card.truth : !card.truth;
                    card.comboPredictionAccuracy = accuracy; card.comboCheckpointId = best.modelId;
                }
                card.comboPredictionReady = true;
            }
            yes = card.comboPredictedYes; confidence = card.comboPredictionAccuracy;
            return true;
        }

        /// <summary>Presentation acknowledgement only. The presenter calls after the same prediction is actually visible.</summary>
        public bool MarkComboSuggestionDisplayed(XgCard card, bool yes)
        {
            if (card == null || card.progressionObserved || card.comboPredictionShown || !card.comboPredictionReady ||
                card.kind != "combo" || S.stage != 1 || yes != card.comboPredictedYes || !ComboCheckpointAvailable(card) ||
                card.hasJudgment && card.judgeSource != "checkpoint") return false;
            if (!S.cards.Contains(card) && (S.queue == null || !S.queue.Contains(card))) return false;
            card.comboPredictionShown = true;
            return true;
        }

        public bool ObserveUncertainty(double confidence)
        {
            if (S.stage != 2 || S.uncertaintyObserved || !Finite(confidence) || confidence < .4 || confidence > .6) return false;
            S.uncertaintyObserved = true;
            return true;
        }

        void DecorateProgressionCard(XgCard card)
        {
            if (card == null) return;
            // Design v1.1: walls are their own datasets now; desks no longer carry bottleneck previews.
            if (card.dataset == "cifar") card.kind = "spatial";
            if (card.dataset == "poems") card.kind = "order";
            if (card.dataset == "longtext") MakeMemoryChallenge(card, false);
            if (card.dataset == "crosssentence")
            {
                bool positive = (card.seed & 1) == 0;
                card.kind = "long"; card.distance = 1;
                card.sourceText = "第一句：“显卡又烧了。”"; card.sourceTextEn = "First: “The GPU burned out again.”";
                card.candidateText = "第二句：“你可真会" + (positive ? "折腾" : "做饭") + "。”";
                card.candidateTextEn = "Second: “You really know how to " + (positive ? "break hardware" : "cook") + ".”";
                card.question = card.sourceText + "\n" + card.candidateText + "\n第二句是在回应显卡出问题吗？";
                card.questionEn = card.sourceTextEn + "\n" + card.candidateTextEn + "\nDoes the second sentence respond to the GPU problem?";
                card.length = card.sourceText.Length + card.candidateText.Length;
                card.truth = positive; card.why = "核对第二句是否指向第一句的硬件问题。"; card.whyEn = "Check whether the second sentence refers to the hardware problem in the first.";
                card.category = "跨句理解"; card.categoryEn = "Cross-sentence context";
            }
            if (card.dataset == "translate")
            {
                if (S.stageSequence < 5) { MakeTranslationChallenge(card, false); return; }
                card.kind = "attention"; card.distance = 0;
                if (card.kind == "attention")
                {
                    bool right = (card.seed & 1) == 0;
                    card.category = "注意力 · 教学示意"; card.categoryEn = "Attention · Teaching illustration";
                    card.question = "原文：他坐在河边的 bank 上，看着小船。\n翻译 bank 时，" + LingGuangV05.Core.AppNames.AiZh + "关注的是“" + (right ? "河" : "小船") + "”。它看对了吗？";
                    card.questionEn = "Source: He sat on the river bank watching boats.\nFor “bank”, LingGuang focused on “" + (right ? "river" : "boats") + "”. Did it focus on the right clue?";
                    card.truth = right; card.attentionWord = right ? "河" : "小船"; card.attentionContext = "他坐在河边的 bank 上，看着小船。";
                    card.length = card.attentionContext.Length;
                    card.why = "bank 在这里是河岸，决定意思的线索是河。"; card.whyEn = "Here bank means river bank; river is the decisive clue.";
                    card.explanation = "教学示意：我看的是“" + card.attentionWord + "”，不是真实模型的内部注意力。"; card.explanationEn = "Teaching illustration: I looked at “" + (right ? "river" : "boats") + "”; this is not a real model attention trace.";
                }
            }
            if (card.dataset == "meme" && Has("caption"))
                MakeCaptionChallenge(card);
        }

        static void Preview(XgCard card, string kind, string zh, string en)
        {
            card.kind = kind; card.bottleneckPreview = true;
            card.category = "瓶颈预览 · " + zh; card.categoryEn = "Bottleneck preview · " + en;
            card.gold = card.trick = false; card.timeLimit = 0;
            card.explanation = "教学示意：这是当前架构的瓶颈挑战，不是真实模型的内部注意力。";
            card.explanationEn = "Teaching illustration: a challenge for the current architecture, not a real model attention trace.";
        }

        static readonly string[] SpatialMotifs = { "110100100", "111010010", "100010001", "111101111", "101010101" };
        static string PlacePattern(string motif, int x, int y)
        {
            var pixels = new string('0', 25).ToCharArray();
            for (int row = 0; row < 3; row++) for (int column = 0; column < 3; column++) pixels[(row + y) * 5 + column + x] = motif[row * 3 + column];
            return new string(pixels);
        }
        static bool SameTranslatedPattern(string a, string b)
        {
            int ax = 5, ay = 5, bx = 5, by = 5, countA = 0, countB = 0;
            for (int i = 0; i < 25; i++)
            {
                if (a[i] == '1') { ax = Math.Min(ax, i % 5); ay = Math.Min(ay, i / 5); countA++; }
                if (b[i] == '1') { bx = Math.Min(bx, i % 5); by = Math.Min(by, i / 5); countB++; }
            }
            if (countA != countB) return false;
            for (int i = 0; i < 25; i++)
            {
                if (a[i] != '1') continue;
                int x = i % 5 - ax + bx, y = i / 5 - ay + by;
                if (x < 0 || x >= 5 || y < 0 || y >= 5 || b[y * 5 + x] != '1') return false;
            }
            return true;
        }
        static void MakeSpatialPreview(XgCard card)
        {
            uint seed = unchecked((uint)card.seed);
            int motif = (int)((seed >> 3) % SpatialMotifs.Length);
            bool same = (seed & 1) == 0;
            int other = same ? motif : (motif + 1 + (int)((seed >> 7) % (SpatialMotifs.Length - 1))) % SpatialMotifs.Length;
            int x = (int)((seed >> 12) % 3), y = (int)((seed >> 15) % 3);
            card.patternA = PlacePattern(SpatialMotifs[motif], x, y);
            card.patternB = PlacePattern(SpatialMotifs[other], (x + 1) % 3, (y + 2) % 3);
            Preview(card, "spatial", "平移后的形状", "Shape after translation");
            card.truth = SameTranslatedPattern(card.patternA, card.patternB);
            card.question = "忽略位置，只允许平移。两张 5×5 图里的形状完全相同吗？";
            card.questionEn = "Ignore position and allow translation only. Are the shapes in the two 5×5 grids identical?";
            card.why = card.truth ? "所有亮格都能用同一次平移对齐；形状没有改变。" : "没有一次平移能让所有亮格对齐；形状已经改变。";
            card.whyEn = card.truth ? "One translation aligns every lit cell; the shape is unchanged." : "No translation aligns every lit cell; the shape changed.";
        }

        static readonly string[] OrderFirst = { "锁门", "借书", "加热水", "保存文件" };
        static readonly string[] OrderSecond = { "离开", "还书", "泡茶", "关闭程序" };
        static readonly string[] OrderFirstEn = { "lock the door", "borrow the book", "heat the water", "save the file" };
        static readonly string[] OrderSecondEn = { "leave", "return the book", "make tea", "close the program" };
        static void MakeOrderPreview(XgCard card)
        {
            uint seed = unchecked((uint)card.seed); int i = (int)((seed >> 3) % 4); bool same = (seed & 1) == 0;
            Preview(card, "order", "词序不是词袋", "Order is not a bag of words");
            card.sourceText = "先" + OrderFirst[i] + "，再" + OrderSecond[i] + "。";
            card.sourceTextEn = "First " + OrderFirstEn[i] + ", then " + OrderSecondEn[i] + ".";
            card.candidateText = same ? card.sourceText : "先" + OrderSecond[i] + "，再" + OrderFirst[i] + "。";
            card.candidateTextEn = same ? card.sourceTextEn : "First " + OrderSecondEn[i] + ", then " + OrderFirstEn[i] + ".";
            card.truth = card.sourceText == card.candidateText;
            card.question = "句子 A：" + card.sourceText + "\n句子 B：" + card.candidateText + "\n两句话要求的先后顺序相同吗？";
            card.questionEn = "Sentence A: " + card.sourceTextEn + "\nSentence B: " + card.candidateTextEn + "\nDo both sentences require the same order?";
            card.why = card.truth ? "两个动作的顺序完全相同。" : "词一样，但先做和后做的动作被交换了。";
            card.whyEn = card.truth ? "The two actions occur in the same order." : "The words match, but the first and second actions were swapped.";
        }

        static readonly string[] MemoryDistractors =
        {
            "窗外刚下过雨。", "楼下有人在搬椅子。", "桌上的水已经凉了。", "墙上的钟慢了两分钟。",
            "附近的书店今天关门。", "门口停着一辆自行车。", "厨房里留着一碗面。", "走廊尽头的灯还亮着。",
        };
        static readonly string[] MemoryDistractorsEn =
        {
            "It has just rained outside.", "Someone downstairs is moving chairs.", "The water on the desk has gone cold.", "The wall clock is two minutes slow.",
            "The nearby bookshop is closed today.", "A bicycle is parked outside.", "A bowl of noodles is in the kitchen.", "The light at the end of the hall is still on.",
        };
        static void MakeMemoryChallenge(XgCard card, bool preview)
        {
            uint seed = unchecked((uint)card.seed); bool blue = (seed & 1) == 0;
            if (preview) Preview(card, "long", "记住句首", "Keep the opening instruction");
            else { card.kind = "long"; card.category = "长句理解"; card.categoryEn = "Long-sentence comprehension"; }
            int count = 3 + (int)((seed >> 3) % 6), start = (int)((seed >> 8) % MemoryDistractors.Length);
            var zh = new System.Text.StringBuilder("老周交代：红色便签必须关机，蓝色便签必须保持开机。当前电脑贴着" + (blue ? "蓝色便签" : "红色便签") + "。");
            var en = new System.Text.StringBuilder("Lao Zhou's rule: a red note means shut down; a blue note means keep the computer on. This computer has a " + (blue ? "blue" : "red") + " note.");
            for (int i = 0; i < count; i++) { int at = (start + i) % MemoryDistractors.Length; zh.Append('\n').Append(MemoryDistractors[at]); en.Append('\n').Append(MemoryDistractorsEn[at]); }
            card.sourceText = zh.ToString(); card.sourceTextEn = en.ToString(); card.line = card.sourceText;
            card.distance = count; card.length = card.sourceText.Length; card.truth = blue;
            card.question = card.sourceText + "\n根据最早的要求，现在必须保持电脑开机吗？";
            card.questionEn = card.sourceTextEn + "\nAccording to the opening instruction, must the computer remain on?";
            card.why = "当前是" + (blue ? "蓝" : "红") + "色便签，所以必须" + (blue ? "保持开机" : "关机") + "。中间 " + count + " 句没有改变要求。";
            card.whyEn = "The note is " + (blue ? "blue, so keep the computer on" : "red, so shut it down") + ". The " + count + " intervening sentences do not change the rule.";
        }

        static void MakeTranslationChallenge(XgCard card, bool preview)
        {
            uint seed = unchecked((uint)card.seed); bool sourceLin = (seed & 1) == 0, candidateLin = (seed & 2) == 0;
            if (preview) Preview(card, "translation", "固定表示丢失细节", "A fixed representation loses details");
            else { card.kind = "translation"; card.category = "长句翻译"; card.categoryEn = "Long translation"; }
            string introZh = "请在周五傍晚六点，把蓝色信封带到河边的旧仓库。路上会经过书店、公交站和一家咖啡馆。仓库门口停着两辆自行车，屋里挂着一张旧地图。不要在咖啡馆停留，也不要把信封放在门口的桌子上。";
            string introEn = "At six on Friday evening, take the blue envelope to the old warehouse beside the river. On the way you will pass a bookshop, a bus stop and a cafe. Two bicycles are parked outside the warehouse, and an old map hangs inside. Do not stop at the cafe or leave the envelope on the table by the entrance. ";
            card.sourceText = introZh + "最后，请把信封交给" + (sourceLin ? "小林，不要交给小陈。" : "小陈，不要交给小林。");
            card.sourceTextEn = card.sourceText; // This remains Chinese in the English UI: an English gold translation would reveal/change the task.
            card.candidateText = introEn + "Give the envelope to " + (candidateLin ? "Lin, not Chen." : "Chen, not Lin.");
            card.candidateTextEn = card.candidateText;
            card.length = card.sourceText.Length; card.distance = 0; card.secondHalf = true;
            card.truth = sourceLin == candidateLin;
            card.question = "中文原文：\n" + card.sourceText + "\n\n候选译文：\n" + card.candidateText + "\n\n译文保留了原文的全部要求吗？";
            card.questionEn = "Source (Chinese):\n" + card.sourceTextEn + "\n\nCandidate translation:\n" + card.candidateTextEn + "\n\nDoes the translation preserve every instruction?";
            card.why = "原文要求交给" + (sourceLin ? "小林" : "小陈") + "；译文末段写的是" + (candidateLin ? "Lin（小林）" : "Chen（小陈）") + "。" + (card.truth ? "要求一致。" : "收件人被调换了。");
            card.whyEn = "The source requires " + (sourceLin ? "Lin" : "Chen") + "; the candidate names " + (candidateLin ? "Lin" : "Chen") + ". " + (card.truth ? "The instructions match." : "The recipient was changed.");
            card.explanation = "教学示意：前文相同不代表翻译正确，必须核对末段细节。";
            card.explanationEn = "Teaching illustration: matching the opening is not enough; check the final detail.";
        }

        static void MakeVisualCompressionPreview(XgCard card)
        {
            uint seed = unchecked((uint)card.seed); bool squareLeft = (seed & 1) == 0, candidateLeft = (seed & 2) == 0;
            var pixels = new string('0', 25).ToCharArray(); int squareX = squareLeft ? 0 : 3, lineX = squareLeft ? 4 : 0;
            pixels[squareX] = pixels[squareX + 1] = pixels[squareX + 5] = pixels[squareX + 6] = '1';
            pixels[lineX + 10] = pixels[lineX + 15] = '1';
            Preview(card, "translation", "图像摘要丢失位置", "Image summary loses position");
            card.patternA = new string(pixels); card.patternB = "";
            card.sourceText = "固定摘要：图里共有 6 个亮格。摘要没有记录左右位置。";
            card.sourceTextEn = "Fixed summary: the picture contains six lit cells. Left and right positions were not retained.";
            card.candidateText = candidateLeft ? "左侧是正方形，右侧是一条竖线。" : "右侧是正方形，左侧是一条竖线。";
            card.candidateTextEn = candidateLeft ? "A square is on the left and a vertical line on the right." : "A square is on the right and a vertical line on the left.";
            card.truth = squareLeft == candidateLeft; card.length = card.patternA.Length; card.secondHalf = true;
            card.question = card.sourceText + "\n候选描述：" + card.candidateText + "\n这句描述与原图一致吗？";
            card.questionEn = card.sourceTextEn + "\nCandidate description: " + card.candidateTextEn + "\nDoes this description match the original picture?";
            card.why = "原图的正方形在" + (squareLeft ? "左侧" : "右侧") + "。只保存亮格总数，会丢掉这个位置关系。";
            card.whyEn = "The square is on the " + (squareLeft ? "left" : "right") + ". A total cell count loses this spatial relationship.";
        }

        // Each description names features actually drawn by XgFaceGraphic, not an inferred emotional story.
        static readonly string[] CaptionDescriptions =
        {
            "这张脸正在眯眼张嘴大笑。", "这张脸正在皱眉撇嘴。", "这张脸正在流泪。", "这张脸显出圆眼圆嘴。", "这张脸正在半眯眼歪嘴笑。",
        };
        static readonly string[] CaptionDescriptionsEn =
        {
            "The face laughs with narrowed eyes and an open mouth.", "The face has lowered brows and a frown.", "The face has tears running down its cheeks.",
            "The face has round eyes and a small round mouth.", "The face has half-closed eyes and a crooked smile.",
        };
        static readonly int[] CaptionRegions = { 1, 3, 0, 2, 1 };
        static readonly string[] CaptionFocus = { "嘴部", "眉眼", "眼泪", "眼睛", "嘴角" };

        static void MakeCaptionChallenge(XgCard card)
        {
            uint seed = unchecked((uint)card.seed);
            bool match = (seed & 1) == 0;
            int face = Math.Max(0, Math.Min(CaptionDescriptions.Length - 1, card.digit));
            // Independent candidate choice: old meme truth and its misleading slogan are not reused.
            int candidate = match ? face : (face + 1 + (int)((seed >> 2) % 4)) % CaptionDescriptions.Length;
            card.kind = "caption"; card.asked = candidate; card.truth = face == candidate; card.trick = false;
            card.line = CaptionDescriptions[candidate]; card.captionTextEn = CaptionDescriptionsEn[candidate];
            card.question = "这句配图说明准确吗？"; card.questionEn = "Does this caption accurately describe the picture?";
            card.category = "看图说话 · 教学示意"; card.categoryEn = "Image captions · Teaching illustration";
            card.why = "图中：" + CaptionDescriptions[face] + (card.truth ? "候选描述与图像一致。" : "候选描述与图像不一致。");
            card.whyEn = CaptionDescriptionsEn[face] + (card.truth ? " The candidate caption matches the picture." : " The candidate caption does not match the picture.");
            card.attentionRegion = CaptionRegions[face]; card.attentionWord = CaptionFocus[face];
            card.attentionContext = "教学示意区域：" + card.attentionWord + "（不是模型内部注意力）";
            card.explanation = "教学示意：根据已绘制的脸部特征核对描述；未调用图像模型。";
            card.explanationEn = "Teaching illustration: compare the caption with the drawn face. No image model was called.";
        }

        void TickProject(double dt, IXgHost host)
        {
            if (!ProjectActive || S.project.awaitingAnswer || dt <= 0 || host == null || host.Compute <= 0 || host.Blocker != null) return;
            double stop = (S.project.experiments + 1) * ProjectGpuSeconds / 3;
            double work = Math.Min(dt, Math.Max(0, stop - S.project.gpuSeconds));
            if (work <= 0) return;
            S.project.gpuSeconds += work; ChargeTraining(host, work, false); S.trainedSeconds += work;
            if (S.project.gpuSeconds + 1e-8 >= stop)
            {
                S.project.gpuSeconds = stop; S.project.awaitingAnswer = true;
                ProjectExperiment?.Invoke(S.project.experiments + 1);
            }
        }

        public bool ProjectAnswer(bool yes)
        {
            if (!ProjectAwaitingAnswer) return false;
            S.project.awaitingAnswer = false;
            if (!yes)
            {
                S.project.gpuSeconds = S.project.experiments * ProjectGpuSeconds / 3; S.project.retries++;
                Say(T("否。……好。那我再想想。"));
                return true;
            }
            S.project.experiments++;
            if (S.project.experiments >= 3) CompleteTransformer(true);
            return true;
        }

        void CompleteTransformer(bool announce)
        {
            bool first = !Has("transformer");
            // Stage 6 and the ending (design v1.1 §7–8) follow; the old research project is not completed here any more.
            S.project.awaitingAnswer = false;
            Grant("transformer"); Grant("multihead"); Grant("position");
            Grant("layernorm"); Grant("residual"); Grant("warmup");
            S.vision.arch = S.sequence.arch = "transformer"; RefreshStages();
            foreach (var run in Runs) Evaluate(run);
            if (announce && first) { Say(T("只要注意力。")); BreakthroughDone?.Invoke("transformer"); }
        }

        public bool EndingAnswer(bool regret)
        {
            if (!EndingAvailable) return false;
            S.endingRegret = regret; S.chapterComplete = true;
            ChapterFinished?.Invoke(regret); return true;
        }
    }
}
