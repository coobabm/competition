using LingGuangV05.Core;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    public static class XgOfflineIncome
    {
        public static void Install(XingGuangController owner)
        {
            if(owner==null||owner.runtime==null)return;
            owner.runtime.OfflineIncomeQuoteProvider=()=>Quote(owner);
            if(owner.runtime.OfflineIncome>0)
                owner.Offline=new XingGuangController.OfflineReport{seconds=owner.runtime.OfflineSeconds,income=owner.runtime.OfflineIncome};
        }
        static XgOfflineQuote Quote(XingGuangController owner)
        {
            var sim=owner?.Sim;
            bool unlocked=XgIdleRules.Unlocked(sim);
            double rate=unlocked&&owner.runtime!=null&&owner.runtime.tickEnabled&&!owner.runtime.Sim.InPrologue
                ?XgIdleRules.IncomeRate(sim,owner.Host):0;
            return new XgOfflineQuote{unlocked=unlocked,eligible=rate>0,incomePerSecond=rate};
        }
        public static bool Enabled(XingGuangController owner)=>owner?.runtime?.Sim?.S.offlineIncome?.enabled==true;
        public static bool SetEnabled(XingGuangController owner,bool enabled)=>owner!=null&&owner.runtime!=null&&owner.runtime.SetOfflineIncomeEnabled(enabled);
        public static double Estimate(XingGuangController owner,double seconds)
        {
            var quote=Quote(owner);
            return quote.eligible?XgOfflineRules.Estimate(quote.incomePerSecond,seconds):0;
        }
    }
}
