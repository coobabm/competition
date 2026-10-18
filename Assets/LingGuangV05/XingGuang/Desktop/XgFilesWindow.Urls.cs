using System;
using System.Globalization;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgFilesWindow
    {
        TMP_InputField dataUrlInput;
        TMP_Text dataUrlClue,dataUrlHint,dataUrlPlaceholder,dataUrlDownloadLabel,dataUrlSourceLabel;
        UnityEngine.UI.Button dataUrlDownload;
        string submittedUrlMessage="";
        float submittedUrlUntil;
        bool submittedUrlEnglish;
        void BuildDataUrl()
        {
            var panel=Rect("Game data address",client,new Vector2(0,1),Vector2.one,new Vector2(8,-162),new Vector2(-8,-48));
            dataUrlSourceLabel=Button(panel,"MNIST source",new Vector2(0,86),new Vector2(108,28),()=>
            {
                dataUrlInput.text=XgSim.MnistSourceUrl;
                dataUrlInput.Select();dataUrlInput.ActivateInputField();
            });
            dataUrlClue=Text(Rect("Source clue",panel,new Vector2(0,1),Vector2.one,new Vector2(116,-28),new Vector2(0,0)),"",13);
            dataUrlClue.richText=false;
            var entry=Rect("Address",panel,new Vector2(0,1),Vector2.one,new Vector2(0,-66),new Vector2(-118,-32));
            var background=entry.gameObject.AddComponent<Image>();background.color=Color.white;
            dataUrlInput=entry.gameObject.AddComponent<TMP_InputField>();dataUrlInput.targetGraphic=background;
            var viewport=Rect("Viewport",entry,Vector2.zero,Vector2.one,new Vector2(8,3),new Vector2(-8,-3));viewport.gameObject.AddComponent<RectMask2D>();
            var text=Text(Rect("Text",viewport,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",14);text.richText=false;
            dataUrlPlaceholder=Text(Rect("Placeholder",viewport,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero),"",14);dataUrlPlaceholder.color=new Color32(105,120,135,255);
            dataUrlInput.textViewport=viewport;dataUrlInput.textComponent=text;dataUrlInput.placeholder=dataUrlPlaceholder;
            dataUrlInput.fontAsset=font;dataUrlInput.pointSize=14;dataUrlInput.lineType=TMP_InputField.LineType.SingleLine;
            dataUrlInput.characterLimit=256;dataUrlInput.richText=false;dataUrlInput.contentType=TMP_InputField.ContentType.Standard;
            dataUrlInput.onValueChanged.AddListener(_=>{submittedUrlUntil=0;RefreshDataUrl();});
            dataUrlDownloadLabel=Button(panel,"Download address",new Vector2(-110,48),new Vector2(110,34),SubmitDataUrl,true);
            dataUrlDownload=dataUrlDownloadLabel.transform.parent.GetComponent<UnityEngine.UI.Button>();
            dataUrlHint=Text(Rect("Address result",panel,Vector2.zero,Vector2.right,new Vector2(0,0),new Vector2(0,44)),"",13);
            dataUrlHint.richText=false;dataUrlHint.textWrappingMode=TextWrappingModes.Normal;
            RefreshDataUrl();
        }
        void ResetDataUrl()
        {
            submittedUrlMessage="";submittedUrlUntil=0;
            if(dataUrlInput!=null){dataUrlInput.DeactivateInputField();dataUrlInput.SetTextWithoutNotify("");}
            RefreshDataUrl();
        }
        void RefreshDataUrl()
        {
            if(dataUrlInput==null||Sim==null)return;
            dataUrlSourceLabel.text=T("MNIST 地址","MNIST source");
            dataUrlClue.text=Sim.DataUrlClue("mnist");
            dataUrlPlaceholder.text=T("输入游戏内的数据网址","Enter an in-game data address");
            dataUrlDownloadLabel.text=T("模拟下载","Download");
            var owner=XingGuangController.Instance;
            if(owner==null||!ReferenceEquals(owner.Sim,Sim)){dataUrlDownload.interactable=false;return;}
            var state=Sim.PreviewDataUrl(dataUrlInput.text,owner.Host);
            dataUrlDownload.interactable=state.canDownload;
            if(submittedUrlUntil>Time.unscaledTime&&submittedUrlEnglish==Sim.English)dataUrlHint.text=submittedUrlMessage;
            else dataUrlHint.text=string.IsNullOrWhiteSpace(dataUrlInput.text)
                ?T("游戏内模拟下载；点击上方 MNIST 地址可填入。","Simulated game download; use MNIST source above to fill the address.")
                :state.message;
        }
        void SubmitDataUrl()
        {
            var owner=XingGuangController.Instance;
            if(Sim==null||owner==null||!ReferenceEquals(owner.Sim,Sim))return;
            // Revalidate price/ownership/gate in core at the actual click, never reuse the preview.
            var state=Sim.DownloadDataUrl(dataUrlInput.text,owner.Host);
            submittedUrlMessage=state.message;submittedUrlEnglish=Sim.English;submittedUrlUntil=Time.unscaledTime+3;
            if(state.status==XgDataUrlStatus.Started){folder="packs";previewId=null;selected.Clear();}
            Refresh();RefreshDataUrl();
        }
        static string DownloadSignature(XgDownloadInfo file)
        {
            return file.fileId+":"+Math.Floor(file.progress*100).ToString(CultureInfo.InvariantCulture)
                +":"+Math.Ceiling(file.eta).ToString(CultureInfo.InvariantCulture)+":"+file.paused;
        }
        void AddQueueRow(int index,XgDownloadInfo file)
        {
            string name=Sim.English?file.nameEn:file.name;
            string percent=Math.Floor(Math.Max(0,Math.Min(1,file.progress))*100).ToString(CultureInfo.InvariantCulture)+"%";
            string timing=file.paused?T("已暂停","Paused")
                :T("模拟下载 · 预计 ","Simulated download · about ")+Math.Ceiling(Math.Max(0,file.eta)).ToString(CultureInfo.InvariantCulture)+T(" 秒"," s");
            AddRow(index,"queue:"+file.fileId,name+"  ·  "+percent,name+"\n"+percent+"\n"+timing+"\n"+XgSim.SizeText(file.doneMB)+" / "+XgSim.SizeText(file.sizeMB),false,false);
            var row=(RectTransform)rows.GetChild(rows.childCount-1);
            var track=Rect("Download progress",row,Vector2.zero,Vector2.right,new Vector2(12,2),new Vector2(-12,5));
            var bg=track.gameObject.AddComponent<Image>();bg.color=new Color32(226,235,242,255);bg.raycastTarget=false;
            var fill=Rect("Progress",track,Vector2.zero,new Vector2((float)Math.Max(0,Math.Min(1,file.progress)),1),Vector2.zero,Vector2.zero);
            var image=fill.gameObject.AddComponent<Image>();image.color=new Color32(69,154,105,255);image.raycastTarget=false;
        }
    }
}
