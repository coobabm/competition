using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>Price of retired training-automation nodes still owed to the wallet; paid once on the next tick, then 0.</summary>
        public double autoRefund;
    }

    /// <summary>
    /// Training by itself is gone (the 自动化 lane of the tech tree and the 自动训练 switch). A save that still owns one of its
    /// nodes gets the price back once and loses the nodes.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The retired nodes and what each cost in the tech tree (before the story discount).</summary>
        static readonly string[] RetiredAutoIds = { "auto1", "auto2", "auto3", "auto4", "auto5" };
        static readonly double[] RetiredAutoCosts = { 250, 900, 3000, 5000, 20000 };

        /// <summary>Takes the retired nodes out of a loaded save and books their price as a pending refund.</summary>
        void RepairAutoRetired()
        {
            if (!Finite(S.autoRefund) || S.autoRefund < 0) S.autoRefund = 0;
            for (int i = 0; i < RetiredAutoIds.Length; i++)
                if (S.unlocked.Remove(RetiredAutoIds[i])) S.autoRefund += Math.Ceiling(RetiredAutoCosts[i] * StoryResearchPrice);
        }

        /// <summary>Pays the pending refund into the wallet and says so in the feed; runs once per save.</summary>
        void PayAutoRefund(IXgHost host)
        {
            if (host == null || S.autoRefund <= 0) return;
            double amount = S.autoRefund;
            S.autoRefund = 0;
            host.Earn(amount);
            Say(T("自动训练下线了，退回 ¥" + F(amount, "0"), "Auto-training is gone, ¥" + F(amount, "0") + " refunded"));
        }
    }
}
