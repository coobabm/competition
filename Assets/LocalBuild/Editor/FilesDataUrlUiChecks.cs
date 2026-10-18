#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Own isolated simulation, restored before the caller's file-selection fixtures begin.
// Source and download clicks go through FilesWindowUiChecks' genuine LCD raycasts.
public static class FilesDataUrlUiChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags).GetValue(o);
    static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Data URL UI: "+why);}
    static double WaitUntil()=>EditorApplication.timeSinceStartup+.5;
    static void Bind(XingGuangController ctl,XgFilesWindow files,XgSim sim)
    {
        typeof(XingGuangController).GetProperty("Sim",Flags).GetSetMethod(true).Invoke(ctl,new object[]{sim});
        ctl.View.Bind(ctl);files.Bind(sim);
    }
    static void Parents(XgSim sim,XgNode node,HashSet<string> seen)
    {
        if(node==null||!seen.Add(node.id))return;
        var ids=new List<string>(node.needs??new string[0]);ids.Add(node.parent);
        foreach(string id in ids){if(string.IsNullOrEmpty(id))continue;Parents(sim,XgCatalog.Node(id),seen);if(!sim.Has(id))sim.S.unlocked.Add(id);}
    }
    static XgSim Fixture(bool english)
    {
        var sim=new XgSim();sim.English=english;sim.S.teachingFlow=false;sim.S.openingFocus=false;sim.S.diskBonusMB=1e9;
        sim.S.stage=sim.S.stageVision=sim.S.stageSequence=6;sim.S.firstSpecialty="vision";
        sim.S.trainingLessonObservedStage=6;sim.S.spiderIntroDone=true;
        foreach(string id in new[]{"train.curve","lenet","conv","rnn","lstm","gru"})if(!sim.Has(id))sim.S.unlocked.Add(id);
        Parents(sim,XgCatalog.Node("mnist.pack"),new HashSet<string>());
        return sim;
    }
    static void VisibleMessage(TMP_Text hint,string expected)
    {
        Need(hint.gameObject.activeInHierarchy&&hint.text==expected,"visible core status: "+expected+" (actual "+hint.text+")");
        hint.ForceMeshUpdate(true,true);Need(!hint.isTextOverflowing,"status message fits its actual rectangle");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<string> capture,Action<string> check)
    {
        var runtime=ctl.runtime;var files=runtime.GetComponent<XgFilesWindow>();
        Need(files!=null&&!runtime.useDiskSave&&!runtime.tickEnabled&&!runtime.TestMode,"isolated Main live-host fixture");
        var original=ctl.Sim;string house=JsonUtility.ToJson(runtime.Sim.S);bool enabled=ctl.enabled,wasOpen=files.window.isOn;ctl.enabled=false;
        try
        {
            for(int locale=0;locale<2;locale++)
            {
                bool english=locale==1;string tag="data-url-"+(english?"en":"zh");language(english);
                var sim=Fixture(english);Bind(ctl,files,sim);runtime.Sim.S.money=100000;
                Need(sim.BuyNode(XgSim.DataEntryNode,ctl.Host),"real Data Import purchase");
                ctl.Window.CloseWindow();files.Show();
                double until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
                Canvas.ForceUpdateCanvases();yield return null;
                var panel=files.client.Find("Game data address");Need(panel!=null,"address controls built");
                var source=(RectTransform)panel.Find("MNIST source");var download=(RectTransform)panel.Find("Download address");
                var input=Field<TMP_InputField>(files,"dataUrlInput");var hint=Field<TMP_Text>(files,"dataUrlHint");var button=Field<Button>(files,"dataUrlDownload");
                Need(Field<TMP_Text>(files,"dataUrlClue").text==sim.DataUrlClue("mnist"),"discoverable source is actual approved clue");
                FilesWindowUiChecks.Click(source);
                until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
                Need(input.text==XgSim.MnistSourceUrl,"real LCD source button fills approved URL");
                var ready=sim.PreviewDataUrl(input.text,ctl.Host);Need(ready.canDownload&&button.interactable,"ready source is downloadable");VisibleMessage(hint,ready.message);
                Canvas.ForceUpdateCanvases();yield return null;capture(tag+"-01-source-price.png");
                // Exercise the real TMP onValueChanged route; this is not a claim about OS clipboard typing.
                input.text="https://not-a-game-source.invalid/data";yield return null;
                var unknown=sim.PreviewDataUrl(input.text,ctl.Host);Need(unknown.status==XgDataUrlStatus.Unknown&&!button.interactable,"unknown source cannot download");VisibleMessage(hint,unknown.message);
                Canvas.ForceUpdateCanvases();yield return null;capture(tag+"-02-unknown.png");
                FilesWindowUiChecks.Click(source);runtime.Sim.S.money=0;
                until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
                var poor=sim.PreviewDataUrl(input.text,ctl.Host);Need(poor.status==XgDataUrlStatus.TooExpensive&&!button.interactable,"insufficient funds disables actual button");VisibleMessage(hint,poor.message);
                Canvas.ForceUpdateCanvases();yield return null;capture(tag+"-03-insufficient.png");
                runtime.Sim.S.money=100000;until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
                ready=sim.PreviewDataUrl(input.text,ctl.Host);Need(ready.canDownload&&button.interactable,"real funds restore preview");double before=runtime.Sim.S.money;
                FilesWindowUiChecks.Click(download);yield return null;Canvas.ForceUpdateCanvases();yield return null;
                Need(Math.Abs(before-runtime.Sim.S.money-ready.price)<1e-6,"actual URL button charged exactly preview catalog price");
                Need(sim.S.downloadQueue.Count==1&&sim.S.dataFiles.Count==0&&sim.Samples("mnist")==0,"purchase queued, not installed or rewarded early");
                double paid=runtime.Sim.S.money;Need(!button.interactable,"same URL disabled immediately after actual purchase");
                FilesWindowUiChecks.Click(download);Need(runtime.Sim.S.money==paid&&sim.S.downloadQueue.Count==1,"repeated actual LCD click does not charge or duplicate queue");
                capture(tag+"-04-queued.png");
                // Advance only the production download scheduler, with explicit fixture time steps.
                var tick=typeof(XgSim).GetMethod("TickDownloads",Flags);Need(tick!=null,"production scheduler method exists");
                tick.Invoke(sim,new object[]{.5});files.Refresh();
                yield return null;Canvas.ForceUpdateCanvases();yield return null;
                var progress=sim.DownloadQueue().First();Need(progress.progress>0&&progress.progress<1,"scheduler produced partial progress");
                var queueRow=files.client.GetComponentsInChildren<XgFilesRow>().FirstOrDefault(x=>x.fileId=="queue:"+progress.fileId);
                Need(queueRow!=null&&queueRow.GetComponentInChildren<TMP_Text>().text.Contains("%"),"real queue row has percentage");
                Need(queueRow.transform.Find("Download progress/Progress")!=null,"real queue row has progress bar");capture(tag+"-05-progress.png");
                // Let the transient 'queued' feedback expire before asserting stable lifecycle states.
                until=EditorApplication.timeSinceStartup+3.1;while(EditorApplication.timeSinceStartup<until)yield return null;
                VisibleMessage(hint,sim.PreviewDataUrl(input.text,ctl.Host).message);
                tick.Invoke(sim,new object[]{120.0});files.Refresh();
                until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
                Need(sim.S.downloadQueue.Count==0&&sim.S.dataFiles.Count==1,"production download completed to one virtual file");
                var file=sim.S.dataFiles[0];Need(!file.installed&&sim.DataFileReady(file.fileId)&&sim.Samples("mnist")==0,"completed file remains pending installation");
                Need(files.client.GetComponentsInChildren<XgFilesRow>().Any(x=>x.fileId==file.fileId),"completed GUID row visible");
                var downloaded=sim.PreviewDataUrl(input.text,ctl.Host);Need(downloaded.status==XgDataUrlStatus.Downloaded&&!button.interactable,"downloaded state blocks duplicate purchase");VisibleMessage(hint,downloaded.message);
                Canvas.ForceUpdateCanvases();yield return null;capture(tag+"-06-ready-to-install.png");
                FilesWindowUiChecks.Click(download);Need(runtime.Sim.S.money==paid&&sim.S.dataFiles.Count==1,"completed data cannot be charged again through LCD button");
                check(tag+": actual source/download LCD clicks, real cost once, unknown/poor/queued/downloaded feedback, percentage, completed GUID pending install");
                files.window.CloseWindow();until=WaitUntil();while(EditorApplication.timeSinceStartup<until)yield return null;
            }
        }
        finally
        {
            files.CancelDrag();files.window.CloseWindow();JsonUtility.FromJsonOverwrite(house,runtime.Sim.S);
            language(original.English);Bind(ctl,files,original);ctl.enabled=enabled;if(wasOpen)files.Show();
        }
    }
}
#endif
