using System;
using System.Collections.Generic;
using LingGuangV05.Core.Hardware;

namespace LingGuangV05.Core
{
    /// <summary>
    /// Card models (design v1.1 §3, §14.3). Every owned card has a model in <see cref="GameState.gpuModels"/>
    /// (kept in step with <see cref="GameState.gpuCount"/>): compute is the sum of the cards' multipliers,
    /// VRAM and watts are the sums of theirs. 淘货 sells new cards on the calendar; 喵鱼 buys old ones back and
    /// sells retired 网吧 machines (a case with a GTX 970). A save from before the models counts each of its cards
    /// as the starting GTX 980 Ti, so it keeps the compute it had.
    /// </summary>
    public sealed partial class ChapterOneSim
    {
        /// <summary>Model ids of the owned cards, in the order they were bought.</summary>
        public IReadOnlyList<string> Cards { get { return S.gpuModels; } }
        public int Slots { get { return S.caseCount * Config.gpusPerCase; } }
        public bool SlotFree { get { return S.gpuCount < Slots; } }

        /// <summary>Σ card multipliers (×1 = a GTX 980 Ti), with the NVMe drive's boost.</summary>
        public double CardCompute
        {
            get
            {
                double sum = 0;
                foreach (string id in S.gpuModels) { var g = HardwareCatalog.Gpu(id); if (g != null) sum += g.compute; }
                return sum * (S.nvme ? HardwareCatalog.NvmeBoost : 1);
            }
        }

        /// <summary>
        /// The biggest single card's memory: training copies the whole model onto every card (data parallel, as in
        /// 2016), so cards add speed, not room for a bigger model.
        /// </summary>
        public double LargestCardVram
        {
            get { double max = 0; foreach (string id in S.gpuModels) { var g = HardwareCatalog.Gpu(id); if (g != null) max = Math.Max(max, g.vramMB); } return max; }
        }

        public double CardVram
        {
            get { double sum = 0; foreach (string id in S.gpuModels) { var g = HardwareCatalog.Gpu(id); if (g != null) sum += g.vramMB; } return sum; }
        }

        /// <summary>Watts the cards (and the NVMe drive) draw at full load; cases come on top.</summary>
        public double CardWatts
        {
            get
            {
                double sum = S.nvme ? HardwareCatalog.NvmeWatts : 0;
                foreach (string id in S.gpuModels) { var g = HardwareCatalog.Gpu(id); if (g != null) sum += g.watts; }
                return sum;
            }
        }

        /// <summary>Everything the house's circuit carries: cards plus cases.</summary>
        public double LoadWatts { get { return RequestedWatts; } }

        /// <summary>Buy a new card on 淘货 on this calendar day.</summary>
        public bool BuyCard(string modelId, DateTime today)
        {
            var g = HardwareCatalog.Gpu(modelId);
            if (g == null) return Reject("没有这款显卡。");
            if (g.rumour) return Reject("亲，" + g.name + " 还没到货哦，厂家没通知。");
            if (!HardwareCatalog.OnSale(g, today)) return Reject(g.name + " 还没上市。");
            double price = HardwareCatalog.Price(g, today);
            if (!SlotFree) return Reject("机箱插槽已满（" + Slots + " 个）：先在喵鱼卖掉一张，或买一台主机准系统。");
            if (!CanSpend(price)) return Reject("经费不足：" + g.name + " 要 ¥" + price.ToString("0") + "。");
            S.money -= price;
            AddCard(g);
            bool burned = false;
            if (g.id == HardwareCatalog.Rx480 && !S.pcieBurned && NextUnit() < HardwareCatalog.PcieBurnChance)
            {
                S.pcieBurned = true; burned = true;
                TripBreaker();
            }
            Raise("gpu.bought", g.id);
            return Accept("亲，" + g.name + " 已到货装好：算力 ×" + g.compute.ToString("0.##") + "，显存 +" + (g.vramMB / 1024).ToString("0.#") + "G，功耗 +" + g.watts.ToString("0") + "W。" +
                (burned ? "开机一股焦味：PCIe 供电超标，跳闸了。去「家庭」合闸。" : S.breakerTripped ? "负载超限，已跳闸。" : ""));
        }

        /// <summary>Buy someone else's used card on 喵鱼 at today's second-hand price.</summary>
        public bool BuyUsedCard(string modelId, DateTime today)
        {
            var g = HardwareCatalog.Gpu(modelId);
            if (g == null || g.rumour || !HardwareCatalog.Released(g, today)) return Reject("这张卡已经被别人拍走了。");
            double price = HardwareCatalog.UsedPrice(g, today);
            if (!SlotFree) return Reject("机箱插槽已满（" + Slots + " 个）。");
            if (!CanSpend(price)) return Reject("经费不足：要 ¥" + price.ToString("0") + "。");
            S.money -= price;
            AddCard(g);
            Raise("gpu.bought", g.id);
            return Accept("收到二手 " + g.name + "：自用 99 新，无拆无修。算力 ×" + g.compute.ToString("0.##") + "。" + (S.breakerTripped ? "负载超限，已跳闸。" : ""));
        }

