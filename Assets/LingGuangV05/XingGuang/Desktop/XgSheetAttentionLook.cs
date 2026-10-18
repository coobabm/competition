using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Original question image plus measured occlusion on a clearly identified demonstration network.</summary>
    public sealed class XgSheetAttentionLook : MonoBehaviour
    {
        XgUi ui; RawImage source; TMP_Text title,caption; RectTransform imageBox;
        readonly Image[] cells=new Image[49]; XgSheetItem item; string signature;
        public bool Showing=>gameObject.activeSelf;
        public XgSheetItem Item=>item;
        public static XgSheetAttentionLook Create(RectTransform parent,XgUi ui,XgJuice fx)
        {
            var rt=Rect("OriginalQuestionAttention",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var view=rt.gameObject.AddComponent<XgSheetAttentionLook>();view.ui=ui;view.Build(rt);rt.gameObject.SetActive(false);return view;
        }
        void Build(RectTransform rt)
        {
            Panel(rt,XgDark.Card);
            title=ui.Text(Rect("Title",rt,new Vector2(0,1),Vector2.one,new Vector2(14,-42),new Vector2(-90,-10)),"",16,XgDark.Ink,TextAlignmentOptions.MidlineLeft);
            var close=ui.Button(rt,T("收起","Close"),Hide,12);PlaceTopRight(close,76,10,68,25);
            imageBox=Rect("OriginalImage",rt,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-100,-70),new Vector2(100,130));
            source=imageBox.gameObject.AddComponent<RawImage>();source.raycastTarget=false;
            for(int i=0;i<49;i++)
            {
                int x=i%7,y=i/7;
                var cell=Rect("Occlusion"+i,imageBox,new Vector2(x/7f,1-(y+1)/7f),new Vector2((x+1)/7f,1-y/7f),Vector2.zero,Vector2.zero);
                cells[i]=Panel(cell,Color.clear);cells[i].raycastTarget=false;
            }
            caption=ui.Text(Rect("Explanation",rt,Vector2.zero,new Vector2(1,0),new Vector2(14,10),new Vector2(-14,100)),"",12,XgDark.Muted,TextAlignmentOptions.TopLeft);
        }
        public void Toggle(XgSim sim,XgSheetItem value)
        {
            if(Showing&&item==value){Hide();return;}
            item=value;signature=null;gameObject.SetActive(true);transform.SetAsLastSibling();Render(sim);
        }
        public void Hide(){gameObject.SetActive(false);item=null;signature=null;}
        public void Sync(XgSim sim,List<XgSheetItem> sheet)
        {
            if(!isActiveAndEnabled)return;
            if(item==null||sheet==null||!sheet.Contains(item)){Hide();return;}Render(sim);
        }
        void Render(XgSim sim)
        {
            if(!isActiveAndEnabled||item==null)return;
            var card=sim.SheetCard(item)?.source;
            string arch=sim.S.vision.arch;
            string key=arch+":"+(LingGuangV05.Core.Lang.IsEnglish?.Invoke()??false)+":"+item.seed+":"+item.dataset+":"+GamePerformance.Low;
            if(signature==key)return;signature=key;
            var texture=XgTeachingPictures.ForCard(card);source.texture=texture;source.enabled=texture!=null;
            foreach(var cell in cells)cell.color=Color.clear;
            title.text=T("看它学 · 原题图","Watch it learn · original image");
            if(texture==null){caption.text=T("这道题没有保存可重现的原图。","This question has no saved, reproducible image.");return;}
            if(GamePerformance.Low){caption.text=T("低画质下显示原题图片。切回高画质可查看热力图。","Original image shown in low graphics mode. Switch to high graphics to view the heatmap.");return;}
            var result=XgSheetOcclusion.For(card,arch);
            if(result==null)
            {
                caption.text=T("这是原题图片。当前没有与这类题匹配的演示分类器，暂不显示热力图。","This is the original question image. No matching demonstration classifier is available for this question type, so no heatmap is shown.");return;
            }
            double max=0;foreach(double value in result.change)max=System.Math.Max(max,System.Math.Abs(value));
            for(int i=0;i<49;i++){float alpha=max>1e-9?(float)(System.Math.Abs(result.change[i])/max)*.72f:0;cells[i].color=result.change[i]>=0?new Color(1,.18f,.06f,alpha):new Color(.08f,.42f,1,alpha);}
            string model=result.model=="cnn"?"CNN":result.model=="mlp"?"MLP":T("感知机","Perceptron");
            caption.text=T("逐块遮住原图的 2×2 像素，共 49 次。红：遮住后目标分数下降；蓝：上升。\n演示网络：","Mask a 2×2 area at each of 49 positions. Red: masking lowers the target score; blue: raises it.\nDemo network: ")+model+T(" · 目标数字 "," · target digit ")+card.asked+T("\n这是小网络的实算结果，不是聊天模型的视觉推理，也不决定答卷对错。","\nMeasured on the small demo network; not the chat model's visual reasoning, and does not determine the sheet answer.");
        }
    }
}
