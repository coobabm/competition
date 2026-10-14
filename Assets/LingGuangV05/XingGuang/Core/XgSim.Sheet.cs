using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One question on the training page's sheet (答卷): a card, its fixed difficulty, and what it answered last.</summary>
    [Serializable]
    public sealed class XgSheetItem
    {
        public string dataset = "";
        /// <summary>A card the player labelled by hand (<see cref="hand"/>), kept whole; otherwise empty and the card is made from <see cref="seed"/>.</summary>
        public XgCard card = new XgCard();
        public bool hand;
        public int seed, level, today;
        /// <summary>Difficulty in mastery units (0–1): it answers right once the run's mastery passes it.</summary>
        public double u;
        /// <summary>The card the current limit holds back (above the ceiling until the limit is fixed).</summary>
        public bool stuck;
        /// <summary>The label is wrong (the data is dirty): its answer is right and the label says otherwise.</summary>
        public bool dirty;
        /// <summary>What it answered after the last round (right or not), so a change can be shown as a flip.</summary>
        public bool right;
        /// <summary>The kind of mistake, shown while it answers wrong (「把 7 认成 1」); empty when the kind says nothing new.</summary>
        public string kind = "", kindEn = "";
        /// <summary>+1 it turned right this round, −1 it turned wrong, 0 no change (not saved).</summary>
        [NonSerialized] public int flip;
        [NonSerialized] internal XgBoardCard board;
    }

    public sealed partial class XgState
    {
        /// <summary>The last cards labelled by hand, a few per dataset: the sheet asks these first.</summary>
        public List<XgCard> sheetHand = new List<XgCard>();
    }

    public sealed partial class XgRun
    {
        /// <summary>The 答卷 of this run (<see cref="XgSim.Sheet"/>), what it was made for, and how many sheets came before.</summary>
        public List<XgSheetItem> sheet = new List<XgSheetItem>();
        public string sheetKey = "";
        public int sheetCount;
        /// <summary>Every question was right after the last round: the next round brings a new sheet.</summary>
        public bool sheetDone;
        /// <summary>The model's shape when the sheet was made, and whether any of its questions has flipped since.</summary>
        public string sheetShape = "";
        public bool sheetTouched;
        /// <summary>The sheet was replaced by the last round or a change of desk (not saved).</summary>
        [NonSerialized] public bool sheetFresh;
    }

    public sealed partial class XgCard
    {
        internal XgCard CopyForSheet() => (XgCard)MemberwiseClone();
    }

    /// <summary>
    /// 答卷 (训练页_答卷诊断能力考_设计.md §一): a few questions kept on the training page for many rounds, so the player
    /// sees the same question turn from ✗ to ✓. The score is the curve's, not a separate exam: every question has a
    /// fixed difficulty u and is answered right while the run's mastery (its accuracy between chance and the desk's
    /// floor) is above u, so answers move with the curve, both ways. A new sheet holds one question it already knows,
    /// two it is about to learn, and one the current limit (<see cref="CeilingLimit"/>) holds above its ceiling.
    /// Nothing here feeds back into training, money or progress.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int SheetSize = 4;
        /// <summary>Hand-labelled cards kept per dataset for the sheet.</summary>
        public const int SheetHandKept = 8;
        /// <summary>Above this flip rate a dirty label is shown as one (标签脏了).</summary>
        public const double SheetDirtyRate = .01;
        /// <summary>Within this much mastery of its ceiling the run has no question left to learn soon.</summary>
        public const double SheetNearRoom = .02;
        /// <summary>A stuck question needs room above the ceiling.</summary>
        public const double SheetStuckRoom = .03;

        /// <summary>Remembers a hand-labelled card (right answer, real data) for the sheet.</summary>
        void RememberSheetCard(XgCard card)
        {
            if (card == null || card.family || card.law || card.kind == "shutdown" || XgCatalog.Dataset(card.dataset) == null) return;
            if (S.sheetHand == null) S.sheetHand = new List<XgCard>();
            S.sheetHand.Add(card.CopyForSheet());
            int n = 0;
            for (int i = S.sheetHand.Count - 1; i >= 0; i--)
                if (S.sheetHand[i].dataset == card.dataset && ++n > SheetHandKept) S.sheetHand.RemoveAt(i);
        }

        /// <summary>How much of the desk the run has learned: 0 at chance, 1 at the desk's floor error.</summary>
        public double SheetMastery(XgRun run) => MasteryOf(run, run.valAcc);

        /// <summary>The mastery the run's current model can reach (its ceiling).</summary>
        public double SheetCeiling(XgRun run) => MasteryOf(run, CeilingAcc(run));

        static double MasteryOf(XgRun run, double acc)
        {
            var d = XgCatalog.Dataset(run?.dataset);
            if (d == null) return 0;
            double chance = 1 - d.chanceError, top = 1 - d.floorError;
            return top <= chance ? 0 : Math.Max(0, Math.Min(1, (acc - chance) / (top - chance)));
        }

        string SheetKeyOf(XgRun run) => run.dataset + "|" + S.stage;

        /// <summary>The run's sheet, made anew when the desk or the chapter changed (no flips then).</summary>
        public List<XgSheetItem> Sheet(XgRun run)
        {
            if (run == null) return new List<XgSheetItem>();
            if (run.sheet == null) run.sheet = new List<XgSheetItem>();
            if (run.sheet.Count == 0 || run.sheetKey != SheetKeyOf(run)) NewSheet(run);
            return run.sheet;
        }

        /// <summary>
        /// After a round: a finished sheet is replaced, then every question answers again and remembers whether it
        /// flipped. Returns the sheet.
        /// </summary>
        public List<XgSheetItem> SheetAfterRound(XgRun run)
        {
            if (run == null) return new List<XgSheetItem>();
            run.sheetFresh = false;
            if (run.sheetDone) NewSheet(run);
            // A sheet made before the lab configured the model (or grew it) was placed on the old ceiling: while none
            // of its questions has moved yet, it is placed again on this model. After that a bigger model is news,
            // and the held-back question may flip.
            else if (!run.sheetTouched && run.sheet != null && run.sheet.Count > 0 && run.sheetShape != ShapeKey(run)) NewSheet(run, false);
            var sheet = Sheet(run);
            bool all = sheet.Count > 0;
            foreach (var item in sheet)
            {
                bool now = SheetRight(run, item);
                item.flip = now == item.right ? 0 : now ? 1 : -1;
                item.right = now;
                if (item.flip != 0) run.sheetTouched = true;
                all &= now;
            }
            run.sheetDone = all;
            return sheet;
        }

        /// <summary>Whether it answers the question right now. A dirty card stays a question mark while the data is dirty.</summary>
        public bool SheetRight(XgRun run, XgSheetItem item)
        {
            if (item.dirty)
            {
                if (BoardFlipRate(item.dataset) > SheetDirtyRate) return false;
                // Cleaned: the label is fixed and the answer it gave all along is right.
                item.dirty = false; item.u = -1;
            }
            return item.u < SheetMastery(run);
        }

        /// <summary>A dirty card shown as dirty (its answer right, the label wrong).</summary>
        public bool SheetDirty(XgSheetItem item) => item.dirty && BoardFlipRate(item.dataset) > SheetDirtyRate;

        static string ShapeKey(XgRun run) => run.arch + "|" + run.depth + "|" + run.width;

        /// <summary>A new sheet; <paramref name="counted"/> false re-places the current one quietly (same count, no "new sheet").</summary>
        void NewSheet(XgRun run, bool counted = true)
        {
            var d = XgCatalog.Dataset(run.dataset);
            run.sheet = run.sheet ?? new List<XgSheetItem>();
            run.sheet.Clear();
            run.sheetKey = SheetKeyOf(run);
            run.sheetShape = ShapeKey(run);
            run.sheetTouched = false;
            run.sheetDone = false;
            if (counted) { run.sheetFresh = true; run.sheetCount++; }
            if (d == null) return;
            var rng = new Random(StableHash(run.dataset) ^ S.dataSalt * 7919 ^ run.sheetCount * 104729);
            double p = SheetMastery(run), c = Math.Max(p, SheetCeiling(run));
            var limit = CeilingLimit(run);
            bool hasStuck = c < 1 - SheetStuckRoom;
            bool nearRoom = c - p >= SheetNearRoom;
            var used = new HashSet<string>();
            var hand = new List<XgCard>();
            if (S.sheetHand != null)
                foreach (var h in S.sheetHand) if (h.dataset == run.dataset) hand.Add(h);
            int level = BoardLevel(run.dataset), today = XgBoardData.Topical(run.dataset) ? Today : 0;

            for (int slot = 0; slot < SheetSize; slot++)
            {
                // Slot 0: known; 1–2: about to be learned; 3: held back by the limit.
                bool stuck = slot == SheetSize - 1 && hasStuck;
                bool known = slot == 0 || !stuck && !nearRoom;
                var item = stuck ? StuckItem(run, limit, rng, used, level, today) : null;
                if (item == null) item = TakeHand(hand, rng, used) ?? MadeItem(run.dataset, rng, used, level, today, null);
                if (item == null) continue;
                double r = .15 + .7 * rng.NextDouble();
                // Just above the ceiling: fixing the limit (a bigger model, the next structure) brings it within reach soon.
                if (stuck) item.u = c + (1 - c) * (.1 + .3 * rng.NextDouble());
                else if (known && p > 0) item.u = p * r;
                else item.u = p + Math.Max(SheetNearRoom, c - p) * (.1 + .5 * rng.NextDouble());
                item.stuck = stuck;
                if (stuck && limit == XgLimit.Noise && BoardFlipRate(run.dataset) > SheetDirtyRate) item.dirty = true;
                SetKind(item, run, stuck ? limit : XgLimit.None);
                item.right = SheetRight(run, item);
                run.sheet.Add(item);
            }
            // Shuffled, so the known question is not always on top.
            for (int i = run.sheet.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = run.sheet[i]; run.sheet[i] = run.sheet[j]; run.sheet[j] = t; }
        }

        XgSheetItem TakeHand(List<XgCard> hand, Random rng, HashSet<string> used)
        {
            while (hand.Count > 0)
            {
                int i = rng.Next(hand.Count);
                var c = hand[i]; hand.RemoveAt(i);
                var item = new XgSheetItem { dataset = c.dataset, card = c.CopyForSheet(), hand = true, seed = c.seed, level = c.level };
                if (Usable(item, used)) return item;
            }
            return null;
        }

        XgSheetItem MadeItem(string dataset, Random rng, HashSet<string> used, int level, int today, Func<XgSheetItem, bool> want)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                var item = new XgSheetItem { dataset = dataset, seed = rng.Next(1, int.MaxValue), level = level, today = today };
                if (want != null && !want(item)) continue;
                if (Usable(item, used)) return item;
            }
            return want == null ? null : MadeItem(dataset, rng, used, level, today, null);
        }

        /// <summary>The held-back question: one whose mistake shows the limit (the far clue, the front of the sentence…).</summary>
        XgSheetItem StuckItem(XgRun run, XgLimit limit, Random rng, HashSet<string> used, int level, int today)
        {
            string want = limit != XgLimit.Structure ? null : run.dataset == "translate" ? "front"
                : run.dataset == "longtext" || run.dataset == "crosssentence" ? "far" : run.dataset == "parallel" ? "decoy" : null;
            if (want == null) return MadeItem(run.dataset, rng, used, level, today, null);
            return MadeItem(run.dataset, rng, used, level, today, it => { SetKind(it, run, XgLimit.None); return KindKey(it, run) == want; });
        }

        bool Usable(XgSheetItem item, HashSet<string> used)
        {
            var b = SheetCard(item);
            if (b == null || string.IsNullOrEmpty(CardText(b, item.dataset))) return false;
            return used.Add(XgBoardData.Signature(b));
        }

        /// <summary>The card as the brain sees it (made again after loading).</summary>
        public XgBoardCard SheetCard(XgSheetItem item)
        {
            if (item.board != null) return item.board;
            try { item.board = item.hand ? XgBoardData.FromCard(item.card) : XgBoardData.Make(item.dataset, item.seed, item.level, item.today); }
            catch (Exception) { item.board = null; }
            return item.board;
        }

        string KindKey(XgSheetItem item, XgRun run) => Mistake(item, run, XgLimit.None)?.key ?? "";

        XgExamMistake Mistake(XgSheetItem item, XgRun run, XgLimit limit)
        {
            var b = SheetCard(item);
            if (b == null) return null;
            // Data short, or an MLP that binds every pixel to its place: the card looks new to it.
            bool guessed = limit == XgLimit.Data || limit == XgLimit.Structure && b.region == "vision";
            var m = new XgExamMistake { card = b, dataset = item.dataset, answer = !b.truth, truth = b.truth, guessed = guessed };
            Classify(m);
            return m;
        }

        void SetKind(XgSheetItem item, XgRun run, XgLimit limit)
        {
            var m = Mistake(item, run, limit);
            item.kind = item.kindEn = "";
            if (m == null) return;
            // 「该答是的答成了否」 says what the answer already says.
            if (m.key == "missyes" || m.key == "falseyes" || m.key == "other") return;
            item.kind = m.kind; item.kindEn = m.kindEn;
        }

        // ───────────── text ─────────────

        public string SheetQuestion(XgSheetItem item)
        {
            var b = SheetCard(item);
            return b == null ? "" : CardText(b, item.dataset);
        }

        /// <summary>What it answers: the right answer while it knows the card (or the label is dirty), the other one before.</summary>
        public string SheetAnswer(XgRun run, XgSheetItem item)
        {
            var b = SheetCard(item);
            if (b == null) return "";
            bool said = item.right || SheetDirty(item) ? b.truth : !b.truth;
            return said ? T("是", "yes") : T("否", "no");
        }

        /// <summary>The label a dirty card carries (the wrong one).</summary>
        public string SheetLabel(XgSheetItem item)
        {
            var b = SheetCard(item);
            return b == null ? "" : !b.truth ? T("是", "yes") : T("否", "no");
        }

        public string SheetKind(XgSheetItem item) => English ? item.kindEn : item.kind;

        static int StableHash(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                foreach (char ch in s ?? "") h = (h ^ ch) * 16777619;
                return h & 0x7fffffff;
            }
        }
    }
}
