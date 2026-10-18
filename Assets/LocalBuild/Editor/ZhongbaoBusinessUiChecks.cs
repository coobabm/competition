#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Explicit stage-3/deployed-checkpoint/installed-app fixtures, actual production host/buttons/disk.
// No model, normal app download, natural stage progression, training or per-second economy claim.
public static class ZhongbaoBusinessUiChecks
{
    const string Key="SidebarLayoutChecks.ZhongbaoBusiness";
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public static bool Requested=>SessionState.GetBool(Key,false);
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount{get;private set;}
    public const string Scope="Bilingual 960x600 actual Zhongbao business UI: explicit stage3, four deployed checkpoint values, installed-app receipt and powered household. The economy/controller tick is disabled; actual RefreshWiring(host) explicitly initializes and refreshes automatic model placement/derived job rates before and after archive reload, without accruing income or advancing time. Actual sign buttons and shared-wallet advances, duplicate disabled-control clicks, real archive save/reload, no work-label page, ToTrain pointer and legacy label router to teaching, actual teaching with invariant business metrics, retained auto/subcontract/ledger/credit navigation. No natural acquisition/progression, model inference, training, continuous income accrual, drag-and-drop or stage4 unlock claim.";
    public static void ClearRequest()=>SessionState.EraseBool(Key);
    [MenuItem("Tools/Local Verification/Zhongbao business actual LCD")]
    public static void Run()
    {
        // A partial import must fail visibly, never silently run the ordinary BookLibrary branch.
        var hook=typeof(SidebarLayoutChecks).GetField("ZhongbaoBusinessHookVersion",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        if(hook==null||Convert.ToInt32(hook.GetRawConstantValue())<1)throw new InvalidOperationException("Import the reviewed ZhongbaoBusiness Sidebar hook before Run. Frames may also be hosted by an isolated runner.");
        SessionState.SetBool(Key,true);try{BookLibraryUiChecks.Run();}catch{ClearRequest();throw;}
    }
    static T Get<T>(object value,string field)
    {
        for(var type=value.GetType();type!=null;type=type.BaseType){var f=type.GetField(field,F);if(f!=null)return (T)f.GetValue(value);}
        throw new InvalidOperationException("Zhongbao QA missing production field: "+field);
    }
    static void Need(bool ok,string why){AssertionCount++;if(!ok)throw new InvalidOperationException("Zhongbao business UI: "+why);}
    static void Done(string text){Checks.Add(text);Debug.Log("Zhongbao business UI: "+text);}
    static IEnumerable<object> Settle(int frames=4){Canvas.ForceUpdateCanvases();while(frames-->0)yield return null;Canvas.ForceUpdateCanvases();}
    static IEnumerable<object> Until(Func<bool> ready,string why,double seconds=12)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){Need(EditorApplication.timeSinceStartup<end,why);yield return null;}foreach(var f in Settle())yield return f;}
    static IEnumerable<object> Ready(WindowManager window)
    {
        foreach(var f in Until(()=>
        {
            if(window==null||!window.isOn||!window.gameObject.activeInHierarchy)return false;
            foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))if(group.alpha<.95f||!group.interactable||!group.blocksRaycasts)return false;
            var a=window.windowAnimator;return a==null||!a.isActiveAndEnabled||!a.IsInTransition(0)&&a.GetCurrentAnimatorStateInfo(0).normalizedTime>=1;
        },"native window is naturally interactive"))yield return f;
    }
    static Button Named(Component root,string name)
    {var bs=root.GetComponentsInChildren<Button>(true).Where(b=>b.name==name&&b.gameObject.activeInHierarchy).ToArray();Need(bs.Length==1,"one active button "+name);return bs[0];}
    static IEnumerable<object> Tap(Button button,Action<RectTransform> click,bool interactable=true)
    {
        Need(button!=null&&button.gameObject.activeInHierarchy,"actual button is visible");
        Need(button.IsInteractable()==interactable,"expected button interactability: "+button.name);
        foreach(var f in Settle())yield return f;click((RectTransform)button.transform);foreach(var f in Settle())yield return f;
    }
    static string Plain(TMP_Text text)=>Regex.Replace(text.text??"","<[^>]*>","");
    static string Texts(Component root)=>string.Join("\n",root.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.gameObject.activeInHierarchy).Select(Plain));
    static RectTransform Root(ZhongbaoView view)=>Get<RectTransform>(view,"root");
    static double OrderRate(XgSim sim)=>XgCatalog.Contracts.Sum(c=>sim.ContractIncome(c));
    static string Metrics(ZhongbaoView view)=>string.Join("|",new[]{"comboNumber","comboMult","comboTier","hudLevel"}.Select(n=>Get<TMP_Text>(view,n).text));
    static string Orders(XgSim sim)=>string.Join(",",sim.S.contracts.OrderBy(x=>x,StringComparer.Ordinal));
    static string Best(XgSim sim)=>string.Join("|",sim.S.best.Select(b=>JsonUtility.ToJson(b)));
    // The shared runner freezes ctl.Update, including its normal TickWiring lifecycle. Do not compare
    // a pre-host preview graph (unplaced models => factor 0) with a fresh graph (default factor 1).
    // Use the production graph builder on both owners; it may place models, never tick earnings/time.
    static void PrepareBusinessWiring(XingGuangController ctl)
    {
        Need(!ctl.enabled&&!ctl.runtime.tickEnabled&&!ctl.runtime.TestMode,"explicit live-host wiring preparation while economic/controller ticks remain frozen");
        double money=ctl.Host.Money,income=ctl.Sim.S.totalIncome,earned=ctl.runtime.Sim.S.totalEarned,seconds=ctl.runtime.Sim.S.gameSeconds;
        string orders=Orders(ctl.Sim),best=Best(ctl.Sim);
        var graph=ctl.Sim.RefreshWiring(ctl.Host);
        Need(graph!=null&&graph.hasRig&&graph.Node("psu")?.online==true&&ctl.Host.Compute>0,"real powered household graph prepared without ticking the simulation");
        foreach(var b in ctl.Sim.S.best)Need(graph.Node("m:"+b.modelId)?.online==true,"fixture checkpoint automatically placed on an actual powered card: "+b.dataset);
        Need(ctl.Host.Money==money&&ctl.Sim.S.totalIncome==income&&ctl.runtime.Sim.S.totalEarned==earned&&ctl.runtime.Sim.S.gameSeconds==seconds&&Orders(ctl.Sim)==orders&&Best(ctl.Sim)==best,"graph preparation changes no balance, advances, signed IDs, checkpoints or economic time");
    }
    [Serializable] sealed class ReloadProbe
    {
        public string phase,orders,best,wiring;
        public double money,totalIncome,totalEarned,gameSeconds,orderRate;
        public int signedCount;
        public bool controllerEnabled,tickEnabled,testMode,hasCachedGraph;
        public List<string> jobs=new List<string>();
    }
    [Serializable] sealed class ReloadEvidence {public List<ReloadProbe> probes=new List<ReloadProbe>();}
    static ReloadProbe Probe(XingGuangController ctl,string phase)
    {
        var sim=ctl.Sim;var graph=Get<XgWiringView>(sim,"wiringView");
        var p=new ReloadProbe{phase=phase,orders=Orders(sim),best=Best(sim),wiring=JsonUtility.ToJson(sim.S.wiring),
            money=ctl.Host.Money,totalIncome=sim.S.totalIncome,totalEarned=ctl.runtime.Sim.S.totalEarned,gameSeconds=ctl.runtime.Sim.S.gameSeconds,orderRate=OrderRate(sim),signedCount=sim.S.contracts.Count,
            controllerEnabled=ctl.enabled,tickEnabled=ctl.runtime.tickEnabled,testMode=ctl.runtime.TestMode,hasCachedGraph=graph!=null};
        foreach(string id in sim.S.contracts)
        {
            var n=graph?.Node("contract:"+id);
            p.jobs.Add(id+" factor="+sim.WireSpeed("contract:"+id).ToString("R",CultureInfo.InvariantCulture)+" model="+(n?.model??"<no cached node>")+" why="+(n?.why??""));
        }
        return p;
    }
    static void SetLive(ChapterOneRuntime rt)
    {typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true).Invoke(rt,new object[]{false});rt.tickEnabled=false;}
    static string IsolateDisk(ChapterOneRuntime rt)
    {
        string root=ChapterOneRuntime.VerificationSaveDirectory;
        Need(!string.IsNullOrEmpty(root)&&Path.GetFullPath(root).IndexOf(Path.DirectorySeparatorChar+"LocalVerification"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)>=0,"explicit isolated LocalVerification save root");
        root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        Need(Path.GetFullPath(rt.SavePath).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"all real saves use the isolated root");
        rt.useDiskSave=true;return Path.GetDirectoryName(rt.SavePath);
    }
    static GameState ReadPrimary(string directory)
    {var loaded=new AtomicSaveStore(directory).Load();Need(loaded.Success&&!loaded.Recovered,"primary atomic snapshot independently readable without backup recovery");return JsonUtility.FromJson<GameState>(loaded.Payload);}

    // Explicit business prerequisite, not evidence of a group message, download or installer interaction.
    // Reflection lets this QA compile while the acquisition candidate is staged; absent API fails at runtime.
    static void InstalledFixture(XgSim sim)
    {
        var receiptMethod=typeof(XgSim).GetMethod("AppReceipt",F,null,new[]{typeof(string)},null);
        Need(receiptMethod!=null,"app acquisition API imported before business QA");
        object receipt=receiptMethod.Invoke(sim,new object[]{"zhongbao"});Need(receipt!=null,"explicit Zhongbao installation receipt available");
        var installed=receipt.GetType().GetField("installed",F);Need(installed!=null&&installed.FieldType==typeof(bool),"installed fixture field schema matches");installed.SetValue(receipt,true);
        Need(IsInstalled(sim),"explicit installed-app prerequisite set; no normal acquisition claim");
    }
    static bool IsInstalled(XgSim sim)
    {
        var method=typeof(XgSim).GetMethod("DesktopAppInstalled",F,null,new[]{typeof(string)},null);
        Need(method!=null,"read-only installation predicate exists");return (bool)method.Invoke(sim,new object[]{"zhongbao"});
    }
    static ChapterOneSim Fixture(bool english)
    {
        var lab=new XgSim();lab.BeginOpening();lab.English=english;
        lab.S.stage=lab.S.stageVision=lab.S.stageSequence=3;lab.S.abilitiesEmerged=new List<int>{1,2,3};
        lab.S.openingFocus=false;lab.S.teachingFlow=false;lab.S.firstSpecialty="vision";
        lab.S.handCorrect=200;lab.S.epochs=20;lab.S.spiderIntroDone=lab.S.firstWordsSaid=true;
        lab.S.teachIntroSaid=lab.S.teachThirdSaid=lab.S.teachTreeSaid=lab.S.teachTrainSaid=true;
        // Nonzero tier separates business orders from unrelated ability-tier IncomePerSecond.
        lab.S.crowdTier=3;lab.S.fundedAbility=3;
        foreach(string id in new[]{"sense","logic","spam","mnist","poems","danmu"})if(!lab.S.desksOpen.Contains(id))lab.S.desksOpen.Add(id);
        lab.S.best.Clear();
        lab.S.best.Add(new XgBest{dataset="mnist",arch="mlp",acc=.995});
        lab.S.best.Add(new XgBest{dataset="poems",arch="rnn",acc=.4});
        lab.S.best.Add(new XgBest{dataset="danmu",arch="mlp",acc=.9});
        lab.S.best.Add(new XgBest{dataset="logic",arch="mlp",acc=.8});
        lab.S.contracts.Clear();lab.SelectDesk("sense");InstalledFixture(lab);
        var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=10000;
        house.S.labState=JsonUtility.ToJson(lab.S);GameCalendar.Advance(house.S,GameCalendar.DayFor(3,0));
        house.S.rentDay=GameCalendar.CurrentDay(house.S);house.S.broadbandMonth=201612;return house;
    }
    static void Bind(XingGuangController ctl,ChapterOneSim house)
    {
        ctl.runtime.useDiskSave=false;ctl.runtime.SetSimulationForTests(house);SetLive(ctl.runtime);ctl.enabled=false;
        Need(ReferenceEquals(ctl.runtime.Sim,house)&&ctl.BoundTo(house),"production lab bound to exact isolated household identity");
        Need(IsInstalled(ctl.Sim),"installed fixture survives real lab JSON binding");
        PrepareBusinessWiring(ctl);
    }
    static IEnumerable<object> Open(ZhongbaoHub hub,string tab="contracts")
    {
        Need(hub.Open(tab),"production app router accepts Zhongbao "+(tab??"default"));
        foreach(var f in Ready(hub.View.Window))yield return f;
        Need(hub.View.Visible,"actual branded business page visible");hub.View.Window.FocusToWindow();foreach(var f in Settle())yield return f;
    }
    static RectTransform Row(ZhongbaoView view,string id)
    {
        var row=view.Contracts.root.GetComponentsInChildren<XgContractDrop>(true).SingleOrDefault(d=>d.contract.id==id);
        Need(row!=null&&row.gameObject.activeInHierarchy,"actual contract card exists: "+id);return (RectTransform)row.transform;
    }
    static Button SignButton(RectTransform row)
    {var buttons=row.GetComponentsInChildren<Button>(true);Need(buttons.Length==1,"one signing control in "+row.name);return buttons[0];}
    static IEnumerable<object> Reveal(ZhongbaoView view,RectTransform row)
    {
        var viewport=Get<RectTransform>(view.Contracts,"viewport");var scroll=viewport.GetComponent<ScrollRect>();
        Canvas.ForceUpdateCanvases();scroll.StopMovement();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,row);
        float hidden=scroll.content.rect.height-viewport.rect.height;
        if(hidden>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-(viewport.rect.center.y-bounds.center.y)/hidden);
        foreach(var f in Settle())yield return f;
        bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,row);
        Need(bounds.min.y>=viewport.rect.yMin-1&&bounds.max.y<=viewport.rect.yMax+1,"entire business card reachable inside actual ScrollRect: "+row.name);
    }
    static void BusinessSurface(ZhongbaoView view)
    {
        var root=Root(view);var pages=Get<Dictionary<string,XgPage>>(view,"pages");
        Need(view.Label==null&&!pages.ContainsKey("label")&&!pages.ContainsKey("teach")&&!pages.Values.Any(p=>p is XgLabelPage),"business app builds no XgLabelPage or hidden work-label page");
        Need(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="label"||t.name=="Tab label"||t.name=="Workspace"&&t.parent!=null&&t.parent.name=="label"),"no dormant label workspace/nav object exists in business hierarchy");
        Need(ZhongbaoView.PageIds.SequenceEqual(new[]{"contracts","auto","subcontract","ledger","credit"}),"business navigation retains all five intended routes");
        Need(Get<TMP_Text>(view,"comboNumber").text==(GameText.IsEnglish?"Income":"收入"),"top strip is actual order income, not teaching combo");
        string expected="¥"+OrderRate(view.Sim).ToString("0.00",CultureInfo.InvariantCulture)+(GameText.IsEnglish?"/s":"/秒");
        Need(Plain(Get<TMP_Text>(view,"comboMult"))==expected,"top strip displays sum of signed business contract rates: "+expected);
        Need(Get<TMP_Text>(view,"comboTier").text==view.Sim.S.contracts.Count+(GameText.IsEnglish?"":"单"),"compact order count is actual saved signed count");
        Need(view.Sim.IncomePerSecond>OrderRate(view.Sim),"explicit tier fixture distinguishes total household lab rate from order-only header rate");
    }
    static void Fits(TMP_Text label,string description)
    {
        Need(label!=null&&label.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(label.text),description+" exists");label.ForceMeshUpdate(true,true);
        // Evidence only: preserve the same fit requirements and record the actual TMP measurements.
        Checks.Add("Text fit metrics: "+description+"; text="+Plain(label)+"; font="+(label.font==null?"null":label.font.name)+"; size="+label.fontSize.ToString(CultureInfo.InvariantCulture)
            +"; rect="+label.rectTransform.rect.size+"; preferred="+label.GetPreferredValues(label.text)+"; preferredHeight="+label.preferredHeight.ToString(CultureInfo.InvariantCulture)
            +"; scale="+label.rectTransform.lossyScale+"; overflowing="+label.isTextOverflowing+"; truncated="+label.isTextTruncated);
        Need(!label.isTextOverflowing&&!label.isTextTruncated,description+" no overflow, ellipsis or truncation");
        Need(label.preferredHeight<=label.rectTransform.rect.height+1,description+" fits height");
        if(label.textWrappingMode==TextWrappingModes.NoWrap)Need(label.GetPreferredValues(label.text).x<=label.rectTransform.rect.width+1,description+" fits width");
    }
    static void Inside(RectTransform child,RectTransform parent,string why)
    {
        var corners=new Vector3[4];child.GetWorldCorners(corners);
        foreach(var corner in corners){var point=parent.InverseTransformPoint(corner);Need(point.x>=parent.rect.xMin-1&&point.x<=parent.rect.xMax+1&&point.y>=parent.rect.yMin-1&&point.y<=parent.rect.yMax+1,why);}
    }
    static void SmallWindow(ZhongbaoView view)
    {
        var root=Root(view);Inside(root,view.Content,"scaled 1266x763 design stays inside real 960x600 content");
        foreach(string field in new[]{"comboNumber","comboMult","comboTier","hudMoney","hudLevel"})Fits(Get<TMP_Text>(view,field),"small-window business header "+field);
        foreach(string id in ZhongbaoView.PageIds)Fits(Named(root,"Tab "+id).GetComponentInChildren<TMP_Text>(true),"small-window navigation "+id);
        Fits(Get<XgBtn>(view.Contracts,"toTrain").label,"ToTrain actionable copy");
        Fits(Get<TMP_Text>(view.Contracts,"summary"),"business summary");
    }
    static void CardLayout(RectTransform row)
    {
        foreach(string name in new[]{"Accuracy","Money","Bonus"})Fits(row.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==name),"card "+row.name+" "+name);
        Fits(SignButton(row).GetComponentInChildren<TMP_Text>(true),"card "+row.name+" signing/requirement control");
        Inside((RectTransform)SignButton(row).transform,row,"sign button stays in its card");
    }
    static IEnumerable<object> Nav(ZhongbaoView view,string tab,Action<RectTransform> click)
    {
        foreach(var f in Tap(Named(Root(view),"Tab "+tab),click))yield return f;
        var pages=Get<Dictionary<string,XgPage>>(view,"pages");Need(view.Tab==tab&&pages[tab].root.gameObject.activeInHierarchy,"actual nav click reaches "+tab);
        Need(pages.Count(p=>p.Value.root.gameObject.activeInHierarchy)==1,"exactly one business page visible after navigation");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;BookLibraryUiChecks.PrepareInput();
        Need(ctl!=null&&ctl.runtime!=null&&language!=null&&click!=null&&capture!=null,"production owner plus language/LCD/capture callbacks");
        var rt=ctl.runtime;Need(!rt.TestMode&&!rt.useDiskSave&&!rt.tickEnabled,"isolated Main lifecycle, real host, ticks initially disabled");
        Need(LocalLlm.Instance==null||!LocalLlm.Instance.enabled&&!LocalLlm.Instance.Ready,"no model service active");
        var original=rt.Sim;bool oldEnabled=ctl.enabled,oldDisk=rt.useDiskSave,oldTick=rt.tickEnabled,oldEnglish=GameText.IsEnglish,oldLabOn=ctl.Window.isOn;
        var probe=StoryFocus.Probe;var exclusive=StoryFocus.ExclusiveProbe;StoryFocus.Probe=()=>true;StoryFocus.ExclusiveProbe=()=>false;
        var notification=DesktopNotifications.Install(rt);bool notificationEnabled=notification.enabled;notification.enabled=false;
        var ending=ctl.GetComponent<XgEndingPreparationHost>();bool endingEnabled=ending!=null&&ending.enabled;if(ending!=null){ending.Hide();ending.enabled=false;}
        var loans=Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include,FindObjectsSortMode.None);var loanStates=loans.Select(x=>x.enabled).ToArray();foreach(var loan in loans){loan.StopAllCoroutines();loan.enabled=false;}
        ZhongbaoView view=null;Vector2 oldSize=default;bool sizeCaptured=false,completed=false;
        ctl.enabled=false;
        try
        {
            foreach(var f in Until(()=>ZhongbaoHub.Instance?.View!=null,"real business hub installed"))yield return f;
            var hub=ZhongbaoHub.Instance;view=hub.View;oldSize=view.Window.windowContainer.sizeDelta;sizeCaptured=true;
            for(int locale=0;locale<2;locale++)
            {
                bool english=locale==1;string tag="zhongbao-business-"+(english?"en":"zh")+"-960x600";language(english);
                view.Window.CloseWindow();ctl.Window.CloseWindow();Bind(ctl,Fixture(english));
                foreach(var f in Until(()=>hub.Unlocked&&ReferenceEquals(hub.Sim,ctl.Sim),"actual app gate accepts explicit installed fixture"))yield return f;
                foreach(var f in Open(hub,locale==0?null:"contracts"))yield return f;
                Need(view.Tab=="contracts","default/explicit launch opens business orders");
                view.Window.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,960);view.Window.windowContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,600);
                foreach(var f in Settle(6))yield return f;view.Refresh(true);
                Need(ctl.Sim.S.stage==3&&ctl.Sim.S.best.Count==4&&ctl.Sim.S.contracts.Count==0,"declared stage3 deployed-checkpoint fixture, no signed orders injected");
                BusinessSurface(view);string directory=IsolateDisk(rt);Need(ctl.SaveNow()&&rt.SaveSlot(2),"real initial business checkpoint saved to isolated slot2");
                foreach(string locked in new[]{"zipcode","homework"})
                {
                    var row=Row(view,locked);foreach(var f in Reveal(view,row))yield return f;var button=SignButton(row);var contract=XgCatalog.Contract(locked);
                    Need(ctl.Sim.ContractAcc(contract)>=contract.threshold&&!ctl.Sim.CanSign(contract)&&XgSim.ContractRequiredStage(contract)==4,"higher order has enough quality but remains stage4 locked: "+locked);
                    Need(!button.IsInteractable()&&Regex.IsMatch(Plain(button.GetComponentInChildren<TMP_Text>(true)),english?@"(?i)stage\s*4":@"第\s*4\s*阶段"),"higher order states actual stage4 gate instead of false accuracy deficit");
                    double money=ctl.Host.Money;foreach(var f in Tap(button,click,false))yield return f;Need(ctl.Host.Money==money&&!ctl.Sim.Signed(locked),"disabled stage4 card neither signs nor pays");
                }
                capture(tag+"-01-stage-three-gates.png");
                foreach(string id in new[]{"cheque","acrostic","danmaku"})
                {
                    var c=XgCatalog.Contract(id);var row=Row(view,id);foreach(var f in Reveal(view,row))yield return f;var button=SignButton(row);
                    Need(XgSim.ContractRequiredStage(c)==3&&ctl.Sim.CanSign(c)&&ctl.Sim.ContractAcc(c)>=c.threshold,"stage3 contract retains real checkpoint accuracy gate: "+id);
                    int count=ctl.Sim.S.contracts.Count;double before=ctl.Host.Money,total=ctl.Sim.S.totalIncome;
                    foreach(var f in Tap(button,click))yield return f;
                    Need(ctl.Sim.Signed(id)&&ctl.Sim.S.contracts.Count==count+1,"actual sign button adds exactly one order: "+id);
                    Need(Math.Abs(ctl.Host.Money-before-c.signBonus)<1e-8&&Math.Abs(rt.Sim.S.money-ctl.Host.Money)<1e-8&&Math.Abs(ctl.Sim.S.totalIncome-total-c.signBonus)<1e-8,"real shared household wallet and lab income receive exact advance once: "+id);
                    double paid=ctl.Host.Money;string orders=Orders(ctl.Sim);button=SignButton(row);
                    foreach(var f in Tap(button,click,false))yield return f;foreach(var f in Tap(button,click,false))yield return f;
                    Need(Orders(ctl.Sim)==orders&&ctl.Host.Money==paid,"repeated physical pointer on signed disabled control cannot double-pay: "+id);
                    PrepareBusinessWiring(ctl);view.Refresh(true);BusinessSurface(view);capture(tag+"-02-signed-"+id+".png");
                }
                Need(ctl.SaveNow()&&rt.SaveSlot(3),"all actual signings and advances saved through real primary/archive persistence");
                double signedMoney=ctl.Host.Money,rate=OrderRate(ctl.Sim),signedIncome=ctl.Sim.S.totalIncome,signedEarned=rt.Sim.S.totalEarned,signedSeconds=rt.Sim.S.gameSeconds;
                int signedCount=ctl.Sim.S.contracts.Count;string signedIds=Orders(ctl.Sim),best=Best(ctl.Sim);
                var reloadEvidence=new ReloadEvidence();reloadEvidence.probes.Add(Probe(ctl,"before-save-swap-prepared"));
                string evidencePath=Path.Combine(directory,"zhongbao-reload-"+(english?"en":"zh")+".json");File.WriteAllText(evidencePath,JsonUtility.ToJson(reloadEvidence,true));
                var disk=ReadPrimary(directory);var diskLab=new XgSim(JsonUtility.FromJson<XgState>(disk.labState));
                Need(Math.Abs(disk.money-signedMoney)<1e-8&&Orders(diskLab)==signedIds&&Best(diskLab)==best&&IsInstalled(diskLab),"independent primary snapshot contains actual orders, balance, checkpoints and installed fixture");
                // Explicit unrelated in-memory save only. The signed archive is restored through the real loader.
                var unrelated=Fixture(english);unrelated.S.money=17;Bind(ctl,unrelated);foreach(var f in Settle())yield return f;
                Need(ctl.Sim.S.contracts.Count==0&&ctl.Host.Money==17,"temporary different save identity has no signed orders");
                IsolateDisk(rt);Need(rt.LoadArchive(3),"real archive loader restores signed business checkpoint");SetLive(rt);ctl.enabled=false;
                reloadEvidence.probes.Add(Probe(ctl,"after-load-before-wiring-preparation"));File.WriteAllText(evidencePath,JsonUtility.ToJson(reloadEvidence,true));PrepareBusinessWiring(ctl);
                foreach(var f in Open(hub))yield return f;view.Refresh(true);
                reloadEvidence.probes.Add(Probe(ctl,"after-load-prepared"));
                File.WriteAllText(evidencePath,JsonUtility.ToJson(reloadEvidence,true));
                Need(Orders(ctl.Sim)==signedIds&&ctl.Sim.S.contracts.Count==signedCount,"actual reload restores every signed ID and exact signed count without duplicates");
                Need(Best(ctl.Sim)==best,"actual reload preserves exact deployed checkpoint state");
                Need(ctl.Host.Money==signedMoney&&rt.Sim.S.money==signedMoney,"actual reload preserves exact shared-wallet balance without paying advances again");
                Need(ctl.Sim.S.totalIncome==signedIncome&&rt.Sim.S.totalEarned==signedEarned&&rt.Sim.S.gameSeconds==signedSeconds,"actual reload preserves exact income totals and frozen economic time");
                Need(Math.Abs(OrderRate(ctl.Sim)-rate)<1e-8,"actual reload restores the same rate after both owners use the actual powered wiring lifecycle");
                Need(IsInstalled(ctl.Sim),"actual reload preserves the explicit installed-app prerequisite");
                BusinessSurface(view);capture(tag+"-03-reloaded.png");
                // Only this control is an actual cross-app pointer: it promises training, not teaching.
                foreach(var f in Tap(Named(view.Contracts.root,"ToTrain"),click))yield return f;
                foreach(var f in Ready(ctl.Window))yield return f;Need(ctl.View.Tab=="train","actual ToTrain LCD button reaches real exe training page");
                // Legacy route compatibility is explicitly a production router call, not an invented UI click.
                Need(hub.Open("label"),"actual router accepts legacy label request");foreach(var f in Ready(ctl.Window))yield return f;
                Need(ctl.View.Tab=="teach"&&ctl.View.Teach.root.gameObject.activeInHierarchy,"legacy label route opens the exe teaching page");
                Need(view.Label==null,"legacy route never instantiates a business label page");
                ctl.Sim.S.teachingPack="";ctl.Sim.SelectDesk("sense");ctl.View.Refresh(true);ctl.Window.FocusToWindow();
                var brain=ctl.GetComponent<XgBrainWindow>();if(brain!=null&&brain.Visible){click((RectTransform)brain.Window.Find("Title/Close"));foreach(var f in Settle())yield return f;}
                foreach(var f in Settle(5))yield return f;
                // Native games legitimately cover classic cards. Use the visible switch before
                // testing a classic answer; never click through or disable that production surface.
                if(ctl.View.Teach.DeskGameVisible)
                {
                    var deskGame=Get<LingGuangV05.Desktop.XingGuang.MiniGames.XgDeskGamePage>(ctl.View.Teach,"deskGame");
                    foreach(var f in Tap(Get<XgBtn>(deskGame,"classic").button,click))yield return f;
                    Need(!ctl.View.Teach.DeskGameVisible,"actual classic-card switch hides the desk game");
                }
                var teach=ctl.View.Teach;int answers=teach.AnswerCount;long records=ctl.Sim.S.learningRecordSerial;
                string businessBefore=Metrics(view),ordersBefore=Orders(ctl.Sim),checkpointBefore=Best(ctl.Sim);double rateBefore=OrderRate(ctl.Sim);
                var card=ctl.Sim.ReviewCard("sense")??ctl.Sim.Card("sense");bool evaluation=Get<bool>(teach,"shownTeachingEvaluation");bool answer=evaluation?Get<bool>(teach,"shownGuess")==card.truth:card.truth;
                foreach(var f in Tap(Get<XgBtn>(teach,answer?"yes":"no").button,click))yield return f;
                Need(teach.AnswerCount==answers+1&&ctl.Sim.S.learningRecordSerial>records,"actual exe teaching button records one genuine answer");
                // Shared-wallet teaching rewards are allowed; only the business metrics must be invariant.
                foreach(var f in Open(hub))yield return f;view.Refresh(true);foreach(var f in Settle())yield return f;
                Need(Orders(ctl.Sim)==ordersBefore&&Best(ctl.Sim)==checkpointBefore&&Math.Abs(OrderRate(ctl.Sim)-rateBefore)<1e-8&&Metrics(view)==businessBefore,"real teaching answer changes no signed-order/checkpoint/order-income metric");
                BusinessSurface(view);capture(tag+"-04-after-teaching.png");
                foreach(string tab in new[]{"auto","subcontract","ledger","credit","contracts"})
                {foreach(var f in Nav(view,tab,click))yield return f;capture(tag+"-05-route-"+tab+".png");}
                SmallWindow(view);
                foreach(string id in new[]{"cheque","acrostic","danmaku","zipcode","homework"})
                {var row=Row(view,id);foreach(var f in Reveal(view,row))yield return f;CardLayout(row);}
                capture(tag+"-06-layout-checked.png");
                Done((english?"en":"zh")+": stage3 real signing/advance/duplicate controls, isolated primary+archive reload, actual training pointer, legacy teaching route, genuine teaching invariant business metrics, five retained pages and 960x600 layout.");
            }
            completed=true;Done(Scope);Done("Assertions: "+AssertionCount+". Screenshot visual review remains required.");
        }
        finally
        {
            // Capture before restoring any original owner so a failing production control remains reviewable.
            if(!completed)try{capture("zhongbao-business-failure.png");}catch(Exception ex){Checks.Add("Failure capture unavailable: "+ex.Message);}
            if(view!=null){view.Window.CloseWindow();if(sizeCaptured)view.Window.windowContainer.sizeDelta=oldSize;}
            rt.useDiskSave=false;rt.tickEnabled=false;rt.SetSimulationForTests(original);SetLive(rt);language(oldEnglish);ctl.enabled=oldEnabled;
            StoryFocus.Probe=probe;StoryFocus.ExclusiveProbe=exclusive;
            if(notification!=null)notification.enabled=notificationEnabled;
            if(ending!=null)ending.enabled=endingEnabled;
            for(int i=0;i<loans.Length;i++)if(loans[i]!=null)loans[i].enabled=loanStates[i];
            if(oldLabOn)ctl.Window.OpenWindow();else ctl.Window.CloseWindow();rt.useDiskSave=oldDisk;rt.tickEnabled=oldTick;
        }
    }
}
#endif
