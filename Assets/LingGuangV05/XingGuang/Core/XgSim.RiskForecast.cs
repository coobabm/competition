using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    public sealed class XgRiskForecast
    {
        public bool available,precise,atFloor,guarded;
        public double tear,ordinaryRoll,ordinaryDrop,scoreDrop;
        public int level;
    }
    public sealed partial class XgSim
    {
        public bool PreciseRiskForecast=>DailyRiskEnhancementAvailable(Has("train.monitor"));
        // Read-only projection of the game's own round rules, never an estimate of an external model.
        // TrainEpoch recovers one point of meme drift before StepAccuracy rolls the ordinary drop.
        public XgRiskForecast TrainingRiskForecast(XgRun run)
        {
            var f=new XgRiskForecast{precise=PreciseRiskForecast};
            var d=run==null?null:XgCatalog.Dataset(run.dataset);
            if(d==null||!Finite(run.valAcc))return f;
            double afterDrift=Math.Max(0,MemeDrift(run.dataset)-MemeDriftEpochRecovery)/100;
            var risk=DropRisk(run,afterDrift);
            f.tear=Clamp01(TearChance(run));
            f.ordinaryRoll=Clamp01(risk.chance*RateSteady(run));
            f.atFloor=run.valAcc-(1-d.chanceError)<=1e-12;
            f.guarded=RelicGuardReady;
            f.ordinaryDrop=f.atFloor||f.guarded?0:(1-f.tear)*f.ordinaryRoll;
            f.scoreDrop=f.atFloor||f.guarded?0:f.tear+f.ordinaryDrop;
            f.level=f.scoreDrop<RiskMedium?0:f.scoreDrop<RiskHigh?1:2;
            f.available=Finite(f.tear)&&Finite(f.ordinaryRoll)&&Finite(f.scoreDrop);
            return f;
        }
        static string ForecastPct(double value,bool precise)=>(value*100).ToString(precise?"0.0":"0",CultureInfo.InvariantCulture)+"%";
        public string TrainingRiskLine(XgRiskForecast f)
        {
            if(f==null||!f.available)return T("风险预报：暂不可用","Risk forecast: unavailable");
            string line=T("本轮分数下降：","Score drops this round: ")+RiskLevelName(f.level,English)+" "+ForecastPct(f.scoreDrop,f.precise);
            if(f.tear>0)line=T("炸：","Blow-up: ")+ForecastPct(f.tear,f.precise)+" · "+line;
            if(f.guarded)line+=T(" · 残差贴纸已充好"," · residual guard charged");
            else if(f.atFloor)line+=T(" · 已在最低分"," · already at score floor");
            if(f.precise)line+=T(" · 概率直觉"," · probability intuition");
            return line;
        }
        public string TrainingRiskDetail(XgRiskForecast f)
        {
            if(f==null||!f.available)return T("当前配置无法给出可靠数值。","No reliable number is available for this configuration.");
            string text=T("按当前模型、数据和学习率计算的游戏训练规则概率；配置改变会重新计算。它描述抽签规则，不保证单轮结果。","Probabilities from the game's training rules for the current model, data and rate; they recalculate when the setup changes. They describe the random rules, not a guaranteed result.");
            if(f.precise)text+="\n"+T("未炸时，普通退步抽签：","Ordinary drop roll, if no blow-up: ")+ForecastPct(f.ordinaryRoll,true)
                +T("；本轮普通退步：","; ordinary score drop this round: ")+ForecastPct(f.ordinaryDrop,true)
                +T("。先判定炸，再判普通退步，二者不直接相加。已计入学习率、该轮新梗恢复、最低分和已充好的残差贴纸。",". A blow-up is checked first, then an ordinary drop; the two chances are not simply added. Includes the rate, this round's meme recovery, score floor and charged residual guard.");
            else text+="\n"+T("购买概率直觉后，显示到0.1个百分点，并解释本轮两种退步的条件。","Probability intuition adds 0.1-percentage-point display precision and explains the two conditional drop paths.");
            if(f.guarded)text+="\n"+T("贴纸挡住分数损失，炸或退步抽签仍可能发生。","The guard prevents score loss; a blow-up or drop roll can still occur.");
            return text;
        }
    }
}