        /// <summary>A retired 网吧 machine: one more case (two slots) with a GTX 970 in it.</summary>
        public bool BuyCafeBox(DateTime today)
        {
            var g = HardwareCatalog.Gpu(HardwareCatalog.Gtx970);
            if (today.Date < HardwareCatalog.CafeRelease) return Reject("网吧老板：月底才清仓，10 月再来。");
            if (S.cafeBoxes >= HardwareCatalog.CafeBoxLimit) return Reject("网吧老板：就这 " + HardwareCatalog.CafeBoxLimit + " 台，全给你了。");
            if (S.caseCount >= Config.maxCases) return Reject("家里放不下更多机箱了。");
            if (!CanSpend(HardwareCatalog.CafeBoxPrice)) return Reject("经费不足：一台要 ¥" + HardwareCatalog.CafeBoxPrice.ToString("0") + "。");
            S.money -= HardwareCatalog.CafeBoxPrice;
            S.caseCount++; S.cafeBoxes++;
            AddCard(g);
            Raise("case.bought", "cafe");
            Raise("gpu.bought", g.id);
            return Accept("网吧退役主机 #" + S.cafeBoxes + " 已接入：E5 + X79 + GTX 970，小集群算力 ×" + (S.cafeBoxes * g.compute).ToString("0.##") + "。" + (S.breakerTripped ? "负载超限，已跳闸。" : ""));
        }

        /// <summary>The Samsung 950 Pro NVMe (September): data reaches the cards faster.</summary>
        public bool BuyNvme(DateTime today)
        {
            if (S.nvme) return Reject("已经装了一块 950 Pro。");
            if (today.Date < HardwareCatalog.NvmeRelease) return Reject("淘货 9 月才进三星 950 Pro 的货。");
            if (!CanSpend(HardwareCatalog.NvmePrice)) return Reject("经费不足：要 ¥" + HardwareCatalog.NvmePrice.ToString("0") + "。");
            S.money -= HardwareCatalog.NvmePrice;
            S.nvme = true;
            Raise("gpu.bought", "nvme");
            return Accept("三星 950 Pro NVMe 已装好：读数据不再卡显卡，训练 ×" + HardwareCatalog.NvmeBoost.ToString("0.0#") + "。");
        }

        /// <summary>
        /// Sell the card at <paramref name="index"/> on 喵鱼 for <paramref name="price"/> (what the buyer agreed,
        /// never more than today's second-hand price). Keeps one card and the board's VRAM.
        /// </summary>
        public bool SellCard(int index, double price, DateTime today)
        {
            if (index < 0 || index >= S.gpuModels.Count) return Reject("没有这张卡。");
            if (S.gpuCount <= 1) return Reject("至少留一张卡，不然它就停了。");
            var g = HardwareCatalog.Gpu(S.gpuModels[index]);
            double value = HardwareCatalog.UsedPrice(g, today);
            if (!Finite(price) || price < 0 || price > value + Epsilon) return Reject("买家出价不对。");
            if (MemoryUsed > CardVram - (g != null ? g.vramMB : 0) + Epsilon) return Reject("卖掉后显存不够放神经元板，先缩小板子。");
            S.gpuModels.RemoveAt(index); S.gpuCount--;
            S.money = AddBounded(S.money, price);
            Raise("gpu.sold", g != null ? g.id : null);
            return Accept("已卖出 " + (g != null ? g.name : "显卡") + "，到账 ¥" + price.ToString("0") + "。" + (S.breakerTripped ? "若仍跳闸，请到「家庭」合闸。" : ""));
        }

        /// <summary>Index of the card with the least compute (the one an old-style "sell a card" gives up).</summary>
        public int WeakestCard()
        {
            int best = -1; double low = double.MaxValue;
            for (int i = 0; i < S.gpuModels.Count; i++)
            {
                var g = HardwareCatalog.Gpu(S.gpuModels[i]);
                double c = g != null ? g.compute : 0;
                if (c < low - 1e-9) { low = c; best = i; }
            }
            return best;
        }

        void AddCard(GpuModel g)
        {
            S.gpuModels.Add(g.id); S.gpuCount++;
            if (RequestedWatts > Config.powerLimitWatts) TripBreaker();
        }

        /// <summary>Before validation: a save from before the models gets one starting card per counted card.</summary>
        void MigrateHardware()
        {
            if (S.gpuModels == null) S.gpuModels = new List<string>();
            if (S.gpuModels.Count == 0 && S.gpuCount > 0 && S.gpuCount <= 1000)
                for (int i = 0; i < S.gpuCount; i++) S.gpuModels.Add(HardwareCatalog.LegacyModel);
        }

        void ValidateHardware()
        {
            if (S.gpuModels == null || S.gpuModels.Count != S.gpuCount) throw new ArgumentException("Save's card list does not match its card count.");
            foreach (string id in S.gpuModels) if (!HardwareCatalog.Ownable(id)) throw new ArgumentException("Save holds an unknown card model.");
            if (S.cafeBoxes < 0 || S.cafeBoxes > HardwareCatalog.CafeBoxLimit || S.cafeBoxes >= S.caseCount) throw new ArgumentException("Save has an invalid number of 网吧 machines.");
        }

        void ValidateHardwareConfig()
        {
            if (!HardwareCatalog.Ownable(Config.starterGpuModel) || !HardwareCatalog.Ownable(Config.legacyGpuModel))
                throw new ArgumentException("Chapter-one configuration names an unknown card model.");
        }
    }
}
