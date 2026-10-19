using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // Same deterministic miniature lessons as the stage presentation, in a separate instance.
    // No XgSim, runtime, wallet, Unity Random or event subscription is reachable from this class.
    public sealed class XgAtlasTinyDemo
    {
        readonly RectTransform root;
        readonly XgTinyPixels[] pictures = new XgTinyPixels[4], weights = new XgTinyPixels[4];
        readonly TMP_Text[] answers = new TMP_Text[4];
        readonly TMP_Text title, status, footer;
        readonly XgTinyCurve curve;
        XgTinyRun run;
        bool english, reduced;
        int stage, phase;
        float elapsed, budget;
        public int Epoch => run == null ? 0 : run.Epoch;
        public bool Showing => root.gameObject.activeSelf;
        public XgAtlasTinyDemo(XgUi ui, RectTransform parent)
        {
            root = Rect("Miniature ability replay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            title = ui.Text(Strip("Title", root, 4, 40, 12, 12), "", 19, XgDark.Ink, TextAlignmentOptions.Center);
            status = ui.Text(Strip("Status", root, 46, 38, 12, 12), "", 15, XgDark.Muted, TextAlignmentOptions.Center);
            curve = Rect("Computed error", root, new Vector2(.26f,.26f), new Vector2(.74f,.72f), Vector2.zero, Vector2.zero).gameObject.AddComponent<XgTinyCurve>();
            curve.raycastTarget = false; curve.GridColor = XgDark.Line;
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                pictures[i] = Rect("Input " + i, root, new Vector2(col*.12f, .5f-row*.24f), new Vector2((col+1)*.12f, .72f-row*.24f), new Vector2(4,18), new Vector2(-4,-4)).gameObject.AddComponent<XgTinyPixels>();
                pictures[i].Dark = true; pictures[i].raycastTarget = false;
                answers[i] = ui.Text(Rect("Answer " + i, root, new Vector2(col*.12f,.5f-row*.24f),new Vector2((col+1)*.12f,.5f-row*.24f),new Vector2(0,-2),new Vector2(0,18)),"",13,XgDark.Ink,TextAlignmentOptions.Center);
                weights[i] = Rect("Weight " + i, root, new Vector2(.76f+col*.12f,.5f-row*.24f),new Vector2(.88f+col*.12f,.72f-row*.24f),new Vector2(4,4),new Vector2(-4,-4)).gameObject.AddComponent<XgTinyPixels>();
                weights[i].Dark = true; weights[i].Signed = true; weights[i].raycastTarget = false;
            }
            footer = ui.Text(Rect("Explanation",root,Vector2.zero,new Vector2(1,.22f),new Vector2(12,4),new Vector2(-12,-4)),"",16,XgDark.Ink,TextAlignmentOptions.Center);
            footer.textWrappingMode = TextWrappingModes.Normal;
            root.gameObject.SetActive(false);
        }
        string T(string zh,string en) => english ? en : zh;
        public void Begin(int ability, bool en, bool reduce)
        {
            stage=ability; english=en; reduced=reduce; phase=0; elapsed=budget=0;
            root.gameObject.SetActive(true);
            Start(stage==3 ? XgTinyLesson.Zero : XgTinyLesson.XorPerceptron);
            // Reduced motion has no timed reveal; both stage-2 curves remain visible for comparison.
            if(reduced && stage==2) { phase=1; Start(XgTinyLesson.XorMlp); }
        }
        void Start(XgTinyLesson lesson)
        {
            run=new XgTinyRun(lesson,XgTinyRun.Seed);
            var reference=lesson==XgTinyLesson.XorMlp ? new XgTinyRun(XgTinyLesson.XorPerceptron,XgTinyRun.Seed).Finish().loss : null;
            if(reduced) run.Finish();
            curve.Set(run.loss,reference,1+run.epochs*(run.net.IsPerceptron ? run.train.Count : 1),.75f);
            title.text=lesson==XgTinyLesson.XorPerceptron ? T("单层网络：一条线的局限","One layer: the limit of one line") : lesson==XgTinyLesson.XorMlp ? T("隐藏层把两条线索组合起来","Hidden layers combine two clues") : T("从小图里认出「0」","Recognising zero in small pictures");
            footer.text=lesson==XgTinyLesson.XorMlp ? T("灰线是单层网络，橙线是多层网络的实算误差。","Grey: one layer. Orange: the computed error of a multilayer network.") : T("左边是8×8输入，中间是实算误差，右边是权重。固定小模型演示，不是你的聊天模型权重。","8×8 inputs, computed error and weights. A fixed miniature example, not your chat model's weights.");
            Refresh();
        }
        public void Tick(float dt, bool reduce)
        {
            if(!Showing || run==null)return;
            if(reduce!=reduced) { Begin(stage,english,reduce); return; }
            if(reduced)return;
            elapsed+=dt;
            if(!run.Done)
            {
                budget+=dt*run.epochs/4.5f;int passes=Mathf.FloorToInt(budget);
                if(passes>0){budget-=passes;run.Step(passes);curve.Changed();Refresh();}
            }
            if(stage==2 && phase==0 && elapsed>=5.3f && run.Done){phase=1;budget=0;Start(XgTinyLesson.XorMlp);}
        }
        void Refresh()
        {
            var samples=stage==3 ? run.test : run.train;
            for(int i=0;i<4;i++)
            {
                var sample=samples[i]; pictures[i].Values=sample.pixels;
                bool yes=run.net.Predict(sample.pixels);
                answers[i].text=(yes ? T("是","Yes") : T("否","No"))+(yes==sample.label?" ✓":" ×");
                bool present=i<run.net.Units;weights[i].gameObject.SetActive(present);
                if(present)weights[i].Values=run.net.UnitWeights(i);
            }
            status.text=T("小模型演示 · ","Miniature example · ")+run.Epoch+" / "+run.epochs+T(" 轮 · 橙线：误差"," epochs · orange: error");
        }
        public void End(){run=null;root.gameObject.SetActive(false);}
    }
}
