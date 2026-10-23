using System;

namespace LingGuangV05.Core
{
    [Serializable] public sealed class XgOfflineState
    {
        public string lineage = "";
        public bool enabled, eligible;
        public double incomePerSecond;
        public long capturedUnix, receipt;
    }

    public sealed class XgOfflineQuote
    {
        public bool unlocked, eligible;
        public double incomePerSecond;
    }

    [Serializable] public sealed class XgOfflineReceipt
    {
        public int version = 1;
        public string lineage = "";
        public long serial, watermark;
        public double amount, seconds;
    }

    /// <summary>Money-only settlement arithmetic; never advances clocks, cards, models or narrative.</summary>
    public static class XgOfflineRules
    {
        public const double Efficiency = .6;
        public const double MaxSeconds = 8 * 60 * 60;
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static double Estimate(double rate,double seconds)
        {
            if(!Finite(rate)||rate<=0||!Finite(seconds)||seconds<=0)return 0;
            double result=rate*Math.Min(MaxSeconds,seconds)*Efficiency;
            return Finite(result)&&result<=double.MaxValue/100?Math.Floor(result*100)/100:0;
        }
        public static bool ValidReceipt(XgOfflineReceipt receipt,string lineage)
            => receipt!=null&&receipt.version==1&&receipt.lineage==lineage&&receipt.serial>=0&&receipt.watermark>=0
                &&Finite(receipt.amount)&&receipt.amount>=0&&Finite(receipt.seconds)&&receipt.seconds>=0&&receipt.seconds<=MaxSeconds;

        // The returned receipt must be durably written BEFORE the wallet is credited.
        public static XgOfflineReceipt Prepare(XgOfflineState state,long lastSeen,long now,XgOfflineReceipt previous)
        {
            if(state==null||previous==null)throw new ArgumentNullException();
            if(!ValidReceipt(previous,state.lineage))throw new ArgumentException("Invalid offline receipt.");
            if(state.receipt<previous.serial)return previous; // Recover an interrupted settlement, once.
            long start=Math.Max(Math.Max(lastSeen,state.capturedUnix),previous.watermark);
            double seconds=start>0&&now>start?Math.Min(MaxSeconds,(double)now-start):0;
            double amount=state.enabled&&state.eligible?Estimate(state.incomePerSecond,seconds):0;
            return new XgOfflineReceipt {
                lineage=state.lineage,watermark=Math.Max(start,now),
                serial=amount>0?checked(Math.Max(state.receipt,previous.serial)+1):Math.Max(state.receipt,previous.serial),
                amount=amount,seconds=amount>0?seconds:0
            };
        }
        public static bool Apply(GameState state,XgOfflineReceipt receipt)
        {
            if(state?.offlineIncome==null||!ValidReceipt(receipt,state.offlineIncome.lineage)
                ||state.offlineIncome.receipt>=receipt.serial||receipt.amount<=0)return false;
            if(!Finite(state.money+receipt.amount)||!Finite(state.totalEarned+receipt.amount))return false;
            state.money+=receipt.amount;
            state.totalEarned+=receipt.amount;
            state.offlineIncome.receipt=receipt.serial;
            return true;
        }
    }
}
