using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    [Serializable] public sealed class XgLessonCurveStamp { public string id,dataset; public int track; public List<float> history=new List<float>(); }
    public sealed partial class XgState
    {
        public List<string> lessonDiscovered=new List<string>(), lessonCollected=new List<string>(), lessonPending=new List<string>();
        public List<string> quizQueued=new List<string>(), quizAnswered=new List<string>();
        public int trainingLessonObservedStage;
        public int trainingSmallSteps;
        public bool lessonAtlasUnread;
        public bool trainingQuizBonusPending;
        public List<XgLessonCurveStamp> lessonCurveStamps=new List<XgLessonCurveStamp>();
    }
    public sealed partial class XgRun
    {
        public bool trainingQuizBonusReserved;
    }
    public sealed partial class XgSim
    {
        public const double TrainingQuizBonusFactor = 1.05;
        public bool TrainingQuizBonusAvailable => S.trainingQuizBonusPending;
        public bool TrainingQuizBonusHeld => S.trainingQuizBonusPending || S.vision.trainingQuizBonusReserved || S.sequence.trainingQuizBonusReserved;
        void ReserveTrainingQuizBonus(XgRun run)
        {
            if (OfflineSimulation || !S.trainingQuizBonusPending) return;
            run.trainingQuizBonusReserved = true;
            S.trainingQuizBonusPending = false;
        }
        void RefundTrainingQuizBonus(XgRun run)
        {
            if (!run.trainingQuizBonusReserved) return;
            run.trainingQuizBonusReserved = false;
            S.trainingQuizBonusPending = true;
        }
        double ConsumeTrainingQuizBonus(XgRun run)
        {
            if (OfflineSimulation) { RefundTrainingQuizBonus(run); return 1; }
            // A round already in flight cannot take a reward earned after it started.
            if (!timedEpochLearning.HasValue) ReserveTrainingQuizBonus(run);
            bool bonus = run.trainingQuizBonusReserved;
            run.trainingQuizBonusReserved = false;
            return bonus ? TrainingQuizBonusFactor : 1;
        }
        bool trainingLessonsBound;
        public bool TrainingLessonModalOpen {get;set;}
        public bool TrainingKnowledgePending => S.lessonPending!=null&&S.lessonPending.Count>0;
        public void InitializeTrainingLessons()
        {
            if(S.lessonCurveStamps==null)S.lessonCurveStamps=new List<XgLessonCurveStamp>();
            if(S.trainParts==null)S.trainParts=new List<string>();
            if(S.lessonDiscovered==null)S.lessonDiscovered=new List<string>();
            if(S.lessonCollected==null)S.lessonCollected=new List<string>();
            if(S.lessonPending==null)S.lessonPending=new List<string>();
            if(S.quizQueued==null)S.quizQueued=new List<string>();
            if(S.quizAnswered==null)S.quizAnswered=new List<string>();
            // Keep forward-compatible progress; only remove entries that cannot be shown now.
            foreach(var id in S.lessonCollected)if(!S.lessonDiscovered.Contains(id))S.lessonDiscovered.Add(id);
            S.lessonPending.RemoveAll(id=>S.lessonCollected.Contains(id));
            S.quizQueued.RemoveAll(id=>S.quizAnswered.Contains(id));
            if(!trainingLessonsBound){trainingLessonsBound=true;EpochDone+=ObserveTrainingEpoch;StageAdvanced+=ObserveTrainingStage;}
            if(S.trainingLessonObservedStage!=S.stage){S.trainingLessonObservedStage=S.stage;NoteTrainingPhenomenon("stage_enter");}
        }
        public void NoteTrainingPhenomenon(string trigger)
        {
            if(S.quizQueued==null)S.quizQueued=new List<string>();
            if(S.quizAnswered==null)S.quizAnswered=new List<string>();
            foreach(var q in XgTrainingLessons.Quizzes)
                if(q.stage==S.stage&&q.trigger==trigger&&!S.quizAnswered.Contains(q.id)&&!S.quizQueued.Contains(q.id)) S.quizQueued.Add(q.id);
        }
        void DiscoverTrainingLesson(string id,XgRun observed=null)
        {
            var card=XgTrainingLessons.Card(id);if(card==null||S.lessonDiscovered.Contains(id))return;
            S.lessonDiscovered.Add(id);S.lessonPending.Add(id);
            if(observed!=null&&observed.histVal.Count>0&&id!="noise")
                S.lessonCurveStamps.Add(new XgLessonCurveStamp{id=id,dataset=observed.dataset,track=observed.track,history=new List<float>(observed.histVal)});
            Say(T("知识卡："+card.title+"，去练页收下。","Knowledge card: "+card.titleEn+". Collect it on Train."));
        }
        void ObserveTrainingStage(int from)
        {
            S.trainingLessonObservedStage=S.stage;NoteTrainingPhenomenon("stage_enter");
            DiscoverTrainingLesson("emergence");
        }
        public static bool TrainingHistoryFlat(IReadOnlyList<float> hist)
        {
            if(hist==null||hist.Count<6)return false;
            float lo=float.PositiveInfinity,hi=float.NegativeInfinity;bool alwaysFalling=true;
            for(int i=hist.Count-6;i<hist.Count;i++)
            {
                float v=hist[i];if(float.IsNaN(v)||float.IsInfinity(v))return false;
                lo=Math.Min(lo,v);hi=Math.Max(hi,v);
                if(i>hist.Count-6&&v>=hist[i-1])alwaysFalling=false;
            }
            return !alwaysFalling&&hi-lo<=.002f;
        }
        static bool FiniteHistoryTail(IReadOnlyList<float> hist)
        {
            if(hist==null||hist.Count<2)return false;
            for(int i=hist.Count-2;i<hist.Count;i++)if(float.IsNaN(hist[i])||float.IsInfinity(hist[i]))return false;
            return true;
        }
        void ObserveTrainingEpoch(XgEpoch e)
        {
            var run=Run((XgTrack)e.track);var hist=run.histVal;
            S.trainingSmallSteps=RateNotch(run)<0&&!e.diverged?S.trainingSmallSteps+1:0;
            if(e.diverged){DiscoverTrainingLesson("nan",run);NoteTrainingPhenomenon("nan");}
            if(TrainPart("holdout")&&PracticeAccuracy(run)-run.valAcc>.15){DiscoverTrainingLesson("overfit",run);NoteTrainingPhenomenon("gap");}
            if(FiniteHistoryTail(hist)){double delta=hist[hist.Count-1]-hist[hist.Count-2];if(delta<-.0005)DiscoverTrainingLesson("regress",run);if(delta>=.1)DiscoverTrainingLesson("grokking",run);}
            bool flat=TrainingHistoryFlat(hist);
            if(flat){if(!S.trainParts.Contains("ceiling"))S.trainParts.Add("ceiling");DiscoverTrainingLesson("ceiling",run);NoteTrainingPhenomenon("plateau");if(S.trainingSmallSteps>=5)DiscoverTrainingLesson("small_step",run);}
            if(S.stage==2&&run.depth>4&&flat){DiscoverTrainingLesson("vanishing",run);NoteTrainingPhenomenon("deep_flat");}
            if(Noise(run.dataset)+DataNoise(run.dataset)>20){DiscoverTrainingLesson("noise",run);NoteTrainingPhenomenon("noise");}
            if(S.stage==3&&(run.dataset=="poems"||run.dataset=="longtext")&&run.valAcc<.65&&run.epoch>=2)NoteTrainingPhenomenon("long_fail");
            if(S.stage==4&&run.track==0&&run.depth>4&&FiniteHistoryTail(hist)&&hist[hist.Count-1]<hist[hist.Count-2])NoteTrainingPhenomenon("deep_worse");
            if(S.stage==4&&run.dataset=="translate"&&!Has("seq2seq"))NoteTrainingPhenomenon("translate_fail");
            if(S.stage==5&&run.dataset=="parallel"&&SerialWiring(Knobs(run)))NoteTrainingPhenomenon("serial");
            // Continuation and shifted-image failures are reported by the actual interaction.
            if(S.stage==6&&PhenomenonSeen("sycophancy"))NoteTrainingPhenomenon("flatter");
        }
        public XgStageQuiz PendingTrainingQuiz()
        {
            InitializeTrainingLessons();
            foreach(var id in S.quizQueued)if(!S.quizAnswered.Contains(id)){var q=XgTrainingLessons.Quiz(id);if(q!=null)return q;}
            return null;
        }
        public bool AnswerTrainingQuiz(string id,int choice,IXgHost host,out bool right)
        {
            InitializeTrainingLessons();right=false;var q=XgTrainingLessons.Quiz(id);
            if(host==null||q==null||q.options==null||choice<0||choice>=q.options.Length||S.quizAnswered.Contains(id)||!S.quizQueued.Contains(id))return false;
            right=choice==q.answer;S.quizAnswered.Add(id);S.quizQueued.Remove(id);
            if(right && !TrainingQuizBonusHeld) S.trainingQuizBonusPending=true;
            return true;
        }
        public void CollectTrainingLesson(string id)
        {
            InitializeTrainingLessons();if(!S.lessonDiscovered.Contains(id))return;
            if(!S.lessonCollected.Contains(id)){S.lessonCollected.Add(id);S.lessonAtlasUnread=true;}S.lessonPending.Remove(id);
        }
        // A halo is only valid while the displayed recorded curve is still the observed history.
        // Old saves and later/replaced curves have no trustworthy point: show no guessed halo.
        public bool TrainingLessonCurvePoint(string id,XgRun run,out int index,out float value)
        {
            index=-1;value=0;if(run==null||S.lessonCurveStamps==null)return false;
            var stamp=S.lessonCurveStamps.Find(x=>x!=null&&x.id==id&&x.dataset==run.dataset&&x.track==run.track);
            if(stamp==null||stamp.history==null||stamp.history.Count==0||stamp.history.Count!=run.histVal.Count)return false;
            for(int i=0;i<stamp.history.Count;i++)if(float.IsNaN(stamp.history[i])||float.IsInfinity(stamp.history[i])||!stamp.history[i].Equals(run.histVal[i]))return false;
            index=stamp.history.Count-1;value=stamp.history[index];return !float.IsNaN(value)&&!float.IsInfinity(value);
        }
        public bool TrainingAtlasLit(string id)
        {
            if(string.IsNullOrEmpty(id))return false;
            if(id.StartsWith("lesson."))return S.lessonCollected!=null&&S.lessonCollected.Contains(id.Substring(7));
            if(id.StartsWith("quiz."))return S.quizAnswered!=null&&S.quizAnswered.Contains(id.Substring(5));return false;
        }
    }
}
