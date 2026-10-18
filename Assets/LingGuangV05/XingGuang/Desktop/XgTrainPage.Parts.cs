using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        static void Part(Component c,bool on) { if(c!=null && c.gameObject.activeSelf!=on)c.gameObject.SetActive(on); }
        void RefreshTrainingParts()
        {
            bool legacy=!Sim.S.teachingFlow;
            bool model=Sim.TrainPart("model"), rates=Sim.TrainPart("rate");
            summary.Show(model); saveModel.Show(Sim.TrainPart("checkpoint"));
            foreach(var b in trackTabs) b.Show(Sim.TrainPart("tracks"));
            Part(rateStrip,rates); Part(risk,Sim.TrainPart("monitor"));
            Part(diagnosticArea,Sim.TrainPart("weights"));
            Part(scaleCard,Sim.TrainPart("next")); Part(ledgerCard,Sim.TrainPart("ledger"));
            Part(trainBill,true); // Per-round price is a transaction cost, not hidden ledger detail.
            bool gear=Sim.TrainPart("gear"), learning=Sim.TrainPart("learning"), books=Sim.TrainPart("books");
            gearTab.Show(gear); learningTab.Show(learning); booksTab.Show(books);
            if(booksView&&!books)booksView=false;
            if(learningView&&!learning)learningView=false;
            if(!gear&&!booksView&&!learningView){if(learning)learningView=true;else if(books)booksView=true;}
            Part(gearCard,gear||learning||books);
            bool side=ledgerSeal!=null||Sim.TrainPart("next")||Sim.TrainPart("ledger")||gear||learning||books;
            Part(right,side); left.offsetMin=new Vector2(side?RightWidth+ColumnGap:0,0);
            curveCard.offsetMax=new Vector2(0,model||Sim.TrainPart("tracks")? -TabsHeight-RowGap:0);
            chart.rectTransform.anchorMin=new Vector2(0,Sim.TrainPart("weights")?.37f:0);
            chart.MarkDrops=Sim.TrainPart("checkpoint");
            chart.ShowValidation=Sim.TrainPart("holdout");
            microscope.Show(Sim.TrainPart("weights"));
            if(!Sim.TrainPart("holdout"))Part(memorizeNote,false);
            if(!Sim.TrainPart("holdout"))
            {
                var run=Sim.Run(Track);
                numbers.text=T(XgCatalog.Dataset(run.dataset).name,XgCatalog.Dataset(run.dataset).nameEn)+" · "+T("第 "+run.epoch+" 轮","Round "+run.epoch);
                legend.text=T("每轮答卷答对率","Accuracy after each round");
                var practice=Sim.PracticeSeries(run); chart.SetData(practice,practice,0);
            }
            if(!Sim.TrainPart("ceiling"))chart.SetCeiling(0,Color.clear);
            if(!legacy&&!Sim.TrainPart("monitor"))hint.text="";
            ledgerSeal?.RefreshState();
            LayoutTrainingSeals();
        }
    }
}
