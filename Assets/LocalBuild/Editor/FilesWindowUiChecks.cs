#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Michsky.DreamOS;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

// Embedded in an isolated live-host Main fixture. Does not open/save scenes or touch player saves.
public static class FilesWindowUiChecks
{
    public static void Run()=>SidebarLayoutChecks.RunFiles();
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags).GetValue(o);
    static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException("Files UI: "+message);}
    static double WaitUntil()=>EditorApplication.timeSinceStartup+.45;
    static void Bind(XingGuangController ctl,XgFilesWindow files,XgSim sim)
    {
        typeof(XingGuangController).GetProperty("Sim",Flags).GetSetMethod(true).Invoke(ctl,new object[]{sim});
        ctl.View.Bind(ctl);ctl.View.Open("train");ctl.View.Refresh(true);files.Bind(sim);
    }
    static XgSim Fresh()
    {
        var s=new XgSim();s.BeginOpening();s.S.handCorrect=12;
        s.S.labels.Add(new XgLabelCount{dataset=XgMemes.SenseDesk,count=12});
        s.S.unlocked.Add("train.curve");s.S.teachingFlow=true;
        s.S.teachIntroSaid=s.S.teachThirdSaid=s.S.teachTreeSaid=s.S.teachTrainSaid=true;
        s.S.trainingLessonObservedStage=s.S.stage;s.S.spiderIntroDone=true;
        return s;
    }
    static XgFilesRow Row(XgFilesWindow files,string id)
    {
        var row=files.client.GetComponentsInChildren<XgFilesRow>().FirstOrDefault(x=>x.fileId==id);
        Need(row!=null,"visible row "+id);return row;
    }
    static AeroLcdSourceBridge Bridge()
    {
        var b=UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Need(b!=null&&b.inputSurface!=null&&b.sourceCamera!=null,"actual LCD bridge");return b;
    }
    static PointerEventData Pointer(RectTransform target)
    {
        var b=Bridge();var surface=b.inputSurface;var src=b.sourceCamera.WorldToScreenPoint(target.TransformPoint(target.rect.center));
        var display=surface.displayRect;var r=display.rect;var tex=b.sourceCamera.targetTexture;
        Need(src.z>0&&src.x>=0&&src.x<tex.width&&src.y>=0&&src.y<tex.height,
            "target outside source camera viewport: "+src+" texture="+tex.width+"x"+tex.height+" / "+Describe(target));
        float u=src.x/tex.width,v=src.y/tex.height;if(surface.flipY)v=1-v;
        var camera=(Camera)typeof(AeroLcdInputSurface).GetMethod("DisplayEventCamera",Flags).Invoke(surface,null);
        var p=RectTransformUtility.WorldToScreenPoint(camera,display.TransformPoint(new Vector3(r.xMin+u*r.width,r.yMin+v*r.height,0)));
        Need(surface.TryMap(p,out var mapped)&&Vector2.Distance(mapped,new Vector2(src.x,src.y))<1,"LCD coordinate roundtrip");
        return new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left};
    }
    static void AssertChineseGlyphs(XgFilesWindow files)
    {
        foreach(var label in files.window.GetComponentsInChildren<TMP_Text>())
        {
            if(!label.gameObject.activeInHierarchy)continue;
            label.ForceMeshUpdate(true,true);
            foreach(char c in label.text)
                if(c>=0x4e00&&c<=0x9fff)
                    Need(label.font!=null&&label.font.HasCharacter(c,true,false),"rendered Chinese glyph missing: "+c+" in "+label.name);
        }
        foreach(char c in files.desktopLabel.text)
            if(c>=0x4e00&&c<=0x9fff)Need(files.desktopLabel.font.HasCharacter(c,true,false),"desktop name glyph missing: "+c);
    }
    public static void Click(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();
        var surface=Bridge().inputSurface;Need(surface.inputEnabled&&surface.isActiveAndEnabled,"LCD input available");
        var p=Pointer(target);surface.OnPointerEnter(p);
        var data=Field<PointerEventData>(surface,"hoverData");var hit=data?.pointerCurrentRaycast.gameObject;
        Need(hit!=null&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit)==target.gameObject,"click not intercepted: "+target.name+" / "+hit+" / "+Describe(target));
        surface.OnPointerDown(p);surface.OnPointerUp(p);
    }
    static bool Restored(WindowManager window)
    {
        if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
        foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))
            if(group.alpha<.95f||!group.blocksRaycasts||!group.interactable)return false;
        var animator=window.windowAnimator;
        return animator==null||!animator.isActiveAndEnabled||(!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1f);
    }
    static string Describe(RectTransform target)
    {
        var b=new StringBuilder();b.Append("target=").Append(target.name).Append(" active=").Append(target.gameObject.activeInHierarchy)
            .Append(" rect=").Append(target.rect).Append(" anchors=").Append(target.anchorMin).Append('/').Append(target.anchorMax)
            .Append(" anchored=").Append(target.anchoredPosition).Append(" scale=").Append(target.lossyScale);
        for(var tr=target.transform;tr!=null;tr=tr.parent)
        {
            b.Append(" | ").Append(tr.name).Append(" active=").Append(tr.gameObject.activeInHierarchy);
            var group=tr.GetComponent<CanvasGroup>();if(group!=null)b.Append(" alpha=").Append(group.alpha).Append(" blocks=").Append(group.blocksRaycasts).Append(" interact=").Append(group.interactable).Append(" ignoreParents=").Append(group.ignoreParentGroups);
            var w=tr.GetComponent<WindowManager>();if(w==null)continue;
            b.Append(" isOn=").Append(w.isOn).Append(" frame=").Append(w.windowContainer.rect).Append(" localScale=").Append(w.windowContainer.localScale);
            var a=w.windowAnimator;if(a!=null&&a.gameObject.activeInHierarchy)
            {var state=a.GetCurrentAnimatorStateInfo(0);b.Append(" animatorEnabled=").Append(a.enabled).Append(" state=").Append(state.fullPathHash).Append(" normalized=").Append(state.normalizedTime).Append(" transition=").Append(a.IsInTransition(0));}
        }
        return b.ToString();
    }
    static void BeginDrag(RectTransform row)
    {
        var surface=Bridge().inputSurface;var p=Pointer(row);surface.OnPointerEnter(p);surface.OnPointerDown(p);
        p.position+=new Vector2(20,8);surface.OnDrag(p);
    }
    static void EndDrag(RectTransform target)
    {
        var surface=Bridge().inputSurface;var p=Pointer(target);surface.OnDrag(p);
        var data=Field<PointerEventData>(surface,"hoverData");var hit=data?.pointerCurrentRaycast.gameObject;
        Need(hit!=null&&hit.GetComponentInParent<XgFilesTrainDrop>()!=null,"real drop destination is the training root: "+hit);
        surface.OnPointerUp(p);
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<string> capture,Action<string> check)
    {
        var rt=ctl.runtime;Need(rt!=null&&!rt.useDiskSave&&!rt.tickEnabled&&!rt.TestMode,"isolated real-host Main required");
        var files=rt.GetComponent<XgFilesWindow>();Need(files!=null&&files.window!=null,"install files shell into Main before running");
        var original=ctl.Sim;bool enabled=ctl.enabled;string house=JsonUtility.ToJson(rt.Sim.S);string tab=ctl.View.Tab;
        var originalFrame=files.window.windowContainer;var originalMin=originalFrame.offsetMin;var originalMax=originalFrame.offsetMax;
        ctl.enabled=false;
        try
        {
            var urlChecks=FilesDataUrlUiChecks.Frames(ctl,language,capture,check);try{while(urlChecks.MoveNext())yield return urlChecks.Current;}finally{(urlChecks as IDisposable)?.Dispose();}
            language(false);var sim=Fresh();Bind(ctl,files,sim);rt.Sim.S.money=1000;
            double until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(!files.shortcutRoot.activeSelf&&!files.CanBrowse,"before purchase folder hidden");files.Show();Need(!files.window.isOn,"before purchase cannot open");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-01-unowned.png");check("unowned folder hidden and Show denied");
            var node=XgCatalog.Node(XgSim.DataEntryNode);Need(node!=null&&sim.NodeCost(node)==50,"ordinary data import price is 50");
            rt.Sim.S.money=49;Need(!sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==49,"insufficient funds do not grant access");
            rt.Sim.S.money=1000;Need(sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==950,"official purchase charges 50 once");
            Need(!sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==950,"duplicate purchase no charge");
            ctl.Window.CloseWindow();until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.shortcutRoot.activeInHierarchy&&files.CanBrowse,"purchase reveals actual shortcut");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-02-owned-desktop.png");Click((RectTransform)files.shortcutRoot.transform);
            until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.window.isOn&&files.window.gameObject.activeInHierarchy,"LCD shortcut opened native window");
            Need(files.titleLabel.text==sim.AiDisplayName+"的资料","player name in title");AssertChineseGlyphs(files);
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-03-open-root-zh.png");check("LCD shortcut opens native player-named folder after purchase");
            Click((RectTransform)Row(files,"training").transform);Need(Field<TMP_Text>(files,"preview").text.Contains("没有"),"empty training log is real empty state");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-04-empty-log.png");
            Click((RectTransform)Row(files,"packs").transform);Click((RectTransform)Row(files,"packs").transform);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            Need(Field<string>(files,"folder")=="packs","LCD double click entered data folder");Need(files.client.GetComponentsInChildren<XgFilesRow>().Length==0,"no fabricated historical files");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-05-empty-data.png");
            language(true);sim.English=true;until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.titleLabel.text==sim.AiDisplayName+"'s files","live English title");yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-06-empty-data-en.png");
            // Explicit fixture records, not claimed as natural progression or old player history.
            sim.RecordTeachingQuestion("验收中的手写数字","Fixture handwriting",true,true,"已保存的题图","Recorded question image","mnist",
                new XgCard{dataset="mnist",digit=3,seed=7,teachingImageKind="handwriting28"});
            sim.S.desksOpen.Add("mnist");
            var a=new XgDownload{fileId=Guid.NewGuid().ToString("N"),dataset="mnist",offer="mnist.pack",sizeMB=1,doneMB=1,batches=1};
            var b=new XgDownload{fileId=Guid.NewGuid().ToString("N"),dataset="mnist",offer="mnist.pack",sizeMB=1,doneMB=1,batches=1};
            sim.S.dataFiles.Add(a);sim.S.dataFiles.Add(b);sim.S.batchesArrived.Add(new XgLabelCount{dataset="mnist",count=2});
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-07-ready-fixture-data.png");
            var back=files.client.Find("Toolbar/Back") as RectTransform;Click(back);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            Click((RectTransform)Row(files,"answers").transform);Need(Field<TMP_Text>(files,"preview").text.Contains("Fixture handwriting"),"CSV reads actual teaching notebook");yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-08-real-record-csv.png");
            Click((RectTransform)Row(files,"images").transform);Click((RectTransform)Row(files,"images").transform);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            var imageRow=files.client.GetComponentsInChildren<XgFilesRow>().FirstOrDefault();Need(imageRow!=null,"recorded unlocked image present");Click((RectTransform)imageRow.transform);
            var image=Field<UnityEngine.UI.RawImage>(files,"previewImage");Need(image.gameObject.activeSelf&&image.texture!=null,"preview uses actual recorded image");yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-09-recorded-image.png");
            check("empty state, actual recorded notebook CSV and recorded image preview");
            Click(back);yield return null;Canvas.ForceUpdateCanvases();yield return null;Need(!files.client.GetComponentsInChildren<XgFilesRow>().Any(x=>x.fileId=="models"),"checkpoint folder stays locked");
            // Begin and cancel through real source LCD before changing to another saved simulation.
            Click((RectTransform)Row(files,"packs").transform);Click((RectTransform)Row(files,"packs").transform);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            BeginDrag((RectTransform)Row(files,a.fileId).transform);Need(files.ActivePayload!=null,"actual LCD row drag creates payload");var stale=files.ActivePayload;
            var next=Fresh();Bind(ctl,files,next);Need(!stale.IsCurrent&&!stale.CanInstallInto(next)&&files.ActivePayload==null,"save swap invalidates payload");Bridge().inputSurface.CancelInteraction();
            until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(!files.window.isOn&&!files.shortcutRoot.activeSelf&&!a.installed,"new unowned save closes old folder without installation");yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-10-swapped-unowned.png");
            Bind(ctl,files,sim);files.Show();until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Click((RectTransform)Row(files,"packs").transform);Click((RectTransform)Row(files,"packs").transform);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            BeginDrag((RectTransform)Row(files,a.fileId).transform);Need(files.ActivePayload!=null,"second source drag begins");files.window.MinimizeWindow();Need(files.ActivePayload==null,"minimize cancels drag immediately");Bridge().inputSurface.CancelInteraction();
            check("real LCD source drag, cross-save cancellation and minimize lifecycle");
            files.Show();ctl.Window.OpenWindow();ctl.View.Open("train");
            // Preserve immediate restore after minimize. Observe natural completion; never force alpha/raycast/animation.
            until=EditorApplication.timeSinceStartup+3;int restoreFrames=0;
            while(restoreFrames<3)
            {
                restoreFrames=Restored(files.window)&&Restored(ctl.Window)?restoreFrames+1:0;
                if(EditorApplication.timeSinceStartup>=until)
                {
                    capture("files-10a-restore-timeout.png");
                    Need(false,"restore remained noninteractive: "+Describe(files.window.windowContainer)+" / TRAIN "+Describe(ctl.Window.windowContainer));
                }
                yield return null;
            }
            check("immediate minimize/reopen reaches naturally visible interactive windows for three frames");
            var sourceFrame=files.window.windowContainer;var targetFrame=ctl.Window.windowContainer;
            // Deliberate side-by-side QA layout, not a change to production window defaults.
            sourceFrame.anchorMin=sourceFrame.anchorMax=new Vector2(.5f,.5f);sourceFrame.sizeDelta=new Vector2(700,650);sourceFrame.anchoredPosition=new Vector2(-490,0);
            targetFrame.anchorMin=targetFrame.anchorMax=new Vector2(.5f,.5f);targetFrame.sizeDelta=new Vector2(850,650);targetFrame.anchoredPosition=new Vector2(330,0);
            files.window.FocusToWindow();Canvas.ForceUpdateCanvases();files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;
            var selected=files.client.Find("Toolbar/SelectReady") as RectTransform;
            capture("files-10b-batch-layout-before-click.png");
            Debug.Log("Files batch layout: "+Describe(selected)+" / TRAIN "+Describe(targetFrame));
            Click(selected);
            BeginDrag((RectTransform)Row(files,a.fileId).transform);Need(files.ActivePayload!=null&&files.ActivePayload.FileIds.Count==2,"ready selection creates immutable batch");
            sim.S.unlocked.Remove(XgSim.DataEntryNode);EndDrag(ctl.View.Train.root);
            Need(!a.installed&&!b.installed,"revoked gate blocks actual target drop");sim.S.unlocked.Add(XgSim.DataEntryNode);
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;Click(selected);BeginDrag((RectTransform)Row(files,a.fileId).transform);
            Need(files.ActivePayload!=null&&files.ActivePayload.FileIds.Count==2,"batch available after gate restored");
            EndDrag(ctl.View.Train.root);Need(a.installed&&b.installed&&files.ActivePayload==null,"actual LCD batch drop installs both and ends ghost");
            Need(ctl.View.Train.root.GetComponent<XgFilesTrainDrop>().LastInstalled==2,"target reports actual installed count");
            Need(!sim.InstallDataFile(a.fileId)&&!sim.InstallDataFile(b.fileId),"duplicate core install is a no-op");
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-11-real-batch-installed.png");
            check("actual LCD target raycast: revoked gate rejects, ready selection batch installs once");
            Debug.Log("FilesWindowUiChecks PASS: real paid gate, LCD opening/records/source drag/batch drop, cross-save and minimize cancellation. Ctrl-key automation remains pending.");
        }
        finally
        {
            Bridge().inputSurface.CancelInteraction();files.CancelDrag();files.window.CloseWindow();
            JsonUtility.FromJsonOverwrite(house,rt.Sim.S);originalFrame.offsetMin=originalMin;originalFrame.offsetMax=originalMax;
            language(original.English);Bind(ctl,files,original);ctl.View.Open(tab);ctl.enabled=enabled;
            if(!ctl.Window.isOn)ctl.Window.OpenWindow();
        }
    }
}
#endif
