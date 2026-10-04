using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Maps the 灵光 lab onto the shared household economy without changing ChapterOneSim:
    /// compute = the cards' summed multipliers as heartbeats (already 0 on a power cut or unpaid bill, and throttled when hot),
    /// VRAM = the cards' summed memory, ¥ = the one wallet, training load = extra kWh on the same daily power bill.
    /// </summary>
    public sealed class XingGuangHost : IXgHost
    {
        /// <summary>Training runs the cards at about half of their rated power on top of the job.</summary>
        public const double TrainingLoad = .5;

        private readonly ChapterOneRuntime runtime;
        public XingGuangHost(ChapterOneRuntime runtime) { this.runtime = runtime; }

        private bool Live => runtime != null && runtime.Sim != null && !runtime.TestMode;

        public double Compute => Live ? runtime.Sim.HeartbeatsPerSecond : 0;
        public double VramMB => Live ? runtime.Sim.MemoryCapacity : 0;
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
            double watts = sim.CardWatts * TrainingLoad;
            sim.S.energyKwh += watts / 1000 * 24 * seconds / sim.Config.dayLengthSeconds;
        }

        public double TrainingWatts => Live ? runtime.Sim.CardWatts * TrainingLoad : 0;

        public string Blocker
        {
            get
            {
                if (runtime == null || runtime.Sim == null) return GameText.T(AppNames.AppZh + "未启动", "Runtime not ready");
                if (runtime.TestMode) return GameText.T("测试模式：" + AppNames.AppZh + "暂停", "Test mode: paused");
                var s = runtime.Sim.S;
                if (s.unpaidPower) return GameText.T("停电：电费欠缴，去「家庭」缴费", "Power cut: pay the bill in Home");
                if (s.breakerTripped) return GameText.T("跳闸：去「家庭」合闸", "Breaker tripped: reset it in Home");
                if (s.gpuCount <= 0) return GameText.T("没有显卡：去「" + AppNames.ShopZh + "」买一张", "No GPU: buy one on " + AppNames.ShopEn);
                if (runtime.Sim.PowerWatts <= 0) return GameText.T("超负载：去「家庭」扩容", "Overloaded: upgrade power in Home");
                return null;
            }
        }
    }
}
