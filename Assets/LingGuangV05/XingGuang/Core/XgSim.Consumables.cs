using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>A stack of one consumable: how many are left and the seconds before it can be used again.</summary>
    [Serializable]
    public sealed class XgStack
    {
        public string id = "";
        public int count;
        public double cooldown;
    }

    /// <summary>
    /// What the consumables (消耗品) add to a save. Old saves have none of these fields, so every one starts at zero:
    /// no stock, no cooldown, no effect running.
    /// </summary>
    public sealed partial class XgState
    {
        public List<XgStack> consumables = new List<XgStack>();
        /// <summary>Seconds left of 重涂硅脂 (no card burn-out) and 红牛 (combo holds, bonus +50%).</summary>
        public double pasteSeconds, redbullSeconds;
        /// <summary>Rounds left of 谷电 (half price), checkpoint rollbacks waiting for a drop, rounds left of 学习率重启 (ceiling +8%).</summary>
        public int offpeakRounds, rollbackGuards, sgdrRounds;
    }

    /// <summary>One consumable as the 道具 page sells it and the 训练 page uses it.</summary>
    public sealed class XgConsumableDef
    {
        public string id = "", name = "", nameEn = "", note = "", noteEn = "", history = "", historyEn = "";
        /// <summary>The tile's mark: a Chinese character or two letters (the lab's font has no emoji).</summary>
        public string glyph = "", glyphEn = "";
        /// <summary>The price of one pack, and how many a pack holds.</summary>
        public double price;
        public int pack = 1;
        /// <summary>Seconds before the next one can be used (0 = none).</summary>
        public double cooldown;
    }

    /// <summary>
    /// The household bill for a training slice, when the host can scale it (ChapterOneSim.BillTraining). The host
    /// keeps the money and the card wear; the lab only says how much cheaper and how much safer this slice is.
    /// A host without it bills <see cref="IXgHost.Train"/> as before and the consumables then change nothing there.
    /// </summary>
    public interface IXgTrainingBill
    {
        /// <summary>
        /// Bills the slice: electricity price × <paramref name="priceFactor"/> (谷电), energy used × <paramref name="energyFactor"/>
        /// (cuDNN: fewer kWh, so the month's tier fills slower too), card burn-out chance × <paramref name="wearFactor"/>
        /// (九州风神 halves it, 重涂硅脂 makes it 0).
        /// </summary>
        void TrainScaled(double seconds, double priceFactor, double energyFactor, double wearFactor);
        /// <summary>The cards cool down: the run of non-stop training that wears them starts again.</summary>
        void CoolCards();
    }

    public static class XgConsumables
    {
        public const string Offpeak = "offpeak", Paste = "paste", RedBull = "redbull", Checkpoint = "ckpt", Sgdr = "sgdr", Clean = "clean";

        /// <summary>The six, in quick-bar order (keys 1–6).</summary>
        public static readonly XgConsumableDef[] All =
        {
            new XgConsumableDef { id = Offpeak, glyph = "谷", glyphEn = "OP", price = 30, pack = 3, cooldown = 0,
                name = "谷电时段", nameEn = "Off-peak power",
                note = "接下来 10 轮电费减半（按谷电价算）。", noteEn = "The next 10 rounds cost half the electricity.",
                history = "2016 年各地已有峰谷电价：夜里 23 点到早上 7 点便宜一半。", historyEn = "In 2016 many cities already had peak and valley prices: half price from 23:00 to 07:00." },
            new XgConsumableDef { id = Paste, glyph = "脂", glyphEn = "TP", price = 30, pack = 2, cooldown = 20,
                name = "重涂硅脂", nameEn = "Fresh thermal paste",
                note = "120 秒内显卡不会烧，发热清零。", noteEn = "No card burn-out for 120 s; heat reset.",
                history = "旧硅脂干了，显卡核心温度会高十几度。", historyEn = "Dried-out paste makes a core run a dozen degrees hotter." },
            new XgConsumableDef { id = RedBull, glyph = "牛", glyphEn = "RB", price = 18, pack = 6, cooldown = 15,
                name = "红牛", nameEn = "Red Bull",
                note = "60 秒内连击不掉，连击加成 +50%。", noteEn = "60 s: the combo holds, its bonus is +50%.",
                history = "手按才有连击。是主角喝，不是灵光喝。", historyEn = "Only hands build a combo. You drink it, not 灵光." },
            new XgConsumableDef { id = Checkpoint, glyph = "存", glyphEn = "CP", price = 40, pack = 2, cooldown = 0,
                name = "检查点回滚", nameEn = "Checkpoint rollback",
                note = "下一次「退步」自动撤销，成绩不变。", noteEn = "The next drop is undone by itself.",
                history = "训练前存一份 checkpoint，练坏了就读回去。", historyEn = "Save a checkpoint before training; if it goes wrong, load it back." },
            new XgConsumableDef { id = Sgdr, glyph = "重", glyphEn = "LR", price = 80, pack = 1, cooldown = 60,
                name = "学习率重启", nameEn = "Learning-rate restart",
                note = "涨不动时用：5 轮内上限 +8%。", noteEn = "5 rounds with the ceiling 8% higher.",
                history = "SGDR，2016 年的新论文：把学习率调回去，重新冲一次。", historyEn = "SGDR, a 2016 paper: set the learning rate back up and make another run." },
            new XgConsumableDef { id = Clean, glyph = "洗", glyphEn = "CL", price = 50, pack = 1, cooldown = 30,
                name = "清洗脚本", nameEn = "Cleaning script",
                note = "清掉当前数据集 50 条脏标注。", noteEn = "Removes 50 noisy labels from this dataset.",
                history = "写个脚本，把前后不一致的标注挑出来。", historyEn = "A script that picks out the labels that contradict each other." },
        };

        public static XgConsumableDef Get(string id)
        {
            foreach (var d in All) if (d.id == id) return d;
            return null;
        }
    }

    /// <summary>
    /// Consumables (消耗品): bought in packs on the 道具 page, used on the 训练 page. Every one is a short help
    /// (cheaper electricity, safer cards, a stronger combo, an undone drop, a higher ceiling, cleaner data); none is
    /// needed for any ability, stage or ending. Counts, cooldowns and running effects are in <see cref="XgState"/>.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Rounds one 谷电 covers, its price share, and how long the other effects last.</summary>
        public const int OffpeakRounds = 10, SgdrRounds = 5, CleanRows = 50, MaxStock = 99;
        public const double OffpeakPriceFactor = .5, PasteSeconds = 120, RedBullSeconds = 60, RedBullBonus = 1.5, SgdrBoost = .08;
        /// <summary>cuDNN's share of the electricity (−30%) and 九州风神's share of the burn-out chance (−50%).</summary>
        public const double CuDnnEnergyFactor = .7, CoolerWearFactor = .5;

        /// <summary>A consumable was used (id).</summary>
        public event Action<string> ConsumableUsed;

        XgStack StackOf(string id, bool create)
        {
            if (S.consumables == null) S.consumables = new List<XgStack>();
            foreach (var s in S.consumables) if (s != null && s.id == id) return s;
            if (!create) return null;
            var made = new XgStack { id = id };
            S.consumables.Add(made);
            return made;
        }

        public int ConsumableCount(string id) { var s = StackOf(id, false); return s == null ? 0 : s.count; }
        /// <summary>Seconds before this consumable can be used again.</summary>
        public double ConsumableCooldown(string id) { var s = StackOf(id, false); return s == null ? 0 : s.cooldown; }

        public bool OffpeakActive => S.offpeakRounds > 0;
        public bool PasteActive => S.pasteSeconds > 0;
        public bool RedBullActive => S.redbullSeconds > 0;
        public bool SgdrActive => S.sgdrRounds > 0;

        /// <summary>Share of a round's electricity price that is billed now (谷电: half).</summary>
        public double TrainPriceFactor => S.offpeakRounds > 0 ? OffpeakPriceFactor : 1;
        /// <summary>Share of the energy a round uses (cuDNN: 70%).</summary>
        public double TrainEnergyFactor => Has("cudnn") ? CuDnnEnergyFactor : 1;
        /// <summary>Share of the card burn-out chance that is left (重涂硅脂 none, 九州风神 half).</summary>
        public double TrainWearFactor => S.pasteSeconds > 0 ? 0 : Has("cooler") ? CoolerWearFactor : 1;

        /// <summary>Bills a slice of training through the host (scaled when it can be), and counts the round off 谷电.</summary>
        void ChargeTraining(IXgHost host, double seconds, bool round)
        {
            if (host == null || seconds <= 0) return;
            if (host is IXgTrainingBill bill) bill.TrainScaled(seconds, round ? TrainPriceFactor : 1, TrainEnergyFactor, TrainWearFactor);
            else host.Train(seconds);
            if (round && S.offpeakRounds > 0) S.offpeakRounds--;
        }

        /// <summary>Buys one pack on the 道具 page. False (with a notice) when the money is short or the shelf is full.</summary>
        public bool BuyConsumable(string id, IXgHost host)
        {
            var def = XgConsumables.Get(id);
            if (def == null || host == null) return false;
            var stack = StackOf(id, true);
            if (stack.count + def.pack > MaxStock) { Say(T("装不下了：" + def.name + " 最多存 " + MaxStock + " 个", "No room: at most " + MaxStock + " " + def.nameEn)); return false; }
            if (host.Money + 1e-9 < def.price || !host.Spend(def.price)) { Say(T("经费不足 ¥" + F(def.price, "0"), "Not enough money: ¥" + F(def.price, "0"))); return false; }
            S.totalSpent += def.price;
            stack.count += def.pack;
            Say(T("买了 " + def.pack + " 个" + def.name, "Bought " + def.pack + " × " + def.nameEn));
            return true;
        }

        /// <summary>Why this consumable cannot be used now (null when it can).</summary>
        public string ConsumableBlocker(string id, string dataset = null)
        {
            var def = XgConsumables.Get(id);
            if (def == null) return T("未知道具");
            if (ConsumableCount(id) <= 0) return T("没有了：去道具页买", "None left: buy more on the Items page");
            double wait = ConsumableCooldown(id);
            if (wait > 0) return T("冷却中，还要 " + F(Math.Ceiling(wait), "0") + " 秒", "Cooling down: " + F(Math.Ceiling(wait), "0") + " s");
            if (id == XgConsumables.Clean)
            {
                string d = dataset ?? Selected.dataset;
                if (Noise(d) + DataNoise(d) < 1) return T("这个数据集没有脏标注", "This dataset has no noisy labels");
            }
            return null;
        }

        /// <summary>
        /// Uses one: the effect starts, the stack loses one and the cooldown starts. <paramref name="dataset"/> is the one 清洗脚本
        /// cleans (default: the selected track's). Returns false, with a notice, when it cannot be used.
        /// </summary>
        public bool UseConsumable(string id, IXgHost host, string dataset = null)
        {
            string blocked = ConsumableBlocker(id, dataset);
            if (blocked != null) { Say(blocked); return false; }
            var def = XgConsumables.Get(id);
            string target = dataset ?? Selected.dataset;
            switch (id)
            {
                case XgConsumables.Offpeak: S.offpeakRounds += OffpeakRounds; break;
                case XgConsumables.Paste:
                    S.pasteSeconds = PasteSeconds;
                    if (host is IXgTrainingBill bill) bill.CoolCards();
                    break;
                case XgConsumables.RedBull: S.redbullSeconds = RedBullSeconds; break;
                case XgConsumables.Checkpoint: S.rollbackGuards++; break;
                case XgConsumables.Sgdr: S.sgdrRounds += SgdrRounds; break;
                case XgConsumables.Clean:
                    {
                        double rows = Math.Min(CleanRows, Math.Floor(Noise(target) + DataNoise(target)));
                        double own = Math.Min(rows, Noise(target));
                        SetCount(S.noise, target, Math.Max(0, Noise(target) - own));
                        CleanDataNoise(target, rows - own);
                        break;
                    }
            }
            var stack = StackOf(id, true);
            stack.count--;
            stack.cooldown = def.cooldown;
            foreach (var run in Runs) Evaluate(run);
            ConsumableUsed?.Invoke(id);
            return true;
        }

        /// <summary>Cooldowns and timed effects run down (every sim step).</summary>
        void TickConsumables(double dt)
        {
            if (S.consumables != null)
                foreach (var s in S.consumables) if (s != null && s.cooldown > 0) s.cooldown = Math.Max(0, s.cooldown - dt);
            if (S.pasteSeconds > 0) S.pasteSeconds = Math.Max(0, S.pasteSeconds - dt);
            if (S.redbullSeconds > 0) S.redbullSeconds = Math.Max(0, S.redbullSeconds - dt);
        }

        /// <summary>A save from before the consumables, or a damaged one: unknown or non-finite entries are dropped.</summary>
        void RepairConsumables()
        {
            if (S.consumables == null) S.consumables = new List<XgStack>();
            S.consumables.RemoveAll(s => s == null || XgConsumables.Get(s.id) == null);
            var seen = new HashSet<string>();
            S.consumables.RemoveAll(s => !seen.Add(s.id));
            foreach (var s in S.consumables)
            {
                s.count = Math.Max(0, Math.Min(MaxStock, s.count));
                if (!Finite(s.cooldown) || s.cooldown < 0) s.cooldown = 0;
            }
            if (!Finite(S.pasteSeconds) || S.pasteSeconds < 0) S.pasteSeconds = 0;
            if (!Finite(S.redbullSeconds) || S.redbullSeconds < 0) S.redbullSeconds = 0;
            S.pasteSeconds = Math.Min(S.pasteSeconds, PasteSeconds);
            S.redbullSeconds = Math.Min(S.redbullSeconds, RedBullSeconds);
            S.offpeakRounds = Math.Max(0, Math.Min(MaxStock * OffpeakRounds, S.offpeakRounds));
            S.rollbackGuards = Math.Max(0, Math.Min(MaxStock, S.rollbackGuards));
            S.sgdrRounds = Math.Max(0, Math.Min(MaxStock * SgdrRounds, S.sgdrRounds));
        }
    }
}
