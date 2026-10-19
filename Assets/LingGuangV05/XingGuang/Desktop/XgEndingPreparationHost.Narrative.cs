using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgEndingPreparationHost
    {
        XgEndingSeedCatalog seeds;
        RectTransform launcher,seedPanel,gallery;
        XgBtn memoriesButton,seedButton;
        float nextNarrativePoll,nextSeedAllowed;
        string shownSeed="";
        XgEndingSeedPrompt shownSeedPrompt;
        TMP_Text seedQuestionLabel,seedLaterLabel;
        TMP_Text[] seedOptionLabels=Array.Empty<TMP_Text>();
        bool seedEnglish;
        int galleryPage;
        WindowManager narrativeSettings;
        float nextSettingsLookup;
        long narrativeRevision=long.MinValue;
        int narrativeStage=-1,narrativeMemories;
        string narrativePhase="";
        XgEndingSeedPrompt narrativePrompt;
        // This host never registers a StoryFocus/Holding owner of its own.
        // Read external owners only, so an open question cannot hold itself forever.
        bool NarrativeAvailable=>Current&&guard!=null&&text!=null&&!boundSave.InPrologue
            &&!StoryFocus.Active&&!InnerVoice.Busy&&!boundLab.StagePresentationHeld
            &&!XgLifeHostActivity.GamePaused&&!XgLifeHostActivity.ReadingWindowVisible
            &&!(LingGuangV05.Desktop.YY.YYChatHub.Instance?.GroupScriptPlaying==true)
            &&!SaveSlotsPanel.IsOpen&&!(narrativeSettings!=null&&narrativeSettings.isOn)
            &&!(owner.Epilogue!=null&&(owner.Epilogue.HoldingSimulation||owner.Epilogue.Showing))
            &&!XgEpilogueFlow.HasCommittedRelease(boundLab.S)&&!boundLab.S.chapterComplete
            &&!(area!=null&&area.gameObject.activeInHierarchy);
        bool NarrativePanelCurrent(RectTransform captured,RectTransform live,object save,object state)
            =>NarrativeAvailable&&captured!=null&&ReferenceEquals(captured,live)&&captured.gameObject.activeInHierarchy
                &&ReferenceEquals(save,boundSave)&&ReferenceEquals(state,boundLab.S);
        static void DisposeNarrativePanel(ref RectTransform panel)
        {var old=panel;panel=null;if(old!=null){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
        void CloseSeed()
        {
            DisposeNarrativePanel(ref seedPanel);shownSeed="";shownSeedPrompt=null;
            seedQuestionLabel=seedLaterLabel=null;seedOptionLabels=Array.Empty<TMP_Text>();
        }
        void CloseSource(){DisposeNarrativePanel(ref sourceView);sourceShown=false;}
        void CloseGallery()
        {if(gallery!=null&&sourceView!=null&&sourceView.IsChildOf(gallery))CloseSource();DisposeNarrativePanel(ref gallery);}
        void SuspendNarrative()
        {
            if(launcher!=null)launcher.gameObject.SetActive(false);
            // No answer/cooldown is written: the same pending question remains available after the hold.
            CloseSeed();CloseGallery();
        }
        void FindNarrativeSettings()
        {
            if(narrativeSettings!=null||Time.unscaledTime<nextSettingsLookup)return;
            nextSettingsLookup=Time.unscaledTime+1;
            if(owner==null||owner.runtime==null)return;
            foreach(var window in DesktopWindowIndex.Get(owner.runtime))
                if(window.name=="Settings"){narrativeSettings=window;break;}
        }
        void ResetNarrative()
        {
            CloseSeed();CloseGallery();DisposeNarrativePanel(ref launcher);
            seeds=null;nextNarrativePoll=nextSeedAllowed=0;galleryPage=0;
            narrativeRevision=long.MinValue;narrativeStage=-1;narrativePrompt=null;
        }
        void PollNarrative()
        {
            FindNarrativeSettings();
            // Visibility is checked every frame, before the throttled snapshot work.
            if(!NarrativeAvailable){SuspendNarrative();return;}
            RefreshSeedLanguage();
            if(Time.unscaledTime<nextNarrativePoll)return;nextNarrativePoll=Time.unscaledTime+.5f;
            var parent=PrologueDirector.Desk?.layer;if(parent==null)return;
            if(seeds==null){var asset=Resources.Load<TextAsset>("LingGuangV05/Data/ending_shutdown_prompts");if(asset!=null)seeds=JsonUtility.FromJson<XgEndingSeedCatalog>(asset.text);}
            long revision=Revision;
            if(narrativeRevision!=revision||narrativeStage!=boundLab.S.stage)
            {
                var d=flow.GetSnapshot();narrativeMemories=d.memories.Count;narrativePhase=d.phase;
                narrativePrompt=flow.NextShutdownPrompt(seeds);narrativeRevision=revision;narrativeStage=boundLab.S.stage;
            }
            var prompt=narrativePrompt;
            if(launcher==null)
            {
                launcher=Rect("EndingNotesLauncher",parent,new Vector2(1,0),new Vector2(1,0),new Vector2(-244,54),new Vector2(-12,132));
                var panel=launcher;object save=boundSave,state=boundLab.S;
                var ui=new XgUi(owner.Font,null,true);
                memoriesButton=ui.Button(launcher,"",()=>{if(NarrativePanelCurrent(panel,launcher,save,state))OpenGallery();},13);
                seedButton=ui.Button(launcher,"",()=>{if(!NarrativePanelCurrent(panel,launcher,save,state))return;var currentPrompt=flow.NextShutdownPrompt(seeds);if(currentPrompt!=null)ShowSeed(currentPrompt);},13);
                PlaceButton(memoriesButton,0,1,40,76);PlaceButton(seedButton,0,1,0,36);
            }
            launcher.gameObject.SetActive(narrativeMemories>0||prompt!=null);
            memoriesButton.Set(GameText.IsEnglish?"It remembers ("+narrativeMemories+")":"它记得（"+narrativeMemories+"）",narrativeMemories>0);
            seedButton.Set(GameText.IsEnglish?"A question without a right answer":"一道没有标准答案的题",prompt!=null);seedButton.Show(prompt!=null);
            if(prompt==null){CloseSeed();return;}
            if(narrativePhase=="testing"&&seedPanel==null&&gallery==null&&Time.unscaledTime>=nextSeedAllowed)ShowSeed(prompt);
        }
        static void PlaceButton(XgBtn button,float left,float right,float bottom,float top)
        {button.rt.anchorMin=new Vector2(left,0);button.rt.anchorMax=new Vector2(right,0);button.rt.offsetMin=new Vector2(6,bottom);button.rt.offsetMax=new Vector2(-6,top);}
        string SeedQuestionText(XgEndingSeedPrompt prompt,bool english)
        {
            string q=english?prompt.questionEn:prompt.questionZh;
            string description=english?prompt.descriptionEn:prompt.descriptionZh;
            string heading=prompt.pulls?(english?"No right answer · your choice becomes a rule":"没有标准答案 · 你在教它一条规则"):(english?"A remembered day":"记得的那一天");
            return Safe(heading+"\n\n"+q+(description.Length>0?"\n\n"+description:""),english);
        }
        void RefreshSeedLanguage()
        {
            bool english=GameText.IsEnglish;
            if(seedPanel==null||shownSeedPrompt==null||seedQuestionLabel==null||seedEnglish==english)return;
            // Only relabel the existing controls: their captured prompt, binding and focus stay intact.
            seedQuestionLabel.text=SeedQuestionText(shownSeedPrompt,english);
            for(int i=0;i<seedOptionLabels.Length;i++)
            {
                var option=shownSeedPrompt.options[i];
                seedOptionLabels[i].text=Safe(english?option.en:option.zh,english);
            }
            seedLaterLabel.text=english?"Later":"稍后再想";
            seedEnglish=english;
        }
        void ShowSeed(XgEndingSeedPrompt prompt)
        {
            if(!NarrativeAvailable||prompt==null)return;var parent=PrologueDirector.Desk?.layer;if(parent==null)return;
            CloseGallery();CloseSeed();shownSeed=prompt.id;shownSeedPrompt=prompt;seedEnglish=GameText.IsEnglish;
            seedPanel=Rect("ShutdownNarrativeQuestion",parent,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-482,-240),new Vector2(-18,240));Panel(seedPanel,XgDark.Page);
            var ui=new XgUi(owner.Font,null,true);
            seedQuestionLabel=ui.Text(Rect("Question",seedPanel,new Vector2(0,.5f),Vector2.one,new Vector2(18,4),new Vector2(-18,-18)),SeedQuestionText(prompt,seedEnglish),17,XgDark.Ink,TextAlignmentOptions.TopLeft);seedQuestionLabel.richText=false;
            seedOptionLabels=new TMP_Text[prompt.options.Count];
            var token=flow.CaptureBinding();object save=boundSave,state=boundLab.S;var panel=seedPanel;
            for(int i=0;i<prompt.options.Count;i++)
            {
                int pick=i;var option=prompt.options[i];var button=ui.Button(seedPanel,Safe(GameText.IsEnglish?option.en:option.zh,GameText.IsEnglish),()=>
                {
                    if(!NarrativePanelCurrent(panel,seedPanel,save,state)||shownSeed!=prompt.id)return;
                    var result=flow.AnswerShutdownPrompt(token,seeds,prompt.id,pick);
                    if(!result.Success){lastCode=result.Code;owner.View?.ShowToast(ErrorText(result.Code).Get(GameText.IsEnglish),4);ShowSeed(prompt);return;}
                    CloseSeed();nextSeedAllowed=Time.unscaledTime+45;
                    flow.DiscoverMemories(flow.CaptureBinding());if(area!=null&&area.gameObject.activeInHierarchy)RefreshPanels();
                },14);
                PlaceButton(button,0,1,186-i*42,222-i*42);button.label.richText=false;seedOptionLabels[i]=button.label;
            }
            var later=ui.Button(seedPanel,GameText.IsEnglish?"Later":"稍后再想",()=>{if(!NarrativePanelCurrent(panel,seedPanel,save,state))return;CloseSeed();nextSeedAllowed=Time.unscaledTime+90;},13);PlaceButton(later,0,1,8,42);
            seedLaterLabel=later.label;
        }
        void OpenGallery()
        {
            if(!NarrativeAvailable)return;var parent=PrologueDirector.Desk?.layer;if(parent==null)return;
            CloseSeed();CloseGallery();gallery=Rect("ItRemembersGallery",parent,new Vector2(.15f,.1f),new Vector2(.85f,.9f),Vector2.zero,Vector2.zero);Panel(gallery,XgDark.Page);
            var ui=new XgUi(owner.Font,null,true);var snapshot=flow.GetSnapshot();int pages=Math.Max(1,(snapshot.memories.Count+3)/4);galleryPage=Math.Max(0,Math.Min(pages-1,galleryPage));
            var header=ui.Text(Rect("Title",gallery,new Vector2(0,1),Vector2.one,new Vector2(18,-52),new Vector2(-18,-12)),GameText.IsEnglish?"It remembers":"它记得",22,XgDark.Ink,TextAlignmentOptions.TopLeft);header.richText=false;
            var token=flow.CaptureBinding();object save=boundSave,state=boundLab.S;long revision=Revision;var panel=gallery;
            var viewport=Rect("MemoryViewport",gallery,Vector2.zero,Vector2.one,new Vector2(12,60),new Vector2(-12,-60));viewport.gameObject.AddComponent<RectMask2D>();Panel(viewport,XgDark.Page).raycastTarget=true;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;scroll.scrollSensitivity=28;
            int rows=Math.Min(4,snapshot.memories.Count-galleryPage*4);var content=Rect("MemoryCards",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,Math.Max(1,rows)*110);scroll.content=content;
            for(int row=0;row<4;row++)
            {
                int index=galleryPage*4+row;if(index>=snapshot.memories.Count)break;var card=snapshot.memories[index];var template=text.memories.Find(t=>t.id==card.id);
                string title=GameText.IsEnglish?(template?.titleEn??"Memory"):(template?.titleZh??"记忆");
                string line=GameText.IsEnglish?card.lineEn:card.line;int currentRow=row;
                var button=ui.Button(content,Safe(Date(card.day)+" · "+title+"\n"+line,GameText.IsEnglish),()=>
                {if(!NarrativePanelCurrent(panel,gallery,save,state))return;if(IsCurrent(save,state,revision)&&flow.IsCurrent(token)&&card.sources.Count>0)OpenGallerySources(save,state,revision,card);else OpenGallery();},15);
                button.rt.anchorMin=new Vector2(0,1);button.rt.anchorMax=Vector2.one;button.rt.offsetMin=new Vector2(6,-104-currentRow*110);button.rt.offsetMax=new Vector2(-6,-4-currentRow*110);button.label.richText=false;
            }
            var previous=ui.Button(gallery,GameText.IsEnglish?"Previous":"上一页",()=>{if(!NarrativePanelCurrent(panel,gallery,save,state))return;galleryPage--;OpenGallery();},14);PlaceButton(previous,0,.28f,12,48);previous.Set(previous.label.text,galleryPage>0);
            var next=ui.Button(gallery,GameText.IsEnglish?"Next":"下一页",()=>{if(!NarrativePanelCurrent(panel,gallery,save,state))return;galleryPage++;OpenGallery();},14);PlaceButton(next,.3f,.58f,12,48);next.Set(next.label.text,galleryPage+1<pages);
            var close=ui.Button(gallery,GameText.IsEnglish?"Back":"返回",()=>{if(!NarrativePanelCurrent(panel,gallery,save,state))return;CloseGallery();},14);PlaceButton(close,.6f,1,12,48);
        }
        void OpenGallerySources(object save,object state,long revision,XgEndingMemoryCard card)
        {
            if(!IsCurrent(save,state,revision)||!NarrativeAvailable||gallery==null||!gallery.gameObject.activeInHierarchy)return;
            if(card.sources.Count==1){OpenSourceRecord(save,state,revision,card.sources[0].refId,gallery,null);return;}
            CloseSource();sourceView=Rect("MemorySources",gallery,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Panel(sourceView,XgDark.Page);sourceShown=true;var panel=sourceView;var parent=gallery;
            var ui=new XgUi(owner.Font,null,true);
            var title=ui.Text(Rect("Title",sourceView,new Vector2(0,1),Vector2.one,new Vector2(18,-56),new Vector2(-18,-12)),GameText.IsEnglish?"Choose a source record":"选择来源记录",20,XgDark.Ink,TextAlignmentOptions.TopLeft);title.richText=false;
            for(int i=0;i<card.sources.Count;i++)
            {
                var source=card.sources[i];var button=ui.Button(sourceView,Date(source.day)+" · "+SourceField(XgEndingPresentation.SourceSummary(boundLab,source,GameText.IsEnglish)),()=>{if(!SourcePanelCurrent(panel,parent,save,state))return;if(!IsCurrent(save,state,revision)){OpenGallery();return;}OpenSourceRecord(save,state,revision,source.refId,parent,()=>{if(IsCurrent(save,state,revision))OpenGallerySources(save,state,revision,card);else OpenGallery();});},15);
                button.rt.anchorMin=new Vector2(0,1);button.rt.anchorMax=Vector2.one;button.rt.offsetMin=new Vector2(18,-132-i*72);button.rt.offsetMax=new Vector2(-18,-64-i*72);button.label.richText=false;
            }
            var back=ui.Button(sourceView,GameText.IsEnglish?"Back":"返回",()=>{if(SourcePanelCurrent(panel,parent,save,state))CloseSource();},14);PlaceButton(back,0,1,12,48);
        }
        static void CreateScrollableText(RectTransform parent,string body,XgUi ui)
        {
            var viewport=Rect("RecordViewport",parent,Vector2.zero,Vector2.one,new Vector2(18,74),new Vector2(-18,-18));viewport.gameObject.AddComponent<RectMask2D>();Panel(viewport,XgDark.Page).raycastTarget=true;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;scroll.scrollSensitivity=28;
            var content=Rect("RecordContent",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);content.pivot=new Vector2(.5f,1);scroll.content=content;
            var label=ui.Text(content,body,22,XgDark.Ink,TextAlignmentOptions.TopLeft);label.richText=false;
            Canvas.ForceUpdateCanvases();float width=Math.Max(80,viewport.rect.width);float height=label.GetPreferredValues(body,width,0).y+24;
            content.sizeDelta=new Vector2(0,Math.Max(viewport.rect.height,height));scroll.verticalNormalizedPosition=1;
        }
    }
}
