using System;
using LingGuangV05.Desktop.Story;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // A computed miniature illustration, not a claim about the player's local LLM weights.
    // Fixed examples never enter the player's training data or autobiographical question log.
    public sealed class XgStageSequenceDemo
    {
        readonly RectTransform root;
        readonly TMP_Text title, explanation, answer;
        readonly TMP_Text[] words = new TMP_Text[6];
        readonly Image[] tiles = new Image[36];
        readonly Image[] bars = new Image[6];
        readonly TMP_Text[] values = new TMP_Text[6];
        readonly float[,] attention = new float[6,6];
        readonly float[] memory = new float[6];
        readonly float[] fading = new float[6];
        int stage, previous = -1;
        bool english, reduced;
        float started;
        static readonly Color Idle = new Color32(26,39,62,255);
        static readonly Color Hot = new Color32(78,206,198,255);
        public bool Visible => root.gameObject.activeSelf;
        public int ActiveToken => previous;
        public float Weight(int query,int key) => attention[query,key];
        public float Remembered(int at) => memory[at];
        public float Ungated(int at) => fading[at];

        public XgStageSequenceDemo(PrologueDesk desk, RectTransform parent)
        {
            root=PrologueDesk.Centered("Sequence Ability Demo",parent,Vector2.zero,new Vector2(896,414));
            title=Text(desk,"Title",new Vector2(0,166),new Vector2(860,48),30);
            explanation=Text(desk,"Explanation",new Vector2(0,119),new Vector2(860,44),18);
            answer=Text(desk,"Result",new Vector2(0,-162),new Vector2(860,64),24);
            for(int i=0;i<6;i++)
            {
                float x=(i-2.5f)*132;
                words[i]=Text(desk,"Token "+i,new Vector2(x,61),new Vector2(128,42),22);
                var bar=PrologueDesk.Centered("Memory "+i,root,new Vector2(x,0),new Vector2(102,18));
                bars[i]=PrologueDesk.Fill(bar,Hot);bars[i].raycastTarget=false;
                values[i]=Text(desk,"Value "+i,new Vector2(x,-44),new Vector2(126,64),17);
                for(int j=0;j<6;j++)
                {
                    var cell=PrologueDesk.Centered("Attention "+i+" "+j,root,new Vector2((j-2.5f)*42,(2.5f-i)*22-17),new Vector2(38,18));
                    tiles[i*6+j]=PrologueDesk.Fill(cell,Idle);tiles[i*6+j].raycastTarget=false;
                }
            }
            root.gameObject.SetActive(false);
        }
        TMP_Text Text(PrologueDesk desk,string name,Vector2 at,Vector2 size,int font)
        {
            var text=desk.Text(PrologueDesk.Centered(name,root,at,size),"",font,Color.white,TextAlignmentOptions.Center);
            text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
        }
        string T(string zh,string en)=>english?en:zh;
        public void Begin(int value,bool en,bool reduceMotion=false)
        {
            stage=value;english=en;reduced=reduceMotion;previous=-1;started=Time.unscaledTime;root.gameObject.SetActive(true);
            string[] zh=stage==4?new[]{"钥匙","在抽屉","吃完饭","洗了碗","准备走","钥匙呢？"}:stage==5?new[]{"我","把","钥匙","放在","蓝色","抽屉"}:new[]{"钥匙","留在","抽屉","它","没有","移动"};
            string[] eng=stage==4?new[]{"The key","in a drawer","Dinner","Dishes","Time to go","Where?"}:stage==5?new[]{"I","put","the key","in","the blue","drawer"}:new[]{"The key","stayed in","the drawer","It","did not","move"};
            for(int i=0;i<6;i++) { words[i].text=en?eng[i]:zh[i];bars[i].gameObject.SetActive(stage<6);values[i].gameObject.SetActive(stage<6); }
            foreach(var tile in tiles)tile.gameObject.SetActive(stage==6);
            title.text=stage==4?T("记住重要的，再往下读","Keep what matters as you read"):stage==5?T("翻译到哪里，就回头看哪里","Look back while translating"):T("让句子里的词彼此看见","Let the words see each other");
            explanation.text=stage==4?T("小型能力演示 · 记忆门保留地点，普通循环逐渐淡忘","Miniature demonstration · a memory gate preserves the location"):
                stage==5?T("小型能力演示 · 亮度是当前译词分给原文的注意力","Miniature demonstration · brightness is attention to source words"):
                T("小型能力演示 · 每一行都是一个词对整句话的注意力","Miniature demonstration · each row is one word's attention");
            // The two paths use the same scalar input, but different recurrent retention.
            float c=0,r=0;
            for(int i=0;i<6;i++) { float input=i==1?1:0;float gate=1/(1+Mathf.Exp(i==1?6:-6));c=gate*c+(1-gate)*input;r=.52f*r+input;memory[i]=c;fading[i]=r; }
            // Scaled-dot-product attention, computed and normalized. Features are deliberately
            // tiny semantic/positional vectors, not pretend values read from a trained model.
            var keys=new float[6,6];var queries=new float[6,6];
            for(int i=0;i<6;i++){keys[i,i]=1;queries[i,i]=4;}
            if(stage==6){queries[3,0]+=6;queries[5,0]+=6;queries[2,1]+=2;}
            for(int q=0;q<6;q++)
            {
                float sum=0;
                for(int k=0;k<6;k++)
                {
                    float dot=0;for(int feature=0;feature<6;feature++)dot+=queries[q,feature]*keys[k,feature];
                    attention[q,k]=Mathf.Exp(dot/Mathf.Sqrt(6));sum+=attention[q,k];
                }
                for(int k=0;k<6;k++)attention[q,k]/=sum;
            }
            Tick();
        }
        public void Tick()
        {
            if(!Visible)return;
            int at=reduced?5:Mathf.Min(5,(int)((Time.unscaledTime-started)/1.1f));
            if(at==previous)return;previous=at;
            for(int i=0;i<6;i++)
            {
                words[i].color=i==at?Hot:Color.white;
                if(stage==4)
                {
                    float v=i<=at?memory[i]:0;
                    bars[i].rectTransform.sizeDelta=new Vector2(102*v,18);
                    values[i].text=i<=at?T("保留 ","Kept ")+Mathf.RoundToInt(v*100)+"%\n"+T("淡忘 ","Fading ")+Mathf.RoundToInt(fading[i]*100)+"%":"";
                }
                else if(stage==5)
                {
                    bars[i].rectTransform.sizeDelta=new Vector2(102*attention[at,i],18);
                    values[i].text=Mathf.RoundToInt(attention[at,i]*100)+"%";
                }
                for(int j=0;j<6;j++)tiles[i*6+j].color=Color.Lerp(Idle,Hot,i<=at?attention[i,j]:0);
            }
            if(stage==4)answer.text=at<5?T("话还在继续，地点没有丢。","The sentence continues. The location stays."):T("钥匙还在抽屉里。","The key is still in the drawer.");
            else if(stage==5)
            {
                string[] output={"I","put","the key","in","the blue","drawer."};
                answer.text="";for(int i=0;i<=at;i++)answer.text+=(i==0?"":" ")+output[i];
            }
            else answer.text=at<3?T("每个词都能直接看向整句话。","Every word can look across the whole sentence."):T("「它」看向「钥匙」：没有忘掉在说谁。","“It” attends to “the key”: the reference is kept.");
        }
        public void End(){root.gameObject.SetActive(false);}
    }
}
