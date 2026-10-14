using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Every relic the player owns (the backpack), the four equipped slots ("" = empty), and the ones ever seen (the collection book).</summary>
        public List<string> relicOwned = new List<string>();
        public List<string> relicSlots = new List<string> { "", "", "", "" };
        public List<string> relicSeen = new List<string>();
        /// <summary>Relics that arrived since the backpack was last opened (the red dot).</summary>
        public List<string> relicFresh = new List<string>();
        /// <summary>The three cards waiting for a choice, and the choices still to come.</summary>
        public List<string> relicPick = new List<string>();
        public int relicPickQueue, relicPicks;
        /// <summary>Drops so far from each rare source, for the per-game caps.</summary>
        public int relicDropMini, relicDropSpider, relicDropRecord;
        /// <summary>Seconds of play and the time of the last drop; rounds charged towards the next free failed round.</summary>
        public double relicClock, relicLastDrop = -1e9;
        public int relicCharge;
        /// <summary>The relics' own random stream, so they never shift the lab's (0 = not started).</summary>
        public long relicRng;
        public int relicSchema;
    }

    /// <summary>
    /// 稀有道具 in the sim: owning and equipping (the backpack and four slots), the sources (a pick of three when 灵光 learns
    /// something, small chances from mini-games, the spider and new records) and the hooks that carry the equipped effects into
    /// the real numbers. Only equipped relics count. The table is in XgRelics.cs.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int RelicPickSize = 3;
        /// <summary>Chances: per correct mini-game answer, per perfect run (every this many right in a row), per word the spider reads, per record at grade A or better.</summary>
        public const double RelicMiniChance = .01, RelicPerfectChance = .05, RelicSpiderChance = .005, RelicRecordChance = .10;
        public const int RelicPerfectRun = 10, RelicRecordGrade = 3;
        /// <summary>Most drops one game takes from each source, and the play seconds between two drops.</summary>
        public const int RelicMiniCap = 3, RelicSpiderCap = 2, RelicRecordCap = 2;
        public const double RelicDropGap = 240;
        /// <summary>Rarity weights of the three cards, and of a drop (no legendary).</summary>
        static readonly double[] RelicPickWeights = { 60, 32, 8 }, RelicDropWeights = { 75, 25, 0 };

        /// <summary>The caps and the gap apply (tests turn them off to count raw chances).</summary>
        public bool RelicLimitsOn = true;

        /// <summary>A relic arrived: it, where it came from ("pick", "mini", "spider", "record"), and whether it was equipped at once.</summary>
        public event Action<XgRelic, string, bool> RelicGained;
        /// <summary>The three cards are ready.</summary>
        public event Action RelicPickOffered;

        XgRelicFx relicFx;
        readonly string[] relicFxKey = new string[XgRelics.Slots];
        long relicRunTicket;
        int relicRunRight;
        bool relicRunClean = true;

        // ───────────── what is held ─────────────

        public IReadOnlyList<string> RelicsOwned => S.relicOwned;
        public bool RelicOwned(string id) => S.relicOwned.Contains(id);
        public bool RelicEquipped(string id) => !string.IsNullOrEmpty(id) && S.relicSlots.Contains(id);
        public bool RelicSeen(string id) => S.relicSeen.Contains(id);
        public XgRelic RelicInSlot(int slot) => slot < 0 || slot >= S.relicSlots.Count ? null : XgRelics.Get(S.relicSlots[slot]);
        public int RelicsEquippedCount { get { int n = 0; foreach (var id in S.relicSlots) if (!string.IsNullOrEmpty(id)) n++; return n; } }
        public int RelicFreeSlot { get { for (int i = 0; i < XgRelics.Slots; i++) if (string.IsNullOrEmpty(S.relicSlots[i])) return i; return -1; } }
        /// <summary>The backpack has relics the player has not looked at yet.</summary>
        public bool RelicsFresh => S.relicFresh.Count > 0;
        public bool RelicFresh(string id) => S.relicFresh.Contains(id);
        public void MarkRelicsViewed() { S.relicFresh.Clear(); }

        /// <summary>What the equipped relics add up to. Rebuilt when the slots change.</summary>
        public XgRelicFx RelicFx
        {
            get
            {
                bool same = relicFx != null;
                for (int i = 0; i < XgRelics.Slots; i++)
                {
                    string id = i < S.relicSlots.Count ? S.relicSlots[i] : "";
                    if (relicFxKey[i] != id) { relicFxKey[i] = id; same = false; }
                }
                if (!same) relicFx = XgRelics.Build(S.relicSlots);
                return relicFx;
            }
        }

        /// <summary>
        /// Equips an owned relic. With <paramref name="slot"/> -1 it takes the first free slot (false if all four are full);
        /// with a slot it goes there and whatever was there goes back to the backpack. An equipped relic moves.
        /// </summary>
        public bool EquipRelic(string id, int slot = -1)
        {
            if (!RelicOwned(id) || slot < -1 || slot >= XgRelics.Slots) return false;
            int at = S.relicSlots.IndexOf(id);
            if (slot == -1)
            {
                if (at >= 0) return true;
                slot = RelicFreeSlot;
                if (slot < 0) return false;
            }
            if (at == slot) return true;
            if (at >= 0) S.relicSlots[at] = "";
            S.relicSlots[slot] = id;
            AfterRelicChange();
            return true;
        }

        /// <summary>Takes a relic off; it stays in the backpack.</summary>
        public bool UnequipRelic(string id)
        {
            int at = string.IsNullOrEmpty(id) ? -1 : S.relicSlots.IndexOf(id);
            if (at < 0) return false;
            S.relicSlots[at] = "";
            AfterRelicChange();
            return true;
        }

        public bool UnequipSlot(int slot) => slot >= 0 && slot < XgRelics.Slots && UnequipRelic(S.relicSlots[slot]);

        void AfterRelicChange()
        {
            // Memory and VRAM follow the slots: the handwriting recogniser is rebuilt and the models are sized again.
            handwritingBuiltFrom = -1;
            foreach (var run in Runs) Evaluate(run);
        }

        // ───────────── getting relics ─────────────

        /// <summary>Puts a relic in the backpack (once per relic). Equipped at once when a slot is free.</summary>
        public bool GrantRelic(string id, string source = "pick")
        {
            var r = XgRelics.Get(id);
            if (r == null || RelicOwned(id)) return false;
            S.relicOwned.Add(id);
            if (!S.relicSeen.Contains(id)) S.relicSeen.Add(id);
            if (!S.relicFresh.Contains(id)) S.relicFresh.Add(id);
            bool equipped = false;
            int free = RelicFreeSlot;
            if (free >= 0) { S.relicSlots[free] = id; equipped = true; AfterRelicChange(); }
            RelicGained?.Invoke(r, source, equipped);
            return true;
        }

        /// <summary>Counts a pick that 灵光 earned by learning something. The three cards appear at once if no choice is waiting.</summary>
        public void OfferRelicPick()
        {
            S.relicPickQueue++;
            S.relicPicks++;
            DealRelicPick();
        }

        /// <summary>The three cards for a choice, or empty when none is waiting.</summary>
        public IReadOnlyList<string> RelicPick => S.relicPick;
        public bool RelicPickPending => S.relicPick.Count > 0;

        void DealRelicPick()
        {
            if (S.relicPick.Count > 0 || S.relicPickQueue <= 0) return;
            var pool = RelicPool(true);
            if (pool.Count == 0) { S.relicPickQueue = 0; return; }
            S.relicPickQueue--;
            for (int n = 0; n < RelicPickSize; n++)
            {
                var r = DrawRelic(pool, RelicPickWeights);
                if (r == null) break;
                S.relicPick.Add(r.id);
                pool.Remove(r);
            }
            RelicPickOffered?.Invoke();
        }

        /// <summary>Takes one of the three cards into the backpack; the other two are gone for this choice.</summary>
        public bool PickRelic(string id)
        {
            if (!S.relicPick.Contains(id)) return false;
            S.relicPick.Clear();
            GrantRelic(id, "pick");
            DealRelicPick();
            return true;
        }

        /// <summary>Relics not owned yet (without the legendary ones when a drop asks).</summary>
        List<XgRelic> RelicPool(bool legendary)
        {
            var list = new List<XgRelic>();
            foreach (var r in XgRelics.All)
                if (!RelicOwned(r.id) && (legendary || r.rarity != XgRarity.Legendary)) list.Add(r);
            return list;
        }

        XgRelic DrawRelic(List<XgRelic> pool, double[] weights)
        {
            double total = 0;
            for (int r = 0; r < 3; r++) if (HasRarity(pool, (XgRarity)r)) total += weights[r];
            if (total <= 0) return null;
            double x = RelicRoll() * total;
            XgRarity pick = XgRarity.Rare;
            for (int r = 0; r < 3; r++)
            {
                if (!HasRarity(pool, (XgRarity)r) || weights[r] <= 0) continue;
                pick = (XgRarity)r;
                if (x < weights[r]) break;
                x -= weights[r];
            }
            var ofRarity = new List<XgRelic>();
            foreach (var c in pool) if (c.rarity == pick) ofRarity.Add(c);
            return ofRarity.Count == 0 ? null : ofRarity[Math.Min(ofRarity.Count - 1, (int)(RelicRoll() * ofRarity.Count))];
        }

        static bool HasRarity(List<XgRelic> pool, XgRarity r) { foreach (var c in pool) if (c.rarity == r) return true; return false; }

        /// <summary>The relics' own stream (xorshift), started from the lab's seed the first time it is needed.</summary>
        double RelicRoll()
        {
            long x = S.relicRng;
            if (x == 0) x = S.rng ^ 0x2545F4914F6CDD1DL;
            if (x == 0) x = 0x5EED5EEDL;
            x ^= x << 13; x ^= (long)((ulong)x >> 7); x ^= x << 17;
            S.relicRng = x == 0 ? 0x5EED5EEDL : x;
            return (double)((ulong)S.relicRng >> 11) / (1UL << 53);
        }

        /// <summary>
        /// One roll for a rare drop from <paramref name="source"/> ("mini", "spider", "record"). A drop never comes before the
        /// first choice of three, comes at most a few times a game and never twice within <see cref="RelicDropGap"/> seconds;
        /// it is never legendary. Returns whether a relic arrived.
        /// </summary>
        public bool TryRelicDrop(string source, double chance)
        {
            if (S.relicPicks <= 0) return false;
            if (RelicLimitsOn)
            {
                if (S.relicClock - S.relicLastDrop < RelicDropGap) return false;
                if (source == "mini" && S.relicDropMini >= RelicMiniCap) return false;
                if (source == "spider" && S.relicDropSpider >= RelicSpiderCap) return false;
                if (source == "record" && S.relicDropRecord >= RelicRecordCap) return false;
            }
            var pool = RelicPool(false);
            if (pool.Count == 0) return false;
            if (RelicRoll() >= chance) return false;
            var r = DrawRelic(pool, RelicDropWeights);
            if (r == null) return false;
            if (source == "mini") S.relicDropMini++; else if (source == "spider") S.relicDropSpider++; else if (source == "record") S.relicDropRecord++;
            S.relicLastDrop = S.relicClock;
            return GrantRelic(r.id, source);
        }

        // ───────────── hooks ─────────────

        /// <summary>
        /// A desk game answered one item (XgSim.MiniPay.cs): a correct answer rolls the 1% drop, a clean run of
        /// <see cref="RelicPerfectRun"/> right answers in one game rolls the 5% one, and 一袋街机游戏币 adds its extra samples.
        /// Returns the samples the relics added to this answer, for the result.
        /// </summary>
        int RelicOnMiniAction(string desk, bool correct)
        {
            if (miniTicket != relicRunTicket) { relicRunTicket = miniTicket; relicRunRight = 0; relicRunClean = true; }
            if (!correct) { relicRunClean = false; return 0; }
            var fx = RelicFx;
            int label = (int)Math.Round(fx.extraSamples);
            int extra = label;
            for (int i = 0; i < fx.miniExtra; i++) { AddLabel(desk); extra += 1 + label; }
            TryRelicDrop("mini", RelicMiniChance);
            if (miniTicket != 0 && relicRunClean && ++relicRunRight % RelicPerfectRun == 0) TryRelicDrop("mini", RelicPerfectChance);
            return extra;
        }

        /// <summary>The extra samples a hand label brings now (for the number the answer reports).</summary>
        int RelicLabelExtra() => (int)Math.Round(RelicFx.extraSamples);

        /// <summary>A label was added to <paramref name="datasetId"/>: the extra rows of 老周的 U 盘 and its kin, with their noise.</summary>
        void RelicLabelBonus(string datasetId)
        {
            var fx = RelicFx;
            if (fx.extraSamples <= 0) return;
            SetCount(S.dataExtra, datasetId, ExtraSamples(datasetId) + fx.extraSamples);
            if (fx.noisePerLabel > 0) SetCount(S.crowdNoise, datasetId, Count(S.crowdNoise, datasetId) + fx.noisePerLabel);
        }

        /// <summary>Training gain per round: the relics add to it (clamped to the whole way).</summary>
        double RelicGainFactor(XgRun run)
        {
            var fx = RelicFx;
            double bonus = fx.gain + (run.track == (int)XgTrack.Vision ? fx.gainVision : fx.gainSequence);
            return 1 + Math.Max(-.9, bonus);
        }

        /// <summary>The rate at which the next failed round is excused, once its charge is full.</summary>
        public bool RelicGuardReady { get { int every = RelicFx.guardEvery; return every > 0 && S.relicCharge >= every; } }

        public void RelicRoundDone()
        {
            int every = RelicFx.guardEvery;
            if (every > 0 && S.relicCharge < every) S.relicCharge++;
        }

        void RelicTick(double dt) { S.relicClock += dt; }

        /// <summary>Multiplier on the model-training VRAM of every card.</summary>
        double RelicVramFactor => 1 + RelicFx.vramBonus;

        /// <summary>Multiplier on the training power bill.</summary>
        double RelicPowerFactor { get { var fx = RelicFx; return Math.Max(.1, (1 - Math.Min(.9, fx.powerCut)) * (1 + fx.powerRise)); } }

        /// <summary>Share of the rent still paid (read by ChapterOneSim through <c>RentFactor</c>).</summary>
        public double RelicRentFactor => Math.Max(0, 1 - RelicFx.rentCut);

        /// <summary>Handwriting drawings the recogniser keeps.</summary>
        public int HandwritingCapacity => (int)Math.Round(XgMiniActionMemory.Capacity * RelicFx.handwritingMult);

        /// <summary>1 + the relics' share of contract and auto-answer income.</summary>
        double RelicIncomeFactor => 1 + RelicFx.incomeBonus;

        // ───────────── saves ─────────────

        void RepairRelics()
        {
            if (S.relicOwned == null) S.relicOwned = new List<string>();
            if (S.relicSeen == null) S.relicSeen = new List<string>();
            if (S.relicFresh == null) S.relicFresh = new List<string>();
            if (S.relicPick == null) S.relicPick = new List<string>();
            if (S.relicSlots == null) S.relicSlots = new List<string>();
            var seen = new HashSet<string>();
            S.relicOwned.RemoveAll(id => XgRelics.Get(id) == null || !seen.Add(id));
            seen.Clear();
            S.relicSeen.RemoveAll(id => XgRelics.Get(id) == null || !seen.Add(id));
            foreach (var id in S.relicOwned) if (!S.relicSeen.Contains(id)) S.relicSeen.Add(id);
            S.relicFresh.RemoveAll(id => !S.relicOwned.Contains(id));
            while (S.relicSlots.Count < XgRelics.Slots) S.relicSlots.Add("");
            if (S.relicSlots.Count > XgRelics.Slots) S.relicSlots.RemoveRange(XgRelics.Slots, S.relicSlots.Count - XgRelics.Slots);
            seen.Clear();
            for (int i = 0; i < S.relicSlots.Count; i++)
                if (string.IsNullOrEmpty(S.relicSlots[i]) || !S.relicOwned.Contains(S.relicSlots[i]) || !seen.Add(S.relicSlots[i])) S.relicSlots[i] = "";
            seen.Clear();
            S.relicPick.RemoveAll(id => XgRelics.Get(id) == null || S.relicOwned.Contains(id) || !seen.Add(id));
            if (S.relicPick.Count > RelicPickSize) S.relicPick.RemoveRange(RelicPickSize, S.relicPick.Count - RelicPickSize);
            S.relicPickQueue = Math.Max(0, Math.Min(12, S.relicPickQueue));
            S.relicPicks = Math.Max(0, Math.Min(99, S.relicPicks));
            S.relicDropMini = Math.Max(0, S.relicDropMini); S.relicDropSpider = Math.Max(0, S.relicDropSpider); S.relicDropRecord = Math.Max(0, S.relicDropRecord);
            if (!Finite(S.relicClock) || S.relicClock < 0) S.relicClock = 0;
            if (!Finite(S.relicLastDrop)) S.relicLastDrop = -1e9;
            S.relicCharge = Math.Max(0, Math.Min(99, S.relicCharge));
            // A save from before the relics that already has abilities: each of them earns its choice of three (dealt one at a time).
            if (S.relicSchema < 1)
            {
                S.relicSchema = 1;
                if (S.abilitiesEmerged != null && S.abilitiesEmerged.Count > 1 && !S.openingFocus)
                {
                    int owed = Math.Min(AbilityCount, S.abilitiesEmerged.Count);
                    S.relicPickQueue += owed; S.relicPicks += owed;
                }
            }
            relicFx = null;
            DealRelicPick();
        }
    }
}
