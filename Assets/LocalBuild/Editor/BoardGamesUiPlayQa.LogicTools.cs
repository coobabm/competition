#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Games;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class BoardGamesUiPlayQa
{
    static string LogicMode=>SessionState.GetString(Key+".Mode","games");
    static string SelectedDetail=>
        (LogicMode=="tools"?"Optional logic tools only; full native rounds were not run. ":"Actual Main boards, unrigged full rounds, rules pauses and Files-to-Train installation; recorder purchase uses the core transaction. ")+
        (LogicMode=="games"?"":"Bodu/Tieba public-image navigation, actual LCD mapped drag through live top-raycast hits, purchased/hidden/save-switch gates, and both verified composers with real isolated snapshot rebinds. Chinese/English LCD captures and TMP checks. Unlocks and window arrangements are fixture setup, not purchase/resize interaction tests. ")+
        "Synthetic pointer down/up/drag/scroll through production LCD input, not physical OS mouse or Player-build certification. Duplicate Files drop and stale composer callbacks are explicit adversarial guards. No model execution or injected game outcomes.";
    static IEnumerator SelectedChecks()
    {
        if(LogicMode!="tools"){var games=Checks();while(games.MoveNext())yield return games.Current;}
        if(LogicMode!="games"){var logic=LogicToolsChecks();while(logic.MoveNext())yield return logic.Current;}
    }
    static IEnumerator ToolWait(float seconds)
    {double until=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<until)yield return null;}
    static string ToolSavePath(string id)=>Path.Combine(output,"Save",id+".json");
    static void WriteToolSave(string id,int level,int index)
    {
        var lab=new XgSim();lab.S.stage=5;lab.S.handCorrect=12;lab.S.unlocked.Add("train.curve");lab.LifeEventIntervalMode=0;
        for(int i=1;i<=level;i++)lab.S.unlocked.Add("board.logic"+i);
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000+index;
        house.S.gpuModels.Clear();house.S.gpuModels.Add("gtx980ti");house.S.gpuCount=1;house.S.labState=JsonUtility.ToJson(lab.S);
        File.WriteAllText(ToolSavePath(id),JsonUtility.ToJson(house.S));
    }
    static void LoadToolSave(string id)
    {
        var prior=ctl.Sim;var state=JsonUtility.FromJson<GameState>(File.ReadAllText(ToolSavePath(id)));
        Rebind(new ChapterOneSim(state));ctl.Sim.English=language==GameText.English;ctl.Sim.LifeEventIntervalMode=0;
        Require(!ReferenceEquals(prior,ctl.Sim),"Actual runtime/controller rebind from serialized isolated save: "+id);
        Require(runtime.Sim.S.money==state.money,"Loaded snapshot owns the active household");
    }
    static void CloseOtherToolWindows(params WindowManager[] keep)
    {
        foreach(var window in UnityEngine.Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(window.isOn&&!keep.Contains(window))window.CloseWindow();
    }
    static void PlaceToolWindow(WindowManager window,bool? left)
    {
        var rect=(RectTransform)window.transform;var parent=rect.parent as RectTransform;
        Require(parent!=null,"Native window has layout parent");
        float width=left.HasValue?(parent.rect.width-64)/2:Mathf.Min(1266,parent.rect.width-64);
        float height=Mathf.Min(763,parent.rect.height-120);
        Require(width>400&&height>400,"Actual desktop is large enough for two exposed native windows");
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,height);
        rect.anchoredPosition=new Vector2(left.HasValue?(left.Value?-1:1)*(width/2+16):0,10);
        // Window placement is fixture setup; live native content, masks and raycasts remain enabled.
        Canvas.ForceUpdateCanvases();
    }
    static void ClickToolText(Transform root,string zh,string en)
    {
        var button=root.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text.Contains(zh)||t.text.Contains(en)));
        Require(button!=null,"Visible public content link: "+en);ClickRect((RectTransform)button.transform);
    }
    static XgBoardPictureDrag PublicPicture(Component source)
    {
        var drag=source.GetComponentsInChildren<XgBoardPictureDrag>(true).FirstOrDefault(d=>d.isActiveAndEnabled&&d.gameObject.activeInHierarchy);
        Require(drag!=null,"Real page owns draggable board image");
        var image=drag.GetComponent<Image>();Require(image!=null&&image.sprite!=null&&image.sprite.texture.width==240&&image.sprite.texture.height==240,"Actual public picture texture is present");
        return drag;
    }
    static void InspectPublicPicture(Component page,XgBoardPictureDrag picture,string tag)
    {
        var content=page is BoduView?Field<RectTransform>(page,"content"):Field<RectTransform>(page,"scrollContent");int line=0;
        foreach(var label in content.GetComponentsInChildren<TMP_Text>())if(!string.IsNullOrWhiteSpace(label.text))Inspect(label,tag+" public line "+line++);
        var rect=(RectTransform)picture.transform;
        foreach(float x in new[]{.1f,.9f})foreach(float y in new[]{.1f,.9f})
        {
            var point=LcdPoint(rect,new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,x),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,y)));
            Require(ExecuteEvents.GetEventHandler<IDragHandler>(ToolHit(point).gameObject)==picture.gameObject,"Public image corner is exposed inside its real scroll viewport: "+tag);
        }
    }
    static bool PictureFullyExposed(XgBoardPictureDrag picture)
    {
        var rect=(RectTransform)picture.transform;
        foreach(float x in new[]{.1f,.9f})foreach(float y in new[]{.1f,.9f})
        {
            if(!TryLcdPoint(rect,new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,x),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,y)),out var point))return false;
            var hit=LcdHit(point).gameObject;
            if(hit==null||ExecuteEvents.GetEventHandler<IDragHandler>(hit)!=picture.gameObject)return false;
        }
        return true;
    }
    static IEnumerator ExposePublicPicture(Component page)
    {
        var scroll=Field<ScrollRect>(page,"scroll");
        for(int step=0;step<10;step++)
        {
            if(PictureFullyExposed(PublicPicture(page)))yield break;
            var e=Pointer(scroll.viewport);var hit=ToolHit(e.position);e.scrollDelta=new Vector2(0,-2);
            Require(ExecuteEvents.GetEventHandler<IScrollHandler>(hit.gameObject)==scroll.gameObject,"Native scroll event routes to public page viewport");
            LcdSurface().OnScroll(e);yield return null;yield return null;
        }
        throw new InvalidOperationException("Public image cannot be fully exposed through the actual scroll UI");
    }
    static RaycastResult ToolHit(Vector2 position)
    {
        var hit=LcdHit(position);Require(hit.gameObject!=null,"Pointer intersects an actual LCD source graphic");return hit;
    }
    static Vector2 ExposedPictureTarget(XgBoardPictureDrop target)
    {
        var rect=(RectTransform)target.transform;
        for(int y=1;y<10;y++)for(int x=1;x<10;x++)
        {
            if(!TryLcdPoint(rect,new Vector2(Mathf.Lerp(rect.rect.xMin,rect.rect.xMax,x/10f),Mathf.Lerp(rect.rect.yMin,rect.rect.yMax,y/10f)),out var point))continue;
            var hit=LcdHit(point).gameObject;
            if(hit!=null&&ExecuteEvents.GetEventHandler<IDropHandler>(hit)==target.gameObject)return point;
        }
        throw new InvalidOperationException("Reader has no exposed raycast point; do not bypass occlusion");
    }
    static PointerEventData BeginPicturePointer(XgBoardPictureDrag source)
    {
        var e=Pointer((RectTransform)source.transform);RequireTop((RectTransform)source.transform,e);var hit=ToolHit(e.position);
        Require(ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject)==source.gameObject,"Top graphic routes actual pointer drag to image component");
        var surface=LcdSurface();surface.OnPointerDown(e);e.position+=new Vector2(20,8);surface.OnDrag(e);
        Require(ReferenceEquals(Field<XgSim>(source,"owner"),ctl.Sim),"BeginDrag captured actual current save");return e;
    }
    static IEnumerator MovePicturePointer(PointerEventData e,Vector2 destination)
    {
        Vector2 start=e.position;
        for(int i=1;i<=8;i++)
        {
            Vector2 next=Vector2.Lerp(start,destination,i/8f);e.delta=next-e.position;e.position=next;
            LcdSurface().OnDrag(e);
            yield return null;
        }
    }
    static GameObject EndPicturePointer(PointerEventData e)
    {
        // Production LCD release performs pointer-up, routed drop and end-drag exactly once.
        var hit=LcdHit(e.position);
        var receiver=hit.gameObject==null?null:ExecuteEvents.GetEventHandler<IDropHandler>(hit.gameObject);
        LcdSurface().OnPointerUp(e);return receiver;
    }
    static IEnumerator RealPictureDrag(XgBoardPictureDrag source,XgBoardPictureDrop target)
    {
        Vector2 destination=ExposedPictureTarget(target);var e=BeginPicturePointer(source);var travel=MovePicturePointer(e,destination);
        while(travel.MoveNext())yield return travel.Current;
        Require(EndPicturePointer(e)==target.gameObject,"Drop was dispatched through the actual exposed reader's canvas hierarchy");yield return null;
    }
    static void InspectReader(string tag)
    {
        foreach(string name in new[]{"readerTitle","readerHelp","readerResult"})Inspect(Field<TMP_Text>(hub,name),tag+" "+name);
    }
    static IEnumerator PublicPictureChecks(bool forum,string tag)
    {
        LoadToolSave("logic-a");gameWindow.OpenWindow();CloseOtherToolWindows(gameWindow);var wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;
        ClickNamed(hub.transform,"Nav board.reader");yield return null;
        Component sourceView;WindowManager sourceWindow;
        if(forum)
        {
            var view=UnityEngine.Object.FindAnyObjectByType<TiebaView>(FindObjectsInactive.Include);Require(view!=null,"Main Tieba view exists");view.Open("home");sourceView=view;sourceWindow=view.GetComponentInParent<WindowManager>();
            wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;ClickToolText(view.transform,"周末小题","Weekend puzzle");
        }
        else
        {
            var view=UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);Require(view!=null,"Main Bodu view exists");view.Open("");sourceView=view;sourceWindow=view.GetComponentInParent<WindowManager>();
            wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;
            var input=Field<TMP_InputField>(view,"search");ClickRect((RectTransform)input.transform);input.text="gomoku";ClickNamed(view.transform,"Go");
            wait=ToolWait(.7f);while(wait.MoveNext())yield return wait.Current;ClickToolText(view.transform,"巷口棋社","Lane Chess Club");
        }
        wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;
        PlaceToolWindow(sourceWindow,true);PlaceToolWindow(gameWindow,false);sourceWindow.FocusToWindow();yield return null;yield return null;
        var expose=ExposePublicPicture(sourceView);while(expose.MoveNext())yield return expose.Current;
        var source=PublicPicture(sourceView);var target=Field<XgBoardPictureDrop>(hub,"boardReader");
        Require(source.GetComponentInParent<WindowManager>()!=target.GetComponentInParent<WindowManager>(),"Image and reader are in distinct real native windows");
        double go=ctl.Sim.Labels("go");int records=ctl.Sim.S.boardRounds.Count,files=ctl.Sim.S.dataFiles.Count;
        InspectPublicPicture(sourceView,source,tag);
        Capture(tag+"-before-drag.png",(RectTransform)sourceView.transform);
        var drag=RealPictureDrag(source,target);while(drag.MoveNext())yield return drag.Current;
        var result=Field<TMP_Text>(hub,"readerResult");Require(result.text.Contains("F6")&&result.text.Contains("8"),"Reader recognized eight real image stones and independently selected F6: "+tag);
        yield return null;InspectReader(tag);Capture(tag+"-recognized.png",(RectTransform)hub.transform);
        Require(ctl.Sim.Labels("go")==go&&ctl.Sim.S.boardRounds.Count==records&&ctl.Sim.S.dataFiles.Count==files,"Public image reading creates no samples/rounds/files");
        // Closed target: send a real pointer path to the same screen position; no hidden target handler is invoked.
        Vector2 oldPoint=ExposedPictureTarget(target);string before=result.text;var hidden=BeginPicturePointer(PublicPicture(sourceView));gameWindow.CloseWindow();
        wait=ToolWait(.8f);while(wait.MoveNext())yield return wait.Current;var move=MovePicturePointer(hidden,oldPoint);while(move.MoveNext())yield return move.Current;
        Require(EndPicturePointer(hidden)!=target.gameObject&&result.text==before,"Closed target is absent from actual drop routing");
        gameWindow.OpenWindow();wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;PlaceToolWindow(gameWindow,false);yield return null;
        // Closing the source cancels its payload even when the destination remains visibly reachable.
        before=result.text;var hiddenSource=BeginPicturePointer(PublicPicture(sourceView));sourceWindow.CloseWindow();
        wait=ToolWait(.8f);while(wait.MoveNext())yield return wait.Current;move=MovePicturePointer(hiddenSource,ExposedPictureTarget(target));while(move.MoveNext())yield return move.Current;
        Require(EndPicturePointer(hiddenSource)==target.gameObject&&result.text==before,"Visible target rejects payload cancelled by closing its real source window");
        sourceWindow.OpenWindow();wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;PlaceToolWindow(sourceWindow,true);yield return null;
        expose=ExposePublicPicture(sourceView);while(expose.MoveNext())yield return expose.Current;
        // Keep source pointer payload from A; the destination is rebound to a different saved B before release.
        var stale=BeginPicturePointer(PublicPicture(sourceView));var oldSim=ctl.Sim;LoadToolSave("logic-b");yield return null;yield return null;
        Require(!ReferenceEquals(oldSim,ctl.Sim)&&ReferenceEquals(target.Owner,ctl.Sim),"Visible destination now belongs to real second loaded save");
        before=result.text;move=MovePicturePointer(stale,ExposedPictureTarget(target));while(move.MoveNext())yield return move.Current;
        Require(EndPicturePointer(stale)==target.gameObject&&result.text==before,"Actual visible new-save drop rejects old-save pointer payload");yield return null;
        Capture(tag+"-stale-save-rejected.png",(RectTransform)hub.transform);
        wait=ToolWait(.7f);while(wait.MoveNext())yield return wait.Current;expose=ExposePublicPicture(sourceView);while(expose.MoveNext())yield return expose.Current;
        drag=RealPictureDrag(PublicPicture(sourceView),target);while(drag.MoveNext())yield return drag.Current;Require(result.text.Contains("F6"),"Fresh drag in new save succeeds after stale rejection");
        // An unpurchased save has no reader entry or active target. Drop routing cannot reach it.
        oldPoint=ExposedPictureTarget(target);LoadToolSave("logic-unbought");yield return null;yield return null;
        var nav=Field<Dictionary<string,Image>>(hub,"navFills")["board.reader"];Require(!nav.gameObject.activeSelf&&!target.gameObject.activeInHierarchy,"Unpurchased save hides entry and target");
        wait=ToolWait(.7f);while(wait.MoveNext())yield return wait.Current;expose=ExposePublicPicture(sourceView);while(expose.MoveNext())yield return expose.Current;
        var gated=BeginPicturePointer(PublicPicture(sourceView));move=MovePicturePointer(gated,oldPoint);while(move.MoveNext())yield return move.Current;
        Require(EndPicturePointer(gated)!=target.gameObject,"Unpurchased target cannot receive actual routed drop");Capture(tag+"-unbought.png",(RectTransform)hub.transform);
        sourceWindow.CloseWindow();
    }
    static IEnumerator ComposerCaptured(bool xiangqi,string tag)
    {
        LoadToolSave("logic-four");CloseOtherToolWindows(gameWindow);gameWindow.OpenWindow();var wait=ToolWait(1);while(wait.MoveNext())yield return wait.Current;
        PlaceToolWindow(gameWindow,null);yield return null;
        var captured=new HashSet<string>();Action change=()=>LoadToolSave("logic-unbought");
        IEnumerator checks=xiangqi?XiangqiComposerUiQaHelper.CheckMain(hub,ctl,change):BoardComposerUiQaHelper.CheckMain(hub,ctl,change);
        while(checks.MoveNext())
        {
            yield return checks.Current;
            var chess=xiangqi?null:Field<ChessGame>(hub,"composedBoard");var chinese=xiangqi?Field<XiangqiRookEndgame>(hub,"xiangqiBoard"):null;
            if(chess==null&&chinese==null)continue;
            string state=(xiangqi?chinese.Checkmate:chess.Result==ChessGame.WhiteWins)?"solved":"composed";
            if(!captured.Add(state))continue;
            yield return null; // Allow an actual LCD render before copying its render target.
            foreach(string name in xiangqi?new[]{"xiangqiTitle","xiangqiHelp","xiangqiStatus","xiangqiNewLabel","xiangqiResetLabel"}:new[]{"composerTitle","composerHelp","composerStatus","composeLabel","resetComposeLabel"})Inspect(Field<TMP_Text>(hub,name),tag+" "+state+" "+name);
            Capture(tag+"-"+state+".png",(RectTransform)hub.transform);
        }
        Require(captured.Contains("composed")&&captured.Contains("solved"),"Both actual generated and pointer-solved native boards captured: "+tag);
        Check(true,"Composer helper completed real entry/reset/reopen/save-switch checks: "+tag);
        yield return null;Capture(tag+"-new-save-unbought.png",(RectTransform)hub.transform);
    }
    static IEnumerator LogicToolsChecks()
    {
        Require(EventSystem.current!=null,"Main native EventSystem exists");
        WriteToolSave("logic-unbought",0,1);WriteToolSave("logic-a",2,2);WriteToolSave("logic-b",2,3);WriteToolSave("logic-four",4,4);
        foreach(string lang in new[]{"zh-CN",GameText.English})
        {
            language=lang;GameText.PublishChanged();ctl.Sim.English=lang==GameText.English;string suffix=lang==GameText.English?"en":"zh";yield return null;
            foreach(bool forum in new[]{false,true}){var pictures=PublicPictureChecks(forum,(forum?"tieba":"bodu")+"-picture-"+suffix);while(pictures.MoveNext())yield return pictures.Current;}
            foreach(bool xiangqi in new[]{false,true}){var compose=ComposerCaptured(xiangqi,(xiangqi?"xiangqi":"chess")+"-composer-"+suffix);while(compose.MoveNext())yield return compose.Current;}
        }
    }
}
#endif
