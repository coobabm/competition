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
    /// VRAM = the cards' summed memory, ¥ = the one wallet, training load = extra kWh on the same daily power bill.
    /// As <see cref="IXgRig"/> it gives the 接线 page the owned cards (HardwareCatalog) and the house PSU and breaker.
    /// </summary>
    public sealed class XingGuangHost : IXgHost, IXgRig, IXgHousePower
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
            var sim = runtime.Sim;
            double watts = System.Math.Max(0, sim.CardWatts - sim.UnpluggedWatts) * TrainingLoad;
            sim.S.energyKwh += watts / 1000 * 24 * seconds / sim.Config.dayLengthSeconds;
        }

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
        public string HousePowerBlocker => runtime != null && runtime.Sim != null && runtime.Sim.S.unpaidPower
            ? Lang.T("停电：电费欠缴，去「家庭」缴费") : BreakerTripped ? Lang.T("跳闸：去「家庭」合闸") : null;
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
                if (s.unpaidPower) return Lang.T("停电：电费欠缴，去「家庭」缴费");
                if (s.breakerTripped) return Lang.T("跳闸：去「家庭」合闸");
                if (s.gpuCount <= 0) return GameText.T("没有显卡：去「" + AppNames.ShopZh + "」买一张", "No GPU: buy one on " + AppNames.ShopEn);
                if (runtime.Sim.PowerWatts <= 0) return Lang.T("超负载：去「家庭」扩容");
                return null;
            }
        }
    }
}
