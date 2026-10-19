#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Real Main/native Mail/LCD/core/SaveNow. Only the stated initial prerequisite state is fabricated.
// No generic presenter save callback, direct button invocation, model run or long-night dependency.
public static class PublicLetterArchiveActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Recommended="zhou_unpressed_button";
    public const string Scope="Independent public-letter archive actual UI: Main/stage-6/fullOpen/local_only committed release/letter phase are explicit fixture prerequisites, including five fixture prerequisite receipts. Real native epilogue inbox, public titles and article controls, LCD wheel, Chinese/English, SaveNow snapshot failure/rollback/retry, two real bookmarks, primary disk ReloadSave, stale callback rejection, recommendation continuation, native Close/Minimize. Asserts no ending save/open/read/completion change or scene return. Does not earn prerequisites or certify a launch night, the other route recommendations, or ending completion.";
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount {get;private set;}
    static T Get<T>(object owner,string field)=>(T)owner.GetType().GetField(field,F).GetValue(owner);
    static void Need(bool ok,string message){AssertionCount++;if(!ok)throw new InvalidOperationException("Public-letter actual UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("Public-letter actual UI: "+message);}
    static IEnumerator Settle(int frames=3){Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerator Until(Func<bool> ready,string label,double seconds=12)
    {
        double end=EditorApplication.timeSinceStartup+seconds;
        while(!ready()){Need(EditorApplication.timeSinceStartup<end,label+" timed out");yield return null;}
        yield return Settle();
    }
    static Button Named(Component root,string name,bool interactable=true)
    {
        var matches=root.GetComponentsInChildren<Button>(true).Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToArray();
        Need(matches.Length==1,"one visible button "+name+", found "+matches.Length);
        if(interactable)Need(matches[0].IsInteractable(),"interactable button "+name);
        return matches[0];
    }
    static IEnumerator Tap(Component root,string name,Action<RectTransform> click)
    {
        yield return Settle();var button=Named(root,name);click((RectTransform)button.transform);yield return Settle();
    }
    static bool VisibleButton(Component root,string name)=>root.GetComponentsInChildren<Button>(true).Any(b=>b.name==name&&b.gameObject.activeInHierarchy);
    static string VisibleText(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));
    static MailInboxView Inbox(XingGuangController ctl)=>ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Content.GetComponent<MailInboxView>();
    static WindowManager Window(XingGuangController ctl)=>ctl.runtime.GetComponent<ChapterOneDesktopRouter>().Get("mail").Window;
    static IEnumerator NativeReady(WindowManager window)
    {
        yield return Until(()=>
        {
            if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))if(group.alpha<.95f||!group.interactable||!group.blocksRaycasts)return false;
            var animator=window.windowAnimator;
            return animator==null||!animator.isActiveAndEnabled||!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native mailbox naturally interactive");
    }
    public static ChapterOneSim Fixture()
    {
        var lab=new XgSim();lab.BeginOpening();lab.S.stage=lab.S.stageVision=lab.S.stageSequence=6;lab.S.fullOpen=true;
        lab.S.abilitiesEmerged=Enumerable.Range(1,6).ToList();lab.S.teachingFlow=false;
        lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        lab.MarkAfterthought("first.drift");lab.S.ending="E3-legacy-public-archive-fixture";
        lab.S.releaseOutcome=new ReleaseOutcome
        {
            route="local_only",decisionOwner="player",shutdownPin="unpinned",committedAt=20161231,
            releaseReceiptId="qa:public-archive-release",sourceLegacyEnding=lab.S.ending,
            prerequisites=new XgReleasePrerequisites
            {
                memoryCardsReceiptId="qa:public-archive-memory",redTeamOneReceiptId="qa:public-archive-red-1",
                redTeamTwoReceiptId="qa:public-archive-red-2",redTeamThreeReceiptId="qa:public-archive-red-3",
                fixedThreeSlotsReceiptId="qa:public-archive-slots"
            }
        };
        lab.S.epilogue=new EpilogueState{phase="letter",letterId=Recommended};
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=1000000;
        house.S.labState=JsonUtility.ToJson(lab.S);
        GameCalendar.Advance(house.S,GameCalendar.DayFor(6,0));house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;
        return house;
    }
    static string EnableDisk(ChapterOneRuntime rt)
    {
        string root=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrWhiteSpace(root),"explicit verification save folder exists");root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        Need(root.IndexOf(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)>=0,"verification folder belongs to LocalVerification");
        Need(Path.GetFullPath(rt.SavePath).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"real primary file is inside verification folder");
        rt.useDiskSave=true;return Path.GetDirectoryName(rt.SavePath);
    }
    static string ReadPayload(string directory)
    {
        var loaded=new AtomicSaveStore(directory).Load();Need(loaded.Success&&!loaded.Recovered,"primary disk payload loads without recovery");return loaded.Payload;
    }
    static XgState ReadLab(string directory)=>JsonUtility.FromJson<XgState>(JsonUtility.FromJson<GameState>(ReadPayload(directory)).labState);
    static string EndingProjection(XgState state)
    {
        var ep=state.epilogue.Copy();ep.revision=0;ep.publicArticleArchives.Clear();
        return JsonUtility.ToJson(ep)+"|"+JsonUtility.ToJson(state.releaseOutcome)+"|"+state.chapterComplete+"|"+state.ending;
    }
    static void Uncompleted(XingGuangController ctl,string expected,int sceneHandle,int completions,string directory)
    {
        Need(EndingProjection(ctl.Sim.S)==expected,"recommendation, release and legacy-ending state unchanged");
        Need(!ctl.Sim.S.chapterComplete&&string.IsNullOrEmpty(ctl.Sim.S.epilogue.completionReceipt)&&completions==0,"no completed chapter or completion event");
        Need(SceneManager.GetActiveScene().handle==sceneHandle&&ctl.runtime.gameObject.scene.handle==sceneHandle,"same Main scene and runtime remain; no title return");
        Need(EndingProjection(ReadLab(directory))==expected,"disk recommendation/consent/open/read/completion fields unchanged");
    }
    static IEnumerator OpenRecommendation(XingGuangController ctl,Action<RectTransform> click)
    {
        Need(ctl.Epilogue.Show(PrologueDirector.Desk?.layer),"production letter-phase epilogue opens on LCD");yield return Settle();
        var view=Get<XgEpilogueView>(ctl.Epilogue,"view");var button=Get<XgBtn>(view,"inbox");
        Need(view.Showing&&button.rt.gameObject.activeInHierarchy&&button.button.IsInteractable(),"production epilogue inbox control available");
        click(button.rt);yield return Until(()=>Inbox(ctl)?.EndingLetter?.Visible==true,"real recommendation presenter visible");
        yield return NativeReady(Window(ctl));
        var p=Inbox(ctl).EndingLetter;
        Need(p.Snapshot.articleId==Recommended&&!p.Snapshot.saved&&!p.Snapshot.opened&&!p.Snapshot.consentShown,"bridge opens only untouched recommended letter");
        Need(!VisibleButton(p.Root,"CompleteEnding"),"unsaved recommendation cannot finish");
    }
    static IEnumerator ScrollBottom(PublicLetterArchivePresenter presenter)
    {
        var scroll=presenter.Root.GetComponentInChildren<ScrollRect>();
        var bridge=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var pointer=typeof(FilesWindowUiChecks).GetMethod("Pointer",BindingFlags.Static|BindingFlags.NonPublic);
        Need(scroll!=null&&bridge?.inputSurface!=null&&pointer!=null,"actual LCD scroll and verified coordinate mapper exist");
        Canvas.ForceUpdateCanvases();
        for(int i=0;i<120&&scroll.content.rect.height>scroll.viewport.rect.height+1&&scroll.verticalNormalizedPosition>.005f;i++)
        {
            var data=(PointerEventData)pointer.Invoke(null,new object[]{scroll.viewport});var surface=bridge.inputSurface;surface.OnPointerEnter(data);
            var hover=(PointerEventData)typeof(AeroLcdInputSurface).GetField("hoverData",F).GetValue(surface);
            Need(hover?.pointerCurrentRaycast.gameObject!=null&&ExecuteEvents.GetEventHandler<IScrollHandler>(hover.pointerCurrentRaycast.gameObject)==scroll.gameObject,"actual wheel hits public article ScrollRect");
            data.scrollDelta=new Vector2(0,-3);surface.OnScroll(data);yield return Settle();
        }
        Need(scroll.content.rect.height<=scroll.viewport.rect.height+1||scroll.verticalNormalizedPosition<=.005f,"actual LCD wheel reaches public article end");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        AssertionCount=0;Checks.Clear();var rt=ctl.runtime;var director=ctl.Epilogue;
        Need(!rt.TestMode&&!rt.tickEnabled&&!ctl.enabled&&ctl.BoundTo(rt.Sim),"real current host with unrelated simulation ticks isolated");
        Need(XgEpilogueFlow.CanBrowsePublicLetters(ctl.Sim.S),"explicit local release/letter prerequisite fixture accepted");
        string directory=EnableDisk(rt);Need(rt.SaveNow(),"fixture checkpoint saved by real SaveNow");
        string expected=EndingProjection(ctl.Sim.S);int scene=SceneManager.GetActiveScene().handle,saves=0,completions=0;
        Action saving=()=>saves++;Action<string> completed=r=>completions++;
        rt.Saving+=saving;director.CompletionSaved+=completed;
        try
        {
            language(false);yield return OpenRecommendation(ctl,click);
            var inbox=Inbox(ctl);yield return Tap(inbox.EndingLetter.Root,"OpenPublicLetters",click);
            var p=inbox.PublicLetters;Need(p!=null&&p.Visible&&p.SavedCount==0,"real header opens empty public archive");
            var cards=p.Root.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("PublicArticle_")).OrderByDescending(b=>((RectTransform)b.transform).anchorMax.y).ToArray();
            Need(cards.Length==3&&cards[0].name=="PublicArticle_"+Recommended,"all three public titles shown with local recommendation first");
            capture("public-archive-list-zh.png");
            foreach(string id in new[]{"zhou_open_door","zhou_copies",Recommended})
            {
                yield return Tap(p.Root,"PublicArticle_"+id,click);Need(p.SelectedArticleId==id,"actual title click selects "+id);
                foreach(bool english in new[]{false,true})
                {
                    language(english);yield return Settle();
                    Need(p.SelectedArticleId==id,"global language switch retains stable public article ID");
                    var article=new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath).text)).Get(id);
                    var paragraphs=p.Root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("PublicParagraph_")).ToArray();
                    Need(paragraphs.Length==9&&article.paragraphs.All(a=>paragraphs.Any(t=>t.name=="PublicParagraph_"+a.id&&t.text==a.Text(english))),"all nine authored paragraphs render in "+(english?"English":"Chinese"));
                    Need(!VisibleButton(p.Root,"CompleteEnding"),"public reading exposes no completion control");
                    yield return ScrollBottom(p);capture("public-archive-"+id+"-"+(english?"en":"zh")+"-bottom.png");
                }
                yield return Tap(p.Root,"BackFromPublicLetters",click);
            }
            Need(saves==0,"browse, real wheel, Back and language changes cause zero SaveNow calls");
            Uncompleted(ctl,expected,scene,completions,directory);
            Done("Real epilogue inbox and header opened all three public titles; actual LCD title clicks/wheel read all three in Chinese and English with zero saves and no completion.");

            language(false);yield return Tap(p.Root,"PublicArticle_zhou_copies",click);
            string diskBefore=ReadPayload(directory),cachedBefore=rt.Sim.S.labState;var epBefore=ctl.Sim.S.epilogue;var releaseBefore=ctl.Sim.S.releaseOutcome;
            int calls=0;Action fault=()=>{calls++;throw new IOException("Public-letter actual UI injected SaveNow snapshot failure");};
            rt.Saving+=fault;
            try{yield return Tap(p.Root,"SavePublicArticle",click);}finally{rt.Saving-=fault;}
            Need(calls==1&&saves==1,"actual save control reached the real failing Saving event once");
            Need(ReferenceEquals(ctl.Sim.S.epilogue,epBefore)&&ReferenceEquals(ctl.Sim.S.releaseOutcome,releaseBefore)&&rt.Sim.S.labState==cachedBefore,"failure restores exact state references and cached lab JSON");
            Need(ReadPayload(directory)==diskBefore&&!ctl.Sim.S.epilogue.HasPublicArticle("zhou_copies",1)&&p.SavedCount==0,"failed save leaves primary payload and bookmarks unchanged");
            Need(Named(p.Root,"SavePublicArticle").IsInteractable()&&VisibleText(p.Root).Contains("没有保存成功"),"Chinese save failure remains visible and retryable");
            capture("public-archive-real-save-failure-zh.png");
            language(true);yield return Settle();
            Need(p.SelectedArticleId=="zhou_copies"&&VisibleText(p.Root).Contains("Saving failed"),"English switch preserves failed selection and retry status");
            capture("public-archive-real-save-failure-en.png");
            yield return Tap(p.Root,"SavePublicArticle",click);
            Need(saves==2&&ctl.Sim.S.epilogue.HasPublicArticle("zhou_copies",1)&&ReadLab(directory).epilogue.HasPublicArticle("zhou_copies",1),"actual retry saves bookmark to memory and primary disk");
            Need(p.Visible&&p.SelectedArticleId=="zhou_copies"&&p.SavedCount==1&&!Named(p.Root,"SavePublicArticle",false).IsInteractable(),"successful archive remains visible and disables duplicate save");
            click((RectTransform)Named(p.Root,"SavePublicArticle",false).transform);yield return Settle();Need(saves==2,"actual click on disabled bookmark does not save twice");
            capture("public-archive-real-save-retry-en.png");
            yield return Tap(p.Root,"BackFromPublicLetters",click);yield return Tap(p.Root,"PublicArticle_zhou_open_door",click);yield return Tap(p.Root,"SavePublicArticle",click);
            Need(saves==3&&p.SavedCount==2&&ReadLab(directory).epilogue.HasPublicArticle("zhou_open_door",1),"second selected public source saved independently");
            Need(ctl.Sim.S.epilogue.revision==epBefore.revision+2&&ctl.Sim.S.epilogue.publicArticleArchives.Count==2,"exactly two bookmark transactions increment revision");
            Uncompleted(ctl,expected,scene,completions,directory);
            Done("Real SaveNow snapshot failure rolled back exact references, cached JSON and primary disk; actual retry and a second bookmark persisted distinct public sources without altering the recommended letter.");

            var staleSave=Get<Action<string>>(p,"saveArticle");Need(staleSave!=null,"capture existing bridge save closure for explicit stale-callback race fixture");
            var oldHouse=rt.Sim;var oldLab=ctl.Sim;var oldToken=director.CaptureBinding();
            Need(rt.ReloadSave(),"actual primary ReloadSave succeeds");
            Need(!ReferenceEquals(oldHouse,rt.Sim)&&!ReferenceEquals(oldLab,ctl.Sim)&&ctl.BoundTo(rt.Sim)&&!director.IsCurrent(oldToken),"reload replaces save/lab identity and rejects old token");
            int afterReload=saves;string reloadedDisk=ReadPayload(directory);
            staleSave(Recommended);yield return Settle();
            Need(saves==afterReload&&ReadPayload(directory)==reloadedDisk&&!ctl.Sim.S.epilogue.HasPublicArticle(Recommended,1),"explicit stale bridge callback cannot save into reloaded owner");
            Need(ctl.Sim.S.epilogue.HasPublicArticle("zhou_copies",1)&&ctl.Sim.S.epilogue.HasPublicArticle("zhou_open_door",1),"both actual bookmarks survive disk reload");
            yield return OpenRecommendation(ctl,click);inbox=Inbox(ctl);
            yield return Tap(inbox.EndingLetter.Root,"OpenPublicLetters",click);p=inbox.PublicLetters;
            Need(p.SavedCount==2&&p.SelectedArticleId=="","reloaded archive opens list with both persisted bookmarks");
            yield return Tap(p.Root,"BackFromPublicLetters",click);
            Need(!p.Visible&&Named(inbox,"PublicLettersEntry").GetComponentInChildren<TMP_Text>().text.Contains("(2)"),"native mailbox title entry displays persisted archive count");
            yield return Tap(inbox,"PublicLettersEntry",click);Need(p.Visible,"actual mailbox title entry reopens archive");
            yield return Tap(p.Root,"PublicArticle_"+Recommended,click);
            Need(!VisibleButton(p.Root,"SavePublicArticle")&&VisibleButton(p.Root,"ContinueRecommendedLetter"),"public recommendation offers only explicit continuation into original letter");
            yield return Tap(p.Root,"ContinueRecommendedLetter",click);
            Need(!p.Visible&&inbox.EndingLetter.Visible&&!inbox.EndingLetter.Snapshot.saved&&!inbox.EndingLetter.Snapshot.opened&&!VisibleButton(inbox.EndingLetter.Root,"CompleteEnding"),"actual continuation resumes untouched original recommendation without granting completion");
            Need(saves==afterReload,"reloaded browsing and explicit continuation add no writes");
            capture("public-archive-reloaded-recommendation-uncompleted.png");
            Uncompleted(ctl,expected,scene,completions,directory);
            Done("Real primary ReloadSave retained both bookmarks and created fresh owners; an explicitly injected old bridge callback was rejected. Mailbox title count and recommendation continuation were then exercised by real LCD controls.");

            foreach(string dismiss in new[]{"Close","Minimize"})
            {
                string selectedBefore=inbox.PublicLetters.SelectedArticleId;
                yield return NativeReady(Window(ctl));yield return Tap(inbox.EndingLetter.Root,"OpenPublicLetters",click);
                Need(inbox.PublicLetters.Visible&&inbox.PublicLetters.SelectedArticleId==selectedBefore,"archive header preserves the previously selected public article");
                if(inbox.PublicLetters.SelectedArticleId.Length>0)yield return Tap(inbox.PublicLetters.Root,"BackFromPublicLetters",click);
                Need(inbox.PublicLetters.SelectedArticleId=="","actual Back returns to public title list before choosing another article");
                yield return Tap(inbox.PublicLetters.Root,"PublicArticle_zhou_copies",click);
                capture("public-archive-before-native-"+dismiss.ToLowerInvariant()+".png");
                int before=saves;string disk=ReadPayload(directory);
                yield return Tap(Window(ctl).windowContainer,"ChapterOne"+dismiss,click);
                Need(!inbox.PublicLetters.Visible&&saves==before&&ReadPayload(directory)==disk,"actual native "+dismiss+" dismisses archive with zero saves");
                Uncompleted(ctl,expected,scene,completions,directory);
                if(dismiss=="Close"){yield return OpenRecommendation(ctl,click);inbox=Inbox(ctl);}
            }
            Done("Actual native Close and Minimize both dismissed the public archive without saves, completion events, recommendation mutation or scene changes.");
        }
        finally{rt.Saving-=saving;if(director!=null)director.CompletionSaved-=completed;}
    }
}
#endif
