using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Bounded visual reactions to real simulation events; never invents learned concepts.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgBrainWindowFeedback:MaskableGraphic
    {
        public XgWallpaperConcepts concepts;
        float pulseAt=-10,goldAt=-10,sweepAt=-10,rebirthAt=-10,nextDraw;
        Vector2 pulseUv;bool correct;int pulses;
        readonly HashSet<string> dataRegions=new HashSet<string>();
        public void ResetFeedback(){dataRegions.Clear();newRegions.Clear();regionBorn.Clear();pulseAt=goldAt=sweepAt=rebirthAt=-10;lastReaction="";animating=false;SetVerticesDirty();}
        readonly List<Vector2> newRegions=new List<Vector2>();
        readonly List<float> regionBorn=new List<float>();
        public int FeedbackCount=>pulses;
        public int MeshBuilds{get;private set;}
        public int CorrectCount{get;private set;} public int WrongCount{get;private set;}
        public int ComboCount{get;private set;} public int EpochCount{get;private set;}
        public int DatasetCount{get;private set;} public int ConceptCount{get;private set;}
        public int AbilityCount{get;private set;} public int KnowledgeCount{get;private set;}
        bool lastLow,animating;string lastReaction="";
        public string LastReaction=>lastReaction;
        void Dirty(string kind){lastReaction=kind;animating=true;SetVerticesDirty();}
        public void Concept(string region){ConceptCount++;Pulse(region,true);Dirty("concept");}
        public void Knowledge(string region,bool ignored){KnowledgeCount++;Pulse(region,false);Dirty("knowledge");}

        static int RegionIndex(string r)=>r=="vision"?0:r=="sequence"?1:r=="tone"?2:3;
        static Vector2 RegionUv(int i)=>i==0?new Vector2(.34f,.59f):i==1?new Vector2(.68f,.59f):i==2?new Vector2(.68f,.35f):new Vector2(.36f,.35f);
        public void Region(string region,bool right){if(right)CorrectCount++;else WrongCount++;Pulse(region,right);Dirty(right?"correct":"wrong");}
        void Pulse(string region,bool right)
        {
            int i=RegionIndex(region);pulseUv=RegionUv(i);
            if(concepts!=null)foreach(var c in concepts.Cells)if(c.region==i){pulseUv=c.uv;break;}
            correct=right;pulseAt=Time.unscaledTime;pulses++;SetVerticesDirty();
        }
        public void Gold(){ComboCount++;goldAt=Time.unscaledTime;pulses++;Dirty("combo");}
        public void Sweep(){EpochCount++;sweepAt=Time.unscaledTime;pulses++;Dirty("epoch");}
        public void Rebirth(){AbilityCount++;rebirthAt=Time.unscaledTime;pulses++;Dirty("ability");}
        public void NewRegion(string dataset)
        {
            if(!dataRegions.Add(dataset))return;
            if(newRegions.Count<19){newRegions.Add(new Vector2(.12f+(newRegions.Count%5)*.18f,.82f-(newRegions.Count/5)*.16f));regionBorn.Add(Time.unscaledTime);}
            DatasetCount++;Pulse(LingGuangV05.XingGuang.XgBoardData.Region(dataset),true);Dirty("dataset");
        }
        protected override void OnDisable(){base.OnDisable();pulseAt=goldAt=sweepAt=rebirthAt=-10;}
        void Update(){
            if(lastLow!=GamePerformance.Low){lastLow=GamePerformance.Low;SetVerticesDirty();}
            if(GamePerformance.Low||!animating)return;
            float now=Time.unscaledTime;if(now<nextDraw)return;nextDraw=now+1f/15;
            SetVerticesDirty();if(now-Mathf.Max(Mathf.Max(pulseAt,goldAt),Mathf.Max(sweepAt,rebirthAt))>=1.2f)animating=false;
        }
        void DrawStatic(VertexHelper vh,Rect r){
            foreach(var uv in newRegions)XgDraw.Ring(vh,r.min+Vector2.Scale(r.size,uv),7,1,new Color(.3f,.8f,1,.18f));
            if(string.IsNullOrEmpty(lastReaction))return;
            var ink=lastReaction=="wrong"||lastReaction=="knowledge"?new Color(1,.25f,.25f,.8f):lastReaction=="combo"?new Color(1,.75f,.15f,.8f):new Color(.3f,1,.8f,.8f);
            // A stable marker acknowledges the last real action without a looping animation.
            XgDraw.Disc(vh,r.min+Vector2.Scale(r.size,pulseUv),4,ink);
            XgDraw.Box(vh,r.min,new Vector2(r.xMax,r.yMin+2),ink);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++;vh.Clear();var r=rectTransform.rect;float now=Time.unscaledTime;
            if(GamePerformance.Low){DrawStatic(vh,r);return;}
            float p=Mathf.Clamp01(1-(now-pulseAt)/.65f);
            if(p>0){var color=correct?new Color(.25f,1,.75f,p):new Color(1,.2f,.2f,p);var at=r.min+Vector2.Scale(r.size,pulseUv);XgDraw.Disc(vh,at,3+7*p,color);color.a*=.15f;XgDraw.Disc(vh,at,13+10*(1-p),color);}
            float g=Mathf.Clamp01((now-goldAt)/.8f);if(g>0&&g<1){var c=new Color(1,.75f,.15f,Mathf.Sin(g*Mathf.PI));XgDraw.Box(vh,r.min,new Vector2(r.xMax,r.yMin+3),c);XgDraw.Box(vh,new Vector2(r.xMin,r.yMax-3),r.max,c);XgDraw.Box(vh,r.min,new Vector2(r.xMin+3,r.yMax),c);XgDraw.Box(vh,new Vector2(r.xMax-3,r.yMin),r.max,c);}
            float s=Mathf.Clamp01((now-sweepAt)/.7f);if(s>0&&s<1){float y=Mathf.Lerp(r.yMax,r.yMin,s);XgDraw.Box(vh,new Vector2(r.xMin,y-2),new Vector2(r.xMax,y+2),new Color(.3f,1,.85f,.6f));if(concepts!=null)foreach(var l in concepts.Links)XgDraw.Seg(vh,r.min+Vector2.Scale(r.size,l.a),r.min+Vector2.Scale(r.size,l.b),1.7f,new Color(.3f,1,.8f,.25f*(1-s)));}
            for(int i=0;i<newRegions.Count;i++){var at=r.min+Vector2.Scale(r.size,newRegions[i]);float growth=Mathf.SmoothStep(0,1,(now-regionBorn[i])/.8f);XgDraw.Ring(vh,at,7*growth,1,new Color(.3f,.8f,1,.18f));}
            float b=Mathf.Clamp01(1-(now-rebirthAt)/.45f);if(b>0)XgDraw.Box(vh,r.min,r.max,new Color(1,1,1,b*.85f));
        }
    }
}
