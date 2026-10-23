using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using LingGuangV05.Desktop.Apps;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    [DisallowMultipleComponent]
    public sealed partial class XgAppAcquisitionHost : MonoBehaviour
    {
        public static XgAppAcquisitionHost Instance {get;private set;}
        XingGuangController owner;ChapterOneSim save;XgSim lab;YYChatHub chat;
        XgAppCatalog apps;XgGroupIntroCatalog catalog;
        WindowManager settings,browserWindow,gameWindow;WebBrowserApp browser;
        XgEndingPreparationHost endingPreparation;
        float nextCheck,nextCheckpoint,nextLookup,nextSaveAttempt;double lineWait;bool saving;
        RectTransform thought,downloadPage;TMP_Text thoughtText,downloadText;
        string thoughtKey="",downloadApp="";int thoughtGeneration,downloadGeneration;float typed;
        XgBtn thoughtNext,downloadAction;
        public static void Install(XingGuangController controller)
        {
            var host=controller.GetComponent<XgAppAcquisitionHost>()??controller.gameObject.AddComponent<XgAppAcquisitionHost>();
            host.owner=controller;host.endingPreparation=controller.GetComponent<XgEndingPreparationHost>();Instance=host;host.Bind();
        }
        public bool Current=>owner!=null&&owner.LifeEventSettingsAvailable&&ReferenceEquals(save,owner.runtime?.Sim)
            &&ReferenceEquals(lab,owner.Sim)&&owner.BoundTo(save)&&lab?.S.appAcquisition?.version==1
            &&chat!=null&&ReferenceEquals(chat.runtime?.Sim,save)&&chat.EndingBoundTo(save);
        XgAppAcquisitionState Data=>lab.S.appAcquisition;
        public bool Playing=>Current&&!string.IsNullOrEmpty(Data.activeScript);
        public int Revision=>Current?Data.revision:-1;
        public bool Held=>!Current||StoryFocus.Active||InnerVoice.Busy||PlayerWindowState.IsMinimized||lab.StagePresentationHeld||XgLifeHostActivity.GamePaused
            ||XgLifeHostActivity.ReadingWindowVisible||SaveSlotsPanel.IsOpen||lab.LifeEventOpen||lab.LifeEventResultPending
            ||(settings!=null&&settings.isOn)||(owner.Epilogue!=null&&owner.Epilogue.HoldingSimulation)
            ||(endingPreparation!=null&&endingPreparation.Showing)||XgEpilogueFlow.HasCommittedRelease(lab.S)||lab.S.chapterComplete;
        XgGroupIntroScript Script=>Playing?catalog?.scripts.Find(s=>s.id==Data.activeScript):null;
        XgGroupIntroProgress Progress=>Playing?lab.IntroProgress(Data.activeScript):null;
        XgGroupIntroStep Step
        {get{var s=Script;var p=Progress;return s!=null&&p!=null&&p.step>=0&&p.step<s.steps.Count?s.steps[p.step]:null;}}
        void Bind()
        {
            if(owner==null)return;
            if(!ReferenceEquals(save,owner.runtime?.Sim)||!ReferenceEquals(lab,owner.Sim))
            {CloseThought();CloseDownload();CloseAttention();save=owner.runtime?.Sim;lab=owner.Sim;lineWait=0;nextSampleHelp=0;nextCheck=nextCheckpoint=0;}
            chat=YYChatHub.Instance;
            if(apps==null){var t=Resources.Load<TextAsset>("LingGuangV05/Data/app_unlocks");if(t!=null)apps=JsonUtility.FromJson<XgAppCatalog>(t.text);}
            if(catalog==null){var t=Resources.Load<TextAsset>("LingGuangV05/Data/group_intro_scripts");if(t!=null)catalog=JsonUtility.FromJson<XgGroupIntroCatalog>(t.text);}
        }
        void LookupWindows()
        {
            if(Time.unscaledTime<nextLookup||owner?.runtime==null)return;nextLookup=Time.unscaledTime+1;
            if(endingPreparation==null)endingPreparation=owner.GetComponent<XgEndingPreparationHost>();
            foreach(var w in DesktopWindowIndex.Get(owner.runtime))
            {
                if(w.name=="Settings")settings=w;
                if(w.name=="Game Hub")gameWindow=w;
                if(browser==null){var candidate=w.GetComponentInChildren<WebBrowserApp>(true);if(candidate!=null){browser=candidate;browserWindow=w;}}
            }
        }
        bool Stamp(object expectedSave,object expectedState,int revision)
            =>Current&&!Held&&ReferenceEquals(save,expectedSave)&&ReferenceEquals(lab.S,expectedState)&&Data.revision==revision;
        bool Commit(Action action)
        {
            if(!Current||saving||Time.unscaledTime<nextSaveAttempt||!owner.runtime.useDiskSave||owner.runtime.TestMode||save.InPrologue)return false;
            saving=true;var oldData=JsonUtility.ToJson(Data);var conv=chat.Conversation(YYChatHub.EraGroup);
            var oldMessages=conv.messages;int unread=conv.unread;string labJson=save.S.labState,chatJson=save.S.chatState;
            conv.messages=new List<YYMessage>(oldMessages);bool success=false;
            try{action();Data.revision++;owner.runtime.MarkDirty();success=owner.SaveNow();return success;}
            catch(Exception error){Debug.LogWarning("Group introduction save failed: "+error.GetType().Name);return false;}
            finally
            {
                saving=false;
                if(!success){lab.S.appAcquisition=JsonUtility.FromJson<XgAppAcquisitionState>(oldData);conv.messages=oldMessages;conv.unread=unread;save.S.labState=labJson;save.S.chatState=chatJson;
                    nextSaveAttempt=Time.unscaledTime+1;CloseThought();owner.View?.ShowToast(GameText.T("保存失败，请重试。","Saving failed. Please retry."),4);}
                try
                {
                    chat.NotifyEndingProjectionSaved();
                    if(success&&conv.messages.Count>oldMessages.Count&&conv.messages[conv.messages.Count-1].from!=YYChatHub.Me)
                    {
                        var expected=save;var line=conv.messages[conv.messages.Count-1];
                        if(!chat.IsShowing(YYChatHub.EraGroup))Attention();
                        LingGuangV05.Desktop.Media.DesktopNotifications.Notify(owner.runtime,"YY",GameText.T("下班屁股群","Clock-out Overwatch squad"),line.text,
                            chat.IsShowing(YYChatHub.EraGroup),()=>{if(Current&&ReferenceEquals(save,expected))chat.router?.Open(YYChatHub.AppId,YYChatHub.EraGroup);},aiReply:true);
                    }
                }catch(Exception error){Debug.LogWarning("Group introduction redraw failed after save: "+error.GetType().Name);}
            }
        }
        void Append(string text,bool player=false,string app="",string labelZh="",string labelEn="")
        {
            if(string.IsNullOrWhiteSpace(text)&&string.IsNullOrEmpty(app))return;
            if(!player&&YYGroup.RevealsPrivateLab(text,save.S.aiName))throw new InvalidOperationException("private_lab_dialogue");
            var c=chat.Conversation(YYChatHub.EraGroup);
            c.messages.Add(new YYMessage{from=player?YYChatHub.Me:YYChatHub.EraGroup,text=text,gameSeconds=save.S.gameSeconds,
                kind=string.IsNullOrEmpty(app)?YYKind.Text:YYKind.AppLink,introApp=app,introLabelZh=labelZh,introLabelEn=labelEn});
            if(!chat.IsShowing(YYChatHub.EraGroup))c.unread++;
        }
        bool EraDelivered(string id)=>!string.IsNullOrEmpty(id)&&save.S.eraDelivered!=null&&save.S.eraDelivered.Contains(id);
        public bool DeferGroup(string text)
        {if(!Playing)return false;Data.deferredGroup.Add(text);owner.runtime.MarkDirty();return true;}
        void Update()
        {
            Bind();LookupWindows();if(!Current||apps==null||catalog==null||save.InPrologue||owner.runtime.TestMode){CloseThought();CloseDownload();CloseAttention();return;}
            bool held=Held;
            lab.TickAppAcquisition(Time.unscaledDeltaTime,held||PlayerWindowState.IsMinimized,browser?.networkManager?.isConnected==true,apps);
            TickAttention(held);
            if(held){CloseThought();if(downloadPage!=null)downloadPage.gameObject.SetActive(false);return;}
            TickDownload();
            if(!Data.gamesOpened&&gameWindow!=null&&gameWindow.isOn&&LingGuangV05.Desktop.Media.DesktopNotifications.IsWindowVisible(gameWindow))
                Commit(()=>Data.gamesOpened=true);
            if(thoughtText!=null&&thought!=null){typed+=Time.unscaledDeltaTime*30;thoughtText.maxVisibleCharacters=(int)typed;}
            if(Time.unscaledTime>=nextCheckpoint){nextCheckpoint=Time.unscaledTime+2;owner.runtime.MarkDirty();}
            if(Playing){TickScript();return;}
            if(downloadPage!=null)return; // Finish/Open remains usable before another optional thought starts.
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;
            if(chat.GroupScriptPlaying||chat.IsTyping(YYChatHub.EraGroup)||InnerVoice.Busy)return;
            string next=lab.NextGroupIntro(catalog,EraDelivered);
            if(!string.IsNullOrEmpty(next)){if(Commit(()=>Data.activeScript=next)){lineWait=0;CloseThought();}return;}
            if(Data.deferredGroup.Count>0)
            {if(Commit(()=>{Append(Data.deferredGroup[0]);Data.deferredGroup.RemoveAt(0);})){nextCheck=Time.unscaledTime+2.5f;}return;}
            // Readiness is private. No NPC line is sent until the player explicitly asks in the group.
            if(!Data.sampleHelpRequested&&lab.DesktopAppInstalled("taohuo")&&lab.S.stage>=2&&lab.NextAbility>0&&lab.ExamSamplesReady<XgSim.ExamOpenShare
                &&!lab.IntroProgress("datapack_intro").legacySkipped)
            {Data.sampleHelpEligible=true;ShowThought("sample-help",GameText.T("样本还不够。要不要问问群里有没有现成的数据包？","There aren't enough examples. Ask the group about ready-made data packs?"),GameText.T("问问群友","Ask the group"),()=>
                {if(Commit(()=>{Data.sampleHelpRequested=true;Append(GameText.T("有人知道哪里能找到标好的图片吗？","Does anyone know where to find labelled pictures?"),true);})){CloseThought();chat.router?.Open(YYChatHub.AppId,YYChatHub.EraGroup);}},true);return;}
        }
        void TickScript()
        {
            var p=Progress;var step=Step;if(p==null)return;
            if(step==null){Commit(()=>{p.completed=true;Data.activeScript="";});CloseThought();return;}
            if(step.type=="self")
            {ShowThought(Script.id+":"+p.step,GameText.T(step.text,step.textEn),GameText.T(step.button,step.buttonEn),()=>
                {if(Commit(()=>p.step++)){if(step.action=="blinkYY")chat.router?.Open(YYChatHub.AppId,YYChatHub.EraGroup);CloseThought();}},
                false,string.IsNullOrEmpty(step.button)&&p.step<Script.steps.Count-1);return;}
            CloseThought();
            if(step.type=="choice"&&p.choice<0)return;
            if(step.type=="group"||step.type=="choice")
            {
                lineWait+=Math.Min(1,Time.unscaledDeltaTime);if(lineWait<2.5)return;
                if(step.type=="group")
                {if(Commit(()=>{Append(GameText.T(step.text,step.textEn));p.step++;}))lineWait=0;return;}
                if(p.choice>=step.options.Count)return;var option=step.options[p.choice];
                if(p.reply<option.replies.Count)
                {int i=p.reply;if(Commit(()=>{Append(GameText.T(option.replies[i],i<option.repliesEn.Count?option.repliesEn[i]:option.replies[i]));p.reply++;}))lineWait=0;return;}
                if(Commit(()=>{p.step++;p.choice=-1;p.reply=0;}))lineWait=0;return;
            }
            if(step.type=="link"||step.type=="open")
            {Commit(()=>{DeliverLink(step);p.step++;});return;}
            if(step.type=="todo"){Commit(()=>{p.todoOffered=true;p.step++;});return;}
        }
        void DeliverLink(XgGroupIntroStep step)
        {
            var app=lab.AppReceipt(step.app);if(app==null)return;
            if(!app.offered){app.offered=true;Append(GameText.T(step.text,step.textEn),false,step.app,step.label,step.labelEn);}
        }
        public bool Typing=>Playing&&!Held&&(Step?.type=="group"||Step?.type=="choice"&&Progress.choice>=0);
        public string[] Choices()
        {
            var step=Step;if(!Playing||Held||step?.type!="choice"||Progress.choice>=0)return Array.Empty<string>();
            var values=new string[step.options.Count];for(int i=0;i<values.Length;i++)values[i]=GameText.T(step.options[i].text,step.options[i].textEn);return values;
        }
        public bool Choose(string value)
        {
            if(!Playing)return false;if(Held)return true;
            var step=Step;var p=Progress;if(step?.type!="choice"||p.choice>=0)return true;
            for(int i=0;i<step.options.Count;i++)
                if(value==GameText.T(step.options[i].text,step.options[i].textEn))
                {int pick=i;if(Commit(()=>{Append(value,true);p.choice=pick;p.reply=0;}))lineWait=0;break;}
            return true;
        }
        public void Skip(int expectedRevision)
        {
            if(!Playing||Held||expectedRevision!=Revision)return;var s=Script;var p=Progress;if(s==null)return;
            if(Commit(()=>{for(int i=p.step;i<s.steps.Count;i++){var step=s.steps[i];if(step.type=="link"||step.type=="open")DeliverLink(step);if(step.type=="todo")p.todoOffered=true;}
                p.skipped=true;p.completed=true;p.step=s.steps.Count;Data.activeScript="";}))CloseThought();
        }
        public void AddTodos(List<XgGuideStep> output)
        {
            if(!Current||catalog==null)return;
            foreach(var s in catalog.scripts)
            {
                var p=lab.IntroProgress(s.id);if(!p.todoOffered)continue;
                var todo=s.steps.Find(x=>x.type=="todo");if(todo==null||TodoDone(todo.done))continue;
                output.Insert(0,new XgGuideStep{id="intro.todo."+s.id,kind=XgGuideKind.Side,zh=todo.text,en=todo.textEn,
                    app="group-intro",tab=s.app,arg=s.id,whyZh="这是刚才群里提到的事。",whyEn="From the conversation in the group."});
            }
        }
        bool TodoDone(string condition)
        {
            if(condition.StartsWith("appInstalled:",StringComparison.Ordinal))return lab.DesktopAppInstalled(condition.Substring(13));
            if(condition=="packInstalled:any")return lab.S.dataFiles.Exists(x=>x.installed);
            if(condition=="enterpriseOrderTaken")return lab.S.contracts.Count>0;
            if(condition=="gamecenterOpened")return Data.gamesOpened;return false;
        }
        public void FollowTodo(XgGuideStep todo)
        {
            if(!Current||Held)return;
            if(!string.IsNullOrEmpty(todo.tab))OpenLink(todo.tab);
            else if(todo.arg=="datapack_intro")owner.runtime.GetComponent<XgFilesWindow>()?.Show();
            else chat.router?.Open("zhongbao","contracts");
        }
        void LateUpdate(){if(Held){CloseThought();TickAttention(true);if(downloadPage!=null)downloadPage.gameObject.SetActive(false);}}
        void OnDisable(){CloseThought();CloseDownload();CloseAttention();}
        void OnDestroy(){CloseThought();CloseDownload();CloseAttention();if(Instance==this)Instance=null;}
    }
}
