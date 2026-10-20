#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.OS;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using GuideNoteT = LingGuangV05.Desktop.Story.GuideNote;

// Embedded in an isolated live-host Main fixture. Does not open/save scenes or touch player saves.
public static class FilesWindowUiChecks
{
    public static void Run()=>SidebarLayoutChecks.RunFiles();
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags StaticFlags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags).GetValue(o);
    static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException("Files UI: "+message);}
    static double WaitUntil()=>EditorApplication.timeSinceStartup+.45;
    static RectTransform InstallHint(XingGuangController ctl) => ctl.View.Train.root.Find("DataInstallHint") as RectTransform;
    static string InstallHintText(XingGuangController ctl) => string.Join("\n", InstallHint(ctl).GetComponentsInChildren<TMP_Text>(true).Select(label => label.text));
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
    static object FileNotice(DesktopNotifications service)
    {
        return Field<IList>(service,"pending").Cast<object>().LastOrDefault(item=>Field<string>(item,"channel")=="资料文件夹");
    }
    static IEnumerator BeforeImportFrames(XingGuangController ctl,XgFilesWindow files,Action<string> capture,Action<string> check)
    {
        // Declared capability fixture: purchase and download still use the product's real paths.
        var purchased=new XgSim {GoldChance=0};
        purchased.S.stage=purchased.S.stageVision=purchased.S.stageSequence=purchased.S.fundedAbility=6;
        foreach(string id in new[]{"perceptron","mlp","lenet","resnet","attention"})purchased.S.unlocked.Add(id);
        Bind(ctl,files,purchased);ctl.runtime.Sim.S.money=100000;
        var service=DesktopNotifications.Install(ctl.runtime);Need(service!=null,"real notification service installed");
        double price=purchased.NodeCost(XgCatalog.Node("spam.pack")),before=ctl.Host.Money;
        Need(purchased.BuyDataOffer("spam.pack",ctl.Host)&&Math.Abs(ctl.Host.Money-(before-price))<.001,"actual pack purchase charges its existing price before Data Import");
        Need(!purchased.CanInstallData&&purchased.S.dataFiles.Count==0&&purchased.S.downloadQueue.Count>0,"paid pack is downloading without Data Import");
        Need(files.CanBrowse&&files.shortcutRoot.activeSelf,"download-start event immediately reveals the single existing shortcut");
        Need(!files.window.isOn,"purchase notification does not open or focus the folder before the player clicks");
        var started=FileNotice(service);Need(started!=null,"purchase queues a desktop notification");
        string body=Field<string>(started,"body");
        Need(body.Contains("已加入下载")&&body.Contains("训练数据")&&body.Contains(purchased.DataInstallBlocker())&&!body.Contains("下载完成"),"purchase notice distinguishes downloading and explains the original import gate");
        Field<Action>(started,"open")();
        Need(files.window.isOn&&Field<string>(files,"folder")=="packs","purchase notice opens Training data in the existing folder");
        Need(!purchased.PreviewDataUrl(XgSim.MnistSourceUrl,ctl.Host).canDownload&&!purchased.CanInstallData,"browsing does not grant URL access or Data Import");
        files.window.CloseWindow();
        typeof(XgSim).GetMethod("TickDownloads",Flags).Invoke(purchased,new object[]{purchased.DownloadQueue().Max(item=>item.eta)+1});
        var ready=purchased.S.dataFiles.Single();
        Need(purchased.DataFileReady(ready.fileId)&&!ready.installed&&!purchased.InstallDataFile(ready.fileId),"finished purchase remains uninstalled and core installation stays gated");
        var finished=FileNotice(service);Need(finished!=null,"completion queues a desktop notification");
        body=Field<string>(finished,"body");
        Need(body.Contains("下载完成")&&body.Contains("资料文件夹")&&body.Contains(purchased.DataInstallBlocker()),"completion notice names the desktop folder and locked installation prerequisite");
        var oldClick=Field<Action>(finished,"open");
        var restored=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(purchased.S)));Bind(ctl,files,restored);
        Need(!restored.CanInstallData&&restored.DataFileReady(ready.fileId)&&files.CanBrowse&&files.shortcutRoot.activeSelf,"reloaded old save with an uninstalled paid pack reveals the folder without buying import");
        files.ShowTrainingData(restored);
        Need(files.window.isOn&&Field<string>(files,"folder")=="packs"&&files.titleLabel.text=="资料文件夹"&&files.desktopLabel.text=="资料文件夹","restored pack is browsable with the exact desktop and title label");
        Need(!restored.InstallDataFile(ready.fileId)&&!restored.S.dataFiles.Single().installed&&!restored.PreviewDataUrl(XgSim.MnistSourceUrl,ctl.Host).canDownload,"save reload preserves both install and URL gates");
        double until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
        Canvas.ForceUpdateCanvases();yield return null;capture("files-00-restored-pack-before-import.png");
        Bind(ctl,files,Fresh());oldClick();
        Need(!files.window.isOn&&files.CanBrowse&&files.shortcutRoot.activeSelf,"empty save keeps the folder and rejects a previous save's notification callback");
        check("real paid purchase reveals the folder before Data Import; start/completion notices, restored ready files, install/URL gates and stale callback isolation");
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
    // ───────────── guide-note diagnostics: read-only; never assigns TopStep/TopSim or calls Show/Apply/Go ─────────────
    static string Try(Func<object> read){try{var v=read();return v==null?"null":v.ToString();}catch(Exception e){var x=e is TargetInvocationException&&e.InnerException!=null?e.InnerException:e;return "err:"+x.GetType().Name+":"+x.Message;}}
    static object StaticValue(Type t,string n){var f=t.GetField(n,StaticFlags);if(f!=null)return f.GetValue(null);var p=t.GetProperty(n,StaticFlags);return p?.GetValue(null);}
    /// <summary>The household facts GuideNote.Update passes to XgGuide.Current (its private House(); reads runtime and PlayerPrefs only).</summary>
    static XgGuideHouse NoteHouse(GuideNoteT note)=>(XgGuideHouse)typeof(GuideNoteT).GetMethod("House",Flags).Invoke(note,null);
    /// <summary>XgGuide.PendingDataImport with the same wallet XgGuide.Current builds (private, side-effect free).</summary>
    static string PendingImport(XgSim sim,double money,double vram)
    {
        var walletType=typeof(XgGuide).GetNestedType("Wallet",BindingFlags.NonPublic);
        var wallet=Activator.CreateInstance(walletType,true);
        walletType.GetField("money").SetValue(wallet,money);walletType.GetField("vram").SetValue(wallet,vram);
        var step=(XgGuideStep)typeof(XgGuide).GetMethod("PendingDataImport",StaticFlags).Invoke(null,new[]{sim,wallet});
        return step==null?"null":step.id+"/"+step.arg;
    }
    static string Ids(System.Collections.Generic.List<XgGuideStep> steps)=>steps==null?"null":"["+string.Join(",",steps.Select(s=>s.id+(string.IsNullOrEmpty(s.arg)?"":"/"+s.arg)))+"]";
    static string SimName(XgSim sim,XgSim guided,XingGuangController ctl)=>sim==null?"null":ReferenceEquals(sim,guided)?"guided":ReferenceEquals(sim,ctl.Sim)?"ctl.Sim(not guided)":"other";
    static string GuideDiagnostics(GuideNoteT note,XingGuangController ctl,XgFilesWindow files,XgSim guided,XgDownload file)
    {
        var rt=ctl.runtime;var b=new StringBuilder();
        var top=GuideNoteT.TopStep;
        b.Append("TopStep=").Append(top==null?"null":top.id+"/"+top.arg)
            .Append(" TopSim=").Append(SimName(GuideNoteT.TopSim,guided,ctl))
            .Append(" | note: enabled=").Append(note!=null&&note.isActiveAndEnabled);
        if(note!=null)
            b.Append(" busy=").Append(Try(()=>Field<bool>(note,"busy")))
                .Append(" shownFor=").Append(Try(()=>SimName(Field<XgSim>(note,"shownFor"),guided,ctl)))
                .Append(" row0Step=").Append(Try(()=>{var s=Field<XgGuideStep>(Field<Array>(note,"rows").GetValue(0),"step");return s==null?"null":s.id+"/"+s.arg;}))
                .Append(" row0Active=").Append(Try(()=>{var r=Field<RectTransform>(Field<Array>(note,"rows").GetValue(0),"rt");return r==null?"unbuilt":(object)r.gameObject.activeInHierarchy;}))
                .Append(" noteVisible=").Append(Try(()=>{var n=Field<RectTransform>(note,"note");return n==null?"unbuilt":(object)n.gameObject.activeInHierarchy;}))
                .Append(" collapsed=").Append(Try(()=>StaticValue(typeof(GuideNoteT),"collapsed")))
                .Append(" noteLabIsCtl=").Append(Try(()=>ReferenceEquals(Field<XingGuangController>(note,"lab"),ctl)))
                .Append(" noteRuntimeIsRt=").Append(Try(()=>ReferenceEquals(Field<object>(note,"runtime"),rt)))
                .Append(" cutscene=").Append(Try(()=>typeof(GuideNoteT).GetProperty("CutscenePlaying",Flags).GetValue(note)));
        b.Append(" | ready inputs: exclusive=").Append(Try(()=>StoryFocus.Exclusive))
            .Append(" testMode=").Append(Try(()=>rt.TestMode)).Append(" inPrologue=").Append(Try(()=>rt.Sim.InPrologue))
            .Append(" prologueDesk=").Append(Try(()=>LingGuangV05.Desktop.Story.PrologueDirector.Desk!=null))
            .Append(" setupShowing=").Append(Try(()=>ctl.View.SetupShowing))
            .Append(" view=").Append(Try(()=>ctl.View.Visible+"/"+ctl.View.Tab))
            .Append(" acquisitionCurrent=").Append(Try(()=>XgAppAcquisitionHost.Instance==null?"none":(object)XgAppAcquisitionHost.Instance.Current));
        b.Append(" | guide: pendingImport=").Append(Try(()=>PendingImport(guided,ctl.Host.Money,ctl.Host.VramMB)))
            .Append(" current=").Append(Try(()=>note==null?"no note":Ids(XgGuide.Current(guided,rt.Sim.S.money,NoteHouse(note)))))
            .Append(" house=").Append(Try(()=>{var h=NoteHouse(note);return "installed:"+h.appInstalled+" unpaid:"+h.unpaidPower+" breaker:"+h.breakerTripped+" noGpu:"+h.noGpu+" overloaded:"+h.overloaded+" vram:"+h.vramMB;}))
            .Append(" money=").Append(Try(()=>ctl.Host.Money));
        b.Append(" | train gate: tabTrain=").Append(XgGuide.TabOpen(guided,XgGuide.Lab,"train")).Append(" tabTree=").Append(XgGuide.TabOpen(guided,XgGuide.Lab,"tree"))
            .Append(" featureTrain=").Append(guided.FeatureVisible("train"))
            .Append(" unlockedSeq/Vis=").Append(guided.TrainingUnlocked(XgTrack.Sequence)).Append('/').Append(guided.TrainingUnlocked(XgTrack.Vision))
            .Append(" openingFocus=").Append(guided.OpeningFocus).Append(" teachingFlow=").Append(guided.S.teachingFlow)
            .Append(" spamLabels=").Append(guided.Labels("spam")).Append(" spamSamples=").Append(guided.Samples("spam")).Append(" need=").Append(XgCatalog.SamplesToTrain)
            .Append(" trainedSamples=").Append(guided.TrainedSamples).Append(" canInstall=").Append(guided.CanInstallData)
            .Append(" file=").Append(file==null?"null":file.fileId+" ready:"+guided.DataFileReady(file.fileId)+" installed:"+file.installed)
            .Append(" spamBatches=").Append(guided.InstalledBatches("spam"));
        b.Append(" | binding: ctlSim=").Append(SimName(ctl.Sim,guided,ctl)).Append(" filesSim=").Append(SimName(files.Sim,guided,ctl))
            .Append(" filesWindow=").Append(Restored(files.window)).Append(" trainWindow=").Append(Restored(ctl.Window))
            .Append(" folder=").Append(Try(()=>Field<string>(files,"folder"))).Append(" payload=").Append(files.ActivePayload!=null);
        return b.ToString();
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
            language(false);
            var beforeImport=BeforeImportFrames(ctl,files,capture,check);try{while(beforeImport.MoveNext())yield return beforeImport.Current;}finally{(beforeImport as IDisposable)?.Dispose();}
            language(false);var sim=Fresh();Bind(ctl,files,sim);rt.Sim.S.money=1000;
            double until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.shortcutRoot.activeSelf&&files.CanBrowse,"folder visible before purchase");files.Show();Need(files.window.isOn,"empty folder opens before purchase");files.window.CloseWindow();
            Need(InstallHint(ctl)!=null&&!InstallHint(ctl).gameObject.activeSelf,"training installation hint respects the existing unowned import gate");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-01-unowned.png");check("empty folder visible and browsable without granting installation");
            var node=XgCatalog.Node(XgSim.DataEntryNode);Need(node!=null,"data import node exists");double importCost=sim.NodeCost(node);Need(importCost>=100,"ordinary data import base price is at least 100");
            rt.Sim.S.money=importCost-1;Need(!sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==importCost-1,"insufficient funds do not grant access");
            rt.Sim.S.money=importCost+1000;Need(sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==1000,"official purchase charges the displayed price once");
            Need(!sim.BuyNode(node.id,ctl.Host)&&rt.Sim.S.money==1000,"duplicate purchase no charge");
            ctl.View.Refresh(true);
            Need(InstallHint(ctl).gameObject.activeSelf&&InstallHintText(ctl).Contains("暂无可安装包")&&InstallHintText(ctl).Contains("资料文件夹 → 训练数据"),"owned empty import names the desktop folder without installing anything");
            Need(sim.S.dataFiles.Count==0,"showing empty installation guidance creates no data packs");
            ctl.Window.CloseWindow();until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.shortcutRoot.activeInHierarchy&&files.CanBrowse,"purchase reveals actual shortcut");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-02-owned-desktop.png");Click((RectTransform)files.shortcutRoot.transform);
            until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.window.isOn&&files.window.gameObject.activeInHierarchy,"LCD shortcut opened native window");
            Need(files.titleLabel.text=="资料文件夹","Chinese folder title");AssertChineseGlyphs(files);
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-03-open-root-zh.png");check("LCD shortcut opens native Training files folder after purchase");
            Click((RectTransform)Row(files,"training").transform);Need(Field<TMP_Text>(files,"preview").text.Contains("没有"),"empty training log is real empty state");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-04-empty-log.png");
            Click((RectTransform)Row(files,"packs").transform);Click((RectTransform)Row(files,"packs").transform);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            Need(Field<string>(files,"folder")=="packs","LCD double click entered data folder");Need(files.client.GetComponentsInChildren<XgFilesRow>().Length==0,"no fabricated historical files");
            yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-05-empty-data.png");
            language(true);sim.English=true;until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            Need(files.titleLabel.text=="Training files","live English title");yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-06-empty-data-en.png");
            ctl.View.Refresh(true);
            Need(InstallHintText(ctl).Contains("No packs ready yet")&&InstallHintText(ctl).Contains("Training files → Training data"),"training import hint follows English and the actual desktop folder name");
            // Explicit fixture records, not claimed as natural progression or old player history.
            sim.RecordTeachingQuestion("验收中的手写数字","Fixture handwriting",true,true,"已保存的题图","Recorded question image","mnist",
                new XgCard{dataset="mnist",digit=3,seed=7,teachingImageKind="handwriting28"});
            sim.S.desksOpen.Add("mnist");
            var a=new XgDownload{fileId=Guid.NewGuid().ToString("N"),dataset="mnist",offer="mnist.pack",sizeMB=1,doneMB=1,batches=1};
            var b=new XgDownload{fileId=Guid.NewGuid().ToString("N"),dataset="mnist",offer="mnist.pack",sizeMB=1,doneMB=1,batches=1};
            sim.S.dataFiles.Add(a);sim.S.dataFiles.Add(b);sim.S.batchesArrived.Add(new XgLabelCount{dataset="mnist",count=2});
            ctl.View.Refresh(true);
            Need(InstallHintText(ctl).Contains("2 ready to install")&&InstallHintText(ctl).Contains("Ctrl")&&!a.installed&&!b.installed,"ready guidance counts the real two files and describes supported batch drag without auto-install");
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
            Need(InstallHintText(ctl).Contains("2 installed; none waiting"),"actual batch-drop refresh replaces ready guidance with the real installed count");
            Need(!sim.InstallDataFile(a.fileId)&&!sim.InstallDataFile(b.fileId),"duplicate core install is a no-op");
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-11-real-batch-installed.png");
            check("actual LCD target raycast: revoked gate rejects, ready selection batch installs once");
            // Bounded guide regression: early-game UI fixture, actual purchases/download, no injected data samples.
            // Gate chain: GuideNote.Update -> XgGuide.Current -> PendingDataImport needs TabOpen(train) ->
            // FeatureVisible("train") -> TrainingUnlocked: an available dataset with XgCatalog.SamplesToTrain samples.
            // Declared setup, not LCD input: the spam desk is hand-labelled through the product answer API
            // (XgSim.SelectDesk/Card/Answer, as XgGuideTests.AfterLabelsItSuggestsTrainingThenAnAssessment does)
            // only until SamplesToTrain. The downloaded pack is never installed, counted or marked by setup;
            // samples/wallet/install baselines are read after setup.
            language(false);var guided=new XgSim();guided.S.epochs=1;guided.S.assessments=1;guided.S.treeIntroDelivered=true;
            Need(!XgGuide.TabOpen(guided,XgGuide.Lab,"train"),"guide fixture without labels keeps Train closed (original gate)");
            guided.SelectDesk("spam");
            for(int i=0;i<40&&guided.Samples("spam")<XgCatalog.SamplesToTrain;i++){var labelCard=guided.Card("spam");guided.Answer("spam",labelCard.truth,ctl.Host);}
            Need(guided.Labels("spam")>=XgCatalog.SamplesToTrain&&guided.TrainingUnlocked(XgTrack.Sequence),
                "declared hand-label setup opens the Train gate: labels="+guided.Labels("spam")+"/"+XgCatalog.SamplesToTrain);
            Need(guided.S.dataFiles.Count==0&&guided.InstalledBatches("spam")==0,"hand-label setup creates no data file or installed batch");
            Bind(ctl,files,guided);rt.Sim.S.money=1000;
            Need(ReferenceEquals(ctl.Sim,guided)&&ReferenceEquals(ctl.View.Controller,ctl)&&ReferenceEquals(files.Sim,guided),"guide fixture bound to controller, view and Files");
            foreach(string purchase in new[]{"train.curve",XgSim.DataEntryNode,"spam.pack"})
            {
                double before=ctl.Host.Money,price=guided.NodeCost(XgCatalog.Node(purchase));
                Need(guided.BuyNode(purchase,ctl.Host)&&Math.Abs(ctl.Host.Money-(before-price))<.001,"guide fixture actual paid purchase "+purchase);
            }
            for(int i=0;i<20&&guided.S.dataFiles.Count==0;i++){guided.Tick(.5,ctl.Host);yield return null;}
            var guideFile=guided.S.dataFiles.FirstOrDefault(f=>guided.DataFileReady(f.fileId));
            Need(guideFile!=null&&!guideFile.installed&&guided.InstalledBatches("spam")==0,"guide pack downloaded but uninstalled");
            Need(guided.CanInstallData&&XgGuide.TabOpen(guided,XgGuide.Lab,"tree")&&XgGuide.TabOpen(guided,XgGuide.Lab,"train"),
                "Tech, data import and Train are open before the note is read: "+GuideDiagnostics(GuideNoteT.Instance,ctl,files,guided,guideFile));
            files.window.CloseWindow();ctl.Window.CloseWindow();
            var note=GuideNoteT.Instance;
            Need(note!=null&&note.isActiveAndEnabled,"actual guide note installed and updating");
            // Pure product reading with the note's own household facts: separates core gates (rent, power, GPU, disk,
            // captcha, Train page) from note/runtime gates (story focus, cutscene, binding, busy) before any waiting.
            XgGuideHouse noteHouse=null;string houseError=null;
            try{noteHouse=NoteHouse(note);}catch(Exception e){houseError=(e.InnerException??e).Message;}
            Need(noteHouse!=null,"read the note's household facts: "+houseError+" / "+GuideDiagnostics(note,ctl,files,guided,guideFile));
            var expected=XgGuide.Current(guided,rt.Sim.S.money,noteHouse);
            Need(expected.Count>0&&expected[0].id=="data.install"&&expected[0].arg==guideFile.fileId,
                "product guide leads with this ready pack's import: "+GuideDiagnostics(note,ctl,files,guided,guideFile));
            check("declared hand-label setup opens Train; XgGuide.Current leads with data.install for the downloaded, uninstalled paid pack");
            // Same 6 s bound as before; on timeout report every gate value, keep a screenshot, and fail.
            until=EditorApplication.timeSinceStartup+6;
            while(!ReferenceEquals(GuideNoteT.TopSim,guided)||GuideNoteT.TopStep?.id!="data.install"||Field<bool>(note,"busy"))
            {
                if(EditorApplication.timeSinceStartup>=until)
                {
                    string why=GuideDiagnostics(note,ctl,files,guided,guideFile);Debug.LogWarning("Files UI guide note timeout: "+why);
                    capture("files-12a-guide-note-timeout.png");
                    Need(false,"guide import note became visible within deadline: "+why);
                }
                yield return null;
            }
            var noteRows=Field<Array>(note,"rows");var firstRow=noteRows.GetValue(0);var noteRow=Field<RectTransform>(firstRow,"rt");
            var noteRoot=Field<RectTransform>(note,"note");var shownStep=Field<XgGuideStep>(firstRow,"step");
            Need(ReferenceEquals(Field<XgSim>(note,"shownFor"),guided)&&shownStep!=null&&shownStep.id=="data.install"&&shownStep.arg==guideFile.fileId,
                "note row 0 is bound to this save's ready pack: "+GuideDiagnostics(note,ctl,files,guided,guideFile));
            Need(Field<TMP_Text>(firstRow,"label").text.Contains("训练数据"),"note row names the 训练数据 folder: "+Field<TMP_Text>(firstRow,"label").text);
            if(!noteRow.gameObject.activeInHierarchy)
            {
                var fold=Field<RectTransform>(note,"titleBar").Find("Fold") as RectTransform;Click(fold);yield return null;
            }
            Canvas.ForceUpdateCanvases();yield return null;
            Need(noteRoot!=null&&noteRoot.gameObject.activeInHierarchy&&noteRow.gameObject.activeInHierarchy,"rendered import note row: "+GuideDiagnostics(note,ctl,files,guided,guideFile));
            var noteButton=Field<UnityEngine.UI.Button>(firstRow,"button");Need(noteButton!=null&&noteButton.IsInteractable(),"import note row button is interactable");
            double samplesBefore=guided.TrainedSamples,walletBefore=ctl.Host.Money,labelsBefore=guided.Labels("spam");
            int batchesBefore=guided.InstalledBatches("spam"),unlockedBefore=guided.S.unlocked.Count;
            string filesBefore=string.Join(";",guided.S.dataFiles.Select(f=>f.fileId+":"+f.installed));
            capture("files-12-guide-ready-before-click.png");Click(noteRow);
            until=EditorApplication.timeSinceStartup+4;
            while(!Restored(files.window)||!Restored(ctl.Window)||Field<string>(files,"folder")!="packs"||ctl.View.Tab!="train")
            {
                if(EditorApplication.timeSinceStartup>=until)
                {
                    string why=GuideDiagnostics(note,ctl,files,guided,guideFile);Debug.LogWarning("Files UI guide navigation timeout: "+why);
                    capture("files-12b-guide-navigation-timeout.png");
                    Need(false,"note click opened Files training-data folder and Train: "+why);
                }
                yield return null;
            }
            Need(Field<TMP_Text>(files,"breadcrumb").text.Contains("训练数据")&&ctl.View.Visible,"Files shows 训练数据 and Train page is visible");
            Need(!guideFile.installed&&guided.InstalledBatches("spam")==batchesBefore&&guided.TrainedSamples==samplesBefore&&guided.Labels("spam")==labelsBefore,"note click installs nothing and adds no samples");
            Need(ctl.Host.Money==walletBefore&&guided.S.unlocked.Count==unlockedBefore,"note click charges nothing and buys nothing");
            Need(string.Join(";",guided.S.dataFiles.Select(f=>f.fileId+":"+f.installed))==filesBefore&&files.ActivePayload==null,"note click leaves install flags unchanged and starts no drag");
            check("actual guide note row LCD click opens Files 训练数据 and Train; no install, no fee, no purchase");
            sourceFrame.anchorMin=sourceFrame.anchorMax=new Vector2(.5f,.5f);sourceFrame.sizeDelta=new Vector2(700,650);sourceFrame.anchoredPosition=new Vector2(-490,0);
            targetFrame.anchorMin=targetFrame.anchorMax=new Vector2(.5f,.5f);targetFrame.sizeDelta=new Vector2(850,650);targetFrame.anchoredPosition=new Vector2(330,0);
            files.window.FocusToWindow();Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();yield return null;
            capture("files-13-guide-navigation-before-drag.png");
            var guideRow=Row(files,guideFile.fileId);Need(guideRow.ready,"guide pack row is a ready source");
            var drop=ctl.View.Train.root.GetComponent<XgFilesTrainDrop>();Need(drop!=null,"Train root carries the actual drop target");
            BeginDrag((RectTransform)guideRow.transform);
            Need(files.ActivePayload!=null&&ReferenceEquals(files.ActivePayload.Source,guided)&&files.ActivePayload.FileIds.Count==1&&files.ActivePayload.FileIds[0]==guideFile.fileId,
                "guide source starts actual LCD drag of the one ready pack");
            Need(!guideFile.installed&&guided.InstalledBatches("spam")==batchesBefore,"drag start alone installs nothing");
            EndDrag(ctl.View.Train.root);
            Need(guideFile.installed&&drop.LastInstalled==1&&files.ActivePayload==null,"actual LCD target hit installs exactly the dragged pack once");
            Need(guided.InstalledBatches("spam")>batchesBefore&&guided.TrainedSamples>samplesBefore,"only actual drop imports the purchased pack");
            Need(ctl.Host.Money==walletBefore,"drag import has no second fee");
            double samplesAfter=guided.TrainedSamples;int batchesAfter=guided.InstalledBatches("spam");
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;
            var installedRow=Row(files,guideFile.fileId);Need(!installedRow.ready,"installed pack row is no longer a ready source");
            BeginDrag((RectTransform)installedRow.transform);Need(files.ActivePayload==null,"installed pack cannot start a second LCD drag");Bridge().inputSurface.CancelInteraction();
            Need(guided.InstalledBatches("spam")==batchesAfter&&guided.TrainedSamples==samplesAfter&&ctl.Host.Money==walletBefore,"second attempt adds no samples or fee");
            files.Refresh();yield return null;Canvas.ForceUpdateCanvases();yield return null;capture("files-14-guide-real-drop-installed.png");
            check("actual LCD source drag and Train target hit install the downloaded paid pack exactly once; re-drag of installed row refused");
            Debug.Log("FilesWindowUiChecks PASS: real paid gate, LCD opening/records/source drag/batch drop, cross-save and minimize cancellation, guide note navigation and drag import. Ctrl-key automation remains pending.");
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
