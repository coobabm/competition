using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Achievements earned (ids from <see cref="XgSim.Achievements"/>).</summary>
        public List<string> achievements = new List<string>();
        /// <summary>Self-insight cards already revealed to the player (wall ids, "pretrain").</summary>
        public List<string> cardsShown = new List<string>();
        /// <summary>Big data packs still downloading: dataset id → seconds left (摆渡云, design v1.1 §3).</summary>
        public List<XgScore> downloads = new List<XgScore>();
    }

    /// <summary>An achievement of the 图鉴: one per stage worked out alone, and the hidden one for all six.</summary>
    public sealed class XgAchievement
    {
        public string id, name, nameEn, note, noteEn;
        public bool hidden;
    }

    /// <summary>
    /// The collection side of the lab (design v1.1 §5.3, §11.2): phenomena that show up through the story rather
    /// than one evaluation, the ability nodes, self-insight cards and achievements.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The six stage walls that can be worked out alone, in stage order ("pretrain" is stage 6).</summary>
        public static readonly string[] InsightWalls = { "combo", "structure", "length", "translation", "parallel", "pretrain" };
        /// <summary>Every wall that leaves a card when worked out alone: the six stages and the extra 越深越差.</summary>
        public static readonly string[] CardWalls = { "combo", "structure", "length", "degrade", "translation", "parallel", "pretrain" };

        public static readonly XgAchievement[] Achievements =
        {
            A("insight.combo", "自己想通了异或", "Worked out XOR", "第 1 阶段没买秘籍就过了墙。", "Passed stage 1 without the secret."),
            A("insight.structure", "看邻居，读前文", "Neighbours and context", "第 2 阶段没买秘籍就过了墙。", "Passed stage 2 without the secret."),
            A("insight.length", "学会忘记", "Learning to forget", "第 3 阶段没买秘籍就过了墙。", "Passed stage 3 without the secret."),
            A("insight.translation", "先读完再说", "Read it all first", "第 4 阶段没买秘籍就过了墙。", "Passed stage 4 without the secret."),
            A("insight.parallel", "也想到了", "Thought of it too", "第 5 阶段在它开口之前，自己只用了注意力。", "In stage 5 you went attention-only before it said so."),
            A("insight.pretrain", "规模和稳定", "Scale and stability", "第 6 阶段没买秘籍就让预训练跑通。", "Got pre-training through in stage 6 without the secret."),
            new XgAchievement { id = "lingguang", name = "灵光一现", nameEn = "A Flash of Insight", note = "六个阶段全部自悟。", noteEn = "Worked out all six stages yourself.", hidden = true },
        };

        static XgAchievement A(string id, string zh, string en, string note, string noteEn) => new XgAchievement { id = id, name = zh, nameEn = en, note = note, noteEn = noteEn };
        public static XgAchievement Achievement(string id) { foreach (var a in Achievements) if (a.id == id) return a; return null; }

        /// <summary>A self-insight card is waiting to be revealed (wall id or "pretrain").</summary>
        public event Action<string> InsightCard;
        public event Action<XgAchievement> AchievementEarned;

        public bool HasAchievement(string id) => S.achievements.Contains(id);

        public static bool IsAtlas(XgNode n) => n != null && (n.kind == XgNodeKind.Phenomenon || n.kind == XgNodeKind.Ability);

        /// <summary>A phenomenon node lights the first time the phenomenon happens; an ability node when the AI can do it.</summary>
        public bool AtlasLit(XgNode n)
        {
            if (n == null) return false;
            if (n.kind == XgNodeKind.Phenomenon) return PhenomenonSeen(n.target);
            if (n.kind == XgNodeKind.Ability) return n.value < 6 ? S.stage >= n.value : S.abilities;
            return false;
        }

        /// <summary>Side research of stages 4–6 only adds a little speed (design v1.1 §11.2: "only the feel of the numbers").</summary>
        double FeelSpeed
        {
            get
            {
                double s = 1;
                if (Has("earlystop")) s *= 1.05;
                if (Has("wordvec")) s *= 1.08;
                if (Has("beamsearch")) s *= 1.1;
                if (Has("subword")) s *= 1.05;
                if (Has("sft")) s *= 1.05;
                if (Has("cot")) s *= 1.05;
                return s;
            }
        }

        /// <summary>Packs this big come down through 摆渡云 at the free 100KB/s and take a while.</summary>
        public const double BigPackSamples = 100000;

        public event Action<string, double> DownloadStarted;

        public double DownloadLeft(string dataset) { foreach (var d in S.downloads) if (d.key == dataset) return d.value; return 0; }
        public bool Downloading(string dataset) => DownloadLeft(dataset) > 0;

        void StartDownload(string dataset)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null || d.samples < BigPackSamples || Downloading(dataset)) return;
            double seconds = Math.Max(20, Math.Min(90, d.samples / 20000));
            S.downloads.Add(new XgScore { key = dataset, value = seconds });
            Say(T("摆渡云：非会员限速 100KB/s，《" + d.name + "》预计 " + F(seconds, "0") + " 秒下完。", "Bodu Cloud: free users are limited to 100KB/s; " + d.nameEn + " needs about " + F(seconds, "0") + " s."));
            DownloadStarted?.Invoke(dataset, seconds);
        }

        /// <summary>摆渡云 super-member acceleration: a tenth of the pack's price, at least ¥50.</summary>
        public double AccelerateCost(string dataset)
        {
            var node = XgCatalog.Node(dataset + ".pack");
            return Math.Max(50, Math.Round((node != null ? node.cost : 500) * .1));
        }

        public bool Accelerate(string dataset, IXgHost host)
        {
            if (!Downloading(dataset)) return false;
            double cost = AccelerateCost(dataset);
            if (!host.Spend(cost)) { Say(T("经费不足 ¥", "Need ¥") + F(cost, "0")); return false; }
            S.totalSpent += cost;
            S.downloads.RemoveAll(d => d.key == dataset);
            Say(T("开通了一天超级会员，下完了。", "Bought a day of super membership; the download finished."));
            return true;
        }

        void TickDownloads(double dt)
        {
            for (int i = S.downloads.Count - 1; i >= 0; i--)
            {
                S.downloads[i].value -= dt;
                if (S.downloads[i].value > 0) continue;
                string key = S.downloads[i].key;
                S.downloads.RemoveAt(i);
                // Keys are dataset ids (public packs) or offer ids (junk / story packs, XgSim.DataSources.cs).
                if (XgCatalog.Dataset(key) != null || DataOfferDef(key) != null) Say(T("下载完成：", "Download finished: ") + DownloadName(key));
            }
        }

        void TickCollection()
        {
            // Phenomena that are moments of the story rather than one evaluation.
            if (WallSeen("translation")) Observe("translation");
            if (WallSeen("parallel")) Observe("serial");
            if (S.abilities) Observe("emergence");
            if (S.stage >= 4 && S.personaSeeded && S.chatTurns >= 3)
                for (int axis = 0; axis < 3; axis++)
                    if (Math.Abs(ActualAxis(axis) - TargetAxis(axis)) >= 25) { Observe("drift"); break; }

            // Self-insight cards and their achievements.
            foreach (var wall in CardWalls)
            {
                if (!S.insights.Contains(wall) || S.cardsShown.Contains(wall)) continue;
                S.cardsShown.Add(wall);
                if (Array.IndexOf(InsightWalls, wall) >= 0) Earn("insight." + wall);
                InsightCard?.Invoke(wall);
            }
            // 近亲繁殖 leaves a card of its own the first time it holds a score down (XgSim.Inbreeding.cs).
            if (PhenomenonSeen(InbreedingId) && !S.cardsShown.Contains(InbreedingId))
            {
                S.cardsShown.Add(InbreedingId);
                InsightCard?.Invoke(InbreedingId);
            }
            if (!HasAchievement("lingguang"))
            {
                bool all = true;
                foreach (var wall in InsightWalls) if (!S.insights.Contains(wall)) { all = false; break; }
                if (all) Earn("lingguang");
            }
        }

        void Earn(string id)
        {
            if (S.achievements.Contains(id)) return;
            var a = Achievement(id);
            if (a == null) return;
            S.achievements.Add(id);
            Say(T("成就：", "Achievement: ") + T(a.name, a.nameEn));
            AchievementEarned?.Invoke(a);
        }

        /// <summary>What a self-insight card shows: the wall, its stage, the golden setting and why it works.</summary>
        public static void InsightCardText(string wall, out int stage, out string name, out string nameEn, out string golden, out string goldenEn, out string why, out string whyEn)
        {
            if (wall == InbreedingId)
            {
                InbreedingCardText(out stage, out name, out nameEn, out golden, out goldenEn, out why, out whyEn);
                return;
            }
            if (wall == "pretrain")
            {
                stage = 6; name = "预训练"; nameEn = "Pre-training";
                golden = "学习率预热 · 跨层直连 · 规模过阈值 · 机房"; goldenEn = "Warm-up · skip connections · scale past the threshold · a server room";
                why = "规模和稳定，两样都要。"; whyEn = "Scale and stability: you need both.";
                return;
            }
            if (wall == EpiphanyCardId)
            {
                stage = 1;
                EpiphanyCardText(out name, out nameEn, out golden, out goldenEn, out why, out whyEn);
                return;
            }
            var w = WallById(wall);
            stage = w != null ? w.stage : 0;
            name = w?.name ?? wall; nameEn = w?.nameEn ?? wall;
            golden = w?.golden ?? ""; goldenEn = w?.goldenEn ?? "";
            why = w?.why ?? ""; whyEn = w?.whyEn ?? "";
        }
    }
}
