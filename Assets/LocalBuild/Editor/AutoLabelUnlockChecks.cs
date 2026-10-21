#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class AutoLabelUnlockChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Flags).Invoke(o,args);
    static void Set(object o,string field,object value) => o.GetType().GetField(field,Flags).SetValue(o,value);
    static object Get(object o,string field) => o.GetType().GetField(field,Flags).GetValue(o);
    [Serializable] sealed class Report
    {
        public string status="running";
        public List<string> passed=new List<string>();
        public string scope="Normal runtime (TestMode=false), isolated disk save. Reflection wires the empty-scene fixture to production BindSave/WriteToSave; gameplay discovery is never injected. A real Update starts the presentation. Only blockedAt is backdated to exercise production timeout -> EndBlocked -> Finish; this is not full cinematic QA. Actual technology Canvas and raycast/click purchase follow.";
    }
    static void Check(Report r,bool ok,string text){if(!ok)throw new InvalidOperationException(text);r.passed.Add(text);}
    sealed class Fixture : IDisposable
    {
        public GameObject owner,beatObject;
        public ChapterOneRuntime runtime;
        public XingGuangController lab;
        public AutoLabelEpiphany beat;
        Action save;
        public Fixture(string dir)
        {
            ChapterOneRuntime.VerificationSaveDirectory=dir;
            owner=new GameObject("Isolated inactive save owner");owner.SetActive(false);
            runtime=owner.AddComponent<ChapterOneRuntime>();runtime.useDiskSave=true;runtime.tickEnabled=false;runtime.EnsureInitialized();
            if(runtime.Sim.InPrologue)runtime.Sim.CompletePrologue(new PrologueProfile{name="小光",self="我",callMe="你",personality="认真"});
            lab=owner.AddComponent<XingGuangController>();lab.runtime=runtime;
            Call(lab,"BindSave",runtime.Sim);
            save=()=>Call(lab,"WriteToSave");runtime.Saving+=save;
            beatObject=new GameObject("Actual AutoLabelEpiphany fixture");beatObject.SetActive(false);
            beat=beatObject.AddComponent<AutoLabelEpiphany>();
        }
        public void Dispose()
        {
            if(beatObject!=null)UnityEngine.Object.DestroyImmediate(beatObject);
            runtime.Saving-=save;runtime.useDiskSave=false;
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }
    static void Answer(XgSim sim,IXgHost wallet)
    {
        var card=sim.Card("sense");
        if(!sim.TeachingGuess(card,out bool guess)||!sim.TryEvaluateTeachingGuess("sense",card.id,guess,guess==card.truth,wallet,out _))
            throw new InvalidOperationException("Ordinary teaching answer rejected");
    }
    static void Discover(XgSim sim,IXgHost wallet,Report report,bool openingOnly=false)
    {
        sim.GoldChance=0;sim.PlatformChecks=false;
        for(int round=0;round<40&&sim.OpeningFocus;round++)
        {
            for(int i=0;i<10&&sim.OpeningFocus;i++)Answer(sim,wallet);
            if(sim.TeachingTreeOpen&&!sim.Has("train.curve")&&!sim.BuyNode("train.curve",wallet))throw new InvalidOperationException("Training prerequisite purchase failed");
            sim.TrainEpoch(XgTrack.Sequence,wallet);
        }
        Check(report,!sim.OpeningFocus&&sim.BestAcc("sense")>=XgCatalog.AutoMinAccuracy,"Ordinary teaching/training reaches the real accuracy gate");
        if(openingOnly)return;
        for(int i=0;i<XgSim.EpiphanyFallbackLabels&&!sim.EpiphanyPending;i++)Answer(sim,wallet);
        Check(report,sim.EpiphanyPending&&sim.AutoLabelHidden&&!sim.AutoLabelRevealed,"Real gameplay produces delayed pending discovery, still hidden");
    }
    public static IEnumerator Frames()
    {
        var report=new Report();string dir=Path.Combine(Path.GetTempPath(),"LingGuang-AutoLabel-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string previous=ChapterOneRuntime.VerificationSaveDirectory;
        var root=new GameObject("Actual technology unlock Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(AudioListener));
        var events=new GameObject("Unlock input",typeof(EventSystem));
        var deskProperty=typeof(PrologueDirector).GetProperty("Desk",BindingFlags.Public|BindingFlags.Static);
        var oldDesk=deskProperty.GetValue(null);Fixture f=null;IDisposable locale=null;
        try
        {
            Check(report,Application.companyName=="CodexIsolatedQA"&&Application.productName.StartsWith("UI-",StringComparison.Ordinal),"PlayerPrefs namespace is isolated by run-isolated-ui.ps1");
            locale=GameText.Bind(()=>"zh-CN");GameText.PublishChanged();
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var rt=(RectTransform)root.transform;rt.sizeDelta=new Vector2(1400,900);
            var font=XgUiV4Fonts.Sans;Check(report,font!=null,"Shipped font is available");
            var host=new TechPurchaseFeedbackChecks.PageHost();var tree=host.tree=new XgTechTreePage();
            host.juice=root.AddComponent<XgJuice>();host.juice.Muted=true;host.juice.LocalHitStop=true;host.juice.Visible=()=>true;host.juice.Init(rt,rt,font);
            tree.Init(host,new XgUi(font,null,true));
            f=new Fixture(Path.Combine(dir,"finish-save"));
            // Start runs once on a real frame; then provide explicit fixture references (inactive owner avoids game bootstrap).
            f.beatObject.SetActive(true);yield return null;
            Set(f.beat,"runtime",f.runtime);Set(f.beat,"lab",f.lab);yield return null;
            Check(report,!f.runtime.TestMode&&f.lab.Sim.DeferAutoLabelReveal,"Actual component Update binds a normal non-test runtime");
            host.sim=f.lab.Sim;Discover(host.sim,host.wallet,report,true);tree.Build(rt);tree.Refresh();yield return null;
            var node=tree.NodeTarget("label.auto").GetComponent<XgTechNodeView>();
            Check(report,node.gameObject.activeInHierarchy&&node.label.text.Contains("？？？"),"Actual technology page displays ??? before discovery");
            Discover(host.sim,host.wallet,report);tree.Refresh();
            Check(report,node.label.text.Contains("？？？"),"Pending presentation still displays ???");
            Check(report,f.lab.SaveNow()&&File.Exists(f.runtime.SavePath),"Pending state is written to isolated disk");
            // Reload the save while the first component is alive, before disposing it (OnDisable intentionally reveals).
            string saveDir=Path.Combine(dir,"finish-save");var reload=new Fixture(saveDir);
            Check(report,reload.lab.Sim.EpiphanyPending&&reload.lab.Sim.AutoLabelHidden,"Pending state survives a genuine runtime disk reload");
            reload.Dispose();
            deskProperty.SetValue(null,new PrologueDesk(rt,rt,rt,font));
            float deadline=Time.realtimeSinceStartup+15;
            while(!AutoLabelEpiphany.Playing&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(report,AutoLabelEpiphany.Playing&&Get(f.beat,"phase").ToString()=="Blocked","Real Update enters the delayed presentation without calling RevealAutoLabel");
            Set(f.beat,"blockedAt",Time.unscaledTime-41f);
            deadline=Time.realtimeSinceStartup+8;
            while(!host.sim.AutoLabelRevealed&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(report,host.sim.AutoLabelRevealed&&!host.sim.EpiphanyPending,"Production timeout -> EndBlocked -> Finish reveals the hidden technology");
            tree.Refresh();yield return null;
            Check(report,!node.label.text.Contains("？？？")&&node.label.text.Contains("自动"),"Actual page replaces ??? with the revealed technology name");
            Check(report,XgTechTree.Status(node.entry,host.sim,host.wallet)==XgSim.NodeStatus.Buyable,"Revealed technology is Buyable with the naturally earned checkpoint");
            var viewport=tree.Pan.viewport;Canvas.ForceUpdateCanvases();tree.Pan.SetView(1,new Vector2(viewport.rect.width*.5f-node.home.x,-viewport.rect.height*.5f-node.home.y));Canvas.ForceUpdateCanvases();yield return null;
            var hit=(RectTransform)node.transform;
            Check(report,TechPurchaseFeedbackChecks.ClickHandlerAt(events.GetComponent<EventSystem>(),hit,hit.rect.center)==node.gameObject,"Actual Canvas raycaster resolves the revealed technology");
            int levelBefore=host.sim.GlobalAutoLevel;int purchases=0;host.sim.NodeBought+=n=>{purchases++;tree.OnBought(n);tree.Refresh();};
            double before=host.wallet.money,price=host.sim.NodeCost(XgTechTree.Next(node.entry,host.sim));
            var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=-1};
            node.OnPointerDown(pointer);node.OnPointerUp(pointer);node.OnPointerClick(pointer);
            Check(report,purchases==1&&Math.Abs(before-host.wallet.money-price)<1e-6&&host.sim.GlobalAutoLevel==levelBefore+1,"Real node click purchases exactly once and debits its live price");
            reload=new Fixture(saveDir);
            Check(report,reload.lab.Sim.AutoLabelRevealed&&!reload.lab.Sim.EpiphanyPending,"Production reveal saves immediately and survives disk reload without another explicit save");reload.Dispose();
            f.Dispose();f=null;deskProperty.SetValue(null,null);
            f=new Fixture(Path.Combine(dir,"disable-save"));f.beatObject.SetActive(true);yield return null;
            Set(f.beat,"runtime",f.runtime);Set(f.beat,"lab",f.lab);yield return null;
            Discover(f.lab.Sim,host.wallet,report);
            Check(report,Get(f.beat,"phase").ToString()=="Idle","Disable scenario is genuinely pending in Idle");
            f.beat.enabled=false;
            Check(report,f.lab.Sim.AutoLabelRevealed&&!f.lab.Sim.DeferAutoLabelReveal,"Disabling the real Idle component reveals pending technology and releases deferral");
            reload=new Fixture(Path.Combine(dir,"disable-save"));
            Check(report,reload.lab.Sim.AutoLabelRevealed&&!reload.lab.Sim.EpiphanyPending,"Disable fallback reveal survives isolated disk reload");reload.Dispose();
            report.status="passed";
        }
        finally
        {
            f?.Dispose();deskProperty.SetValue(null,oldDesk);ChapterOneRuntime.VerificationSaveDirectory=previous;
            locale?.Dispose();UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(events);
            string path=Path.Combine(dir,"report.json");File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("AutoLabelUnlockChecks "+report.status+": "+path);
        }
    }
}
#endif
