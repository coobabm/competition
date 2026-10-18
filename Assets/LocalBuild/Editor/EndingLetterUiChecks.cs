#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Prepared integration frames. Caller supplies an isolated, already-interactive Mail window and actual LCD click.
// Persistence callbacks below are explicit protocol fixtures; they do not certify AtomicSaveStore or final routing.
public static class EndingLetterUiChecks
{
    public static int AssertionCount {get;private set;}
    static void Need(bool ok,string label){AssertionCount++;if(!ok)throw new InvalidOperationException("EndingLetterUiChecks: "+label);}
    static Button Named(EndingLetterPresenter p,string name)=>p.Root.GetComponentsInChildren<Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
    static IEnumerable FramesFor(double seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
    // Real LCD wheel events, including hit verification. No direct ScrollRect geometry assignment grants the gate.
    public static IEnumerator ScrollToEnd(EndingLetterPresenter presenter)
    {
        var scroll=presenter.Root.GetComponentInChildren<ScrollRect>();
        var bridge=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Need(bridge!=null&&bridge.inputSurface!=null,"actual LCD wheel bridge exists");
        var pointer=typeof(FilesWindowUiChecks).GetMethod("Pointer",BindingFlags.Static|BindingFlags.NonPublic);
        Need(pointer!=null,"shared verified LCD coordinate mapper available");
        for(int i=0;i<120&&!presenter.ReadToEnd;i++)
        {
            Canvas.ForceUpdateCanvases();
            var data=(PointerEventData)pointer.Invoke(null,new object[]{scroll.viewport});
            var surface=bridge.inputSurface;surface.OnPointerEnter(data);
            var hover=(PointerEventData)typeof(AeroLcdInputSurface).GetField("hoverData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(surface);
            Need(hover?.pointerCurrentRaycast.gameObject!=null&&ExecuteEvents.GetEventHandler<IScrollHandler>(hover.pointerCurrentRaycast.gameObject)==scroll.gameObject,"actual wheel lands on letter ScrollRect");
            data.scrollDelta=new Vector2(0,-3);surface.OnScroll(data);
            yield return null;yield return null;yield return null;
        }
        foreach(var f in FramesFor(.5))yield return f;
        Need(presenter.ReadToEnd&&Named(presenter,"CompleteEnding").IsInteractable(),"actual wheel reaches visible letter end and enables explicit finish");
        Need(presenter.Session.View==EndingLetterView.Reading,"scrolling to bottom never completes the letter");
    }
    public static IEnumerator Frames(RectTransform host,TMP_FontAsset font,WindowManager window,Action<RectTransform> click,Action<string> capture)
    {
        AssertionCount=0;var presenter=EndingLetterPresenter.Install(host,font,window);
        object owner=new object();long revision=1;bool fail=false;int writes=0,completes=0,backs=0;
        Action<EndingLetterRequest,Action<EndingLetterResult>> persist=(request,done)=>
        {writes++;if(!fail)revision++;done(new EndingLetterResult{owner=owner,revision=revision,success=!fail});};
        var callbacks=new EndingLetterCallbacks{IsCurrent=(o,r)=>ReferenceEquals(o,owner)&&r==revision,RequestSaveArticle=persist,RequestOpen=persist,ReadingPositionChanged=persist,
            RequestComplete=(r,done)=>{completes++;persist(r,done);},RequestBackToDesktop=r=>backs++};
        presenter.Bind(new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_open_door",completionEligible=true,reduceFx=true,soundEnabled=false},callbacks,true);
        try
        {
            foreach(var f in FramesFor(.2))yield return f;
            fail=true;click((RectTransform)Named(presenter,"SaveArticle").transform);
            Need(!presenter.Snapshot.saved&&presenter.Session.ErrorCopyId=="UI-SAVE-FAIL","failed owner save leaves article unsaved");
            yield return null;
            fail=false;click((RectTransform)Named(presenter,"SaveArticle").transform);
            Need(presenter.Unread&&presenter.Snapshot.saved,"unread indicator only after save success");
            yield return null;
            click((RectTransform)Named(presenter,"OpenLetter").transform);yield return null;
            Need(presenter.Session.View==EndingLetterView.Consent&&EventSystem.current.currentSelectedGameObject==Named(presenter,"NotNow").gameObject,"explicit open shows notice with safe default focus");
            click((RectTransform)Named(presenter,"NotNow").transform);Need(!presenter.Snapshot.opened&&completes==0,"actual cancel does not open/complete");
            yield return null;
            click((RectTransform)Named(presenter,"OpenLetter").transform);yield return null;
            click((RectTransform)Named(presenter,"ReadFirst").transform);
            foreach(var f in FramesFor(.3))yield return f;
            Need(presenter.Session.View==EndingLetterView.Reading&&!presenter.Unread&&completes==0,"reading is distinct from completion");
            Need(presenter.Root.GetComponentsInChildren<TMP_Text>().Count(t=>t.name.StartsWith("Paragraph_"))==9,"all nine authored paragraphs present");
            Need(!presenter.ReadToEnd&&!Named(presenter,"CompleteEnding").IsInteractable(),"long letter cannot finish before actual reading reaches end");
            click((RectTransform)Named(presenter,"CompleteEnding").transform);Need(completes==0,"disabled finish pointer cannot send completion");
            var scroll=presenter.Root.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.4f;
            foreach(var f in FramesFor(.4))yield return f;
            string paragraph=presenter.Snapshot.paragraphId;Need(paragraph!="greeting","scroll persists stable paragraph ID through callback");
            presenter.SetLanguage(true);foreach(var f in FramesFor(.3))yield return f;
            Need(presenter.Snapshot.paragraphId==paragraph,"English switch retains paragraph identity");
            Need(!presenter.ReadToEnd&&!Named(presenter,"CompleteEnding").IsInteractable(),"programmatic position and language restore do not earn reading gate");
            var wheel=ScrollToEnd(presenter);while(wheel.MoveNext())yield return wheel.Current;
            Need(completes==0,"actual bottom has no automatic completion callback");
            presenter.ShowInbox();presenter.RequestOpen();foreach(var f in FramesFor(.4))yield return f;
            Need(!presenter.ReadToEnd&&!Named(presenter,"CompleteEnding").IsInteractable(),"reopening resets gate even when persisted position restores near end");
            wheel=ScrollToEnd(presenter);while(wheel.MoveNext())yield return wheel.Current;
            capture("ending-letter-en-reader");
            fail=true;click((RectTransform)Named(presenter,"CompleteEnding").transform);
            Need(presenter.Session.View==EndingLetterView.Reading&&presenter.Session.ErrorCopyId=="UI-FAIL"&&completes==1,"actual final button failure leaves page retryable");
            yield return null;
            fail=false;click((RectTransform)Named(presenter,"CompleteEnding").transform);
            Need(presenter.Session.View==EndingLetterView.Complete&&completes==2,"retry reports success after owner receipt");
            var snapshot=presenter.Snapshot;snapshot.english=true;presenter.Bind(snapshot,callbacks,true);presenter.RequestOpen();
            int before=writes;foreach(var f in FramesFor(.3))yield return f;
            Need(!presenter.Root.GetComponentsInChildren<Button>().Any(b=>b.name=="CompleteEnding"&&b.gameObject.activeInHierarchy),"archive has no completion control");
            click((RectTransform)Named(presenter,"BackToDesktop").transform);
            Need(writes==before&&backs==1&&!presenter.Visible,"archive back has zero writes and no duplicate completion");
            // New save identity and different article: neither borrows the previous reading geometry.
            owner=new object();revision=1;
            presenter.Bind(new EndingLetterInput{owner=owner,revision=revision,articleId="zhou_copies",saved=true,opened=true,consentShown=true,completionEligible=true,english=true},callbacks,true);
            presenter.RequestOpen();foreach(var f in FramesFor(.4))yield return f;
            Need(!presenter.ReadToEnd&&!Named(presenter,"CompleteEnding").IsInteractable(),"new owner and switched article reset reading gate without rewriting opened facts");
        }
        finally {presenter.Session.Hide();}
    }
}
#endif
