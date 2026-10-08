using System.Collections.Generic;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using AppNames = LingGuangV05.Core.AppNames;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Maps the 灵光 lab onto the shared household economy without changing ChapterOneSim:
    /// compute = the cards' summed multipliers as heartbeats (already 0 on a power cut or unpaid bill, and throttled when hot),
    /// VRAM = the cards' summed memory, ¥ = the one wallet, training = each round's electricity billed at once at the month's
    /// 阶梯电价 tier (ChapterOneSim.Bills.cs; before the first rent it is extra kWh on the daily bill, as before).
    /// As <see cref="IXgRig"/> it gives the 接线 page the owned cards (HardwareCatalog) and the house PSU and breaker.
    /// </summary>
    public sealed class XingGuangHost : IXgHost, IXgRig, IXgHousePower, IXgTrainingBill
    {
        /// <summary>Training runs the cards at about half of their rated power on top of the job.</summary>
        public const double TrainingLoad = .5;

        private readonly ChapterOneRuntime runtime;
        public XingGuangHost(ChapterOneRuntime runtime) { this.runtime = runtime; }

        private bool Live => runtime != null && runtime.Sim != null && !runtime.TestMode;

        public double Compute => Live ? runtime.Sim.HeartbeatsPerSecond : 0;
        /// <summary>One card's memory: every card holds a whole copy of the model (data parallel).</summary>
        public double VramMB => Live ? runtime.Sim.LargestCardVram : 0;
        public double Money => Live ? runtime.Sim.S.money : 0;

        public bool Spend(double amount)
        {
            if (!Live || amount < 0 || runtime.Sim.S.money + 1e-9 < amount) return false;
            runtime.Sim.S.money -= amount;
            runtime.MarkDirty();
            return true;
        }

        public void Earn(double amount)
        {
            if (!Live || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            var s = runtime.Sim.S;
            s.money += amount;
            s.totalEarned += amount;
            runtime.MarkDirty();
        }

        public void Train(double seconds)
        {
            if (!Live || seconds <= 0) return;
            runtime.Sim.BillTraining(seconds);
            runtime.MarkDirty();
        }

        /// <summary>The scaled bill of the consumables, cuDNN and the cooler (Core/ChapterOneSim.Bills.cs).</summary>
        public void TrainScaled(double seconds, double priceFactor, double energyFactor, double wearFactor)
        {
            if (!Live || seconds <= 0) return;
            runtime.Sim.BillTraining(seconds, priceFactor, energyFactor, wearFactor);
            runtime.MarkDirty();
        }

        public void CoolCards() { if (Live) { runtime.Sim.CoolCards(); runtime.MarkDirty(); } }

        /// <summary>
        /// What the next training round will cost at the month's tier, scaled the way <see cref="TrainScaled"/> will bill it
        /// (the training page shows it on the button). 0 before the first rent.
        /// </summary>
        public double RoundPrice(double seconds, double priceFactor, double energyFactor) => Live ? runtime.Sim.TrainingRoundPrice(seconds, priceFactor, energyFactor) : 0;
        /// <summary>The cards are hot and may burn out (the training page warns).</summary>
        public bool WearRiskLive => Live && runtime.Sim.WearRiskLive;
        /// <summary>Whether rent and electricity are being billed (from the lab's first day).</summary>
        public bool EconomyActive => Live && runtime.Sim.EconomyActive;
        public double MonthKwh => Live ? runtime.Sim.MonthKwh : 0;
        public int PowerTier => Live ? runtime.Sim.PowerTier : 1;
        /// <summary>Today's bills so far: training electricity and the day's total (0 when the calendar has not reached today's bill).</summary>
        public void TodayBills(out double power, out double total)
        {
            power = total = 0;
            if (!Live) return;
            var s = runtime.Sim.S;
            if (s.billsDay != GameCalendar.CurrentDay(s) || s.billsToday == null) return;
            power = s.billsToday.power; total = s.billsToday.Total;
        }
        /// <summary>The calendar date the bills are for (DateTime.MinValue when not live).</summary>
        public System.DateTime Today => Live ? GameCalendar.Now(runtime.Sim.S).Date : System.DateTime.MinValue;
        /// <summary>The rent of today's calendar day.</summary>
        public double RentToday => Live ? runtime.Sim.RentFor(GameCalendar.Now(runtime.Sim.S).Date) : 0;
        public double TierOneKwh => Live ? runtime.Sim.Config.tierOneKwh : 240;
        public double TierTwoKwh => Live ? runtime.Sim.Config.tierTwoKwh : 400;

        public double TrainingWatts => Live ? System.Math.Max(0, runtime.Sim.CardWatts - runtime.Sim.UnpluggedWatts) * TrainingLoad : 0;

        // ───────────── IXgRig (接线) ─────────────

        readonly List<XgCardSpec> cards = new List<XgCardSpec>();
        string cardsKey = "";

        public IReadOnlyList<XgCardSpec> Cards
        {
            get
            {
                var owned = runtime != null && runtime.Sim != null ? runtime.Sim.Cards : null;
                string key = owned == null ? "" : string.Join(",", owned);
                if (key == cardsKey) return cards;
                cardsKey = key; cards.Clear();
                if (owned != null)
                    foreach (string id in owned)
                    {
                        var g = HardwareCatalog.Gpu(id);
                        if (g != null) cards.Add(new XgCardSpec { id = g.id, name = g.name, nameEn = g.nameEn, compute = g.compute, vramMB = g.vramMB, watts = g.watts });
                    }
                return cards;
            }
        }

        public double PowerLimitWatts => runtime != null && runtime.Sim != null ? runtime.Sim.Config.powerLimitWatts : 3500;
        public double HouseWatts => runtime != null && runtime.Sim != null ? runtime.Sim.HouseWatts : 0;
        public bool BreakerTripped => runtime != null && runtime.Sim != null && runtime.Sim.S.breakerTripped;
        public string HousePowerBlocker => runtime != null && runtime.Sim != null && runtime.Sim.S.landlordCut ? LandlordCut
            : runtime != null && runtime.Sim != null && runtime.Sim.S.unpaidPower
            ? Lang.T("停电：电费欠缴，去「家庭」缴费") : BreakerTripped ? Lang.T("跳闸：去「家庭」合闸") : null;

        static string LandlordCut => GameText.T("房东拉了电闸：先去标注台挣钱把房租交上", "The landlord cut the power: earn the rent back on the labelling desk first");
        public void TripBreaker() { if (Live) { runtime.Sim.TripBreakerNow(); runtime.MarkDirty(); } }
        public bool ResetBreaker() { if (!Live) return false; bool ok = runtime.Sim.ResetBreaker(); if (ok) runtime.MarkDirty(); return ok; }
        public double UnpluggedWatts
        {
            get => runtime != null && runtime.Sim != null ? runtime.Sim.UnpluggedWatts : 0;
            set { if (runtime != null && runtime.Sim != null) runtime.Sim.UnpluggedWatts = value; }
        }

        public string Blocker
        {
            get
            {
                if (runtime == null || runtime.Sim == null) return GameText.T(AppNames.AppZh + "未启动", "Runtime not ready");
                if (runtime.TestMode) return GameText.T("测试模式：" + AppNames.AppZh + "暂停", "Test mode: paused");
                var s = runtime.Sim.S;
                if (s.landlordCut) return LandlordCut;
                if (s.unpaidPower) return Lang.T("停电：电费欠缴，去「家庭」缴费");
                if (s.breakerTripped) return Lang.T("跳闸：去「家庭」合闸");
                if (s.gpuCount <= 0) return GameText.T("没有显卡：去「" + AppNames.ShopZh + "」买一张", "No GPU: buy one on " + AppNames.ShopEn);
                if (runtime.Sim.PowerWatts <= 0) return Lang.T("超负载：去「家庭」扩容");
                return null;
            }
        }
    }
}
