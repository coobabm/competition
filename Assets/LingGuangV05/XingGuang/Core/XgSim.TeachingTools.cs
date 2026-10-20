using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgTeachingThreshold { public string dataset=""; public double value=.5; }
    [Serializable] public sealed class XgTeachingReview
    {
        public long id; public string dataset="",text="",textEn="",why="",whyEn="";
        public int seed; public bool truth,submittedLabel,suspected,hard,resolved;
        public double probabilityYes;
    }
    public sealed class XgTeachingPrediction
    {
        public bool yes,guessed; public double probabilityYes,confidence;
    }
    public sealed partial class XgState
    {
        public List<XgTeachingThreshold> teachingThresholds=new List<XgTeachingThreshold>();
        public List<XgTeachingReview> teachingReviews=new List<XgTeachingReview>();
        public bool teachingUncertainFirst; public long teachingReviewSerial;
    }
    public sealed partial class XgSim
    {
        public static bool BinaryToolDataset(string dataset)=>dataset=="spam"||dataset=="headline"||dataset=="review";
        public bool TeachingUncertainFirst=>Has("sigmoid")&&S.teachingUncertainFirst;
        public void SetTeachingUncertainFirst(bool enabled){S.teachingUncertainFirst=Has("sigmoid")&&enabled;}
        public double TeachingThreshold(string dataset)
        {
            if(!Has("bias")||S.teachingThresholds==null)return .5;
            foreach(var item in S.teachingThresholds)if(item!=null&&item.dataset==dataset)return double.IsNaN(item.value)||double.IsInfinity(item.value)?.5:Math.Max(.05,Math.Min(.95,item.value));
            return .5;
        }
        public bool SetTeachingThreshold(string dataset,double threshold)
        {
            if(!Has("bias")||!BinaryToolDataset(dataset)||double.IsNaN(threshold)||double.IsInfinity(threshold))return false;
            if(S.teachingThresholds==null)S.teachingThresholds=new List<XgTeachingThreshold>();
            threshold=Math.Max(.05,Math.Min(.95,threshold));
            foreach(var item in S.teachingThresholds)if(item!=null&&item.dataset==dataset){item.value=threshold;return true;}
            S.teachingThresholds.Add(new XgTeachingThreshold{dataset=dataset,value=threshold});return true;
        }
        public XgTeachingPrediction PredictTeachingBinary(XgCard card)
        {
            if(card==null||!BinaryToolDataset(card.dataset))return null;
            Board.Predict(XgBoardData.FromCard(card),Knobs(Run(XgCatalog.Dataset(card.dataset).track)),out _,out var trace);
            double score=double.IsNaN(trace.Score)||double.IsInfinity(trace.Score)?0:Math.Max(-40,Math.Min(40,trace.Score));
            // A model readout, not a random skill roll or an empirical accuracy percentage.
            double p=trace.Guessed?.5:1/(1+Math.Exp(-score));double threshold=TeachingThreshold(card.dataset);
            bool yes=Math.Abs(threshold-.5)<1e-9?trace.AnswerYes:p>threshold;
            return new XgTeachingPrediction{yes=yes,guessed=trace.Guessed,probabilityYes=p,confidence=yes?p:1-p};
        }
        public static XgCard TeachingPhraseCard(string dataset,XgPhrase phrase,int seed)=>new XgCard{dataset=dataset,question=phrase.text,questionEn=phrase.textEn,truth=phrase.yes,seed=seed,level=phrase.level};
        public XgPhrase PickTeachingPhrase(string dataset,Random random,out int seed)
        {
            // Headlines: half from this month's browser news, half from the authored bank, so every stage has 20+ distinct items.
            XgPhrase Pick()=>dataset=="headline"&&random.Next(2)==0?(XgTeachingHeadlines.Pick(Today,random)??XgMemes.Pick(dataset,random,3,Today,"",false)):XgMemes.Pick(dataset,random,3,Today,"",false);
            var selected=Pick();seed=random.Next();
            if(!TeachingUncertainFirst||!BinaryToolDataset(dataset))return selected;
            double best=Math.Abs(PredictTeachingBinary(TeachingPhraseCard(dataset,selected,seed)).probabilityYes-.5);
            // Rank a real, bounded candidate batch. No future-dated or family phrases enter it.
            for(int i=0;i<11;i++){var candidate=Pick();int candidateSeed=random.Next();double distance=Math.Abs(PredictTeachingBinary(TeachingPhraseCard(dataset,candidate,candidateSeed)).probabilityYes-.5);
                if(distance<best){best=distance;selected=candidate;seed=candidateSeed;}}
            return selected;
        }
        public double TeachingDeskUncertainty(string dataset)
        {
            if(!BinaryToolDataset(dataset))return 1;
            double best=1;var random=new Random(XgBoardData.Seed(dataset,0,XgBoardData.Use.Diagnostic,S.dataSalt));
            for(int i=0;i<8;i++){var p=XgMemes.Pick(dataset,random,3,Today,"",false);double d=Math.Abs(PredictTeachingBinary(TeachingPhraseCard(dataset,p,random.Next())).probabilityYes-.5);best=Math.Min(best,d);}
            return best;
        }
        public void RememberTeachingBinary(XgCard card,bool submittedLabel,XgTeachingPrediction prediction,string why,string whyEn)
        {
            if(card==null||prediction==null||!BinaryToolDataset(card.dataset))return;
            if(S.teachingReviews==null)S.teachingReviews=new List<XgTeachingReview>();
            bool suspect=Math.Max(prediction.probabilityYes,1-prediction.probabilityYes)>=.8 && (prediction.probabilityYes>.5)!=submittedLabel;
            bool hard=submittedLabel!=card.truth;
            if(!suspect&&!hard)return;
            // Same question is a single review item; repeated errors cannot fill the tray with duplicates.
            foreach(var old in S.teachingReviews)if(old!=null&&!old.resolved&&old.dataset==card.dataset&&old.text==card.question)return;
            if(S.teachingReviews.Count>=64){int old=S.teachingReviews.FindIndex(x=>x==null||x.resolved);if(old<0)old=0;S.teachingReviews.RemoveAt(old);}
            S.teachingReviews.Add(new XgTeachingReview{id=++S.teachingReviewSerial,dataset=card.dataset,text=card.question,textEn=card.questionEn,seed=card.seed,truth=card.truth,submittedLabel=submittedLabel,probabilityYes=prediction.probabilityYes,suspected=suspect,hard=hard,why=why,whyEn=whyEn});
        }
        // The hall only needs a count. Keep the same live eligibility as the tray without
        // allocating and sorting a list on every visible-page refresh.
        public int TeachingReviewCount(bool suspectOnly)
        {
            if(S.teachingReviews==null||(suspectOnly?!Has("dataclean"):!Has("label.hard")))return 0;
            int count=0;foreach(var r in S.teachingReviews)if(r!=null&&!r.resolved&&(suspectOnly?r.suspected:r.hard))count++;
            return count;
        }
        public List<XgTeachingReview> TeachingReviewTray(bool suspectOnly)
        {
            var rows=new List<XgTeachingReview>();if(S.teachingReviews==null)return rows;
            if(suspectOnly?!Has("dataclean"):!Has("label.hard"))return rows;
            foreach(var r in S.teachingReviews)if(r!=null&&!r.resolved&&(suspectOnly?r.suspected:r.hard))rows.Add(r);
            rows.Sort((a,b)=>{int c=Math.Abs(b.probabilityYes-.5).CompareTo(Math.Abs(a.probabilityYes-.5));return c!=0?c:a.id.CompareTo(b.id);});return rows;
        }
        public bool ResolveTeachingReview(long id,bool answer,out bool right)
        {
            right=false;if(S.teachingReviews==null)return false;
            var r=S.teachingReviews.Find(x=>x!=null&&x.id==id);if(r==null||r.resolved||!(r.suspected&&Has("dataclean")||r.hard&&Has("label.hard")))return false;
            right=answer==r.truth;if(!right)return true;
            r.resolved=true;r.submittedLabel=r.truth;return true;
        }
    }
}
