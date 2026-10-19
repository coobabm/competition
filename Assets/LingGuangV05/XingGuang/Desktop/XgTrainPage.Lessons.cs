using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        RectTransform lessonLayer, quizPanel;
        TMP_Text lessonTitle, lessonBody, quizText;
        XgBtn lessonCollect, quizNext;
        XgBtn[] quizChoices;
        string shownLessonId="", shownQuizId="", quizResult="", lessonViewKey="";
        XgStageQuiz answeredQuiz;
        string reviewLessonId="";
        float nextLessonPoll;
        XgSim lessonOwner;
        void BindLessonOwner()
        {
            if(ReferenceEquals(lessonOwner,Sim))return;
            if(lessonOwner!=null)lessonOwner.TrainingLessonModalOpen=false;
            lessonOwner=Sim;answeredQuiz=null;reviewLessonId="";shownQuizId="";shownLessonId="";quizResult="";
            HideTrainingLessons();
        }
        public void HideTrainingLessons()
        {
            CancelKnowledgeFlight();
            lessonViewKey="";nextLessonPoll=0;
            if(lessonLayer!=null)lessonLayer.gameObject.SetActive(false);
            if(quizPanel!=null)quizPanel.gameObject.SetActive(false);
            if(lessonOwner!=null)lessonOwner.TrainingLessonModalOpen=false;
        }
        void BuildLessons()
        {
            quizPanel=ui.Card(root,"StageQuiz",new Vector2(0,1),Vector2.one,new Vector2(0,-134),Vector2.zero);
            quizText=ui.Text(Rect("Question",quizPanel,Vector2.zero,Vector2.one,new Vector2(12,66),new Vector2(-12,-8)),"",16,XgDark.Ink,TextAlignmentOptions.TopLeft);
            quizChoices=new XgBtn[4];
            for(int i=0;i<quizChoices.Length;i++)
            {
                int choice=i;
                quizChoices[i]=ui.Button(quizPanel,"",()=>AnswerLessonQuiz(choice),13);
                quizChoices[i].rt.anchorMin=new Vector2(i/4f,0);quizChoices[i].rt.anchorMax=new Vector2((i+1)/4f,0);
                quizChoices[i].rt.offsetMin=new Vector2(8,10);quizChoices[i].rt.offsetMax=new Vector2(-8,52);
            }
            quizNext=ui.Button(quizPanel,T("去看看 →","Show me →"),()=>{
                var q=answeredQuiz;answeredQuiz=null;shownQuizId="";quizResult="";
                if(q!=null)FocusNode(XgTrainingLessons.QuizNode(q));
                RefreshLessons();
            },14);
            quizNext.rt.anchorMin=new Vector2(.65f,0);quizNext.rt.anchorMax=new Vector2(1,0);
            quizNext.rt.offsetMin=new Vector2(8,10);quizNext.rt.offsetMax=new Vector2(-8,52);
            lessonLayer=Rect("KnowledgeCardLayer",root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            Panel(lessonLayer,new Color(0,0,0,.78f));
            var box=ui.Card(lessonLayer,"KnowledgeCard",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-210,-230),new Vector2(210,230));
            lessonBox=box;
            lessonTitle=ui.Text(Rect("Title",box,new Vector2(0,1),Vector2.one,new Vector2(24,-64),new Vector2(-24,-20)),"",24,XgDark.Accent,TextAlignmentOptions.MidlineLeft);
            lessonBody=ui.Text(Rect("Body",box,Vector2.zero,Vector2.one,new Vector2(24,76),new Vector2(-24,-76)),"",17,XgDark.Ink,TextAlignmentOptions.TopLeft);
            lessonCollect=ui.Button(box,T("收下","Keep"),CollectKnowledgeVisual,18);
            lessonCollect.rt.anchorMin=new Vector2(0,0);lessonCollect.rt.anchorMax=new Vector2(1,0);
            lessonCollect.rt.offsetMin=new Vector2(24,20);lessonCollect.rt.offsetMax=new Vector2(-24,62);
            lessonLayer.gameObject.SetActive(false);quizPanel.gameObject.SetActive(false);
        }
        void RefreshLessons()
        {
            BindLessonOwner();Sim.InitializeTrainingLessons();
            if(knowledgeFlying)return;
            if(quizPanel==null)BuildLessons();
            bool available=view.Visible&&root!=null&&root.gameObject.activeInHierarchy&&Sim.TeachingTrainOpen&&!StoryFocus.Active&&!XgEmergenceCutscene.Busy;
            var q=answeredQuiz??Sim.PendingTrainingQuiz();
            bool showQuiz=available&&q!=null;
            quizPanel.gameObject.SetActive(showQuiz);
            left.offsetMax=new Vector2(left.offsetMax.x,showQuiz?-144:0);
            right.offsetMax=new Vector2(right.offsetMax.x,showQuiz?-144:0);
            if(showQuiz)
            {
                shownQuizId=q.id;quizText.text=answeredQuiz!=null?quizResult:T(q.q,q.qEn);
                for(int i=0;i<quizChoices.Length;i++)
                {
                    bool show=answeredQuiz==null&&i<q.options.Length;
                    quizChoices[i].Show(show);
                    if(show){quizChoices[i].Set(T(q.options[i],q.optionsEn[i]),true,XgDark.Button,XgDark.Ink);
                        quizChoices[i].rt.anchorMin=new Vector2((float)i/q.options.Length,0);
                        quizChoices[i].rt.anchorMax=new Vector2((float)(i+1)/q.options.Length,0);}
                }
                quizNext.Show(answeredQuiz!=null);
            }
            if(!available||cardT>=0){lessonLayer.gameObject.SetActive(false);Sim.TrainingLessonModalOpen=false;return;}
            string id=reviewLessonId!=""?reviewLessonId:Sim.S.lessonPending.Find(candidate=>XgTrainingLessons.Card(candidate)!=null)??"";
            var lesson=XgTrainingLessons.Card(id);
            if(lesson==null){lessonLayer.gameObject.SetActive(false);Sim.TrainingLessonModalOpen=false;return;}
            if(shownLessonId!=id)XgBrainWindow.KnowledgeShown(Sim,T(lesson.title,lesson.titleEn));
            shownLessonId=id;
            lessonTitle.text=KnowledgeTerms(KnowledgeTitle(lesson));
            lessonBody.text=KnowledgeTerms(T(lesson.saw,lesson.sawEn)+"\n\n"+T(lesson.body,lesson.bodyEn));
            UpdateKnowledgeHalo(id);
            lessonCollect.Set(reviewLessonId!=""?T("返回","Back"):T("收下","Keep"),true);
            lessonLayer.gameObject.SetActive(true);lessonLayer.SetAsLastSibling();Sim.TrainingLessonModalOpen=true;
        }
        void AnswerLessonQuiz(int choice)
        {
            var q=XgTrainingLessons.Quiz(shownQuizId);
            if(q==null||!Sim.AnswerTrainingQuiz(q.id,choice,Host,out bool right))return;
            answeredQuiz=q;
            quizResult=T(right?"答对了 · 一轮训练收益 +5%（不叠加）":"看看解释",right?"Correct · +5% learning for one round (no stacking)":"Here is why")+"\n"+T(q.explain,q.explainEn);
            RefreshLessons();view.Refresh(true);
        }
        public void TickTrainingLessons()
        {
            if(Sim==null)return;
            BindLessonOwner();
            if(!view.Visible||root==null||!root.gameObject.activeInHierarchy){HideTrainingLessons();return;}
            if(Time.unscaledTime<nextLessonPoll)return;nextLessonPoll=Time.unscaledTime+.2f;
            BindLessonOwner();Sim.InitializeTrainingLessons();
            string key=Sim.S.stage+"|"+Sim.TeachingTrainOpen+"|"+StoryFocus.Active+"|"+XgEmergenceCutscene.Busy+"|"+(cardT>=0)+"|"+string.Join(",",Sim.S.lessonPending)+"|"+string.Join(",",Sim.S.quizQueued)+"|"+Sim.S.quizAnswered.Count;
            if(key==lessonViewKey)return;lessonViewKey=key;RefreshLessons();
        }
        public bool ShowTrainingReview(string target)
        {
            BindLessonOwner();Sim.InitializeTrainingLessons();
            if(!Sim.TrainingAtlasLit(target))return false;
            if(target.StartsWith("lesson."))reviewLessonId=target.Substring(7);
            else if(target.StartsWith("quiz."))
            {
                answeredQuiz=XgTrainingLessons.Quiz(target.Substring(5));
                if(answeredQuiz==null)return false;
                quizResult=T(answeredQuiz.q,answeredQuiz.qEn)+"\n"+T(answeredQuiz.explain,answeredQuiz.explainEn);
            }
            else return false;
            view.ShowTab("train");RefreshLessons();return true;
        }
    }
}

