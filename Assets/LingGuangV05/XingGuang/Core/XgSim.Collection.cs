using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Achievements earned (ids from <see cref="XgSim.Achievements"/>).</summary>
        public List<string> achievements = new List<string>();
        /// <summary>Self-insight cards already revealed to the player ("pretrain"; older saves also hold wall ids).</summary>
        public List<string> cardsShown = new List<string>();
        /// <summary>Big data packs still downloading: dataset id → seconds left (摆渡云, design v1.1 §3).</summary>
        public List<XgScore> downloads = new List<XgScore>();
    }

    /// <summary>
    /// An achievement, and the holographic card it gives (成就 page, XgSim.Cards.cs). Category groups the album;
    /// rarity sets the finish (0 普通 silver, 1 稀有 gold, 2 史诗 holographic, 3 传说 cosmos, 4 隐藏传说 lenticular); hidden cards show "？？？" until earned.
    /// </summary>
    public sealed class XgAchievement
    {
        public string id, name, nameEn, note, noteEn;
        public bool hidden;
        public string category = XgSim.CardInsight, glyph = "";
        public int rarity = 2;
        /// <summary>The back of the card: why it matters, in a sentence or two.</summary>
        public string flavor = "", flavorEn = "";
        /// <summary>Hidden legends are lenticular: tilting the card flips the subject to this second glyph.</summary>
        public string glyph2 = "";
        /// <summary>For hidden cards: the rumour the album gives instead of the condition.</summary>
        public string hint = "", hintEn = "";
    }

    /// <summary>
    /// The collection side of the lab (design v1.1 §5.3, §11.2): phenomena that show up through the story rather
    /// than one evaluation, the ability nodes, self-insight cards and achievements.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Things worked out alone that flip in a self-insight card (pre-training without the secret).</summary>
        public static readonly string[] InsightCards = { "pretrain" };
        /// <summary>Prefix of an ability that emerged with none of its architecture discounts ("ability.3"), kept in <see cref="XgState.insights"/>.</summary>
        public const string ScaleInsight = "ability.";

        /// <summary>The whole album (XgSim.Cards.cs builds it: abilities, insights, roads, cures, phenomena, data, fun, endings).</summary>
        public static XgAchievement[] Achievements => album ?? (album = BuildAlbum());
        static XgAchievement[] album;

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
            if (n.kind == XgNodeKind.Ability) return HasAbility(n.value);
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
            if (!host.Spend(cost)) { Say(T("经费不足 ¥") + F(cost, "0")); return false; }
            S.totalSpent += cost;
            S.downloads.RemoveAll(d => d.key == dataset);
            Say(T("开通了一天超级会员，下完了。"));
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
                if (XgCatalog.Dataset(key) != null || DataOfferDef(key) != null) Say(T("下载完成：") + DownloadName(key));
            }
        }

        void TickCollection()
        {
            TickCards();
            // Phenomena that are moments of the story rather than one evaluation.
            // The bottlenecks the abilities grew past: a sequence that cannot reach the other sentence, a loop that reads one word at a time.
            if (HasAbility(5)) Observe("translation");
            if (HasEmerged(5)) Observe("serial");
            if (S.abilities) Observe("emergence");
            if (S.stage >= 4 && S.personaSeeded && S.chatTurns >= 3)
                for (int axis = 0; axis < 3; axis++)
                    if (Math.Abs(ActualAxis(axis) - TargetAxis(axis)) >= 25) { Observe("drift"); break; }

            // Self-insight cards and their achievements.
            foreach (var id in InsightCards)
            {
                if (!S.insights.Contains(id) || S.cardsShown.Contains(id)) continue;
                S.cardsShown.Add(id);
                Earn("insight." + id);
                InsightCard?.Invoke(id);
            }
            // 近亲繁殖 leaves a card of its own the first time it holds a score down (XgSim.Inbreeding.cs).
            if (PhenomenonSeen(InbreedingId) && !S.cardsShown.Contains(InbreedingId))
            {
                S.cardsShown.Add(InbreedingId);
                InsightCard?.Invoke(InbreedingId);
            }
            if (!HasAchievement("lingguang"))
            {
                bool all = S.insights.Contains("pretrain");
                for (int i = 2; i <= AbilityCount && all; i++) if (!S.insights.Contains(ScaleInsight + i)) all = false;
                if (all) Earn("lingguang");
            }
        }

        void Earn(string id)
        {
            if (S.achievements.Contains(id)) return;
            var a = Achievement(id);
            if (a == null) return;
            S.achievements.Add(id);
            Say(T("获得卡片：") + T(a.name, a.nameEn) + T("（" + RarityName(a.rarity, false) + "）", " (" + RarityName(a.rarity, true) + ")"));
            AchievementEarned?.Invoke(a);
        }

        /// <summary>What a self-insight card shows: what was worked out, its stage, the setting and why it works.</summary>
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
            // Wall ids of older saves have no card any more.
            stage = 0; name = wall; nameEn = wall; golden = goldenEn = why = whyEn = "";
        }
    }
}
