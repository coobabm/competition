using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Compatibility component name retained. Private learning notes never enter a human YY conversation.</summary>
    public sealed class LaoZhouYY : MonoBehaviour
    {
        static LaoZhouYY instance;
        ChapterOneRuntime runtime;
        RectTransform window,client,choices;
        TMP_Text body;
        sealed class Note { public string text; public XgZhouReply[] replies; public Note(string text, XgZhouReply[] replies=null){this.text=text;this.replies=replies;} }
        readonly Queue<Note> notes=new Queue<Note>();
        XgZhouReply[] pending;
        float nextCheck;
        public const float TypeBase=.9f, TypePerChar=.045f, TypeMax=3.2f, BubbleGap=.5f;
        static string T(string zh,string en)=>GameText.T(zh,en);
        public static LaoZhouYY Install(MonoBehaviour host)
        {
            return host==null?null:host.GetComponent<LaoZhouYY>()??host.gameObject.AddComponent<LaoZhouYY>();
        }
        void OnEnable(){instance=this;}
        void OnDisable(){if(instance==this)instance=null;Close();}
        public static bool Lesson(XgTutorLesson lesson)
        {
            if(instance==null||lesson==null||PrologueDirector.Desk==null)return false;
            instance.notes.Enqueue(new Note(T(lesson.zhouZh,lesson.zhouEn),XgSim.ZhouReplies(lesson.id)));
            if(instance.window==null)instance.ShowNext();return true;
        }
        void Update()
        {
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;
            if(runtime==null)runtime=FindAnyObjectByType<ChapterOneRuntime>();
            if(runtime==null||runtime.Sim==null||runtime.TestMode||runtime.Sim.InPrologue||StoryFocus.Active)return;
            if(window!=null||InnerVoice.Busy)return;
            if(notes.Count>0){ShowNext();return;}
            var sim=GuideNote.TopSim;if(sim==null||sim.RelicPickPending)return;
            if((sim.S.chapterComplete||sim.EndingAvailable)&&!sim.S.tutorSeen.Contains(XgSim.ZhouEndingKey))
            {
                sim.S.tutorSeen.Add(XgSim.ZhouEndingKey);notes.Enqueue(new Note(sim.ZhouEndingLine(GameText.IsEnglish)));runtime.MarkDirty();ShowNext();return;
            }
            if(sim.S.tutorSeen.Contains(XgSim.ZhouStageKey(sim.S.stage)))return;
            var note=sim.ZhouStageNoteNow(sim.S.stage);if(note==null)return;
            sim.S.tutorSeen.Add(XgSim.ZhouStageKey(note.stage));sim.ZhouStageShown(note.stage);runtime.MarkDirty();
            notes.Enqueue(new Note(T(note.zh,note.en)+"\n\n"+sim.SelfStudyStatus(GuideNote.TopStep,GameText.IsEnglish),note.replies));ShowNext();
        }
        void ShowNext()
        {
            var desk=PrologueDirector.Desk;if(desk==null||StoryFocus.Active||notes.Count==0)return;
            if(window==null)
            {
                client=desk.Window("Private Study Notes",T("我的自学便签","My study notes"),Vector2.zero,new Vector2(700,390),out window,()=>{window=null;client=null;body=null;choices=null;});
                body=desk.Text(PrologueDesk.Rect("Text",client,Vector2.zero,Vector2.one,new Vector2(22,135),new Vector2(-22,-20)),"",18,PrologueDesk.Ink,TextAlignmentOptions.TopLeft);
                body.richText=false;
                choices=PrologueDesk.Rect("Choices",client,Vector2.zero,new Vector2(1,0),new Vector2(18,10),new Vector2(-18,125));
            }
            var note=notes.Dequeue();body.text=note.text;pending=note.replies;DrawChoices();
        }
        void DrawChoices()
        {
            if(choices==null)return;
            foreach(Transform child in choices){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var desk=PrologueDirector.Desk;
            if((pending==null||pending.Length==0)&&notes.Count>0){desk.Button(choices,T("下一条","Next"),Vector2.zero,new Vector2(230,32),ShowNext);return;}
            if(pending==null||pending.Length==0){desk.Button(choices,T("记下了","Noted"),Vector2.zero,new Vector2(230,32),Close);return;}
            for(int i=0;i<pending.Length;i++)
            {
                var choice=pending[i];desk.Button(choices,T(choice.zh,choice.en),new Vector2(0,40-i*36),new Vector2(620,32),()=>Choose(choice));
            }
        }
        void Choose(XgZhouReply choice)
        {
            var sim=GuideNote.TopSim;if(sim==null)return;
            bool resolved=sim.ZhouApply(choice.effect);
            var continuation=new List<Note>();
            if(!string.IsNullOrEmpty(choice.answerZh))continuation.Add(new Note(T(choice.answerZh,choice.answerEn)));
            var next=XgSim.ZhouNode(choice.next);
            if(next!=null){sim.ZhouApply(next.effect);continuation.Add(new Note(T(next.zh,next.en),next.replies));}
            var waiting=notes.ToArray();notes.Clear();
            foreach(var note in continuation)notes.Enqueue(note);
            foreach(var note in waiting)notes.Enqueue(note);
            if(resolved)InnerVoice.Say("先把这些留给自己。下一次，用新题检验。","Keep these notes to myself. Next time, test fresh questions.",2.5f);
            runtime?.MarkDirty();if(notes.Count>0)ShowNext();else Close();
        }
        void Close(){if(window!=null)Destroy(window.gameObject);window=null;client=null;body=null;choices=null;pending=null;}
    }
}
