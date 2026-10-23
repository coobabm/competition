using System;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgAppAcquisitionHost
    {
        float nextSampleHelp;
        RectTransform attention,downloadProgress,installWizard;Image attentionImage;float attentionSeconds;
        void CloseAttention(){Dispose(ref attention);attentionImage=null;attentionSeconds=0;}
        void Attention()
        {
            var button=chat.router?.Get(YYChatHub.AppId)?.TaskbarShortcut?.transform as RectTransform;
            if(button==null||!button.gameObject.activeInHierarchy)return;
            attentionSeconds=4;
            if(attention!=null)return;
            attention=Rect("GroupIntroAttention",button,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            attention.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
            attentionImage=Panel(attention,Color.clear);attentionImage.raycastTarget=false;
        }
        void TickAttention(bool held)
        {
            if(attention==null)return;
            if(chat.IsShowing(YYChatHub.EraGroup)||attentionSeconds<=0){CloseAttention();return;}
            attention.gameObject.SetActive(!held);
            if(held)return;
            attentionSeconds-=Time.unscaledDeltaTime;
            attentionImage.color=new Color(1,.65f,.18f,.45f*Mathf.PingPong(attentionSeconds,1));
        }
        static void Dispose(ref RectTransform panel)
        {var old=panel;panel=null;if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
        void CloseThought(){if(thought!=null)thoughtGeneration++;Dispose(ref thought);thoughtKey="";}
        void CloseDownload(){if(downloadPage!=null)downloadGeneration++;Dispose(ref downloadPage);installWizard=null;downloadApp="";}
        /// <param name="groupButton">Add a 「看看群聊」 next to 「继续」: only when the step names no button of its own and is not the script's last thought.</param>
        void ShowThought(string key,string body,string button,Action next,bool optional=false,bool groupButton=false)
        {
            if(Held||optional&&Time.unscaledTime<nextSampleHelp)return;
            key+="|"+Revision+"|"+GameText.LanguageId;
            if(thought!=null&&thoughtKey==key)
            {return;}
            var parent=PrologueDirector.Desk?.layer;if(parent==null)return;
            CloseThought();thoughtKey=key;
            thought=Rect("GroupIntroThought",parent,new Vector2(.2f,0),new Vector2(.8f,0),new Vector2(0,56),new Vector2(0,230));
            Panel(thought,new Color(.08f,.1f,.14f,.93f));var ui=new XgUi(owner.Font,null);
            ui.Text(Rect("Me",thought,new Vector2(0,1),Vector2.one,new Vector2(16,-30),new Vector2(-16,-6)),GameText.T("我","Me"),13,new Color(.66f,.8f,1),TextAlignmentOptions.TopLeft);
            thoughtText=ui.Text(Rect("Thought",thought,Vector2.zero,Vector2.one,new Vector2(16,52),new Vector2(-16,-34)),body,17,Color.white,TextAlignmentOptions.TopLeft);
            thoughtText.richText=false;thoughtText.maxVisibleCharacters=0;typed=0;
            var panel=thought;int generation=thoughtGeneration,revision=Revision;object expectedSave=save,expectedState=lab.S;
            thoughtNext=ui.Button(thought,string.IsNullOrEmpty(button)?GameText.T("继续","Continue"):button,()=>
            {
                if(!Stamp(expectedSave,expectedState,revision)||generation!=thoughtGeneration||panel!=thought||!panel.gameObject.activeInHierarchy)return;
                if(typed<body.Length){typed=body.Length;thoughtText.maxVisibleCharacters=body.Length;return;}
                next();
            },15);
            Position(thoughtNext,.55f,1,10,44);
            if(optional)
            {
                var later=ui.Button(thought,GameText.T("稍后再问","Ask later"),()=>
                {if(!Stamp(expectedSave,expectedState,revision)||generation!=thoughtGeneration||panel!=thought)return;nextSampleHelp=Time.unscaledTime+90;CloseThought();},14);
                Position(later,0,.5f,10,44);
            }
            else if(groupButton)
            {
                var open=ui.Button(thought,GameText.T("看看群聊","Check the group"),()=>
                {if(Stamp(expectedSave,expectedState,revision)&&generation==thoughtGeneration&&panel==thought)chat.router?.Open(YYChatHub.AppId,YYChatHub.EraGroup);},14);
                Position(open,0,.5f,10,44);
            }
        }
        static void Position(XgBtn b,float left,float right,float bottom,float top)
        {b.rt.anchorMin=new Vector2(left,0);b.rt.anchorMax=new Vector2(right,0);b.rt.offsetMin=new Vector2(12,bottom);b.rt.offsetMax=new Vector2(-12,top);}
        void OpenInstalledApp(string appId)
        {
            if(appId=="miaoyu")
            {LingGuangV05.Desktop.Miaoyu.MiaoyuView.Instance?.Open("used");return;}
            chat.router?.Open(XgSim.DesktopRoute(appId));
        }
        /// <summary>The player already followed this app's group link (download started, installed, or the games window opened).</summary>
        public bool LinkFollowed(string appId)
        {
            if(!Current||string.IsNullOrEmpty(appId))return false;
            if(appId=="gamecenter")return Data.gamesOpened;
            var r=lab.S.appAcquisition.apps.Find(a=>a.id==appId);return r!=null&&(r.downloading||r.downloaded||r.installed);
        }
        public void OpenLink(string appId)
        {
            if(!Current||Held||apps==null)return;var receipt=lab.AppReceipt(appId);if(receipt==null||!receipt.offered)return;
            if(appId=="gamecenter")
            {
                foreach(var w in DesktopWindowIndex.Get(owner.runtime))if(w.name=="Game Hub")
                {w.OpenWindow();if(w.isOn)Commit(()=>Data.gamesOpened=true);return;}
                return;
            }
            if(receipt.installed){OpenInstalledApp(appId);return;}
            var app=apps.apps.Find(x=>x.id==appId);if(app==null||browserWindow?.windowContainer==null)return;
            browserWindow.OpenWindow();CloseDownload();downloadApp=appId;
            // A small local client page inside the existing native browser; closing restores its normal content.
            downloadPage=Rect("GroupAppDownloadPage",browserWindow.windowContainer,new Vector2(0,0),new Vector2(1,1),new Vector2(12,20),new Vector2(-12,-86));
            Panel(downloadPage,new Color32(245,248,252,255));var ui=new XgUi(owner.Font,browserWindow);
            string name=GameText.T(app.name,app.nameEn);
            ui.Text(Rect("Address",downloadPage,new Vector2(0,1),Vector2.one,new Vector2(22,-40),new Vector2(-22,-8)),"https://"+appId+".com/client",14,new Color32(42,104,170,255),TextAlignmentOptions.TopLeft).richText=false;
            ui.Text(Rect("Title",downloadPage,new Vector2(0,1),Vector2.one,new Vector2(28,-105),new Vector2(-28,-52)),name+GameText.T(" · 客户端下载"," · Client download"),27,new Color32(30,50,74,255),TextAlignmentOptions.TopLeft).richText=false;
            downloadText=ui.Text(Rect("Status",downloadPage,Vector2.zero,Vector2.one,new Vector2(28,110),new Vector2(-28,-130)),"",18,new Color32(50,65,80,255),TextAlignmentOptions.TopLeft);downloadText.richText=false;
            var track=Rect("ProgressTrack",downloadPage,Vector2.zero,Vector2.right,new Vector2(28,88),new Vector2(-28,100));
            Panel(track,new Color32(210,222,236,255)).raycastTarget=false;
            downloadProgress=Rect("Progress",track,Vector2.zero,Vector2.up,Vector2.zero,Vector2.zero);
            Panel(downloadProgress,new Color32(48,131,211,255)).raycastTarget=false;
            var panel=downloadPage;object expectedSave=save,expectedState=lab.S;int generation=downloadGeneration;
            downloadAction=ui.Button(downloadPage,"",()=>
            {
                if(!Current||Held||!ReferenceEquals(save,expectedSave)||!ReferenceEquals(lab.S,expectedState)||generation!=downloadGeneration||panel!=downloadPage||!panel.gameObject.activeInHierarchy)return;
                var live=lab.AppReceipt(appId);if(live.installed){CloseDownload();OpenInstalledApp(appId);return;}
                if(!live.downloaded){if(browser?.networkManager?.isConnected==true)Commit(()=>lab.StartDesktopDownload(appId,apps));}
                else Commit(()=>lab.AdvanceDesktopInstall(appId));
                TickDownload();
            },17);Position(downloadAction,.45f,1,24,72);
            var close=ui.Button(downloadPage,GameText.T("返回浏览器","Back to browser"),()=>
            {if(Current&&ReferenceEquals(save,expectedSave)&&ReferenceEquals(lab.S,expectedState)&&generation==downloadGeneration&&panel==downloadPage)CloseDownload();},15);Position(close,0,.4f,24,72);
            TickDownload();
        }
        void TickDownload()
        {
            if(downloadPage==null)return;
            if(!Current||browserWindow==null||!browserWindow.isOn){CloseDownload();return;}
            downloadPage.gameObject.SetActive(!Held);if(Held)return;
            var app=apps.apps.Find(x=>x.id==downloadApp);var r=lab.AppReceipt(downloadApp);if(app==null||r==null){CloseDownload();return;}
            if(r.downloaded&&installWizard==null)
            {
                installWizard=Rect("ClientInstallWizard",downloadPage,new Vector2(.15f,.15f),new Vector2(.85f,.85f),Vector2.zero,Vector2.zero);
                Panel(installWizard,new Color32(231,239,249,255));
                var ui=new XgUi(owner.Font,browserWindow);
                ui.Text(Rect("Heading",installWizard,new Vector2(0,1),Vector2.one,new Vector2(20,-62),new Vector2(-20,-16)),
                    GameText.T(app.name,app.nameEn)+GameText.T(" · 安装向导"," · Setup"),22,new Color32(30,50,74,255),TextAlignmentOptions.TopLeft).richText=false;
                downloadText.rectTransform.SetParent(installWizard,false);downloadText.rectTransform.anchorMin=Vector2.zero;downloadText.rectTransform.anchorMax=Vector2.one;
                downloadText.rectTransform.offsetMin=new Vector2(20,84);downloadText.rectTransform.offsetMax=new Vector2(-20,-76);
                downloadAction.rt.SetParent(installWizard,false);Position(downloadAction,.4f,1,18,64);
            }
            bool online=browser?.networkManager?.isConnected==true;
            if(downloadProgress!=null)downloadProgress.anchorMax=new Vector2(app.sizeMB>0?(float)Math.Min(1,r.receivedMB/app.sizeMB):0,1);
            string progress=r.receivedMB.ToString("0.0")+" / "+app.sizeMB.ToString("0.0")+" MB";
            downloadText.text=r.installed?GameText.T("安装完成。桌面图标和待办已更新。","Installed. The desktop icon and to-do list have been updated."):
                r.downloaded?GameText.T("下载完成。请依次点击：下一步 → 安装 → 完成。","Download complete. Click Next → Install → Finish.")+"\n\n"+GameText.T("安装步骤 ","Installation step ")+(r.installStep+1)+" / 3":
                r.downloading?progress+"\n"+(online?"ADSL · 2.5 MB/s":GameText.T("网络断开，下载已暂停。","Disconnected. Download paused.")):
                app.sizeMB.ToString("0.0")+" MB\nADSL · 2.5 MB/s\n"+GameText.T("从群友分享的页面下载客户端。","Download the client from the page shared in the group.");
            string label=r.installed?GameText.T("打开","Open"):r.downloaded?
                r.installStep==0?GameText.T("下一步","Next"):r.installStep==1?GameText.T("安装","Install"):GameText.T("完成","Finish"):
                r.downloading?GameText.T("下载中…","Downloading…"):GameText.T("下载","Download");
            downloadAction.Set(label,r.installed||r.downloaded||!r.downloading&&online);
        }
    }
}
